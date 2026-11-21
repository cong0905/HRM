using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace HRM.Next.Application.Audit;

public static class AuditMasker
{
    private static readonly Regex SensitiveKeyPattern = new(
        @"(?i)(password|passwd|pwd|token|secret|newpassword|currentpassword|code|cookie|hash|auth|credentials|key)",
        RegexOptions.Compiled);

    private static readonly Regex CitizenIdKeyPattern = new(
        @"(?i)(citizenid|cccd|cmnd|identitycard|ssn)",
        RegexOptions.Compiled);

    private static readonly Regex AddressKeyPattern = new(
        @"(?i)(address|diachi|street|homeaddress)",
        RegexOptions.Compiled);

    private static readonly Regex PhoneKeyPattern = new(
        @"(?i)(phone|sdt|phonenumber|mobile)",
        RegexOptions.Compiled);

    // Multi-character separators (=>, ->, :=) MUST precede single-character separators (=, :) to prevent partial-token splitting.
    // Supports bare keys and quoted keys ("key", 'key', `key`).
    // Supports double quotes, single quotes, backticks, unclosed quotes terminated by delimiter/EOF, and bare tokens.
    private static readonly Regex FreeTextAssignmentPattern = new(
        @"(?i)(?:""|'|`|\b)[a-z0-9_\-]*(?:password|passwd|pwd|token|secret|key|cred|credential|cookie|hash|auth)[a-z0-9_\-]*(?:""|'|`|\b)\s*(?:=>|->|:=|=|:|\s(?=[""'`]))\s*(?:""(?:[^""\\\r\n]|\\.)*(?:""|(?=[;,\r\n}]|$))|'(?:[^'\\\r\n]|\\.)*(?:'|(?=[;,\r\n}]|$))|`(?:[^`\\\r\n]|\\.)*(?:`|(?=[;,\r\n}]|$))|[^\s,;&}\r\n]+)",
        RegexOptions.Compiled);

    // Fail-closed fallback: if a credential label is followed by an assignment operator but value is abnormal/unparseable,
    // redact the entire assignment clause up to the next delimiter or end of line.
    private static readonly Regex DanglingAssignmentPattern = new(
        @"(?i)(?:""|'|`|\b)[a-z0-9_\-]*(?:password|passwd|pwd|token|secret|key|cred|credential|cookie|hash|auth)[a-z0-9_\-]*(?:""|'|`|\b)\s*(?:=>|->|:=|=|:)\s*[^;,&}\r\n]*",
        RegexOptions.Compiled);

    private static readonly Regex FreeTextBearerPattern = new(
        @"(?i)\bbearer\s+[^\s,;&\r\n]+",
        RegexOptions.Compiled);

    // Detect credential labels remaining in text to enforce fail-closed redaction
    private static readonly Regex CredentialLabelPattern = new(
        @"(?i)(?:""|'|`|\b)[a-z0-9_\-]*(?:password|passwd|pwd|token|secret|key|cred|credential|cookie|hash|auth)[a-z0-9_\-]*(?:""|'|`|\b)",
        RegexOptions.Compiled);

    // Recognized safe already-redacted assignment (e.g. "CurrentPassword":"[REDACTED]" or key: [REDACTED])
    private static readonly Regex SafeRedactedAssignmentPattern = new(
        @"(?i)(?:""|'|`|\b)[a-z0-9_\-]*(?:password|passwd|pwd|token|secret|key|cred|credential|cookie|hash|auth)[a-z0-9_\-]*(?:""|'|`|\b)\s*(?:=>|->|:=|=|:)\s*(?:""|'|`)?\[REDACTED\](?:""|'|`)?",
        RegexOptions.Compiled);

    private static readonly Regex FreeTextCccdPattern = new(
        @"\b\d{9,12}\b",
        RegexOptions.Compiled);

