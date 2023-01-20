using System.Text;

namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/Durable/BookmarkFile.cs
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
