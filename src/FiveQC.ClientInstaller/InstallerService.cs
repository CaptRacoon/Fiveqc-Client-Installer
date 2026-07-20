using System.Diagnostics;

namespace FiveQC.ClientInstaller;

internal sealed class InstallerService
{
    private readonly GitHubService _gitHubService;

    public InstallerService(GitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    public static bool IsFiveMRunning()
    {
        try
        {
            Process[] processes = Process.GetProcesses();
            try
            {
                foreach (Process process in processes)
                {
                    if (process.ProcessName.Contains("FiveM", StringComparison.OrdinalIgnoreCase) ||
                        process.ProcessName.Contains("CitizenFX", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }
            finally
            {
                foreach (Process process in processes)
                    process.Dispose();
            }
        }
        catch
        {
            return false;
        }
    }

    public async Task<InstallResult> InstallAsync(
        string fiveMRoot,
        RemoteConfig config,
        GitHubRelease release,
        IReadOnlyCollection<InstallSelection> selections,
        IProgress<(int Percent, string Status)> progress,
        CancellationToken cancellationToken)
    {
        string? normalizedRoot = FiveMPathService.Normalize(fiveMRoot);
        if (normalizedRoot is null)
            throw new DirectoryNotFoundException("Le dossier FiveM sélectionné n'est pas valide.");

        List<ClientMod> modsToInstall = selections
            .Where(selection => selection.Selected || selection.Mod.Required)
            .Select(selection => selection.Mod)
            .ToList();

        if (modsToInstall.Count == 0)
            throw new InvalidOperationException("Aucune modification n'a été sélectionnée.");

        string workDirectory = Path.Combine(
            AppConstants.ProductDataDirectory,
            "Temp",
            Guid.NewGuid().ToString("N"));

        string backupDirectory = Path.Combine(
            AppConstants.ProductDataDirectory,
            "Backups",
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

        Directory.CreateDirectory(workDirectory);

        try
        {
            int completed = 0;

            foreach (ClientMod mod in modsToInstall)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string assetName = ExpandTemplate(mod.AssetName, config);
                if (Path.GetFileName(assetName) != assetName)
                    throw new InvalidDataException($"Le nom d'asset '{assetName}' est invalide.");

                GitHubAsset asset = release.Assets.FirstOrDefault(candidate =>
                    candidate.Name.Equals(assetName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new FileNotFoundException(
                        $"Le fichier GitHub Release '{assetName}' est absent de la dernière release.");

                int itemIndex = completed;
                int downloadStart = 2 + (itemIndex * 70 / modsToInstall.Count);
                int downloadEnd = 2 + ((itemIndex + 1) * 70 / modsToInstall.Count);
                int downloadSpan = Math.Max(1, downloadEnd - downloadStart);

                string downloadedFile = GetSafeCombinedPath(workDirectory, asset.Name);
                progress.Report((downloadStart, $"Téléchargement : {mod.Name}"));

                var downloadProgress = new Progress<int>(percent =>
                {
                    int globalPercent = downloadStart + (percent * downloadSpan / 100);
                    progress.Report((globalPercent, $"Téléchargement : {mod.Name} — {percent}%"));
                });

                await _gitHubService.DownloadFileAsync(
                    asset.BrowserDownloadUrl,
                    downloadedFile,
                    downloadProgress,
                    cancellationToken);

                progress.Report((downloadEnd, $"Vérification : {mod.Name}"));
                string? digest = await _gitHubService.ResolveAssetDigestAsync(
                    release,
                    asset,
                    cancellationToken);
                await GitHubService.VerifySha256Async(downloadedFile, digest, cancellationToken);

                string destinationRelative = ExpandTemplate(mod.Destination, config);
                string destination = GetSafeCombinedPath(normalizedRoot, destinationRelative);

                if (File.Exists(destination))
                {
                    string backup = GetSafeCombinedPath(backupDirectory, destinationRelative);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                    File.Copy(destination, backup, overwrite: true);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                string temporaryDestination = destination + ".fiveqc-new";
                File.Copy(downloadedFile, temporaryDestination, overwrite: true);
                File.Move(temporaryDestination, destination, overwrite: true);

                completed++;
                int installPercent = 72 + (int)Math.Round(completed * 27.0 / modsToInstall.Count);
                progress.Report((installPercent, $"Installation : {mod.Name}"));
            }

            progress.Report((100, "Installation terminée."));
            return new InstallResult(completed, backupDirectory);
        }
        finally
        {
            try
            {
                if (Directory.Exists(workDirectory))
                    Directory.Delete(workDirectory, recursive: true);
            }
            catch
            {
                // Un fichier temporaire verrouillé sera nettoyé au prochain passage manuel.
            }
        }
    }

    private static string ExpandTemplate(string value, RemoteConfig config)
    {
        _ = config;
        return value.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string GetSafeCombinedPath(string root, string relative)
    {
        string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string combined = Path.GetFullPath(Path.Combine(rootFull, relative));
        if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Un chemin de la configuration tente de sortir du dossier autorisé.");
        return combined;
    }
}
