ECHO OFF
CLS

SETLOCAL
PUSHD %VS80COMNTOOLS%
@call vsvars32.bat
POPD

REM - Makes no difference what the bitness of the resource dll is since it is only loaded as a data file.
Vbc.exe /noconfig /platform:x86 /win32Resource:resTempl1.res /define:"CONFIG=\"RELEASE\",TRACE=-1,_MyType=\"Windows\",PLATFORM=\"x86\"" /reference:System.Data.dll,System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Xml.dll /out:obj\SEAddInRes.dll /target:library resproxy.vb

ENDLOCAL

ECHO Compile Complete