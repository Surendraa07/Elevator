namespace ElevatorSystem.Forms;

/// <summary>Shared look for request and floor buttons, lit (pressed, waiting) or unlit (served), in the current theme.</summary>
public static class ButtonStyle
{
    public static void Apply(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 2;
        button.Cursor = Cursors.Hand;
        SetLit(button, false);
    }

    public static void SetLit(Button button, bool lit)
    {
        Theme theme = Theme.Current;
        button.BackColor = lit ? theme.LitBack : theme.ButtonBack;
        button.FlatAppearance.BorderColor = lit ? theme.LitBorder : theme.ButtonBorder;
        button.ForeColor = lit ? theme.LitText : theme.ButtonText;
    }
}
