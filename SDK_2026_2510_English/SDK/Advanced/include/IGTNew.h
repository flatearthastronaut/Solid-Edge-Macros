

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0622 */
/* at Mon Jan 18 22:14:07 2038
 */
/* Compiler settings for SEMarshal.idl:
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

#ifndef __IGTNew_h__
#define __IGTNew_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __ISECurveBody_FWD_DEFINED__
#define __ISECurveBody_FWD_DEFINED__
typedef interface ISECurveBody ISECurveBody;

#endif 	/* __ISECurveBody_FWD_DEFINED__ */


#ifndef __ISECurvePath_FWD_DEFINED__
#define __ISECurvePath_FWD_DEFINED__
typedef interface ISECurvePath ISECurvePath;

#endif 	/* __ISECurvePath_FWD_DEFINED__ */


#ifndef __ISEShell_FWD_DEFINED__
#define __ISEShell_FWD_DEFINED__
typedef interface ISEShell ISEShell;

#endif 	/* __ISEShell_FWD_DEFINED__ */


#ifndef __ISEShell2_FWD_DEFINED__
#define __ISEShell2_FWD_DEFINED__
typedef interface ISEShell2 ISEShell2;

#endif 	/* __ISEShell2_FWD_DEFINED__ */


#ifndef __ISECurve_FWD_DEFINED__
#define __ISECurve_FWD_DEFINED__
typedef interface ISECurve ISECurve;

#endif 	/* __ISECurve_FWD_DEFINED__ */


#ifndef __ISESurfaceBody_FWD_DEFINED__
#define __ISESurfaceBody_FWD_DEFINED__
typedef interface ISESurfaceBody ISESurfaceBody;

#endif 	/* __ISESurfaceBody_FWD_DEFINED__ */


#ifndef __ISESurfaceBody2_FWD_DEFINED__
#define __ISESurfaceBody2_FWD_DEFINED__
typedef interface ISESurfaceBody2 ISESurfaceBody2;

#endif 	/* __ISESurfaceBody2_FWD_DEFINED__ */


#ifndef __ISESurfaceBody3_FWD_DEFINED__
#define __ISESurfaceBody3_FWD_DEFINED__
typedef interface ISESurfaceBody3 ISESurfaceBody3;

#endif 	/* __ISESurfaceBody3_FWD_DEFINED__ */


#ifndef __ISESurfaceBody4_FWD_DEFINED__
#define __ISESurfaceBody4_FWD_DEFINED__
typedef interface ISESurfaceBody4 ISESurfaceBody4;

#endif 	/* __ISESurfaceBody4_FWD_DEFINED__ */


#ifndef __ISESurfaceBody5_FWD_DEFINED__
#define __ISESurfaceBody5_FWD_DEFINED__
typedef interface ISESurfaceBody5 ISESurfaceBody5;

#endif 	/* __ISESurfaceBody5_FWD_DEFINED__ */


#ifndef __ISESurfaceBody6_FWD_DEFINED__
#define __ISESurfaceBody6_FWD_DEFINED__
typedef interface ISESurfaceBody6 ISESurfaceBody6;

#endif 	/* __ISESurfaceBody6_FWD_DEFINED__ */


#ifndef __ISEFace_FWD_DEFINED__
#define __ISEFace_FWD_DEFINED__
typedef interface ISEFace ISEFace;

#endif 	/* __ISEFace_FWD_DEFINED__ */


#ifndef __ISEFace2_FWD_DEFINED__
#define __ISEFace2_FWD_DEFINED__
typedef interface ISEFace2 ISEFace2;

#endif 	/* __ISEFace2_FWD_DEFINED__ */


#ifndef __ISEFace3_FWD_DEFINED__
#define __ISEFace3_FWD_DEFINED__
typedef interface ISEFace3 ISEFace3;

#endif 	/* __ISEFace3_FWD_DEFINED__ */


#ifndef __ISEFace4_FWD_DEFINED__
#define __ISEFace4_FWD_DEFINED__
typedef interface ISEFace4 ISEFace4;

#endif 	/* __ISEFace4_FWD_DEFINED__ */


#ifndef __ISEFace5_FWD_DEFINED__
#define __ISEFace5_FWD_DEFINED__
typedef interface ISEFace5 ISEFace5;

#endif 	/* __ISEFace5_FWD_DEFINED__ */


#ifndef __ISEEdge_FWD_DEFINED__
#define __ISEEdge_FWD_DEFINED__
typedef interface ISEEdge ISEEdge;

#endif 	/* __ISEEdge_FWD_DEFINED__ */


#ifndef __ISEEdge2_FWD_DEFINED__
#define __ISEEdge2_FWD_DEFINED__
typedef interface ISEEdge2 ISEEdge2;

#endif 	/* __ISEEdge2_FWD_DEFINED__ */


#ifndef __ISEEdge3_FWD_DEFINED__
#define __ISEEdge3_FWD_DEFINED__
typedef interface ISEEdge3 ISEEdge3;

#endif 	/* __ISEEdge3_FWD_DEFINED__ */


#ifndef __ISEVertex_FWD_DEFINED__
#define __ISEVertex_FWD_DEFINED__
typedef interface ISEVertex ISEVertex;

#endif 	/* __ISEVertex_FWD_DEFINED__ */


#ifndef __ISELoop_FWD_DEFINED__
#define __ISELoop_FWD_DEFINED__
typedef interface ISELoop ISELoop;

#endif 	/* __ISELoop_FWD_DEFINED__ */


#ifndef __ISELoop2_FWD_DEFINED__
#define __ISELoop2_FWD_DEFINED__
typedef interface ISELoop2 ISELoop2;

#endif 	/* __ISELoop2_FWD_DEFINED__ */


#ifndef __ISELoop3_FWD_DEFINED__
#define __ISELoop3_FWD_DEFINED__
typedef interface ISELoop3 ISELoop3;

#endif 	/* __ISELoop3_FWD_DEFINED__ */


#ifndef __ISEEdgeUse_FWD_DEFINED__
#define __ISEEdgeUse_FWD_DEFINED__
typedef interface ISEEdgeUse ISEEdgeUse;

#endif 	/* __ISEEdgeUse_FWD_DEFINED__ */


#ifndef __ISEEdgeUse2_FWD_DEFINED__
#define __ISEEdgeUse2_FWD_DEFINED__
typedef interface ISEEdgeUse2 ISEEdgeUse2;

#endif 	/* __ISEEdgeUse2_FWD_DEFINED__ */


#ifndef __ISEComponentDefinition_FWD_DEFINED__
#define __ISEComponentDefinition_FWD_DEFINED__
typedef interface ISEComponentDefinition ISEComponentDefinition;

#endif 	/* __ISEComponentDefinition_FWD_DEFINED__ */


#ifndef __ISEComponentOccurrence_FWD_DEFINED__
#define __ISEComponentOccurrence_FWD_DEFINED__
typedef interface ISEComponentOccurrence ISEComponentOccurrence;

#endif 	/* __ISEComponentOccurrence_FWD_DEFINED__ */


#ifndef __ISEComponentDefinition2_FWD_DEFINED__
#define __ISEComponentDefinition2_FWD_DEFINED__
typedef interface ISEComponentDefinition2 ISEComponentDefinition2;

#endif 	/* __ISEComponentDefinition2_FWD_DEFINED__ */


#ifndef __ISEComponentOccurrence2_FWD_DEFINED__
#define __ISEComponentOccurrence2_FWD_DEFINED__
typedef interface ISEComponentOccurrence2 ISEComponentOccurrence2;

#endif 	/* __ISEComponentOccurrence2_FWD_DEFINED__ */


#ifndef __ISEAssemblyBody_FWD_DEFINED__
#define __ISEAssemblyBody_FWD_DEFINED__
typedef interface ISEAssemblyBody ISEAssemblyBody;

#endif 	/* __ISEAssemblyBody_FWD_DEFINED__ */


#ifndef __IEnumSEAssemblyBodies_FWD_DEFINED__
#define __IEnumSEAssemblyBodies_FWD_DEFINED__
typedef interface IEnumSEAssemblyBodies IEnumSEAssemblyBodies;

#endif 	/* __IEnumSEAssemblyBodies_FWD_DEFINED__ */


#ifndef __ISECurveVertex_FWD_DEFINED__
#define __ISECurveVertex_FWD_DEFINED__
typedef interface ISECurveVertex ISECurveVertex;

#endif 	/* __ISECurveVertex_FWD_DEFINED__ */


#ifndef __IEnumSECurveVertices_FWD_DEFINED__
#define __IEnumSECurveVertices_FWD_DEFINED__
typedef interface IEnumSECurveVertices IEnumSECurveVertices;

#endif 	/* __IEnumSECurveVertices_FWD_DEFINED__ */


#ifndef __ISECurveBody2_FWD_DEFINED__
#define __ISECurveBody2_FWD_DEFINED__
typedef interface ISECurveBody2 ISECurveBody2;

#endif 	/* __ISECurveBody2_FWD_DEFINED__ */


#ifndef __ISECurve2_FWD_DEFINED__
#define __ISECurve2_FWD_DEFINED__
typedef interface ISECurve2 ISECurve2;

#endif 	/* __ISECurve2_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "objidl.h"
#include "gtfordm.h"
#include "gsfordm.h"
#include "dmcurve.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_SEMarshal_0000_0000 */
/* [local] */ 

