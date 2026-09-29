$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
$project=Split-Path $PSScriptRoot
$a=[Reflection.Assembly]::LoadFrom((Join-Path $project 'Compiled Executables\BoltCircleHoles-v0.16.exe'))
$t=$a.GetType('BoltCircleHoles.MainWindow',$true)
$form=[Activator]::CreateInstance($t,@($true))
function Field($name){$t.GetField($name,[Reflection.BindingFlags]'NonPublic,Instance').GetValue($form)}
try {
    $form.Show();[Windows.Forms.Application]::DoEvents()
    $a2=Field 'a2Sizes';$symbol=Field 'a2Symbol'
    $a2.SelectedIndex=5
    if($symbol.Kind -ne 'Button'){throw 'Button symbol failed'}
    foreach($name in @('metricThreads','inchThreads')){
        $list=Field $name
        if($list.Enabled -or $list.Text -ne 'Coming soon'){throw 'Thread placeholder must stay disabled'}
    }
    $a2.SelectedIndex=0
    if($symbol.Kind -ne 'Counterbore' -or !(Field 'pick').Enabled){throw 'Counterbore selection regressed'}
    $t.GetMethod('Reset',[Reflection.BindingFlags]'NonPublic,Instance').Invoke($form,@())
    if((Field 'metricThreads').Enabled -or (Field 'inchThreads').Enabled){throw 'Reset enabled thread placeholders'}
    $a2.SelectedIndex=5
    foreach($control in $form.Controls){
        if($control.Bottom -gt $form.ClientSize.Height -or $control.Right -gt $form.ClientSize.Width){throw 'Control outside window'}
    }
    $bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height)
    try {
        $form.DrawToBitmap($bitmap,(New-Object Drawing.Rectangle(0,0,$form.Width,$form.Height)))
        $bitmap.Save((Join-Path $PSScriptRoot 'window-preview.png'))
    } finally {$bitmap.Dispose()}
    'PASS: disabled thread placeholders, dynamic A2 symbols, reset behavior, and control bounds.'
} finally {$form.Dispose()}
