namespace Tsrx
{
    /// <summary>
    /// Class to merge & sort multiple text pre-sorted text files (e.g. log files).
    /// Performs the merge/sort on disk in temporary files.
    /// The temporary StreamReader provided in the merge callback is disposed after the call back completes and temporary files are deleted.
    /// </summary>
    public sealed class ExternalMergeSort
    {
        public sealed class Settings
        {
            internal string Id { get; } = Guid.NewGuid().ToString();

            public List<string> FileNames { get; set; } = new List<string>();

            public Func<string, bool>? LineFilter { get; set; } = null;

            public string TempDirectory { get; set; } = $"{Path.GetTempPath()}MergeSort";

            public Action<double>? ProgressHandler { get; set; }

            public IComparer<string> Comparer { get; set; } = StringComparer.InvariantCulture;
        }

        private readonly Settings _settings;
        private readonly double _progressIncrement;

        private double _progress;
        private int _i = 0;

        public ExternalMergeSort(Settings settings)
        {
            _settings = settings;
            _progressIncrement = 1 / ((double)settings.FileNames.Count - 1);
        }

        public void Merge(Action<StreamReader> then) 
        {
            Directory.CreateDirectory(_settings.TempDirectory);

            if (_settings.FileNames.Count == 0) return;

            var result = MergeSort(0, _settings.FileNames.Count - 1);

            then.Invoke(result.GetResults());

            result.Dispose();
        }


        private MergeReader MergeSort(int min, int max)
        {

            if (min == max) return new MergeReader(_settings.FileNames[min], _settings);

            _settings.ProgressHandler?.Invoke(Math.Round(_progress += _progressIncrement, 2));

            var mid = (max - min) / 2 + min;

            var left = MergeSort(min, mid);
            var right = MergeSort(mid + 1, max);

            if (left is null)
            {
                return right;
            }

            if (right is null)
            {
                return left;
            }
            
            var result = Merge(left, right);

            return result;
        }

        private string GetFileName() {
            return $"{_settings.TempDirectory}/{_settings.Id}.{_i++}.tmp";
        }

        private MergeReader Merge(MergeReader left, MergeReader right)
        {
            var tempFileName = GetFileName();
            var tempFile = File.CreateText(tempFileName);

            while (!left.Finished && !right.Finished)
            {
                if (_settings.Comparer.Compare(left.Peek(), right.Peek()) <= 0)
                {
                    tempFile.WriteLine(left.Read());
                    continue;
                }

                tempFile.WriteLine(right.Read());
            }

            if (!left.Finished)
            {
                left.CopyTo(tempFile);
            }

            if (!right.Finished)
            {
                right.CopyTo(tempFile);
            }

            tempFile.Close();
            left.Dispose();
            right.Dispose();

            return new MergeReader(tempFileName, _settings);
        }

        private sealed class MergeReader
        {
            public bool Finished => _currentLine is null;

            private readonly string _file;
            private readonly Settings _settings;
            private readonly StreamReader _stream;

            private string? _currentLine;

            public MergeReader(string file, Settings settings)
            {
                _file = file;
                _stream = File.OpenText(file);
                _settings = settings;
                MoveNext();
            }

            public string? Peek()
            {
                return _currentLine;
            }

            public string? Read()
            {
                var line = _currentLine;
                MoveNext();
                return line;
            }

            public void Dispose()
            {
                _stream.Close();
                if (_file.Contains(_settings.Id)) File.Delete(_file);
            }

            public StreamReader GetResults()
            {

                _stream.DiscardBufferedData();
                _stream.BaseStream.Seek(0, SeekOrigin.Begin);

                return _stream;
            }

            public void CopyTo(StreamWriter stream)
            {
                while (!Finished)
                {
                    stream.WriteLine(_currentLine);
                    MoveNext();
                }
            }

            public StreamReader Complete()
            {
                _stream.Close();
                return File.OpenText(_file);
            }

            private void MoveNext()
            {
                if (_settings.LineFilter is null)
                {
                    _currentLine = _stream.ReadLine();
                    return;
                }

                while (!Finished || !_stream.EndOfStream)
                {
                    var line = _stream.ReadLine();
                    
                    if (line is not null && !_settings.LineFilter.Invoke(line)) continue;
                    
                    _currentLine = line;
                    return;
                }
            }
        }
    }
}