#pragma warning(disable: 4005)
#define TARGET_IS_NT40_OR_LATER 1
#ifndef __WINDOWS_H__
#include <windows.h>
#endif
#ifndef __OBJBASE_H__
#include <objbase.h>
#endif
DEFINE_GUID(IID_ISECurveBody, 0x3233f13, 0x64eb, 0x4c99, 0x85, 0xe6, 0x9e, 0xe1, 0x95, 0x9a, 0x63, 0x15);
DEFINE_GUID(IID_ISECurvePath, 0x499f004a, 0xc995, 0x45b9, 0x8c, 0xab, 0x2f, 0xcc, 0xbc, 0x49, 0xa0, 0xfd);
DEFINE_GUID(IID_ISEShell, 0x6899f8f2, 0x90eb, 0x4e28, 0xb6, 0xa4, 0x3e, 0xa7, 0x7f, 0x6f, 0x1a, 0x93);
DEFINE_GUID(IID_ISEShell2, 0xc087d81, 0x761c, 0x465a, 0xaa, 0xa1, 0x80, 0x1b, 0xf8, 0xfa, 0x93, 0xcc);
DEFINE_GUID(IID_ISECurve, 0xaada9398, 0x2ee2, 0x4041, 0x8f, 0x87, 0xdd, 0x6f, 0x38, 0x23, 0x50, 0x3b);
DEFINE_GUID(IID_ISESurfaceBody, 0x712815e, 0xd31a, 0x11d1, 0xbe, 0xc8, 0x8, 0x0, 0x36, 0xd7, 0xb3, 0x2);
DEFINE_GUID(IID_ISESurfaceBody2, 0x9de673c1, 0x7b54, 0x11d3, 0x97, 0x34, 0x0, 0x60, 0x97, 0x38, 0x28, 0xb5);
DEFINE_GUID(IID_ISESurfaceBody3, 0x687b9bee, 0xd1e1, 0x4d77, 0x90, 0x72, 0xd8, 0xd0, 0x10, 0xb7, 0x7d, 0x8e);
DEFINE_GUID(IID_ISESurfaceBody4, 0x5bda9a1e, 0x6418, 0x4880, 0x8a, 0x2b, 0xc3, 0xff, 0x79, 0x41, 0x11, 0xaf);
DEFINE_GUID(IID_ISESurfaceBody5, 0x36f47b59, 0x1853, 0x4170, 0xb8, 0xa, 0xac, 0xfa, 0xb4, 0x1, 0x38, 0xde);
DEFINE_GUID(IID_ISESurfaceBody6, 0xaa0fcd5d, 0xecf1, 0x4e56, 0xbe, 0x26, 0x1d, 0x38, 0x28, 0x2b, 0x93, 0x79);
DEFINE_GUID(IID_ISEFace, 0xb03b890e, 0xf3dd, 0x11d1, 0xbe, 0xc9, 0x8, 0x0, 0x36, 0xd7, 0xb3, 0x2);
DEFINE_GUID(IID_ISEFace2, 0x9de673c0, 0x7b54, 0x11d3, 0x97, 0x34, 0x0, 0x60, 0x97, 0x38, 0x28, 0xb5);
DEFINE_GUID(IID_ISEFace3, 0x5270701e, 0x1067, 0x4bb2, 0x97, 0x4, 0xa4, 0xa0, 0xed, 0xfb, 0xc5, 0x59);
DEFINE_GUID(IID_ISEFace4, 0xf746707b, 0x7c6e, 0x476f, 0xbb, 0x21, 0xab, 0x2, 0xde, 0x24, 0x5e, 0x2b);
DEFINE_GUID(IID_ISEFace5, 0x917e478d, 0x85d2, 0x4f72, 0xb4, 0x8f, 0x3, 0x12, 0x3f, 0xe5, 0x44, 0x10);
DEFINE_GUID(IID_ISEEdge,0xcdfd024c, 0xf3dd, 0x11d1, 0xbe, 0xc9, 0x8, 0x0, 0x36, 0xd7, 0xb3, 0x2);
DEFINE_GUID(IID_ISEEdge2, 0x8bdddca9, 0x140, 0x47c7, 0xbc, 0x25, 0x2a, 0x5d, 0x68, 0x7d, 0xac, 0x9f);
DEFINE_GUID(IID_ISEEdge3, 0xe2009aaa, 0x6b4f, 0x4338, 0x9b, 0xec, 0x1b, 0xb8, 0x50, 0xb0, 0xc4, 0xbf);
DEFINE_GUID(IID_ISEVertex , 0xa309ac6f, 0xe978, 0x4c29, 0xa4, 0x1e, 0xcc, 0x3b, 0x34, 0x93, 0x5f, 0xd5);
DEFINE_GUID(IID_ISELoop, 0x80a06bec, 0x39d2, 0x40a2, 0x87, 0x60, 0x33, 0xf7, 0x51, 0x29, 0x4a, 0x8);
DEFINE_GUID(IID_ISELoop2, 0xcd10403a, 0x85dc, 0x46d8, 0xb0, 0xd5, 0xdb, 0xee, 0x39, 0x2, 0x53, 0x10);
DEFINE_GUID(IID_ISELoop3, 0x41e46527, 0x82f8, 0x445c, 0xba, 0xc, 0x80, 0x9d, 0x73, 0xc6, 0x9e, 0x24);
DEFINE_GUID(IID_ISEEdgeUse, 0x49c40e77, 0x3f01, 0x4a24, 0x97, 0x47, 0xfe, 0xbd, 0x69, 0xde, 0xc9, 0x95);
DEFINE_GUID(IID_ISEEdgeUse2, 0x8f466d92, 0xf1b9, 0x4929, 0x86, 0xa0, 0xb4, 0xf8, 0xf5, 0x6c, 0x85, 0xc8);
DEFINE_GUID(IID_ISEComponentDefinition,0xaf17f175, 0x93a6, 0x11d3, 0xa3, 0xe2, 0x0, 0x4, 0xac, 0x96, 0x95, 0xbb);
DEFINE_GUID(IID_ISEComponentOccurrence,0xaf17f176, 0x93a6, 0x11d3, 0xa3, 0xe2, 0x0, 0x4, 0xac, 0x96, 0x95, 0xbb);
DEFINE_GUID(IID_ISEComponentDefinition2,0x2b4dfa3a, 0x3f2a, 0x45f6, 0xb5, 0x40, 0xa7, 0x78, 0xd9, 0x1d, 0xd8, 0x2a);
DEFINE_GUID(IID_ISEComponentOccurrence2,0x35d71da6, 0x9ae5, 0x461a, 0x89, 0x5b, 0x73, 0xb5, 0xed, 0x57, 0x99, 0x5e);
DEFINE_GUID(IID_IEnumSEAssemblyBodies, 0x71624f92, 0xa9a7, 0x497b, 0x9b, 0x6f, 0x8, 0xac, 0x68, 0x94, 0xc4, 0xe2);
DEFINE_GUID(IID_ISEAssemblyBody, 0x50d9aadf, 0x7ff8, 0x4f6e, 0x97, 0x5, 0x37, 0xd2, 0xdf, 0x16, 0x44, 0x3f);
DEFINE_GUID(IID_ISECurveVertex, 0x235a8921, 0xe798, 0x4638, 0x88, 0xdb, 0x4b, 0xfc, 0x5b, 0xbb, 0xf, 0xe8);
DEFINE_GUID(IID_IEnumSECurveVertices, 0xf52f2b56, 0x26de, 0x49c3, 0xb4, 0x4e, 0x9b, 0x15, 0xf4, 0x1d, 0x5f, 0x89);
DEFINE_GUID(IID_ISECurveBody2, 0x591a075d, 0x38be, 0x4e18, 0x86, 0xf6, 0x55, 0x4, 0x7a, 0x62, 0x0, 0x66);
DEFINE_GUID(IID_ISECurve2, 0x88f45660, 0x836f, 0x47a0, 0x8e, 0x2d, 0x75, 0x3e, 0x97, 0x9, 0xc6, 0x71);
typedef /* [unique] */ ISECurveBody *LPSECURVEBODY;

typedef /* [unique] */ ISECurvePath *LPSECURVEPATH;

typedef /* [unique] */ ISEShell *LPSESHELL;

typedef /* [unique] */ ISEShell2 *LPSESHELL2;

typedef /* [unique] */ ISECurve *LPSECURVE;

typedef /* [unique] */ ISESurfaceBody *LPSESURFACEBODY;

typedef /* [unique] */ ISESurfaceBody2 *LPSESURFACEBODY2;

typedef /* [unique] */ ISESurfaceBody3 *LPSESURFACEBODY3;

typedef /* [unique] */ ISESurfaceBody4 *LPSESURFACEBODY4;

typedef /* [unique] */ ISESurfaceBody5 *LPSESURFACEBODY5;

typedef /* [unique] */ ISESurfaceBody6 *LPSESURFACEBODY6;

typedef /* [unique] */ ISEFace *LPSEFACE;

typedef /* [unique] */ ISEFace2 *LPSEFACE2;

typedef /* [unique] */ ISEFace3 *LPSEFACE3;

typedef /* [unique] */ ISEFace4 *LPSEFACE4;

typedef /* [unique] */ ISEFace5 *LPSEFACE5;

typedef /* [unique] */ ISEEdge *LPSEEDGE;

typedef /* [unique] */ ISEEdge2 *LPSEEDGE2;

typedef /* [unique] */ ISEEdge3 *LPSEEDGE3;

typedef /* [unique] */ ISEVertex *LPSEVERTEX;

typedef /* [unique] */ ISELoop *LPSELOOP;

typedef /* [unique] */ ISELoop2 *LPSELOOP2;

typedef /* [unique] */ ISELoop3 *LPSELOOP3;

typedef /* [unique] */ ISEEdgeUse *LPSEEDGEUSE;

typedef /* [unique] */ ISEEdgeUse2 *LPSEEDGEUSE2;

typedef /* [unique] */ ISEComponentDefinition *LPSECOMPONENTDEFINITION;

typedef /* [unique] */ ISEComponentOccurrence *LPSECOMPONENTOCCURRENCE;

typedef /* [unique] */ ISEComponentDefinition2 *LPSECOMPONENTDEFINITION2;

typedef /* [unique] */ ISEComponentOccurrence2 *LPSECOMPONENTOCCURRENCE2;

typedef /* [unique] */ IEnumSEAssemblyBodies *LPENUM_SEASSEMBLYBODIES;

typedef /* [unique] */ ISEAssemblyBody *LPSEASSEMBLYBODY;

typedef /* [unique] */ ISECurveVertex *LPSECURVEVERTEX;

typedef /* [unique] */ IEnumSECurveVertices *LPENUM_SECURVEVERTICES;

typedef /* [unique] */ ISECurveBody2 *LPSECURVEBODY2;

typedef /* [unique] */ ISECurve2 *LPSECURVE2;

typedef 
enum tagSEComponentCountOption
    {
        IMMEDIATE_OCCURRENCES	= 0,
        IMMEDIATE_PART_OCCURRENCES	= ( IMMEDIATE_OCCURRENCES + 1 ) ,
        IMMEDIATE_UNIQUE_OCCURRENCES	= ( IMMEDIATE_PART_OCCURRENCES + 1 ) ,
        IMMEDIATE_UNIQUE_PART_OCCURRENCES	= ( IMMEDIATE_UNIQUE_OCCURRENCES + 1 ) ,
        ALL_OCCURRENCES_ESTIMATE	= ( IMMEDIATE_UNIQUE_PART_OCCURRENCES + 1 ) ,
        ALL_UNIQUE_OCCURRENCES_ESTIMATE	= ( ALL_OCCURRENCES_ESTIMATE + 1 ) ,
        ALL_OCCURRENCES	= ( ALL_UNIQUE_OCCURRENCES_ESTIMATE + 1 ) ,
        ALL_UNIQUE_OCCURRENCES	= ( ALL_OCCURRENCES + 1 ) ,
        ALL_PART_OCCURRENCES	= ( ALL_UNIQUE_OCCURRENCES + 1 ) ,
        ALL_UNIQUE_PART_OCCURRENCES	= ( ALL_PART_OCCURRENCES + 1 ) 
    } 	SECOMPONENTCOUNTOPTION;

#ifndef __UVISUAL_H__
typedef struct tagSEMaterial
    {
    float m_DiffuseColor[ 3 ];
    float m_AmbientColor[ 3 ];
    float m_SpecularColor[ 3 ];
    float m_EmissionColor[ 3 ];
    float m_Shininess;
    float m_Opacity;
    } 	SEMATERIAL;

#endif

//////////////////SE-Custom interface on Curve Body: (ISECurveBody)////////////////////////////////////////
// 1. GetExactRangeBox: Returns the exact minimum and maximum points of this Curve Body
//    'pMinPoint' is an array of three doubles containing the minimum point.
//    'pMaxPoint' is an array of three doubles containing the maximum point.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0000_v0_0_s_ifspec;

#ifndef __ISECurveBody_INTERFACE_DEFINED__
#define __ISECurveBody_INTERFACE_DEFINED__

