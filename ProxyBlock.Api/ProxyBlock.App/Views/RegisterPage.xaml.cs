using ProxyBlock.App.Services;
using System.Text.RegularExpressions;

namespace ProxyBlock.App.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = "";
        try
        {
            if (string.IsNullOrWhiteSpace(FullNameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                ErrorLabel.Text = "Please fill all fields.";
                return;
            }
            if (!EmailEntry.Text.Trim().EndsWith(".edu.pk", StringComparison.OrdinalIgnoreCase))
            {
                ErrorLabel.Text = "Use your university email ending with .edu.pk.";
                return;
            }
            if (GenderPicker.SelectedIndex < 0)
            {
                ErrorLabel.Text = "Please select Male or Female.";
                return;
            }
            if (RolePicker.SelectedIndex < 0)
            {
                ErrorLabel.Text = "Please select Student or Teacher.";
                return;
            }
            if (RolePicker.SelectedIndex == 0) // Student
            {
                var roll = RollNumberEntry.Text?.Trim().ToUpperInvariant() ?? "";
                if (!Regex.IsMatch(roll, @"^[FS]\d{2}[A-Z]+\d[A-Z]\d+$"))
                {
                    ErrorLabel.Text = "Roll number must follow the university pattern (e.g. F25BARIN1M01379).";
                    return;
                }
            }

            var api = new ApiService();
            var ok = await api.RegisterAsync(
                FullNameEntry.Text.Trim(),
                RollNumberEntry.Text?.Trim() ?? "",
                EmailEntry.Text.Trim(),
                PasswordEntry.Text,
                GenderPicker.SelectedIndex,  // 0 = Male, 1 = Female
                RolePicker.SelectedIndex);  // 0 = Student, 1 = Teacher

            if (!ok)
            {
                ErrorLabel.Text = "Registration failed (email/roll number may be taken).";
                return;
            }

            await DisplayAlert("Done", "Account created. Please login.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
        }
    }
}
