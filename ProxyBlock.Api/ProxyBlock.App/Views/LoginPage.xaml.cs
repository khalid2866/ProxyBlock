using ProxyBlock.App.Services;

namespace ProxyBlock.App.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = "";
        try
        {
            var api = new ApiService();
            var result = await api.LoginAsync(
                EmailEntry.Text?.Trim() ?? "", PasswordEntry.Text ?? "");

            if (result == null)
            {
                ErrorLabel.Text = "Invalid email or password.";
                return;
            }

            await SecureStorage.Default.SetAsync("token", result.Token);
            await SecureStorage.Default.SetAsync("role", result.Role.ToString());

            if (result.Role == 1)
                await Navigation.PushAsync(new TeacherHomePage(result.Token));
            else
                await Navigation.PushAsync(new StudentHomePage(result.Token));
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}
