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

namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/Durable/FileSetPosition.cs
    /// The FileSetPosition is a value type (struct) that contains information about the position of a file in a set of files.
    /// The struct has two properties: File and NextLineStart. The File property is a string that contains the name of the file.
    /// The NextLineStart property is a long that indicates the starting position of the next line to be read from the file.
    /// The struct has a constructor that takes two arguments: nextLineStart and file, which are used to initialize the two properties of the struct.
    /// There is also a static field None which is set to the default value of the struct.
    /// </summary>
    public struct FileSetPosition
    {
        public string File { get; }

        public long NextLineStart { get; }

        public FileSetPosition(long nextLineStart, string file)
        {
            NextLineStart = nextLineStart;
            File = file;
        }

        public static readonly FileSetPosition None = default(FileSetPosition);
    }
}
