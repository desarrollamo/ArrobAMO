$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$repo=Split-Path -Parent $PSScriptRoot
$installer=Join-Path $repo 'release-v0.5.3.1\ArrobAMOInstaller.exe'
if(!(Test-Path $installer)){throw 'Installer file missing'}
$target=Join-Path $env:TEMP ('ArrobAMO-Installer-Integration-'+[guid]::NewGuid().ToString('N'))
$profile="$env:LOCALAPPDATA\ArrobAMO"
$before=@{}
foreach($file in @('bookmarks.txt','history.txt','settings.ini','permissions.txt','session.txt')){
    $p=Join-Path $profile $file
    if(Test-Path $p){$before[$file]=(Get-FileHash $p -Algorithm SHA256).Hash}
}
$key='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ArrobAMO'
$regVersion=if(Test-Path $key){(Get-ItemProperty $key).DisplayVersion}else{'NONE'}
$form=$null
try {
    $assembly=[System.Reflection.Assembly]::LoadFrom($installer)
    $type=$assembly.GetType('ArrobAMOSetup.SetupForm',$true)
    $form=[Activator]::CreateInstance($type)
    $flags=[System.Reflection.BindingFlags]'Instance,NonPublic'
    $parameters = New-Object 'System.Object[]' 1
    $parameters[0] = [string]$target
    $type.GetMethod('SetIsolatedTestDirectory',$flags).Invoke($form,$parameters) | Out-Null
    $form.StartPosition=[System.Windows.Forms.FormStartPosition]::Manual
    $form.Location=[System.Drawing.Point]::new(-2000,-2000)
    $form.ShowInTaskbar=$false
    $form.Opacity=0
    $form.Show()
    $type.GetField('accept',$flags).GetValue($form).Checked=$true
    $type.GetField('launch',$flags).GetValue($form).Checked=$false
    $type.GetField('install',$flags).GetValue($form).PerformClick()
    $progress=$type.GetField('progress',$flags).GetValue($form)
    if($progress.Value -ne 5){throw "Progress incomplete: $($progress.Value)"}
    $names=@('ArrobAMO.exe','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll','LICENSE-USER.txt','ArrobAMOUninstaller.exe')
    foreach($name in $names){if(!(Test-Path (Join-Path $target $name))){throw "Install missing: $name"}}
    $ver=(Get-Item (Join-Path $target 'ArrobAMO.exe')).VersionInfo.FileVersion
    if($ver -ne '0.5.3.1'){throw "Unexpected installed version: $ver"}
    $uninstaller=(Get-Item (Join-Path $target 'ArrobAMOUninstaller.exe')).Length
    if($uninstaller -lt 1000){throw 'Uninstaller invalid'}
    foreach($file in $before.Keys){
        $after=(Get-FileHash (Join-Path $profile $file) -Algorithm SHA256).Hash
        if($after -ne $before[$file]){throw "User profile changed: $file"}
    }
    $regAfter=if(Test-Path $key){(Get-ItemProperty $key).DisplayVersion}else{'NONE'}
    if($regAfter -ne $regVersion){throw 'Integration test altered real registry'}
    Write-Output 'TEST_INSTALLER_GUI=PASS'
    Write-Output 'TEST_INSTALLER_ACTUAL_EXTRACTION=PASS'
    Write-Output 'TEST_USER_PROFILE_UNCHANGED=PASS'
    Write-Output 'TEST_REAL_REGISTRY_UNCHANGED=PASS'
    Write-Output ('TEST_VERSION='+$ver)
    exit 0
}
finally{
    if($form -ne $null){$form.Close();$form.Dispose()}
    if(Test-Path $target){Remove-Item $target -Recurse -Force -ErrorAction Stop}
}
