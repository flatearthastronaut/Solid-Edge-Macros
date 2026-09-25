#include "stdafx.h"
#include "Commands.h"
#include "resource.h"
#include "edgebar.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

ADDINDocument::ADDINDocument()
{
	m_pDocument = NULL;
	m_pCommands = NULL;
	m_pEdgeBar = NULL;
	m_pDocumentEventsObj = NULL;
}

ADDINDocument::~ADDINDocument()
{
	// Destroy edge bar first since the edge bar object has a back pointer to this
	// document (it needs to obtain the SE document from this one to remove the
	// added page).
	DestroyEdgeBar();
	HRESULT hr = UnadviseFromEvents();
	C_RELEASE( m_pDocumentEventsObj );
}

// This function AddRefs the Solid Edge document.
HRESULT ADDINDocument::SetDocumentObject( 
	LPDISPATCH pDocumentDispatch
	,BOOL bAdviseEvents
	)
{
	ASSERT( pDocumentDispatch );

	HRESULT hr = NOERROR;

	m_pDocument = pDocumentDispatch;

	if( bAdviseEvents )
	{
		// Create document event handler
		hr = AdviseEvents();
	}

	return hr;
}

HRESULT ADDINDocument::AdviseEvents()
{
	ASSERT( NULL != m_pDocument );

	HRESULT hr = NOERROR;

	if( NULL == m_pDocumentEventsObj )
	{
		XDocumentEventsObj::CreateInstance(&m_pDocumentEventsObj);
		if( m_pDocumentEventsObj )
		{
			m_pDocumentEventsObj->AddRef();
		}
		else
		{
			hr = E_OUTOFMEMORY;
		}
	}

	if( m_pDocumentEventsObj )
	{
		VARIANT varResult;
		VariantInit(&varResult);
		DISPPARAMS disp = { NULL, NULL, 0, 0 };

		// Get the document events object and connect the event sink.
		hr = GetDocument()->Invoke( 0x47,
			IID_NULL,
			LOCALE_USER_DEFAULT,
			DISPATCH_PROPERTYGET,
			&disp,
			&varResult,
			NULL,
			NULL );

		if( SUCCEEDED( hr ) )
		{
			LPUNKNOWN pDocumentEventProp = (LPDISPATCH)(varResult.byref);
			hr = m_pDocumentEventsObj->Connect(pDocumentEventProp);

			C_RELEASE( pDocumentEventProp );
		}

		m_pDocumentEventsObj->m_pDocument = this;
	}

	return hr;
}

HRESULT ADDINDocument::UnadviseFromEvents()
{
	HRESULT hr = NOERROR;

	if( NULL != m_pDocumentEventsObj )
	{
		// Get the document events object and disconnect the event sink.
		VARIANT varResult;
		VariantInit(&varResult);
		DISPPARAMS disp = { NULL, NULL, 0, 0 };

		hr = GetDocument()->Invoke( 0x47,
			IID_NULL,
			LOCALE_USER_DEFAULT,
			DISPATCH_PROPERTYGET,
			&disp,
			&varResult,
			NULL,
			NULL );

		if( SUCCEEDED( hr ) )
		{
			LPUNKNOWN pDocumentEventProp = (LPDISPATCH)(varResult.byref);
			hr = m_pDocumentEventsObj->Disconnect(pDocumentEventProp);

			C_RELEASE(pDocumentEventProp);
		}
	}

	return hr;
}

HRESULT ADDINDocument::XDocumentEvents::raw_BeforeClose( void )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	// Edge bar page is removed when I destroy the addin "document". Now that I
	// am using the new edge bar page event set, the remove page event may have
	// already removed the page.

	HRESULT hr = S_OK;

	if( m_pDocument )// Something is wrong if this is NULL!
	{
		hr = m_pDocument->GetCommandsObject()->DestroyADDINDocument(m_pDocument->GetDocument());
	}

	return S_OK;
}

HRESULT ADDINDocument::XDocumentEvents::raw_BeforeSave( void )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT ADDINDocument::XDocumentEvents::raw_AfterSave( void )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}

HRESULT ADDINDocument::XDocumentEvents::raw_SelectSetChanged( LPDISPATCH pSelectSet )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return S_OK;
}


HRESULT ADDINDocument::CreateEdgeBar()
{
	HRESULT hr = NOERROR;

	if( m_pEdgeBar )
	{
		delete m_pEdgeBar;
		m_pEdgeBar = NULL;
	}

	m_pEdgeBar = new CEdgeBar;

	if( m_pEdgeBar )
	{
		m_pEdgeBar->CreateEdgeBar( this );
	}
	else
	{
		hr = E_OUTOFMEMORY;
	}

	return hr;
}

void ADDINDocument::DestroyEdgeBar()
{
	if( m_pEdgeBar )
	{
		delete m_pEdgeBar;
		m_pEdgeBar = NULL;
	}
}

HRESULT ADDINDocument::RemoveEdgeBarPage()
{
	HRESULT hr = NOERROR;

	if( m_pEdgeBar )
	{
		m_pEdgeBar->DeleteEdgeBar();
		DestroyEdgeBar();
	}

	return hr;
}

