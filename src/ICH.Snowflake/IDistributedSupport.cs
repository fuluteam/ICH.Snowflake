using System.Threading.Tasks;

namespace ICH.Snowflake
{
    public interface IDistributedSupport
    {
        Task<int> GetNextWorkId();
        Task RefreshAlive();
    }
}