Option Strict Off
Option Explicit On

Friend Class Form1
    Inherits System.Windows.Forms.Form

    ' With VB .NET there are two ways to work with events. The .NET way using delegates is how
    ' I set up events for m_Button. I use AddOrRemoveButtonEventHandlers to add or remove the
    ' delegates.
    Dim m_Button As SolidEdgeFramework.ISECommandBarButton
    ' Another change for VB .NET means I have to dim the various events variables as the
    ' event interface and not as an object.
    Dim m_ButtonEvents As SolidEdgeFramework.ISECommandBarButtonEvents_Event

    ' The way to do events with VB .NET relies on using the MS VB compatibility assembly and then
    ' using "WithEvents" in the declaration. I do this for m_Button2 and the rest of the events
    ' this sample uses.
    Dim m_Button2 As SolidEdgeFramework.CommandBarButton
    ' But I still have to use the event interface.
    Dim WithEvents m_ButtonEvents2 As SolidEdgeFramework.ISECommandBarButtonEvents_Event

    ' A command that does nothing.
    Dim m_Cmd1 As SolidEdgeFramework.Command
    ' And the events handler. Note the usage of the ISECommandEvents_Event interface is used.
    Dim WithEvents m_Cmd1Events As SolidEdgeFramework.ISECommandEvents_Event

    ' Another command that happens to hook up to the mouse and window events.
    Dim m_Cmd2 As SolidEdgeFramework.Command
    Dim WithEvents m_Cmd2Events As SolidEdgeFramework.ISECommandEvents_Event
    Dim WithEvents m_Cmd2Mouse As SolidEdgeFramework.Mouse
    Dim m_Cmd2Window As Object
    Dim WithEvents m_Cmd2WindowEvents As SolidEdgeFramework.ISECommandWindowEvents_Event

    ' A command that simply sets up a button as a macro that runs notepad. No events needed for such
    ' a command as edge detects it is a macro and enables the command by default. Hence once created, 
    ' This server is not needed for the button to function properly.
    Dim m_Button3 As SolidEdgeFramework.CommandBarButton

    ' A couple of buttons under a popup that is added to the toolbar.
    Dim m_PopupButton1 As SolidEdgeFramework.CommandBarButton
    Dim WithEvents m_PopupEvents1 As SolidEdgeFramework.ISECommandBarButtonEvents_Event

    Dim WithEvents m_PopupEvents2 As SolidEdgeFramework.ISECommandBarButtonEvents_Event
    Dim m_PopupButton2 As SolidEdgeFramework.CommandBarButton

    ' And finally the app object complete with events.
    Dim m_objApp As SolidEdgeFramework.Application
    Dim WithEvents m_AppEvents As SolidEdgeFramework.ISEApplicationEvents_Event

    ' With .NET VB users get the joy of managing the lifetime of COM objects. I set up this function
    ' to help out with that.
    Private Sub ReleaseEdgeObjects(ByVal bExiting As Boolean)

        ' I use "on error" because I have to call ReleaseComObject on all Edge objects. Note that I don't
        ' need to do that for the event objects since these are really interfaces with the interface being
        ' implemented by the .NET object and not an edge object.
        On Error Resume Next

        If Not m_Button Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Button)
        End If
        m_Button = Nothing

        If Not m_Button2 Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Button2)
        End If
        m_Button2 = Nothing

        If Not m_Button3 Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Button3)
        End If
        m_Button3 = Nothing

        If Not m_PopupButton1 Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_PopupButton1)
        End If
        m_PopupButton1 = Nothing

        If Not m_PopupButton2 Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_PopupButton2)
        End If
        m_PopupButton2 = Nothing

        m_ButtonEvents = Nothing

        m_ButtonEvents2 = Nothing

        m_PopupEvents1 = Nothing

        m_PopupEvents2 = Nothing

        If Not m_Cmd1 Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Cmd1)
        End If
        m_Cmd1 = Nothing

        m_Cmd1Events = Nothing

        If Not m_Cmd2 Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Cmd2)
        End If
        m_Cmd2 = Nothing

        m_Cmd2Events = Nothing

        m_Cmd2WindowEvents = Nothing

        If True = bExiting Then
            If Not m_objApp Is Nothing Then
                System.Runtime.InteropServices.Marshal.ReleaseComObject(m_objApp)
            End If
            m_AppEvents = Nothing
        End If

    End Sub

    ' This sub adds or removes the event handlers for m_Button the .NET way - using "handlers" a.k.a. "delegates"
    Private Sub AddOrRemoveButtonEventHandlers(ByVal Add As Boolean)
        If Add Then
            If Not m_ButtonEvents Is Nothing Then
                AddHandler m_ButtonEvents.Click, AddressOf Button1Events_Click
                AddHandler m_ButtonEvents.Help, AddressOf Button1Events_Help
                AddHandler m_ButtonEvents.UpdateUI, AddressOf ButtonEvents1_UpdateUI
            End If
        Else
            If Not m_ButtonEvents Is Nothing Then
                RemoveHandler m_ButtonEvents.Click, AddressOf Button1Events_Click
                RemoveHandler m_ButtonEvents.Help, AddressOf Button1Events_Help
                RemoveHandler m_ButtonEvents.UpdateUI, AddressOf ButtonEvents1_UpdateUI
            End If
        End If
    End Sub


    ' Command 2 adds a toolbar to the app

    Private Sub Command2_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command2.Click
        Dim objMyCommandBar As SolidEdgeFramework.CommandBar

        'Look to see if the bar exists. If so, toggle its display state.
        objMyCommandBar = m_objApp.Environments.Item("Part").CommandBars.Item("F2")
        If Not objMyCommandBar Is Nothing Then
            If objMyCommandBar.Visible = False Then
                objMyCommandBar.Visible = True
            Else
                objMyCommandBar.Visible = False
            End If

        End If
        'Build the bar. Calling it will either create F2, or add any buttons that
        'have been deleted by the user. User can modify the bar using this sample,
        'or Solid Edge customization.
        Call BuildBar()

        Me.Command4.Enabled = True
    End Sub

    ' Command3 toggles tooltip display on and off

    Private Sub Command3_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command3.Click
        Dim objEnvironment As Object
        Dim objCommandBars As SolidEdgeFramework.CommandBars
        Dim Tooltips As Boolean

        For Each objEnvironment In m_objApp.Environments

            If objEnvironment.Name = "Part" Then

                objCommandBars = objEnvironment.CommandBars

                Tooltips = objCommandBars.DisplayTooltips
                Debug.Print("Tooltips: " & Tooltips)

                If Tooltips = True Then
                    objCommandBars.DisplayTooltips = False
                    Me.Command3.Text = "Tooltips on"
                Else
                    objCommandBars.DisplayTooltips = True
                    Me.Command3.Text = "Tooltips off"
                End If
            End If
        Next objEnvironment

    End Sub

    ' Command deletes the toolbar

    Private Sub Command4_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command4.Click
        Dim objEnvironment As Object
        Dim objCommandBars As SolidEdgeFramework.CommandBars
        Dim objMyCommandBar As SolidEdgeFramework.CommandBar

        For Each objEnvironment In m_objApp.Environments

            objCommandBars = objEnvironment.CommandBars

            If objEnvironment.Name = "Part" Then

                objMyCommandBar = objCommandBars.Item("F2")

                If Not objMyCommandBar Is Nothing Then
                    objMyCommandBar.Delete()
                    Me.Command4.Enabled = False

                    ReleaseEdgeObjects(False)

                End If
            End If
        Next objEnvironment

    End Sub

    Private Sub Command5_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command5.Click
        If Not m_Button Is Nothing Then m_Button.Delete()
        If Not m_Button Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Button)
        End If
        m_Button = Nothing

        If Not m_ButtonEvents Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_ButtonEvents)
        End If
        m_ButtonEvents = Nothing

    End Sub

    Private Sub Command6_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command6.Click
        If Not m_Button2 Is Nothing Then m_Button2.Delete()
        If Not m_Button2 Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Button2)
        End If
        m_Button2 = Nothing

        If Not m_ButtonEvents2 Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_ButtonEvents2)
        End If
        m_ButtonEvents2 = Nothing

    End Sub

    Private Sub Command7_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command7.Click
        If Not m_Button Is Nothing Then
            If m_Button.Caption = "CMD 1" Then
                m_Button.Caption = "Test"
            Else
                m_Button.Caption = "CMD 1"
            End If
        End If
    End Sub

    Private Sub Command8_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command8.Click
        End
    End Sub

    Private Sub Form1_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load
        Dim objCommandBars As Object
        Dim objMyCommandBar As SolidEdgeFramework.CommandBar
        Dim LargeButtons As Boolean
        Dim Tooltips As Boolean

        On Error Resume Next

        ' With VB .NET we find right away that .NET does not provide an OLE message filter for VB users. VB 6 provided
        ' the message filter so VB users did not have to worry about what it is or how it works. No longer is that
        ' true. Since this is an exe (not running in-proc with Edge) that uses COM and OLE to interact with edge, a
        ' message filter needs to be registered with OLE. Luckily after searching I found 
        ' http://msdn.microsoft.com/en-us/library/ms228772(VS.80).aspx. Google for "call rejected by callee" to find
        ' the article. I used MSDN search on the same string but did not find the above article. I did find
        ' ms-help://MS.VSCC.v90/MS.MSDNQTR.v90.en/dv_extcore/html/4335626d-ed57-4d77-a493-6bad09e5cc1a.htm but it is a C#
        ' sample whereas the former has both C# and a VB .NET sample filter.

        MessageFilterAPI.RegisterMessageFilter()

        'Get the SolidEdge Application object
        m_objApp = GetObject(, "SolidEdge.Application")

        If Not m_objApp Is Nothing Then

            'Get ApplicationEvents; Need to know when the app exits so I can release any
            'objects
            'UPGRADE_WARNING: Couldn't resolve default property of object m_objApp.ApplicationEvents. Click for more: 'ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'

            ' I do this same thing with in-proc add-ins with no problem. But here when I obtain the events I get a "managed debugging assistant"
            ' "Reentrancy" exception. The message box that appears tells me all I have to do is look at the stack and I will see the problem
            ' but the stack shows me no clues as to what is happening. With VB 6 there is no problem. After running the converter I had
            ' an error here so I had to change how I declared m_AppEvents (from SolidEdgeFramework.ApplicationEvents to
            ' SolidEdgeFramework.ISEApplicationEvents_Event. What is the root issue with the "Reentrancy" exception?
            ' I can set a break in Edge where the app object is queried for IID_IConnectionPointContainer and in the Advise method in
            ' atlcom.h. The breakpoint for the query call is tripped twice. After the second time I hit "continue" in the debugger
            ' and at that time I get the Renentrancy exception here in this code. I hit "Continue" on the dialog and then the
            ' breakpoint at Advise in Edge is tripped. So something happens between the second call VB .NET makes to the app
            ' object to get the CPC interface and the call to Advise that connencts the sink to edge. When Advise calls back out to
            ' this VB app I see no exceptions. So I have no idea what code is tripping the MDA.
            ' I get the same MDA reentrancy exception on each attempt to connect events in Edge (whether I use "withevents" or not).
            ' The other exceptions occur in BuildBar, which is called when this app's form appears and the user clicks the
            ' "Show toolbar" button.

            m_AppEvents = m_objApp.ApplicationEvents

            'Get CommandBars collection of the Part environment
            objCommandBars = m_objApp.Environments.Item("Part").CommandBars

            If Not objCommandBars Is Nothing Then

                LargeButtons = objCommandBars.LargeButtons

                Tooltips = objCommandBars.DisplayTooltips

                If Tooltips = True Then
                    Me.Command3.Text = "Tooltips off"
                Else
                    Me.Command3.Text = "Tooltips on"
                End If

                'See if my toolbar already exists and set form text appropriately
                objMyCommandBar = objCommandBars.Item("F2")

                If Not objMyCommandBar Is Nothing Then
                    'Bar already exists. Call BuildBar in order to connect to the
                    'buttons.
                    Call BuildBar()
                    Me.Command4.Enabled = True
                Else
                    Me.Command4.Enabled = False
                End If
            End If
        Else
            MsgBox("Solid Edge has to be running to use this sample")
        End If
    End Sub
    Sub BuildBar()

        Dim objCommandBars As Object
        Dim objCommandBarControls As SolidEdgeFramework.CommandBarControls
        Dim objMyCommandBar As SolidEdgeFramework.CommandBar
        Dim objPopup As SolidEdgeFramework.CommandBarPopup
        Dim SepIndex As Integer
        Dim BarButtonEvents As Object


        objCommandBars = m_objApp.Environments.Item("Part").CommandBars

        If Not objCommandBars Is Nothing Then
            'First find each control to see if they already exist (previously created by this code)
            'Later, I'll create them if they don't exist.
            m_Button = objCommandBars.FindControl(SolidEdgeFramework.SeControlType.seControlButton, 1, "VB Sample Command 1")

            If Not m_Button Is Nothing Then
                BarButtonEvents = m_Button.CommandBarButtonEvents
                'Button exists. Simply connect up to the events to enable the button
                m_ButtonEvents = BarButtonEvents

                AddOrRemoveButtonEventHandlers(True)

            End If

            m_Button2 = objCommandBars.FindControl(SolidEdgeFramework.SeControlType.seControlButton, 1, "VB Sample Command 2")

            If Not m_Button2 Is Nothing Then
                BarButtonEvents = m_Button2.CommandBarButtonEvents
                'Button exists. Simply connect up to the events to enable the button
                m_ButtonEvents2 = BarButtonEvents
            End If

            'Button3 is a macro (runs MS notepad). Hence, no need for events! I get it
            'so it is not created below.
            m_Button3 = objCommandBars.FindControl(SolidEdgeFramework.SeControlType.seControlButton, 1, "VB Sample Command 3")

            m_PopupButton1 = objCommandBars.FindControl(SolidEdgeFramework.SeControlType.seControlButton, 1, "VB Sample Popup 1")
            If Not m_PopupButton1 Is Nothing Then
                'Button exists. Simply connect up to the events to enable the button
                m_PopupEvents1 = m_PopupButton1.CommandBarButtonEvents
            End If

            m_PopupButton2 = objCommandBars.FindControl(SolidEdgeFramework.SeControlType.seControlButton, 1, "VB Sample Popup 2")
            If Not m_PopupButton2 Is Nothing Then
                'Button exists. Simply connect up to the events to enable the button
                m_PopupEvents2 = m_PopupButton2.CommandBarButtonEvents
            End If

            If m_Button Is Nothing Or m_Button2 Is Nothing Or m_Button3 Is Nothing Or m_PopupButton1 Is Nothing Or m_PopupButton2 Is Nothing Then

                objMyCommandBar = objCommandBars.Item("F2")

                If objMyCommandBar Is Nothing Then
                    objMyCommandBar = objCommandBars.Add("F2", SolidEdgeFramework.SeBarPosition.seBarFloating)
                    Me.Command4.Enabled = True
                End If

                If Not objMyCommandBar Is Nothing Then
                    objCommandBarControls = objMyCommandBar.Controls

                    objPopup = objMyCommandBar.FindControl(SolidEdgeFramework.SeControlType.seControlPopup, 1, "Popup Group")

                    If objPopup Is Nothing Then
                        objPopup = objMyCommandBar.Controls.Add(1, 1)

                        If Not objPopup Is Nothing Then
                            objPopup.Caption = "Popup 1"
                            objPopup.TooltipText = "First popup"
                            objPopup.DescriptionText = "VB Sample popup command 1"
                            objPopup.Tag = "Popup Group"
                            'objPopup.LoadFace ("g:\ingr\froot\tmp\bitmap1.bmp")
                            m_PopupButton1 = objPopup.Controls.Item(1)
                        End If

                        If Not m_PopupButton1 Is Nothing Then
                            m_PopupEvents1 = m_PopupButton1.CommandBarButtonEvents
                            m_PopupButton1.Caption = "Popup 1"
                            m_PopupButton1.TooltipText = "VB Sample popup command 1"
                            m_PopupButton1.DescriptionText = "VB Sample popup command 1"
                            m_PopupButton1.Tag = "VB Sample Popup 1"
                            'm_PopupButton1.LoadFace ("g:\ingr\froot\tmp\bitmap1.bmp")
                        End If

                        If Not objPopup Is Nothing Then
                            m_PopupButton2 = objPopup.Controls.Add(, 1)
                            If Not m_PopupButton2 Is Nothing Then
                                m_PopupEvents2 = m_PopupButton2.CommandBarButtonEvents
                                m_PopupButton2.Caption = "Popup 2"
                                m_PopupButton2.TooltipText = "VB Sample popup command 2"
                                m_PopupButton2.DescriptionText = "VB Sample popup command 2"
                                m_PopupButton2.Tag = "VB Sample Popup 2"
                            End If
                        End If
                    End If

                    If m_Button Is Nothing Then
                        Call objMyCommandBar.Controls.Add(SolidEdgeFramework.SeControlType.seControlSeparator, 0)

                        m_Button = objMyCommandBar.Controls.Add(, 1)
                        BarButtonEvents = m_Button.CommandBarButtonEvents
                        m_ButtonEvents = BarButtonEvents
                        AddOrRemoveButtonEventHandlers(True)

                        m_Button.Caption = "CMD 1"
                        m_Button.TooltipText = "VB Sample Command 1"
                        m_Button.Tag = "VB Sample Command 1"
                        m_Button.DescriptionText = "VB Sample Command 1"
                    End If

                    If m_Button2 Is Nothing Then
                        m_Button2 = objMyCommandBar.Controls.Add(, 1)
                        BarButtonEvents = m_Button2.CommandBarButtonEvents
                        m_ButtonEvents2 = BarButtonEvents
                        m_Button2.Caption = "CMD 2"
                        m_Button2.TooltipText = "VB Sample Command 2"
                        m_Button2.Tag = "VB Sample Command 2"
                        m_Button2.DescriptionText = "VB Sample Command 2"
                    End If

                    ' This next button shows how to simply launch another application. The code launches notepad but alternative code
                    ' showing how to launch a browser is included.
                    If m_Button3 Is Nothing Then
                        m_Button3 = objMyCommandBar.Controls.Add(, 1)

                        m_Button3.Caption = "Notepad"
                        'm_Button3.Caption = "Navigate"

                        m_Button3.TooltipText = "Notepad macro"
                        'm_Button3.TooltipText = "Open a web site"

                        m_Button3.DescriptionText = "Macro which runs Notepad"
                        'm_Button3.DescriptionText = "Macro that opens solidedge.com in the user's browser"

                        m_Button3.Tag = "VB Sample Command 3"

                        m_Button3.OnAction = "notepad.exe"
                        'm_Button3.OnAction = "www.solidedge.com"

                        ' If switching from notepad to a web site, after commenting and uncommenting the appropriate lines above,
                        ' comment out the next line that sets ParameterText for notepad.
                        m_Button3.ParameterText = "test.txt"

                        ' Get the index of this button so I can add a separator. I need it
                        ' because I've already added a separator and the "Before" parameter
                        ' is no longer the same as the button index.
                        SepIndex = m_Button3.Index
                        Call objMyCommandBar.Controls.Add(SolidEdgeFramework.SeControlType.seControlSeparator, 0, SepIndex)
                    End If
                End If
            End If
        End If

    End Sub


    Private Sub Form1_FormClosed(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed

        Call m_AppEvents_BeforeQuit()

        'Call End. I do so because if the Tip of the Day dialog box command
        'is invoked, and later the user closes the main form, this program
        'fails to shut down completely.
        End

        MessageFilterAPI.RevokeMessageFilter()

    End Sub

    Private Sub m_AppEvents_BeforeQuit() Handles m_AppEvents.BeforeQuit

        ReleaseEdgeObjects(True)

    End Sub

    Public Sub Button1Events_Click()
        m_Cmd1 = m_objApp.CreateCommand(SolidEdgeConstants.seCmdFlag.seTerminateAfterActivation)
        m_Cmd1Events = m_Cmd1

        m_Cmd1.Start()
    End Sub
    Public Sub Button1Events_Help(ByVal hFrameWnd As Integer, ByVal uHelpCommand As Integer)
        Debug.Print("Command 2 help invoked")

    End Sub

    Private Sub ButtonEvents1_UpdateUI()
        Dim CommandBars As SolidEdgeFramework.CommandBars
        Dim Button As SolidEdgeFramework.CommandBarButton

        CommandBars = m_objApp.Environments.Item("Part").CommandBars
        If Not CommandBars Is Nothing Then
            Button = CommandBars.FindControl(SolidEdgeFramework.SeControlType.seControlButton, 1, "VB Sample Command 1")
            If Not Button Is Nothing Then Button.Enabled = True
        End If
    End Sub

    Private Sub m_ButtonEvents2_Click() Handles m_ButtonEvents2.Click
        m_Cmd2 = m_objApp.CreateCommand(2)
        m_Cmd2Events = m_Cmd2
        m_Cmd2Mouse = m_Cmd2.Mouse
        m_Cmd2Window = m_Cmd2.Window
        m_Cmd2WindowEvents = m_Cmd2Window
        m_Cmd2.Start()
    End Sub


    Private Sub m_ButtonEvents2_Help(ByVal hFrameWnd As Integer, ByVal uHelpCommand As Integer) Handles m_ButtonEvents2.Help
        Debug.Print("Command 2 help invoked")
    End Sub

    Private Sub m_ButtonEvents2_UpdateUI() Handles m_ButtonEvents2.UpdateUI
        Dim CommandBars As SolidEdgeFramework.CommandBars
        Dim Button As SolidEdgeFramework.CommandBarButton
        CommandBars = m_objApp.Environments.Item("Part").CommandBars
        If Not CommandBars Is Nothing Then
            Button = CommandBars.FindControl(SolidEdgeFramework.SeControlType.seControlButton, 1, "VB Sample Command 2")
            If Not Button Is Nothing Then Button.Enabled = True
        End If
    End Sub

    Private Sub m_PopupEvents1_Click() Handles m_PopupEvents1.Click
        Debug.Print("popup click event")

    End Sub

    Private Sub m_PopupEvents1_UpdateUI() Handles m_PopupEvents1.UpdateUI
        Dim CommandBars As SolidEdgeFramework.CommandBars
        Dim Button As SolidEdgeFramework.CommandBarButton
        CommandBars = m_objApp.Environments.Item("Part").CommandBars
        If Not CommandBars Is Nothing Then
            Button = CommandBars.FindControl(SolidEdgeFramework.SeControlType.seControlButton, 1, "VB Sample Popup 1")
            If Not Button Is Nothing Then
                Button.Enabled = True
            End If
        End If

    End Sub

    Private Sub m_PopupEvents2_Click() Handles m_PopupEvents2.Click
        Debug.Print("popup click event")

    End Sub

    Private Sub m_PopupEvents2_UpdateUI() Handles m_PopupEvents2.UpdateUI
        Dim CommandBars As SolidEdgeFramework.CommandBars
        Dim Button As SolidEdgeFramework.CommandBarButton
        CommandBars = m_objApp.Environments.Item("Part").CommandBars
        If Not CommandBars Is Nothing Then
            Button = CommandBars.FindControl(SolidEdgeFramework.SeControlType.seControlButton, 1, "VB Sample Popup 2")
            If Not Button Is Nothing Then
                Button.Enabled = True
            End If
        End If
    End Sub

    Private Sub m_Cmd1Events_Activate() Handles m_Cmd1Events.Activate
        Debug.Print("Cmd1 activated")
    End Sub

    Private Sub m_Cmd1Events_Deactivate() Handles m_Cmd1Events.Deactivate
        Debug.Print("Cmd1 deactivated")

    End Sub

    Private Sub m_Cmd1Events_Terminate() Handles m_Cmd1Events.Terminate
        System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Cmd1)
    End Sub

    Private Sub m_Cmd2Events_Activate() Handles m_Cmd2Events.Activate
        Debug.Print("Cmd2 Activated!")

    End Sub

    Private Sub m_Cmd2Events_Deactivate() Handles m_Cmd2Events.Deactivate
        Debug.Print("Cmd2 Decativated")

    End Sub

    Private Sub m_Cmd2Events_Terminate() Handles m_Cmd2Events.Terminate
        If Not m_Cmd2Mouse Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Cmd2Mouse)
        End If
        m_Cmd2Mouse = Nothing

        If Not m_Cmd2Window Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Cmd2Window)
        End If
        m_Cmd2Window = Nothing

        If Not m_Cmd2WindowEvents Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Cmd2WindowEvents)
        End If
        m_Cmd2WindowEvents = Nothing

        If Not m_Cmd2 Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_Cmd2)
        End If
        m_Cmd2 = Nothing

        Debug.Print("Cmd2 terminated!")
    End Sub
End Class