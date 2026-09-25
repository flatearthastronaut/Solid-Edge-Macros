// Commands.cpp : implementation file
//

#include "stdafx.h"
#include "Commands.h"
#include "resource.h"
#include "locate.h"
#include "util.h"
#include "htmlhelp.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CCommands

// Init static instance data. I made these static simply for convenience.
ISEAddInExPtr CCommands::m_pSEAddIn = NULL;
ApplicationPtr CCommands::m_pApplication = NULL;;

// Implement two accessor functions for the static members. This allows any
// code to easily get to the Solid Edge add-in API or the Solid Edge
// application API.
AddInPtr GetAddInPtr() { return CCommands::GetAddIn(); }
ApplicationPtr GetApplicationPtr() { return CCommands::GetApplicationPtr(); }

CCommands::CCommands()
{
	m_pApplication = NULL;

	m_pSEAddIn = NULL;
  
	m_pApplicationEventsObj = NULL;
	
	m_pApplicationEventsExObj = NULL;
	m_pApplicationEventsEx2Obj = NULL;
	m_pAplicationFrameSwitchingEventsObj = NULL;

	m_pAplicationLicenseEventsObj = NULL;

	m_pAplicationDocumentLoadingEventsObj = NULL;

	m_pAddInEventsObj = NULL;
	m_pAddInEventsExObj = NULL;
	m_pAddInEventsEx2Obj = NULL;

	m_pShortCutMenuEventsObj = NULL;

	m_pAddInEdgeBarEventsObj = NULL;

	m_pXSaveAsTranslatorEventsObj = NULL;
}

CCommands::~CCommands()
{
	// MUST set the static app point to NULL now. DO NOT allow this object's destructor to run
	// when the add-in module unloads since the order of the unloading is not in my control.
	// Failure to null the pointer now can result in a crash during shutdown as the Edge module
	// that contains the app object may already be unloaded!
	m_pApplication = NULL;

	m_pSEAddIn = NULL;

	DestroyAllADDINDocuments();
}

HRESULT CCommands::SetApplicationObject( LPDISPATCH pApplicationDispatch,
                                         BOOL bWithEvents )
{
	ASSERT( pApplicationDispatch );

	HRESULT hr = NOERROR;

	m_pApplication = pApplicationDispatch;

	if( bWithEvents )
	{
		// Create Application event handlers
		XApplicationEventsObj::CreateInstance(&m_pApplicationEventsObj);
		if(m_pApplicationEventsObj)
		{
			m_pApplicationEventsObj->AddRef();
			hr = m_pApplicationEventsObj->Connect(m_pApplication);
			m_pApplicationEventsObj->m_pCommands = this;
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}
			
		// Create ApplicationEx event handlers
		XApplicationEventsExObj::CreateInstance(&m_pApplicationEventsExObj);
		if(m_pApplicationEventsExObj)
		{
			m_pApplicationEventsExObj->AddRef();
			hr = m_pApplicationEventsExObj->Connect(m_pApplication);
			m_pApplicationEventsExObj->m_pCommands = this;
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}

		// Create ApplicationEx2 event handlers
		XApplicationEventsEx2Obj::CreateInstance(&m_pApplicationEventsEx2Obj);
		if(m_pApplicationEventsEx2Obj)
		{
			m_pApplicationEventsEx2Obj->AddRef();
			hr = m_pApplicationEventsEx2Obj->Connect(m_pApplication);
			m_pApplicationEventsEx2Obj->m_pCommands = this;
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}

		// Create ApplicationFrameSwitching event handlers
		XApplicationFrameSwitchingEventsObj::CreateInstance(&m_pAplicationFrameSwitchingEventsObj);
		if(m_pAplicationFrameSwitchingEventsObj)
		{
			m_pAplicationFrameSwitchingEventsObj->AddRef();
			hr = m_pAplicationFrameSwitchingEventsObj->Connect(m_pApplication);
			m_pAplicationFrameSwitchingEventsObj->m_pCommands = this;
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}

		// Create ApplicatoinLicenseEvents event handlers
		XApplicationLicenseEventsObj::CreateInstance(&m_pAplicationLicenseEventsObj);
		if (m_pAplicationLicenseEventsObj)
		{
			m_pAplicationLicenseEventsObj->AddRef();
			hr = m_pAplicationLicenseEventsObj->Connect(m_pApplication);
			m_pAplicationLicenseEventsObj->m_pCommands = this;
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}

		// Create ApplicationDocumentLoadingEvents event handlers
		XApplicationDocumentLoadingEventsObj::CreateInstance(&m_pAplicationDocumentLoadingEventsObj);
		if (m_pAplicationDocumentLoadingEventsObj)
		{
			m_pAplicationDocumentLoadingEventsObj->AddRef();
			hr = m_pAplicationDocumentLoadingEventsObj->Connect(m_pApplication);
			m_pAplicationDocumentLoadingEventsObj->m_pCommands = this;
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}
		// Create shortcut (context) menu event handler
		XShortCutMenuEventsObj::CreateInstance(&m_pShortCutMenuEventsObj);

		if(m_pShortCutMenuEventsObj)
		{
			m_pShortCutMenuEventsObj->AddRef();
			hr = m_pShortCutMenuEventsObj->Connect(m_pApplication);
			m_pShortCutMenuEventsObj->m_pCommands = this;
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}
	}

	return hr;
}

HRESULT CCommands::UnadviseFromEvents()
{
	HRESULT hr = NOERROR;

	if( NULL != m_pAddInEventsObj )
	{
		hr = m_pAddInEventsObj->Disconnect(m_pSEAddIn);
		m_pAddInEventsObj->Release();
		m_pAddInEventsObj = NULL;
	}

	if( NULL != m_pAddInEventsExObj )
	{
		hr = m_pAddInEventsExObj->Disconnect(m_pSEAddIn);
		m_pAddInEventsExObj->Release();
		m_pAddInEventsExObj = NULL;
	}

	if( NULL != m_pAddInEventsEx2Obj )
	{
		hr = m_pAddInEventsEx2Obj->Disconnect(m_pSEAddIn);
		m_pAddInEventsEx2Obj->Release();
		m_pAddInEventsEx2Obj = NULL;
	}

	if( NULL != m_pApplicationEventsObj )
	{
		hr = m_pApplicationEventsObj->Disconnect(m_pApplication);
		m_pApplicationEventsObj->Release();
		m_pApplicationEventsObj = NULL;
	}
	
	if( NULL != m_pApplicationEventsExObj )
	{
		hr = m_pApplicationEventsExObj->Disconnect(m_pApplication);
		m_pApplicationEventsExObj->Release();
		m_pApplicationEventsExObj = NULL;
	}

	if( NULL != m_pApplicationEventsEx2Obj )
	{
		hr = m_pApplicationEventsEx2Obj->Disconnect(m_pApplication);
		m_pApplicationEventsEx2Obj->Release();
		m_pApplicationEventsEx2Obj = NULL;
	}

	if( NULL != m_pAplicationFrameSwitchingEventsObj )
	{
		hr = m_pAplicationFrameSwitchingEventsObj->Disconnect(m_pApplication);
		m_pAplicationFrameSwitchingEventsObj->Release();
		m_pAplicationFrameSwitchingEventsObj = NULL;
	}

	if( NULL != m_pAplicationLicenseEventsObj )
	{
		hr = m_pAplicationLicenseEventsObj->Disconnect(m_pApplication);
		m_pAplicationLicenseEventsObj->Release();
		m_pAplicationLicenseEventsObj = NULL;
	}

	if (NULL != m_pAplicationDocumentLoadingEventsObj)
	{
		hr = m_pAplicationDocumentLoadingEventsObj->Disconnect(m_pApplication);
		m_pAplicationDocumentLoadingEventsObj->Release();
		m_pAplicationDocumentLoadingEventsObj = NULL;
	}

	if( NULL != m_pShortCutMenuEventsObj )
	{
		hr = m_pShortCutMenuEventsObj->Disconnect(m_pApplication);
		m_pShortCutMenuEventsObj->Release();
		m_pShortCutMenuEventsObj = NULL;
	}
	
	if( NULL != m_pAddInEdgeBarEventsObj )
	{
		hr = m_pAddInEdgeBarEventsObj->Disconnect(m_pSEAddIn);
		m_pAddInEdgeBarEventsObj->Release();
		m_pAddInEdgeBarEventsObj = NULL;
	}

	// Although there is an AddInSaveAsTranslatorEvents that can be used to connect and disconnect, the addin object
	// actually supports the connection point just like with other add-in event sets such as AddInEvents(Ex/2) and EdgeBarEvents.
	// It makes disconnecting easy.
	if( NULL != m_pXSaveAsTranslatorEventsObj )
	{
		hr = m_pXSaveAsTranslatorEventsObj->Disconnect(m_pSEAddIn);
		m_pXSaveAsTranslatorEventsObj->Release();
		m_pXSaveAsTranslatorEventsObj = NULL;
	}

	return hr;
}

