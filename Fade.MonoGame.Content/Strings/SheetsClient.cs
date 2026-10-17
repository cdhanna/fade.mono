using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Fade.MonoGame.Content.Strings;

/// <summary>
/// The few Google Sheets API v4 calls the strings tool needs, over plain HttpClient: read every
/// tab, append rows, write ranges, and spreadsheet batch updates (formatting, metadata).
///
/// Auth is a service account: a JSON key from Google Cloud, whose email the sheet is shared with.
/// The key signs a JWT (RS256), which Google trades for an access token good for an hour.
/// The key is found, in order, at --credentials, at $FADE_SHEETS_CREDENTIALS, or at
/// .secrets/google-sheets-sa.json in any folder above the strings folder. It must never be put
/// under Assets/, where the content build would ship it.
/// </summary>
public sealed class SheetsClient : IDisposable
{
    public const string SCOPE = "https://www.googleapis.com/auth/spreadsheets";
    private const string API = "https://sheets.googleapis.com/v4/spreadsheets/";
    private const string DEFAULT_KEY_PATH = ".secrets/google-sheets-sa.json";

    private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    private readonly string _email;
    private readonly string _privateKey;
    private readonly string _tokenUri;
    private string? _token;
    private DateTime _tokenExpires;

    public string Email => _email;

