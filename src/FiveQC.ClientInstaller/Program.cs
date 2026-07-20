using System.Diagnostics;

namespace FiveQC.ClientInstaller;

internal static class Program
{
    [STAThread]
    private static async Task Main(string[] args)
    {
        if (args.Length >= 3 && args[0].Equals("--apply-update", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyUpdateAsync(args);
            return;
        }

        CleanupOldUpdateFiles();
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static async Task ApplyUpdateAsync(string[] args)
    {
        if (!int.TryParse(args[1], out int oldProcessId))
            return;

        string targetPath = args[2];
        string currentUpdateExe = Environment.ProcessPath ?? Application.ExecutablePath;

        try
        {
            using Process? oldProcess = Process.GetProcessById(oldProcessId);
            await oldProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45));
        }
        catch
        {
            // Le processus est déjà fermé ou ne répond plus. On tente quand même le remplacement.
        }

        Exception? lastError = null;
        for (int attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(currentUpdateExe, targetPath, overwrite: true);
                Process.Start(new ProcessStartInfo
                {
                    FileName = targetPath,
                    Arguments = "--updated",
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(targetPath)!
                });
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                await Task.Delay(500);
            }
        }

        try
        {
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "FiveQC-update-error.txt"), lastError?.ToString());
        }
        catch { }
    }

    private static void CleanupOldUpdateFiles()
    {
        string updatesDirectory = Path.Combine(AppConstants.ProductDataDirectory, "Updates");
        if (!Directory.Exists(updatesDirectory))
            return;

        foreach (string file in Directory.EnumerateFiles(updatesDirectory, "*.exe"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-2))
                    File.Delete(file);
            }
            catch { }
        }
    }
}
