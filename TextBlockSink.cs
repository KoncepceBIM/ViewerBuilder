using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ViewerBuilder
{
    class TextBlockSink : ILogEventSink
    {
        public delegate void UpdateTextCallback(string message);

        public TextBlockSink(UpdateTextCallback callback)
        {
            this.callback = callback;
        }

        readonly ITextFormatter _textFormatter = new MessageTemplateTextFormatter("{Timestamp} [{Level}] {Message}{Exception}", CultureInfo.InvariantCulture);
        private readonly UpdateTextCallback callback;

        public ConcurrentQueue<string> Events { get; } = new ConcurrentQueue<string>();

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            var renderSpace = new StringWriter();
            _textFormatter.Format(logEvent, renderSpace);

            var msg = renderSpace.ToString();
            Events.Enqueue(msg);
            callback?.Invoke(msg);
            
        }
    }
}
