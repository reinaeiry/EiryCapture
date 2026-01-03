using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace StreamingAgent.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ScreenCaptureService _captureService = new();
    private ScreenRegion? _selectedRegion;
    private MirrorWindow? _mirrorWindow;
    private KeybindConfig _keybinds = new();
    private bool _wasRunningBeforeSelection;

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                var exeDir = Path.GetDirectoryName(exePath)!;
                var iconPath = Path.Combine(exeDir, "EiryCapture.ico");
                if (File.Exists(iconPath))
                {
                    Icon = new BitmapImage(new Uri(iconPath));
                }
            }
        }
        catch
        {
            // Ignore icon load failures and keep default icon.
        }

        FpsSlider.ValueChanged += (_, _) =>
        {
            FpsValueText.Text = ((int)FpsSlider.Value).ToString();
        };
        FpsValueText.Text = ((int)FpsSlider.Value).ToString();
        StatusText.Text = "Idle";

        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnSelectRegionClick(object sender, RoutedEventArgs e)
    {
        _wasRunningBeforeSelection = _captureService.IsRunning;
        if (_wasRunningBeforeSelection)
        {
            _captureService.Stop();
            StatusText.Text = "Paused for region selection";
        }

        var selector = new RegionSelectionWindow();
        var result = selector.ShowDialog();

        if (result == true && selector.SelectedRegion.IsValid)
        {
            _selectedRegion = selector.SelectedRegion;
            RegionText.Text = $"Region: {_selectedRegion.Value.Width}x{_selectedRegion.Value.Height} at {_selectedRegion.Value.X},{_selectedRegion.Value.Y}";

            if (_wasRunningBeforeSelection)
            {
                var fps = (int)FpsSlider.Value;
                _captureService.DrawCursor = ShowCursorCheckBox.IsChecked == true;
                _captureService.Start(_selectedRegion.Value, fps);
                StatusText.Text = $"Capturing at {fps} FPS (region changed)";
            }
        }
        else
        {
            RegionText.Text = "No region selected";
            if (_wasRunningBeforeSelection)
            {
                // Resume previous capture if selection was cancelled
                if (_selectedRegion is { IsValid: true } region)
                {
                    var fps = (int)FpsSlider.Value;
                    _captureService.DrawCursor = ShowCursorCheckBox.IsChecked == true;
                    _captureService.Start(region, fps);
                    StatusText.Text = $"Capturing at {fps} FPS";
                }
                else
                {
                    StatusText.Text = "Idle";
                }
            }
        }
    }

    private void OnStartClick(object sender, RoutedEventArgs e)
    {
        if (_selectedRegion is null || !_selectedRegion.Value.IsValid)
        {
            MessageBox.Show(this, "Please select a valid region first.", "No region", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var fps = (int)FpsSlider.Value;
            _captureService.DrawCursor = ShowCursorCheckBox.IsChecked == true;
            _captureService.Start(_selectedRegion.Value, fps);
            StatusText.Text = $"Capturing at {fps} FPS";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(this, $"Failed to start capture: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Error starting capture";
        }
    }

    private void OnStopClick(object sender, RoutedEventArgs e)
    {
        _captureService.Stop();
        StatusText.Text = "Stopped";
    }

    private void OnShowMirrorClick(object sender, RoutedEventArgs e)
    {
        if (_mirrorWindow == null || !_mirrorWindow.IsLoaded)
        {
            _mirrorWindow = new MirrorWindow(_captureService)
            {
                Owner = this
            };
            _mirrorWindow.Show();
        }
        else
        {
            if (_mirrorWindow.WindowState == WindowState.Minimized)
            {
                _mirrorWindow.WindowState = WindowState.Normal;
            }
            _mirrorWindow.Activate();
        }
    }

    private void OnHideMirrorClick(object sender, RoutedEventArgs e)
    {
        if (_mirrorWindow != null && _mirrorWindow.IsLoaded)
        {
            _mirrorWindow.Hide();
        }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        _captureService.Dispose();
        base.OnClosed(e);
    }

    private void OnKeybindsClick(object sender, RoutedEventArgs e)
    {
        var dialog = new KeybindsWindow(_keybinds)
        {
            Owner = this
        };
        if (dialog.ShowDialog() == true)
        {
            _keybinds = dialog.Config;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_keybinds.SelectRegionGesture != null && _keybinds.SelectRegionGesture.Matches(this, e))
        {
            OnSelectRegionClick(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (_keybinds.StartStopGesture != null && _keybinds.StartStopGesture.Matches(this, e))
        {
            if (_captureService.IsRunning)
            {
                OnStopClick(this, new RoutedEventArgs());
            }
            else
            {
                OnStartClick(this, new RoutedEventArgs());
            }
            e.Handled = true;
            return;
        }

        if (_keybinds.ShowHideMirrorGesture != null && _keybinds.ShowHideMirrorGesture.Matches(this, e))
        {
            if (_mirrorWindow != null && _mirrorWindow.IsVisible)
            {
                OnHideMirrorClick(this, new RoutedEventArgs());
            }
            else
            {
                OnShowMirrorClick(this, new RoutedEventArgs());
            }
            e.Handled = true;
        }
    }
}

public class KeybindConfig
{
    public KeyGesture? SelectRegionGesture { get; set; }
    public KeyGesture? StartStopGesture { get; set; }
    public KeyGesture? ShowHideMirrorGesture { get; set; }
}