using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NevApps.Classes;
using NevApps.Models;

namespace NevApps.Services;

/// <summary>
///     Core service for interacting with the NevApps Web API.
///     Manages communication for feedback, issue reporting, and system announcements.
/// </summary>
public class NevAPIService
{
    private readonly HttpClient _httpClient;
    private readonly StateService _stateService;

    public NevAPIService(HttpClient httpClient, StateService stateService)
    {
        _httpClient = httpClient;
        _stateService = stateService;
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("APIKey", Constants.APIKey);
    }

    /// <summary>
    ///     Generic helper for GET requests.
    ///     <br /><b>State Logic:</b> If the request fails, it updates <see cref="StateService" /> to reflect that the API is
    ///     offline.
    /// </summary>
    private async Task<T> GetAsync<T>(string endpoint) where T : new()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<T>($"{Constants.APIUrl}{endpoint}") ?? new T();
        }
        catch
        {
            _stateService.SetNevAPIState(false);
            return new T();
        }
    }

    /// <summary>
    ///     Generic helper for POST requests using JSON serialization.
    ///     Uses default property naming to match the backend model requirements.
    /// </summary>
    private async Task<HttpResponseMessage> PostJsonAsync(string endpoint, object payload)
    {
        return await _httpClient.PostAsJsonAsync($"{Constants.APIUrl}{endpoint}", payload,
            new JsonSerializerOptions { PropertyNamingPolicy = null });
    }

    /// <summary>
    ///     Checks the connection to the Azure backend via the health endpoint.
    ///     Updates the global application state based on the result.
    /// </summary>
    /// <returns>True if the service is reachable and healthy.</returns>
    public async Task<bool> IsNevAPIAvailableAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{Constants.APIUrl}/api/support/health");
            _stateService.SetNevAPIState(true);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            _stateService.SetNevAPIState(false);
            return false;
        }
    }

    /// <summary>
    ///     Fetches all public feedback entries stored as blobs in the cloud for Admin review.
    /// </summary>
    public Task<FeedbackResponse> LoadFeedbacksAsync()
    {
        return GetAsync<FeedbackResponse>("/api/support?blobType=feedback");
    }

    /// <summary>
    ///     Fetches all reported system issues from the cloud storage for Admin review.
    /// </summary>
    public Task<IssueResponse> LoadIssuesAsync()
    {
        return GetAsync<IssueResponse>("/api/support?blobType=issue");
    }

    /// <summary>
    ///     Retrieves developer feedback response directed to a specific user.
    /// </summary>
    public Task<FeedbackResponse> GetDevFeedbackAsync(string userId)
    {
        return GetAsync<FeedbackResponse>($"/api/support/reply?userId={userId}");
    }

    /// <summary>
    ///     Retrieves developer replies for issues reported by a user.
    /// </summary>
    public Task<IssueResponse> GetDevReplyAsync(string userId)
    {
        return GetAsync<IssueResponse>($"/api/support/reply?userId={userId}");
    }

    /// <summary>
    ///     Submits a new user feedback entry to blob.
    /// </summary>
    public Task<HttpResponseMessage> SendFeedbackAsync(object payload)
    {
        return PostJsonAsync("/api/support/feedback", payload);
    }

    /// <summary>
    ///     Sends a combined response/reply regarding a developer's feedback or issue resolution.
    /// </summary>
    public Task<HttpResponseMessage> SendDevFeedbackAndIssueReplyAsync(object payload)
    {
        return PostJsonAsync("/api/support/reply", payload);
    }

    /// <summary>
    ///     Uploads a text-based issue log to the blob as a multipart form file.
    ///     <br /><b>File Format:</b> The log is saved as '{userId}.txt' for developer review.
    /// </summary>
    /// <param name="logContent">The raw string content of the error log.</param>
    /// <param name="userId">The unique ID used to name the log file.</param>
    public async Task<HttpResponseMessage> SendIssueLogAsync(string logContent, string userId)
    {
        var formData = new MultipartFormDataContent();
        var byteContent = new StringContent(logContent, Encoding.UTF8, "text/plain");
        formData.Add(byteContent, "logFile", $"{userId}.txt");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{Constants.APIUrl}/api/support/issues")
            { Content = formData };
        request.Headers.TryAddWithoutValidation("APIKey", Constants.APIKey);

        return await _httpClient.SendAsync(request);
    }

    /// <summary>
    ///     Retrieves the latest system-wide announcements or broadcast messages for the app.
    /// </summary>
    public Task<BroadcastResponse> GetBroadcastsAsync()
    {
        return GetAsync<BroadcastResponse>("/api/support/announcements");
    }

    /// <summary>
    ///     Uploads a receipt image file directly to the backend Minimal API for secure cloud OCR parsing.
    /// </summary>
    /// <param name="photo">The FileResult object captured from the device camera.</param>
    /// <param name="type">The expense type context (e.g., "Retail", "Gas").</param>
    public async Task<HttpResponseMessage> ParseReceiptImageAsync(FileResult photo, string type)
    {
        using var formData = new MultipartFormDataContent();
        var fileStream = await photo.OpenReadAsync();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(photo.ContentType);
        formData.Add(streamContent, "file", photo.FileName);

        var request = new HttpRequestMessage(HttpMethod.Post, $"{Constants.APIUrl}/api/parser/receipt?type={type}")
        {
            Content = formData
        };

        request.Headers.Add("APIKey", Constants.APIKey);

        return await _httpClient.SendAsync(request);
    }
}