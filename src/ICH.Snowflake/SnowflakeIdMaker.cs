using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ICH.Snowflake
{
    public class SnowflakeIdMaker : ISnowflakeIdMaker
    {
        private readonly SnowflakeOption _option;
        static object locker = new object();
        //最后的时间戳
        private long lastTimestamp = -1L;
        //最后的序号
        private uint lastIndex = 0;
        /// <summary>
        /// 当前工作节点
        /// </summary>
        private int? _workId;
        private readonly IServiceProvider _provider;


        public SnowflakeIdMaker(IOptions<SnowflakeOption> options, IServiceProvider provider)
        {
            _provider = provider;
            _option = options.Value;
        }

        private async Task Init()
        {
            var distributed = _provider.GetService<IDistributedSupport>();
            if (distributed != null)
            {
                _workId = await distributed.GetNextWorkId();
            }
        }

        public long NextId(int? workId = null)
        {
            if (workId != null)
            {
                _workId = workId.Value;
            }
            if (_workId > 511)
            {
                throw new Exception("机器码取值范围为0-511");
            }

            lock (locker)
            {
                if (_workId == null)
                {
                    Init().Wait();
                }
                var currentTimeStamp = TimeStamp();
                if (lastIndex >= 8192)
                {
                    currentTimeStamp = TimeStamp(lastTimestamp);
                }
                if (currentTimeStamp > lastTimestamp)
                {
                    lastIndex = 0;
                    lastTimestamp = currentTimeStamp;
                }
                else if (currentTimeStamp < lastTimestamp)
                {
                    //throw new Exception("时间戳生成出现错误");
                    //发生时钟回拨，切换workId，可解决。
                    Init().Wait();
                    return NextId();
                }
                var time = currentTimeStamp << 22;
                var work = _workId.Value << 13;
                var id = time | work | lastIndex;
                lastIndex++;
                return id;
            }
        }
        private long TimeStamp(long lastTimestamp = 0L)
        {
            var dt1970 = new DateTime(1970, 1, 1);
            var current = (DateTime.Now.Ticks - dt1970.Ticks) / 10000;
            if (lastTimestamp == current)
            {
                return TimeStamp(lastTimestamp);
            }
            return current;
        }

        private long GetTimestamp(DateTime time)
        {
            var dt1970 = new DateTime(1970, 1, 1);
            return (time.Ticks - dt1970.Ticks) / 10000;
        }
    }
}