using ProxyBlock.App.Services;
using ZXing.Net.Maui;

namespace ProxyBlock.App.Views;

public partial class ScanPage : ContentPage
{
    private readonly ApiService _api = new();
    private bool _handled;

    public ScanPage(string token)
    {
        InitializeComponent();
        _api.SetToken(token);
        CameraView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        CameraView.IsDetecting = false;
    }

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_handled) return;
        var value = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(value)) return;
        _handled = true;
        CameraView.IsDetecting = false;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var parts = value.Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[0], out var sessionId))
                {
                    await DisplayAlert("Invalid QR", "This is not a ProxyBlock QR code.", "OK");
                    await Navigation.PopAsync();
                    return;
                }
                var result = await _api.MarkAttendanceAsync(sessionId, parts[1]);
                await DisplayAlert(result.ok ? "Done" : "Failed",
                    result.ok ? "Attendance marked." : result.message,
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            await Navigation.PopAsync();
        });
    }
}
