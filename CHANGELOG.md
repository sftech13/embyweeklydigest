# Changelog

All notable changes to EmbyWeeklyDigest are listed here, newest first.

---

## v1.2.0

- The 5-year TV-only cutoff is now a current-year-and-previous-year window applied to both movies and TV shows, to keep the popup short enough to read on clients that can't scroll it (some TVs/apps)
- Fixed items with no `ProductionYear` metadata slipping through the year filter regardless of age — the year is now parsed from the title as a fallback when the field is missing
- Fixed a duplicated year still appearing when the underlying title metadata itself already contained the year twice (e.g. `Title (2026) (2026)`)

## v1.1.0

- Title formatting fixes: no more duplicated year (`Title (2011) (2011)` → `Title (2011)`), and HTML entities in stored metadata (e.g. `&amp;`) are now decoded before display
- Titles now show community rating when available, e.g. `Movie Title (2026) - 7.4/10`
- Movies and TV shows are sorted by release year, newest first
- TV shows older than 5 years are excluded by default, to keep the digest focused on what's new-to-you rather than old library scans
- Plugin icon added (shown in Dashboard → Plugins)

## v1.0.0

Initial release.

- **Weekly Digest scheduled task** — appears under Dashboard → Scheduled Tasks → EmbyWeeklyDigest. Default trigger is Friday 6:00 PM; change the day/time or run it on demand from Emby's own Scheduled Tasks page
- Builds a popup listing every movie and every brand-new TV series added to the library in the last 7 days (new episodes of existing shows are not included)
- Sends the digest as a popup (`MessageCommand`) to all active Emby sessions
- **Deferred delivery** — offline users automatically receive the digest the next time they log in, via the same store/replay mechanism as EmbyNotify
- Config page with:
  - Popup header text
  - Toggle to include movies / TV shows independently
  - Toggle to skip sending entirely when nothing new was added
  - Auto-dismiss timeout (0 = stays until dismissed)
  - **Send Test Digest** button with a configurable lookback window, for trying it out without waiting for the scheduled run
  - Digest history with per-user delivery badges and a Dismiss control
- `POST /EmbyWeeklyDigest/SendNow`, `GET /EmbyWeeklyDigest/Digests`, `DELETE /EmbyWeeklyDigest/Digests/{id}` API endpoints (admin auth required)
