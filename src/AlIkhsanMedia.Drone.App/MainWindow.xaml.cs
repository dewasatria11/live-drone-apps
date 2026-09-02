using System.Windows;
using System.IO;
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
    private async void SaveQr(object sender, RoutedEventArgs e)
    {
        if (session is null || (sender as WpfButton)?.DataContext is not DashboardSlotViewModel slot || !session.SetupLinks.TryGetValue(slot.Slot.Id, out var link)) return;
        var uri = new Uri($"http://{slot.RtmpUrl.Host}:{session.PortalPort}/s/{link.Token}");
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "PNG image|*.png", FileName = $"{slot.DisplayName}-setup.png" }; if (dialog.ShowDialog() != true) return;
        await File.WriteAllBytesAsync(dialog.FileName, AlIkhsanMedia.Drone.SetupPortal.SetupQrCodeGenerator.GeneratePng(uri)); CopyMessage.Text = "QR setup berhasil disimpan";
    }
    private void ShowGuide(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("1. Hubungkan laptop dan drone pada jaringan yang sama.\n2. Salin URL RTMP ke DJI Fly.\n3. Salin URL RTSP ke vMix.\n4. Pastikan status slot Live sebelum siaran.", "Panduan Setup", MessageBoxButton.OK, MessageBoxImage.Information);
    private async void ShowDiagnostics(object sender, RoutedEventArgs e)
    {
        if (session is null) return;
        await session.Dashboard.RefreshAsync(default);
        var live = session.Dashboard.Slots.Count(slot => slot.IsLive);
        System.Windows.MessageBox.Show($"Engine: {session.Dashboard.EngineStatusText}\nSlot Live: {live} dari {session.Dashboard.Slots.Count}", "Diagnostik", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
