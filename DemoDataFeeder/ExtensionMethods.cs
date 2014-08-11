using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoDataFeeder
{
    public static class ExtensionMethods
    {
        private static Random random = new Random();

        public static T Next<T>(this T[] items)
        {
            return items[random.Next(0, items.Length - 1)];
        }
    }
}