/* interface ISECurveBody */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISECurveBody;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("95CCEE8C-B034-4f7a-8FB6-394F5007BC76")
    ISECurveBody : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetExactRangeBox( 
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISECurveBodyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISECurveBody * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISECurveBody * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISECurveBody * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetExactRangeBox )( 
            ISECurveBody * This,
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]);
        
        END_INTERFACE
    } ISECurveBodyVtbl;

    interface ISECurveBody
    {
        CONST_VTBL struct ISECurveBodyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISECurveBody_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISECurveBody_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISECurveBody_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISECurveBody_GetExactRangeBox(This,pMinPoint,pMaxPoint)	\
    ( (This)->lpVtbl -> GetExactRangeBox(This,pMinPoint,pMaxPoint) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISECurveBody_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0001 */
/* [local] */ 

//////////////////SE-Custom interface on Curve Path: (ISECurvePath)////////////////////////////////////////
// 1. GetExactRangeBox: Returns the exact minimum and maximum points of this Curve Path
//    'pMinPoint' is an array of three doubles containing the minimum point.
//    'pMaxPoint' is an array of three doubles containing the maximum point.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0001_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0001_v0_0_s_ifspec;

#ifndef __ISECurvePath_INTERFACE_DEFINED__
#define __ISECurvePath_INTERFACE_DEFINED__

/* interface ISECurvePath */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISECurvePath;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("BA37DDEE-83AE-48f2-803F-62CA6A4D6F05")
    ISECurvePath : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetExactRangeBox( 
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISECurvePathVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISECurvePath * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISECurvePath * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISECurvePath * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetExactRangeBox )( 
            ISECurvePath * This,
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]);
        
        END_INTERFACE
    } ISECurvePathVtbl;

    interface ISECurvePath
    {
        CONST_VTBL struct ISECurvePathVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISECurvePath_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISECurvePath_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISECurvePath_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISECurvePath_GetExactRangeBox(This,pMinPoint,pMaxPoint)	\
    ( (This)->lpVtbl -> GetExactRangeBox(This,pMinPoint,pMaxPoint) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISECurvePath_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0002 */
/* [local] */ 

//////////////////SE-Custom interface on Shell: (ISEShell)////////////////////////////////////////
// 1. GetExactRangeBox: Returns the exact minimum and maximum points of this Shell
//    'pMinPoint' is an array of three doubles containing the minimum point.
//    'pMaxPoint' is an array of three doubles containing the maximum point.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0002_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0002_v0_0_s_ifspec;

#ifndef __ISEShell_INTERFACE_DEFINED__
#define __ISEShell_INTERFACE_DEFINED__

/* interface ISEShell */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEShell;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("51527A35-5CA4-45ce-95EF-AF27B48574ED")
    ISEShell : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetExactRangeBox( 
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEShellVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEShell * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEShell * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEShell * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetExactRangeBox )( 
            ISEShell * This,
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]);
        
        END_INTERFACE
    } ISEShellVtbl;

    interface ISEShell
    {
        CONST_VTBL struct ISEShellVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEShell_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEShell_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEShell_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEShell_GetExactRangeBox(This,pMinPoint,pMaxPoint)	\
    ( (This)->lpVtbl -> GetExactRangeBox(This,pMinPoint,pMaxPoint) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEShell_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0003 */
/* [local] */ 

//////////////////SE-Custom interface on Shell: (ISEShell2)////////////////////////////////////////
// 1. GetID: Returns the parasolid ID of the Shell
// 2. GetTag: Returns the parasolid Session Tag of the Shell
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0003_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0003_v0_0_s_ifspec;

#ifndef __ISEShell2_INTERFACE_DEFINED__
#define __ISEShell2_INTERFACE_DEFINED__

/* interface ISEShell2 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEShell2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2E6CDEBA-5801-45de-BF79-5DAD0C7298C3")
    ISEShell2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetID( 
            /* [out] */ int *pnID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTag( 
            /* [out] */ int *pnTag) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEShell2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEShell2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEShell2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEShell2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetID )( 
            ISEShell2 * This,
            /* [out] */ int *pnID);
        
        HRESULT ( STDMETHODCALLTYPE *GetTag )( 
            ISEShell2 * This,
            /* [out] */ int *pnTag);
        
        END_INTERFACE
    } ISEShell2Vtbl;

    interface ISEShell2
    {
        CONST_VTBL struct ISEShell2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEShell2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEShell2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEShell2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEShell2_GetID(This,pnID)	\
    ( (This)->lpVtbl -> GetID(This,pnID) ) 

#define ISEShell2_GetTag(This,pnTag)	\
    ( (This)->lpVtbl -> GetTag(This,pnTag) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEShell2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0004 */
/* [local] */ 

//////////////////SE-Custom interface on Curve: (ISECurve)////////////////////////////////////////
// 1. GetExactRangeBox: Returns the exact minimum and maximum points of this Curve
//    'pMinPoint' is an array of three doubles containing the minimum point.
//    'pMaxPoint' is an array of three doubles containing the maximum point.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0004_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0004_v0_0_s_ifspec;

#ifndef __ISECurve_INTERFACE_DEFINED__
#define __ISECurve_INTERFACE_DEFINED__

/* interface ISECurve */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISECurve;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F0A768B9-CF4C-4bd8-A008-E709697725D3")
    ISECurve : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetExactRangeBox( 
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISECurveVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISECurve * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISECurve * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISECurve * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetExactRangeBox )( 
            ISECurve * This,
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]);
        
        END_INTERFACE
    } ISECurveVtbl;

    interface ISECurve
    {
        CONST_VTBL struct ISECurveVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISECurve_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISECurve_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISECurve_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISECurve_GetExactRangeBox(This,pMinPoint,pMaxPoint)	\
    ( (This)->lpVtbl -> GetExactRangeBox(This,pMinPoint,pMaxPoint) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISECurve_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0005 */
/* [local] */ 

//////////////////SE-Custom interface on Surface Body: (ISESurfaceBody)///////////////////////////////////////////
// 1. EnumVertices: Returns the total number of unique vertices on this Surface Body. The
//    enumerator is over IDMVertex, the OLEforDM interface.
// 2. GetFacets: Returns the facetted version of this Surface Body, where the facet-triangular
//    plane is guaranteed to be within a maximum-distance of ChordHeightTol from the surface.
//    'nFacetCount' is the number of facet-triangles with 'ppPoints' being the flat array consisting
//    of nFacetCount*3*3 doubles, where each triangle is output as 3 triplets for each of its vertices.
//    The memory has been allocated using CoTaskMemAlloc (and MUST be freed by the caller using CoTaskMemFree).
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0005_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0005_v0_0_s_ifspec;

#ifndef __ISESurfaceBody_INTERFACE_DEFINED__
#define __ISESurfaceBody_INTERFACE_DEFINED__

/* interface ISESurfaceBody */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISESurfaceBody;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0712815E-D31A-11d1-BEC8-080036D7B302")
    ISESurfaceBody : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumVertices( 
            /* [out] */ LPENUM_DMVERTICES *ppEnumVertices) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFacets( 
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISESurfaceBodyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISESurfaceBody * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISESurfaceBody * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISESurfaceBody * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumVertices )( 
            ISESurfaceBody * This,
            /* [out] */ LPENUM_DMVERTICES *ppEnumVertices);
        
        HRESULT ( STDMETHODCALLTYPE *GetFacets )( 
            ISESurfaceBody * This,
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints);
        
        END_INTERFACE
    } ISESurfaceBodyVtbl;

    interface ISESurfaceBody
    {
        CONST_VTBL struct ISESurfaceBodyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISESurfaceBody_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISESurfaceBody_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISESurfaceBody_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISESurfaceBody_EnumVertices(This,ppEnumVertices)	\
    ( (This)->lpVtbl -> EnumVertices(This,ppEnumVertices) ) 

#define ISESurfaceBody_GetFacets(This,ChordHeightTol,nFacetCount,ppPoints)	\
    ( (This)->lpVtbl -> GetFacets(This,ChordHeightTol,nFacetCount,ppPoints) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISESurfaceBody_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0006 */
/* [local] */ 

//////////////////SE-Custom interface on Surface Body: (ISESurfaceBody2)////////////////////////////////////////
// 1. GetFacetsWithNormals: Returns the facetted version of this Surface Body with normals at each vertex,
//    where the facet-triangular plane is guaranteed to be within a maximum-distance of ChordHeightTol from the surface.
//    'nFacetCount' is the number of facet-triangles with 'ppPoints' and 'ppNormals being the flat arrays consisting
//    of nFacetCount*3*3 doubles, where each triangle is output as 3 triplets for each of its vertices.
//    The memory has been allocated using CoTaskMemAlloc (and MUST be freed by the caller using CoTaskMemFree).
// 2. GetEntityFromParasolID: Given the persistant parasolid ID of a topological entity (face/edge/vertex) belonging to
//    this Surface Body, this method returns the desired interface on the G&T object corresponding to that entity.
//    It is to be noted that this method may not succeed or return erroneous result if the body was modified since
//    the time the client obtained the parasolid ID (input to this method) from the body.
// 3. GetParasolIDFromEntity: This method, which is the counterpart of the method GetEntityFromParsolID, returns the
//    persistant parasolid ID of the input G&T entity contained in this Surface Body.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0006_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0006_v0_0_s_ifspec;

#ifndef __ISESurfaceBody2_INTERFACE_DEFINED__
#define __ISESurfaceBody2_INTERFACE_DEFINED__

/* interface ISESurfaceBody2 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISESurfaceBody2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9DE673C1-7B54-11d3-9734-0060973828B5")
    ISESurfaceBody2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFacetsWithNormals( 
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints,
            /* [size_is][size_is][out] */ double **ppNormals) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetEntityFromParasolID( 
            /* [in] */ int nID,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParasolIDFromEntity( 
            /* [in] */ LPUNKNOWN pEntityUnk,
            /* [out] */ int *pnID) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISESurfaceBody2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISESurfaceBody2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISESurfaceBody2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISESurfaceBody2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetFacetsWithNormals )( 
            ISESurfaceBody2 * This,
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints,
            /* [size_is][size_is][out] */ double **ppNormals);
        
        HRESULT ( STDMETHODCALLTYPE *GetEntityFromParasolID )( 
            ISESurfaceBody2 * This,
            /* [in] */ int nID,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        HRESULT ( STDMETHODCALLTYPE *GetParasolIDFromEntity )( 
            ISESurfaceBody2 * This,
            /* [in] */ LPUNKNOWN pEntityUnk,
            /* [out] */ int *pnID);
        
        END_INTERFACE
    } ISESurfaceBody2Vtbl;

    interface ISESurfaceBody2
    {
        CONST_VTBL struct ISESurfaceBody2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISESurfaceBody2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISESurfaceBody2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISESurfaceBody2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISESurfaceBody2_GetFacetsWithNormals(This,ChordHeightTol,nFacetCount,ppPoints,ppNormals)	\
    ( (This)->lpVtbl -> GetFacetsWithNormals(This,ChordHeightTol,nFacetCount,ppPoints,ppNormals) ) 

#define ISESurfaceBody2_GetEntityFromParasolID(This,nID,riid,ppvObject)	\
    ( (This)->lpVtbl -> GetEntityFromParasolID(This,nID,riid,ppvObject) ) 

#define ISESurfaceBody2_GetParasolIDFromEntity(This,pEntityUnk,pnID)	\
    ( (This)->lpVtbl -> GetParasolIDFromEntity(This,pEntityUnk,pnID) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISESurfaceBody2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0007 */
/* [local] */ 

//////////////////SE-Custom interface on Surface Body: (ISESurfaceBody3)////////////////////////////////////////
// 1. GetFacetsWithNormalsAndTextureCoords: Returns the facetted version of this Surface Body with normals and uv
//    texture coordinates at each vertex, where the facet-triangular plane is guaranteed to be within a maximum-distance
//    of ChordHeightTol from the surface. 'nFacetCount' is the number of facet-triangles with 'ppPoints' and 'ppNormals'
//    being the flat arrays consisting of nFacetCount*3*3 doubles, where each triangle is output as 3 triplets for each
//    of its vertices. 'ppTexCoords' is a flat array consisting of nFacetCount*3*2 doubles, where each triangle is output
//    as 3 uv pairs for each of its vertices. The memory has been allocated using CoTaskMemAlloc (and MUST be freed by
//    the caller using CoTaskMemFree).
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0007_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0007_v0_0_s_ifspec;

#ifndef __ISESurfaceBody3_INTERFACE_DEFINED__
#define __ISESurfaceBody3_INTERFACE_DEFINED__

/* interface ISESurfaceBody3 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISESurfaceBody3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("687B9BEE-D1E1-4d77-9072-D8D010B77D8E")
    ISESurfaceBody3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFacetsWithNormalsAndTextureCoords( 
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints,
            /* [size_is][size_is][out] */ double **ppNormals,
            /* [size_is][size_is][out] */ double **ppTexCoords,
            /* [size_is][size_is][out] */ long **ppStyleIDs,
            /* [size_is][size_is][out] */ long **ppFaceIDs) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISESurfaceBody3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISESurfaceBody3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISESurfaceBody3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISESurfaceBody3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetFacetsWithNormalsAndTextureCoords )( 
            ISESurfaceBody3 * This,
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints,
            /* [size_is][size_is][out] */ double **ppNormals,
            /* [size_is][size_is][out] */ double **ppTexCoords,
            /* [size_is][size_is][out] */ long **ppStyleIDs,
            /* [size_is][size_is][out] */ long **ppFaceIDs);
        
        END_INTERFACE
    } ISESurfaceBody3Vtbl;

    interface ISESurfaceBody3
    {
        CONST_VTBL struct ISESurfaceBody3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISESurfaceBody3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISESurfaceBody3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISESurfaceBody3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISESurfaceBody3_GetFacetsWithNormalsAndTextureCoords(This,ChordHeightTol,nFacetCount,ppPoints,ppNormals,ppTexCoords,ppStyleIDs,ppFaceIDs)	\
    ( (This)->lpVtbl -> GetFacetsWithNormalsAndTextureCoords(This,ChordHeightTol,nFacetCount,ppPoints,ppNormals,ppTexCoords,ppStyleIDs,ppFaceIDs) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISESurfaceBody3_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0008 */
/* [local] */ 

//////////////////SE-Custom interface on Surface Body: (ISESurfaceBody4)////////////////////////////////////////
// 1. GetExactRangeBox: Returns the exact minimum and maximum points of this Surface Body
//    'pMinPoint' is an array of three doubles containing the minimum point.
//    'pMaxPoint' is an array of three doubles containing the maximum point.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0008_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0008_v0_0_s_ifspec;

#ifndef __ISESurfaceBody4_INTERFACE_DEFINED__
#define __ISESurfaceBody4_INTERFACE_DEFINED__

/* interface ISESurfaceBody4 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISESurfaceBody4;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("43C68C58-5FE8-4d44-B08C-0BF3F61770BB")
    ISESurfaceBody4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetExactRangeBox( 
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISESurfaceBody4Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISESurfaceBody4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISESurfaceBody4 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISESurfaceBody4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetExactRangeBox )( 
            ISESurfaceBody4 * This,
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]);
        
        END_INTERFACE
    } ISESurfaceBody4Vtbl;

    interface ISESurfaceBody4
    {
        CONST_VTBL struct ISESurfaceBody4Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISESurfaceBody4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISESurfaceBody4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISESurfaceBody4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISESurfaceBody4_GetExactRangeBox(This,pMinPoint,pMaxPoint)	\
    ( (This)->lpVtbl -> GetExactRangeBox(This,pMinPoint,pMaxPoint) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISESurfaceBody4_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0009 */
/* [local] */ 

//////////////////SE-Custom interface on Surface Body: (ISESurfaceBody5)////////////////////////////////////////
// 1. SetBodyOverride: To set or unset override property on the Surface body
// 2. IsBodyOverriden: Returns whether the Surface body is overriden or not
//    It returns true if the surface body participates in a assembly feature/pipe
//    or frame etc.
// 3. GetOccurrenceObject: Returns the corresponding object of the body
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0009_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0009_v0_0_s_ifspec;

#ifndef __ISESurfaceBody5_INTERFACE_DEFINED__
#define __ISESurfaceBody5_INTERFACE_DEFINED__

/* interface ISESurfaceBody5 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISESurfaceBody5;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0EE63671-CEA7-4de3-BF1D-0E5D2A992D75")
    ISESurfaceBody5 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetBodyOverride( 
            /* [in] */ boolean bSet) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsBodyOverriden( 
            /* [out] */ boolean *pBodyOverriden) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetOccurrenceObject( 
            /* [out] */ LPUNKNOWN *ppOcc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSimplifiedBody( 
            /* [out] */ boolean *pSimplifiedBody) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetSimplifiedBody( 
            /* [in] */ boolean bSet) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISESurfaceBody5Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISESurfaceBody5 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISESurfaceBody5 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISESurfaceBody5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetBodyOverride )( 
            ISESurfaceBody5 * This,
            /* [in] */ boolean bSet);
        
        HRESULT ( STDMETHODCALLTYPE *IsBodyOverriden )( 
            ISESurfaceBody5 * This,
            /* [out] */ boolean *pBodyOverriden);
        
        HRESULT ( STDMETHODCALLTYPE *GetOccurrenceObject )( 
            ISESurfaceBody5 * This,
            /* [out] */ LPUNKNOWN *ppOcc);
        
        HRESULT ( STDMETHODCALLTYPE *IsSimplifiedBody )( 
            ISESurfaceBody5 * This,
            /* [out] */ boolean *pSimplifiedBody);
        
        HRESULT ( STDMETHODCALLTYPE *SetSimplifiedBody )( 
            ISESurfaceBody5 * This,
            /* [in] */ boolean bSet);
        
        END_INTERFACE
    } ISESurfaceBody5Vtbl;

    interface ISESurfaceBody5
    {
        CONST_VTBL struct ISESurfaceBody5Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISESurfaceBody5_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISESurfaceBody5_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISESurfaceBody5_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISESurfaceBody5_SetBodyOverride(This,bSet)	\
    ( (This)->lpVtbl -> SetBodyOverride(This,bSet) ) 

#define ISESurfaceBody5_IsBodyOverriden(This,pBodyOverriden)	\
    ( (This)->lpVtbl -> IsBodyOverriden(This,pBodyOverriden) ) 

#define ISESurfaceBody5_GetOccurrenceObject(This,ppOcc)	\
    ( (This)->lpVtbl -> GetOccurrenceObject(This,ppOcc) ) 

#define ISESurfaceBody5_IsSimplifiedBody(This,pSimplifiedBody)	\
    ( (This)->lpVtbl -> IsSimplifiedBody(This,pSimplifiedBody) ) 

#define ISESurfaceBody5_SetSimplifiedBody(This,bSet)	\
    ( (This)->lpVtbl -> SetSimplifiedBody(This,bSet) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISESurfaceBody5_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0010 */
/* [local] */ 

//////////////////SE-Custom interface on Surface Body: (ISESurfaceBody6)////////////////////////////////////////
// 1. GetTag: Get the Session Tag
// 2. GetEntityType: Returns the Parasolid Entity Class of the Entity
// 3. EnumLoops: Returns the total number of unique Loops on this Surface Body.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0010_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0010_v0_0_s_ifspec;

#ifndef __ISESurfaceBody6_INTERFACE_DEFINED__
#define __ISESurfaceBody6_INTERFACE_DEFINED__

/* interface ISESurfaceBody6 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISESurfaceBody6;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("A561FC74-183F-419c-AD3A-F276B939938C")
    ISESurfaceBody6 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetTag( 
            /* [out] */ int *pnTag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetEntityType( 
            /* [in] */ LPUNKNOWN pEntityUnk,
            /* [retval][out] */ int *EntityClass) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumLoops( 
            /* [out] */ LPENUM_DMLOOPS *ppEnumLoops) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISESurfaceBody6Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISESurfaceBody6 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISESurfaceBody6 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISESurfaceBody6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetTag )( 
            ISESurfaceBody6 * This,
            /* [out] */ int *pnTag);
        
        HRESULT ( STDMETHODCALLTYPE *GetEntityType )( 
            ISESurfaceBody6 * This,
            /* [in] */ LPUNKNOWN pEntityUnk,
            /* [retval][out] */ int *EntityClass);
        
        HRESULT ( STDMETHODCALLTYPE *EnumLoops )( 
            ISESurfaceBody6 * This,
            /* [out] */ LPENUM_DMLOOPS *ppEnumLoops);
        
        END_INTERFACE
    } ISESurfaceBody6Vtbl;

    interface ISESurfaceBody6
    {
        CONST_VTBL struct ISESurfaceBody6Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISESurfaceBody6_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISESurfaceBody6_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISESurfaceBody6_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISESurfaceBody6_GetTag(This,pnTag)	\
    ( (This)->lpVtbl -> GetTag(This,pnTag) ) 

#define ISESurfaceBody6_GetEntityType(This,pEntityUnk,EntityClass)	\
    ( (This)->lpVtbl -> GetEntityType(This,pEntityUnk,EntityClass) ) 

#define ISESurfaceBody6_EnumLoops(This,ppEnumLoops)	\
    ( (This)->lpVtbl -> EnumLoops(This,ppEnumLoops) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISESurfaceBody6_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0011 */
/* [local] */ 

//////////////////SE-Custom interface on Face: (ISEFace)////////////////////////////////////////////////////
// 1. GetFacets: Returns the facetted version of this Face, where the facet-triangular
//    plane is guaranteed to be within a maximum-distance of ChordHeightTol from the surface.
//    'nFacetCount' is the number of facet-triangles with 'ppPoints' being the flat array consisting
//    of nFacetCount*3*3 doubles, where each triangle is output as 3 triplets for each of its vertices.
//    The memory has been allocated using CoTaskMemAlloc (and MUST be freed by the caller using CoTaskMemFree).
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0011_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0011_v0_0_s_ifspec;

#ifndef __ISEFace_INTERFACE_DEFINED__
#define __ISEFace_INTERFACE_DEFINED__

/* interface ISEFace */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEFace;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B03B890E-F3DD-11d1-BEC9-080036D7B302")
    ISEFace : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFacets( 
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEFaceVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEFace * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEFace * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEFace * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetFacets )( 
            ISEFace * This,
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints);
        
        END_INTERFACE
    } ISEFaceVtbl;

    interface ISEFace
    {
        CONST_VTBL struct ISEFaceVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEFace_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEFace_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEFace_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEFace_GetFacets(This,ChordHeightTol,nFacetCount,ppPoints)	\
    ( (This)->lpVtbl -> GetFacets(This,ChordHeightTol,nFacetCount,ppPoints) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEFace_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0012 */
/* [local] */ 

//////////////////SE-Custom interface on Face: (ISEFace2)////////////////////////////////////////
// 1. GetFacetsWithNormals: Returns the facetted version of this Face with normals at each vertex,
//    where the facet-triangular plane is guaranteed to be within a maximum-distance of ChordHeightTol from the surface.
//    'nFacetCount' is the number of facet-triangles with 'ppPoints' and 'ppNormals being the flat arrays consisting
//    of nFacetCount*3*3 doubles, where each triangle is output as 3 triplets for each of its vertices.
//    The memory has been allocated using CoTaskMemAlloc (and MUST be freed by the caller using CoTaskMemFree).
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0012_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0012_v0_0_s_ifspec;

#ifndef __ISEFace2_INTERFACE_DEFINED__
#define __ISEFace2_INTERFACE_DEFINED__

/* interface ISEFace2 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEFace2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9DE673C0-7B54-11d3-9734-0060973828B5")
    ISEFace2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFacetsWithNormals( 
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints,
            /* [size_is][size_is][out] */ double **ppNormals) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEFace2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEFace2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEFace2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEFace2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetFacetsWithNormals )( 
            ISEFace2 * This,
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints,
            /* [size_is][size_is][out] */ double **ppNormals);
        
        END_INTERFACE
    } ISEFace2Vtbl;

    interface ISEFace2
    {
        CONST_VTBL struct ISEFace2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEFace2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEFace2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEFace2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEFace2_GetFacetsWithNormals(This,ChordHeightTol,nFacetCount,ppPoints,ppNormals)	\
    ( (This)->lpVtbl -> GetFacetsWithNormals(This,ChordHeightTol,nFacetCount,ppPoints,ppNormals) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEFace2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0013 */
/* [local] */ 

//////////////////SE-Custom interface on Face: (ISEFace3)////////////////////////////////////////
// 1. GetFacetsWithNormalsAndTextureCoords: Returns the facetted version of this Surface Body with normals and uv
//    texture coordinates at each vertex, where the facet-triangular plane is guaranteed to be within a maximum-distance
//    of ChordHeightTol from the surface. 'nFacetCount' is the number of facet-triangles with 'ppPoints' and 'ppNormals'
//    being the flat arrays consisting of nFacetCount*3*3 doubles, where each triangle is output as 3 triplets for each
//    of its vertices. 'ppTexCoords' is a flat array consisting of nFacetCount*3*2 doubles, where each triangle is output
//    as 3 uv pairs for each of its vertices. The memory has been allocated using CoTaskMemAlloc (and MUST be freed by
//    the caller using CoTaskMemFree).
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0013_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0013_v0_0_s_ifspec;

#ifndef __ISEFace3_INTERFACE_DEFINED__
#define __ISEFace3_INTERFACE_DEFINED__

/* interface ISEFace3 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEFace3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5270701E-1067-4bb2-9704-A4A0EDFBC559")
    ISEFace3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFacetsWithNormalsAndTextureCoords( 
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints,
            /* [size_is][size_is][out] */ double **ppNormals,
            /* [size_is][size_is][out] */ double **ppTexCoords) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEFace3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEFace3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEFace3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEFace3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetFacetsWithNormalsAndTextureCoords )( 
            ISEFace3 * This,
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nFacetCount,
            /* [size_is][size_is][out] */ double **ppPoints,
            /* [size_is][size_is][out] */ double **ppNormals,
            /* [size_is][size_is][out] */ double **ppTexCoords);
        
        END_INTERFACE
    } ISEFace3Vtbl;

    interface ISEFace3
    {
        CONST_VTBL struct ISEFace3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEFace3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEFace3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEFace3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEFace3_GetFacetsWithNormalsAndTextureCoords(This,ChordHeightTol,nFacetCount,ppPoints,ppNormals,ppTexCoords)	\
    ( (This)->lpVtbl -> GetFacetsWithNormalsAndTextureCoords(This,ChordHeightTol,nFacetCount,ppPoints,ppNormals,ppTexCoords) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEFace3_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0014 */
/* [local] */ 

//////////////////SE-Custom interface on Face: (ISEFace4)////////////////////////////////////////
// 1. GetExactRangeBox: Returns the exact minimum and maximum points of this Face
//    'pMinPoint' is an array of three doubles containing the minimum point.
//    'pMaxPoint' is an array of three doubles containing the maximum point.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0014_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0014_v0_0_s_ifspec;

#ifndef __ISEFace4_INTERFACE_DEFINED__
#define __ISEFace4_INTERFACE_DEFINED__

/* interface ISEFace4 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEFace4;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("17B378D2-1671-4b1d-8E77-7BD498A43FCA")
    ISEFace4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetExactRangeBox( 
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEFace4Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEFace4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEFace4 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEFace4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetExactRangeBox )( 
            ISEFace4 * This,
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]);
        
        END_INTERFACE
    } ISEFace4Vtbl;

    interface ISEFace4
    {
        CONST_VTBL struct ISEFace4Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEFace4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEFace4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEFace4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEFace4_GetExactRangeBox(This,pMinPoint,pMaxPoint)	\
    ( (This)->lpVtbl -> GetExactRangeBox(This,pMinPoint,pMaxPoint) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEFace4_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0015 */
/* [local] */ 

//////////////////SE-Custom interface on Face: (ISEFace4)////////////////////////////////////////
// 1. GetID: Returns the parasolid ID of the Face
// 2. GetTag: Returns the parasolid Session Tag of the Face
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0015_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0015_v0_0_s_ifspec;

#ifndef __ISEFace5_INTERFACE_DEFINED__
#define __ISEFace5_INTERFACE_DEFINED__

/* interface ISEFace5 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEFace5;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("73FAFD0D-78F9-4da5-B62C-860C2909CD23")
    ISEFace5 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetID( 
            /* [out] */ int *pnID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTag( 
            /* [out] */ int *pnTag) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEFace5Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEFace5 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEFace5 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEFace5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetID )( 
            ISEFace5 * This,
            /* [out] */ int *pnID);
        
        HRESULT ( STDMETHODCALLTYPE *GetTag )( 
            ISEFace5 * This,
            /* [out] */ int *pnTag);
        
        END_INTERFACE
    } ISEFace5Vtbl;

    interface ISEFace5
    {
        CONST_VTBL struct ISEFace5Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEFace5_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEFace5_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEFace5_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEFace5_GetID(This,pnID)	\
    ( (This)->lpVtbl -> GetID(This,pnID) ) 

#define ISEFace5_GetTag(This,pnTag)	\
    ( (This)->lpVtbl -> GetTag(This,pnTag) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEFace5_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0016 */
/* [local] */ 

//////////////////SE-Custom interface on Edge: (ISEEdge)////////////////////////////////////////////////////
// 1. GetStrokes: Returns the stroked or polyline version of this Edge, where each of the segments
//    is guaranteed to be within a maximum-distance of ChordHeightTol from the curve of the Edge.
//    'nStrokeCount' is the number of points in the polyline with 'ppPoints' being the array consisting
//    of nStrokeCount*3 doubles.  The parameters on the Edge-curve at which each of these points
//    are evaluated is returned in 'ppParams'. Either of these arrays,  'ppPoints' or 'ppParams' can
//    NULL (when that particular output is not desired by the caller).
//    The memory has been allocated using CoTaskMemAlloc (and MUST be freed by the caller using CoTaskMemFree).
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0016_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0016_v0_0_s_ifspec;

#ifndef __ISEEdge_INTERFACE_DEFINED__
#define __ISEEdge_INTERFACE_DEFINED__

/* interface ISEEdge */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEEdge;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("CDFD024C-F3DD-11d1-BEC9-080036D7B302")
    ISEEdge : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetStrokes( 
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nStrokeCount,
            /* [size_is][size_is][out] */ double **ppPoints,
            /* [size_is][size_is][out] */ double **ppParams) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEEdgeVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEEdge * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEEdge * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEEdge * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetStrokes )( 
            ISEEdge * This,
            /* [in] */ double ChordHeightTol,
            /* [out] */ long *nStrokeCount,
            /* [size_is][size_is][out] */ double **ppPoints,
            /* [size_is][size_is][out] */ double **ppParams);
        
        END_INTERFACE
    } ISEEdgeVtbl;

    interface ISEEdge
    {
        CONST_VTBL struct ISEEdgeVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEEdge_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEEdge_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEEdge_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEEdge_GetStrokes(This,ChordHeightTol,nStrokeCount,ppPoints,ppParams)	\
    ( (This)->lpVtbl -> GetStrokes(This,ChordHeightTol,nStrokeCount,ppPoints,ppParams) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEEdge_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0017 */
/* [local] */ 

//////////////////SE-Custom interface on Edge: (ISEEdge2)////////////////////////////////////////
// 1. GetExactRangeBox: Returns the exact minimum and maximum points of this Edge
//    'pMinPoint' is an array of three doubles containing the minimum point.
//    'pMaxPoint' is an array of three doubles containing the maximum point.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0017_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0017_v0_0_s_ifspec;

#ifndef __ISEEdge2_INTERFACE_DEFINED__
#define __ISEEdge2_INTERFACE_DEFINED__

/* interface ISEEdge2 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEEdge2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("DC529C93-F335-43b7-85FA-DFAA8E1C6A19")
    ISEEdge2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetExactRangeBox( 
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEEdge2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEEdge2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEEdge2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEEdge2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetExactRangeBox )( 
            ISEEdge2 * This,
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]);
        
        END_INTERFACE
    } ISEEdge2Vtbl;

    interface ISEEdge2
    {
        CONST_VTBL struct ISEEdge2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEEdge2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEEdge2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEEdge2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEEdge2_GetExactRangeBox(This,pMinPoint,pMaxPoint)	\
    ( (This)->lpVtbl -> GetExactRangeBox(This,pMinPoint,pMaxPoint) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEEdge2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0018 */
/* [local] */ 

//////////////////SE-Custom interface on Edge: (ISEEdge3)////////////////////////////////////////
// 1. GetID: Returns the Parasolid ID this Edge
// 2. GetTag: Returns the Parasolid Session Tag of this Edge
// 3. GetBody: Returns the Body associated with this Edge
// 4. EnumShells: Returns the total number of unique Shells on this Edge
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0018_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0018_v0_0_s_ifspec;

#ifndef __ISEEdge3_INTERFACE_DEFINED__
#define __ISEEdge3_INTERFACE_DEFINED__

/* interface ISEEdge3 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEEdge3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("43D23E94-B7F2-455e-870B-647EFED614EB")
    ISEEdge3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetID( 
            /* [out] */ int *pnID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTag( 
            /* [out] */ int *pnTag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBody( 
            /* [out] */ LPDMSURFACEBODY *pBody) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumShells( 
            /* [out] */ LPENUM_DMSHELLS *pShells) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEEdge3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEEdge3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEEdge3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEEdge3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetID )( 
            ISEEdge3 * This,
            /* [out] */ int *pnID);
        
        HRESULT ( STDMETHODCALLTYPE *GetTag )( 
            ISEEdge3 * This,
            /* [out] */ int *pnTag);
        
        HRESULT ( STDMETHODCALLTYPE *GetBody )( 
            ISEEdge3 * This,
            /* [out] */ LPDMSURFACEBODY *pBody);
        
        HRESULT ( STDMETHODCALLTYPE *EnumShells )( 
            ISEEdge3 * This,
            /* [out] */ LPENUM_DMSHELLS *pShells);
        
        END_INTERFACE
    } ISEEdge3Vtbl;

    interface ISEEdge3
    {
        CONST_VTBL struct ISEEdge3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEEdge3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEEdge3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEEdge3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEEdge3_GetID(This,pnID)	\
    ( (This)->lpVtbl -> GetID(This,pnID) ) 

#define ISEEdge3_GetTag(This,pnTag)	\
    ( (This)->lpVtbl -> GetTag(This,pnTag) ) 

#define ISEEdge3_GetBody(This,pBody)	\
    ( (This)->lpVtbl -> GetBody(This,pBody) ) 

#define ISEEdge3_EnumShells(This,pShells)	\
    ( (This)->lpVtbl -> EnumShells(This,pShells) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEEdge3_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0019 */
/* [local] */ 

//////////////////SE-Custom interface on Vertex: (ISEVertex)////////////////////////////////////////
// 1. GetID: Returns the Parasolid ID this Vertex
// 2. GetTag: Returns the Parasolid Session Tag of this Vertex
// 3. GetBody: Returns the Body associated with this Vertex
// 4. EnumShells: Returns the total number of unique Shells on this Vertex.
// 5. EnumLoops: Returns the total number of unique Loops on this Vertex.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0019_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0019_v0_0_s_ifspec;

#ifndef __ISEVertex_INTERFACE_DEFINED__
#define __ISEVertex_INTERFACE_DEFINED__

/* interface ISEVertex */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEVertex;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9C8D3832-3793-43fd-89BB-65310E9FE72E")
    ISEVertex : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetID( 
            /* [out] */ int *pnID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTag( 
            /* [out] */ int *pnTag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBody( 
            /* [out] */ LPDMSURFACEBODY *pBody) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumShells( 
            /* [out] */ LPENUM_DMSHELLS *pShells) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumLoops( 
            /* [out] */ LPENUM_DMLOOPS *pLoops) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEVertexVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEVertex * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEVertex * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEVertex * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetID )( 
            ISEVertex * This,
            /* [out] */ int *pnID);
        
        HRESULT ( STDMETHODCALLTYPE *GetTag )( 
            ISEVertex * This,
            /* [out] */ int *pnTag);
        
        HRESULT ( STDMETHODCALLTYPE *GetBody )( 
            ISEVertex * This,
            /* [out] */ LPDMSURFACEBODY *pBody);
        
        HRESULT ( STDMETHODCALLTYPE *EnumShells )( 
            ISEVertex * This,
            /* [out] */ LPENUM_DMSHELLS *pShells);
        
        HRESULT ( STDMETHODCALLTYPE *EnumLoops )( 
            ISEVertex * This,
            /* [out] */ LPENUM_DMLOOPS *pLoops);
        
        END_INTERFACE
    } ISEVertexVtbl;

    interface ISEVertex
    {
        CONST_VTBL struct ISEVertexVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEVertex_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEVertex_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEVertex_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEVertex_GetID(This,pnID)	\
    ( (This)->lpVtbl -> GetID(This,pnID) ) 

#define ISEVertex_GetTag(This,pnTag)	\
    ( (This)->lpVtbl -> GetTag(This,pnTag) ) 

#define ISEVertex_GetBody(This,pBody)	\
    ( (This)->lpVtbl -> GetBody(This,pBody) ) 

#define ISEVertex_EnumShells(This,pShells)	\
    ( (This)->lpVtbl -> EnumShells(This,pShells) ) 

#define ISEVertex_EnumLoops(This,pLoops)	\
    ( (This)->lpVtbl -> EnumLoops(This,pLoops) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEVertex_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0020 */
/* [local] */ 

//////////////////SE-Custom interface on Loop: (ISELoop)////////////////////////////////////////////////////
// 1. EnumEdgeUses2: Enumerates all the EdgeUses that make up the Loop. In addition to enumerating the
//    EdgeUses as in IDMLoop::EnumEdgeUses, this method also enumerates 2D-only EdgeUses that represent
//    an isoparametric UV segment of a surface with a degenerate boundary. Some EdgeUses in this enumeration
//    may return true for method ISEEdgeUse::IsDegenerateBoundary.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0020_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0020_v0_0_s_ifspec;

#ifndef __ISELoop_INTERFACE_DEFINED__
#define __ISELoop_INTERFACE_DEFINED__

/* interface ISELoop */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISELoop;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("80A06BEC-39D2-40a2-8760-33F751294A08")
    ISELoop : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumEdgeUses2( 
            /* [out] */ LPENUM_DMEDGEUSES *pEnumEdgeUses) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISELoopVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISELoop * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISELoop * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISELoop * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumEdgeUses2 )( 
            ISELoop * This,
            /* [out] */ LPENUM_DMEDGEUSES *pEnumEdgeUses);
        
        END_INTERFACE
    } ISELoopVtbl;

    interface ISELoop
    {
        CONST_VTBL struct ISELoopVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISELoop_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISELoop_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISELoop_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISELoop_EnumEdgeUses2(This,pEnumEdgeUses)	\
    ( (This)->lpVtbl -> EnumEdgeUses2(This,pEnumEdgeUses) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISELoop_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0021 */
/* [local] */ 

//////////////////SE-Custom interface on Loop: (ISELoop2)////////////////////////////////////////
// 1. GetExactRangeBox: Returns the exact minimum and maximum points of this Loop
//    'pMinPoint' is an array of three doubles containing the minimum point.
//    'pMaxPoint' is an array of three doubles containing the maximum point.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0021_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0021_v0_0_s_ifspec;

#ifndef __ISELoop2_INTERFACE_DEFINED__
#define __ISELoop2_INTERFACE_DEFINED__

/* interface ISELoop2 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISELoop2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("A5107603-D999-4ee0-A4A1-5515280C64D6")
    ISELoop2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetExactRangeBox( 
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISELoop2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISELoop2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISELoop2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISELoop2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetExactRangeBox )( 
            ISELoop2 * This,
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]);
        
        END_INTERFACE
    } ISELoop2Vtbl;

    interface ISELoop2
    {
        CONST_VTBL struct ISELoop2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISELoop2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISELoop2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISELoop2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISELoop2_GetExactRangeBox(This,pMinPoint,pMaxPoint)	\
    ( (This)->lpVtbl -> GetExactRangeBox(This,pMinPoint,pMaxPoint) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISELoop2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0022 */
/* [local] */ 

//////////////////SE-Custom interface on Loop: (ISELoop3)////////////////////////////////////////
// 1. GetID: Returns the Parasolid ID this Loop
// 2. GetTag: Returns the Parasolid Session Tag of this Loop
// 3. GetBody: Returns the Body associated with this Loop
// 4. EnumShells: Returns the total number of unique Shells on this Loop.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0022_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0022_v0_0_s_ifspec;

#ifndef __ISELoop3_INTERFACE_DEFINED__
#define __ISELoop3_INTERFACE_DEFINED__

/* interface ISELoop3 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISELoop3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("C8188B59-34FF-43b3-931D-D0F49C233CD1")
    ISELoop3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetID( 
            /* [out] */ int *pnID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTag( 
            /* [out] */ int *pnTag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBody( 
            /* [out] */ LPDMSURFACEBODY *pBody) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumVertices( 
            /* [out] */ LPENUM_DMVERTICES *pEnumVertices) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISELoop3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISELoop3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISELoop3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISELoop3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetID )( 
            ISELoop3 * This,
            /* [out] */ int *pnID);
        
        HRESULT ( STDMETHODCALLTYPE *GetTag )( 
            ISELoop3 * This,
            /* [out] */ int *pnTag);
        
        HRESULT ( STDMETHODCALLTYPE *GetBody )( 
            ISELoop3 * This,
            /* [out] */ LPDMSURFACEBODY *pBody);
        
        HRESULT ( STDMETHODCALLTYPE *EnumVertices )( 
            ISELoop3 * This,
            /* [out] */ LPENUM_DMVERTICES *pEnumVertices);
        
        END_INTERFACE
    } ISELoop3Vtbl;

    interface ISELoop3
    {
        CONST_VTBL struct ISELoop3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISELoop3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISELoop3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISELoop3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISELoop3_GetID(This,pnID)	\
    ( (This)->lpVtbl -> GetID(This,pnID) ) 

#define ISELoop3_GetTag(This,pnTag)	\
    ( (This)->lpVtbl -> GetTag(This,pnTag) ) 

#define ISELoop3_GetBody(This,pBody)	\
    ( (This)->lpVtbl -> GetBody(This,pBody) ) 

#define ISELoop3_EnumVertices(This,pEnumVertices)	\
    ( (This)->lpVtbl -> EnumVertices(This,pEnumVertices) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISELoop3_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0023 */
/* [local] */ 

//////////////////SE-Custom interface on EdgeUse: (ISEEdgeUse)////////////////////////////////////////////////////
// 1. IsDegenerateBoundary: Returns a boolean flag indicating if the EdgeUse is a 2D-only entity that
//	 represents an isoparametric UV segment of a surface with a degenerate boundary. One example is the
//	 NURBS representation of a cone where the apex is a degenerate boundary. EdgeUses that return true
//	 for IsDegenerateBoundary will not have an associated Edge or partner EdgeUse.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0023_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0023_v0_0_s_ifspec;

#ifndef __ISEEdgeUse_INTERFACE_DEFINED__
#define __ISEEdgeUse_INTERFACE_DEFINED__

/* interface ISEEdgeUse */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEEdgeUse;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("49C40E77-3F01-4a24-9747-FEBD69DEC995")
    ISEEdgeUse : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsDegenerateBoundary( 
            /* [out] */ boolean *pIsDegenerateBoundary) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEEdgeUseVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEEdgeUse * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEEdgeUse * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEEdgeUse * This);
        
        HRESULT ( STDMETHODCALLTYPE *IsDegenerateBoundary )( 
            ISEEdgeUse * This,
            /* [out] */ boolean *pIsDegenerateBoundary);
        
        END_INTERFACE
    } ISEEdgeUseVtbl;

    interface ISEEdgeUse
    {
        CONST_VTBL struct ISEEdgeUseVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEEdgeUse_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEEdgeUse_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEEdgeUse_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEEdgeUse_IsDegenerateBoundary(This,pIsDegenerateBoundary)	\
    ( (This)->lpVtbl -> IsDegenerateBoundary(This,pIsDegenerateBoundary) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEEdgeUse_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0024 */
/* [local] */ 

//////////////////SE-Custom interface on EdgeUse: (ISEEdgeUse2)////////////////////////////////////////
// 1. GetExactRangeBox: Returns the exact minimum and maximum points of this EdgeUse
//    'pMinPoint' is an array of three doubles containing the minimum point.
//    'pMaxPoint' is an array of three doubles containing the maximum point.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0024_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0024_v0_0_s_ifspec;

#ifndef __ISEEdgeUse2_INTERFACE_DEFINED__
#define __ISEEdgeUse2_INTERFACE_DEFINED__

/* interface ISEEdgeUse2 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEEdgeUse2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8D77DBA4-3CE6-492d-8F0C-EB1226051775")
    ISEEdgeUse2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetExactRangeBox( 
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEEdgeUse2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEEdgeUse2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEEdgeUse2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEEdgeUse2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetExactRangeBox )( 
            ISEEdgeUse2 * This,
            /* [out] */ double pMinPoint[ 3 ],
            /* [out] */ double pMaxPoint[ 3 ]);
        
        END_INTERFACE
    } ISEEdgeUse2Vtbl;

    interface ISEEdgeUse2
    {
        CONST_VTBL struct ISEEdgeUse2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEEdgeUse2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEEdgeUse2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEEdgeUse2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEEdgeUse2_GetExactRangeBox(This,pMinPoint,pMaxPoint)	\
    ( (This)->lpVtbl -> GetExactRangeBox(This,pMinPoint,pMaxPoint) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEEdgeUse2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0025 */
/* [local] */ 

//////////////////SE-Custom interface on Component Definition: (ISEComponentDefinition)////////////////////////////////////////////////////
// 1. GetName: Returns the name of the component definition. Caller needs to free the output string using CoTaskMemFree.
// 2. GetFileName: Returns the name of the Solid Edge document associated with this component definition.
//    Caller needs to free the output string using CoTaskMemFree.
// 3. GetRangeBox: Returns the 3D extent of the component definition.
// 4. GetComponentCount: Gets the number of occurrences contained by an assembly component definition.
//    Caller specifies an enum of type SECOMPONENTCOUNTOPTION that can have one of the following values:
//    ALL_OCCURRENCES_ESTIMATE - to get an approximate estimate of the grand total of all occurrences contained in definition
//    ALL_UNIQUE_OCCURRENCES_ESTIMATE - to get an approximate estimate of the grand total of all unique occurrences contained in definition
//    ALL_OCCURRENCES - to get the precise grand total of all occurrences contained in definition
//    ALL_UNIQUE_OCCURRENCES - to get the precise grand total of all unique occurrences contained in definition
//    ALL_PART_OCCURRENCES - to get the precise grand total of all part occurrences contained in definition
//    ALL_UNIQUE_PART_OCCURRENCES - to get the precise grand total of all unique part occurrences contained in definition
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0025_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0025_v0_0_s_ifspec;

#ifndef __ISEComponentDefinition_INTERFACE_DEFINED__
#define __ISEComponentDefinition_INTERFACE_DEFINED__

/* interface ISEComponentDefinition */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEComponentDefinition;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("AF17F175-93A6-11d3-A3E2-0004AC9695BB")
    ISEComponentDefinition : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [string][out] */ OLECHAR **ppwszName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFileName( 
            /* [string][out] */ OLECHAR **ppwszFileName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRangeBox( 
            /* [out] */ double daMinPoint[ 3 ],
            /* [out] */ double daMaxPoint[ 3 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetComponentCount( 
            /* [in] */ SECOMPONENTCOUNTOPTION eOption,
            /* [out] */ long *nCount) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEComponentDefinitionVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEComponentDefinition * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEComponentDefinition * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEComponentDefinition * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            ISEComponentDefinition * This,
            /* [string][out] */ OLECHAR **ppwszName);
        
        HRESULT ( STDMETHODCALLTYPE *GetFileName )( 
            ISEComponentDefinition * This,
            /* [string][out] */ OLECHAR **ppwszFileName);
        
        HRESULT ( STDMETHODCALLTYPE *GetRangeBox )( 
            ISEComponentDefinition * This,
            /* [out] */ double daMinPoint[ 3 ],
            /* [out] */ double daMaxPoint[ 3 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetComponentCount )( 
            ISEComponentDefinition * This,
            /* [in] */ SECOMPONENTCOUNTOPTION eOption,
            /* [out] */ long *nCount);
        
        END_INTERFACE
    } ISEComponentDefinitionVtbl;

    interface ISEComponentDefinition
    {
        CONST_VTBL struct ISEComponentDefinitionVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEComponentDefinition_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEComponentDefinition_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEComponentDefinition_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEComponentDefinition_GetName(This,ppwszName)	\
    ( (This)->lpVtbl -> GetName(This,ppwszName) ) 

#define ISEComponentDefinition_GetFileName(This,ppwszFileName)	\
    ( (This)->lpVtbl -> GetFileName(This,ppwszFileName) ) 

#define ISEComponentDefinition_GetRangeBox(This,daMinPoint,daMaxPoint)	\
    ( (This)->lpVtbl -> GetRangeBox(This,daMinPoint,daMaxPoint) ) 

#define ISEComponentDefinition_GetComponentCount(This,eOption,nCount)	\
    ( (This)->lpVtbl -> GetComponentCount(This,eOption,nCount) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEComponentDefinition_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0026 */
/* [local] */ 

//////////////////SE-Custom interface on Component Occurrence: (ISEComponentOccurrence)////////////////////////////////////////////////////
// 1. GetName: Returns the name of the component occurrence. Caller needs to free the output string using CoTaskMemFree.
// 2. GetFileName: Returns the name of the Solid Edge document containing this occurrence's definition.
//    Caller needs to free the output string using CoTaskMemFree.
// 3. GetRangeBox: Returns the 3D extent of the component occurrence.
// 4. GetComponentCount: Gets the number of occurrences contained below this component occurrece.
//    Caller specifies an enum of type SECOMPONENTCOUNTOPTION that can have one of the following values:
//    IMMEDIATE_OCCURRENCES - to get the number of first level occurrences below this occurrence
//    IMMEDIATE_PART_OCCURRENCES - to get the number of first level part occurrences below this occurrence
//    IMMEDIATE_UNIQUE_OCCURRENCES - to get the number of first level unique occurrences below this occurrence
//    IMMEDIATE_UNIQUE_PART_OCCURRENCES - to get the number of first level unique part occurrences below this occurrence
//    ALL_OCCURRENCES_ESTIMATE - to get an estimated grand total of all occurrences below this occurrence
//    ALL_UNIQUE_OCCURRENCES_ESTIMATE - to get an estimated grand total of all unique occurrences below this occurrence
//    ALL_OCCURRENCES - to get the actual grand total of all occurrences below this occurrence
//    ALL_UNIQUE_OCCURRENCES - to get the actual grand total of all unique occurrences below this occurrence
//    ALL_PART_OCCURRENCES - to get the actual grand total of all part occurrences below this occurrence
//    ALL_UNIQUE_PART_OCCURRENCES - to get the actual grand total of all unique part occurrences below this occurrence
// 5. IsAssembly: Returns a boolean flag indicating whether this occurrence is a part occurrence.
// 6. GetMaterial: Returns color related information of the occurrence in the structure declared in this header.
//    This method applies only to part occurrences. Also NOTE, the color info can be extracted only from SE assemblies
//    saved in or after version 7. This function returns S_FALSE if it cannot access this information.
// 7. ThisAsOccurrence: It is a read-only property. It returns the corresponding occurrence object
//    of the sub-occurrence from the sub-assembly document.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0026_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0026_v0_0_s_ifspec;

#ifndef __ISEComponentOccurrence_INTERFACE_DEFINED__
#define __ISEComponentOccurrence_INTERFACE_DEFINED__

/* interface ISEComponentOccurrence */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEComponentOccurrence;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("AF17F176-93A6-11d3-A3E2-0004AC9695BB")
    ISEComponentOccurrence : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [string][out] */ OLECHAR **ppwszName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFileName( 
            /* [string][out] */ OLECHAR **ppwszFileName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRangeBox( 
            /* [out] */ double daMinPoint[ 3 ],
            /* [out] */ double daMaxPoint[ 3 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetComponentCount( 
            /* [in] */ SECOMPONENTCOUNTOPTION eOption,
            /* [out] */ long *nCount) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsAssembly( 
            /* [out] */ boolean *pIsAssembly) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMaterial( 
            /* [out] */ SEMATERIAL *pMaterial) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ThisAsOccurrence( 
            /* [out] */ LPUNKNOWN *ppThisAsOccurrence) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateElementProxyToBodyOverride( 
            /* [in] */ ULONG cbKeySize,
            /* [size_is][in] */ BYTE *pKey,
            /* [out] */ LPDMELEMENTPROXY *ppElemProxy) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFileMissing( 
            /* [out] */ boolean *bFileMissing) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEComponentOccurrenceVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEComponentOccurrence * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEComponentOccurrence * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEComponentOccurrence * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            ISEComponentOccurrence * This,
            /* [string][out] */ OLECHAR **ppwszName);
        
        HRESULT ( STDMETHODCALLTYPE *GetFileName )( 
            ISEComponentOccurrence * This,
            /* [string][out] */ OLECHAR **ppwszFileName);
        
        HRESULT ( STDMETHODCALLTYPE *GetRangeBox )( 
            ISEComponentOccurrence * This,
            /* [out] */ double daMinPoint[ 3 ],
            /* [out] */ double daMaxPoint[ 3 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetComponentCount )( 
            ISEComponentOccurrence * This,
            /* [in] */ SECOMPONENTCOUNTOPTION eOption,
            /* [out] */ long *nCount);
        
        HRESULT ( STDMETHODCALLTYPE *IsAssembly )( 
            ISEComponentOccurrence * This,
            /* [out] */ boolean *pIsAssembly);
        
        HRESULT ( STDMETHODCALLTYPE *GetMaterial )( 
            ISEComponentOccurrence * This,
            /* [out] */ SEMATERIAL *pMaterial);
        
        HRESULT ( STDMETHODCALLTYPE *ThisAsOccurrence )( 
            ISEComponentOccurrence * This,
            /* [out] */ LPUNKNOWN *ppThisAsOccurrence);
        
        HRESULT ( STDMETHODCALLTYPE *CreateElementProxyToBodyOverride )( 
            ISEComponentOccurrence * This,
            /* [in] */ ULONG cbKeySize,
            /* [size_is][in] */ BYTE *pKey,
            /* [out] */ LPDMELEMENTPROXY *ppElemProxy);
        
        HRESULT ( STDMETHODCALLTYPE *GetFileMissing )( 
            ISEComponentOccurrence * This,
            /* [out] */ boolean *bFileMissing);
        
        END_INTERFACE
    } ISEComponentOccurrenceVtbl;

    interface ISEComponentOccurrence
    {
        CONST_VTBL struct ISEComponentOccurrenceVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEComponentOccurrence_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEComponentOccurrence_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEComponentOccurrence_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEComponentOccurrence_GetName(This,ppwszName)	\
    ( (This)->lpVtbl -> GetName(This,ppwszName) ) 

#define ISEComponentOccurrence_GetFileName(This,ppwszFileName)	\
    ( (This)->lpVtbl -> GetFileName(This,ppwszFileName) ) 

#define ISEComponentOccurrence_GetRangeBox(This,daMinPoint,daMaxPoint)	\
    ( (This)->lpVtbl -> GetRangeBox(This,daMinPoint,daMaxPoint) ) 

#define ISEComponentOccurrence_GetComponentCount(This,eOption,nCount)	\
    ( (This)->lpVtbl -> GetComponentCount(This,eOption,nCount) ) 

#define ISEComponentOccurrence_IsAssembly(This,pIsAssembly)	\
    ( (This)->lpVtbl -> IsAssembly(This,pIsAssembly) ) 

#define ISEComponentOccurrence_GetMaterial(This,pMaterial)	\
    ( (This)->lpVtbl -> GetMaterial(This,pMaterial) ) 

#define ISEComponentOccurrence_ThisAsOccurrence(This,ppThisAsOccurrence)	\
    ( (This)->lpVtbl -> ThisAsOccurrence(This,ppThisAsOccurrence) ) 

#define ISEComponentOccurrence_CreateElementProxyToBodyOverride(This,cbKeySize,pKey,ppElemProxy)	\
    ( (This)->lpVtbl -> CreateElementProxyToBodyOverride(This,cbKeySize,pKey,ppElemProxy) ) 

#define ISEComponentOccurrence_GetFileMissing(This,bFileMissing)	\
    ( (This)->lpVtbl -> GetFileMissing(This,bFileMissing) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEComponentOccurrence_INTERFACE_DEFINED__ */


#ifndef __ISEComponentDefinition2_INTERFACE_DEFINED__
#define __ISEComponentDefinition2_INTERFACE_DEFINED__

/* interface ISEComponentDefinition2 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEComponentDefinition2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2B4DFA3A-3F2A-45f6-B540-A778D91DD82A")
    ISEComponentDefinition2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumAssemblyBodies( 
            /* [out] */ LPENUM_SEASSEMBLYBODIES *ppEnumAsmBodies) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEComponentDefinition2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEComponentDefinition2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEComponentDefinition2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEComponentDefinition2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAssemblyBodies )( 
            ISEComponentDefinition2 * This,
            /* [out] */ LPENUM_SEASSEMBLYBODIES *ppEnumAsmBodies);
        
        END_INTERFACE
    } ISEComponentDefinition2Vtbl;

    interface ISEComponentDefinition2
    {
        CONST_VTBL struct ISEComponentDefinition2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEComponentDefinition2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEComponentDefinition2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEComponentDefinition2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEComponentDefinition2_EnumAssemblyBodies(This,ppEnumAsmBodies)	\
    ( (This)->lpVtbl -> EnumAssemblyBodies(This,ppEnumAsmBodies) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEComponentDefinition2_INTERFACE_DEFINED__ */


#ifndef __ISEComponentOccurrence2_INTERFACE_DEFINED__
#define __ISEComponentOccurrence2_INTERFACE_DEFINED__

/* interface ISEComponentOccurrence2 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEComponentOccurrence2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("35D71DA6-9AE5-461a-895B-73B5ED57995E")
    ISEComponentOccurrence2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumSubAssemblyBodies( 
            /* [out] */ LPENUM_SEASSEMBLYBODIES *ppEnumAsmBodies) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEComponentOccurrence2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEComponentOccurrence2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEComponentOccurrence2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEComponentOccurrence2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumSubAssemblyBodies )( 
            ISEComponentOccurrence2 * This,
            /* [out] */ LPENUM_SEASSEMBLYBODIES *ppEnumAsmBodies);
        
        END_INTERFACE
    } ISEComponentOccurrence2Vtbl;

    interface ISEComponentOccurrence2
    {
        CONST_VTBL struct ISEComponentOccurrence2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEComponentOccurrence2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEComponentOccurrence2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEComponentOccurrence2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEComponentOccurrence2_EnumSubAssemblyBodies(This,ppEnumAsmBodies)	\
    ( (This)->lpVtbl -> EnumSubAssemblyBodies(This,ppEnumAsmBodies) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEComponentOccurrence2_INTERFACE_DEFINED__ */


#ifndef __ISEAssemblyBody_INTERFACE_DEFINED__
#define __ISEAssemblyBody_INTERFACE_DEFINED__

/* interface ISEAssemblyBody */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISEAssemblyBody;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("50D9AADF-7FF8-4f6e-9705-37D2DF16443F")
    ISEAssemblyBody : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetContextDefinition( 
            /* [out] */ LPDMCOMPONENTDEFINITION *ppCtxDef) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTransformation( 
            /* [out] */ double Matrix[ 16 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateElementProxy( 
            /* [in] */ ULONG cbKeySize,
            /* [size_is][in] */ BYTE *pKey,
            /* [out] */ LPDMELEMENTPROXY *ppElemProxy) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PutTransformation( 
            /* [in] */ double Matrix[ 16 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [string][out] */ OLECHAR **ppwszName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRangeBox( 
            /* [out] */ double daMinPoint[ 3 ],
            /* [out] */ double daMaxPoint[ 3 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMaterial( 
            /* [out] */ SEMATERIAL *pMaterial) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBody( 
            /* [out] */ LPDMSURFACEBODY *ppBody) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ThisAsAssemblyBody( 
            /* [out] */ LPUNKNOWN *ppThisAsAssemblyBody) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParentAssemblyBody( 
            /* [out] */ LPUNKNOWN *ppUnknown) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISEAssemblyBodyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISEAssemblyBody * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISEAssemblyBody * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISEAssemblyBody * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextDefinition )( 
            ISEAssemblyBody * This,
            /* [out] */ LPDMCOMPONENTDEFINITION *ppCtxDef);
        
        HRESULT ( STDMETHODCALLTYPE *GetTransformation )( 
            ISEAssemblyBody * This,
            /* [out] */ double Matrix[ 16 ]);
        
        HRESULT ( STDMETHODCALLTYPE *CreateElementProxy )( 
            ISEAssemblyBody * This,
            /* [in] */ ULONG cbKeySize,
            /* [size_is][in] */ BYTE *pKey,
            /* [out] */ LPDMELEMENTPROXY *ppElemProxy);
        
        HRESULT ( STDMETHODCALLTYPE *PutTransformation )( 
            ISEAssemblyBody * This,
            /* [in] */ double Matrix[ 16 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            ISEAssemblyBody * This,
            /* [string][out] */ OLECHAR **ppwszName);
        
        HRESULT ( STDMETHODCALLTYPE *GetRangeBox )( 
            ISEAssemblyBody * This,
            /* [out] */ double daMinPoint[ 3 ],
            /* [out] */ double daMaxPoint[ 3 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetMaterial )( 
            ISEAssemblyBody * This,
            /* [out] */ SEMATERIAL *pMaterial);
        
        HRESULT ( STDMETHODCALLTYPE *GetBody )( 
            ISEAssemblyBody * This,
            /* [out] */ LPDMSURFACEBODY *ppBody);
        
        HRESULT ( STDMETHODCALLTYPE *ThisAsAssemblyBody )( 
            ISEAssemblyBody * This,
            /* [out] */ LPUNKNOWN *ppThisAsAssemblyBody);
        
        HRESULT ( STDMETHODCALLTYPE *GetParentAssemblyBody )( 
            ISEAssemblyBody * This,
            /* [out] */ LPUNKNOWN *ppUnknown);
        
        END_INTERFACE
    } ISEAssemblyBodyVtbl;

    interface ISEAssemblyBody
    {
        CONST_VTBL struct ISEAssemblyBodyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISEAssemblyBody_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISEAssemblyBody_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISEAssemblyBody_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISEAssemblyBody_GetContextDefinition(This,ppCtxDef)	\
    ( (This)->lpVtbl -> GetContextDefinition(This,ppCtxDef) ) 

#define ISEAssemblyBody_GetTransformation(This,Matrix)	\
    ( (This)->lpVtbl -> GetTransformation(This,Matrix) ) 

#define ISEAssemblyBody_CreateElementProxy(This,cbKeySize,pKey,ppElemProxy)	\
    ( (This)->lpVtbl -> CreateElementProxy(This,cbKeySize,pKey,ppElemProxy) ) 

#define ISEAssemblyBody_PutTransformation(This,Matrix)	\
    ( (This)->lpVtbl -> PutTransformation(This,Matrix) ) 

#define ISEAssemblyBody_GetName(This,ppwszName)	\
    ( (This)->lpVtbl -> GetName(This,ppwszName) ) 

#define ISEAssemblyBody_GetRangeBox(This,daMinPoint,daMaxPoint)	\
    ( (This)->lpVtbl -> GetRangeBox(This,daMinPoint,daMaxPoint) ) 

#define ISEAssemblyBody_GetMaterial(This,pMaterial)	\
    ( (This)->lpVtbl -> GetMaterial(This,pMaterial) ) 

#define ISEAssemblyBody_GetBody(This,ppBody)	\
    ( (This)->lpVtbl -> GetBody(This,ppBody) ) 

#define ISEAssemblyBody_ThisAsAssemblyBody(This,ppThisAsAssemblyBody)	\
    ( (This)->lpVtbl -> ThisAsAssemblyBody(This,ppThisAsAssemblyBody) ) 

#define ISEAssemblyBody_GetParentAssemblyBody(This,ppUnknown)	\
    ( (This)->lpVtbl -> GetParentAssemblyBody(This,ppUnknown) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISEAssemblyBody_INTERFACE_DEFINED__ */


#ifndef __IEnumSEAssemblyBodies_INTERFACE_DEFINED__
#define __IEnumSEAssemblyBodies_INTERFACE_DEFINED__

/* interface IEnumSEAssemblyBodies */
/* [object][uuid] */ 


EXTERN_C const IID IID_IEnumSEAssemblyBodies;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("71624F92-A9A7-497b-9B6F-08AC6894C4E2")
    IEnumSEAssemblyBodies : public IUnknown
    {
    public:
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG cBodies,
            /* [out] */ ISEAssemblyBody **rgelt,
            /* [out] */ ULONG *pcFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SkipBodies( 
            /* [in] */ ULONG cBodies) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ResetEnum( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ LPENUM_SEASSEMBLYBODIES *ppEnum) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IEnumSEAssemblyBodiesVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumSEAssemblyBodies * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumSEAssemblyBodies * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumSEAssemblyBodies * This);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumSEAssemblyBodies * This,
            /* [in] */ ULONG cBodies,
            /* [out] */ ISEAssemblyBody **rgelt,
            /* [out] */ ULONG *pcFetched);
        
        HRESULT ( STDMETHODCALLTYPE *SkipBodies )( 
            IEnumSEAssemblyBodies * This,
            /* [in] */ ULONG cBodies);
        
        HRESULT ( STDMETHODCALLTYPE *ResetEnum )( 
            IEnumSEAssemblyBodies * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumSEAssemblyBodies * This,
            /* [out] */ LPENUM_SEASSEMBLYBODIES *ppEnum);
        
        END_INTERFACE
    } IEnumSEAssemblyBodiesVtbl;

    interface IEnumSEAssemblyBodies
    {
        CONST_VTBL struct IEnumSEAssemblyBodiesVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumSEAssemblyBodies_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IEnumSEAssemblyBodies_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IEnumSEAssemblyBodies_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IEnumSEAssemblyBodies_Next(This,cBodies,rgelt,pcFetched)	\
    ( (This)->lpVtbl -> Next(This,cBodies,rgelt,pcFetched) ) 

#define IEnumSEAssemblyBodies_SkipBodies(This,cBodies)	\
    ( (This)->lpVtbl -> SkipBodies(This,cBodies) ) 

#define IEnumSEAssemblyBodies_ResetEnum(This)	\
    ( (This)->lpVtbl -> ResetEnum(This) ) 

#define IEnumSEAssemblyBodies_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumSEAssemblyBodies_RemoteNext_Proxy( 
    IEnumSEAssemblyBodies * This,
    /* [in] */ ULONG cBodies,
    /* [length_is][size_is][out] */ ISEAssemblyBody **rgelt,
    /* [out] */ ULONG *pcFetched);


void __RPC_STUB IEnumSEAssemblyBodies_RemoteNext_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumSEAssemblyBodies_INTERFACE_DEFINED__ */


#ifndef __ISECurveVertex_INTERFACE_DEFINED__
#define __ISECurveVertex_INTERFACE_DEFINED__

/* interface ISECurveVertex */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISECurveVertex;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("235A8921-E798-4638-88DB-4BFC5BBB0FE8")
    ISECurveVertex : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetPoint( 
            /* [out] */ double point[ 3 ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetID( 
            /* [out] */ int *pnID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTag( 
            /* [out] */ int *pnTag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBody( 
            /* [out] */ LPDMCURVEBODY *pBody) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISECurveVertexVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISECurveVertex * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISECurveVertex * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISECurveVertex * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetPoint )( 
            ISECurveVertex * This,
            /* [out] */ double point[ 3 ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetID )( 
            ISECurveVertex * This,
            /* [out] */ int *pnID);
        
        HRESULT ( STDMETHODCALLTYPE *GetTag )( 
            ISECurveVertex * This,
            /* [out] */ int *pnTag);
        
        HRESULT ( STDMETHODCALLTYPE *GetBody )( 
            ISECurveVertex * This,
            /* [out] */ LPDMCURVEBODY *pBody);
        
        END_INTERFACE
    } ISECurveVertexVtbl;

    interface ISECurveVertex
    {
        CONST_VTBL struct ISECurveVertexVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISECurveVertex_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISECurveVertex_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISECurveVertex_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISECurveVertex_GetPoint(This,point)	\
    ( (This)->lpVtbl -> GetPoint(This,point) ) 

#define ISECurveVertex_GetID(This,pnID)	\
    ( (This)->lpVtbl -> GetID(This,pnID) ) 

#define ISECurveVertex_GetTag(This,pnTag)	\
    ( (This)->lpVtbl -> GetTag(This,pnTag) ) 

#define ISECurveVertex_GetBody(This,pBody)	\
    ( (This)->lpVtbl -> GetBody(This,pBody) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISECurveVertex_INTERFACE_DEFINED__ */


#ifndef __IEnumSECurveVertices_INTERFACE_DEFINED__
#define __IEnumSECurveVertices_INTERFACE_DEFINED__

/* interface IEnumSECurveVertices */
/* [object][uuid] */ 


EXTERN_C const IID IID_IEnumSECurveVertices;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F52F2B56-26DE-49C3-B44E-9B15F41D5F89")
    IEnumSECurveVertices : public IUnknown
    {
    public:
        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG cVertices,
            /* [out] */ LPSECURVEVERTEX *ppCurveVertex,
            /* [out] */ ULONG *pcFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG cVertices) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ LPENUM_SECURVEVERTICES *ppEnum) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IEnumSECurveVerticesVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumSECurveVertices * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumSECurveVertices * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumSECurveVertices * This);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumSECurveVertices * This,
            /* [in] */ ULONG cVertices,
            /* [out] */ LPSECURVEVERTEX *ppCurveVertex,
            /* [out] */ ULONG *pcFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumSECurveVertices * This,
            /* [in] */ ULONG cVertices);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumSECurveVertices * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumSECurveVertices * This,
            /* [out] */ LPENUM_SECURVEVERTICES *ppEnum);
        
        END_INTERFACE
    } IEnumSECurveVerticesVtbl;

    interface IEnumSECurveVertices
    {
        CONST_VTBL struct IEnumSECurveVerticesVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumSECurveVertices_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IEnumSECurveVertices_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IEnumSECurveVertices_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IEnumSECurveVertices_Next(This,cVertices,ppCurveVertex,pcFetched)	\
    ( (This)->lpVtbl -> Next(This,cVertices,ppCurveVertex,pcFetched) ) 

#define IEnumSECurveVertices_Skip(This,cVertices)	\
    ( (This)->lpVtbl -> Skip(This,cVertices) ) 

#define IEnumSECurveVertices_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define IEnumSECurveVertices_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumSECurveVertices_RemoteNext_Proxy( 
    IEnumSECurveVertices * This,
    /* [in] */ ULONG cVertices,
    /* [length_is][size_is][out] */ LPSECURVEVERTEX *ppCurveVertex,
    /* [out] */ ULONG *pcFetched);


void __RPC_STUB IEnumSECurveVertices_RemoteNext_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumSECurveVertices_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0033 */
/* [local] */ 

//////////////////SE-Custom interface on Curve Body: (ISECurveBody2)////////////////////////////////////////
// 1. EnumVertices: Returns an enum of curve vertices.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0033_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0033_v0_0_s_ifspec;

#ifndef __ISECurveBody2_INTERFACE_DEFINED__
#define __ISECurveBody2_INTERFACE_DEFINED__

/* interface ISECurveBody2 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISECurveBody2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("591A075D-38BE-4E18-86F6-55047A620066")
    ISECurveBody2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumCurveVertices( 
            /* [out] */ LPENUM_SECURVEVERTICES *ppEnumCurveVertices) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISECurveBody2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISECurveBody2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISECurveBody2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISECurveBody2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCurveVertices )( 
            ISECurveBody2 * This,
            /* [out] */ LPENUM_SECURVEVERTICES *ppEnumCurveVertices);
        
        END_INTERFACE
    } ISECurveBody2Vtbl;

    interface ISECurveBody2
    {
        CONST_VTBL struct ISECurveBody2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISECurveBody2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISECurveBody2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISECurveBody2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISECurveBody2_EnumCurveVertices(This,ppEnumCurveVertices)	\
    ( (This)->lpVtbl -> EnumCurveVertices(This,ppEnumCurveVertices) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISECurveBody2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_SEMarshal_0000_0034 */
/* [local] */ 

//////////////////SE-Custom interface on Curve Object: (ISECurve2)////////////////////////////////////////
// 1. EnumVertices: Returns an enum of curve vertices.
///////////////////////////////////////////////////////////////////////////////////////////////////////////


extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0034_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_SEMarshal_0000_0034_v0_0_s_ifspec;

#ifndef __ISECurve2_INTERFACE_DEFINED__
#define __ISECurve2_INTERFACE_DEFINED__

/* interface ISECurve2 */
/* [object][uuid] */ 


EXTERN_C const IID IID_ISECurve2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("88F45660-836F-47A0-8E2D-753E9709C671")
    ISECurve2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumCurveVertices( 
            /* [out] */ LPENUM_SECURVEVERTICES *ppEnumCurveVertices) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISECurve2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISECurve2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISECurve2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISECurve2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCurveVertices )( 
            ISECurve2 * This,
            /* [out] */ LPENUM_SECURVEVERTICES *ppEnumCurveVertices);
        
        END_INTERFACE
    } ISECurve2Vtbl;

    interface ISECurve2
    {
        CONST_VTBL struct ISECurve2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISECurve2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISECurve2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISECurve2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISECurve2_EnumCurveVertices(This,ppEnumCurveVertices)	\
    ( (This)->lpVtbl -> EnumCurveVertices(This,ppEnumCurveVertices) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISECurve2_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* [local] */ HRESULT STDMETHODCALLTYPE IEnumSEAssemblyBodies_Next_Proxy( 
    IEnumSEAssemblyBodies * This,
    /* [in] */ ULONG cBodies,
    /* [out] */ ISEAssemblyBody **rgelt,
    /* [out] */ ULONG *pcFetched);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumSEAssemblyBodies_Next_Stub( 
    IEnumSEAssemblyBodies * This,
    /* [in] */ ULONG cBodies,
    /* [length_is][size_is][out] */ ISEAssemblyBody **rgelt,
    /* [out] */ ULONG *pcFetched);

/* [local] */ HRESULT STDMETHODCALLTYPE IEnumSECurveVertices_Next_Proxy( 
    IEnumSECurveVertices * This,
    /* [in] */ ULONG cVertices,
    /* [out] */ LPSECURVEVERTEX *ppCurveVertex,
    /* [out] */ ULONG *pcFetched);


/* [call_as] */ HRESULT STDMETHODCALLTYPE IEnumSECurveVertices_Next_Stub( 
    IEnumSECurveVertices * This,
    /* [in] */ ULONG cVertices,
    /* [length_is][size_is][out] */ LPSECURVEVERTEX *ppCurveVertex,
    /* [out] */ ULONG *pcFetched);



/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


