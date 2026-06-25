using MediaBrowser.Model.Plugins;

namespace EmbyWeeklyDigest.Plugin.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string Header { get; set; } = "What's New This Week";
        public int TimeoutMs { get; set; } = 0;
        public bool IncludeMovies { get; set; } = true;
        public bool IncludeSeries { get; set; } = true;
        public bool SkipWhenEmpty { get; set; } = true;
    }
}
