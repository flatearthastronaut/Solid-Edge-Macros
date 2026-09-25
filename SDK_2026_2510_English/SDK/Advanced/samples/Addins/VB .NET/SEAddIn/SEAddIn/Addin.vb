Imports Microsoft.VisualBasic
Imports Microsoft.Win32
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.ComTypes
Imports System.Windows.Forms
Imports SolidEdgeFramework
Imports System.Reflection


'TODO: If you are basing your add-in on this sample -
' 1) Generate your own GUID and replace the one below in the GuidAttribute to avoid
'    clasing with the sample add-in.
' 2) Give your add-in a unique program Id. For example, XYZCorp.SEAddin

'Developers Note: .NET has added a FinalReleaseComObject API. The newer API will release the
' actual Solid Edge object immediately so one doesn't have to wait for GC to collect the RCW, which
' is when the call to release the Edge object actually occurs if one calls ReleaseComObject.
'
' BUT BEWARE - FinalReleaseComObject will not only affect one .NET client, but any other .NET client
' that may happen to be using the same Edge object! Unfortunately this sample was changed in ST4 before
' that fact was determined. So this sample only calls FinalReleaseComObject on objects for which the addin
' knows only it should have a reference.

<System.Runtime.InteropServices.GuidAttribute("5B651DC4-F307-4571-B961-45F5285BA999")> _
<System.Runtime.InteropServices.ProgId("Siemens.SEAddinSample")> _
<System.Runtime.InteropServices.ComVisible(True)> Public Class Addin

    Implements SolidEdgeFramework.ISolidEdgeAddIn
    Implements SolidEdgeFramework.ISEApplicationEvents
    Implements SolidEdgeFramework.ISEFileUIEvents

    Implements System.IDisposable

    ' Declare member arrays to hold command IDs. These are set to identifiers defined by
    ' the addin and passed to Edge. After passing them to Edge, the arrays will be updated
    ' with the command IDs edge dynamically assigns to the actual command controls such as
    ' buttons and menu entries. An addin typically does not need those IDs but they would be
    ' needed, for example, to call the Edge application object's StartCommand API or to
    ' directly send the edge app window a WM_COMMAND message (which is what StartCommand api
    ' actually does).
    Private ApplicationCommandIDs(2) As Integer
    Private PartCommandIDs(2) As Integer
    Private SketchCommandIDs(2) As Integer
    Private AssemblyCommandIDs(2) As Integer
    Private DraftCommandIDs(2) As Integer

    ' Declare member to handle commands
    Private mCommands As Commands

    ' I will keep a copy of the Edge application interface
    Private pApplication As SolidEdgeFramework.Application

    Private Application_CP As IConnectionPoint
    Private Application_CP_Cookie As Integer

    Private FileUI_CP As IConnectionPoint
    Private FileUI_CP_Cookie As Integer

    Private pshortcutevents As SolidEdgeFramework.ISEShortCutMenuEvents_Event
    ' Set up to create edge bar pages
    Private pEdgeBarPages As Collection

    Dim pAddin As SolidEdgeFramework.AddIn


    Private Sub AddOrRemoveEventHandlers(ByVal Add As Boolean)
        If Add Then

            ' Sept. 19, 2022 - In ST7, the Solid Edge APIs were reworked to provide light weight wrapper
            ' objects that can be returned to clients and which do not suffer the limitations outlined
            ' below when RCWs are created for them. Thus it is NO LONGER NECESSARY TO CALL ReleaseComObject.
            ' I leave the long comment that explains the issues RCWs presented to, not only Solid Edge, but
            ' other applications such as Excel, Word ... It doesn't hurt to make the calls but they are no
            ' longer needed. If any issue arises where such a call "fixes" the issue, let Solid Edge support
            ' know about it as it is now considered a defect/bug in Solid Edge code. Until an RCW releases
            ' an API object, only a tiny bit of memory is leaked and it really isn't worth the effort to try
            ' and avoid that. Also, Solid Edge no longer runs .NET garbage collection. Just let the .NET
            ' framework do so when it decides to.

            ' .NET COM interop is a real mess. The VB user now has to manage the lifetime of "RCWs", runtime
            ' callable wrappers that .NET creates to "wrap" a COM object. The big issue is that the RCW will
            ' not release the COM object until the RCW is garbage collected. When will GC be performed on an
            ' individual RCW? Hard to tell. Depends on .NET memory usage and a variety of other factors. Edge
            ' will try to force GC to run when a document is closed or the app shuts down. So it is extremely
            ' important to release the com object (using marshal.ReleaseComObject)
            ' even WHEN THE VB CODE ITSELF DOES NOT EXPLICITLY CAUSE AN RCW TO BE CREATED. When does that
            ' happen? One case is whenever Edge fires any event in an event set, regardless of whether the
            ' VB code handles the event or not! Hence, if you handle one event in an event set, you need to
            ' handle every event in the event set that has an Edge object as a parameter simply so you can
            ' release the com object. And what exactly does "ReleaseComObject" do? Does it actually release
            ' the com object? No. It actually "releases" the RCW and if the reference count goes to zero,
            ' then and only then is the RCW a candidate for GC (garbage collection). When GC actually occurs
            ' on a RCW, the RCW then releases the actual COM object. Since Edge MUST unmap memory when a 
            ' document is closed (lots of megabytes, even hundreds of them can be tied up in a document),
            ' relying on the psuedo non-deterministic nature of the GC of RCWs is risky, to say the least.
            ' Edge will actually try to force all objects that are GC eligible to be collected by doing
            ' the following:
            ' GC.Collect(GC.MaxGenerations), GC.WaitForPendingFinalizers(), GC.Collect(GC.MaxGenerations)
            ' According to the Micorosft .NET team, that is the best approach to trying to force them to
            ' collect all the RCWs. I suggested to the team that as soon as an RCW was GC eligible, it should
            ' release the COM object. I was told that interop was basically done and to live with their
            ' design. I complained loudly that VB 6 users never had to deal with COM lifetime issues like
            ' this but hey, it is Microsoft.
            '
            ' For events I have chosen to have the class implement the interface as opposed to the AddHandler/RemoveHandler
            ' .NET paradigm. There are two reasons I have done this. First, when a COM object such as the solid edge
            ' application object has its event set connected to, the object connecting has to supply an interface
            ' that contains event handlers for every event in the interface. So for example if .NET AddHandler is called
            ' for the BeforeCommandRun event, .NET connects an interface to the application object that handles each event.
            ' Only when BeforeCommandRun is called by the application object does the caller to AddHandler get its event
            ' handler called. .NET "stubs" out all the remaining handlers in the interface and simply returns to edge.
            ' If another event handler is added via AddHandler, .NET will connect an entirely different interface to
            ' edge. Hence if ten handlers are added, Edge fires every event ten times to .NET for every event fired.
            ' By using the "Implements" paradigm, .NET only connects the specific interface to Edge. This reduces the
            ' overhead in Edge for firing events.
            '
            ' Actually the above is a bit misleading when I say .NET "stubs" out all the remaining handlers in the interface
            ' and simply returns to edge. If the event that is stubbed out has any COM object passed to .NET from Edge,
            ' .NET still creates the RCW, which is sometimes called a "ghost RCW" since the .NET client programmer never
            ' "sees" (encounters) the RCW since no code was written. By implementing the interface, one is forced to write
            ' minimum code for each event and that means the .net programmer has direct knowledge that the RCW(s) are indeed
            ' being created and hence can call ReleaseComObject.
            '
            ' The second reason to use "Implements" is more subtle and is related to how Solid Edge handles file UI events.
            ' Since the .NET runtime actually connects a new event interface to Edge each time AddHandler is called, 
            ' if the user adds more than one handler to the FileUI event source, when Edge
            ' fires events there appears to be multiple listeners to Edge and only one returns E_NOTIMPL (via the exception thrown in 
            ' the code below) and the other stubs (unseen by the .net programmer) return S_OK. When S_OK is sent to Edge, Edge thinks 
            ' the user "canceled" the listener's own file UI since no filename is returned. Thus Edge will not show its own
            ' file processing dialog.
            ' This means that if AddHandler is used for file UI events, the .NET programmer can find that everything works fine
            ' if there is only one call to AddHandler for the UI events but can find a problem when AddHandler is called for a
            ' second file UI event.
            Dim CPC As IConnectionPointContainer

            CPC = pApplication

            If Not CPC Is Nothing Then
                Dim FileUI_Events_guid As Guid = New Guid("ECC667A1-A4AA-11D1-AECC-08003616CE02") ' I used oleview.exe framewrk.tlb and then found the interface to get this guid!
                Dim Application_Events_guid As Guid = New Guid("90223887-09CD-11D1-BA07-080036230602") ' I used oleview.exe framewrk.tlb and then found the interface to get this guid!

                CPC.FindConnectionPoint(Application_Events_guid, Application_CP)
                If Not Application_CP Is Nothing Then
                    Application_CP.Advise(Me, Application_CP_Cookie)
                End If

                ' Could not get this to work consistently. At some point this started working after I debugged for a while, running multiple times.
                ' I posted on the MS dev forums and was told it should work. But I did not imagine what happened. I suspect there is some
                ' .net voodoo going on with the meta-type data being generated at runtime. Hence the code above that explicitly defined the guids.

                'Dim i As Type

                'i = Me.GetType.GetInterface("ISEFileUIEvents")

                'FileUI_Events_guid = i.GUID

                CPC.FindConnectionPoint(FileUI_Events_guid, FileUI_CP)
                If Not FileUI_CP Is Nothing Then
                    FileUI_CP.Advise(Me, FileUI_CP_Cookie)
                End If

            End If

            If Not pshortcutevents Is Nothing Then
                AddHandler pshortcutevents.BuildMenu, AddressOf ShortcutEvents_BuildMenu
            End If
        Else
            If Not Application_CP Is Nothing Then
                If Not Application_CP_Cookie = -1 Then
                    Application_CP.Unadvise(Application_CP_Cookie)
                End If
            End If

            If Not FileUI_CP Is Nothing Then
                If Not FileUI_CP_Cookie = -1 Then
                    FileUI_CP.Unadvise(FileUI_CP_Cookie)
                End If
            End If

            If Not pshortcutevents Is Nothing Then
                RemoveHandler pshortcutevents.BuildMenu, AddressOf ShortcutEvents_BuildMenu

                ' Must call ReleaseComObject now or GC will do so at shutdown as DLLs are unloading, which is
                ' a major problem since there is no control over the order DLLs are unloaded and that can lead
                ' to exceptions that may or may not be caught by the .NET runtime.
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pshortcutevents)
                pshortcutevents = Nothing
            End If

            End If
    End Sub

