using System.Diagnostics;
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
            if (d is SpreadsheetGridControl control)
            {
                control.RebuildGrid();
            }
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
            DrawSelection();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _cellsHost = GetTemplateChild("PART_Cells") as ItemsControl;
            _overlay = GetTemplateChild("PART_Overlay") as Canvas;

            if (_cellsHost == null || _overlay == null)
                return;

            RebuildGrid(); // ✅ ONLY source of ItemsSource

            _cellsHost.PreviewMouseLeftButtonDown += OnMouseDown;
            _cellsHost.PreviewMouseMove += OnMouseMove;
            _cellsHost.PreviewMouseLeftButtonUp += OnMouseUp;
        }


        private CellAddress GetCellFromPoint(Point point)
        {
            if (_cellsHost == null)
                return new CellAddress(0, 0);

            var element = _cellsHost.InputHitTest(point) as DependencyObject;
            if (element == null)
                return new CellAddress(0, 0);

            var container = ItemsControl.ContainerFromElement(_cellsHost, element)
                            as ContentPresenter;

            if (container?.Content is CellAddress cell)
                return cell;

            return new CellAddress(0, 0);
        }


        private static T? FindVisualParent<T>(DependencyObject child)
            where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }


        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var point = e.GetPosition(_cellsHost);
            var cell = GetCellFromPoint(point);
            Debug.WriteLine(_cellsHost.Items.Count);
            _selectionManager.Update(cell);
            DrawSelection();


        }
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _cellsHost?.ReleaseMouseCapture();
            DrawSelection();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_cellsHost == null) return;

            _cellsHost.CaptureMouse();

            var point = e.GetPosition(_cellsHost);
            var cell = GetCellFromPoint(point);

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

            var startContainer = GetContainer(range.Start);
            var endContainer = GetContainer(range.End);

            if (startContainer == null || endContainer == null)
                return;

            // Get top-left points of both cells
            var pStart = startContainer.TransformToVisual(_overlay)
                                       .Transform(new Point(0, 0));

            var pEnd = endContainer.TransformToVisual(_overlay)
                                   .Transform(new Point(0, 0));

            // Calculate rectangle bounds correctly
            double left = Math.Min(pStart.X, pEnd.X);
            double top = Math.Min(pStart.Y, pEnd.Y);

            double right = Math.Max(
                pStart.X + startContainer.ActualWidth,
                pEnd.X + endContainer.ActualWidth);

            double bottom = Math.Max(
                pStart.Y + startContainer.ActualHeight,
                pEnd.Y + endContainer.ActualHeight);

            var rect = new Rectangle
            {
                Width = right - left,
                Height = bottom - top,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(40, 30, 144, 255))
            };

            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);

            _overlay.Children.Add(rect);
        }

        private FrameworkElement? GetContainer(CellAddress cell)
        {
            int index = cell.Row * Columns + cell.Column;
            return _cellsHost?
                .ItemContainerGenerator
                .ContainerFromIndex(index) as FrameworkElement;
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
