using Microsoft.Web.WebView2.Wpf;

namespace AlIkhsanMedia.Drone.App;

internal sealed class PreviewController
{
    private WebView2? active;
    public async Task<bool> ShowAsync(WebView2 view, Uri previewUri, CancellationToken ct)
    {
        if (previewUri.Host is not ("127.0.0.1" or "localhost")) return false;
        if (active is not null && !ReferenceEquals(active, view)) active.Visibility = System.Windows.Visibility.Collapsed;
        active = view; view.Visibility = System.Windows.Visibility.Visible;
        await view.EnsureCoreWebView2Async(); ct.ThrowIfCancellationRequested();
        view.CoreWebView2.NavigationStarting += (_, e) => { if (e.Uri is not null && !e.Uri.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase)) e.Cancel = true; };
        view.Source = previewUri; return true;
    }
    public void Hide(WebView2 view) { if (ReferenceEquals(active, view)) { view.Visibility = System.Windows.Visibility.Collapsed; active = null; } }
}
