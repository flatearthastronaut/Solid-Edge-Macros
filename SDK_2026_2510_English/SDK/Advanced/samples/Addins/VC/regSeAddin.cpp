#include "stdafx.h"
#include "comcat.h"
#include "string.h"
#include "secatids.h"

// Notes:
//
//        pReqCategories may be NULL if and only if cReqCategories is zero.
//
//        pEnvironmentCategories may be NULL if and only if cEnvironmentCategories is zero.
//        If pEnvironmentCategories is NULL, ISEApplication is assumed by SE.
//
//        If szTopic is NULL, dwHelpContext is registered as the default help context for the addin, otherwise
//        szTopic is registered as the default topic to be used. These values along with the help filename are
//        used by the Solid Edge AddIn Manager if the user selects the addin from the list of addins and then
//        hits the addin help button. This means the addin does not have to be loaded and running in Solid Edge
//        for the AddIn Manager to invoke help for the addin.

STDAPI RegisterSEAddIn( 
					   CLSID AddInCLSID,              // Class ID of the relevant class
					   BOOL bAutoConnect,             // Auto connect indicator
					   LCID lcid,                     // Locale ID of addin's description.
					   TCHAR *szDescription,          // Locale specific addin description
					   TCHAR *szSummary,              // Locale specific addin summary
					   TCHAR *szHelpFilename,         // Name of help file
					   DWORD dwHelpContext,           // Default help context used by SE AddIn Manager
					   TCHAR *szHelpTopic,            // Default help topic used by SE AddIn Manager
					   ULONG cReqCategories,          // Number of required categories
					   CATID* pReqCategories,         // Array of cReqCategories required by the addin
					   ULONG cEnvironmentCategories,  // Number of SE environments addin supplies commands for
					   CATID* pEnvironmentCategories, // Array of SE environment categories
					   ULONG cPropertySets,           // Number of property sets addin might define
					   FMTID* pPropertySetFmtids )    // Array of property set identifiers
{
	// registers object, typelib and all interfaces in typelib

	ICatRegister *pCatReg = NULL;

	HRESULT hr;

	hr = CoCreateInstance( CLSID_StdComponentCategoriesMgr, NULL, CLSCTX_INPROC_SERVER, IID_ICatRegister,
		(LPVOID *)&pCatReg );

	// Register AddInCLSID.
	if( pCatReg )
	{
		// First register the component category for SE AddIns (not really necessary).
		CATEGORYINFO catinfo;

		catinfo.catid = CATID_SolidEdgeAddIn;
		catinfo.lcid = 0x409;
		wcscpy_s(catinfo.szDescription, L"Solid Edge Add-in");

		// Now register the AddInCLSID as implementing the ISolidEdgeAddIn interface
		CATID Kittens[1];
		Kittens[0] = CATID_SolidEdgeAddIn;

		hr = pCatReg->RegisterClassImplCategories( AddInCLSID, 1, Kittens );

		// Now register the interfaces AddInCLSID requires (from SE).
		if( 0 != cReqCategories )
		{
			hr = pCatReg->RegisterClassReqCategories( AddInCLSID, cReqCategories, pReqCategories );
		}

		// Build a string that represents the input clsid registry key, open that key and write the
		// remaining entries as values and subkeys.

		LPCTSTR SubKey = _T("CLSID");
		HKEY ClsidKey;
		LONG lrc;
		LPOLESTR lpClsidString;

		CString MyString = SubKey;

		StringFromCLSID( AddInCLSID, &lpClsidString );

		MyString += _T("\\");
		MyString += lpClsidString;

		lrc = RegOpenKeyEx( HKEY_CLASSES_ROOT, MyString, 0, KEY_SET_VALUE | KEY_CREATE_SUB_KEY, &ClsidKey );

		if( !lrc )
		{
			TCHAR lcidstring[10];

			_itot_s( lcid, lcidstring, sizeof( lcidstring )/sizeof( lcidstring[0] ), 16 );
			CString ValueName = lcidstring;

			DWORD cBytes = ((DWORD)_tcslen( szDescription )) * sizeof( TCHAR );

			lrc = RegSetValueEx( ClsidKey, ValueName, 0, REG_SZ, (LPBYTE)szDescription, cBytes );

			// Do summary now since the lcid is needed as the name of the value.
			if( szSummary )
			{
				CString szSummarySubKey = _T("Summary");
				HKEY hkSummaryKey;
				DWORD dwDisposition;

				lrc = RegCreateKeyEx( ClsidKey, szSummarySubKey, 0, REG_NONE, 0, KEY_SET_VALUE | KEY_CREATE_SUB_KEY, NULL, &hkSummaryKey, &dwDisposition );

				if( ERROR_SUCCESS == lrc )
				{
					DWORD cBytes = ((DWORD)_tcslen( szSummary )) * sizeof( TCHAR );

					lrc = RegSetValueEx( hkSummaryKey, ValueName, 0, REG_SZ, (LPBYTE)szSummary, cBytes );

					RegCloseKey( hkSummaryKey );
				}
			}

			if( szHelpFilename )
			{
				CString szHelpSubKey = _T("Help");
				HKEY hkHelpKey;
				DWORD dwDisposition;

				lrc = RegCreateKeyEx( ClsidKey, szHelpSubKey, 0, REG_NONE, 0, KEY_SET_VALUE | KEY_CREATE_SUB_KEY, NULL, &hkHelpKey, &dwDisposition );

				if( ERROR_SUCCESS == lrc )
				{
					DWORD cBytes = ((DWORD)_tcslen( szHelpFilename )) * sizeof( TCHAR );

					lrc = RegSetValueEx( hkHelpKey, ValueName, 0, REG_SZ, (LPBYTE)szHelpFilename, cBytes );

					if( NULL == szHelpTopic  )
					{
						CString strHelpContext = _T("Context");

						lrc = RegSetValueEx( hkHelpKey, strHelpContext, 0, REG_DWORD, (LPBYTE)&dwHelpContext, sizeof(DWORD) );
					}
					else
					{
						CString strHelpContext = _T("Topic");

						DWORD cBytes = ((DWORD)_tcslen( szHelpTopic )) * sizeof( TCHAR );

						lrc = RegSetValueEx( hkHelpKey, ValueName, 0, REG_SZ, (LPBYTE)szHelpTopic, cBytes );
					}
					RegCloseKey( hkHelpKey );
				}
			}

			ValueName = _T("AutoConnect");
			DWORD dwValue = bAutoConnect;
			cBytes = sizeof( dwValue );

			lrc = RegSetValueEx( ClsidKey, ValueName, 0, REG_DWORD, (LPBYTE)&dwValue, cBytes );

			CString szSubKey = _T("Environment Categories");
			HKEY hkEnvironmentsKey;
			DWORD dwDisposition;

			lrc = RegCreateKeyEx( ClsidKey, szSubKey, 0, REG_NONE, 0, KEY_CREATE_SUB_KEY, NULL, &hkEnvironmentsKey, &dwDisposition );

			if( ERROR_SUCCESS == lrc )
			{
				ULONG i;

				for( i = 0; i < cEnvironmentCategories; ++i )
				{
					LPOLESTR lpCatidString = NULL;

					StringFromCLSID( pEnvironmentCategories[i], &lpCatidString );

					if( NULL != lpCatidString )
					{
						CString EnvCatidKey(lpCatidString);

						HKEY hkEnvironmentsCatidKey;

						lrc = RegCreateKeyEx( hkEnvironmentsKey, EnvCatidKey, 0, REG_NONE, 0, 0, NULL, &hkEnvironmentsCatidKey, &dwDisposition );      

						if( ERROR_SUCCESS == lrc )
						{
							RegCloseKey( hkEnvironmentsCatidKey );
						}

						CoTaskMemFree( lpCatidString );
					}
				}

				RegCloseKey( hkEnvironmentsKey );
			}

			if( cPropertySets )
			{
				CString szSubKey = _T("Property Set Formats");
				HKEY hkPropertySetsKey;
				DWORD dwDisposition;

				lrc = RegCreateKeyEx( ClsidKey, szSubKey, 0, REG_NONE, 0, KEY_CREATE_SUB_KEY, NULL, &hkPropertySetsKey, &dwDisposition );

				if( ERROR_SUCCESS == lrc )
				{
					ULONG i;

					for( i = 0; i < cPropertySets; ++i )
					{
						LPOLESTR lpFmtidString = NULL;

						StringFromCLSID( pPropertySetFmtids[i], &lpFmtidString );

						if( lpFmtidString )
						{
							CString PropertySetKey(lpFmtidString);

							HKEY hkPropertySetFmtidKey;

							lrc = RegCreateKeyEx( hkPropertySetsKey, PropertySetKey, 0, REG_NONE, 0, 0, NULL, &hkPropertySetFmtidKey, &dwDisposition );      

							if( ERROR_SUCCESS == lrc )
							{
								RegCloseKey( hkPropertySetFmtidKey );
							}

							CoTaskMemFree( lpFmtidString );
						}
					}

					RegCloseKey(hkPropertySetsKey);
				}
			}

			RegCloseKey( ClsidKey );
		}

		pCatReg->Release();
		pCatReg = NULL;
	}

	return( hr );
}

