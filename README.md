# NGINX configuration file parser and builder

A .net standard library for reading and writing nginx configuration files.

## Quick start

Install nuget package [![NuGet](https://img.shields.io/nuget/v/NginxConfigParser?style=flat-square)](https://www.nuget.org/packages/NginxConfigParser)

### Create new file

```cs
NginxConfig.Create()
    .AddOrUpdate("http:server:listen", "80")
    .AddOrUpdate("http:server:root", "/var/wwwroot")
    // add location
    .AddOrUpdate("http:server:location", "/", true, "default")
    .AddOrUpdate("http:server:location:root", "/app1")
    // add location
    .AddOrUpdate("http:server:location[1]", "~ ^/(images|javascript|js|css|flash|media|static)/", true)
    .AddOrUpdate("http:server:location[1]:root", "/app2")
    .AddOrUpdate("http:server:location[1]:expires", "1d")
    // add location
    .AddOrUpdate("http:server:location[2]", "~/api", true, "api")
    .AddOrUpdate("http:server:location[2]:proxy_pass", "http://server.com")
    // save file
    .Save("temp2.conf");
```

The `temp2.conf` file content ：

```
http   {

  server   {
    listen  80;
    root  /var/wwwroot;

    location  / { # default
      root  /app1;
    }

    location  ~ ^/(images|javascript|js|css|flash|media|static)/ {
      root  /app2;
      expires  1d;
    }

    location  ~/api { # api
      proxy_pass  http://server.com;
    }

  }
}

```

### Read exist file

```cs
// load from file
var config = NginxConfig.LoadFrom("nginx.conf");

// read single value
Console.WriteLine(config.GetToken("worker_processes")); // read the root key 'worker_processes'
Console.WriteLine(config["http:log_format"])); // read the key 'log_format' from http section
Console.WriteLine(config["http:server[2]:root"])); // read the key 'root' from second http server

// read multi values
// read all values by key 'include' from http section
config.GetTokens("http:include").ToList().ForEach(item =>
{
    Console.WriteLine(item);
});

// read all group section
Console.WriteLine(config.GetGroup("http"));  // get all from group key 'http'

// add or update value
config.AddOrUpdate("http:root", "/var/wwwroot");
config.AddOrUpdate("http:server:root", "/var/wwwroot/server1");
config.AddOrUpdate("http:server[2]:root", "/var/wwwroot/server2");

// add group
config.AddOrUpdate("http:server:location", "/api", true, comment: "new location");
// add value to group
config.AddOrUpdate("http:server:location:root", "/var/wwwroot/api");
Console.WriteLine(config["http:server:location:root"]);

// delete value
config.Remove("http:server:access_log"); // remove by key
config.Remove("http:server:location"); // remove the location section

// save to file
config.Save("nginx.conf");
// or get file content
string configContent = config.ToString();

```

## Buy Me A Coffee

[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/jxnkwlp)

<img src="./docs/weixin-thanks.jpg" width="300" />
