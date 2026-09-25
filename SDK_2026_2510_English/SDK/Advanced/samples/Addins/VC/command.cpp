#include "stdafx.h"
#include "commands.h"

/////////////////////////////////////////////////////////////////////////////
// CCommand

// Note: The CCommand object is meant to be a base class that provides much of 
//       the common functionality needed by many commands. This includes connection
//       of the various sinks a command most likely needs and providing generic
//       implementations of those sinks. Additionally, CCommand defines virtual member
//       functions for each event it handles. The generic implementatoin provided by
//       CCommand object that Solid Edge invokes in turn calls the analogous virtual
//       member function on itself. Derived classes in many cases will simply
//       override whatever virtual CCommand member functions they care to respond to
//       while letting the CCommand object handle the other events.

CCommand::CCommand()
{
	m_pCommands = NULL;

	m_pCommandEventsObj = NULL;
	m_pMouseEventsObj = NULL;
	m_pWindowEventsObj = NULL;
	m_pLocateFilterEventsObj = NULL;
}

CCommand::~CCommand()
{
	UnadviseFromCommandEvents();
	ReleaseInterfaces();

	C_RELEASE(m_pCommandEventsObj);

	C_RELEASE(m_pMouseEventsObj);

	C_RELEASE(m_pWindowEventsObj);

	C_RELEASE(m_pLocateFilterEventsObj);
}

// CreateCommand calls Solid Edge to create a command control
// which is used to obtain the command interface and to connect
// to command events provided by the control. If CommandType
// is seNoDeactivate, the mouse interface is also obtained and
// both the mouse and window event sinks are connected.

HRESULT CCommand::CreateCommand(SolidEdgeConstants::seCmdFlag CommandType)
{
	HRESULT hr = S_OK;

	m_pSECommand = m_pCommands->GetApplicationPtr()->CreateCommand(CommandType);
	// Create Command event handler

	XCommandEventsObj::CreateInstance(&m_pCommandEventsObj);
	if( m_pCommandEventsObj )
	{
		m_pCommandEventsObj->AddRef();
		hr = m_pCommandEventsObj->Connect(m_pSECommand);
		m_pCommandEventsObj->m_pCommand = this;
	}
	else
	{
		hr = E_OUTOFMEMORY;
	}

	if( SUCCEEDED( hr ) )
	{
		if( SolidEdgeConstants::seNoDeactivate == CommandType )
		{
			// Get the mouse and connect our sink.
			m_pSEMouse = m_pSECommand->GetMouse();

			// Create Mouse event handler
			XMouseEventsObj::CreateInstance(&m_pMouseEventsObj);
			if( m_pMouseEventsObj )
			{
				m_pMouseEventsObj->AddRef();
				hr = m_pMouseEventsObj->Connect(m_pSEMouse);
				m_pMouseEventsObj->m_pCommand = this;
			}
			else
			{
				hr = E_OUTOFMEMORY;
			}

			if( SUCCEEDED( hr ) )
			{
				// Create Window event handler
				XWindowEventsObj::CreateInstance(&m_pWindowEventsObj);
				if( m_pWindowEventsObj )
				{
					m_pWindowEventsObj->AddRef();
					hr = m_pWindowEventsObj->Connect(m_pSECommand->GetWindow());
					m_pWindowEventsObj->m_pCommand = this;
				}
				else
				{
					hr = E_OUTOFMEMORY;
				}
			}

			// Create locate fitler event handler. Edge will call the event allowing the add-in
			// to perform any checking that may be needed to validate or reject the object as the
			// user locates objects.
			XLocateFilterEventsObj::CreateInstance(&m_pLocateFilterEventsObj);
			if( m_pLocateFilterEventsObj )
			{
				m_pLocateFilterEventsObj->AddRef();
				hr = m_pLocateFilterEventsObj->Connect(m_pSEMouse);
				m_pLocateFilterEventsObj->m_pCommand = this;
			}
			else
			{
				hr = E_OUTOFMEMORY;
			}
		}
	}

	return hr;
}

