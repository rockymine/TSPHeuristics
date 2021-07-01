using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Utils {
    public class TableInfo {
        public List<string> Header { get; set; } = new();
        public TableCellInfo[,] Cells { get; set; }
        public bool HasData => Cells != null;
    }

    public class TableCellInfo {
        public string Value { get; set; }
        public string Class { get; set; }
        public TableCellInfo(string value, string tclass) {
            Value = value;
            Class = tclass;
        }
    }
}