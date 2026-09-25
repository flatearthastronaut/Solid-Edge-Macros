// samplloc.cpp : implementation file
//

#include "stdafx.h"
#include "Commands.h"
#include "resource.h"
#include "locate.h"
#include "util.h"
#include "myoptions.h"


#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

bool EdgeVersionSupportsAlphaImageOnCommandBar()
{
	bool bRC = false;

	DWORD dwMajor = 0;
	DWORD dwMinor = 0;

	GetSolidEdgeVersion( dwMajor, dwMinor, GetApplicationPtr() );

	// The new add-in enhancements were added to ST2 but back-ported to ST MP7.
	// However, the change to allow alpha images on the command bar were made
	// later and only exist in ST2.
	if( dwMajor > 100 )
	{
		bRC = true;
	}

	return true;
}
//using namespace SolidEdgeConstants;

CSampleLocateCommand::CSampleLocateCommand() : CCommand()
{
	m_hWndRibbon = NULL;

	// Set up some default values for the two edit fields on the command's ribbon bar. These values will
	// be converted to database units using the UnitsOfMeasure interface when the ribbon bar is initialized.
	m_bstrLengthValue = "1.0";
	m_dLength = 1.0;

	m_bstrAngleValue = "180.0";
	m_dAngle = 180.0;
}

CSampleLocateCommand::~CSampleLocateCommand()
{
}

HRESULT CSampleLocateCommand::CreateRibbon()
{
	HRESULT hr = S_OK;

	// Obtain the ribbon bar interface from the Solid Edge command object. Failure to get the ribbon interface would most
	// likely be due to running with an earlier version of Solid Edge. Ribbon bars became available with version 9 of Solid
	// Edge.

	// With ST, Edge moved from the command ribbon UI to a "task panel" UI that resides in a docking pane. Edge will convert
	// dialogs from the horizontal layout required by the old ribbon UI to a vertical layout. Conversion is not always
	// the most desirable but for legacy add-ins, it usually works fairly well.

	ISolidEdgeRibbonBarPtr pRibbonBar = GetCommand();

	if( NULL == m_hWndRibbon )
	{
		// This is the first call to create the ribbon bar. Add a dialog box to the ribbon bar.
		// Note that in Solid Edge, the dialog box that is added to the ribbon bar should be 21 
		// dialog units high. Solid Edge takes care of resizing the dialog box whenever the user
		// switches between large and small buttons.

		if( NULL != pRibbonBar )
		{
			// When examing this sample, take a good look at the dialog box using the resource editor.
			// In particular, notice that in order to maintain good spacing, I have pulled a couple of
			// tricks in the design of the sample dialog box. One is to use a picture control with small 
			// spacing (3 dialog units wide) to give the appearance of separators. Another is to add
			// some static text boxes between some of the controls. That includes some static text boxes 
			// before and after the picture controls. Last but not least, I added an extra space character 
			// before the lables for the two edit fields. The reason for the static text boxes (which 
			// actually contain no text) and the extra spaces in the labels? The Solid Edge ribbon bar
			// holder moves the position of the controls around to optimize space usage on the dialog box.
			// This can result in too little spacing between the controls (IMHO).

			ISolidEdgeRibbonBarExPtr pRibbonbarEx = pRibbonBar;
			if( NULL != pRibbonbarEx )
			{
				TCHAR ResourceFilename[MAX_PATH];

				GetModuleFileName( hMyInstance(), ResourceFilename, sizeof( ResourceFilename ) );

				m_hWndRibbon = (HWND) pRibbonbarEx->AddRibbonEx( IDD_DIALOG1, ResourceFilename );
			}
			else
			{
				m_hWndRibbon = (HWND) pRibbonBar->AddRibbon( IDD_DIALOG1, (long) (hMyInstance()) );
			}

			if( NULL != m_hWndRibbon )  // This should not happen, but then again this is software ...
			{
				// We'll add keyboard accelerators to this command's ribbon bar. Ribbon bar accelerators are single 
				// characters. The command's WindowProc will receive seWM_ACCELERATORSELECTED messages with the character 
				// passed in the wParam.
				// In order to receive the message, the dialog box itself must have focus. Unfortunately
				// with the new UI in ST, when a dialog is inserted into the command bar/task panel, the
				// dialog only has focus if the user has (or the software) has set focus on a control in
				// the dialog box. For the user, this means clicking some control on the dialog. For some
				// odd reason, if the user clicks on the dialog but doesn't click on a control, the dialog
				// itself doesn't have focus! So these accelerators are not as useful as they used to be. Of
				// course there is still the key down command event. In any case, click one of the controls
				// like the check box and then hit the 'o' or 'f' key and the windowproc will be called
				// accordingly. This focus issue is due to the new UI and is, unfortunately, not controlled by
				// the Edge programmers (as of the time I am writing this).

				SAFEARRAY *Accelerators;  
				SAFEARRAYBOUND AcceleratorsBound;

				AcceleratorsBound.lLbound = 0;
				AcceleratorsBound.cElements = 2; // In this sample, ok and cancel buttons will be accelerated

				char x = 'o'; // 'o' for "Ok" button 

				Accelerators = SafeArrayCreate(VT_I1, 1, &AcceleratorsBound );

				long index = 0;
				hr = SafeArrayPutElement(Accelerators, &index, (void*)&x);

				x = 'f';      // 'f' for "Finish" button

				index = 1;
				hr = SafeArrayPutElement(Accelerators, &index, (void*)&x);

				// Inform the ribbon bar that we have accelerators.
				pRibbonBar->SetAccelerators( &Accelerators );

				SafeArrayDestroy( Accelerators );

				// Adding "edit" fields to the ribbon bar results in a list the ribbon bar holder maintains
				// which can be used to set/move focus among the controls in the list programatically by calling 
				// NextFocus. In this sample, the focus will move between two of the edit controls whenever the 
				// user has the focus in one of the two fields and then hits the "return" (a.k.a. "enter") key. 
				// See the KeyDown method for the details of how this is done.

				pRibbonBar->AddEditField( IDS_LENGTH );
				pRibbonBar->AddEditField( IDS_ANGLE );

				//  Make life easier. Get a CWnd from the ribbon window handle.
				CWnd *pWnd = CWnd::FromHandle( m_hWndRibbon );

				if( NULL != pWnd )
				{
					// Set up a default value in the length and angle fields to be one database distance/angle unit each

					// This sample addin only runs in Part ...
					SolidEdgeDocumentPtr pDoc;

					DocumentTypeConstants nDocType = igUnknownDocument;
					_bstr_t bstrDocName; // empty string will cause GetDocPtr to retrieve the active document
					GetDocPtr( bstrDocName, pDoc, nDocType );

					if( NULL != pDoc )
					{
						UnitsOfMeasurePtr pUom;

						try
						{
							pUom = pDoc->UnitsOfMeasure;
						}
						catch( _com_error &e )
						{
							// I'll simply ignore this error and leave the edit fields blank.
							hr = e.Error();
						}

						if( NULL != pUom )
						{
							CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDS_ANGLE );

							if( NULL != pEdit )
							{
								// Let the UOM object convert my angle to data base units.
								_variant_t vt = pUom->ParseUnit( igUnitAngle, m_bstrAngleValue );

								// Now let it format that unit for display.
								m_bstrAngleValue = pUom->FormatUnit( igUnitAngle, vt );

								m_dAngle = vt;  // _variant_t objects make life so much easier

								// Now let's display the angle in the edit field
								pEdit->SetWindowText( m_bstrAngleValue );
							}

							pEdit = (CEdit *)pWnd->GetDlgItem( IDS_LENGTH );

							if( NULL != pEdit )
							{
								// Let the UOM object convert my length to data base units.
								_variant_t vt = pUom->ParseUnit( igUnitDistance, m_bstrLengthValue );

								// Now let it format that unit for display.
								m_bstrLengthValue = pUom->FormatUnit( igUnitDistance, vt );

								m_dLength = vt;

								// Now let's display the length in the edit field
								pEdit->SetWindowText( m_bstrLengthValue );
							}

							pEdit->EnableWindow(TRUE);
						}
					}

					CButton* pButton = (CButton*)pWnd->GetDlgItem( IDC_RADIO1 );
					if( pButton )
					{
						pButton->SetCheck(1);
					}
					// This sample has a ribbon that contains a combo box. Seed the box with some really catchy text.
					CComboBox* pComboBox = (CComboBox *)pWnd->GetDlgItem(IDS_COMBO1);

					if( NULL != pComboBox )
					{
						pComboBox->InsertString(0, _T("Solid") );
						pComboBox->InsertString(1, _T("Edge") );
						pComboBox->InsertString(2, _T("Rules!") ); // So let it be written, so let it be done.

						// Now set the current selection.
						pComboBox->SetCurSel(0);

						pComboBox->EnableWindow(TRUE);
					}

					// This sample has a ribbon that contains a combo box. Seed the box with some really catchy text.
					CListBox* pListBox = (CListBox *)pWnd->GetDlgItem(IDS_LIST1);

					if( NULL != pListBox )
					{
						pListBox->InsertString(0, _T("Go") );
						pListBox->InsertString(1, _T("Big") );
						pListBox->InsertString(2, _T("Orange!") );  // As in the University of Tennessee, of course.

						// Now set the current selection.
						pListBox->SetCurSel(0);

						pListBox->EnableWindow(TRUE);
					}

					CWnd* pSlider = (CWnd*)pWnd->GetDlgItem( IDC_SLIDER1 );

					pSlider->EnableWindow(TRUE);

					// Since the addin command created the ribbon bar after the command started, a specific
					// call must be made to show the ribbon. Note: If you added the ribbon before calling the Start
					// function of the command, you don't need to call ShowRibbon.
					pRibbonBar->ShowRibbon( );

					// Make sure focus is set on the ribbon bar.
					// Do this since accelerators are being used. Or is it because Solid Edge
					// can actually display two ribbon bars (see for example, the SketchPoint
					// tool in the Draft environment).
					pRibbonBar->SetCurrentFocus();
				}
			}
		}
	}
	else
	{
		if( NULL != pRibbonBar )
		{
			// Make sure my ribbon bar is the one with the focus. If you are running in Draft, and the user has started the
			// SketchPoint command, you would need to override CCommand::Idle and set the focus there. The reason for that
			// is the user can change focus to the SketchPoint ribbon bar and then move back to the draft window. At that time,
			// the command would start to receive Idle calls. Meanwhile, the ribbon with the focus would still be the 
			// SketchPoint ribbon. That is probably not what a command would want, especially if the ribbon bar for the
			// command has installed accelerators.

			pRibbonBar->SetCurrentFocus();
		}
	}

	return hr;
}

