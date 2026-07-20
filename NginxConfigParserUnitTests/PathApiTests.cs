using NginxConfigParser;
using Xunit;

namespace NginxConfigParserUnitTests;

public class PathApiTests
{
    private static NginxConfig Sample() => NginxConfig.Load("""
        error_log  logs/error.log;
        http {
          sendfile on;
          include a.conf;
          include b.conf;
          server {
            listen 80;
            root /a;
          }
          server {
            listen 8080;
            root /b;
          }
        }
        """);

    [Fact]
    public void GetToken_And_Indexer_ReturnSameValue()
    {
        var config = Sample();

        Assert.Equal(config.GetToken("http:sendfile")!.Value, config["http:sendfile"]!.Value);
        Assert.Equal("logs/error.log", config["error_log"]!.Value);
    }

    [Fact]
    public void GetToken_MissingPath_ReturnsNull()
    {
        var config = Sample();

        Assert.Null(config.GetToken("http:missing"));
        Assert.Null(config["http:server[9]:root"]);
    }

    [Fact]
    public void GetTokens_ReturnsAllMatches()
    {
        var config = Sample();
        var includes = config.GetTokens("http:include");

        Assert.Equal(2, includes.Count);
        Assert.Equal("a.conf", includes[0].Value);
        Assert.Equal("b.conf", includes[1].Value);
    }

    [Fact]
    public void GetTokens_MissingPath_ReturnsEmptyList()
    {
        var config = Sample();

        var missing = config.GetTokens("http:does_not_exist");
        Assert.NotNull(missing);
        Assert.Empty(missing);

        var nestedMissing = config.GetTokens("missing:include");
        Assert.NotNull(nestedMissing);
        Assert.Empty(nestedMissing);
    }

    [Fact]
    public void GetGroup_ReturnsRootGroup()
    {
        var config = Sample();
        var http = config.GetGroup("http");

        Assert.NotNull(http);
        Assert.Equal("http", http.Key);
        Assert.Contains(http.Tokens, t => t is ValueToken v && v.Key == "sendfile");
    }

    [Fact]
    public void GetGroup_Missing_ReturnsNull()
    {
        var config = Sample();
        Assert.Null(config.GetGroup("stream"));
    }

    [Fact]
    public void IndexedPath_SelectsCorrectServer()
    {
        var config = Sample();

        Assert.Equal("/a", config["http:server[0]:root"]!.Value);
        Assert.Equal("/b", config["http:server[1]:root"]!.Value);
        Assert.Equal("80", config["http:server:listen"]!.Value);
        Assert.Equal("8080", config["http:server[1]:listen"]!.Value);
    }

    [Fact]
    public void EmptyKeyPath_Throws()
    {
        var config = Sample();

        Assert.Throws<ArgumentException>(() => config.GetToken(""));
        Assert.Throws<ArgumentException>(() => config.GetTokens(" "));
        Assert.Throws<ArgumentException>(() => _ = config["\t"]);
        Assert.Throws<ArgumentException>(() => config.GetGroup(null!));
    }

    [Fact]
    public void InvalidKeyFormat_Throws()
    {
        var config = Sample();

        Assert.ThrowsAny<Exception>(() => config.GetToken("http:bad-key"));
        Assert.ThrowsAny<Exception>(() => config.GetToken("http:server[abc]:root"));
    }
}
