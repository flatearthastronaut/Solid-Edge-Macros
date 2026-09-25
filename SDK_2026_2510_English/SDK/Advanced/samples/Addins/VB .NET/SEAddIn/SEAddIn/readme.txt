This project builds a VB .NET Solid Edge AddIn. I have added explicit targets for both x86
and x64 assemblies. I have also created a set of .bat files to build the various targets.
The add-in adds commands in the application, part, assembly and draft environments. The
add-in has two commands, which essentially do nothing. However the first command does,
when run in part, assembly or draft, create a command object in Edge, reacts to the
command events and also connects up to the mouse and reacts to the mouse events. It also adds
a very simple dialog to the ribbon bar and connects up to the command window events to
handle events from that dialog. Note that edge "auto formats" the dialog and if you examine
the dialog in the resource file, you will see a trick where I add an empty picture control
to the dialog so edge maintains some respectable spacing between the two buttons on the
dialog. The formatting edge does is a "left to right packing" in pre-ST and a
"verticularization" in ST (although there is pressure to go back to the classic ribbon
bar in ST).

When in assembly, if the user right-clicks on a part, the add-in responds to the short cut
menu event and adds the first command to the shortcut menu Edge presents the user.

Also, in each of the assembly, part and draft environments, the add-in adds a page to the
edge bar.


Resource management:

For certain API calls, Edge asks the caller to pass in resource identifiers (e.g., a bitmap
resource ID) and the handle to the resource module. Some newer APIs (usually with "Ex" added
to the original API name) will take the resource filename (full path is needed). The add-in
takes two approaches. It assumes there is a resource DLL in the same directory as the
add-in's DLL/assembly and attempts to load that resource file as a data file using a 
native Windows API call. If that fails, the add-in assumes its module has the resources
embedded in the module and uses the module handle to retrieve all resources (and in the calls
to Edge that take in a resource module handle). Whether the add-in loads the independent
resource file or the "Ex" APIs edge provides that take in a module filename are used, in
both cases the resource module is loaded as a data file (see the flag passed into the
api_LoadLibraryEx call in the WinAPI.vb project file). The reason the module is loaded
as a data file is that doing so makes the resource "bitness independent", by which I mean
that the module can be loaded in either a 32 or 64 bit process. That allows a single resource
module to be built and used in both a 32 and 64 bit environment.

