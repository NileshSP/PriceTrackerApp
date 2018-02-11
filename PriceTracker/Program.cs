using System;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using PriceTracker;

namespace PriceTracker
{
    class Program
    {
        static void Main(string[] args)
        {
            APIClient objPrice = new APIClient();
            var maintask = Task.Factory.StartNew(() => objPrice.StartProcessAsync());
            maintask.Wait();
        }
    }
}
