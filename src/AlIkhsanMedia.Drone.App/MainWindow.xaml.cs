using System.Windows;
using AlIkhsanMedia.Drone.Core;
using WpfButton = System.Windows.Controls.Button;
#pragma warning disable CA1001

namespace AlIkhsanMedia.Drone.App;

public partial class MainWindow : Window
{
    private RuntimeSession? session;
    private readonly System.Windows.Forms.NotifyIcon tray = new() { Icon = System.Drawing.SystemIcons.Application, Visible = true, Text = "Al Ikhsan Media (Drone Version)" };
    private bool allowClose;
    public MainWindow() { InitializeComponent(); tray.DoubleClick += (_, _) => ShowFromTray(); var menu = new System.Windows.Forms.ContextMenuStrip(); menu.Items.Add("Buka Dashboard", null, (_, _) => ShowFromTray()); menu.Items.Add("Keluar", null, (_, _) => { allowClose = true; Close(); }); tray.ContextMenuStrip = menu; Loaded += OnLoaded; Closing += OnClosing; Closed += (_, _) => tray.Dispose(); }
    private void ShowFromTray() { Show(); WindowState = WindowState.Normal; Activate(); }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try { session = await RuntimeSession.StartAsync(CancellationToken.None); DataContext = session.Dashboard; }
        catch (Exception ex) { EngineStatus.Text = "Perlu tindakan"; EngineMessage.Text = ex.Message; }
    }
    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!allowClose && session?.Dashboard.Slots.Any(static slot => slot.IsLive) == true)
        {
            var choice = System.Windows.MessageBox.Show("Video drone masih aktif. Pilih Yes untuk menghentikan dan keluar, No untuk tetap berjalan di tray, atau Cancel untuk kembali.", "Al Ikhsan Media", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (choice == MessageBoxResult.No) { e.Cancel = true; Hide(); return; }
            if (choice != MessageBoxResult.Yes) { e.Cancel = true; return; }
            allowClose = true;
        }
        if (session is not null) await session.DisposeAsync();
    }
    private async void CopyRtmp(object sender, RoutedEventArgs e) { if (session is not null && (sender as WpfButton)?.DataContext is DashboardSlotViewModel slot) { await session.Dashboard.CopyRtmpAsync(slot, default); CopyMessage.Text = "URL berhasil disalin"; } }
    private async void CopyRtsp(object sender, RoutedEventArgs e) { if (session is not null && (sender as WpfButton)?.DataContext is DashboardSlotViewModel slot) { await session.Dashboard.CopyRtspAsync(slot, default); CopyMessage.Text = "URL berhasil disalin"; } }
}