There are GetResourceHandle and GetResourceString utility functions in the project. The
resource handle is setup in the OnConnection method (for now at least as I may move it to
the "New" subroutine in Addin.vb.


The project already has a native resource file that does not participate in builds as a
project file. Double click it ResTempl1.res and it will open and reveal a few resources I 
have added.


FYI: How to add a native resource file to a VB .NET project:

Open the project and run file/new and pick "native resource template". A .rct file is generated.
After adding a resource, save the file out and pick the .res extension.
Then rigth click on the project and pick add\existing item and pick the .res file.
The result is the .res file shows up in the solution explorer pane. Add resources to this file
that Edge loads for the addin. For instance, if a command wants to add a dialog to the edge
ribbon bar/command bar, the dialog resource id and the resource dll handle are passed to edge 
and hence those resources have to be win32 resources. Note that the Solid Edge API for edgebar 
(known as "docking panes" in ST) does not load the add-in dialog. Hence the VB .NET form used 
is not a win32 resource. Also, after adding the .res file to the project, the properties pane for
the file has a "build action" that can be set to "Embedded resource". This does not mean the
resource embedded is a win32 resource but rather a .NET resource. I did not try to generate
both a .net and a win32 resource from the same file (as of now).

Why the difference between an edgebar dialog and ribbon/command bar dialog? Edge "auto-formats"
the ribbon/command bar dialog to fit the UI design, which is a good thing since as I noted,
the layout of the command's dialog has evolved over time with add-ins not having to react to
the changes (that empty picture control is the add-in's "protection" mechanism and ST treats
such an item as a special case when auto-formatting in ST).


The next step is to compile the .res file as a win32 resource. I have some good news and some
bad news regarding that. The good news is that the VB .Net compiler supports building a .res
file as native win32 resources and embedding them in a VB .NET assembly/dll. The bad news is 
that the IDE, neither in VS 2005 nor VS 2008, supports doing so directly. This can only be 
accomplished currently only with a command line compilation! Google showed me that apparently 
the C# IDE does support compiling a native resource into the DLL/assembly. I zinged the VB
.NET dev team on that one.



How to compile in VS 2005 so the resource is embedded in the DLL/assembly:

I had a very hard time getting a command line to work. I finally did the following.

After adding references to the solid edge type libs and creating a specific target e.g. x86, using the
configuration manager, I did a build, got rid of all errors and then did a final build. The
output window for the build had the command line for the compiler. So I copied that entire line and
placed it in e.g., CompileDebugx86.bat. Finally I could run the bat file and get a build. Then I added:

/win32Resource:resTempl1.res

to the giant "vbc.exe" line in the bat file.

I repeated the above step to create the CompileReleasex86.bat file and the x64 and AnyCPU bat files.

Note that as files are added or settings are changed, the step to copy from the output window and paste
into the bat file(s) may need to be repeated. I did replace the specfic files in the project dir
with "*.vb" in the bat files. Hopefully that will mean constant updating is not necessary. Oh, and
I removed the hard-coded path to the vbc.exe (compiler). Hopefully since the bat files try to mimic
the Visual Studio command prompt window tool setups, they will run from any command prompt or from
Windows Explorer. If not ... well open the Visual Studio Tools command prompt window and run from
there.


HELP! MY RESOURCES DON'T SHOW UP!!!!

I kept loosing my resources because I would build from the IDE and the IDE, as noted, will not add
the win32 resources to the DLL/assembly. I finally went ahead and added another command line to
the bat files where I build a DLL that only has resources (I had to create an empty .vb file named
resproxy.vb since the compiler would not compile without at least one .vb file. The bat file also
copies the AddInRes.dll file produced to the second output directory the IDE creates. That way
whatever DLL/assembly is actually registered, at runtime the code (mentioned above) should be
able to find the independent resource file and load it.

Why have a separate resource only DLL? Internationalization. If your add-in is a big time seller,
millions of customers from all over the world will want to have it. So you will need to create
versions in multiple languages. But if by some odd quirk of fate, some bug shows up in your code
and gets past testing and into the customer base and a service pack fix is needed, the code
can be fixed and redelivered independent of the resources. As noted above, the add-in code
supports both approaches giving precedence to the independent resource DLL. That is, this sample
has both an independent resource DLL and the same resources are also embedded into the add-in's
code module. If you (or a customer) "loses" the independent resource module, and you used the
command line compilation bat files to produce the code module, the add-in will still have the
necessary resources at runtime (sort of a backup plan). If you only internationalize the 
independent resource module and somehow the module is lost on a customer machine, the customer
will at least see some resources.

Speaking of internationalization, don't assume the locale of the user's machine matches the
language of the installed Solid Edge. Some users run with an OS locale different than what
they installed with Edge. A sophisticated add-in would install the matching language as Edge
or install multiple resource DLLs and load the correct one at runtime. The add-in does not
currently have any code that uses the solid edge install data API that can be used to
find the installed language. That is an exercise left up to another developer.


Registration - It is very important that the addin be registered correctly. One issue I found was that
the build process from the IDE was creating two different directories with the addin in it. Since
I have the project setting "Register for COM interop" set in the Project/Properties/Compile page,
the IDE was registering one DLL in a "bin/x86/Debug" directory while the command line I copied from
the build output window was for a dll in "obj\x86\Debug". The latter is the one my build bat file
used to insert the win32 resources. The result was that when I ran, the icons on the commands failed
to show up. I finally found that after building the dll via the bat file, I needed to run regasm on
the dll because I have the "register for com interop" setting selected and the IDE registered the
DLL it just built that did not have resources in it. 

Also, very important, was that I also had to use the /codebase option for regasm. Without
that option, only the assembly entry appeared in the registry and Edge could not load the addin.
Now theoretically (I did not verify this), I could have copied the addin DLL and the interop DLLs
to the edge program directory and the addin would load. But THAT IS NOT RECOMMENDED since the interop
DLLs could interfere with interop DLLs edge delivers. Also, if a user uninstalls edge to install a
new version, you don't want the customer to lose your add-in. Since Edge detects add-ins via the
Windows registry APIs (examine the "implemented category" entry in the registration code of
SEAddIn.vb), the installation of the add-in and edge are totally independent.


To compile and register, I added the regasm call to the compile bat file(s). Note that it is very
important in the bat file to call the correct vsvarsXX.bat file, done by pushing the VS80COMNTOOLS
environment variable. In addition, the "register for COM interop" step FAILS for a x64 platform
configuration. This is a known IDE issue as the setting calls regasm.exe without invoking the correct 
version of regasm.exe that is in the Microsoft.NET\Framework64\vxxx directory. Luckily I found that 
the Visual Studio edition I have has two different command prompt entries under the "Visual Studio Tools" 
(system) menu. 

By examining the shortcuts for those, I was able to ascertain how to create the bat files so that
I could invoke the correct regasm.exe tool (see for example CompileReleasex64.bat where I invoke
"vcvarsall.bat" passing in the "amd64" parameter (by the way, "x64" works too). If all else fails,
one can open the correct Visual Studio 2005 command prompt window and run regasm.exe on the
DLL directly.


When first building the projects, do so from the IDE. That should ensure that the interop assemblies
(not delivered with the add-in) are created. Currently the project has dependencies on the interop
assemblies created by importing a reference to the solid edge framewrk.tlb and constants.tlb files.
The compilation bat files I created DO NOT create the interop assemblies and instead rely on their
preexistance.


Speaking of interop DLLS, Microsoft has confirmed that if the primary interop DLLS in the Edge program
directory are dropped into the GAC (golbal assembly cache), the interop DLLs are registered. I did that
and woe be unto me. The Visual Basic IDE could not let me browse to the .tlb files in the Edge program
dir and add them as references to my VB project. Instead, the IDE added entries to the files in the GAC.
That is bad since an end user's machine will not have the files in the GAC since Edge does not add them
to the GAC. So I deleted the files from the GAC. Then the IDE gave me a message when I browsed to the tlb
files to add them to the project that the "specified file cannot be found" (something like that). So
I searched the registry and found references to the files (no longer) in the GAC all over the place. The
most important ones were for the entries in the TYPELIB registry hives (HKCU and HKLM plus the Wow6432node
equivalents). So I ran regasm /u on the primary interop assemblies. The regasm tool did nothing. Hence
I still could not add references to the typelibs (whether by browsing or using the "COM" entry on the add
reference dialog). So I contacted Microsoft since an entry on the VB MSDN forum from a couple of years
ago went unaswered as did a new one I posted (posted before I found the registry problem). Microsoft has
admitted that the regasm.exe /u (/unregister) fails to remove the entries regasm.exe adds to the registry.
Thought you might want to know in case you ever encounter this issue. Bottom line is never add the 
primary interop assemblies Edge delivers to the GAC (e.g., interop.SolidEdgeFrameworkLib.dll) nor call
regasm.exe on them!

To top off the add reference/GAC issue, the problem did not show up in VS 2003 (and still does not).
Apparently the 2005 IDE (and 2008?) changed to ignore the user's intent when the user actually uses the
browse dialog to specifically choose the .tlb file to import (and get a "local copy"). No, selecting
"local copy" in the property window, which by the way gives the typelib CLSID that is in the TYPELIB
registry hive, has no effect. Hopefully Microsoft will fix this issue but as I said, if you don't put
the edge assemblies in the GAC (or register them), you will not have this problem. But hey, I spent a
long time determining the root cause of the issue so I figured I'd share this info with you as I did with
Microsoft.

Speaking of the interop assemblies (DLLs) created when adding a reference to a typelib file, make sure
you always have the "Local copy" property set for the reference. Also make sure that when you deploy
your add-in, those DLLs are copied into the same directory as your add-in code module.


HELP! MY ADD-IN FAILS TO SHOW UP IN EDGE BUT THE ADD-IN MANAGER LISTS THE ADD-IN!

The add-in is not fully registered. Check to see if the "codebase" entry of the "InprocServer32"
registry key exists and has the correct path to the add-in server (DLL). This will occur if you build
without the "Regiter for COM interop" box checked.


Other Issues I encountered and overcame.

I got an "invalid assembly" when I built the x64 targets. I found this is what is needed in a post build step 
to get registered for com interop:

D:\Windows\Microsoft.NET\Framework64\v2.0.50727\RegAsm.exe $(TargetName)$(TargetExt)

But when I added it to the post build step, the x86 build failed with the invalid assembly error (BC40010).

Apparently the IDE doesn't support a different setting for "register for com interop" and the post build
step, for each build target! Ugh.

So what to do? I decided to leave the switch on using the assumption that for now, x86 is the most
common platform target. This means you need to turn that switch off when building the x64 target and
manually register the assembly using the correct regasm.exe tool. But remember, just use the two
bat files that build the debug and release targets before you run to test your code and they should take
care of the registration issue for you.

HELP! My code changes don't seem to work or I am getting "code does not match" icon when I set a breakpoint
and try to run.

If you are building and testing an x86 configuration, and the sample project does not have the register for
COM interop option set. Make sure you also run the corresponding bat file so the code is registered. Ditto
for the x64 targets (especially since you have to turn off the register for com interop switch yourself and
use the bat file or invoke the 64 bit regasm.exe tool).

Speaking of 32 versus 64 bit and the tools involved, Microsoft Visual Studio 2005 and 2008 have a tools
submenu under their main menu. Both of the tools sub menus have "command prompt" entries. One is for
an x86 environment and one exists for the x64 environment. If you open the correct window and run, e.g.,
regasm.exe, the tool will succeed when run against the corresponding (32 or 64 bit) assembly/DLL.


When first installing on a clean machine, using the bat files fails to compile because of missing
interop DLLs! Yes the compile line utilites, since they do not want to assume the install location of
Edge (or even that edge is on a machine), do NOT invoke tlbimp.exe to build the interop assemblies from
the type libraries. So before using the bat files, open the IDE and build each target so that the IDE
generates the interop assemblies from the type lib files.


Enjoy.