using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace PexorisExifStripper
{
    public class ImageMetadataInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public long FileSize { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MetadataTagCount { get; set; }
        public bool HasGps { get; set; }
        public string GpsCoordinates { get; set; }
        public string CameraModel { get; set; }
        public string DateTaken { get; set; }
        public string Software { get; set; }
        public string Status { get; set; }
        public List<string> DetailedTags { get; set; }

        public ImageMetadataInfo()
        {
            DetailedTags = new List<string>();
            Status = "Pending";
        }
    }

    public static class ExifHelper
    {
        private static readonly HashSet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp", ".webp"
        };

        public static bool IsSupportedImage(string path)
        {
            string ext = Path.GetExtension(path);
            return !string.IsNullOrEmpty(ext) && SupportedExtensions.Contains(ext);
        }

        public static ImageMetadataInfo InspectFile(string filePath)
        {
            var info = new ImageMetadataInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Extension = Path.GetExtension(filePath).ToUpperInvariant(),
                Status = "Pending"
            };

            try
            {
                var fi = new FileInfo(filePath);
                info.FileSize = fi.Length;

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var img = Image.FromStream(fs, false, false))
                    {
                        info.Width = img.Width;
                        info.Height = img.Height;
                        info.MetadataTagCount = img.PropertyIdList != null ? img.PropertyIdList.Length : 0;

                        if (info.MetadataTagCount > 0)
                        {
                            ExtractTags(img, info);
                        }
                        else
                        {
                            info.GpsCoordinates = "None";
                            info.CameraModel = "None";
                            info.DateTaken = "None";
                            info.Status = "Clean (No Tags)";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                info.Status = "Read Error: " + ex.Message;
                info.GpsCoordinates = "Error";
                info.CameraModel = "Error";
                info.DateTaken = "Error";
            }

            return info;
        }

        private static void ExtractTags(Image img, ImageMetadataInfo info)
        {
            string make = null;
            string model = null;
            string dateTaken = null;
            string software = null;
            string artist = null;

            // GPS
            string latRef = null;
            double? lat = null;
            string lonRef = null;
            double? lon = null;

            foreach (var prop in img.PropertyItems)
            {
                try
                {
                    switch (prop.Id)
                    {
                        case 0x010F: // Camera Make
                            make = GetString(prop).Trim();
                            info.DetailedTags.Add("Camera Make: " + make);
                            break;
                        case 0x0110: // Camera Model
                            model = GetString(prop).Trim();
                            info.DetailedTags.Add("Camera Model: " + model);
                            break;
                        case 0x0131: // Software
                            software = GetString(prop).Trim();
                            info.DetailedTags.Add("Software: " + software);
                            break;
                        case 0x013B: // Artist
                            artist = GetString(prop).Trim();
                            info.DetailedTags.Add("Artist: " + artist);
                            break;
                        case 0x9003: // Date Taken
                        case 0x0132: // Modify Date
                            if (dateTaken == null)
                            {
                                dateTaken = GetString(prop).Trim();
                                info.DetailedTags.Add("Date/Time: " + dateTaken);
                            }
                            break;
                        case 0x0001: // GPS Lat Ref
                            latRef = GetString(prop).Trim();
                            break;
                        case 0x0002: // GPS Lat
                            lat = ParseGpsCoordinate(prop);
                            break;
                        case 0x0003: // GPS Lon Ref
                            lonRef = GetString(prop).Trim();
                            break;
                        case 0x0004: // GPS Lon
                            lon = ParseGpsCoordinate(prop);
                            break;
                        case 0x0112: // Orientation
                            int orientation = BitConverter.ToUInt16(prop.Value, 0);
                            info.DetailedTags.Add("Orientation: " + orientation);
                            break;
                        default:
                            info.DetailedTags.Add(string.Format("EXIF Tag 0x{0:X4} ({1} bytes)", prop.Id, prop.Len));
                            break;
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(make) || !string.IsNullOrEmpty(model))
            {
                if (!string.IsNullOrEmpty(make) && !string.IsNullOrEmpty(model) && !model.StartsWith(make, StringComparison.OrdinalIgnoreCase))
                    info.CameraModel = make + " " + model;
                else
                    info.CameraModel = !string.IsNullOrEmpty(model) ? model : make;
            }
            else
            {
                info.CameraModel = "None";
            }

            info.DateTaken = !string.IsNullOrEmpty(dateTaken) ? dateTaken : "None";
            info.Software = !string.IsNullOrEmpty(software) ? software : "None";

            if (lat.HasValue && lon.HasValue)
            {
                info.HasGps = true;
                string ns = !string.IsNullOrEmpty(latRef) ? latRef : "N";
                string ew = !string.IsNullOrEmpty(lonRef) ? lonRef : "E";
                info.GpsCoordinates = string.Format("{0:0.0000}° {1}, {2:0.0000}° {3}", lat.Value, ns, lon.Value, ew);
                info.DetailedTags.Add("GPS Location: " + info.GpsCoordinates);
            }
            else
            {
                info.HasGps = false;
                info.GpsCoordinates = "None";
            }

            if (info.MetadataTagCount > 0)
                info.Status = info.HasGps ? "Contains GPS & EXIF" : "Contains " + info.MetadataTagCount + " Tags";
            else
                info.Status = "Clean";
        }

        private static string GetString(PropertyItem prop)
        {
            if (prop.Value == null || prop.Value.Length == 0) return string.Empty;
            string val = Encoding.UTF8.GetString(prop.Value);
            int nullIdx = val.IndexOf('\0');
            return nullIdx >= 0 ? val.Substring(0, nullIdx) : val;
        }

        private static double? ParseGpsCoordinate(PropertyItem prop)
        {
            try
            {
                if (prop.Value.Length < 24) return null;
                uint degNum = BitConverter.ToUInt32(prop.Value, 0);
                uint degDen = BitConverter.ToUInt32(prop.Value, 4);
                uint minNum = BitConverter.ToUInt32(prop.Value, 8);
                uint minDen = BitConverter.ToUInt32(prop.Value, 12);
                uint secNum = BitConverter.ToUInt32(prop.Value, 16);
                uint secDen = BitConverter.ToUInt32(prop.Value, 20);

                double deg = (degDen != 0) ? (double)degNum / degDen : degNum;
                double min = (minDen != 0) ? (double)minNum / minDen : minNum;
                double sec = (secDen != 0) ? (double)secNum / secDen : secNum;

                return deg + (min / 60.0) + (sec / 3600.0);
            }
            catch
            {
                return null;
            }
        }

        public static bool StripMetadata(string filePath, bool createBackup, out string error)
        {
            error = null;
            string tempOut = null;
            try
            {
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                tempOut = Path.Combine(Path.GetDirectoryName(filePath), "pexoris_tmp_" + Guid.NewGuid().ToString("N") + ext);

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var src = Image.FromStream(fs, false, false))
                    {
                        RotateFlipType rft = GetOrientationFlip(src);

                        // Create clean bitmap surface
                        PixelFormat pFormat = (src.PixelFormat == PixelFormat.Format32bppArgb || src.PixelFormat == PixelFormat.Format32bppPArgb)
                            ? PixelFormat.Format32bppArgb
                            : PixelFormat.Format24bppRgb;

                        using (var cleanBmp = new Bitmap(src.Width, src.Height, pFormat))
                        {
                            using (var g = Graphics.FromImage(cleanBmp))
                            {
                                g.CompositingMode = CompositingMode.SourceCopy;
                                g.CompositingQuality = CompositingQuality.HighQuality;
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.SmoothingMode = SmoothingMode.HighQuality;
                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                                g.DrawImage(src, 0, 0, src.Width, src.Height);
                            }

                            if (rft != RotateFlipType.RotateNoneFlipNone)
                            {
                                cleanBmp.RotateFlip(rft);
                            }

                            // Save clean without any property items or EXIF segments
                            if (ext == ".png")
                            {
                                cleanBmp.Save(tempOut, ImageFormat.Png);
                            }
                            else if (ext == ".bmp")
                            {
                                cleanBmp.Save(tempOut, ImageFormat.Bmp);
                            }
                            else if (ext == ".tif" || ext == ".tiff")
                            {
                                cleanBmp.Save(tempOut, ImageFormat.Tiff);
                            }
                            else
                            {
                                // JPEG with 98% quality (lossless perception, no artifacts)
                                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                                EncoderParameters encParams = new EncoderParameters(1);
                                encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 98L);
                                cleanBmp.Save(tempOut, jpgEncoder, encParams);
                            }
                        }
                    }
                }

                // Verify temp file written
                var fiTemp = new FileInfo(tempOut);
                if (!fiTemp.Exists || fiTemp.Length == 0)
                {
                    error = "Temporary stripped file was empty.";
                    return false;
                }

                // Backup if requested
                if (createBackup)
                {
                    string backupPath = filePath + ".bak";
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    File.Copy(filePath, backupPath, true);
                }

                // Replace original file with clean file
                File.Delete(filePath);
                File.Move(tempOut, filePath);

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                if (tempOut != null && File.Exists(tempOut))
                {
                    try { File.Delete(tempOut); } catch { }
                }
                return false;
            }
        }

        private static RotateFlipType GetOrientationFlip(Image img)
        {
            try
            {
                foreach (var prop in img.PropertyItems)
                {
                    if (prop.Id == 0x0112 && prop.Value.Length >= 2)
                    {
                        int val = BitConverter.ToUInt16(prop.Value, 0);
                        switch (val)
                        {
                            case 1: return RotateFlipType.RotateNoneFlipNone;
                            case 2: return RotateFlipType.RotateNoneFlipX;
                            case 3: return RotateFlipType.Rotate180FlipNone;
                            case 4: return RotateFlipType.Rotate180FlipX;
                            case 5: return RotateFlipType.Rotate90FlipX;
                            case 6: return RotateFlipType.Rotate90FlipNone;
                            case 7: return RotateFlipType.Rotate270FlipX;
                            case 8: return RotateFlipType.Rotate270FlipNone;
                        }
                    }
                }
            }
            catch { }
            return RotateFlipType.RotateNoneFlipNone;
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public static string FormatFileSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return string.Format("{0:0.0} MB", bytes / (1024.0 * 1024.0));
            if (bytes >= 1024)
                return string.Format("{0:0.0} KB", bytes / 1024.0);
            return bytes + " B";
        }
    }
}
