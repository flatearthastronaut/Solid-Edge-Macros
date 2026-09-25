#if !defined(AFX_EDGEBARPARENT_H__2F8F587E_776F_11D3_A3E3_0004AC969A5D__INCLUDED_)
#define AFX_EDGEBARPARENT_H__2F8F587E_776F_11D3_A3E3_0004AC969A5D__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
// EdgeBarParent.h : header file
//

/////////////////////////////////////////////////////////////////////////////
// CEdgeBarParent window

// This class can be used as the CWnd parent for a CDialog box that can then
// be displayed in the Solid Edge EdgeBar. Other than being a CWnd, this class
// initially exists in order to forward resize events to its child windows.

class CEdgeBarParent : public CWnd
{
// Construction
public:
	CEdgeBarParent();

// Attributes
public:

// Operations
public:

// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CEdgeBarParent)
	//}}AFX_VIRTUAL

// Implementation
public:
	virtual ~CEdgeBarParent();

	// Generated message map functions
protected:
	//{{AFX_MSG(CEdgeBarParent)
	afx_msg void OnSize(UINT nType, int cx, int cy);
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_EDGEBARPARENT_H__2F8F587E_776F_11D3_A3E3_0004AC969A5D__INCLUDED_)
