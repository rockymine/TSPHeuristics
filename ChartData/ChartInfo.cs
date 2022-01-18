using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChartData {
    public class ChartInfo {
        public string Title { get; set; }
        public ChartSet XAxis { get; set; } = new();
        public List<ChartSet> YAxis { get; set; } = new();

        public ChartInfo DeepCopy() {
            return new ChartInfo {
                Title = Title,
                XAxis = XAxis.DeepCopy(),
                YAxis = YAxis.Select(c => c.DeepCopy()).ToList()
            };
        }
    }

    public class ChartSet {
        public string Title { get; set; }
        public List<double> Values { get; set; } = new();
        public List<string> Colors { get; set; } = new();

        public ChartSet DeepCopy() {
            return new ChartSet {
                Title = Title,
                Values = new List<double>(Values),
                Colors = new List<string>(Colors)
            };
        }

        public void Add(double value, string color) {
            Values.Add(value);
            Colors.Add(color);
        }
    }
}