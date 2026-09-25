ECHO OFF
CLS

SETLOCAL
PUSHD %VS80COMNTOOLS%
@call vsvars32.bat
POPD

Vbc.exe /noconfig /imports:Microsoft.VisualBasic,System,System.Collections,System.Collections.Generic,System.Data,System.Diagnostics /nowarn:42016,41999,42017,42018,42019,42032,42036,42020,42021,42022 /rootnamespace:SEAddIn /win32Resource:resTempl1.res /doc:obj\Release\SEAddIn.xml /define:"CONFIG=\"Release\",TRACE=-1,_MyType=\"Windows\",PLATFORM=\"AnyCPU\"" /reference:D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Data.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll,obj\Release\Interop.SolidEdgeConstants.dll,obj\Release\Interop.SolidEdgeFramework.dll /debug:pdbonly /optimize+ /out:obj\Release\SEAddIn.dll /resource:obj\Release\SEAddIn.Dialog1.resources /resource:obj\Release\SEAddIn.EdgeBarForm.resources /resource:obj\Release\SEAddIn.Resources.resources /target:library *.vb "My Project\AssemblyInfo.vb" "My Project\Application.Designer.vb" "My Project\Resources.Designer.vb" "My Project\Settings.Designer.vb"

regasm.exe /codebase obj\Release\SEAddIn.dll

Vbc.exe /noconfig /win32Resource:resTempl1.res /define:"CONFIG=\"Debug\",DEBUG=-1,TRACE=-1,_MyType=\"Windows\"" /reference:D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Data.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll /out:obj\Release\SEAddInRes.dll /target:library resproxy.vb

copy obj\Release\SEAddInRes.dll bin\Release\SEAddInRes.dll

ENDLOCAL

ECHO Compile Complete