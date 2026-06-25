using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

namespace EmbyWeeklyDigest.Plugin.ScheduledTasks
{
    public class WeeklyDigestTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger _logger;

        public WeeklyDigestTask(ILogManager logManager)
            => _logger = logManager.GetLogger(nameof(WeeklyDigestTask));

        public string Name        => "Send Weekly Digest";
        public string Description => "Sends a popup listing movies and TV shows added in the last 7 days.";
        public string Category    => "EmbyWeeklyDigest";
        public string Key         => "EmbyWeeklyDigestSend";

        public bool IsHidden  => false;
        public bool IsEnabled => true;
        public bool IsLogged  => true;

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerWeekly,
                DayOfWeek = DayOfWeek.Friday,
                TimeOfDayTicks = TimeSpan.FromHours(18).Ticks
            };
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            progress.Report(0);
            var result = await Plugin.Instance.SendDigestAsync(7).ConfigureAwait(false);
            if (result.Error != null)
                _logger.Error("EmbyWeeklyDigest: scheduled run failed: {0}", result.Error);
            else
                _logger.Info("EmbyWeeklyDigest: scheduled run — {0}", result.Message);
            progress.Report(100);
        }
    }
}
