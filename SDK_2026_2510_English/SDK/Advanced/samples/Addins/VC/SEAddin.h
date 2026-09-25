// SEAddin.h : Declaration of the CSEAddin

#ifndef __SEADDIN_H_
#define __SEADDIN_H_

#include "resource.h"       // main symbols
#include "commands.h"

using namespace SolidEdgeFramework;

#define COM_TYPELIBINTERFACE_ENTRY(x)\
	{&__uuidof(x), \
	offsetofclass(x, _ComMapClass), \
	_ATL_SIMPLEMAPENTRY},

// Define the GUID of this sample add-in. If you are copying this sample and
// creating your own real add-in, you REALLY NEED TO CHANGE THE GUID. Doing
// so will avoid any issue with clashing GUIDS on the same machine. Use
// the guidgen tool in the Visual Studio Tools menu to generate a guid. Simply
// changing some value below by hand will not guarantee uniqueness!!
DEFINE_GUID(CLSID_SEAddIn, 
0x6D4144EA,0x2FC2,0x11D3, 0x92,0x76,0x00,0xC0,0x4F,0x79,0xBE,0x98);

// Define the class that starts it all. This is the object that implements the
// ISolidEdgeAddIn interface. Edge will detect this COM class using the Microsoft
// registry APIs ( in particular EnumClassesOfCategories) because the add-in will
// register itself as implementing ISolidEdgeAddIn. 
/////////////////////////////////////////////////////////////////////////////
// CSEAddIn
class CSEAddIn :
	public ISolidEdgeAddIn,
	public CComObjectRoot,
	public ISupportErrorInfo,
	public IDropTarget,
	public CComCoClass<CSEAddIn, &CLSID_SEAddIn>
{
public:
	CSEAddIn()
	{
		m_pCommands = NULL;
	}

	~CSEAddIn()
	{
		m_pCommands = NULL;
	}
	DECLARE_REGISTRY_RESOURCEID(IDR_ASMLOC)

	BEGIN_COM_MAP(CSEAddIn)
		COM_TYPELIBINTERFACE_ENTRY(ISolidEdgeAddIn)
		COM_INTERFACE_ENTRY(ISupportErrorInfo)
		COM_INTERFACE_ENTRY(IDropTarget)
	END_COM_MAP()

	BEGIN_CONNECTION_POINT_MAP(CSEAddIn)
	END_CONNECTION_POINT_MAP()


	// ISupportsErrorInfo
	STDMETHOD(InterfaceSupportsErrorInfo)(REFIID riid);

	// ISolidEdgeAddin
	STDMETHOD(raw_OnConnection)( THIS_ IDispatch* pAppDispatch, SeConnectMode ConnectMode, SolidEdgeFramework::AddIn* pUnkAddIn );
	STDMETHOD(raw_OnConnectToEnvironment)( BSTR EnvironmentCatid, LPDISPATCH pEnvironment, VARIANT_BOOL bFirstTime );
	STDMETHOD(raw_OnDisconnection)( THIS_ SeDisconnectMode DisconnectMode );
	
	// IDropTarget
	STDMETHOD(DragEnter)(LPDATAOBJECT, DWORD, POINTL, LPDWORD);
	STDMETHOD(DragOver)(DWORD, POINTL, LPDWORD);
	STDMETHOD(DragLeave)();
	STDMETHOD(Drop)(LPDATAOBJECT, DWORD, POINTL pt, LPDWORD);

public:

protected:
	CCommandsObj* m_pCommands;
};

#endif //__SEADDIN_H_
