# FiveQC Client Installer

Installateur Windows x64 en C# / .NET 8 pour distribuer les modifications clientes approuvées de FiveQuébec.

## Fichiers distribués

| Mod | Statut | Destination chez le joueur |
|---|---:|---|
| `SirenSetting_Limit_Adjuster.asi` | Obligatoire | `FiveM.app\plugins\SirenSetting_Limit_Adjuster.asi` |
| `OpenCamera.asi` | Facultatif | `FiveM.app\plugins\OpenCamera.asi` |
| `vehshare.ytd` | Obligatoire | `FiveM.app\mods\vehshare.ytd` |

`carcols.ymt` n’est plus distribué et n’est plus installé dans un dossier `platform-*`.

## Fonctions incluses

- Interface sombre inspirée de LSPDFR, avec la bannière FiveQuébec.
- Détection automatique de `FiveM.app` et sélection manuelle au besoin.
- Mods obligatoires et facultatifs gérés par `distribution/config.json` sur GitHub.
- Téléchargement individuel des fichiers depuis la dernière GitHub Release.
- Aucun ZIP de mods : chaque `.asi` et `.ytd` est publié comme asset distinct.
- Vérification SHA-256 de chaque fichier téléchargé.
- Sauvegarde automatique avant remplacement.
- Mise à jour de l’installateur au lancement.
- Exécutable Windows x64 autonome en un seul fichier.

## Structure du Payload

```text
Payload/
├─ plugins/
│  ├─ SirenSetting_Limit_Adjuster.asi
│  └─ OpenCamera.asi
└─ mods/
   └─ vehshare.ytd
```

## Mise à jour d’un fichier client

1. Remplace le fichier correspondant dans `Payload`.
2. Commit et pousse les changements avec GitHub Desktop.
3. Dans GitHub, ouvre **Actions → Build and publish FiveQC installer → Run workflow**.
4. Entre une nouvelle version, par exemple `1.0.4`.
5. Le workflow crée une nouvelle release avec les fichiers individuels et leurs SHA-256.

## Assets de la Release

```text
FiveQC-Client-Installer.exe
FiveQC-Client-Installer.exe.sha256
SirenSetting_Limit_Adjuster.asi
SirenSetting_Limit_Adjuster.asi.sha256
OpenCamera.asi
OpenCamera.asi.sha256
vehshare.ytd
vehshare.ytd.sha256
```

Le joueur télécharge seulement `FiveQC-Client-Installer.exe`.
