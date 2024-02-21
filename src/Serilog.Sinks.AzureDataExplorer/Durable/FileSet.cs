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

using System.Text.RegularExpressions;
using Serilog.Debugging;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/Durable/FileSet.cs
    /// class is a utility class that provides operations related to managing a set of files in a directory.
    /// It holds information about a specific set of files, including a bookmark file, log folder, and candidate search path.
    /// It also includes regular expression for matching the filenames of the files in the set.
    /// The class provides methods for opening the bookmark file, getting the buffer files, cleaning up buffer files and invalid payload files, making invalid payload file names, and deleting files exceeding a cumulative size.
    /// </summary>
    class FileSet
    {
        readonly string m_bookmarkFilename;
        readonly string m_candidateSearchPath;
        readonly string m_logFolder;
        readonly Regex m_filenameMatcher;

        const string InvalidPayloadFilePrefix = "invalid-";

        public FileSet(string bufferBaseFilename, RollingInterval rollingInterval)
        {
            if (bufferBaseFilename == null) throw new ArgumentNullException(nameof(bufferBaseFilename));

            m_bookmarkFilename = Path.GetFullPath(bufferBaseFilename + ".bookmark");
            m_logFolder = Path.GetDirectoryName(m_bookmarkFilename);
            m_candidateSearchPath = Path.GetFileName(bufferBaseFilename) + "-*.clef";
            var dateRegularExpressionPart = rollingInterval.GetMatchingDateRegularExpressionPart();
            m_filenameMatcher = new Regex("^" + Regex.Escape(Path.GetFileName(bufferBaseFilename)) + "-(?<date>"
                                         + dateRegularExpressionPart + ")(?<sequence>_[0-9]{3,}){0,1}\\.clef");
        }

        public BookmarkFile OpenBookmarkFile()
        {
            return new BookmarkFile(m_bookmarkFilename);
        }

        public string[] GetBufferFiles()
        {
            return Directory.GetFiles(m_logFolder, m_candidateSearchPath)
                .Select(n => new KeyValuePair<string, Match>(n, m_filenameMatcher.Match(Path.GetFileName(n))))
                .Where(nm => nm.Value.Success)
                .OrderBy(nm => nm.Value.Groups["date"].Value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(nm => int.Parse("0" + nm.Value.Groups["sequence"].Value.Replace("_", "")))
                .Select(nm => nm.Key)
                .ToArray();
        }

        public void CleanUpBufferFiles(long bufferSizeLimitBytes, int alwaysRetainCount)
        {
            try
            {
                var bufferFiles = GetBufferFiles();
                Array.Reverse(bufferFiles);
                DeleteExceedingCumulativeSize(bufferFiles.Select(f => new FileInfo(f)), bufferSizeLimitBytes, 2);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Exception thrown while cleaning up buffer files: {0}", ex);
            }
        }

        public string MakeInvalidPayloadFilename(int statusCode)
        {
            var invalidPayloadFilename = $"{InvalidPayloadFilePrefix}{statusCode}-{Guid.NewGuid():n}.json";
            return Path.Combine(m_logFolder, invalidPayloadFilename);
        }

        public void CleanUpInvalidPayloadFiles(long maxNumberOfBytesToRetain)
        {
            try
            {
                var candidateFiles = from file in Directory.EnumerateFiles(m_logFolder, $"{InvalidPayloadFilePrefix}*.json")
                                     let candiateFileInfo = new FileInfo(file)
                                     orderby candiateFileInfo.LastWriteTimeUtc descending
                                     select candiateFileInfo;

                DeleteExceedingCumulativeSize(candidateFiles, maxNumberOfBytesToRetain, 0);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Exception thrown while cleaning up invalid payload files: {0}", ex);
            }
        }

        static void DeleteExceedingCumulativeSize(IEnumerable<FileInfo> files, long maxNumberOfBytesToRetain, int alwaysRetainCount)
        {
            long cumulative = 0;
            var i = 0;
            foreach (var file in files)
            {
                cumulative += file.Length;

                if (i++ < alwaysRetainCount)
                    continue;

                if (cumulative <= maxNumberOfBytesToRetain)
                    continue;

                try
                {
                    file.Delete();
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Exception thrown while trying to delete file {0}: {1}", file.FullName, ex);
                }
            }
        }
    }
}
