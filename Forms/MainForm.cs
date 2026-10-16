using System.ComponentModel;
using ElevatorSystem.Data;
using ElevatorSystem.Models;
using ElevatorSystem.Observers;

namespace ElevatorSystem.Forms;

/// <summary>
/// The building: one request button and one status display per floor, and for each elevator a shaft
/// view and a car control panel. The form only wires things together; the elevators, states,
/// dispatcher and observers do the work.
/// </summary>
public sealed class MainForm : Form
{
    private const int FloorHeight = 230;
    private const int TopOffset = 16;
    private const int BuildingColumnWidth = 230;
    private const int ShaftWidth = 250;
    private const int PanelWidth = 180;

    private readonly Theme _theme = Theme.Current;
    private readonly ElevatorLogRepository _repository = new();
    private readonly BackgroundLogWriter _logWriter;
    private readonly List<Elevator> _elevators = new();
    private readonly Dispatcher _dispatcher;
    private readonly Dictionary<int, Button> _hallButtons = new();
    private readonly Dictionary<int, Label> _floorDisplays = new();
    private readonly List<CarPanelControl> _carPanels = new();
    private readonly ToolStripStatusLabel _statusLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _dbLabel = new() { Text = "Database: starting…" };

    private LogForm? _logForm;
    private bool _logErrorReported;
    private bool _closeWhenLogSaved;

    public MainForm(int elevatorCount)
    {
        string mode = elevatorCount == 1 ? "single elevator" : "two elevators";
        Text = _theme.IsSpace ? $"Elevator Simulator – Space Station – {mode}" : $"Elevator Simulator – {mode}";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = _theme.UiFont(9F);
        BackColor = _theme.FormBack;
        ForeColor = _theme.Text;

        for (int id = 1; id <= elevatorCount; id++)
        {
            _elevators.Add(new Elevator(id));
        }

        // The dispatcher attaches itself first, so a served hall call resets before the displays redraw.
        _dispatcher = new Dispatcher(_elevators);
        _dispatcher.HallCallChanged += OnHallCallChanged;

        _logWriter = new BackgroundLogWriter(_repository);
        _logWriter.WriteFailed += OnLogWriteFailed;
        _logWriter.EntriesWritten += OnLogEntriesWritten;

        BuildLayout();
        AttachObservers();

        _logWriter.Enqueue(new LogEntry(DateTime.Now, 0, 0,
            $"System: simulator started in {(elevatorCount == 1 ? "single" : "two")}-elevator mode"));

        Load += OnFormLoad;
        FormClosing += OnFormClosing;
        FormClosed += OnFormClosed;
    }

