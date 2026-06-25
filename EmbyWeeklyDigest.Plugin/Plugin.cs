using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmbyWeeklyDigest.Plugin.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;

namespace EmbyWeeklyDigest.Plugin
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        private readonly IServerApplicationHost _applicationHost;
        private readonly ILogger _logger;

        public static Plugin Instance { get; private set; }
        public NotificationStore Store { get; private set; }

        public Plugin(
            IApplicationPaths appPaths,
            IXmlSerializer xmlSerializer,
            IServerApplicationHost applicationHost,
            ILogManager logManager)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
            _applicationHost = applicationHost;
            _logger = logManager.GetLogger(nameof(Plugin));
            Store = new NotificationStore(appPaths, logManager);
        }

        public override string Name => "EmbyWeeklyDigest";
        public override Guid Id => new Guid("c203ed6a-462b-40a6-87fa-ef58535a490d");
        public override string Description => "Sends a weekly popup digest of movies and TV shows added in the last 7 days.";

        public Stream GetThumbImage() =>
            GetType().Assembly.GetManifestResourceStream("EmbyWeeklyDigest.Plugin.thumb.png");

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "EmbyWeeklyDigest",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.Web.config.html",
                    IsMainConfigPage = true
                },
                new PluginPageInfo
                {
                    Name = "embyweeklydigestconfig",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.Web.config.js"
                }
            };
        }

        internal async Task<DigestSendResult> SendDigestAsync(int lookbackDays)
        {
            var result = new DigestSendResult();
            try
            {
                var libraryManager = _applicationHost.Resolve<ILibraryManager>();
                var sessionManager = _applicationHost.Resolve<ISessionManager>();
                if (libraryManager == null || sessionManager == null)
                {
                    result.Error = "Required services not available";
                    return result;
                }

                var config = Configuration;
                var digest = DigestBuilder.BuildSinceDays(libraryManager, lookbackDays, config.IncludeMovies, config.IncludeSeries);
                result.MovieCount = digest.Movies.Count;
                result.SeriesCount = digest.Series.Count;

                if (digest.IsEmpty)
                {
                    result.Skipped = true;
                    result.Message = "No new movies or TV shows in the last " + lookbackDays + " day(s); nothing sent.";
                    if (config.SkipWhenEmpty) return result;
                }

                var text = digest.IsEmpty ? "No new movies or TV shows this week." : DigestBuilder.ToMessageText(digest);
                var header = string.IsNullOrWhiteSpace(config.Header) ? "What's New This Week" : config.Header;

                PendingDigest pending = null;
                try
                {
                    pending = Store.Add(header, text, config.TimeoutMs);
                    result.DigestId = pending.Id;
                }
                catch (Exception ex)
                {
                    _logger.Warn("EmbyWeeklyDigest: notification store unavailable, continuing send: {0}", ex.Message);
                }

                var command = new MessageCommand
                {
                    Header    = header,
                    Text      = text,
                    TimeoutMs = config.TimeoutMs
                };

                var sessions = sessionManager.Sessions;
                if (sessions != null)
                {
                    foreach (var session in sessions)
                    {
                        try
                        {
                            await sessionManager.SendMessageCommand(session.Id, session.Id, command, CancellationToken.None).ConfigureAwait(false);
                            result.SessionsMessaged++;
                            if (pending != null && !string.IsNullOrEmpty(session.UserId))
                            {
                                try { Store.MarkDelivered(pending.Id, session.UserId, session.UserName ?? session.UserId); }
                                catch { }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug("EmbyWeeklyDigest: failed to message session {0}: {1}", session.Id, ex.Message);
                            result.SessionsFailed++;
                        }
                    }
                }

                result.Message = $"Digest sent ({digest.Movies.Count} movie(s), {digest.Series.Count} show(s)) to {result.SessionsMessaged} session(s).";
            }
            catch (Exception ex)
            {
                _logger.Error("EmbyWeeklyDigest SendDigestAsync error: {0}", ex.Message);
                result.Error = ex.Message;
            }

            return result;
        }
    }

    public class DigestSendResult
    {
        public int SessionsMessaged { get; set; }
        public int SessionsFailed { get; set; }
        public int MovieCount { get; set; }
        public int SeriesCount { get; set; }
        public bool Skipped { get; set; }
        public string DigestId { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }
}