HRESULT CSampleLocateCommand::Activate()
{
	using namespace SolidEdgeConstants;

	HRESULT hr = S_OK;

	try
	{
		// Set up the mouse object. Getting the mouse setting correct can be a real
		// pain in the app. The type of environment can affect the mouse and of course
		// valid filters depend on what env the command is running in. For instance,
		// adding seLocateFace in a draft/drawing env doesn't make sense (but I can still
		// set the filter with no error). Meanwhile, while in say the Part env, using
		// seDynamicsRectangle doesn't work but does in draft.
		if( m_pSEMouse )
		{
			m_pSEMouse->put_ScaleMode( 1 );                // Design model coordinates.
			m_pSEMouse->put_WindowTypes( 1 );              // Graphic window's only please.

			// Enable the drag mode to get Edge to fire the MouseDrag event. This can
			// be useful for fence locate in draft or 2d (classic) part and asm environments.
			// See the code in MouseDrag to see how it can work.
			m_pSEMouse->put_EnabledDrag( VARIANT_TRUE );
			m_pSEMouse->put_EnabledMove( VARIANT_TRUE );   // Request move events.

			_bstr_t strFilename; // empty so I get the active doc.

			DraftDocumentPtr pDraftDoc;
			hr = GetDocPtr( strFilename, pDraftDoc );

			if( NULL == pDraftDoc )
			{
				m_pSEMouse->put_LocateMode( seLocateQuickPick );
			}
			else
			{
				// Note: Using seSmartLocate in part won't let me locate faces and edges. I said
				// this mouse was a pain in the App. I'm sure all this is documented in the
				// programmers reference guide. I'm just coding up what I see works.

				// Note: In order to obtain located objects during move events, set your locate
				//       filter to seSmartLocate.
				m_pSEMouse->put_LocateMode( seSmartLocate );
			}
			//m_pSEMouse->put_LocateMode(seSmartLocate);
			//m_pSEMouse->put_LocateMode( seLocateSimple );

			// Set up a few commonly located types in non-draft files.
			m_pSEMouse->AddToLocateFilter( seLocateFace );		//Need to use both interaces combined
			//m_pSEMouse->AddToLocateFilter(seLocateMeshFacet); //Need to use both interaces combined

			m_pSEMouse->AddToLocateFilter( seLocateEdge );
			m_pSEMouse->AddToLocateFilter( seLocateVertex );

			// Some other filter settings that can be used.
			//m_pSEMouse->AddToLocateFilter( seLocateRefPlane );
			//m_pSEMouse->AddToLocateFilter( seLocatePart );		
			//m_pSEMouse->AddToLocateFilter( seLocateCone );
			//m_pSEMouse->AddToLocateFilter( seLocateSphere );
			//m_pSEMouse->AddToLocateFilter( seLocateTorus );

			// Set up a few commonly located types in draft or part/assembly sketching environments.
			m_pSEMouse->AddToLocateFilter( seLocateLine2d );
			m_pSEMouse->AddToLocateFilter( seLocateCircle2d );
			//m_pSEMouse->AddToLocateFilter(seLocateCoordinateSystem);

			// Some other filter settings that can be used.
			//m_pSEMouse->AddToLocateFilter( seLocateLWRLine2d );

			// I believe this mode is for draft files. Edge will draw a rectangle for you.
			//m_pSEMouse->PutDynamicsMode( seDynamicsRectangle );

			// If you need to do inter document locates, Edge started supporting that in version ...
			// some version that I fail to recall.
			//m_pSEMouse->PutInterDocumentLocate(VARIANT_TRUE);

			// New for ST8. The mouse supports a locate "front to back", which is an ordering of located elements along the bore line.
			// For compatibility, a new interface was added. So if I can get the interface, I set the front to back locate mode.
			ISEMouseEx2Ptr pM2 = m_pSEMouse;
			
			VARIANT_BOOL vbFrontToBack = VARIANT_TRUE;

			if( pM2 )
			{
				pM2->PutLocateFrontToBack( vbFrontToBack );
			}
		}

		CreateRibbon();
	}
	catch( _com_error &e )
	{
		hr = e.Error();
	}

	return hr;
}

// With older versions of Visual Studio, Microsoft manged to create every smart pointer based on
// the IDispatch interface instead of the acutual dispatch interface Edge defined that derived
// from IDispatch. That is, in the MS definition of the smart pointers, each one had 
// "__uuidof(IDispatch)", essentially making the smart pointers useless when it comes to
// identification (and most everything else). We quickly pointed that out to Microsoft and they actually 
// responded by making the suggested change, which you see below. I don't recall which version of Visual 
// Studio the bug was fixed in (6 perhaps). So I'm leaving these here but if you are using Visual Studio 
// 2005 or 2008 you will want to just rely on the compiler to import the typelib and do the right thing.
// Since I am using Visual Studio 2008, I no longer declare, for instance "::Circle2dPtr" and instead
// just use Circle2dPtr. All one has to do to see if they have the "fix" is to open any .tlh file
// and search for "_COM_SMARTPTR_TYPEDEF" and see if the uuidof has IDispatch or not.

//_COM_SMARTPTR_TYPEDEF(Line2d, __uuidof(Line2d));
//_COM_SMARTPTR_TYPEDEF(Circle2d, __uuidof(Circle2d));
//_COM_SMARTPTR_TYPEDEF(Occurrence, __uuidof(Occurrence));

// I am getting the reference key for some of the objects I locate. So I will just store the data
// here. Normally one would store the key (perhaps in add-in storage) and use it to try and bind
// back to the object at a later time. If the binding fails, the topology no longer exists.
SAFEARRAY *pRefKey = 0;

