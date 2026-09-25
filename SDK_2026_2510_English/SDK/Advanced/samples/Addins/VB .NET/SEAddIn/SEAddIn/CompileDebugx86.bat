ECHO OFF
CLS

SETLOCAL
PUSHD %VS80COMNTOOLS%
@call vsvars32.bat
POPD

Vbc.exe /noconfig /imports:Microsoft.VisualBasic,System,System.Collections,System.Collections.Generic,System.Data,System.Diagnostics /nowarn:42016,41999,42017,42018,42019,42032,42036,42020,42021,42022 /platform:x86 /rootnamespace:SEAddIn /win32Resource:resTempl1.res /doc:obj\x86\Debug\SEAddIn.xml /define:"CONFIG=\"Debug\",DEBUG=-1,TRACE=-1,_MyType=\"Windows\",PLATFORM=\"x86\"" /reference:D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Data.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll,obj\x86\Debug\Interop.SolidEdgeConstants.dll,obj\x86\Debug\Interop.SolidEdgeFramework.dll /debug+ /debug:full /out:obj\x86\Debug\SEAddIn.dll /resource:obj\x86\Debug\SEAddIn.Dialog1.resources /resource:obj\x86\Debug\SEAddIn.EdgeBarForm.resources /resource:obj\x86\Debug\SEAddIn.Resources.resources /target:library *.vb "My Project\AssemblyInfo.vb" "My Project\Application.Designer.vb" "My Project\Resources.Designer.vb" "My Project\Settings.Designer.vb"

Vbc.exe /noconfig /platform:x86 /win32Resource:resTempl1.res /define:"CONFIG=\"Debug\",DEBUG=-1,TRACE=-1,_MyType=\"Windows\",PLATFORM=\"x86\"" /reference:D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Data.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll /out:obj\x86\Debug\SEAddInRes.dll /target:library resproxy.vb

regasm.exe /codebase obj\x86\Debug\SEAddIn.dll

copy obj\x86\Debug\SEAddInRes.dll bin\x86\Debug\SEAddInRes.dll

ENDLOCAL

ECHO Compile Complete
