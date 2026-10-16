using ElevatorSystem.Forms;

namespace ElevatorSystem;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Last line of defence: show a readable message instead of crashing on an unexpected error.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show($"An unexpected error occurred:{Environment.NewLine}{e.Exception.Message}",
                "Elevator Simulator", MessageBoxButtons.OK, MessageBoxIcon.Error);

        int elevatorCount;
        using (var modeForm = new ModeSelectForm())
        {
            if (modeForm.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            elevatorCount = modeForm.ElevatorCount;
            Theme.Current = modeForm.SelectedTheme;
        }

        Application.Run(new MainForm(elevatorCount));
    }
}
