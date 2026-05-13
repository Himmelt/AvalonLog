using System.IO;
using System.Text;

namespace AvalonLog.Core;

public class LogTextWriter(Action<string> write, Action<string> writeLine) : TextWriter {

    public override Encoding Encoding => Encoding.Default;

    public override void Write(string? s) {
        if (s != null) write(s);
    }

    public override void WriteLine(string? s) {
        writeLine(s ?? "");
    }

    public override void WriteLine() {
        writeLine("");
    }
}