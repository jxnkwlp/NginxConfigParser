using NginxConfigParser;
using Xunit;

namespace NginxConfigParserUnitTests;

public class LoadAndParseTests
{
    [Fact]
    public void Load_ParsesRootLevelKeys()
    {
        var config = NginxConfig.Load("""
            worker_processes  5;
            error_log  logs/error.log;
            """);

        Assert.Equal("5", config["worker_processes"]!.Value);
        Assert.Equal("logs/error.log", config["error_log"]!.Value);
    }

    [Fact]
    public void Load_ParsesNestedGroups()
    {
        var config = NginxConfig.Load("""
            http {
              sendfile     on;
              server {
                listen 80;
                root html;
              }
            }
            """);

        Assert.Equal("on", config["http:sendfile"]!.Value);
        Assert.Equal("80", config["http:server:listen"]!.Value);
        Assert.Equal("html", config["http:server:root"]!.Value);
    }

    [Fact]
    public void Load_ParsesMultipleSameNameGroups()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                listen 80;
                server_name a.com;
              }
              server {
                listen 8080;
                server_name b.com;
              }
            }
            """);

        Assert.Equal("80", config["http:server:listen"]!.Value);
        Assert.Equal("8080", config["http:server[1]:listen"]!.Value);
        Assert.Equal("a.com", config["http:server:server_name"]!.Value);
        Assert.Equal("b.com", config["http:server[1]:server_name"]!.Value);
    }

    [Fact]
    public void Load_ParsesMultipleSameNameValues()
    {
        var config = NginxConfig.Load("""
            http {
              include    conf/mime.types;
              include    /etc/nginx/proxy.conf;
              include    /etc/nginx/fastcgi.conf;
            }
            """);

        var includes = config.GetTokens("http:include");
        Assert.Equal(3, includes.Count);
        Assert.Equal("conf/mime.types", includes[0].Value);
        Assert.Equal("/etc/nginx/proxy.conf", includes[1].Value);
        Assert.Equal("/etc/nginx/fastcgi.conf", includes[2].Value);
    }

    [Fact]
    public void Load_SkipsEmptyLines()
    {
        var config = NginxConfig.Load("""

            worker_processes  1;

            events {
              worker_connections  1024;
            }

            """);

        Assert.Equal("1", config["worker_processes"]!.Value);
        Assert.Equal("1024", config["events:worker_connections"]!.Value);
    }

    [Fact]
    public void Load_ParsesFullLineComments()
    {
        var config = NginxConfig.Load("""
            # top comment
            worker_processes  1;
            http {
              # inside comment
              sendfile on;
            }
            """);

        var tokens = config.GetTokens().ToList();
        Assert.Contains(tokens, t => t is CommentToken c && c.Content == "top comment");

        var http = config.GetGroup("http");
        Assert.NotNull(http);
        Assert.Contains(http.Tokens, t => t is CommentToken c && c.Content == "inside comment");
    }

    [Fact]
    public void Load_ParsesInlineComments()
    {
        var config = NginxConfig.Load("""
            worker_processes  5;  ## Default: 1
            events {
              worker_connections  4096;  ## Default: 1024
            }
            """);

        Assert.Equal("Default: 1", config["worker_processes"]!.Comment);
        Assert.Equal("Default: 1024", config["events:worker_connections"]!.Comment);
    }

    [Fact]
    public void LoadFrom_ReadsTestConfSample()
    {
        var config = NginxConfig.LoadFrom("test.conf");

        Assert.Equal("5", config["worker_processes"]!.Value);
        Assert.Equal("logs/error.log", config["error_log"]!.Value);
        Assert.NotNull(config.GetGroup("http"));
        Assert.Equal("on", config["http:sendfile"]!.Value);

        var servers = config.GetTokens("http:server");
        Assert.True(servers.Count >= 2);
    }

    [Fact]
    public void Load_NullContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => NginxConfig.Load(null!));
    }

    [Fact]
    public void LoadFrom_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => NginxConfig.LoadFrom("does-not-exist.conf"));
    }

    [Fact]
    public void LoadFrom_WhitespacePath_Throws()
    {
        Assert.Throws<ArgumentException>(() => NginxConfig.LoadFrom("  "));
    }
}