HRESULT CCommands::SetAddInObject( AddIn* pSolidEdgeAddIn,
                                   BOOL bWithEvents )
{
	// This function assumes pSolidEdgeAddIn has already been AddRef'd
	//  for us, which containing class did in its QueryInterface call
	//  just before it called us.

	ASSERT( pSolidEdgeAddIn );

	HRESULT hr = NOERROR;

	m_pSEAddIn = pSolidEdgeAddIn;

	if( bWithEvents )
	{
		// If running with a version of Edge that supports ISEAddInEventsEx2, I want to use that event
		// set.
		XAddInEventsEx2Obj::CreateInstance(&m_pAddInEventsEx2Obj);
		if( m_pAddInEventsEx2Obj )
		{
			m_pAddInEventsEx2Obj->AddRef();
			hr = m_pAddInEventsEx2Obj->Connect(m_pSEAddIn->GetAddInEvents());
			m_pAddInEventsEx2Obj->m_pCommands = this;
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}

		// I'm not checking if ISEAddInEventsEx2 was available. If it is, I'll use this object for
		// the on line help request that both event sets have.
		XAddInEventsExObj::CreateInstance(&m_pAddInEventsExObj);
		if( m_pAddInEventsExObj )
		{
			m_pAddInEventsExObj->AddRef();
			hr = m_pAddInEventsExObj->Connect(m_pSEAddIn->GetAddInEvents());
			m_pAddInEventsExObj->m_pCommands = this;
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}

		// I could be running a version of Edge that doesn't support the newer addin events. So I'll create the original
		// events object just in case. But I have another reason to create both. The only code difference between the
		// event sets is that the new one has the on-line help event for a command. So the "Ex" event object will simply
		// forward the calls for all the other events to this object.

		// Note: Solid Edge will fire the most recently added event set that the add-in connects to. So If I am in ST8
		//		 and I have connected to ISEAddInEventsEx2, I should never see Solid Edge fire an event to my
		//		 ISEAddInEventsEx or ISEAddInEvents event handlers. I can still reuse the code but I don't really have
		//		 to call Connect in an older event set if I connected to a newer one. But connecting to each is not really
		//       an issue - I won't receive a call to start a command more than once when a button in the UI is clicked!
		//       If I do decide to not call Connect once I make a connection in the above calls, I would want to keep
		//       some sort of data to know not to call Disconnect either.
		XAddInEventsObj::CreateInstance(&m_pAddInEventsObj);
		if( m_pAddInEventsObj )
		{
			m_pAddInEventsObj->AddRef();
			hr = m_pAddInEventsObj->Connect(m_pSEAddIn->GetAddInEvents());
			m_pAddInEventsObj->m_pCommands = this;
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}

		XAddInEdgeBarEventsObj::CreateInstance(&m_pAddInEdgeBarEventsObj);
		if( m_pAddInEdgeBarEventsObj )
		{

			m_pAddInEdgeBarEventsObj->AddRef();
			hr = m_pAddInEdgeBarEventsObj->Connect(m_pSEAddIn->GetAddInEdgeBarEvents());
			if( SUCCEEDED(hr) )
			{
				m_pAddInEdgeBarEventsObj->m_pCommands = this;
			}
			else
			{
				// I assume this version of edge did not support the event set. Edge bar
				// page will be added the old way.
				m_pAddInEdgeBarEventsObj->Release();
				m_pAddInEdgeBarEventsObj = NULL;
			}
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}

		ISEAddInSaveAsTranslatorPtr pSaveAsTranslator = m_pSEAddIn;
		if( pSaveAsTranslator )
		{
			XSaveAsTranslatorEventsObj::CreateInstance(&m_pXSaveAsTranslatorEventsObj);
			if( m_pXSaveAsTranslatorEventsObj )
			{
				m_pXSaveAsTranslatorEventsObj->AddRef();
				hr = m_pXSaveAsTranslatorEventsObj->Connect(pSaveAsTranslator->GetAddInSaveAsTranslatorEvents());
				if( SUCCEEDED(hr) )
				{
					m_pXSaveAsTranslatorEventsObj->m_pCommands = this;
				}
				else
				{
					// I assume this version of edge did not support the event set. Edge bar
					// page will be added the old way.
					m_pXSaveAsTranslatorEventsObj->Release();
					m_pXSaveAsTranslatorEventsObj = NULL;
				}
			}
			else
			{
				hr = E_OUTOFMEMORY;
			}
		}
	}

	return hr;
}

/////////////////////////////////////////////////////////////////////////////
// Application event handler.

// Application events: This is the only sink that exists for application events.

// TODO: Fill out the implementation for those events you wish handle
//  Use m_pCommands->GetApplicationPtr() to access the Solid
//  Edge Application object
HRESULT CCommands::XApplicationEvents::raw_AfterActiveDocumentChange( LPDISPATCH theDocument)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	
	// Edge added an event set for edge bar pages. If I am running in a version that supports
	// the event set, I defer to the event when creating the page (raw_AddPage). The version
	// the set was added was Solid Edge ST (a.k.a. version 100). I don't need to perform any
	// version check since I either have connected to the event set or not.
	if( NULL == m_pCommands->m_pAddInEdgeBarEventsObj )
	{
		PartDocumentPtr pPartDoc = theDocument;

		if( NULL != pPartDoc )
		{
			ADDINDocumentObj* pMyDoc;

			// CreateADDINDocument will store the document in the CMapSEDocDispatchToMyDoc (m_pDocuments)
			// It also does an additional AddRef since I am passing in a variable to get the doc pointer
			// returned. Hence the Release call below.
			m_pCommands->CreateADDINDocument( theDocument, TRUE, &pMyDoc );

			if( NULL != pMyDoc )
			{
				// edgebar will be removed in either the raw_RemovePage event handler or when the
				// doc close event is fired to ADDINDocument::XDocumentEvents::raw_BeforeClose
				pMyDoc->CreateEdgeBar();

				pMyDoc->Release();
			}
		}
	}

	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_AfterDocumentOpen( LPDISPATCH theDocument )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_AfterDocumentPrint( LPDISPATCH theDocument,
                                                               long hDC,
                                                               double *ModelToDC,
                                                               long *Rect )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_AfterDocumentSave( LPDISPATCH theDocument )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_AfterEnvironmentActivate( LPDISPATCH theEnvironment )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_AfterNewDocumentOpen( LPDISPATCH theDocument )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_AfterNewWindow( LPDISPATCH theWindow )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_AfterWindowActivate( LPDISPATCH theWindow )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_BeforeCommandRun( long lCommandID )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_AfterCommandRun( long lCommandID )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_BeforeDocumentClose( LPDISPATCH theDocument )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	
	// edgebar removed in either the raw_RemovePage event handler or when the
	// doc close event is fired to ADDINDocument::XDocumentEvents::raw_BeforeClose

	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_BeforeDocumentPrint( LPDISPATCH theDocument,
                                                                long hDC,
                                                                double *ModelToDC,
                                                                long *Rect )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_BeforeEnvironmentDeactivate( LPDISPATCH theEnvironment )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_BeforeWindowDeactivate( LPDISPATCH theWindow )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_BeforeQuit( void )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT CCommands::XApplicationEvents::raw_BeforeDocumentSave( LPDISPATCH theDocument )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

static int s_RadioState = 0;
static int s_CheckState = 1;