    private void BuildLayout()
    {
        int floorCount = ElevatorSettings.FloorCount;
        int shaftHeight = floorCount * FloorHeight + 20;

        // Building column: one group per floor, aligned with that floor in the shafts. Top floor first.
        for (int floor = floorCount - 1; floor >= 0; floor--)
        {
            int rowTop = TopOffset + 10 + (floorCount - 1 - floor) * FloorHeight;
            var group = new GroupBox
            {
                Text = $"Floor {floor}",
                Bounds = new Rectangle(12, rowTop + 8, BuildingColumnWidth - 12, FloorHeight - 24),
                Font = _theme.UiFont(9.5F, FontStyle.Bold)
            };
            _theme.StylePanel(group);

            int requestFloor = floor;
            var requestButton = new Button
            {
                Text = floor == 0 ? "▲  Request elevator" : "▼  Request elevator",
                Bounds = new Rectangle(14, 28, group.Width - 28, 44),
                Font = _theme.UiFont(10F, FontStyle.Bold)
            };
            ButtonStyle.Apply(requestButton);
            requestButton.Click += (_, _) => OnRequestButtonClick(requestFloor);

            var caption = new Label
            {
                Text = "Elevator location",
                Bounds = new Rectangle(14, 82, group.Width - 28, 18),
                Font = _theme.UiFont(8F),
                BackColor = Color.Transparent,
                ForeColor = _theme.MutedText
            };
            var display = new Label
            {
                Bounds = new Rectangle(14, 102, group.Width - 28, 70),
                BackColor = _theme.DisplayBack,
                ForeColor = _theme.DisplayText,
                Font = new Font("Consolas", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };

            group.Controls.AddRange(new Control[] { requestButton, caption, display });
            Controls.Add(group);
            _hallButtons[floor] = requestButton;
            _floorDisplays[floor] = display;
        }

        // One shaft and one car panel per elevator, side by side.
        int x = 12 + BuildingColumnWidth + 10;
        foreach (Elevator elevator in _elevators)
        {
            var shaft = new ElevatorShaftControl(elevator)
            {
                Bounds = new Rectangle(x, TopOffset, ShaftWidth, shaftHeight)
            };
            Controls.Add(shaft);

            var panel = new CarPanelControl(elevator.Id, floorCount);
            panel.Location = new Point(x + ShaftWidth + 10, TopOffset + (shaftHeight - panel.Height) / 2);
            panel.Width = PanelWidth;
            Elevator owner = elevator;
            panel.FloorButtonClicked += (_, floor) => OnCarFloorButtonClick(owner, floor);
            Controls.Add(panel);
            _carPanels.Add(panel);

            x += ShaftWidth + 10 + PanelWidth + 20;
        }

        int bottom = TopOffset + shaftHeight + 12;
        var logButton = new Button
        {
            Text = "Log",
            Bounds = new Rectangle(12, bottom, 130, 36),
            Font = _theme.UiFont(10F, FontStyle.Bold)
        };
        if (_theme.IsSpace)
        {
            ButtonStyle.Apply(logButton);
        }

        logButton.Click += OnLogButtonClick;
        Controls.Add(logButton);

        var statusStrip = new StatusStrip { SizingGrip = false };
        if (_theme.IsSpace)
        {
            // The system renderer paints BackColor, unlike the default professional renderer's gradient.
            statusStrip.RenderMode = ToolStripRenderMode.System;
            statusStrip.BackColor = Color.FromArgb(14, 20, 36);
            statusStrip.ForeColor = _theme.Text;
        }
        statusStrip.Items.Add(_statusLabel);
        statusStrip.Items.Add(_dbLabel);
        Controls.Add(statusStrip);

        ClientSize = new Size(x - 8, bottom + 36 + 12 + statusStrip.Height);

        if (_theme.IsSpace)
        {
            // Space outside the station: drawn once, the translucent panels sit on top of it.
            BackgroundImage = SpaceBackdrop.CreateStarfield(ClientSize, seed: 7, withPlanet: true);
            BackgroundImageLayout = ImageLayout.None;
        }
    }

    private void AttachObservers()
    {
        // Logger: every event of every elevator becomes a row in the database.
        _ = new LogObserver(_logWriter, _elevators);

        // Floor displays: each shows where every elevator is.
        foreach ((int floor, Label display) in _floorDisplays)
        {
            _ = new FloorDisplayObserver(display, floor, _elevators);
        }

        // Car panels: each follows only its own elevator.
        for (int i = 0; i < _elevators.Count; i++)
        {
            _ = new CarPanelObserver(_carPanels[i], _elevators[i]);
        }
    }

    private void OnFormLoad(object? sender, EventArgs e)
    {
        // Create or check the database on a background thread; the simulator is usable meanwhile.
        var worker = new BackgroundWorker();
        worker.DoWork += (_, args) => args.Result = _repository.EnsureDatabase();
        worker.RunWorkerCompleted += (_, args) =>
        {
            worker.Dispose();

            // Start writing even after a failure: the writer keeps entries and retries, e.g. once the file
            // is unlocked. Started before any message box, which would otherwise hold it up until dismissed.
            _logWriter.Start();

            if (args.Error != null)
            {
                ReportLogError(args.Error);
            }
            else
            {
                bool created = (bool)args.Result!;
                _dbLabel.Text = $"Database: {_repository.DisplayPath}{(created ? " (created)" : string.Empty)}";
            }
        };
        worker.RunWorkerAsync();
    }

    private void OnRequestButtonClick(int floor)
    {
        Elevator assigned = _dispatcher.HandleHallRequest(floor);
        _statusLabel.Text = $"Request at floor {floor} → {assigned.Name}";
    }

    private void OnCarFloorButtonClick(Elevator elevator, int floor)
    {
        // Car requests bypass the dispatcher: only this car serves them.
        elevator.RequestFloor(floor, RequestSource.Car);
        _statusLabel.Text = $"{elevator.Name}: floor {floor} selected";
    }

    private void OnHallCallChanged(object? sender, HallCallEventArgs e)
    {
        if (_hallButtons.TryGetValue(e.Floor, out Button? button))
        {
            ButtonStyle.SetLit(button, e.IsActive);
        }
    }

    private void OnLogButtonClick(object? sender, EventArgs e)
    {
        if (_logForm == null || _logForm.IsDisposed)
        {
            // Modeless, so the elevators can be watched and used while the log is open.
            _logForm = new LogForm(_repository, _logWriter);
            _logForm.Show(this);
        }
        else
        {
            _logForm.Activate();
            _logForm.ReloadLog();
        }
    }

    private void OnLogWriteFailed(object? sender, DataAccessException error) => ReportLogError(error);

    private void OnLogEntriesWritten(object? sender, int count)
    {
        if (_logErrorReported)
        {
            _logErrorReported = false;
            _dbLabel.Text = $"Database: {_repository.DisplayPath} (logging resumed)";
        }
    }

    /// <summary>Shows a database problem once in a message box; repeats of it only go to the status bar.</summary>
    private void ReportLogError(Exception error)
    {
        string message = error is DataAccessException ? error.Message : $"Database error: {error.Message}";
        _dbLabel.Text = "Database: error – events are kept and retried";
        _statusLabel.Text = message;

        if (_logErrorReported || _closeWhenLogSaved)
        {
            return;
        }

        _logErrorReported = true;
        MessageBox.Show(this,
            message + Environment.NewLine + Environment.NewLine + "The elevators keep working. Events are kept in memory and saved when the database is available again.",
            "Logging problem", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closeWhenLogSaved)
        {
            return;
        }

        // Save the remaining log entries (on the background worker) before the window really closes.
        _closeWhenLogSaved = true;
        e.Cancel = true;
        Text += " – saving log…";
        Enabled = false;
        _logWriter.Enqueue(new LogEntry(DateTime.Now, 0, 0, "System: simulator closed"));
        _logWriter.FlushThen(() => BeginInvoke(Close));
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        foreach (Elevator elevator in _elevators)
        {
            elevator.Dispose();
        }

        _logWriter.Dispose();
    }
}
