using ZamanliKapatma.Core;

namespace ZamanliKapatma;

/// <summary>İşlemden hemen önce gösterilen, geri sayımlı uyarı penceresi.</summary>
internal sealed class WarningForm : Form
{
    private readonly Scheduler _scheduler;
    private readonly TableLayoutPanel _root;
    private readonly Label _countdownLabel;
    private readonly Font _titleFont;
    private readonly Font _countdownFont;

    public WarningForm(
        Scheduler scheduler,
        Icon icon,
        TimeSpan snoozeDelay,
        Action executeNow,
        Action snooze,
        Action skip)
    {
        _scheduler = scheduler;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Text = "Zamanlı Kapatma";
        Icon = icon;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ControlBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        Padding = new Padding(18);

        _titleFont = new Font(Font.FontFamily, 12f, FontStyle.Bold);
        _countdownFont = new Font(Font.FontFamily, 24f, FontStyle.Bold);

        var title = new Label
        {
            Text = scheduler.Settings.Action.PendingMessage(),
            AutoSize = true,
            Font = _titleFont,
        };
        _countdownLabel = new Label
        {
            AutoSize = true,
            Font = _countdownFont,
            ForeColor = Color.FromArgb(220, 38, 38),
            Margin = new Padding(3, 10, 3, 6),
        };
        var hint = new Label { Text = "Açık dosyalarınızı kaydetmeyi unutmayın.", AutoSize = true };

        var snoozeButton = new Button { Text = $"{DurationText.Format(snoozeDelay)} ertele", AutoSize = true };
        var skipButton = new Button { Text = scheduler.Settings.IsRecurring ? "Bugünlük atla" : "İptal et", AutoSize = true };
        var nowButton = new Button { Text = "Şimdi yap", AutoSize = true };
        snoozeButton.Click += (_, _) => snooze();
        skipButton.Click += (_, _) => skip();
        nowButton.Click += (_, _) => executeNow();

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 14, 0, 0),
        };
        buttons.Controls.AddRange(new Control[] { snoozeButton, skipButton, nowButton });

        _root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true };
        _root.Controls.Add(title);
        _root.Controls.Add(_countdownLabel);
        _root.Controls.Add(hint);
        _root.Controls.Add(buttons);
        Controls.Add(_root);

        // Esc işlemi iptal eder; Enter'a yanlışlıkla basmak bilgisayarı kapatmasın diye odak "ertele"de.
        CancelButton = skipButton;
        ActiveControl = snoozeButton;

        UpdateCountdown(DateTime.Now);
    }

    public void UpdateCountdown(DateTime now)
    {
        if (_scheduler.Target is not { } target)
        {
            return;
        }

        int seconds = (int)Math.Max(0, Math.Ceiling((target - now).TotalSeconds));
        _countdownLabel.Text = $"{seconds} saniye";
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var preferred = _root.GetPreferredSize(Size.Empty);
        ClientSize = new Size(preferred.Width + Padding.Horizontal, preferred.Height + Padding.Vertical);
        CenterToScreen();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _titleFont.Dispose();
            _countdownFont.Dispose();
        }
    }
}
