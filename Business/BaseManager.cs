using System.Data;

namespace PlumMediaCenter.Business
{
    public class BaseManager
    {
        public BaseManager(Manager manager = null)
        {
            this.Manager = manager;
        }
        public Manager Manager;
        public IDbConnection Connection
        {
            get
            {
                return this.Manager.Connection;
            }
        }
    }
}