
#ifndef __SEPARTREADER_H__
#define __SEPARTREADER_H__

#ifdef __SEADAPTER_DEF__
#define SEPARTREADER_API
#else
#ifdef SEPARTREADER_EXPORTS
#define SEPARTREADER_API __declspec(dllexport)
#else
#define SEPARTREADER_API __declspec(dllimport)
#endif
#endif

// error codes
typedef enum
{
	SE_ERROR_no_errors = 0,
	SE_ERROR_not_implemented = 10001,
	SE_ERROR_no_memory = 10002,
	SE_ERROR_invalid_input = 10003,
	SE_ERROR_data_not_available = 10004,
	SE_ERROR_failed_to_load = 10005,
	SE_ERROR_detailed_part_not_available = 10006,
	SE_ERROR_simplified_part_not_available = 10007,
	SE_ERROR_flatpattern_part_not_available = 10008,
	SE_ERROR_weld_parts_part_not_available = 10009,
	SE_ERROR_weld_beads_part_not_available = 10010,
	SE_ERROR_invalid_call_for_file_type = 10011,
	SE_ERROR_unknown = 10012,
	SE_ERROR_weld_parts_not_available = 10013,
	SE_ERROR_weld_beads_not_available = 10014,
	SE_ERROR_no_bodies_in_the_stream = 10015,
	SE_ERROR_expects_only_one_body = 10016
} SE_ERROR_t;

// filetypes
typedef enum
{
	SE_FILE_unknown_file = 0,
	SE_FILE_part_file = 1,
	SE_FILE_sheetmetal_file = 2,
	SE_FILE_weldment_file = 3
} SE_FILE_t;

//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
// CSEThreadInfo contains the necessary information about the thread/hole
class CSEHoleThreadInfo
{
public:

	// various enumerators which are need to define hole/threads
	enum CSEThreadSettingType 
	{
		UnknownSettingType = 0,
		HoleSetting = 1,              // 'standard' threads, or more appropriately non-pipe thread.
									// these settings came from holes.txt. 
		StraightPipeThreadSetting = 2, // straight pipe threads - no taper angle
		TaperedPipeThreadSetting = 4   // tapered pipe threads
	} ;

	enum CSEThreadLocationEnum{
		Inside,
		Outside,
		LocationUnknown
	};

	CSEThreadSettingType   m_ThreadSetting ;
    CSEThreadLocationEnum  m_ThreadLocation;
    double              m_NominalDiameter;
    double              m_InternalDiameter;
    double              m_ExternalDiameter;
    LPCWSTR				m_pThreadDesignation; // for Pipe Threads, serves as External thread designation

	double              m_InsideEffectiveThreadLength ;
	double              m_OutsideEffectiveThreadLength ;
	double              m_ThreadHeight ;

	LPCWSTR				m_pInternalThreadDesignation;

    // Default Constructor
    CSEHoleThreadInfo();
	virtual ~CSEHoleThreadInfo();
} ;

class CSEHoleTapType
{
public:

	enum CSETapFormEnum{
		UNCForm,        // American Standard, all measurements will be in inches
		UNFForm,        // --do--
		UNEFForm,       // --do--
		ISOCForm,       // Enlish Standard, all measurements will be in millimeters.
		ISOFForm,       // Enlish Standard, all measurements will be in millimeters.
		NonStandardForm,
		NULLForm,
	};

	enum CSETapDepthEnum{
		FiniteThreadDepth,
		ToExtentThreadDepth,
		TapDepthUnknown
	};

	CSETapFormEnum      m_TapForm;
    CSETapDepthEnum     m_TapDepthType;
    double              m_TapDepth;
    double              m_TapOffset;
	double              m_TaperAngle;

	CSEHoleThreadInfo   m_ThreadInfo;
    
	// Default Constructor
    CSEHoleTapType();
	virtual ~CSEHoleTapType();
};     

class CSEThreadInfo
{
public:
	//
	// Various Hole Description Enumerators
	enum CSEHoleTypeEnum 
	{
		RegularHole,
		CounterSinkHole,
		CounterBoreHole,
		CounterDrillHole,    // a.k.a Recessed CounterSink Hole, available through Solid Edge UI as a counterbore with bottom angle
		HoleTypeUnknown,
		ThreadFeature
	};

	enum CSEHoleExtentEnum 
	{
		ThroughAllHole,		// hole is to penetrate all faces of the model.
		ThroughNextHole,	// only goes through next face
		FromToHole,			// is driven by two planes (driving plane data not available through attributes)
		BlindHole,          // has a finite depth
		HoleExtentUnknown,
		KeypointHole        // depth is tied to a keypoint (keypoint data not available through attributes)
	};

