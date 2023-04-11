

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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ConditionalWeakTable<DependencyObject, List<DependencyProperty>> table;
        private readonly List<WeakReference> references;
        private readonly object referencesLock = new object();

        internal WeakStorageForProperties()
        {
            table = new ConditionalWeakTable<DependencyObject, List<DependencyProperty>>();
            references = new List<WeakReference>();
        }

        public void Add(DependencyObject item, DependencyProperty dependencyProperty)
        {
            List<DependencyProperty> dependencyProperties = GetOrCreateValue(item);
            lock (dependencyProperties)
            {
                if (!dependencyProperties.Contains(dependencyProperty))
                {
                    dependencyProperties.Add(dependencyProperty);
                }
            }
        }

        public void Add(KeyValuePair<DependencyObject, DependencyProperty> tuple)
        {
            Add(tuple.Key, tuple.Value);
        }

        public void Remove(DependencyObject item, DependencyProperty dependencyProperty)
        {
            RemoveValue(item, dependencyProperty);
        }

        public void Remove(KeyValuePair<DependencyObject, DependencyProperty> tuple)
        {
            RemoveValue(tuple.Key, tuple.Value);
        }

        public IEnumerator<KeyValuePair<DependencyObject, DependencyProperty>> GetEnumerator()
        {
            lock (referencesLock)
            {
                List<KeyValuePair<DependencyObject, DependencyProperty>> retValue = new List<KeyValuePair<DependencyObject, DependencyProperty>>();
                foreach (var wdo in references.ToList())
                {
                    if (wdo.Target != null)
                    {
                        var dependecyObject = wdo.Target as DependencyObject;
                        foreach (var dp in table.GetOrCreateValue(dependecyObject).ToList())
                        {
                            retValue.Add(new KeyValuePair<DependencyObject, DependencyProperty>(dependecyObject, dp));
                        }
                    }
                    else
                    {
                        references.Remove(wdo);
                    }
                }
                return retValue.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<DependencyProperty> GetOrCreateValue(DependencyObject item)
        {
            lock (referencesLock)
            {
                foreach (var wdo in references.ToList())
                {
                    if (wdo.Target != null)
                    {
                        var dependencyObject = wdo.Target as DependencyObject;

                        // We already have the item as key
                        if (item == dependencyObject)
                        {
                            return table.GetOrCreateValue(item);
                        }
                    }

                    // Cleanup
                    else
                    {
                        references.Remove(wdo);
                    }
                }

                // Not found
                references.Add(new WeakReference(item));
                return table.GetOrCreateValue(item);
            }
        }

        private void RemoveValue(DependencyObject item, DependencyProperty dependencyProperty)
        {
            if (table.TryGetValue(item, out List<DependencyProperty> dependencyProperties))
            {
                lock(dependencyProperties)
                {
                    dependencyProperties.Remove(dependencyProperty);
                }
            }
            else
            {

                // Cleanup references
                CleanUpStore();
            }
        }

        internal void CleanUpStore()
        {
            lock (referencesLock)
            {
                // Cleanup references
                foreach (var wdo in references.Where(wdo => wdo.Target == null).ToList())
                {
                    references.Remove(wdo);
                }
            }
        }
    }
}
