using System;
using ICH.Snowflake;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SnowflakeDependencyInjection
    {
        public static IServiceCollection AddSnowflake(this IServiceCollection service, Action<SnowflakeOption> option)
        {
            service.Configure(option);
            service.AddSingleton<ISnowflakeIdMaker, SnowflakeIdMaker>();
            return service;
        }
    }
}