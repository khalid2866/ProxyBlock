using ProxyBlock.App.Services;

namespace ProxyBlock.App.Views;

public partial class TeacherHomePage : ContentPage
{
    private readonly ApiService _api = new();
    private int _sessionId;
    private IDispatcherTimer? _timer;

    public TeacherHomePage(string token)
    {
        InitializeComponent();
        _api.SetToken(token);
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = "";
        try
        {
            var session = await _api.StartSessionAsync(
                CourseEntry.Text?.Trim() ?? "SCD");
            if (session == null)
            {
                ErrorLabel.Text = "Could not start session.";
                return;
            }
            _sessionId = session.Id;
            TokenLabel.Text = session.CurrentToken;
            InfoLabel.Text = $"Session {_sessionId} — token refreshes every 10s";
            StartButton.IsVisible = false;
            EndButton.IsVisible = true;

            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(10);
            _timer.Tick += async (s, ev) =>
            {
                var t = await _api.GetTokenAsync(_sessionId);
                if (t != null) TokenLabel.Text = t;
            };
            _timer.Start();
        }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; }
    }

    private async void OnEndClicked(object sender, EventArgs e)
    {
        try
        {
            _timer?.Stop();
            await _api.EndSessionAsync(_sessionId);
            TokenLabel.Text = "";
            InfoLabel.Text = "Session ended.";
            StartButton.IsVisible = true;
            EndButton.IsVisible = false;
        }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; }
    }
}
