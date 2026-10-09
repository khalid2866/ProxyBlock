using ProxyBlock.App.Services;

namespace ProxyBlock.App.Views;

public partial class StudentHomePage : ContentPage
{
    private readonly ApiService _api = new();

    public StudentHomePage(string token)
    {
        InitializeComponent();
        _api.SetToken(token);
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
