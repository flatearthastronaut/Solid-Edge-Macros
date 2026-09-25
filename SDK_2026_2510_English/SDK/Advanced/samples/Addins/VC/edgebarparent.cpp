// EdgeBarParent.cpp : implementation file
//

#include "stdafx.h"
#include "EdgeBarParent.h"
#include "EdgeBarDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CEdgeBarParent

CEdgeBarParent::CEdgeBarParent()
{
}

CEdgeBarParent::~CEdgeBarParent()
{
}


BEGIN_MESSAGE_MAP(CEdgeBarParent, CWnd)
	//{{AFX_MSG_MAP(CEdgeBarParent)
	ON_WM_SIZE()
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()


/////////////////////////////////////////////////////////////////////////////
// CEdgeBarParent message handlers

void CEdgeBarParent::OnSize(UINT nType, int cx, int cy) 
{
	CWnd::OnSize(nType, cx, cy);

	// TODO: Add your message handler code here

	CWnd *pChild = GetWindow( GW_CHILD );

	while( pChild )
	{
		pChild->MoveWindow(0, 0, cx, cy);

		// Give the dialog a shot at reorganizing its layout.
		if( pChild->IsKindOf( RUNTIME_CLASS( EdgeBarDlg ) ) )
		{
			EdgeBarDlg* pDialog = (EdgeBarDlg*)pChild;

			pDialog->OnSize( nType, cx, cy );
		}

		pChild = pChild->GetWindow( GW_HWNDNEXT );
	}

	// Yes the only child in this trivial add-in sample is the dialog
	// that was created.
}