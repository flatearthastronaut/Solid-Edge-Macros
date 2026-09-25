: This file exists to allow one to generate .NET interop assemblies from SolidEdge type library (.tlb) files. One should create
: and use their own interop assemblies to avoid runtime binding problems that can occur when two or more .NET automation clients
: load into the same process while referencing interop assemblies that have the same identity (basically the name of the assembly)
: but may differ in functionality (e.g., one assembly generated from an earlier version of edge that doesn't contain an API
: added later that another assembly contains).
: If you are debugging and find some type or function is missing even though the object browser indicates it is there during
: development, you may be a victim of the .NET loader loading an incorrect assembly. This is easy to detect using the debugger.
: Look at the "modules" window in the VS IDE when you are running and see exactly from where each assembly is loaded.
: By generating your own assemblies using a unique label (arg 3 to this macro) and delivering them with your tool, you can avoid 
: this issue. For a unique lable I recommend for add-ins or other COM objects, one use the short guid of the add-in for the lable. 
: A company name is also probably sufficient for the label.
:
: Example invokation:
:
: GenEdgeInteropAssemblies "c:\program files\solid edge ST" c:\tmp\MyProject Siemens
:
: Usage: The first arg is the path to the SolidEdge type libraries. The second arg is a path to where the generated 
: assemblies should be created. The third argument is a unique lable. Note that "" are needed whenever there is a space in the path(s). 
:
: Example:
: tlbimp constant.tlb /out:interop.EdgeConstant%3.dll /sysarray /primary /keyfile : thekey.snk
:

:/sysarray should be used if the type library uses any SAFEARRAYs that are not one-dimensional or that do not have a zero lower bound. Using
: this option will cause SAFEARRAYs to be treated as System.Array objects.

: After running this bat file, there will be additional dlls without the prepended "interop." tag. These dlls can be generated when one typelib
: references another typelib (and the typelib referenced is registered on the machine). They can be ignored and deleted.

: I did not add an argument for the name used for the namespaces. I create namespace names based on
: what VB .NET creates by default when one adds a reference by browsing to the solid edge type libraries (.tlb files in program dir).
: The goal is to allow a user that has already generated interop assemblies to delete the assembly reference and
: recreate new references such that code still compiles. One would do such a replacement to avoid DLL collisions with other
: interop assemblies created by another macro/add-in or .NET automation client. Regardless of what Microsoft indicates, such
: collisions occur because the assembly identity is connected to the name of the interop assembly.

@echo off
setlocal

:I will try to make this bat file invokable from any command prompt or another bat file (in which case you might want to double-click
:from explorer). If you are not invoking from a VS command prompt window, the following is needed so we can access the tlbimp.exe
: tool. If you don't want this, comment out everything up to and including the ":Generate" lable.

:What version of Visual Studio is available?

if "%VS100COMNTOOLS%" EQU "" goto VS9

PUSHD %VS100COMNTOOLS%
@call vsvars32.bat
POPD

goto Generate

:VS9
if "%VS90COMNTOOLS%" EQU "" goto VS8

PUSHD %VS90COMNTOOLS%
@call vsvars32.bat
POPD

goto Generate

:VS8

if "%VS80COMNTOOLS%" EQU "" goto Generate

PUSHD %VS80COMNTOOLS%
@call vsvars32.bat
POPD
echo "Unable to find Visual Studio tools directory"

:Generate

REM Using ~ to strip out quotes for paths that have spaces in them.

if "%~1" EQU "" goto usage

if "%~2" EQU "" goto usage

if "%3" EQU "" goto usage

tlbimp %1\InstallData.tlb /out:%2\interop.SolidEdgeInstallData%3.dll /namespace:SolidEdge.InstallData.Interop

tlbimp %1\constant.tlb /out:%2\interop.SolidEdgeConstant%3.dll /namespace:SolidEdge.Constant.Interop

tlbimp %1\RevMgr.tlb /out:%2\interop.SolidEdgeRevisionManager%3.dll /namespace:SolidEdge.RevisionManager.Interop /sysarray

tlbimp %1\framewrk.tlb /out:%2\interop.SolidEdgeFramework%3.dll /namespace:SolidEdge.Framework.Interop /sysarray

tlbimp %1\fwksupp.tlb /out:%2\interop.SolidEdgeFrameworkSupport%3.dll /namespace:SolidEdge.FrameworkSupport.Interop /sysarray /reference:%2\interop.SolidEdgeFramework%3.dll

tlbimp %1\geometry.tlb /out:%2\interop.SolidEdgeGeometry%3.dll /namespace:SolidEdge.Geometry.Interop /sysarray /reference:%2\interop.SolidEdgeFramework%3.dll

tlbimp %1\draft.tlb /out:%2\interop.SolidEdgeDraft%3.dll /namespace:SolidEdge.Draft.Interop /sysarray /reference:%2\interop.SolidEdgeFramework%3.dll  /reference:%2\interop.SolidEdgeFrameworkSupport%3.dll

tlbimp %1\part.tlb /out:%2\interop.SolidEdgePart%3.dll /namespace:SolidEdge.Part.Interop /sysarray /reference:%2\interop.SolidEdgeFramework%3.dll  /reference:%2\interop.SolidEdgeFrameworkSupport%3.dll /reference:%2\interop.SolidEdgeGeometry%3.dll

tlbimp %1\assembly.tlb /out:%2\interop.SolidEdgeAssembly%3.dll /namespace:SolidEdge.Assembly.Interop /sysarray /reference:%2\interop.SolidEdgeFramework%3.dll  /reference:%2\interop.SolidEdgePart%3.dll

tlbimp %1\PropAuto.dll /out:%2\interop.SolidEdgePropAuto%3.dll /namespace:SolidEdge.PropAuto.Interop /sysarray

:usage
echo GenEdgeInteropAssemblies typelibpath outdir UniqueLable
endlocal
