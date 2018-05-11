using System.Threading.Tasks;

namespace PlumMediaCenter.Business
{
    public interface IProcessable
    {
        Task Process();
    }
}
