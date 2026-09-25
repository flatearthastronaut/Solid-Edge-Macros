Friend Class Command

    Implements IDisposable

    Dim m_Command As SolidEdgeFramework.Command
    Dim m_Mouse As SolidEdgeFramework.Mouse

    Dim m_CommandWindowEvents As SolidEdgeFramework.ISECommandWindowEvents_Event

    Dim m_CommandEvents As SolidEdgeFramework.ISECommandEvents_Event

    Dim m_MouseEvents As SolidEdgeFramework.ISEMouseEvents_Event

    Dim m_ResourceHandle As Int32

    Public Sub CreateCommand(ByVal TheApp As SolidEdgeFramework.Application)
        If Not TheApp Is Nothing Then

            m_Command = TheApp.CreateCommand(SolidEdgeConstants.seCmdFlag.seNoDeactivate)

            If Not m_Command Is Nothing Then
                ' Get the command events object so I can react to Activate, Terminate etc.
                m_CommandEvents = m_Command
                ' Get the command window events. Notifications from the ribbon bar dialog come
                ' in via the windowproc event (as do lots and lots of other window messages)
                m_CommandWindowEvents = m_Command.Window
                ' Get the mouse events so I can respond to MouseClick. Note the Mouse will not
                ' be ready for filter calls until the command is activated so I set that up in
                ' the Activate event.
                m_Mouse = m_Command.Mouse

                m_MouseEvents = m_Mouse

                AddOrRemoveEventHandlers(True)
            End If
        End If
    End Sub

    Public Sub Start()
        ' When m_Command.Start is called, the ribbon needs to be in place so edge will show it.
        If Not m_Command Is Nothing Then
            Dim Ribbon As SolidEdgeFramework.ISolidEdgeRibbonBar

            Ribbon = m_Command

            ' Module handles are 64 bits in a 64 bit application. They simply cannot be passed to Edge using the original SetAddInInfo
            ' API. When 64 bit Edge came out, a new interface was created that "Ex tends" the original interface by one method that
            ' takes in a resource module filename instead of a handle. Edge will load the resource module as a data file, which also
            ' avoids a pitfall where a resource module may accidentally contain an entry point (code - think "dll main") that can keep the 
            ' module from loading if an attempt to load one with a 32 bit entry point is made by calling the windows API LoadLibrary or
            ' LoadLibraryEx where the input flag is not the flag for a data file.

            Dim ResFilename As String

            ResFilename = GetResourceFilename()

            Dim RibbonEx As SolidEdgeFramework.ISolidEdgeRibbonBarEx

            RibbonEx = m_Command ' or = Ribbon, either one shoudl work

            If Not RibbonEx Is Nothing Then
                RibbonEx.AddRibbonEx(104, ResFilename)
            ElseIf Not Ribbon Is Nothing Then
                Ribbon.AddRibbon(104, GetResourceHandle())
            End If

            m_Command.Start()
            End If
    End Sub

