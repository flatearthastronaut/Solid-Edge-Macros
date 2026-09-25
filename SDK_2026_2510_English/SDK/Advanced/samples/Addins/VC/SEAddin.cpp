// SEAddin.cpp : Implementation of ISEAddin
#include "stdafx.h"
#include "AsmLoc.h"
#include "SEAddin.h"
#include "secatids.h"
#include "util.h"
#include <atlsafe.h>
#include <io.h>

/////////////////////////////////////////////////////////////////////////////
// CSEAddIn

#define C_RELEASE( x ) if( NULL != x ) { x->Release(); x = NULL; }
#define RETURN_ON_FAILURE( x ) if( FAILED(x) ) return x;

// Note: hMyInstance exists due to problems in certain versions of Solid Edge where the module state data's 
//       instance is set to that of Solid Edge with AfxSetResourceHandle calls. This happens, for instance, 
//       when Solid Edge gets focus after a some dialog box or window is dismissed. If that happens when 
//       the addin's module state data is set, the addin's instance handle is overwritten (yes that's a 
//       Solid Edge logic problem). If you experience resource problems, such as a control that doesn't
//       seem to exist on one of "your" dialog boxes or LoadString returns a string that is your string, 
//       call AfxSetResourceHandle(hMyInstance()) before accessing the resource. It doesn't hurt to call even 
//       if Solid Edge does not write its handle on top of the addin's. Most of these problems have been
//       cured if the version of Solid Edge is 7 or greater. If you find a case where that's not true,
//       please contact Solid Edge support where I'm sure I'll hear about it.

HINSTANCE s_hMyInstance;

HINSTANCE hMyInstance()
{
  return s_hMyInstance;
}

static CString s_TooltipFolder;

void SetMyTooltipsFolder()
{
	TCHAR ResourceFilename[MAX_PATH] = {0};

	GetModuleFileName( hMyInstance(), ResourceFilename, sizeof( ResourceFilename ) );

	CString strMod = ResourceFilename;

	CString strFolder;

	int index = strMod.ReverseFind('\\');
	if( -1 != index )
	{
		while( -1 != index )
		{
			strMod = strMod.Left( index );

			strFolder = strMod;
			strFolder += "\\tooltips";
			if( -1 != _access( strFolder, 0 ) )// ENOENT is never returned so ignore the MSDN. Stepped into the code and found either -1 or 0. Nothing else.
			{
				s_TooltipFolder = strFolder;
				s_TooltipFolder += "\\";
				break;
			}
			index = strMod.ReverseFind('\\');
		}
	}
}

void MyTooltipsFolder(CString &Folder)
{
	Folder = s_TooltipFolder;
}

bool EdgeVersionSupportsMultipleTabs( ApplicationPtr& pEdge )
{
	bool bRC = false;

	DWORD dwMajor = 0;
	DWORD dwMinor = 0;

	GetSolidEdgeVersion( dwMajor, dwMinor, pEdge );

	// The new add-in enhancements were added to ST2 but back-ported to ST MP7. So I
	// check to see if this is a version of Edge that supports the enhancements.
	if( dwMajor > 100 || (dwMajor == 100 && dwMinor >= 7) )
	{
		bRC = true;
	}

	return true;
}

bool EdgeVersionSupportsChangeUI( ApplicationPtr& pEdge )
{
	bool bRC = false;

	DWORD dwMajor = 0;
	DWORD dwMinor = 0;

	GetSolidEdgeVersion( dwMajor, dwMinor, pEdge );

	// The new add-in enhancements were added to ST9 but back-ported to ST8 MP6. So I
	// check to see if this is a version of Edge that supports the enhancements.
	if( dwMajor > 108 || (dwMajor == 108 && dwMinor >= 6) )
	{
		bRC = true;
	}

	return true;
}

bool EdgeVersionSupportsEnhancedTooltips( ApplicationPtr& pEdge )
{
	bool bRC = false;

	DWORD dwMajor = 0;
	DWORD dwMinor = 0;

	GetSolidEdgeVersion( dwMajor, dwMinor, pEdge );

	// The new add-in enhancements were added to ST9 but back-ported to ST8 MP6. So I
	// check to see if this is a version of Edge that supports the enhancements.
	if( dwMajor > 109 )
	{
		bRC = true;
	}

	return true;
}

bool EdgeVersionSupportsBackstage( ApplicationPtr& pEdge )
{
	bool bRC = false;

	DWORD dwMajor = 0;
	DWORD dwMinor = 0;

	GetSolidEdgeVersion( dwMajor, dwMinor, pEdge );

	// The new add-in enhancements were added to ST9 but back-ported to ST8 MP6. So I
	// check to see if this is a version of Edge that supports the enhancements.
	if( dwMajor > 219 ||  (dwMajor == 220 && dwMinor >= 7) )
	{
		bRC = true;
	}

	return true;
}

static bool s_bSupportsSVG = false;
bool EdgeVersionSupportsSVG()
{
	return s_bSupportsSVG;
}

bool EdgeVersionSupportsSVG( ApplicationPtr& pEdge )
{
	bool bRC = false;

	DWORD dwMajor = 0;
	DWORD dwMinor = 0;

	GetSolidEdgeVersion( dwMajor, dwMinor, pEdge );

	// The new add-in enhancements were added to ST9 but back-ported to ST8 MP6. So I
	// check to see if this is a version of Edge that supports the enhancements.
	if( dwMajor >= 225 )
	{
		bRC = true;
		s_bSupportsSVG = true;
	}

	return true;
}

LANGID GetSolidEdgeUserDefaultLangID( ApplicationPtr& pTheEdge )
{
	LANGID lang_id = GetUserDefaultLangID();
	if( pTheEdge )
	{
		try
		{
			_variant_t vLangID;

			pTheEdge->GetGlobalParameter( SolidEdgeFramework::seApplicationGlobalDefaultUserLangID, &vLangID );

			lang_id = (unsigned short )vLangID;
		}
		catch( _com_error &e )
		{
			// probably not a version that supports this constant so I'm ignoring this and returning the OS value.
			e;
		}
	}
	return lang_id;
}

static long s_LocTestCmdId = 0;
long GetLocTestCmdId() {return s_LocTestCmdId;}

static long s_LocTestShortCutMenuCmdId = 0;
long GetLocTestShortCutMenuCmdId() {return s_LocTestShortCutMenuCmdId;}

