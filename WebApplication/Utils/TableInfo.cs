using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Utils {
    public class TableInfo {
        public List<string> Header { get; set; } = new();
        public TableCellInfo[,] Cells { get; set; }
        public bool HasData => Cells != null;

        public static TableCellInfo[,] AddRow(TableCellInfo[,] old) {
            var temp = new TableCellInfo[old.GetLength(0) + 1, old.GetLength(1)];
            for (int i = 0; i < old.GetLength(0); i++) {
                for (int j = 0; j < old.GetLength(1); j++) {
                    temp[i, j] = old[i, j];
                }
            }
            return temp;
        }
    }

    public class TableCellInfo {
        public string Value { get; set; }
        public string Class { get; set; }
        public TableCellInfo(string value, string tclass) {
            Value = value;
            Class = tclass;
        }
    }

    public enum TableType {
        DistanceMatrix,
        RouteSummary,
        SwapSummary
    }
}