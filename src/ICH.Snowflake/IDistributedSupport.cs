using System.Threading.Tasks;

namespace ICH.Snowflake
{
public interface IDistributedSupport
{
    /// <summary>
    /// 获取下一个可用的机器id
    /// </summary>
    /// <returns></returns>
    Task<int> GetNextWorkId();
    /// <summary>
    /// 刷新机器id的存活状态
    /// </summary>
    /// <returns></returns>
    Task RefreshAlive();
}
}