using System;
using System.IO;
using System.Text;

namespace AvalonLog;

public class LogTextWriter : TextWriter
{
    private readonly Action<string> _write;
    private readonly Action<string> _writeLine;

    public LogTextWriter(Action<string> write, Action<string> writeLine)
    {
        _write = write;
        _writeLine = writeLine;
    }

    public override Encoding Encoding => Encoding.Default;

    public override void Write(string? s)
    {
        if (s != null) _write(s);
    }

    public override void WriteLine(string? s)
    {
        _writeLine(s ?? "");
    }

    public override void WriteLine()
    {
        _writeLine("");
    }
}