STDMETHODIMP CSEAddIn::raw_OnConnection( IDispatch *pAppDispatch, SeConnectMode ConnectMode, AddIn* pAddIn )
{
	AFX_MANAGE_STATE( AfxGetStaticModuleState() );
  
	s_hMyInstance = AfxGetResourceHandle();
	
	SetMyTooltipsFolder();

	HRESULT hr = S_OK;

	// Create commands
	CCommandsObj::CreateInstance(&m_pCommands);
	m_pCommands->AddRef();

	// The QueryInterface above AddRef'd the Application object.  It will
	//  be Release'd in CCommands' destructor.
	hr = m_pCommands->SetApplicationObject(pAppDispatch);

	// The QueryInterface above AddRef'd the AddIn object. It too will be released
	// in CCommands' destructor.
	hr = m_pCommands->SetAddInObject(pAddIn);

	if( SUCCEEDED(hr) )
	{
		//TODO: Set your gui version.

		// Set the gui version. If the version changed since the last time 
		// addin was connected, SE will purge any gui related to 
		// this it and force bFirstTime to VARIANT_TRUE when calling 
		// OnConnectToEnvironment.

		// Note: Starting with MP7 of ST, this add-in will created a different GUI. So I bump
		// the gui version so Edge will purge the old GUI (toolbar and accelerators - if the user assigned
		// accelerators). However if the user doesn't install MP7 but installs the new version of the
		// add-in, there will be an issue because the code below that puts up the new GUI will not
		// execute and instead the old GUI will be reinstalled. Later if the user applies MP7, the code
		// below will install the new GUI but the toolbars will not be built. This will force the user to
		// manually disconnect the add-in in each environment it runs in and then reconnect it. To avoid
		// the user having to manually help the add-in, the user should only install the new add-in AFTER 
		// upgrading to MP7 (or later). This is just a sample add-in. Hopefully a real add-in has a setup
		// and any update of the add-in would inform the user of this issue and fail to install until the
		// user applies MP7.
		m_pCommands->GetAddIn()->put_GuiVersion( 9 );
	}

	LANGID lang_id = GetSolidEdgeUserDefaultLangID( m_pCommands->GetApplicationPtr() );
	return( hr );
}

// Note: During initialization, environments connect to all addins BEFORE 
// loading any command bars. That way, if an addin has changed its gui 
// version, or is unavailable for some reason (uninstalled, refused to 
// connect because of a license problem), any command bar buttons it may 
// have previously added to SolidEdge can be purged when the user's command 
// bar data is loaded. OnConnectToEnvironment is NOT the place to use command 
// bar automation to manipulate Solid Ege toobars since they too have not 
// been read from the registry. The only toolbars and buttons that can be 
// manipulated using automation are those created in OnConnectToEnvironment 
// thefirst time the a new gui version of the addin is loaded. 
// If you need to manipulate Solid Edge bars or bars after they have been 
// loaded, connect to the application event set and listenfor app environment 
// transition events. Bars are loaded by the time that event is fired. 

#define NUMBER_OF_COMMANDS 2// Number of commands on the menu (V20 and earlier) or in a group (ST1 and later).

// This code demonstrates how to add user interface to Solid Edge. The code is a bit complicated as it tries
// to support any version of Solid Edge. This includes versions before Solid Edge ST. Since this sample
// is shipping with ST7 the only code really needed is the code that uses the new SetAddInInfoEx2 API created
// for ST7.

