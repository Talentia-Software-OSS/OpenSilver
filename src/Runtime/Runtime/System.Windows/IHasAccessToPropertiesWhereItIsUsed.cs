

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


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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

    public class WeakStorageForProperties : IEnumerable<KeyValuePair<DependencyObject, DependencyProperty>>
    {

        private Dictionary<int, WeakReference<DependencyObject>> _objectRefTable;
        private Dictionary<int, List<DependencyProperty>> _propertyRefTable;

        private ConditionalWeakTable<DependencyObject, List<DependencyProperty>> table;
        private List<WeakReference<DependencyObject>> references;

        public WeakStorageForProperties()
        {
            table = new ConditionalWeakTable<DependencyObject, List<DependencyProperty>>();
            references = new List<WeakReference<DependencyObject>>();
        }

        public void Add(DependencyObject item, DependencyProperty dependencyProperty)
        {
            List<DependencyProperty> dependencyProperties = GetOrCreateValue(item);
            if (!dependencyProperties.Contains(dependencyProperty))
            {
                dependencyProperties.Add(dependencyProperty);
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
            List<KeyValuePair<DependencyObject, DependencyProperty>> retValue = new List<KeyValuePair<DependencyObject, DependencyProperty>>();
            foreach (var wdo in references.ToList())
            {
                if (wdo.TryGetTarget(out var dependencyObject))
                {
                    foreach (var dp in table.GetOrCreateValue(dependencyObject).ToList())
                    {
                        retValue.Add(new KeyValuePair<DependencyObject, DependencyProperty>(dependencyObject, dp));
                    }
                }
                else
                {
                    references.Remove(wdo);
                }
            }
            return retValue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<DependencyProperty> GetOrCreateValue(DependencyObject item)
        {
            foreach (var wdo in references.ToList())
            {
                if(wdo.TryGetTarget(out var dependencyObject))
                {
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
            references.Add(new WeakReference<DependencyObject>(item));
            return table.GetOrCreateValue(item);
        }

        private void RemoveValue(DependencyObject item, DependencyProperty dependencyProperty)
        {
            if (table.TryGetValue(item, out List<DependencyProperty> dependencyProperties))
            {
                dependencyProperties.Remove(dependencyProperty);
            }
            else
            {

                // Cleanup references
                foreach (var wdo in references.ToList())
                {
                    if (!wdo.TryGetTarget(out var dependencyObject))
                    {
                        references.Remove(wdo);
                    }
                }
            }
        }
    }
}
