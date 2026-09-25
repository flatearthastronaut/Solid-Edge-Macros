

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0622 */
/* at Mon Jan 18 22:14:07 2038
 */
/* Compiler settings for dmcurve.idl:
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

#ifndef __dmcurve_h__
#define __dmcurve_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IDMCurveBodies_FWD_DEFINED__
#define __IDMCurveBodies_FWD_DEFINED__
typedef interface IDMCurveBodies IDMCurveBodies;

#endif 	/* __IDMCurveBodies_FWD_DEFINED__ */


#ifndef __IDMCurve2DBodies_FWD_DEFINED__
#define __IDMCurve2DBodies_FWD_DEFINED__
typedef interface IDMCurve2DBodies IDMCurve2DBodies;

#endif 	/* __IDMCurve2DBodies_FWD_DEFINED__ */


#ifndef __IDMCurveBody_FWD_DEFINED__
#define __IDMCurveBody_FWD_DEFINED__
typedef interface IDMCurveBody IDMCurveBody;

#endif 	/* __IDMCurveBody_FWD_DEFINED__ */


#ifndef __IDMCurvePath_FWD_DEFINED__
#define __IDMCurvePath_FWD_DEFINED__
typedef interface IDMCurvePath IDMCurvePath;

#endif 	/* __IDMCurvePath_FWD_DEFINED__ */


#ifndef __IDMCurveObject_FWD_DEFINED__
#define __IDMCurveObject_FWD_DEFINED__
typedef interface IDMCurveObject IDMCurveObject;

#endif 	/* __IDMCurveObject_FWD_DEFINED__ */


#ifndef __IDMCurve2DBody_FWD_DEFINED__
#define __IDMCurve2DBody_FWD_DEFINED__
typedef interface IDMCurve2DBody IDMCurve2DBody;

#endif 	/* __IDMCurve2DBody_FWD_DEFINED__ */


#ifndef __IDMCurve2DPath_FWD_DEFINED__
#define __IDMCurve2DPath_FWD_DEFINED__
typedef interface IDMCurve2DPath IDMCurve2DPath;

#endif 	/* __IDMCurve2DPath_FWD_DEFINED__ */


#ifndef __IDMCurve2DObject_FWD_DEFINED__
#define __IDMCurve2DObject_FWD_DEFINED__
typedef interface IDMCurve2DObject IDMCurve2DObject;

#endif 	/* __IDMCurve2DObject_FWD_DEFINED__ */


#ifndef __IEnumDMCurveBodies_FWD_DEFINED__
#define __IEnumDMCurveBodies_FWD_DEFINED__
typedef interface IEnumDMCurveBodies IEnumDMCurveBodies;

#endif 	/* __IEnumDMCurveBodies_FWD_DEFINED__ */


#ifndef __IEnumDMCurve2DBodies_FWD_DEFINED__
#define __IEnumDMCurve2DBodies_FWD_DEFINED__
typedef interface IEnumDMCurve2DBodies IEnumDMCurve2DBodies;

#endif 	/* __IEnumDMCurve2DBodies_FWD_DEFINED__ */


#ifndef __IEnumDMCurvePaths_FWD_DEFINED__
#define __IEnumDMCurvePaths_FWD_DEFINED__
typedef interface IEnumDMCurvePaths IEnumDMCurvePaths;

#endif 	/* __IEnumDMCurvePaths_FWD_DEFINED__ */


#ifndef __IEnumDMCurveObjects_FWD_DEFINED__
#define __IEnumDMCurveObjects_FWD_DEFINED__
typedef interface IEnumDMCurveObjects IEnumDMCurveObjects;

#endif 	/* __IEnumDMCurveObjects_FWD_DEFINED__ */


#ifndef __IEnumDMCurve2DPaths_FWD_DEFINED__
#define __IEnumDMCurve2DPaths_FWD_DEFINED__
typedef interface IEnumDMCurve2DPaths IEnumDMCurve2DPaths;

#endif 	/* __IEnumDMCurve2DPaths_FWD_DEFINED__ */


#ifndef __IEnumDMCurve2DObjects_FWD_DEFINED__
#define __IEnumDMCurve2DObjects_FWD_DEFINED__
typedef interface IEnumDMCurve2DObjects IEnumDMCurve2DObjects;

#endif 	/* __IEnumDMCurve2DObjects_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "objidl.h"
#include "dmroot.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_dmcurve_0000_0000 */
/* [local] */ 

