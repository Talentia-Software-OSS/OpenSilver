

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !MIGRATION
using Windows.Foundation;
#endif


#if MIGRATION
namespace System.Windows
#else
namespace Windows.UI.Xaml
#endif
{
    internal partial class ControlToWatch
    {
        private WeakReference<UIElement> _controlToWatch;
        private WeakReference<Action<Point, Size>> _onPositionOrSizeChanged;

        internal UIElement ControltoWatch
        {
            get
            {
                if (_controlToWatch != null && _controlToWatch.TryGetTarget(out var control)) {
                    return control;
                }
                IsDisposed = true;
                return null;
            }
            set
            {
                _controlToWatch = new WeakReference<UIElement>(value);
            }
        }

        internal Size PreviousSize;
        internal Point PreviousPosition;
        internal Action<Point, Size> OnPositionOrSizeChanged
        {
            get
            {
                if (_onPositionOrSizeChanged != null && _onPositionOrSizeChanged.TryGetTarget(out var onPositionOrSizeChanged))
                {
                    return onPositionOrSizeChanged;
                }
                IsDisposed = true;
                return null;
            }
            set
            {
                _onPositionOrSizeChanged = new WeakReference<Action<Point, Size>>(value);
            }
        }

        internal bool IsDisposed { get; private set; }

        internal ControlToWatch(UIElement controlToWatch, Action<Point, Size> OnPositionOrSizeChangedCallback)
        {
            ControltoWatch = controlToWatch;
            OnPositionOrSizeChanged = OnPositionOrSizeChangedCallback;
        }
    }
}
