#ifndef EDGEBAR_H
#define EDGEBAR_H

#include "commands.h"
#include "EdgeBarParent.h"
#include "EdgeBarDlg.h"

// The EdgeBar class defnintion. This class deals with the Solid Edge Edge bar interface.

class CEdgeBar
{
public:
	CEdgeBar();
	~CEdgeBar();

	// Method that creates the add-in's edge bar.
	HRESULT CreateEdgeBar( ADDINDocument* pedgebartstDoc );

	// Method to store the dialog. Passing in NULL will result in the current
	// window to be destroyed.
	void SetEdgeBarDialogBox( CDialog *pEdgeBarDialogBox = NULL );
	CDialog* GetEdgeBarDialogBox() { return m_pEdgeBarDialogBox; }

	// MFC wants a CWnd as the parent of a CDialog. When the add-in calls Edge to create
	// an edge bar entry, edge will create a window and give the handle to the window to
	// the add-in. I will use that window handle as the parent of the dialog. The
	// CEdgeBarParent object does have a trivial implementation of OnSize.
	void SetEdgeBarParent( CEdgeBarParent *pEdgeBarParent = NULL ) { m_pEdgeBarParent = pEdgeBarParent; }
	CEdgeBarParent* GetEdgeBarParent() { return m_pEdgeBarParent; }

	void DeleteEdgeBar( );

private:
	// Do not delete or addref. Back pointer only. CEdgeBar lifetime is
	// determined by this document.
	ADDINDocument* m_pEdgeBarDoc;

	// Declare the dialog pointer.
	CDialog *m_pEdgeBarDialogBox;

	// Declare the parent of the dialog.
	CEdgeBarParent *m_pEdgeBarParent;
};


#endif
