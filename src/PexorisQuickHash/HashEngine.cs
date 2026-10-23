using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PexorisQuickHash
{
    public class HashResult
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long FileSizeBytes { get; set; }
        public string FormattedSize { get; set; }
        public string MD5 { get; set; }
        public string SHA1 { get; set; }
        public string SHA256 { get; set; }
        public string SHA384 { get; set; }
        public string SHA512 { get; set; }
        public string CRC32 { get; set; }
        public long ElapsedMs { get; set; }

        public HashResult()
        {
            FilePath = string.Empty;
            FileName = string.Empty;
            FormattedSize = "0 B";
            MD5 = string.Empty;
            SHA1 = string.Empty;
            SHA256 = string.Empty;
            SHA384 = string.Empty;
            SHA512 = string.Empty;
            CRC32 = string.Empty;
        }

        public string GetHashByAlgorithm(string algo)
        {
            if (string.IsNullOrEmpty(algo)) return string.Empty;
            string key = algo.Trim().ToUpperInvariant();
            if (key == "MD5") return MD5;
            if (key == "SHA-1" || key == "SHA1") return SHA1;
            if (key == "SHA-256" || key == "SHA256") return SHA256;
            if (key == "SHA-384" || key == "SHA384") return SHA384;
            if (key == "SHA-512" || key == "SHA512") return SHA512;
            if (key == "CRC32" || key == "CRC-32") return CRC32;
            return string.Empty;
        }
    }

    public static class Crc32
    {
        private static readonly uint[] Table;

        static Crc32()
        {
            const uint poly = 0xedb88320;
            Table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 8; j > 0; j--)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ poly;
                    else
                        crc >>= 1;
                }
                Table[i] = crc;
            }
        }

        public static uint Update(uint currentCrc, byte[] buffer, int offset, int count)
        {
            uint crc = ~currentCrc;
            for (int i = offset; i < offset + count; i++)
            {
                crc = Table[(crc & 0xff) ^ buffer[i]] ^ (crc >> 8);
            }
            return ~crc;
        }
    }

    public static class HashEngine
    {
        public static HashResult ComputeAllHashes(string filePath, Action<int> progressCallback, bool lowercase)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            FileInfo fi = new FileInfo(filePath);
            HashResult res = new HashResult();
            res.FilePath = filePath;
            res.FileName = fi.Name;
            res.FileSizeBytes = fi.Length;
            res.FormattedSize = FormatBytes(fi.Length);

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            using (MD5 md5 = MD5.Create())
            using (SHA1 sha1 = SHA1.Create())
            using (SHA256 sha256 = SHA256.Create())
            using (SHA384 sha384 = SHA384.Create())
            using (SHA512 sha512 = SHA512.Create())
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536))
            {
                byte[] buffer = new byte[65536];
                long totalRead = 0;
                long totalLength = fi.Length;
                uint crc = 0;

                int bytesRead;
                int lastReportedPercent = -1;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    totalRead += bytesRead;

                    // Update streaming crypto hashes
                    md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                    sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                    sha384.TransformBlock(buffer, 0, bytesRead, null, 0);
                    sha512.TransformBlock(buffer, 0, bytesRead, null, 0);

                    // Update CRC32
                    crc = Crc32.Update(crc, buffer, 0, bytesRead);

                    if (totalLength > 0 && progressCallback != null)
                    {
                        int pct = (int)((totalRead * 100) / totalLength);
                        if (pct != lastReportedPercent)
                        {
                            lastReportedPercent = pct;
                            progressCallback(pct);
                        }
                    }
                }

                // Finalize crypto blocks
                byte[] empty = new byte[0];
                md5.TransformFinalBlock(empty, 0, 0);
                sha1.TransformFinalBlock(empty, 0, 0);
                sha256.TransformFinalBlock(empty, 0, 0);
                sha384.TransformFinalBlock(empty, 0, 0);
                sha512.TransformFinalBlock(empty, 0, 0);

                res.MD5 = ToHexString(md5.Hash, lowercase);
                res.SHA1 = ToHexString(sha1.Hash, lowercase);
                res.SHA256 = ToHexString(sha256.Hash, lowercase);
                res.SHA384 = ToHexString(sha384.Hash, lowercase);
                res.SHA512 = ToHexString(sha512.Hash, lowercase);
                res.CRC32 = crc.ToString(lowercase ? "x8" : "X8");
            }

            sw.Stop();
            res.ElapsedMs = sw.ElapsedMilliseconds;
            return res;
        }

        public static string DetectAlgorithm(string hashText)
        {
            if (string.IsNullOrEmpty(hashText)) return string.Empty;
            string clean = hashText.Trim().Replace(" ", "").Replace("-", "");
            int len = clean.Length;
            if (len == 8) return "CRC32";
            if (len == 32) return "MD5";
            if (len == 40) return "SHA-1";
            if (len == 64) return "SHA-256";
            if (len == 96) return "SHA-384";
            if (len == 128) return "SHA-512";
            return string.Empty;
        }

        public static bool CompareHash(string candidate, HashResult result, out string matchedAlgo)
        {
            matchedAlgo = string.Empty;
            if (string.IsNullOrEmpty(candidate) || result == null) return false;

            string clean = candidate.Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "");
            if (string.IsNullOrEmpty(clean)) return false;

            if (result.MD5.ToLowerInvariant() == clean) { matchedAlgo = "MD5"; return true; }
            if (result.SHA1.ToLowerInvariant() == clean) { matchedAlgo = "SHA-1"; return true; }
            if (result.SHA256.ToLowerInvariant() == clean) { matchedAlgo = "SHA-256"; return true; }
            if (result.SHA384.ToLowerInvariant() == clean) { matchedAlgo = "SHA-384"; return true; }
            if (result.SHA512.ToLowerInvariant() == clean) { matchedAlgo = "SHA-512"; return true; }
            if (result.CRC32.ToLowerInvariant() == clean) { matchedAlgo = "CRC32"; return true; }

            return false;
        }

        public static string ExportToTextReport(HashResult result)
        {
            if (result == null) return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine(" Pexoris QuickHash • File Integrity & Checksum Report");
            sb.AppendLine(" Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("================================================================================");
            sb.AppendLine("File Name:   " + result.FileName);
            sb.AppendLine("File Path:   " + result.FilePath);
            sb.AppendLine("File Size:   " + result.FormattedSize + " (" + result.FileSizeBytes.ToString("N0") + " bytes)");
            sb.AppendLine("Time Taken:  " + result.ElapsedMs + " ms");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("CRC32:       " + result.CRC32);
            sb.AppendLine("MD5:         " + result.MD5);
            sb.AppendLine("SHA-1:       " + result.SHA1);
            sb.AppendLine("SHA-256:     " + result.SHA256);
            sb.AppendLine("SHA-384:     " + result.SHA384);
            sb.AppendLine("SHA-512:     " + result.SHA512);
            sb.AppendLine("================================================================================");
            sb.AppendLine("Verified with Pexoris QuickHash • https://pexoris.com/tools/quickhash/");
            return sb.ToString();
        }

        public static string ExportToCsv(List<HashResult> results)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("FileName,FilePath,SizeBytes,CRC32,MD5,SHA1,SHA256,SHA384,SHA512");
            foreach (HashResult r in results)
            {
                sb.AppendLine(string.Format("\"{0}\",\"{1}\",{2},{3},{4},{5},{6},{7},{8}",
                    EscapeCsv(r.FileName),
                    EscapeCsv(r.FilePath),
                    r.FileSizeBytes,
                    r.CRC32,
                    r.MD5,
                    r.SHA1,
                    r.SHA256,
                    r.SHA384,
                    r.SHA512));
            }
            return sb.ToString();
        }

        private static string EscapeCsv(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\"", "\"\"");
        }

        public static string ToHexString(byte[] bytes, bool lowercase)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            string format = lowercase ? "x2" : "X2";
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString(format));
            }
            return sb.ToString();
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024.0)).ToString("F2") + " MB";
            return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("F2") + " GB";
        }
    }
}
