using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace PexorisDuplicateFinder
{
    public class DuplicateFileItem
    {
        public string FullPath { get; set; }
        public string FileName { get; set; }
        public string DirectoryName { get; set; }
        public long SizeBytes { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int GroupId { get; set; }
        public bool IsOriginal { get; set; }
        public string Sha256Hash { get; set; }

        public string FormattedSize
        {
            get { return FormatBytes(SizeBytes); }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0") + " KB";
            if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024.0)).ToString("0.00") + " MB";
            return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("0.00") + " GB";
        }
    }

    public class DuplicateGroup
    {
        public int GroupId { get; set; }
        public long FileSize { get; set; }
        public string Sha256Hash { get; set; }
        public List<DuplicateFileItem> Items { get; set; }

        public DuplicateGroup()
        {
            Items = new List<DuplicateFileItem>();
        }
    }

    public static class DuplicateEngine
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public int wFunc;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pTo;
            public short fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszProgressTitle;
        }

        private const int FO_DELETE = 0x0003;
        private const short FOF_ALLOWUNDO = 0x0040;
        private const short FOF_NOCONFIRMATION = 0x0010;
        private const short FOF_SILENT = 0x0004;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        public static bool DeleteFileSafe(string path, bool sendToRecycleBin)
        {
            try
            {
                if (!File.Exists(path)) return false;

                // Remove read-only attribute if set
                FileAttributes attrs = File.GetAttributes(path);
                if ((attrs & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);
                }

                if (sendToRecycleBin)
                {
                    SHFILEOPSTRUCT shf = new SHFILEOPSTRUCT();
                    shf.wFunc = FO_DELETE;
                    shf.fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT;
                    shf.pFrom = path + '\0' + '\0';
                    int ret = SHFileOperation(ref shf);
                    if (ret == 0 && !shf.fAnyOperationsAborted)
                    {
                        return true;
                    }
                }

                // Direct file delete fallback
                File.Delete(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<DuplicateGroup> ScanDirectory(
            string rootPath,
            long minSizeBytes,
            string searchPattern,
            Action<string, int> progressCallback,
            Func<bool> cancellationCheck)
        {
            List<DuplicateGroup> duplicateGroups = new List<DuplicateGroup>();
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) return duplicateGroups;

            // Tier 1: Group files by exact byte size
            Dictionary<long, List<string>> sizeGroups = new Dictionary<long, List<string>>();
            Queue<string> dirsToVisit = new Queue<string>();
            dirsToVisit.Enqueue(rootPath);

            int filesFound = 0;
            string[] patterns = string.IsNullOrEmpty(searchPattern) ? new string[] { "*.*" } : searchPattern.Split(';');

            while (dirsToVisit.Count > 0)
            {
                if (cancellationCheck != null && cancellationCheck()) return duplicateGroups;

                string currentDir = dirsToVisit.Dequeue();
                if (progressCallback != null)
                {
                    progressCallback(currentDir, filesFound);
                }

                try
                {
                    // Enqueue subdirectories
                    foreach (string subDir in Directory.GetDirectories(currentDir))
                    {
                        try
                        {
                            DirectoryInfo di = new DirectoryInfo(subDir);
                            // Skip reparse points / symlinks and hidden system volume info
                            if ((di.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                            if (di.Name.Equals("$Recycle.Bin", StringComparison.OrdinalIgnoreCase)) continue;
                            if (di.Name.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase)) continue;

                            dirsToVisit.Enqueue(subDir);
                        }
                        catch { }
                    }
                }
                catch { }

                // Gather files in directory matching filters
                for (int p = 0; p < patterns.Length; p++)
                {
                    string pat = patterns[p].Trim();
                    if (string.IsNullOrEmpty(pat)) continue;

                    string[] files = null;
                    try
                    {
                        files = Directory.GetFiles(currentDir, pat);
                    }
                    catch { continue; }

                    if (files == null) continue;

                    for (int f = 0; f < files.Length; f++)
                    {
                        string filePath = files[f];
                        try
                        {
                            FileInfo fi = new FileInfo(filePath);
                            long len = fi.Length;
                            if (len < minSizeBytes || len == 0) continue;

                            filesFound++;
                            List<string> list;
                            if (!sizeGroups.TryGetValue(len, out list))
                            {
                                list = new List<string>();
                                sizeGroups[len] = list;
                            }
                            list.Add(filePath);
                        }
                        catch { }
                    }
                }
            }

            // Tier 2 & Tier 3: Verify potential duplicates using 4KB header & full SHA-256
            int currentGroupIdx = 1;

            foreach (KeyValuePair<long, List<string>> pair in sizeGroups)
            {
                if (cancellationCheck != null && cancellationCheck()) return duplicateGroups;
                if (pair.Value.Count < 2) continue; // Size is unique; cannot be duplicate

                // Sub-group by partial 4KB hash
                Dictionary<string, List<string>> partialHashGroups = new Dictionary<string, List<string>>();
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    string path = pair.Value[i];
                    string pHash = ComputePartialHash(path);
                    if (string.IsNullOrEmpty(pHash)) continue;

                    List<string> subList;
                    if (!partialHashGroups.TryGetValue(pHash, out subList))
                    {
                        subList = new List<string>();
                        partialHashGroups[pHash] = subList;
                    }
                    subList.Add(path);
                }

                // For matching partial hashes, compute full SHA-256
                foreach (KeyValuePair<string, List<string>> subPair in partialHashGroups)
                {
                    if (cancellationCheck != null && cancellationCheck()) return duplicateGroups;
                    if (subPair.Value.Count < 2) continue;

                    Dictionary<string, List<string>> fullHashGroups = new Dictionary<string, List<string>>();
                    for (int j = 0; j < subPair.Value.Count; j++)
                    {
                        string path = subPair.Value[j];
                        string fHash = ComputeFullSha256(path);
                        if (string.IsNullOrEmpty(fHash)) continue;

                        List<string> fullList;
                        if (!fullHashGroups.TryGetValue(fHash, out fullList))
                        {
                            fullList = new List<string>();
                            fullHashGroups[fHash] = fullList;
                        }
                        fullList.Add(path);
                    }

                    // Create confirmed duplicate groups
                    foreach (KeyValuePair<string, List<string>> finalGroup in fullHashGroups)
                    {
                        if (finalGroup.Value.Count < 2) continue;

                        DuplicateGroup grp = new DuplicateGroup();
                        grp.GroupId = currentGroupIdx++;
                        grp.FileSize = pair.Key;
                        grp.Sha256Hash = finalGroup.Key;

                        // Sort by modified date so oldest file is first
                        List<DuplicateFileItem> items = new List<DuplicateFileItem>();
                        for (int k = 0; k < finalGroup.Value.Count; k++)
                        {
                            string p = finalGroup.Value[k];
                            try
                            {
                                FileInfo fi = new FileInfo(p);
                                DuplicateFileItem itm = new DuplicateFileItem();
                                itm.FullPath = p;
                                itm.FileName = fi.Name;
                                itm.DirectoryName = fi.DirectoryName;
                                itm.SizeBytes = fi.Length;
                                itm.ModifiedDate = fi.LastWriteTime;
                                itm.GroupId = grp.GroupId;
                                itm.Sha256Hash = grp.Sha256Hash;
                                itm.IsOriginal = false;
                                items.Add(itm);
                            }
                            catch { }
                        }

                        if (items.Count >= 2)
                        {
                            // Mark the earliest created or modified file as Original
                            items.Sort(delegate (DuplicateFileItem a, DuplicateFileItem b)
                            {
                                return a.ModifiedDate.CompareTo(b.ModifiedDate);
                            });

                            items[0].IsOriginal = true;
                            grp.Items = items;
                            duplicateGroups.Add(grp);
                        }
                    }
                }
            }

            return duplicateGroups;
        }

        private static string ComputePartialHash(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buffer = new byte[Math.Min(4096, fs.Length)];
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);
                    using (MD5 md5 = MD5.Create())
                    {
                        byte[] hash = md5.ComputeHash(buffer, 0, bytesRead);
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < hash.Length; i++)
                        {
                            sb.Append(hash[i].ToString("x2"));
                        }
                        return sb.ToString();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static string ComputeFullSha256(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(fs);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++)
                    {
                        sb.Append(hash[i].ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
