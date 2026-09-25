#pragma once

#include "resource.h"
#include "afxwin.h"

// This add-in has one command that exercises the command bar API and this 
// is the options dialog that is shown when the user clicks the options
// button created when the command adds the standard solid edge header to the 
// command bar.

// MyOptions dialog - Trivial example. The dialog simply allows the user
// to change the text on the standard command bar header controls.

class MyOptions : public CDialog
{
	DECLARE_DYNAMIC(MyOptions)

public:
	MyOptions(CWnd* pParent = NULL);   // standard constructor
	virtual ~MyOptions();

// Dialog Data
	enum { IDD = IDD_DIALOG5 };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()
public:
	// Many Edge commands that work with the command bar have a standard
	// header (maintained by edge) that can contain an options button and/or
	// what edge internally calls a "doit" button. The "doit" button can be, for
	// example, a "Preview" button or a "Cancel" button. The semantics are
	// up to the command and commands modify the text based on the current
	// semantics. That is, while you run an edge command that uses the
	// command bar header, even though the text may change, the underlying
	// controls are constant while the text is supposed to give the user an
	// idea of the semantics.
	CString m_strDoit;
	CString m_strOptions;

	afx_msg void OnBnClickedOk();
};