#Region "SolidEdgeAddInInterface"

    Private Sub ISolidEdgeAddIn_OnConnection(ByVal Application As Object, ByVal ConnectMode As SolidEdgeFramework.SeConnectMode, ByVal AddInInstance As SolidEdgeFramework.AddIn) Implements SolidEdgeFramework.ISolidEdgeAddIn.OnConnection

        pAddin = AddInInstance
        pApplication = Application

        ' We have shutdown issues due to garbage collection running when the framework is unloaded because it
        ' can run after Edge DLLs are unloaded. Microsoft support said to avoid the VB compatibility DLL's
        ' support for "WithEvents" and do events the .NET way. So I am using AddHandler and RemoveHandler
        ' in a new routine, AddOrRemoveEventHandlers.
        pshortcutevents = pApplication.ShortcutMenuEvents

        ' add event handlers. Handlers are removed in the Dispose method of the commands object.
        AddOrRemoveEventHandlers(True)

        mCommands = New Commands
        mCommands.m_SEapp = Application
        mCommands.m_myAddIn = AddInInstance
        mCommands.m_addinEvents = AddInInstance.AddInEvents

        ' add event handlers. Handlers are removed in the Dispose method of the commands object.
        mCommands.AddOrRemoveEventHandlers(True)

        ' The GUI version should be incremented anytime changes to the UI occur such as adding
        ' new commands to a command bar, removing commands from the add-in etc. Edge will detect
        ' the change and automatically purge the system of any saved command bar data, user assigned
        ' accelerators and other data saved relating to the add-in.
        AddInInstance.GuiVersion = 1
        ' Let the add-in be seen by the user when the add-in manager runs.
        AddInInstance.Visible = True

        ' The following code loads the win32 resource module handle. This handle is passed to edge
        ' for example, when adding a dialog to the command ribbon bar. Also, strings in the string
        ' table can be retrieved using windows apis that take in the handle to the resource module.
        ' There are two approaches for resources. One is to embed the win32 resources directly in
        ' the module (must use the vbc command line compile "/win32Resource:myresource.res" option.
        ' The other is to build a separate resource only module. The advantage of the latter is that
        ' changes to the add-in code (say for a maintenance pack) does not affect the resources since
        ' they are separated from the code (no internationalization issues when making fixes to code).

        ' This add-in assumes the resources are separate from the code and the module is registered
        ' with a codebase (codebase may still be valid if not registered that way but I did not test
        ' that). It also assumes the resource dll sits in the same directory as the assembly (dll)
        ' code module.

        Dim MyModuleFilename As String

        MyModuleFilename = Me.GetType().Module.Assembly.GetName().CodeBase

        Dim MyResourceModuleFilename As String

        ' convert to lower case to avoid any issue with case of chars. For example, even though the
        ' build outputs the file with .dll, CodeBase returns .DLL!
        MyModuleFilename = MyModuleFilename.ToLower()

        MyResourceModuleFilename = MyModuleFilename.Replace("seaddin.dll", "seaddinres.dll")

        Dim Filename As String

        ' codebase name has "file:///". So remove first 8 chars
        Filename = MyResourceModuleFilename.Remove(0, 8)

        Dim ResHandle As Int32

        ' Is ResHandle coming back as zero? The VB IDE creates two DLLs when I am building debug.
        ' One goes in a "obj\x86\Debug" dir and another in "bin\x86\debug" dir. The SEAddInRes.dll
        ' has to be in the correct location. I have a compiledebugx86.bat file that builds the win32
        ' resource directly into the code module AND builds a separate resource. Since building from
        ' the IDE loses the resources in the code module, I normally have to build from the command
        ' line after making code changes and building (to fix compile errors) from the IDE. But
        ' for some reason, all of a sudden, everytime I ran debug, the IDE would rebuild the project,
        ' whether I just built from the IDE and not the command line or not. Been doing this for a
        ' couple of days with no problem and then BAM! the IDE just decided it has to build everytime
        ' I git the "go" button. Think I'll modify the build bat file to just copy the freaking res
        ' dll to every directory. I'm really beginning to love VB .NET. Either that or I'm beginning
        ' to see why Jason Newell dropped it in favor of Visual C++.
        ResHandle = LoadResourceFile(Filename)

        SetResourceFilename(Filename)

        If 0 <> ResHandle Then
            SetResourceHandle(ResHandle)
        Else
            ' Set up the handle to the win32 resources to make access easy across classes and modules.
            ' This assumes you have built the win32 resources into the add-ins assembly (DLL) using the
            ' vbc command line compiler and the /win32Resource:ResTempl1.res switch.
            SetResourceHandle(Marshal.GetHINSTANCE(Me.GetType().Module).ToInt32())
            SetResourceFilename(Me.GetType().Module().FullyQualifiedName())

        End If

        ' Addin by default will be added to "addins" tab. To get a tab of its own, simply
        ' register the addin description with "\n" prepended to the description or add the
        ' \n progrmatically. I retrieve the description from the resource file. Uncomment
        ' the code that prepends the character if you want the sample to have a tab of its own.
        Dim Description As String
        Description = GetResourceString(104)
        If Description.Length = 0 Then
            Description = "Sample VB Addin"
            'Description = Chr(10) & "Sample VB Addin"
            'Else
            'Description = Chr(10) & Description
        End If

        ' Want a tab all your own? When registering the description, prepend "\n" to the description.
        ' Or, simply set the description now and prepend "\n". Normally an add-in will appear on the
        ' Add-Ins tab (or tools menu in Pre-ST edge). But the "\n" tells edge to give the add-in its
        ' own tab (or top-level menu in Pre-ST edge). Uncomment the next line to make that happen.
        'Description = Chr(10) & Description

        AddInInstance.Description = Description

    End Sub

    Private Sub ISolidEdgeAddIn_OnConnectToEnvironment(ByVal EnvCatID As String, ByVal pEnvironmentDispatch As Object, ByVal bFirstTime As Boolean) Implements SolidEdgeFramework.ISolidEdgeAddIn.OnConnectToEnvironment

        ' Tech note: Calling SetAddinInfo directly worked ok in the .NET version 1 (.1). But then Microsoft upgraded to
        ' version 2 and broke the call. I worked with Microsoft and they suggested the work-around  below that uses
        ' reflection. Jason Newell also arrived at that solution. Microsoft fixed the issue with a service pack.

        On Error Resume Next

        Dim CmdIds As System.Array

        Dim CmdImageMediumColor As Integer
        Dim CmdImageLargeColor As Integer
        Dim CmdImageMediumBlackAndWhite As Integer
        Dim CmdImageLargeBlackAndWhite As Integer

        CmdImageMediumColor = 101
        CmdImageLargeColor = 102
        'monochrome support ended long ago (could always send in -1 for any image)
        CmdImageMediumBlackAndWhite = -1
        CmdImageLargeBlackAndWhite = -1

        'String format for Edge is: "UniqueCommandString\nCaption\nDescription\nTooltip". The first part can be
        ' used to find a control via the command bars FindControl API (if the command ID is unknown or not used).
        Dim CommandNames(2) As String

        CommandNames(0) = GetResourceString(101)
        If CommandNames(0).Length = 0 Then
            CommandNames(0) = "XYZ VB addin sample command 1" & Chr(10) & "VB Command1" & Chr(10) & "Sample 1" & Chr(10) & "Sample command 1"
        End If
        CommandNames(1) = GetResourceString(102)
        If CommandNames(1).Length = 0 Then
            CommandNames(1) = "XYZ VB addin sample command 2" & Chr(10) & "VB Command2" & Chr(10) & "Sample 2" & Chr(10) & "Sample command 2"
        End If

        Dim CategoryName As String
        CategoryName = GetResourceString(103)
        If CategoryName.Length() = 0 Then
            CategoryName = "VB .NET AddIn"
        End If

        Dim ResHandle As Int32
        Dim ResFilename As String

        ResHandle = GetResourceHandle()
        ResFilename = GetResourceFilename()

        ' After calling SetAddinInfo, the command IDs will be updated to the unique actual runtime-assigned command IDs used in the
        ' Solid Edge UI. Use them to directly start an add-in command (using WM_COMMAND or the Edge app StartCommand API) or to
        ' add command bar buttons or perhaps on shortcut menus (added when the shortcut menu event is fired, which this add-in does
        ' in at least one case just to demo that ability.

        ' add the commands to the application environment

        ' Module handles are 64 bits in a 64 bit application. They simply cannot be passed to Edge using the original SetAddInInfo
        ' API. When 64 bit Edge came out, a new interface was created that "Ex tends" the original interface by one method that
        ' takes in a resource module filename instead of a handle. Edge will load the resource module as a data file, which also
        ' avoids a pitfall where a resource module may accidentally contain an entry point (code - think "dll main") that can keep the 
        ' module from loading if an attempt to load one with a 32 bit entry point is made by calling the windows API LoadLibrary or
        ' LoadLibraryEx where the input flag is not the flag for a data file.
        Dim pAddinEx As SolidEdgeFramework.ISEAddInEx

        pAddinEx = pAddin

        If (LCase(EnvCatID) = LCase(CATID_SEApplication)) Then
            ApplicationCommandIDs(0) = 1
            ApplicationCommandIDs(1) = 2

            If Not pAddinEx Is Nothing Then
                pAddinEx.SetAddInInfoEx(ResFilename, EnvCatID, CategoryName, CmdImageMediumColor, CmdImageLargeColor, CmdImageMediumBlackAndWhite, CmdImageLargeBlackAndWhite, 2, CommandNames, ApplicationCommandIDs)
            Else
                pAddin.SetAddInInfo(ResHandle, EnvCatID, CategoryName, CmdImageMediumColor, CmdImageLargeColor, CmdImageMediumBlackAndWhite, CmdImageLargeBlackAndWhite, 2, CommandNames, ApplicationCommandIDs)
            End If

            ' Note: When Microsoft updated the .NET runtime from 1.1(?) to 2.0(?), they broke the ability to call SetAddInInfo
            ' directly as I do above. That led to the following code, which I leave here but comment out. I assume you are using
            ' the latest Visual Studio and have the service pack for the .NET framework that fixed the issue. If not, the code
            ' below still works.
            'Dim p As New ParameterModifier(10)

            'Dim objMethodArgs() As Object = {Marshal.GetHINSTANCE(Me.GetType().Module).ToInt32(), EnvCatID, "VB .NET AddIn", CmdImageMediumColor, CmdImageLargeColor, CmdImageMediumBlackAndWhite, CmdImageLargeBlackAndWhite, 2, CommandNames, ApplicationCommandIDs}

            'p(9) = True

            'Dim mods() As ParameterModifier = {p}

            'CType(pAddin, Object).GetType().InvokeMember("SetAddInInfo", System.Reflection.BindingFlags.InvokeMethod, _
            '                                             Nothing, pAddin, objMethodArgs, mods, Nothing, Nothing)

            'CmdIds = objMethodArgs(9)

            'ApplicationCommandIDs(0) = CmdIds(0)
            'ApplicationCommandIDs(1) = CmdIds(1)
        End If

        ' add the commands in part
        If (LCase(EnvCatID) = LCase(CATID_SEPart)) Then
            PartCommandIDs(0) = 1
            PartCommandIDs(1) = 2

            If Not pAddinEx Is Nothing Then
                pAddinEx.SetAddInInfoEx(ResFilename, EnvCatID, CategoryName, CmdImageMediumColor, CmdImageLargeColor, CmdImageMediumBlackAndWhite, CmdImageLargeBlackAndWhite, 2, CommandNames, PartCommandIDs)
            Else
                pAddin.SetAddInInfo(ResHandle, EnvCatID, CategoryName, CmdImageMediumColor, CmdImageLargeColor, CmdImageMediumBlackAndWhite, CmdImageLargeBlackAndWhite, 2, CommandNames, PartCommandIDs)
            End If
        End If

        If (LCase(EnvCatID) = LCase(CATID_SESketch)) Then
            SketchCommandIDs(0) = 1
            SketchCommandIDs(1) = 2

            If Not pAddinEx Is Nothing Then
                pAddinEx.SetAddInInfoEx(ResFilename, EnvCatID, CategoryName, CmdImageMediumColor, CmdImageLargeColor, CmdImageMediumBlackAndWhite, CmdImageLargeBlackAndWhite, 2, CommandNames, SketchCommandIDs)
            Else
                pAddin.SetAddInInfo(ResHandle, EnvCatID, CategoryName, CmdImageMediumColor, CmdImageLargeColor, CmdImageMediumBlackAndWhite, CmdImageLargeBlackAndWhite, 2, CommandNames, SketchCommandIDs)
            End If
        End If

        ' add the commands in assembly
        If (LCase(EnvCatID) = LCase(CATID_SEAssembly)) Then
            AssemblyCommandIDs(0) = 1
            AssemblyCommandIDs(1) = 2

            If Not pAddinEx Is Nothing Then
                pAddinEx.SetAddInInfoEx(ResFilename, EnvCatID, CategoryName, CmdImageMediumColor, CmdImageLargeColor, CmdImageMediumBlackAndWhite, CmdImageLargeBlackAndWhite, 2, CommandNames, AssemblyCommandIDs)
            Else
                pAddin.SetAddInInfo(ResHandle, EnvCatID, CategoryName, CmdImageMediumColor, CmdImageLargeColor, CmdImageMediumBlackAndWhite, CmdImageLargeBlackAndWhite, 2, CommandNames, AssemblyCommandIDs)
            End If
        End If

        ' register the commands in part
        If (LCase(EnvCatID) = LCase(CATID_SEDraft)) Then
            'V20:- AS THE NUMBER OF COMMANDS INCREASED, ADD CODE FOR FOLLOWING
            DraftCommandIDs(0) = 1 ' IMP
            DraftCommandIDs(1) = 2

            If Not pAddinEx Is Nothing Then
                pAddinEx.SetAddInInfoEx(ResFilename, EnvCatID, CategoryName, CmdImageMediumColor, CmdImageLargeColor, CmdImageMediumBlackAndWhite, CmdImageLargeBlackAndWhite, 2, CommandNames, DraftCommandIDs)
            Else
                pAddin.SetAddInInfo(ResHandle, EnvCatID, CategoryName, CmdImageMediumColor, CmdImageLargeColor, CmdImageMediumBlackAndWhite, CmdImageLargeBlackAndWhite, 2, CommandNames, DraftCommandIDs)
            End If
        End If

        ' SetAddinInfo will automatically create a menu in pre-ST edge. But with ST, there is no menu
        ' so Edge automatically creates a command ribbon group in ST and puts the icon on the button
        ' control added to the ribbon. But it adds no text.

        ' In order to get text on the command ribbon, I need to create a command bar button so I can
        ' set the style to indicate I want an icon and text. This only needs to be done the first time
        ' the add-in is connected to since Edge saves command bar data between sessions (plus users can
        ' customize the UI and we don't want to keep recreating the stock command bar).
        If bFirstTime Then
            Dim Button As SolidEdgeFramework.CommandBarButton

            ' The addin API will automatically create a comamnd bar with the given name and add my
            ' command to the bar. No need to pass in the dynamically assigned command ID since I am
            ' using the addin API since it knows what add-in I am. Since I use the same IDs (1 & 2)
            ' for each environment, I can make these same calls for each environment.
            ' More customization can be achieved using the environment command bar APIs. Adding 
            ' buttons via that API requires the unique command IDs returned in this add-ins various 
            ' command ID arrays (different for each environment). But the addin API is very
            ' convenient since it will automatically pick up the tooltip, description and icon.
            Button = pAddin.AddCommandBarButton(EnvCatID, CategoryName, 1)

            If Not Button Is Nothing Then
                Button.Style = SeButtonStyle.seButtonIconAndCaption
            End If

            Button = pAddin.AddCommandBarButton(EnvCatID, CategoryName, 2)

            If Not Button Is Nothing Then
                Button.Style = SeButtonStyle.seButtonIconAndCaption
            End If

        End If

        If Not pEnvironmentDispatch Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pEnvironmentDispatch)
        End If

    End Sub

    Private Sub ISolidEdgeAddIn_OnDisconnection(ByVal DisconnectMode As SolidEdgeFramework.SeDisconnectMode) Implements SolidEdgeFramework.ISolidEdgeAddIn.OnDisconnection
        On Error Resume Next

        RemoveAllEdgeBarPages()
    End Sub
