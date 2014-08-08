using statsd.net.shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Structures
{
    public class TimeWindow
    {
        public const string ONE_MINUTE = "1minute";
        public const string FIVE_MINUTE = "5minute";
        public const string HOUR = "hour";
        public const string DAY = "day";
        public const string WEEKDAY = "weekday";
        public const string WEEK = "week";
        public const string MONTH = "month";

        public String OneMinute { get; private set; }
        public String FiveMinute { get; private set; }
        public String OneHour { get; private set; }
        public String DayOfMonth { get; private set; }
        public String DayOfWeek { get; private set; }
        public String Week { get; private set; }
        public String Month { get; private set; }
        public String[] AllPeriods { get; private set; }

        public TimeWindow()
            : this(DateTime.Now)
        {
        }

        public TimeWindow(DateTime now)
        {
            OneMinute = ONE_MINUTE + "_" + now.Minute;
            FiveMinute = FIVE_MINUTE + "_" + (now.Minute / 5);
            OneHour = HOUR + "_" + now.Hour;
            DayOfMonth = DAY + "_" + now.Day;
            DayOfWeek = WEEKDAY + "_" + now.DayOfWeek;
            Week = WEEK + "_" + now.GetIso8601WeekOfYear();
            Month = MONTH + "_" + now.Month;

            AllPeriods = new String[] { OneMinute, FiveMinute, OneHour, DayOfMonth, DayOfWeek, Week, Month };
        }

        public string GetTimePeriod(CalendargramRetentionPeriod period)
        {
            switch (period)
            {
                case CalendargramRetentionPeriod.OneMinute: return OneMinute;
                case CalendargramRetentionPeriod.FiveMinute: return FiveMinute;
                case CalendargramRetentionPeriod.Hour: return OneHour;
                case CalendargramRetentionPeriod.Day: return DayOfMonth;
                case CalendargramRetentionPeriod.Week: return Week;
                case CalendargramRetentionPeriod.DayOfWeek: return DayOfWeek;
                case CalendargramRetentionPeriod.Month: return Month;
                default: throw new ArgumentException("Unknown period: " + period.ToString(), "period");
            }
        }

        public List<String> GetDifferences(TimeWindow other)
        {
            var differences = new List<String>();
            if (other.OneMinute != OneMinute) differences.Add(other.OneMinute);
            if (other.FiveMinute != FiveMinute) differences.Add(other.FiveMinute);
            if (other.OneHour != OneHour) differences.Add(other.OneHour);
            if (other.DayOfMonth != DayOfMonth) differences.Add(other.DayOfMonth);
            if (other.DayOfWeek != DayOfWeek) differences.Add(other.DayOfWeek);
            if (other.Week != Week) differences.Add(other.Week);
            if (other.Month != Month) differences.Add(other.Month);
            return differences;
        }
    }
}
