using System;
using System.Collections.Generic;
using System.Linq;

namespace PriceTracker
{
    public static class FinanacialFunctionsExtensions<T> where T : struct,IConvertible,IFormattable,IComparable
    {
        // Simple moving average caller function for initial validation
        // <source>list of items to average for</source>
        // <sampleLength>sampling length of the moving average</sampleLength>
        public static IEnumerable<T> SimpleMovingAverage(IEnumerable<T> source, int sampleLength)
        {       
            if (source == null) throw new ArgumentNullException("source");
            if (sampleLength <= 0) throw new ArgumentException("Invalid sample length");
        
            return SimpleMovingAverageImpl(source, sampleLength);
        }

        // Simple moving average implementation function
        // <source>list of items to average for</source>
        // <sampleLength>sampling length of the moving average</sampleLength>
        private static IEnumerable<T> SimpleMovingAverageImpl(IEnumerable<T> source, int sampleLength)
        {
            Queue<T> sample = new Queue<T>(sampleLength);
        
            foreach (T d in source)
            {
                if (sample.Count == sampleLength)
                {
                    sample.Dequeue();
                }
                sample.Enqueue(d);
                yield return ((Func<dynamic>)(() => 
                {
                    if(typeof(T) == typeof(int)) { return sample.OfType<int>().Average(); }
                    else if(typeof(T) == typeof(decimal)) { return sample.OfType<decimal>().Average(); }
                    else if(typeof(T) == typeof(double)) { return sample.OfType<double>().Average(); }
                    else if(typeof(T) == typeof(float)) { return sample.OfType<float>().Average(); }
                    else { return 0; }
                }))();
            }
        }

        // Measure time for the execution of the functionality
        public static void MeasureTime(string Msg, Action action)
        {
            DateTime before = DateTime.Now;
            action();
            DateTime after = DateTime.Now; 
            TimeSpan duration = after.Subtract(before);
            Console.WriteLine(" {0} duration(milliseconds) : {1} ", Msg, duration.TotalMilliseconds);
        } 
    }
}