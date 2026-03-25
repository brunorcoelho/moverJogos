using System;
using System.IO;

namespace GameMover.Models
{
    public class DiskInfo
    {
        public string DriveLetter { get; set; }
        public string VolumeLabel { get; set; }
        public long TotalSize { get; set; }
        public long FreeSpace { get; set; }

        public string DisplayText
        {
            get
            {
                string label = string.IsNullOrEmpty(VolumeLabel) ? "Disco Local" : VolumeLabel;
                return string.Format("{0} ({1}) — {2} livres de {3}",
                    DriveLetter, label, FormatSize(FreeSpace), FormatSize(TotalSize));
            }
        }

        public string ShortDisplay
        {
            get
            {
                return string.Format("{0} — {1} livres", DriveLetter, FormatSize(FreeSpace));
            }
        }

        public double UsagePercent
        {
            get
            {
                if (TotalSize == 0) return 0;
                return (double)(TotalSize - FreeSpace) / TotalSize * 100;
            }
        }

        public static string FormatSize(long bytes)
        {
            if (bytes >= 1_073_741_824L)
                return string.Format("{0:F1} GB", bytes / 1_073_741_824.0);
            if (bytes >= 1_048_576L)
                return string.Format("{0:F1} MB", bytes / 1_048_576.0);
            if (bytes >= 1024L)
                return string.Format("{0:F1} KB", bytes / 1024.0);
            return string.Format("{0} B", bytes);
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
