

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


using CSHTML5.Internal;
using OpenSilver.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if BRIDGE
using Bridge;
#endif
#if !MIGRATION
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
#endif

#if MIGRATION
namespace System.Windows.Media.Animation
#else
namespace Windows.UI.Xaml.Media.Animation
#endif
{
    internal class AnimationInfo
    {
        public JavascriptCallback Callback { get; set; }
        public string Element { get; set; }
        public string Key { get; set; }
    }


    internal static class AnimationHelpers
    {
        internal static void CallVelocity(
            AnimationTimeline animation,
            object domElement, 
            Duration Duration,
            EasingFunctionBase easingFunction, 
            string visualStateGroupName, 
            Action callbackForWhenfinished, 
            object jsFromToValues)
        {
            string easingFunctionAsString = "linear";
            if (easingFunction != null)
            {
                easingFunctionAsString = easingFunction.GetFunctionAsString();
            }

            double duration = Duration.TimeSpan.TotalMilliseconds;
            if (duration == 0)
            {
                ++duration;
            }

            string sElement = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(domElement);

            var sb = new StringBuilder();
            sb.AppendLine("(function(el) {");
            sb.AppendLine($@"const options = {{
easing:""{INTERNAL_HtmlDomManager.EscapeStringForUseInJavaScript(easingFunctionAsString)}"",
duration:{duration.ToInvariantString()},
queue:false,
queue:""{visualStateGroupName}""
}};");


            // Add callback on complete
            if (callbackForWhenfinished != null)
            {
                var callbackInfo = new AnimationInfo()
                {
                    Callback = JavascriptCallbackHelper.CreateSelfDisposedJavaScriptCallback(callbackForWhenfinished),
                    Element = sElement,
                    Key = visualStateGroupName
                };
                animation.RegisterCallback(callbackInfo);
                string callback = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(callbackInfo.Callback);
                sb.Append($"options.complete = {callback};");
            }

            if (easingFunction != null)
            {
                Dictionary<string, object> additionalOptions = easingFunction.GetAdditionalOptionsForVelocityCall();
                if (additionalOptions != null)
                {
                    foreach (string key in additionalOptions.Keys)
                    {
                        string sAdditionalOptions = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(additionalOptions[key]);
                        sb.Append($@"options.{INTERNAL_HtmlDomManager.EscapeStringForUseInJavaScript(key)} = {sAdditionalOptions};");                    }
                }
            }

            sb.AppendLine($"document.velocityHelpers.animate(el, {jsFromToValues}, options, '{visualStateGroupName}');")
              .Append($"}})({sElement});");
            OpenSilver.Interop.ExecuteJavaScriptFastAsync(sb.ToString());
        }

        internal static void StopVelocity(string domElement, string visualStateGroupName)
        {
            OpenSilver.Interop.ExecuteJavaScriptFastAsync($@"Velocity({domElement}, ""stop"", ""{visualStateGroupName}"");");
        }

        internal static void ApplyValue(DependencyObject target, PropertyPath propertyPath, object value, bool isVisualStateChange)
        {
            if (isVisualStateChange)
            {
                propertyPath.INTERNAL_PropertySetVisualState(target, value);
            }
            else
            {
                propertyPath.INTERNAL_PropertySetAnimationValue(target, value);
            }
        }

        //Note: this method is needed because JSIL doesn't know that a nullable whose value is null is equal to null. (Color? v = null; if(v == null) ...)
        internal static bool IsValueNull(object from)
        {
            return from == null || CheckIfObjectIsNullNullable(from);
        }

        //Note: CheckIfObjectIsNullNullable and CheckIfNullableIsNotNull below come from DataContractSerializer_Helpers.cs
        internal static bool CheckIfObjectIsNullNullable(object obj)
        {
            Type type = obj.GetType();
            if (type.FullName.StartsWith("System.Nullable`1"))
            {
                //I guess we'll have to use reflection here
                return !CheckIfNullableIsNotNull(obj);
            }
            else
            {
                return false;
            }
        }

#if !BRIDGE
        [JSIL.Meta.JSReplacement("$obj.hasValue")]
#else
        [Template("{obj}.hasValue")]
#endif
        internal static bool CheckIfNullableIsNotNull(object obj)
        {
            return (obj != null);
        }
    }
}
