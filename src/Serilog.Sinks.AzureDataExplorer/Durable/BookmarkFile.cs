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

using System.Text;

namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/Durable/BookmarkFile.cs
    /// this class is a file-based bookmark mechanism.
    /// It provides a way to persist a bookmark across multiple executions of an application.
    /// The bookmark is stored in a file and is represented as a FileSetPosition object, which contains the starting position of the next line in a file and the file name.
    /// The class provides methods to read and write the bookmark to the file, and implements the IDisposable interface to clean up any resources it uses.
    /// </summary>
    sealed class BookmarkFile : IDisposable
    {
        readonly FileStream m_bookmark;

        public BookmarkFile(string bookmarkFilename)
        {
            m_bookmark = System.IO.File.Open(bookmarkFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        }

        public FileSetPosition TryReadBookmark()
        {
            if (m_bookmark.Length != 0)
            {
                m_bookmark.Position = 0;

                // Important not to dispose this StreamReader as the stream must remain open.
                var reader = new StreamReader(m_bookmark, Encoding.UTF8, false, 128);
                var current = reader.ReadLine();

                if (current != null)
                {
                    var parts = current.Split(new[] { ":::" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        return new FileSetPosition(long.Parse(parts[0]), parts[1]);
                    }
                }
            }

            return FileSetPosition.None;
        }

        public void WriteBookmark(FileSetPosition bookmark)
        {
            if (bookmark.File == null)
                return;

            // Don't need to truncate, since we only ever read a single line and
            // writes are always newline-terminated
            m_bookmark.Position = 0;

            // Cannot dispose, as `leaveOpen` is not available on all target platforms
            var writer = new StreamWriter(m_bookmark);
            writer.WriteLine("{0}:::{1}", bookmark.NextLineStart, bookmark.File);
            writer.Flush();
        }

        public void Dispose()
        {
            m_bookmark.Dispose();
        }
    }
}
