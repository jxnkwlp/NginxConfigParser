using System.Text;
using NginxConfigParser;
using Xunit;

namespace NginxConfigParserUnitTests;

public class SerializationTests
{
    [Fact]
    public void ToString_PreservesRelativeOrderOfValuesAndGroups()
    {
        var config = NginxConfig.Load("""
            worker_processes 1;
            events {
              worker_connections 1024;
            }
            error_log logs/error.log;
            http {
              sendfile on;
            }
            """);

        var output = config.ToString();
        var workerPos = output.IndexOf("worker_processes", StringComparison.Ordinal);
        var eventsPos = output.IndexOf("events", StringComparison.Ordinal);
        var errorLogPos = output.IndexOf("error_log", StringComparison.Ordinal);
        var httpPos = output.IndexOf("http", StringComparison.Ordinal);

        Assert.True(workerPos >= 0);
        Assert.True(eventsPos > workerPos);
        Assert.True(errorLogPos > eventsPos);
        Assert.True(httpPos > errorLogPos);
    }

    [Fact]
    public void ToString_PreservesOrderInsideGroup()
    {
        var config = NginxConfig.Load("""
            http {
              include a.conf;
              server {
                listen 80;
              }
              sendfile on;
            }
            """);

        var output = config.ToString();
        var includePos = output.IndexOf("include", StringComparison.Ordinal);
        var serverPos = output.IndexOf("server", StringComparison.Ordinal);
        var sendfilePos = output.IndexOf("sendfile", StringComparison.Ordinal);

        Assert.True(includePos >= 0);
        Assert.True(serverPos > includePos);
        Assert.True(sendfilePos > serverPos);
    }

    [Fact]
    public void RoundTrip_KeyValuesSurviveReload()
    {
        var original = NginxConfig.Create()
            .AddOrUpdate("worker_processes", "2")
            .AddOrUpdate("http:server:listen", "80")
            .AddOrUpdate("http:server:root", "/var/www")
            .AddOrUpdate("http:server:location", "/", true, "default")
            .AddOrUpdate("http:server:location:root", "/app");

        var reloaded = NginxConfig.Load(original.ToString());

        Assert.Equal("2", reloaded["worker_processes"]!.Value);
        Assert.Equal("80", reloaded["http:server:listen"]!.Value);
        Assert.Equal("/var/www", reloaded["http:server:root"]!.Value);
        Assert.Equal("/", reloaded["http:server:location"]!.Value);
        Assert.Equal("default", reloaded["http:server:location"]!.Comment);
        Assert.Equal("/app", reloaded["http:server:location:root"]!.Value);
    }

    [Fact]
    public void Save_WritesUtf8File()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nginx-config-{Guid.NewGuid():N}.conf");
        try
        {
            NginxConfig.Create()
                .AddOrUpdate("worker_processes", "1")
                .Save(path);

            var bytes = File.ReadAllBytes(path);
            Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);

            var reloaded = NginxConfig.LoadFrom(path);
            Assert.Equal("1", reloaded["worker_processes"]!.Value);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Save_WithEncoding_UsesProvidedEncoding()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nginx-config-{Guid.NewGuid():N}.conf");
        try
        {
            NginxConfig.Create()
                .AddOrUpdate("pid", "nginx.pid")
                .Save(path, Encoding.Unicode);

            var text = File.ReadAllText(path, Encoding.Unicode);
            Assert.Contains("pid", text, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void BuiltConfig_MatchesExpectedStructure()
    {
        var content = NginxConfig.Create()
            .AddOrUpdate("http:server:listen", "80")
            .AddOrUpdate("http:server:location", "/", true, "default")
            .AddOrUpdate("http:server:location:root", "/app1")
            .ToString();

        Assert.Contains("http", content, StringComparison.Ordinal);
        Assert.Contains("server", content, StringComparison.Ordinal);
        Assert.Contains("listen  80;", content, StringComparison.Ordinal);
        Assert.Contains("location  / { # default", content, StringComparison.Ordinal);
        Assert.Contains("root  /app1;", content, StringComparison.Ordinal);
    }
}
