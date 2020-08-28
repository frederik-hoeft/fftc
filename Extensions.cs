using System;
using System.Collections.Generic;
using System.Text;

namespace fftc
{
    public static class Extensions
    {
        public static string ToHumanReadableFileSize(this double fileSize, int decimals)
        {
            string[] units = new string[] { "B", "KB", "MB", "GB", "TB", "PB" };
            int i = 0;
            while (fileSize > 1000)
            {
                fileSize /= 1000;
                fileSize = Math.Round(fileSize, decimals, MidpointRounding.AwayFromZero);
                if (i < units.Length)
                {
                    i++;
                }
            }
            return fileSize.ToString() + units[i];
        }
    }
}