HRESULT CCommands::XApplicationEventsEx::raw_OnCommandUpdateUI( long CommandID, long* CommandFlags, BSTR* MenuItemText )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	if( 25010 == CommandID && s_CheckState )
	{
		*CommandFlags |= SolidEdgeConstants::seCmdActive_ChangeText;

		//*MenuItemText = SysAllocString(L"AsmLoc Add-in turns the place line command off whenever the AsmLoc checkbox is checked.");
		DWORD dwMajor = 0;
		DWORD dwMinor = 0;

		GetSolidEdgeVersion( dwMajor, dwMinor, GetApplicationPtr() );

		// And, if this is Solid Edge 2022, uncomment these lines to change the text (caption) on the tab, the tooltip and description. Pre 2022, the only way to change the caption was
		// to use SolidEdgeConstants::seCmdActive_ChangeUI. Now, a sneaky programmer has added an extra check to see if "\n" follows the tooltip and assumes the next substring is a
		// description. Unfortunately Solid Edge has no status bar to show a description when the app menu is active. But, this will work for commands on the command ribbon.
		if( dwMajor == 222 )
		{
			// The first part is the caption. The second part is the tooltip. If there are three parts, the third part is the desciption in Solid Edge 2022 (Major version 222).
			*CommandFlags |= (SolidEdgeConstants::seCmdActive_ChangeText | SolidEdgeConstants::seCmdActive_ChangeTooltip);
			*MenuItemText = SysAllocString(L"AsmLoc Place Line\nPlaces a line the AsmLoc way\nAsmLoc Places a line");
		}
	}
	else if( 25010 == CommandID && !s_CheckState )
	{
		// To reset the description/tooltip, set the flag and do nothing else.
		*CommandFlags |= SolidEdgeConstants::seCmdActive_ChangeText;
	}
	else if( 45024 == CommandID && s_CheckState )
	{
		*CommandFlags = 4;

		*MenuItemText = SysAllocString(L"AsmLoc Add-in turns the place line command off whenever the AsmLoc checkbox is checked.");
	}
	else if( 45024 == CommandID && !s_CheckState )
	{
		// To reset the description/tooltip, set the flag and do nothing else.
		*CommandFlags |= SolidEdgeConstants::seCmdActive_ChangeText;
	}
	else if( 11901 == CommandID )
	{
		// This is the 3DPrint command on the Solid Edge App menu that displays a dialog. Remove the tab by uncommenting the following lihe.
		// 
		//*CommandFlags = SolidEdgeConstants::seCmdActive_Remove;
		
		// Change the UI by defining each UI string by name by uncommenting the next two lines.
		//*CommandFlags = SolidEdgeConstants::seCmdActive_ChangeUI;
		//*MenuItemText = SysAllocString(L"caption=AsmLoc 3DPrint\ntooltip=AsmLoc 3DPrint using additive manufacturing\ndescription=Prepares the active document for additive manufacturing using a chosen provider");

		DWORD dwMajor = 0;
		DWORD dwMinor = 0;

		GetSolidEdgeVersion( dwMajor, dwMinor, GetApplicationPtr() );

		// Change the text (caption) on the tab and the tooltip by uncommenting the next two lines.
		//*CommandFlags |= (SolidEdgeConstants::seCmdActive_ChangeText | SolidEdgeConstants::seCmdActive_ChangeTooltip);
		//*MenuItemText = SysAllocString(L"AsmLoc 3DPrint\nAsmLoc 3DPrint using additive manufacturing");
		
		// And, if this is Solid Edge 2022, uncomment these lines to change the text (caption) on the tab, the tooltip and description. Pre 2022, the only way to change the caption was
		// to use SolidEdgeConstants::seCmdActive_ChangeUI. Now, a sneaky programmer has added an extra check to see if "\n" follows the tooltip and assumes the next substring is a
		// description. Unfortunately Solid Edge has no status bar to show a description when the app menu is active. But, this will work for commands on the command ribbon.
		if( dwMajor == 222 )
		{
			// The second part is the tooltip. If there are three parts, the third part is the desciption.
			*CommandFlags |= (SolidEdgeConstants::seCmdActive_ChangeText | SolidEdgeConstants::seCmdActive_ChangeTooltip);
			*MenuItemText = SysAllocString(L"AsmLoc 3DPrint\nAsmLoc 3DPrint using additive manufacturing\nPrepares the active document for additive manufacturing using a chosen provider");
		}
	}
	
	// So what is an easy way to discover Solid Edge command IDs? Create a .reg file (or run regedit) and set the "TooltipCmdID"
	// registry setting. Just modify the version number. Here is one for ST9:
	//
	// Windows Registry Editor Version 5.00

	// [HKEY_CURRENT_USER\Software\Unigraphics Solutions\Solid Edge\Version 109\ToolbarSettings]
	// "TooltipCmdID"=dword:00000001
	// Example of how to stop the save commands.
	// Common Microsoft commands:
	// #define ID_FILE_SAVE                    0xE103
	// #define ID_FILE_SAVE_AS                 0xE104
	//
	// Common Solid Edge commands:
	// #define ID_FILE_SAVE_AS_SPECIAL         40012
	// #define ID_FILE_SAVE_ALL                40008
	//
	// Any other commands that arise can have their command IDs discovered by using the TooltipCmdID reg entry mentioned in the comment above.

	// Uncomment this code to test disabling of some save commands to show this is possible.
	//if( 0xE103 == CommandID || 0xE104 == CommandID || 40012 == CommandID || 40008 == CommandID )
	//{
	//	*CommandFlags = 0;
	//}

	return S_OK;
}

HRESULT CCommands::XApplicationEventsEx2::raw_OnBeforeDocumentOpen( ApplicationBeforeDocumentOpenEvent Context, 
																 BSTR Filename, 
																 VARIANT_BOOL* vbCancelOpen )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	static int s_Test = 0; // I use this so I can run in debug and easily turn this "on" or "off" by setting s_Test to 1 or 0.
	if( s_Test )
	{
		*vbCancelOpen = VARIANT_TRUE;
	}

	return S_OK;
}
HRESULT CCommands::XShortCutMenuEvents::raw_BuildMenu (BSTR bstrEnvironmentCatid,
													   enum ShortCutMenuContextConstants Context,
													   LPDISPATCH pGraphicDispatch,
													   SAFEARRAY **MenuStrings,
													   SAFEARRAY **CommandIDs)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	HRESULT hr = NOERROR;

	CComSafeArray<BSTR> saCommandStrings;// For OLE automation, strings are passed as BSTRs in a safe array.
	hr = saCommandStrings.Create();
	if( SUCCEEDED(hr) )
	{
		CComSafeArray<long> saCommandIDs;
		hr = saCommandIDs.Create();
	
		if( SUCCEEDED(hr) )
		{
			CString sCmdString;
			sCmdString.LoadString(IDS_SHORTCUT_CMD);
	
			saCommandStrings.Add( sCmdString.AllocSysString() );

			saCommandIDs.Add( GetLocTestShortCutMenuCmdId() );
	
			// Its up to Solid Edge to destroy the safe arrays, including the bstrs. Since I am using the convenience of CComSafeArray I
			// have to detatch after putting the pointers into the return args as the CComSafeArray destructor will fire before I return
			// to Edge. Otherwise, Edge will have a pointer to a deleted safearray.
			*MenuStrings = saCommandStrings;

			saCommandStrings.Detach();

			*CommandIDs = saCommandIDs;

			saCommandIDs.Detach();
		}
	}
	return (hr);
}

/////////////////////////////////////////////////////////////////////////////
// AddIn event handlers.

// AddIn events: Over time new event sets have been added. This is an update for ST8
//               so I implement the latest event set added in ST8 and then the event
//               set added in ST6. Finally the original event set.
//
//		 Note for the following functions: 
//       nCmdID is the identifier the add-in passed to Solid Edge in either the SetAddInInfo
//		 SetAddInInfoEx, SetAddInInfoEx2 or AddCommand API calls.
//       It is NOT the runtime command identifier returned by Solid Edge
//       from those calls and which is used as the parameter in the WM_COMMAND message
//       posted to the app by Windows whenever a button or menu item is selected.

SAFEARRAY *pRefKey1 = 0;

void SetRefKey( SAFEARRAY * pRefKey ) { pRefKey1 = pRefKey; }

HRESULT CCommands::XAddInEventsEx2::raw_OnCommand( long nCmdID,
												  ShortCutMenuContextConstants Context, 
												  DocumentTypeConstants ActiveDocumentType, 
												  LPDISPATCH pActiveDocument, 
												  LPDISPATCH pActiveWindow, 
												  LPDISPATCH pActiveSelectSet )
{
	if( m_pCommands->m_pAddInEventsObj )
	{
		// This is just code that demonstrates the event set so I'm doing nothing fancy here. Declare some variables to let
		// me see what happens when I step thru the code.

		_bstr_t strDocumentName;
		_bstr_t strWindowCaption;
		long nNumberOfSelectedObjects = 0;

		SolidEdgeDocumentPtr pSEDocument = pActiveDocument;
		if( pSEDocument != NULL )
		{
			strDocumentName = pSEDocument->Name;
		}
		WindowPtr pSEWindow = pActiveWindow;
		if( pSEWindow != NULL )
		{
			strWindowCaption = pSEWindow->Caption;
		}
		SelectSetPtr pSESelectSet = pActiveSelectSet;
		if( pSESelectSet )
		{
			nNumberOfSelectedObjects = pSESelectSet->Count;
		}

		return m_pCommands->m_pAddInEventsObj->raw_OnCommand( nCmdID );
	}

	return E_UNEXPECTED;
}

HRESULT CCommands::XAddInEventsEx2::raw_OnCommandHelp( long hFrameWnd,
													   long uHelpCommand,
													   long nCmdID )
{
	if( m_pCommands->m_pAddInEventsObj )
	{
		return m_pCommands->m_pAddInEventsObj->raw_OnCommandHelp( hFrameWnd, uHelpCommand, nCmdID );
	}

	return E_UNEXPECTED;
}

HRESULT CCommands::XAddInEventsEx2::raw_OnCommandUpdateUI( long nCmdID,
														   ShortCutMenuContextConstants Context, 
														   DocumentTypeConstants ActiveDocumentType, 
														   LPDISPATCH pActiveDocument, 
														   LPDISPATCH pActiveWindow, 
														   LPDISPATCH pActiveSelectSet,
														   long *lCmdFlags,
														   BSTR *MenuItemText,
														   long *nIDBitmap )
{
	if( m_pCommands->m_pAddInEventsObj )
	{
		return m_pCommands->m_pAddInEventsObj->raw_OnCommandUpdateUI( nCmdID, lCmdFlags, MenuItemText, nIDBitmap );
	}

	return E_UNEXPECTED;
}

