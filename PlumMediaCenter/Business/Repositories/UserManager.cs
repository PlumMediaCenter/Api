using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Business.Repositories
{
    public class UserRepository : BaseRepository<User>
    {
        public UserRepository() : base()
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