using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;

namespace EmbyWeeklyDigest.Plugin
{
    public class NotificationDelivery : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;

        public NotificationDelivery(ISessionManager sessionManager, ILogManager logManager)
        {
            _sessionManager = sessionManager;
            _logger = logManager.GetLogger(nameof(NotificationDelivery));
        }

        public void Run()
        {
            _sessionManager.SessionStarted += OnSessionStarted;
        }

        private async void OnSessionStarted(object sender, SessionEventArgs e)
        {
            var session = e.SessionInfo;
            if (session == null || string.IsNullOrEmpty(session.UserId)) return;

            var store = Plugin.Instance?.Store;
            if (store == null) return;

            // Wait for the client UI to fully initialize after the session is established
            await Task.Delay(10000).ConfigureAwait(false);

            var pending = store.GetActiveUndeliveredFor(session.UserId);
            if (pending.Count == 0) return;

            _logger.Info("EmbyWeeklyDigest: {0} pending digest(s) queued for {1}", pending.Count, session.UserName);

            foreach (var digest in pending)
            {
                try
                {
                    var command = new MessageCommand
                    {
                        Header    = digest.Header,
                        Text      = digest.Text,
                        TimeoutMs = digest.TimeoutMs
                    };

                    await _sessionManager.SendMessageCommand(
                        session.Id, session.Id, command, CancellationToken.None
                    ).ConfigureAwait(false);

                    store.MarkDelivered(digest.Id, session.UserId, session.UserName ?? session.UserId);

                    _logger.Debug("EmbyWeeklyDigest: delivered digest to {0}", session.UserName);
                }
                catch (Exception ex)
                {
                    _logger.Warn("EmbyWeeklyDigest: failed to deliver to {0}: {1}", session.UserName, ex.Message);
                }
            }
        }

        public void Dispose()
        {
            _sessionManager.SessionStarted -= OnSessionStarted;
        }
    }
}
