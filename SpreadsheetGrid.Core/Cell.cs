using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpreadsheetGrid.Core
{
    public class Cell
    {
        public int Row { get; }
        public int Column { get; }
        public string Value { get; set; }

        public Cell(int row, int column)
        {
            Row = row;
            Column = column;
        }
    }

}
