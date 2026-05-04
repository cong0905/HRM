using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HRM.BLL.Interfaces;
using HRM.Common.Constants;
using HRM.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace HRM.BLL.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public GeminiService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _baseUrl = configuration["Gemini:BaseUrl"] ?? GeminiConfig.BaseUrl;
    }

    public async Task<string> GetResponseAsync(string userMessage, string systemInstruction = "")
    {
        try
        {
            var url = $"{_baseUrl}?key={_apiKey}";
            var requestBody = new GeminiRequest(userMessage, systemInstruction);
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Lỗi API ({response.StatusCode}): {responseBody}";
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);

            var botMessage = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text;
            return botMessage ?? "Xin lỗi, tôi không thể trả lời câu hỏi này lúc này.";
        }
        catch (Exception ex)
        {
            return $"Lỗi kết nối Gemini: {ex.Message}";
        }
    }
}