HRESULT CSampleLocateCommand::MouseClick(short sButton, short sShift, double dX, double dY, double dZ, LPDISPATCH pWindowDispatch, long lKeyPointType, LPDISPATCH pGraphicDispatch)
{
	HRESULT hr = NOERROR;

	int nObjectType;

	LPUNKNOWN pObjectUnk = NULL;

	hr = GetLocatedGraphicTypeFromReference( pGraphicDispatch, nObjectType, &pObjectUnk );

	if( SUCCEEDED(hr) )
	{
		long PointOnGraphicFlag = 0;
		double POGx = 0, POGy = 0, POGz = 0;

		try
		{
			// Get the actual point on the graphic element (in database coordinates). Note this is not
			// the same as the input dX, dY, dZ coordinates. Those are either in window coordinates
			// or are the window coordinates transformed to data base coordinates (depends on the
			// value of ScaleMode). That is, the inputs to this routine are NOT the intersection of the 
			// bore line and the element.
			m_pSEMouse->PointOnGraphic( &PointOnGraphicFlag, &POGx, &POGy, &POGz );
			CString strObjType(_T("unknown"));

			switch (nObjectType)
			{
				case SolidEdgeGeometry::igFace:		{ strObjType = "FACE"; break; }
				case SolidEdgeGeometry::igEdge:		{ strObjType = "EDGE"; break; }
				case SolidEdgeGeometry::igVertex:	{ strObjType = "VERTEX"; break; }
			}

			CString msg;
			msg.Format(_T("PointOnGraphic(%s): X = %3.2lf mm, Y = %3.2lf mm, (PickedPoint: X=%3.2lf mm, Y=%3.2lf mm)"), strObjType, POGx*1000., POGy*1000., dX*1000., dY*1000.);
			ApplicationPtr	pApp = m_pCommands->GetApplicationPtr();
			pApp->PutActiveStatusBarPart(1L);
			pApp->PutStatusBar((LPCTSTR)msg);
		}
		catch( _com_error &e )
		{
			hr = e.Error ();
		}

		try
		{
			// Note: Locating cones, cylinders, etc. always results in the located object type
			// as a face (which is what they are, topologically speaking). In order to determine
			// if the face was a cone, cylinder, etc., See what interface the face supports.

			OccurrencePtr pOccurrence = pObjectUnk;
			ConePtr pCone = pObjectUnk;
			CylinderPtr pCylinder = pObjectUnk;
			SpherePtr pSphere = pObjectUnk;
			Circle2dPtr pCircle2d = pObjectUnk;

			VertexPtr pVertex = pObjectUnk;
			void SetRefKey( SAFEARRAY * pRefKey );

			SAFEARRAY *Point = NULL;

			if( NULL != pVertex )
			{
				SAFEARRAYBOUND PointBound;

				PointBound.lLbound = 0;
				PointBound.cElements = 3;

				Point = SafeArrayCreate(VT_R8, 1, &PointBound );

				pVertex->GetPointData( &Point );

				double Coord = 0.0;

				for( long index = 0; index < 3; ++index )
				{
					hr = SafeArrayGetElement(Point, &index, &Coord);
				}
			}

			if( Point )
			{
				SafeArrayDestroy( Point );
				Point = NULL;
			}

			switch (nObjectType)
			{
				case SolidEdgeGeometry::igFace:
				{
					AfxMessageBox( _T("Located a face") );

					FacePtr pFace = pObjectUnk;

					pFace->GetReferenceKey( &pRefKey );

					SetRefKey( pRefKey );

					break;
				}
				case SolidEdgeGeometry::igEdge:
				{
					EdgePtr pEdge = pObjectUnk;

					pEdge->GetReferenceKey( &pRefKey );

					SetRefKey( pRefKey );

					::CoordinateSystemPtr pCS = pEdge->Parent;

					if( NULL != pCS )
					{
						AfxMessageBox(_T("Edge belongs to a CS"));
					}
					else
					{
						int nObjectType;

						LPUNKNOWN pObjectUnk = NULL;

						hr = GetLocatedGraphicTypeFromReference( pEdge->Parent, nObjectType, &pObjectUnk );

						if( SUCCEEDED(hr) )
						{
							CString strMsg;
							strMsg.Format(_T("Edge parent has type of %ld"), nObjectType);

							AfxMessageBox(strMsg);
						}
					}

					break;
				}
				case SolidEdgeFramework::igLine2d:
				{
					AfxMessageBox( _T("Located a 2d line") );

					break;
				}
				default:
				{
					break;
				}
			}

			if( NULL != pCone )
			{
				AfxMessageBox( _T("Located a cone.") );
			}
			if( NULL != pCylinder )
			{
				AfxMessageBox( _T("Located a cylinder.") );
			}
			if( NULL != pSphere )
			{
				AfxMessageBox( _T("Located a sphere.") );
			}
			if( NULL != pOccurrence )
			{
				AfxMessageBox( _T("Located an occurrence.") );

				pOccurrence->GetReferenceKey( &pRefKey );

				SetRefKey( pRefKey );
			}

			if( NULL != pCircle2d )
			{
				double dX, dY;

				try
				{
					pCircle2d->GetCenterPoint( &dX, &dY );
					pCircle2d->SetCenterPoint( dX + .01, dY + .01 );
				}
				catch( _com_error &e )
				{
					hr = e.Error ();
				}
				AfxMessageBox( _T("Located a circle2d.") );
			}
		}
		catch( _com_error &e )
		{
			hr = e.Error ();
		}
	}

	return hr;
}

// Create a part with say a cylinder in it and then run one of the two locate commands. Click on the face
// and then continue to move the mouse over the part, notice the data being updated in the status bar. 
// That's about all this add-in's MouseMove does.
HRESULT CSampleLocateCommand::MouseMove(short sButton, short sShift, double dX, double dY, double dZ, LPDISPATCH pWindowDispatch, long lKeyPointType, LPDISPATCH pGraphicDispatch)
{
	HRESULT hr = NOERROR;

	if( pGraphicDispatch )
	{
		int nObjectType;

		LPUNKNOWN pObjectUnk = NULL;

		hr = GetLocatedGraphicTypeFromReference( pGraphicDispatch, nObjectType, &pObjectUnk );

		// Are you getting that deja vu feeling? No? Keep looking.
		if( SUCCEEDED(hr) )
		{
			long PointOnGraphicFlag = 0;
			double POGx = 0, POGy = 0, POGz = 0;

			try
			{
				// Get the actual point on the graphic element (in database coordinates). Note this is not
				// the same as the input dX, dY, dZ coordinates. Those are either in window coordinates
				// or are the window coordinates transformed to data base coordinates (depends on the
				// value of ScaleMode). That is, the inputs to this routine are NOT the intersection of the 
				// bore line and the element. Note that if the user has not clicked the element to actually
				// locate it, the point is not available. But once the element is located (moving over really
				// just highlights the element that is passed into this routine), the point is updated
				// dynamically.
				m_pSEMouse->PointOnGraphic( &PointOnGraphicFlag, &POGx, &POGy, &POGz );

				if( PointOnGraphicFlag )
				{
					CString strObjType(_T("unknown"));
					switch (nObjectType)
					{
						case SolidEdgeGeometry::igFace:		{ strObjType = _T("FACE"); break; }
						case SolidEdgeGeometry::igEdge:		{ strObjType = _T("EDGE"); break; }
						case SolidEdgeGeometry::igVertex:	{ strObjType = _T("VERTEX"); break; }
					}
					CString msg;
					msg.Format(_T("PointOnGraphic(%s): X = %3.2lf mm, Y = %3.2lf mm, (PickedPoint: X=%3.2lf mm, Y=%3.2lf mm)"), strObjType, POGx*1000., POGy*1000, dX*1000., dY*1000.);
					ApplicationPtr	pApp = m_pCommands->GetApplicationPtr();
					pApp->PutActiveStatusBarPart(1L);
					pApp->PutStatusBar((LPCTSTR)msg);
				}
			}
			catch( _com_error &e )
			{
				hr = e.Error ();
			}
			// Note: Locating cones, cylinders, etc. always results in the located object type
			// as a face (which is what they are, topologically speaking). In order to determine
			// if the face was a cone, cylinder, etc., See what interface the face supports.

			ConePtr pCone = pObjectUnk;
			CylinderPtr pCylinder = pObjectUnk;
			SpherePtr pSphere = pObjectUnk;
			Circle2dPtr pCircle2d = pObjectUnk;

			VertexPtr pVertex = pObjectUnk;

			if( NULL != pVertex )
			{
				SAFEARRAY *Point;  
				SAFEARRAYBOUND PointBound;

				PointBound.lLbound = 0;
				PointBound.cElements = 3;

				Point = SafeArrayCreate(VT_I1, 1, &PointBound );

				pVertex->GetPointData( &Point );
			}

			switch (nObjectType)
			{
				case SolidEdgeGeometry::igFace:
				{
					TRACE( "Located a face\n" );

					break;
				}
				case SolidEdgeGeometry::igEdge:
				{
					TRACE( "Located an edge\n" );

					break;
				}
				case SolidEdgeFramework::igLine2d:
				{
					TRACE( "Located a 2d line\n" );

					break;
				}
			}
		}
	}

	return hr;
}

