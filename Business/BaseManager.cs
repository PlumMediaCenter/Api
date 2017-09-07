using System.Data;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Business
{
    public class BaseManager
    {
        public BaseManager(Manager manager = null)
        {
            this.Manager = manager;
        }

        public IDbConnection NewConnection()
        {
            return ConnectionManager.GetNewConnection();
        }
        public Manager Manager;

        public string BaseUrl
        {
            get
            {
                return this.Manager.BaseUrl;
            }
        }
    }
}