#Region "EVENTS"
    Public Sub AddOrRemoveEventHandlers(ByVal Add As Boolean)
        If Add Then
            ' Add the handlers for events we want to respond to.
            If Not m_CommandEvents Is Nothing Then

                ' Must be a corresponding RemoveHandler for each call to AddHandler.
                AddHandler m_CommandEvents.Activate, AddressOf m_CommandEvents_Activate
                AddHandler m_CommandEvents.Terminate, AddressOf m_CommandEvents_Terminate
            End If
            If Not m_CommandWindowEvents Is Nothing Then
                AddHandler m_CommandWindowEvents.WindowProc, AddressOf m_CommandWindowEvents_WindowProc
            End If
            If Not m_MouseEvents Is Nothing Then
                AddHandler m_MouseEvents.MouseClick, AddressOf m_MouseEvents_MouseClick
            End If

        Else
            ' Remove the handlers as we are done responding to events
            If Not m_CommandEvents Is Nothing Then

                ' The corresponding RemoveHandler calls.
                RemoveHandler m_CommandEvents.Activate, AddressOf m_CommandEvents_Activate
                RemoveHandler m_CommandEvents.Terminate, AddressOf m_CommandEvents_Terminate

                ' Must call FinalReleaseComObject now or GC will do so at shutdown as DLLs are unloading, which is
                ' a major problem since there is no control over the order DLLs are unloaded and that can lead
                ' to exceptions that may or may not be caught by the .NET runtime.
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(m_CommandEvents)
                m_CommandEvents = Nothing
            End If
            If Not m_CommandWindowEvents Is Nothing Then
                RemoveHandler m_CommandWindowEvents.WindowProc, AddressOf m_CommandWindowEvents_WindowProc
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(m_CommandWindowEvents)
                m_CommandWindowEvents = Nothing
            End If
            If Not m_MouseEvents Is Nothing Then
                RemoveHandler m_MouseEvents.MouseClick, AddressOf m_MouseEvents_MouseClick
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(m_MouseEvents)
                m_MouseEvents = Nothing
            End If
        End If
    End Sub

    Public Sub m_CommandEvents_Activate()

        MsgBox("Command activated")

        ' set up the mouse to do some simple locate operations. See the "MouseObject" sample in the
        ' Custom folder for a more complete sample. The mouse is only ready to be manipulated during
        ' or after the activate call.
        m_Command.Mouse.AddToLocateFilter(SolidEdgeConstants.seLocateFilterConstants.seLocateRefPlane)

        ' To locate a part in an assembly, uncomment out the next line.
        'm_Command.Mouse.AddToLocateFilter(SolidEdgeConstants.seLocateFilterConstants.seLocatePart)

        ' Set up filter to locate a face. If locating a face of a part in an assembly, the above
        ' seLocatePart filter needs to be removed (keep it commented out) and the part has to be
        ' activated.
        m_Command.Mouse.AddToLocateFilter(SolidEdgeConstants.seLocateFilterConstants.seLocateFace)

        m_Command.Mouse.ScaleMode = 1 ' design model coordinates
        m_Command.Mouse.WindowTypes = 1 ' graphic windows only

        m_Command.Mouse.LocateMode = SolidEdgeConstants.seLocateModes.seLocateQuickPick

    End Sub

    Public Sub m_CommandEvents_Terminate()
        MsgBox("Command terminated")
        AddOrRemoveEventHandlers(False)

        ' Perform FinalReleaseComObject on the mouse and command. Microsoft indicates that calling FinalReleaseComObject
        ' will actually cause the .NET runtime to release the real reference on the actual COM server's COM object.
        ' I found that failing to do this can result in deadlock if this command is running when the user closes a document.
        If Not m_Mouse Is Nothing Then
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(m_Mouse)
            m_Mouse = Nothing
        End If
        If Not m_Command Is Nothing Then
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(m_Command)
            m_Command = Nothing
        End If
    End Sub

    Public Function m_CommandWindowEvents_WindowProc(ByVal pTheDoc As Object, ByVal pTheView As Object, ByVal nMsg As UInteger, ByVal wParam As UInteger, ByVal lParam As Integer) As Integer

        ' Very busy function.
        If nMsg = WindowMessages.WM_COMMAND Then
            If 1000 = wParam Then
                MsgBox("Button1 clicked")
            ElseIf 1001 = wParam Then
                MsgBox("Check box clicked")
            End If
        End If
    End Function

    Sub m_MouseEvents_MouseClick(ByVal sButton As Short, ByVal sShift As Short, ByVal dX As Double, ByVal dY As Double, ByVal dZ As Double, ByVal pWindowDispatch As Object, ByVal lKeyPointType As Integer, ByVal pGraphicDispatch As Object)
        MsgBox("Mouse clicked")

        Dim WhatIsIt As String
        WhatIsIt = ""

        If Not pGraphicDispatch Is Nothing Then
            Select Case pGraphicDispatch.Type
                Case SolidEdgeConstants.ObjectType.igRefPlane
                    WhatIsIt = "Reference plane"
                Case SolidEdgeConstants.ObjectType.igAsmRefPlane
                    WhatIsIt = "Assembly reference plane"
                Case SolidEdgeConstants.ObjectType.igPart
                    WhatIsIt = "Part"
                Case SolidEdgeConstants.GNTTypePropertyConstants.igFace
                    WhatIsIt = "Face"
                Case SolidEdgeConstants.ObjectType.igReference
                    'Locates in assembly and certain other environments of data for a part (as one example)
                    ' can be a "reference" to the actual entity. A reference is itself an object that has
                    ' an "object" member as well as a matrix and a few other datum.
                    Select Case pGraphicDispatch.object.Type
                        Case SolidEdgeConstants.ObjectType.igRefPlane
                            WhatIsIt = "Reference::Reference plane"
                        Case SolidEdgeConstants.ObjectType.igPart
                            WhatIsIt = "Reference::igPart"
                        Case SolidEdgeConstants.GNTTypePropertyConstants.igFace
                            WhatIsIt = "Reference::Face"
                    End Select
                Case Else
                    WhatIsIt = pGraphicDispatch.Type.ToString

            End Select

            Dim Message As String
            Message = "Located: " & WhatIsIt

            MsgBox(Message)

        End If
    End Sub

#End Region


    Private disposedValue As Boolean = False        ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' TODO: free managed resources when explicitly called
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

    Public Sub New()

    End Sub
End Class
