using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace StreamingAgent.App;

public partial class MirrorWindow : Window
{
    private readonly ScreenCaptureService _captureService;

    public MirrorWindow(ScreenCaptureService captureService)
    {
        InitializeComponent();
        _captureService = captureService;
        _captureService.FrameCaptured += OnFrameCaptured;
    }

    private void OnFrameCaptured(BitmapSource frame)
    {
        CaptureImage.Source = frame;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _captureService.FrameCaptured -= OnFrameCaptured;
        base.OnClosing(e);
    }
}