HRESULT CCommands::XAddInEventsEx2::raw_OnCommandOnLineHelp( long uHelpCommand,
														   long nCmdID,
														   BSTR* HelpURL )
{
	// If ISEAddInEventsEx2 was available, then ISEAddInEventsEx was too. So defer to it.

	if( m_pCommands->m_pAddInEventsExObj )
	{
		return m_pCommands->m_pAddInEventsExObj->raw_OnCommandOnLineHelp( uHelpCommand, nCmdID, HelpURL );
	}

	return S_OK;
}

HRESULT CCommands::XAddInEventsEx::raw_OnCommand( long nCmdID )
{
	if( m_pCommands->m_pAddInEventsObj )
	{
		return m_pCommands->m_pAddInEventsObj->raw_OnCommand( nCmdID );
	}
	
	return E_UNEXPECTED;
}

HRESULT CCommands::XAddInEventsEx::raw_OnCommandHelp( long hFrameWnd,
													  long uHelpCommand,
													  long nCmdID )
{
	if( m_pCommands->m_pAddInEventsObj )
	{
		return m_pCommands->m_pAddInEventsObj->raw_OnCommandHelp( hFrameWnd, uHelpCommand, nCmdID );
	}

	return E_UNEXPECTED;
}

HRESULT CCommands::XAddInEventsEx::raw_OnCommandUpdateUI( long nCmdID,
                                                          long *lCmdFlags,
                                                          BSTR *MenuItemText,
                                                          long *nIDBitmap )
{
	if( m_pCommands->m_pAddInEventsObj )
	{
		return m_pCommands->m_pAddInEventsObj->raw_OnCommandUpdateUI( nCmdID, lCmdFlags, MenuItemText, nIDBitmap );
	}

	return E_UNEXPECTED;
}

HRESULT CCommands::XAddInEventsEx::raw_OnCommandOnLineHelp( long uHelpCommand,
															long nCmdID,
															BSTR* HelpURL )
{
	// Lets see. I am not a real add-in. But I want to have something happen so programmers can see how to use this
	// api. What great web site can give Solid Edge? I know!

	*HelpURL = SysAllocString( L"www.solidedge.com" );

	// Of course it is theoretically possible to even give a .htm file that resides on disk.

	return S_OK;
}

HRESULT CCommands::XAddInEvents::raw_OnCommand( long nCmdID )
{
	HRESULT hr = S_OK;

	// I use the AFX_MANAGE_STATE macro so I will wrap all the code below in a try/catch.
	// If I fail to catch an exception, I don't want the user to see any of those nasty
	// invalid context activations exceptions windows (or MFC) started throwing with VS 2005.
	// The module state data now contains an "activation cookie" and contexts are "stacked".
	// If the add-in fails to catch an exception and the macro doesn't finish, the next time
	// the module state data is "popped", whatever context popped will have a cookie that doesn't
	// match the cookied on top of the stack. Lots of times the invalid activation context exception
	// can be followed by a crash (seems to depend on what code runs after the exception).
					
	// Use AFX_MANAGE_STATE. For command 0 and 1, I can access resources.
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	try
	{
		// TODO: Replace this with the actual code to execute the command
		//  Use m_pApplication to access the Solid Edge Application object,
		//  and VERIFY_OK to see error strings in DEBUG builds of your add-in
		//  (see stdafx.h)

		if( 6 == nCmdID )
		{
			// With ST3, Edge added a user setting that allows the user to choose between a horizontal bar that is very similar to
			// the original ribbon bar and a vertical bar, which is the task panel. If an add-in needs to present a different API
			// based on the UI the user has chosen (many edge commands do so), there is are global parameters on the frame that
			// can be used to determine the UI mode.

			// User turns command bar mode on using the ST3 application user options. Look at the "helpers" page
			// and you will see a "horizontal tool bar mode" and a "vertical docking mode". Horizontal mode is analogous
			// to the pre ST command ribbon bar/toolbar docked at the top of the app. The command bar is the vertical
			// "task panel" UI docked on the left (at least out of the box that is where it is docked).

			// For this sample, I have chosen command 6 to demonstrate the API that can be used to find the UI mode and start
			// a command that uses the original ribbon API or the newer command bar API. Since I already have command zero
			// using the command bar api and command 1 using the ribbon api, I simply change the command ID based on the
			// UI mode.

			bool bCommandBarMode = false;
			bool bQuickBarMode = false;

			ApplicationPtr	pApp = m_pCommands->GetApplicationPtr();

			if( pApp )
			{
				try
				{
					// Note: Edge now calls the docking pane container of the task panel the "command bar". The floating horizontal UI
					// is called the "quick bar". There is a mode for both but as of ST3, the user sees only one or the other. There
					// are two globals to provide for extensibility.
					_variant_t vCommandUIMode;

					pApp->GetGlobalParameter( SolidEdgeFramework::seApplicationGlobalCommandBarMode, &vCommandUIMode );

					bCommandBarMode = (int)vCommandUIMode == SolidEdgeConstants::seApplicationGlobalShowCommandBarMode;
					bQuickBarMode = (int)vCommandUIMode == SolidEdgeConstants::seApplicationGlobalShowQuickBarMode;
				}
				catch( _com_error &e )
				{
					bQuickBarMode = true;
					e;// assume running with a version of edge pre ST3 (could check the version before making the call).
				}
			}

			// Edge chose to expose a mode for each UI setting istead of a single mode to provide for future changes
			// that may or may not occur. So I look for the newest mode first and then the older mode.
			if( bCommandBarMode )
			{
				nCmdID = 0;
			}
			else if( bQuickBarMode )
			{
				// Note that "quick bar" is basically equivalent to the old ribbon bar that was docked on the app frame
				// in pre ST versions of edge.
				nCmdID = 1;
			}
			else
			{
				nCmdID = 1;
			}
		}

		// I only have code for some of the commands.
		switch ( nCmdID )
		{
			case 0:
			{
				// This command exercises the command bar API. Note that as of ST3, the user chooses either the vertical
				// or horizontal UI mode. However this command does not check the UI setting. This is to demonstrate that
				// for add-ins that already exist and use the command bar UI, edge will automatically change the mode
				// while the command has its UI up and running. To see this, before running the first command of the add-in,
				// make sure edge is in quick bar (horizontal toolbar) mode. Run an edge command like place line before
				// and after this command when in quick bar mode. Note the place line command will use the quickbar but
				// this command forces edge to display the command bar without actually affecting the user's UI setting.
				
				//Newer add-ins that want to be more ST3 compliant would check
				// the mode (which I did above for command 6).
				SolidEdgeDocumentPtr pDoc;

				try
				{
					pDoc = m_pCommands->GetApplicationPtr()->ActiveDocument;
				}
				catch( _com_error &e )
				{
					hr = e.Error();// No active doc (most likely E_FAIL.)
				}

				if( pDoc )
				{
					HRESULT hr = NOERROR;

					CSampleLocateCommand1Obj* pTheCommand = NULL;

					CSampleLocateCommand1Obj::CreateInstance(&pTheCommand);

					if( pTheCommand )
					{
						pTheCommand->AddRef();

						pTheCommand->SetCommandsObject( m_pCommands );
						hr = pTheCommand->CreateCommand( SolidEdgeConstants::seNoDeactivate );
						if( SUCCEEDED( hr ) )
						{
							// Very very important! Start the command so events will start flowing this way.
							pTheCommand->GetCommand()->Start();
						}
					}
				}
				else
				{
					AfxMessageBox(_T("This command needs a document opened first!"));
				}
		      
				break;
			}

			case 1:
			{
				// This sample command wants to process user events, including locates by the
				// SE provided smart mouse. It also wants to behave like a SE command.
				// So it calls the CreateCommand method to get hold of its
				// very own command control. The mouse control is one of the command's
				// properties. This sample will be using it too. CreateCommand will get a mouse
				// if I pass in seNoDeactivate.
				// This command also exercies the solid edge ribbon bar api. In ST3, the bar is called
				// the "quick bar" or the horizontal toolbar mode.
				
				SolidEdgeDocumentPtr pDoc;

				try
				{
					pDoc = m_pCommands->GetApplicationPtr()->ActiveDocument;
				}
				catch( _com_error &e )
				{
					hr = e.Error();// No active doc (most likely E_FAIL.)
				}

				if( pDoc )
				{
					HRESULT hr = NOERROR;

					CSampleLocateCommandObj* pTheCommand = NULL;

					CSampleLocateCommandObj::CreateInstance(&pTheCommand);

					if( pTheCommand )
					{
						pTheCommand->AddRef();

						pTheCommand->SetCommandsObject( m_pCommands );
						hr = pTheCommand->CreateCommand( SolidEdgeConstants::seNoDeactivate );
						if( SUCCEEDED( hr ) )
						{
							// Very very important! Start the command so events will start flowing this way.
							pTheCommand->GetCommand()->Start();
						}
					}
				}
				else
				{
					AfxMessageBox(_T("This command needs a document opened first!"));
				}
				break;
			}
		
			case 2:
			{
				// Set the flag used on OnCommandUpdateUI so I know when to check the button.
				s_RadioState = 1 - s_RadioState;

				break;
			}
			case 3:
			{
				// Set the flag used on OnCommandUpdateUI so I know when to check the button.
				s_CheckState = 1 - s_CheckState;

				break;
			}
			case 5:
			{
				// Note: The following type change is due to the fact that StartCommand was originally used
				// to start edge commands. But the API will start any command, including my special add-in
				// command.
				SolidEdgeCommandConstants LocTestCmdId = (SolidEdgeCommandConstants)GetLocTestCmdId();

				AfxMessageBox(_T("Asmloc sample command invoked. This command will now start the command the user does not have direct access to."));

				GetApplicationPtr()->StartCommand( LocTestCmdId );
				
				break;
			}
			case 6:
			case 7:
			{
				AfxMessageBox(_T("AsmLoc sample command invoked. But this is the extent of the command!"));
				
				break;
			}

			case 10:
			{
				AfxMessageBox(_T("Special AsmLoc command invoked! But this is the extent of the command!"));
				
				break;
			}

			case 11:
			{
				AfxMessageBox(_T("Special AsmLoc Backstage command invoked! But this is the extent of the command!"));

				break;
			}

			case 12:
			{
				AfxMessageBox(_T("Special AsmLoc Backstage command invoked! But this is the extent of the command!"));

				break;
			}

			default:
			{
			  break;
			}
		}
  }
  catch( _com_error &e )
  {
	  hr = e.Error();
  }

  return hr;
}

