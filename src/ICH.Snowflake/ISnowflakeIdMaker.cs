namespace ICH.Snowflake
{
    public interface ISnowflakeIdMaker
    {
        long NextId(int? workId = null);
    }
}