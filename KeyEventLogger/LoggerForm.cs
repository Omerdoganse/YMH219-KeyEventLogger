namespace KeyEventLogger;

/// <summary>
/// Ana pencere. Tus olaylari yalnizca bu pencere odaktayken yakalanir.
/// </summary>
public class LoggerForm : Form
{
    public LoggerForm()
    {
        Text = "YMH219 - Key Event Logger";
        ClientSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
    }
}
