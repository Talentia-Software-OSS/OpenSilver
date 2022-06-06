

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


//#define CHECK_THAT_ID_EXISTS 
//#define PERFORMANCE_ANALYSIS

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using OpenSilver.Internal;

namespace CSHTML5.Internal
{
    // Note: this class is intented to be used by the Simulator only, not when compiled to JavaScript.
#if BRIDGE
    [Bridge.External] //we exclude this class
#endif

#if CSHTML5NETSTANDARD
    public class INTERNAL_Html2dContextReference : DynamicObject
#else
    internal class INTERNAL_Html2dContextReference : DynamicObject
#endif
    {
        static Dictionary<string, INTERNAL_Html2dContextReference> IdToInstance = new Dictionary<string, INTERNAL_Html2dContextReference>();

        public static INTERNAL_Html2dContextReference GetInstance(string elementId)
        {
            if (IdToInstance.ContainsKey(elementId))
                return IdToInstance[elementId];
            else
            {
                var newInstance = new INTERNAL_Html2dContextReference(elementId);
                IdToInstance[elementId] = newInstance;
                return newInstance;
            }
        }

        string _domElementUniqueIdentifier;

        // Note: It's important that the constructor stays Private because we need to recycle the instances that correspond to the same ID using the "GetInstance" public static method, so that each ID always corresponds to the same instance. This is useful to ensure that private fields such as "_display" work propertly.
        private INTERNAL_Html2dContextReference(string elementId)
        {
            _domElementUniqueIdentifier = elementId;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            string methodName = binder.Name;
            result = InvokeMethod(methodName, args);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            string propertyName = binder.Name;
            string propertyValue = (string)value;
            SetPropertyValue(propertyName, propertyValue);
            return true;
        }

        void SetPropertyValue(string propertyName, string propertyValue)
        {
            string javaScriptCodeToExecute = "document.set2dContextProperty(\"" + _domElementUniqueIdentifier + "\",\"" + propertyName + "\",\"" + propertyValue + "\")";
            //INTERNAL_SimulatorPerformanceOptimizer.QueueJavaScriptCode(javaScriptCodeToExecute);
            INTERNAL_SimulatorExecuteJavaScript.ExecuteJavaScriptAsync(javaScriptCodeToExecute);
        }

        object InvokeMethod(string methodName, object[] args)
        {
            string argsString = string.Join(", ", args.Select(x => INTERNAL_HtmlDomManager.ConvertToStringToUseInJavaScriptCode(x)));
            return InvokeMethodImpl(methodName, argsString);
        }

        void InvokeMethodVoid(string methodName, object[] args)
        {
            string argsString = string.Join(", ", args.Select(x => INTERNAL_HtmlDomManager.ConvertToStringToUseInJavaScriptCode(x)));
            InvokeMethodImplVoid(methodName, argsString);
        }

        object InvokeMethod(string methodName)
        {
            return InvokeMethodImpl(methodName, string.Empty);
        }

        void InvokeMethodVoid(string methodName)
        {
            InvokeMethodImplVoid(methodName, string.Empty);
        }

        object InvokeMethod(string methodName, double arg0)
        {
            string args = $"{arg0.ToInvariantString()}";
            return InvokeMethodImpl(methodName, args);
        }

        void InvokeMethodVoid(string methodName, double arg0)
        {
            string args = $"{arg0.ToInvariantString()}";
            InvokeMethodImplVoid(methodName, args);
        }

        object InvokeMethod(string methodName, double arg0, double arg1)
        {
            string args = $"{arg0.ToInvariantString()}, {arg1.ToInvariantString()}";
            return InvokeMethodImpl(methodName, args);
        }

        void InvokeMethodVoid(string methodName, double arg0, double arg1)
        {
            string args = $"{arg0.ToInvariantString()}, {arg1.ToInvariantString()}";
            InvokeMethodImplVoid(methodName, args);
        }

        object InvokeMethod(string methodName, double arg0, double arg1, double arg2, double arg3)
        {
            string args = $"{arg0.ToInvariantString()}, {arg1.ToInvariantString()}, {arg2.ToInvariantString()}, {arg3.ToInvariantString()}";
            return InvokeMethodImpl(methodName, args);
        }

        void InvokeMethodVoid(string methodName, double arg0, double arg1, double arg2, double arg3)
        {
            string args = $"{arg0.ToInvariantString()}, {arg1.ToInvariantString()}, {arg2.ToInvariantString()}, {arg3.ToInvariantString()}";
            InvokeMethodImplVoid(methodName, args);
        }

        object InvokeMethod(string methodName, double arg0, double arg1, double arg2, double arg3, double arg4, double arg5)
        {
            string args = $"{arg0.ToInvariantString()}, {arg1.ToInvariantString()}, {arg2.ToInvariantString()}, {arg3.ToInvariantString()}, {arg4.ToInvariantString()}, {arg5.ToInvariantString()}";
            return InvokeMethodImpl(methodName, args);
        }

        void InvokeMethodVoid(string methodName, double arg0, double arg1, double arg2, double arg3, double arg4, double arg5)
        {
            string args = $"{arg0.ToInvariantString()}, {arg1.ToInvariantString()}, {arg2.ToInvariantString()}, {arg3.ToInvariantString()}, {arg4.ToInvariantString()}, {arg5.ToInvariantString()}";
            InvokeMethodImplVoid(methodName, args);
        }

        object InvokeMethod(string methodName, double[] args)
        {
            string argsString = string.Join(", ", args.Select(x => x.ToInvariantString()));
            return InvokeMethodImpl(methodName, argsString);
        }

        void InvokeMethodVoid(string methodName, double[] args)
        {
            string argsString = string.Join(", ", args.Select(x => x.ToInvariantString()));
            InvokeMethodImplVoid(methodName, argsString);
        }

        object InvokeMethodImpl(string methodName, string args)
        {
            return OpenSilver.Interop.ExecuteJavaScriptAsync($"document.invoke2dContextMethod(\"{_domElementUniqueIdentifier}\", \"{methodName}\", \"{args}\")");
        }

        void InvokeMethodImplVoid(string methodName, string args)
        {
            OpenSilver.Interop.ExecuteJavaScriptFastAsync($"document.invoke2dContextMethod(\"{_domElementUniqueIdentifier}\", \"{methodName}\", \"{args}\")");
        }

        public string fillStyle { set { SetPropertyValue("fillStyle", value); } }
        public string strokeStyle { set { SetPropertyValue("strokeStyle", value); } }
        public string lineWidth { set { SetPropertyValue("lineWidth", value); } }
        public string lineDashOffset { set { SetPropertyValue("lineDashOffset", value); } }


        public void transform(double m11, double m12, double m21, double m22, double dx, double dy) { InvokeMethodVoid("transform", m11, m12, m21, m22, dx, dy); }
        public void translate(double x, double y) { InvokeMethodVoid("translate", x, y); }
        public void rotate(double angle) { InvokeMethodVoid("rotate", angle); }
        public void scale(double x, double y) { InvokeMethodVoid("scale", x, y); }

        public void save() { InvokeMethodVoid("save"); }
        public void restore() { InvokeMethodVoid("restore"); }

        public void fill(params object[] args) { InvokeMethodVoid("fill", args); }
        public void stroke(params object[] args) { InvokeMethodVoid("stroke", args); }
        public void setLineDash(params double[] args) { InvokeMethodVoid("setLineDash", args); }

        public void beginPath() { InvokeMethodVoid("beginPath"); }
        public void closePath() { InvokeMethodVoid("closePath"); }
        public void createLinearGradient(double x0, double y0, double x1, double y1) { InvokeMethodVoid("createLinearGradient", x0, y0, x1, y1); }

        public void arc(params object[] args) { InvokeMethodVoid("arc", args); }
        public void ellipse(params object[] args) { InvokeMethodVoid("ellipse", args); }
        public void rect(double x, double y, double width, double height) { InvokeMethodVoid("rect", x, y, width, height); }

        public void moveTo(double x, double y) { InvokeMethodVoid("moveTo", x, y); }
        public void lineTo(double x, double y) { InvokeMethodVoid("lineTo", x, y); }
        public void bezierCurveTo(double cp1x, double cp1y, double cp2x, double cp2y, double x, double y) { InvokeMethodVoid("bezierCurveTo", cp1x, cp1y, cp2x, cp2y, x, y); }
        public void quadraticCurveTo(double cpx, double cpy, double x, double y) { InvokeMethodVoid("quadraticCurveTo", cpx, cpy, x, y); }
    }
}