#End Region

#Region "REGISTRATION"

    ' Code to register/unregister the additional add-in data was derived from Jason Newell (jasonnewell.net)

    ' When Regasm is run on this project, either through IDE or command window, these functions will be called.

    ' Setup the required Solid Edge registry values for an addin that were not automatically
    ' added due to the guid, progid and com visible attributes.
    <ComRegisterFunctionAttribute()> _
    Public Shared Sub RegisterFunction(ByVal t As Type)

        Dim Key As RegistryKey = Registry.ClassesRoot.CreateSubKey("CLSID\{" + t.GUID.ToString() + "}")

        If Not (Key Is Nothing) Then
            ' Tell Edge to automatically connect to the add-in.
            Key.SetValue("AutoConnect", 1)
            ' Set the description
            Key.SetValue("409", "SampleAddin")
            ' Set the summary
            Key.SetValue("Summary", "Sample VB .NET add-in")
            ' Add the Microsoft standard "Implemented Categories" subkey and add ISolidEdgeAddIn as
            ' an implemented category. This is what allows Solid Edge to use the Windows registry
            ' APIs to find an addin registered on the machine.
            Key.CreateSubKey("Implemented Categories\" & CATID_SolidEdgeAddIn)
            ' Set the environment categories to indicate what environments the add-in should
            ' be connected to.
            Key.CreateSubKey("Environment Categories\" & CATID_SEApplication)
            Key.CreateSubKey("Environment Categories\" & CATID_SEPart)
            Key.CreateSubKey("Environment Categories\" & CATID_SEAssembly)
            Key.CreateSubKey("Environment Categories\" & CATID_SEDraft)
            Key.CreateSubKey("Environment Categories\" & CATID_SESketch)

            Key.Close()
        End If
    End Sub

    ' Remove any registry values specifically added above.
    <ComUnregisterFunctionAttribute()> _
    Public Shared Sub UnregisterFunction(ByVal t As Type)
        Registry.ClassesRoot.DeleteSubKeyTree("CLSID\{" + t.GUID.ToString() + "}")
    End Sub
#End Region


    Private disposedValue As Boolean = False        ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' TODO: free managed resources when explicitly called
                ' I commented out the code that obtained the app events and I crashed when shutting down
                ' because the shortcut events event provider Finalize method ran. That implies I did not
                ' release the com object for that event leading me to assume that I had an exception here
                ' so I added each test below
                AddOrRemoveEventHandlers(False)

                If Not pAddin Is Nothing Then
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(pAddin)
                    pAddin = Nothing
                End If

                If Not pApplication Is Nothing Then
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(pApplication)
                    pApplication = Nothing
                End If

                If Not mCommands Is Nothing Then
                    mCommands.Dispose()
                End If
            End If

            ' TODO: free shared unmanaged resources
        End If
        Me.disposedValue = True
    End Sub

#Region " IDisposable Support "
    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region

#Region "EdgeBar"
    Private Function AddEdgeBarPage(ByRef theDocument As Object) As Boolean

        On Error Resume Next
        Dim hWndTab As Integer
        Dim lngReturn As Integer
        Dim objISolidEdgeBar As SolidEdgeFramework.ISolidEdgeBar
        Dim objForm As EdgeBarForm

        ' Ensure we don't add an EdgeBarTab for a document more than once.
        For Each objForm In pEdgeBarPages
            If objForm.Document Is theDocument Then
                AddEdgeBarPage = False
                Exit Function
            End If
        Next objForm

        Dim Tooltip As String
        Tooltip = GetResourceString(104)

        Dim ResHandle As Int32

        ResHandle = GetResourceHandle()

        Dim ResFilename As String

        ResFilename = GetResourceFilename()

        ' Cast the pAddin object as ISolidEdgeBar
        objISolidEdgeBar = pAddin

        Dim SolidEdgeBarEx As SolidEdgeFramework.ISolidEdgeBarEx

        SolidEdgeBarEx = pAddin

        ' Module handles are 64 bits in a 64 bit application. They simply cannot be passed to Edge using the original AddPage
        ' API. When 64 bit Edge came out, a new interface was created that "Ex tends" the original interface by one method that
        ' takes in a resource module filename instead of a handle. Edge will load the resource module as a data file, which also
        ' avoids a pitfall where a resource module may accidentally contain an entry point (code - think "dll main") that can keep the 
        ' module from loading if an attempt to load one with a 32 bit entry point is made by calling the windows API LoadLibrary or
        ' LoadLibraryEx where the input flag is not the flag for a data file.

        If Not SolidEdgeBarEx Is Nothing Then
            hWndTab = SolidEdgeBarEx.AddPageEx(theDocument, ResFilename, 103, Tooltip, SolidEdgeConstants.EdgeBarConstant.DONOT_MAKE_ACTIVE)
        Else
            hWndTab = objISolidEdgeBar.AddPage(theDocument, ResHandle, 103, Tooltip, SolidEdgeConstants.EdgeBarConstant.DONOT_MAKE_ACTIVE)
        End If

        If hWndTab Then
            objForm = New SEAddIn.EdgeBarForm

            objForm.EdgeBarPageHandle = hWndTab

            ' Reparent the form to the newly added page.
            Call ChangeParentWindow(objForm.Handle.ToInt32, objForm.EdgeBarPageHandle, False)

            ' Pass specified objects to the form.
            objForm.Initialize(pAddin, theDocument)

            ' Add the newly created frmEdgeBarPage to the collection for later reference.
            Call pEdgeBarPages.Add(objForm)

            objForm.Show()

            AddEdgeBarPage = True
        End If
        '-------------------------------------
        'End Fix PR - 5551133
    End Function

    Private Function RemoveEdgeBarPage(ByRef theDocument As Object) As Boolean

        On Error Resume Next

        RemoveEdgeBarPage = False

        Dim hWndTab As Integer
        Dim objISolidEdgeBar As SolidEdgeFramework.ISolidEdgeBar
        Dim objForm As EdgeBarForm
        Dim i As Short

        objISolidEdgeBar = pAddin

        i = 1
        For Each objForm In pEdgeBarPages
            If objForm.Document Is theDocument Then
                hWndTab = objForm.EdgeBarPageHandle

                RemoveEdgeBarPage = True

                ' I have been trying to get the Form to be cleaned up when edge runs garbage
                ' collection. I had the managed memory dump on the form down to only being referenced by a
                ' "native window" object, which in turn referenced the form. Microsoft support said that closing
                ' the form should result in the "native window" releasing the form. I know that RemovePage will
                ' destroy the input hWndTab and the form is a child so it too should be destroyed. So I tried
                ' two things, first I did a "Form.Parent = nothing". But still the form remains in the heap.
                ' So I move Close back to being before RemovePage. I got a problem calling RemovePage because
                ' when the form closes, it calls ReleaseComObject on the document and it was the only reference
                ' on the document (apparently) so .NET runtime thru an exception because when I called RemovePage, the RCW
                ' was disconnected from the com object. So I added a "Set" on the Form.Document property to
                ' set the Form.Document to nothing so I can call Close and then RemovePage.
                '
                ' If some other issue arises, there is a backup plan. Before RemovePage and Close is called, we
                ' could try calling ChangeParentWindow and pass in a null handle for the parent.

                objForm.Document = Nothing

                objForm.Close()

                Call objISolidEdgeBar.RemovePage(theDocument, hWndTab, 0)

                objForm = Nothing
                Call pEdgeBarPages.Remove(i)
                Exit For
            End If
            i = i + 1
        Next objForm
    End Function

    Private Sub RemoveAllEdgeBarPages()

        On Error Resume Next
        Dim hWndTab As Integer
        Dim objISolidEdgeBar As SolidEdgeFramework.ISolidEdgeBar
        Dim objForm As EdgeBarForm
        Dim i As Short

        Dim pDocument As SolidEdgeFramework.SolidEdgeDocument 'added by Manisha on 20-Nov-06

        ' critical line
        objISolidEdgeBar = pAddin

        For Each objForm In pEdgeBarPages

            pDocument = objForm.Document
            objForm.Document = Nothing

            hWndTab = objForm.EdgeBarPageHandle
            objForm.Close()
            objISolidEdgeBar.RemovePage(pDocument, hWndTab, 0)
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pDocument)
            pDocument = Nothing
        Next objForm
        ' reset the entire collection
        For i = 1 To pEdgeBarPages.Count()
            pEdgeBarPages.Remove((i))
        Next

    End Sub

#End Region

#Region "Shortcut Events"

    Public Sub ShortcutEvents_BuildMenu(ByVal EnvCatID As String, ByVal Context As SolidEdgeFramework.ShortCutMenuContextConstants, ByVal pGraphicDispatch As Object, ByRef MenuStrings As System.Array, ByRef CommandID As System.Array)

        ' Demo shortcut menu events. Open an assembly, right click no a part in the pathfinder pane. This event handler
        ' will be called. Note that the AssemblyCommandIDs(0) is not zero. It will be the unique ID dynamically assigned
        ' by edge when SetAddinInfo was called. Edge has no idea what object handles the event call (multiple objects can do so).

        Dim objSelectset As SolidEdgeFramework.SelectSet

        Dim LowerEnvCatiD As String
        Dim LowerSEAssembly As String

        LowerEnvCatiD = EnvCatID.ToLower()
        LowerSEAssembly = CATID_SEAssembly.ToLower()

        If LowerEnvCatiD = LowerSEAssembly Then
            ' the code here will only display a menu on a part
            objSelectset = pApplication.ActiveDocument.SelectSet

            If Not objSelectset Is Nothing Then
                ' if the set is <> 1 and not a part get out (don't add the menu item)
                If objSelectset.Count <> 1 Then
                    Exit Sub
                ElseIf objSelectset.Item(1).Type <> SolidEdgeFramework.ObjectType.igPart Then
                    Exit Sub
                Else
                    Try
                        Dim MenuString As String
                        MenuString = GetResourceString(106)
                        If MenuString.Length() = 0 Then
                            MenuString = "VB addin sample command 1"
                        End If
                        MenuStrings = New String() {MenuString}
                        CommandID = New Integer() {AssemblyCommandIDs(0)}
                    Catch e As Exception
                        MsgBox(e.Message)
                    Finally
                    End Try
                End If

                System.Runtime.InteropServices.Marshal.ReleaseComObject(objSelectset)

                objSelectset = Nothing

            End If
        End If
    End Sub
#End Region

#Region "Application Events"
    ' Note that the event methods below will call ReleaseComObject for the passed in RCWs if they are not stored (and if stored by the add-in,
    ' eventually they should be released via that call). With later versions of .NET there is a "FinalReleaseComObject". Don't use that call.
    ' Using that call can actually cause the RCW to be given its final release event if another .NET add-in happens to be holding onto the RCW!
    ' Unfortunately before I realized this, I put out this VB .NET sample full of those calls. No problem as long as no other .NET add-in is
    ' running. But alas, that is not always the case. I jumped on using that API when .NET added it because I found that the actual COM object
    ' inside Solid Edge got a release call when FinalReleaseComObject was called. That avoided problems with calls to the COM object after Edge
    ' unmapped the object from memory (such as a document object when the document was closed) for which all the garbage collection calls in Edge
    ' were trying to address. Note that none of the events here (or code in this add-in) attempt to run garbage collection. Leave that up to Edge
    ' since there can be multiple .NET add-in running and we don't want to bog the system down by having each one run GC on its own. Note that
    ' Edge also handles multiple .NET runtimes loaded due to multiple add-in running in different .NET runtimes (e.g, the 2.0 and 4.0 runtime)
    ' by running GC in each.

    Private Sub AfterActiveDocumentChange(ByVal theDocument As Object) Implements SolidEdgeFramework.ISEApplicationEvents.AfterActiveDocumentChange
        On Error Resume Next

        Dim PageAdded As Boolean

        ' 4/25/8: RDH - Active document can be nothing if last doc is closed so I am testing this to avoid
        ' any null object reference hidden by on error resume next
        If Not theDocument Is Nothing Then

            'If UCase(pApplication.ActiveEnvironment) = "PART" Or UCase(pApplication.ActiveEnvironment) = "ASSEMBLY" Then
            If UCase(pApplication.ActiveEnvironment) = "PART" Or _
            UCase(pApplication.ActiveEnvironment) = "ASSEMBLY" Or _
            UCase(pApplication.ActiveEnvironment) = "DRAFT" Then
                PageAdded = AddEdgeBarPage(theDocument)
            End If
        End If

        If Not PageAdded Then
            If Not theDocument Is Nothing Then
                System.Runtime.InteropServices.Marshal.ReleaseComObject(theDocument)
            End If
        End If
    End Sub

    Private Sub BeforeDocumentClose(ByVal theDocument As Object) Implements SolidEdgeFramework.ISEApplicationEvents.BeforeDocumentClose
        On Error Resume Next

        Dim PageRemoved As Boolean

        PageRemoved = RemoveEdgeBarPage(theDocument)

        ' If the page was removed, then it was added eariler and we did not release the com object since it is stored in the form
        ' so this is the corresponding release
        If PageRemoved Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theDocument)
        End If

        ' This release is for the addref the .NET runtime did when the doc was passed to this event.
        If Not theDocument Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theDocument)
        End If
    End Sub
    Private Sub AfterEnvironmentActivate(ByVal theEnvironment As Object) Implements SolidEdgeFramework.ISEApplicationEvents.AfterEnvironmentActivate
        On Error Resume Next

        Dim theDocument As Object

        ' 4/25/8: RDH - No doc if the env is the app so avoid any attempt to get the doc to avoid "noise" when
        ' debugging while trying to trap all .NET runtime exceptions. In the case of the app env, trying to get
        ' the active doc from the app results in a com error being return that then becomes an exception
        If UCase(pApplication.ActiveEnvironment) <> "APPLICATION" Then
            If UCase(pApplication.ActiveEnvironment) <> "PART" And _
            UCase(pApplication.ActiveEnvironment) <> "ASSEMBLY" And _
            UCase(pApplication.ActiveEnvironment) <> "DRAFT" Then
                ' its some sub environment, remove the edgebar pages
                Call RemoveEdgeBarPage(pApplication.ActiveDocument)
            Else
                Call AddEdgeBarPage(pApplication.ActiveDocument)
            End If
        End If

        If Not theEnvironment Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theEnvironment)

        End If
    End Sub
    Private Sub AfterWindowActivate(ByVal theWindow As Object) Implements SolidEdgeFramework.ISEApplicationEvents.AfterWindowActivate
        On Error Resume Next

        If Not theWindow Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theWindow)
        End If
    End Sub

    Private Sub BeforeWindowDeactivate(ByVal theWindow As Object) Implements SolidEdgeFramework.ISEApplicationEvents.BeforeWindowDeactivate
        On Error Resume Next

        If Not theWindow Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theWindow)
        End If
    End Sub
    Private Sub BeforeEnvironmentDeactivate(ByVal theEnvironment As Object) Implements SolidEdgeFramework.ISEApplicationEvents.BeforeEnvironmentDeactivate
        On Error Resume Next

        If Not theEnvironment Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theEnvironment)
        End If
    End Sub
    Private Sub AfterNewDocumentOpen(ByVal theDocument As Object) Implements SolidEdgeFramework.ISEApplicationEvents.AfterNewDocumentOpen
        On Error Resume Next

        If Not theDocument Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theDocument)
        End If
    End Sub
    Private Sub AfterDocumentOpen(ByVal theDocument As Object) Implements SolidEdgeFramework.ISEApplicationEvents.AfterDocumentOpen
        On Error Resume Next

        If Not theDocument Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theDocument)
        End If
    End Sub

    Private Sub AfterDocumentPrint(ByVal theDocument As Object, ByVal hDC As Integer, ByRef ModelToDC As Double, ByRef Rect As Integer) Implements SolidEdgeFramework.ISEApplicationEvents.AfterDocumentPrint
        On Error Resume Next

        If Not theDocument Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theDocument)
        End If
    End Sub
    Private Sub AfterDocumentSave(ByVal theDocument As Object) Implements SolidEdgeFramework.ISEApplicationEvents.AfterDocumentSave
        On Error Resume Next

        If Not theDocument Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theDocument)
        End If
    End Sub
    Private Sub BeforeDocumentSave(ByVal theDocument As Object) Implements SolidEdgeFramework.ISEApplicationEvents.BeforeDocumentSave
        On Error Resume Next

        If Not theDocument Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theDocument)
        End If
    End Sub
    Private Sub AfterNewWindow(ByVal theWindow As Object) Implements SolidEdgeFramework.ISEApplicationEvents.AfterNewWindow
        On Error Resume Next

        If Not theWindow Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theWindow)
        End If
    End Sub
    Private Sub BeforeDocumentPrint(ByVal theDocument As Object, ByVal hDC As Integer, ByRef ModelToDC As Double, ByRef Rect As Integer) Implements SolidEdgeFramework.ISEApplicationEvents.BeforeDocumentPrint
        On Error Resume Next

        If Not theDocument Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(theDocument)
        End If
    End Sub

    Private Sub AfterCommandRun(ByVal theCommandID As Integer) Implements SolidEdgeFramework.ISEApplicationEvents.AfterCommandRun

    End Sub

    Private Sub BeforeCommandRun(ByVal theCommandID As Integer) Implements SolidEdgeFramework.ISEApplicationEvents.BeforeCommandRun

    End Sub

    Private Sub BeforeQuit() Implements SolidEdgeFramework.ISEApplicationEvents.BeforeQuit

    End Sub
