
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

#if MIGRATION
namespace System.Windows.Data
#else
namespace Windows.UI.Xaml.Data
#endif
{
    internal abstract class PropertyPathNode : IPropertyPathNode
    {
        private WeakReference<IPropertyPathNodeListener> _nodeListener;
        
        protected PropertyPathNode()
        {
            Value = DependencyProperty.UnsetValue;
        }

        private WeakReference<object> Source { get; set; }

        public object Value { get; private set; }

        public bool IsBroken { get; private set; }

        public IPropertyPathNode Next { get; set; }

        internal abstract Type TypeImpl { get; }

        internal void UpdateValueAndIsBroken(object newValue, bool isBroken)
        {
            IsBroken = isBroken;
            Value = newValue;

            IPropertyPathNodeListener listener = null;
            _nodeListener?.TryGetTarget(out listener);
            if (listener != null)
            {
                listener.ValueChanged(this);
            }
        }

        internal abstract void OnSourceChanged(object oldSource, object newSource);
        
        internal abstract void UpdateValue();

        internal abstract void SetValue(object value);

        Type IPropertyPathNode.Type => TypeImpl;

        void IPropertyPathNode.SetSource(object source)
        {
            object oldSource = null;
            Source?.TryGetTarget(out oldSource);
            Source = new WeakReference<object>(source);

            if (oldSource != Source)
            {
                OnSourceChanged(oldSource, source);
            }

            UpdateValue();

            if (Next != null)
            {
                Next.SetSource(Value == DependencyProperty.UnsetValue ? null : Value);
            }
        }

        void IPropertyPathNode.SetValue(object value)
        {
            SetValue(value);
        }

        void IPropertyPathNode.Listen(IPropertyPathNodeListener listener)
        {
            if (listener != null)
            {
                _nodeListener = new WeakReference<IPropertyPathNodeListener>(listener);
            }
        }

        void IPropertyPathNode.Unlisten(IPropertyPathNodeListener listener)
        {
            IPropertyPathNodeListener existingListener = null;
            _nodeListener?.TryGetTarget(out existingListener);
            if (existingListener == listener)
            {
                _nodeListener = null;
            }
        }

        protected object GetSourceObj()
        {
            object source = null;
            Source?.TryGetTarget(out source);
            return source;
        }
    }
}
