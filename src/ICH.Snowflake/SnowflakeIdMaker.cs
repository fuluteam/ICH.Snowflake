using System;
using System.ComponentModel;
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
        /// 工作机器长度，最大支持1024个节点，可根据实际情况调整，比如调整为9，则最大支持512个节点，可把多出来的一位分配至序号，提高单位毫秒内支持的最大序号
        /// </summary>
        private readonly int _workIdLength;
        /// <summary>
        /// 支持的最大工作节点
        /// </summary>
        private readonly int _maxWorkId;

        /// <summary>
        /// 序号长度，最大支持4096个序号
        /// </summary>
        private readonly int _indexLength;
        /// <summary>
        /// 支持的最大序号
        /// </summary>
        private readonly int _maxIndex;

        /// <summary>
        /// 当前工作节点
        /// </summary>
        private int? _workId;

        private readonly IServiceProvider _provider;


        public SnowflakeIdMaker(IOptions<SnowflakeOption> options, IServiceProvider provider)
        {
            _provider = provider;
            _option = options.Value;
            _workIdLength = _option.WorkIdLength;
            _maxWorkId = 1 << _workIdLength;
            //工作机器id和序列号的总长度是22位，为了使组件更灵活，根据机器id的长度计算序列号的长度。
            _indexLength = 22 - _workIdLength;
            _maxIndex = 1 << _indexLength;

        }

        private async Task Init()
        {
            var distributed = _provider.GetService<IDistributedSupport>();
            if (distributed != null)
            {
                _workId = await distributed.GetNextWorkId();
            }
            else
            {
                _workId = _option.WorkId;
            }
        }

        public long NextId(int? workId = null)
        {
            if (workId != null)
            {
                _workId = workId.Value;
            }
            if (_workId > _maxWorkId)
            {
                throw new ArgumentException($"机器码取值范围为0-{_maxWorkId}");
            }

            lock (locker)
            {
                if (_workId == null)
                {
                    Init().Wait();
                }
                var currentTimeStamp = TimeStamp();
                if (lastIndex >= _maxIndex)
                {
                    //如果当前序列号大于允许的最大序号，则表示，当前单位毫秒内，序号已用完，则获取时间戳。
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
                    return NextId(workId);
                }

                long id = 0L;
                var work = _workId.Value << _indexLength;
                var time = currentTimeStamp << (_indexLength + _workIdLength);
                id = time | work | lastIndex;
                lastIndex++;
                return id;
            }
        }
        private long TimeStamp(long lastTimestamp = 0L)
        {
            var current = (DateTime.Now.Ticks - _option.StartTimeStamp.Ticks) / 10000;
            if (lastTimestamp == current)
            {
                return TimeStamp(lastTimestamp);
            }
            return current;
        }
    }
}