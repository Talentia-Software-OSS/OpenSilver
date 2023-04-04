
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

using DotNetForHtml5;
using DotNetForHtml5.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CSHTML5.Internal
{
    public enum PendingJavascriptImplementationType
    {
        PendingJavascriptJoin,
        PendingJavascriptStringBuilder,
        PendingJavascriptBuffer,
        PendingJavascriptSpan,
        PendingJavascriptHeap
    }

    internal interface IPendingJavascript
    {
        void AddJavaScript(string javascript);

        object ExecuteJavaScript(string javascript, bool flush);
    }

    internal sealed class PendingJavascriptJoin : IPendingJavascript
    {
        private readonly List<string> _pending = new List<string>();
        private readonly IWebAssemblyExecutionHandler _webAssemblyExecutionHandler;

        public PendingJavascriptJoin(IWebAssemblyExecutionHandler webAssemblyExecutionHandler)
        {
            _webAssemblyExecutionHandler = webAssemblyExecutionHandler ?? throw new ArgumentNullException(nameof(webAssemblyExecutionHandler));
        }

        public void AddJavaScript(string javascript)
        {
            if (javascript == null) return;

            lock (_pending)
            {
                _pending.Add(javascript);
            }
        }

        public object ExecuteJavaScript(string javascript, bool flush)
        {

            if (flush)
            {
                string aggregatedPendingJavaScriptCode = ReadAndClearAggregatedPendingJavaScriptCode();

                if (!string.IsNullOrWhiteSpace(aggregatedPendingJavaScriptCode))
                {
                    javascript = string.Join(Environment.NewLine, new List<string>
                    {
                        "// [START OF PENDING JAVASCRIPT]",
                        aggregatedPendingJavaScriptCode,
                        "// [END OF PENDING JAVASCRIPT]" + Environment.NewLine,
                        javascript
                    });
                }
            }

            return _webAssemblyExecutionHandler.ExecuteJavaScriptWithResult(javascript);
        }



        internal string ReadAndClearAggregatedPendingJavaScriptCode()
        {
            lock (_pending)
            {
                if (_pending.Count == 0)
                    return null;

                _pending.Add(string.Empty);
                string aggregatedPendingJavaScriptCode = string.Join(";\n", _pending);
                _pending.Clear();
                return aggregatedPendingJavaScriptCode;
            }
        }
    }

    internal sealed class PendingJavascriptStringBuilder : IPendingJavascript
    {
        private readonly StringBuilder _javascriptBuilder;
        private readonly int _maxBufferSize;
        private readonly IJavaScriptExecutionHandler _executionHandler;

        public PendingJavascriptStringBuilder(int maxBufferSize, IJavaScriptExecutionHandler executionHandler)
        {
            _maxBufferSize = maxBufferSize;
            _executionHandler = executionHandler;
            _javascriptBuilder = new StringBuilder(maxBufferSize, maxBufferSize);
        }

        public void AddJavaScript(string javascript)
        {
            if (string.IsNullOrEmpty(javascript))
            {
                return;
            }

            lock (_javascriptBuilder)
            {
                if (_javascriptBuilder.Capacity - _javascriptBuilder.Length < javascript.Length + 2)
                {
                    ExecuteQueuedJavaScript();
                }

                _javascriptBuilder.Append(javascript);
                _javascriptBuilder.Append(";\n");
            }
        }

        public object ExecuteJavaScript(string javascript, bool flush)
        {
            if (flush)
            {
                if (!string.IsNullOrEmpty(javascript))
                {
                    AddJavaScript(javascript);
                    return ExecuteQueuedJavaScriptWithResult();
                }

                ExecuteQueuedJavaScript();
                return null;
            }

            return _executionHandler.ExecuteJavaScriptWithResult(javascript);
        }

        private void ExecuteQueuedJavaScript()
        {
            string javascript = null;

            lock (_javascriptBuilder)
            {
                if (_javascriptBuilder.Length == 0)
                {
                    return;
                }

                javascript = _javascriptBuilder.ToString();
                _javascriptBuilder.Clear();
            }

            _executionHandler.ExecuteJavaScript(javascript);
        }

        private object ExecuteQueuedJavaScriptWithResult()
        {
            string javascript = null;

            lock (_javascriptBuilder)
            {
                if (_javascriptBuilder.Length == 0)
                {
                    return null;
                }

                javascript = _javascriptBuilder.ToString();
                _javascriptBuilder.Clear();
            }

            return _executionHandler.ExecuteJavaScriptWithResult(javascript);
        }
    }

    internal class PendingJavascriptBuffer : IPendingJavascript
    {
        private readonly object _lock = new object();
        private readonly char[] _buffer;
        private int _bufferIndex;
        private readonly IJavaScriptExecutionHandler _executionHandler;

        public PendingJavascriptBuffer(int bufferSize, IJavaScriptExecutionHandler executionHandler)
        {
            _executionHandler = executionHandler;
            _buffer = new char[bufferSize];
            _bufferIndex = 0;
        }

        private void AppendJavaScript(string javascript)
        {
            if (string.IsNullOrEmpty(javascript))
            {
                return;
            }

            // Check if the JavaScript is too large for the buffer
            if (javascript.Length + 2 > _buffer.Length)
            {
                // The JavaScript is too large for the buffer, so execute it separately
                _executionHandler.ExecuteJavaScript(javascript);
                return;
            }

            lock (_lock)
            {
                if (_buffer.Length - _bufferIndex < javascript.Length + 2)
                {
                    ExecuteQueuedJavaScript();
                }

                javascript.CopyTo(0, _buffer, _bufferIndex, javascript.Length);
                _bufferIndex += javascript.Length;
                _buffer[_bufferIndex++] = ';';
                _buffer[_bufferIndex++] = '\n';
            }
        }

        public void AddJavaScript(string javascript)
        {
            AppendJavaScript(javascript);

            if (_bufferIndex >= _buffer.Length)
            {
                ExecuteQueuedJavaScript();
            }
        }

        public object ExecuteJavaScript(string javascript, bool flush)
        {
            lock (_lock)
            {
                if (flush)
                {
                    if (!string.IsNullOrEmpty(javascript))
                    {
                        AppendJavaScript(javascript);
                        return ExecuteQueuedJavaScriptWithResult();
                    }

                    ExecuteQueuedJavaScript();
                    return null;
                }

                return _executionHandler.ExecuteJavaScriptWithResult(javascript);
            }
        }

        private void ExecuteQueuedJavaScript()
        {
            if (_bufferIndex == 0)
            {
                return;
            }

            var javascript = new string(_buffer, 0, _bufferIndex);
            _executionHandler.ExecuteJavaScript(javascript);

            _bufferIndex = 0;
        }

        private object ExecuteQueuedJavaScriptWithResult()
        {
            if (_bufferIndex == 0)
            {
                return null;
            }

            var javascript = new string(_buffer, 0, _bufferIndex);
            var result = _executionHandler.ExecuteJavaScriptWithResult(javascript);

            _bufferIndex = 0;

            return result;
        }
    }

    internal class PendingJavascriptSpan : IPendingJavascript
    {
        private char[] _buffer;
        private int _length;
        private readonly int _maxSize;
        private readonly IJavaScriptExecutionHandler _executionHandler;

        public PendingJavascriptSpan(int maxSize, IJavaScriptExecutionHandler executionHandler)
        {
            _maxSize = maxSize;
            _buffer = new char[maxSize];
            _length = 0;
            _executionHandler = executionHandler;
        }

        public void AddJavaScript(string javascript)
        {
            if (string.IsNullOrEmpty(javascript))
            {
                return;
            }

            // Check if the JavaScript is too large for the buffer
            if (javascript.Length + 2 > _maxSize)
            {
                // The JavaScript is too large for the buffer, so execute it separately
                _executionHandler.ExecuteJavaScript(javascript);
                return;
            }

            // Append the new JavaScript to the buffer
            var requiredCapacity = _length + javascript.Length + 2; // Add 2 for the semicolon and newline characters
            if (requiredCapacity > _maxSize)
            {
                // The JavaScript is too large for the buffer, so execute the current buffer and the new JavaScript separately
                ExecuteQueuedJavaScript();
            }

            // The JavaScript fits in the buffer, so append it to the buffer
            var jsSpan = javascript.AsSpan();
            jsSpan.CopyTo(_buffer.AsSpan(_length));
            _length += jsSpan.Length;
            _buffer[_length++] = ';';
            _buffer[_length++] = '\n';
        }

        public object ExecuteJavaScript(string javascript, bool flush)
        {
            if (flush)
            {
                lock (_buffer)
                {
                    AddJavaScript(javascript);
                    return ExecuteQueuedJavaScriptWithResult();
                }
            }

            return _executionHandler.ExecuteJavaScriptWithResult(javascript);
        }

        private void ExecuteQueuedJavaScript()
        {
            if (_length == 0)
            {
                return;
            }

            var javascript = new ReadOnlySpan<char>(_buffer, 0, _length);
            _executionHandler.ExecuteJavaScript(javascript.ToString());

            // Reset the buffer length to zero
            _length = 0;
        }

        private object ExecuteQueuedJavaScriptWithResult()
        {
            if (_length == 0)
            {
                return null;
            }

            var javascript = new ReadOnlySpan<char>(_buffer, 0, _length);
            var result = _executionHandler.ExecuteJavaScriptWithResult(javascript.ToString());

            // Reset the buffer length to zero
            _length = 0;

            return result;
        }
    }

    internal sealed class PendingJavascriptHeap : IPendingJavascript
    {
        private const string CallJSMethodName = "callJSUnmarshalledHeap";

        private static readonly Encoding DefaultEncoding = Encoding.UTF8;
        private static readonly byte[] Delimiter = DefaultEncoding.GetBytes(";\n");
        private readonly object _syncObj = new object();
        private readonly IWebAssemblyExecutionHandler _webAssemblyExecutionHandler;
        private byte[] _buffer;
        private int _currentLength;

        public PendingJavascriptHeap(int bufferSize, IWebAssemblyExecutionHandler webAssemblyExecutionHandler)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentException("Buffer size can not be less or equal to 0");
            }

            _webAssemblyExecutionHandler = webAssemblyExecutionHandler ?? throw new ArgumentNullException(nameof(webAssemblyExecutionHandler));
            _buffer = new byte[bufferSize];
        }

        public void AddJavaScript(string javascript)
        {
            if (javascript == null) return;

            lock (_syncObj)
            {
                var maxByteCount = DefaultEncoding.GetMaxByteCount(javascript.Length);
                if (maxByteCount + _currentLength + Delimiter.Length > _buffer.Length)
                {
                    // Flush the buffer and start over
                    ExecuteJavaScript(null, true);
                }

                _currentLength += DefaultEncoding.GetBytes(javascript, 0, javascript.Length, _buffer, _currentLength);

                Buffer.BlockCopy(Delimiter, 0, _buffer, _currentLength, Delimiter.Length);
                _currentLength += Delimiter.Length;
            }
        }

        public object ExecuteJavaScript(string javascript, bool flush)
        {
            if (!flush)
            {
                return _webAssemblyExecutionHandler.ExecuteJavaScriptWithResult(javascript);
            }

            AddJavaScript(javascript);

            if (_currentLength == 0)
            {
                return null;
            }

            var curLength = _currentLength;
            _currentLength = 0;
            //Here we pass a reference to _buffer object and current length
            //Js will read data from the heap
            unsafe
            {
                fixed (byte* pointerToFirst = _buffer)
                {
                    return _webAssemblyExecutionHandler.InvokeUnmarshalled<byte[], int, object>(
                        CallJSMethodName,
                        _buffer,
                        curLength);
                }
            }
        }
    }

    internal sealed class PendingJavascriptSimulator : IPendingJavascript
    {
        private readonly List<string> _pending = new List<string>();
        private readonly IJavaScriptExecutionHandler _jsExecutionHandler;

        public PendingJavascriptSimulator(IJavaScriptExecutionHandler jsExecutionHandler)
        {
            _jsExecutionHandler = jsExecutionHandler ?? throw new ArgumentNullException(nameof(jsExecutionHandler));
        }

        public void AddJavaScript(string javascript)
        {
            lock (_pending)
            {
                _pending.Add(javascript);
            }
        }

        public object ExecuteJavaScript(string javascript, bool flush)
        {
            if (flush)
            {
                string aggregatedPendingJavaScriptCode = ReadAndClearAggregatedPendingJavaScriptCode();

                if (!string.IsNullOrWhiteSpace(aggregatedPendingJavaScriptCode))
                {
                    javascript = string.Join(Environment.NewLine, new List<string>
                    {
                        "// [START OF PENDING JAVASCRIPT]",
                        aggregatedPendingJavaScriptCode,
                        "// [END OF PENDING JAVASCRIPT]" + Environment.NewLine,
                        javascript
                    });
                }
            }

            return PerformActualInteropCall(javascript);
        }

        private object PerformActualInteropCall(string javaScriptToExecute)
        {
            if (INTERNAL_SimulatorExecuteJavaScript.EnableInteropLogging)
            {
                javaScriptToExecute = "//---- START INTEROP ----"
                    + Environment.NewLine
                    + javaScriptToExecute
                    + Environment.NewLine
                    + "//---- END INTEROP ----";
            }

            try
            {
                if (INTERNAL_SimulatorExecuteJavaScript.EnableInteropLogging)
                {
                    Debug.WriteLine(javaScriptToExecute);
                }
                if (string.IsNullOrEmpty(javaScriptToExecute))
                {
                    return null;
                }

                return _jsExecutionHandler.ExecuteJavaScriptWithResult(javaScriptToExecute);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("Unable to execute the following JavaScript code: " + Environment.NewLine + javaScriptToExecute, ex);
            }
        }

        internal string ReadAndClearAggregatedPendingJavaScriptCode()
        {
            lock (_pending)
            {
                if (_pending.Count == 0)
                    return null;

                _pending.Add(string.Empty);
                string aggregatedPendingJavaScriptCode = string.Join(";\n", _pending);
                _pending.Clear();
                return aggregatedPendingJavaScriptCode;
            }
        }
    }
}