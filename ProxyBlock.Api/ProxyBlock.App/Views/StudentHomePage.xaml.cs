using ProxyBlock.App.Services;

namespace ProxyBlock.App.Views;

public partial class StudentHomePage : ContentPage
{
    private readonly ApiService _api = new();
    private readonly string _token;

    public StudentHomePage(string token)
    {
        InitializeComponent();
        _token = token;
        _api.SetToken(token);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSectionsAsync();
    }

    private async Task LoadSectionsAsync()
    {
        try { SectionsList.ItemsSource = await _api.GetEnrolledSectionsAsync(); }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; }
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Camera needed", "Allow camera access to scan the QR code.", "OK");
            return;
        }
        await Navigation.PushAsync(new ScanPage(_token));
    }

    private async void OnMarkClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = "";
        ResultLabel.Text = "";
        try
        {
            if (!int.TryParse(SessionEntry.Text, out var sessionId))
            {
                ErrorLabel.Text = "Enter a valid session ID.";
                return;
            }
            var (ok, message) = await _api.MarkAttendanceAsync(
                sessionId, TokenEntry.Text?.Trim() ?? "");
            if (ok) ResultLabel.Text = "Marked present!";
            else ErrorLabel.Text = message;
        }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; }
    }
}
