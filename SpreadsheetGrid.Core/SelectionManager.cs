using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpreadsheetGrid.Core
{
 

    public sealed class SelectionManager
    {
        public SelectionRange? Current { get; private set; }

        public void Start(CellAddress cell)
        {
            Current = new SelectionRange(cell, cell);
        }

        public void Update(CellAddress cell)
        {
            if (Current == null) return;
            Current = new SelectionRange(Current.Start, cell);
        }

        public void Clear()
        {
            Current = null;
        }
    }

}
