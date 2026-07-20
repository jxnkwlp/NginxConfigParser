using NginxConfigParser;
using Xunit;

namespace NginxConfigParserUnitTests;

public class CreateAndBuilderTests
{
    [Fact]
    public void Create_ReturnsEmptyConfig()
    {
        var config = NginxConfig.Create();

        Assert.Empty(config.GetTokens());
        Assert.Equal(string.Empty, config.ToString());
    }

    [Fact]
    public void AddOrUpdate_CreatesNestedValues()
    {
        var config = NginxConfig.Create()
            .AddOrUpdate("http:server:listen", "80")
            .AddOrUpdate("http:server:root", "/var/wwwroot");

        Assert.Equal("80", config["http:server:listen"]!.Value);
        Assert.Equal("/var/wwwroot", config.GetToken("http:server:root")!.Value);
    }

    [Fact]
    public void AddOrUpdate_AddAsGroup_CreatesGroupWithValue()
    {
        var config = NginxConfig.Create()
            .AddOrUpdate("http:server:location", "/", true, "default")
            .AddOrUpdate("http:server:location:root", "/app1");

        var location = config.GetToken("http:server:location");
        Assert.NotNull(location);
        Assert.IsType<GroupToken>(location);
        Assert.Equal("/", location.Value);
        Assert.Equal("default", location.Comment);
        Assert.Equal("/app1", config["http:server:location:root"]!.Value);
    }

    [Fact]
    public void AddOrUpdate_MultipleIndexedGroups()
    {
        var config = NginxConfig.Create()
            .AddOrUpdate("http:server:location", "/", true, "default")
            .AddOrUpdate("http:server:location:root", "/app1")
            .AddOrUpdate("http:server:location[1]", "~ ^/static/", true)
            .AddOrUpdate("http:server:location[1]:root", "/app2")
            .AddOrUpdate("http:server:location[1]:expires", "1d");

        Assert.Equal("/", config["http:server:location"]!.Value);
        Assert.Equal("~ ^/static/", config["http:server:location[1]"]!.Value);
        Assert.Equal("/app2", config["http:server:location[1]:root"]!.Value);
        Assert.Equal("1d", config["http:server:location[1]:expires"]!.Value);
    }

    [Fact]
    public void AddOrUpdate_UpdatesExistingValueAndComment()
    {
        var config = NginxConfig.Create()
            .AddOrUpdate("worker_processes", "1")
            .AddOrUpdate("worker_processes", "4", comment: "updated");

        Assert.Equal("4", config["worker_processes"]!.Value);
        Assert.Equal("updated", config["worker_processes"]!.Comment);
    }

    [Fact]
    public void AddOrUpdate_IsFluent()
    {
        var config = NginxConfig.Create();
        var same = config.AddOrUpdate("pid", "logs/nginx.pid");

        Assert.Same(config, same);
    }
}
