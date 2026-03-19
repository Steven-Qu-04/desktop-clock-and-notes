# DesktopClockExe

This is a Windows wrapper for `time.html`.

What it does:
- Loads the existing HTML UI inside WebView2
- Copies `time.html` and the background image into the build output
- Attaches the app window to the desktop worker window so it appears like wallpaper

Important limitation:
- A true desktop-background window sits behind desktop icons on Windows.
- That means interaction can be limited anywhere the icon layer is on top.

Build steps on a machine with the .NET 8 SDK:

```powershell
cd DesktopClockExe
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true
```

The generated `.exe` will be under:

```text
DesktopClockExe\bin\Release\net8.0-windows\win-x64\publish\
```
