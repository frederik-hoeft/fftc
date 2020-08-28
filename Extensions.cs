using System;

namespace fftc
{
    /// <summary>
    /// Provides any extensions needed by fftc.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Converts a large number of bytes to a more readable formatted string including the units.
        /// </summary>
        /// <param name="fileSize">The number of bytes to be converted.</param>
        /// <param name="decimals">The amount of decimals to round the result to (if any).</param>
        public static string ToHumanReadableFileSize(this ulong fileSize, int decimals)
        {
            string[] units = new string[] { "B", "KB", "MB", "GB", "TB", "PB" };
            int i = 0;
            double fileSizeDouble = fileSize;
            while (fileSizeDouble > 1000)
            {
                fileSizeDouble /= 1000;
                fileSizeDouble = Math.Round(fileSizeDouble, decimals, MidpointRounding.AwayFromZero);
                if (i < units.Length)
                {
                    i++;
                }
            }
            return fileSizeDouble.ToString() + units[i];
        }
    }
}