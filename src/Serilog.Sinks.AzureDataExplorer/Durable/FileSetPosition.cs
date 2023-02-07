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
