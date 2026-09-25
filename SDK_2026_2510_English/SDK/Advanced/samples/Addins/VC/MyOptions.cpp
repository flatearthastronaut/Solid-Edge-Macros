// MyOptions.cpp : implementation file
//

#include "stdafx.h"
#include "MyOptions.h"


// MyOptions dialog

IMPLEMENT_DYNAMIC(MyOptions, CDialog)

MyOptions::MyOptions(CWnd* pParent /*=NULL*/)
	: CDialog(MyOptions::IDD, pParent)
{

}

MyOptions::~MyOptions()
{
}

void MyOptions::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
}


BEGIN_MESSAGE_MAP(MyOptions, CDialog)
	ON_BN_CLICKED(IDOK, &MyOptions::OnBnClickedOk)
END_MESSAGE_MAP()


// MyOptions message handlers

void MyOptions::OnBnClickedOk()
{
	// TODO: Add your control notification handler code here

	CEdit* pEdit = (CEdit*)GetDlgItem(IDC_EDIT1);
	if( pEdit )
	{
		pEdit->GetWindowText( m_strDoit );
	}
	pEdit = (CEdit*)GetDlgItem(IDC_EDIT2);
	if( pEdit )
	{
		pEdit->GetWindowText( m_strOptions );
	}

	OnOK();
}
