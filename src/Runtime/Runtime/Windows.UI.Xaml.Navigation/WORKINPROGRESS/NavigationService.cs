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

#if WORKINPROGRESS

using System;

#if MIGRATION
namespace System.Windows.Navigation
#else

namespace Windows.UI.Xaml.Navigation
#endif
{
    public sealed class NavigationService
    {
        public bool CanGoBack { get; }

        public void GoBack()
        {

        }

        public bool Navigate(Uri source)
        {
            return false;
        }
        
    }
}

#endif