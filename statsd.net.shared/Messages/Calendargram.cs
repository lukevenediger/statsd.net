using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Messages
{
    public sealed class Calendargram : StatsdMessage
    {
        public string Value { get; private set; }
        public CalendargramRetentionPeriod Period { get; private set; }
        private string _rawPeriod;

        public const int MAX_VALUE_LENGTH = 255;
        public const string PERIOD_ONE_MINUTE = "1min";
        public const string PERIOD_FIVE_MINUTES = "5min";
        public const string PERIOD_HOUR = "h";
        public const string PERIOD_DAY = "d";
        public const string PERIOD_WEEK = "w";
        public const string PERIOD_MONTH = "m";
        public const string PERIOD_DAY_OF_WEEK = "dow";

        public Calendargram(string name, string value, string period)
        {
            if (value.Length > MAX_VALUE_LENGTH)
            {
                throw new ArgumentException(String.Format("Cannot have a Calendargram value longer than {0} characters", MAX_VALUE_LENGTH));
            }
            base.MessageType = MessageType.Calendargram;
            base.Name = name;
            Value = value;
            _rawPeriod = period;
            switch (period)
            {
                case PERIOD_ONE_MINUTE: Period = CalendargramRetentionPeriod.OneMinute; break;
                case PERIOD_FIVE_MINUTES: Period = CalendargramRetentionPeriod.FiveMinute; break;
                case PERIOD_HOUR: Period = CalendargramRetentionPeriod.Hour; break;
                case PERIOD_DAY: Period = CalendargramRetentionPeriod.Day; break;
                case PERIOD_DAY_OF_WEEK: Period = CalendargramRetentionPeriod.DayOfWeek; break;
                case PERIOD_WEEK: Period = CalendargramRetentionPeriod.Week; break;
                case PERIOD_MONTH: Period = CalendargramRetentionPeriod.Month; break;
                default: throw new ArgumentException("Unknown retention period: " + period, "period");
            }
        }

        public Calendargram(string name, string value, CalendargramRetentionPeriod period)
        {
            if (value.Length > MAX_VALUE_LENGTH)
            {
                throw new ArgumentException(String.Format("Cannot have a Calendargram value longer than {0} characters", MAX_VALUE_LENGTH));
            }
            base.MessageType = MessageType.Calendargram;
            base.Name = name;
            Value = value;
            Period = period;
        }

        public override string ToString()
        {
            return Name + ":" + Value + "|cg|" + _rawPeriod;
        }
    }
}
