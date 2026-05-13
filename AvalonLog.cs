using AvalonEditB;
using AvalonEditB.Document;
using AvalonEditB.Editing;
using AvalonEditB.Search;
using AvalonLog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace AvalonLog;

public class AvalonLog : ContentControl {

    private int _docLength = 0;
    private bool _isAlive = true;
    private int _lastPrintDelay = 30;
    private long _printInterval = 50L;
    private long _printCallsCounter = 0;
    private int _maxCharsInLog = 1024_000;
    private int _maxVisibleLines = 0;
    private double _trimRatio = 1.1;
    private int _trimmedLineCount = 0;
    private bool _dontPrintJustBuffer = false;
    private bool _stillLessThanMaxChars = true;
    private SolidColorBrush? _prevMsgBrush = null;

    private readonly TextEditor _log = new();
    private readonly SearchPanel _searchPanel;
    private readonly ColorizingTransformer _color;
    private readonly StringBuilder _buffer = new();
    private readonly SelectedTextHighlighter _hiLi;
    private readonly Stopwatch _stopWatch = Stopwatch.StartNew();
    private readonly List<NewColor> _offsetColors = [new NewColor(-1, null)];

    private static readonly string NewLine = Environment.NewLine;
    private SolidColorBrush _customBrush = BrushHelper.FreezeIt(Brushes.Black);
    private SolidColorBrush _defaultBrush = BrushHelper.FreezeIt(Brushes.Black);

    public AvalonLog() {
        _hiLi = new SelectedTextHighlighter(_log);
        _color = new ColorizingTransformer(_log, _offsetColors, _defaultBrush);
        _searchPanel = SearchPanel.Install(_log, enableReplace: false);

        base.Content = _log;

        _log.FontFamily = new FontFamily("Cascadia Code");
        _log.FontSize = 14.0;
        _log.IsReadOnly = true;
        _log.Encoding = Encoding.Default;
        _log.ShowLineNumbers = true;
        _log.Options.EnableHyperlinks = true;
        _log.TextArea.SelectionCornerRadius = 0.0;
        _log.TextArea.SelectionBorder = null;
        _log.TextArea.TextView.LinkTextForegroundBrush = BrushHelper.FreezeIt(Brushes.Blue);

        _log.TextArea.TextView.LineTransformers.Add(_color);
        _log.TextArea.SelectionChanged += _color.SelectionChangedDelegate;

        _log.TextArea.TextView.LineTransformers.Add(_hiLi);
        _log.TextArea.SelectionChanged += _hiLi.SelectionChangedDelegate;

        if (_log.TextArea.LeftMargins[0] is LineNumberMargin) {
            _log.TextArea.LeftMargins[0] = new CumulativeLineNumberMargin(() => _trimmedLineCount) {
                HighlightCurrentLineNumber = false
            };
        }

        _defaultBrush = BrushHelper.FreezeIt((SolidColorBrush)_log.Foreground.Clone());
    }

    private void SetCustomBrush(int red, int green, int blue) {
        byte r = BrushHelper.ClampToByte(red);
        byte g = BrushHelper.ClampToByte(green);
        byte b = BrushHelper.ClampToByte(blue);
        var col = _customBrush.Color;
        if (col.R != r || col.G != g || col.B != b) {
            _customBrush = BrushHelper.FreezeIt(new SolidColorBrush(Color.FromRgb(r, g, b)));
        }
    }

    private string GetBufferText() {
        string txt = _buffer.ToString();
        _buffer.Clear();
        return txt;
    }

    private void TrimExcessLines() {
        if (_maxVisibleLines <= 0) return;
        int lineCount = _log.Document.LineCount;
        int threshold = (int)(_maxVisibleLines * _trimRatio);
        if (lineCount <= threshold) return;

        int removeCount = lineCount - _maxVisibleLines;
        int removeLength = _log.Document.GetLineByNumber(removeCount + 1).Offset;
        _log.Document.Remove(0, removeLength);
        _trimmedLineCount += removeCount;

        // Adjust _offsetColors: find last entry in removed range, keep it (Off=0), remove prior entries, shift remaining
        int lastInRangeIdx = -1;
        for (int i = 0; i < _offsetColors.Count; i++) {
            if (_offsetColors[i].Off < removeLength) {
                lastInRangeIdx = i;
            } else {
                break;
            }
        }

        if (lastInRangeIdx >= 0) {
            _offsetColors[lastInRangeIdx] = new NewColor(0, _offsetColors[lastInRangeIdx].Brush);
            if (lastInRangeIdx > 0) {
                _offsetColors.RemoveRange(0, lastInRangeIdx);
            }
        }

        for (int i = 0; i < _offsetColors.Count; i++) {
            if (_offsetColors[i].Off > 0) {
                _offsetColors[i] = new NewColor(_offsetColors[i].Off - removeLength, _offsetColors[i].Brush);
            }
        }

        lock (_buffer) {
            _docLength -= removeLength;
        }
    }

