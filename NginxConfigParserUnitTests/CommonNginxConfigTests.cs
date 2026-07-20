using NginxConfigParser;
using Xunit;

namespace NginxConfigParserUnitTests;

/// <summary>
/// Coverage for common nginx.conf directives beyond the bundled test.conf sample.
/// </summary>
public class CommonNginxConfigTests
{
    [Fact]
    public void Parses_GlobalWorkerAndProcessDirectives()
    {
        var config = NginxConfig.Load("""
            user nginx;
            worker_processes auto;
            worker_rlimit_nofile 65535;
            pid /run/nginx.pid;
            error_log /var/log/nginx/error.log warn;
            """);

        Assert.Equal("nginx", config["user"]!.Value);
        Assert.Equal("auto", config["worker_processes"]!.Value);
        Assert.Equal("65535", config["worker_rlimit_nofile"]!.Value);
        Assert.Equal("/run/nginx.pid", config["pid"]!.Value);
        Assert.Equal("/var/log/nginx/error.log warn", config["error_log"]!.Value);
    }

    [Fact]
    public void Parses_EventsBlock()
    {
        var config = NginxConfig.Load("""
            events {
              use epoll;
              multi_accept on;
              worker_connections 10240;
            }
            """);

        Assert.Equal("epoll", config["events:use"]!.Value);
        Assert.Equal("on", config["events:multi_accept"]!.Value);
        Assert.Equal("10240", config["events:worker_connections"]!.Value);
    }

    [Fact]
    public void Parses_HttpPerformanceAndGzipDirectives()
    {
        var config = NginxConfig.Load("""
            http {
              sendfile on;
              tcp_nopush on;
              tcp_nodelay on;
              keepalive_timeout 65;
              types_hash_max_size 2048;
              server_tokens off;
              client_max_body_size 20m;
              gzip on;
              gzip_vary on;
              gzip_proxied any;
              gzip_comp_level 5;
              gzip_types text/plain text/css application/json application/javascript;
            }
            """);

        Assert.Equal("on", config["http:sendfile"]!.Value);
        Assert.Equal("on", config["http:tcp_nopush"]!.Value);
        Assert.Equal("on", config["http:tcp_nodelay"]!.Value);
        Assert.Equal("65", config["http:keepalive_timeout"]!.Value);
        Assert.Equal("2048", config["http:types_hash_max_size"]!.Value);
        Assert.Equal("off", config["http:server_tokens"]!.Value);
        Assert.Equal("20m", config["http:client_max_body_size"]!.Value);
        Assert.Equal("on", config["http:gzip"]!.Value);
        Assert.Equal("on", config["http:gzip_vary"]!.Value);
        Assert.Equal("any", config["http:gzip_proxied"]!.Value);
        Assert.Equal("5", config["http:gzip_comp_level"]!.Value);
        Assert.Equal("text/plain text/css application/json application/javascript", config["http:gzip_types"]!.Value);
    }

