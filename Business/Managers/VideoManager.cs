using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business.Managers
{
    public class VideoManager : BaseManager
    {
        public VideoManager(Manager manager = null) : base(manager)
        {
        }

    }
}