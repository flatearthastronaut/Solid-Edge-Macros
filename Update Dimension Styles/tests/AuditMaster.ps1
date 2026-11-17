param([switch]$Clean)
$ErrorActionPreference='Stop'
$path=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\normal (new design).dft'))
if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw 'Project master draft is missing.'}
function Release($o){if($null -ne $o -and [Runtime.InteropServices.Marshal]::IsComObject($o)){[void][Runtime.InteropServices.Marshal]::ReleaseComObject($o)}}
$app=$null;$docs=$null;$previous=$null;$doc=$null;$styles=$null;$opened=$false
try {
 $app=[Runtime.InteropServices.Marshal]::GetActiveObject('SolidEdge.Application');$docs=$app.Documents
 if($docs.Count -gt 0){$previous=$app.ActiveDocument}
 for($i=1;$i -le $docs.Count;$i++){$item=$docs.Item($i);if($item.FullName -eq $path){$doc=$item;break};Release $item}
 if($null -eq $doc){$doc=$docs.Open($path);$opened=$true}
 $styles=$doc.DimensionStyles
 Write-Output ('Master: '+$doc.FullName+'; active style: '+$styles.Active)
 for($i=1;$i -le $styles.Count;$i++){$style=$styles.Item($i);try{Write-Output ($style.Name+' | units '+$style.PrimaryUnits+'/'+$style.SecondaryUnits+' | dual '+$style.DualDisplay+' | orientation '+$style.TextOrientation+' | precision '+$style.PrimaryDecimalRoundOff+'/'+$style.SecondaryDecimalRoundOff)}finally{Release $style}}
 if($Clean){
  if(-not $opened){throw 'Close the master first so unsaved user edits cannot be overwritten.'}
  $backup=Join-Path $PSScriptRoot ('.work\normal-before-cleanup-'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss')+'.dft');Copy-Item -LiteralPath $path -Destination $backup
  Write-Output ('BACKUP: '+$backup)
  $removed=0
  foreach($name in @('ANSI','ANSImm','BSI','DIN','GRINDSTOCK','ISO','JIS','Z1')){try{$styles.Remove($name);$removed++;Write-Output ('REMOVED: '+$name)}catch{Write-Output ('KEPT: '+$name+' | '+$_.Exception.Message)}}
  if($removed -gt 0){$doc.Save();Write-Output ('SAVED: '+$removed+' removals')}
 }
}finally{Release $styles;if($opened -and $null -ne $doc){$doc.Close($false)};if($null -ne $previous){$previous.Activate()};Release $doc;Release $previous;Release $docs;Release $app}
