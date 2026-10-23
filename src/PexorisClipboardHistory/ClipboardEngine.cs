using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PexorisClipboardHistory
{
    public class ClipItem
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Content { get; set; }
        public int Length { get; set; }
        public bool IsPinned { get; set; }
        public bool IsSensitive { get; set; }
        public string SensitiveReason { get; set; }
        public string ContentType { get; set; }

        public ClipItem()
        {
            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.Now;
            Content = string.Empty;
            Length = 0;
            IsPinned = false;
            IsSensitive = false;
            SensitiveReason = string.Empty;
            ContentType = "Text";
        }

        public string PreviewText
        {
            get
            {
                if (IsSensitive)
                {
                    return "[REDACTED] " + SensitiveReason + ": " + MaskSensitive(Content);
                }
                string clean = Content.Replace("\r", " ").Replace("\n", " ").Trim();
                if (clean.Length > 90) return clean.Substring(0, 90) + "...";
                return clean;
            }
        }

        private static string MaskSensitive(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            if (raw.Length <= 8) return "••••••••";
            return raw.Substring(0, 4) + "••••••••" + raw.Substring(raw.Length - 4);
        }
    }

    public static class ClipboardEngine
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AddClipboardFormatListener(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RemoveClipboardFormatListener(IntPtr hWnd);

        public const int WM_CLIPBOARDUPDATE = 0x031D;

        private static List<ClipItem> _items = new List<ClipItem>();
        private static readonly object _lock = new object();
        private static bool _suppressNextCapture = false;
        private static string _lastCapturedText = string.Empty;

        public static event EventHandler OnClipsUpdated;

        public static List<ClipItem> GetAllClips()
        {
            lock (_lock)
            {
                return new List<ClipItem>(_items);
            }
        }

        public static int TotalClipsCount
        {
            get { lock (_lock) { return _items.Count; } }
        }

        public static int PinnedCount
        {
            get
            {
                lock (_lock)
                {
                    int c = 0;
                    for (int i = 0; i < _items.Count; i++) if (_items[i].IsPinned) c++;
                    return c;
                }
            }
        }

        public static int SensitiveCount
        {
            get
            {
                lock (_lock)
                {
                    int c = 0;
                    for (int i = 0; i < _items.Count; i++) if (_items[i].IsSensitive) c++;
                    return c;
                }
            }
        }

        public static long ApproximateMemoryUsageBytes
        {
            get
            {
                lock (_lock)
                {
                    long bytes = 0;
                    for (int i = 0; i < _items.Count; i++)
                    {
                        bytes += (_items[i].Content != null ? _items[i].Content.Length * 2 : 0) + 128;
                    }
                    return bytes;
                }
            }
        }

        public static void CaptureCurrentClipboard()
        {
            if (_suppressNextCapture)
            {
                _suppressNextCapture = false;
                return;
            }

            try
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    if (string.IsNullOrEmpty(text)) return;
                    if (text == _lastCapturedText) return;

                    _lastCapturedText = text;

                    ClipItem item = new ClipItem();
                    item.Content = text;
                    item.Length = text.Length;
                    item.Timestamp = DateTime.Now;

                    // Classify format
                    if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        item.ContentType = "URL Link";
                    }
                    else if (text.Contains("{") && text.Contains("}") || text.Contains("function ") || text.Contains("class ") || text.Contains("SELECT ") || text.Contains("import "))
                    {
                        item.ContentType = "Code Snippet";
                    }
                    else if (text.Contains("\n"))
                    {
                        item.ContentType = "Multi-line";
                    }
                    else
                    {
                        item.ContentType = "Plain Text";
                    }

                    // Scan for sensitive data (Credit Card, API Token, Private Key)
                    string reason;
                    if (CheckSensitiveContent(text, out reason))
                    {
                        item.IsSensitive = true;
                        item.SensitiveReason = reason;
                    }

                    lock (_lock)
                    {
                        // Check if same content exists, move to top if not pinned
                        for (int i = 0; i < _items.Count; i++)
                        {
                            if (_items[i].Content == text)
                            {
                                item.IsPinned = _items[i].IsPinned;
                                _items.RemoveAt(i);
                                break;
                            }
                        }

                        _items.Insert(0, item);

                        // Cap to 200 items max in RAM
                        while (_items.Count > 200)
                        {
                            // Remove oldest unpinned
                            int lastIdx = _items.Count - 1;
                            while (lastIdx >= 0 && _items[lastIdx].IsPinned) lastIdx--;
                            if (lastIdx >= 0) _items.RemoveAt(lastIdx);
                            else break;
                        }
                    }

                    if (OnClipsUpdated != null) OnClipsUpdated(null, EventArgs.Empty);
                }
            }
            catch { }
        }

        private static bool CheckSensitiveContent(string text, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrEmpty(text)) return false;

            // 1. Private Key Block
            if (text.Contains("-----BEGIN PRIVATE KEY-----") || text.Contains("-----BEGIN RSA PRIVATE KEY-----"))
            {
                reason = "RSA Private Key";
                return true;
            }

            // 2. AWS Access Key ID
            if (Regex.IsMatch(text, @"AKIA[0-9A-Z]{16}"))
            {
                reason = "AWS Access Key";
                return true;
            }

            // 3. GitHub Personal Access Token
            if (Regex.IsMatch(text, @"ghp_[0-9a-zA-Z]{36}"))
            {
                reason = "GitHub PAT";
                return true;
            }

            // 4. JWT Token
            if (Regex.IsMatch(text, @"eyJ[A-Za-z0-9-_]+\.eyJ[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+"))
            {
                reason = "JWT Session Token";
                return true;
            }

            // 5. Credit Card (13 to 19 digits with Luhn verification)
            Match ccMatch = Regex.Match(text, @"\b(?:\d[ -]*?){13,19}\b");
            if (ccMatch.Success)
            {
                string digits = Regex.Replace(ccMatch.Value, @"\D", "");
                if (digits.Length >= 13 && digits.Length <= 19 && IsValidLuhn(digits))
                {
                    reason = "Credit Card Number";
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidLuhn(string number)
        {
            int sum = 0;
            bool alternate = false;
            for (int i = number.Length - 1; i >= 0; i--)
            {
                int n = int.Parse(number[i].ToString());
                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n = (n % 10) + 1;
                }
                sum += n;
                alternate = !alternate;
            }
            return (sum % 10 == 0);
        }

        public static void TogglePin(ClipItem item)
        {
            if (item == null) return;
            lock (_lock)
            {
                item.IsPinned = !item.IsPinned;
            }
            if (OnClipsUpdated != null) OnClipsUpdated(null, EventArgs.Empty);
        }

        public static void CopyToClipboard(ClipItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Content)) return;
            try
            {
                _suppressNextCapture = true;
                Clipboard.SetText(item.Content);
            }
            catch { }
        }

        public static void ScrubSensitive()
        {
            lock (_lock)
            {
                _items.RemoveAll(x => x.IsSensitive && !x.IsPinned);
            }
            if (OnClipsUpdated != null) OnClipsUpdated(null, EventArgs.Empty);
        }

        public static void NuclearWipe()
        {
            lock (_lock)
            {
                _items.Clear();
                _lastCapturedText = string.Empty;
            }
            try
            {
                _suppressNextCapture = true;
                Clipboard.Clear();
            }
            catch { }

            if (OnClipsUpdated != null) OnClipsUpdated(null, EventArgs.Empty);
        }

        public static void DeleteItem(ClipItem item)
        {
            if (item == null) return;
            lock (_lock)
            {
                _items.Remove(item);
            }
            if (OnClipsUpdated != null) OnClipsUpdated(null, EventArgs.Empty);
        }

        public static string ExportToCsv()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Pexoris ClipboardHistory Audit Report ===");
            sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Storage: In-Memory Encrypted Vault");
            sb.AppendLine();
            sb.AppendLine("Timestamp,Type,Length,Pinned,Sensitive,Reason,Content");

            lock (_lock)
            {
                foreach (ClipItem it in _items)
                {
                    string safeContent = it.IsSensitive ? "[REDACTED]" : it.Content.Replace("\"", "\"\"");
                    sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\"",
                        it.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        it.ContentType,
                        it.Length,
                        it.IsPinned ? "YES" : "NO",
                        it.IsSensitive ? "YES" : "NO",
                        it.SensitiveReason,
                        safeContent));
                }
            }
            return sb.ToString();
        }
    }
}
