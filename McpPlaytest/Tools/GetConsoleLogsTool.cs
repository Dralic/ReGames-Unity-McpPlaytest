using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace McpPlaytest
{
    public class GetConsoleLogsTool : PlaytestToolBase
    {
        private static readonly List<LogEntry> _logBuffer = new List<LogEntry>();
        private static readonly object _logLock = new object();
        private const int MAX_LOG_ENTRIES = 500;
        private static bool _isSubscribed = false;

        private struct LogEntry
        {
            public string message;
            public string stackTrace;
            public LogType type;
            public string timestamp;
        }

        public GetConsoleLogsTool()
        {
            this.Name = "get_playtest_logs";
            this.Description = "Get recent Unity console log entries during Play Mode";
            EnsureSubscribed();
        }

        private static void EnsureSubscribed()
        {
            if (_isSubscribed) return;
            Application.logMessageReceived += OnLogMessageReceived;
            _isSubscribed = true;
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            lock (_logLock)
            {
                _logBuffer.Add(new LogEntry
                {
                    message = condition,
                    stackTrace = stackTrace,
                    type = type,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                });

                while (_logBuffer.Count > MAX_LOG_ENTRIES)
                {
                    _logBuffer.RemoveAt(0);
                }
            }
        }

        public override JObject Execute(JObject parameters)
        {
            int count = parameters["count"]?.ToObject<int>() ?? 50;
            var filter = parameters["filter"]?.ToString() ?? "all";

            var logsArray = new JArray();

            lock (_logLock)
            {
                int startIndex = Math.Max(0, _logBuffer.Count - count);

                for (int i = _logBuffer.Count - 1; i >= startIndex; i--)
                {
                    var entry = _logBuffer[i];

                    if (filter != "all")
                    {
                        string typeStr = this.LogTypeToFilterString(entry.type);
                        if (typeStr != filter) continue;
                    }

                    logsArray.Add(new JObject
                    {
                        ["message"] = entry.message,
                        ["stackTrace"] = entry.stackTrace,
                        ["type"] = entry.type.ToString(),
                        ["timestamp"] = entry.timestamp
                    });

                    if (logsArray.Count >= count) break;
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["logs"] = logsArray,
                ["totalBuffered"] = _logBuffer.Count
            };
        }

        private string LogTypeToFilterString(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return "error";
                case LogType.Warning:
                    return "warning";
                case LogType.Log:
                    return "log";
                default:
                    return "log";
            }
        }
    }
}
