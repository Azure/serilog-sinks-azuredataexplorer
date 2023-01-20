namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/Durable/FileSetPosition.cs
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
