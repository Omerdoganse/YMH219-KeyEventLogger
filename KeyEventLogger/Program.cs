namespace KeyEventLogger;

static class Program
{
    /// <summary>
    /// Uygulamanin giris noktasi.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Beklenmeyen arayuz hatalarinda uygulama kapanmasin, kullaniciya mesaj gosterilsin.
        Application.ThreadException += (_, e) =>
            MessageBox.Show("Beklenmeyen bir hata olustu:\n" + e.Exception.Message,
                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);

        Application.Run(new LoggerForm());
    }
}