void CCommand::UnadviseFromCommandEvents()
{
	if( NULL != m_pMouseEventsObj && NULL != m_pSEMouse )
	{
		// Disconnect from mouse event source
		m_pMouseEventsObj->Disconnect(m_pSEMouse);
	}

	if( NULL != m_pWindowEventsObj && NULL != m_pSECommand )
	{
		// Disconnect from window event source
		IUnknownPtr pWindow = m_pSECommand->GetWindow();
		if( pWindow )
		{
			m_pWindowEventsObj->Disconnect(pWindow);
		}
	}

	if( NULL != m_pLocateFilterEventsObj && NULL != m_pSEMouse )
	{
		// Disconnect from command event source.
		m_pLocateFilterEventsObj->Disconnect(m_pSEMouse);
	}

	if( NULL != m_pCommandEventsObj && NULL != m_pSECommand )
	{
		// Disconnect from command event source.
		m_pCommandEventsObj->Disconnect(m_pSECommand);
	}
}

// ReleaseInterfaces default implementation releases all interfaces obtained
// from Solid Edge

void CCommand::ReleaseInterfaces()
{
	m_pSECommand    = NULL;
	m_pSEMouse      = NULL;
}

// Command events

// Want to respond to an event? Simply override the corresponding virtual member
// function of your CCommand derived object. The event handlers catch the events
// and perform any actions assumed to be common to most CCommand derived objects
// and then call the cooresponding virtual member function on the CCommand object.

  // TODO: The XCommandEvents handler invokes the analogous virtual member functions
  //       on the CCommand class. Override each member function in your derived
  //       class if you need to respond to an event.
  //       Common code for any event may be executed in the XCommandEvents handler
  //       for that event.

HRESULT CCommand::XCommandEvents::raw_Activate( )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->Activate();

	return hr;
}

HRESULT CCommand::XCommandEvents::raw_Deactivate( )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->Deactivate();

	return hr;
}

HRESULT CCommand::XCommandEvents::raw_Terminate( )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	// Unadvise from command events. Wouldn't want to cause a reference leak now, would we?
	// Now you see why that back pointer is stored in the event template object.
	m_pCommand->UnadviseFromCommandEvents();

	hr = m_pCommand->Terminate();

	// Release any other references to interfaces that may have been obtained from Solid Edge.
	// For example, if I've acquired and stored the mouse interface, command interface, any
	// dispatch interface etc.

	m_pCommand->ReleaseInterfaces();

	// Now release this. This should release the only explicit reference performed
	// immediately after CCommands called CreateInstance.
	LPUNKNOWN pMyUnknown = NULL;

	pMyUnknown = m_pCommand->GetMyUnknown();
	if( pMyUnknown )
	{
		pMyUnknown->Release();
	}

	return hr;
}

HRESULT CCommand::XCommandEvents::raw_Idle( long lCount, VARIANT_BOOL* pbMore )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	// TODO: This is your chance to get something accomplished while Solid Edge has a few idle cycles.
	//       But hurry! You never know when the user might wake up and demand more attention. Well,
	//       actually, maybe you do. Murphy's Law says that will happen as soon as you try to do
	//       something that's really useful, but requires a little time. Perhaps a worker thread would
	//       be more appropriate in that case. Just make sure you set its priority fairly low!

	// Default implementation is to simply tell Solid Edge this command is so slick it can accomplish
	// everything it needs to do in user time. Setting More to VARIANT_FALSE will result in this
	// command getting no more idle cycles.

	hr = m_pCommand->Idle( lCount, pbMore );

	return hr;
}
HRESULT CCommand::XCommandEvents::raw_KeyDown( short* KeyCode, short Shift )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->KeyDown( KeyCode, Shift );

	return hr;
}
HRESULT CCommand::XCommandEvents::raw_KeyPress(  short* KeyAscii )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->KeyPress( KeyAscii );

	return hr;
}
HRESULT CCommand::XCommandEvents::raw_KeyUp(  short* KeyCode, short Shift )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->KeyUp( KeyCode, Shift );

	return hr;
}

// Command mouse events

// Want to respond to a mouse event? Simply override the corresponding virtual member
// function of your CCommand derived object. The mouse event handler catches the events
// and calls the cooresponding virtual member function on the CCommand object.

  // TODO: The XMouseEvents handler invokes the analogous virtual member functions
  //       on the CCommand class. Override each member function in your derived
  //       class if you need to respond to an event.
  //       Common code for any event may be executed in the XMouseEvents handler
  //       for that event.

HRESULT CCommand::XMouseEvents::raw_MouseDown( short sButton, short sShift, double dX, double dY, double dZ, LPDISPATCH pWindowDispatch, long lKeyPointType, LPDISPATCH pGraphicDispatch )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->MouseDown( sButton, sShift, dX, dY, dZ, pWindowDispatch, lKeyPointType, pGraphicDispatch );

	return hr;
}