STDMETHODIMP CSEAddIn::raw_OnConnectToEnvironment( BSTR EnvironmentCatid, LPDISPATCH pEnvironment,
                                                   VARIANT_BOOL bFirstTime )
{
	HRESULT hr = S_OK;

	bool bEdgeSupportsMultipleMenus = EdgeVersionSupportsMultipleTabs( m_pCommands->GetApplicationPtr() );
	bool bEdgeSupportsEnhancedTips = EdgeVersionSupportsEnhancedTooltips( m_pCommands->GetApplicationPtr() );
	bool bEdgeSupportsBackstage = EdgeVersionSupportsBackstage( m_pCommands->GetApplicationPtr() );
	bool bEdgeSupportsSVG = EdgeVersionSupportsSVG( m_pCommands->GetApplicationPtr() );

	// Create string info for the primary category of this add-in (primary determine by first call to
	// SetAddinInfoEx.
	CString strCategoryStringCat1;
	CString strCmdStringsCat1[NUMBER_OF_COMMANDS];
	
	// I'm going to add the same commands in the first tab, frist group to the backstage.
	CString strBackstageCategoryString;
	CString strBackstageCategoryTabString;
	CString strCmdStringsBackstage[NUMBER_OF_COMMANDS];

	// Create the string info for the primary category to have a second group of commands. That is, the addin
	// will have its first tab contain two groups.
	CString strCmdStringsCat1Group2[NUMBER_OF_COMMANDS];

	// Create the string info for a command that will be added to the primary category and which will have
	// a great big alpha button.
	CString strCmdStringCat1BigImage;

	// Create string for backstage if the version of Edge supports it.
	CString strCmdStringBackstageBigImage;
	CString strCmdStringBackstageTabBigImage;
	CString strCmdStringBackstageTabCommand12;
	CString strCmdStringBackstageTabCommand13;

	// Create the string info for the add-ins second tab. Examine the IDS_NAME string in the string table of
	// the resource file and notice that "\n" is prepended to the string. The string is set as the description
	// of the add-in and Edge uses the "\n" as an indication that this add-in will get a top-level menu in
	// pre-ST versions of Edge and its own tab in ST and later. This also allows ST to create multiple tabs.
	CString strCategoryStringCat2;
	CString strCmdStringsCat2[NUMBER_OF_COMMANDS];

	// Create the strings for the three groups I will add. Two to the first tab and one on the second. This
	// is accomplished via the AddCommandBarButton API calls I make below.
	CString strCommandBarName1;
	CString strCommandBarName2;
	CString strCommandBarName3;

	HINSTANCE hInstance;

	long index = 0;

	// Have to scope off module state management because we are calling 
	// back into SolidEdge. Early versions of SolidEdge did not manage module 
	// state data so we have to be sure that any module state changes we 
	// do are undone before making any circular calls. The AFX_MANAGE_STATE macro
	// does all the magic while the scoping (embedding the code in a set of "{,}"
	// makes sure the macro goes out of scope and restores the module state at the
	// time I am done with obtaining resources. This was fixed a very long time ago
	// but perhaps there is a customer using an add-in on a very old version.
	{
		AFX_MANAGE_STATE( AfxGetStaticModuleState() );

		// The command name should not be localized to other languages. The 
		//  tooltip, command description, and status strings related to this
		//  command are stored in the string table and should be localized.

		// Tip: You can add a macro string and and a parameter argument to
		//      by appending them to the command string. For example if you
		//      add "\nNotepad.exe\nFile.txt", when that button is pushed,
		//      Solid Edge will run "Notepad.exe File.txt".

		hInstance = AfxGetInstanceHandle();	

		// I will set up all the data to create two tabs in ST MP7 and later. I only use the
		// data in that case. I will also set up the first tab to have two groups. Plus the
		// first tab will have a command that uses a great big alhpa button (see the PNG entries
		// in the resources.)
		
		// Speaking of the PNG entry, I was copying&pasteing an image from another
		// resource file and when I did a build I got this error:
		//
		// AsmLoc.rc(111) : error RC2170 : bitmap file res\AddIn_32.png is not in 3.00 format
		//
		// When I looked at the properties of the file on disk, I saw that it had just been
		// modified. But I did not modify it myself. I have been told by the creator of the PNG
		// file that this is a Visual Studio issue. I tried writing over the file on disk using
		// my original copy but still got the error. I finally deleted the PNG from the resource,
		// dropped the original back into the "res" folder and re-added it to the resource file.
		// That fixed the error. I have since been told that if I even have the PNG image opened
		// in the IDE and resources are saved, Visual Studio will rewrite the file using the
		// invalid format! Of course when I looked the error up in the MSDN, Microsoft says
		// the image needs to be redrawn or converted to the "3.x" format. So a word of warning,
		// once you import a PNG file, keep the original file around in case this happens to you
		// too!!

		// First create the non-localized (hopefully) unique command tags (identifiers). Failure to
		// use unique tags can result in the commands not being added. So I suggest an add-in use
		// something good, like the short guid of the add-in class id.
		LPCTSTR szCommandsCat1[NUMBER_OF_COMMANDS] = {_T("6D4144EA_AsmLocCommand1"), _T("6D4144EA_AsmLocCommand2")};
		LPCTSTR szCommandsCat1BigImage[1] = {_T("6D4144EA_AsmLocCommand5")};
		LPCTSTR szCommandsCat1Group2[NUMBER_OF_COMMANDS] = {_T("6D4144EA_AsmLocCommand6"), _T("6D4144EA_AsmLocCommand7")};
		
		// Prep for backstage commands. I'm putting three into the tab added to the backstage, including
		// one with a big alpha image.
		LPCTSTR szCommandsBackstageBigImage[1] = {_T("6D4144EA_AsmLocCommand11")};
		LPCTSTR szCommandsBackstageTabCommandsBigImage[1] = {_T("6D4144EA_AsmLocCommand12")};
		LPCTSTR szCommandsBackstageTabCommands2[2] = {_T("6D4144EA_AsmLocCommand13"),_T("6D4144EA_AsmLocCommand14")};

		LPCTSTR szCommandsCat2[NUMBER_OF_COMMANDS] = {_T("6D4144EA_AsmLocCommand3"), _T("6D4144EA_AsmLocCommand4")};

		UINT StringResourceIDsCat1[NUMBER_OF_COMMANDS] = {IDS_CMD_STRING1, IDS_CMD_STRING2};
		if( bEdgeSupportsEnhancedTips )
		{
			StringResourceIDsCat1[0] = IDS_STRING124;
		}
		
		UINT StringResourceIDsBackstage[NUMBER_OF_COMMANDS] = {IDS_BACKSTAGE_TAB_COMMAND13, IDS_BACKSTAGE_TAB_COMMAND14};
		if( bEdgeSupportsEnhancedTips )
		{
			StringResourceIDsBackstage[0] = IDS_BACKSTAGE_TAB_COMMAND_ENHANCED_TIP;
		}

		UINT StringResourceIDsCat1Group2[NUMBER_OF_COMMANDS] = {IDS_CMD_STRING6, IDS_CMD_STRING7};
		UINT StringResourceIDsCat2[NUMBER_OF_COMMANDS] = {IDS_CMD_STRING3, IDS_CMD_STRING4};

		// First I load the strings from the resource file and append them to the tags created above.
		strCategoryStringCat1.LoadString(IDS_CATEGORY_STRING1);

		CString strTipFolder; 
		MyTooltipsFolder(strTipFolder);

		CString strEnhancedTooltipText = strTipFolder;
		strEnhancedTooltipText += _T("commands_box.htm");

		// change the value of the static nAddNotepadOrURL to add code that ShellExecute can handle.
		static int nAddNotepadOrURL = 0;
		for( index = 0; index < NUMBER_OF_COMMANDS; ++index )
		{
			strCmdStringsCat1[index].LoadString(StringResourceIDsCat1[index]);
			strCmdStringsBackstage[index].LoadString(StringResourceIDsBackstage[index]);

			strCmdStringsCat1[index] = szCommandsCat1[index] + strCmdStringsCat1[index];
			strCmdStringsBackstage[index] = szCommandsBackstageTabCommands2[index] + strCmdStringsBackstage[index];

			if( bEdgeSupportsEnhancedTips && 0 == index )
			{
				// Couldn't just use Format method on string I passed into Format. So pull a double switch.
				CString withURL;

				withURL.Format( strCmdStringsCat1[index].GetBuffer(), strEnhancedTooltipText );
				strCmdStringsCat1[index] = withURL;

				withURL.Format( strCmdStringsBackstage[index].GetBuffer(), strEnhancedTooltipText );
				strCmdStringsBackstage[index] = withURL;
			}

			strCmdStringsCat1Group2[index].LoadString(StringResourceIDsCat1Group2[index]);
			strCmdStringsCat1Group2[index] = szCommandsCat1Group2[index] + strCmdStringsCat1Group2[index];
			if( nAddNotepadOrURL && 1 == index )
			{
				if( nAddNotepadOrURL == 1 )
				{
					// Launch notepad.
					CString strAppend = "\nNotepad.exe\nFile.txt";
					strCmdStringsCat1Group2[index] = strCmdStringsCat1Group2[index] + strAppend;
				}
				else
				{
					// Launch registered browser and go to a web site.
					CString strAppend = "\nwww.solidedge.com";
					strCmdStringsCat1Group2[index] = strCmdStringsCat1Group2[index] + strAppend;
				}
			}
		}

		// Load the command with a big button's data.
		strCmdStringCat1BigImage.LoadString( bEdgeSupportsEnhancedTips ? IDS_STRING123 : IDS_CMD_STRING5 );
		if( bEdgeSupportsEnhancedTips )
		{
			CString strEnhancedTooltipText = strTipFolder;
			strEnhancedTooltipText += _T("hammer.htm");
			// Couldn't just use Format method on string I passed into Format. So pull a double switch.
			CString withURL;

			withURL.Format( strCmdStringCat1BigImage.GetBuffer(), strEnhancedTooltipText );
			strCmdStringCat1BigImage = withURL;
		}

		strCmdStringBackstageBigImage = strCmdStringCat1BigImage;// just copying for BS
		strCmdStringBackstageTabBigImage = strCmdStringCat1BigImage;// just copying for BS

		strCmdStringCat1BigImage = szCommandsCat1BigImage[0] + strCmdStringCat1BigImage;

		strCmdStringBackstageBigImage = szCommandsBackstageBigImage[0] + strCmdStringBackstageBigImage;
		strCmdStringBackstageTabBigImage = szCommandsBackstageTabCommandsBigImage[0] + strCmdStringBackstageTabBigImage;

		strCmdStringBackstageTabCommand12 = szCommandsBackstageTabCommands2[0] + strCmdStringBackstageTabCommand12;
		strCmdStringBackstageTabCommand13 = szCommandsBackstageTabCommands2[1] + strCmdStringBackstageTabCommand13;

		// Now I load the string resources for the commands on the second tab.
		strCategoryStringCat2.LoadString(IDS_CATEGORY_STRING2);
		for( index = 0; index < NUMBER_OF_COMMANDS; ++index )
		{
			strCmdStringsCat2[index].LoadString(StringResourceIDsCat2[index]);
			strCmdStringsCat2[index] = szCommandsCat2[index] + strCmdStringsCat2[index];
		}

		strBackstageCategoryString.LoadString(IDS_BACKSTAGE_CATEGORY);// NEVER LOCALIZE!
		strBackstageCategoryTabString.LoadString(IDS_BACKSTAGE_CATEGORY_TAB);// NEVER LOCALIZE!
																			 // Now load the command bar names. Examine the entries in the string table. Note the strings
		// have a "\n" in them to create two parts. Also note the part of the string preceding the "\n" are identical
		// to the category strings. That lets ST7 and later determine how to factor the user interface into groups
		// and which groups go on which tabs.
		strCommandBarName1.LoadString(IDS_COMMANDBAR_NAME1);
		strCommandBarName2.LoadString(IDS_COMMANDBAR_NAME2);
		strCommandBarName3.LoadString(IDS_COMMANDBAR_NAME3);
	}

	// I will add two commands to versions of Solid Edge prior to ST1 (version 20 and earlier). Prior to ST1 an add-in could only call
	// SetAddInInfo (or SetAddInInfoEx when 64 bit support came about) one time and that call resulted in a menu structure being created by Solid Edge. 
	// Tool bars were supported then and calls could be made to create multiple tool bars out of the commands added. With ST1 Solid Edge started 
	// creating groups out of any tool bars created - one group per tool bar since tool bars are no longer part of the user interface. 
	// This allowed for a decent factorization of the add-in's user interface for presentation to the user. Note that a tool bar is also known as
	// a command bar in the API and the buttons are command bar buttons.
	
	// Starting with ST1, multiple calls can be made to SetAddInInfo/SetAddInInfoEx. Each call with a unique category name will cause on group of
	// controls to appear on the ribbon. If the addin specifies that it wants its own tabs, the group appears on a tab that uses the category name.
	// The group uses the same name. If the addin doesn't use its own tabs, the group or groups appear on the "AddIns" tab. To create multiple groups
	// on its own tab, is a bit difficult but it can be done. The tool bars are the key (a.k.a. "command bars" with "command bar buttons" in the API).
	// With ST the command bar name can have two parts, again separated by a new line character. The first part is used to identify the category/tab
	// the on which the group should be created. The second part is the group name. To avoid duplication Solid Edge will replace or remove controls
	// added to the "default" group created when SetAddInInfoEx was called.

	// Starting with ST7, yet another interface was added, ISEAddInEx2 with a SetAddInInfoEx2 interface. This interface has two new features. The
	// category name passed in can be used to directly create both a tab AND a group. And the new API takes in an array of control styles for the
	// controls added to the user interface. Previously one had to use command bar button styles to effect styles such as displaying a control with
	// an icon and text, or displaying a large control with text under the icon or perhaps to create a check box button control or radio button control.
	// That is, with ST7 and later, the command bar button APIs are even less useful and hopefully not even needed. Now let's see how this all comes
	// together to create a user interface.
	
	// For ST1 and later, I show how to add two tabs. I will use the newer SetAddInInfoEx2 for ST7 and later and SetAddInInfoEx for ST1 thru ST6.
	// The first tab will have two groups and the second tab will have one group.

	// A note on command IDs. Use the input IDs in the command event handlers. The ID you pass to SetAddInInfo is the ID passed to the event handlers.
	// Edge will NOT use the input command IDs for the controls created and any window command messages as such IDs need to be unique across the user
	// interface. Hence Solid Edge will generate a unique ID for each input ID and return those IDs added to the UI in the array of IDs (it is both input
	// and output). Use the returned IDs, for example, in calls to the application's StartCommand API or in response to shortcut menu events. Since Solid
	// Edge doesn't know the origin of such calls, and those calls result in Windows posting WM_COMMAND messages, the returned IDs must be used in such
	// calls. However when the WM_COMMAND event is eventually processed, Solid Edge will be able to map the ID in the event to the addin command ID and
	// still invoke the OnCommand event handlers with the addin supplied ID. The order addins load and the fact that macros added by a user also get 
	// assigned a dynamic ID means the IDs returned are only good for the session they are created in.

	CComSafeArray<BSTR> saTab1Group1CommandStrings;// For OLE automation, strings are passed as BSTRs in a safe array.
	saTab1Group1CommandStrings.Create();
	CComSafeArray<long> saTab1Group1CommandIDs;// For OLE automation, command IDs are passed in a safe array.
	saTab1Group1CommandIDs.Create();
	CComSafeArray<long> saTab1Group1CommandButtonStyles;// For OLE automation, command button styles are passed in a safe array.
	saTab1Group1CommandButtonStyles.Create();

	for( int index = 0; index < NUMBER_OF_COMMANDS; ++index )
	{
		saTab1Group1CommandStrings.Add( strCmdStringsCat1[index].AllocSysString() );
		saTab1Group1CommandIDs.Add( index );
		saTab1Group1CommandButtonStyles.Add( seButtonIconAndCaption );
	}

	// I also want to add a big button with text underneath it like other built-in command in Solid Edge do. I add it to the first group
	// in the first tab created. Hence I could have added the data to the safe arrays above. Then in ST7 or later I would be done. For
	// ST1 and before ST7 I have to use a command bar button to set the style to get the effect I desire. So why make two calls to
	// SetAddInInfoEx(2)? Just to show that it is possible to do so. Some addins have split personalities. Either the user can configure
	// the addin independent of Solid Edge or the addin may perform a license check and add additional functionality depending on the
	// license. Such addins might want to take advantage of the ability to add sets of commands to the UI in separate calls.

	// Declare the tab data
	CComSafeArray<BSTR> saTab1Group1MrBigCommandString;// For OLE automation, strings are passed as BSTRs in a safe array.
	hr = saTab1Group1MrBigCommandString.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saTab1Group1MrBigCommandID;// For OLE automation, command IDs are passed in a safe array.
	hr = saTab1Group1MrBigCommandID.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saTab1Group1MrBigCommandButtonStyle;// For OLE automation, command button styles are passed in a safe array.
	hr = saTab1Group1MrBigCommandButtonStyle.Create();
	RETURN_ON_FAILURE(hr)

	// I want to add to the backstage if this version supports that.
	// Declare the tab data.
	CComSafeArray<BSTR> saBackstageCommandString;// For OLE automation, strings are passed as BSTRs in a safe array.
	hr = saBackstageCommandString.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saBackstageCommandID;// For OLE automation, command IDs are passed in a safe array.
	hr = saBackstageCommandID.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saBackstageCommandButtonStyle;// For OLE automation, command button styles are passed in a safe array.
	hr = saBackstageCommandButtonStyle.Create();
	RETURN_ON_FAILURE(hr)

	CComSafeArray<BSTR> saBackstageTabCommandString;// For OLE automation, strings are passed as BSTRs in a safe array.
	hr = saBackstageTabCommandString.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saBackstageTabCommandID;// For OLE automation, command IDs are passed in a safe array.
	hr = saBackstageTabCommandID.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saBackstageTabCommandButtonStyle;// For OLE automation, command button styles are passed in a safe array.
	hr = saBackstageTabCommandButtonStyle.Create();
	RETURN_ON_FAILURE(hr)

	CComSafeArray<BSTR> saBackstageTabCommand2String;// For OLE automation, strings are passed as BSTRs in a safe array.
	hr = saBackstageTabCommand2String.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saBackstageTabCommand2ID;// For OLE automation, command IDs are passed in a safe array.
	hr = saBackstageTabCommand2ID.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saBackstageTabCommand2ButtonStyle;// For OLE automation, command button styles are passed in a safe array.
	hr = saBackstageTabCommand2ButtonStyle.Create();
	RETURN_ON_FAILURE(hr)

	// Now fill in some data. This is for the first tab and first group on that tab.
	saTab1Group1MrBigCommandString.Add( strCmdStringCat1BigImage.AllocSysString() );
	saTab1Group1MrBigCommandID.Add( NUMBER_OF_COMMANDS + 3 );
	saTab1Group1MrBigCommandButtonStyle.Add( seButtonIconAndCaptionBelow );// seButtonIconAndCaptionBelow is the way to get a big button.
	
	// Fill in the backstage data.
	saBackstageCommandString.Add( strCmdStringBackstageBigImage.AllocSysString() );
	saBackstageCommandID.Add( 11 );
	saBackstageCommandButtonStyle.Add( seButtonIconAndCaptionBelow );// seButtonIconAndCaptionBelow is the way to get a big button.

	saBackstageTabCommandString.Add( strCmdStringBackstageTabBigImage.AllocSysString() );
	saBackstageTabCommandID.Add( 12 );
	saBackstageTabCommandButtonStyle.Add( seButtonIconAndCaptionBelow );// seButtonIconAndCaptionBelow is the way to get a big button.

	for( int index = 0; index < NUMBER_OF_COMMANDS; ++index )
	{
		saBackstageTabCommand2String.Add( strCmdStringsBackstage[index].AllocSysString(), FALSE );
		saBackstageTabCommand2ID.Add( index + 13 );
		saBackstageTabCommand2ButtonStyle.Add( seButtonIconAndCaption );
	}

	// Now set up a second group on the first tab.
	CComSafeArray<BSTR> saTab1Group2CommandStrings;// For OLE automation, strings are passed as BSTRs in a safe array.
	hr = saTab1Group2CommandStrings.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saTab1Group2CommandIDs;// For OLE automation, command IDs are passed in a safe array.
	hr = saTab1Group2CommandIDs.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saTab1Group2CommandButtonStyles;// For OLE automation, command button styles are passed in a safe array.
	hr = saTab1Group2CommandButtonStyles.Create();
	RETURN_ON_FAILURE(hr)

	for( int index = 0; index < NUMBER_OF_COMMANDS; ++index )
	{
		saTab1Group2CommandStrings.Add( strCmdStringsCat1Group2[index].AllocSysString() );
		saTab1Group2CommandIDs.Add( index + 6 );
		saTab1Group2CommandButtonStyles.Add( seButtonIconAndCaption );
	}

	// Finally I add a second tab with a single group. This group will set up a radio and check box.
	CComSafeArray<BSTR> saTab2Group1CommandStrings;// For OLE automation, strings are passed as BSTRs in a safe array.
	hr = saTab2Group1CommandStrings.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saTab2Group1CommandIDs;// For OLE automation, command IDs are passed in a safe array.
	hr = saTab2Group1CommandIDs.Create();
	RETURN_ON_FAILURE(hr)
	CComSafeArray<long> saTab2Group1CommandButtonStyles;// For OLE automation, command button styles are passed in a safe array.
	hr = saTab2Group1CommandButtonStyles.Create();
	RETURN_ON_FAILURE(hr)

	for( int index = 0; index < NUMBER_OF_COMMANDS; ++index )
	{
		saTab2Group1CommandStrings.Add( strCmdStringsCat2[index].AllocSysString() );
		saTab2Group1CommandIDs.Add( index + 2 );// command ids are 2,3
		saTab2Group1CommandButtonStyles.Add( index == 0 ? seRadioButton : seCheckButtonAndIcon );
	}

	_bstr_t bszCategoryName1(strCategoryStringCat1);
	_bstr_t bszCategoryName2(strCategoryStringCat2);
	_bstr_t bszBackstageCategoryName(strBackstageCategoryString);

	// Now push the data into the safe arrays.
	if( SUCCEEDED(hr) )
	{
		// Call SetAddInInfo. This allows SolidEdge to present commands as one of the
		// categories in its toolbar customization dialog box. It also allows any buttons
		// that may exist on persisted tool bars to reconnect to the correct command when
		// the tool bars are read in. Of course in ST, there are no tool bars but the categories
		// are still available in customization (users can add commands to the "quick access toolbar"
		// at the top of the command ribbon.

		// For legacy add-ins (pre-ST), toolbar bitmaps are 24 pixels wide by 23 pixels high 
		// for large buttons and 16 pixels wide by 15 pixels for medium buttons. The background
		// color is (192,192,192). The glyph id passed in identifies the glyphs in the resource 
		// file whose instance handle is hInstance. The number of commands for a legacy add-in
		// does not have to correspond with the width of the glyph. For example, a glyph for a 
		// legacy add-in that passes in two commands can be 48X15 and edge will handle this.
		// For non-legacy add-ins, the glyph size should correspond to the number of commands.
		
		// For ST, only the "medium" (what ever happened to small and large) image is used and ST 
		// really wants a 16X16 image. No big deal if I use the older sizes as Edge will make the 
		// minor adjustment. 
	
		// Starting with ST2 (a.k.a V102), Edge will choose the medium or large image based
		// on the machine's DPI setting. The large image size should be 20X20. But if your add-in
		// wants to run in pre-ST Edge, the 24X23 image will usually appear just fine. The caller 
		// can pass in -1 for any image not provided and Edge will use the image that is passed in 
		// regardless of the DPI setting of the machine and regardless of just how crappy an image 
		// may look after being resized by the UI system.

		// Also, starting with ST MP7, the new capabilities for add-ins to create a UI have been
		// back ported. Besides the above, an add-in can add large images such as those seen for
		// many edge commands. The image size is 32X32 (medium) and (40X40) for large images.

		// One other note concerning the background color of the glyph and how edge tries to 
		// determine if the add-in is a legacy add-in. If the add-in is using the new capabilities 
		// by calling SetAddInInfo multiple times, edge assumes the first
		// pixel of the glyph is the background color of the image (except for PNG
		// images, which don't have a transparent color since they have an alpha plane). If the
		// add-in only makes one call to SetAddInInfo, Edge has an algorithm that is executed to try 
		// and determine if the add-in is a legacy add-in. If your add-in image height is 15 (medium)
		// or 23 (large) with 4 bits per pixel, then edge assumes the add-in is a 
		// "legacy add-in" and will continue to assume the background is (192,192,192). If you have
		// problems with the image transparency, try editing the glyph so that the first pixel of
		// each image is (192,192,192). For instance, I have a sample add-in from an ISV that has no
		// transparent pixels (the background is all red). When passing them to the new UI system,
		// all the red was ignored and the image looked really bad. Setting the first pixel to the
		// background color would fix that issue. Luckily the image was 4 bpp and the new test by
		// edge took care of the problem because edge changes the first pixel to the background
		// color automatically (yes you give up one pixel, which you can see if you zoom in on such
		// an image. But if you are not so lucky (say you don't use 4 bpp), a bit of editing of the image
		// can make a big difference.

		// Edge can be forgiving when it comes to the image height. So an add-in can ignore the
		// early edge image heights of 15 (23) pixels and pass in an image that is 16 (24) pixels
		// high. Not sure where that 15 (23) size came from. I believe it was related to an Office
		// standard.

		// How do I use the newer "Ex" interface? See if it is present. If not, then this is
		// definitely not ST. If it is present, I still don't know if I have ST and in particular
		// a version of ST that supports the new UI enhancements.
		ISEAddInEx2Ptr pAddInEx2 = m_pCommands->GetAddIn();
		ISEAddInExPtr pAddInEx = m_pCommands->GetAddIn();
		if( NULL != pAddInEx )
		{
			// Why does the ISEAddInEx interface exist? Primarily to allow an add-in to separate its
			// resource module from the code module (I don't do that here as the resources are embedded
			// in the DLL). Why do that? Because resources may need to be internationalized and who wants
			// to do that again if there is a bug in the add-in and only code needs to be redelivered?
			// There is another reason too. If the separate resource module has an entry point, its
			// "bitness", must match edge. That is, 32 bit edge and 64 bit edge can only load 32 bit and
			// 64 bit modules respectively. Well almost. Windows has a method to load resource modules as
			// data only (See LoadLibraryEx and LOAD_LIBRARY_AS_DATAFILE). Visual Studio allows for one to
			// build a resource/data only module by specifying via the target's properties that the module
			// has no entry point. In any case, the Ex interface takes in a module filename and Edge will
			// load the lib for the add-in (as a data file). So entry point or not, edge will do the right
			// thing if I use this newer Ex interfaces.

			TCHAR ResourceFilename[MAX_PATH];

			GetModuleFileName( hInstance, ResourceFilename, sizeof( ResourceFilename ) );

			// Edge really only uses the "medium" image size starting with ST. Starting with ST, the size should be 16X16 or
			// scaling will occur. If the size is off by much, the resizing is not always pretty. -1 can
			// be sent in for any of the image resource IDs that are no longer used.

			// Set up the primary (first) category of the add-in. Since my description has "\n" prepended, this
			// results in a tab dedicated to this add-in. Note edge stopped support monochrome a number of versions
			// ago. But I'll still pass 'em in in case the addin is running in a very old version.
			if( NULL != pAddInEx2 )
			{
				bszCategoryName1 = strCommandBarName1;// This string has the form "Tab1Name\nGroup1Name".

				// Notice that as of Solid Edge 2025, SVG images that can be rendered by the Microsoft Direct2D system are
				// supported by Solid Edge. Currently there are limitations with Direct2D SVG rendering. In particular the image
				// should have a viewBox and NOT a Width and Height attribute. Using a viewBox allows the SVG image to be scaled properly
				// when the user's desktop DPI is > 100%. Also, a single SVG can be used for both small and large images on the ribbon.

				VERIFY_OK(pAddInEx2->SetAddInInfoEx2(ResourceFilename, EnvironmentCatid,
					bszCategoryName1, bEdgeSupportsSVG ? IDR_TOOLBAR_LARGE_SVG : IDR_TOOLBAR_MEDIUM, bEdgeSupportsSVG ? IDR_TOOLBAR_LARGE_SVG : IDR_TOOLBAR_LARGE, IDR_TOOLBAR_MEDIUMMONOCHROME,
					IDR_TOOLBAR_LARGEMONOCHROME, NUMBER_OF_COMMANDS, saTab1Group1CommandStrings.GetSafeArrayPtr(),
					saTab1Group1CommandIDs.GetSafeArrayPtr(), saTab1Group1CommandButtonStyles.GetSafeArrayPtr()));

				// I have added code to demonstrate shortcut menu events. So store a command that I will add when the event is fired.
				s_LocTestShortCutMenuCmdId = saTab1Group1CommandIDs.GetAt(0);
			}
			else
			{
				VERIFY_OK(pAddInEx->SetAddInInfoEx(ResourceFilename, EnvironmentCatid,
					bszCategoryName1, IDR_TOOLBAR_MEDIUM, IDR_TOOLBAR_LARGE, IDR_TOOLBAR_MEDIUMMONOCHROME,
					IDR_TOOLBAR_LARGEMONOCHROME, NUMBER_OF_COMMANDS, saTab1Group1CommandStrings.GetSafeArrayPtr(), saTab1Group1CommandIDs.GetSafeArrayPtr()));
			}
			
			// TIP: Examine the safe array of command IDs after the call to SetAddInInfoEx. They have changed!
			// Edge will call the command events (in commands.cpp) passing in the command IDs I passed into
			// SetAddInInfoEx (or AddCommand). But Edge can have more than one add-in and a command ID in the app
			// has to be unique across not just all add-ins but the entire application's set of commands. So Edge assigns
			// a unique ID for each command I add and the assigned IDs are returned via the safe array. I don't
			// use them in this sample but if for instance I ever wanted to call the application's StartCommand API to
			// start one of this add-in's commands, I would need the returned IDs to do so.

			if( bEdgeSupportsMultipleMenus )
			{
				// Cool. Edge supports multiple tabs and multiple groups per tab. Plus I can "add" data to an
				// existing category. I can't just rely on the interface being available as this capability was
				// added to some versions of Solid Edge via service pack updates.

				// First add some more commands to the primary category. Once I add command bar buttons below
				// for these two commands, they will magically move from the primary group (each tab has at least
				// one group regardless of how many command bars are converted to groups) to the group specified
				// by the command bar. Note I just reuse the image from the first call.
				if( NULL != pAddInEx2 )
				{
					bszCategoryName1 = strCommandBarName3;

					// Notice that as of Solid Edge 2025, SVG images that can be rendered by the Microsoft Direct2D system are
					// supported by Solid Edge. Currently there are limitations with Direct2D SVG rendering. In particular the image
					// should have a viewBox and NOT a Width and Height attribute. Using a viewBox allows the SVG image to be scaled properly
					// when the user's desktop DPI is > 100%. Also, a single SVG can be used for both small and large images on the ribbon.

					VERIFY_OK(pAddInEx2->SetAddInInfoEx2(ResourceFilename, EnvironmentCatid,
						bszCategoryName1, bEdgeSupportsSVG ? IDR_TOOLBAR_LARGE_SVG : IDR_TOOLBAR_MEDIUM, bEdgeSupportsSVG ? IDR_TOOLBAR_LARGE_SVG : IDR_TOOLBAR_LARGE, IDR_TOOLBAR_MEDIUMMONOCHROME,
						IDR_TOOLBAR_LARGEMONOCHROME, NUMBER_OF_COMMANDS, saTab1Group2CommandStrings.GetSafeArrayPtr(),
						saTab1Group2CommandIDs.GetSafeArrayPtr(), saTab1Group2CommandButtonStyles.GetSafeArrayPtr()));

					// And I can add yet more to the primary category. In this case, the command added will have a
					// big button. Plus it is a PNG (alpha) image to boot. I have both a 32X32 and 40X40 version of my
					// big png image. Edge will use the 32X32 if the DPI setting of the machine is 96, and the 40X40
					// version if the DPI is greater than 96 (user can easily set the DPI to 120, or more).

					// Notice that as of ST7 this sample's string table for this command is modified. I added a backslash to the string for this
					// command in order to add a separator before the control added to the ribbon group for this command. In the string table
					// I had to double up the backslash since in c++ the way to get a backslash to appear as an actual char in the string
					// two must be present (to indicate it is not an escape sequence). I do this because when I use the new SetAddInInfoEx2
					// interface I don't execute any toolbar code below and that is where one could set up the separator for this command by
					// using the BeginGroup property of a command bar button.

					bszCategoryName1 = strCommandBarName1;
					VERIFY_OK(pAddInEx2->SetAddInInfoEx2(ResourceFilename, EnvironmentCatid,
						bszCategoryName1, bEdgeSupportsSVG ? IDR_ADDIN_SVG : IDB_PNG1, bEdgeSupportsSVG ? IDR_ADDIN_SVG : IDB_PNG3, -1,
						-1, 1, saTab1Group1MrBigCommandString.GetSafeArrayPtr(),
						saTab1Group1MrBigCommandID.GetSafeArrayPtr(), saTab1Group1MrBigCommandButtonStyle.GetSafeArrayPtr()));

					// Now add an entirely new category. Since the add-in has "\n" prepended to its description to indicate
					// it wants a top level menu/tab, this results in the second tab being created.
					bszCategoryName2 = strCommandBarName2;
					VERIFY_OK(pAddInEx2->SetAddInInfoEx2(ResourceFilename, EnvironmentCatid,
						bszCategoryName2, bEdgeSupportsSVG ? IDR_TOOLBAR_LARGE_SVG : IDR_TOOLBAR_MEDIUM, bEdgeSupportsSVG ? IDR_TOOLBAR_LARGE_SVG : IDR_TOOLBAR_LARGE, IDR_TOOLBAR_MEDIUMMONOCHROME,
						IDR_TOOLBAR_LARGEMONOCHROME, NUMBER_OF_COMMANDS, saTab2Group1CommandStrings.GetSafeArrayPtr(),
						saTab2Group1CommandIDs.GetSafeArrayPtr(), saTab2Group1CommandButtonStyles.GetSafeArrayPtr()));

					if( bEdgeSupportsBackstage )
					{
						// Add an entry to the system menu (a.k.a. the "backstage"). This command will be below all backstage
						// tabs and before the exit command.
						bszBackstageCategoryName = strBackstageCategoryString;
						VERIFY_OK(pAddInEx2->SetAddInInfoEx2(ResourceFilename, EnvironmentCatid,
							bszBackstageCategoryName, bEdgeSupportsSVG ? IDR_ADDIN_SVG : IDB_PNG1, bEdgeSupportsSVG ? IDR_ADDIN_SVG : IDB_PNG3, -1,
							-1, 1, saBackstageCommandString.GetSafeArrayPtr(),
							saBackstageCommandID.GetSafeArrayPtr(), saBackstageCommandButtonStyle.GetSafeArrayPtr()));

						// Now add a tab/flyout to the system menu/backstage with one command. Images for the backstage
						// flyouts are 32X32 and will be scaled if need be.
						bszBackstageCategoryName = strBackstageCategoryTabString;
						VERIFY_OK(pAddInEx2->SetAddInInfoEx2(ResourceFilename, EnvironmentCatid,
							bszBackstageCategoryName, bEdgeSupportsSVG ? IDR_ADDIN_SVG : IDB_PNG1, bEdgeSupportsSVG ? IDR_ADDIN_SVG : IDB_PNG3, -1,
							-1, 1, saBackstageTabCommandString.GetSafeArrayPtr(),
							saBackstageTabCommandID.GetSafeArrayPtr(), saBackstageTabCommandButtonStyle.GetSafeArrayPtr()));

						// Now, instead of adding a tab to the system menu/backstage, the categorn name is the same as the
						// previous one just added. The result is that I am simply adding two more commands to the
						// tab/flyout. This would be useful, for example, if your images are not all in a single glyph.
						// Here I am just resuing the old bitmaps that have been around forever. These images are 24x23
						// and will be scaled. Just a demo add-in so forgiveness is in order.
						bszBackstageCategoryName = strBackstageCategoryTabString;
						VERIFY_OK(pAddInEx2->SetAddInInfoEx2(ResourceFilename, EnvironmentCatid,
							bszBackstageCategoryName, bEdgeSupportsSVG ? IDR_TOOLBAR_LARGE_SVG : IDR_TOOLBAR_MEDIUM, bEdgeSupportsSVG ? IDR_TOOLBAR_LARGE_SVG : IDR_TOOLBAR_LARGE, -1,
							-1, 2, saBackstageTabCommand2String.GetSafeArrayPtr(),
							saBackstageTabCommand2ID.GetSafeArrayPtr(), saBackstageTabCommand2ButtonStyle.GetSafeArrayPtr()));
					}
				}
				else
				{
					VERIFY_OK(pAddInEx->SetAddInInfoEx(ResourceFilename, EnvironmentCatid,
							  bszCategoryName1, IDR_TOOLBAR_MEDIUM, IDR_TOOLBAR_LARGE, IDR_TOOLBAR_MEDIUMMONOCHROME,
							  IDR_TOOLBAR_LARGEMONOCHROME, NUMBER_OF_COMMANDS, saTab1Group2CommandStrings.GetSafeArrayPtr(),
							  saTab1Group2CommandIDs.GetSafeArrayPtr()));

					// And I can add yet more to the primary category. In this case, the command added will have a
					// big button. Plus it is a PNG (alpha) image to boot. I have both a 32X32 and 40X40 version of my
					// big png image. Edge will use the 32X32 if the dpi setting of the machine is 96, and the 40X40
					// version if the dpi is greater than 96 (user can easily set the dpi to 120, or more).
					VERIFY_OK(pAddInEx->SetAddInInfoEx(ResourceFilename, EnvironmentCatid,
							  bszCategoryName1, IDB_PNG1, IDB_PNG3, -1,
							  -1, 1, saTab1Group1MrBigCommandString.GetSafeArrayPtr(),
							  saTab1Group1MrBigCommandID.GetSafeArrayPtr()));

					// Now add an entirely new category. Since the add-in has "\n" prepended to its description to indicate
					// it wants a top level menu/tab, this results in the second tab being created.
					VERIFY_OK(pAddInEx->SetAddInInfoEx(ResourceFilename, EnvironmentCatid,
							  bszCategoryName2, IDR_TOOLBAR_MEDIUM, IDR_TOOLBAR_LARGE, IDR_TOOLBAR_MEDIUMMONOCHROME,
							  IDR_TOOLBAR_LARGEMONOCHROME, NUMBER_OF_COMMANDS, saTab2Group1CommandStrings.GetSafeArrayPtr(),
							  saTab2Group1CommandIDs.GetSafeArrayPtr()));
				}
			}
		}
		else
		{
			// Old restricted UI. Not much to see here, move along.
			VERIFY_OK(m_pCommands->GetAddIn()->SetAddInInfo((long)hInstance, EnvironmentCatid,
					  bszCategoryName1, IDR_TOOLBAR_MEDIUM, IDR_TOOLBAR_LARGE, IDR_TOOLBAR_MEDIUMMONOCHROME,
					  IDR_TOOLBAR_LARGEMONOCHROME, NUMBER_OF_COMMANDS, saTab1Group1CommandStrings.GetSafeArrayPtr(),
					  saTab1Group1CommandIDs.GetSafeArrayPtr()));
		}

		// Only add buttons if this is the first time the environment has been initialized 
		// with this addin connected. After the first time, SolidEdge will persist command bars
		// and buttons unless the user customized the gui and removed them. Failure
		// to heed the flag can result in replicated or reappearing buttons or problems for the
		// customer that customized the UI. For example, a removed command can reappear. Note if the
		// ISEAddInEx2 interface was used, the button control styles are passed in via that interface
		// and hence this code doesn't need to use command bars to effect style changes.
		if( VARIANT_TRUE == bFirstTime && pAddInEx2 == NULL )
		{
			// In ST, command bars correlate to groups on the command ribbon.
			_bstr_t bszCommandBarName1(strCommandBarName1);
			_bstr_t bszCommandBarName2(strCommandBarName2);
			_bstr_t bszCommandBarName3(strCommandBarName3);

			for( index = 0; index < NUMBER_OF_COMMANDS; ++index )
			{
				// TODO: Further customization of the button can be done here.

				CommandBarButtonPtr pButton = m_pCommands->GetAddIn()->AddCommandBarButton( EnvironmentCatid, bszCommandBarName1, index); 

				if( pButton )
				{
					// I want a caption along with the icon. By default Edge only adds an icon so this is how I get
					// text too. In ST the toolbar is converted to the ribbon. This command is in a category added
					// via SetAddInInfoEx so it currently only has an icon. AddCommandBarButtons is currently only
					// useful as a way to further customize the add-in's UI.
					pButton->Style = seButtonIconAndCaption;
				}
			}

			if( bEdgeSupportsMultipleMenus )
			{
				long CmdIndex = 5;

				CommandBarButtonPtr pBigButton = m_pCommands->GetAddIn()->AddCommandBarButton( EnvironmentCatid, bszCommandBarName1, CmdIndex); 

				if( pBigButton )
				{
					// must use seButtonIconAndCaptionBelow since I am loading a big icon. Failure to do so will cause the
					// system to reduce the button size. The large image size is 32X32. Anything smaller will be upsized
					// and the results are not necessarily pretty just as the downsize can be down-right ugly (dropouts due
					// the the button being 1/4th as dense after resizing).
					pBigButton->Style = seButtonIconAndCaptionBelow;
				}

				// Now add a second group to the primary tab. Examine the resource file and note the string format that is
				// used to indicate what category/tab the group will belong too. Again, edge will magically move the commands
				// from the primary group to the new group (ie., no duplication). Don't make the calls and you will see they
				// remain in the first group.
				CmdIndex = 6;
				for( index = 0; index < NUMBER_OF_COMMANDS; ++index )
				{
					CommandBarButtonPtr pButton = m_pCommands->GetAddIn()->AddCommandBarButton( EnvironmentCatid, bszCommandBarName3, CmdIndex); 

					if( pButton )
					{
						pButton->Style = seButtonIconAndCaption;// must use this since I am loading a bit icon
					}

					CmdIndex++;
				}

				// Now I will modify the commands on the primary/first group of the second tab. Again, examine the string resource
				// and note the format of the string.
				for( index = 0; index < NUMBER_OF_COMMANDS; ++index )
				{
					CommandBarButtonPtr pButton = m_pCommands->GetAddIn()->AddCommandBarButton( EnvironmentCatid, bszCommandBarName2, index+2); 

					if( pButton )
					{
						// And while we are at it, let's make one a check and one a radio button.
						if( index == 0 )
						{
							pButton->Style = seRadioButton;
						}
						else
						{
							// seCheckButton will result in a button with text (text defined via SetAddInInfoEx). But I want
							// an image associted with the command (image also defined via SetAddInInfoEx). Sorry, no big
							// images for check buttons.
							pButton->Style = seCheckButtonAndIcon;
						}
					}
				}
			}
		}

		// Useless for ST but no harm done as this add-in has been around since day one.
		EnvironmentPtr pEnv = pEnvironment;
		if( NULL != pEnv )
		{
			CommandBarsPtr pBars = pEnv->CommandBars;
			if( NULL != pBars )
			{
				CommandBarPtr pBar = pBars->Item( bszCategoryName1 );
				if( NULL != pBar )
				{
					// Wizard default is to position it in the top area of the Solid Edge frame.
					pBar->Position = seBarTop;
				}
			}
		}

		// Show the capability of adding a command for which there is no UI. The returned command ID
		// can be used in the application's StartCommand API or for adding an entry on a shortcut menu.
		// This is also a way to add a command that has no image since SetAddInInfoEx requires the glyph 
		// to be in a one-to-one correspondence (order matters) with the commands that are added.
		_bstr_t CommandName = "LocTest";

		// Notice that the command ID returned from the call is the real command ID behind the command inside Edge.
		// I could use the returned ID in the edge application's StartCommand API or in response to shortcut menu events.
		s_LocTestCmdId = m_pCommands->GetAddIn()->AddCommand( EnvironmentCatid, CommandName, (long)10); 

		static bool s_AlreadyLoadedTranslatorData = false;
		if( false == s_AlreadyLoadedTranslatorData )
		{
			CString strEnv(EnvironmentCatid);
			CString strSupportedPartEnv = _T("{26618396-09d6-11d1-ba07-080036230602}"); // part
			CString strSupportedSyncPartEnv = _T("{D9B0BB85-3A6C-4086-A0BB-88A1AAD57A58}"); // direct modeling/synchronous part
			strEnv.MakeLower();
			strSupportedPartEnv.MakeLower();
			strSupportedSyncPartEnv.MakeLower();
			if( strEnv == strSupportedPartEnv || strEnv == strSupportedSyncPartEnv )
			{
				s_AlreadyLoadedTranslatorData = true;

				// See if this version of Solid Edge supports SaveAs for custom tranlations.
				ISEAddInSaveAsTranslatorPtr pTranslator = m_pCommands->GetAddIn();
				if( pTranslator )
				{
					_bstr_t strFilter( L"AsmLoc X_T document (*.z_t),*.z_t" );

					pTranslator->SetSaveAsTranlatorInfo( igPartDocument, strFilter );

					_bstr_t strFilter1( L"AsmLoc Parasolid document (*.x_t),*.x_t" );

					pTranslator->SetSaveAsTranlatorInfo( igPartDocument, strFilter1 );
				}
			}
		}
	}
	else
	{
		hr = E_OUTOFMEMORY;
	}

	return hr;
}

