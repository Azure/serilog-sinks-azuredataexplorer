﻿// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Serilog.Events;

namespace Serilog.Sinks.AzureDataExplorer.Extensions
{
    //This class is a static utility class that provides extensions for logging events.
    //It includes methods to convert LogEvent and IReadOnlyDictionary<string, LogEventPropertyValue> objects into JSON and IDictionary<string, object> formats, allowing for easier serialization and manipulation of log event data.
    //The private implementation section includes methods for converting the data and simplifying the structure of the resulting dictionary.
    internal static class LogEventExtensions
    {
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
            if (logEvent.Exception != null)
            {
                var exceptionDetails = new
                {
                    ExceptionStr = logEvent.Exception.ToString(),
                    Type = logEvent.Exception.GetType().FullName,
                    Message = logEvent.Exception.Message,
                    Source = logEvent.Exception.Source,
                    TargetSite = logEvent.Exception.TargetSite?.Name,
                    StackTrace = logEvent.Exception.StackTrace,
                    HelpLink = logEvent.Exception.HelpLink,
                    Data = logEvent.Exception.Data,
                    InnerException = logEvent.Exception.InnerException?.ToString()
                };
                eventObject.Add("ExceptionEx", exceptionDetails);
            }
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
