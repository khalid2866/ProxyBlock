using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace ProxyBlock.App.Services;

public class LoginResult
{
    public string Token { get; set; } = "";
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public int Role { get; set; }
}

public class SessionResult
{
    public int Id { get; set; }
    public string CurrentToken { get; set; } = "";
    public DateTime TokenExpiresAt { get; set; }
}

public class TokenResult
{
    public string CurrentToken { get; set; } = "";
    public DateTime TokenExpiresAt { get; set; }
}

public class SectionItem
{
    public int Id { get; set; }
    public string CourseName { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public string SectionName { get; set; } = "";
    public int StudentCount { get; set; }
    public string TeacherName { get; set; } = "";
}

public class CreateSectionResult
{
    public int Id { get; set; }
}

public class RosterItem
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string RollNumber { get; set; } = "";
    public bool Present { get; set; }
}

public class RosterResult
{
    public int Total { get; set; }
    public int Present { get; set; }
    public List<RosterItem> Roster { get; set; } = new();
}

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService()
    {
#if ANDROID
        // Emulator reaches your PC's localhost through 10.0.2.2.
        // The dev HTTPS cert isn't trusted on the emulator, so we bypass
        // validation here — DEV ONLY, never in a release build.
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
        };
        var baseUrl = "https://10.0.2.2:7173";
#else
        var handler = new HttpClientHandler();
        var baseUrl = "https://localhost:7173";
#endif
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<LoginResult?> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync("/api/Auth/login",
            new { email, password });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<LoginResult>();
    }

    public async Task<bool> RegisterAsync(string fullName, string rollNumber,
        string email, string password, int gender, int role)
    {
        var res = await _http.PostAsJsonAsync("/api/Auth/register",
            new { fullName, rollNumber, email, password, gender, role });
        return res.IsSuccessStatusCode;
    }

    public async Task<SessionResult?> StartSessionAsync(string courseName, int? sectionId = null)
    {
        var res = await _http.PostAsJsonAsync("/api/Sessions/start",
            new { courseName, latitude = 29.39, longitude = 71.69, sectionId });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<SessionResult>();
    }

    public async Task<string?> GetTokenAsync(int sessionId)
    {
        var doc = await _http.GetFromJsonAsync<TokenResult>(
            $"/api/Sessions/{sessionId}/token");
        return doc?.CurrentToken;
    }

    public async Task EndSessionAsync(int sessionId)
    {
        await _http.PostAsync($"/api/Sessions/{sessionId}/end", null);
    }

    public async Task<(bool ok, string message)> MarkAttendanceAsync(
        int sessionId, string token)
    {
        var res = await _http.PostAsJsonAsync("/api/Attendance/mark",
            new { sessionId, token });
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    // ----- Class sections -----

    public async Task<(bool ok, string message, int sectionId)> CreateSectionAsync(
        string courseName, string courseCode, string sectionName)
    {
        var res = await _http.PostAsJsonAsync("/api/Sections",
            new { courseName, courseCode, sectionName });
        if (!res.IsSuccessStatusCode)
            return (false, await res.Content.ReadAsStringAsync(), 0);
        var created = await res.Content.ReadFromJsonAsync<CreateSectionResult>();
        return (true, "Section created.", created?.Id ?? 0);
    }

    public async Task<(bool ok, string message)> UploadRosterAsync(
        string sectionName, string courseName, string courseCode,
        Stream fileStream, string fileName)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(sectionName), "sectionName");
        form.Add(new StringContent(courseName), "courseName");
        form.Add(new StringContent(courseCode), "courseCode");
        form.Add(new StreamContent(fileStream), "file", fileName);
        var res = await _http.PostAsync("api/Sections/upload-roster", form);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<List<SectionItem>> GetMySectionsAsync()
    {
        return await _http.GetFromJsonAsync<List<SectionItem>>("/api/Sections/mine")
            ?? new();
    }

    public async Task<List<SectionItem>> GetEnrolledSectionsAsync()
    {
        return await _http.GetFromJsonAsync<List<SectionItem>>("/api/Sections/enrolled")
            ?? new();
    }

    public async Task<(bool ok, string message)> AddStudentAsync(int sectionId, string rollNumber)
    {
        var res = await _http.PostAsJsonAsync($"/api/Sections/{sectionId}/students",
            new { rollNumber });
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<RosterResult?> GetRosterAsync(int sectionId, int sessionId)
    {
        return await _http.GetFromJsonAsync<RosterResult>(
            $"/api/Sections/{sectionId}/roster?sessionId={sessionId}");
    }

    public async Task<(bool ok, string message)> VerifyOtpAsync(string email, string code)
    {
        var res = await _http.PostAsJsonAsync("/api/Auth/verify-otp",
            new { email, code });
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<(bool ok, string message)> ResendOtpAsync(string email)
    {
        var res = await _http.PostAsJsonAsync("/api/Auth/resend-otp",
            new { email });
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public void SetToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
}