STDMETHODIMP CSEAddIn::raw_OnDisconnection( SeDisconnectMode DisconnectMode )
{
	AFX_MANAGE_STATE( AfxGetStaticModuleState() );

	HRESULT hr = S_OK;

	if( m_pCommands )
	{
		m_pCommands->UnadviseFromEvents();
		m_pCommands->Release();
		m_pCommands = NULL;
	}

	// TODO: Perform any cleanup work here

  return hr;
}

// Return S_OK or E_ABORT to indicate Edge should do nothing. Return S_FALSE to let Edge go to the next add-in or to perform the task.
// DEV Note: This is dangerous as you can shut down drag and drop so choose your return codes wisely as the OLE data for the drop
// can be aborted and even if the Drop code returns S_FALSE, when Edge tries to perform the Drop, it will find the OLE data is
// not present. There is probably no reason to have every method return S_OK or E_ABORT.
STDMETHODIMP_(HRESULT __stdcall) CSEAddIn::DragEnter(LPDATAOBJECT, DWORD, POINTL, LPDWORD)
{
	return S_FALSE;
}

STDMETHODIMP_(HRESULT __stdcall) CSEAddIn::DragOver(DWORD, POINTL, LPDWORD)
{
	return S_FALSE;
}

STDMETHODIMP_(HRESULT __stdcall) CSEAddIn::DragLeave()
{
	return S_FALSE;
}

STDMETHODIMP_(HRESULT __stdcall) CSEAddIn::Drop(LPDATAOBJECT, DWORD, POINTL pt, LPDWORD)
{
	// So here's the deal. An add-in has no window. So Edge simply QI's for IDropTarget when a drop occurs anywhere on the ribbon and if your add-in
	// returns the interface, Drop is called. And, if you return S_OK. Edge is done. So, first add-in that supports the data object wins.
	return S_FALSE;
}

STDMETHODIMP CSEAddIn::InterfaceSupportsErrorInfo(REFIID riid)
{
	static const IID* arr[] = 
	{
		&__uuidof(ISEAddIn),
	};
	for (int i=0;i<sizeof(arr)/sizeof(arr[0]);i++)
	{
		if (InlineIsEqualGUID(*arr[i],riid))
			return S_OK;
	}
	return S_FALSE;
}
