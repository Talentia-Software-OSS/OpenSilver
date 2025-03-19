

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

extern alias opensilver;
using INTERNAL_Simulator = opensilver::DotNetForHtml5.Core.INTERNAL_Simulator;

using System;
using System.Runtime.InteropServices;
using opensilver::CSHTML5.Types;
using Microsoft.Web.WebView2.Core;
using Simulator.WebView2.OpenSilver.Interop;

namespace DotNetForHtml5.EmulatorWithoutJavascript
{
    [ComVisible(true)]
    public class OnCallbackSimulator2
    {
        public OnCallbackSimulator2()
        {
            CheckIsRunningInTheSimulator();
        }

        public void OnCallbackFromJavaScriptError(string idWhereCallbackArgsAreStored)
        {
            opensilver::CSHTML5.Internal.OnCallBackImpl.Instance.OnCallbackFromJavaScriptError(idWhereCallbackArgsAreStored);
        }

        public object OnCallbackFromJavaScript(
            int callbackId,
            string idWhereCallbackArgsAreStored,
            object callbackArgsObject,
            bool returnValue)
        {
            object result = null;
            var actionExecuted = false;
            void InvokeCallback()
            {
                try
                {
                    result = opensilver::CSHTML5.Internal.OnCallBackImpl.Instance.OnCallbackFromJavaScript(
                        callbackId,
                        idWhereCallbackArgsAreStored,
                        callbackArgsObject,
                        MakeArgumentsForCallbackSimulator,
                        true,
                        returnValue
                    );

                    actionExecuted = true;
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine("DEBUG: OnCallBack: OnCallBackFromJavascript: " + ex);
                    throw;
                }
            }

            // Go back to the UI thread because DotNetBrowser calls the callback from the socket background thread:
            if (returnValue)
            {
                var timeout = TimeSpan.FromSeconds(30);
                INTERNAL_Simulator.OpenSilverDispatcherInvoke(InvokeCallback, timeout);
                if (!actionExecuted)
                {
                    throw GenerateDeadlockException(timeout);
                }
            }
            else
            {
                INTERNAL_Simulator.OpenSilverDispatcherBeginInvoke(InvokeCallback);
            }

            return returnValue ? result : null;
        }

        private static object[] MakeArgumentsForCallbackSimulator(
            int count,
            int callbackId,
            string idWhereCallbackArgsAreStored,
            object callbackArgs,
            Type[] callbackGenericArgs)
        {
            var result = new object[count];

            for (int i = 0; i < count; i++)
            {
                var arg = new INTERNAL_JSObjectReference(callbackArgs, idWhereCallbackArgsAreStored, i);
                if (callbackGenericArgs != null
                    && i < callbackGenericArgs.Length
                    && callbackGenericArgs[i] != typeof(object)
                    && (
                    callbackGenericArgs[i].IsPrimitive
                    || callbackGenericArgs[i] == typeof(string)))
                {
                    // Attempt to cast from JS object to the desired primitive or string type. This is useful for example when passing an Action<string> to an Interop.ExecuteJavaScript so as to not get an exception that says that it cannot cast the JS object into string (when running in the Simulator only):
                    result[i] = Convert.ChangeType(arg, callbackGenericArgs[i]);
                }
                else
                {
                    result[i] = arg;
                }
            }
            return result;
        }

        private static void CheckIsRunningInTheSimulator()
        {
            if (!opensilver::OpenSilver.Interop.IsRunningInTheSimulator)
            {
                throw new InvalidOperationException($"'{nameof(OnCallbackSimulator2)}' is not supported in the browser.");
            }
        }

        private static ApplicationException GenerateDeadlockException(TimeSpan timeout)
        {
            return new ApplicationException(
                $"The callback method has not finished execution in {timeout} seconds.\n" +
                "This method was called in a sync way, and very likely, the process is deadlocked. It happens when the code from the UI thread calls JS code, which calls C# back synchronously.\n" +
                "The Example:\n" +
                "OpenSilver.Interop.ExecuteJavaScript(\"$0();\", (Func<string>)(() => \"Message from C#\"));\n" +
                "The solution:\n" +
                "If a callback returns a value(for example, a Func), verify that this callback is not invoked from the C# code in the UI thread.");
        }
    }
}