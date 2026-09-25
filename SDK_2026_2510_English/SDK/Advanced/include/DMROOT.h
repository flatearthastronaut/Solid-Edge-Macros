

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0622 */
/* at Mon Jan 18 22:14:07 2038
 */
/* Compiler settings for DMROOT.idl:
    Oicf, W1, Zp8, env=Win64 (32b run), target_arch=AMD64 8.01.0622 
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
/* @@MIDL_FILE_HEADING(  ) */

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif /* __RPCNDR_H_VERSION__ */

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __DMROOT_h__
#define __DMROOT_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IDMPlane_FWD_DEFINED__
#define __IDMPlane_FWD_DEFINED__
typedef interface IDMPlane IDMPlane;

#endif 	/* __IDMPlane_FWD_DEFINED__ */


#ifndef __IDMCurve_FWD_DEFINED__
#define __IDMCurve_FWD_DEFINED__
typedef interface IDMCurve IDMCurve;

#endif 	/* __IDMCurve_FWD_DEFINED__ */


#ifndef __IDMCurve2D_FWD_DEFINED__
#define __IDMCurve2D_FWD_DEFINED__
typedef interface IDMCurve2D IDMCurve2D;

#endif 	/* __IDMCurve2D_FWD_DEFINED__ */


#ifndef __IDMCircle_FWD_DEFINED__
#define __IDMCircle_FWD_DEFINED__
typedef interface IDMCircle IDMCircle;

#endif 	/* __IDMCircle_FWD_DEFINED__ */


#ifndef __IDMEllipse_FWD_DEFINED__
#define __IDMEllipse_FWD_DEFINED__
typedef interface IDMEllipse IDMEllipse;

#endif 	/* __IDMEllipse_FWD_DEFINED__ */


#ifndef __IDMLine_FWD_DEFINED__
#define __IDMLine_FWD_DEFINED__
typedef interface IDMLine IDMLine;

#endif 	/* __IDMLine_FWD_DEFINED__ */


#ifndef __IDMPLine_FWD_DEFINED__
#define __IDMPLine_FWD_DEFINED__
typedef interface IDMPLine IDMPLine;

#endif 	/* __IDMPLine_FWD_DEFINED__ */


#ifndef __IDMPolyline_FWD_DEFINED__
#define __IDMPolyline_FWD_DEFINED__
typedef interface IDMPolyline IDMPolyline;

#endif 	/* __IDMPolyline_FWD_DEFINED__ */


#ifndef __IDMBSplineCurve_FWD_DEFINED__
#define __IDMBSplineCurve_FWD_DEFINED__
typedef interface IDMBSplineCurve IDMBSplineCurve;

#endif 	/* __IDMBSplineCurve_FWD_DEFINED__ */


#ifndef __IDMCircle2D_FWD_DEFINED__
#define __IDMCircle2D_FWD_DEFINED__
typedef interface IDMCircle2D IDMCircle2D;

#endif 	/* __IDMCircle2D_FWD_DEFINED__ */


#ifndef __IDMEllipse2D_FWD_DEFINED__
#define __IDMEllipse2D_FWD_DEFINED__
typedef interface IDMEllipse2D IDMEllipse2D;

#endif 	/* __IDMEllipse2D_FWD_DEFINED__ */


#ifndef __IDMLine2D_FWD_DEFINED__
#define __IDMLine2D_FWD_DEFINED__
typedef interface IDMLine2D IDMLine2D;

#endif 	/* __IDMLine2D_FWD_DEFINED__ */


#ifndef __IDMPolyline2D_FWD_DEFINED__
#define __IDMPolyline2D_FWD_DEFINED__
typedef interface IDMPolyline2D IDMPolyline2D;

#endif 	/* __IDMPolyline2D_FWD_DEFINED__ */


#ifndef __IDMBSplineCurve2D_FWD_DEFINED__
#define __IDMBSplineCurve2D_FWD_DEFINED__
typedef interface IDMBSplineCurve2D IDMBSplineCurve2D;

#endif 	/* __IDMBSplineCurve2D_FWD_DEFINED__ */


#ifndef __IDMReferenceKey_FWD_DEFINED__
#define __IDMReferenceKey_FWD_DEFINED__
typedef interface IDMReferenceKey IDMReferenceKey;

#endif 	/* __IDMReferenceKey_FWD_DEFINED__ */


#ifndef __IDMReference_FWD_DEFINED__
#define __IDMReference_FWD_DEFINED__
typedef interface IDMReference IDMReference;

#endif 	/* __IDMReference_FWD_DEFINED__ */


#ifndef __IEnumDMReferenceKeys_FWD_DEFINED__
#define __IEnumDMReferenceKeys_FWD_DEFINED__
typedef interface IEnumDMReferenceKeys IEnumDMReferenceKeys;

#endif 	/* __IEnumDMReferenceKeys_FWD_DEFINED__ */


#ifndef __IDMGeometricLocate_FWD_DEFINED__
#define __IDMGeometricLocate_FWD_DEFINED__
typedef interface IDMGeometricLocate IDMGeometricLocate;

#endif 	/* __IDMGeometricLocate_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "objidl.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_DMROOT_0000_0000 */
/* [local] */ 