	enum CSEHoleTreatmentEnum 
	{
		NoTreatment,
		TappedHole,          // threaded hole
		TaperedHole,         // hole has a taper angle
		HoleTreatmentUnknown
	};

	enum CSEHoleOrientationEnum 
	{
		NormalToPlacementPlane,
		NormalToEntrySurface,
		CoAxial,
		HoleOrientationUnknown
	};

	enum CSEHoleMeasurementUnitEnum
	{
		StandardMeasurementUnit,  // inches
		MetricMeasurementUnit,
		HoleMeasurementUnitUnknown
	};

	enum CSEHoleTaperTypeEnum
	{
		TaperRatio,
		TaperAngle,
		HoleTaperTypeUnknown,
		TaperRLRatio
	};

	enum CSEHoleVBottomDimTypeEnum
	{
		VBottomDimTypeUnknown,
		VBottomDimToFlat,
		VBottomDimToV
	} ;

	enum CSEHoleTaperDimTypeEnum
	{
		TaperDimTypeUnknown,
		TaperDimAtTop,
		TaperDimAtBottom 
	} ;

	enum CSEHoleCounterboreProfileLocationTypeEnum
	{
		CounterboreProfileIsUnknown,
		CounterboreProfileIsAtTop,
		CounterboreProfileIsAtBottom 
	} ;

public:
	
	int m_nVersion;					// version number

    int m_nExtraFaces;				// number of split hole faces
    int *m_pExtraFaces;				// split hole faces
    int m_nCounterBoreFaces;		// number of counter-bore faces
    int *m_pCounterBoreFaces;		// counterbore faces
    int m_nCounterSinkFaces;		// number of countersink faces
    int *m_pCounterSinkFaces;		// countersink faces
    int m_nBlindConeFaces;			// instead of being flat, the hole has a v-bottom. 
    int *m_pBlindConeFaces;			// faces that comprise the v-bottom. 
    bool m_bThreadsStartAtFaceBase;	// indicates whether the threads start at the base of the cylinder,     
    double m_gRootPoint[3];			// root point of the circle defining the hole
    double m_gNormal[3];			// normal of the plane defining the hole. 

    CSEHoleTypeEnum m_HoleType;
    double          m_HoleDiameter;
    double          m_CBoreDiameter;
    double          m_CBoreDepth;
    double          m_CSinkAngle;
    double          m_CSinkDiameter;
    double          m_CSinkRecessDepth;
    double          m_BlindConeAngle;  // // v-bottom angle in degrees, = 0 Means, Flat Bottom.

    // HoleExtent, Actual, From/To Collected by the Extent Collector.
    CSEHoleExtentEnum m_HoleExtent;
    double m_HoleDepth;

    // Hole Internal Treatments
    CSEHoleTreatmentEnum	m_HoleTreatment;
    CSEHoleTapType			m_ThreadType;		// contains the thread information
    CSEHoleTaperTypeEnum	m_TaperType;		// how the taper value was calculated
    double					m_TaperValue;		// result of the taper calculation, if angle, it'll be in degrees
	                              // convert to a ratio by dHoleTaperRatio = tan( dHoleTaperRatio * GPI/180.0 );
    // Hole Orientation
    CSEHoleOrientationEnum m_HoleOrientation;

    // The above Measuments are in this type
    CSEHoleMeasurementUnitEnum m_HoleMeasurementUnitType;

	CSEHoleVBottomDimTypeEnum m_HoleVBottomDimType ;
	CSEHoleTaperDimTypeEnum m_HoleTaperDimType ;
	CSEHoleCounterboreProfileLocationTypeEnum m_HoleCounterboreProfileLocationType ;

	double m_TaperRValue ;
	double m_TaperLValue ;	

	CSEThreadInfo();
	virtual ~CSEThreadInfo();
	void Init(); // initialiazes the members just like the constructor
};
// this class contains the relevant hole information based
// on parasolid entity identifiers
class CSEHoleInfo
{
public :

	int m_nFaceId;
	int m_nThreads;

	CSEThreadInfo m_Info[2];

	CSEHoleInfo()
	{
		m_nFaceId = 0;
		m_nThreads = 0;
	}

	virtual ~CSEHoleInfo();
};
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////

// This class is exported from the SEReader.dll
class SEPARTREADER_API CSEPartReader 
{
private:
	LPVOID m_pData;

public:

	// default constructor
	CSEPartReader(void);