HRESULT CSampleLocateCommand::MouseDrag(short sButton, short sShift, double dX, double dY, double dZ, LPDISPATCH pWindowDispatch, short DragState, long lKeyPointType, LPDISPATCH pGraphicDispatch )
{
	using namespace SolidEdgeConstants;

	HRESULT hr = NOERROR;

	// On Drag state exit get the last down event data. If the data shows the user
	// used the left button, build the select set or add to it if the event included
	// the control key being depressed.

	if( DragState == seMouseExitDrag  &&  NULL != pGraphicDispatch )
	{ 
		short sLastDownEventShift = m_pSEMouse->LastDownEventShift;
		short sLastDownEventButton = m_pSEMouse->LastDownEventButton;

		if( seLEFT == sLastDownEventButton )
		{
			// Run the locate command in draft and do a fence select of multiple elements. Note edge sends in
			// a SelectSet object. The elements in the set are alreay filtered. For example, this command
			// sets lines and circles in the filter calls but not arcs. Place a line, circle and arc and
			// run the command. Then fence select all three elements and note the count of elements in the
			// select set will be two and the arc will be the odd element out.
			::SelectSetPtr pSelectSet;

			if( NULL != pGraphicDispatch )
			{
				pSelectSet = pGraphicDispatch;
			}

			// If the user fences more than one element, Edge will send in a SelectSet. Otherwise the
			// dispatch is for the only element in the fence (no SelectSet created). Of course, there
			// is nothing that forces the user to fence anything. This event handler will still be called
			// but no element will be passed in.
			if( NULL != pSelectSet )
			{
				long lCount = pSelectSet->Count;

				IDispatchPtr pElementDispatch;

				for( long Index = 1; Index <= lCount; ++Index )
				{
					pElementDispatch = pSelectSet->Item( Index );

					// What do I have? Should be a line or an arc if you are testing the claim in the comment
					// above. Use two smart pointers. Whichever one is not NULL is the indicator of what you
					// have for this item.
					Line2dPtr pLine = pElementDispatch;

					Circle2dPtr pCircle = pElementDispatch;

					if( NULL != pLine )
					{
						TRACE("Input select set contains a line\n");
					}
					else if( NULL != pCircle )
					{
						TRACE("Input select set contains a circle\n");
					}
				}
			}
			else
			{
				// I'm only interested in demonstrating line and circle locate in draft/sketch environments.
				Line2dPtr pLine = pGraphicDispatch;

				Circle2dPtr pCircle = pGraphicDispatch;

				if( NULL != pLine )
				{
					TRACE("Input select set contains a line\n");
				}
				else if( NULL != pCircle )
				{
					TRACE("Input select set contains a circle\n");
				}
			}
		}
	}

	return hr;
}

static int s_Valid = 1; // Delcare in function and VC++ debugger can't be used to change value when break pt is hit!
HRESULT CSampleLocateCommand::Filter(LPDISPATCH pGraphicDispatch, VARIANT_BOOL *vbValid)
{
	// Demonstrate how an add-in can influence what the user locates. 
	HRESULT hr = NOERROR;

	if( s_Valid )
	{
		*vbValid = VARIANT_TRUE;
	}
	else
	{
		*vbValid = VARIANT_FALSE;
	}

	return hr;
}

// The KeyDown event is handles simply to show some of the ways the ribbon bar can be
// manipulated.

// The keys and their uses are as follows:
//
//  'r' - Remove the command bar entries (i.e., delete it)
//  'a' - Add the command bar entries
//  'h' - Hide the command bar group two entry
//  's' - Show the command bar group two entry
//  'g' - Set focus on the command bar group one entry
//  'v' - Ensure that the command bar group two entry is visible.
//  return/enter - Move focus among the edit fields defined in the ISolidEdgeRibbonBar::AddEditField call.

HRESULT CSampleLocateCommand::KeyDown(short* KeyCode, short Shift)
{
	TRACE("Key down event\n");

	ISolidEdgeRibbonBarPtr pRibbonBar;

	pRibbonBar = GetCommand();

	if( NULL != pRibbonBar )
	{
		// If the user is trying to key in "rad" for an angle, I don't
		// want to process the key too! In fact, don't process any key
		// if one of my edit controls has focus.
		bool bSkip = false;

		HWND hWndFocus = ::GetFocus();
		if( m_hWndRibbon )
		{
			CWnd *pWnd = CWnd::FromHandle(  m_hWndRibbon );

			if( pWnd )
			{
				CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDS_ANGLE );

				if( pEdit && hWndFocus == pEdit->GetSafeHwnd() )
				{
					bSkip = true;
				}
				else
				{
					CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDS_LENGTH );
				
					if( pEdit && hWndFocus == pEdit->GetSafeHwnd() )
					{
						bSkip = true;
					}
					else
					{
						CListBox* pList = (CListBox *)pWnd->GetDlgItem( IDS_LIST1 );
						
						if( pList && hWndFocus == pList->GetSafeHwnd() )
						{
							bSkip = true;
						}
						else
						{
							CComboBox* pComboBox = (CComboBox *)pWnd->GetDlgItem(IDS_COMBO1);

							if( pComboBox && hWndFocus == pComboBox->GetSafeHwnd() )
							{
								bSkip = true;
							}
						}
					}
				}
			}
		}

		// See if this is the "return/enter" key. If so, see which control has the focus on the
		// ribbon bar. Controls added as an "edit field" using the AddEditField member function
		// of the ribbon bar interface define a focus list. Focus can be moved from one control to
		// another by caling NextFocus. This sample does just that whenever the user hits enter.

		if( 0xd == *KeyCode )
		{
			int id;

			pRibbonBar->GetCurrentFocus(&id);

			// GetCurrentFocus returns -1 if the control with the focus is not in the list of
			// "edit" fields added by calling AddEditField. In that case, I don't bother to
			// move the focus.

			if( -1 != id )
			{
				TRACE("ribbon bar control with foucs is %d\n", id);

				pRibbonBar->NextFocus();
				pRibbonBar->GetCurrentFocus(&id);
			}
		}
		else if( bSkip )
		{
			return S_OK;
		}
		else if( 0x53 == *KeyCode ) // The 's' key
		{
			pRibbonBar->ShowRibbon();
		}
		else if( 0x48 == *KeyCode ) // The 'h' key
		{
			pRibbonBar->HideRibbon();
		}
		else if( 0x52 == *KeyCode ) // The 'r' key
		{
			pRibbonBar->RemoveRibbon();
			m_hWndRibbon = 0;
		}
		else if( 0x41 == *KeyCode ) // The 'a' key
		{
			CreateRibbon();
		}
		else if( 0x47 == *KeyCode ) // The 'g' key
		{
			pRibbonBar->SetCurrentFocus();
		}
	}

	return S_OK;
}

#include "resource.h"

