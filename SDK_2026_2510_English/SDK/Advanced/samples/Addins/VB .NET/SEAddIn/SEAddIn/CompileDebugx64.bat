ECHO OFF
CLS

SETLOCAL
PUSHD %VS80COMNTOOLS%
@call "%vcinstalldir%\vcvarsall.bat" amd64
POPD

D:\Windows\Microsoft.NET\Framework\v2.0.50727\Vbc.exe /noconfig /imports:Microsoft.VisualBasic,System,System.Collections,System.Collections.Generic,System.Diagnostics /nowarn:42016,41999,42017,42018,42019,42032,42036,42020,42021,42022,40010 /platform:x64 /rootnamespace:SEAddIn /win32Resource:resTempl1.res /doc:obj\x64\Debug\SEAddIn.xml /define:"CONFIG=\"Debug\",DEBUG=-1,TRACE=-1,_MyType=\"Windows\",PLATFORM=\"x64\"" /reference:D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Data.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll,obj\x64\Debug\Interop.SolidEdgeConstants.dll,obj\x64\Debug\Interop.SolidEdgeFramework.dll /debug+ /debug:full /out:obj\x64\Debug\SEAddIn.dll /resource:obj\x64\Debug\SEAddIn.Dialog1.resources /resource:obj\x64\Debug\SEAddIn.EdgeBarForm.resources /resource:obj\x64\Debug\SEAddIn.Resources.resources /target:library *.vb "My Project\AssemblyInfo.vb" "My Project\Application.Designer.vb" "My Project\Resources.Designer.vb" "My Project\Settings.Designer.vb"

REM - Doesn't really matter if the resource file is 32 or 64 bit since it is loaded as a data file
Vbc.exe /noconfig /platform:x64 /win32Resource:resTempl1.res /nowarn:40010 /define:"CONFIG=\"Debug\",DEBUG=-1,TRACE=-1,_MyType=\"Windows\",PLATFORM=\"x64\"" /reference:D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Data.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll /out:obj\x64\Debug\SEAddInRes.dll /target:library resproxy.vb

regasm.exe /codebase obj\x64\Debug\SEAddIn.dll

copy obj\x64\Debug\SEAddInRes.dll bin\x64\Debug\SEAddInRes.dll

ENDLOCAL

ECHO Compile Complete
