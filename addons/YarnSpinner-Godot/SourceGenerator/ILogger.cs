/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;

namespace YarnSpinnerGodot
{

public interface ILogger : IDisposable
{
    void Write(object obj);
    void WriteLine(object obj);
}

public class FileLogger : ILogger
{
    System.IO.TextWriter writer;

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
        writer.Write(text);
    }

    public void WriteLine(object text)
    {
        writer.WriteLine(text);
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
}
}