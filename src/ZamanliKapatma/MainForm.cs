using ZamanliKapatma.Core;

namespace ZamanliKapatma;

/// <summary>Planın ayrıntılı olarak ayarlandığı ana pencere. Kapatılınca tepsiye gizlenir.</summary>
internal sealed class MainForm : Form
{
    private readonly Scheduler _scheduler;
    private readonly TableLayoutPanel _root;
    private readonly Font _headerFont;
    private readonly Font _statusFont;
    private readonly ComboBox _actionBox;
    private readonly RadioButton _afterRadio;
    private readonly RadioButton _atRadio;
    private readonly NumericUpDown _hoursBox;
    private readonly NumericUpDown _minutesBox;
    private readonly DateTimePicker _timePicker;
    private readonly CheckBox _dailyCheck;
    private readonly CheckBox _forceCheck;
    private readonly CheckBox _startupCheck;
    private readonly Label _statusLabel;
    private readonly Button _startButton;
    private readonly Button _toggleButton;
    private readonly Button _cancelButton;
    private bool _updatingStartupCheck;

    /// <summary>Kullanıcı pencereyi kapattığında (uygulama tepside çalışmaya devam eder).</summary>
    public event EventHandler? HiddenToTray;

    public MainForm(Scheduler scheduler, Icon icon)
    {
        _scheduler = scheduler;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Text = TrayApplicationContext.AppTitle;
        Icon = icon;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Padding = new Padding(14);

        _headerFont = new Font(Font.FontFamily, 9.75f, FontStyle.Bold);
        _statusFont = new Font(Font, FontStyle.Bold);

        // Ne yapılsın?
        _actionBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true,
            Width = 220,
        };
        _actionBox.Format += (_, e) =>
        {
            if (e.ListItem is PowerAction action)
            {
                e.Value = action.DisplayName();
            }
        };
        foreach (var action in Enum.GetValues<PowerAction>())
        {
            _actionBox.Items.Add(action);
        }

