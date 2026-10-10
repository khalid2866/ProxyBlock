using ProxyBlock.App.Services;

namespace ProxyBlock.App.Views;

public partial class OtpPage : ContentPage
{
    private readonly ApiService _api = new();
    private readonly string _email;

    public OtpPage(string email)
    {
        InitializeComponent();
        _email = email;
        InfoLabel.Text = $"We sent a 6-digit code to {_email}";
    }

    private async void OnVerifyClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = "";
        var code = CodeEntry.Text?.Trim() ?? "";
        if (code.Length != 6) { ErrorLabel.Text = "Enter the 6-digit code."; return; }
        try
        {
            var (ok, message) = await _api.VerifyOtpAsync(_email, code);
            if (!ok) { ErrorLabel.Text = message; return; }
            await DisplayAlert("Verified", "Your email is verified. Please login.", "OK");
            await Navigation.PopToRootAsync();
        }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; }
    }

    private async void OnResendClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = "";
        try
        {
            var (ok, message) = await _api.ResendOtpAsync(_email);
            ErrorLabel.Text = ok ? "New code sent." : message;
        }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; }
    }
}
