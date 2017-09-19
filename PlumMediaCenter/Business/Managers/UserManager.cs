using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace PlumMediaCenter.Business.Managers
{
    public class UserManager : BaseManager
    {
        public UserManager(Manager manager) : base(manager)
        {
        }

        /// <summary>
        /// The userId of the currently logged in user. 
        /// </summary>
        /// <returns></returns>
        public int CurrentProfileId
        {
            get
            {
                return 1;
            }
        }
    }
}