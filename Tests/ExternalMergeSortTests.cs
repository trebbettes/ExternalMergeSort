using Tsrx;
using Xunit;

namespace Tests
{
    public class ExternalMergeSortTests
    {
        private readonly List<string> _fileNames;
        private readonly string _lines = "abcdefghijklmnopqrstuvwxyz";
        private readonly int _fileCount = 50;

        public ExternalMergeSortTests()
        {
            var dir = $"{Path.GetTempPath()}MergeSortTests";
            
            if (Directory.Exists(dir)) Directory.Delete(dir, true);

            Directory.CreateDirectory(dir);
            
            var files = new List<string>();

            for (var i = 0; i < _fileCount; i++)
            {
                var fileName = $"{dir}\\test{i}.txt";
                var file = File.CreateText(fileName);
                files.Add(fileName);
                //var values = i % 2 == 0 ? lines.Reverse() : lines;
                foreach (var c in _lines) file.WriteLine(c);
                file.Close();
            }

            _fileNames = files;
        }

        [Fact]
        public void AllLinesShouldAppearInResults()
        {
            var ms = new ExternalMergeSort(new ExternalMergeSort.Settings
            {
                FileNames = _fileNames,
            });

            ms.Merge(results =>
            {
                var lines = 0;
                while (results.ReadLine() != null)
                {
                    lines++;
                }
                Assert.Equal(_fileCount * _lines.Length, lines);
            });
        }


        [Fact]
        public void ResultsShouldBeInTheCorrectOrder()
        {
            var ms = new ExternalMergeSort(new ExternalMergeSort.Settings
            {
                FileNames = _fileNames,
            });

            ms.Merge(results =>
            {
                var lastValue = 0;
                while (true)
                {
                    var value = results.ReadLine();
                    if (value is null)
                    {
                        Assert.True(true);
                        return;
                    }
                    if (value[0] < lastValue)
                    {
                        Assert.False(true);
                    }
                    lastValue = value[0];
                }
            });
        }


        [Fact]
        public void FilteredLinesShouldNotBeIncludedInResults()
        {
            var ms = new ExternalMergeSort(new ExternalMergeSort.Settings
            {
                FileNames = _fileNames,
                LineFilter = (line) => !line.Contains('a')
            });

            ms.Merge(results =>
            {
                var lines = 0;
                while (results.ReadLine() != null)
                {
                    lines++;
                }
                Assert.Equal(_fileCount * _lines.Length - _fileCount, lines);
            });
        }

        [Fact]
        public void ProgressHandlerShouldGiveExpectedResults()
        {
            var progress = new List<double>();
            var ms = new ExternalMergeSort(new ExternalMergeSort.Settings
            {
                FileNames = _fileNames,
                ProgressHandler = val => progress.Add(val)
            });

            ms.Merge(results => { });

            Assert.Equal(_fileCount - 1, progress.Count);
            Assert.True(progress.Last() == 1);
        }


        [Fact]
        public void TemporaryFilesShouldBeCreatedInTheCorrectPlace()
        {
            var dir = $"{Directory.GetCurrentDirectory()}/MergeTest";
            var ms = new ExternalMergeSort(new ExternalMergeSort.Settings
            {
                FileNames = _fileNames,
                TempDirectory = dir
            });

            ms.Merge(results =>
            {
                Assert.True(Directory.GetFiles(dir).Length == 1);
            });
        }


        [Fact]
        public void TemporaryFilesShouldBeDeleted()
        {
            var dir = $"{Directory.GetCurrentDirectory()}/MergeTest";
            var ms = new ExternalMergeSort(new ExternalMergeSort.Settings
            {
                FileNames = _fileNames,
                TempDirectory = dir
            });

            ms.Merge(results => { });

            Assert.True(Directory.GetFiles(dir).Length == 0);
        }


        [Fact]
        public void SourceFilesShouldNotBeDelted()
        {
            var ms = new ExternalMergeSort(new ExternalMergeSort.Settings
            {
                FileNames = _fileNames,
            });

            ms.Merge(results => { });

            Assert.True(_fileNames.All(f => File.Exists(f)));
        }
    }
}