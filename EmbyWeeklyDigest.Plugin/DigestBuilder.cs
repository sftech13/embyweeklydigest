using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace EmbyWeeklyDigest.Plugin
{
    public class DigestResult
    {
        public List<string> Movies { get; } = new List<string>();
        public List<string> Series { get; } = new List<string>();
        public bool IsEmpty => Movies.Count == 0 && Series.Count == 0;
    }

    public static class DigestBuilder
    {
        public static DigestResult BuildSinceDays(ILibraryManager libraryManager, int days, bool includeMovies, bool includeSeries)
        {
            var result = new DigestResult();
            var cutoff = DateTimeOffset.UtcNow.AddDays(-days);
            var minYear = DateTime.UtcNow.Year - 1;

            if (includeMovies)
            {
                var movies = libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { "Movie" },
                    Recursive = true,
                    IsVirtualItem = false,
                    MinDateCreated = cutoff,
                    OrderBy = new[] { (ItemSortBy.ProductionYear, SortOrder.Descending), (ItemSortBy.DateCreated, SortOrder.Descending) }
                });

                foreach (var item in movies)
                {
                    if (EffectiveYear(item) is int y && y < minYear)
                        continue;

                    result.Movies.Add(FormatTitle(item.Name, item.ProductionYear, item.CommunityRating));
                }
            }

            if (includeSeries)
            {
                var series = libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { "Series" },
                    Recursive = true,
                    IsVirtualItem = false,
                    MinDateCreated = cutoff,
                    OrderBy = new[] { (ItemSortBy.ProductionYear, SortOrder.Descending), (ItemSortBy.DateCreated, SortOrder.Descending) }
                });

                foreach (var item in series)
                {
                    if (EffectiveYear(item) is int y && y < minYear)
                        continue;

                    result.Series.Add(FormatTitle(item.Name, item.ProductionYear, item.CommunityRating));
                }
            }

            return result;
        }

        private static readonly Regex YearSuffixPattern = new Regex(@"\(\d{4}\)");
        private static readonly Regex TrailingYearPattern = new Regex(@"\((\d{4})\)\s*$");
        private static readonly Regex DuplicateYearPattern = new Regex(@"(\(\d{4}\))\s*\1");

        private static int? EffectiveYear(BaseItem item)
        {
            if (item.ProductionYear.HasValue) return item.ProductionYear.Value;
            var match = TrailingYearPattern.Match(item.Name ?? string.Empty);
            return match.Success ? int.Parse(match.Groups[1].Value) : (int?)null;
        }

        private static string FormatTitle(string rawName, int? year, float? communityRating)
        {
            var name = DuplicateYearPattern.Replace(System.Net.WebUtility.HtmlDecode(rawName), "$1");
            var titleWithYear = year.HasValue && year.Value > 0 && !YearSuffixPattern.IsMatch(name)
                ? $"{name} ({year.Value})"
                : name;

            return communityRating.HasValue && communityRating.Value > 0
                ? $"{titleWithYear} - {communityRating.Value:0.0}/10"
                : titleWithYear;
        }

        public static string ToMessageText(DigestResult digest)
        {
            var sb = new StringBuilder();

            if (digest.Movies.Count > 0)
            {
                sb.AppendLine("New Movies:");
                foreach (var title in digest.Movies)
                    sb.AppendLine("• " + title);
            }

            if (digest.Series.Count > 0)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine("New TV Shows:");
                foreach (var title in digest.Series)
                    sb.AppendLine("• " + title);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
