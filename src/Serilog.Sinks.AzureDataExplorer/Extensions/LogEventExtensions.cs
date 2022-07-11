using Serilog.Events;

namespace Serilog.Sinks.Azuredataexplorer.Extensions
{
    internal static class LogEventExtensions
    {
        internal static string Json(this LogEvent logEvent, IFormatProvider? formatProvider = null)
        {
            return System.Text.Json.JsonSerializer.Serialize(ConvertToDictionary(logEvent, formatProvider));
        }

        internal static IDictionary<string, object> Dictionary(
            this LogEvent logEvent,
            IFormatProvider? formatProvider = null)
        {
            return ConvertToDictionary(logEvent, formatProvider);
        }

        internal static string Json(this IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            return System.Text.Json.JsonSerializer.Serialize(ConvertToDictionary(properties));
        }

        internal static IDictionary<string, object?> Dictionary(this IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            return ConvertToDictionary(properties);
        }

        #region Private implementation

        private static IDictionary<string, object?> ConvertToDictionary(IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            var expObject = new Dictionary<string, object?>(properties.Count);
            foreach (var property in properties)
            {
                expObject.Add(property.Key, Simplify(property.Value));
            }

            return expObject;
        }

        private static Dictionary<string, object> ConvertToDictionary(
            LogEvent logEvent,
            IFormatProvider? formatProvider = null)
        {
            var eventObject = new Dictionary<string, object>(5);

            eventObject.Add("Timestamp", logEvent.Timestamp.ToUniversalTime().ToString("o"));

            // TODO: Avoid calling ToString on enums
            eventObject.Add("Level", logEvent.Level.ToString());
            eventObject.Add("Message", logEvent.RenderMessage(formatProvider));
            eventObject.Add("Exception", logEvent.Exception);
            eventObject.Add("Properties", logEvent.Properties.Dictionary());

            return eventObject;
        }

        private static object? Simplify(LogEventPropertyValue data)
        {
            if (data is ScalarValue value)
            {
                return value.Value;
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (data is DictionaryValue dictValue)
            {
                var expObject = new Dictionary<string, object?>(dictValue.Elements.Count);
                foreach (var item in dictValue.Elements)
                {
                    if (item.Key.Value is string key)
                    {
                        expObject.Add(key, Simplify(item.Value));
                    }
                }

                return expObject;
            }

            if (data is SequenceValue seq)
            {
                return seq.Elements.Select(Simplify).ToArray();
            }

            if (data is not StructureValue str)
            {
                return null;
            }

            {
                try
                {
                    if (str.TypeTag == null)
                    {
                        return str.Properties.ToDictionary(p => p.Name, p => Simplify(p.Value));
                    }

                    if (!str.TypeTag.StartsWith("DictionaryEntry") && !str.TypeTag.StartsWith("KeyValuePair"))
                    {
                        return str.Properties.ToDictionary(p => p.Name, p => Simplify(p.Value));
                    }

                    var key = Simplify(str.Properties[0].Value);

                    if (key == null)
                    {
                        return null;
                    }

                    return new Dictionary<string, object?>(1)
                    {
                        { key!.ToString()!, Simplify(str.Properties[1].Value) }
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return null;
        }

        #endregion
    }
}