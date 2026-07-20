using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace FiveQC.ClientInstaller;

internal static partial class FiveMPathService
{
    public static string? FindAutomatically()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string[] candidates =
        [
            Path.Combine(localAppData, "FiveM", "FiveM.app"),
            Path.Combine(localAppData, "FiveM", "FiveM Application Data"),
            Path.Combine(localAppData, "FiveM")
        ];

        foreach (string candidate in candidates)
        {
            string? normalized = Normalize(candidate);
            if (normalized is not null)
                return normalized;
        }

        return FindFromProtocolRegistry();
    }

    public static string? Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
            if (File.Exists(path))
                path = Path.GetDirectoryName(path)!;

            path = Path.GetFullPath(path);

            string appChild = Path.Combine(path, "FiveM.app");
            if (Directory.Exists(appChild))
                path = appChild;

            if (!Directory.Exists(path))
                return null;

            if (path.EndsWith("FiveM Application Data", StringComparison.OrdinalIgnoreCase))
                return path;

            if (path.EndsWith("FiveM.app", StringComparison.OrdinalIgnoreCase))
                return path;

            bool looksLikeApp = Directory.Exists(Path.Combine(path, "citizen")) ||
                                File.Exists(Path.Combine(path, "CitizenFX.ini"));
            return looksLikeApp ? path : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindFromProtocolRegistry()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\fivem\shell\open\command");
            string? command = key?.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(command))
                return null;

            Match match = QuotedExecutableRegex().Match(command);
            string executable = match.Success ? match.Groups[1].Value : command.Split(' ')[0];
            string? root = Path.GetDirectoryName(executable);
            return root is null ? null : Normalize(root);
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex("\\\"([^\\\"]+\\.exe)\\\"", RegexOptions.IgnoreCase)]
    private static partial Regex QuotedExecutableRegex();
}
