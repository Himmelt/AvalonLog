using AvalonEditB;
using AvalonEditB.Document;
using AvalonEditB.Rendering;
using System.Windows.Media;

namespace AvalonLog.Core;

internal struct NewColor(int off, SolidColorBrush? brush) {

    public int Off = off;
    public SolidColorBrush? Brush = brush;

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

internal struct RangeColor(int start, int ende, SolidColorBrush? brush) {

    public int Ende = ende;
    public int Start = start;
    public SolidColorBrush? Brush = brush;

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

internal class ColorizingTransformer(TextEditor ed, List<NewColor> offsetColors, SolidColorBrush defaultBrush) : DocumentColorizingTransformer {

    private int _selEnd = -9;
    private int _selStart = -9;

    public void SetDefaultBrush(SolidColorBrush brush) => defaultBrush = brush;

    public void SelectionChangedDelegate(object? sender, EventArgs e) {
        if (ed.SelectionLength == 0) {
            _selStart = -9;
            _selEnd = -9;
        } else {
            _selStart = ed.SelectionStart;
            _selEnd = _selStart + ed.SelectionLength;
        }
    }

    protected override void ColorizeLine(DocumentLine line) {
        if (line.IsDeleted) return;

        int stLn = line.Offset;
        int enLn = line.EndOffset;
        var cs = RangeColor.GetInRange(offsetColors, stLn, enLn);
        bool any = false;

        if (_selStart == _selEnd || _selStart > enLn || _selEnd < stLn) {
            foreach (var c in cs) {
                if (c.Brush == null && any) {
                    ChangeLinePart(c.Start, c.Ende, element => element.TextRunProperties.SetForegroundBrush(defaultBrush));
                } else if (c.Brush != null) {
                    any = true;
                    ChangeLinePart(c.Start, c.Ende, el => el.TextRunProperties.SetForegroundBrush(c.Brush));
                }
            }
        } else {
            foreach (var c in cs) {
                var br = c.Brush ?? defaultBrush;
                int st = c.Start;
                int en = c.Ende;

                foreach (var seg in ed.TextArea.Selection.Segments) {
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