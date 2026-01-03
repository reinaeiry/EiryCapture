using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StreamingAgent.App;

public partial class KeybindsWindow : Window
{
    public KeybindConfig Config { get; private set; }

    public KeybindsWindow(KeybindConfig current)
    {
        InitializeComponent();
        Config = new KeybindConfig
        {
            SelectRegionGesture = current.SelectRegionGesture,
            StartStopGesture = current.StartStopGesture,
            ShowHideMirrorGesture = current.ShowHideMirrorGesture
        };

        ApplyToTextBox(SelectRegionBox, Config.SelectRegionGesture);
        ApplyToTextBox(StartStopBox, Config.StartStopGesture);
        ApplyToTextBox(ShowHideMirrorBox, Config.ShowHideMirrorGesture);
    }

    private static void ApplyToTextBox(TextBox box, KeyGesture? gesture)
    {
        if (gesture == null)
        {
            box.Text = string.Empty;
            box.Tag = null;
        }
        else
        {
            box.Text = gesture.GetDisplayStringForCulture(System.Globalization.CultureInfo.CurrentCulture);
            box.Tag = gesture;
        }
    }

    private void OnKeyBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var box = (TextBox)sender;

        if (e.Key == Key.Tab)
        {
            e.Handled = false;
            return;
        }

        if (e.Key == Key.Escape)
        {
            box.Text = string.Empty;
            box.Tag = null;
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        var gesture = new KeyGesture(key, modifiers);
        box.Text = gesture.GetDisplayStringForCulture(System.Globalization.CultureInfo.CurrentCulture);
        box.Tag = gesture;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Config.SelectRegionGesture = SelectRegionBox.Tag as KeyGesture;
        Config.StartStopGesture = StartStopBox.Tag as KeyGesture;
        Config.ShowHideMirrorGesture = ShowHideMirrorBox.Tag as KeyGesture;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        SelectRegionBox.Text = string.Empty;
        SelectRegionBox.Tag = null;
        StartStopBox.Text = string.Empty;
        StartStopBox.Tag = null;
        ShowHideMirrorBox.Text = string.Empty;
        ShowHideMirrorBox.Tag = null;
    }
}
