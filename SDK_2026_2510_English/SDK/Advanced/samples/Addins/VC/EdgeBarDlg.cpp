// ModelDlg.cpp : implementation file
//

#include "stdafx.h"
#include "EdgeBarDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// EdgeBarDlg dialog

EdgeBarDlg::EdgeBarDlg(UINT nIDTemplate, CWnd* pParent /*=NULL*/)
	: CDialog(nIDTemplate, pParent)
{
	//{{AFX_DATA_INIT(EdgeBarDlg)
	//}}AFX_DATA_INIT
}

EdgeBarDlg::~EdgeBarDlg()
{
}

int EdgeBarDlg::Init()
{
  // TODO: Add any needed args to this method and initialize any data that may
  //       needed when OnInitDialog is called.
  return 0;
}

void EdgeBarDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(ModelDlg)
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(EdgeBarDlg, CDialog)
	//{{AFX_MSG_MAP(EdgeBarDlg)
	ON_WM_SIZE()
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// EdgeBarDlg message handlers

#define Margin 10

void EdgeBarDlg::OnSize(UINT nType, int cx, int cy) 
{
	CDialog::OnSize(nType, cx, cy);
	
	// TODO: Add your message handler code here

	// I'll move the two buttons so if the user squeezes the docking pane,
	// the buttons will remain visible.

	CWnd* pOK = GetDlgItem( IDOK );
	CWnd* pCancel = GetDlgItem( IDCANCEL );

	if( pOK && pCancel )// should not be null ...
	{
		CRect rectOKButton;

		pOK->GetWindowRect(rectOKButton);

		ScreenToClient(rectOKButton);
		
		CRect rectCancelButton;

		pCancel->GetWindowRect(rectCancelButton);

		ScreenToClient(rectCancelButton);

		CRect rect;

		GetClientRect(rect);

		int x_OK = rect.right - Margin - rectOKButton.Width();
		if( x_OK < Margin )
		{
			x_OK = Margin;
		}
		
		int x_CANCEL = rect.right - Margin - rectCancelButton.Width();
		if( x_CANCEL < Margin )
		{
			x_CANCEL = Margin;
		}

		pOK->SetWindowPos(NULL, x_OK, rectOKButton.top, 0, 0, SWP_NOZORDER|SWP_NOACTIVATE|SWP_NOSIZE|SWP_NOOWNERZORDER|SWP_NOSENDCHANGING);
		
		pCancel->SetWindowPos(NULL, x_CANCEL, rectCancelButton.top, 0, 0, SWP_NOZORDER|SWP_NOACTIVATE|SWP_NOSIZE|SWP_NOOWNERZORDER|SWP_NOSENDCHANGING);
	}

  // You may need to move your controls around ...
}


BOOL DialogDroptarget::OnDrop(CWnd * pWnd, COleDataObject * pDataObject, DROPEFFECT dropEffect, CPoint point)
{
	return 0;
}

DROPEFFECT DialogDroptarget::OnDragEnter(CWnd * pWnd, COleDataObject * pDataObject, DWORD dwKeyState, CPoint point)
{
	return DROPEFFECT_COPY;

	// Calling base class results in DROPEFFECT_NONE.
	//return COleDropTarget::OnDragEnter(pWnd, pDataObject, dwKeyState, point);
}

DROPEFFECT DialogDroptarget::OnDragOver(CWnd * pWnd, COleDataObject * pDataObject, DWORD dwKeyState, CPoint point)
{
	return DROPEFFECT_COPY;
}


BOOL EdgeBarDlg::OnInitDialog()
{
	CDialog::OnInitDialog();
		
	m_DropInAnyTime.Register(this);

	// TODO:  Add extra initialization here

	return TRUE;  // return TRUE unless you set the focus to a control
				  // EXCEPTION: OCX Property Pages should return FALSE
}
