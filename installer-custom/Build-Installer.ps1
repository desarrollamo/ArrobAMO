param([string]$ExpectedVersion = '0.5.3.1')
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$dist=Join-Path $repo "dist-v$ExpectedVersion"
$release=Join-Path $repo "release-v$ExpectedVersion"
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$icon=Join-Path $repo 'native\ArrobAMO.ico'
if (!(Test-Path $compiler)) { throw 'No se encontro el compilador .NET Framework x64.' }
if (!(Test-Path $icon)) { throw 'Falta el logo orbital (.ico).' }
$main=Join-Path $dist 'ArrobAMO.exe'
if (!(Test-Path $main)) { throw "Falta la compilacion de ArrobAMO: $main" }
$version=(Get-Item $main).VersionInfo.FileVersion
if ($version -ne $ExpectedVersion) { throw "La version local es $version y se pidio $ExpectedVersion." }
$setupSource=Get-Content (Join-Path $PSScriptRoot 'Setup.cs') -Raw
if (!$setupSource.Contains($ExpectedVersion)) { throw 'El codigo del instalador tiene otra version.' }
New-Item -Path $release -ItemType Directory -Force | Out-Null
$temp=Join-Path $env:TEMP ("ArrobAMO-InstallerBuild-"+[guid]::NewGuid().ToString('N'))
New-Item -Path $temp -ItemType Directory -Force | Out-Null
try {
    $uninstaller=Join-Path $temp 'ArrobAMOUninstaller.exe'
    & $compiler /nologo /target:winexe /platform:x64 ("/out:"+$uninstaller) ("/win32icon:"+$icon) /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'Uninstall.cs') (Join-Path $PSScriptRoot 'UninstallerAssemblyInfo.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Error de compilacion del desinstalador.' }
    $names=@('ArrobAMO.exe','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll','LICENSE-USER.txt')
    $resources=@()
    foreach($name in $names){
        $file=Join-Path $dist $name
        if(!(Test-Path $file)){throw "Falta el archivo: $name"}
        $resources+=("/resource:"+$file+","+$name)
    }
    $resources+=("/resource:"+$uninstaller+",ArrobAMOUninstaller.exe")
    $installer=Join-Path $release 'ArrobAMOInstaller.exe'
    & $compiler /nologo /target:winexe /platform:x64 ("/out:"+$installer) ("/win32icon:"+$icon) /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll @resources (Join-Path $PSScriptRoot 'Setup.cs') (Join-Path $PSScriptRoot 'InstallerAssemblyInfo.cs')
    if($LASTEXITCODE -ne 0){throw 'Error de compilacion del instalador.'}
    $test=Start-Process -FilePath $installer -ArgumentList '--self-test' -PassThru -Wait
    if($test.ExitCode -ne 0){throw "El instalador fallo al probar su contenido: $($test.ExitCode)"}
    Write-Output "INSTALLER_OK=$installer"
    Write-Output "VERSION=$ExpectedVersion"
    Write-Output "SIZE=$((Get-Item $installer).Length)"
    Write-Output "SHA256=$((Get-FileHash $installer -Algorithm SHA256).Hash)"
}
finally {if(Test-Path $temp){Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue}}
