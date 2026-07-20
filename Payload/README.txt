FIVEQUÉBEC - PAYLOAD DES MODIFICATIONS CLIENTES
================================================

Dépose les trois fichiers bruts dans cette structure :

Payload\
├── plugins\
│   ├── SirenSetting_Limit_Adjuster.asi
│   └── OpenCamera.asi
└── carcols\
    └── carcols.ymt

IMPORTANT
---------
Ces fichiers ne sont pas compressés dans un ZIP lors d'une release.
GitHub Actions les publie individuellement comme assets de la release :

- SirenSetting_Limit_Adjuster.asi
- OpenCamera.asi
- carcols.ymt

L'installateur télécharge seulement les fichiers requis ou sélectionnés.
OpenCamera.asi demeure facultatif pour le joueur.

Avec serverBuild = 3751, carcols.ymt est installé uniquement ici :
FiveM.app\citizen\platform-3751\data\carcols.ymt
