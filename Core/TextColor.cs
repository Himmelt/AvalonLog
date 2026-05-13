using AvalonEditB;
using AvalonEditB.Document;
using AvalonEditB.Rendering;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace AvalonLog.Core;

internal struct NewColor {
    public int Off;
    public SolidColorBrush? Brush;

    public NewColor(int off, SolidColorBrush? brush) {
        Off = off;
        Brush = brush;
    }

    public static NewColor FindCurrentInList(List<NewColor> cs, int currOff) {
        int lo = 0;
        int hi = cs.Count - 1;

        while (lo <= hi) {
            int mid = lo + (hi - lo) / 2;
            if (cs[mid].Off <= currOff) {
                if (mid == cs.Count - 1) return cs[mid];
                if (cs[mid + 1].Off > currOff) return cs[mid];
                lo = mid + 1;
            } else {
                hi = mid - 1;
            }
        }

        return cs[0];
    }
}

internal struct RangeColor {
    public int Start;
    public int Ende;
    public SolidColorBrush? Brush;

    public RangeColor(int start, int ende, SolidColorBrush? brush) {
        Start = start;
        Ende = ende;
        Brush = brush;
    }

    public static List<RangeColor> GetInRange(List<NewColor> cs, int stOff, int enOff) {
        var result = new List<RangeColor>();
        int i = enOff;

        while (true) {
            var c = NewColor.FindCurrentInList(cs, i);
            if (c.Off <= stOff) {
                result.Add(new RangeColor(stOff, enOff, c.Brush));
                break;
            } else {
                result.Add(new RangeColor(i, enOff, c.Brush));
                i = c.Off - 1;
            }
        }

        result.Reverse();
        return result;
    }
}

internal class ColorizingTransformer : DocumentColorizingTransformer {
    private readonly TextEditor _ed;
    private readonly List<NewColor> _offsetColors;
    private SolidColorBrush _defaultBrush;

    private int _selStart = -9;
    private int _selEnd = -9;

    public ColorizingTransformer(TextEditor ed, List<NewColor> offsetColors, SolidColorBrush defaultBrush) {
        _ed = ed;
        _offsetColors = offsetColors;
        _defaultBrush = defaultBrush;
    }

    public void SetDefaultBrush(SolidColorBrush brush) => _defaultBrush = brush;

    public void SelectionChangedDelegate(object? sender, EventArgs e) {
        if (_ed.SelectionLength == 0) {
            _selStart = -9;
            _selEnd = -9;
        } else {
            _selStart = _ed.SelectionStart;
            _selEnd = _selStart + _ed.SelectionLength;
        }
    }

    protected override void ColorizeLine(DocumentLine line) {
        if (line.IsDeleted) return;

        int stLn = line.Offset;
        int enLn = line.EndOffset;
        var cs = RangeColor.GetInRange(_offsetColors, stLn, enLn);
        bool any = false;

        if (_selStart == _selEnd || _selStart > enLn || _selEnd < stLn) {
            foreach (var c in cs) {
                if (c.Brush == null && any) {
                    ChangeLinePart(c.Start, c.Ende, element => element.TextRunProperties.SetForegroundBrush(_defaultBrush));
                } else if (c.Brush != null) {
                    any = true;
                    ChangeLinePart(c.Start, c.Ende, el => el.TextRunProperties.SetForegroundBrush(c.Brush));
                }
            }
        } else {
            foreach (var c in cs) {
                var br = c.Brush ?? _defaultBrush;
                int st = c.Start;
                int en = c.Ende;

                foreach (var seg in _ed.TextArea.Selection.Segments) {
                    if (seg.EndOffset < stLn) continue;
                    if (seg.StartOffset > enLn) continue;

                    if (seg.StartOffset == seg.EndOffset) {
                        ChangeLinePart(st, en, el => el.TextRunProperties.SetForegroundBrush(br));
                    } else if (seg.StartOffset > en) {
                        ChangeLinePart(st, en, el => el.TextRunProperties.SetForegroundBrush(br));
                    } else if (seg.EndOffset <= st) {
                        ChangeLinePart(st, en, el => el.TextRunProperties.SetForegroundBrush(br));
                    } else {
                        if (st < seg.StartOffset)
                            ChangeLinePart(st, seg.StartOffset, el => el.TextRunProperties.SetForegroundBrush(br));
                        if (en > seg.EndOffset)
                            ChangeLinePart(seg.EndOffset, en, el => el.TextRunProperties.SetForegroundBrush(br));
                    }
                }
            }
        }
    }
}
