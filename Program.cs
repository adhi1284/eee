using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;

namespace OVFTEAM;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private const string AppTitle = "OVFTEAM.COM - DEADSHOT.io";
    private static readonly string AssetDirectory = Path.Combine(AppContext.BaseDirectory, "assets");
    private static readonly Dictionary<string, string> AssetFiles = CreateAssetFiles();
    private static readonly Dictionary<string, string> AssetByName = CreateAssetByName();
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.Black };
    private CoreWebView2Environment? _environment;
    private bool _isFullscreen;
    private Rectangle _restoreBounds;
    private FormBorderStyle _restoreBorderStyle;
    private FormWindowState _restoreWindowState;
    private bool _restoreTopMost;

    public MainForm()
    {
        Text = AppTitle;
        Width = 1280;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;
        Icon = ReadAppIcon();
        WindowThemeSync.Attach(this);
        Controls.Add(_webView);
        Shown += async (_, _) => await InitializeAsync();
    }

    private static Dictionary<string, string> CreateAssetFiles()
    {
        if (!Directory.Exists(AssetDirectory))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return Directory.EnumerateFiles(AssetDirectory, "*", SearchOption.AllDirectories).ToDictionary(
            file => NormalizeRequestPath(Path.GetRelativePath(AssetDirectory, file)),
            file => file,
            StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> CreateAssetByName()
    {
        return AssetFiles.Keys.ToDictionary(
            path => path.Split('/')[^1],
            path => path,
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task InitializeAsync()
    {
        var options = new CoreWebView2EnvironmentOptions(BrowserArguments());
        _environment = await CoreWebView2Environment.CreateAsync(null, ProfileDir(), options);
        await _webView.EnsureCoreWebView2Async(_environment);

        ConfigureCore(_webView.CoreWebView2);
        _webView.CoreWebView2.Navigate("https://deadshot.io");
    }

    private void ConfigureCore(CoreWebView2 core)
    {
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.IsReputationCheckingRequired = false;
        core.Settings.AreDevToolsEnabled = Environment.GetEnvironmentVariable("DEBUG") == "1";
        foreach (var filter in AssetFilters)
        {
            core.AddWebResourceRequestedFilter(filter, CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
        }
        foreach (var domain in BlockedDomains)
        {
            core.AddWebResourceRequestedFilter($"*://{domain}/*", CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
            core.AddWebResourceRequestedFilter($"*://*.{domain}/*", CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
        }
        core.WebResourceRequested += OnWebResourceRequested;
        core.NewWindowRequested += OnNewWindowRequested;
        core.DocumentTitleChanged += (_, _) => Text = AppTitle;
        core.ContainsFullScreenElementChanged += (_, _) =>
        {
            if (core.ContainsFullScreenElement)
            {
                EnterFullscreen();
                return;
            }

            ExitFullscreen();
        };
    }
    private void EnterFullscreen()
    {
        if (_isFullscreen)
        {
            return;
        }
        _isFullscreen = true;
        _restoreBounds = Bounds;
        _restoreBorderStyle = FormBorderStyle;
        _restoreWindowState = WindowState;
        _restoreTopMost = TopMost;
        var screenBounds = Screen.FromControl(this).Bounds;
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        WindowState = FormWindowState.Normal;
        Bounds = screenBounds;
    }
    private void ExitFullscreen()
    {
        if (!_isFullscreen)
        {
            return;
        }
        _isFullscreen = false;
        TopMost = _restoreTopMost;
        FormBorderStyle = _restoreBorderStyle;
        WindowState = FormWindowState.Normal;
        Bounds = _restoreBounds;
        WindowState = _restoreWindowState;
    }
    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var uri = e.Request.Uri;
        if (TryMatchDeadshotAsset(uri, out var assetPath))
        {
            try
            {
                var stream = File.OpenRead(AssetFiles[assetPath]);
                var cacheControl = IsFinalPkgRequest(uri)
                    ? "no-store, max-age=0"
                    : "public, max-age=31536000";
                e.Response = CreateResponse(stream, ContentTypeFor(assetPath), cacheControl);
                return;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
        if (IsDeadshotUrl(uri))
        {
            return;
        }

        if (IsBlockedHost(uri))
        {
            e.Response = _environment!.CreateWebResourceResponse(
                Stream.Null,
                204,
                "No Content",
                "Content-Length: 0\r\nAccess-Control-Allow-Origin: *\r\n");
        }
    }

    private CoreWebView2WebResourceResponse CreateResponse(Stream content, string contentType, string cacheControl)
    {
        return _environment!.CreateWebResourceResponse(
            content,
            200,
            "OK",
            $"Content-Type: {contentType}\r\nAccess-Control-Allow-Origin: *\r\nCache-Control: {cacheControl}\r\n");
    }

    private async void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        // Docs: https://learn.microsoft.com/vi-vn/dotnet/api/microsoft.web.webview2.core.corewebview2newwindowrequestedeventargs?view=webview2-dotnet-1.0.4022.49
        e.Handled = true;
        if (!e.IsUserInitiated)
        {
            return;
        }
        if (IsGoogleLoginUrl(e.Uri) && _environment is not null)
        {
            var deferral = e.GetDeferral();
            try
            {
                var popup = new LoginForm(_environment);
                popup.Show(this);
                await popup.InitializeAsync();
                e.NewWindow = popup.Core;
            }
            finally
            {
                deferral.Complete();
            }
            return;
        }
        OpenExternalUrl(e.Uri);
    }
    private static void OpenExternalUrl(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            return;
        }
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
    private static bool IsGoogleLoginUrl(string rawUrl)
    {
        return Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri)
            && uri.Host.Equals("accounts.google.com", StringComparison.OrdinalIgnoreCase);
    }
    private static bool IsDeadshotUrl(string rawUrl)
    {
        return Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri)
            && uri.Host.Equals("deadshot.io", StringComparison.OrdinalIgnoreCase);
    }
    private static bool IsFinalPkgRequest(string rawUrl)
    {
        return Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri)
            && uri.Host.Equals("deadshot.io", StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.Equals("/final.pkg", StringComparison.OrdinalIgnoreCase);
    }
    private static string ProfileDir()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(baseDir, "OVFTEAM.COM - DEADSHOT.io", "WebView2");
        Directory.CreateDirectory(dir);
        return dir;
    }
    private static string BrowserArguments()
    {
        return string.Join(' ',
            "--autoplay-policy=no-user-gesture-required",
            // "--remote-debugging-port=0",
            "--disable-background-timer-throttling",
            "--disable-renderer-backgrounding",
            "--disable-backgrounding-occluded-windows",
            "--disable-features=msSmartScreenProtection",
            "--enable-gpu-rasterization",
            "--ignore-gpu-blocklist",
            "--no-first-run",
            "--disable-sync",
            "--disable-component-update");
    }
    private static readonly string[] BlockedDomains =
    [
        "adnxs.com",
        "adsafeprotected.com",
        "amazon-adsystem.com",
        "cloudflareinsights.com",
        "doubleclick.net",
        "google-analytics.com",
        "googleadservices.com",
        "googlesyndication.com",
        "googletagmanager.com",
        "googletagservices.com",
        "imasdk.googleapis.com",
        "pubmatic.com",
        "rubiconproject.com",
        "scorecardresearch.com",
        "smilewanted.com",
        "the-ozone-project.com",
        "tynt.com",
        "vntsm.com",
        "yellowblue.io"
    ];
    private static readonly string[] AssetFilters =
    [
        "https://deadshot.io/audio/*",
        "https://deadshot.io/css/*",
        "https://deadshot.io/final.pkg*",
        "https://deadshot.io/favicon.ico",
        "https://deadshot.io/favicon.png",
        "https://deadshot.io/promo/*",
        "https://deadshot.io/skins/*",
        "https://deadshot.io/textures/*"
    ];
    private static bool IsBlockedHost(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host;
        return BlockedDomains.Any(domain =>
            host.Equals(domain, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase));
    }
    private static bool TryMatchDeadshotAsset(string rawUrl, out string assetPath)
    {
        assetPath = string.Empty;
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri)
            || !uri.Host.Equals("deadshot.io", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        var requestPath = NormalizeRequestPath(uri.AbsolutePath);
        if (requestPath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
        {
            requestPath = requestPath["assets/".Length..];
        }
        if (AssetFiles.ContainsKey(requestPath))
        {
            assetPath = requestPath;
            return true;
        }
        var matchingPath = AssetFiles.Keys.FirstOrDefault(path =>
            requestPath.EndsWith($"/assets/{path}", StringComparison.OrdinalIgnoreCase)
            || requestPath.EndsWith($"/{path}", StringComparison.OrdinalIgnoreCase));
        if (matchingPath is not null)
        {
            assetPath = matchingPath;
            return true;
        }
        var fileName = requestPath.Split('/')[^1];
        return AssetByName.TryGetValue(fileName, out assetPath!);
    }
    private static string NormalizeRequestPath(string path)
    {
        return Uri.UnescapeDataString(path).Replace('\\', '/').TrimStart('/').ToLowerInvariant();
    }
    private static string ContentTypeFor(string assetPath)
    {
        return Path.GetExtension(assetPath).ToLowerInvariant() switch
        {
            ".css" => "text/css; charset=utf-8",
            ".glb" => "model/gltf-binary",
            ".js" => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".ico" => "image/x-icon",
            ".svg" => "image/svg+xml",
            ".woff2" => "font/woff2",
            ".woff" => "font/woff",
            ".ttf" => "font/ttf",
            ".mp3" => "audio/mpeg",
            ".pkg" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }
    internal static Icon ReadAppIcon()
    {
        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
    }
}
internal sealed class LoginForm : Form
{
    private readonly CoreWebView2Environment _environment;
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };

    public LoginForm(CoreWebView2Environment environment)
    {
        _environment = environment;
        Text = "Google Login";
        Icon = MainForm.ReadAppIcon();
        Width = 900;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;
        WindowThemeSync.Attach(this);
        Controls.Add(_webView);
    }
    public CoreWebView2 Core => _webView.CoreWebView2;
    public async Task InitializeAsync()
    {
        await _webView.EnsureCoreWebView2Async(_environment);
        _webView.CoreWebView2.WindowCloseRequested += (_, _) => Close();
        _webView.CoreWebView2.NavigationCompleted += async (_, _) =>
        {
            if (MainFormIsDeadshot(_webView.Source?.ToString()))
            {
                await Task.Delay(500);
                Close();
            }
        };
    }
    private static bool MainFormIsDeadshot(string? rawUrl)
    {
        return rawUrl is not null
            && Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri)
            && uri.Host.Equals("deadshot.io", StringComparison.OrdinalIgnoreCase);
    }
}
internal static partial class WindowThemeSync
{
    private const int DwMwaUseImmersiveDarkMode = 20;
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    public static void Attach(Form form)
    {
        form.HandleCreated += OnHandleCreated;
        form.Disposed += OnDisposed;
        if (form.IsHandleCreated)
        {
            Apply(form);
        }
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }
    private static void OnHandleCreated(object? sender, EventArgs e)
    {
        if (sender is Form form)
        {
            Apply(form);
        }
    }
    private static void OnDisposed(object? sender, EventArgs e)
    {
        if (sender is not Form form)
        {
            return;
        }
        form.HandleCreated -= OnHandleCreated;
        form.Disposed -= OnDisposed;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }
    private static void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General
            && e.Category != UserPreferenceCategory.VisualStyle
            && e.Category != UserPreferenceCategory.Color)
        {
            return;
        }
        foreach (Form form in Application.OpenForms)
        {
            if (form.IsDisposed || !form.IsHandleCreated)
            {
                continue;
            }

            if (form.InvokeRequired)
            {
                form.BeginInvoke(() => Apply(form));
                continue;
            }
            Apply(form);
        }
    }
    private static void Apply(Form form)
    {
        if (!OperatingSystem.IsWindows() || !form.IsHandleCreated)
        {
            return;
        }
        var useDarkTitleBar = !UsesLightTheme();
        DwmSetWindowAttribute(form.Handle, DwMwaUseImmersiveDarkMode, ref useDarkTitleBar, sizeof(int));
    }
    private static bool UsesLightTheme()
    {
        using var personalize = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        var value = personalize?.GetValue(AppsUseLightTheme);
        return value switch
        {
            0 => false,
            _ => true
        };
    }
    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attribute, [MarshalAs(UnmanagedType.Bool)] ref bool attributeValue, int attributeSize);
}
