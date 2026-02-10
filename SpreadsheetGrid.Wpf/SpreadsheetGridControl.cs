using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SpreadsheetGrid.Core;

namespace SpreadsheetGrid.Wpf
{
    public class SpreadsheetGridControl : Control
    {
        private readonly SelectionManager _selectionManager = new();

        private ItemsControl? _cellsHost;
        private Canvas? _gridLines;
        private Canvas? _selectionLayer;

        static SpreadsheetGridControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SpreadsheetGridControl),
                new FrameworkPropertyMetadata(typeof(SpreadsheetGridControl)));
        }

        #region Dependency Properties

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
                new PropertyMetadata(10, OnGridSizeChanged));

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
                new PropertyMetadata(10, OnGridSizeChanged));

        private static void OnGridSizeChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is SpreadsheetGridControl c)
            {
                c.RebuildGrid();
                c.DrawGridLines();
                c.DrawSelection();
            }
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _cellsHost = GetTemplateChild("PART_Cells") as ItemsControl;
            _gridLines = GetTemplateChild("PART_GridLines") as Canvas;
            _selectionLayer = GetTemplateChild("PART_Selection") as Canvas;

            if (_cellsHost == null || _gridLines == null || _selectionLayer == null)
                return;

            RebuildGrid();

            SizeChanged += (_, __) =>
            {
                DrawGridLines();
                DrawSelection();
            };

            _cellsHost.PreviewMouseLeftButtonDown += OnMouseDown;
            _cellsHost.PreviewMouseMove += OnMouseMove;
            _cellsHost.PreviewMouseLeftButtonUp += OnMouseUp;
        }

        private void RebuildGrid()
        {
            if (_cellsHost == null)
                return;

            _cellsHost.ItemsSource =
                Enumerable.Range(0, Rows * Columns)
                    .Select(i => new CellAddress(i / Columns, i % Columns))
                    .ToList();

            _selectionManager.Clear();
        }

        #region Mouse / Selection

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_cellsHost == null) return;

            _cellsHost.CaptureMouse();

            var cell = GetCellFromPoint(e.GetPosition(_cellsHost));
            _selectionManager.Start(cell);
            DrawSelection();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_cellsHost == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            var cell = GetCellFromPoint(e.GetPosition(_cellsHost));
            _selectionManager.Update(cell);
            DrawSelection();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _cellsHost?.ReleaseMouseCapture();
        }

        private CellAddress GetCellFromPoint(Point point)
        {
            if (_cellsHost == null)
                return new CellAddress(0, 0);

            var element = _cellsHost.InputHitTest(point) as DependencyObject;
            var container = ItemsControl.ContainerFromElement(_cellsHost, element!)
                            as ContentPresenter;

            return container?.Content as CellAddress
                   ?? new CellAddress(0, 0);
        }

        #endregion

        #region Grid Lines (NO GAPS)

        private void DrawGridLines()
        {
            if (_gridLines == null || _cellsHost == null)
                return;

            _gridLines.Children.Clear();

            double w = _cellsHost.ActualWidth;
            double h = _cellsHost.ActualHeight;

            if (w <= 0 || h <= 0) return;

            double cellW = w / Columns;
            double cellH = h / Rows;

            var brush = new SolidColorBrush(Color.FromRgb(176, 176, 176));

            for (int c = 0; c <= Columns; c++)
            {
                double x = Math.Round(c * cellW);
                _gridLines.Children.Add(new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = h,
                    Stroke = brush,
                    StrokeThickness = 1,
                    SnapsToDevicePixels = true
                });
            }

            for (int r = 0; r <= Rows; r++)
            {
                double y = Math.Round(r * cellH);
                _gridLines.Children.Add(new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = w,
                    Y2 = y,
                    Stroke = brush,
                    StrokeThickness = 1,
                    SnapsToDevicePixels = true
                });
            }
        }

        #endregion

        #region Selection (PIXEL PERFECT)

        private void DrawSelection()
        {
            if (_selectionLayer == null || _cellsHost == null)
                return;

            _selectionLayer.Children.Clear();

            var range = _selectionManager.Current;
            if (range == null) return;

            var start = GetContainer(range.Start);
            var end = GetContainer(range.End);

            if (start == null || end == null) return;

            var p1 = start.TransformToVisual(_selectionLayer)
                          .Transform(new Point(0, 0));

            var p2 = end.TransformToVisual(_selectionLayer)
                        .Transform(new Point(end.ActualWidth, end.ActualHeight));

            double left = Math.Min(p1.X, p2.X);
            double top = Math.Min(p1.Y, p2.Y);
            double right = Math.Max(p1.X, p2.X);
            double bottom = Math.Max(p1.Y, p2.Y);

            var rect = new Rectangle
            {
                Width = right - left,
                Height = bottom - top,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(40, 30, 144, 255)),
                SnapsToDevicePixels = true
            };

            Canvas.SetLeft(rect, left + 1);
            Canvas.SetTop(rect, top + 1);

            _selectionLayer.Children.Add(rect);
        }

        private FrameworkElement? GetContainer(CellAddress cell)
        {
            int index = cell.Row * Columns + cell.Column;
            return _cellsHost?
                .ItemContainerGenerator
                .ContainerFromIndex(index) as FrameworkElement;
        }

        #endregion
    }
}
