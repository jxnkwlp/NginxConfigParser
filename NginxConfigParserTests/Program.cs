using System;
using System.IO;
using System.Linq;
using NginxConfigParser;

namespace NginxConfigParserTests
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // create new file 
            NginxConfig.Create()
                .AddOrUpdate("http:server:listen", "80")
                .AddOrUpdate("http:server:root", "/var/wwwroot")
                // add location 
                .AddOrUpdate("http:server:location", "/", true, comment: "default")
                .AddOrUpdate("http:server:location:root", "/app1")
                // add location
                .AddOrUpdate("http:server:location[1]", "~ ^/(images|javascript|js|css|flash|media|static)/", true)
                .AddOrUpdate("http:server:location[1]:root", "/app2")
                .AddOrUpdate("http:server:location[1]:expires", "/1d")
                // add location 
                .AddOrUpdate("http:server:location[2]", "~/api", true, comment: "api")
                .AddOrUpdate("http:server:location[2]:proxy_pass", "http://server.com")
                // save file 
                .Save("temp2.conf");

            Console.WriteLine("temp2.conf content: ");
            Console.WriteLine(File.ReadAllText("temp2.conf"));

            // load exist files
            var config = NginxConfig.LoadFrom("test.conf");

            // read group 
            var group = config.GetGroup("http");
            Console.WriteLine(config.GetGroup("http"));

            // read key 
            Console.WriteLine(config["error_log"]);
            Console.WriteLine(config.GetToken("error_log"));

            // read key 
            Console.WriteLine(config["http"]);

            // read key 
            //Console.WriteLine(config["http:include"]);
            //Console.WriteLine(config["http:include[1]"]);

            // read key 
            config.GetTokens("http:include").ToList().ForEach(item =>
            {
                Console.WriteLine(item);
            });

            // read
            Console.WriteLine(config["http:sendfile"]);
            Console.WriteLine(config.GetToken("http:sendfile"));

            // update value
            config.AddOrUpdate("http:sendfile", "off", comment: "updated!");
            Console.WriteLine(config["http:sendfile"]);

            // add value
            config.AddOrUpdate("http:root", "/var/wwwroot");
            Console.WriteLine(config["http:root"]);

            // add group and value 
            config.AddOrUpdate("http:server2:root", "/var/wwwroot");
            Console.WriteLine(config["http:server2:root"]);

            //config.AddOrUpdate("http:server:root", "/var/wwwroot", "updated 1");
            //Console.WriteLine(config["http:server:root"]);

            // update
            config.AddOrUpdate("http:server[0]:root", "/var/wwwroot", comment: "updated 2");
            Console.WriteLine(config["http:server[0]:root"]);

            // update
            config.AddOrUpdate("http:server[1]:root", "/var/wwwroot", comment: "updated 3");
            Console.WriteLine(config["http:server[1]:root"]);

            // add group and value 
            config.AddOrUpdate("http:server[3]:root", "/var/wwwroot", comment: "new");
            Console.WriteLine(config["http:server[3]:root"]);

            // add value to group 
            config.AddOrUpdate("http:server[3]:location", "/", true, comment: "new loaction");
            config.AddOrUpdate("http:server[3]:location:root", "/var/wwwroot");
            Console.WriteLine(config["http:server[3]:location:root"]);

            // remove
            // config.Remove("http:upstream");

            // save as file
            config.Save("temp.conf");

            // get content and save 
            // Console.WriteLine(config.ToString());
            // File.WriteAllText("temp2.conf", config.ToString());
        }
    }
}
