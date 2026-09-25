// AsmLoc.cpp : Implementation of DLL Exports.

// Note: The Wizard took his best shot at building a path to your
//       Solid Edge Sdk include file directory. If after adding
//       the above project settings, the project fails to build
//       due to missing include files, add the necessary include
//       paths to the c++ (preprocessor) project settings under
//       the addtional include file paths field.
//        


// Note: Proxy/Stub Information
//		To build a separate proxy/stub DLL, 
//		run nmake -f AsmLocps.mk in the project directory.

#include "stdafx.h"
#include <initguid.h>
#include "resource.h"
#include "secatids.h"
#include "AsmLoc.h"

#include "SEAddin.h"
#include "winnt.h"
#include "addins_i.c"

CComModule _Module;

BEGIN_OBJECT_MAP(ObjectMap)
	OBJECT_ENTRY(CLSID_SEAddIn, CSEAddIn)
END_OBJECT_MAP()

class CASMLOCApp : public CWinApp
{
public:
	virtual BOOL InitInstance();
	virtual int ExitInstance();
};

CASMLOCApp theApp;

BOOL CASMLOCApp::InitInstance()
{
	_Module.Init(ObjectMap, m_hInstance);
	return CWinApp::InitInstance();
}

STDAPI DllUnregisterServer(void);

int CASMLOCApp::ExitInstance()
{
	_Module.Term();
	return CWinApp::ExitInstance();
}

/////////////////////////////////////////////////////////////////////////////
// Used to determine whether the DLL can be unloaded by OLE

STDAPI DllCanUnloadNow(void)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return (_Module.GetLockCount()==0 && AfxDllCanUnloadNow()==S_OK) ? S_OK : S_FALSE;
}

/////////////////////////////////////////////////////////////////////////////
// Returns a class factory to create an object of the requested type

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv)
#pragma warning(suppress : 6387) // DMW 10/02/2010 Suppress spurious warning about DllGetClassObject - see http://connect.microsoft.com/VisualStudio/feedback/details/498862/c6387-warning-on-stock-dllgetclassobject-code-with-static-analyser
{
	return _Module.GetClassObject(rclsid, riid, ppv);
}

/////////////////////////////////////////////////////////////////////////////
// DllRegisterServer - Adds entries to the system registry
#include "comcat.h"

STDAPI DllRegisterServer(void)
{
	// registers object, typelib and all interfaces in typelib

	HRESULT hr;

	hr = _Module.RegisterServer(TRUE);

	// TODO: Register the correct locale.

	// Register CLSID_SEAddIn. This is a Solid Edge add-in with auto connection.
	BOOL bAutoConnect = 1;
	LCID lcid = 0x409;
	ULONG cReqCategories = 0;

	ULONG cEnvironmentCategories = 1;
	CATID EnvironmentCategories[1];

	// I choose to register this sample in all environments. Note that this means when
	// running in ST, where there are new document types (and hence envirnomentments), the
	// add-in is already primed to show up there (and any future document environments
	// that may come about). If your add-in only supports specific environments, enumerate
	// them explicitly by modifying the cEnvironmentCategories and EnvironmentCategories
	// variables (and of course populate the array).
	//
	// Also, I use CATID_SEALL as opposed to CATID_SEAllDocumentEnvrionments because this
	// sample puts up an edge bar page in the part environment and I want the page to show
	// up if the user's first opened document is part.
	EnvironmentCategories[0] = CATID_SEAll;

	TCHAR szDescription[255];
	TCHAR *pszDescription;

	int sts = ::LoadString( _Module.GetModuleInstance(), IDS_NAME, szDescription, 255 );
	if( sts )
	{
		pszDescription = szDescription;
	}
	else
	{
		pszDescription = _T("Nameless");
	}

	TCHAR szSummary[512];
	TCHAR *pszSummary;

	sts = ::LoadString( _Module.GetModuleInstance(), IDS_SUMMARY, szSummary, 512 );
	if( sts )
	{
		pszSummary = szSummary;
	}
	else
	{
		pszSummary = _T("Wizard generated Solid Edge AddIn");
	}

	TCHAR szHelpFilename[255];
	TCHAR *pszHelpFilename;

	sts = ::LoadString( _Module.GetModuleInstance(), IDS_HELPFILENAME, szHelpFilename, 255 );
	if( sts )
	{
		pszHelpFilename = szHelpFilename;
	}
	else
	{
		pszHelpFilename = NULL;
	}

	ULONG cPropertySets = 0;

	RegisterSEAddIn( CLSID_SEAddIn, bAutoConnect, lcid, pszDescription, pszSummary, pszHelpFilename, IntroContext, NULL,
					 cReqCategories, NULL, cEnvironmentCategories, EnvironmentCategories, cPropertySets, NULL );
	//RegisterSEAddIn( CLSID_SEAddIn, bAutoConnect, lcid, pszDescription, cReqCategories, NULL, cEnvironmentCategories, EnvironmentCategories );

	return( hr );
}

/////////////////////////////////////////////////////////////////////////////
// DllUnregisterServer - Removes entries from the system registry

STDAPI DllUnregisterServer(void)
{
	_Module.UnregisterServer();

	ULONG cReqCategories = 0;

	ULONG cEnvironmentCategories = 1;
	CATID EnvironmentCategories[1];

	EnvironmentCategories[0] = CATID_SEPart;

	ULONG cPropertySets = 0;

	UnRegisterSEAddIn( CLSID_SEAddIn, cReqCategories, NULL, cEnvironmentCategories, EnvironmentCategories, cPropertySets, NULL );

	return S_OK;
}

/////////////////////////////////////////////////////////////////////////////
// Debugging support

// GetLastErrorDescription is used in the implementation of the VERIFY_OK
//  macro, defined in stdafx.h.

#ifdef _DEBUG

void GetLastErrorDescription(CComBSTR& bstr)
{
	CComPtr<IErrorInfo> pErrorInfo;
	if (GetErrorInfo(0, &pErrorInfo) == S_OK)
		pErrorInfo->GetDescription(&bstr);
}

#endif //_DEBUG

