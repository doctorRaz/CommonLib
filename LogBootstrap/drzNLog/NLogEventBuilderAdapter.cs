using dRz.Abstractions.Logger;
using System;
using System.Collections.Generic;

namespace dRz.LogBootstrap.drzNLog
{
    internal sealed class NLogEventBuilderAdapter : ILogEventBuilder
    {
        public ILogEventBuilder Exception(Exception exception)
        {
            _ = _inner.Exception(exception);
            return this;
        }

        public void Log() => _inner.Log();

        public ILogEventBuilder Message(string message)
        {
            _ = _inner.Message(message);
            return this;
        }

        public ILogEventBuilder Property(string name, object value)
        {
            _ = _inner.Property(name, value);
            return this;
        }

        public ILogEventBuilder Properties(IEnumerable<KeyValuePair<string, object>> properties)
        {
            if (properties == null)
            {
                return this;
            }

            foreach (KeyValuePair<string, object> kvp in properties)
            {
                _ = _inner.Property(kvp.Key, kvp.Value);
            }
            return this;
        }

        internal NLogEventBuilderAdapter(NLog.LogEventBuilder inner)
        {
            _inner = inner;
        }

        private readonly NLog.LogEventBuilder _inner;
    }
}