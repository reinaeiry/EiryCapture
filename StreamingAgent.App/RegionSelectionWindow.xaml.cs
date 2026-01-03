using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace StreamingAgent.App;

public partial class RegionSelectionWindow : Window
{
    private Point _startPoint;
    private bool _isSelecting;

    public ScreenRegion SelectedRegion { get; private set; }

    public RegionSelectionWindow()
    {
        InitializeComponent();

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        KeyDown += OnKeyDown;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSelecting = true;
        _startPoint = e.GetPosition(this);
        SelectionRectangle.Visibility = Visibility.Visible;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting)
        {
            return;
        }

        var currentPoint = e.GetPosition(this);

        var x = Math.Min(currentPoint.X, _startPoint.X);
        var y = Math.Min(currentPoint.Y, _startPoint.Y);
        var width = Math.Abs(currentPoint.X - _startPoint.X);
        var height = Math.Abs(currentPoint.Y - _startPoint.Y);

        if (width < 2 || height < 2)
        {
            SelectionRectangle.Visibility = Visibility.Collapsed;
            return;
        }

        SelectionRectangle.Visibility = Visibility.Visible;
        System.Windows.Controls.Canvas.SetLeft(SelectionRectangle, x);
        System.Windows.Controls.Canvas.SetTop(SelectionRectangle, y);
        SelectionRectangle.Width = width;
        SelectionRectangle.Height = height;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting)
        {
            return;
        }

        _isSelecting = false;
        ReleaseMouseCapture();

        var currentPoint = e.GetPosition(this);

        var x = Math.Min(currentPoint.X, _startPoint.X);
        var y = Math.Min(currentPoint.Y, _startPoint.Y);
        var width = Math.Abs(currentPoint.X - _startPoint.X);
        var height = Math.Abs(currentPoint.Y - _startPoint.Y);

        if (width < 4 || height < 4)
        {
            DialogResult = false;
            Close();
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var pixelX = (int)Math.Round((x + (Left - SystemParameters.VirtualScreenLeft)) * dpi.DpiScaleX);
        var pixelY = (int)Math.Round((y + (Top - SystemParameters.VirtualScreenTop)) * dpi.DpiScaleY);
        var pixelWidth = (int)Math.Round(width * dpi.DpiScaleX);
        var pixelHeight = (int)Math.Round(height * dpi.DpiScaleY);

        SelectedRegion = new ScreenRegion(pixelX, pixelY, pixelWidth, pixelHeight);
        DialogResult = true;
        Close();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
