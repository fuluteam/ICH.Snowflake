using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ICH.Snowflake
{
    public class SnowflakeBackgroundServices : BackgroundService
    {
        private readonly ISnowflakeIdMaker _idMaker;
        private readonly IDistributedSupport _distributed;
        private readonly SnowflakeOption option;
        public SnowflakeBackgroundServices(ISnowflakeIdMaker idMaker, IDistributedSupport distributed, IOptions<SnowflakeOption> options)
        {
            _idMaker = idMaker;
            option = options.Value;
            _distributed = distributed;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                while (true)
                {
                    //定时刷新机器id的存活状态
                    await _distributed.RefreshAlive();
                    await Task.Delay(option.RefreshAliveInterval.Add(TimeSpan.FromMinutes(1)), stoppingToken);
                }

            }
        }
    }
}