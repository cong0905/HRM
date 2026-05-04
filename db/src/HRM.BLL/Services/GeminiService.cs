using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HRM.BLL.Interfaces;
using HRM.Common.Constants;
using HRM.Domain.Models;

namespace HRM.BLL.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;

    public GeminiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> GetResponseAsync(string userMessage, string systemInstruction = "")
    {
        try
        {
            var url = $"{GeminiConfig.BaseUrl}?key={GeminiConfig.ApiKey}";
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
