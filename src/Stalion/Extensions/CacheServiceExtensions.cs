using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stalion.Extensions
{
    public static class CacheServiceExtensions
    {
        public static T Get<T>(this Services.IEditableStringCacheService cacheService, string key, Func<T> acquire)
        {
            return Get(cacheService, key, 60, acquire);
        }

        public static T Get<T>(this Services.IEditableStringCacheService cacheService, string key, int cacheTime, Func<T> acquire)
        {
            if(cacheService.IsSet(key))
            {
                return cacheService.Get<T>(key);
            }
            else
            {
                var result = acquire();
                cacheService.Set(key, result, cacheTime);
                return result;
            }
        }

    }
}
