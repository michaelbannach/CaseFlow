using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CaseFlow.Domain.Enums;
using CaseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaseFlow.Web.IntegrationTests;

public class ClarificationFlowTests : IClassFixture<CaseFlowWebApplicationFactory>
{
    private readonly CaseFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ClarificationFlowTests(CaseFlowWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Sachbearbeiter_Can_Add_Clarification_When_Case_Is_InBearbeitung_And_Get_Returns_Message()
    {
        // Arrange: Erfasser creates case
        var erfasserEmail = NewShortEmail("e");
        const string erfasserPassword = "Test123!Test123!";
        await RegisterAsync(erfasserEmail, erfasserPassword, role: "Erfasser", departmentId: null);

        var erfasserToken = await LoginAsync(erfasserEmail, erfasserPassword);
        SetBearer(erfasserToken);

        var departmentId = await GetAnyDepartmentIdAsync();
        var formCaseId = await CreateKostenantragAsync(departmentId);

        // IMPORTANT: attach at least one PDF before leaving Neu
        await EnsureAtLeastOnePdfAsync(formCaseId);

        // Arrange: create Sachbearbeiter in SAME department as the case
        var sbEmail = NewShortEmail("sb");
        const string sbPassword = "Test123!Test123!";
        await RegisterAsync(sbEmail, sbPassword, role: "Sachbearbeiter", departmentId: departmentId);

        var sbToken = await LoginAsync(sbEmail, sbPassword);
        SetBearer(sbToken);

        // Act: allowed transition Neu -> InBearbeitung
        await SetStatusAsync(formCaseId, ProcessingStatus.InBearbeitung);

        // Act: create clarification (NEW RULE: only allowed in InBearbeitung)
        const string message = "Bitte fehlende Unterlagen nachreichen.";
        var clarificationId = await CreateClarificationAsync(formCaseId, message);
        Assert.True(clarificationId > 0);

        // Optional but realistic: after adding clarification, set case to InKlaerung
        await SetStatusAsync(formCaseId, ProcessingStatus.InKlaerung);

        // Act: GET clarifications
        var getResp = await _client.GetAsync($"/api/formcases/{formCaseId}/clarifications");
        if (getResp.StatusCode != HttpStatusCode.OK)
            throw await BuildHttpFailureExceptionAsync("Get clarifications failed", getResp);

        var json = await getResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() >= 1);

        var last = doc.RootElement.EnumerateArray().Last();
        Assert.Equal(formCaseId, last.GetProperty("formCaseId").GetInt32());
        Assert.Equal(message, last.GetProperty("message").GetString());
        Assert.True(last.GetProperty("createdAt").GetDateTimeOffset() > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task Create_Clarification_When_Case_Not_InBearbeitung_Returns_400()
    {
        // Arrange: Erfasser creates case (Status remains Neu)
        var erfasserEmail = NewShortEmail("e");
        const string erfasserPassword = "Test123!Test123!";
        await RegisterAsync(erfasserEmail, erfasserPassword, role: "Erfasser", departmentId: null);

        var erfasserToken = await LoginAsync(erfasserEmail, erfasserPassword);
        SetBearer(erfasserToken);

        var departmentId = await GetAnyDepartmentIdAsync();
        var formCaseId = await CreateKostenantragAsync(departmentId);

        // IMPORTANT: attach at least one PDF before leaving Neu
        await EnsureAtLeastOnePdfAsync(formCaseId);

        // Arrange: create Sachbearbeiter in SAME department as the case
        var sbEmail = NewShortEmail("sb");
        const string sbPassword = "Test123!Test123!";
        await RegisterAsync(sbEmail, sbPassword, role: "Sachbearbeiter", departmentId: departmentId);

        var sbToken = await LoginAsync(sbEmail, sbPassword);
        SetBearer(sbToken);

        // Act 1: try to add clarification while case is still Neu -> must fail
        var postRespNeu = await _client.PostAsJsonAsync(
            $"/api/formcases/{formCaseId}/clarifications",
            new { Message = "Das sollte nicht gehen (Neu)." });

        Assert.Equal(HttpStatusCode.BadRequest, postRespNeu.StatusCode);

        // Act 2 (optional but useful): move to InKlaerung and try again -> must fail
        await SetStatusAsync(formCaseId, ProcessingStatus.InBearbeitung);
        await SetStatusAsync(formCaseId, ProcessingStatus.InKlaerung);

        var postRespInKlaerung = await _client.PostAsJsonAsync(
            $"/api/formcases/{formCaseId}/clarifications",
            new { Message = "Das sollte nicht gehen (InKlaerung)." });

        Assert.Equal(HttpStatusCode.BadRequest, postRespInKlaerung.StatusCode);
    }

    [Fact]
    public async Task Stammdaten_Cannot_Add_Clarification_Returns_400()
    {
        // Arrange: Erfasser creates case
        var erfasserEmail = NewShortEmail("e");
        const string erfasserPassword = "Test123!Test123!";
        await RegisterAsync(erfasserEmail, erfasserPassword, role: "Erfasser", departmentId: null);

        var erfasserToken = await LoginAsync(erfasserEmail, erfasserPassword);
        SetBearer(erfasserToken);

        var departmentId = await GetAnyDepartmentIdAsync();
        var formCaseId = await CreateKostenantragAsync(departmentId);

        // IMPORTANT: attach at least one PDF before leaving Neu
        await EnsureAtLeastOnePdfAsync(formCaseId);

        // Arrange: create Sachbearbeiter in SAME department and move case to InBearbeitung
        var sbEmail = NewShortEmail("sb");
        const string sbPassword = "Test123!Test123!";
        await RegisterAsync(sbEmail, sbPassword, role: "Sachbearbeiter", departmentId: departmentId);

        var sbToken = await LoginAsync(sbEmail, sbPassword);
        SetBearer(sbToken);

        await SetStatusAsync(formCaseId, ProcessingStatus.InBearbeitung);

        // Arrange: create Stammdaten user (no department needed)
        var stammdatenEmail = NewShortEmail("s");
        const string stammdatenPassword = "Test123!Test123!";
        await RegisterAsync(stammdatenEmail, stammdatenPassword, role: "Stammdaten", departmentId: null);

        var stammdatenToken = await LoginAsync(stammdatenEmail, stammdatenPassword);
        SetBearer(stammdatenToken);

        // Act: attempt to post clarification
        var postResp = await _client.PostAsJsonAsync(
            $"/api/formcases/{formCaseId}/clarifications",
            new { Message = "Ich darf das nicht." });

        Assert.Equal(HttpStatusCode.BadRequest, postResp.StatusCode);
    }

    [Fact]
    public async Task Get_Clarifications_For_NonExisting_FormCase_Returns_404()
    {
        // Arrange: create any authenticated user (Erfasser is enough)
        var email = NewShortEmail("e");
        const string password = "Test123!Test123!";
        await RegisterAsync(email, password, role: "Erfasser", departmentId: null);

        var token = await LoginAsync(email, password);
        SetBearer(token);

        // Act
        var resp = await _client.GetAsync("/api/formcases/999999/clarifications");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---------------- Helpers ----------------

    private static string NewShortEmail(string prefix)
        => $"{prefix}{Guid.NewGuid():N}"[..9] + "@t.de"; // always << 50 chars

    private void SetBearer(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task RegisterAsync(string email, string password, string role, int? departmentId)
    {
        if (email.Length > 50)
            email = email[..50];

        var registerBody = new
        {
            email,
            password,
            name = "Integration Test",
            role,
            departmentId
        };

        var resp = await _client.PostAsJsonAsync("/api/auth/register", registerBody);

        if (resp.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.Created))
            throw await BuildHttpFailureExceptionAsync("Register failed", resp);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        if (resp.StatusCode != HttpStatusCode.OK)
            throw await BuildHttpFailureExceptionAsync("Login failed", resp);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("token", out var tokenEl))
            throw new Exception($"Login response missing 'token'. Body: {json}");

        var token = tokenEl.GetString();
        if (string.IsNullOrWhiteSpace(token))
            throw new Exception($"Login returned empty token. Body: {json}");

        return token!;
    }

    private async Task<int> GetAnyDepartmentIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Departments
            .OrderBy(d => d.Id)
            .Select(d => d.Id)
            .FirstAsync();
    }

    private async Task<int> CreateKostenantragAsync(int departmentId)
    {
        var createResp = await _client.PostAsJsonAsync("/api/formcases", new
        {
            FormType = FormType.Kostenantrag,
            DepartmentId = departmentId,

            ApplicantName = "Integration Test",
            ApplicantStreet = "Teststreet 1",
            ApplicantZip = 12345,
            ApplicantCity = "TestCity",
            ApplicantPhone = "0000",
            ApplicantEmail = "integration@test.de",

            Subject = "Clarification Test",
            Notes = "created by integration test",

            Amount = 12.34m,
            CostType = "Test"
        });

        if (createResp.StatusCode != HttpStatusCode.Created)
            throw await BuildHttpFailureExceptionAsync("Create FormCase failed", createResp);

        var id = await ReadIntPropertyAsync(createResp, "id");
        Assert.True(id > 0);
        return id;
    }

    private async Task EnsureAtLeastOnePdfAsync(int formCaseId)
    {
        // Minimal PDF content; enough to satisfy "at least one attachment exists"
        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4\n%âãÏÓ\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF");

        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        // Name "file" must match your AttachmentsController parameter name.
        content.Add(fileContent, "file", "test.pdf");

        var resp = await _client.PostAsync($"/api/formcases/{formCaseId}/attachments", content);

        if (resp.StatusCode != HttpStatusCode.Created &&
            resp.StatusCode != HttpStatusCode.OK &&
            resp.StatusCode != HttpStatusCode.NoContent)
        {
            throw await BuildHttpFailureExceptionAsync("Upload attachment failed", resp);
        }
    }

    private async Task SetStatusAsync(int formCaseId, ProcessingStatus newStatus)
    {
        var patchResp = await _client.PatchAsJsonAsync(
            $"/api/formcases/{formCaseId}/status",
            new { NewStatus = newStatus });

        if (patchResp.StatusCode != HttpStatusCode.NoContent)
            throw await BuildHttpFailureExceptionAsync($"Patch status to {newStatus} failed", patchResp);
    }

    private async Task<int> CreateClarificationAsync(int formCaseId, string message)
    {
        var postResp = await _client.PostAsJsonAsync(
            $"/api/formcases/{formCaseId}/clarifications",
            new { Message = message });

        if (postResp.StatusCode != HttpStatusCode.Created)
            throw await BuildHttpFailureExceptionAsync("Create clarification failed", postResp);

        return await ReadIntPropertyAsync(postResp, "id");
    }

    private static async Task<int> ReadIntPropertyAsync(HttpResponseMessage response, string propertyName)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty(propertyName, out var prop))
            throw new Exception($"Response missing '{propertyName}'. Status {(int)response.StatusCode} {response.StatusCode}. Body: {json}");

        if (prop.ValueKind != JsonValueKind.Number || !prop.TryGetInt32(out var value))
            throw new Exception($"Response property '{propertyName}' is not an int. Status {(int)response.StatusCode} {response.StatusCode}. Body: {json}");

        return value;
    }

    private static async Task<Exception> BuildHttpFailureExceptionAsync(string title, HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync();
        var req = resp.RequestMessage;

        return new Exception(
            $"{title}\n" +
            $"Status: {(int)resp.StatusCode} {resp.StatusCode}\n" +
            $"Request: {req?.Method} {req?.RequestUri}\n" +
            $"Body: {body}"
        );
    }
}
