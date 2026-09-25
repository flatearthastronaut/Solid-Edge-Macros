ECHO OFF
CLS

SETLOCAL
PUSHD %VS80COMNTOOLS%
@call vsvars32.bat
POPD

Vbc.exe /noconfig /imports:Microsoft.VisualBasic,System,System.Collections,System.Collections.Generic,System.Data,System.Diagnostics /nowarn:42016,41999,42017,42018,42019,42032,42036,42020,42021,42022 /platform:x86 /rootnamespace:SEAddIn /win32Resource:resTempl1.res /doc:obj\x86\Release\SEAddIn.xml /define:"CONFIG=\"Release\",TRACE=-1,_MyType=\"Windows\",PLATFORM=\"x86\"" /reference:D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Data.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll,obj\x86\Release\Interop.SolidEdgeConstants.dll,obj\x86\Release\Interop.SolidEdgeFramework.dll /debug:pdbonly /optimize+ /out:obj\x86\Release\SEAddIn.dll /resource:obj\x86\Release\SEAddIn.Dialog1.resources /resource:obj\x86\Release\SEAddIn.EdgeBarForm.resources /resource:obj\x86\Release\SEAddIn.Resources.resources /target:library *.vb "My Project\AssemblyInfo.vb" "My Project\Application.Designer.vb" "My Project\Resources.Designer.vb" "My Project\Settings.Designer.vb"

regasm.exe /codebase obj\x86\Release\SEAddIn.dll

Vbc.exe /noconfig /platform:x86 /win32Resource:resTempl1.res /define:"CONFIG=\"RELEASE\",TRACE=-1,_MyType=\"Windows\",PLATFORM=\"x86\"" /reference:D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Data.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll,D:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll /out:obj\x86\Release\SEAddInRes.dll /target:library resproxy.vb

copy obj\x86\Release\SEAddInRes.dll bin\x86\Release\SEAddInRes.dll

ENDLOCAL

ECHO Compile Complete