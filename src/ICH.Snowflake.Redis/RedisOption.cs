namespace ICH.Snowflake.Redis
{
    public class RedisOption: SnowflakeOption
    {
        public int Database { get; set; }
        public string ConnectionString { get; set; }
        public string InstanceName { get; set; }
    }
}