// I created a MakeHelp.bat file in the project directory. It's a cheap version of a help
// file that I have not kept up to date. Also Windows over time has modified how help
// works. I am no expert on help so help is pretty much up to the add-in. At one time
// the project had a post-build step that installed help into the Windows help directory.
// However I removed that step because how help works has changed over time and one day
// the step generated an error (but the build of the DLL succeeds).


// The wizard has created an include file in the project's hlp directory that contains 
// definitions for each of the context identifiers and htm pages for the addin's introduction
// and commands. This allows the help author to modify the help file source files without
// necessarily forcing changes to the addin's help support functions.
#include "hlp\asmlochelp.h"

static DWORD ConvertCommandIDToHelpID( long nCmdID, CString& strTopic )
{
	// TODO: This assumes the command ids passed to Solid Edge are
	//       0,1,2 ... while your help context ids for the commands
	//       are 2,3,4 .... The help context ids are defined in
	//       hlp\asmlochelp.h. The wizard has assigned 1 to
	//       be the id for your help introduction, 2 for your first
	//       command and 3 for your second.
	//       Modify this code or redefine your context ids if this is 
	//       no longer true in your case. Note: During initial testing
	//       with html help, I was unable to assign 0 to the intro, 1
	//       for the first command, 2 for the second ... and get html
	//       help to bring up the correct help page for the commands.
	//       For some reason, it appeared that using 0 for the intro
	//       caused a problem. That is why 1 is used for the intro.

	// Note: If you prefer to use topic based HtmlHelp invokation, set 
	//       strTopic to the name of the particular topic you want HtmlHelp
	//       to display. In that case, the returned help context identifier
	//       will be ignored and the topic will be used.
	//       Example: strTopic = ModalDialogBoxCommandTopic;

	switch ( nCmdID )
	{
	case 0:
	case 1:
		{
			nCmdID = LocateCommandContext;
			// strTopic = LocateCommandTopic;
			break;
		}
	case 2:
		{
			nCmdID = ModalDialogBoxCommandContext;
			// strTopic = ModalDialogBoxCommandTopic;
			break;
		}
	default:
		{
			// Assuming you have added more commands than the wizard generated default,
			// this code will work assuming the help author incremented context identifiers
			// by one while you incremented the command ids passed to Solid Edge by one.
			// Of course, they two must be in sync. Of course, the best approach is to
			// add to the enumerated cases in this switch statement and to maintain definitions
			// in the hlp\tsthelphelp.h file.
			//nCmdID += 2;
			nCmdID = ModalDialogBoxCommandContext;
			break;
		}
	}
	return( (DWORD) nCmdID );
}

HRESULT CCommands::XAddInEvents::raw_OnCommandHelp( long hFrameWnd,
												   long uHelpCommand,
												   long nCmdID )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_FALSE;

	// TODO: Bring up add-in's help for this command.
	// Note: -1 => Bring up help for add-in itself.

	// Assumes that you have registered your help file with Windows. If you do not
	// register the help file with Windows, you have to supply your own path.

	HWND hHelpWnd = 0;

	// Gee, Windows Vista no longer supports .hlp files. I have converted the help to
	// chm based. But the MakeHelp.bat file delivered with this add-in has encountered
	// yet another Microsoft issue. Apparently HTML Help Workshop is delivered with
	// Visual Studio (I don't recall installing it so that's my assumption) and is not
	// in the user's path like the old help compiler was. So I had to edit that file
	// specifically for my machine. You will have to modify it for your environment.
	// By the way, on my machine the directory that has the compiler in it is
	// d:\program files (x86)\html help workshop\hhc.exe
	CString strHelpPath;

	// The following code assumes the chm file is located in the same directory as
	// your server. So on a machine where your build is occurring, if you are testing
	// help you will need to copy the chm file to the output direcotry where your
	// DLL resides. You might want to add a post build step to always copy the chm
	// file from the hlp directory to the output directory where the DLL is placed to
	// facilitate testing for all build targets (debug, release, win32 and x64).
	TCHAR tcsFileName[1024] = {0} ;
	DWORD dwError = GetModuleFileName(hMyInstance(), tcsFileName, sizeof(tcsFileName) / sizeof(*tcsFileName)) ;
	if(dwError)
	{
		strHelpPath = tcsFileName;

		strHelpPath.MakeLower(); // Replace won't work if the case doesn't match.

		strHelpPath.Replace( _T("asmloc.dll"), _T("asmlochelp.chm") );
	}

	if( -1 == nCmdID )
	{
		// Note: If you need to bring up a topic other than the default topic, you can append
		//       "::/Topic.htm" to the second argument where Topic.htm is the particular html 
		//       page you want HtmlHelp to display. This may be useful for example, if you 
		//       have more than one addin documented in a single help file. For an example, see
		//       the code below.

		// Note: The wizard did not generate code to pass in the frames hwnd because HtmlHelp
		//       will bring up a window that acts like a modal dialog box. The user will be unable
		//       to interact with Solid Edge unless the help window is dismissed or collapsed to
		//       the task bar. At least in the version of HtmlHelp Solid Edge was testing with ...
		hHelpWnd = AddinHtmlHelp( 0, strHelpPath, HH_DISPLAY_TOPIC, 0 );
	}
	else
	{
		DWORD dwHelpID = 0;
		// To support upwards compatibility, Solid Edge passes in a WinHelp definition
		// for the type of help requested (uHelpCommand). For html help, this needs to
		// be converted since Microsoft chose to use different values for the actual
		// definitions that accomplish the "same" functionality in the two help
		// systems! 

		// Note that for HtmlHelp, the dwData parameter (the dwHelpID variable
		// here) can point to the name of an html page when HH_DISPLAY_TOPIC is passed
		// in or a context identifier if HH_HELP_CONTEXT  is passed in. It is up to the
		// help author to decide which method is preferred. 
		// If NULL is passed in for dwData when HH_DISPLAY_TOPIC is used, HtmlHelp
		// will bring up the default topic. The wizard prefers the alternative and
		// apparently equivalent method of appending "::/Topic.htm" to the file name
		// where "Topic.htm" is the topic you want to have HtmlHelp display. This avoids
		// having to cast the pointer to the topic string to a DWORD (or DWORD_PTR on 64
		// bit machines).

		long uHtmlHelpCommand;

		switch (uHelpCommand)
		{
		case HELP_CONTEXT:
			{
				CString strTopic;

				uHtmlHelpCommand = HH_HELP_CONTEXT;

				dwHelpID = ConvertCommandIDToHelpID( nCmdID, strTopic );

				if( ! strTopic.IsEmpty() )
				{
					// A topic was supplied. Append the topic to the filename and set the help 
					// context identifier to zero (or else HtmlHelp will assume it is a pointer
					// to the topic itself and access it as such and generate an access violation!

					strHelpPath += "::/";
					strHelpPath += strTopic;

					dwHelpID = 0;
				}
				break;
			}
		case HELP_FINDER:
			{
				// Use HH_DISPLAY_TOPIC with the dwHelpID set to zero to bring up the default topic.
				// See the notes above on how to bring up a topic different from the default topic.
				uHtmlHelpCommand = HH_DISPLAY_TOPIC;
				break;
			}
		default:
			{
				// Use HH_DISPLAY_TOPIC with the dwHelpID set to zero to bring up the default topic.
				// See the notes above on how to bring up a topic different from the default topic.
				uHtmlHelpCommand = HH_DISPLAY_TOPIC;
				break;
			}
		}

		// Note: The wizard did not generate code to pass in the frames hwnd because HtmlHelp
		//       will bring up a window that acts like a modal dialog box. The user will be unable
		//       to interact with Solid Edge unless the help window is dismissed or collapsed to
		//       the task bar. At least in the version of HtmlHelp Solid Edge was testing with ...

		// Note: Yet another Microsoft bug. I kept getting the infamous error message when trying
		// to invoke help on a topic, "HH_HELP_CONTEXT called without a [MAP] section site:microsoft.com".
		// Totally bogus as the help project file had everything that was needed according to the
		// information I could find (an alias section preceeding the map section). I eventually
		// found this article: http://support.microsoft.com/kb/188444/EN-US/
		// The gist of the article is that one should use notepad or wordpad to edit the files.
		// Open the project file in notepad. Notice I have "#include asmlochelp.ali". Being a dummy
		// I had originally used the help authoring UI to specify the alias file and it had inserted
		// "= asmlochelp.ali". So after reading the article I opened all my files in notepad and
		// saved them out while also chaning the "=" to "#include". To make a long story short, after
		// spending an embarrasing amount of time trying to figure out why I kept getting the error,
		// I finally got it to work. I hate to say it but typical Microsoft modus-operandi. Note the
		// above article is dated March 1 2005 and the article admits there are bugs. Well this is
		// June 2009 and apparently the bug still is not fixed. But hey, as long as there is a kb
		// article, the MS mindset appears to be "why fix the bug (ever)."
		hHelpWnd = AddinHtmlHelp( 0, strHelpPath, uHtmlHelpCommand, dwHelpID );
	}

	if( hHelpWnd )
	{
		hr = S_OK;
	}

	return( hr );
}

