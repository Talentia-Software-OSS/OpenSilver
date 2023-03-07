
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
using System.Windows.Data;

namespace CSHTML5.Internal
{
    internal static class JavascriptCallbackHelper
    {

        public static JavascriptCallback CreateSelfDisposedJavaScriptCallback(Action action)
        {
            var callback = new JavascriptCallback();
            callback.Callback = (Action)(() => { action(); callback.Dispose(); });
            return callback;
        }

        public static JavascriptCallback CreateSelfDisposedJavaScriptCallback<T>(Action<T> action)
        {
            var callback = new JavascriptCallback();
            callback.Callback = (Action<T>)((arg) => { action(arg); callback.Dispose(); });
            return callback;
        }

        public static JavascriptCallback CreateSelfDisposedJavaScriptCallback<T1, T2>(Action<T1, T2> action)
        {
            var callback = new JavascriptCallback();
            callback.Callback = (Action<T1, T2>)(
                (arg1, arg2) => { action(arg1, arg2); callback.Dispose(); });
            return callback;
        }

        public static JavascriptCallback CreateSelfDisposedJavaScriptCallback<T1, T2, T3>(Action<T1, T2, T3> action)
        {
            var callback = new JavascriptCallback();
            callback.Callback = (Action<T1, T2, T3>)(
                (arg1, arg2, arg3) => { action(arg1, arg2, arg3); callback.Dispose(); });
            return callback;
        }
        public static JavascriptCallback CreateSelfDisposedJavaScriptCallbackk<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action)
        {
            var callback = new JavascriptCallback();
            callback.Callback = (Action<T1, T2, T3, T4>)(
                (arg1, arg2, arg3, arg4) => { action(arg1, arg2, arg3, arg4); callback.Dispose(); });
            return callback;
        }
        public static JavascriptCallback CreateSelfDisposedJavaScriptCallback<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action)
        {
            var callback = new JavascriptCallback();
            callback.Callback = (Action<T1, T2, T3, T4, T5>)(
                (arg1, arg2, arg3, arg4, arg5) => { action(arg1, arg2, arg3, arg4, arg5); callback.Dispose(); });
            return callback;
        }
    }
}