using NginxConfigParser;
using Xunit;

namespace NginxConfigParserUnitTests;

/// <summary>
/// Regression tests for https://github.com/jxnkwlp/NginxConfigParser/issues/15
/// Semicolons / hashes inside quoted values must not terminate the statement.
/// </summary>
public class QuotedValueParsingTests
{
    [Fact]
    public void Issue15_AddHeader_SemicolonInsideDoubleQuotes_IsPreserved()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                add_header X-Xss-Protection "1;mode=block";
              }
            }
            """);

        Assert.Equal("X-Xss-Protection \"1;mode=block\"", config["http:server:add_header"]!.Value);
    }

    [Fact]
    public void Issue15_RoundTrip_PreservesQuotedSemicolon()
    {
        var original = NginxConfig.Load("""
            http {
              server {
                add_header X-Xss-Protection "1;mode=block";
              }
            }
            """);

        var reloaded = NginxConfig.Load(original.ToString());

        Assert.Equal("X-Xss-Protection \"1;mode=block\"", reloaded["http:server:add_header"]!.Value);
        Assert.Contains("\"1;mode=block\"", reloaded.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SemicolonInsideSingleQuotes_IsPreserved()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                add_header X-Custom 'a;b;c';
              }
            }
            """);

        Assert.Equal("X-Custom 'a;b;c'", config["http:server:add_header"]!.Value);
    }

    [Fact]
    public void HashInsideDoubleQuotes_IsNotTreatedAsComment()
    {
        var config = NginxConfig.Load("""
            http {
              set $foo "bar#baz";
            }
            """);

        Assert.Equal("$foo \"bar#baz\"", config["http:set"]!.Value);
        Assert.True(string.IsNullOrEmpty(config["http:set"]!.Comment));
    }

    [Fact]
    public void TrailingComment_StillParsed_WhenValueHasQuotedSemicolon()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                add_header X-Xss-Protection "1;mode=block"; # security
              }
            }
            """);

        var token = config["http:server:add_header"];
        Assert.Equal("X-Xss-Protection \"1;mode=block\"", token!.Value);
        Assert.Equal("security", token.Comment);
    }

    [Fact]
    public void MultipleAddHeaders_WithQuotedSemicolons()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                add_header X-Xss-Protection "1;mode=block";
                add_header Content-Security-Policy "default-src 'self'; script-src 'self'";
                add_header X-Frame-Options SAMEORIGIN;
              }
            }
            """);

        var headers = config.GetTokens("http:server:add_header");
        Assert.Equal(3, headers.Count);
        Assert.Equal("X-Xss-Protection \"1;mode=block\"", headers[0].Value);
        Assert.Equal("Content-Security-Policy \"default-src 'self'; script-src 'self'\"", headers[1].Value);
        Assert.Equal("X-Frame-Options SAMEORIGIN", headers[2].Value);
    }

    [Fact]
    public void BraceInsideQuotes_DoesNotStartGroup()
    {
        var config = NginxConfig.Load("""
            http {
              set $msg "hello{world}";
            }
            """);

        Assert.Equal("$msg \"hello{world}\"", config["http:set"]!.Value);
        Assert.IsType<ValueToken>(config["http:set"]);
    }

    [Fact]
    public void UnpairedDoubleQuote_ThrowsWithLineNumber()
    {
        var ex = Assert.Throws<Exception>(() => NginxConfig.Load("""
            http {
              add_header X-Test "unclosed;
            }
            """));

        Assert.Contains("Unpaired quote", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("line", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Builder_CanCreateQuotedSemicolonValue()
    {
        var config = NginxConfig.Create()
            .AddOrUpdate("http:server:add_header", "X-Xss-Protection \"1;mode=block\"", addAsGroup: false);

        var output = config.ToString();
        Assert.Contains("\"1;mode=block\"", output, StringComparison.Ordinal);

        var reloaded = NginxConfig.Load(output);
        Assert.Equal("X-Xss-Protection \"1;mode=block\"", reloaded["http:server:add_header"]!.Value);
    }
}
