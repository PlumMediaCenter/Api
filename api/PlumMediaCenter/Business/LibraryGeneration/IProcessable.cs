using System.Threading.Tasks;

namespace PlumMediaCenter.Business.LibraryGeneration
{
    public interface IProcessable
    {
        Task Process();
    }
}