    private void PrintToLog() {
        if (!_isAlive) return;
        string txt;
        lock (_buffer) {
            txt = GetBufferText();
        }

        if (txt.Length > 0) {
            _log.AppendText(txt);
            _log.ScrollToEnd();
            if (_log.WordWrap) _log.ScrollToEnd();
            TrimExcessLines();
            _stopWatch.Restart();
        }
    }

    private void PrintOrBuffer(string txt, bool addNewLine, SolidColorBrush brush) {
        if (!_stillLessThanMaxChars || (txt.Length == 0 && !addNewLine) || !_isAlive) return;

        lock (_buffer) {
            if (_prevMsgBrush != brush) {
                _offsetColors.Add(new NewColor(_docLength, brush));
                _prevMsgBrush = brush;
            }

            if (addNewLine) {
                _buffer.AppendLine(txt);
                _docLength += txt.Length + NewLine.Length;
            } else {
                _buffer.Append(txt);
                _docLength += txt.Length;
            }
        }

        if (_docLength > _maxCharsInLog && _isAlive) {
            _stillLessThanMaxChars = false;
            try { _log.Dispatcher.Invoke(PrintToLog); } catch (InvalidOperationException) { } catch (TaskCanceledException) { }
            string itsOverTxt = $"{NewLine}{NewLine}  **** STOP OF LOGGING **** Log has more than {_maxCharsInLog} characters! Clear Log view first {NewLine}{NewLine}{NewLine}{NewLine} ";
            lock (_buffer) {
                _offsetColors.Add(new NewColor(_docLength, BrushHelper.FreezeIt(Brushes.Red)));
                _buffer.AppendLine(itsOverTxt);
                _docLength += itsOverTxt.Length;
            }
            try { _log.Dispatcher.Invoke(PrintToLog); } catch (InvalidOperationException) { } catch (TaskCanceledException) { }
        } else if (_dontPrintJustBuffer) {
            long k = Interlocked.Increment(ref _printCallsCounter);
            Timer? timer = null;
            timer = new Timer(_ => {
                if (!_dontPrintJustBuffer || !_isAlive) {
                    if (Interlocked.Read(ref _printCallsCounter) == k && _isAlive) {
                        try { _log.Dispatcher.Invoke(PrintToLog); } catch (InvalidOperationException) { } catch (TaskCanceledException) { }
                    }
                    timer?.Dispose();
                } else {
                    timer?.Change(50, Timeout.Infinite);
                }
            }, null, 50, Timeout.Infinite);
        } else {
            if (_stopWatch.ElapsedMilliseconds > _printInterval && _isAlive) {
                try { _log.Dispatcher.Invoke(PrintToLog); } catch (InvalidOperationException) { } catch (TaskCanceledException) { }
            } else {
                long k = Interlocked.Increment(ref _printCallsCounter);
                Timer? timer = null;
                timer = new Timer(_ => {
                    if (Interlocked.Read(ref _printCallsCounter) == k && _isAlive) {
                        try { _log.Dispatcher.Invoke(PrintToLog); } catch (InvalidOperationException) { } catch (TaskCanceledException) { }
                    }
                    timer?.Dispose();
                }, null, _lastPrintDelay, Timeout.Infinite);
            }
        }
    }

    private void Print(SolidColorBrush br, string s) {
        _customBrush = br;
        PrintOrBuffer(s, true, _customBrush);
    }

    public bool IsAlive {
        get => _isAlive;
        set => _isAlive = value;
    }

    public ScrollBarVisibility VerticalScrollBarVisibility {
        get => _log.VerticalScrollBarVisibility;
        set => _log.VerticalScrollBarVisibility = value;
    }

    public ScrollBarVisibility HorizontalScrollBarVisibility {
        get => _log.HorizontalScrollBarVisibility;
        set => _log.HorizontalScrollBarVisibility = value;
    }

    public new FontFamily FontFamily {
        get => _log.FontFamily;
        set { _log.FontFamily = value; SyncDefaultBrush(); }
    }

    public new double FontSize {
        get => _log.FontSize;
        set { _log.FontSize = value; SyncDefaultBrush(); }
    }

    private void SyncDefaultBrush() {
        var newBrush = BrushHelper.FreezeIt((SolidColorBrush)_log.Foreground.Clone());
        if (_defaultBrush != newBrush) {
            _defaultBrush = newBrush;
            _color.SetDefaultBrush(_defaultBrush);
        }
    }

    public bool ShowLineNumbers {
        get => _log.ShowLineNumbers;
        set => _log.ShowLineNumbers = value;
    }

    public bool EnableHyperlinks {
        get => _log.Options.EnableHyperlinks;
        set => _log.Options.EnableHyperlinks = value;
    }

    public int LastPrintDelay {
        get => _lastPrintDelay;
        set => _lastPrintDelay = value;
    }

    public long PrintInterval {
        get => _printInterval;
        set => _printInterval = value;
    }

    public string GetText() => _log.Text;

