using System.ComponentModel;
using System.Data;
using ElevatorSystem.Data;

namespace ElevatorSystem.Forms;

/// <summary>
/// Shows the operation history in a DataGridView. The table is loaded on a BackgroundWorker and bound
/// in RunWorkerCompleted, on the UI thread.
/// </summary>
public sealed class LogForm : Form
{
    private readonly ElevatorLogRepository _repository;
    private readonly BackgroundLogWriter _logWriter;
    private readonly BackgroundWorker _loader = new();
    private readonly DataGridView _grid;
    private readonly Button _refreshButton;
    private readonly Label _statusLabel;

    // True from the moment a reload is requested until the grid is bound, including the flush wait.
    private bool _loading;

    public LogForm(ElevatorLogRepository repository, BackgroundLogWriter logWriter)
    {
        _repository = repository;
        _logWriter = logWriter;

        Text = "Elevator operation log";
        Size = new Size(820, 520);
        MinimumSize = new Size(500, 300);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);

        _refreshButton = new Button { Text = "Refresh", Bounds = new Rectangle(10, 8, 90, 30) };
        _refreshButton.Click += (_, _) => ReloadLog();
        _statusLabel = new Label { AutoSize = true, Location = new Point(112, 14) };

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 46 };
        toolbar.Controls.Add(_refreshButton);
        toolbar.Controls.Add(_statusLabel);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window
        };

        ApplyTheme(Theme.Current, toolbar);
        Controls.Add(_grid);
        Controls.Add(toolbar);

        _loader.DoWork += (_, e) => e.Result = _repository.LoadLogs();
        _loader.RunWorkerCompleted += OnLogLoaded;
        Shown += (_, _) => ReloadLog();
    }

    /// <summary>Saves any log entries still queued, then reloads the grid from the database.</summary>
    public void ReloadLog()
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        _refreshButton.Enabled = false;
        _statusLabel.Text = "Loading…";

        // Flush first so the grid includes the events that just happened.
        _logWriter.FlushThen(() =>
        {
            if (!IsDisposed)
            {
                _loader.RunWorkerAsync();
            }
        });
    }

    private void OnLogLoaded(object? sender, RunWorkerCompletedEventArgs e)
    {
        _loading = false;
        if (IsDisposed)
        {
            return;
        }

        _refreshButton.Enabled = true;

        if (e.Error != null)
        {
            string message = e.Error is DataAccessException ? e.Error.Message : $"Could not load the operation log. {e.Error.Message}";
            _statusLabel.Text = "The log could not be loaded.";
            MessageBox.Show(this, message, "Operation log", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var table = (DataTable)e.Result!;
        _grid.DataSource = table;
        FormatColumns();
        _statusLabel.Text = $"{table.Rows.Count} entries (newest first) – {_repository.DisplayName}";
    }

    private void ApplyTheme(Theme theme, Panel toolbar)
    {
        if (!theme.IsSpace)
        {
            return;
        }

        Font = theme.UiFont(9F);
        BackColor = theme.FormBack;
        ForeColor = theme.Text;
        toolbar.BackColor = theme.GridHeaderBack;
        ButtonStyle.Apply(_refreshButton);

        // Custom header colours only show when the visual-styles header renderer is off.
        _grid.EnableHeadersVisualStyles = false;
        _grid.BackgroundColor = theme.GridBack;
        _grid.GridColor = theme.GridLines;
        _grid.BorderStyle = BorderStyle.None;
        _grid.DefaultCellStyle.BackColor = theme.GridBack;
        _grid.DefaultCellStyle.ForeColor = theme.GridText;
        _grid.DefaultCellStyle.SelectionBackColor = theme.GridSelection;
        _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(16, 23, 40);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = theme.GridHeaderBack;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = theme.HeadingText;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = theme.GridHeaderBack;
        _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
    }

    private void FormatColumns()
    {
        SetColumn("Id", "Id", 8);
        SetColumn("EventTime", "Time", 22, "yyyy-MM-dd HH:mm:ss.fff");
        SetColumn("ElevatorId", "Elevator", 10);
        SetColumn("Floor", "Floor", 8);
        SetColumn("Status", "Event / status", 70);
    }

    private void SetColumn(string name, string header, float fillWeight, string? format = null)
    {
        DataGridViewColumn? column = _grid.Columns[name];
        if (column == null)
        {
            return;
        }

        column.HeaderText = header;
        column.FillWeight = fillWeight;
        if (format != null)
        {
            column.DefaultCellStyle.Format = format;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loader.Dispose();
        }

        base.Dispose(disposing);
    }
}
