using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator.WebView2.OpenSilver.Interop
{
    internal static class WebView2ResultConverter
    {

        /// <summary>
        /// This method should be called as a wrapper whenever calling CoreWebView2.ExecuteScriptAsync(String) Method
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static object ConvertBrowserResult(string result)
        {
            switch (result)
            {
                case "undefined":
                    return null;
                case "null":
                    return null;
                case "true":
                    return true;
                case "false":
                    return false;

                // If the result is a number, we try to parse it
                default:
                    if (int.TryParse(result, out var resInt))
                        return resInt;
                    if (double.TryParse(result, out var resDouble))
                        return resDouble;
                    return result;
            }
        }
    }
}
