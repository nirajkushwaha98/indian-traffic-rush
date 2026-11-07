using System;

namespace ITR
{
    /// <summary>Date source for daily systems. Tests replace <see cref="Now"/> to simulate days passing.</summary>
    public static class Clock
    {
        public static Func<DateTime> Now = () => DateTime.Now;

        public static DateTime Today => Now().Date;

        public static string Key(DateTime d) => d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        public static string TodayKey => Key(Today);
        public static string YesterdayKey => Key(Today.AddDays(-1));
    }
}
