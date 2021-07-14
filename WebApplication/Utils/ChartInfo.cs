using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Utils {
    public class ChartInfo {
        public List<int> XValues { get; set; } = new();
        public List<double> YValues { get; set; } = new();
        public string XAxis { get; set; }
        public string YAxis { get; set; }
        public string Title { get; set; }
    }
}