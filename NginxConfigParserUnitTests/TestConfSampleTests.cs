using NginxConfigParser;
using Xunit;

namespace NginxConfigParserUnitTests;

/// <summary>
/// Deeper assertions against the bundled test.conf sample.
/// </summary>
public class TestConfSampleTests
{
    private static NginxConfig LoadSample() => NginxConfig.LoadFrom("test.conf");

    [Fact]
    public void TestConf_ParsesRootDirectives()
    {
        var config = LoadSample();

        Assert.Equal("www www", config["user"]!.Value);
        Assert.Equal("5", config["worker_processes"]!.Value);
        Assert.Equal("logs/error.log", config["error_log"]!.Value);
        Assert.Equal("logs/nginx.pid", config["pid"]!.Value);
        Assert.Equal("8192", config["worker_rlimit_nofile"]!.Value);
        Assert.Equal("4096", config["events:worker_connections"]!.Value);
    }

    [Fact]
    public void TestConf_ParsesHttpIncludesAndBasics()
    {
        var config = LoadSample();

        var includes = config.GetTokens("http:include");
        Assert.Equal(3, includes.Count);
        Assert.Equal("conf/mime.types", includes[0].Value);
        Assert.Equal("/etc/nginx/proxy.conf", includes[1].Value);
        Assert.Equal("/etc/nginx/fastcgi.conf", includes[2].Value);

        Assert.Equal("index.html index.htm index.php", config["http:index"]!.Value);
        Assert.Equal("application/octet-stream", config["http:default_type"]!.Value);
        Assert.Equal("on", config["http:sendfile"]!.Value);
        Assert.Equal("on", config["http:tcp_nopush"]!.Value);
        Assert.Equal("128", config["http:server_names_hash_bucket_size"]!.Value);
        Assert.Equal("logs/access.log  main", config["http:access_log"]!.Value);
    }

    [Fact]
    public void TestConf_ParsesThreeServers()
    {
        var config = LoadSample();
        var servers = config.GetTokens("http:server");

        Assert.Equal(3, servers.Count);
        Assert.Equal("php/fastcgi", servers[0].Comment);
        Assert.Equal("simple reverse-proxy", servers[1].Comment);
        Assert.Equal("simple load balancing", servers[2].Comment);

        Assert.Equal("domain1.com www.domain1.com", config["http:server:server_name"]!.Value);
        Assert.Equal("html", config["http:server:root"]!.Value);
        Assert.Equal("~ \\.php$", config["http:server:location"]!.Value);
        Assert.Equal("127.0.0.1:1025", config["http:server:location:fastcgi_pass"]!.Value);

        Assert.Equal("domain2.com www.domain2.com", config["http:server[1]:server_name"]!.Value);
        Assert.Equal("~ ^/(images|javascript|js|css|flash|media|static)/", config["http:server[1]:location"]!.Value?.Trim());
        Assert.Equal("/var/www/virtual/big.server.com/htdocs", config["http:server[1]:location:root"]!.Value);
        Assert.Equal("30d", config["http:server[1]:location:expires"]!.Value);
        Assert.Equal("/", config["http:server[1]:location[1]"]!.Value);
        Assert.Equal("http://127.0.0.1:8080", config["http:server[1]:location[1]:proxy_pass"]!.Value);

        Assert.Equal("big.server.com", config["http:server[2]:server_name"]!.Value);
        Assert.Equal("http://big_server_com", config["http:server[2]:location:proxy_pass"]!.Value);
    }

    [Fact]
    public void TestConf_ParsesUpstream()
    {
        var config = LoadSample();
        var upstream = config.GetToken("http:upstream") as GroupToken;

        Assert.NotNull(upstream);
        Assert.Equal("big_server_com", upstream.Value);

        var servers = config.GetTokens("http:upstream:server");
        Assert.Equal(4, servers.Count);
        Assert.Equal("127.0.0.3:8000 weight=5", servers[0].Value);
        Assert.Equal("127.0.0.3:8001 weight=5", servers[1].Value);
        Assert.Equal("192.168.0.1:8000", servers[2].Value);
        Assert.Equal("192.168.0.1:8001", servers[3].Value);
    }

    [Fact]
    public void TestConf_GetGroupHttp_ContainsExpectedChildren()
    {
        var config = LoadSample();
        var http = config.GetGroup("http");

        Assert.NotNull(http);
        Assert.Contains(http.Tokens, t => t is GroupToken g && g.Key == "server");
        Assert.Contains(http.Tokens, t => t is GroupToken g && g.Key == "upstream");
        Assert.Contains(http.Tokens, t => t is ValueToken v && v.Key == "sendfile");
    }
}