//HRESULT CCommands::XAddInEvents::raw_OnCommandHelp( long hFrameWnd,
//                                                    long uHelpCommand,
//                                                    long nCmdID )
//{
//	AFX_MANAGE_STATE(AfxGetStaticModuleState());
//
//	HRESULT hr = S_FALSE;
//
//	// TODO: Bring up add-in's help for this command.
//	// Note: -1 => Bring up help for add-in itself.
//
//	// Example using WinHelp:
//	// Assumes that you have registered your help file with Windows. If you do not
//	// register the help file with Windows, you have to supply your own path.
//
//	// I would assume that a real add-in would deliver a very useful help file,
//	// probably in the same directory as the add-in's DLL. If so, get the module
//	// path and append the help filename to it and pass that into the API calls
//	// below.
//	
//
//	// Gee, Windows Vista no longer supports .hlp files. I have converted the help to
//	// chm based. But the MakeHelp.bat file delivered with this add-in has encountered
//	// yet another Microsoft issue. Apparently HTML Help Workshop is delivered with
//	// Visual Studio (I don't recall installing it so that's my assumption) and is not
//	// in the user's path like the old help compiler was. So I had to edit that file
//	// specifically for my machine. You will have to modify it for your environment.
//	// By the way, on my machine the directory that has the compiler in it is
//	// d:\program files (x86)\html help workshop\hhc.exe
//	CString strHelpPath = "asmlochelp.chm";
//
//	// The following code assumes the chm file is located in the same directory as
//	// your server. So on a machine where your build is occurring, if you are testing
//	// help you will need to copy the chm file to the output direcotry where your
//	// DLL resides. You might want to add a post build step to always copy the chm
//	// file from the hlp directory to the output directory where the DLL is placed to
//	// facilitate testing for all build targets (debug, release, win32 and x64).
//	TCHAR tcsFileName[1024] = {0} ;
//	DWORD dwError = GetModuleFileName(hMyInstance(), tcsFileName, sizeof(tcsFileName) / sizeof(*tcsFileName)) ;
//	if(dwError)
//	{
//		strHelpPath = tcsFileName;
//
//		strHelpPath.MakeLower(); // Replace won't work if the case doesn't match.
//
//		strHelpPath.Replace( "asmloc.dll", "asmlochelp.chm" );
//	}
//
//	BOOL bOk = FALSE;
//
//	UINT uHtmlHelpCommand;
//
//	// Note: Solid Edge is sending in WinHelp values. It is up to the add-in to convert these to
//	// html values. This is my best shot at the conversion (I said I am no help expert). Also, I
//	// currently have Vista and Microsoft may have retained all the bugs of every Windows OS since
//	// Win95 even in Vista but the mentats in Redmond have decided not to deliver the original
//	// executable that can run help on .hlp files.
//	switch ( uHelpCommand )
//	{
//		case HELP_CONTEXT:
//		{
//			uHtmlHelpCommand = HH_HELP_CONTEXT;
//			break;
//		}
//
//		case HELP_FINDER:
//		{
//			uHtmlHelpCommand = HH_DISPLAY_TOPIC;
//			break;
//		}
//
//		default:
//		{
//			uHtmlHelpCommand = HH_DISPLAY_TOPIC;
//			break;
//		}
//	}
//
//	DWORD dwHelpID = ConvertCommandIDToHelpID( nCmdID );
//
//	// Note on HtmlHelp - as mentioned, the version of Visual Studio I am working with delivers the html
//	// help workshop. When I examined the MSDN, it said I should copy the htmlhelp.h file to my project
//	// and the htmlhelp.lib file to my lib dir (this is what we actually do in Edge and have done so since
//	// edge moved to chm files. Instead of doing that, I simply included the include file at the top of this
//	// file and added htmlhelp.lib to the project/linker/input "Additional dependencies" settings. And I
//	// was able to compile and run. But be aware, the warranty for this add-in does not cover every version
//	// of Visual Studio, the Windows OS, the version of IE, the version of common controls ... that may
//	// affect this code's ability to compile.
//	// Note on HtmlHelp: I passed in HH_HELP_CONTEXT and dwHelpID of zero and locked up Edge. Well actually
//	// I found that deep down in Windows the help control (some function on the stack with the module
//	// being and ocx) delivered by Windows was waiting on an object. Apparently they are willing to wait
//	// longer than me as I finally restarted. Seems the input to HtmlHelp is quite sensitive.
//	HWND hWndHelp = ::HtmlHelp( NULL, strHelpPath, uHtmlHelpCommand, dwHelpID );
//	if ( ! hWndHelp )
//	{
//		hWndHelp = ::HtmlHelp( NULL, strHelpPath, HH_DISPLAY_TOPIC, 0 );
//	}
//
//	// Rather than deleting, I am commenting out the code that ran for me since I originally wrote the first 
//	// pass of this addin circa 1999 because of Vista changes. Just in case ...
//
//	//if( -1 == nCmdID )
//	//{
//	//	bOk = ::WinHelp( (HWND)hFrameWnd, strHelpPath, HELP_FINDER, 0 );
//	//}
//	//else
//	//{
//	//	DWORD dwHelpID = ConvertCommandIDToHelpID( nCmdID );
//	//	bOk = ::WinHelp( (HWND)hFrameWnd, strHelpPath, uHelpCommand, dwHelpID );
//	//}
//
//	if( hWndHelp )
//	{
//		hr = S_OK;
//	}
//
//	return( hr ); // I believe if the hr indicates an error, edge will inform the user for us.
//}
//

HRESULT CCommands::XAddInEvents::raw_OnCommandUpdateUI( long nCmdID,
                                                        long *lCmdFlags,
                                                        BSTR *MenuItemText,
                                                        long *nIDBitmap )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	
	// TODO: Update the user interface for the command whose id is nCmdID. 
	// Note: Taking no action results in the command being enabled.

	*lCmdFlags = SolidEdgeConstants::seCmdActive_Enabled;

	// For check and radio buttons, the input flag is always cleared of the checked bit. Do nothing
	// to "uncheck".
	if( 2 == nCmdID )
	{
		if( s_RadioState )
		{
			*lCmdFlags |= SolidEdgeConstants::seCmdActive_Checked;
		}
	}
	else if( 3 == nCmdID )
	{
		if( s_CheckState )
		{
			*lCmdFlags |= SolidEdgeConstants::seCmdActive_Checked;
		}
	}
	
	// The following code can be used to examine how the change UI enhancements to Solid Edge can be used. Use the static flag to
	// turn various UI changes on. Note that some of the code below uses hard-coded paths so it will need to be modified for your usage.
	bool EdgeVersionSupportsChangeUI(ApplicationPtr & pEdge);// "Real" code wouldn't copy these prototypes.
	void MyTooltipsFolder(CString & Folder);

	static int s_replace = 0;
	CBitmap Test;
	if( s_replace == 1 && (0 == nCmdID || 10 == nCmdID) )
	{
		CString strTest;
		strTest.LoadString(IDS_DESCRIPTION);

		Test.LoadBitmap( IDB_CHECK/*IDB_BITMAP3*/ );

		if( nIDBitmap )
		{
			*nIDBitmap = IDB_CHECK;
			*lCmdFlags |= SolidEdgeConstants::seCmdActive_UseBitmap;
		}

		if( MenuItemText )
		{
			BSTR newText = SysAllocString(L"dynamically added text");
			if( newText )
			{
				*MenuItemText = newText;
				*lCmdFlags |= SolidEdgeConstants::seCmdActive_ChangeText;
			}
		}
	}
	else if( s_replace == 2 )
	{
		if( 0 == nCmdID || 10 == nCmdID )
		{
			*MenuItemText = SysAllocString(L"new menu text");
			*lCmdFlags |= SolidEdgeConstants::seCmdActive_ChangeText;
		}
	}
	else if( s_replace == 3 )
	{
		*MenuItemText = SysAllocString(L"caption=new menu text\ndescription=Still displays a message box\ntooltip=new tooltip text\nimagefile=D:\\AsmLoc\\VC\\res\\CHECK.BMP");
		*lCmdFlags |=  SolidEdgeConstants::seCmdActive_ChangeUI;
	}
	else if( s_replace == 4 )
	{
		*MenuItemText = SysAllocString(L"Description=Still displays a message box\ntooltip=new tooltip text\nimagefile=D:\\AsmLoc\\VC\\res\\CHECK.BMP");
		*lCmdFlags |=  SolidEdgeConstants::seCmdActive_ChangeUI;
	}
	else if( s_replace == 5 )
	{
		*MenuItemText = SysAllocString(L"caption=new menu text\nimagefile=D:\\AsmLocST7\\VC\\res\\CHECK.BMP");
		*lCmdFlags |=  SolidEdgeConstants::seCmdActive_ChangeUI;
	}
	else if( s_replace == 6 )
	{
		*lCmdFlags |= SolidEdgeConstants::seCmdActive_Remove;
	}
	else if( s_replace == 7 )
	{
		*MenuItemText = SysAllocString(L"imagefile=D:\\AsmLoc\\VC\\res\\CHECK.BMP\naction=notepad.exe\nparameter=test.txt");
		*lCmdFlags |=  SolidEdgeConstants::seCmdActive_ChangeUI;
	}
	else if( s_replace == 8 && (nCmdID == 0 || nCmdID == 5) )
	{
		CString strTipFolder; 
		MyTooltipsFolder(strTipFolder);

		CString strItemText = _T("tooltipurl=");
		strItemText += strTipFolder;
		strItemText += _T("commands_box.htm");

		*MenuItemText = strItemText.AllocSysString();

		*lCmdFlags |=  SolidEdgeConstants::seCmdActive_ChangeUI;
	}
	else if( s_replace == 9 && nCmdID == 0 )
	{
		*MenuItemText = SysAllocString(L"tooltip=Still displays a message box");
		*lCmdFlags |=  SolidEdgeConstants::seCmdActive_ChangeUI;
	}

	return S_OK;
}

