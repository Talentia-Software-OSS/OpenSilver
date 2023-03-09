

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
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
#if MIGRATION
using System.Windows;
#else
using Windows.UI.Core;
#endif

namespace CSHTML5.Internal
{
    /// <summary>
    /// </summary>
    /// <exclude/>
    public class HtmlEventProxy : IDisposable
    {
        /// <summary>
        /// </summary>
        /// <exclude/>
        public class EventArgsWithJSEventObject : EventArgs
        {
            public readonly object JSEventObject;
            public EventArgsWithJSEventObject(object jsEventObject)
            {
                JSEventObject = jsEventObject;
            }
        }

        // Fields
        private EventHandler<EventArgsWithJSEventObject> _eventHandler;
        private object _sender;
        private object _domElementRef;
        private string _eventName = null;
        private JavascriptCallback _handler;

        // Constructor
        internal HtmlEventProxy(string eventName, object domElementRef, Action<object> originalEventHandler, bool sync)
        {
            this._eventName = eventName;
            this._domElementRef = domElementRef;
            this._sender = this;
            this._eventHandler = (EventHandler<HtmlEventProxy.EventArgsWithJSEventObject>)((s, e) => { originalEventHandler(e.JSEventObject); });
            this._handler = CreateHandler(sync);
        }

        internal JavascriptCallback Handler
        {
            get { return _handler; }
        }

#if BRIDGE
        [Bridge.External]
#endif
        private void OnEventImpl(object jsEventArg)
        {
            if (this._eventHandler != null)
            {
                this._eventHandler(this._sender, new EventArgsWithJSEventObject(jsEventArg));
            }
        }

        private JavascriptCallback CreateHandler(bool sync)
        {
            if (sync)
            {
                return JavascriptCallback.CreateWeak(new Func<object, string>(jsEventArg =>
                {
                    OnEventImpl(jsEventArg);
                    return "";
                }));
            }

            return JavascriptCallback.CreateWeak(new Action<object>(jsEventArg => OnEventImpl(jsEventArg)));
        }

        public void Dispose()
        {
            if (_domElementRef != null)
            {
                INTERNAL_EventsHelper.DetachEvent(_eventName, _domElementRef, this);
                _handler.Dispose();
                _handler = null;

                // Free memory:
                _domElementRef = null;
                _sender = null;
                _eventHandler = null;
            }
        }

    }
}
