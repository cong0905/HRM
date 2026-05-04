using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HRM.Domain.Models
{
    public class GeminiRequest
    {
        [JsonPropertyName("systemInstruction")]
        public Content? SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public List<Content> Contents { get; set; } = new List<Content>();

        public GeminiRequest(string text, string systemInstructionText = "")
        {
            Contents.Add(new Content
            {
                Parts = new List<Part> { new Part { Text = text } }
            });

            if (!string.IsNullOrEmpty(systemInstructionText))
            {
                SystemInstruction = new Content
                {
                    Parts = new List<Part> { new Part { Text = systemInstructionText } }
                };
            }
        }
    }

    public class Content
    {
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new List<Part>();
    }

    public class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }
}
