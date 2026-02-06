using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpreadsheetGrid.Core
{
 
    public sealed class SelectionRange
    {
        public CellAddress Start { get; }
        public CellAddress End { get; }

        public SelectionRange(CellAddress start, CellAddress end)
        {
            Start = start;
            End = end;
        }

        public IEnumerable<CellAddress> Cells()
        {
            int r1 = Math.Min(Start.Row, End.Row);
            int r2 = Math.Max(Start.Row, End.Row);
            int c1 = Math.Min(Start.Column, End.Column);
            int c2 = Math.Max(Start.Column, End.Column);

            for (int r = r1; r <= r2; r++)
                for (int c = c1; c <= c2; c++)
                    yield return new CellAddress(r, c);
        }
    }


}
