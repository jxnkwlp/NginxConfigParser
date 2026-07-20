using NginxConfigParser;
using Xunit;

namespace NginxConfigParserUnitTests;

public class ErrorAndEdgeCaseTests
{
    [Fact]
    public void AddOrUpdate_EmptyKeyPath_Throws()
    {
        var config = NginxConfig.Create();
        Assert.Throws<ArgumentException>(() => config.AddOrUpdate("", "1"));
        Assert.Throws<ArgumentException>(() => config.Remove(" "));
    }

    [Fact]
    public void Save_EmptyFileName_Throws()
    {
        var config = NginxConfig.Create();
        Assert.Throws<ArgumentException>(() => config.Save(""));
        Assert.Throws<ArgumentNullException>(() => config.Save("out.conf", null!));
    }

    [Fact]
    public void Parser_UnexpectedQuotedLine_IncludesLineNumber()
    {
        var ex = Assert.Throws<Exception>(() => NginxConfig.Load("""
            'orphaned continuation';
            """));

        Assert.Contains("line 1", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetTokens_RootLevelDuplicates()
    {
        var config = NginxConfig.Load("""
            include a.conf;
            include b.conf;
            """);

        var includes = config.GetTokens("include");
        Assert.Equal(2, includes.Count);
    }

    [Fact]
    public void CommentOnlyFile_ParsesWithoutValues()
    {
        var config = NginxConfig.Load("""
            # only a comment
            """);

        var tokens = config.GetTokens().ToList();
        Assert.Single(tokens);
        Assert.IsType<CommentToken>(tokens[0]);
    }

    [Fact]
    public void GroupWithValue_ParsesLocationModifier()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                location ~ \.php$ {
                  fastcgi_pass 127.0.0.1:1025;
                }
              }
            }
            """);

        Assert.Equal("~ \\.php$", config["http:server:location"]!.Value);
        Assert.Equal("127.0.0.1:1025", config["http:server:location:fastcgi_pass"]!.Value);
    }

    [Fact]
    public void TokenModel_ValueAndGroupToString()
    {
        var config = NginxConfig.Create()
            .AddOrUpdate("pid", "a.pid", comment: "c")
            .AddOrUpdate("http:server", null!, true, "srv");

        Assert.Contains("# c", config["pid"]!.ToString(), StringComparison.Ordinal);
        Assert.Contains("server", config["http:server"]!.ToString(), StringComparison.Ordinal);
    }
}
