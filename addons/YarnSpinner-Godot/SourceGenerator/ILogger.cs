/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
#nullable enable
namespace YarnSpinnerGodot
{


    public interface ILogger : IDisposable
    {
        void Write(object obj);
        void WriteLine(object obj);
        void WriteException(System.Exception ex, string? message = null);
        void Inc();
        void Dec();
        void SetDepth(int depth);
    }

    public class FileLogger : ILogger
    {
        System.IO.TextWriter writer;
        private int depth = 0;

        public FileLogger(System.IO.TextWriter writer)
        {
            this.writer = writer;
        }

        public void Dispose()
        {
            writer.Flush();
            writer.Dispose();
        }

        public void Write(object text)
        {
            var tabs = new String('\t', depth);
            writer.Write(tabs + text);
        }

        public void WriteLine(object text)
        {
            var tabs = new String('\t', depth);
            writer.WriteLine(tabs + text);
        }

        public void WriteException(System.Exception ex, string? message)
        {
            var tabs = new String('\t', depth);
            if (message == null)
            {
                writer.WriteLine($"{tabs}Exception: {ex.Message}");
            }
            else
            {
                writer.WriteLine($"{tabs}{message}: {ex.Message}");
            }
        }

        public void Inc()
        {
            depth += 1;
        }

        public void Dec()
        {
            depth = Math.Max(depth - 1, 0);
        }

        public void SetDepth(int depth)
        {
            this.depth = Math.Max(depth, 0);
        }
    }

    public class NullLogger : ILogger
    {
        public void Dispose()
        {
        }

        public void Write(object text)
        {
        }

        public void WriteLine(object text)
        {
        }

        public void WriteException(System.Exception ex, string? message = null)
        {
        }

        public void Inc()
        {
        }

        public void Dec()
        {
        }

        public void SetDepth(int depth)
        {
        }
    }
}