HRESULT CSampleLocateCommand::WindowProc( LPDISPATCH pUnkDoc, LPDISPATCH pUnkView, UINT nMsg, WPARAM wParam, LPARAM lParam, LRESULT *lResult )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	switch( nMsg )
	{
	case WM_COMMAND:
		{
			// Control notifications are usually sent to the command embedded in WM_COMMAND messages. For more information
			// on control notification, see the MSDN. Since Solid Edge creates the window (actually a CDialog) that displays
			// the ribbon, control notifications are sent to Solid Edge which turns around and routes them to this event
			// handler. From there, you are on your own. I mean, its all standard message processing.

			switch ( wParam )
			{
				// Buttons are so simple. Simply look for the button id in wParam and respond accordingly.
			case IDS_OK:
				{
					TRACE( "OK button hit\n" );

					break;
				}

			case IDS_CANCEL:
				{
					TRACE( "CANCEL button hit\n" );
					// Let's be real fancy and kill the command. Did I say "kill"? Not very politically correct is it.
					// What would the PETI (People for the Ethical Treatment of Interfaces) people say?

					GetCommand()->Done = VARIANT_TRUE;

					break;
				}

			case IDC_RADIO1:
				{
					TRACE( "Radio button hit\n" );
					CWnd *pWnd = CWnd::FromHandle( m_hWndRibbon );

					if( NULL != pWnd )
					{
						CButton* pButton = (CButton*)pWnd->GetDlgItem( IDC_RADIO1 );
						if( pButton )
						{
							if( pButton->GetCheck() )
							{
								TRACE("Radio 1 is checked\n");
							}
						}
					}

					break;
				}
			case IDS_BUTTON1:
				{
					TRACE( "Button one hit\n" );

					CWnd *pWnd = CWnd::FromHandle( m_hWndRibbon );

					if( NULL != pWnd )
					{
						CButton* pButton = (CButton*)pWnd->GetDlgItem( IDS_BUTTON1 );
						if( pButton )
						{
							pButton->SetWindowText(_T("B -1"));
						}

						CWnd* pStatic = pWnd->GetDlgItem(IDC_STATIC13);
						if( pStatic )
						{
							pStatic->SetWindowText(_T("RD"));
						}

						CButton* pRadio = (CButton*)pWnd->GetDlgItem( IDC_RADIO1 );
						if( pRadio )
						{
							pRadio->Invalidate(FALSE);
							pRadio->SetWindowText(_T("RadiOO"));
							pRadio->Invalidate();
							pRadio->UpdateWindow();
							pWnd->Invalidate();
							pWnd->UpdateWindow();
						}

						CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDS_ANGLE );

						if( pEdit )
						{
							pEdit->SetWindowText( _T("90") );//m_bstrAngleValue );
						}
						pEdit = (CEdit *)pWnd->GetDlgItem( IDS_LENGTH );

						if( NULL != pEdit )
						{
							pEdit->SetWindowText( _T("2.0"));//m_bstrLengthValue );
						}
					}


					break;
				}

			default:
				{
					// This case handles generic notifications from common controls which notify the parent via the WM_COMMAND message.
					// The notification message is the hi word of wParam and the control identifier is the lo word of the wParam. The
					// values of the control can be determined by opening your dialog box in the resource window and looking at the
					// properties of the various controls. The specific notification codes are determined by each control. The common
					// control notification messages are usually defined in WinUser.h.

					WORD NotifyMessage = HIWORD( wParam );
					WORD ControlId = LOWORD( wParam );

					TRACE( "Notify message = %6ld, Control id = %6ld\n", NotifyMessage, ControlId );

					switch( ControlId )
					{
					case IDS_COMBO1:
						{
							switch( NotifyMessage )
							{
							case CBN_SETFOCUS:
								{
									// Just to show this can be trapped as can CBN_KILLFOCUS or any other notification.
									break;
								}

							case CBN_SELCHANGE:
								{
									CWnd *pWnd = CWnd::FromHandle( m_hWndRibbon );

									if( NULL != pWnd )
									{
										CComboBox* pComboBox = (CComboBox *)pWnd->GetDlgItem(IDS_COMBO1);

										if( NULL != pComboBox )
										{
											int iSel = pComboBox->GetCurSel();

											CString strSelected;

											pComboBox->GetLBText( iSel, strSelected );

											TRACE( "String selected is: %s\n", strSelected );
										}
									}

									break;
								}

							default:
								{
									break;
								}
							}

							break;
						}

					case IDS_ANGLE:
						{
							switch( NotifyMessage )
							{
							case EN_SETFOCUS:
								{
									break;
								}

							case EN_KILLFOCUS:
								{
									TRACE("Angle edit change notification received\n");

									CWnd *pWnd = CWnd::FromHandle( m_hWndRibbon );

									CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDS_ANGLE );

									if( NULL != pEdit )
									{
										CString strText;

										pEdit->GetWindowText( strText );

										// Set up a default value in the "double" edit field to be one database distance unit
										PartDocumentPtr pPartDoc;

										try
										{
											_bstr_t bstrDocName; // empty string will cause GetDocPtr to retrieve the active document
											GetDocPtr( bstrDocName, pPartDoc );

											// Addin only runs in Part ...

											if( NULL != pPartDoc )
											{
												UnitsOfMeasurePtr pUom = pPartDoc->UnitsOfMeasure;

												if( NULL != pUom )
												{
													try
													{
														_bstr_t bstrText = strText;

														// Get the value the user input in the edit field in database units. If we are
														// unable to parse the string, the user entered invalid characters and in that case,
														// the catch clause will restore the field to its prior value.

														_variant_t vt = pUom->ParseUnit( igUnitAngle, bstrText );

														// Now let it format that unit for display.
														m_bstrAngleValue = pUom->FormatUnit( igUnitAngle, vt );

														// Now let's display the angle string in the edit field.
														// I do this because the user could key in 180 and format
														// unit could convert it to 180 degrees.
														pEdit->SetWindowText( m_bstrAngleValue );

														// Now store the actual angle in database units. This is the value
														// that can be passed to an API that needs an angle.
														m_dAngle = vt;
													}
													catch ( _com_error &e )
													{
														hr = e.Error();

														// Bad input. Reset the window text.
														pEdit->SetWindowText( m_bstrAngleValue );
													}
												}
											}
										}
										catch( _com_error &e )
										{
											HRESULT hr = e.Error();
										}
									}

									break;
								}

							default:
								{
									break;
								}
							}

							break;
						}

					case IDS_LENGTH:
						{
							switch( NotifyMessage )
							{
							case EN_SETFOCUS:
								{
									break;
								}

							case EN_KILLFOCUS:
								{
									CWnd *pWnd = CWnd::FromHandle( m_hWndRibbon );

									CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDS_LENGTH );

									if( NULL != pEdit )
									{
										CString strText;

										pEdit->GetWindowText( strText );

										// Set up a default value in the "double" edit field to be one database distance unit
										SolidEdgeDocumentPtr pDoc;

										SolidEdgeFramework::DocumentTypeConstants nDocType = igUnknownDocument;

										try
										{
											_bstr_t bstrDocName; // empty string will cause GetDocPtr to retrieve the active document
											GetDocPtr( bstrDocName, pDoc, nDocType );

											// Addin only runs in Part ...

											if( NULL != pDoc )
											{
												UnitsOfMeasurePtr pUom = pDoc->UnitsOfMeasure;

												if( NULL != pUom )
												{
													try
													{
														_bstr_t bstrText = strText;

														// Get the value the user input in the edit field in database units. If we are
														// unable to parse the string, the user entered invalid characters and in that case,
														// the catch clause will restore the field to its prior value.

														_variant_t vt = pUom->ParseUnit( igUnitDistance, bstrText );

														// Now let it format that unit for display.
														m_bstrLengthValue = pUom->FormatUnit( igUnitDistance, vt );

														// Now let's display the length string in the edit field.
														// I do this because the user could key in 1.0 and format
														// unit could convert it to "1.0 mm". Or the user could key
														// in 1.0 m and that will be converted to 1000 mm. The actual
														// unit depends on the user's settings, which is why the UOM
														// object is so useful.
														pEdit->SetWindowText( m_bstrLengthValue );

														// Now store the actual length in database units. This is the value
														// that can be passed to an API that needs a length.
														m_dLength = vt;
													}
													catch ( _com_error &e )
													{
														hr = e.Error();

														// Bad input. Reset the window text.
														pEdit->SetWindowText( m_bstrLengthValue );
													}
												}
											}
										}
										catch( _com_error &e )
										{
											HRESULT hr = e.Error();
										}
									}

									break;
								}

							default:
								{
									break;
								}
							}

							break;
						}

					case IDS_LIST1:
						{
							switch( NotifyMessage )
							{
							case LBN_SETFOCUS:
								{
									break;
								}

							case LBN_SELCHANGE:
								{
									CWnd *pWnd = CWnd::FromHandle( m_hWndRibbon );

									if( NULL != pWnd )
									{
										CListBox* pListBox = (CListBox *)pWnd->GetDlgItem(IDS_LIST1);

										if( NULL != pListBox )
										{
											int iSel = pListBox->GetCurSel();

											CString strSelected;

											pListBox->GetText( iSel, strSelected );

											TRACE( "String selected is: %s\n", strSelected );
										}
									}

									break;
								}

							default:
								{
									break;
								}
							}

							break;
						}

					default:
						{
							break;
						}
					}
				}
			}

			break;
		}

	case SolidEdgeConstants::seWM_ACCELERATORSELECTED:  // value is 4226 (decimal)
		{
			char key = (char)wParam;

			if( key == 'o' )
			{
				TRACE( "OK accelerator hit\n" );
			}
			else if( key == 'f' )
			{
				TRACE( "Finish accelerator hit\n" );
				// Let's be real fancy and kill the command.

				GetCommand()->Done = VARIANT_TRUE;
			}

			break;
		}

	default:
		break;
	}

	return hr;
}

CSampleLocateCommand1::CSampleLocateCommand1() : CSampleLocateCommand()
{
	m_hWndGroup1 = NULL;
	m_hWndGroup2 = NULL;
}

CSampleLocateCommand1::~CSampleLocateCommand1()
{
}

