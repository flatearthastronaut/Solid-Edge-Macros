// Commands.h : header file
//

#if !defined(AFX_COMMANDS_H__03B8616A_1896_11D1_BA18_080036230602__INCLUDED_)
#define AFX_COMMANDS_H__03B8616A_1896_11D1_BA18_080036230602__INCLUDED_

#include "stdafx.h"
#include "AsmLoc.h"

// This class is used to handle generic application events and command events
// fired specifically to this addin. Trivial implementations of the application
// events are provided. To handle a specific app event, replace the implementation
// provided with your own.

// This add-in exercies the Solid Edge edge bar API by adding UI to the edge bar.
// To facilitate this, the add-in has its own "document", which is an
// object that subscribes to the Solid Edge document event set so that when a
// document is closed, the add-in can remove any edge bar UI added for that document.
// It also holds onto the dialog inserted into the edge bar.
class ADDINDocument;

// Use the following typedef when creating an instance of CEDGEBARTSTDocument COM object.
typedef CComObject<ADDINDocument> ADDINDocumentObj;

// Define a mapping from a Solid Edge document dispatch pointer to my add-in document.
typedef CTypedPtrMap<CMapPtrToPtr,LPDISPATCH,CComObject<ADDINDocument>*> CMapSEDocDispatchToMyDoc;

