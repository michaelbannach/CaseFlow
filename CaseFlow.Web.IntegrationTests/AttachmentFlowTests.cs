using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CaseFlow.Domain.Enums;
using Xunit;

namespace CaseFlow.Web.IntegrationTests;

public class AttachmentFlowTests : IClassFixture<CaseFlowWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AttachmentFlowTests(CaseFlowWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Upload_And_Download_Attachment_EndToEnd_Works()
    {
        // 1) Register + Login => JWT
        var email = $"u{Guid.NewGuid().ToString("N")[..8]}@t.de"; // keep < 50 chars
        const string password = "Test123!Test123!";

        await RegisterAsync(email, password);
        var token = await LoginAsync(email, password);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var employeeId = ExtractEmployeeIdFromJwt(token);

       
        // 3) Create FormCase
        var createBody = new
        {
            
            FormType = FormType.Kostenantrag,
            DepartmentId = 1,

            ApplicantName = "Integration Test",
            ApplicantStreet = "Teststreet 1",
            ApplicantZip = 12345,
            ApplicantCity = "TestCity",
            ApplicantPhone = "0000",
            ApplicantEmail = "integration@test.de",
            Subject = "Attachment E2E",
            Notes = "created by integration test",
            Amount = 12.34m,
            CostType = "Test",
            ServiceDescription = "Service",
            Justification = "Justification"
        };

        var createResp = await _client.PostAsJsonAsync("/api/formcases", createBody);
        if (createResp.StatusCode == HttpStatusCode.MethodNotAllowed)
        {
            var allow = createResp.Headers.TryGetValues("Allow", out var values)
                ? string.Join(", ", values)
                : "<none>";

            throw new Exception($"POST /api/formcases returned 405. Allow: {allow}");
        }

        if (createResp.StatusCode != HttpStatusCode.Created)
            throw await BuildHttpFailureExceptionAsync("Create FormCase failed", createResp);
        if (createResp.StatusCode != HttpStatusCode.Created)
            throw await BuildHttpFailureExceptionAsync("Create FormCase failed", createResp);

        var formCaseId = await ReadIntPropertyAsync(createResp, "id");
        Assert.True(formCaseId > 0);

        // 4) Upload (multipart/form-data) to: /api/formcases/{formCaseId}/attachments
        var pdfBytes = CreateMinimalPdfBytes();

        using var form = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "File", "test.pdf");

        form.Add(new StringContent(employeeId.ToString()), "UploadedByEmployeeId");

        var uploadResp = await _client.PostAsync($"/api/formcases/{formCaseId}/attachments", form);
        if (uploadResp.StatusCode != HttpStatusCode.Created)
            throw await BuildHttpFailureExceptionAsync("Upload attachment failed", uploadResp);

        var attachmentId = await ReadIntPropertyAsync(uploadResp, "id");
        Assert.True(attachmentId > 0);

        // 5) Download: /api/attachments/{id}/download
        var downloadResp = await _client.GetAsync($"/api/attachments/{attachmentId}/download");
        if (downloadResp.StatusCode != HttpStatusCode.OK)
            throw await BuildHttpFailureExceptionAsync("Download attachment failed", downloadResp);

        var downloadedBytes = await downloadResp.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(downloadedBytes);

        // PDF sanity check
        Assert.True(downloadedBytes.Length >= 4);
        Assert.Equal((byte)'%', downloadedBytes[0]);
        Assert.Equal((byte)'P', downloadedBytes[1]);
        Assert.Equal((byte)'D', downloadedBytes[2]);
        Assert.Equal((byte)'F', downloadedBytes[3]);

        Assert.Equal(pdfBytes, downloadedBytes);
    }

    private async Task RegisterAsync(string email, string password)
    {
        var registerBody = new
        {
            email,
            password,
            name = "Integration Test",
            role = "Erfasser",
            departmentId = (int?)null
        };

        var resp = await _client.PostAsJsonAsync("/api/auth/register", registerBody);

        if (resp.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.Created))
            throw await BuildHttpFailureExceptionAsync("Register failed", resp);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var loginBody = new { email, password };

        var resp = await _client.PostAsJsonAsync("/api/auth/login", loginBody);
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

    private static int ExtractEmployeeIdFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        var employeeIdClaim = token.Claims.FirstOrDefault(c => c.Type == "employeeId")?.Value;
        Assert.False(string.IsNullOrWhiteSpace(employeeIdClaim));

        return int.Parse(employeeIdClaim!);
    }

    private static byte[] CreateMinimalPdfBytes()
    {
        var pdf =
            "%PDF-1.4\n" +
            "1 0 obj\n<<>>\nendobj\n" +
            "trailer\n<<>>\n" +
            "%%EOF\n";

        return Encoding.UTF8.GetBytes(pdf);
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

    private static async Task AssertRouteAllowsPostAsync(string path)
    {
        // OPTIONS is the easiest way to get Allow header if the server provides it
        var req = new HttpRequestMessage(HttpMethod.Options, path);
        var resp = await new HttpClient { BaseAddress = new Uri("http://localhost") }.SendAsync(req);

        // If this fails due to BaseAddress mismatch, fall back to not using a separate client.
        // We'll do the real check using a safe GET attempt:
        // - If GET works but POST returns 405 => POST truly not mapped.
        // - If GET fails too => route might be different.
        if (resp.StatusCode == HttpStatusCode.MethodNotAllowed || resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.NoContent)
        {
            if (resp.Headers.TryGetValues("Allow", out var values))
            {
                var allow = string.Join(", ", values);
                if (!allow.Contains("POST", StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"Route '{path}' does NOT allow POST. Allow: {allow}");
                return;
            }
        }

        // Fallback: simple GET probe
        // (If GET is 200/401/403, route exists. If 404, route likely different.)
        // This is still a useful signal.
        // NOTE: We can’t reuse _client here because this is static; keep message actionable.
        throw new Exception($"Could not reliably determine allowed methods for '{path}'. " +
                            $"OPTIONS returned {(int)resp.StatusCode} {resp.StatusCode}. " +
                            $"If POST /api/formcases returns 405, your Create endpoint route is different in the current codebase.");
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
