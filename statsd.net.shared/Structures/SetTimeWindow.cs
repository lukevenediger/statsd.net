using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Structures
{
    public class SetTimeWindow
    {
        public const string HOUR = "hour";
        public const string DAY = "day";
        public const string WEEKDAY = "weekday";
        public const string WEEK = "week";
        public const string MONTH = "month";

        public String OneHour { get; private set; }
        public String DayOfMonth { get; private set; }
        public String DayOfWeek { get; private set; }
        public String Week { get; private set; }
        public String Month { get; private set; }
        public String[] AllPeriods { get; private set; }

        public SetTimeWindow()
        {
            var now = DateTime.Now;
            OneHour = HOUR + "_" + now.Hour;
            DayOfMonth = DAY + "_" + now.Day;
            DayOfWeek = WEEKDAY + "_" + now.DayOfWeek;
            Week = WEEK + "_" + now.GetIso8601WeekOfYear();
            Month = MONTH + "_" + now.Month;

            AllPeriods = new String[] { OneHour, DayOfMonth, DayOfWeek, Week, Month };
        }

        public List<String> GetDifferences(SetTimeWindow other)
        {
            var differences = new List<String>();
            if (other.OneHour != OneHour) differences.Add(other.OneHour);
            if (other.DayOfMonth != DayOfMonth) differences.Add(other.DayOfMonth);
            if (other.DayOfWeek != DayOfWeek) differences.Add(other.DayOfWeek);
            if (other.Week != Week) differences.Add(other.Week);
            if (other.Month != Month) differences.Add(other.Month);
            return differences;
        }
    }
}