HRESULT CSampleLocateCommand1::CreateRibbon()
{
	HRESULT hr = S_OK;

	// With Solid Edge ST, the old ribbon bar UI for commands was replaced with a command bar UI (not to be confused with
	// the CommandBars API that allowed for the creation of a CommandBar (a.k.a. toolbar). Got that? This command
	// will use the new API to create a couple of groups as well as a generic command bar header. Unlike calls made
	// using the old ribbon bar API in ST (see CSampleLocateCommand::CreateRibbon), the dialogs added to the command
	// bar via this API are not reformatted.

	ISolidEdgeCommandBarPtr pCommandBar = GetCommand();

	if( NULL == m_hWndGroup1 )
	{
		// This is the first call to create the command bar. Add dialog boxes to the command bar.

		if( NULL != pCommandBar )
		{
			TCHAR ResourceFilename[MAX_PATH];

			GetModuleFileName( hMyInstance(), ResourceFilename, sizeof( ResourceFilename ) );

			VARIANT_BOOL vbExpandable = VARIANT_TRUE;
			VARIANT_BOOL vbExpanded = VARIANT_TRUE;
			VARIANT_BOOL vbEnabled = VARIANT_TRUE;
			VARIANT_BOOL vbRedraw = VARIANT_TRUE;
			VARIANT_BOOL vbWithOptions = VARIANT_TRUE;
			VARIANT_BOOL vbWithDoit = VARIANT_TRUE;

			_bstr_t vDoitText("Cancel");
			_bstr_t vOptionsText; // Use edge provided default

			// Add a command bar header like what a number of Edge commands have. The header will
			// have two buttons on it and when the user clicks either one, notification will be 
			// sent to the command's WindowProc event sink. In case you are wondering, the control
			// IDs are constant and defined in CommandBarHeaderDialogControlIDs. Make sure those
			// values are not used by any other control your add-in may have on it's dialogs that
			// get inserted into the command bar since all notifications come through WindowProc.

			pCommandBar->AddCommandBarHeader( vbWithOptions, vbWithDoit, vDoitText, vOptionsText );

			int nBitmapID = EdgeVersionSupportsAlphaImageOnCommandBar() ? IDB_PNG2 : IDB_BITMAP2;

			// Add a command bar group. The bitmap has first pixel as transparent color.
			m_hWndGroup1 = (HWND) pCommandBar->AddGroup( IDS_TITLE1, IDD_DIALOG3, nBitmapID, ResourceFilename,
				vbExpandable, vbExpanded, vbEnabled, vbRedraw);

			if( NULL != m_hWndGroup1 )  // This should not happen, but then again this is software ...
			{
				// There are two ways to get a bitmap on a button. IDD_DIALOG3 has a button on it
				// with an id of IDC_X. The caption of the button is "IDB_X". Solid Edge will examine
				// the dialog when it is added and for each button that has "IDB_" in the caption, edge
				// will look for a bitmap resource with that name. Open the resource view and expand
				// the "Bitmap" tree. Note that a named bitmap exists with "IDB_X" as the name. That bitmap
				// is loaded automatically.

				// Oh, and if you are wondering, Edge usually uses an image that is 24 X 23 for its commands'
				// command bar button images. Smaller sizes work and that's up to you. And where do these
				// odd sizes come from. I mean 24 X 23?

				// Now if you examine the "IDB_X" bitmap, you will see that it is not 24 X 23. That's because
				// this sample is just that, a sample. You might be familiar with edge commands that have
				// "Accept" and "Deselect" buttons in the command bar dialogs. Well I stole those bitmaps
				// for this sample but I did not put the "Deselect" text on the IDC_X button since I wanted
				// to point out how a bitmap can be automatically loaded using the "IDB_" prefix in the
				// button's window text.


				// The second way to get a bitmap on a button is to call AddBitmapToButton. Again, note
				// that IDD_DIALOG3 also has a button with and id of IDC_ACCEPT. That button has text on
				// it with no "IDB_" in the name. That is because we want a button that has both a bitmap
				// and text. So the dialog button size needs to be large enough to accomodate the bitmap
				// and the text and I will call the method to add the bitmap. AddBitmapToButton will take
				// in an optional resource filename. If one is not provided, the resource used when calling
				// AddGroup will be used. Both are in the same resource so I choose not to send in the name.

				pCommandBar->AddBitmapToButton( (long)m_hWndGroup1, IDC_CHECK, IDB_CHECK );

				// We'll add keyboard accelerators to this command's ribbon bar. Ribbon bar accelerators are single 
				// characters. The command's WindowProc will receive seWM_ACCELERATORSELECTED messages with the character 
				// passed in the wParam.
				// In order to receive the message, the dialog box itself must have focus. Unfortunately
				// with the new UI in ST, when a dialog is inserted into the command bar/task panel, the
				// dialog only has focus if the user has (or the software) has set focus on a control in
				// the dialog box. For the user, this means clicking some control on the dialog. For some
				// odd reason, if the user clicks on the dialog but doesn't click on a control, the dialog
				// itself doesn't have focus! So these accelerators are not as useful as they used to be. Of
				// course there is still the key down command event. In any case, click one of the controls
				// like the check box and then hit the 'o' or 'f' key and the windowproc will be called
				// accordingly. This focus issue is due to the new UI and is, unfortunately, not controlled by
				// the Edge programmers (as of the time I am writing this).

				SAFEARRAY *Accelerators;  
				SAFEARRAYBOUND AcceleratorsBound;

				AcceleratorsBound.lLbound = 0;
				AcceleratorsBound.cElements = 2; // In this sample, ok and cancel buttons will be accelerated

				char x = 'o'; // 'o' for "Ok" button 

				Accelerators = SafeArrayCreate(VT_I1, 1, &AcceleratorsBound );

				long index = 0;
				hr = SafeArrayPutElement(Accelerators, &index, (void*)&x);

				x = 'f';      // 'f' for "Finish" button

				index = 1;
				hr = SafeArrayPutElement(Accelerators, &index, (void*)&x);

				// Inform the ribbon bar that we have accelerators.
				pCommandBar->SetAccelerators( (long)m_hWndGroup1, &Accelerators );

				SafeArrayDestroy( Accelerators );

				// Adding "edit" fields to the ribbon bar results in a list the ribbon bar holder maintains
				// which can be used to set/move focus among the controls in the list programatically by calling 
				// NextFocus. In this sample, the focus will move between two of the edit controls whenever the 
				// user has the focus in one of the two fields and then hits the "return" (a.k.a. "enter") key. 
				// See the KeyDown method for the details of how this is done.

				pCommandBar->AddEditField(  (long)m_hWndGroup1, IDS_LENGTH );
				pCommandBar->AddEditField(  (long)m_hWndGroup1, IDS_ANGLE );

				//  Make life easier. Get a CWnd from the ribbon window handle.
				CWnd *pWnd = CWnd::FromHandle(  m_hWndGroup1 );

				if( NULL != pWnd )
				{
					// Set up a default value in the length and angle fields to be one database distance/angle unit each

					SolidEdgeDocumentPtr pDoc;

					SolidEdgeFramework::DocumentTypeConstants nDocType = igUnknownDocument;

					_bstr_t bstrDocName; // empty string will cause GetDocPtr to retrieve the active document
					GetDocPtr( bstrDocName, pDoc, nDocType );

					if( NULL != pDoc )
					{
						UnitsOfMeasurePtr pUom;

						try
						{
							pUom = pDoc->UnitsOfMeasure;
						}
						catch( _com_error &e )
						{
							// I'll simply ignore this error and leave the edit fields blank.
							hr = e.Error();
						}

						if( NULL != pUom )
						{
							CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDS_ANGLE );

							if( NULL != pEdit )
							{
								// Let the UOM object convert my angle to data base units.
								_variant_t vt = pUom->ParseUnit( igUnitAngle, m_bstrAngleValue );

								// Now let it format that unit for display.
								m_bstrAngleValue = pUom->FormatUnit( igUnitAngle, vt );

								m_dAngle = vt;  // _variant_t objects make life so much easier

								// Now let's display the angle in the edit field
								pEdit->SetWindowText( m_bstrAngleValue );
							}

							pEdit = (CEdit *)pWnd->GetDlgItem( IDS_LENGTH );

							if( NULL != pEdit )
							{
								// Let the UOM object convert my length to data base units.
								_variant_t vt = pUom->ParseUnit( igUnitDistance, m_bstrLengthValue );

								// Now let it format that unit for display.
								m_bstrLengthValue = pUom->FormatUnit( igUnitDistance, vt );

								m_dLength = vt;

								// Now let's display the length in the edit field
								pEdit->SetWindowText( m_bstrLengthValue );
							}

							pEdit->EnableWindow(TRUE);
						}
					}

					CButton* pButton = (CButton*)pWnd->GetDlgItem( IDC_RADIO1 );
					if( pButton )
					{
						pButton->SetCheck(1);
					}
					// This sample has a ribbon that contains a combo box. Seed the box with some really catchy text.
					CComboBox* pComboBox = (CComboBox *)pWnd->GetDlgItem(IDS_COMBO1);

					if( NULL != pComboBox )
					{
						pComboBox->InsertString(0, _T("Solid") );
						pComboBox->InsertString(1, _T("Edge") );
						pComboBox->InsertString(2, _T("Rules!") ); // So let it be written, so let it be done.

						// Now set the current selection.
						pComboBox->SetCurSel(0);

						pComboBox->EnableWindow(TRUE);
					}

					// This sample has a ribbon that contains a combo box. Seed the box with some really catchy text.
					CListBox* pListBox = (CListBox *)pWnd->GetDlgItem(IDS_LIST1);

					if( NULL != pListBox )
					{
						pListBox->InsertString(0, _T("Go") );
						pListBox->InsertString(1, _T("Big") );
						pListBox->InsertString(2, _T("Orange!") );  // As in the University of Tennessee, of course.

						// Now set the current selection.
						pListBox->SetCurSel(0);

						pListBox->EnableWindow(TRUE);
					}

					pCommandBar->ShowGroup( IDS_TITLE1, (long)m_hWndGroup1 );

					// Make sure focus is set on the ribbon bar.
					pCommandBar->SetCurrentFocus((long)m_hWndGroup1); // Do this since accelerators are being used. Or is it because Solid Edge
					// can actually display two ribbon bars (see for example, the SketchPoint
					// tool in the Draft environment).
				}
			}

			m_hWndGroup2 = (HWND) pCommandBar->AddGroup( IDS_TITLE2, IDD_DIALOG4, nBitmapID, ResourceFilename,
														 vbExpandable, vbExpanded, vbEnabled, vbRedraw);

			if( m_hWndGroup2 )
			{
				pCommandBar->ShowGroup( IDS_TITLE2, (long)m_hWndGroup2 );
			}
		}
	}
	else
	{
		if( NULL != pCommandBar )
		{
			pCommandBar->SetCurrentFocus( (long)m_hWndGroup1 );
		}
	}

	return hr;
}

