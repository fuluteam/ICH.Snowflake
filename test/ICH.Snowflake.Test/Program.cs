using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ICH.Snowflake.Test
{
    class Program
    {
        public static void Main(string[] args)

        {
            var services = new ServiceCollection();
            services.AddSnowflakeWithRedis(opt =>
            {
                opt.InstanceName = "aaa:";
                opt.ConnectionString = "10.0.0.146";
                opt.RefreshAliveInterval = TimeSpan.FromHours(1);
            });
            var idMaker = services.BuildServiceProvider().GetService<ISnowflakeIdMaker>();
            idMaker.NextId();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000000; i++)
            {
                var id = idMaker.NextId();
            }
            sw.Stop();
            Console.WriteLine(10000000 / sw.ElapsedMilliseconds);
        }
    }
}
