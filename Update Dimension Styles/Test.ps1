param([switch]$Live)
$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
    $entry=if($Live){'NativeSmoke'}else{'Tests'}
    $sources=@('Program.cs','StyleRepair.cs','MasterDocument.cs',('tests\'+$entry+'.cs'),'..\Hammill Common\ComSupport.cs','..\Hammill Common\MacroHost.cs','..\Add-ins\Hammill-Macro-Launcher\Catalog.cs','..\Add-ins\Hammill-Macro-Launcher\VideoTooltip.cs','..\Add-ins\Hammill-Macro-Launcher\DimensionStyleCycle\StyleSequence.cs')
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 ("/main:"+$entry) /r:Microsoft.CSharp.dll /r:System.Core.dll /r:System.Xml.Linq.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:tests\Tests.exe @sources
    if($LASTEXITCODE -ne 0){throw 'Test compilation failed.'}
    & .\tests\Tests.exe
    if($LASTEXITCODE -ne 0){throw 'Style repair tests failed.'}
}finally{Pop-Location}
