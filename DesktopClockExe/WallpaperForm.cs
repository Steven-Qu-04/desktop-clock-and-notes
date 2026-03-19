using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DesktopClockExe;

public sealed class WallpaperForm : Form
{
    private readonly WebView2 _webView = new();
    private Screen _targetScreen;
    private bool _spanAllScreens;

    public WallpaperForm()
    {
        _targetScreen = GetPreferredScreen();
        Text = "Desktop Clock";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Rectangle initialBounds = GetTargetBounds();
        Bounds = initialBounds;
        Location = new Point(initialBounds.Left, initialBounds.Top);
        ShowInTaskbar = false;
        TopMost = false;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;

        _webView.Dock = DockStyle.Fill;
        _webView.DefaultBackgroundColor = Color.Black;
        Controls.Add(_webView);

        Load += async (_, _) => await InitializeAsync();
        Shown += (_, _) => ApplyWindowPlacement();
    }

    private async Task InitializeAsync()
    {
        try
        {
            AppLogger.Log($"InitializeAsync target={_targetScreen.DeviceName} mode=interactive-desktop-window");
            string appDir = AppContext.BaseDirectory;
            string htmlPath = Path.Combine(appDir, "Assets", "time.html");
            string userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DesktopClockExe",
                "WebView2");

            Directory.CreateDirectory(userDataDir);

            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataDir);
            await _webView.EnsureCoreWebView2Async(environment);
            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _webView.CoreWebView2.WebMessageReceived += (_, args) => HandleWebMessage(args.WebMessageAsJson);
            _webView.Source = new Uri(htmlPath);

            PostMonitorInfo();
            ApplyWindowPlacement();
        }
        catch (COMException)
        {
            AppLogger.Log("WebView2 COM exception");
            MessageBox.Show(
                "WebView2 could not start. The app will stay closed.",
                "Desktop Clock",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            Close();
        }
        catch (Exception ex)
        {
            AppLogger.Log("Startup failure", ex);
            MessageBox.Show(
                $"Desktop Clock failed to start.\n\n{ex.Message}",
                "Desktop Clock",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Close();
        }
    }

    private void ApplyWindowPlacement()
    {
        Rectangle bounds = GetTargetBounds();
        Bounds = bounds;
        Location = new Point(bounds.Left, bounds.Top);
        WindowState = FormWindowState.Normal;
        SendToBack();
    }

    private void HandleWebMessage(string messageJson)
    {
        if (string.IsNullOrWhiteSpace(messageJson))
        {
            return;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(messageJson);
            JsonElement root = document.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                using JsonDocument nested = JsonDocument.Parse(root.GetString() ?? "{}");
                root = nested.RootElement.Clone();
            }
            if (!root.TryGetProperty("type", out JsonElement typeElement))
            {
                return;
            }

            string? type = typeElement.GetString();
            if (type == "get-monitor-info")
            {
                PostMonitorInfo();
                return;
            }

            if (type == "quit-app")
            {
                BeginInvoke(new Action(Close));
                return;
            }

            if (type == "move-to-monitor" && root.TryGetProperty("index", out JsonElement indexElement))
            {
                int requestedIndex = indexElement.GetInt32();
                if (requestedIndex == 0)
                {
                    _spanAllScreens = true;
                    ApplyWindowPlacement();
                    PostMonitorInfo();
                    return;
                }

                Screen? nextScreen = Screen.AllScreens.FirstOrDefault(screen => GetScreenIndex(screen) == requestedIndex);
                if (nextScreen != null)
                {
                    _spanAllScreens = false;
                    _targetScreen = nextScreen;
                    ApplyWindowPlacement();
                    PostMonitorInfo();
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log("Failed to handle web message", ex);
        }
    }

    private void PostMonitorInfo()
    {
        if (_webView.CoreWebView2 == null)
        {
            return;
        }

        string monitorJson = JsonSerializer.Serialize(new
        {
            type = "monitor-info",
            activeIndex = _spanAllScreens ? 0 : GetScreenIndex(_targetScreen),
            monitors = Screen.AllScreens.Select(screen => new
            {
                index = GetScreenIndex(screen),
                label = GetScreenIndex(screen).ToString(),
                name = screen.DeviceName
            })
        });
        _webView.CoreWebView2.PostWebMessageAsJson(monitorJson);
    }

    private Rectangle GetTargetBounds()
    {
        if (!_spanAllScreens)
        {
            return _targetScreen.WorkingArea;
        }

        Screen[] screens = Screen.AllScreens;
        if (screens.Length == 0)
        {
            return Screen.PrimaryScreen?.WorkingArea ?? Rectangle.Empty;
        }

        int left = screens.Min(screen => screen.WorkingArea.Left);
        int top = screens.Min(screen => screen.WorkingArea.Top);
        int right = screens.Max(screen => screen.WorkingArea.Right);
        int bottom = screens.Max(screen => screen.WorkingArea.Bottom);

        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private static Screen GetPreferredScreen()
    {
        Screen[] screens = Screen.AllScreens;
        if (screens.Length == 0)
        {
            return Screen.PrimaryScreen!;
        }

        Screen preferred = screens[0];

        foreach (Screen screen in screens)
        {
            if (!screen.Primary &&
                (screen.Bounds.Left > preferred.Bounds.Left ||
                 (screen.Bounds.Left == preferred.Bounds.Left && screen.Bounds.Top >= preferred.Bounds.Top)))
            {
                preferred = screen;
            }
        }

        return preferred;
    }

    private static int GetScreenIndex(Screen screen)
    {
        string digits = new string(screen.DeviceName.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out int index))
        {
            return index;
        }

        return Array.IndexOf(Screen.AllScreens, screen) + 1;
    }
}
