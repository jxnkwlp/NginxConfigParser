using System;
using System.Linq;
using NginxConfigParser;

namespace NginxConfigParserTests
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            NginxConfig.Create()
                .AddOrUpdate("http:server:listen", "80")
                .AddOrUpdate("http:server:root", "/var/wwwroot")
                .Save("temp2.conf");

            var config = NginxConfig.LoadFrom("test.conf");
             
            Console.WriteLine(config.GetGroup("http"));

            Console.WriteLine(config["error_log"]);
            Console.WriteLine(config.GetToken("error_log"));

            Console.WriteLine(config["http"]);

            //Console.WriteLine(config["http:include"]);
            //Console.WriteLine(config["http:include[1]"]);

            config.GetTokens("http:include").ToList().ForEach(item =>
            {
                Console.WriteLine(item);
            });

            Console.WriteLine(config["http:sendfile"]);
            Console.WriteLine(config.GetToken("http:sendfile"));

            config.AddOrUpdate("http:sendfile", "off", "updated!");

            Console.WriteLine(config["http:sendfile"]);

            config.AddOrUpdate("http:root", "/var/wwwroot");
            Console.WriteLine(config["http:root"]);

            config.AddOrUpdate("http:server2:root", "/var/wwwroot");
            Console.WriteLine(config["http:server2:root"]);

            //config.AddOrUpdate("http:server:root", "/var/wwwroot", "updated 1");
            //Console.WriteLine(config["http:server:root"]);

            config.AddOrUpdate("http:server[0]:root", "/var/wwwroot", "updated 2");
            Console.WriteLine(config["http:server[0]:root"]);

            config.AddOrUpdate("http:server[1]:root", "/var/wwwroot", "updated 3");
            Console.WriteLine(config["http:server[1]:root"]);

            config.AddOrUpdate("http:server[3]:root", "/var/wwwroot", "new");
            Console.WriteLine(config["http:server[3]:root"]);

            //Console.WriteLine(config["http:server"]);
            //config.Remove("http:server:access_log");
            //Console.WriteLine(config["http:server"]);

            //Console.WriteLine(config["http:server:location"]);
            //config.Remove("http:server:location");
            //Console.WriteLine(config["http:server:location"]);


            config.Save("temp.conf");

            // Console.WriteLine(config.ToString());
        }
    }
}
