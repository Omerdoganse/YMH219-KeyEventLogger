using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace KeyEventLogger;

/// <summary>
/// Kayit durumunu renkli bir daire ve yazi ile gosteren kucuk kontrol.
/// </summary>
public class RecordingIndicator : Control
{
    private bool _isRecording;

    public RecordingIndicator()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Size = new Size(170, 32);
        Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        TabStop = false;
    }

    [DefaultValue(false)]
    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            if (_isRecording == value) return;
            _isRecording = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color back = _isRecording ? Color.FromArgb(255, 235, 235) : Color.FromArgb(235, 235, 235);
        Color dot = _isRecording ? Color.FromArgb(220, 40, 40) : Color.Gray;
        string text = _isRecording ? "KAYIT AÇIK" : "KAYIT KAPALI";

        using (var bg = new SolidBrush(back))
        using (var border = new Pen(dot, 1.5f))
        {
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            g.FillRectangle(bg, rect);
            g.DrawRectangle(border, rect);
        }

        int d = Height - 14;
        using (var dotBrush = new SolidBrush(dot))
            g.FillEllipse(dotBrush, 8, 7, d, d);

        TextRenderer.DrawText(g, text, Font, new Rectangle(14 + d, 0, Width - 14 - d, Height), dot,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }
}
