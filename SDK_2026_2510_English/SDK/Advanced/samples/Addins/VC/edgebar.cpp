#include "stdafx.h"
#include "resource.h"
#include "EdgeBar.h"

CEdgeBar::CEdgeBar()
{
	m_pEdgeBarDialogBox = NULL;
	m_pEdgeBarParent = NULL;
	m_pEdgeBarDoc = NULL;
}

CEdgeBar::~CEdgeBar()
{
	// This will delete the parent window.

	DeleteEdgeBar();
}

HRESULT CEdgeBar::CreateEdgeBar( ADDINDocument* pEdgeBarDoc )
{
	ASSERT( pEdgeBarDoc );

	HRESULT hr = NO_ERROR;

	ISolidEdgeBarPtr pBar = GetAddInPtr();

	if( NULL != pBar )
	{
		m_pEdgeBarDoc = pEdgeBarDoc;

		HWND hWndBarPage = 0;

		// First add a page to the EdgeBar. The nOptions arg is set to zero to indicate that
		// I want resize events. I want those events so I can resize or move controls.

		try
		{
			CString sEdgeBarTip;
			sEdgeBarTip.LoadString( IDS_EDGEBAR );
			_bstr_t bstrEdgeBarTip = sEdgeBarTip;

			ISolidEdgeBarExPtr pBarEx = pBar;
			if( NULL != pBarEx )
			{
				TCHAR ResourceFilename[MAX_PATH];

				GetModuleFileName( hMyInstance(), ResourceFilename, sizeof( ResourceFilename ) );

				// The Ex intf is available. See if SVG support is too.
				long nImageResource = IDB_EDGEBAR;
				if( EdgeVersionSupportsSVG() )
				{
					nImageResource = IDR_EDGEBAR_SVG;
				}

				// Pass in a direction value of 4 - "Up to Edge" to decide where the pane docks.
				hWndBarPage = (HWND)pBarEx->AddPageEx ( m_pEdgeBarDoc->GetDocument(), ResourceFilename, nImageResource, bstrEdgeBarTip, 4 );
			}
			else
			{
				// Pass in a direction value of 4 - "Up to Edge" to decide where the pane docks.
				hWndBarPage = (HWND)pBar->AddPage ( m_pEdgeBarDoc->GetDocument(), (long)hMyInstance(), IDB_EDGEBAR, bstrEdgeBarTip, 4 );
			}
		}
		catch( _com_error &e )
		{
			hr = e.Error();
		}

		if( hWndBarPage )
		{
			// Allocate a dialog using the template id of the resource dialog that is a child and
			// has no border.

			EdgeBarDlg* pEdgeBarDlg = new EdgeBarDlg();

			if( pEdgeBarDlg )
			{
				BOOL bRc = FALSE;

				// Seed the dialog with the data which is needed to initialize the dialog when its OnInitDialog method
				// is called.

				pEdgeBarDlg->Init();

				// Now since I'm using a CDialog box, I need a CWnd parent. The EdgeBar page's window handle
				// I obtained needs to be the parent of the CWnd parent of the CDialog. I want the CWnd parent
				// to have the same size as the EdgeBar page.

				// Get the rectangle from the EdgeBar page's window handle.

				RECT r;
				::GetWindowRect(hWndBarPage, &r);

				// Create the parent of the ModelView (CDialog derived) dialog.

				CEdgeBarParent *pParentCWnd = new CEdgeBarParent;

				if( pParentCWnd )
				{
					SetEdgeBarParent( pParentCWnd );

					// Simply create the window as a static child window. Make its initial size that of the
					// EdgeBar page I added.

					// Single "=" on purpose.
					if( bRc = pParentCWnd->CreateEx(0,_T("STATIC"), NULL, WS_VISIBLE | WS_CHILD,
													0, 0 , r.right - r.left, r.bottom - r.top,
													hWndBarPage, NULL, NULL) )
					{
						// Now create the ModelView dialog using the resource template id of the dialog that is a
						// child and has no border and whose parent I just created.

						// Single "=" on purpose.
						if( bRc = pEdgeBarDlg->Create( IDD_EDGEBARDIALOG, pParentCWnd ) )
						{
							// Set the position of the dialog to that of its parent.

							pEdgeBarDlg->MoveWindow(0, 0, r.right - r.left,r.bottom - r.top);

							// Let the show begin.

							SetEdgeBarDialogBox( pEdgeBarDlg );

							// Make my page the active (top) page in the EdgeBar. This is not something
							// a real add-in would normally do.
							try
							{
								hr = pBar->SetActivePage ( m_pEdgeBarDoc->GetDocument(), (long)hWndBarPage, 0 );
							}
							catch( _com_error &e )
							{
								// Eat this error. I should have a page but it just won't become active.
								HRESULT hr = e.Error(); 
							}

							// Does your edge bar dialog show up unattached to the edgebar/docking pane when the
							// call to ShowWindow is made? The dialog should be a child window and not a popup! 
							// And I never include a border ...
							pEdgeBarDlg->ShowWindow ( SW_SHOW );
						}
					}
				}

				if( FALSE == bRc )
				{
					// This will delete the dialog box.
					delete pEdgeBarDlg;

					pBar->RemovePage( m_pEdgeBarDoc->GetDocument(), (long)hWndBarPage, 0 );
				}
			}
		}
	}
	else
	{
		hr = E_FAIL;
	}

	return hr;
}

void CEdgeBar::SetEdgeBarDialogBox( CDialog *pEdgeBarDialogBox )
{ 
	if( m_pEdgeBarDialogBox )
	{ 
		BOOL brc = m_pEdgeBarDialogBox->DestroyWindow();
		delete m_pEdgeBarDialogBox;
	}

	m_pEdgeBarDialogBox = pEdgeBarDialogBox;
}

// Deletes the dialog box on the page, removes the page from the Solid Edge Edgebar
// and deletes the dialog box's parent.
void CEdgeBar::DeleteEdgeBar()
{ 
	SetEdgeBarDialogBox();

	if( m_pEdgeBarParent ) 
	{ 
		HWND hCurParentHwnd = GetParent( m_pEdgeBarParent->m_hWnd );

		if( hCurParentHwnd )
		{
			ISolidEdgeBarPtr pBar = GetAddInPtr();

			if( NULL != pBar )
			{
				try
				{
					pBar->RemovePage( m_pEdgeBarDoc->GetDocument(), (long)hCurParentHwnd, 0 );
				}
				catch( _com_error &e )
				{
					HRESULT hr = e.Error();
				}
			}
		}

		delete m_pEdgeBarParent;
		m_pEdgeBarParent = NULL;
	} 

	m_pEdgeBarParent = NULL;
}