    public SheetsClient(string keyPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(keyPath));
        var root = doc.RootElement;
        _email = root.GetProperty("client_email").GetString() ?? throw new InvalidDataException("the key has no client_email");
        _privateKey = root.GetProperty("private_key").GetString() ?? throw new InvalidDataException("the key has no private_key");
        _tokenUri = root.TryGetProperty("token_uri", out var uri) ? uri.GetString()! : "https://oauth2.googleapis.com/token";
    }

    /// <summary>Where the service-account key is, or null when there is none.</summary>
    public static string? FindKey(string? explicitPath, string searchFrom)
    {
        if (!string.IsNullOrEmpty(explicitPath)) return File.Exists(explicitPath) ? explicitPath : null;
        var env = Environment.GetEnvironmentVariable("FADE_SHEETS_CREDENTIALS");
        if (!string.IsNullOrEmpty(env)) return File.Exists(env) ? env : null;
        for (var dir = new DirectoryInfo(searchFrom); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, DEFAULT_KEY_PATH);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    /// <summary>The spreadsheet id, from either a bare id or a docs.google.com URL.</summary>
    public static string ParseSheetId(string urlOrId)
    {
        const string marker = "/spreadsheets/d/";
        var at = urlOrId.IndexOf(marker, StringComparison.Ordinal);
        if (at < 0) return urlOrId.Trim();
        var rest = urlOrId.Substring(at + marker.Length);
        var end = rest.IndexOfAny(new[] { '/', '?', '#' });
        return end < 0 ? rest : rest.Substring(0, end);
    }

    private async Task<string> GetTokenAsync()
    {
        if (_token != null && DateTime.UtcNow < _tokenExpires) return _token;

        static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = B64(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        var claims = B64(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = _email, scope = SCOPE, aud = _tokenUri, iat = now, exp = now + 3600
        }));
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_privateKey);
        var signature = B64(rsa.SignData(Encoding.ASCII.GetBytes(header + "." + claims), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

        using var response = await _http.PostAsync(_tokenUri, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = header + "." + claims + "." + signature,
        }));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new SheetsException($"Google refused the service account key: {body}");

        var json = JsonNode.Parse(body)!;
        _token = json["access_token"]!.GetValue<string>();
        _tokenExpires = DateTime.UtcNow.AddSeconds(json["expires_in"]!.GetValue<int>() - 120);
        return _token;
    }

    private async Task<JsonNode> SendAsync(HttpMethod method, string url, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync());
        if (body != null)
            request.Content = new StringContent(body is JsonNode n ? n.ToJsonString() : JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            var message = text;
            try { message = JsonNode.Parse(text)?["error"]?["message"]?.GetValue<string>() ?? text; } catch { }
            throw new SheetsException($"{(int)response.StatusCode} {response.ReasonPhrase}: {message}", (int)response.StatusCode);
        }
        return text.Length == 0 ? new JsonObject() : JsonNode.Parse(text)!;
    }

    /// <summary>A range in A1 notation on one tab, with the tab name quoted.</summary>
    public static string Range(string tab, string a1) => "'" + tab.Replace("'", "''") + "'!" + a1;

    /// <summary>The spreadsheet's title, tabs (title and sheetId), and spreadsheet-level developer metadata.</summary>
    public async Task<SheetInfo> GetInfoAsync(string spreadsheetId)
    {
        var json = await SendAsync(HttpMethod.Get, API + spreadsheetId
            + "?fields=properties.title,sheets.properties(sheetId,title,gridProperties),developerMetadata");
        var info = new SheetInfo { title = json["properties"]?["title"]?.GetValue<string>() ?? "" };
        foreach (var sheet in json["sheets"]?.AsArray() ?? new JsonArray())
        {
            var p = sheet!["properties"]!;
            info.tabs.Add((p["title"]!.GetValue<string>(), p["sheetId"]!.GetValue<int>(),
                p["gridProperties"]?["columnCount"]?.GetValue<int>() ?? 26));
        }
        foreach (var m in json["developerMetadata"]?.AsArray() ?? new JsonArray())
        {
            var key = m!["metadataKey"]?.GetValue<string>();
            if (key != null) info.metadata[key] = m["metadataValue"]?.GetValue<string>() ?? "";
        }
        return info;
    }

    /// <summary>Every cell of each tab, as strings, rows padded to the longest row.</summary>
    public async Task<Dictionary<string, List<string[]>>> ReadTabsAsync(string spreadsheetId, IEnumerable<string> tabs)
    {
        var list = tabs.ToList();
        var result = new Dictionary<string, List<string[]>>();
        if (list.Count == 0) return result;

        var query = string.Join("&", list.Select(t => "ranges=" + Uri.EscapeDataString(Range(t, "A:ZZ"))));
        var json = await SendAsync(HttpMethod.Get, API + spreadsheetId + "/values:batchGet?" + query);
        var ranges = json["valueRanges"]?.AsArray() ?? new JsonArray();
        for (var i = 0; i < list.Count && i < ranges.Count; i++)
        {
            var rows = new List<string[]>();
            foreach (var row in ranges[i]!["values"]?.AsArray() ?? new JsonArray())
                rows.Add(row!.AsArray().Select(c => c?.ToString() ?? "").ToArray());
            // Sheets leaves trailing empty cells off; pad so every row has every column.
            var width = rows.Count == 0 ? 0 : rows.Max(r => r.Length);
            for (var r = 0; r < rows.Count; r++)
                if (rows[r].Length < width) rows[r] = rows[r].Concat(Enumerable.Repeat("", width - rows[r].Length)).ToArray();
            result[list[i]] = rows;
        }
        return result;
    }

    /// <summary>Writes several ranges at once. RAW, so Sheets never turns text into numbers, dates or formulas.</summary>
    public Task WriteRangesAsync(string spreadsheetId, List<(string range, List<List<string>> values)> data)
    {
        if (data.Count == 0) return Task.CompletedTask;
        var body = new JsonObject
        {
            ["valueInputOption"] = "RAW",
            ["data"] = new JsonArray(data.Select(d => (JsonNode)new JsonObject
            {
                ["range"] = d.range,
                ["values"] = ToJson(d.values),
            }).ToArray()),
        };
        return SendAsync(HttpMethod.Post, API + spreadsheetId + "/values:batchUpdate", body);
    }

    /// <summary>Adds rows after the last row of a tab. RAW, like every write.</summary>
    public Task AppendRowsAsync(string spreadsheetId, string tab, List<List<string>> rows)
    {
        if (rows.Count == 0) return Task.CompletedTask;
        var url = API + spreadsheetId + "/values/" + Uri.EscapeDataString(Range(tab, "A1"))
                  + ":append?valueInputOption=RAW&insertDataOption=INSERT_ROWS";
        return SendAsync(HttpMethod.Post, url, new JsonObject { ["values"] = ToJson(rows) });
    }

    /// <summary>spreadsheets.batchUpdate: formatting, new tabs, metadata. Returns the replies.</summary>
    public async Task<JsonArray> BatchUpdateAsync(string spreadsheetId, JsonArray requests)
    {
        if (requests.Count == 0) return new JsonArray();
        var json = await SendAsync(HttpMethod.Post, API + spreadsheetId + ":batchUpdate", new JsonObject { ["requests"] = requests });
        return json["replies"]?.AsArray() ?? new JsonArray();
    }

    private static JsonArray ToJson(List<List<string>> rows)
        => new JsonArray(rows.Select(r => (JsonNode)new JsonArray(r.Select(c => (JsonNode)JsonValue.Create(c ?? "")!).ToArray())).ToArray());

    public void Dispose() => _http.Dispose();
}

public sealed class SheetInfo
{
    public string title = "";
    public readonly List<(string title, int sheetId, int columnCount)> tabs = new();
    public readonly Dictionary<string, string> metadata = new();
}

public sealed class SheetsException : Exception
{
    public readonly int status;
    public SheetsException(string message, int status = 0) : base(message) { this.status = status; }
}
