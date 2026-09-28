# Integration check of the real executable's per-user installer. Leaves the
# verified menu installed at the release executable's current path.
$ErrorActionPreference = 'Stop'
$executable = Join-Path $PSScriptRoot 'Compiled Executables\SolidEdgeConvert.exe'
$menuPath = 'Software\Classes\SystemFileAssociations\.par\shell\SolidEdgeMacros.Convert'
$user = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser, [Microsoft.Win32.RegistryView]::Registry64)
function Invoke-Installer([string] $argument) {
    $process = Start-Process -FilePath $executable -ArgumentList $argument -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Menu command failed: $argument" }
}
function Test-InstalledMenu {
    $menu = $user.OpenSubKey($menuPath)
    if ($null -eq $menu) { throw 'Convert menu was not registered.' }
    try {
        if ($menu.GetValue('MUIVerb') -ne 'Convert') { throw 'Incorrect menu label.' }
        if ($menu.GetValue('SubCommands', 'missing') -ne '') { throw 'Missing submenu registration.' }
        $step = $menu.OpenSubKey('shell\01Step')
        try {
            if ($step.GetValue('MUIVerb') -ne 'STEP (.stp)') { throw 'Incorrect STEP label.' }
            if ($step.GetValue('MultiSelectModel') -ne 'Single') { throw 'Selection mode incorrect.' }
            $command = $step.OpenSubKey('command')
            try {
                $expected = '"' + $executable + '" --step "%1"'
                if ($command.GetValue('') -ne $expected) { throw 'Explorer command differs from the release executable path.' }
            }
            finally { $command.Dispose() }
        }
        finally { $step.Dispose() }
    }
    finally { $menu.Dispose() }
}
try {
    Invoke-Installer '--install'
    Test-InstalledMenu
    Invoke-Installer '--install'
    Test-InstalledMenu
    Invoke-Installer '--uninstall'
    $removed = $user.OpenSubKey($menuPath)
    if ($null -ne $removed) { $removed.Dispose(); throw 'Uninstall left the menu registered.' }
    Invoke-Installer '--install'
    Test-InstalledMenu
    Write-Output 'PASS install, reinstall, uninstall, and final menu registration. Convert > STEP (.stp) is installed.'
}
finally { $user.Dispose() }