    [Fact]
    public void Parses_SslServerBlock()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                listen 443 ssl http2;
                listen [::]:443 ssl http2;
                server_name example.com www.example.com;
                ssl_certificate /etc/nginx/ssl/example.com.crt;
                ssl_certificate_key /etc/nginx/ssl/example.com.key;
                ssl_protocols TLSv1.2 TLSv1.3;
                ssl_ciphers HIGH:!aNULL:!MD5;
                ssl_session_cache shared:SSL:10m;
                ssl_session_timeout 10m;
                root /var/www/example;
                index index.html index.htm;
              }
            }
            """);

        Assert.Equal("443 ssl http2", config["http:server:listen"]!.Value);
        Assert.Equal("[::]:443 ssl http2", config["http:server:listen[1]"]!.Value);
        Assert.Equal("example.com www.example.com", config["http:server:server_name"]!.Value);
        Assert.Equal("/etc/nginx/ssl/example.com.crt", config["http:server:ssl_certificate"]!.Value);
        Assert.Equal("/etc/nginx/ssl/example.com.key", config["http:server:ssl_certificate_key"]!.Value);
        Assert.Equal("TLSv1.2 TLSv1.3", config["http:server:ssl_protocols"]!.Value);
        Assert.Equal("HIGH:!aNULL:!MD5", config["http:server:ssl_ciphers"]!.Value);
        Assert.Equal("shared:SSL:10m", config["http:server:ssl_session_cache"]!.Value);
        Assert.Equal("10m", config["http:server:ssl_session_timeout"]!.Value);
        Assert.Equal("/var/www/example", config["http:server:root"]!.Value);
        Assert.Equal("index.html index.htm", config["http:server:index"]!.Value);
    }

    [Fact]
    public void Parses_HttpToHttpsRedirectServer()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                listen 80;
                server_name example.com;
                return 301 https://$host$request_uri;
              }
            }
            """);

        Assert.Equal("80", config["http:server:listen"]!.Value);
        Assert.Equal("301 https://$host$request_uri", config["http:server:return"]!.Value);
    }

    [Fact]
    public void Parses_ReturnDirective_WithStatusAndUrl()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                location /old {
                  return 301 https://example.com/new;
                }
              }
            }
            """);

        Assert.Equal("301 https://example.com/new", config["http:server:location:return"]!.Value);
    }

    [Fact]
    public void Parses_ProxyPassAndProxyHeaders()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                location /api/ {
                  proxy_pass http://backend;
                  proxy_http_version 1.1;
                  proxy_set_header Host $host;
                  proxy_set_header X-Real-IP $remote_addr;
                  proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
                  proxy_set_header X-Forwarded-Proto $scheme;
                  proxy_set_header Upgrade $http_upgrade;
                  proxy_set_header Connection upgrade;
                  proxy_read_timeout 60s;
                  proxy_connect_timeout 5s;
                  proxy_buffering off;
                }
              }
            }
            """);

        Assert.Equal("http://backend", config["http:server:location:proxy_pass"]!.Value);
        Assert.Equal("1.1", config["http:server:location:proxy_http_version"]!.Value);
        Assert.Equal("Host $host", config["http:server:location:proxy_set_header"]!.Value);
        Assert.Equal(6, config.GetTokens("http:server:location:proxy_set_header").Count);
        Assert.Equal("60s", config["http:server:location:proxy_read_timeout"]!.Value);
        Assert.Equal("5s", config["http:server:location:proxy_connect_timeout"]!.Value);
        Assert.Equal("off", config["http:server:location:proxy_buffering"]!.Value);
    }

    [Fact]
    public void Parses_TryFilesAndStaticLocation()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                root /var/www/html;
                location / {
                  try_files $uri $uri/ /index.html;
                }
                location /assets/ {
                  alias /var/www/assets/;
                  expires 7d;
                  access_log off;
                }
              }
            }
            """);

        Assert.Equal("/var/www/html", config["http:server:root"]!.Value);
        Assert.Equal("$uri $uri/ /index.html", config["http:server:location:try_files"]!.Value);
        Assert.Equal("/assets/", config["http:server:location[1]"]!.Value);
        Assert.Equal("/var/www/assets/", config["http:server:location[1]:alias"]!.Value);
        Assert.Equal("7d", config["http:server:location[1]:expires"]!.Value);
        Assert.Equal("off", config["http:server:location[1]:access_log"]!.Value);
    }

    [Fact]
    public void Parses_RewriteDirectives()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                location /blog {
                  rewrite ^/blog/(.*)$ /index.php?post=$1 last;
                  rewrite ^/feed$ /index.php?feed=rss permanent;
                }
              }
            }
            """);

        var rewrites = config.GetTokens("http:server:location:rewrite");
        Assert.Equal(2, rewrites.Count);
        Assert.Equal("^/blog/(.*)$ /index.php?post=$1 last", rewrites[0].Value);
        Assert.Equal("^/feed$ /index.php?feed=rss permanent", rewrites[1].Value);
    }

    [Fact]
    public void Parses_UpstreamWithWeightsAndBackup()
    {
        var config = NginxConfig.Load("""
            http {
              upstream app_servers {
                server 10.0.0.1:8080 weight=3;
                server 10.0.0.2:8080 weight=2;
                server 10.0.0.3:8080 backup;
                keepalive 32;
              }
              server {
                location / {
                  proxy_pass http://app_servers;
                }
              }
            }
            """);

        var upstream = config.GetToken("http:upstream") as GroupToken;
        Assert.NotNull(upstream);
        Assert.Equal("app_servers", upstream.Value);

        var servers = config.GetTokens("http:upstream:server");
        Assert.Equal(3, servers.Count);
        Assert.Equal("10.0.0.1:8080 weight=3", servers[0].Value);
        Assert.Equal("10.0.0.2:8080 weight=2", servers[1].Value);
        Assert.Equal("10.0.0.3:8080 backup", servers[2].Value);
        Assert.Equal("32", config["http:upstream:keepalive"]!.Value);
        Assert.Equal("http://app_servers", config["http:server:location:proxy_pass"]!.Value);
    }

    [Fact]
    public void Parses_FastcgiPhpLocation()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                root /var/www/wordpress;
                index index.php;
                location ~ \.php$ {
                  include fastcgi_params;
                  fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;
                  fastcgi_pass unix:/run/php/php8.2-fpm.sock;
                  fastcgi_index index.php;
                  fastcgi_read_timeout 120;
                }
              }
            }
            """);

        Assert.Equal("~ \\.php$", config["http:server:location"]!.Value);
        Assert.Equal("fastcgi_params", config["http:server:location:include"]!.Value);
        Assert.Equal("SCRIPT_FILENAME $document_root$fastcgi_script_name", config["http:server:location:fastcgi_param"]!.Value);
        Assert.Equal("unix:/run/php/php8.2-fpm.sock", config["http:server:location:fastcgi_pass"]!.Value);
        Assert.Equal("index.php", config["http:server:location:fastcgi_index"]!.Value);
        Assert.Equal("120", config["http:server:location:fastcgi_read_timeout"]!.Value);
    }

    [Fact]
    public void Parses_MapBlock()
    {
        var config = NginxConfig.Load("""
            http {
              map $http_upgrade $connection_upgrade {
                default upgrade;
                websocket upgrade;
              }
              server {
                location / {
                  proxy_set_header Upgrade $http_upgrade;
                  proxy_set_header Connection $connection_upgrade;
                }
              }
            }
            """);

        var map = config.GetToken("http:map") as GroupToken;
        Assert.NotNull(map);
        Assert.Equal("$http_upgrade $connection_upgrade", map.Value);
        Assert.Equal("upgrade", config["http:map:default"]!.Value);
        Assert.Equal("upgrade", config["http:map:websocket"]!.Value);
    }

    [Fact]
    public void Parses_LimitReqAndLimitConn()
    {
        var config = NginxConfig.Load("""
            http {
              limit_req_zone $binary_remote_addr zone=one:10m rate=5r/s;
              limit_conn_zone $binary_remote_addr zone=addr:10m;
              server {
                location /login {
                  limit_req zone=one burst=10 nodelay;
                  limit_conn addr 10;
                }
              }
            }
            """);

        Assert.Equal("$binary_remote_addr zone=one:10m rate=5r/s", config["http:limit_req_zone"]!.Value);
        Assert.Equal("$binary_remote_addr zone=addr:10m", config["http:limit_conn_zone"]!.Value);
        Assert.Equal("zone=one burst=10 nodelay", config["http:server:location:limit_req"]!.Value);
        Assert.Equal("addr 10", config["http:server:location:limit_conn"]!.Value);
    }

    [Fact]
    public void Parses_AddHeaderWithoutSemicolonInQuotes()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                add_header X-Frame-Options SAMEORIGIN;
                add_header X-Content-Type-Options nosniff;
                add_header Referrer-Policy no-referrer-when-downgrade;
                add_header Strict-Transport-Security "max-age=31536000" always;
              }
            }
            """);

        var headers = config.GetTokens("http:server:add_header");
        Assert.Equal(4, headers.Count);
        Assert.Equal("X-Frame-Options SAMEORIGIN", headers[0].Value);
        Assert.Equal("X-Content-Type-Options nosniff", headers[1].Value);
        Assert.Equal("Referrer-Policy no-referrer-when-downgrade", headers[2].Value);
        Assert.Equal("Strict-Transport-Security \"max-age=31536000\" always", headers[3].Value);
    }

    [Fact]
    public void Parses_AccessDenyAndAllow()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                location /admin {
                  allow 10.0.0.0/8;
                  allow 192.168.0.0/16;
                  deny all;
                }
              }
            }
            """);

        var allows = config.GetTokens("http:server:location:allow");
        Assert.Equal(2, allows.Count);
        Assert.Equal("10.0.0.0/8", allows[0].Value);
        Assert.Equal("192.168.0.0/16", allows[1].Value);
        Assert.Equal("all", config["http:server:location:deny"]!.Value);
    }

    [Fact]
    public void Parses_TypesBlock()
    {
        var config = NginxConfig.Load("""
            http {
              types {
                text/html html htm shtml;
                text/css css;
                application/javascript js;
                image/svg+xml svg svgz;
              }
              default_type application/octet-stream;
            }
            """);

        var types = config.GetToken("http:types") as GroupToken;
        Assert.NotNull(types);
        // MIME type keys contain '/' / '+' so path API cannot address them; assert via group tokens.
        Assert.Contains(types.Tokens, t => t is ValueToken v && v.Key == "text/html" && v.Value == "html htm shtml");
        Assert.Contains(types.Tokens, t => t is ValueToken v && v.Key == "text/css" && v.Value == "css");
        Assert.Contains(types.Tokens, t => t is ValueToken v && v.Key == "application/javascript" && v.Value == "js");
        Assert.Contains(types.Tokens, t => t is ValueToken v && v.Key == "image/svg+xml" && v.Value == "svg svgz");
        Assert.Equal("application/octet-stream", config["http:default_type"]!.Value);
    }

    [Fact]
    public void Parses_MultipleVhostsAndLocations()
    {
        var config = NginxConfig.Load("""
            http {
              server {
                listen 80;
                server_name api.example.com;
                location /health {
                  return 200 ok;
                }
                location /v1/ {
                  proxy_pass http://api_v1;
                }
              }
              server {
                listen 80;
                server_name static.example.com;
                root /var/www/static;
                location ~* \.(jpg|jpeg|png|gif|ico|css|js)$ {
                  expires 30d;
                  access_log off;
                }
              }
            }
            """);

        Assert.Equal("api.example.com", config["http:server:server_name"]!.Value);
        Assert.Equal("/health", config["http:server:location"]!.Value);
        Assert.Equal("200 ok", config["http:server:location:return"]!.Value);
        Assert.Equal("/v1/", config["http:server:location[1]"]!.Value);
        Assert.Equal("http://api_v1", config["http:server:location[1]:proxy_pass"]!.Value);

        Assert.Equal("static.example.com", config["http:server[1]:server_name"]!.Value);
        Assert.Equal("/var/www/static", config["http:server[1]:root"]!.Value);
        Assert.Equal("~* \\.(jpg|jpeg|png|gif|ico|css|js)$", config["http:server[1]:location"]!.Value);
        Assert.Equal("30d", config["http:server[1]:location:expires"]!.Value);
    }

    [Fact]
    public void Parses_LogFormatAndAccessLog()
    {
        // Single-line log_format (multi-line quoted forms are a known parser limitation).
        var config = NginxConfig.Load("""
            http {
              log_format main $remote_addr - $remote_user [$time_local] $status;
              log_format detailed $request_time $upstream_response_time;
              access_log /var/log/nginx/access.log main;
              access_log /var/log/nginx/detailed.log detailed;
            }
            """);

        var formats = config.GetTokens("http:log_format");
        Assert.Equal(2, formats.Count);
        Assert.Equal("main $remote_addr - $remote_user [$time_local] $status", formats[0].Value);
        Assert.Equal("detailed $request_time $upstream_response_time", formats[1].Value);

        var logs = config.GetTokens("http:access_log");
        Assert.Equal(2, logs.Count);
        Assert.Equal("/var/log/nginx/access.log main", logs[0].Value);
        Assert.Equal("/var/log/nginx/detailed.log detailed", logs[1].Value);
    }

    [Fact]
    public void Parses_OpenFileCacheAndResolver()
    {
        var config = NginxConfig.Load("""
            http {
              open_file_cache max=1000 inactive=20s;
              open_file_cache_valid 30s;
              open_file_cache_min_uses 2;
              open_file_cache_errors on;
              resolver 8.8.8.8 8.8.4.4 valid=300s;
              resolver_timeout 5s;
            }
            """);

        Assert.Equal("max=1000 inactive=20s", config["http:open_file_cache"]!.Value);
        Assert.Equal("30s", config["http:open_file_cache_valid"]!.Value);
        Assert.Equal("2", config["http:open_file_cache_min_uses"]!.Value);
        Assert.Equal("on", config["http:open_file_cache_errors"]!.Value);
        Assert.Equal("8.8.8.8 8.8.4.4 valid=300s", config["http:resolver"]!.Value);
        Assert.Equal("5s", config["http:resolver_timeout"]!.Value);
    }

    [Fact]
    public void RoundTrip_CommonReverseProxyConfig()
    {
        var original = NginxConfig.Create()
            .AddOrUpdate("worker_processes", "auto")
            .AddOrUpdate("events:worker_connections", "4096")
            .AddOrUpdate("http:gzip", "on")
            .AddOrUpdate("http:upstream", "backend", true)
            .AddOrUpdate("http:upstream:server", "127.0.0.1:8080")
            .AddOrUpdate("http:server:listen", "80")
            .AddOrUpdate("http:server:server_name", "app.local")
            .AddOrUpdate("http:server:location", "/", true)
            .AddOrUpdate("http:server:location:proxy_pass", "http://backend")
            .AddOrUpdate("http:server:location:proxy_set_header", "Host $host");

        var reloaded = NginxConfig.Load(original.ToString());

        Assert.Equal("auto", reloaded["worker_processes"]!.Value);
        Assert.Equal("4096", reloaded["events:worker_connections"]!.Value);
        Assert.Equal("on", reloaded["http:gzip"]!.Value);
        Assert.Equal("backend", reloaded["http:upstream"]!.Value);
        Assert.Equal("127.0.0.1:8080", reloaded["http:upstream:server"]!.Value);
        Assert.Equal("app.local", reloaded["http:server:server_name"]!.Value);
        Assert.Equal("http://backend", reloaded["http:server:location:proxy_pass"]!.Value);
        Assert.Equal("Host $host", reloaded["http:server:location:proxy_set_header"]!.Value);
    }
}
