# EiryCapture

EiryCapture is a small Windows screen-region capture tool. It lets you pick a rectangular portion of your monitor, mirrors that region in a dedicated window, and then you share that window (with system audio) in Discord.

Under the hood, it uses the Windows desktop DC and GDI `BitBlt` to grab only the pixels you care about at a configurable FPS, then renders them in a WPF mirror window. Discord sees that mirror window as a normal app, so your main desktop remains fully usable.

## Requirements

- Windows 10 or later (64-bit)
- .NET 8 SDK (for building from source)
- For running the published exe: no .NET install needed (self-contained publish)

## Building from source

From the repo root:

```powershell
dotnet build -c Release
```

This compiles the WPF app and produces binaries under:

- `StreamingAgent.App/bin/Release/net8.0-windows/`

To create a single-file, self-contained exe called **EiryCapture.exe** for Windows x64:

```powershell
dotnet publish StreamingAgent.App/StreamingAgent.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The publish output will be in:

- `StreamingAgent.App/bin/Release/net8.0-windows/win-x64/publish/`

Copy that whole `publish` folder to another machine and run `EiryCapture.exe` directly.

## Installing and using EiryCapture

1. **Install**
   - Build and publish as above.
   - Optionally place a custom icon file named `EiryCapture.ico` next to `EiryCapture.exe` so the app/taskbar icon uses it.
   - Create a desktop or Start Menu shortcut to `EiryCapture.exe` if desired.

2. **Basic usage**
   - Run `EiryCapture.exe`.
   - In the controller window:
     - Click **Select Region** and drag the area of your monitor you want to stream.
     - Adjust **FPS** (10–60) as needed.
     - Use the **Show cursor in capture** checkbox to include or hide your mouse cursor.
     - Click **Start** to begin capturing.
     - Click **Show Mirror** to bring up the mirror window titled *Streaming Region*.
   - In Discord:
     - Choose to share an application/window and select **Streaming Region**.
     - Enable system audio in Discord’s share UI so desktop sound is broadcast.

3. **Keybinds**
   - Click **Keybinds…** in the controller to assign hotkeys for:
     - Selecting region
     - Starting/stopping capture
     - Showing/hiding the mirror window
   - Keybinds work while the controller window has focus.

## Contributing

1. Fork the repository and create a feature branch.
2. Make focused changes (UI, capture behavior, keybinds, etc.), keeping the existing patterns:
   - Capture logic lives in `StreamingAgent.App/ScreenCaptureService.cs`.
   - UI and controller behavior live under `StreamingAgent.App/*.xaml` and `*.xaml.cs`.
3. Build in Release to ensure everything compiles:

   ```powershell
   dotnet build -c Release
   ```

4. Manually verify:
   - Selecting regions
   - Starting/stopping capture
   - Discord sharing of the *Streaming Region* window with audio
5. Submit a pull request describing the change and any UX or performance impact.

## Notes on icons

- To customize the app icon in the title bar and taskbar, drop a valid `EiryCapture.ico` file next to `EiryCapture.exe` in the publish folder. The app will load it automatically on startup.
- If the icon file is missing or invalid, EiryCapture falls back to the default Windows/WPF icon.