HRESULT CSampleLocateCommand1::WindowProc( LPDISPATCH pUnkDoc, LPDISPATCH pUnkView, UINT nMsg, WPARAM wParam, LPARAM lParam, LRESULT *lResult )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	HRESULT hr = S_OK;

	switch( nMsg )
	{
	case WM_COMMAND:
		{
			// Control notifications are usually sent to the command embedded in WM_COMMAND messages. For more information
			// on control notification, see the MSDN. Since Solid Edge creates the window (actually a CDialog) that displays
			// the ribbon, control notifications are sent to Solid Edge which turns around and routes them to this event
			// handler. From there, you are on your own. I mean, its all standard message processing.

			switch ( wParam )
			{
				// Buttons are so simple. Simply look for the button id in wParam and respond accordingly.
			case CommandBarHeaderDoitButton: // Edge defined "control" ID - can't use on my own dialogs!
				{
					TRACE( "cancel (doit) button hit on command bar header\n" );

					GetCommand()->Done = VARIANT_TRUE;

					break;
				}

			case CommandBarHeaderOptionsButton: // Edge defined "control" ID - can't use on my own dialogs!
				{
					TRACE( "options button hit on command bar header\n" );

					MyOptions optionsDlg;

					if( IDOK == optionsDlg.DoModal() )
					{
						CString strDoit = optionsDlg.m_strDoit;

						CString strOptions = optionsDlg.m_strOptions;

						ISolidEdgeCommandBarPtr pCommandBar = GetCommand();

						if( NULL != pCommandBar )
						{
							_bstr_t bstrDoit = strDoit;
							_bstr_t bstrOptions = strOptions;

							try
							{
								pCommandBar->SetCommandBarHeaderText( bstrDoit, bstrOptions );
							}
							catch( _com_error &e )
							{
								e; // soothe the compiler
							}
						}
					}

					break;
				}

			case IDS_OK:
				{
					TRACE( "OK button hit\n" );

					break;
				}

			case IDS_CANCEL:
				{
					TRACE( "CANCEL button hit\n" );
					// Let's be real fancy and kill the command. Did I say "kill"? Not very politically correct is it.
					// What would the PETI (People for the Ethical Treatment of Interfaces) people say?

					GetCommand()->Done = VARIANT_TRUE;

					break;
				}

			case IDC_RADIO1:
				{
					TRACE( "Radio button hit\n" );
					CWnd *pWnd = CWnd::FromHandle( m_hWndGroup1 );

					if( NULL != pWnd )
					{
						CButton* pButton = (CButton*)pWnd->GetDlgItem( IDC_RADIO1 );
						if( pButton )
						{
							if( pButton->GetCheck() )
							{
								TRACE("Radio 1 is checked\n");
							}
							else
							{
								// Without more than one radio control grouped together
								// the control is always checked.
								TRACE("Radio 1 is not checked\n");
							}
						}
					}

					break;
				}
			case IDC_CHECK1:
				{
					TRACE( "Check button hit\n" );
					CWnd *pWnd = CWnd::FromHandle( m_hWndGroup1 );

					if( NULL != pWnd )
					{
						CButton* pButton = (CButton*)pWnd->GetDlgItem( IDC_CHECK1 );
						if( pButton )
						{
							if( pButton->GetCheck() )
							{
								TRACE("Check button is checked\n");
							}
							else
							{
								TRACE("Check button is not checked\n");
							}
						}
					}

					break;
				}
			case IDC_X:
				{
					TRACE( "X Button hit\n" );

					break;
				}

			case IDC_CHECK:
				{
					TRACE( "Accept Button hit\n" );

					break;
				}
			default:
				{
					// This case handles generic notifications from common controls which notify the parent via the WM_COMMAND message.
					// The notification message is the hi word of wParam and the control identifier is the lo word of the wParam. The
					// values of the control can be determined by opening your dialog box in the resource window and looking at the
					// properties of the various controls. The specific notification codes are determined by each control. The common
					// control notification messages are usually defined in WinUser.h.

					WORD NotifyMessage = HIWORD( wParam );
					WORD ControlId = LOWORD( wParam );

					TRACE( "Notify message = %6ld, Control id = %6ld\n", NotifyMessage, ControlId );

					switch( ControlId )
					{
					case IDS_COMBO1:
						{
							switch( NotifyMessage )
							{
							case CBN_SETFOCUS:
								{
									// Just to show this can be trapped as can CBN_KILLFOCUS or any other notification.
									break;
								}

							case CBN_SELCHANGE:
								{
									CWnd *pWnd = CWnd::FromHandle( m_hWndGroup1 );

									if( NULL != pWnd )
									{
										CComboBox* pComboBox = (CComboBox *)pWnd->GetDlgItem(IDS_COMBO1);

										if( NULL != pComboBox )
										{
											int iSel = pComboBox->GetCurSel();

											CString strSelected;

											pComboBox->GetLBText( iSel, strSelected );

											TRACE( "String selected is: %s\n", strSelected );
										}
									}

									break;
								}

							default:
								{
									break;
								}
							}

							break;
						}

					case IDS_ANGLE:
						{
							switch( NotifyMessage )
							{
							case EN_SETFOCUS:
								{
									break;
								}

							case EN_KILLFOCUS:
								{
									TRACE("Angle edit change notification received\n");

									CWnd *pWnd = CWnd::FromHandle( m_hWndGroup1 );

									CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDS_ANGLE );

									if( NULL != pEdit )
									{
										CString strText;

										pEdit->GetWindowText( strText );

										// Set up a default value in the "double" edit field to be one database distance unit
										SolidEdgeDocumentPtr pDoc;

										SolidEdgeFramework::DocumentTypeConstants nDocType = igUnknownDocument;

										try
										{
											_bstr_t bstrDocName; // empty string will cause GetDocPtr to retrieve the active document
											GetDocPtr( bstrDocName, pDoc, nDocType );

											// Addin only runs in Part ...

											if( NULL != pDoc )
											{
												UnitsOfMeasurePtr pUom = pDoc->UnitsOfMeasure;

												if( NULL != pUom )
												{
													try
													{
														_bstr_t bstrText = strText;

														// Get the value the user input in the edit field in database units. If we are
														// unable to parse the string, the user entered invalid characters and in that case,
														// the catch clause will restore the field to its prior value.

														_variant_t vt = pUom->ParseUnit( igUnitAngle, bstrText );

														// Now let it format that unit for display.
														m_bstrAngleValue = pUom->FormatUnit( igUnitAngle, vt );

														// Now let's display the angle string in the edit field.
														// I do this because the user could key in 180 and format
														// unit could convert it to 180 degrees.
														pEdit->SetWindowText( m_bstrAngleValue );

														// Now store the actual angle in database units. This is the value
														// that can be passed to an API that needs an angle.
														m_dAngle = vt;
													}
													catch ( _com_error &e )
													{
														hr = e.Error();

														// Bad input. Reset the window text.
														pEdit->SetWindowText( m_bstrAngleValue );
													}
												}
											}
										}
										catch( _com_error &e )
										{
											HRESULT hr = e.Error();
										}
									}

									break;
								}

							default:
								{
									break;
								}
							}

							break;
						}

					case IDS_LENGTH:
						{
							switch( NotifyMessage )
							{
							case EN_SETFOCUS:
								{
									break;
								}

							case EN_KILLFOCUS:
								{
									CWnd *pWnd = CWnd::FromHandle( m_hWndGroup1 );

									CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDS_LENGTH );

									if( NULL != pEdit )
									{
										CString strText;

										pEdit->GetWindowText( strText );

										// Set up a default value in the "double" edit field to be one database distance unit
										SolidEdgeDocumentPtr pDoc;

										SolidEdgeFramework::DocumentTypeConstants nDocType = igUnknownDocument;

										try
										{
											_bstr_t bstrDocName; // empty string will cause GetDocPtr to retrieve the active document
											GetDocPtr( bstrDocName, pDoc, nDocType );

											// Addin only runs in Part ...

											if( NULL != pDoc )
											{
												UnitsOfMeasurePtr pUom = pDoc->UnitsOfMeasure;

												if( NULL != pUom )
												{
													try
													{
														_bstr_t bstrText = strText;

														// Get the value the user input in the edit field in database units. If we are
														// unable to parse the string, the user entered invalid characters and in that case,
														// the catch clause will restore the field to its prior value.

														_variant_t vt = pUom->ParseUnit( igUnitDistance, bstrText );

														// Now let it format that unit for display.
														m_bstrLengthValue = pUom->FormatUnit( igUnitDistance, vt );

														// Now let's display the length string in the edit field.
														// I do this because the user could key in 1.0 and format
														// unit could convert it to "1.0 mm". Or the user could key
														// in 1.0 m and that will be converted to 1000 mm. The actual
														// unit depends on the user's settings, which is why the UOM
														// object is so useful.
														pEdit->SetWindowText( m_bstrLengthValue );

														// Now store the actual length in database units. This is the value
														// that can be passed to an API that needs a length.
														m_dLength = vt;
													}
													catch ( _com_error &e )
													{
														hr = e.Error();

														// Bad input. Reset the window text.
														pEdit->SetWindowText( m_bstrLengthValue );
													}
												}
											}
										}
										catch( _com_error &e )
										{
											HRESULT hr = e.Error();
										}
									}

									break;
								}

							default:
								{
									break;
								}
							}

							break;
						}

					case IDS_LIST1:
						{
							switch( NotifyMessage )
							{
							case LBN_SETFOCUS:
								{
									break;
								}

							case LBN_SELCHANGE:
								{
									CWnd *pWnd = CWnd::FromHandle( m_hWndGroup1 );

									if( NULL != pWnd )
									{
										CListBox* pListBox = (CListBox *)pWnd->GetDlgItem(IDS_LIST1);

										if( NULL != pListBox )
										{
											int iSel = pListBox->GetCurSel();

											CString strSelected;

											pListBox->GetText( iSel, strSelected );

											TRACE( "String selected is: %s\n", strSelected );
										}
									}

									break;
								}

							default:
								{
									break;
								}
							}

							break;
						}

					default:
						{
							break;
						}
					}
				}
			}

			break;
		}

	case SolidEdgeConstants::seWM_ACCELERATORSELECTED:  // value is 4226 (decimal)
		{
			char key = (char)wParam;

			if( key == 'o' )
			{
				TRACE( "OK accelerator hit\n" );
			}
			else if( key == 'f' )
			{
				TRACE( "Finish accelerator hit\n" );
				// Let's be real fancy and kill the command.

				GetCommand()->Done = VARIANT_TRUE;
			}

			break;
		}

	default:
		break;
	}

	return hr;
}

