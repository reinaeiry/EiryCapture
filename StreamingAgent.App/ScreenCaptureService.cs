using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace StreamingAgent.App;

public readonly struct ScreenRegion
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public ScreenRegion(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool IsValid => Width > 0 && Height > 0;
}

public sealed class ScreenCaptureService : IDisposable
{
    private readonly System.Timers.Timer _timer;
    private readonly object _sync = new();
    private IntPtr _desktopDc = IntPtr.Zero;
    private IntPtr _memoryDc = IntPtr.Zero;
    private IntPtr _bitmap = IntPtr.Zero;
    private IntPtr _oldBitmap = IntPtr.Zero;
    private int _width;
    private int _height;
    private ScreenRegion _region;
    private int _fps = 30;
    private bool _drawCursor = true;

    public bool IsRunning { get; private set; }
    public bool DrawCursor
    {
        get => _drawCursor;
        set => _drawCursor = value;
    }

    public event Action<BitmapSource>? FrameCaptured;

    public ScreenCaptureService()
    {
        _timer = new System.Timers.Timer
        {
            Interval = 33,
            AutoReset = true,
        };
        _timer.Elapsed += OnTick;
    }

    public void Start(ScreenRegion region, int fps = 60)
    {
        if (!region.IsValid)
        {
            throw new ArgumentException("Region must have positive size", nameof(region));
        }

        if (fps < 10)
        {
            fps = 10;
        }
        if (fps > 60)
        {
            fps = 60;
        }

        _fps = fps;
        _timer.Interval = 1000.0 / _fps;

        _region = region;

        lock (_sync)
        {
            InitializeResources(region.Width, region.Height);
        }

        IsRunning = true;
        _timer.Start();
    }

    public void UpdateRegion(ScreenRegion region)
    {
        if (!region.IsValid)
        {
            throw new ArgumentException("Region must have positive size", nameof(region));
        }

        _region = region;

        lock (_sync)
        {
            if (region.Width != _width || region.Height != _height)
            {
                InitializeResources(region.Width, region.Height);
            }
        }
    }

    public void Stop()
    {
        IsRunning = false;
        _timer.Stop();
    }

    private void OnTick(object? sender, ElapsedEventArgs e)
    {
        if (!IsRunning || !_region.IsValid)
        {
            return;
        }

        lock (_sync)
        {
            if (_desktopDc == IntPtr.Zero || _memoryDc == IntPtr.Zero || _bitmap == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.BitBlt(
                _memoryDc,
                0,
                0,
                _width,
                _height,
                _desktopDc,
                _region.X,
                _region.Y,
                NativeMethods.SRCCOPY);

            if (_drawCursor)
            {
                var cursorInfo = new NativeMethods.CURSORINFO();
                cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);
                if (NativeMethods.GetCursorInfo(ref cursorInfo) &&
                    (cursorInfo.flags & NativeMethods.CURSOR_SHOWING) == NativeMethods.CURSOR_SHOWING)
                {
                    int cursorX = cursorInfo.ptScreenPos.x - _region.X;
                    int cursorY = cursorInfo.ptScreenPos.y - _region.Y;

                    if (cursorX >= 0 && cursorY >= 0 && cursorX < _width && cursorY < _height)
                    {
                        int cursorW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXCURSOR);
                        int cursorH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYCURSOR);

                        NativeMethods.DrawIconEx(
                            _memoryDc,
                            cursorX,
                            cursorY,
                            cursorInfo.hCursor,
                            cursorW,
                            cursorH,
                            0,
                            IntPtr.Zero,
                            NativeMethods.DI_NORMAL);
                    }
                }
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null)
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                        _bitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(_width, _height));

                    FrameCaptured?.Invoke(bitmapSource);
                }));
            }
        }
    }

    private void InitializeResources(int width, int height)
    {
        if (width == _width && height == _height && _desktopDc != IntPtr.Zero)
        {
            return;
        }

        ReleaseResources();

        _width = width;
        _height = height;

        _desktopDc = NativeMethods.GetDC(IntPtr.Zero);
        _memoryDc = NativeMethods.CreateCompatibleDC(_desktopDc);
        _bitmap = NativeMethods.CreateCompatibleBitmap(_desktopDc, _width, _height);
        _oldBitmap = NativeMethods.SelectObject(_memoryDc, _bitmap);
    }

    private void ReleaseResources()
    {
        if (_memoryDc != IntPtr.Zero && _oldBitmap != IntPtr.Zero)
        {
            NativeMethods.SelectObject(_memoryDc, _oldBitmap);
            _oldBitmap = IntPtr.Zero;
        }

        if (_bitmap != IntPtr.Zero)
        {
            NativeMethods.DeleteObject(_bitmap);
            _bitmap = IntPtr.Zero;
        }

        if (_memoryDc != IntPtr.Zero)
        {
            NativeMethods.DeleteDC(_memoryDc);
            _memoryDc = IntPtr.Zero;
        }

        if (_desktopDc != IntPtr.Zero)
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, _desktopDc);
            _desktopDc = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        IsRunning = false;
        _timer.Stop();
        _timer.Elapsed -= OnTick;
        ReleaseResources();
        GC.SuppressFinalize(this);
    }

    ~ScreenCaptureService()
    {
        ReleaseResources();
    }

    private static class NativeMethods
    {
        public const int SRCCOPY = 0x00CC0020;
        public const int CAPTUREBLT = 0x40000000;
        public const int CURSOR_SHOWING = 0x00000001;
        public const int DI_NORMAL = 0x0003;
        public const int SM_CXCURSOR = 13;
        public const int SM_CYCURSOR = 14;

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(ref CURSORINFO pci);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BitBlt(
            IntPtr hdc,
            int nXDest,
            int nYDest,
            int nWidth,
            int nHeight,
            IntPtr hdcSrc,
            int nXSrc,
            int nYSrc,
            int dwRop);

        [DllImport("user32.dll")]
        public static extern bool DrawIconEx(
            IntPtr hdc,
            int xLeft,
            int yTop,
            IntPtr hIcon,
            int cxWidth,
            int cyWidth,
            int istepIfAniCur,
            IntPtr hbrFlickerFreeDraw,
            int diFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }
    }
}
