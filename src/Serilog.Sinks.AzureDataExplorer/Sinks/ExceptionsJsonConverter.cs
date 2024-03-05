// Copyright 2014 Serilog Contributors
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

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("Serilog.Sinks.AzureDataExplorer.Tests,PublicKey=" + "002400000480000094000000060200000024000052534131000400000100010025d2229d740f195c0a4cdcb468a4ed69c33a9f2738727a6c34a80ab8b75263a33bd5ac958f0e8b82658a7ee429cc4536166a7ac908691c600a84b20a67db8f5324f43a168a93665f6b449588d2168d6189a27f41bf7b95e6cd1f184bf6f9f9020429972e3132f34f60777ff25edd96d0527d88d2adb4dffa4ed31016aa6cc5b0")]

namespace Serilog.Sinks.AzureDataExplorer.Sinks
{

    internal sealed class ExceptionsJsonConverter<TExceptionType> : JsonConverter<TExceptionType> where TExceptionType : Exception
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Exception).IsAssignableFrom(typeToConvert);
        }

        public override TExceptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Deserializing exceptions is not allowed");
        }

        public override void Write(Utf8JsonWriter writer, TExceptionType value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Error", value.Message);
            writer.WriteString("Type", value.GetType().Name);
            if (value.InnerException is { } innerException)
            {
                writer.WritePropertyName("InnerException");
                Write(writer, (TExceptionType)innerException, options);
            }
            writer.WriteEndObject();
        }
    }
}