// The KeyDown event is handles simply to show some of the ways the command bar can be
// manipulated.

// The keys and their uses are as follows:
//
//  'r' - Remove the command bar entries (i.e., delete it)
//  'a' - Add the command bar entries
//  'h' - Hide the command bar group two entry
//  's' - Show the command bar group two entry
//  'g' - Set focus on the command bar group one entry
//  'c' - Collapse the command bar group one entry
//  'e' - Expand the command bar group one entry
//  'd' - Disable the command bar group one entry
//  'v' - Ensure that the command bar group two entry is visible.
//  return/enter - Move focus among the edit fields defined in the ISolidEdgeCommandBar::AddEditField call.

HRESULT CSampleLocateCommand1::KeyDown(short* KeyCode, short Shift)
{
	TRACE("Key down event\n");

	ISolidEdgeCommandBarPtr pCommandBar;

	pCommandBar = GetCommand();

	if( NULL != pCommandBar )
	{
		// If the user is trying to key in "rad" for an angle, I don't
		// want to process the key too! In fact, don't process any key
		// if one of my edit controls has focus.
		bool bSkip = false;

		HWND hWndFocus = ::GetFocus();
		if( m_hWndGroup1 )
		{
			CWnd *pWnd = CWnd::FromHandle(  m_hWndGroup1 );

			if( pWnd )
			{
				CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDS_ANGLE );

				if( pEdit && hWndFocus == pEdit->GetSafeHwnd() )
				{
					bSkip = true;
				}
				else
				{
					CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDS_LENGTH );
				
					if( pEdit && hWndFocus == pEdit->GetSafeHwnd() )
					{
						bSkip = true;
					}
					else
					{
						CListBox* pList = (CListBox *)pWnd->GetDlgItem( IDS_LIST1 );
						
						if( pList && hWndFocus == pList->GetSafeHwnd() )
						{
							bSkip = true;
						}
						else
						{
							CComboBox* pComboBox = (CComboBox *)pWnd->GetDlgItem(IDS_COMBO1);

							if( pComboBox && hWndFocus == pComboBox->GetSafeHwnd() )
							{
								bSkip = true;
							}
							else
							{
								if( m_hWndGroup2 )
								{
									CWnd *pWnd = CWnd::FromHandle(  m_hWndGroup2 );

									if( pWnd )
									{
										CEdit* pEdit = (CEdit *)pWnd->GetDlgItem( IDC_EDIT1 );
									
										if( pEdit && hWndFocus == pEdit->GetSafeHwnd() )
										{
											bSkip = true;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		// See if this is the "return/enter" key. If so, see which control has the focus on the
		// ribbon bar. Controls added as an "edit field" using the AddEditField member function
		// of the ribbon bar interface define a focus list. Focus can be moved from one control to
		// another by caling NextFocus. This sample does just that whenever the user hits enter.

		if( 0xd == *KeyCode )
		{
			int id;

			pCommandBar->GetCurrentFocus((long)m_hWndGroup1, &id);

			// GetCurrentFocus returns -1 if the control with the focus is not in the list of
			// "edit" fields added by calling AddEditField. In that case, I don't bother to
			// move the focus.

			if( -1 != id )
			{
				TRACE("ribbon bar control with foucs is %d\n", id);

				pCommandBar->NextFocus((long)m_hWndGroup1);
				pCommandBar->GetCurrentFocus((long)m_hWndGroup1,&id);
			}
		}
		else if( bSkip )
		{
			return S_OK;
		}
		// Note on HideGroup and ShowGroup - this can cause the order of the groups to change.
		// If HideGroup is called on the first group, when ShowGroup is called on it, the
		// group will redisplay after the remaining group. Change the two calls so the
		// argument is IDS_TITLE1 to see what I mean. Hence these two APIs can effectively be
		// used to reorder the dialogs in the command bar.
		else if( 0x53 == *KeyCode ) // The 's' key
		{
			pCommandBar->ShowGroup(IDS_TITLE2);
		}
		else if( 0x48 == *KeyCode ) // The 'h' key
		{
			pCommandBar->HideGroup(IDS_TITLE2);
		}
		else if( 0x52 == *KeyCode ) // The 'r' key
		{
			// Normally a command that is not exercising "full disclosure", i.e., is
			// not showing its entire command bar UI would remove one group and add
			// another. But this is a cheap sample add-in so it is not so sophisticated.
			// So I just remove both groups.
			pCommandBar->RemoveGroup(IDS_TITLE1);
			pCommandBar->RemoveGroup(IDS_TITLE2);
			
			VARIANT_BOOL vbWithXXX = VARIANT_FALSE;
			_bstr_t strTmp;

			pCommandBar->AddCommandBarHeader( vbWithXXX, vbWithXXX, strTmp, strTmp );

			m_hWndGroup1 = 0;
		}
		else if( 0x41 == *KeyCode ) // The 'a' key
		{
			CreateRibbon();
		}
		else if( 0x47 == *KeyCode ) // The 'g' key
		{
			pCommandBar->SetCurrentFocus((long)m_hWndGroup1);
		}
		else if( 0x43 == *KeyCode ) // The 'c' key
		{
			pCommandBar->ExpandGroup( IDS_TITLE1, VARIANT_FALSE, VARIANT_FALSE );
		}
		else if( 0x45 == *KeyCode ) // The 'e' key
		{
			pCommandBar->ExpandGroup( IDS_TITLE1, VARIANT_TRUE, VARIANT_FALSE );
		}
		else if( 0x44 == *KeyCode ) // The 'd' key
		{
			VARIANT_BOOL vbDisabled = pCommandBar->IsGroupEnabled( IDS_TITLE1 );

			if( VARIANT_TRUE == vbDisabled )
			{
				pCommandBar->EnableGroup( IDS_TITLE1, VARIANT_FALSE, VARIANT_FALSE );
			}
			else
			{
				pCommandBar->EnableGroup( IDS_TITLE1, VARIANT_TRUE, VARIANT_FALSE );
			}
		}
		else if( 0x56 == *KeyCode ) // The 'v' key
		{
			// Currently the style used by the Solid Edge command bar/task panel
			// is an "explorer style". This means that EnsureVisible has no effect.
			// But the method was added to the API since the capability will exist if it is
			// determined that the style should be changed in the future.

			// To see this behavior, resize the command bar docking pane so that the
			// second group is not completely visible to the user. Instead of the
			// user having to use the scroll bar to scroll the second group into view,
			// we can do it for the user.
			pCommandBar->EnsureVisible( IDS_TITLE2 );
		}
	}

	return S_OK;
}
