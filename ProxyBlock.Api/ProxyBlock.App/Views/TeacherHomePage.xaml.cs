using ProxyBlock.App.Services;

namespace ProxyBlock.App.Views;

public partial class TeacherHomePage : ContentPage
{
    private readonly ApiService _api = new();
    private int _sessionId;
    private int? _sectionId;
    private IDispatcherTimer? _timer;

    public TeacherHomePage(string token)
    {
        InitializeComponent();
        _api.SetToken(token);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSectionsAsync();
    }

    private async Task LoadSectionsAsync()
    {
        try { SectionsList.ItemsSource = await _api.GetMySectionsAsync(); }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; }
    }

    private void OnSectionSelected(object sender, SelectionChangedEventArgs e)
    {
        var s = e.CurrentSelection.FirstOrDefault() as SectionItem;
        _sectionId = s?.Id;
        SelectedLabel.Text = s == null
            ? "No class selected"
            : $"Selected: {s.CourseCode} {s.SectionName} ({s.StudentCount} students)";
    }

    private async void OnCreateSectionClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = "";
        try
        {
            var (ok, message, _) = await _api.CreateSectionAsync(
                CourseNameEntry.Text?.Trim() ?? "",
                CourseCodeEntry.Text?.Trim() ?? "",
                SectionNameEntry.Text?.Trim() ?? "");
            if (!ok) { ErrorLabel.Text = message; return; }
            CourseNameEntry.Text = CourseCodeEntry.Text = SectionNameEntry.Text = "";
            await LoadSectionsAsync();
        }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; }
    }

    private async void OnUploadRosterClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = "";
        try
        {
            var sectionName = SectionNameEntry.Text?.Trim() ?? "";
            var courseName = CourseNameEntry.Text?.Trim() ?? "";
            var courseCode = CourseCodeEntry.Text?.Trim() ?? "";
            if (sectionName == "" || courseName == "" || courseCode == "")
            {
                ErrorLabel.Text = "Fill course name, code and section first, then upload the list.";
                return;
            }

            var file = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
                    { DevicePlatform.WinUI, new[] { ".xlsx" } }
                })
            });
            if (file == null) return;

            using var stream = await file.OpenReadAsync();
            var (ok, message) = await _api.UploadRosterAsync(
                sectionName, courseName, courseCode, stream, file.FileName);

            if (!ok) { ErrorLabel.Text = message; return; }
            await DisplayAlert("Roster uploaded", message, "OK");
            CourseNameEntry.Text = CourseCodeEntry.Text = SectionNameEntry.Text = "";
            await LoadSectionsAsync();
        }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; }
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = "";
        var section = SectionsList.SelectedItem as SectionItem;
        if (section == null) { ErrorLabel.Text = "Select a class first."; return; }
        try
        {
            var session = await _api.StartSessionAsync(
                $"{section.CourseCode} {section.SectionName}", section.Id);
            if (session == null) { ErrorLabel.Text = "Could not start session."; return; }

            _sessionId = session.Id;
            _sectionId = section.Id;
            TokenLabel.Text = session.CurrentToken;
            QrView.Value = $"{_sessionId}:{session.CurrentToken}";
            QrView.IsVisible = true;
            InfoLabel.Text = $"Session {_sessionId} — QR refreshes every 10s";
            StartButton.IsVisible = false;
            EndButton.IsVisible = true;
            RosterButton.IsVisible = true;
            await RefreshRosterAsync();

            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(10);
            _timer.Tick += async (s, ev) =>
            {
                var t = await _api.GetTokenAsync(_sessionId);
                if (t != null)
                {
                    TokenLabel.Text = t;
                    QrView.Value = $"{_sessionId}:{t}";
                }
                await RefreshRosterAsync();
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
            QrView.Value = "";
            QrView.IsVisible = false;
            InfoLabel.Text = "Session ended.";
            StartButton.IsVisible = true;
            EndButton.IsVisible = false;
            RosterButton.IsVisible = false;
        }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; }
    }

    private async void OnAddStudentClicked(object sender, EventArgs e)
    {
        ErrorLabel.Text = "";
        var section = SectionsList.SelectedItem as SectionItem;
        if (section == null) { ErrorLabel.Text = "Select a class first."; return; }
        var roll = RollEntry.Text?.Trim() ?? "";
        if (roll == "") { ErrorLabel.Text = "Enter a roll number."; return; }
        try
        {
            var (ok, message) = await _api.AddStudentAsync(section.Id, roll);
            if (!ok) { ErrorLabel.Text = message; return; }
            RollEntry.Text = "";
            await DisplayAlert("Added", $"{roll} is now enrolled.", "OK");
            await LoadSectionsAsync();
        }
        catch (Exception ex) { ErrorLabel.Text = ex.Message; }
    }

    private async void OnRosterClicked(object sender, EventArgs e) =>
        await RefreshRosterAsync();

    private async Task RefreshRosterAsync()
    {
        if (_sectionId == null) return;
        try
        {
            var roster = await _api.GetRosterAsync(_sectionId.Value, _sessionId);
            if (roster == null) return;
            RosterSummaryLabel.Text = $"Present: {roster.Present} / {roster.Total}";
            RosterList.ItemsSource = roster.Roster;
        }
        catch { /* roster refresh is best-effort */ }
    }
}
