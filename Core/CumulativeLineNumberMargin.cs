using AvalonEditB.Editing;
using AvalonEditB.Rendering;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AvalonLog.Core;

internal class CumulativeLineNumberMargin : LineNumberMargin {
    private readonly Func<int> _getTrimmedLineCount;
    private Typeface _typeface = new Typeface("Consolas");
    private double _emSize = 12.0;
    private int _maxLineNumberLength = 2;

    public CumulativeLineNumberMargin(Func<int> getTrimmedLineCount) {
        _getTrimmedLineCount = getTrimmedLineCount;
    }

    protected override Size MeasureOverride(Size availableSize) {
        _typeface = new Typeface(
            (FontFamily)GetValue(TextBlock.FontFamilyProperty),
            (FontStyle)GetValue(TextBlock.FontStyleProperty),
            (FontWeight)GetValue(TextBlock.FontWeightProperty),
            (FontStretch)GetValue(TextBlock.FontStretchProperty)
        );
        _emSize = (double)GetValue(TextBlock.FontSizeProperty);

        int cumulativeCount = (Document?.LineCount ?? 1) + _getTrimmedLineCount();
        int newLength = cumulativeCount.ToString(CultureInfo.CurrentCulture).Length;
        if (newLength < 2) newLength = 2;
        _maxLineNumberLength = newLength;

        var ft = new FormattedText(
            new string('9', _maxLineNumberLength),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _typeface,
            _emSize,
            LineNumberForegroundColor,
            VisualTreeHelper.GetDpi(this).PixelsPerDip
        );
        return new Size(ft.Width + 3.0, 0.0);
    }

    protected override void OnRender(DrawingContext drawingContext) {
        var textView = TextView;
        var renderSize = RenderSize;
        if (textView == null || !textView.VisualLinesValid) return;

        if (BackgroundColor != null) {
            drawingContext.DrawRectangle(BackgroundColor, null, new Rect(0, 0, renderSize.Width, renderSize.Height));
        }

        int highlightedLine = textView.HighlightedLine;
        int trimmedCount = _getTrimmedLineCount();

        foreach (VisualLine visualLine in textView.VisualLines) {
            int docLineNumber = visualLine.FirstDocumentLine.LineNumber;
            int displayLineNumber = docLineNumber + trimmedCount;

            var foreground = (docLineNumber == highlightedLine && HighlightCurrentLineNumber)
                ? CurrentLineNumberForegroundColor
                : LineNumberForegroundColor;

            var ft = new FormattedText(
                displayLineNumber.ToString(CultureInfo.CurrentCulture),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _typeface,
                _emSize,
                foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            double y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop);
            drawingContext.DrawText(ft, new Point(renderSize.Width - ft.Width, y - textView.VerticalOffset));
        }
    }
}