    public string GetText(ISegment seg) => _log.Document.GetText(seg);

    public Selection Selection => _log.TextArea.Selection;

    public SearchPanel SearchPanel => _searchPanel;

    public bool WordWrap {
        get => _log.WordWrap;
        set {
            if (value) {
                _log.WordWrap = true;
                _log.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            } else {
                _log.WordWrap = false;
                _log.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
        }
    }

    public int MaximumCharacterAllowance {
        get => _maxCharsInLog;
        set => _maxCharsInLog = value;
    }

    public int MaxVisibleLines {
        get => _maxVisibleLines;
        set => _maxVisibleLines = value;
    }

    public double TrimRatio {
        get => _trimRatio;
        set => _trimRatio = Math.Clamp(value, 1.0, 2.0);
    }

    [Obsolete("It is not actually obsolete, but normally not used, so hidden from editor tools.")]
    public TextEditor AvalonEdit => _log;

    public SelectedTextHighlighter SelectedTextHighLighter => _hiLi;

    public void Clear() {
        lock (_buffer) {
            _dontPrintJustBuffer = true;
            _buffer.Clear();
            _docLength = 0;
            _prevMsgBrush = null;
            _stillLessThanMaxChars = true;
            _trimmedLineCount = 0;
            Interlocked.Exchange(ref _printCallsCounter, 0L);
        }

        try {
            _log.Dispatcher.Invoke(() => {
                _log.Clear();
                _offsetColors.Clear();
                _offsetColors.Add(new NewColor(-1, null));
                _defaultBrush = BrushHelper.FreezeIt((SolidColorBrush)_log.Foreground.Clone());
                _color.SetDefaultBrush(_defaultBrush);
                _stopWatch.Restart();
                _dontPrintJustBuffer = false;
            });
        } catch (InvalidOperationException) { } catch (TaskCanceledException) { }
    }

    public LogTextWriter GetTextWriter(int red, int green, int blue) {
        var br = BrushHelper.OfRGB(red, green, blue);
        return new LogTextWriter(
            s => PrintOrBuffer(s, false, br),
            s => PrintOrBuffer(s, true, br)
        );
    }

    public LogTextWriter GetTextWriter(SolidColorBrush br) {
        var fbr = BrushHelper.FreezeIt(br);
        return new LogTextWriter(
            s => PrintOrBuffer(s, false, fbr),
            s => PrintOrBuffer(s, true, fbr)
        );
    }

    public LogTextWriter GetConditionalTextWriter(Func<string, bool> predicate, SolidColorBrush br) {
        var fbr = BrushHelper.FreezeIt(br);
        return new LogTextWriter(
            s => { if (predicate(s)) PrintOrBuffer(s, false, fbr); },
            s => { if (predicate(s)) PrintOrBuffer(s, true, fbr); }
        );
    }

    public LogTextWriter GetConditionalTextWriter(Func<string, bool> predicate, int red, int green, int blue) {
        var br = BrushHelper.OfRGB(red, green, blue);
        return new LogTextWriter(
            s => { if (predicate(s)) PrintOrBuffer(s, false, br); },
            s => { if (predicate(s)) PrintOrBuffer(s, true, br); }
        );
    }

    public void AppendWithLastColor(string s) {
        PrintOrBuffer(s, false, _customBrush);
    }

    public void AppendLineWithLastColor(string s) {
        PrintOrBuffer(s, true, _customBrush);
    }

    public void Append(string s) {
        PrintOrBuffer(s, false, _defaultBrush);
    }

    public void AppendLine(string s) {
        PrintOrBuffer(s, true, _defaultBrush);
    }

    public void Append(string s, SolidColorBrush brush) {
        _customBrush = BrushHelper.FreezeIt(brush);
        PrintOrBuffer(s, false, _customBrush);
    }

    public void AppendLine(string s, SolidColorBrush brush) {
        _customBrush = BrushHelper.FreezeIt(brush);
        PrintOrBuffer(s, true, _customBrush);
    }

    public void Append(string s, int red, int green, int blue) {
        SetCustomBrush(red, green, blue);
        PrintOrBuffer(s, false, _customBrush);
    }

    public void AppendLine(string s, int red, int green, int blue) {
        SetCustomBrush(red, green, blue);
        PrintOrBuffer(s, true, _customBrush);
    }

    /// <summary>
    /// Appends an empty line. The blank line inherits the color of the previous message
    /// to maintain visual continuity within colored blocks. This is a deliberate design choice.
    /// </summary>
    public void AppendLine() {
        var br = _prevMsgBrush ?? _defaultBrush;
        PrintOrBuffer("", true, br);
    }

    public void PrintLine(string s) {
        Print(_customBrush, s);
    }

    public void PrintLine(string s, SolidColorBrush brush) {
        _customBrush = BrushHelper.FreezeIt(brush);
        Print(_customBrush, s);
    }

    public void PrintLine(string s, int red, int green, int blue) {
        SetCustomBrush(red, green, blue);
        Print(_customBrush, s);
    }
}