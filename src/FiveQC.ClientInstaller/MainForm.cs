using System.Diagnostics;
using System.Reflection;

namespace FiveQC.ClientInstaller;

internal sealed class MainForm : Form
{
    private readonly GitHubService _gitHubService = new();
    private readonly InstallerService _installerService;
    private readonly CancellationTokenSource _lifetimeCts = new();

    private RemoteConfig? _config;
    private GitHubRelease? _latestRelease;

    private readonly TextBox _pathTextBox;
    private readonly FlowLayoutPanel _modsPanel;
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly Label _buildLabel;
    private readonly Label _versionLabel;
    private readonly Button _installButton;
    private readonly Button _browseButton;
    private readonly Button _supportButton;
    private readonly Dictionary<string, CheckBox> _modCheckboxes = new(StringComparer.OrdinalIgnoreCase);

    public MainForm()
    {
        _installerService = new InstallerService(_gitHubService);

        Text = "FiveQuébec — Installation des modifications clientes";
        Width = 1040;
        Height = 760;
        MinimumSize = new Size(900, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.5f);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Background,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 215));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        Controls.Add(root);

        HeaderPanel header = BuildHeader();
        root.Controls.Add(header, 0, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(24, 18, 24, 12),
            BackColor = Theme.Background
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        root.Controls.Add(content, 0, 1);

        Panel leftCard = CreateCard();
        leftCard.Margin = new Padding(0, 0, 12, 0);
        content.Controls.Add(leftCard, 0, 0);

        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20),
            BackColor = Theme.Surface
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftCard.Controls.Add(leftLayout);

        leftLayout.Controls.Add(CreateSectionTitle("DOSSIER FIVEM", "L'installateur tente de le détecter automatiquement."), 0, 0);

        var pathRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(0, 10, 0, 10),
            BackColor = Theme.Surface
        };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
        _pathTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.SurfaceAlt,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f),
            Margin = new Padding(0, 2, 10, 2)
        };
        _browseButton = Theme.CreateButton("Parcourir…");
        _browseButton.Dock = DockStyle.Fill;
        _browseButton.Click += BrowseButton_Click;
        pathRow.Controls.Add(_pathTextBox, 0, 0);
        pathRow.Controls.Add(_browseButton, 1, 0);
        leftLayout.Controls.Add(pathRow, 0, 1);

        leftLayout.Controls.Add(CreateSectionTitle("MODIFICATIONS", "Les éléments obligatoires ne peuvent pas être décochés."), 0, 2);

        _modsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 10, 0, 0)
        };
        leftLayout.Controls.Add(_modsPanel, 0, 3);

        Panel rightCard = CreateCard();
        rightCard.Margin = new Padding(12, 0, 0, 0);
        content.Controls.Add(rightCard, 1, 0);

        var infoLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(20),
            BackColor = Theme.Surface
        };
        infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        infoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        rightCard.Controls.Add(infoLayout);

        infoLayout.Controls.Add(CreateSectionTitle("ÉTAT DU CLIENT", "Configuration et versions chargées depuis GitHub."), 0, 0);

        _versionLabel = CreateInfoLabel("Installateur", $"v{GitHubService.GetCurrentVersion().ToString(3)}");
        _buildLabel = CreateInfoLabel("Build serveur", "Chargement…");
        infoLayout.Controls.Add(_versionLabel, 0, 2);
        infoLayout.Controls.Add(_buildLabel, 0, 3);
        infoLayout.Controls.Add(CreateInfoLabel("Canal", "Release stable GitHub"), 0, 4);

        var note = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ForeColor = Theme.MutedText,
            Font = new Font("Segoe UI", 9.2f),
            Text = "Une sauvegarde des fichiers remplacés est créée avant chaque installation. Ferme FiveM avant de commencer.",
            Padding = new Padding(0, 18, 0, 0)
        };
        infoLayout.Controls.Add(note, 0, 5);

        _supportButton = Theme.CreateButton("Aide / Discord");
        _supportButton.Dock = DockStyle.Fill;
        _supportButton.Enabled = false;
        _supportButton.Click += (_, _) => OpenSupportUrl();
        infoLayout.Controls.Add(_supportButton, 0, 7);

        Panel footer = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(17, 19, 22),
            Padding = new Padding(24, 13, 24, 13)
        };
        root.Controls.Add(footer, 0, 2);

        var footerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = footer.BackColor
        };
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        footer.Controls.Add(footerLayout);

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Theme.MutedText,
            Text = "Initialisation…",
            TextAlign = ContentAlignment.MiddleLeft
        };
        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous,
            Value = 0,
            Margin = new Padding(0, 2, 0, 0)
        };
        _installButton = Theme.CreateButton("INSTALLER / RÉPARER", primary: true);
        _installButton.Dock = DockStyle.Fill;
        _installButton.Enabled = false;
        _installButton.Click += InstallButton_Click;

        footerLayout.Controls.Add(_statusLabel, 0, 0);
        footerLayout.SetColumnSpan(_statusLabel, 1);
        footerLayout.Controls.Add(_progressBar, 0, 1);
        footerLayout.Controls.Add(_installButton, 2, 0);
        footerLayout.SetRowSpan(_installButton, 2);

        Shown += MainForm_Shown;
        FormClosing += (_, _) => _lifetimeCts.Cancel();
    }

    private HeaderPanel BuildHeader()
    {
        var header = new HeaderPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(30, 24, 30, 18)
        };

        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream("FiveQC.ClientInstaller.Assets.banner.jpg");
        if (stream is not null)
        {
            using Image loaded = Image.FromStream(stream);
            header.BannerImage = new Bitmap(loaded);
        }

        var overlayLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        overlayLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        overlayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        overlayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        overlayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.Controls.Add(overlayLayout);

        var eyebrow = new Label
        {
            AutoSize = true,
            Text = "FIVEQUÉBEC  •  CLIENT MOD MANAGER",
            ForeColor = Theme.Accent,
            Font = new Font("Segoe UI Semibold", 10f),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 4)
        };
        var title = new Label
        {
            AutoSize = true,
            Text = "Installation des modifications clientes",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 24f),
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        var subtitle = new Label
        {
            AutoSize = true,
            Text = "Installation guidée, sauvegarde automatique et mise à jour par GitHub.",
            ForeColor = Color.FromArgb(215, 219, 223),
            Font = new Font("Segoe UI", 10.5f),
            BackColor = Color.Transparent,
            Margin = new Padding(2, 5, 0, 0)
        };
        overlayLayout.Controls.Add(eyebrow, 0, 1);
        overlayLayout.Controls.Add(title, 0, 2);
        overlayLayout.Controls.Add(subtitle, 0, 3);
        return header;
    }

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        _pathTextBox.Text = FiveMPathService.FindAutomatically() ?? string.Empty;
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            SetBusy(true, "Connexion à GitHub…");
            Task<RemoteConfig> configTask = _gitHubService.GetRemoteConfigAsync(_lifetimeCts.Token);
            Task<GitHubRelease> releaseTask = _gitHubService.GetLatestReleaseAsync(_lifetimeCts.Token);
            await Task.WhenAll(configTask, releaseTask);

            _config = configTask.Result;
            _latestRelease = releaseTask.Result;
            ValidateRemoteConfig(_config);

            _buildLabel.Text = $"Build serveur\r\n{_config.ServerBuild}  ({_config.PlatformFolderTemplate.Replace("{build}", _config.ServerBuild.ToString())})";
            _supportButton.Enabled = Uri.TryCreate(_config.SupportUrl, UriKind.Absolute, out _);
            PopulateMods(_config.Mods);
            _installButton.Enabled = true;
            SetBusy(false, "Prêt à installer.");

            await CheckForApplicationUpdateAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SetBusy(false, "Impossible de charger la configuration GitHub.");
            MessageBox.Show(
                this,
                $"L'installateur n'a pas pu charger sa configuration.\n\n{ex.Message}",
                "FiveQC — Erreur de connexion",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task CheckForApplicationUpdateAsync()
    {
        if (_latestRelease is null || !GitHubService.IsReleaseNewer(_latestRelease.TagName))
            return;

        DialogResult result = MessageBox.Show(
            this,
            $"Une nouvelle version de l'installateur est disponible ({_latestRelease.TagName}).\n\nVoulez-vous la télécharger et redémarrer maintenant?",
            "Mise à jour disponible",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (result != DialogResult.Yes)
        {
            _statusLabel.Text = $"Mise à jour {_latestRelease.TagName} disponible — installation reportée.";
            return;
        }

        GitHubAsset? asset = _latestRelease.Assets.FirstOrDefault(a =>
            a.Name.Equals(AppConstants.InstallerAssetName, StringComparison.OrdinalIgnoreCase));
        if (asset is null)
        {
            MessageBox.Show(this, "La release GitHub ne contient pas l'exécutable attendu.", "Mise à jour", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            SetBusy(true, "Téléchargement de la mise à jour…");
            string updateDirectory = Path.Combine(AppConstants.ProductDataDirectory, "Updates");
            Directory.CreateDirectory(updateDirectory);
            string updatePath = Path.Combine(updateDirectory, $"FiveQC-Client-Installer-{_latestRelease.TagName}.exe");
            var progress = new Progress<int>(p =>
            {
                _progressBar.Value = p;
                _statusLabel.Text = $"Téléchargement de la mise à jour… {p}%";
            });

            await _gitHubService.DownloadFileAsync(asset.BrowserDownloadUrl, updatePath, progress, _lifetimeCts.Token);
            string? installerDigest = await _gitHubService.ResolveAssetDigestAsync(_latestRelease, asset, _lifetimeCts.Token);
            await GitHubService.VerifySha256Async(updatePath, installerDigest, _lifetimeCts.Token);

            string currentPath = Environment.ProcessPath ?? Application.ExecutablePath;
            Process.Start(new ProcessStartInfo
            {
                FileName = updatePath,
                Arguments = $"--apply-update {Environment.ProcessId} \"{currentPath}\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(updatePath)!
            });
            Close();
        }
        catch (Exception ex)
        {
            SetBusy(false, "Échec de la mise à jour.");
            MessageBox.Show(this, ex.Message, "Mise à jour impossible", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PopulateMods(IEnumerable<ClientMod> mods)
    {
        _modsPanel.Controls.Clear();
        _modCheckboxes.Clear();

        foreach (ClientMod mod in mods)
        {
            var card = new Panel
            {
                Width = Math.Max(430, _modsPanel.ClientSize.Width - 28),
                Height = 86,
                BackColor = Theme.SurfaceAlt,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(14, 10, 14, 10)
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = card.BackColor
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(layout);

            var name = new Label
            {
                Dock = DockStyle.Fill,
                Text = mod.Required ? $"{mod.Name}   • OBLIGATOIRE" : mod.Name,
                ForeColor = mod.Required ? Theme.Accent : Theme.Text,
                Font = new Font("Segoe UI Semibold", 10f),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var description = new Label
            {
                Dock = DockStyle.Fill,
                Text = mod.Description,
                ForeColor = Theme.MutedText,
                Font = new Font("Segoe UI", 8.8f),
                AutoEllipsis = true,
                TextAlign = ContentAlignment.TopLeft
            };
            var checkbox = new CheckBox
            {
                Dock = DockStyle.Fill,
                Checked = mod.Required,
                Enabled = !mod.Required,
                FlatStyle = FlatStyle.Flat,
                Cursor = mod.Required ? Cursors.Default : Cursors.Hand
            };
            layout.Controls.Add(name, 0, 0);
            layout.Controls.Add(description, 0, 1);
            layout.Controls.Add(checkbox, 1, 0);
            layout.SetRowSpan(checkbox, 2);

            _modCheckboxes[mod.Id] = checkbox;
            _modsPanel.Controls.Add(card);
        }
    }

    private async void InstallButton_Click(object? sender, EventArgs e)
    {
        if (_config is null || _latestRelease is null)
            return;

        string? root = FiveMPathService.Normalize(_pathTextBox.Text);
        if (root is null)
        {
            MessageBox.Show(this, "Sélectionne le dossier FiveM.app valide.", "Dossier FiveM", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (InstallerService.IsFiveMRunning())
        {
            MessageBox.Show(this, "Ferme complètement FiveM avant d'installer les modifications.", "FiveM est ouvert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        List<InstallSelection> selections = _config.Mods
            .Select(mod => new InstallSelection(mod, mod.Required || (_modCheckboxes.TryGetValue(mod.Id, out CheckBox? box) && box.Checked)))
            .ToList();

        try
        {
            SetBusy(true, "Préparation de l'installation…");
            var progress = new Progress<(int Percent, string Status)>(state =>
            {
                _progressBar.Value = Math.Clamp(state.Percent, 0, 100);
                _statusLabel.Text = state.Status;
            });

            InstallResult result = await _installerService.InstallAsync(
                root,
                _config,
                _latestRelease,
                selections,
                progress,
                _lifetimeCts.Token);

            SetBusy(false, $"Installation terminée — {result.InstalledCount} fichier(s) installé(s).");
            MessageBox.Show(
                this,
                $"Installation terminée avec succès.\n\nFichiers installés : {result.InstalledCount}\nSauvegarde : {result.BackupDirectory}",
                "FiveQC — Terminé",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            SetBusy(false, "Installation annulée.");
        }
        catch (Exception ex)
        {
            SetBusy(false, "L'installation a échoué.");
            MessageBox.Show(this, ex.Message, "Erreur d'installation", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Sélectionne le dossier FiveM.app (ou le dossier FiveM qui le contient)",
            ShowNewFolderButton = false,
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_pathTextBox.Text) ? _pathTextBox.Text : string.Empty
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _pathTextBox.Text = FiveMPathService.Normalize(dialog.SelectedPath) ?? dialog.SelectedPath;
    }

    private void OpenSupportUrl()
    {
        if (_config?.SupportUrl is not { Length: > 0 } url)
            return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }

    private void SetBusy(bool busy, string status)
    {
        _installButton.Enabled = !busy && _config is not null;
        _browseButton.Enabled = !busy;
        _modsPanel.Enabled = !busy;
        _statusLabel.Text = status;
        if (!busy && _progressBar.Value != 100)
            _progressBar.Value = 0;
        UseWaitCursor = busy;
    }

    private static void ValidateRemoteConfig(RemoteConfig config)
    {
        if (config.SchemaVersion != 2)
            throw new InvalidDataException($"Version de configuration non supportée : {config.SchemaVersion}.");
        if (config.ServerBuild <= 0)
            throw new InvalidDataException("Le numéro de build serveur est invalide.");
        if (config.Mods.Count == 0)
            throw new InvalidDataException("La configuration ne contient aucune modification.");
        if (config.Mods.Any(mod => string.IsNullOrWhiteSpace(mod.Id) || string.IsNullOrWhiteSpace(mod.AssetName) || string.IsNullOrWhiteSpace(mod.Destination)))
            throw new InvalidDataException("Une entrée de modification est incomplète.");
    }

    private static Panel CreateCard()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };
        return panel;
    }

    private static Control CreateSectionTitle(string title, string subtitle)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Surface,
            Margin = Padding.Empty
        };
        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = title,
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI Semibold", 11f),
            Margin = Padding.Empty
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = subtitle,
            ForeColor = Theme.MutedText,
            Font = new Font("Segoe UI", 8.8f),
            Margin = new Padding(0, 3, 0, 0)
        }, 0, 1);
        return layout;
    }

    private static Label CreateInfoLabel(string title, string value)
    {
        return new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 58,
            Text = $"{title}\r\n{value}",
            ForeColor = Theme.Text,
            BackColor = Theme.SurfaceAlt,
            Font = new Font("Segoe UI", 9.4f),
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 0, 0, 9)
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
            _gitHubService.Dispose();
        }
        base.Dispose(disposing);
    }
}