        // Ne zaman? (iki seçenek aynı tabloda olduğu için radyo butonları otomatik gruplanır)
        _afterRadio = new RadioButton { Text = Lang.T("Şu kadar süre sonra:", "After:"), AutoSize = true, Anchor = AnchorStyles.Left };
        _atRadio = new RadioButton { Text = Lang.T("Şu saatte:", "At:"), AutoSize = true, Anchor = AnchorStyles.Left };
        _hoursBox = new NumericUpDown { Maximum = 99, Width = 56, TextAlign = HorizontalAlignment.Center, Anchor = AnchorStyles.Left };
        _minutesBox = new NumericUpDown { Maximum = 59, Width = 56, TextAlign = HorizontalAlignment.Center, Anchor = AnchorStyles.Left };
        _timePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Width = 80,
            Anchor = AnchorStyles.Left,
        };
        _dailyCheck = new CheckBox
        {
            Text = Lang.T("Her gün tekrarla", "Repeat daily"),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(12, 3, 3, 3),
        };

        var whenTable = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Margin = new Padding(0) };
        whenTable.Controls.Add(_afterRadio, 0, 0);
        whenTable.Controls.Add(Row(_hoursBox, InlineLabel(Lang.T("saat", "hours")), _minutesBox, InlineLabel(Lang.T("dakika", "minutes"))), 1, 0);
        whenTable.Controls.Add(_atRadio, 0, 1);
        whenTable.Controls.Add(Row(_timePicker, _dailyCheck), 1, 1);
        whenTable.Controls.Add(InlineLabel(Lang.T("Hızlı seç:", "Quick pick:")), 0, 2);
        whenTable.Controls.Add(Row(QuickButton(15), QuickButton(30), QuickButton(60), QuickButton(120)), 1, 2);

        // Seçenekler
        _forceCheck = new CheckBox { Text = Lang.T("Açık uygulamaları zorla kapat (kaydedilmemiş işler kaybolabilir)", "Force-close open apps (unsaved work may be lost)"), AutoSize = true };
        _startupCheck = new CheckBox { Text = Lang.T("Windows açılışında otomatik başlat", "Start automatically with Windows"), AutoSize = true };

        // Durum ve butonlar
        _statusLabel = new Label { AutoSize = true, Font = _statusFont, Margin = new Padding(3, 14, 3, 6) };
        _startButton = new Button { Text = Lang.T("Zamanla", "Schedule"), AutoSize = true };
        _toggleButton = new Button { Text = Lang.T("Devre dışı bırak", "Disable"), AutoSize = true };
        _cancelButton = new Button { Text = Lang.T("Planı iptal et", "Cancel plan"), AutoSize = true };

        _root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true };
        _root.Controls.Add(Header(Lang.T("Ne yapılsın?", "What should happen?"), first: true));
        _root.Controls.Add(_actionBox);
        _root.Controls.Add(Header(Lang.T("Ne zaman?", "When?")));
        _root.Controls.Add(whenTable);
        _root.Controls.Add(Header(Lang.T("Seçenekler", "Options")));
        _root.Controls.Add(_forceCheck);
        _root.Controls.Add(_startupCheck);
        _root.Controls.Add(_statusLabel);
        _root.Controls.Add(Row(_startButton, _toggleButton, _cancelButton));
        Controls.Add(_root);
        AcceptButton = _startButton;

        LoadSettings(_scheduler.Settings);

        _afterRadio.CheckedChanged += (_, _) => UpdateInputStates();
        _atRadio.CheckedChanged += (_, _) => UpdateInputStates();
        _actionBox.SelectedIndexChanged += (_, _) => UpdateInputStates();
        _startupCheck.CheckedChanged += (_, _) => OnStartupCheckChanged();
        _startButton.Click += (_, _) => OnStartClicked();
        _toggleButton.Click += (_, _) => _scheduler.Toggle(DateTime.Now);
        _cancelButton.Click += (_, _) => _scheduler.Cancel();

        UpdateInputStates();
        RefreshStartupCheck();
        RefreshStatus(DateTime.Now);
    }

    /// <summary>Form alanlarını verilen ayarlarla doldurur.</summary>
    public void LoadSettings(ScheduleSettings settings)
    {
        _actionBox.SelectedItem = settings.Action;
        _afterRadio.Checked = settings.Mode == ScheduleMode.AfterDuration;
        _atRadio.Checked = settings.Mode == ScheduleMode.AtTime;

        int totalMinutes = (int)Math.Round(settings.Duration.TotalMinutes);
        _hoursBox.Value = Math.Clamp(totalMinutes / 60, 0, 99);
        _minutesBox.Value = totalMinutes % 60;

        _timePicker.Value = DateTime.Today + settings.TimeOfDay.ToTimeSpan();
        _dailyCheck.Checked = settings.RepeatDaily;
        _forceCheck.Checked = settings.ForceCloseApps;
    }

    public void RefreshStatus(DateTime now)
    {
        _statusLabel.Text = Lang.T("Durum: ", "Status: ") + _scheduler.Describe(now);
        _statusLabel.ForeColor = _scheduler.Status switch
        {
            SchedulerStatus.Running => Color.FromArgb(37, 99, 235),
            SchedulerStatus.Paused => Color.FromArgb(180, 83, 9),
            _ => SystemColors.GrayText,
        };

        _toggleButton.Text = _scheduler.Status == SchedulerStatus.Paused ? Lang.T("Etkinleştir", "Enable") : Lang.T("Devre dışı bırak", "Disable");
        _toggleButton.Enabled = _scheduler.Status != SchedulerStatus.Idle;
        _cancelButton.Enabled = _scheduler.Status != SchedulerStatus.Idle;

        GrowToFitContent();
    }

    public void RefreshStartupCheck()
    {
        _updatingStartupCheck = true;
        try
        {
            _startupCheck.Checked = StartupManager.IsEnabled();
        }
        finally
        {
            _updatingStartupCheck = false;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var preferred = _root.GetPreferredSize(Size.Empty);
        ClientSize = new Size(preferred.Width + Padding.Horizontal, preferred.Height + Padding.Vertical);
        CenterToScreen();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible)
        {
            RefreshStartupCheck();
            RefreshStatus(DateTime.Now);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            // Pencereyi kapatmak uygulamayı kapatmaz; tepside çalışmaya devam eder.
            e.Cancel = true;
            Hide();
            HiddenToTray?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _headerFont.Dispose();
            _statusFont.Dispose();
        }
    }

    private void OnStartClicked()
    {
        var settings = ReadSettings();
        if (settings.Mode == ScheduleMode.AfterDuration && settings.Duration < TimeSpan.FromMinutes(1))
        {
            MessageBox.Show(this, Lang.T("Süre en az 1 dakika olmalı.", "The duration must be at least 1 minute."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (settings.IsRecurring && !StartupManager.IsEnabled())
        {
            var answer = MessageBox.Show(
                this,
                Lang.T(
                    "Her gün tekrarlanan planın çalışması için uygulamanın Windows açılışında başlaması gerekir.\n\nOtomatik başlatma açılsın mı?",
                    "A daily plan only works if the app starts with Windows.\n\nTurn on automatic startup?"),
                Text,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer == DialogResult.Yes)
            {
                TrySetStartup(true);
                RefreshStartupCheck();
            }
        }

        _scheduler.Start(settings, DateTime.Now);
    }

    private ScheduleSettings ReadSettings()
    {
        var time = _timePicker.Value;
        return new ScheduleSettings
        {
            Action = _actionBox.SelectedItem is PowerAction action ? action : PowerAction.Shutdown,
            Mode = _afterRadio.Checked ? ScheduleMode.AfterDuration : ScheduleMode.AtTime,
            Duration = TimeSpan.FromMinutes((double)(_hoursBox.Value * 60 + _minutesBox.Value)),
            TimeOfDay = new TimeOnly(time.Hour, time.Minute),
            RepeatDaily = _dailyCheck.Checked,
            ForceCloseApps = _forceCheck.Checked,
        };
    }

    private void UpdateInputStates()
    {
        bool afterMode = _afterRadio.Checked;
        _hoursBox.Enabled = afterMode;
        _minutesBox.Enabled = afterMode;
        _timePicker.Enabled = !afterMode;
        _dailyCheck.Enabled = !afterMode;
        _forceCheck.Enabled = _actionBox.SelectedItem is not PowerAction.Sleep;
    }

    private void OnStartupCheckChanged()
    {
        if (_updatingStartupCheck)
        {
            return;
        }

        if (!TrySetStartup(_startupCheck.Checked))
        {
            RefreshStartupCheck();
        }
    }

    private bool TrySetStartup(bool enabled)
    {
        try
        {
            StartupManager.SetEnabled(enabled);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            MessageBox.Show(this, Lang.T("Otomatik başlatma ayarı değiştirilemedi: ", "Could not change the startup setting: ") + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    /// <summary>Durum metni uzarsa pencereyi büyütür (küçültmez; böylece pencere titremez).</summary>
    private void GrowToFitContent()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        var preferred = _root.GetPreferredSize(Size.Empty);
        int width = Math.Max(ClientSize.Width, preferred.Width + Padding.Horizontal);
        int height = Math.Max(ClientSize.Height, preferred.Height + Padding.Vertical);
        if (width != ClientSize.Width || height != ClientSize.Height)
        {
            ClientSize = new Size(width, height);
        }
    }

    private Button QuickButton(int minutes)
    {
        var button = new Button
        {
            Text = DurationText.Format(TimeSpan.FromMinutes(minutes)),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(6, 0, 6, 0),
        };
        button.Click += (_, _) =>
        {
            _afterRadio.Checked = true;
            _hoursBox.Value = minutes / 60;
            _minutesBox.Value = minutes % 60;
            _startButton.Focus();
        };
        return button;
    }

    private Label Header(string text, bool first = false) => new()
    {
        Text = text,
        AutoSize = true,
        Font = _headerFont,
        Margin = new Padding(3, first ? 0 : 12, 3, 4),
    };

    private static Label InlineLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 0, 8, 0),
    };

    private static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
        };
        row.Controls.AddRange(controls);
        return row;
    }
}
