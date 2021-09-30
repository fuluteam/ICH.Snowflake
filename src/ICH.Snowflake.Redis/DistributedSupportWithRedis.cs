using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ICH.Snowflake.Redis
{
    public class DistributedSupportWithRedis : IDistributedSupport
    {
        private IRedisClient _redisClient;
        /// <summary>
        /// 当前生成的work节点
        /// </summary>
        private readonly string _currentWorkIndex;
        /// <summary>
        /// 使用过的work节点
        /// </summary>
        private readonly string _inUse;

        private readonly RedisOption _redisOption;

        private int _workId;
        public DistributedSupportWithRedis(IRedisClient redisClient, IOptions<RedisOption> redisOption)
        {
            _redisClient = redisClient;
            _redisOption = redisOption.Value;
            _currentWorkIndex = "current.work.index";
            _inUse = "in.use";
        }

        public async Task<int> GetNextWorkId()
        {
            _workId = (int)(await _redisClient.IncrementAsync(_currentWorkIndex)) - 1;
            if (_workId > 1 << _redisOption.WorkIdLength)
            {
                //表示所有节点已全部被使用过，则从历史列表中，获取当前已回收的节点id
                var newWorkdId = await _redisClient.SortedRangeByScoreWithScoresAsync(_inUse, 0,
                    GetTimestamp(DateTime.Now.AddMinutes(-5)), 0, 1, Order.Ascending);
                if (!newWorkdId.Any())
                {
                    throw new Exception("没有可用的节点");
                }
                _workId = int.Parse(newWorkdId.First().Key);
            }
            //将正在使用的workId写入到有序列表中
            await _redisClient.SortedAddAsync(_inUse, _workId.ToString(), GetTimestamp());
            return _workId;
        }
        private long GetTimestamp(DateTime? time = null)
        {
            if (time == null)
            {
                time = DateTime.Now;
            }
            var dt1970 = new DateTime(1970, 1, 1);
            return (time.Value.Ticks - dt1970.Ticks) / 10000;
        }
        public async Task RefreshAlive()
        {
            await _redisClient.SortedAddAsync(_inUse, _workId.ToString(), GetTimestamp());
        }
    }
}