class CCommands : 
	public CComObjectRoot,
	public CComCoClass<CCommands, &CLSID_Commands>
{
protected:
	// The one and only Solid Edge application object! Use this object to communicate with the solid
	// edge application, e.g., to obtain your favorite Solid Edge interface.

	// Make the app pointer static so I can easily get to it from anywhere in the add-in.
	static ApplicationPtr m_pApplication;

	// Each add-in has a corresponding API created by Edge. I will store that interface. Again,
	// I am making it static so I can easily get to it from anywhere in the add-in.
	static ISEAddInExPtr    m_pSEAddIn;

	// Declare a document map. I use this, for instance, when trying to determine if I have already
	// added an edge bar UI for a particular document.
	CMapSEDocDispatchToMyDoc m_pDocuments;

public:
	CCommands();
	~CCommands();
  
	HRESULT SetApplicationObject ( LPDISPATCH pApplicationDispatch, BOOL bWithEvents = TRUE );
		
	static ApplicationPtr GetApplicationPtr  ( void ) { return m_pApplication; }

	HRESULT UnadviseFromEvents();

	HRESULT SetAddInObject( AddIn* pSolidEdgeAddIn, BOOL bWithEvents = TRUE );

	static ISEAddInPtr GetAddIn()  { return m_pSEAddIn; }

	HRESULT CreateADDINDocument ( LPDISPATCH pSEDocumentDispatch,
								  BOOL bWithEvents = TRUE,
								  ADDINDocumentObj** ppADDINDocument = NULL );

	HRESULT DestroyADDINDocument      ( LPDISPATCH pSEDOcumentDispatch );

	HRESULT DestroyAllADDINDocuments  ( void );

	ADDINDocumentObj* GetDocument     ( LPDISPATCH pSEDocumentDispatch );

	BEGIN_COM_MAP(CCommands)
	END_COM_MAP()
	DECLARE_NOT_AGGREGATABLE(CCommands)

protected:
	
	// This class template is used as the base class for the
	// event handler objects which are declared below.

	template <class IEvents, const IID* piidEvents, const GUID* plibid,
	class XEvents, const CLSID* pClsidEvents>
	class XEventHandler :
		public CComDualImpl<IEvents, piidEvents, plibid>,
		public CComObjectRoot,
		public CComCoClass<XEvents, pClsidEvents>
	{
	public:
		BEGIN_COM_MAP(XEvents)
			COM_INTERFACE_ENTRY_IID(*piidEvents, IEvents)
		END_COM_MAP()
		DECLARE_NOT_AGGREGATABLE(XEvents)
		HRESULT Connect(IUnknown* pUnk)
		{ HRESULT hr; VERIFY(SUCCEEDED(hr = AtlAdvise(pUnk, this, *piidEvents, &m_dwAdvise))); return hr; }
		HRESULT Disconnect(IUnknown* pUnk)
		{ HRESULT hr; VERIFY(SUCCEEDED(hr = AtlUnadvise(pUnk, *piidEvents, m_dwAdvise))); return hr; }

		// Store a back pointer to the embedding command object (makes life easy).
		// Do not delete! Its merely a back pointer to the object that controls the
		// lifetime of your event handlers. Use this pointer in the event handling methods 
		// based on this object template to communicate to the embedding class (CCommands).

		CCommands* m_pCommands;

	protected:
		DWORD m_dwAdvise;
	};

	class XShortCutMenuEvents : public XEventHandler<ISEShortCutMenuEvents, 
		&__uuidof(ISEShortCutMenuEvents), &LIBID_AsmLocLib, 
		XShortCutMenuEvents, &CLSID_ADDINShortCutMenuEvents>
	{
	public:
		STDMETHOD (raw_BuildMenu)         (BSTR bstrEnvironmentCatid,
			enum ShortCutMenuContextConstants Context,
			LPDISPATCH pGraphicDispatch,
			SAFEARRAY **pMenuStrings,
			SAFEARRAY **pCommandIDs);
	};
	typedef CComObject<XShortCutMenuEvents> XShortCutMenuEventsObj;
	XShortCutMenuEventsObj* m_pShortCutMenuEventsObj;

	// This object handles command events fired by Solid Edge to the addin object
	class XAddInEvents : public XEventHandler<ISEAddInEvents, 
		&__uuidof(ISEAddInEvents), &LIBID_AsmLocLib, 
		XAddInEvents, &CLSID_ASMLOCAddInEvents>
	{
	public:
		// ISEAddInEvents methods

		// OnCommand is called when the user invokes one of this add-in's commands.
		// The command identifier passed in is one of the identifiers passed to
		// edge when the add-in adds its commands.
		STDMETHOD (raw_OnCommand)         ( long nCmdID );

		// OnCommandHelp is called when the user invokes help on one of this add-in's 
		// commands. The command identifier passed in is one of the identifiers passed to
		// edge when the add-in adds its commands or -1 (to invoke non-command specific
		// help). The uHelpCommand is one of the Windows help values (see the MSDN).
		STDMETHOD (raw_OnCommandHelp)     ( long hFrameWnd,
											long uHelpCommand,
											long nCmdID );

		// OnCommandUpdateUI is called when Edge needs the add-in to update the UI
		// for a particular command. Usually this is used to enable a command (Edge
		// disables a command if no action is taken) but the add-in can use this to
		// modify the text, set a bitmap, set a check mark etc (see the SECommandActivation
		// constants enumeration. The command identifier passed in is one of the identifiers 
		// passed to edge when the add-in adds its commands.
		STDMETHOD (raw_OnCommandUpdateUI) ( long nCmdID, 
											long *lCmdFlags,
											BSTR *MenuItemText,
											long *nIDBitmap );
	};
	typedef CComObject<XAddInEvents> XAddInEventsObj;

	XAddInEventsObj* m_pAddInEventsObj;

	class XAddInEventsEx : public XEventHandler<ISEAddInEventsEx, 
		&__uuidof(ISEAddInEventsEx), &LIBID_AsmLocLib, 
		XAddInEventsEx, &CLSID_ASMLOCAddInEventsEx>
	{
	public:
		// ISEAddInEvents methods

		// OnCommand is called when the user invokes one of this add-in's commands.
		// The command identifier passed in is one of the identifiers passed to
		// edge when the add-in adds its commands.
		STDMETHOD (raw_OnCommand)         ( long nCmdID );

		// OnCommandHelp is called when the user invokes help on one of this add-in's 
		// commands. The command identifier passed in is one of the identifiers passed to
		// edge when the add-in adds its commands or -1 (to invoke non-command specific
		// help). The uHelpCommand is one of the Windows help values (see the MSDN).
		STDMETHOD (raw_OnCommandHelp)     ( long hFrameWnd,
											long uHelpCommand,
											long nCmdID );

		// OnCommandUpdateUI is called when Edge needs the add-in to update the UI
		// for a particular command. Usually this is used to enable a command (Edge
		// disables a command if no action is taken) but the add-in can use this to
		// modify the text, set a bitmap, set a check mark etc (see the SECommandActivation
		// constants enumeration. The command identifier passed in is one of the identifiers 
		// passed to edge when the add-in adds its commands.
		STDMETHOD (raw_OnCommandUpdateUI) ( long nCmdID, 
											long *lCmdFlags,
											BSTR *MenuItemText,
											long *nIDBitmap );
		// OnCommandOnLineHelp is called when the user invokes help on one of this add-in's 
		// commands with Solid Edge ST6 or later with on-line help enabled. The command identifier 
		// passed in is one of the identifiers passed to edge when the add-in adds its commands 
		// or -1 (to invoke non-command specific help). The uHelpCommand is one of the Windows 
		// help values (see the MSDN).
		// If the add-in supports on-line help, return the complete URL and Solid Edge will
		// navigate to the link. If the event is not implemented, return NULL for the HelpURL
		// and Solid Edge will call the original OnCommandHelp API. Also, if for some reason
		// on-line help is disabled or unavailable (network down or no network?) such that the
		// URL is unusable, Solid Edge may still call OnCommandHelp to invoke local help.
		STDMETHOD (raw_OnCommandOnLineHelp) ( long uHelpCommand,
											  long nCmdID,
											  BSTR* HelpURL );
	};
	typedef CComObject<XAddInEventsEx> XAddInEventsExObj;

	XAddInEventsExObj* m_pAddInEventsExObj;

	// As of ST8 Solid Edge provided an enhanced interface for the add-in to receive events.
	// The new interface works like the previous ones but has more inputs to enable an add-in
	// to determine how a command was invoked - from the normal command UI or from a shortcut/context
	// menu. And also Solid Edge is now passing in certain parameters commands can obtain
	// through API calls but may appreciate not having to make the calls.

	class XAddInEventsEx2 : public XEventHandler<ISEAddInEventsEx2, 
		&__uuidof(ISEAddInEventsEx2), &LIBID_AsmLocLib, 
		XAddInEventsEx2, &CLSID_ASMLOCAddInEventsEx2>
	{
	public:
		// ISEAddInEventsEx2 methods

		// OnCommand is called when the user invokes one of this add-in's commands.
		// The command identifier passed in is one of the identifiers passed to
		// edge when the add-in adds its commands. The Context can be used to determine if the command was
		// started by a user that selected the add-in command from a short cut (context) menu entry the
		// add-in may have added in response to BuildMenu event. The active document/window/select set
		// may or may not be NULL. Some add-ins add commands to the no document (a.k.a. application)
		// environment and there is no document, window or select set in that case. So always check
		// your pointers before accessing them!
		STDMETHOD (raw_OnCommand)         ( long nCmdID,
											ShortCutMenuContextConstants Context, 
											DocumentTypeConstants ActiveDocumentType, 
											LPDISPATCH pActiveDocument, 
											LPDISPATCH pActiveWindow, 
											LPDISPATCH pActiveSelectSet );

		// OnCommandHelp is called when the user invokes help on one of this add-in's 
		// commands. The command identifier passed in is one of the identifiers passed to
		// edge when the add-in adds its commands or -1 (to invoke non-command specific
		// help). The uHelpCommand is one of the Windows help values (see the MSDN).
		STDMETHOD (raw_OnCommandHelp)     ( long hFrameWnd,
			long uHelpCommand,
			long nCmdID );

		// OnCommandUpdateUI is called when Edge needs the add-in to update the UI
		// for a particular command. Usually this is used to enable a command (Edge
		// disables a command if no action is taken) but the add-in can use this to
		// modify the text, set a bitmap, set a check mark etc (see the SECommandActivation
		// constants enumeration. The command identifier passed in is one of the identifiers 
		// passed to edge when the add-in adds its commands.
		STDMETHOD (raw_OnCommandUpdateUI) ( long nCmdID, 
											ShortCutMenuContextConstants Context, 
											DocumentTypeConstants ActiveDocumentType, 
											LPDISPATCH pActiveDocument, 
											LPDISPATCH pActiveWindow, 
											LPDISPATCH pActiveSelectSet,
											long *lCmdFlags,
											BSTR *MenuItemText,
											long *nIDBitmap );
		// OnCommandOnLineHelp is called when the user invokes help on one of this add-in's 
		// commands with Solid Edge ST6 or later with on-line help enabled. The command identifier 
		// passed in is one of the identifiers passed to edge when the add-in adds its commands 
		// or -1 (to invoke non-command specific help). The uHelpCommand is one of the Windows 
		// help values (see the MSDN).
		// If the add-in supports on-line help, return the complete URL and Solid Edge will
		// navigate to the link. If the event is not implemented, return NULL for the HelpURL
		// and Solid Edge will call the original OnCommandHelp API. Also, if for some reason
		// on-line help is disabled or unavailable (network down or no network?) such that the
		// URL is unusable, Solid Edge may still call OnCommandHelp to invoke local help.
		STDMETHOD (raw_OnCommandOnLineHelp) ( long uHelpCommand,
											  long nCmdID,
											  BSTR* HelpURL );
	};
	typedef CComObject<XAddInEventsEx2> XAddInEventsEx2Obj;

	XAddInEventsEx2Obj* m_pAddInEventsEx2Obj;

	// This object handles events fired by the Application object
	class XApplicationEvents : public XEventHandler<ISEApplicationEvents, 
		&__uuidof(ISEApplicationEvents), &LIBID_AsmLocLib, 
		XApplicationEvents, &CLSID_ASMLOCApplicationEvents>
	{
	public:
		// ISEApplicationEvents methods
		STDMETHOD (raw_AfterActiveDocumentChange)   ( LPDISPATCH theDocument );

		STDMETHOD (raw_AfterCommandRun)             ( long theCommandID );

		STDMETHOD (raw_AfterDocumentOpen)           ( LPDISPATCH theDocument );

		STDMETHOD (raw_AfterDocumentPrint)          ( LPDISPATCH theDocument,
													  long hDC, 
													  double *ModelToDC,
													  long *Rect );

		STDMETHOD (raw_AfterDocumentSave)           ( LPDISPATCH theDocument );

		STDMETHOD (raw_AfterEnvironmentActivate)    ( LPDISPATCH theEnvironment );

		STDMETHOD (raw_AfterNewDocumentOpen)        ( LPDISPATCH theDocument );

		STDMETHOD (raw_AfterNewWindow)              ( LPDISPATCH theWindow );

		STDMETHOD (raw_AfterWindowActivate)         ( LPDISPATCH theWindow );

		STDMETHOD (raw_BeforeCommandRun)            ( long theCommandID );

		STDMETHOD (raw_BeforeDocumentClose)         ( LPDISPATCH theDocument );

		STDMETHOD (raw_BeforeDocumentPrint)         ( LPDISPATCH theDocument,
													  long hDC, 
													  double *ModelToDC, 
													  long *Rect );

		STDMETHOD (raw_BeforeEnvironmentDeactivate) ( LPDISPATCH theEnvironment );

		STDMETHOD (raw_BeforeWindowDeactivate)      ( LPDISPATCH theWindow );

		STDMETHOD (raw_BeforeQuit)                  ( void );

		STDMETHOD (raw_BeforeDocumentSave)          ( LPDISPATCH theDocument );
	};
	typedef CComObject<XApplicationEvents> XApplicationEventsObj;
	XApplicationEventsObj* m_pApplicationEventsObj;

	// This object handles events fired by the Application object
	class XApplicationEventsEx : public XEventHandler<ISEApplicationEventsEx, 
		&__uuidof(ISEApplicationEventsEx), &LIBID_AsmLocLib, 
		XApplicationEventsEx, &CLSID_ASMLOCApplicationEventsEx>
	{
	public:
		// ISEApplicationEventsEx methods
		STDMETHOD(raw_OnCommandUpdateUI)				( long CommandID, 
														  long* CommandFlags, 
														  BSTR* MenuItemText);
	};
	typedef CComObject<XApplicationEventsEx> XApplicationEventsExObj;
	XApplicationEventsExObj* m_pApplicationEventsExObj;

	// This object handles events fired by the Application object
	class XApplicationEventsEx2 : public XEventHandler<ISEApplicationEventsEx2, 
		&__uuidof(ISEApplicationEventsEx2), &LIBID_AsmLocLib, 
		XApplicationEventsEx2, &CLSID_ASMLOCApplicationEventsEx2>
	{
	public:
		// ISEApplicationEventsEx2 methods
		STDMETHOD(raw_OnBeforeDocumentOpen)				( ApplicationBeforeDocumentOpenEvent Context, 
														  BSTR Filename, 
														  VARIANT_BOOL* vbCancelOpen);
	};
	typedef CComObject<XApplicationEventsEx2> XApplicationEventsEx2Obj;
	XApplicationEventsEx2Obj* m_pApplicationEventsEx2Obj;

	// This object handles events fired by the Application object
	class XApplicationFrameSwitchingEvents : public XEventHandler<ISEApplicationActiveFrameSwitchingEvents, 
		&__uuidof(ISEApplicationActiveFrameSwitchingEvents), &LIBID_AsmLocLib, 
		XApplicationFrameSwitchingEvents, &CLSID_ASMLOCApplicationFrameSwitchingEvents>
	{
	public:
		// ISEApplicationActiveFrameSwitchingEvents methods
		STDMETHOD(raw_OnApplicationActiveFrameSwitching)	( enum ApplicationActiveFrameSwitchingEvent Context,
															 long hWndPreviouslyActiveFrame,
															 long hWndNewlyActiveFrame );
	};
	typedef CComObject<XApplicationFrameSwitchingEvents> XApplicationFrameSwitchingEventsObj;
	XApplicationFrameSwitchingEventsObj* m_pAplicationFrameSwitchingEventsObj;

	// This object handles events fired by the Application object
	class XApplicationLicenseEvents : public XEventHandler<ISEApplicationLicenseEvents,
		& __uuidof(ISEApplicationLicenseEvents), & LIBID_AsmLocLib,
		XApplicationLicenseEvents, & CLSID_ASMLOCApplicationLicenseEvents>
	{
	public:
		// ISEApplicationLicenseEvents methods
		STDMETHOD(raw_OnApplicationLicense)	(enum ApplicationLicenseEvent Context,
			BSTR FeatureName);
	};
	typedef CComObject<XApplicationLicenseEvents> XApplicationLicenseEventsObj;
	XApplicationLicenseEventsObj* m_pAplicationLicenseEventsObj;


	// This object handles events fired by the Application object
	class XApplicationDocumentLoadingEvents : public XEventHandler<ISEApplicationDocumentLoadingEvents,
		&__uuidof(ISEApplicationDocumentLoadingEvents), &LIBID_AsmLocLib,
		XApplicationDocumentLoadingEvents, &CLSID_ASMLOCApplicationDocumentLoadingEvents>
	{
	public:
		// ISEApplicationActiveFrameSwitchingEvents methods
		STDMETHOD(raw_OnApplicationDocumentLoading)	(BSTR TopLevelFilename, enum ApplicationDocumentLoadingEvent Context,
			unsigned long waitLevel,
			VARIANT_BOOL* Cancel);
	};
	typedef CComObject<XApplicationDocumentLoadingEvents> XApplicationDocumentLoadingEventsObj;
	XApplicationDocumentLoadingEventsObj* m_pAplicationDocumentLoadingEventsObj;


	// This object handles edge bar events fired by Solid Edge to the addin object.
	// This event set was introduced with Solid Edge ST (a.k.a. version 100).
	// If the event set is obtainable, the add-in uses these events to manipulate
	// the edge bar. If it is not available, the add-in will manipulate the edge
	// bar when the AfterActiveDocumentChange event is fired. DO NOT make the
	// common mistake of manipulating the edge bar when the AfterDocumentOpen
	// event is fired! That event is fired for any number of reasons, with the user
	// specifically opening a document in the UI being on one reason (for example,
	// assembly and draft may open documents for version checking or any other number
	// of reasons without ever UI activating the document).
	class XAddInEdgeBarEvents : public XEventHandler<ISEAddInEdgeBarEvents, 
		&__uuidof(ISEAddInEdgeBarEvents), &LIBID_AsmLocLib, 
		XAddInEdgeBarEvents, &CLSID_ASMLOCAddInEdgeBarEvents>
	{
	public:
		// ISEAddInEvents methods
		STDMETHOD (raw_AddPage)				( LPDISPATCH theDocument );

		STDMETHOD (raw_RemovePage)			( LPDISPATCH theDocument );

		STDMETHOD (raw_IsPageDisplayable)	( IDispatch * theDocument,
											  BSTR EnvironmentCatID,
											  VARIANT_BOOL * vbIsPageDisplayable );
	};
	typedef CComObject<XAddInEdgeBarEvents> XAddInEdgeBarEventsObj;
	XAddInEdgeBarEventsObj* m_pAddInEdgeBarEventsObj;

	class XSaveAsTranslatorEvents : public XEventHandler<ISEAddInSaveAsTranslatorEvents, 
		&__uuidof(ISEAddInSaveAsTranslatorEvents), &LIBID_AsmLocLib, 
		XSaveAsTranslatorEvents, &CLSID_ADDINSaveAsTranslatorEvents>
	{
	public:
		STDMETHOD (raw_OnOptions)			(LPDISPATCH theDocument, BSTR FilenameExtension);
		STDMETHOD (raw_OnOptionsUpdateUI)	(LPDISPATCH theDocument, BSTR FilenameExtension, long* Flags);
		STDMETHOD (raw_OnSaveAs)			(LPDISPATCH theDocument, BSTR SaveAsFilename, long* hResult);
	};
	typedef CComObject<XSaveAsTranslatorEvents> XSaveAsTranslatorEventsObj;
	XSaveAsTranslatorEventsObj* m_pXSaveAsTranslatorEventsObj;

public:

	friend class XAddInEvents;
	friend class XAddInEventsEx;
};
// Use the following typedef when creating an instance of CCommands.
typedef CComObject<CCommands> CCommandsObj;