HRESULT CCommands::XAddInEdgeBarEvents::raw_AddPage( LPDISPATCH pSEDocumentDispatch )
{
	HRESULT hr = S_OK;

	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	ADDINDocumentObj* pMyDoc;

	PartDocumentPtr pPartDoc = pSEDocumentDispatch;

	if( pPartDoc )
	{
		m_pCommands->CreateADDINDocument( pSEDocumentDispatch, TRUE, &pMyDoc );

		if( NULL != pMyDoc )
		{
			pMyDoc->CreateEdgeBar();

			pMyDoc->Release();
		}
	}

	return hr;
}

HRESULT CCommands::XAddInEdgeBarEvents::raw_RemovePage( LPDISPATCH pSEDocumentDispatch )
{
	HRESULT hr = S_OK;
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	ADDINDocumentObj* pDoc = NULL;

	BOOL bAlreadyThere = m_pCommands->m_pDocuments.Lookup(pSEDocumentDispatch, pDoc);

	if( pDoc )
	{
		pDoc->RemoveEdgeBarPage();
	}

	return hr;
}

HRESULT CCommands::XAddInEdgeBarEvents::raw_IsPageDisplayable ( IDispatch * pSEDocumentDispatch,
																BSTR EnvironmentCatID,
																VARIANT_BOOL * vbIsPageDisplayable )
{
	HRESULT hr = S_OK;
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	ADDINDocumentObj* pDoc = NULL;

	BOOL bAlreadyThere = m_pCommands->m_pDocuments.Lookup(pSEDocumentDispatch, pDoc);

	if( pDoc )
	{
		// Note: In stdafx.h, I have defined _ATL_CSTRING_EXPLICIT_CONSTRUCTORS. That caused the
		// assignment of strEnv = EnvironmentCatID to generate an error. So I pass the BSTR to the
		// CString via the constructor ( strEnv(EnvironmentCatID)). I am now building with Visual Studio 2008
		// (MFC ver is 9). If compiling with a previous version of MFC, the code may need to be
		// changed back if it fails to compile.
		CString strEnv(EnvironmentCatID);
		CString strSupportedEnv = _T("{26618396-09d6-11d1-ba07-080036230602}"); // part

		CString strLIP = _T("{0DDABC90-125E-4cfe-9CB7-DC97FB74CCF4}"); // sketch/layout in part
		strLIP.MakeLower();

		strEnv.MakeLower();
		strSupportedEnv.MakeLower();

		if( strEnv != strSupportedEnv ) //&& strEnv != strLIP ) // uncomment if wanted in sketch/layout in part
		{
			*vbIsPageDisplayable = VARIANT_FALSE;
		}
	}

	return hr;
}

// Creates a CEDGEBARTSTDocument given the dispatch of a Solid Edge document. Connects to the Solid Edge 
// document's event set if bWithEvents is TRUE. If ppEDGEBARTSTDocument is not NULL, the created document
// will be AddRef'd and returned in ppEDGEBARTSTDocument. If a CEDGEBARTSTDocument corresponding to 
// pDocDispatch already exists, a new one will NOT be created. If such is the case, the existing document 
// will be AddRef'd and returned if ppEDGEBARTSTDocument is not NULL.
HRESULT CCommands::CreateADDINDocument ( LPDISPATCH pSEDocumentDispatch,
										 BOOL bWithEvents,
										 ADDINDocumentObj** ppADDINDocument )
{
	HRESULT hr = NOERROR;

	ADDINDocumentObj* pDoc = NULL;

	BOOL bAlreadyThere = m_pDocuments.Lookup( pSEDocumentDispatch, pDoc );

	if( FALSE == bAlreadyThere )
	{
		ADDINDocumentObj::CreateInstance( &pDoc );
		if( pDoc )
		{
			pDoc->AddRef();
			pDoc->SetCommandsObject( this );
			pDoc->SetDocumentObject( pSEDocumentDispatch, bWithEvents );

			m_pDocuments.SetAt( pSEDocumentDispatch, pDoc );
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}
	}

	if( ppADDINDocument )
	{
		*ppADDINDocument = pDoc;
		(*ppADDINDocument)->AddRef();
	}

	return hr;
}

HRESULT CCommands::DestroyADDINDocument( LPDISPATCH pSEDocumentDispatch )
{
	HRESULT hr = NOERROR;

	ADDINDocumentObj* pDoc = NULL;

	BOOL bDocExists = m_pDocuments.Lookup(pSEDocumentDispatch, pDoc);

	if( bDocExists )
	{  
		m_pDocuments.RemoveKey(pSEDocumentDispatch);

		C_RELEASE(pDoc);
	}
	else
	{
		hr = E_INVALIDARG;
	}

	return hr;
}

HRESULT CCommands::DestroyAllADDINDocuments( void )
{
	HRESULT hr = NOERROR;

	POSITION pos = m_pDocuments.GetStartPosition();

	while( NULL != pos )
	{
		LPDISPATCH pDocDispatch = NULL;
		ADDINDocumentObj *pDoc = NULL;

		m_pDocuments.GetNextAssoc( pos, pDocDispatch, pDoc );

		// This should be the final Release. The doc will disconnect
		// from any event set and release the dispatch pointer it
		// has stored.
		C_RELEASE(pDoc)
	}

	m_pDocuments.RemoveAll();

	return hr;
}

// Given the dispatch of a Solid Edge document, lookup, AddRef and return the ADDINDocument
// if one exists in the collection. Returns NULL if one does not exist.
ADDINDocumentObj* CCommands::GetDocument( LPDISPATCH pDocDispatch )
{
	ADDINDocumentObj* pDoc = NULL;

	BOOL bDocExists = m_pDocuments.Lookup(pDocDispatch, pDoc);

	if( NULL != pDoc )
	{
		pDoc->AddRef();
	}

	return pDoc;
}

HRESULT CCommands::XSaveAsTranslatorEvents::raw_OnOptions( LPDISPATCH theDocument, BSTR FilenameExtension )
{
	HRESULT hr = S_OK;

	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	// To do: Bring up your translator options user interface if you have one.
	AfxMessageBox(_T("This is just an example. I don't really have any options."), MB_OK);

	return hr;
}

HRESULT CCommands::XSaveAsTranslatorEvents::raw_OnOptionsUpdateUI( LPDISPATCH theDocument, BSTR FilenameExtension, long* Flags )
{
	HRESULT hr = S_OK;

	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	// To do: If you support bringing up an options user interface, set Flags to non-zero. The
	// user interface on the SolidEdge will enable an options button the user can press. If the
	// user presses it, your OnOptions event will be raised.

	// Note: I originally committed a grave error here. I didn't call wcsstr to check for the substring
	//       AND I had a space before the "*" in "*.x_t" when I called SetSaveAsTranlatorInfo in
	//       SEAddin.cpp. I just removed the space. Until then, I always enabled the options dialog.
	//       But to test, I wanted to enable it for my mysterious z_t file extension and disable it
	//       for x_t.
	if( wcscmp( FilenameExtension, L"*.x_t" ) )
	{
		*Flags = 1;// Pretend I have an options UI for this format.
	}

	return hr;
}