STDAPI UnRegisterSEAddIn( 
						 CLSID AddInCLSID,              // Class ID of the relevant class
						 ULONG cReqCategories,          // Number of required categories
						 CATID* pReqCategories,         // Array of cReqCategories required by the addin
						 ULONG cEnvironmentCategories,  // Number of SE environments addin supplies commands for
						 CATID* pEnvironmentCategories, // Array of SE environment categories
						 ULONG cPropertySets,           // Number of property sets addin might define
						 FMTID* pPropertySetFmtids )    // Array of property set identifiers
{
	HRESULT hr = NOERROR;

	LPCTSTR SubKey = _T("CLSID");
	LONG lrc;
	LPOLESTR lpClsidString;
	LPOLESTR lpImplCatString;

	CString ClsidString = SubKey;
	CString RequiredCategoriesString;
	CString ImplementedCategoriesString;
	CString EnvironmentCategoriesString;
	CString PropertSetString;
	CString HelpString;
	CString SummaryString;

	StringFromCLSID( AddInCLSID, &lpClsidString );

	ClsidString += _T("\\");
	ClsidString += lpClsidString;

	RequiredCategoriesString = ClsidString;
	RequiredCategoriesString += _T("\\Requried Categories");

	for( ULONG i = 0; i < cReqCategories; ++i )
	{
		LPOLESTR lpCatidString;

		StringFromCLSID( pReqCategories[i], &lpCatidString );

		CString ReqCatidKeyString = RequiredCategoriesString;
		ReqCatidKeyString += _T("\\");
		ReqCatidKeyString += lpCatidString;

		lrc = RegDeleteKey( HKEY_CLASSES_ROOT, ReqCatidKeyString );
	}

	ImplementedCategoriesString = ClsidString;
	ImplementedCategoriesString += _T("\\Implemented Categories");

	StringFromCLSID( CATID_SolidEdgeAddIn, &lpImplCatString );

	CString ImplCatString = ImplementedCategoriesString;
	ImplCatString += _T("\\");
	ImplCatString += lpImplCatString;

	lrc = RegDeleteKey( HKEY_CLASSES_ROOT, ImplCatString );

	EnvironmentCategoriesString = ClsidString;
	EnvironmentCategoriesString += _T("\\Environment Categories");

	for( ULONG i = 0; i < cEnvironmentCategories; ++i )
	{
		LPOLESTR lpCatidString = NULL;

		StringFromCLSID( pEnvironmentCategories[i], &lpCatidString );

		if( NULL != lpCatidString )
		{
			CString EnvCatidKeyString = EnvironmentCategoriesString;
			EnvCatidKeyString += _T("\\");
			EnvCatidKeyString += lpCatidString;

			lrc = RegDeleteKey( HKEY_CLASSES_ROOT, EnvCatidKeyString );

			CoTaskMemFree( lpCatidString );
		}
	}

	PropertSetString = ClsidString;
	PropertSetString += _T("\\Property Set Formats");

	for( ULONG i = 0; i < cPropertySets; ++i )
	{
		LPOLESTR lpFmtidString = NULL;

		if( NULL != lpFmtidString )
		{
			StringFromCLSID( pPropertySetFmtids[i], &lpFmtidString );

			CString PropertySetFmtidKeyString = PropertSetString;
			PropertySetFmtidKeyString += _T("\\");
			PropertySetFmtidKeyString += lpFmtidString;

			lrc = RegDeleteKey( HKEY_CLASSES_ROOT, PropertySetFmtidKeyString );

			CoTaskMemFree( lpFmtidString );
		}
	}

	HelpString = ClsidString;
	HelpString += _T("\\Help");
	SummaryString = ClsidString;
	SummaryString += _T("\\Summary");

	lrc = RegDeleteKey( HKEY_CLASSES_ROOT, RequiredCategoriesString );
	lrc = RegDeleteKey( HKEY_CLASSES_ROOT, ImplementedCategoriesString );
	lrc = RegDeleteKey( HKEY_CLASSES_ROOT, EnvironmentCategoriesString );
	lrc = RegDeleteKey( HKEY_CLASSES_ROOT, ClsidString );
	lrc = RegDeleteKey( HKEY_CLASSES_ROOT, HelpString );
	lrc = RegDeleteKey( HKEY_CLASSES_ROOT, SummaryString );
	lrc = RegDeleteKey( HKEY_CLASSES_ROOT, ClsidString );

	return( hr );
}