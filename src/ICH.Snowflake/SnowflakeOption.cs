using System;

namespace ICH.Snowflake
{
    public class SnowflakeOption
    {
        /// <summary>
        /// 工作机器ID
        /// </summary>
        public uint WorkId { get; set; }
        /// <summary>
        /// 刷新存活状态的间隔时间
        /// </summary>
        public TimeSpan RefreshAliveInterval { get; set; }
    }
}