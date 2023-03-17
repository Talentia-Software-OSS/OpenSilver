
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;

namespace CSHTML5.Internal
{
    internal class JavascriptCallback : IDisposable
    {
        private static readonly SynchronizedStore<JavascriptCallback> _store = new SynchronizedStore<JavascriptCallback>();
        private static readonly Timer _timer = new Timer(CleanUpStore, null, 1000, 1000);

        private MethodInfo _delegateInfo;
        private WeakReference _delegateTarget;
        private Delegate _delegate;
        private bool _isStaticTarget;
        private bool _isDisposed;

        public int Id { get; private set; }
        public Type ReturnType { get; private set; }
        public Type DelegateType { get; private set; }


        public static JavascriptCallback CreateWeak(Delegate callback)
        {
            return new JavascriptCallback(callback);
        }

        public static JavascriptCallback Create(Delegate callback)
        {
            return new JavascriptCallback(callback, false);
        }

        public static JavascriptCallback Get(int index)
        {
            return _store.Get(index);
        }

        private static void CleanUpStore(object state)
        {
            _store.Where(jc => 
                !jc._isStaticTarget && 
                !jc._isDisposed &&
                jc._delegateTarget != null &&
                !jc._delegateTarget.IsAlive).ForEach(jc => 
                {
                    //Console.WriteLine("Cleaning up {0}", jc.Id);
                    _store.Clean(jc.Id);
                });
        }

        private JavascriptCallback(Delegate callback)
            :this(callback, true)
        { 
        }

        private JavascriptCallback(Delegate callback, bool createWeak)
        {
            Id = _store.Add(this);
            DelegateType = callback.GetType();
            ReturnType = callback.Method.ReturnType;

            // Create hard links for Dynamic classes
            if (createWeak && callback.Target != null && IsGeneratedClass(callback.Target.GetType()))
            {
                //Console.WriteLine(callback.Target.GetType().FullName);
                createWeak = false;
            }

            if (createWeak)
            {
                _isStaticTarget = callback.Target == null;
                _delegateTarget = new WeakReference(callback.Target);
                _delegateInfo = callback.Method;
            }
            else
            {
                _delegate = callback;
            }
        }

        /// <summary>
        /// Detect Dynamic types generated when creating an action that only survives in a given context
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool IsGeneratedClass(Type type)
        {
            return type.Name.Contains("DisplayClass");
        }

        public object InvokeDelegate(params object[] args)
        {
            if (_delegate != null)
            {
                return _delegate.DynamicInvoke(args);
            }
            else
            {
                if (null != _delegateTarget.Target)
                {
                    return _delegateInfo.Invoke(_delegateTarget.Target, args);
                }
                else if (_isStaticTarget)
                {
                    return _delegateInfo.Invoke(null, args);
                }
                return null;
            }
        }

        ~JavascriptCallback()
        {
            Dispose();
        }

        public void Dispose()
        {
            Clean();
        }

        public void Clean()
        {
            if (!_isDisposed)
            {
                _store.Clean(Id);
                _delegate = null;
                _delegateTarget = null;
                OpenSilver.Interop.ExecuteJavaScriptFastAsync($"document.cleanupCallbackFunc({Id})");
                _isDisposed = true;
            }
        }
    }
}