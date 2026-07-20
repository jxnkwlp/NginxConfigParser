using NginxConfigParser;
using Xunit;

namespace NginxConfigParserUnitTests;

public class UpdateAndRemoveTests
{
    [Fact]
    public void AddOrUpdate_UpdatesExistingNestedValue()
    {
        var config = NginxConfig.Load("""
            http {
              sendfile on;
              server {
                root /old;
              }
            }
            """);

        config.AddOrUpdate("http:sendfile", "off", comment: "updated");
        config.AddOrUpdate("http:server:root", "/new", comment: "changed");

        Assert.Equal("off", config["http:sendfile"]!.Value);
        Assert.Equal("updated", config["http:sendfile"]!.Comment);
        Assert.Equal("/new", config["http:server:root"]!.Value);
    }

    [Fact]
    public void AddOrUpdate_CreatesIntermediateGroups()
    {
        var config = NginxConfig.Create()
            .AddOrUpdate("http:server2:root", "/var/wwwroot");

        Assert.Equal("/var/wwwroot", config["http:server2:root"]!.Value);
        Assert.IsType<GroupToken>(config.GetToken("http"));
        Assert.IsType<GroupToken>(config.GetToken("http:server2"));
    }

    [Fact]
    public void AddOrUpdate_IndexOutOfRange_Throws()
    {
        var config = NginxConfig.Create()
            .AddOrUpdate("http:server:listen", "80");

        Assert.Throws<IndexOutOfRangeException>(() =>
            config.AddOrUpdate("http:server[2]:root", "/x"));
    }

    [Fact]
    public void Remove_WithoutIndex_RemovesAllMatching()
    {
        var config = NginxConfig.Load("""
            http {
              include a.conf;
              include b.conf;
              sendfile on;
            }
            """);

        config.Remove("http:include");

        Assert.Empty(config.GetTokens("http:include"));
        Assert.Equal("on", config["http:sendfile"]!.Value);
    }

    [Fact]
    public void Remove_WithIndex_RemovesOnlyThatItem()
    {
        var config = NginxConfig.Load("""
            http {
              include a.conf;
              include b.conf;
              include c.conf;
            }
            """);

        config.Remove("http:include[1]");

        var includes = config.GetTokens("http:include");
        Assert.Equal(2, includes.Count);
        Assert.Equal("a.conf", includes[0].Value);
        Assert.Equal("c.conf", includes[1].Value);
    }

    [Fact]
    public void Remove_IndexedGroup()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                listen 80;
              }
              server {
                listen 8080;
              }
              server {
                listen 9090;
              }
            }
            """);

        config.Remove("http:server[1]");

        var servers = config.GetTokens("http:server");
        Assert.Equal(2, servers.Count);
        Assert.Equal("80", config["http:server:listen"]!.Value);
        Assert.Equal("9090", config["http:server[1]:listen"]!.Value);
    }

    [Fact]
    public void Remove_EntireGroup()
    {
        var config = NginxConfig.Load("""
            events {
              worker_connections 1024;
            }
            http {
              sendfile on;
            }
            """);

        config.Remove("events");

        Assert.Null(config.GetGroup("events"));
        Assert.NotNull(config.GetGroup("http"));
    }

    [Fact]
    public void Remove_IndexOutOfRange_Throws()
    {
        var config = NginxConfig.Load("""
            http {
              include a.conf;
            }
            """);

        Assert.Throws<IndexOutOfRangeException>(() => config.Remove("http:include[3]"));
    }

    [Fact]
    public void Remove_IsFluent()
    {
        var config = NginxConfig.Create().AddOrUpdate("pid", "a.pid");
        Assert.Same(config, config.Remove("pid"));
        Assert.Null(config["pid"]);
    }
}
