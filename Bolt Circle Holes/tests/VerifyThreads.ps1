$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
$project=Split-Path $PSScriptRoot
$a=[Reflection.Assembly]::LoadFrom((Join-Path $project 'Compiled Executables\BoltCircleHoles-v0.20.exe'))
$metric=[BoltCircleHoles.ThreadChart]::Load($true);$inch=[BoltCircleHoles.ThreadChart]::Load($false)
if($metric.Count -ne 15 -or $inch.Count -ne 13){throw 'Missing chart sizes'}
# Independently calculate the revised 2026-10-07 PDF rule for every inch-column
# value: round full depth and the family-specific extra allowance separately.
foreach($row in @($metric)+@($inch)){
    $spec=$row.Thread
    $raw=if($spec.Metric){(1.5*$spec.NominalInches*25.4+3.25)/25.4}else{1.5*$spec.NominalInches+.125}
    $full=[Math]::Ceiling($raw*100-1e-8)/100
    $pitch=if($spec.Metric){$spec.Pitch/25.4}else{1/$spec.Pitch}
    $minimum=if($spec.Metric){3.25/25.4}else{.125}
    $extra=[Math]::Ceiling([Math]::Max($minimum,2.5*$pitch)*100-1e-8)/100
    if([Math]::Abs($spec.FullDepthInches-$full)-gt 1e-9 -or [Math]::Abs($row.Depth-$full-$extra)-gt 1e-9){throw ('Chart mismatch: '+$row.Screw)}
    $d=[BoltCircleHoles.HoleEngine]::Dimensions($row)
    if([Math]::Abs($d[2]-$row.Depth*.0254)-gt 1e-12){throw 'Unit conversion failed'}
}
foreach($n in @(1,2,6,13,999)){
    $start=[double[]]@(.03,-.04,.03683)
    $points=[BoltCircleHoles.ThreadChart]::Centers($start,[double[]]@(0,0,-1),$n)
    if($points.Count -ne $n){throw 'Count wrong'}
    for($i=0;$i -lt $n;$i++){
        $p=$points[$i];$radius=[Math]::Sqrt($p[0]*$p[0]+$p[1]*$p[1])
        if([Math]::Abs($radius-.05)-gt 1e-12 -or $p[2]-ne $start[2]){throw 'Radius or plane changed'}
        if($i -eq 0 -and ($p[0]-ne $start[0] -or $p[1]-ne $start[1])){throw 'First hole moved'}
        if($i -gt 0){$prev=$points[$i-1];$dot=($prev[0]*$p[0]+$prev[1]*$p[1])/.0025;if([Math]::Abs($dot-[Math]::Cos(2*[Math]::PI/$n))-gt 1e-10){throw 'Spacing wrong'}}
    }
}
foreach($case in @(@([double[]]@(0,0,.03),[double[]]@(0,0,1),6),@([double[]]@(.03,.04,.03),[double[]]@(1,0,0),6))){
    $rejected=$false;try{[void][BoltCircleHoles.ThreadChart]::Centers($case[0],$case[1],$case[2])}catch{$rejected=$true};if(!$rejected){throw 'Invalid multi-hole placement accepted'}
}
$t=$a.GetType('BoltCircleHoles.MainWindow');$form=[Activator]::CreateInstance($t,@($true))
function Field($name){$t.GetField($name,[Reflection.BindingFlags]'NonPublic,Instance').GetValue($form)}
try {
    $form.Show();[Windows.Forms.Application]::DoEvents()
    $mt=Field 'metricThreads';$it=Field 'inchThreads';$a2=Field 'a2Sizes'
    if($mt.Items.Count-ne 15 -or $it.Items.Count-ne 13 -or !$mt.Enabled -or !$it.Enabled){throw 'Thread lists failed'}
    $a2.SelectedIndex=0;$mt.SelectedIndex=7
    if($a2.SelectedIndex-ne -1 -or (Field 'metricSizes').SelectedIndex-ne -1){throw 'Stale A2 selection'}
    if((Field 'dimensions').Text -notmatch '0.720 in' -or (Field 'dimensions').Text -notmatch '0.870 in'){throw 'Metric display wrong'}
    (Field 'six').PerformClick();if((Field 'holeCount').Value-ne 6){throw '6 button failed'}
    $it.SelectedIndex=3;if($mt.SelectedIndex-ne -1 -or (Field 'dimensions').Text -notmatch '1/4-20 UNC'){throw 'Inch selection failed'}
    if((Field 'dimensions').Text -notmatch '0.500 in' -or (Field 'dimensions').Text -notmatch '0.630 in'){throw 'Revised inch depth missing'}
    $mt.SelectedIndex=13
    if((Field 'dimensions').Text -notmatch 'M22 x 2.5' -or (Field 'dimensions').Text -notmatch '1.430 in' -or (Field 'dimensions').Text -notmatch '1.680 in'){throw 'M22 entry incorrect'}
    $mt.SelectedIndex=14
    if((Field 'dimensions').Text -notmatch 'M24 x 3' -or (Field 'dimensions').Text -notmatch '1.550 in' -or (Field 'dimensions').Text -notmatch '1.850 in'){throw 'M24 entry incorrect'}
    $a2.SelectedIndex=0;if($it.SelectedIndex-ne -1){throw 'Stale thread selection'}
    $mt.SelectedIndex=7
    $bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height)
    try{$form.DrawToBitmap($bitmap,(New-Object Drawing.Rectangle(0,0,$form.Width,$form.Height)));$bitmap.Save((Join-Path $PSScriptRoot 'threads-preview.png'))}finally{$bitmap.Dispose()}
} finally {$form.Dispose()}
'PASS: all 28 revised chart entries, units, circular positions/counts, invalid supports, selection switching, M22/M24, and 6 button.'