#End Region

#Region "Application File UI Events"

    '   FileUIEvents
    '   If the add-in throws the NotImplementedException, Edge gets the COM return code E_NOTIMPL. Edge sees that code and moves on to the
    '   next event listener. If the add-in does not throw that exception, .NET will send Edge the COM return code S_OK. Edge then assumes
    '   the event was handled and will not call out to any other listener. Generally there should only be one listener for File UI events.
    '   Obviously Edge cannot have multiple add-ins showing the user file dialogs and having each return a file to open. If the add-in
    '   shows a file UI and the user cancels the operation, do not throw the exception (so Edge sees S_OK). In that case, simply return
    '   no filename. If all listeners return the exception, Edge will then show its own file UI.
    Private Sub OnCreateInPlacePartUI(ByRef Filename As String, ByRef AppendToTitle As String, ByRef Template As String) Implements SolidEdgeFramework.ISEFileUIEvents.OnCreateInPlacePartUI

        Throw New System.NotImplementedException

    End Sub

    Private Sub OnFileNewUI(ByRef Filename As String, ByRef AppendToTitle As String) Implements SolidEdgeFramework.ISEFileUIEvents.OnFileNewUI

        Throw New System.NotImplementedException

    End Sub

    Private Sub OnFileOpenUI(ByRef Filename As String, ByRef AppendToTitle As String) Implements SolidEdgeFramework.ISEFileUIEvents.OnFileOpenUI

        Throw New System.NotImplementedException

    End Sub

    Private Sub OnFileSaveAsImageUI(ByRef Filename As String, ByRef AppendToTitle As String, ByRef Width As Integer, ByRef Height As Integer, ByRef ImageQuality As SolidEdgeFramework.SeImageQualityType) Implements SolidEdgeFramework.ISEFileUIEvents.OnFileSaveAsImageUI

        Throw New System.NotImplementedException

    End Sub

    Private Sub OnFileSaveAsUI(ByRef Filename As String, ByRef AppendToTitle As String) Implements SolidEdgeFramework.ISEFileUIEvents.OnFileSaveAsUI

        Throw New System.NotImplementedException

    End Sub

    Private Sub OnPlacePartUI(ByRef Filename As String, ByRef AppendToTitle As String) Implements SolidEdgeFramework.ISEFileUIEvents.OnPlacePartUI

        Throw New System.NotImplementedException

    End Sub

#End Region


    Public Sub New()

        MyBase.New()

        pEdgeBarPages = New Collection

    End Sub
End Class
