
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
using System.Runtime.CompilerServices;
using System.Windows;

namespace CSHTML5.Internal
{
    internal class JavascriptCallback : IDisposable
    {
        private static readonly SynchronizedStore<JavascriptCallback> _store = new SynchronizedStore<JavascriptCallback>();

        public int Id { get; private set; }

        public Delegate Callback { get; set; }

        public WeakReference<Delegate> CallbackWeakReference { get; set; }

        public static JavascriptCallback Create(Delegate callback)
        {
            var jc = new JavascriptCallback
            {
                Callback = callback
            };
            jc.Id = _store.Add(jc);

            //Console.WriteLine("{0} => {1}.{2}", jc.Id, callback.Method.ReflectedType.FullName, callback.Method.Name);

            return jc;
        }

        public static JavascriptCallback CreateWeak(Delegate callback)
        {
            var jc = new JavascriptCallback
            {
                CallbackWeakReference = new WeakReference<Delegate>(callback)
            };
            jc.Id = _store.Add(jc);

            return jc;
        }

        public static JavascriptCallback Get(int index)
        {
            return _store.Get(index);
        }

        public Delegate GetCallback()
        {
            if (Callback != null)
            {
                return Callback;
            }

            if (CallbackWeakReference.TryGetTarget(out var callback))
            {
                return callback;
            }

            _store.Clean(Id);
            Clean();

            return null;
        }

        public void Dispose()
        {
            Clean();
        }

        public void Clean()
        {
            _store.Clean(Id);
            OpenSilver.Interop.ExecuteJavaScriptFastAsync($"document.cleanupCallbackFunc({Id})");
        }
    }
}