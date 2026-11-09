# MouseMacro

A .NET 10 WPF application for recording and playing back mouse macros, plus a high-speed auto-clicker. Built with MVVM and [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet).

## Features

- **Macro** — record mouse movements, clicks, and wheel events to memory; play them back with adjustable speed and optional looping; save/load recordings as plain text; edit the macro text directly in the app.
- **Clicker** — auto-click at a configurable rate (default 10,000 clicks/sec) with a choice of Left/Right/Middle button.
- **Global hotkeys** (work even when the app isn't focused):
  - `Left Shift + Ctrl + S` — start/stop playback (Macro) or the clicker (Clicker)
  - `Left Shift + Ctrl + R` — start/stop recording (Macro)
- **Themes** — Dark and Light, switchable from the header.
- Settings persist to `appsettings.json` on every change.

## Requirements

- Windows
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (includes the Windows Desktop runtime)

## Build & run

```powershell
dotnet build MouseMacro.slnx
dotnet run --project MouseMacro
```

Or open `MouseMacro.slnx` in Visual Studio / VS Code and press F5.

## Tests

```powershell
dotnet test MouseMacro.slnx
```

The `MouseMacro.Tests` project (xUnit) covers settings persistence, macro parsing/compression, clicker behavior, and hotkey routing. Mouse input, hooks, and file dialogs are mocked via interfaces, so tests never move the real cursor or open windows.

## License

[MIT](LICENSE)
