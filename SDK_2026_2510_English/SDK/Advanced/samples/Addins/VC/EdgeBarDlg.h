#if !defined(AFX_EDGEBARDLG_H__01B28EB6_3DE8_11D3_9278_00C04F79BE98__INCLUDED_)
#define AFX_EDGEBARDLG_H__01B28EB6_3DE8_11D3_9278_00C04F79BE98__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
// EdgeBarDlg.h : header file
//

/////////////////////////////////////////////////////////////////////////////
// ModelDlg dialog
#include "stdafx.h"
#include <afxcmn.h>
#include "resource.h"
#include <afxole.h>

class CEdgeBarParent;

class DialogDroptarget : public COleDropTarget
{
	virtual BOOL OnDrop(CWnd* pWnd, COleDataObject* pDataObject, DROPEFFECT dropEffect, CPoint point);
	virtual DROPEFFECT OnDragEnter(CWnd* pWnd, COleDataObject* pDataObject,	DWORD dwKeyState, CPoint point);
	virtual DROPEFFECT OnDragOver(CWnd* pWnd, COleDataObject* pDataObject,DWORD dwKeyState, CPoint point);
};

// This add-in has only one edge bar dialog. Sophisticated add-ins may, for
// example, have different dialogs base on the type of Solid Edge document.
class EdgeBarDlg : public CDialog
{
	// Construction
public:
	EdgeBarDlg(UINT nIDTemplate = IDD_EDGEBARDIALOG,CWnd* pParent = NULL);   // standard constructor
	~EdgeBarDlg();
	
	DialogDroptarget m_DropInAnyTime;

	// Dialog Data
	//{{AFX_DATA(ModelDlg)
	enum { IDD = IDD_EDGEBARDIALOG };
	//}}AFX_DATA


	// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(ModelDlg)
protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

	// Implementation
public:
	// TODO: If you need to initialize some data before OnInitDialog is called, add the
	//       appropriate arguments to the following Init method and call it after you
	//       create the dialog box.
	int Init();

protected:

	// Generated message map functions
	//{{AFX_MSG(EdgeBarDlg)
	afx_msg void OnSize(UINT nType, int cx, int cy);
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()

	// The parent will recieve OnSize notifications from Edge. I want to pass that
	// on to this dialog. In order to do so, the parent needs to be friendly with
	// the dialog (OnSize is protected).
	friend CEdgeBarParent;

public:
	virtual BOOL OnInitDialog();
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_EDGEBARDLG_H__01B28EB6_3DE8_11D3_9278_00C04F79BE98__INCLUDED_)
