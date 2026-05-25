namespace HRM.BLL.Interfaces;

public interface IGeminiService
{
    Task<string> GetResponseAsync(string userMessage, string systemInstruction = "");
}