// Define a base class for an individual command. This class provides all the essential
// ingredients for writing a fully functional Solid Edge command. It pvovides default
// handling of all the various events which can be fired by the Solid Edge command control 
// (obtained by calling CreateCommand). When an event occurs, a virtual function on this base 
// class is called by embedded event handler classes. ALL ONE HAS TO DO IS SUBCLASS FROM 
// THIS OBJECT AND OVERRIDE ANY SPECIFIC EVENT THE SUBCLASS IS INTERESTED IN RECEIVING. 
// Users of the command and mouse controls should feel quite familiar with this object. 
// That's because the object returned from CreateCommand is essentially the command control 
// and its mouse property is the mouse control.

// Note: I choose to class an individual command from IUnknown, which I then add to
//       the COM_MAP because I am using the "GetUnknown" function that the BEGIN_COM_MAP
//       macro declares for the CCommand object. The reason for doing this is that once
//       the CCommands object creates a CCommandObj (the templatized COM version of CCommand),
//       it addrefs the CCommandObj and then abandons the CCommand. From that point on, the
//       lifetime of CCommand is controlled by Solid Edge. Eventually, the XCommandEvents
//       object embedded in CCommands (you can see that object below) receives a Terminate
//       call from Solid Edge. At that point, the command should perform all clean up code
//       and stop execution. Once the CCommand object disconnects from the Solid Edge
//       event sinks, there should be no reference to Solid Edge held by the command, and
//       there is, and never really was, any other external reference to the CCommand object.
//       Hence, the CCommand object must dereference itself so that its destructor will be called
//       and hence its allocated memory will be freed.
//
//       But theres a slight problem with that. There is a difference between a CCommandObj
//       object, and a CCommand object. The former is created by the use of the CComObject
//       ATL template, with CCommand as the template argument. That implicitly creates a
//       C++ COM object whose base class is CCommand object. Hence, member functions declared
//       on CCommand object cannot directly call IUnknown functions. They exist somewhere "up"
//       the class graph.
//
//       Apparently, ATL solves this "dilemma" by defining a GetUnknown function in BEGIN_COM_MAP.
//       Sounds simple but there is a caveat. The object have at least one "_ATL_SIMPLEMAP_ENTRY" in
//       the COM map for GetUnknown to work ( asserts without one ). Hence, the reason for adding
//       IUnknown to the object and the map.
//
//       I could not find any documentation on GetUnknown, so I wrote a GetMyUnknown which calls
//       GetUnknown (see code below) to provide some degree of protection in case that ever changes.
//       
//       Alternatively, one could use the fact that any event set that the object connects to results
//       in a reference on that object. Simply allow the disconnection process to delete the object.
//       I chose not to to strengthen my control over the lifetime of the object.


