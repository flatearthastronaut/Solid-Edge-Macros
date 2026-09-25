ECHO OFF
CLS

REM - Same as compiledebugx86.bat

SETLOCAL
PUSHD %VS80COMNTOOLS%
@call vsvars32.bat
POPD

D:\Windows\Microsoft.NET\Framework\v2.0.50727\Vbc.exe /noconfig /imports:Microsoft.VisualBasic,System,System.Collections,System.Collections.Generic,System.Data,System.Diagnostics /nowarn:42016,41999,42017,42018,42019,42032,42036,42020,42021,42022 /rootnamespace:SEAddIn /win32Resource:resTempl1.res /doc:obj\Debug\SEAddIn.xml /define:"CONFIG=\"Debug\",DEBUG=-1,TRACE=-1,_MyType=\"Windows\",PLATFORM=\"AnyCPU\"" /reference:D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Data.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll,obj\Debug\Interop.SolidEdgeConstants.dll,obj\Debug\Interop.SolidEdgeFramework.dll /debug+ /debug:full /out:obj\Debug\SEAddIn.dll /resource:obj\Debug\SEAddIn.Dialog1.resources /resource:obj\Debug\SEAddIn.EdgeBarForm.resources /resource:obj\Debug\SEAddIn.Resources.resources /target:library *.vb "My Project\AssemblyInfo.vb" "My Project\Application.Designer.vb" "My Project\Resources.Designer.vb" "My Project\Settings.Designer.vb"

regasm.exe /codebase obj\Release\SEAddIn.dll

REM - Doesn't really matter if the resource file is 32 or 64 bit since it is loaded as a data file
Vbc.exe /noconfig /win32Resource:resTempl1.res /define:"CONFIG=\"Debug\",DEBUG=-1,TRACE=-1,_MyType=\"Windows\"" /reference:D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Data.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll /out:obj\Debug\SEAddInRes.dll /target:library resproxy.vb

copy obj\Debug\SEAddInRes.dll bin\Debug\SEAddInRes.dll

ENDLOCAL

ECHO Compile Complete