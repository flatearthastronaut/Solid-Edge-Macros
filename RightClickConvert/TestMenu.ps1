# Integration check of the real executable's per-user installer. Leaves the
# verified menu installed at the release executable's current path.
$ErrorActionPreference = 'Stop'
$executable = Join-Path $PSScriptRoot 'Compiled Executables\SolidEdgeConvert.exe'
$menus = @(
    @{ Path='Software\Classes\SystemFileAssociations\.par\shell\SolidEdgeMacros.Convert'; Verb='01Step'; Label='STEP (.stp)'; Argument='--step'; Class='{B84A6BE1-A4D2-4CD2-A1AE-60EAA476AD11}' },
    @{ Path='Software\Classes\SystemFileAssociations\.dft\shell\SolidEdgeMacros.Convert'; Verb='01Pdf'; Label='PDF (.pdf)'; Argument='--pdf'; Class='{AF39D53D-3C70-4055-8197-442F4C5180B2}' },
    @{ Path='Software\Classes\SystemFileAssociations\.dft\shell\SolidEdgeMacros.Convert'; Verb='02PdfWithDate'; Label='PDF with Date'; Argument='--pdf-date'; Class='{D8F750C4-A88D-4F7A-BD12-590D6813B742}' },
    @{ Path='Software\Classes\SystemFileAssociations\.stp\shell\SolidEdgeMacros.Convert'; Verb='01Part'; Label='Solid Edge Part (.par)'; Argument='--part'; Class='{46FDDBA1-7601-445B-A3A6-7F7624B196C2}' },
    @{ Path='Software\Classes\SystemFileAssociations\.step\shell\SolidEdgeMacros.Convert'; Verb='01Part'; Label='Solid Edge Part (.par)'; Argument='--part'; Class='{46FDDBA1-7601-445B-A3A6-7F7624B196C2}' },
    @{ Path='Software\Classes\SystemFileAssociations\.x_t\shell\SolidEdgeMacros.Convert'; Verb='01Part'; Label='Solid Edge Part (.par)'; Argument='--parasolid-part'; Class='{263031DD-6A5B-4776-B4BC-39EB97D16A95}' },
    @{ Path='Software\Classes\SystemFileAssociations\.x_b\shell\SolidEdgeMacros.Convert'; Verb='01Part'; Label='Solid Edge Part (.par)'; Argument='--parasolid-part'; Class='{263031DD-6A5B-4776-B4BC-39EB97D16A95}' },
    @{ Path='Software\Classes\SystemFileAssociations\.x_t\shell\SolidEdgeMacros.Convert'; Verb='02Assembly'; Label='Solid Edge Assembly (.asm)'; Argument='--parasolid-assembly'; Class='{826FDBF0-726C-4EE8-B8B2-AE63E63ACAF9}' },
    @{ Path='Software\Classes\SystemFileAssociations\.x_b\shell\SolidEdgeMacros.Convert'; Verb='02Assembly'; Label='Solid Edge Assembly (.asm)'; Argument='--parasolid-assembly'; Class='{826FDBF0-726C-4EE8-B8B2-AE63E63ACAF9}' },
    @{ Path='Software\Classes\SystemFileAssociations\.stp\shell\SolidEdgeMacros.Convert'; Verb='02Assembly'; Label='Solid Edge Assembly (.asm)'; Argument='--step-assembly'; Class='{B5CC13CF-4069-451E-87E6-6E74686DD5AF}' },
    @{ Path='Software\Classes\SystemFileAssociations\.step\shell\SolidEdgeMacros.Convert'; Verb='02Assembly'; Label='Solid Edge Assembly (.asm)'; Argument='--step-assembly'; Class='{B5CC13CF-4069-451E-87E6-6E74686DD5AF}' }
)
$user = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser, [Microsoft.Win32.RegistryView]::Registry64)
function Invoke-Installer([string] $argument) {
    $process = Start-Process -FilePath $executable -ArgumentList $argument -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Menu command failed: $argument" }
}
function Test-InstalledMenu {
    foreach ($entry in $menus) {
    $menu = $user.OpenSubKey($entry.Path)
    if ($null -eq $menu) { throw 'Convert menu was not registered.' }
    try {
        if ($menu.GetValue('MUIVerb') -ne 'Convert') { throw 'Incorrect menu label.' }
        if ($menu.GetValue('SubCommands', 'missing') -ne '') { throw 'Missing submenu registration.' }
        if ($menu.GetValue('MultiSelectModel') -ne 'Player') { throw 'Parent menu does not allow multiple selections.' }
        $step = $menu.OpenSubKey('shell\' + $entry.Verb)
        try {
            if ($step.GetValue('MUIVerb') -ne $entry.Label) { throw 'Incorrect conversion label.' }
            if ($step.GetValue('MultiSelectModel') -ne 'Player') { throw 'Selection mode incorrect.' }
            $command = $step.OpenSubKey('command')
            try {
                $expected = '"' + $executable + '" ' + $entry.Argument + ' "%1"'
                if ($command.GetValue('') -ne $expected) { throw 'Explorer command differs from the release executable path.' }
                if ($command.GetValue('DelegateExecute') -ne $entry.Class) { throw 'Incorrect multi-selection handler.' }
                $server = $user.OpenSubKey('Software\Classes\CLSID\' + $entry.Class + '\LocalServer32')
                try {
                    if ($server.GetValue('') -ne ('"' + $executable + '" --shell-server')) { throw 'Incorrect COM server command.' }
                    if ($server.GetValue('ServerExecutable') -ne $executable) { throw 'Incorrect COM executable path.' }
                }
                finally { $server.Dispose() }
            }
            finally { $command.Dispose() }
        }
        finally { $step.Dispose() }
    }
    finally { $menu.Dispose() }
    }
}
try {
    Invoke-Installer '--install'
    Test-InstalledMenu
    Invoke-Installer '--install'
    Test-InstalledMenu
    Invoke-Installer '--uninstall'
    foreach ($entry in $menus) {
        $removed = $user.OpenSubKey($entry.Path)
        if ($null -ne $removed) { $removed.Dispose(); throw 'Uninstall left a menu registered.' }
        $removedServer = $user.OpenSubKey('Software\Classes\CLSID\' + $entry.Class)
        if ($null -ne $removedServer) { $removedServer.Dispose(); throw 'Uninstall left a COM server registered.' }
    }
    Invoke-Installer '--install'
    Test-InstalledMenu
    Write-Output 'PASS install, reinstall, uninstall, and final registration for all seven conversions and six source extensions.'
}
finally { $user.Dispose() }
