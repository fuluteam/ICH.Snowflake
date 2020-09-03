using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ICH.Snowflake.Redis
{
    public class RedisClient : IRedisClient
    {
        private readonly string _instance;
        private readonly RedisOption _options;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private volatile ConnectionMultiplexer _connection;
        private readonly ConcurrentDictionary<int, IDatabase> _dataBases = new ConcurrentDictionary<int, IDatabase>();

        public RedisClient( IOptions<RedisOption> options)
        {
            _options = options.Value;
            _instance = _options.InstanceName;
        }

        private async Task<IDatabase> ConnectAsync(int db = -1, CancellationToken token = default)
        {
            db = db < 0 ? _options.Database : db;
            if (_dataBases.TryGetValue(db, out IDatabase cache))
            {
                if (_connection.IsConnected)
                {
                    return cache;
                }
            }
            await _connectionLock.WaitAsync(token);
            try
            {
                if (_dataBases.TryGetValue(db, out cache))
                {
                    if (_connection.IsConnected)
                    {
                        return cache;
                    }
                }

                _connection = await ConnectionMultiplexer.ConnectAsync(_options.ConnectionString);
                cache = _connection.GetDatabase(db);
                _dataBases.AddOrUpdate(db, cache, (key, value) => cache);
                return cache;
            }
            finally
            {
                _connectionLock.Release();
            }
        }




        public void Dispose()
        {
            _connectionLock?.Dispose();
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
            }
        }
        public async Task<long> IncrementAsync(string key, long num = 1, int db = -1)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            var redis = await ConnectAsync(db);
            var value = await redis.StringIncrementAsync(GetKeyForRedis(key), num);
            return value;
        }
        public async Task<bool> SortedAddAsync(string key, string member, double score, int db)
        {
            var redis = await ConnectAsync(db);
            return await redis.SortedSetAddAsync(GetKeyForRedis(key), member, score);
        }
        public async Task<Dictionary<string, double>> SortedRangeByScoreWithScoresAsync(string key, double min, double max, long skip,
            long take, Order order, int db)
        {
            var redis = await ConnectAsync(db);
            var result = await redis.SortedSetRangeByScoreWithScoresAsync(GetKeyForRedis(key), min, max, Exclude.None, order, skip, take);
            var dic = new Dictionary<string, double>();
            foreach (var entry in result)
            {
                dic.Add(entry.Element, entry.Score);
            }
            return dic;
        }
        public string GetKeyForRedis(string key)
        {
            return $"{_instance}{key}";
        }

    }
}