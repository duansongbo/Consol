using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace ConsoleApplication1
{
    public class FastProperyAccess
    {
        public delegate T GetPropertyValue<T>();
        public delegate void SetPropertyValue<T>(T Value);
        private static ConcurrentDictionary<string, Delegate> myDelegateCache = new ConcurrentDictionary<string, Delegate>();
        public static GetPropertyValue<T> CreateGetPropertyValueDelegate<TSource, T>(TSource obj, string propertyName)
        {

            string key = string.Format("Delegate-GetProperty-{0}-{1}", typeof(TSource).FullName, propertyName);
            GetPropertyValue<T> result = (GetPropertyValue<T>)myDelegateCache.GetOrAdd(
                key,
                newkey =>
                {
                    return Delegate.CreateDelegate(typeof(GetPropertyValue<T>), obj, typeof(TSource).GetProperty(propertyName).GetGetMethod());
                }
                );

            return result;
        }
        public static SetPropertyValue<T> CreateSetPropertyValueDelegate<TSource, T>(TSource obj, string propertyName)
        {
            string key = string.Format("Delegate-SetProperty-{0}-{1}", typeof(TSource).FullName, propertyName);
            SetPropertyValue<T> result = (SetPropertyValue<T>)myDelegateCache.GetOrAdd(
               key,
               newkey =>
               {
                   return Delegate.CreateDelegate(typeof(SetPropertyValue<T>), obj, typeof(TSource).GetProperty(propertyName).GetSetMethod());
               }
               );

            return result;
        }
    }
}
