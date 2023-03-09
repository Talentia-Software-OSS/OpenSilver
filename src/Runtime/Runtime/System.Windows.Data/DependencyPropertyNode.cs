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
using System.Diagnostics;
using CSHTML5.Internal;

#if MIGRATION
namespace System.Windows.Data
#else
namespace Windows.UI.Xaml.Data
#endif
{
    internal sealed class DependencyPropertyNode : PropertyPathNode
    {
        private readonly DependencyProperty _dp;
        private IPropertyChangedListener _dpListener;

        internal DependencyPropertyNode(DependencyProperty dp)
        {
            Debug.Assert(dp != null, "dp can't be null !");

            _dp = dp;
        }

        internal override Type TypeImpl => _dp.PropertyType;
        
        internal override void SetValue(object value)
        {
            DependencyObject sourceDO = GetSourceDO();
            if (sourceDO != null)
            {
                sourceDO.SetValue(_dp, value);
            }
        }

        internal override void UpdateValue()
        {
            DependencyObject sourceDO = GetSourceDO();
            if (sourceDO != null)
            {
                object value = sourceDO.GetValue(_dp);
                UpdateValueAndIsBroken(value, value == null);
            }
        }

        internal override void OnSourceChanged(object oldvalue, object newValue)
        {
            IPropertyChangedListener listener = _dpListener;
            if (listener != null)
            {
                _dpListener = null;
                listener.Detach();
            }

            DependencyObject sourceDO = GetSourceDO();
            if (sourceDO != null)
            {
                _dpListener = INTERNAL_PropertyStore.ListenToChanged(
                    sourceDO, _dp, OnPropertyChanged
                );
            }
        }

        private DependencyObject GetSourceDO()
        {
            return GetSourceObj() as DependencyObject;
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Binding exception: {ex}");
            }
        }
    }
}
