@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

echo ==================================================
echo       FiveQC Client Installer - Build local
echo ==================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERREUR] Le SDK .NET 8 n'est pas installe.
    echo Telecharge Visual Studio 2022 avec le workload ".NET desktop development"
    echo ou installe le SDK .NET 8, puis relance ce fichier.
    pause
    exit /b 1
)

if not exist "Payload\plugins\SirenSetting_Limit_Adjuster.asi" (
    echo [ERREUR] Payload\plugins\SirenSetting_Limit_Adjuster.asi est manquant.
    pause
    exit /b 1
)

if not exist "Payload\plugins\OpenCamera.asi" (
    echo [ERREUR] Payload\plugins\OpenCamera.asi est manquant. Il est facultatif pour le joueur, mais doit etre publie pour offrir l'option.
    pause
    exit /b 1
)

if not exist "Payload\carcols\carcols.ymt" (
    echo [ERREUR] Payload\carcols\carcols.ymt est manquant.
    pause
    exit /b 1
)

if not exist "Payload\mods\vehshare.ytd" (
    echo [ERREUR] Payload\mods\vehshare.ytd est manquant.
    pause
    exit /b 1
)

set VERSION=1.0.0
if not "%~1"=="" set VERSION=%~1

if exist "artifacts" rmdir /s /q "artifacts"
mkdir "artifacts\publish"

echo [1/3] Publication de l'executable Windows x64...
dotnet publish "src\FiveQC.ClientInstaller\FiveQC.ClientInstaller.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:Version=%VERSION% -o "artifacts\publish"
if errorlevel 1 goto :fail

echo [2/3] Copie des fichiers clients individuels...
copy /y "artifacts\publish\FiveQC-Client-Installer.exe" "artifacts\FiveQC-Client-Installer.exe" >nul
copy /y "Payload\plugins\SirenSetting_Limit_Adjuster.asi" "artifacts\SirenSetting_Limit_Adjuster.asi" >nul
copy /y "Payload\plugins\OpenCamera.asi" "artifacts\OpenCamera.asi" >nul
copy /y "Payload\carcols\carcols.ymt" "artifacts\carcols.ymt" >nul
copy /y "Payload\mods\vehshare.ytd" "artifacts\vehshare.ytd" >nul
if errorlevel 1 goto :fail

echo [3/3] Generation des SHA-256...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$files = @('artifacts/FiveQC-Client-Installer.exe','artifacts/SirenSetting_Limit_Adjuster.asi','artifacts/OpenCamera.asi','artifacts/carcols.ymt','artifacts/vehshare.ytd'); foreach ($file in $files) { $name = Split-Path $file -Leaf; $hash = (Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant(); ('{0}  {1}' -f $hash, $name) | Set-Content ($file + '.sha256') -NoNewline }"
if errorlevel 1 goto :fail

echo.
echo Build termine dans : %CD%\artifacts
echo Aucun ZIP de mods n'a ete cree.
pause
exit /b 0

:fail
echo.
echo [ERREUR] Le build a echoue.
pause
exit /b 1
