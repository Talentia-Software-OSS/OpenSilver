

/*===================================================================================
* 
*   Copyright (c) Userware/OpenSilver.net
*      
*   This file is part of the OpenSilver Runtime (https://opensilver.net), which is
*   licensed under the MIT license: https://opensource.org/licenses/MIT
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/


using DotNetForHtml5;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

#if MIGRATION
namespace System.Windows
#else
namespace Windows.UI.Xaml
#endif
{
    public partial interface IHasAccessToPropertiesWhereItIsUsed
    {
        WeakStorageForProperties PropertiesWhereUsed
        {
            get;
        }
    }

    public static class WeakStorageForPropertiesTracker
    {
        private static readonly Dictionary<WeakReference, WeakStorageForProperties> tracker
            = new Dictionary<WeakReference, WeakStorageForProperties>();
        private static readonly object _lock = new object();
        private static readonly Timer _timer = new Timer(
                CleanUpTrackingStore,
                null,
                Cshtml5Initializer.CleanupTimersInterval,
                Cshtml5Initializer.CleanupTimersInterval);

        private static void CleanUpTrackingStore(object state)
        {
            lock (_lock)
            {
                foreach (var storage in tracker.ToList())
                {
                    storage.Value.CleanUpStore();
                    if (storage.Key.Target == null)
                    {
                        tracker.Remove(storage.Key);
                    }
                }
            }
        }

        public static WeakStorageForProperties GetStorage(IHasAccessToPropertiesWhereItIsUsed obj)
        {
            lock(_lock)
            {
                foreach(var storage in tracker.ToList())
                {
                    if (storage.Key.Target == null)
                    {
                        storage.Value.CleanUpStore();
                        tracker.Remove(storage.Key);
                    }
                    else if (storage.Key.Target == obj)
                    {
                        return storage.Value;
                    }
                }

                var result = new WeakStorageForProperties();
                tracker[new WeakReference(obj)] = result;
                return result;
            }
        }
    }


    public class WeakStorageForProperties : IEnumerable<KeyValuePair<DependencyObject, DependencyProperty>>
    {
        private readonly ConcurrentDictionary<WeakReference, List<DependencyProperty>> table;

        internal WeakStorageForProperties()
        {
            table = new ConcurrentDictionary<WeakReference, List<DependencyProperty>>();
        }

        public void Add(DependencyObject item, DependencyProperty dependencyProperty)
        {
            CleanUpStore();
            var keyItem = table.Keys.ToList().FirstOrDefault(refObj => refObj.Target == item);
            if (null == keyItem)
            {
                keyItem = new WeakReference(item);
            }

            table.AddOrUpdate(
                keyItem, 
                new List<DependencyProperty> { dependencyProperty },
                (_, list) => 
                {
                    lock(list)
                    {
                        list.Add(dependencyProperty); 
                        return list;
                    }
                });
        }

        public void Add(KeyValuePair<DependencyObject, DependencyProperty> tuple)
        {
            Add(tuple.Key, tuple.Value);
        }

        public void Remove(DependencyObject item, DependencyProperty dependencyProperty)
        {
            CleanUpStore();
            var keyItem = table.Keys.ToList().FirstOrDefault(refObj => refObj.Target == item);
            if (keyItem != null && table.TryGetValue(keyItem, out var list))
            {
                lock (list)
                {
                    list.Remove(dependencyProperty);
                }
            }
        }

        public void Remove(KeyValuePair<DependencyObject, DependencyProperty> tuple)
        {
            Remove(tuple.Key, tuple.Value);
        }

        public IEnumerator<KeyValuePair<DependencyObject, DependencyProperty>> GetEnumerator()
        {
            CleanUpStore();
            foreach (var kvp in table)
            {
                var item = kvp.Key;
                foreach (var dp in kvp.Value.ToList())
                {
                    yield return new KeyValuePair<DependencyObject, DependencyProperty>(item.Target as DependencyObject, dp);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal void CleanUpStore()
        {
            foreach (var kvp in table)
            {
                if (kvp.Key.Target == null)
                {
                    table.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
