﻿
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
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Markup;
using CSHTML5.Internal;

#if MIGRATION
namespace System.Windows.Data
#else
namespace Windows.UI.Xaml.Data
#endif
{
    internal class StandardPropertyPathNode : PropertyPathNode
    {
        private readonly Type _resolvedType;
        private readonly string _propertyName;
        private readonly bool _bindsDirectlyToSource;

        private IPropertyChangedListener _dpListener;
        private DependencyProperty _dp;
        private PropertyInfo _prop;
        private FieldInfo _field;

        internal StandardPropertyPathNode(string typeName, string propertyName)
        {
            _resolvedType = typeName != null ? Type.GetType(typeName) : null;
            _propertyName = propertyName;
        }

        /// <summary>
        /// This constructor is called only when there is no path, which means 
        /// that the binding's source is directly the value we are looking for.
        /// </summary>
        internal StandardPropertyPathNode()
        {
            _bindsDirectlyToSource = true;
        }

        internal override Type TypeImpl
        {
            get
            {
                if (_dp != null)
                {
                    return _dp.PropertyType;
                }

                if (_prop != null)
                {
                    return _prop.PropertyType;
                }

                if (_field != null)
                {
                    return _field.FieldType;
                }

                return null;
            }
        }

        internal override void SetValue(object value)
        {
            object source = GetSourceObj();
            if (source == null)
            {
                Unsubscribe();
                return;
            }

            if (_dp != null)
            {
                ((DependencyObject)source).SetValue(_dp, value);
            }
            else if (_prop != null)
            {
                _prop.SetValue(source, value);
            }
            else if (_field != null)
            {
                _field.SetValue(source, value);
            }
        }

        internal override void UpdateValue()
        {
            object source = GetSourceObj();
            if (source == null)
            {
                Unsubscribe();
                return;
            }

            if (_dp != null)
            {
                UpdateValueAndIsBroken(((DependencyObject)source).GetValue(_dp), CheckIsBroken());
            }
            else if (_prop != null)
            {
                //TODO: this.ValueType = PropertyInfo.PropertyType;
                //this.ValueType = null; //todo: don't know what this is for
                try
                {
#if BRIDGE
                    //Bridge throws an exception when trying to call GetValue through PropertyInfo.GetValue on a Static Property while putting an instance as a parameter (which should not be the case in my opinion).
                    //Because of that, we need to check whether the property is Static and then accordingly call GetValue with either null or the instance as a parameter.
                    object propertyValue = null;
                    MethodInfo methodInfo = _prop.GetMethod;
                    if (INTERNAL_BridgeWorkarounds.MethodInfoIsStatic_SimulatorCompatible(methodInfo))
                    {
                        propertyValue = _prop.GetValue(null);
                    }
                    else
                    {
                        propertyValue = _prop.GetValue(this.Source);
                    }
                    UpdateValueAndIsBroken(propertyValue, CheckIsBroken());
#else
                    UpdateValueAndIsBroken(_prop.GetValue(source), CheckIsBroken());
#endif
                }
                catch
                {
                    UpdateValueAndIsBroken(null, CheckIsBroken());
                }
            }
            else if (_field != null)
            {
                try
                {
                    UpdateValueAndIsBroken(_field.GetValue(source), CheckIsBroken());
                }
                catch
                {
                    UpdateValueAndIsBroken(null, CheckIsBroken());
                }
            }
            else if (_bindsDirectlyToSource)
            {
                UpdateValueAndIsBroken(source, CheckIsBroken());
            }
            else
            {
                UpdateValueAndIsBroken(null, CheckIsBroken());
            }
        }

        internal override void OnSourceChanged(object oldValue, object newValue)
        {
            if (oldValue is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged -= new PropertyChangedEventHandler(OnSourcePropertyChanged);
            }

            IPropertyChangedListener listener = _dpListener;
            if (listener != null)
            {
                _dpListener = null;
                listener.Detach();
            }

            _dp = null;
            _prop = null;
            _field = null;
            object source = GetSourceObj();

            if (source == null)
            {
                return;
            }

            if (_bindsDirectlyToSource)
                return;

            inpc = newValue as INotifyPropertyChanged;
            if (inpc != null)
            {
                inpc.PropertyChanged += new PropertyChangedEventHandler(OnSourcePropertyChanged);
            }

            if (newValue is DependencyObject sourceDO)
            {
                Type type = _resolvedType ?? source.GetType();

                DependencyProperty dependencyProperty = INTERNAL_TypeToStringsToDependencyProperties.GetPropertyInTypeOrItsBaseTypes(type, _propertyName);

                if (dependencyProperty != null)
                {
                    _dp = dependencyProperty;
                    _dpListener = listener = INTERNAL_PropertyStore.ListenToChanged(sourceDO, dependencyProperty, OnPropertyChanged);
                }
            }

            if (_dp == null)
            {
                Type sourceType = source.GetType();
                for (Type t = sourceType; t != null; t = t.BaseType)
                {
                    _prop = t.GetProperty(
                        _propertyName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly
                    );

                    if (_prop != null)
                    {
                        break;
                    }
                }

                if (_prop == null)
                {
                    // Try in case it is a simple field instead of a property:
                    _field = sourceType.GetField(_propertyName);
                }
            }
        }

        private void OnSourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == _propertyName || string.IsNullOrEmpty(e.PropertyName)) && (_prop != null || _field != null))
            {
                UpdateValue();

                IPropertyPathNode next = Next;
                if (next != null)
                {
                    next.SetSource(Value);
                }
            }
        }

        private void OnPropertyChanged(object sender, IDependencyPropertyChangedEventArgs args)
        {
            try
            {
                UpdateValue();
                if (Next != null)
                {
                    Next.SetSource(Value);
                }
            }
            catch (XamlParseException ex)
            {
                if (CSharpXamlForHtml5.Environment.IsRunningInJavaScript)
                    throw ex;
                else
                    MessageBox.Show(ex.ToString());
            }
            catch (Exception ex)
            {
                //Ignore
                Debug.WriteLine("Binding exception: " + ex.ToString());
            }
        }

        private bool CheckIsBroken()
        {
            return GetSourceObj() == null || (!_bindsDirectlyToSource && (_prop == null && _field == null && _dp == null));
        }

        protected void Unsubscribe()
        {
            // Unsubscribe
            if (Value is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged -= new PropertyChangedEventHandler(OnSourcePropertyChanged);
            }

            IPropertyChangedListener listener = _dpListener;
            if (listener != null)
            {
                _dpListener = null;
                listener.Detach();
            }
        }
    }
}
