using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ICH.Snowflake.Test
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(1L<<12);

            var services = new ServiceCollection();
            services.AddSnowflake(opt => opt.WorkId = 19);
            services.AddSnowflakeWithRedis(opt =>
            {
                opt.InstanceName = "aaa:";
                opt.ConnectionString = "10.0.0.146";
                opt.RefreshAliveInterval = TimeSpan.FromHours(1);
            });
            var idMaker = services.BuildServiceProvider().GetService<ISnowflakeIdMaker>();
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(idMaker.NextId());
            }
            ;
        }
    }
}