// The ever changing Solid Edge addin command object! An object derived from CCommand will usually
// be  created by the CCommands object whenever its OnCommand function is called. The CCommands object 
// AddRefs it and then calls SetCommandObject. CCommand must do the final Release in order to keep
// from leaking (remember that after creation by CCommands, a CCommand is on its own).
// Use it to connect up any sinks a particular command needs in order to function successfully 
// such as the command or mouse events and to obtain the Command and Mouse interfaces if they 
// are needed.

class CCommand :
	IUnknown, 
	public CComObjectRoot,
	public CComCoClass<CCommand, &CLSID_ASMLOCCommand>
{
protected:
  // Do not delete m_pCommands! It exists as a mere convenience for accessing the CCommands
  // object created when the addin is connected to Solid Edge. Use the access function
  // provided below (see GetCommandsObject()).
	CCommands* m_pCommands;

	ISECommandPtr  m_pSECommand;
	ISEMouseExPtr    m_pSEMouse;

public:
	CCommand();
	virtual ~CCommand();

	LPUNKNOWN GetMyUnknown() { return GetUnknown(); }

	// Helper members to set and get the CCommands object.
	void SetCommandsObject( CCommands* pCommands ) { ASSERT( pCommands ); m_pCommands = pCommands; }
	CCommands* GetCommandsObject()                 { ASSERT( m_pCommands ); return m_pCommands; }

	// CreateCommand helper member which calls the Solid Edge application object to obtain a command 
	// control from Solid Edge. If CommandType is seNoDeactivate, the mouse control is also obtained 
	// and both the mouse and window event sinks are connected. Use seNoDeactivate if your command
	// plans on processing user input obtained from Solid Edge. If seNoDeactivate is not specified,
	// the Solid Edge command control will Terminate the command immediately after invocation. That
	// is normally what one would want if for instance, the addin command simply takes some action
	// that needs no user input, or obtains all user input via a modal dialog box.
	virtual HRESULT CreateCommand( SolidEdgeConstants::seCmdFlag CommandType );

	// GetCommand returns the Solid Edge command control.
	ISECommandPtr GetCommand() { return m_pSECommand; }

	// UnadviseFromCommandEvents unadvises from the command event sets (command, mouse, window ...).
	virtual void UnadviseFromCommandEvents();

	// ReleaseInterfaces releases the command, mouse and window interfaces
	virtual void ReleaseInterfaces();

	BEGIN_COM_MAP(CCommand)
		COM_INTERFACE_ENTRY(IUnknown)
	END_COM_MAP()
	DECLARE_NOT_AGGREGATABLE(CCommand)

protected:

	//  Here is a base class, XEventHandler, for the various sinks that can be obtained from the
	//  command control Solid Edge returns from CreateCommand to subclass from. It handles connection
	//  and disconnection.

	template <class IEvents, const IID* piidEvents, const GUID* plibid,
		class XEvents, const CLSID* pClsidEvents>
	class XEventHandler :
		public CComDualImpl<IEvents, piidEvents, plibid>,
		public CComObjectRoot,
		public CComCoClass<XEvents, pClsidEvents>
	{
	public:
		BEGIN_COM_MAP(XEvents)
			COM_INTERFACE_ENTRY_IID(*piidEvents, IEvents)
		END_COM_MAP()
		DECLARE_NOT_AGGREGATABLE(XEvents)
		HRESULT Connect(IUnknown* pUnk)
		{ HRESULT hr; VERIFY(SUCCEEDED( hr = AtlAdvise(pUnk, this, *piidEvents, &m_dwAdvise))); return hr; }
		HRESULT Disconnect(IUnknown* pUnk)
		{ HRESULT hr; VERIFY(SUCCEEDED( hr = AtlUnadvise(pUnk, *piidEvents, m_dwAdvise))); return hr; }

		// Store a back pointer to the embedding command object (makes life easy).
		// Do not delete! Its merely a back pointer to the object that controls the
		// lifetime of your event handlers. Use this pointer in the event handling methods 
		// based on this object template to communicate with the embedding object (CCOmmand).
		CCommand* m_pCommand;
	protected:
		// cookie time
		DWORD m_dwAdvise;
	};

	// Now derive a class from XEventHandler to handle the ISECommandEvents interface.
	class XCommandEvents : public XEventHandler<ISECommandEvents, 
		&__uuidof(ISECommandEvents), &LIBID_AsmLocLib, 
		XCommandEvents, &CLSID_CommandEvents>
	{
	public:
		// ISECommandEvents methods
		STDMETHOD(raw_Activate)   ( void );

		STDMETHOD(raw_Deactivate) ( void );

		STDMETHOD(raw_Terminate)  ( void );

		STDMETHOD(raw_Idle)       ( long lCount, 
									VARIANT_BOOL* pbMore );

		STDMETHOD(raw_KeyDown)    ( short* KeyCode,
									short Shift );

		STDMETHOD(raw_KeyPress)   ( short* KeyAscii );

		STDMETHOD(raw_KeyUp)      ( short* KeyCode,
									short Shift );
	};
	typedef CComObject<XCommandEvents> XCommandEventsObj;
	XCommandEventsObj* m_pCommandEventsObj;

	// Now derive a class from XEventHandler to handle the ISEMouseEvents interface.
	class XMouseEvents : public XEventHandler<ISEMouseEvents, 
		&__uuidof(ISEMouseEvents), &LIBID_AsmLocLib, 
		XMouseEvents, &CLSID_MouseEvents>
	{
	public:
		// ISEMouseEvents methods
		STDMETHOD(raw_MouseDown)		( short sButton,
										  short sShift,
										  double dX,
										  double dY,
										  double dZ,
										  LPDISPATCH pWindowDispatch,
										  long lKeyPointType,
										  LPDISPATCH pGraphicDispatch );

		STDMETHOD(raw_MouseUp)			( short sButton,
										  short sShift,
										  double dX,
										  double dY,
										  double dZ,
										  LPDISPATCH pWindowDispatch,
										  long lKeyPointType,
										  LPDISPATCH pGraphicDispatch );

		STDMETHOD(raw_MouseMove)		( short sButton,
										  short sShift,
										  double dX,
										  double dY,
										  double dZ,
										  LPDISPATCH pWindowDispatch,
										  long lKeyPointType,
										  LPDISPATCH pGraphicDispatch );

		STDMETHOD(raw_MouseClick)		( short sButton,
										  short sShift,
										  double dX,
										  double dY,
										  double dZ,
										  LPDISPATCH pWindowDispatch,
										  long lKeyPointType,
										  LPDISPATCH pGraphicDispatch );

		STDMETHOD(raw_MouseDblClick)	( short sButton,
										  short sShift,
										  double dX,
										  double dY,
										  double dZ,
										  LPDISPATCH pWindowDispatch,
										  long lKeyPointType,
										  LPDISPATCH pGraphicDispatch );

		STDMETHOD(raw_MouseDrag)		( short sButton,
										  short sShift,
										  double dX,
										  double dY,
										  double dZ,
										  LPDISPATCH pWindowDispatch,
										  short DragState,
										  long lKeyPointType,
										  LPDISPATCH pGraphicDispatch );
	};
	typedef CComObject<XMouseEvents> XMouseEventsObj;
	XMouseEventsObj* m_pMouseEventsObj;

	// Now derive a class from XEventHandler to handle the ISELocateFilterEvents interface.
	class XLocateFilterEvents : public XEventHandler<ISELocateFilterEvents, 
		&__uuidof(ISELocateFilterEvents), &LIBID_AsmLocLib, 
		XLocateFilterEvents, &CLSID_LocateFilterEvents>
	{
	public:
		// ISEMouseEvents methods
		STDMETHOD(raw_Filter)         ( LPDISPATCH pGraphicDispatch,
										VARIANT_BOOL *vbValid );

	};
	typedef CComObject<XLocateFilterEvents> XLocateFilterEventsObj;
	XLocateFilterEventsObj* m_pLocateFilterEventsObj;

	// Now derive a class from XEventHandler to handle the ISECommandWindowEvents interface.
	class XWindowEvents : public XEventHandler<ISECommandWindowEvents, 
		&__uuidof(ISECommandWindowEvents), &LIBID_AsmLocLib, 
		XWindowEvents, &CLSID_CommandWindowEvents>
	{
	public:
	// ISEWindowEvents methods
    STDMETHOD(raw_WindowProc) ( LPDISPATCH pUnkDoc,
                                LPDISPATCH pUnkView,
                                UINT nMsg,
                                WPARAM wParam,
                                LPARAM lParam,
                                LRESULT *lResult );
	};
	typedef CComObject<XWindowEvents> XWindowEventsObj;
	XWindowEventsObj* m_pWindowEventsObj;

	// Wow. That's a lot of obtuse template definitions. Now to make life easier for writing an
	// individual command, this base class will define analogous virutal methods that the
	// templatized objects will use (via that m_pCommand back pointer).

	// Member functions below are called by the embedded event handlers whenever Solid Edge fires
	// an event to one of the handlers. Override the functions below that your CCommand derived
	// class cares to respond to. Default implementations provided for free.

	// The command control events.
	STDMETHOD(Activate)  ( void )                   {return S_OK;}

	STDMETHOD(Deactivate)( void )                   {return S_OK;}

	STDMETHOD(Terminate) ( void )                   {return S_OK;}

	STDMETHOD(Idle)      ( long lCount,
						   VARIANT_BOOL* pbMore )  { *pbMore = VARIANT_FALSE; return S_OK;};

	STDMETHOD(KeyDown)   ( short* KeyCode,
						   short Shift )            {return S_OK;} 

	STDMETHOD(KeyPress)  ( short* KeyAscii )        {return S_OK;}

	STDMETHOD(KeyUp)     ( short* KeyCode,
						   short Shift )            {return S_OK;}


	// The mouse control events
	STDMETHOD(MouseDown)    ( short sButton,
							  short sShift,
							  double dX,
							  double dY,
							  double dZ,
							  LPDISPATCH pWindowDispatch,
							  long lKeyPointType,
							  LPDISPATCH pGraphicDispatch ) {return S_OK;}

	STDMETHOD(MouseUp)      ( short sButton,
							  short sShift,
							  double dX,
							  double dY,
							  double dZ,
							  LPDISPATCH pWindowDispatch,
							  long lKeyPointType,
							  LPDISPATCH pGraphicDispatch ) {return S_OK;}

	STDMETHOD(MouseMove)    ( short sButton,
							  short sShift,
							  double dX,
							  double dY,
							  double dZ,
							  LPDISPATCH pWindowDispatch,
							  long lKeyPointType,
							  LPDISPATCH pGraphicDispatch ) {return S_OK;}

	STDMETHOD(MouseClick)   ( short sButton,
							  short sShift,
							  double dX,
							  double dY,
							  double dZ,
							  LPDISPATCH pWindowDispatch,
							  long lKeyPointType,
							  LPDISPATCH pGraphicDispatch ) {return S_OK;}

	STDMETHOD(MouseDblClick)( short sButton,
							  short sShift,
							  double dX,
							  double dY,
							  double dZ,
							  LPDISPATCH pWindowDispatch,
							  long lKeyPointType,
							  LPDISPATCH pGraphicDispatch ) {return S_OK;}

	STDMETHOD(MouseDrag)    ( short sButton,
							  short sShift,
							  double dX,
							  double dY,
							  double dZ,
							  LPDISPATCH pWindowDispatch,
							  short DragState,
							  long lKeyPointType,
							  LPDISPATCH pGraphicDispatch ) {return S_OK;}

	STDMETHOD(WindowProc)   ( LPDISPATCH pDocDispatch,
							  LPDISPATCH pViewDispatch,
							  UINT nMsg,
							  WPARAM wParam,
							  LPARAM lParam,
							  LRESULT *lResult )            {return S_OK;}

	STDMETHOD(Filter)       ( LPDISPATCH pGraphicDispatch,
							  VARIANT_BOOL *vbValid )      {return S_OK;}

	friend class XCommandEvents;
	friend class XMouseEvents;
	friend class XWindowEvents;
	friend class XLocateFilterEvents;

  public:
};