#pragma warning(disable: 4005)
#define TARGET_IS_NT40_OR_LATER 1
#ifndef __WINDOWS_H__
#include <windows.h>
#endif
#ifndef __OBJBASE_H__
#include <objbase.h>
#endif
DEFINE_GUID(IID_IDMCurveBodies, 0x0002D230, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMCurve2DBodies, 0x0002D231, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMCurveBody, 0x0002D232, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMCurvePath, 0x0002D233, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMCurveObject, 0x0002D234, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMCurve2DBody, 0x0002D235, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMCurve2DPath, 0x0002D236, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IDMCurve2DObject, 0x0002D237, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IEnumDMCurveBodies, 0x0002D238, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IEnumDMCurve2DBodies, 0x0002D239, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IEnumDMCurvePaths, 0x0002D23A, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IEnumDMCurveObjects, 0x0002D23B, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IEnumDMCurve2DPaths, 0x0002D23C, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IEnumDMCurve2DObjects, 0x0002D23D, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
typedef /* [unique] */ IDMCurveBodies *LPDMCURVEBODIES;

typedef /* [unique] */ IDMCurve2DBodies *LPDMCURVE2DBODIES;

typedef /* [unique] */ IDMCurveBody *LPDMCURVEBODY;

typedef /* [unique] */ IDMCurve2DBody *LPDMCURVE2DBODY;

typedef /* [unique] */ IDMCurvePath *LPDMCURVEPATH;

typedef /* [unique] */ IDMCurveObject *LPDMCURVEOBJECT;

typedef /* [unique] */ IDMCurve2DPath *LPDMCURVE2DPATH;

typedef /* [unique] */ IDMCurve2DObject *LPDMCURVE2DOBJECT;

typedef /* [unique] */ IEnumDMCurveBodies *LPENUM_DMCURVEBODIES;

typedef /* [unique] */ IEnumDMCurve2DBodies *LPENUM_DMCURVE2DBODIES;

typedef /* [unique] */ IEnumDMCurvePaths *LPENUM_DMCURVEPATHS;

typedef /* [unique] */ IEnumDMCurveObjects *LPENUM_DMCURVEOBJECTS;

typedef /* [unique] */ IEnumDMCurve2DPaths *LPENUM_DMCURVE2DPATHS;

typedef /* [unique] */ IEnumDMCurve2DObjects *LPENUM_DMCURVE2DOBJECTS;



extern RPC_IF_HANDLE __MIDL_itf_dmcurve_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_dmcurve_0000_0000_v0_0_s_ifspec;

#ifndef __IDMCurveBodies_INTERFACE_DEFINED__
#define __IDMCurveBodies_INTERFACE_DEFINED__

/* interface IDMCurveBodies */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCurveBodies;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D230-0000-0000-C000-000000000046")
    IDMCurveBodies : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumCurveBodies( 
            /* [retval][out] */ LPENUM_DMCURVEBODIES *ppEnumCurveBodies) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCurveBodiesVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCurveBodies * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCurveBodies * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCurveBodies * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCurveBodies )( 
            IDMCurveBodies * This,
            /* [retval][out] */ LPENUM_DMCURVEBODIES *ppEnumCurveBodies);
        
        END_INTERFACE
    } IDMCurveBodiesVtbl;

    interface IDMCurveBodies
    {
        CONST_VTBL struct IDMCurveBodiesVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCurveBodies_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCurveBodies_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCurveBodies_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCurveBodies_EnumCurveBodies(This,ppEnumCurveBodies)	\
    ( (This)->lpVtbl -> EnumCurveBodies(This,ppEnumCurveBodies) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMCurveBodies_INTERFACE_DEFINED__ */


#ifndef __IDMCurve2DBodies_INTERFACE_DEFINED__
#define __IDMCurve2DBodies_INTERFACE_DEFINED__

/* interface IDMCurve2DBodies */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCurve2DBodies;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D231-0000-0000-C000-000000000046")
    IDMCurve2DBodies : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumCurve2DBodies( 
            /* [retval][out] */ LPENUM_DMCURVE2DBODIES *EnumCurve2DBodies) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCurve2DBodiesVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCurve2DBodies * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCurve2DBodies * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCurve2DBodies * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCurve2DBodies )( 
            IDMCurve2DBodies * This,
            /* [retval][out] */ LPENUM_DMCURVE2DBODIES *EnumCurve2DBodies);
        
        END_INTERFACE
    } IDMCurve2DBodiesVtbl;

    interface IDMCurve2DBodies
    {
        CONST_VTBL struct IDMCurve2DBodiesVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCurve2DBodies_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCurve2DBodies_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCurve2DBodies_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCurve2DBodies_EnumCurve2DBodies(This,EnumCurve2DBodies)	\
    ( (This)->lpVtbl -> EnumCurve2DBodies(This,EnumCurve2DBodies) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMCurve2DBodies_INTERFACE_DEFINED__ */


#ifndef __IDMCurveBody_INTERFACE_DEFINED__
#define __IDMCurveBody_INTERFACE_DEFINED__

/* interface IDMCurveBody */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCurveBody;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D232-0000-0000-C000-000000000046")
    IDMCurveBody : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumCurvePaths( 
            /* [retval][out] */ LPENUM_DMCURVEPATHS *ppEnumCurvePaths) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumCurveObjects( 
            /* [retval][out] */ LPENUM_DMCURVEOBJECTS *ppEnumCurveObjects) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDocument( 
            /* [retval][out] */ LPDMCURVEBODIES *pDoc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HasArea( 
            /* [retval][out] */ boolean *pHasArea) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRangeBox( 
            /* [out] */ double min_point[ 3 ],
            /* [out] */ double max_point[ 3 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetArea( 
            /* [retval][out] */ double *pArea) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGeometryForm( 
            /* [retval][out] */ DWORD *pForm) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCurveBodyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCurveBody * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCurveBody * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCurveBody * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCurvePaths )( 
            IDMCurveBody * This,
            /* [retval][out] */ LPENUM_DMCURVEPATHS *ppEnumCurvePaths);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCurveObjects )( 
            IDMCurveBody * This,
            /* [retval][out] */ LPENUM_DMCURVEOBJECTS *ppEnumCurveObjects);
        
        HRESULT ( STDMETHODCALLTYPE *GetDocument )( 
            IDMCurveBody * This,
            /* [retval][out] */ LPDMCURVEBODIES *pDoc);
        
        HRESULT ( STDMETHODCALLTYPE *HasArea )( 
            IDMCurveBody * This,
            /* [retval][out] */ boolean *pHasArea);
        
        HRESULT ( STDMETHODCALLTYPE *GetRangeBox )( 
            IDMCurveBody * This,
            /* [out] */ double min_point[ 3 ],
            /* [out] */ double max_point[ 3 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetArea )( 
            IDMCurveBody * This,
            /* [retval][out] */ double *pArea);
        
        HRESULT ( STDMETHODCALLTYPE *GetGeometryForm )( 
            IDMCurveBody * This,
            /* [retval][out] */ DWORD *pForm);
        
        END_INTERFACE
    } IDMCurveBodyVtbl;

    interface IDMCurveBody
    {
        CONST_VTBL struct IDMCurveBodyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCurveBody_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCurveBody_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCurveBody_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCurveBody_EnumCurvePaths(This,ppEnumCurvePaths)	\
    ( (This)->lpVtbl -> EnumCurvePaths(This,ppEnumCurvePaths) ) 

#define IDMCurveBody_EnumCurveObjects(This,ppEnumCurveObjects)	\
    ( (This)->lpVtbl -> EnumCurveObjects(This,ppEnumCurveObjects) ) 

#define IDMCurveBody_GetDocument(This,pDoc)	\
    ( (This)->lpVtbl -> GetDocument(This,pDoc) ) 

#define IDMCurveBody_HasArea(This,pHasArea)	\
    ( (This)->lpVtbl -> HasArea(This,pHasArea) ) 

#define IDMCurveBody_GetRangeBox(This,min_point,max_point)	\
    ( (This)->lpVtbl -> GetRangeBox(This,min_point,max_point) ) 

#define IDMCurveBody_GetArea(This,pArea)	\
    ( (This)->lpVtbl -> GetArea(This,pArea) ) 

#define IDMCurveBody_GetGeometryForm(This,pForm)	\
    ( (This)->lpVtbl -> GetGeometryForm(This,pForm) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMCurveBody_INTERFACE_DEFINED__ */


#ifndef __IDMCurvePath_INTERFACE_DEFINED__
#define __IDMCurvePath_INTERFACE_DEFINED__

/* interface IDMCurvePath */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCurvePath;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D233-0000-0000-C000-000000000046")
    IDMCurvePath : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumCurveObjects( 
            /* [retval][out] */ LPENUM_DMCURVEOBJECTS *ppEnumCurveObjects) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCurveBody( 
            /* [retval][out] */ LPDMCURVEBODY *pBody) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsClosed( 
            /* [retval][out] */ boolean *pIsClosed) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsVoid( 
            /* [retval][out] */ boolean *pIsVoid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsPlanar( 
            /* [out] */ boolean *pIsPlanar,
            /* [out] */ LPDMPLANE *pPlane) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsPointInside( 
            /* [in] */ double pPoint[ 3 ],
            /* [retval][out] */ boolean *pIsPointInside) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRangeBox( 
            /* [out] */ double min_point[ 3 ],
            /* [out] */ double max_point[ 3 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetArea( 
            /* [retval][out] */ double *pArea) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCurvePathVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCurvePath * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCurvePath * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCurvePath * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCurveObjects )( 
            IDMCurvePath * This,
            /* [retval][out] */ LPENUM_DMCURVEOBJECTS *ppEnumCurveObjects);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurveBody )( 
            IDMCurvePath * This,
            /* [retval][out] */ LPDMCURVEBODY *pBody);
        
        HRESULT ( STDMETHODCALLTYPE *IsClosed )( 
            IDMCurvePath * This,
            /* [retval][out] */ boolean *pIsClosed);
        
        HRESULT ( STDMETHODCALLTYPE *IsVoid )( 
            IDMCurvePath * This,
            /* [retval][out] */ boolean *pIsVoid);
        
        HRESULT ( STDMETHODCALLTYPE *IsPlanar )( 
            IDMCurvePath * This,
            /* [out] */ boolean *pIsPlanar,
            /* [out] */ LPDMPLANE *pPlane);
        
        HRESULT ( STDMETHODCALLTYPE *IsPointInside )( 
            IDMCurvePath * This,
            /* [in] */ double pPoint[ 3 ],
            /* [retval][out] */ boolean *pIsPointInside);
        
        HRESULT ( STDMETHODCALLTYPE *GetRangeBox )( 
            IDMCurvePath * This,
            /* [out] */ double min_point[ 3 ],
            /* [out] */ double max_point[ 3 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetArea )( 
            IDMCurvePath * This,
            /* [retval][out] */ double *pArea);
        
        END_INTERFACE
    } IDMCurvePathVtbl;

    interface IDMCurvePath
    {
        CONST_VTBL struct IDMCurvePathVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCurvePath_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCurvePath_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCurvePath_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCurvePath_EnumCurveObjects(This,ppEnumCurveObjects)	\
    ( (This)->lpVtbl -> EnumCurveObjects(This,ppEnumCurveObjects) ) 

#define IDMCurvePath_GetCurveBody(This,pBody)	\
    ( (This)->lpVtbl -> GetCurveBody(This,pBody) ) 

#define IDMCurvePath_IsClosed(This,pIsClosed)	\
    ( (This)->lpVtbl -> IsClosed(This,pIsClosed) ) 

#define IDMCurvePath_IsVoid(This,pIsVoid)	\
    ( (This)->lpVtbl -> IsVoid(This,pIsVoid) ) 

#define IDMCurvePath_IsPlanar(This,pIsPlanar,pPlane)	\
    ( (This)->lpVtbl -> IsPlanar(This,pIsPlanar,pPlane) ) 

#define IDMCurvePath_IsPointInside(This,pPoint,pIsPointInside)	\
    ( (This)->lpVtbl -> IsPointInside(This,pPoint,pIsPointInside) ) 

#define IDMCurvePath_GetRangeBox(This,min_point,max_point)	\
    ( (This)->lpVtbl -> GetRangeBox(This,min_point,max_point) ) 

#define IDMCurvePath_GetArea(This,pArea)	\
    ( (This)->lpVtbl -> GetArea(This,pArea) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMCurvePath_INTERFACE_DEFINED__ */


#ifndef __IDMCurveObject_INTERFACE_DEFINED__
#define __IDMCurveObject_INTERFACE_DEFINED__

/* interface IDMCurveObject */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCurveObject;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D234-0000-0000-C000-000000000046")
    IDMCurveObject : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCurvePath( 
            /* [retval][out] */ LPDMCURVEPATH *pCurvePath) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBody( 
            /* [retval][out] */ LPDMCURVEBODY *pBody) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsParamReversed( 
            /* [retval][out] */ boolean *pIsReversed) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCurveObjectVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCurveObject * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCurveObject * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCurveObject * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurvePath )( 
            IDMCurveObject * This,
            /* [retval][out] */ LPDMCURVEPATH *pCurvePath);
        
        HRESULT ( STDMETHODCALLTYPE *GetBody )( 
            IDMCurveObject * This,
            /* [retval][out] */ LPDMCURVEBODY *pBody);
        
        HRESULT ( STDMETHODCALLTYPE *IsParamReversed )( 
            IDMCurveObject * This,
            /* [retval][out] */ boolean *pIsReversed);
        
        END_INTERFACE
    } IDMCurveObjectVtbl;

    interface IDMCurveObject
    {
        CONST_VTBL struct IDMCurveObjectVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCurveObject_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCurveObject_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCurveObject_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCurveObject_GetCurvePath(This,pCurvePath)	\
    ( (This)->lpVtbl -> GetCurvePath(This,pCurvePath) ) 

#define IDMCurveObject_GetBody(This,pBody)	\
    ( (This)->lpVtbl -> GetBody(This,pBody) ) 

#define IDMCurveObject_IsParamReversed(This,pIsReversed)	\
    ( (This)->lpVtbl -> IsParamReversed(This,pIsReversed) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMCurveObject_INTERFACE_DEFINED__ */


#ifndef __IDMCurve2DBody_INTERFACE_DEFINED__
#define __IDMCurve2DBody_INTERFACE_DEFINED__

/* interface IDMCurve2DBody */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCurve2DBody;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D235-0000-0000-C000-000000000046")
    IDMCurve2DBody : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumCurve2DPaths( 
            /* [retval][out] */ LPENUM_DMCURVE2DPATHS *ppEnumCurve2DPaths) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumCurve2DObjects( 
            /* [retval][out] */ LPENUM_DMCURVE2DOBJECTS *ppEnumCurve2DObjects) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDocument( 
            /* [retval][out] */ LPDMCURVE2DBODIES *pDoc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HasArea( 
            /* [retval][out] */ boolean *pHasArea) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRangeBox( 
            /* [out] */ double min_point[ 2 ],
            /* [out] */ double max_point[ 2 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetArea( 
            /* [retval][out] */ double *pArea) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGeometryForm( 
            /* [retval][out] */ DWORD *pForm) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCurve2DBodyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCurve2DBody * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCurve2DBody * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCurve2DBody * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCurve2DPaths )( 
            IDMCurve2DBody * This,
            /* [retval][out] */ LPENUM_DMCURVE2DPATHS *ppEnumCurve2DPaths);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCurve2DObjects )( 
            IDMCurve2DBody * This,
            /* [retval][out] */ LPENUM_DMCURVE2DOBJECTS *ppEnumCurve2DObjects);
        
        HRESULT ( STDMETHODCALLTYPE *GetDocument )( 
            IDMCurve2DBody * This,
            /* [retval][out] */ LPDMCURVE2DBODIES *pDoc);
        
        HRESULT ( STDMETHODCALLTYPE *HasArea )( 
            IDMCurve2DBody * This,
            /* [retval][out] */ boolean *pHasArea);
        
        HRESULT ( STDMETHODCALLTYPE *GetRangeBox )( 
            IDMCurve2DBody * This,
            /* [out] */ double min_point[ 2 ],
            /* [out] */ double max_point[ 2 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetArea )( 
            IDMCurve2DBody * This,
            /* [retval][out] */ double *pArea);
        
        HRESULT ( STDMETHODCALLTYPE *GetGeometryForm )( 
            IDMCurve2DBody * This,
            /* [retval][out] */ DWORD *pForm);
        
        END_INTERFACE
    } IDMCurve2DBodyVtbl;

    interface IDMCurve2DBody
    {
        CONST_VTBL struct IDMCurve2DBodyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCurve2DBody_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCurve2DBody_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCurve2DBody_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCurve2DBody_EnumCurve2DPaths(This,ppEnumCurve2DPaths)	\
    ( (This)->lpVtbl -> EnumCurve2DPaths(This,ppEnumCurve2DPaths) ) 

#define IDMCurve2DBody_EnumCurve2DObjects(This,ppEnumCurve2DObjects)	\
    ( (This)->lpVtbl -> EnumCurve2DObjects(This,ppEnumCurve2DObjects) ) 

#define IDMCurve2DBody_GetDocument(This,pDoc)	\
    ( (This)->lpVtbl -> GetDocument(This,pDoc) ) 

#define IDMCurve2DBody_HasArea(This,pHasArea)	\
    ( (This)->lpVtbl -> HasArea(This,pHasArea) ) 

#define IDMCurve2DBody_GetRangeBox(This,min_point,max_point)	\
    ( (This)->lpVtbl -> GetRangeBox(This,min_point,max_point) ) 

#define IDMCurve2DBody_GetArea(This,pArea)	\
    ( (This)->lpVtbl -> GetArea(This,pArea) ) 

#define IDMCurve2DBody_GetGeometryForm(This,pForm)	\
    ( (This)->lpVtbl -> GetGeometryForm(This,pForm) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMCurve2DBody_INTERFACE_DEFINED__ */


#ifndef __IDMCurve2DPath_INTERFACE_DEFINED__
#define __IDMCurve2DPath_INTERFACE_DEFINED__

/* interface IDMCurve2DPath */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCurve2DPath;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D236-0000-0000-C000-000000000046")
    IDMCurve2DPath : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumCurve2DObjects( 
            /* [retval][out] */ LPENUM_DMCURVEOBJECTS *ppEnumCurve2DObjects) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCurve2DBody( 
            /* [retval][out] */ LPDMCURVE2DBODY *pBody) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsClosed( 
            /* [retval][out] */ boolean *pIsClosed) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsVoid( 
            /* [retval][out] */ boolean *pIsVoid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsPointInside( 
            /* [in] */ double pPoint[ 2 ],
            /* [retval][out] */ boolean *pIsPointInside) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRangeBox( 
            /* [out] */ double min_point[ 2 ],
            /* [out] */ double max_point[ 2 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetArea( 
            /* [retval][out] */ double *pArea) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCurve2DPathVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCurve2DPath * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCurve2DPath * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCurve2DPath * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCurve2DObjects )( 
            IDMCurve2DPath * This,
            /* [retval][out] */ LPENUM_DMCURVEOBJECTS *ppEnumCurve2DObjects);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurve2DBody )( 
            IDMCurve2DPath * This,
            /* [retval][out] */ LPDMCURVE2DBODY *pBody);
        
        HRESULT ( STDMETHODCALLTYPE *IsClosed )( 
            IDMCurve2DPath * This,
            /* [retval][out] */ boolean *pIsClosed);
        
        HRESULT ( STDMETHODCALLTYPE *IsVoid )( 
            IDMCurve2DPath * This,
            /* [retval][out] */ boolean *pIsVoid);
        
        HRESULT ( STDMETHODCALLTYPE *IsPointInside )( 
            IDMCurve2DPath * This,
            /* [in] */ double pPoint[ 2 ],
            /* [retval][out] */ boolean *pIsPointInside);
        
        HRESULT ( STDMETHODCALLTYPE *GetRangeBox )( 
            IDMCurve2DPath * This,
            /* [out] */ double min_point[ 2 ],
            /* [out] */ double max_point[ 2 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetArea )( 
            IDMCurve2DPath * This,
            /* [retval][out] */ double *pArea);
        
        END_INTERFACE
    } IDMCurve2DPathVtbl;

    interface IDMCurve2DPath
    {
        CONST_VTBL struct IDMCurve2DPathVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCurve2DPath_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCurve2DPath_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCurve2DPath_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCurve2DPath_EnumCurve2DObjects(This,ppEnumCurve2DObjects)	\
    ( (This)->lpVtbl -> EnumCurve2DObjects(This,ppEnumCurve2DObjects) ) 

#define IDMCurve2DPath_GetCurve2DBody(This,pBody)	\
    ( (This)->lpVtbl -> GetCurve2DBody(This,pBody) ) 

#define IDMCurve2DPath_IsClosed(This,pIsClosed)	\
    ( (This)->lpVtbl -> IsClosed(This,pIsClosed) ) 

#define IDMCurve2DPath_IsVoid(This,pIsVoid)	\
    ( (This)->lpVtbl -> IsVoid(This,pIsVoid) ) 

#define IDMCurve2DPath_IsPointInside(This,pPoint,pIsPointInside)	\
    ( (This)->lpVtbl -> IsPointInside(This,pPoint,pIsPointInside) ) 

#define IDMCurve2DPath_GetRangeBox(This,min_point,max_point)	\
    ( (This)->lpVtbl -> GetRangeBox(This,min_point,max_point) ) 

#define IDMCurve2DPath_GetArea(This,pArea)	\
    ( (This)->lpVtbl -> GetArea(This,pArea) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMCurve2DPath_INTERFACE_DEFINED__ */


#ifndef __IDMCurve2DObject_INTERFACE_DEFINED__
#define __IDMCurve2DObject_INTERFACE_DEFINED__

/* interface IDMCurve2DObject */
/* [object][uuid] */ 


EXTERN_C const IID IID_IDMCurve2DObject;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D237-0000-0000-C000-000000000046")
    IDMCurve2DObject : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCurve2DPath( 
            /* [retval][out] */ LPDMCURVE2DPATH *pCurve2DPath) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBody( 
            /* [retval][out] */ LPDMCURVE2DBODY *pBody) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsParamReversed( 
            /* [retval][out] */ boolean *pIsReversed) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDMCurve2DObjectVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDMCurve2DObject * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDMCurve2DObject * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDMCurve2DObject * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurve2DPath )( 
            IDMCurve2DObject * This,
            /* [retval][out] */ LPDMCURVE2DPATH *pCurve2DPath);
        
        HRESULT ( STDMETHODCALLTYPE *GetBody )( 
            IDMCurve2DObject * This,
            /* [retval][out] */ LPDMCURVE2DBODY *pBody);
        
        HRESULT ( STDMETHODCALLTYPE *IsParamReversed )( 
            IDMCurve2DObject * This,
            /* [retval][out] */ boolean *pIsReversed);
        
        END_INTERFACE
    } IDMCurve2DObjectVtbl;

    interface IDMCurve2DObject
    {
        CONST_VTBL struct IDMCurve2DObjectVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDMCurve2DObject_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDMCurve2DObject_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDMCurve2DObject_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDMCurve2DObject_GetCurve2DPath(This,pCurve2DPath)	\
    ( (This)->lpVtbl -> GetCurve2DPath(This,pCurve2DPath) ) 

#define IDMCurve2DObject_GetBody(This,pBody)	\
    ( (This)->lpVtbl -> GetBody(This,pBody) ) 

#define IDMCurve2DObject_IsParamReversed(This,pIsReversed)	\
    ( (This)->lpVtbl -> IsParamReversed(This,pIsReversed) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDMCurve2DObject_INTERFACE_DEFINED__ */


#ifndef __IEnumDMCurveBodies_INTERFACE_DEFINED__
#define __IEnumDMCurveBodies_INTERFACE_DEFINED__

/* interface IEnumDMCurveBodies */
/* [object][uuid] */ 


EXTERN_C const IID IID_IEnumDMCurveBodies;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D238-0000-0000-C000-000000000046")
    IEnumDMCurveBodies : public IUnknown
    {
    public:
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG cSurfaceBody,
            /* [out] */ LPDMCURVEBODY *pCurveBody,
            /* [out] */ ULONG *pcFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG cCurves) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ LPENUM_DMCURVEBODIES *pEnumCurveBody) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IEnumDMCurveBodiesVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumDMCurveBodies * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumDMCurveBodies * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumDMCurveBodies * This);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumDMCurveBodies * This,
            /* [in] */ ULONG cSurfaceBody,
            /* [out] */ LPDMCURVEBODY *pCurveBody,
            /* [out] */ ULONG *pcFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumDMCurveBodies * This,
            /* [in] */ ULONG cCurves);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumDMCurveBodies * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumDMCurveBodies * This,
            /* [out] */ LPENUM_DMCURVEBODIES *pEnumCurveBody);
        
        END_INTERFACE
    } IEnumDMCurveBodiesVtbl;

    interface IEnumDMCurveBodies
    {
        CONST_VTBL struct IEnumDMCurveBodiesVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumDMCurveBodies_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IEnumDMCurveBodies_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IEnumDMCurveBodies_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IEnumDMCurveBodies_Next(This,cSurfaceBody,pCurveBody,pcFetched)	\
    ( (This)->lpVtbl -> Next(This,cSurfaceBody,pCurveBody,pcFetched) ) 

#define IEnumDMCurveBodies_Skip(This,cCurves)	\
    ( (This)->lpVtbl -> Skip(This,cCurves) ) 

#define IEnumDMCurveBodies_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define IEnumDMCurveBodies_Clone(This,pEnumCurveBody)	\
    ( (This)->lpVtbl -> Clone(This,pEnumCurveBody) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurveBodies_RemoteNext_Proxy( 
    IEnumDMCurveBodies * This,
    /* [in] */ ULONG cCurveBody,
    /* [length_is][size_is][out] */ LPDMCURVEBODY *pCurveBody,
    /* [out] */ ULONG *pcFetched);


void __RPC_STUB IEnumDMCurveBodies_RemoteNext_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumDMCurveBodies_INTERFACE_DEFINED__ */


#ifndef __IEnumDMCurve2DBodies_INTERFACE_DEFINED__
#define __IEnumDMCurve2DBodies_INTERFACE_DEFINED__

/* interface IEnumDMCurve2DBodies */
/* [object][uuid] */ 


EXTERN_C const IID IID_IEnumDMCurve2DBodies;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D239-0000-0000-C000-000000000046")
    IEnumDMCurve2DBodies : public IUnknown
    {
    public:
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG cCurve2DBody,
            /* [out] */ LPDMCURVE2DBODY *pCurve2DBody,
            /* [out] */ ULONG *pcFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG cCurves) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ LPENUM_DMCURVE2DBODIES *pEnumCurve2DBody) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IEnumDMCurve2DBodiesVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumDMCurve2DBodies * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumDMCurve2DBodies * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumDMCurve2DBodies * This);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumDMCurve2DBodies * This,
            /* [in] */ ULONG cCurve2DBody,
            /* [out] */ LPDMCURVE2DBODY *pCurve2DBody,
            /* [out] */ ULONG *pcFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumDMCurve2DBodies * This,
            /* [in] */ ULONG cCurves);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumDMCurve2DBodies * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumDMCurve2DBodies * This,
            /* [out] */ LPENUM_DMCURVE2DBODIES *pEnumCurve2DBody);
        
        END_INTERFACE
    } IEnumDMCurve2DBodiesVtbl;

    interface IEnumDMCurve2DBodies
    {
        CONST_VTBL struct IEnumDMCurve2DBodiesVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumDMCurve2DBodies_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IEnumDMCurve2DBodies_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IEnumDMCurve2DBodies_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IEnumDMCurve2DBodies_Next(This,cCurve2DBody,pCurve2DBody,pcFetched)	\
    ( (This)->lpVtbl -> Next(This,cCurve2DBody,pCurve2DBody,pcFetched) ) 

#define IEnumDMCurve2DBodies_Skip(This,cCurves)	\
    ( (This)->lpVtbl -> Skip(This,cCurves) ) 

#define IEnumDMCurve2DBodies_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define IEnumDMCurve2DBodies_Clone(This,pEnumCurve2DBody)	\
    ( (This)->lpVtbl -> Clone(This,pEnumCurve2DBody) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurve2DBodies_RemoteNext_Proxy( 
    IEnumDMCurve2DBodies * This,
    /* [in] */ ULONG cCurve2DBody,
    /* [length_is][size_is][out] */ LPDMCURVE2DBODY *pCurve2DBody,
    /* [out] */ ULONG *pcFetched);


void __RPC_STUB IEnumDMCurve2DBodies_RemoteNext_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumDMCurve2DBodies_INTERFACE_DEFINED__ */


#ifndef __IEnumDMCurvePaths_INTERFACE_DEFINED__
#define __IEnumDMCurvePaths_INTERFACE_DEFINED__

/* interface IEnumDMCurvePaths */
/* [object][uuid] */ 


EXTERN_C const IID IID_IEnumDMCurvePaths;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D23A-0000-0000-C000-000000000046")
    IEnumDMCurvePaths : public IUnknown
    {
    public:
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG cCurvePath,
            /* [out] */ LPDMCURVEPATH *pCurvePath,
            /* [out] */ ULONG *pcFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG cCurves) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ LPENUM_DMCURVEPATHS *pEnumCurvePath) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IEnumDMCurvePathsVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumDMCurvePaths * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumDMCurvePaths * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumDMCurvePaths * This);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumDMCurvePaths * This,
            /* [in] */ ULONG cCurvePath,
            /* [out] */ LPDMCURVEPATH *pCurvePath,
            /* [out] */ ULONG *pcFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumDMCurvePaths * This,
            /* [in] */ ULONG cCurves);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumDMCurvePaths * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumDMCurvePaths * This,
            /* [out] */ LPENUM_DMCURVEPATHS *pEnumCurvePath);
        
        END_INTERFACE
    } IEnumDMCurvePathsVtbl;

    interface IEnumDMCurvePaths
    {
        CONST_VTBL struct IEnumDMCurvePathsVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumDMCurvePaths_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IEnumDMCurvePaths_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IEnumDMCurvePaths_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IEnumDMCurvePaths_Next(This,cCurvePath,pCurvePath,pcFetched)	\
    ( (This)->lpVtbl -> Next(This,cCurvePath,pCurvePath,pcFetched) ) 

#define IEnumDMCurvePaths_Skip(This,cCurves)	\
    ( (This)->lpVtbl -> Skip(This,cCurves) ) 

#define IEnumDMCurvePaths_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define IEnumDMCurvePaths_Clone(This,pEnumCurvePath)	\
    ( (This)->lpVtbl -> Clone(This,pEnumCurvePath) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurvePaths_RemoteNext_Proxy( 
    IEnumDMCurvePaths * This,
    /* [in] */ ULONG cCurvePath,
    /* [length_is][size_is][out] */ LPDMCURVEPATH *pCurvePath,
    /* [out] */ ULONG *pcFetched);


void __RPC_STUB IEnumDMCurvePaths_RemoteNext_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumDMCurvePaths_INTERFACE_DEFINED__ */


#ifndef __IEnumDMCurveObjects_INTERFACE_DEFINED__
#define __IEnumDMCurveObjects_INTERFACE_DEFINED__

/* interface IEnumDMCurveObjects */
/* [object][uuid] */ 


EXTERN_C const IID IID_IEnumDMCurveObjects;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D23B-0000-0000-C000-000000000046")
    IEnumDMCurveObjects : public IUnknown
    {
    public:
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG cCurveObject,
            /* [out] */ LPDMCURVEOBJECT *pCurveObject,
            /* [out] */ ULONG *pcFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG cCurves) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ LPENUM_DMCURVEOBJECTS *pEnumCurveObject) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IEnumDMCurveObjectsVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumDMCurveObjects * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumDMCurveObjects * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumDMCurveObjects * This);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumDMCurveObjects * This,
            /* [in] */ ULONG cCurveObject,
            /* [out] */ LPDMCURVEOBJECT *pCurveObject,
            /* [out] */ ULONG *pcFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumDMCurveObjects * This,
            /* [in] */ ULONG cCurves);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumDMCurveObjects * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumDMCurveObjects * This,
            /* [out] */ LPENUM_DMCURVEOBJECTS *pEnumCurveObject);
        
        END_INTERFACE
    } IEnumDMCurveObjectsVtbl;

    interface IEnumDMCurveObjects
    {
        CONST_VTBL struct IEnumDMCurveObjectsVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumDMCurveObjects_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IEnumDMCurveObjects_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IEnumDMCurveObjects_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IEnumDMCurveObjects_Next(This,cCurveObject,pCurveObject,pcFetched)	\
    ( (This)->lpVtbl -> Next(This,cCurveObject,pCurveObject,pcFetched) ) 

#define IEnumDMCurveObjects_Skip(This,cCurves)	\
    ( (This)->lpVtbl -> Skip(This,cCurves) ) 

#define IEnumDMCurveObjects_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define IEnumDMCurveObjects_Clone(This,pEnumCurveObject)	\
    ( (This)->lpVtbl -> Clone(This,pEnumCurveObject) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurveObjects_RemoteNext_Proxy( 
    IEnumDMCurveObjects * This,
    /* [in] */ ULONG cCurveObject,
    /* [length_is][size_is][out] */ LPDMCURVEOBJECT *pCurveObject,
    /* [out] */ ULONG *pcFetched);


void __RPC_STUB IEnumDMCurveObjects_RemoteNext_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumDMCurveObjects_INTERFACE_DEFINED__ */


#ifndef __IEnumDMCurve2DPaths_INTERFACE_DEFINED__
#define __IEnumDMCurve2DPaths_INTERFACE_DEFINED__

/* interface IEnumDMCurve2DPaths */
/* [object][uuid] */ 


EXTERN_C const IID IID_IEnumDMCurve2DPaths;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D23C-0000-0000-C000-000000000046")
    IEnumDMCurve2DPaths : public IUnknown
    {
    public:
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG cCurve2DPath,
            /* [out] */ LPDMCURVE2DPATH *pCurve2DPath,
            /* [out] */ ULONG *pcFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG cCurves) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ LPENUM_DMCURVE2DPATHS *pEnumCurve2DPath) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IEnumDMCurve2DPathsVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumDMCurve2DPaths * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumDMCurve2DPaths * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumDMCurve2DPaths * This);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumDMCurve2DPaths * This,
            /* [in] */ ULONG cCurve2DPath,
            /* [out] */ LPDMCURVE2DPATH *pCurve2DPath,
            /* [out] */ ULONG *pcFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumDMCurve2DPaths * This,
            /* [in] */ ULONG cCurves);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumDMCurve2DPaths * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumDMCurve2DPaths * This,
            /* [out] */ LPENUM_DMCURVE2DPATHS *pEnumCurve2DPath);
        
        END_INTERFACE
    } IEnumDMCurve2DPathsVtbl;

    interface IEnumDMCurve2DPaths
    {
        CONST_VTBL struct IEnumDMCurve2DPathsVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumDMCurve2DPaths_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IEnumDMCurve2DPaths_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IEnumDMCurve2DPaths_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IEnumDMCurve2DPaths_Next(This,cCurve2DPath,pCurve2DPath,pcFetched)	\
    ( (This)->lpVtbl -> Next(This,cCurve2DPath,pCurve2DPath,pcFetched) ) 

#define IEnumDMCurve2DPaths_Skip(This,cCurves)	\
    ( (This)->lpVtbl -> Skip(This,cCurves) ) 

#define IEnumDMCurve2DPaths_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define IEnumDMCurve2DPaths_Clone(This,pEnumCurve2DPath)	\
    ( (This)->lpVtbl -> Clone(This,pEnumCurve2DPath) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurve2DPaths_RemoteNext_Proxy( 
    IEnumDMCurve2DPaths * This,
    /* [in] */ ULONG cCurve2DPath,
    /* [length_is][size_is][out] */ LPDMCURVE2DPATH *pCurve2DPath,
    /* [out] */ ULONG *pcFetched);


void __RPC_STUB IEnumDMCurve2DPaths_RemoteNext_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumDMCurve2DPaths_INTERFACE_DEFINED__ */


#ifndef __IEnumDMCurve2DObjects_INTERFACE_DEFINED__
#define __IEnumDMCurve2DObjects_INTERFACE_DEFINED__

/* interface IEnumDMCurve2DObjects */
/* [object][uuid] */ 


EXTERN_C const IID IID_IEnumDMCurve2DObjects;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0002D23D-0000-0000-C000-000000000046")
    IEnumDMCurve2DObjects : public IUnknown
    {
    public:
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG cCurve2DObject,
            /* [out] */ LPDMCURVE2DOBJECT *pCurve2DObject,
            /* [out] */ ULONG *pcFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG cCurves) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ LPENUM_DMCURVE2DOBJECTS *pEnumCurve2DObject) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IEnumDMCurve2DObjectsVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumDMCurve2DObjects * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumDMCurve2DObjects * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumDMCurve2DObjects * This);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumDMCurve2DObjects * This,
            /* [in] */ ULONG cCurve2DObject,
            /* [out] */ LPDMCURVE2DOBJECT *pCurve2DObject,
            /* [out] */ ULONG *pcFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumDMCurve2DObjects * This,
            /* [in] */ ULONG cCurves);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumDMCurve2DObjects * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumDMCurve2DObjects * This,
            /* [out] */ LPENUM_DMCURVE2DOBJECTS *pEnumCurve2DObject);
        
        END_INTERFACE
    } IEnumDMCurve2DObjectsVtbl;

    interface IEnumDMCurve2DObjects
    {
        CONST_VTBL struct IEnumDMCurve2DObjectsVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumDMCurve2DObjects_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IEnumDMCurve2DObjects_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IEnumDMCurve2DObjects_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IEnumDMCurve2DObjects_Next(This,cCurve2DObject,pCurve2DObject,pcFetched)	\
    ( (This)->lpVtbl -> Next(This,cCurve2DObject,pCurve2DObject,pcFetched) ) 

#define IEnumDMCurve2DObjects_Skip(This,cCurves)	\
    ( (This)->lpVtbl -> Skip(This,cCurves) ) 

#define IEnumDMCurve2DObjects_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define IEnumDMCurve2DObjects_Clone(This,pEnumCurve2DObject)	\
    ( (This)->lpVtbl -> Clone(This,pEnumCurve2DObject) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurve2DObjects_RemoteNext_Proxy( 
    IEnumDMCurve2DObjects * This,
    /* [in] */ ULONG cCurve2DObject,
    /* [length_is][size_is][out] */ LPDMCURVE2DOBJECT *pCurve2DObject,
    /* [out] */ ULONG *pcFetched);


void __RPC_STUB IEnumDMCurve2DObjects_RemoteNext_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumDMCurve2DObjects_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* [local] */ HRESULT STDMETHODCALLTYPE IEnumDMCurveBodies_Next_Proxy( 
    IEnumDMCurveBodies * This,
    /* [in] */ ULONG cSurfaceBody,
    /* [out] */ LPDMCURVEBODY *pCurveBody,
    /* [out] */ ULONG *pcFetched);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurveBodies_Next_Stub( 
    IEnumDMCurveBodies * This,
    /* [in] */ ULONG cCurveBody,
    /* [length_is][size_is][out] */ LPDMCURVEBODY *pCurveBody,
    /* [out] */ ULONG *pcFetched);

/* [local] */ HRESULT STDMETHODCALLTYPE IEnumDMCurve2DBodies_Next_Proxy( 
    IEnumDMCurve2DBodies * This,
    /* [in] */ ULONG cCurve2DBody,
    /* [out] */ LPDMCURVE2DBODY *pCurve2DBody,
    /* [out] */ ULONG *pcFetched);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurve2DBodies_Next_Stub( 
    IEnumDMCurve2DBodies * This,
    /* [in] */ ULONG cCurve2DBody,
    /* [length_is][size_is][out] */ LPDMCURVE2DBODY *pCurve2DBody,
    /* [out] */ ULONG *pcFetched);

/* [local] */ HRESULT STDMETHODCALLTYPE IEnumDMCurvePaths_Next_Proxy( 
    IEnumDMCurvePaths * This,
    /* [in] */ ULONG cCurvePath,
    /* [out] */ LPDMCURVEPATH *pCurvePath,
    /* [out] */ ULONG *pcFetched);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurvePaths_Next_Stub( 
    IEnumDMCurvePaths * This,
    /* [in] */ ULONG cCurvePath,
    /* [length_is][size_is][out] */ LPDMCURVEPATH *pCurvePath,
    /* [out] */ ULONG *pcFetched);

/* [local] */ HRESULT STDMETHODCALLTYPE IEnumDMCurveObjects_Next_Proxy( 
    IEnumDMCurveObjects * This,
    /* [in] */ ULONG cCurveObject,
    /* [out] */ LPDMCURVEOBJECT *pCurveObject,
    /* [out] */ ULONG *pcFetched);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurveObjects_Next_Stub( 
    IEnumDMCurveObjects * This,
    /* [in] */ ULONG cCurveObject,
    /* [length_is][size_is][out] */ LPDMCURVEOBJECT *pCurveObject,
    /* [out] */ ULONG *pcFetched);

/* [local] */ HRESULT STDMETHODCALLTYPE IEnumDMCurve2DPaths_Next_Proxy( 
    IEnumDMCurve2DPaths * This,
    /* [in] */ ULONG cCurve2DPath,
    /* [out] */ LPDMCURVE2DPATH *pCurve2DPath,
    /* [out] */ ULONG *pcFetched);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurve2DPaths_Next_Stub( 
    IEnumDMCurve2DPaths * This,
    /* [in] */ ULONG cCurve2DPath,
    /* [length_is][size_is][out] */ LPDMCURVE2DPATH *pCurve2DPath,
    /* [out] */ ULONG *pcFetched);

/* [local] */ HRESULT STDMETHODCALLTYPE IEnumDMCurve2DObjects_Next_Proxy( 
    IEnumDMCurve2DObjects * This,
    /* [in] */ ULONG cCurve2DObject,
    /* [out] */ LPDMCURVE2DOBJECT *pCurve2DObject,
    /* [out] */ ULONG *pcFetched);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumDMCurve2DObjects_Next_Stub( 
    IEnumDMCurve2DObjects * This,
    /* [in] */ ULONG cCurve2DObject,
    /* [length_is][size_is][out] */ LPDMCURVE2DOBJECT *pCurve2DObject,
    /* [out] */ ULONG *pcFetched);



/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


