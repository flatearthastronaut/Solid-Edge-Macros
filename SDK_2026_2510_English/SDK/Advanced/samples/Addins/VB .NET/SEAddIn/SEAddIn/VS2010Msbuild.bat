REM Use this bat file to build any configuration on any platform as long as the configuration is either "release" or "debug" and the target is "x64", "x86" or "AnyCPU".
REM Unfortunately for "anycpu", this tool only registers a 64 bit add-in server when used on a 64 bit machine. I don't have a 32 bit machine/OS so I did not test to
REM see if the tool would register only a 32 bit add-in server on such a platform. Of course a real add-in will have some sort of setup tool for the end user.

REM I added this bat file to the sample because with VS 2010, one could get a build error: error TI0000: "A single valid machine type compatible with the input type
REM library must be specified." I have also seen "BC2000 unable to find required file. When I googled for these errors I found this:
REM http://blogs.msdn.com/visualstudio/archive/2010/05/07/building-on-cross-targeting-scenarios-and-64-bit-msbuild.aspx
REM 
REM The above link indicated I should use msbuild.exe, which gets us to the code below.
REM
REM Oh, and after all this work, I went back to copy the exact errors for the above comment, and everything built from the IDE! Microsoft Magic.


ECHO OFF

set CONFIG=%1
set PLATFORM=%2

if not defined CONFIG goto USAGE
if not defined PLATFORM goto USAGE

CLS

SETLOCAL
PUSHD %VS100COMNTOOLS%
POPD

if /i "%2"=="x64" goto BUILD64

if /i "%2" EQU "x86" goto BUILD86

if /i "%2" EQU "AnyCPU" goto BUILDANY

goto usage

:BUILD64
@call "%vcinstalldir%\vcvarsall.bat" amd64
goto BUILD

:BUILD86
@call "%vcinstalldir%\vcvarsall.bat"
goto BUILD

:BUILDANY
REM @call "%vcinstalldir%\vcvarsall.bat"
goto BUILD

:BUILD

msbuild seaddin.vbproj /p:configuration="%CONFIG%" /p:platform="%PLATFORM%"


REM - Doesn't really matter if the resource file is 32 or 64 bit since it is loaded as a data file.
REM - Ok, the above is not quite true. If I change to x64, I get BC2000 error telling me mscorlib.dll could not be found when vbc tries to start!

if /i "%2" EQU "AnyCPU" goto RESOURCEBUILDANY

Vbc.exe /noconfig /imports:Microsoft.VisualBasic,System,System.Collections,System.Collections.Generic,System.Diagnostics /optionstrict:custom /nowarn:42016,41999,42017,42018,42019,42032,42036,42020,42021,42022 /nostdlib /platform:x64 /rootnamespace:SEAddIn /sdkpath:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0" /define:"CONFIG=\"Debug\",DEBUG=-1,TRACE=-1,_MyType=\"Windows\",PLATFORM=\"x86\"" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Core.dll","C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.dll" /out:obj\%PLATFORM%\%CONFIG%\SEAddInRes.dll /target:library resproxy.vb

goto REG

REM RESOURCEBUILDANY exists because VS leaves the platform off the subdir names for anycpu

:RESOURCEBUILDANY
Vbc.exe /noconfig /imports:Microsoft.VisualBasic,System,System.Collections,System.Collections.Generic,System.Diagnostics /optionstrict:custom /nowarn:42016,41999,42017,42018,42019,42032,42036,42020,42021,42022 /nostdlib /platform:x64 /rootnamespace:SEAddIn /sdkpath:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0" /define:"CONFIG=\"Debug\",DEBUG=-1,TRACE=-1,_MyType=\"Windows\",PLATFORM=\"x86\"" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Core.dll","C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.dll" /out:obj\%CONFIG%\SEAddInRes.dll /target:library resproxy.vb

:REG

if /i "%2"=="x64" goto REG8664

if /i "%2" EQU "x86" goto REG8664

if /i "%2" EQU "AnyCPU" goto REGANY

:REG8664

regasm.exe /codebase obj\%PLATFORM%\%CONFIG%\SEAddIn.dll
copy obj\%PLATFORM%\%CONFIG%\SEAddInRes.dll bin\%PLATFORM%\%CONFIG%\SEAddInRes.dll

goto END

:REGANY

regasm.exe /codebase obj\%CONFIG%\SEAddIn.dll
copy obj\%CONFIG%\SEAddInRes.dll bin\%CONFIG%\SEAddInRes.dll

:END

ENDLOCAL

ECHO Compile Complete

goto EXIT

:USAGE

ECHO Usage: vs2010build target platform
ECHO where target is one of x86 x64 AnyCPU platform is one debug or release
ECHO Example: vs2010build debu/release x86

:EXIT
