using statsd.net.shared.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace statsd.net.shared.Messages
{
    public sealed class Gauge : StatsdMessage
    {
        public int Value { get; set; }
        public GaugeOperation GaugeOperation { get; set; }

        public Gauge(string name, int value, GaugeOperation gaugeOperation = GaugeOperation.set)
        {
            Name = name;
            Value = value;
            MessageType = MessageType.Gauge;
        }

        public override string ToString()
        {
            if (GaugeOperation == GaugeOperation.set)
            {
                return String.Format("{0}:{1}|g", Name, Value);
            }
            else
            {
                return String.Format("{0}:{1}{2}|g", Name, (GaugeOperation == GaugeOperation.increment ? "+" : "-"), Value);
            }
        }
    }
}