HRESULT CCommand::XMouseEvents::raw_MouseUp( short sButton, short sShift, double dX, double dY, double dZ, LPDISPATCH pWindowDispatch, long lKeyPointType, LPDISPATCH pGraphicDispatch )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->MouseUp( sButton, sShift, dX, dY, dZ, pWindowDispatch, lKeyPointType, pGraphicDispatch );

	return hr;
}

HRESULT CCommand::XMouseEvents::raw_MouseMove( short sButton, short sShift, double dX, double dY, double dZ, LPDISPATCH pWindowDispatch, long lKeyPointType, LPDISPATCH pGraphicDispatch )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->MouseMove( sButton, sShift, dX, dY, dZ, pWindowDispatch, lKeyPointType, pGraphicDispatch );

	return hr;
}

HRESULT CCommand::XMouseEvents::raw_MouseClick( short sButton, short sShift, double dX, double dY, double dZ, LPDISPATCH pWindowDispatch, long lKeyPointType, LPDISPATCH pGraphicDispatch )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->MouseClick( sButton, sShift, dX, dY, dZ, pWindowDispatch, lKeyPointType, pGraphicDispatch );

	return hr;
}

HRESULT CCommand::XMouseEvents::raw_MouseDblClick( short sButton, short sShift, double dX, double dY, double dZ, LPDISPATCH pWindowDispatch, long lKeyPointType, LPDISPATCH pGraphicDispatch )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->MouseDblClick( sButton, sShift, dX, dY, dZ, pWindowDispatch, lKeyPointType, pGraphicDispatch );

	return hr;
}

HRESULT CCommand::XMouseEvents::raw_MouseDrag( short sButton, short sShift, double dX, double dY, double dZ, LPDISPATCH pWindowDispatch, short DragState, long lKeyPointType, LPDISPATCH pGraphicDispatch )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->MouseDrag( sButton, sShift, dX, dY, dZ, pWindowDispatch, DragState, lKeyPointType, pGraphicDispatch );

	return hr;
}

// Command window events

// Want to respond to a window event? Simply override the corresponding virtual member
// function of your CCommand derived object. The window event handler catches the events
// and calls the cooresponding virtual member function on the CCommand object.

  // TODO: The XWindowEvents handler invokes the analogous virtual member function
  //       on the CCommand class. Override that member function in your derived
  //       class if you need to respond to an event.
  //       Common code for any event may be executed in the XWindowEvents handler
  //       for that event.

HRESULT CCommand::XWindowEvents::raw_WindowProc( LPDISPATCH pUnkDoc, LPDISPATCH pUnkView, UINT nMsg, WPARAM wParam, LPARAM lParam, LRESULT *lResult )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	//  Generic window event has occurred. If you have a cursor, this is where WM_SETCURSOR should
	//  be handled.

	//  Note that the lResult is how you return the value normally returned via the return code of a 
	//  standard WindowProc function. For example, for WM_SETCURSOR, Windows says the following:
	//
	//  "If the parent window returns TRUE, further processing is halted. Passing the message to a 
	//  window’s parent window gives the parent window control over the cursor’s setting in a child 
	//  window."
	//  Hence, you set *lResult to TRUE or FALSE depending on whether you set the cursor or not. In 
	// other words, setting to TRUE indicates that you don't want Solid Edge to set the cursor for you.

	hr = m_pCommand->WindowProc( pUnkDoc, pUnkView, nMsg, wParam, lParam, lResult );

	return hr;
}

// Locate filter events

// Want to respond to a locate filter event? Simply override the corresponding virtual member
// function of your CCommand derived object. The locate filter event handler catches the events
// and calls the cooresponding virtual member function on the CCommand object.

// TODO: The XLocateFilterEvents handler invokes the analogous virtual member functions
//       on the CCommand class. Override each member function in your derived
//       class if you need to respond to an event.
//       Common code for any event may be executed in the XLocateFilterEvents handler
//       for that event.

HRESULT CCommand::XLocateFilterEvents::raw_Filter( LPDISPATCH pGraphicDispatch, VARIANT_BOOL *vbValid )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	hr = m_pCommand->Filter( pGraphicDispatch, vbValid );

	return hr;
}