	// destructor
	virtual ~CSEPartReader(void);

	// clears the data the object is currently
	// holding
	SE_ERROR_t Clear();

	// set the file name if the default constructor
	// has been used
	SE_ERROR_t Load( LPCWSTR sSEFileName );
#if defined(_NATIVE_WCHAR_T_DEFINED)
	SE_ERROR_t Load(const unsigned short *sSEFileName );
#endif

	// this method can be used to find out the last saved
	// version of the se file
	SE_ERROR_t GetLastSavedVersion( ULONG* pnMajorVersion, ULONG* pnMinorVersion = NULL, ULONG* pnUpdateVersion = NULL, ULONG* pnBuildNumber = NULL );

	// this method is for part and sheetmetal files to
	// get the detailed part
	SE_ERROR_t GetDetailedPart( int nParasolidVersion, LPSTREAM *pStream );
	SE_ERROR_t GetDetailedPartWithPSColors( int nParasolidVersion, LPSTREAM *pStream );

	// this method is for part and sheetmetal files to
	// get the simplified part
	SE_ERROR_t GetSimplifiedPart( int nParasolidVersion, LPSTREAM *pStream );
	SE_ERROR_t GetSimplifiedPartWithPSColors( int nParasolidVersion, LPSTREAM *pStream );

	// this method is for sheetmetal files only to
	// get the simplified part
	SE_ERROR_t GetFlatPatternPart( int nParasolidVersion, LPSTREAM *pStream );
	SE_ERROR_t GetFlatPatternPartWithPSColors( int nParasolidVersion, LPSTREAM *pStream );

	// gets the number of constructions in a file
	SE_ERROR_t GetConstructionsCount( ULONG* nConstructions );

	// gets the contruction parts
	SE_ERROR_t GetConstructionParts( int nParasolidVersion, LPSTREAM *pStream );
	SE_ERROR_t GetConstructionPartsWithPSColors( int nParasolidVersion, LPSTREAM *pStream );

	// gets the number of weld parts in a weldment file
	SE_ERROR_t GetWeldPartsCount( ULONG* nWeldParts );

	// gets the weld parts in a weldment file
	SE_ERROR_t GetWeldParts( int nParasolidVersion, LPSTREAM *pStream );
	SE_ERROR_t GetWeldPartsWithPSColors( int nParasolidVersion, LPSTREAM *pStream );

	// gets the number of weld beads in a weldment file
	SE_ERROR_t GetWeldBeadsCount( ULONG* nWeldBeads );

	// gets the weld beads in a weldment file
	SE_ERROR_t GetWeldBeads( int nParasolidVersion, LPSTREAM *pStream );
	SE_ERROR_t GetWeldBeadsWithPSColors( int nParasolidVersion, LPSTREAM *pStream );

	// gets the hole information
	SE_ERROR_t GetHoleInfo( LPSTREAM pStream, int& nHoleInfo, CSEHoleInfo* &pSEHoleInfo );

	// to free the memory allocated by GetHoleInfo
	SE_ERROR_t FreeHoleInfo( int& nHoleInfo, CSEHoleInfo* &pSEHoleInfo );
};

#endif

//  $Log: /Curr_P/PSL/STDDLLS/LITPRTDT/DEV/Include/SEPartReader.h $ 
// 
// 15    9/10/04 5:40p Gkunda
// copying the designation strings instead of just using the gusertext
// buffers - gan
// 
// 14    8/03/04 10:55a Gkunda
// for exposing thread info from part reader - gan
// 
// 13    8/02/04 3:46p Gkunda
// 5016697 for exposing hole info from part reader - gan
// 
// 12    3/05/04 10:32a Gkunda
// ifdefed out the dll export for adapter - gan
// 
// 11    5/10/02 11:55a Gkunda
// made the destructor virtual. - gan
// 
// 10    4/19/02 5:27p Gkunda
// new part reader related changes. - gan
// 
// 9     4/19/02 4:25p Gkunda
// new part reader related changes. - gan
// 
// 8     3/26/02 10:33a Gkunda
// converted the double pointers to single pointers. - gan
// 
// 7     3/26/02 10:22a Gkunda
// 
// 6     3/25/02 2:49p Gkunda
// Checking in changes to SEPartReader to provide minimum frustrum
// support.
// 
// 5     3/13/02 12:27p Gkunda
// last round of changes. - gan
// 
// 4     3/13/02 8:38a Gkunda
// separtreader changes. - gan
// 
// 3     3/12/02 10:09a Gkunda
// next round of separt reader. - gan
// 
