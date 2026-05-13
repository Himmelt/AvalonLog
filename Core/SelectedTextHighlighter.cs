using AvalonEditB;
using AvalonEditB.Document;
using AvalonEditB.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace AvalonLog.Core;

public class SelectedTextHighlighter(TextEditor lg) : DocumentColorizingTransformer {

    private string? _highTxt;
    private int _curSelEnd = -1;
    private int _curSelStart = -1;
    private bool _isEnabled = true;
    private SolidColorBrush _colorHighlight = BrushHelper.Brighter(210, Brushes.Blue);

    public event Action? OnHighlightCleared;
    public event Action<string, List<int>>? OnHighlightChanged;

    private void SelectionChanged() {
        if (!_isEnabled) return;

        string? selTxt = null;
        var sel = lg.TextArea.Selection;

        if (sel.Length >= 2 && sel.StartPosition.Line == sel.EndPosition.Line) {
            string selt = lg.SelectedText;
            if (selt.Trim().Length >= 2)
                selTxt = selt;
        }

        if (!string.IsNullOrEmpty(selTxt)) {
            _highTxt = selTxt;
            _curSelStart = lg.SelectionStart;
            _curSelEnd = _curSelStart + selTxt.Length - 1;
            lg.TextArea.TextView.Redraw();

            var doc = lg.Document;
            var selTxtCapture = selTxt;
            Task.Run(() => {
                try {
                    string tx = doc.CreateSnapshot().Text;
                    var locations = new List<int>();
                    int index = tx.IndexOf(selTxtCapture, 0, StringComparison.Ordinal);
                    while (index >= 0) {
                        locations.Add(index);
                        int st = index + selTxtCapture.Length;
                        if (st >= tx.Length) break;
                        index = tx.IndexOf(selTxtCapture, st, StringComparison.Ordinal);
                    }

                    if (_isEnabled) {
                        Application.Current.Dispatcher.Invoke(() => {
                            OnHighlightChanged?.Invoke(selTxtCapture, locations);
                        });
                    }
                } catch { /* document may have been disposed */ }
            });
        } else {
            if (_highTxt is not null) {
                _highTxt = null;
                lg.TextArea.TextView.Redraw();
                OnHighlightCleared?.Invoke();
            }
        }
    }

    public SolidColorBrush ColorHighlighting {
        get => _colorHighlight;
        set => _colorHighlight = BrushHelper.FreezeIt(value);
    }

    public bool IsEnabled {
        get => _isEnabled;
        set {
            _isEnabled = value;
            if (value) SelectionChanged();
            else if (_highTxt is not null) {
                lg.TextArea.TextView.Redraw();
                OnHighlightCleared?.Invoke();
            }
        }
    }

    protected override void ColorizeLine(DocumentLine line) {
        if (!_isEnabled || _highTxt == null) return;

        int lineStartOffset = line.Offset;
        string text = lg.Document.GetText(line);
        int index = text.IndexOf(_highTxt, 0, StringComparison.Ordinal);

        while (index >= 0) {
            int st = lineStartOffset + index;
            int en = lineStartOffset + index + _highTxt.Length - 1;

            if ((st < _curSelStart || st > _curSelEnd) && (en < _curSelStart || en > _curSelEnd)) {
                ChangeLinePart(st, en + 1, el => el.TextRunProperties.SetBackgroundBrush(_colorHighlight));
            }

            int start = index + _highTxt.Length;
            index = text.IndexOf(_highTxt, start, StringComparison.Ordinal);
        }
    }

    public void SelectionChangedDelegate(object? sender, EventArgs e) {
        SelectionChanged();
    }
}