    public static string MaskCitizenId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var trimmed = raw.Trim();
        if (trimmed.Length <= 4) return "****";
        return new string('*', trimmed.Length - 4) + trimmed[^4..];
    }

    public static string SanitizeFreeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Phase 1: If text is structured JSON (object or array), parse and mask properties
        var trimmed = text.Trim();
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                var node = JsonNode.Parse(trimmed);
                if (node is JsonObject or JsonArray)
                {
                    MaskNode(node);
                    return node.ToJsonString();
                }
            }
            catch
            {
                // Malformed JSON falls through to embedded JSON and free-text sanitization
            }
        }

        // Phase 1b: Try masking embedded JSON blocks within free text
        var result = TryMaskEmbeddedJson(text);

        // Phase 2: Redact key-value assignments (quoted and bare keys, all separators, quoted and bare values)
        result = FreeTextAssignmentPattern.Replace(result, "[REDACTED]");
        result = DanglingAssignmentPattern.Replace(result, "[REDACTED]");
        result = FreeTextBearerPattern.Replace(result, "[REDACTED]");

        // Mask citizen IDs
        result = FreeTextCccdPattern.Replace(result, m => MaskCitizenId(m.Value));

        // Phase 3: Fail-closed verification
        // If there is still any credential label whose value is not verified to be [REDACTED],
        // redact the containing clause/segment to prevent unlabelled credential value leakage.
        result = EnforceFailClosedRedaction(result);

        return result;
    }

    private static string TryMaskEmbeddedJson(string text)
    {
        var braceIndex = text.IndexOf('{');
        if (braceIndex < 0) return text;

        var sb = new System.Text.StringBuilder();
        var currentIndex = 0;

        while (currentIndex < text.Length)
        {
            var openBrace = text.IndexOf('{', currentIndex);
            if (openBrace < 0)
            {
                sb.Append(text.AsSpan(currentIndex));
                break;
            }

            sb.Append(text.AsSpan(currentIndex, openBrace - currentIndex));

            var closeBrace = FindMatchingBrace(text, openBrace);
            if (closeBrace > openBrace)
            {
                var candidate = text.Substring(openBrace, closeBrace - openBrace + 1);
                try
                {
                    var node = JsonNode.Parse(candidate);
                    if (node is JsonObject or JsonArray)
                    {
                        MaskNode(node);
                        sb.Append(node.ToJsonString());
                        currentIndex = closeBrace + 1;
                        continue;
                    }
                }
                catch
                {
                    // Not valid JSON block, proceed as plain text
                }
            }

            sb.Append('{');
            currentIndex = openBrace + 1;
        }

        return sb.ToString();
    }

    private static int FindMatchingBrace(string text, int openIndex)
    {
        var depth = 0;
        var inQuote = false;
        var quoteChar = '\0';
        var isEscaped = false;

        for (var i = openIndex; i < text.Length; i++)
        {
            var c = text[i];
            if (isEscaped)
            {
                isEscaped = false;
                continue;
            }

            if (c == '\\' && inQuote)
            {
                isEscaped = true;
                continue;
            }

            if (inQuote)
            {
                if (c == quoteChar)
                {
                    inQuote = false;
                }
                continue;
            }

            if (c is '"' or '\'')
            {
                inQuote = true;
                quoteChar = c;
                continue;
            }

            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }

        return -1;
    }

    private static string EnforceFailClosedRedaction(string text)
    {
        if (!CredentialLabelPattern.IsMatch(text))
        {
            return text;
        }

        // If the text can be split by delimiters (; or newlines), inspect each segment independently
        var delimiterMatch = Regex.Match(text, @"[;\r\n]");
        if (!delimiterMatch.Success)
        {
            // Single clause: check if the label is part of a verified safe pattern
            var sanitizedCheck = SafeRedactedAssignmentPattern.Replace(text, string.Empty);
            if (CredentialLabelPattern.IsMatch(sanitizedCheck))
            {
                // Unverified credential label present -> redact entire text fail-closed
                return "[REDACTED]";
            }
            return text;
        }

        // Multiple segments separated by ; or newlines
        var segments = Regex.Split(text, @"(?<=[;\r\n])");
        var sb = new System.Text.StringBuilder();

        foreach (var seg in segments)
        {
            var sanitizedSeg = SafeRedactedAssignmentPattern.Replace(seg, string.Empty);
            if (CredentialLabelPattern.IsMatch(sanitizedSeg))
            {
                // Retain trailing delimiter if present
                var trailingDelim = string.Empty;
                var trimmed = seg;
                if (seg.EndsWith(';') || seg.EndsWith('\n') || seg.EndsWith('\r'))
                {
                    trailingDelim = seg[^1].ToString();
                    trimmed = seg[..^1];
                    if (seg.EndsWith("\r\n"))
                    {
                        trailingDelim = "\r\n";
                        trimmed = seg[..^2];
                    }
                }
                sb.Append("[REDACTED]").Append(trailingDelim);
            }
            else
            {
                sb.Append(seg);
            }
        }

        return sb.ToString();
    }

    public static string? MaskJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var node = JsonNode.Parse(json);
            if (node is null) return null;
            MaskNode(node);
            return node.ToJsonString();
        }
        catch
        {
            return "[UNPARSEABLE_MASKED]";
        }
    }

    public static string MaskObject<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return MaskJson(json) ?? "{}";
    }

    private static void MaskNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            var properties = obj.ToList();
            foreach (var prop in properties)
            {
                if (SensitiveKeyPattern.IsMatch(prop.Key))
                {
                    obj[prop.Key] = "[REDACTED]";
                }
                else if (CitizenIdKeyPattern.IsMatch(prop.Key))
                {
                    var val = prop.Value?.ToString();
                    obj[prop.Key] = MaskCitizenId(val);
                }
                else if (AddressKeyPattern.IsMatch(prop.Key))
                {
                    obj[prop.Key] = "[PROTECTED_ADDRESS]";
                }
                else if (PhoneKeyPattern.IsMatch(prop.Key))
                {
                    obj[prop.Key] = "[PROTECTED_PHONE]";
                }
                else if (prop.Value is JsonValue jsonVal && jsonVal.TryGetValue<string>(out var strVal))
                {
                    obj[prop.Key] = SanitizeFreeText(strVal);
                }
                else if (prop.Value is not null)
                {
                    MaskNode(prop.Value);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            for (var i = 0; i < arr.Count; i++)
            {
                var item = arr[i];
                if (item is JsonValue jsonVal && jsonVal.TryGetValue<string>(out var strVal))
                {
                    arr[i] = SanitizeFreeText(strVal);
                }
                else if (item is not null)
                {
                    MaskNode(item);
                }
            }
        }
    }
}
