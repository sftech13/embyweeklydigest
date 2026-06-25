using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;

namespace EmbyWeeklyDigest.Plugin
{
    public class DeliveryRecord
    {
        public string Username { get; set; }
        public DateTime DeliveredAt { get; set; }
    }

    public class PendingDigest
    {
        public string Id { get; set; }
        public string Header { get; set; }
        public string Text { get; set; }
        public int TimeoutMs { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Active { get; set; }
        public Dictionary<string, DeliveryRecord> Deliveries { get; set; }

        public PendingDigest()
        {
            Id = Guid.NewGuid().ToString("N");
            CreatedAt = DateTime.UtcNow;
            Active = true;
            Deliveries = new Dictionary<string, DeliveryRecord>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class NotificationStore
    {
        private readonly string _filePath;
        private readonly ILogger _logger;
        private readonly object _lock = new object();
        private List<PendingDigest> _digests;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public NotificationStore(IApplicationPaths appPaths, ILogManager logManager)
        {
            _filePath = Path.Combine(appPaths.DataPath, "embyweeklydigest-notifications.json");
            _logger = logManager.GetLogger(nameof(NotificationStore));
            _digests = Load();
        }

        private List<PendingDigest> Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<List<PendingDigest>>(json, _jsonOpts)
                           ?? new List<PendingDigest>();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("EmbyWeeklyDigest: failed to load notification store: {0}", ex.Message);
            }
            return new List<PendingDigest>();
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_digests, _jsonOpts);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.Error("EmbyWeeklyDigest: failed to save notification store: {0}", ex.Message);
            }
        }

        public PendingDigest Add(string header, string text, int timeoutMs)
        {
            var digest = new PendingDigest
            {
                Header    = header,
                Text      = text,
                TimeoutMs = timeoutMs
            };
            lock (_lock)
            {
                _digests.Insert(0, digest);
                if (_digests.Count > 50)
                    _digests.RemoveRange(50, _digests.Count - 50);
                Save();
            }
            return digest;
        }

        public void MarkDelivered(string digestId, string userId, string username)
        {
            lock (_lock)
            {
                var d = _digests.Find(x => x.Id == digestId);
                if (d == null) return;
                d.Deliveries[userId] = new DeliveryRecord
                {
                    Username    = username ?? userId,
                    DeliveredAt = DateTime.UtcNow
                };
                Save();
            }
        }

        public void Dismiss(string digestId)
        {
            lock (_lock)
            {
                var d = _digests.Find(x => x.Id == digestId);
                if (d == null) return;
                d.Active = false;
                Save();
            }
        }

        public List<PendingDigest> GetAll()
        {
            lock (_lock)
                return new List<PendingDigest>(_digests);
        }

        public List<PendingDigest> GetActiveUndeliveredFor(string userId)
        {
            lock (_lock)
                return _digests.FindAll(d =>
                    d.Active && !d.Deliveries.ContainsKey(userId));
        }
    }
}