// Use the following typedef when creating an instance of CCommand.
typedef CComObject<CCommand> CCommandObj;
typedef CComAggObject<CCommand> CAggCommandObj;

// This add-in will manipulate the edge bar. I have a dialog I will
// add to the edge bar. The document takes care of the details.
class EdgeBarDlg;
#include "EdgeBarParent.h"
class CEdgeBar;

// Use this class to handle document events.
class ADDINDocument : 
	public CComObjectRoot,
	public CComCoClass<ADDINDocument, &CLSID_ADDINDocument>
{
protected:

	// I store a pointer to the solid edge document.
	IDispatchPtr  m_pDocument;

	// I store a back pointer to the CCommands object. Don't Release()!!
	CCommands* m_pCommands;

	// I store a CEdgeBar object that helps with the creation of the
	// edge bar in Edge.
	CEdgeBar *m_pEdgeBar;

public:
	ADDINDocument();
	~ADDINDocument();

	HRESULT SetDocumentObject     ( LPDISPATCH pDocumentDispatch,
									BOOL bWithEvents = TRUE );
	IDispatchPtr GetDocument  ( void ) { return m_pDocument; }

	HRESULT UnadviseFromEvents ( void );
	HRESULT AdviseEvents ( void );

	void SetCommandsObject( CCommands* pCommands ) { ASSERT( pCommands ); m_pCommands = pCommands; }
	CCommands* GetCommandsObject()                 { ASSERT( m_pCommands ); return m_pCommands; }

	// Sending in (default) NULL for the Set functions will cause any current one to
	// be deleted.

	// Some helper methods.
	HRESULT CreateEdgeBar();
	HRESULT RemoveEdgeBarPage();
	void DestroyEdgeBar();

	BEGIN_COM_MAP(ADDINDocument)
	END_COM_MAP()
	DECLARE_NOT_AGGREGATABLE(ADDINDocument)


protected:

	// This class template is used as the base class for the
	// event handler objects which are declared below.

	template <class IEvents, const IID* piidEvents, const GUID* plibid,
	class XEvents, const CLSID* pClsidEvents>
	class XEventHandler :
		public CComDualImpl<IEvents, piidEvents, plibid>,
		public CComObjectRoot,
		public CComCoClass<XEvents, pClsidEvents>
	{
	public:
		XEventHandler() { m_pDocument = NULL; }
		BEGIN_COM_MAP(XEvents)
			COM_INTERFACE_ENTRY_IID(*piidEvents, IEvents)
		END_COM_MAP()
		DECLARE_NOT_AGGREGATABLE(XEvents)
		HRESULT Connect(IUnknown* pUnk)
		{ HRESULT hr; VERIFY(SUCCEEDED(hr = AtlAdvise(pUnk, this, *piidEvents, &m_dwAdvise))); return hr; }
		HRESULT Disconnect(IUnknown* pUnk)
		{ HRESULT hr; VERIFY(SUCCEEDED(hr = AtlUnadvise(pUnk, *piidEvents, m_dwAdvise))); return hr; }

		// Store a back pointer to the embedding doc object (makes life easy).
		// Do not delete! Its merely a back pointer to the object that controls the
		// lifetime of your event handlers. Use this pointer in the event handling methods 
		// based on this object template to communicate to the embedding class.

		ADDINDocument* m_pDocument;

	protected:
		DWORD m_dwAdvise;
	};

	// Now derive a class from XEventHandler to handle the ISEDocumentEvents interface.
	class XDocumentEvents : public XEventHandler<ISEDocumentEvents, 
		&__uuidof(ISEDocumentEvents), &LIBID_AsmLocLib, 
		XDocumentEvents, &CLSID_ADDINDocumentEvents>
	{
	public:
		// ISEDocumentEvents methods
		STDMETHOD (raw_BeforeClose)       ( void );

		STDMETHOD (raw_BeforeSave)        ( void );

		STDMETHOD (raw_AfterSave)         ( void );

		STDMETHOD (raw_SelectSetChanged)  ( LPDISPATCH pSelectSet );
	};
	typedef CComObject<XDocumentEvents> XDocumentEventsObj;
	XDocumentEventsObj* m_pDocumentEventsObj;

	// I am not bothering to declare analogous methods like I did for the CCommands
	// object. Might be useful though if you derive different add-in documents from
	// a base (this?) class.
public:

	friend class XDocumentEvents;
};
//{{AFX_INSERT_LOCATION}}
// Microsoft Developer Studio will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_COMMANDS_H__03B8616A_1896_11D1_BA18_080036230602__INCLUDED)
