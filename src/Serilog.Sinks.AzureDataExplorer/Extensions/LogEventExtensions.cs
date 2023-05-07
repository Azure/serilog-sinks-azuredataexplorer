﻿using Serilog.Events;

namespace Serilog.Sinks.AzureDataExplorer.Extensions
{
    //This class is a static utility class that provides extensions for logging events.
    //It includes methods to convert LogEvent and IReadOnlyDictionary<string, LogEventPropertyValue> objects into JSON and IDictionary<string, object> formats, allowing for easier serialization and manipulation of log event data.
    //The private implementation section includes methods for converting the data and simplifying the structure of the resulting dictionary.
    internal static class LogEventExtensions
    {
        private const string SourceContextPropertyName = "SourceContext";

        internal static string Json(this LogEvent logEvent, IFormatProvider formatProvider = null)
        {
            return System.Text.Json.JsonSerializer.Serialize(ConvertToDictionary(logEvent, formatProvider));
        }

        internal static IDictionary<string, object> Dictionary(
            this LogEvent logEvent,
            IFormatProvider formatProvider = null)
        {
            return ConvertToDictionary(logEvent, formatProvider);
        }

        internal static string Json(this IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            return System.Text.Json.JsonSerializer.Serialize(ConvertToDictionary(properties));
        }

        internal static IDictionary<string, object> Dictionary(this IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            return ConvertToDictionary(properties);
        }

        /// <summary>
        /// Get the table name based on table name mappings or return the default table name
        /// </summary>
        /// <param name="logEvent">The log event to evaluate against</param>
        /// <param name="tableNameMappings">The table name mappings</param>
        /// <param name="defaultTableName">The default table name</param>
        /// <returns></returns>
        internal static string GetTableName(this LogEvent logEvent, IReadOnlyDictionary<string, string> tableNameMappings, string defaultTableName)
        {
            if (tableNameMappings == null || tableNameMappings.Count == 0)
            {
                return defaultTableName;
            }

            if (logEvent.Properties == null || logEvent.Properties.Count == 0)
            {
                return defaultTableName;
            }

            if (logEvent.Properties.ContainsKey(SourceContextPropertyName))
            {
                var sourceContextPropertyValue = logEvent.Properties[SourceContextPropertyName];
                if (sourceContextPropertyValue != null && sourceContextPropertyValue is ScalarValue scalarValue && scalarValue.Value != null)
                {
                    var sourceContext = scalarValue.Value.ToString();
                    if (!string.IsNullOrWhiteSpace(sourceContext) && tableNameMappings.ContainsKey(sourceContext))
                    {
                        return tableNameMappings[sourceContext];
                    }
                }
            }

            return defaultTableName;
        }

        #region Private implementation

        private static IDictionary<string, object> ConvertToDictionary(IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            var expObject = new Dictionary<string, object>(properties.Count);
            foreach (var property in properties)
            {
                expObject.Add(property.Key, Simplify(property.Value));
            }

            return expObject;
        }

        private static Dictionary<string, object> ConvertToDictionary(
            LogEvent logEvent,
            IFormatProvider formatProvider = null)
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

        private static object Simplify(LogEventPropertyValue data)
        {
            if (data is ScalarValue value)
            {
                return value.Value;
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (data is DictionaryValue dictValue)
            {
                var expObject = new Dictionary<string, object>(dictValue.Elements.Count);
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

                    return new Dictionary<string, object>(1)
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
