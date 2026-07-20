namespace FiveQC.ClientInstaller;

internal static class AppConstants
{
    // Modifie seulement ces valeurs si tu renommes ou déplaces le dépôt GitHub.
    public const string GitHubOwner = "CaptRacoon";
    public const string GitHubRepository = "Fiveqc-Client-Installer";
    public const string ConfigBranch = "main";
    public const string ConfigPath = "distribution/config.json";
    public const string InstallerAssetName = "FiveQC-Client-Installer.exe";

    public static string LatestReleaseApiUrl =>
        $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepository}/releases/latest";

    public static string RemoteConfigUrl =>
        $"https://raw.githubusercontent.com/{GitHubOwner}/{GitHubRepository}/{ConfigBranch}/{ConfigPath}";

    public static string ProductDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveQCClientInstaller");
}
