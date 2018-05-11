using System;
using System.Threading;

namespace PlumMediaCenter.Business
{
    public class UserAccessor
    {
        private readonly AsyncLocal<ApiUser> _current = new AsyncLocal<ApiUser>();

        public ApiUser Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }
    public class ApiUser
    {
        private long? _Id;
        public long Id
        {
            get
            {
                if (_Id == null)
                {
                    throw new Exception("User is not logged in");
                }
                return _Id.Value;
            }
            set
            {
                _Id = value;
            }
        }
    }
}