#pragma warning(disable: 4005)
#define TARGET_IS_NT40_OR_LATER 1
#ifndef __WINDOWS_H__
#include <windows.h>
#endif
#ifndef __OBJBASE_H__
#include <objbase.h>
#endif
DEFINE_GUID(IID_IDMCurve, 0x0002D211, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMCurve2D, 0x0002D212, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMPlane, 0x0002D217, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMCircle, 0x0002D219, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMEllipse, 0x0002D21A, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMLine, 0x0002D21B, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMPolyLine, 0x0002D21C, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMBSplineCurve, 0x0002D21D, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMPLine, 0xb82efd0d, 0xfb56, 0x4210, 0xb2, 0x75, 0xfc, 0xdb, 0x71, 0xdf, 0x6d, 0xe4);
DEFINE_GUID(IID_IDMCircle2D, 0x0002D21E, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMEllipse2D, 0x0002D21F, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMLine2D, 0x0002D220, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMPolyLine2D, 0x0002D221, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMBSplineCurve2D, 0x0002D222, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
typedef /* [unique] */ IDMCurve2D *LPDMCURVE2D;

typedef /* [unique] */ IDMCurve *LPDMCURVE;

typedef /* [unique] */ IDMCircle *LPDMCIRCLE;

typedef /* [unique] */ IDMEllipse *LPDMELLIPSE;

typedef /* [unique] */ IDMLine *LPDMLINE;

typedef /* [unique] */ IDMPolyline *LPDMPOLYLINE;

typedef /* [unique] */ IDMBSplineCurve *LPDMBSPLINECURVE;

typedef /* [unique] */ IDMPLine *LPDMPLINE;

typedef /* [unique] */ IDMPlane *LPDMPLANE;

typedef /* [unique] */ IDMCircle2D *LPDMCIRCLE2D;

typedef /* [unique] */ IDMEllipse2D *LPDMELLIPSE2D;

typedef /* [unique] */ IDMLine2D *LPDMLINE2D;

typedef /* [unique] */ IDMPolyline2D *LPDMPOLYLINE2D;

typedef /* [unique] */ IDMBSplineCurve2D *LPDMBSPLINECURVE2D;



extern RPC_IF_HANDLE __MIDL_itf_DMROOT_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_DMROOT_0000_0000_v0_0_s_ifspec;

#ifndef __IDMPlane_INTERFACE_DEFINED__
#define __IDMPlane_INTERFACE_DEFINED__

/* interface IDMPlane */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMPlane;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D217-0000-0000-C000-000000000046")
    IDMPlane : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetPlaneData( 
            /* [out] */ double pRootPoint[ 3 ],
            /* [out] */ double pNormalVector[ 3 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMPlaneVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMPlane * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMPlane * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMPlane * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetPlaneData )( 
            IDMPlane * This,
            /* [out] */ double pRootPoint[ 3 ],
            /* [out] */ double pNormalVector[ 3 ]);
        
        END_INTERFACE
    } IDMPlaneVtbl;

    interface IDMPlane
    {
        CONST_VTBL struct IDMPlaneVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMPlane_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMPlane_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMPlane_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMPlane_GetPlaneData(This,pRootPoint,pNormalVector)	\
    ( (This)->lpVtbl -> GetPlaneData(This,pRootPoint,pNormalVector) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMPlane_INTERFACE_DEFINED__ */


#ifndef __IDMCurve_INTERFACE_DEFINED__
#define __IDMCurve_INTERFACE_DEFINED__

/* interface IDMCurve */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCurve;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D211-0000-0000-C000-000000000046")
    IDMCurve : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCurveType( 
            /* [out] */ IID *pIID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRangeBox( 
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetEndPoints( 
            /* [out] */ double pStartPoint[ 3 ],
            /* [out] */ double pEndPoint[ 3 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParamExtents( 
            /* [out] */ double *pMinParam,
            /* [out] */ double *pMaxParam) = 0;
        
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetParamAtPoint( 
            /* [in] */ ULONG nPoints,
            /* [in] */ double *pPoints,
            /* [in] */ double *pGuessParams,
            /* [out] */ double *pMaxDeviations,
            /* [out] */ double *pParams,
            /* [out] */ DWORD *pFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetPointAtParam( 
            /* [in] */ ULONG nParams,
            /* [in][size_is] */ double *pParams,
            /* [out][size_is] */ double *pPoints) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTangent( 
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out][size_is] */ double *pTangents) = 0;
        
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetCurvature( 
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out] */ double *pDirections,
            /* [out] */ double *pCurvatures) = 0;
        
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetDerivatives( 
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out] */ double *pFirstDerivs,
            /* [out] */ double *pSecondDerivs,
            /* [out] */ double *pThirdDerivs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParamAtLength( 
            /* [in] */ double FromParam,
            /* [in] */ double Length,
            /* [out] */ double *pParam) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLengthAtParam( 
            /* [in] */ double FromParam,
            /* [in] */ double ToParam,
            /* [out] */ double *pLength) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetContinuity( 
            /* [out] */ DWORD *nLevel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParamAnomaly( 
            /* [out] */ double pPeriodicity[ 2 ],
            /* [out] */ boolean *pIsSingular) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGeometryForm( 
            /* [out] */ DWORD *pForm) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCurveVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCurve * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCurve * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCurve * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurveType )( 
            IDMCurve * This,
            /* [out] */ IID *pIID);
        
        HRESULT ( STDMETHODCALLTYPE *GetRangeBox )( 
            IDMCurve * This,
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetEndPoints )( 
            IDMCurve * This,
            /* [out] */ double pStartPoint[ 3 ],
            /* [out] */ double pEndPoint[ 3 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetParamExtents )( 
            IDMCurve * This,
            /* [out] */ double *pMinParam,
            /* [out] */ double *pMaxParam);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *GetParamAtPoint )( 
            IDMCurve * This,
            /* [in] */ ULONG nPoints,
            /* [in] */ double *pPoints,
            /* [in] */ double *pGuessParams,
            /* [out] */ double *pMaxDeviations,
            /* [out] */ double *pParams,
            /* [out] */ DWORD *pFlags);
        
        HRESULT ( STDMETHODCALLTYPE *GetPointAtParam )( 
            IDMCurve * This,
            /* [in] */ ULONG nParams,
            /* [in][size_is] */ double *pParams,
            /* [out][size_is] */ double *pPoints);
        
        HRESULT ( STDMETHODCALLTYPE *GetTangent )( 
            IDMCurve * This,
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out][size_is] */ double *pTangents);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *GetCurvature )( 
            IDMCurve * This,
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out] */ double *pDirections,
            /* [out] */ double *pCurvatures);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *GetDerivatives )( 
            IDMCurve * This,
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out] */ double *pFirstDerivs,
            /* [out] */ double *pSecondDerivs,
            /* [out] */ double *pThirdDerivs);
        
        HRESULT ( STDMETHODCALLTYPE *GetParamAtLength )( 
            IDMCurve * This,
            /* [in] */ double FromParam,
            /* [in] */ double Length,
            /* [out] */ double *pParam);
        
        HRESULT ( STDMETHODCALLTYPE *GetLengthAtParam )( 
            IDMCurve * This,
            /* [in] */ double FromParam,
            /* [in] */ double ToParam,
            /* [out] */ double *pLength);
        
        HRESULT ( STDMETHODCALLTYPE *GetContinuity )( 
            IDMCurve * This,
            /* [out] */ DWORD *nLevel);
        
        HRESULT ( STDMETHODCALLTYPE *GetParamAnomaly )( 
            IDMCurve * This,
            /* [out] */ double pPeriodicity[ 2 ],
            /* [out] */ boolean *pIsSingular);
        
        HRESULT ( STDMETHODCALLTYPE *GetGeometryForm )( 
            IDMCurve * This,
            /* [out] */ DWORD *pForm);
        
        END_INTERFACE
    } IDMCurveVtbl;

    interface IDMCurve
    {
        CONST_VTBL struct IDMCurveVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCurve_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCurve_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCurve_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCurve_GetCurveType(This,pIID)	\
    ( (This)->lpVtbl -> GetCurveType(This,pIID) ) 

#define IDMCurve_GetRangeBox(This,pMinPoint,pMaxPoint)	\
    ( (This)->lpVtbl -> GetRangeBox(This,pMinPoint,pMaxPoint) ) 

#define IDMCurve_GetEndPoints(This,pStartPoint,pEndPoint)	\
    ( (This)->lpVtbl -> GetEndPoints(This,pStartPoint,pEndPoint) ) 

#define IDMCurve_GetParamExtents(This,pMinParam,pMaxParam)	\
    ( (This)->lpVtbl -> GetParamExtents(This,pMinParam,pMaxParam) ) 

#define IDMCurve_GetParamAtPoint(This,nPoints,pPoints,pGuessParams,pMaxDeviations,pParams,pFlags)	\
    ( (This)->lpVtbl -> GetParamAtPoint(This,nPoints,pPoints,pGuessParams,pMaxDeviations,pParams,pFlags) ) 

#define IDMCurve_GetPointAtParam(This,nParams,pParams,pPoints)	\
    ( (This)->lpVtbl -> GetPointAtParam(This,nParams,pParams,pPoints) ) 

#define IDMCurve_GetTangent(This,nParams,pParams,pTangents)	\
    ( (This)->lpVtbl -> GetTangent(This,nParams,pParams,pTangents) ) 

#define IDMCurve_GetCurvature(This,nParams,pParams,pDirections,pCurvatures)	\
    ( (This)->lpVtbl -> GetCurvature(This,nParams,pParams,pDirections,pCurvatures) ) 

#define IDMCurve_GetDerivatives(This,nParams,pParams,pFirstDerivs,pSecondDerivs,pThirdDerivs)	\
    ( (This)->lpVtbl -> GetDerivatives(This,nParams,pParams,pFirstDerivs,pSecondDerivs,pThirdDerivs) ) 

#define IDMCurve_GetParamAtLength(This,FromParam,Length,pParam)	\
    ( (This)->lpVtbl -> GetParamAtLength(This,FromParam,Length,pParam) ) 

#define IDMCurve_GetLengthAtParam(This,FromParam,ToParam,pLength)	\
    ( (This)->lpVtbl -> GetLengthAtParam(This,FromParam,ToParam,pLength) ) 

#define IDMCurve_GetContinuity(This,nLevel)	\
    ( (This)->lpVtbl -> GetContinuity(This,nLevel) ) 

#define IDMCurve_GetParamAnomaly(This,pPeriodicity,pIsSingular)	\
    ( (This)->lpVtbl -> GetParamAnomaly(This,pPeriodicity,pIsSingular) ) 

#define IDMCurve_GetGeometryForm(This,pForm)	\
    ( (This)->lpVtbl -> GetGeometryForm(This,pForm) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve_RemoteGetParamAtPoint_Proxy( 
    IDMCurve * This,
    /* [in] */ ULONG nPoints,
    /* [size_is][in] */ double *pPoints,
    /* [in] */ ULONG nGuessParams,
    /* [size_is][in] */ double *pGuessParams,
    /* [in] */ ULONG nMaxDeviations,
    /* [size_is][out] */ double *pMaxDeviations,
    /* [size_is][out] */ double *pParams,
    /* [in] */ ULONG nFlags,
    /* [size_is][out] */ DWORD *pFlags);


void __RPC_STUB IDMCurve_RemoteGetParamAtPoint_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve_RemoteGetCurvature_Proxy( 
    IDMCurve * This,
    /* [in] */ ULONG nParams,
    /* [size_is][in] */ double *pParams,
    /* [in] */ ULONG nDirections,
    /* [size_is][out] */ double *pDirections,
    /* [in] */ ULONG nCurvatures,
    /* [size_is][out] */ double *pCurvatures);


void __RPC_STUB IDMCurve_RemoteGetCurvature_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve_RemoteGetDerivatives_Proxy( 
    IDMCurve * This,
    /* [in] */ ULONG nParams,
    /* [size_is][in] */ double *pParams,
    /* [in] */ ULONG nFirstDerivs,
    /* [size_is][out] */ double *pFirstDerivs,
    /* [in] */ ULONG nSecondDerivs,
    /* [size_is][out] */ double *pSecondDerivs,
    /* [in] */ ULONG nThirdDerivs,
    /* [size_is][out] */ double *pThirdDerivs);


void __RPC_STUB IDMCurve_RemoteGetDerivatives_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IDMCurve_INTERFACE_DEFINED__ */


#ifndef __IDMCurve2D_INTERFACE_DEFINED__
#define __IDMCurve2D_INTERFACE_DEFINED__

/* interface IDMCurve2D */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCurve2D;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D212-0000-0000-C000-000000000046")
    IDMCurve2D : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCurveType( 
            /* [out] */ IID *pIID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRangeBox( 
            /* [out] */ double pMinPoint[ 2 ],
            /* [out] */ double pMaxPoint[ 2 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetEndPoints( 
            /* [out] */ double pStartPoint[ 2 ],
            /* [out] */ double pEndPoint[ 2 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParamExtents( 
            /* [out] */ double *pMinParam,
            /* [out] */ double *pMaxParam) = 0;
        
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetParamAtPoint( 
            /* [in] */ ULONG nPoints,
            /* [in] */ double *pPoints,
            /* [in] */ double *pGuessParams,
            /* [out] */ double *pMaxDeviations,
            /* [out] */ double *pParams,
            /* [out] */ DWORD *pFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetPointAtParam( 
            /* [in] */ ULONG nParams,
            /* [in][size_is] */ double *pParams,
            /* [out][size_is] */ double *pPoints) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTangent( 
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out][size_is] */ double *pTangents) = 0;
        
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetCurvature( 
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out] */ double *pDirections,
            /* [out] */ double *pCurvatures) = 0;
        
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetDerivatives( 
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out] */ double *pFirstDerivs,
            /* [out] */ double *pSecondDerivs,
            /* [out] */ double *pThirdDerivs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParamAtLength( 
            /* [in] */ double FromParam,
            /* [in] */ double Length,
            /* [out] */ double *pParam) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLengthAtParam( 
            /* [in] */ double FromParam,
            /* [in] */ double ToParam,
            /* [out] */ double *pLength) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetContinuity( 
            /* [out] */ DWORD *nLevel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParamAnomaly( 
            /* [out] */ double pPeriodicity[ 2 ],
            /* [out] */ boolean *pIsSingular) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGeometryForm( 
            /* [out] */ DWORD *pForm) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCurve2DVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCurve2D * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCurve2D * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCurve2D * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurveType )( 
            IDMCurve2D * This,
            /* [out] */ IID *pIID);
        
        HRESULT ( STDMETHODCALLTYPE *GetRangeBox )( 
            IDMCurve2D * This,
            /* [out] */ double pMinPoint[ 2 ],
            /* [out] */ double pMaxPoint[ 2 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetEndPoints )( 
            IDMCurve2D * This,
            /* [out] */ double pStartPoint[ 2 ],
            /* [out] */ double pEndPoint[ 2 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetParamExtents )( 
            IDMCurve2D * This,
            /* [out] */ double *pMinParam,
            /* [out] */ double *pMaxParam);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *GetParamAtPoint )( 
            IDMCurve2D * This,
            /* [in] */ ULONG nPoints,
            /* [in] */ double *pPoints,
            /* [in] */ double *pGuessParams,
            /* [out] */ double *pMaxDeviations,
            /* [out] */ double *pParams,
            /* [out] */ DWORD *pFlags);
        
        HRESULT ( STDMETHODCALLTYPE *GetPointAtParam )( 
            IDMCurve2D * This,
            /* [in] */ ULONG nParams,
            /* [in][size_is] */ double *pParams,
            /* [out][size_is] */ double *pPoints);
        
        HRESULT ( STDMETHODCALLTYPE *GetTangent )( 
            IDMCurve2D * This,
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out][size_is] */ double *pTangents);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *GetCurvature )( 
            IDMCurve2D * This,
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out] */ double *pDirections,
            /* [out] */ double *pCurvatures);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *GetDerivatives )( 
            IDMCurve2D * This,
            /* [in] */ ULONG nParams,
            /* [in] */ double *pParams,
            /* [out] */ double *pFirstDerivs,
            /* [out] */ double *pSecondDerivs,
            /* [out] */ double *pThirdDerivs);
        
        HRESULT ( STDMETHODCALLTYPE *GetParamAtLength )( 
            IDMCurve2D * This,
            /* [in] */ double FromParam,
            /* [in] */ double Length,
            /* [out] */ double *pParam);
        
        HRESULT ( STDMETHODCALLTYPE *GetLengthAtParam )( 
            IDMCurve2D * This,
            /* [in] */ double FromParam,
            /* [in] */ double ToParam,
            /* [out] */ double *pLength);
        
        HRESULT ( STDMETHODCALLTYPE *GetContinuity )( 
            IDMCurve2D * This,
            /* [out] */ DWORD *nLevel);
        
        HRESULT ( STDMETHODCALLTYPE *GetParamAnomaly )( 
            IDMCurve2D * This,
            /* [out] */ double pPeriodicity[ 2 ],
            /* [out] */ boolean *pIsSingular);
        
        HRESULT ( STDMETHODCALLTYPE *GetGeometryForm )( 
            IDMCurve2D * This,
            /* [out] */ DWORD *pForm);
        
        END_INTERFACE
    } IDMCurve2DVtbl;

    interface IDMCurve2D
    {
        CONST_VTBL struct IDMCurve2DVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCurve2D_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCurve2D_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCurve2D_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCurve2D_GetCurveType(This,pIID)	\
    ( (This)->lpVtbl -> GetCurveType(This,pIID) ) 

#define IDMCurve2D_GetRangeBox(This,pMinPoint,pMaxPoint)	\
    ( (This)->lpVtbl -> GetRangeBox(This,pMinPoint,pMaxPoint) ) 

#define IDMCurve2D_GetEndPoints(This,pStartPoint,pEndPoint)	\
    ( (This)->lpVtbl -> GetEndPoints(This,pStartPoint,pEndPoint) ) 

#define IDMCurve2D_GetParamExtents(This,pMinParam,pMaxParam)	\
    ( (This)->lpVtbl -> GetParamExtents(This,pMinParam,pMaxParam) ) 

#define IDMCurve2D_GetParamAtPoint(This,nPoints,pPoints,pGuessParams,pMaxDeviations,pParams,pFlags)	\
    ( (This)->lpVtbl -> GetParamAtPoint(This,nPoints,pPoints,pGuessParams,pMaxDeviations,pParams,pFlags) ) 

#define IDMCurve2D_GetPointAtParam(This,nParams,pParams,pPoints)	\
    ( (This)->lpVtbl -> GetPointAtParam(This,nParams,pParams,pPoints) ) 

#define IDMCurve2D_GetTangent(This,nParams,pParams,pTangents)	\
    ( (This)->lpVtbl -> GetTangent(This,nParams,pParams,pTangents) ) 

#define IDMCurve2D_GetCurvature(This,nParams,pParams,pDirections,pCurvatures)	\
    ( (This)->lpVtbl -> GetCurvature(This,nParams,pParams,pDirections,pCurvatures) ) 

#define IDMCurve2D_GetDerivatives(This,nParams,pParams,pFirstDerivs,pSecondDerivs,pThirdDerivs)	\
    ( (This)->lpVtbl -> GetDerivatives(This,nParams,pParams,pFirstDerivs,pSecondDerivs,pThirdDerivs) ) 

#define IDMCurve2D_GetParamAtLength(This,FromParam,Length,pParam)	\
    ( (This)->lpVtbl -> GetParamAtLength(This,FromParam,Length,pParam) ) 

#define IDMCurve2D_GetLengthAtParam(This,FromParam,ToParam,pLength)	\
    ( (This)->lpVtbl -> GetLengthAtParam(This,FromParam,ToParam,pLength) ) 

#define IDMCurve2D_GetContinuity(This,nLevel)	\
    ( (This)->lpVtbl -> GetContinuity(This,nLevel) ) 

#define IDMCurve2D_GetParamAnomaly(This,pPeriodicity,pIsSingular)	\
    ( (This)->lpVtbl -> GetParamAnomaly(This,pPeriodicity,pIsSingular) ) 

#define IDMCurve2D_GetGeometryForm(This,pForm)	\
    ( (This)->lpVtbl -> GetGeometryForm(This,pForm) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve2D_RemoteGetParamAtPoint_Proxy( 
    IDMCurve2D * This,
    /* [in] */ ULONG nPoints,
    /* [size_is][in] */ double *pPoints,
    /* [in] */ ULONG nGuessParams,
    /* [size_is][in] */ double *pGuessParams,
    /* [in] */ ULONG nMaxDeviations,
    /* [size_is][out] */ double *pMaxDeviations,
    /* [size_is][out] */ double *pParams,
    /* [in] */ ULONG nFlags,
    /* [size_is][out] */ DWORD *pFlags);


void __RPC_STUB IDMCurve2D_RemoteGetParamAtPoint_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve2D_RemoteGetCurvature_Proxy( 
    IDMCurve2D * This,
    /* [in] */ ULONG nParams,
    /* [size_is][in] */ double *pParams,
    /* [in] */ ULONG nDirections,
    /* [size_is][out] */ double *pDirections,
    /* [in] */ ULONG nCurvatures,
    /* [size_is][out] */ double *pCurvatures);


void __RPC_STUB IDMCurve2D_RemoteGetCurvature_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve2D_RemoteGetDerivatives_Proxy( 
    IDMCurve2D * This,
    /* [in] */ ULONG nParams,
    /* [size_is][in] */ double *pParams,
    /* [in] */ ULONG nFirstDerivs,
    /* [size_is][out] */ double *pFirstDerivs,
    /* [in] */ ULONG nSecondDerivs,
    /* [size_is][out] */ double *pSecondDerivs,
    /* [in] */ ULONG nThirdDerivs,
    /* [size_is][out] */ double *pThirdDerivs);


void __RPC_STUB IDMCurve2D_RemoteGetDerivatives_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IDMCurve2D_INTERFACE_DEFINED__ */


#ifndef __IDMCircle_INTERFACE_DEFINED__
#define __IDMCircle_INTERFACE_DEFINED__

/* interface IDMCircle */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCircle;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D219-0000-0000-C000-000000000046")
    IDMCircle : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCircleData( 
            /* [out] */ double pBasePoint[ 3 ],
            /* [out] */ double pAxisVector[ 3 ],
            /* [out] */ double *pRadius) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCircleVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCircle * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCircle * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCircle * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetCircleData )( 
            IDMCircle * This,
            /* [out] */ double pBasePoint[ 3 ],
            /* [out] */ double pAxisVector[ 3 ],
            /* [out] */ double *pRadius);
        
        END_INTERFACE
    } IDMCircleVtbl;

    interface IDMCircle
    {
        CONST_VTBL struct IDMCircleVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCircle_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCircle_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCircle_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCircle_GetCircleData(This,pBasePoint,pAxisVector,pRadius)	\
    ( (This)->lpVtbl -> GetCircleData(This,pBasePoint,pAxisVector,pRadius) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMCircle_INTERFACE_DEFINED__ */


#ifndef __IDMEllipse_INTERFACE_DEFINED__
#define __IDMEllipse_INTERFACE_DEFINED__

/* interface IDMEllipse */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMEllipse;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D21A-0000-0000-C000-000000000046")
    IDMEllipse : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetEllipseData( 
            /* [out] */ double pBasePoint[ 3 ],
            /* [out] */ double pAxisVector[ 3 ],
            /* [out] */ double pMajorAxis[ 3 ],
            /* [out] */ double *pMinorMajorRatio) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMEllipseVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMEllipse * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMEllipse * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMEllipse * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetEllipseData )( 
            IDMEllipse * This,
            /* [out] */ double pBasePoint[ 3 ],
            /* [out] */ double pAxisVector[ 3 ],
            /* [out] */ double pMajorAxis[ 3 ],
            /* [out] */ double *pMinorMajorRatio);
        
        END_INTERFACE
    } IDMEllipseVtbl;

    interface IDMEllipse
    {
        CONST_VTBL struct IDMEllipseVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMEllipse_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMEllipse_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMEllipse_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMEllipse_GetEllipseData(This,pBasePoint,pAxisVector,pMajorAxis,pMinorMajorRatio)	\
    ( (This)->lpVtbl -> GetEllipseData(This,pBasePoint,pAxisVector,pMajorAxis,pMinorMajorRatio) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMEllipse_INTERFACE_DEFINED__ */


#ifndef __IDMLine_INTERFACE_DEFINED__
#define __IDMLine_INTERFACE_DEFINED__

/* interface IDMLine */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMLine;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D21B-0000-0000-C000-000000000046")
    IDMLine : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetLineData( 
            /* [out] */ double pRootPoint[ 3 ],
            /* [out] */ double pDirectionVector[ 3 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMLineVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMLine * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMLine * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMLine * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetLineData )( 
            IDMLine * This,
            /* [out] */ double pRootPoint[ 3 ],
            /* [out] */ double pDirectionVector[ 3 ]);
        
        END_INTERFACE
    } IDMLineVtbl;

    interface IDMLine
    {
        CONST_VTBL struct IDMLineVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMLine_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMLine_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMLine_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMLine_GetLineData(This,pRootPoint,pDirectionVector)	\
    ( (This)->lpVtbl -> GetLineData(This,pRootPoint,pDirectionVector) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMLine_INTERFACE_DEFINED__ */


#ifndef __IDMPLine_INTERFACE_DEFINED__
#define __IDMPLine_INTERFACE_DEFINED__

/* interface IDMPLine */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMPLine;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B82EFD0D-FB56-4210-B275-FCDB71DF6DE4")
    IDMPLine : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetPLineData( 
            /* [out] */ long *PointsCount,
            /* [out] */ double **ppPoints,
            /* [out] */ boolean *Closed,
            /* [out] */ double *BaseParameter) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMPLineVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMPLine * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMPLine * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMPLine * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetPLineData )( 
            IDMPLine * This,
            /* [out] */ long *PointsCount,
            /* [out] */ double **ppPoints,
            /* [out] */ boolean *Closed,
            /* [out] */ double *BaseParameter);
        
        END_INTERFACE
    } IDMPLineVtbl;

    interface IDMPLine
    {
        CONST_VTBL struct IDMPLineVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMPLine_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMPLine_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMPLine_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMPLine_GetPLineData(This,PointsCount,ppPoints,Closed,BaseParameter)	\
    ( (This)->lpVtbl -> GetPLineData(This,PointsCount,ppPoints,Closed,BaseParameter) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMPLine_INTERFACE_DEFINED__ */


#ifndef __IDMPolyline_INTERFACE_DEFINED__
#define __IDMPolyline_INTERFACE_DEFINED__

/* interface IDMPolyline */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMPolyline;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D21C-0000-0000-C000-000000000046")
    IDMPolyline : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetPolylineInfo( 
            /* [out] */ ULONG *pNumPoints,
            /* [out] */ boolean *pIsClosed,
            /* [out] */ boolean *pIsPlanar,
            /* [out] */ double pPlaneVector[ 3 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetPolylineData( 
            /* [in] */ ULONG nPoints,
            /* [out][size_is] */ double *pPoints) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMPolylineVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMPolyline * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMPolyline * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMPolyline * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetPolylineInfo )( 
            IDMPolyline * This,
            /* [out] */ ULONG *pNumPoints,
            /* [out] */ boolean *pIsClosed,
            /* [out] */ boolean *pIsPlanar,
            /* [out] */ double pPlaneVector[ 3 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetPolylineData )( 
            IDMPolyline * This,
            /* [in] */ ULONG nPoints,
            /* [out][size_is] */ double *pPoints);
        
        END_INTERFACE
    } IDMPolylineVtbl;

    interface IDMPolyline
    {
        CONST_VTBL struct IDMPolylineVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMPolyline_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMPolyline_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMPolyline_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMPolyline_GetPolylineInfo(This,pNumPoints,pIsClosed,pIsPlanar,pPlaneVector)	\
    ( (This)->lpVtbl -> GetPolylineInfo(This,pNumPoints,pIsClosed,pIsPlanar,pPlaneVector) ) 

#define IDMPolyline_GetPolylineData(This,nPoints,pPoints)	\
    ( (This)->lpVtbl -> GetPolylineData(This,nPoints,pPoints) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMPolyline_INTERFACE_DEFINED__ */


#ifndef __IDMBSplineCurve_INTERFACE_DEFINED__
#define __IDMBSplineCurve_INTERFACE_DEFINED__

/* interface IDMBSplineCurve */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMBSplineCurve;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D21D-0000-0000-C000-000000000046")
    IDMBSplineCurve : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetBSplineInfo( 
            /* [out] */ ULONG *pOrder,
            /* [out] */ ULONG *pNumPoles,
            /* [out] */ ULONG *pNumKnots,
            /* [out] */ boolean *pIsRational,
            /* [out] */ boolean *pIsClosed,
            /* [out] */ boolean *pIsPeriodic,
            /* [out] */ boolean *pIsPlanar,
            /* [out] */ double pPlaneVector[ 3 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBSplineData( 
            /* [in] */ ULONG nPoles,
            /* [in] */ ULONG nKnots,
            /* [in] */ ULONG nWeights,
            /* [out][size_is] */ double *pPoles,
            /* [out][size_is] */ double *pKnots,
            /* [out][size_is] */ double *pWeights) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMBSplineCurveVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMBSplineCurve * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMBSplineCurve * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMBSplineCurve * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetBSplineInfo )( 
            IDMBSplineCurve * This,
            /* [out] */ ULONG *pOrder,
            /* [out] */ ULONG *pNumPoles,
            /* [out] */ ULONG *pNumKnots,
            /* [out] */ boolean *pIsRational,
            /* [out] */ boolean *pIsClosed,
            /* [out] */ boolean *pIsPeriodic,
            /* [out] */ boolean *pIsPlanar,
            /* [out] */ double pPlaneVector[ 3 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetBSplineData )( 
            IDMBSplineCurve * This,
            /* [in] */ ULONG nPoles,
            /* [in] */ ULONG nKnots,
            /* [in] */ ULONG nWeights,
            /* [out][size_is] */ double *pPoles,
            /* [out][size_is] */ double *pKnots,
            /* [out][size_is] */ double *pWeights);
        
        END_INTERFACE
    } IDMBSplineCurveVtbl;

    interface IDMBSplineCurve
    {
        CONST_VTBL struct IDMBSplineCurveVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMBSplineCurve_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMBSplineCurve_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMBSplineCurve_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMBSplineCurve_GetBSplineInfo(This,pOrder,pNumPoles,pNumKnots,pIsRational,pIsClosed,pIsPeriodic,pIsPlanar,pPlaneVector)	\
    ( (This)->lpVtbl -> GetBSplineInfo(This,pOrder,pNumPoles,pNumKnots,pIsRational,pIsClosed,pIsPeriodic,pIsPlanar,pPlaneVector) ) 

#define IDMBSplineCurve_GetBSplineData(This,nPoles,nKnots,nWeights,pPoles,pKnots,pWeights)	\
    ( (This)->lpVtbl -> GetBSplineData(This,nPoles,nKnots,nWeights,pPoles,pKnots,pWeights) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMBSplineCurve_INTERFACE_DEFINED__ */


#ifndef __IDMCircle2D_INTERFACE_DEFINED__
#define __IDMCircle2D_INTERFACE_DEFINED__

/* interface IDMCircle2D */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCircle2D;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D21E-0000-0000-C000-000000000046")
    IDMCircle2D : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCircleData( 
            /* [out] */ double pBasePoint[ 2 ],
            /* [out] */ double *pRadius) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCircle2DVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCircle2D * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCircle2D * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCircle2D * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetCircleData )( 
            IDMCircle2D * This,
            /* [out] */ double pBasePoint[ 2 ],
            /* [out] */ double *pRadius);
        
        END_INTERFACE
    } IDMCircle2DVtbl;

    interface IDMCircle2D
    {
        CONST_VTBL struct IDMCircle2DVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCircle2D_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCircle2D_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCircle2D_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCircle2D_GetCircleData(This,pBasePoint,pRadius)	\
    ( (This)->lpVtbl -> GetCircleData(This,pBasePoint,pRadius) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMCircle2D_INTERFACE_DEFINED__ */


#ifndef __IDMEllipse2D_INTERFACE_DEFINED__
#define __IDMEllipse2D_INTERFACE_DEFINED__

/* interface IDMEllipse2D */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMEllipse2D;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D21F-0000-0000-C000-000000000046")
    IDMEllipse2D : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetEllipseData( 
            /* [out] */ double pBasePoint[ 2 ],
            /* [out] */ double pMajorAxis[ 2 ],
            /* [out] */ double *pMinorMajorRatio) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMEllipse2DVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMEllipse2D * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMEllipse2D * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMEllipse2D * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetEllipseData )( 
            IDMEllipse2D * This,
            /* [out] */ double pBasePoint[ 2 ],
            /* [out] */ double pMajorAxis[ 2 ],
            /* [out] */ double *pMinorMajorRatio);
        
        END_INTERFACE
    } IDMEllipse2DVtbl;

    interface IDMEllipse2D
    {
        CONST_VTBL struct IDMEllipse2DVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMEllipse2D_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMEllipse2D_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMEllipse2D_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMEllipse2D_GetEllipseData(This,pBasePoint,pMajorAxis,pMinorMajorRatio)	\
    ( (This)->lpVtbl -> GetEllipseData(This,pBasePoint,pMajorAxis,pMinorMajorRatio) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMEllipse2D_INTERFACE_DEFINED__ */


#ifndef __IDMLine2D_INTERFACE_DEFINED__
#define __IDMLine2D_INTERFACE_DEFINED__

/* interface IDMLine2D */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMLine2D;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D220-0000-0000-C000-000000000046")
    IDMLine2D : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetLineData( 
            /* [out] */ double pRootPoint[ 2 ],
            /* [out] */ double pDirectionVector[ 2 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMLine2DVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMLine2D * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMLine2D * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMLine2D * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetLineData )( 
            IDMLine2D * This,
            /* [out] */ double pRootPoint[ 2 ],
            /* [out] */ double pDirectionVector[ 2 ]);
        
        END_INTERFACE
    } IDMLine2DVtbl;

    interface IDMLine2D
    {
        CONST_VTBL struct IDMLine2DVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMLine2D_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMLine2D_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMLine2D_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMLine2D_GetLineData(This,pRootPoint,pDirectionVector)	\
    ( (This)->lpVtbl -> GetLineData(This,pRootPoint,pDirectionVector) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMLine2D_INTERFACE_DEFINED__ */


#ifndef __IDMPolyline2D_INTERFACE_DEFINED__
#define __IDMPolyline2D_INTERFACE_DEFINED__

/* interface IDMPolyline2D */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMPolyline2D;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D221-0000-0000-C000-000000000046")
    IDMPolyline2D : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetPolylineInfo( 
            /* [out] */ ULONG *pNumPoints,
            /* [out] */ boolean *pIsClosed) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetPolylineData( 
            /* [in] */ ULONG nPoints,
            /* [out][size_is] */ double *pPoints) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMPolyline2DVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMPolyline2D * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMPolyline2D * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMPolyline2D * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetPolylineInfo )( 
            IDMPolyline2D * This,
            /* [out] */ ULONG *pNumPoints,
            /* [out] */ boolean *pIsClosed);
        
        HRESULT ( STDMETHODCALLTYPE *GetPolylineData )( 
            IDMPolyline2D * This,
            /* [in] */ ULONG nPoints,
            /* [out][size_is] */ double *pPoints);
        
        END_INTERFACE
    } IDMPolyline2DVtbl;

    interface IDMPolyline2D
    {
        CONST_VTBL struct IDMPolyline2DVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMPolyline2D_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMPolyline2D_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMPolyline2D_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMPolyline2D_GetPolylineInfo(This,pNumPoints,pIsClosed)	\
    ( (This)->lpVtbl -> GetPolylineInfo(This,pNumPoints,pIsClosed) ) 

#define IDMPolyline2D_GetPolylineData(This,nPoints,pPoints)	\
    ( (This)->lpVtbl -> GetPolylineData(This,nPoints,pPoints) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMPolyline2D_INTERFACE_DEFINED__ */


#ifndef __IDMBSplineCurve2D_INTERFACE_DEFINED__
#define __IDMBSplineCurve2D_INTERFACE_DEFINED__

/* interface IDMBSplineCurve2D */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMBSplineCurve2D;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D222-0000-0000-C000-000000000046")
    IDMBSplineCurve2D : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetBSplineInfo( 
            /* [out] */ ULONG *pOrder,
            /* [out] */ ULONG *pNumPoles,
            /* [out] */ ULONG *pNumKnots,
            /* [out] */ boolean *pIsRational,
            /* [out] */ boolean *pIsClosed,
            /* [out] */ boolean *pIsPeriodic) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBSplineData( 
            /* [in] */ ULONG nPoles,
            /* [in] */ ULONG nKnots,
            /* [in] */ ULONG nWeights,
            /* [out][size_is] */ double *pPoles,
            /* [out][size_is] */ double *pKnots,
            /* [out][size_is] */ double *pWeights) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMBSplineCurve2DVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMBSplineCurve2D * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMBSplineCurve2D * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMBSplineCurve2D * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetBSplineInfo )( 
            IDMBSplineCurve2D * This,
            /* [out] */ ULONG *pOrder,
            /* [out] */ ULONG *pNumPoles,
            /* [out] */ ULONG *pNumKnots,
            /* [out] */ boolean *pIsRational,
            /* [out] */ boolean *pIsClosed,
            /* [out] */ boolean *pIsPeriodic);
        
        HRESULT ( STDMETHODCALLTYPE *GetBSplineData )( 
            IDMBSplineCurve2D * This,
            /* [in] */ ULONG nPoles,
            /* [in] */ ULONG nKnots,
            /* [in] */ ULONG nWeights,
            /* [out][size_is] */ double *pPoles,
            /* [out][size_is] */ double *pKnots,
            /* [out][size_is] */ double *pWeights);
        
        END_INTERFACE
    } IDMBSplineCurve2DVtbl;

    interface IDMBSplineCurve2D
    {
        CONST_VTBL struct IDMBSplineCurve2DVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMBSplineCurve2D_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMBSplineCurve2D_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMBSplineCurve2D_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMBSplineCurve2D_GetBSplineInfo(This,pOrder,pNumPoles,pNumKnots,pIsRational,pIsClosed,pIsPeriodic)	\
    ( (This)->lpVtbl -> GetBSplineInfo(This,pOrder,pNumPoles,pNumKnots,pIsRational,pIsClosed,pIsPeriodic) ) 

#define IDMBSplineCurve2D_GetBSplineData(This,nPoles,nKnots,nWeights,pPoles,pKnots,pWeights)	\
    ( (This)->lpVtbl -> GetBSplineData(This,nPoles,nKnots,nWeights,pPoles,pKnots,pWeights) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMBSplineCurve2D_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_DMROOT_0000_0014 */
/* [local] */ 

DEFINE_GUID(IID_IDMReferenceKey, 0x0002D22B, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMReference, 0x0002D22C, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IEnumDMReferenceKeys, 0x0002D22D, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
typedef /* [unique] */ IDMReferenceKey *LPDMREFERENCEKEY;

typedef /* [unique] */ IDMReference *LPDMREFERENCE;

typedef /* [unique] */ IEnumDMReferenceKeys *LPENUM_DMREFERENCEKEYS;



extern RPC_IF_HANDLE __MIDL_itf_DMROOT_0000_0014_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_DMROOT_0000_0014_v0_0_s_ifspec;

#ifndef __IDMReferenceKey_INTERFACE_DEFINED__
#define __IDMReferenceKey_INTERFACE_DEFINED__

/* interface IDMReferenceKey */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMReferenceKey;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D22B-0000-0000-C000-000000000046")
    IDMReferenceKey : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetObjectType( 
            /* [out] */ IID *pRefIID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetKeySize( 
            /* [out] */ ULONG *pcbKeySize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetKey( 
            /* [in] */ ULONG cbKeySize,
            /* [out][size_is] */ BYTE *pKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModifyCounter( 
            /* [out] */ long *pCounter) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMReferenceKeyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMReferenceKey * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMReferenceKey * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMReferenceKey * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectType )( 
            IDMReferenceKey * This,
            /* [out] */ IID *pRefIID);
        
        HRESULT ( STDMETHODCALLTYPE *GetKeySize )( 
            IDMReferenceKey * This,
            /* [out] */ ULONG *pcbKeySize);
        
        HRESULT ( STDMETHODCALLTYPE *GetKey )( 
            IDMReferenceKey * This,
            /* [in] */ ULONG cbKeySize,
            /* [out][size_is] */ BYTE *pKey);
        
        HRESULT ( STDMETHODCALLTYPE *GetModifyCounter )( 
            IDMReferenceKey * This,
            /* [out] */ long *pCounter);
        
        END_INTERFACE
    } IDMReferenceKeyVtbl;

    interface IDMReferenceKey
    {
        CONST_VTBL struct IDMReferenceKeyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMReferenceKey_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMReferenceKey_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMReferenceKey_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMReferenceKey_GetObjectType(This,pRefIID)	\
    ( (This)->lpVtbl -> GetObjectType(This,pRefIID) ) 

#define IDMReferenceKey_GetKeySize(This,pcbKeySize)	\
    ( (This)->lpVtbl -> GetKeySize(This,pcbKeySize) ) 

#define IDMReferenceKey_GetKey(This,cbKeySize,pKey)	\
    ( (This)->lpVtbl -> GetKey(This,cbKeySize,pKey) ) 

#define IDMReferenceKey_GetModifyCounter(This,pCounter)	\
    ( (This)->lpVtbl -> GetModifyCounter(This,pCounter) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMReferenceKey_INTERFACE_DEFINED__ */


#ifndef __IDMReference_INTERFACE_DEFINED__
#define __IDMReference_INTERFACE_DEFINED__

/* interface IDMReference */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMReference;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D22C-0000-0000-C000-000000000046")
    IDMReference : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE BindKeyToInterface( 
            /* [in] */ REFIID IDMInterface,
            /* [in] */ DWORD cSizeInBytes,
            /* [size_is][in] */ BYTE *pKey,
            /* [iid_is][out] */ void **ppvObject) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMReferenceVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMReference * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMReference * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMReference * This);
        
        HRESULT ( STDMETHODCALLTYPE *BindKeyToInterface )( 
            IDMReference * This,
            /* [in] */ REFIID IDMInterface,
            /* [in] */ DWORD cSizeInBytes,
            /* [size_is][in] */ BYTE *pKey,
            /* [iid_is][out] */ void **ppvObject);
        
        END_INTERFACE
    } IDMReferenceVtbl;

    interface IDMReference
    {
        CONST_VTBL struct IDMReferenceVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMReference_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMReference_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMReference_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMReference_BindKeyToInterface(This,IDMInterface,cSizeInBytes,pKey,ppvObject)	\
    ( (This)->lpVtbl -> BindKeyToInterface(This,IDMInterface,cSizeInBytes,pKey,ppvObject) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMReference_INTERFACE_DEFINED__ */


#ifndef __IEnumDMReferenceKeys_INTERFACE_DEFINED__
#define __IEnumDMReferenceKeys_INTERFACE_DEFINED__

/* interface IEnumDMReferenceKeys */
/* [object][uuid] */ 


EXTERN_C const IID IID_IEnumDMReferenceKeys;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D22D-0000-0000-C000-000000000046")
    IEnumDMReferenceKeys : public IUnknown
    {
    public:
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG cRefKey,
            /* [out] */ LPDMREFERENCEKEY *pRefKey,
            /* [out] */ ULONG *pcFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG cRefKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ LPENUM_DMREFERENCEKEYS *pEnumReferenceKeys) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IEnumDMReferenceKeysVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumDMReferenceKeys * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumDMReferenceKeys * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumDMReferenceKeys * This);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumDMReferenceKeys * This,
            /* [in] */ ULONG cRefKey,
            /* [out] */ LPDMREFERENCEKEY *pRefKey,
            /* [out] */ ULONG *pcFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumDMReferenceKeys * This,
            /* [in] */ ULONG cRefKey);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumDMReferenceKeys * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumDMReferenceKeys * This,
            /* [out] */ LPENUM_DMREFERENCEKEYS *pEnumReferenceKeys);
        
        END_INTERFACE
    } IEnumDMReferenceKeysVtbl;

    interface IEnumDMReferenceKeys
    {
        CONST_VTBL struct IEnumDMReferenceKeysVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumDMReferenceKeys_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IEnumDMReferenceKeys_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IEnumDMReferenceKeys_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IEnumDMReferenceKeys_Next(This,cRefKey,pRefKey,pcFetched)	\
    ( (This)->lpVtbl -> Next(This,cRefKey,pRefKey,pcFetched) ) 

#define IEnumDMReferenceKeys_Skip(This,cRefKey)	\
    ( (This)->lpVtbl -> Skip(This,cRefKey) ) 

#define IEnumDMReferenceKeys_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define IEnumDMReferenceKeys_Clone(This,pEnumReferenceKeys)	\
    ( (This)->lpVtbl -> Clone(This,pEnumReferenceKeys) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMReferenceKeys_RemoteNext_Proxy( 
    IEnumDMReferenceKeys * This,
    /* [in] */ ULONG cRefKey,
    /* [length_is][size_is][out] */ LPDMREFERENCEKEY *pRefKey,
    /* [out] */ ULONG *pcFetched);


void __RPC_STUB IEnumDMReferenceKeys_RemoteNext_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumDMReferenceKeys_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_DMROOT_0000_0017 */
/* [local] */ 

DEFINE_GUID(IID_IDMGeometricLocate, 0x0002D22E, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
typedef /* [unique] */ IDMGeometricLocate *LPDMGEOMETRICLOCATE;

typedef struct tagDMBoreLine
    {
    double m_point[ 3 ];
    double m_direction[ 3 ];
    double m_front;
    double m_back;
    double m_radius;
    } 	DMBORELINE;

typedef DMBORELINE *LPDMBORELINE;

typedef 
enum tagDMSELECTTYPE
    {
        SELECTTYPE_INSIDE	= 0x1,
        SELECTTYPE_OVERLAP	= 0x2
    } 	DMSELECTTYPE;

typedef struct tagDMShape
    {
    ULONG m_nPoints;
    double *m_pPoints;
    double m_direction[ 3 ];
    double m_front;
    double m_back;
    DWORD m_type;
    } 	DMSHAPE;

typedef DMSHAPE *LPDMSHAPE;



extern RPC_IF_HANDLE __MIDL_itf_DMROOT_0000_0017_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_DMROOT_0000_0017_v0_0_s_ifspec;

#ifndef __IDMGeometricLocate_INTERFACE_DEFINED__
#define __IDMGeometricLocate_INTERFACE_DEFINED__

/* interface IDMGeometricLocate */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMGeometricLocate;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D22E-0000-0000-C000-000000000046")
    IDMGeometricLocate : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE PointLocate( 
            /* [in] */ LPDMBORELINE pBoreline,
            /* [in] */ ULONG nTypes,
            /* [in][size_is] */ IID types[  ],
            /* [out] */ LPENUM_DMREFERENCEKEYS *pEnumReferenceKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ShapeLocate( 
            /* [in] */ LPDMSHAPE pShape,
            /* [in] */ ULONG nTypes,
            /* [in][size_is] */ IID types[  ],
            /* [out] */ LPENUM_DMREFERENCEKEYS *pEnumReferenceKey) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMGeometricLocateVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMGeometricLocate * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMGeometricLocate * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMGeometricLocate * This);
        
        HRESULT ( STDMETHODCALLTYPE *PointLocate )( 
            IDMGeometricLocate * This,
            /* [in] */ LPDMBORELINE pBoreline,
            /* [in] */ ULONG nTypes,
            /* [in][size_is] */ IID types[  ],
            /* [out] */ LPENUM_DMREFERENCEKEYS *pEnumReferenceKey);
        
        HRESULT ( STDMETHODCALLTYPE *ShapeLocate )( 
            IDMGeometricLocate * This,
            /* [in] */ LPDMSHAPE pShape,
            /* [in] */ ULONG nTypes,
            /* [in][size_is] */ IID types[  ],
            /* [out] */ LPENUM_DMREFERENCEKEYS *pEnumReferenceKey);
        
        END_INTERFACE
    } IDMGeometricLocateVtbl;

    interface IDMGeometricLocate
    {
        CONST_VTBL struct IDMGeometricLocateVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMGeometricLocate_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMGeometricLocate_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMGeometricLocate_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMGeometricLocate_PointLocate(This,pBoreline,nTypes,types,pEnumReferenceKey)	\
    ( (This)->lpVtbl -> PointLocate(This,pBoreline,nTypes,types,pEnumReferenceKey) ) 

#define IDMGeometricLocate_ShapeLocate(This,pShape,nTypes,types,pEnumReferenceKey)	\
    ( (This)->lpVtbl -> ShapeLocate(This,pShape,nTypes,types,pEnumReferenceKey) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMGeometricLocate_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* [local] */ HRESULT STDMETHODCALLTYPE IDMCurve_GetParamAtPoint_Proxy( 
    IDMCurve * This,
    /* [in] */ ULONG nPoints,
    /* [in] */ double *pPoints,
    /* [in] */ double *pGuessParams,
    /* [out] */ double *pMaxDeviations,
    /* [out] */ double *pParams,
    /* [out] */ DWORD *pFlags);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve_GetParamAtPoint_Stub( 
    IDMCurve * This,
    /* [in] */ ULONG nPoints,
    /* [size_is][in] */ double *pPoints,
    /* [in] */ ULONG nGuessParams,
    /* [size_is][in] */ double *pGuessParams,
    /* [in] */ ULONG nMaxDeviations,
    /* [size_is][out] */ double *pMaxDeviations,
    /* [size_is][out] */ double *pParams,
    /* [in] */ ULONG nFlags,
    /* [size_is][out] */ DWORD *pFlags);

/* [local] */ HRESULT STDMETHODCALLTYPE IDMCurve_GetCurvature_Proxy( 
    IDMCurve * This,
    /* [in] */ ULONG nParams,
    /* [in] */ double *pParams,
    /* [out] */ double *pDirections,
    /* [out] */ double *pCurvatures);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve_GetCurvature_Stub( 
    IDMCurve * This,
    /* [in] */ ULONG nParams,
    /* [size_is][in] */ double *pParams,
    /* [in] */ ULONG nDirections,
    /* [size_is][out] */ double *pDirections,
    /* [in] */ ULONG nCurvatures,
    /* [size_is][out] */ double *pCurvatures);

/* [local] */ HRESULT STDMETHODCALLTYPE IDMCurve_GetDerivatives_Proxy( 
    IDMCurve * This,
    /* [in] */ ULONG nParams,
    /* [in] */ double *pParams,
    /* [out] */ double *pFirstDerivs,
    /* [out] */ double *pSecondDerivs,
    /* [out] */ double *pThirdDerivs);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve_GetDerivatives_Stub( 
    IDMCurve * This,
    /* [in] */ ULONG nParams,
    /* [size_is][in] */ double *pParams,
    /* [in] */ ULONG nFirstDerivs,
    /* [size_is][out] */ double *pFirstDerivs,
    /* [in] */ ULONG nSecondDerivs,
    /* [size_is][out] */ double *pSecondDerivs,
    /* [in] */ ULONG nThirdDerivs,
    /* [size_is][out] */ double *pThirdDerivs);

/* [local] */ HRESULT STDMETHODCALLTYPE IDMCurve2D_GetParamAtPoint_Proxy( 
    IDMCurve2D * This,
    /* [in] */ ULONG nPoints,
    /* [in] */ double *pPoints,
    /* [in] */ double *pGuessParams,
    /* [out] */ double *pMaxDeviations,
    /* [out] */ double *pParams,
    /* [out] */ DWORD *pFlags);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve2D_GetParamAtPoint_Stub( 
    IDMCurve2D * This,
    /* [in] */ ULONG nPoints,
    /* [size_is][in] */ double *pPoints,
    /* [in] */ ULONG nGuessParams,
    /* [size_is][in] */ double *pGuessParams,
    /* [in] */ ULONG nMaxDeviations,
    /* [size_is][out] */ double *pMaxDeviations,
    /* [size_is][out] */ double *pParams,
    /* [in] */ ULONG nFlags,
    /* [size_is][out] */ DWORD *pFlags);

/* [local] */ HRESULT STDMETHODCALLTYPE IDMCurve2D_GetCurvature_Proxy( 
    IDMCurve2D * This,
    /* [in] */ ULONG nParams,
    /* [in] */ double *pParams,
    /* [out] */ double *pDirections,
    /* [out] */ double *pCurvatures);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve2D_GetCurvature_Stub( 
    IDMCurve2D * This,
    /* [in] */ ULONG nParams,
    /* [size_is][in] */ double *pParams,
    /* [in] */ ULONG nDirections,
    /* [size_is][out] */ double *pDirections,
    /* [in] */ ULONG nCurvatures,
    /* [size_is][out] */ double *pCurvatures);

/* [local] */ HRESULT STDMETHODCALLTYPE IDMCurve2D_GetDerivatives_Proxy( 
    IDMCurve2D * This,
    /* [in] */ ULONG nParams,
    /* [in] */ double *pParams,
    /* [out] */ double *pFirstDerivs,
    /* [out] */ double *pSecondDerivs,
    /* [out] */ double *pThirdDerivs);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IDMCurve2D_GetDerivatives_Stub( 
    IDMCurve2D * This,
    /* [in] */ ULONG nParams,
    /* [size_is][in] */ double *pParams,
    /* [in] */ ULONG nFirstDerivs,
    /* [size_is][out] */ double *pFirstDerivs,
    /* [in] */ ULONG nSecondDerivs,
    /* [size_is][out] */ double *pSecondDerivs,
    /* [in] */ ULONG nThirdDerivs,
    /* [size_is][out] */ double *pThirdDerivs);

/* [local] */ HRESULT STDMETHODCALLTYPE IEnumDMReferenceKeys_Next_Proxy( 
    IEnumDMReferenceKeys * This,
    /* [in] */ ULONG cRefKey,
    /* [out] */ LPDMREFERENCEKEY *pRefKey,
    /* [out] */ ULONG *pcFetched);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMReferenceKeys_Next_Stub( 
    IEnumDMReferenceKeys * This,
    /* [in] */ ULONG cRefKey,
    /* [length_is][size_is][out] */ LPDMREFERENCEKEY *pRefKey,
    /* [out] */ ULONG *pcFetched);



/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


