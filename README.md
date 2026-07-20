# FiveQC Client Installer

Installateur Windows x64 en C# / .NET 8 pour distribuer les modifications clientes approuvées de FiveQuébec.

## Fonctions incluses

- Interface sombre inspirée de LSPDFR, avec la bannière fournie.
- Détection automatique de `FiveM.app` et sélection manuelle au besoin.
- Mods obligatoires et facultatifs gérés par `distribution/config.json` sur GitHub.
- Build du serveur défini à distance dans la configuration GitHub.
- Téléchargement individuel des fichiers depuis la dernière GitHub Release.
- Aucun ZIP de mods : chaque `.asi` et `.ymt` est publié comme asset distinct.
- Vérification SHA-256 de chaque fichier téléchargé.
- Sauvegarde automatique avant remplacement.
- Mise à jour automatique de l'installateur au lancement.
- Exécutable Windows x64 autonome en un seul fichier.

## Emplacements configurés

| Mod | Statut | Destination |
|---|---:|---|
| `SirenSetting_Limit_Adjuster.asi` | Obligatoire | `plugins\SirenSetting_Limit_Adjuster.asi` |
| `OpenCamera.asi` | Facultatif | `plugins\OpenCamera.asi` |
| `carcols.ymt` | Obligatoire | `citizen\platform-{build}\data\carcols.ymt` |

## Dépôt GitHub

Le code est préconfiguré pour :

- propriétaire : `CaptRacoon`
- dépôt : `Fiveqc-Client-Installer`
- branche de configuration : `main`

Ces valeurs se trouvent dans `src/FiveQC.ClientInstaller/AppConstants.cs`.
Le dépôt doit être public pour que les joueurs puissent lire la configuration et télécharger les releases sans jeton GitHub intégré.

## Contenu du dossier Payload

```text
Payload/
├─ plugins/
│  ├─ SirenSetting_Limit_Adjuster.asi
│  └─ OpenCamera.asi
└─ carcols/
   └─ carcols.ymt
```

Les fichiers restent bruts dans le dépôt. Le workflow ne crée pas de `FiveQC-Client-Mods.zip`.

## Build serveur et destination de carcols.ymt

Dans `distribution/config.json` :

```json
"serverBuild": 3751,
"platformFolderTemplate": "platform-{build}"
```

Avec cette valeur, `carcols.ymt` est installé uniquement ici :

```text
FiveM.app\citizen\platform-3751\data\carcols.ymt
```

Lors d'un changement de build, modifie seulement `serverBuild`.

## Première publication

1. Copie tout le projet dans le dépôt GitHub.
2. Vérifie que les trois fichiers sont présents dans `Payload`.
3. Modifie l'URL Discord dans `distribution/config.json`.
4. Commit et pousse sur `main`.
5. Crée et pousse un tag :

```bat
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions crée une release contenant ces assets individuels :

```text
FiveQC-Client-Installer.exe
FiveQC-Client-Installer.exe.sha256
SirenSetting_Limit_Adjuster.asi
SirenSetting_Limit_Adjuster.asi.sha256
OpenCamera.asi
OpenCamera.asi.sha256
carcols.ymt
carcols.ymt.sha256
```

Il n'y a aucun ZIP de modifications clientes.

## Mise à jour

Pour modifier les fichiers clients ou l'installateur :

1. Remplace le ou les fichiers dans `Payload`.
2. Commit et pousse les changements.
3. Crée un nouveau tag supérieur, par exemple `v1.0.1`.
4. GitHub Actions publie les nouveaux fichiers bruts dans la release.

L'installateur télécharge uniquement les mods obligatoires et les options sélectionnées par le joueur.

## Build local

Installe Visual Studio 2022 avec **.NET desktop development** ou le SDK .NET 8, puis lance :

```bat
build.bat 1.0.0
```

Le résultat se trouve dans `artifacts`. Le script local copie aussi les trois fichiers individuellement et génère leurs SHA-256, sans créer de ZIP.
