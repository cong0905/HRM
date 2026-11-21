using HRM.Next.Application.Audit;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed class AuditMaskerTests
{
    public static readonly TheoryData<string, string> SeparatorAndQuotingCases = new()
    {
        // = separator
        { "client_secret = 'ValAlpha123!'", "ValAlpha123!" },
        { "client_secret=\"ValBeta456@\"", "ValBeta456@" },
        { "client_secret = ValGamma789#;", "ValGamma789#" },
        { "CLIENT_SECRET=`ValDelta999$`", "ValDelta999$" },

        // : separator
        { "access_token: 'ValEpsilon111'", "ValEpsilon111" },
        { "access_token:\"ValZeta222\"", "ValZeta222" },
        { "ACCESS_TOKEN : ValEta333,", "ValEta333" },

        // => separator
        { "client_secret => 'BluebirdFixture!478'", "BluebirdFixture!478" },
        { "client_secret=>\"CustomVal555\"", "CustomVal555" },
        { "client-secret   =>   BareVal666;", "BareVal666" },
        { "ClientSecret => `TickVal777`", "TickVal777" },

        // -> separator
        { "access_token -> 'Qz9Xv2Lc7Nm5Rt8Yp4'", "Qz9Xv2Lc7Nm5Rt8Yp4" },
        { "access_token->\"ArrowThinDouble888\"", "ArrowThinDouble888" },
        { "access-token   ->   ArrowThinBare999&", "ArrowThinBare999" },

        // := separator
        { "password := 'ColonEqualsVal101'", "ColonEqualsVal101" },
        { "api_key := \"ColonEqualsVal102\"", "ColonEqualsVal102" },

        // Aliases & cases
        { "CurrentPassword => 'CurrentPwdVal103'", "CurrentPwdVal103" },
        { "new-password -> 'NewPwdVal104'", "NewPwdVal104" },
        { "refresh_token = 'RefreshTokVal105'", "RefreshTokVal105" },

        // Unclosed quotes (fail-closed)
        { "client_secret => 'UnclosedVal106; next", "UnclosedVal106" },
        { "password: \"UnclosedVal107", "UnclosedVal107" }
    };

    public static readonly TheoryData<string, string> QuotedJsonAndStructureCases = new()
    {
        // Serialized JSON with PascalCase and camelCase keys
        { "{\"CurrentPassword\":\"BluebirdFixture!478\",\"NewPassword\":\"Qz9Xv2Lc7Nm5Rt8Yp4\"}", "BluebirdFixture!478" },
        { "{\"CurrentPassword\":\"BluebirdFixture!478\",\"NewPassword\":\"Qz9Xv2Lc7Nm5Rt8Yp4\"}", "Qz9Xv2Lc7Nm5Rt8Yp4" },
        { "{\"currentPassword\":\"CamelVal111!\",\"newPassword\":\"CamelVal222@\"}", "CamelVal111!" },
        { "{\"client_secret\":\"SnakeVal333#\",\"access_token\":\"SnakeVal444$\"}", "SnakeVal333#" },

        // Quoted keys in text with different separators
        { "\"CurrentPassword\":\"QuotedTextVal555%\"", "QuotedTextVal555%" },
        { "\"new-password\" : \"SpacedQuotedVal666^\"", "SpacedQuotedVal666^" },
        { "'client_secret' => 'QuotedArrowVal777&'", "QuotedArrowVal777&" },
        { "'access_token' -> 'QuotedThinArrowVal888*'", "QuotedThinArrowVal888*" },

        // Embedded JSON in free text
        { "Payload was: {\"CurrentPassword\":\"EmbeddedVal999(\"} for review", "EmbeddedVal999(" },

        // Malformed / unclosed / broken structured text (fail-closed)
        { "{\"CurrentPassword\":\"BrokenJsonVal101", "BrokenJsonVal101" },
        { "\"CurrentPassword\" => 'BrokenArrowVal102", "BrokenArrowVal102" },
        { "\"password\": \"BrokenColonVal103", "BrokenColonVal103" },
        { "Broken payload CurrentPassword BluebirdVal104", "BluebirdVal104" },

        // Malformed / unparsed credential labels with value but no operator (fail-closed)
        { "Malformed client_secret BluebirdFixture!478; api_key Qz9Xv2Lc7Nm5Rt8Yp4", "BluebirdFixture!478" },
        { "Malformed client_secret BluebirdFixture!478; api_key Qz9Xv2Lc7Nm5Rt8Yp4", "Qz9Xv2Lc7Nm5Rt8Yp4" },
        { "Malformed client_secret BluebirdFixture!478", "BluebirdFixture!478" },
        { "Malformed api_key Qz9Xv2Lc7Nm5Rt8Yp4", "Qz9Xv2Lc7Nm5Rt8Yp4" },
        { "Broken access_token TokenValNoOp201", "TokenValNoOp201" },
        { "Broken refresh_token RefreshValNoOp202", "RefreshValNoOp202" },
        { "Broken secret SecretValNoOp203", "SecretValNoOp203" }
    };

    [Theory]
    [MemberData(nameof(SeparatorAndQuotingCases))]
    public void SanitizeFreeText_RedactsSensitiveAssignments_WithoutLeavingValue(string input, string rawSecret)
    {
        var result = AuditMasker.SanitizeFreeText(input);
        Assert.DoesNotContain(rawSecret, result);
        Assert.Contains("[REDACTED]", result);
    }

    [Theory]
    [MemberData(nameof(QuotedJsonAndStructureCases))]
    public void SanitizeFreeText_RedactsQuotedJsonAndBrokenStructure_WithoutLeavingValue(string input, string rawSecret)
    {
        var result = AuditMasker.SanitizeFreeText(input);
        Assert.DoesNotContain(rawSecret, result);
        Assert.Contains("[REDACTED]", result);
    }

    [Fact]
    public void MaskJson_RedactsNestedObjectAndArrayValues()
    {
        var nested = new
        {
            meta = new
            {
                client_secret = "DirectNestedSecretVal!",
                note = "Config client_secret => 'StringNestedValAlpha!'; access_token -> 'StringNestedValBeta!'"
            },
            tags = new[]
            {
                "SafeTag",
                "client_secret = 'ArrayStringValGamma!'",
                "Unrelated"
            }
        };

        var masked = AuditMasker.MaskObject(nested);

        Assert.DoesNotContain("DirectNestedSecretVal!", masked);
        Assert.DoesNotContain("StringNestedValAlpha!", masked);
        Assert.DoesNotContain("StringNestedValBeta!", masked);
        Assert.DoesNotContain("ArrayStringValGamma!", masked);
    }
}

