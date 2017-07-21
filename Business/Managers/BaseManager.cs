using System.Data;

namespace PlumMediaCenter.Business.Managers
{
    public class BaseManager
    {
        public BaseManager(Manager manager = null)
        {
            this.Manager = manager != null ? manager : new Manager();
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