using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;

namespace PlumMediaCenter
{
    public class ThreadStorage
    {
        public static T Resolve<T>(string key, Func<T> factory)
        {
            if (Slots.ContainsKey(key) == false)
            {
                Slots[key] = Thread.AllocateNamedDataSlot(key);
                var factoryVal = factory();
                Thread.SetData(Slots[key], factoryVal);
            }
            var value = Thread.GetData(Slots[key]);
            return (T)value;

        }
        private static Dictionary<string, LocalDataStoreSlot> Slots = new Dictionary<string, LocalDataStoreSlot>();

    }
}