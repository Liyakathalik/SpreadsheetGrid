using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SpreadsheetGrid.Core;
namespace SpreadsheetGrid.Wpf
{
    public class SpreadsheetGridControl : Control
    {
        private readonly SelectionManager _selectionManager = new();
        private Canvas? _overlay;
        private Point _dragStart;
        private ItemsControl? _cellsHost;
        static SpreadsheetGridControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SpreadsheetGridControl),
                new FrameworkPropertyMetadata(typeof(SpreadsheetGridControl)));
        }

        public int Rows
        {
            get => (int)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register(
                nameof(Rows),
                typeof(int),
                typeof(SpreadsheetGridControl),
                new PropertyMetadata(20));

        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                nameof(Columns),
                typeof(int),
                typeof(SpreadsheetGridControl),
                new PropertyMetadata(10));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _cellsHost = GetTemplateChild("PART_Cells") as ItemsControl;
            _overlay = GetTemplateChild("PART_Overlay") as Canvas;

            if (_cellsHost == null || _overlay == null)
                return;

            _cellsHost.ItemsSource =
                Enumerable.Range(0, Rows * Columns).ToList();

            _cellsHost.PreviewMouseLeftButtonDown += OnMouseDown;
            _cellsHost.PreviewMouseMove += OnMouseMove;
            _cellsHost.PreviewMouseLeftButtonUp += OnMouseUp;
        }

        private CellAddress GetCellFromPoint(Point point)
        {
            if (_cellsHost == null)
                return new CellAddress(0, 0);

            double cellWidth = _cellsHost.ActualWidth / Columns;
            double cellHeight = _cellsHost.ActualHeight / Rows;

            int column = Math.Max(0, Math.Min(
                Columns - 1,
                (int)(point.X / cellWidth)));

            int row = Math.Max(0, Math.Min(
                Rows - 1,
                (int)(point.Y / cellHeight)));

            return new CellAddress(row, column);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var point = e.GetPosition(_cellsHost);
            var cell = GetCellFromPoint(point);

            _selectionManager.Update(cell);
            DrawSelection();
        }
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            DrawSelection();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(this);
            var cell = GetCellFromPoint(_dragStart);

            _selectionManager.Start(cell);
            DrawSelection();
        }
        private void DrawSelection()
        {
            if (_overlay == null || _cellsHost == null)
                return;

            _overlay.Children.Clear();

            var range = _selectionManager.Current;
            if (range == null)
                return;

            double cellWidth = _cellsHost.ActualWidth / Columns;
            double cellHeight = _cellsHost.ActualHeight / Rows;

            int r1 = Math.Min(range.Start.Row, range.End.Row);
            int r2 = Math.Max(range.Start.Row, range.End.Row);
            int c1 = Math.Min(range.Start.Column, range.End.Column);
            int c2 = Math.Max(range.Start.Column, range.End.Column);

            var rect = new Rectangle
            {
                Width = (c2 - c1 + 1) * cellWidth,
                Height = (r2 - r1 + 1) * cellHeight,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(40, 30, 144, 255))
            };

            Canvas.SetLeft(rect, c1 * cellWidth);
            Canvas.SetTop(rect, r1 * cellHeight);

            _overlay.Children.Add(rect);
        }




        private static T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
