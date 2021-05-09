# NGINX configuration file parser and builder

A .net standard library for reading and writing nginx configuration files.

## Quick start

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

// delete value
config.Remove("http:server:access_log"); // remove by key
config.Remove("http:server:location"); // remove the location section

// save to file
config.Save("nginx.conf");
// or get file content
string configContent = config.ToString();

// create new file
NginxConfig.Create()
    .AddOrUpdate("http:server:listen", "80")
    .AddOrUpdate("http:server:root", "/var/wwwroot")
    .Save("temp2.conf");

```
