using System.Threading.Tasks;

namespace PlumMediaCenter.Business
{
    public interface IProcessable
    {
        int? Id { get; set; }
        Task Process();
    }
}