HRESULT CCommands::XSaveAsTranslatorEvents::raw_OnSaveAs( LPDISPATCH theDocument, BSTR SaveAsFilename, long* hResult )
{
	HRESULT hr = S_OK;

	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	// To do: Implement SaveAs and set the hResult to a COM HRESULT value such as S_OK (0) or S_FALSE if you want to
	// cancel or any COM error code such as E_FAIL.

	if( hResult )// surely Solid Edge would not send me a null pointer!
	{
		*hResult = NOERROR;

		PartDocumentPtr pPartDoc = theDocument;
		if( pPartDoc )
		{
			try
			{
				pPartDoc->SaveAs( SaveAsFilename );
			}
			catch( _com_error& e )
			{
				*hResult = e.Error();
			}
		}
		else
		{
			*hResult = E_INVALIDARG;
		}
	}
	return hr;
}

// A couple of utility APIs to help with frame switching.

// SwitchWindowOnwer makes sure the window is not a child window. Then it sets the window owner to the new frame.
// Dev note: What happens if your non-modal window doesn't follow the active frame? If the user activates your
// window, the OS will also activate the owned window. This will cause what I call "frame flopping" in the
// application. That is when the frame containing the active document is deactivated and another frame is
// activated. And, who wants that?
// 
// Dev note: So, my add-in has a docking pane and what happens to docking panes? Well, currently, Solid Edge only
// supports docking panes that are docked in the main application frame window. So of course, clicking in a docked
// pane will activate the main window. If the user has floated the docking pane, Solid Edge makes sure floating
// docking panes follow the active frame. Basically, the two APIs here are what Solid Edge does for those windows
// and any other windows that have to "follow the frame".
// 
// SwitchWindowOnwer - Take in the frames being swtiched and a window currently owned by the "previous frame"
// and switch its owner to the newly activated frame.
// 
// hPreviousFrame is the frame being deactivated.
// hNewFrame is the frame being activated.
// hWnd is the handle of a window that is either owned by the previous frame or is a child of the previous frame.
bool SwitchWindowOwner( HWND hPreviousFrame, HWND hNewFrame, HWND hWnd )
{
	bool bSwappedOwners = false;

	if( hWnd && hNewFrame )
	{
		// Dev Note: Solid Edge registers a WM_ACTIVEFRAMECHANING message it sends to a window to give that
		// window a chance to do its own frame swtiching. Some MFC windows will actually set a "m_pParent"
		// in instance data (luckily it isn't private) and Solid Edge has some such windows. This lets those
		// windows set the stored MFC parent instance data otherwise, MFC will cause frame flopping even though
		// the window's owner/parent is switched. In some cases the window is so complicated, other actions
		// need to be taken to completely avoid frame flopping or to ensure the UI of such a window continues
		// to work properly (the "Variable Table" is one such window in Edge that needs this message.
		// You may find it useful to do the same thing so I leave you these two droids, I mean these lines of
		// code just in case. Of course, you need to define the message one way or the other.

		//LRESULT lr = ::SendMessage( hWnd, WM_ACTIVEFRAMECHANGING, (WPARAM)hPreviousFrame, (LPARAM)hNewFrame );
		//if( WM_ACTIVEFRAMECHANING_LRESULT == lr )
		//{
		//	// This window indicated it did the right thing for it. Just move along.
		//	return true;
		//}

		// MSDN says not to use GWLP_HWNDPARENT to change the parent of a window. So, no WS_CHILD shall pass.
		// One has to remove WS_CHILD and add WS_POPUP first, then change the owner, sync UI state ...
		// and all that is irrelevant to this code.
		if( !(::GetWindowLong( hWnd, GWL_STYLE ) & WS_CHILD) )
		{
			HWND hOwner = ::GetWindow(hWnd, GW_OWNER );// There is a GetParent but not GetOwner in the Windows API. MS doesn't like symmetry?

			// hOwner should be the previous frame but I am assuming the caller always has the input hWnd "follow the active frame" so
			// I'm not testing that. But, I will test to avoid calls if it is already owned by the new frame.
			if( hOwner != hNewFrame )
			{
				SetWindowLongPtr( hWnd, GWLP_HWNDPARENT, (LONG_PTR)hNewFrame );

				// For debugging. Catastrophic failure can occur ( crash, boom bang ) if this fails.
				HWND hNewOwner = ::GetWindow( hWnd, GW_OWNER );// There is a GetParent but not GetOwner.

				ASSERT( hNewOwner == hNewFrame );

				bSwappedOwners = true;
			}
		}
	}

	return bSwappedOwners;
}

// SwitchWindowOwnerToActiveFrame will first call SwitchWindowOwner. That call will "fail" if the window is a child window.
// If so, then the parent is set to the new frame.
bool SwitchWindowOwnerToActiveFrame( HWND hPreviousFrame, HWND hNewFrame, HWND hWnd )
{
	HWND hWndCurrentOwner = (HWND)::GetWindowLongPtr( hWnd, GWLP_HWNDPARENT );

	bool bRet = false;

	// Test hNewFrame just in case when Solid Edge starts we don't see any null input. Note that setting the owner to a null window
	// results in a window being owned by the desktop. That isn't bad - the Solid Edge frames are such windows. But, is that what
	// you want?
	if( hNewFrame )
	{
		bRet = SwitchWindowOwner( hWndCurrentOwner, hNewFrame, hWnd );
		if( false == bRet )
		{
			// Failure is not an option. This must be a child window.
		
			if( ::GetWindowLong( hWnd, GWL_STYLE ) & WS_CHILD )
			{
				HWND hWndParent = ::GetParent( hWnd );
				// Another sanity check. Who knows what windows are lurking around a complex application. Of course, you probably
				// only called this routine because you know what each window is and how it is supposed to act.
				if( hWndParent && hWndParent == hWndCurrentOwner )
				{
					::SetParent( hWnd, hNewFrame );

					bRet = true;
				}
			}
		}
	}

	return bRet;
}

STDMETHODIMP_(HRESULT __stdcall) CCommands::XApplicationFrameSwitchingEvents::raw_OnApplicationActiveFrameSwitching(ApplicationActiveFrameSwitchingEvent Context, long hWndPreviouslyActiveFrame, long hWndNewlyActiveFrame)
{
	HWND hWndPreviousFrame = (HWND)LongToHandle( hWndPreviouslyActiveFrame ); // There's that lack of symmetry again. HandleToLong works fine given an HWND!
	HWND hWndNewFrame = (HWND)LongToHandle( hWndNewlyActiveFrame );

	HWND hWndActiveFrame = NULL;
	HWND hWndMainFrame = NULL;

	// Well now, how do I create a UI window that is tied to the active frame? If I create a window and activate it (even via DoModal for a modal dialog), I would
	// probably need the active frame. And, Solid Edge is not about to change the meaning of the Application's "hWnd" API. That API will always return the main
	// frame's HWND. Ah, there is a new API to get the active frame's HWND. Let's give it a try. I can tell if the previous frame window is the main
	// frame or a floating frame by comparing the two. Not that the previous frame being "floating" or not is something I need to know. Or will I some day?
	try
	{
		// Just some code I use for debugging.
		hWndActiveFrame = (HWND)LongToHandle(GetApplicationPtr()->ActiveFramehWnd);
		hWndMainFrame = (HWND)LongToHandle(GetApplicationPtr()->hWnd);
	}
	catch( _com_error &e )
	{
		e;// soothe the compiler
	}

	// Get the list of windows that need to follow the frame and switch them to the newly activated frame before the user has a chance to set focus on one
	// and cause frame flopping.

	// TBD - Of course.
	return S_OK;
}


STDMETHODIMP_(HRESULT __stdcall) CCommands::XApplicationLicenseEvents::raw_OnApplicationLicense(enum ApplicationLicenseEvent Context, BSTR FeatureName)
{

	bool		bLicensed = false;
	try
	{
		bLicensed = false;
		// Just some code I use for debugging.
		////HINSTANCE hinstHandle = nullptr;
		////::MessageBox(NULL, "Test", "Test", MB_OK);
		////hinstHandle = (HINSTANCE)(GetApplicationPtr()->LicenseHandle);
		////
		////typedef HRESULT(*PROC_SEGWC)(LPCSTR, bool&);
		////PROC_SEGWC pfnSEGWC = NULL;

		////pfnSEGWC = (PROC_SEGWC)GetProcAddress(hinstHandle, "SELicensorIsFeatureValid");
		////if (NULL != pfnSEGWC)
		////{
		////	(pfnSEGWC)("solidedge", bLicensed);
		////}
	}
	catch (_com_error& e)
	{
		e;// soothe the compiler
	}

	// TBD - Of course.
	return S_OK;
}

STDMETHODIMP_(HRESULT __stdcall) CCommands::XApplicationDocumentLoadingEvents::raw_OnApplicationDocumentLoading(BSTR TopLevelFilename, ApplicationDocumentLoadingEvent Context, unsigned long waitLevel, VARIANT_BOOL* Cancel)
{
	*Cancel = VARIANT_FALSE;

	return S_OK;
}
