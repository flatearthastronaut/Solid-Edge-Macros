#ifndef __IGL_H__
#define __IGL_H__

// System dependencies
#include <objbase.h>

// OpenGL dependencies
#include <gl\gl.h>
#include <gl\glu.h>

#ifdef  __cplusplus
	// Solid Edge wrapper to avoid certain compiler warnings ...
	#if defined(HeapAlloc)
		#include "ca_comdef.h"
	#else
		#include <comdef.h>
	#endif

	// Smart interface macros
	#ifndef DECLARE_SMART_INTERFACE
		#define DECLARE_SMART_INTERFACE(iface, iid) DECLARE_INTERFACE_IID(iface, iid); const IID IID_##iface = __uuidof(iface); _COM_SMARTPTR_TYPEDEF(iface, __uuidof(iface)); DECLARE_INTERFACE_IID(iface, iid)
		#define DECLARE_SMART_INTERFACE_(iface, baseiface, iid) DECLARE_SMART_INTERFACE( iface, iid ) : public baseiface
	#endif  DECLARE_SMART_INTERFACE
#else
	// Compilation error avoidance only
	// IGL Interface IIDs
	DEFINE_GUID(IID_IGL,			0x0002D280, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
	DEFINE_GUID(IID_IGLU,			0x0002D283, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
	DEFINE_GUID(IID_IWGL,			0x0002D282, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
	DEFINE_GUID(IID_IGLControl,		0x0002D281, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
	DEFINE_GUID(IID_IGLUControl,	0x0002D284, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
	DEFINE_GUID(IID_IGLExtension, 	0x66EEE92E, 0x5AB5, 0x11D1, 0xA2, 0x66, 0x08, 0x00, 0x36, 0x54, 0xE9, 0x02);
	DEFINE_GUID(IID_IViewGLObject,	0x0002D201, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

	// Smart interface compatibility
	#ifndef DECLARE_SMART_INTERFACE
		#define DECLARE_SMART_INTERFACE(iface, iid) DECLARE_INTERFACE(iface)
		#define DECLARE_SMART_INTERFACE_(iface, baseiface, iid) DECLARE_INTERFACE_( iface, baseiface )
	#endif  DECLARE_SMART_INTERFACE
#endif

// Forward declarations
typedef interface IGL *LPGL;
typedef interface IGLU *LPGLU;
typedef interface IWGL *LPWGL;
typedef interface IGLControl *LPGLCONTROL;
typedef interface IGLUControl *LPGLUCONTROL;
typedef interface IGLExtension *LPGLEXTENSION;
typedef interface IViewGLObject FAR * LPVIEWGLOBJECT;

// Define OLE4DM hints: can be ORed together
#ifndef IGL_HINT_XOR
#define IGL_HINT_XOR		0x01
#endif

#ifndef IGL_HINT_PLOT
#define IGL_HINT_PLOT		0x02
#endif

#ifndef IGL_HINT_SHADE
#define IGL_HINT_SHADE		0x04
#endif

#ifndef IGL_HINT_VHL
#define IGL_HINT_VHL		0x08
#endif

#ifndef IGL_HINT_NOSTYLES
#define IGL_HINT_NOSTYLES	0x10
#endif

// Custom IGL states
enum IGLstate
{
	IGL_INTERNAL				= 0x80000000,	// Common bit

	IGL_CULLING					= 0x80000001,	// Culling state
	IGL_SHARPEN_LEVEL			= 0x80000002,	// Current sharpen level (1-5, 2 is default) [ use with glGetIntegerv() ]

	IGL_PREALPHA_DEPTH_BUFFER	= 0x80000010,	// Enable generation of a pre-alpha depth buffer
	IGL_PREALPHA_DEPTH_TEST		= 0x80000011,	// Enable pre-alpha depth test
	IGL_POSTALPHA_DEPTH_TEST	= 0x80000012,	// Enable post-alpha depth test

	IGL_DIRECT_GRAPHICS_ACCESS	= 0xFFFFFFFF	// Used to enable/disable DGA mode
};

// IGL context structure handle
typedef struct IGLC_s *HIGLC;

// IGL extension enumerations
enum IGLExtensionItem
{
	IGLEXT_STYLE_EXT,		//@emem GStyle3D extension
	IGLEXT_SURFACE_EXT,		//@emem Surface extension
	IGLEXT_DRAWGEOMETRY_EXT,//@emem DrawGeometry extension
};

enum IGLExtensionState
{
	IGLEXT_STATE_STYLE,		//@emem Current style
	IGLEXT_STATE_SURFACE,	//@emem Current surface
};

// OLE4DM IGL Control Interface
#ifndef __IGLControl_INTERFACE_DEFINED__
#define __IGLControl_INTERFACE_DEFINED__
DECLARE_SMART_INTERFACE_(IGLControl, IUnknown, "0002D281-0000-0000-C000-000000000046")
{
  STDMETHOD(QueryInterface)(THIS_ REFIID riid, LPVOID FAR *ppvObj) PURE;
  STDMETHOD_(ULONG, AddRef)(THIS) PURE;
  STDMETHOD_(ULONG, Release)(THIS) PURE;
  STDMETHOD_(HRESULT, CreateContext)(THIS_ HIGLC *higlc) PURE;
  STDMETHOD_(HRESULT, DeleteContext)(THIS_ HIGLC higlc) PURE;
  STDMETHOD_(HRESULT, PushIntoContext)(THIS_ HIGLC higlc, HIGLC *hprev) PURE;
  STDMETHOD_(HRESULT, RestoreContext)(THIS_ HIGLC higlc) PURE;
  STDMETHOD_(HRESULT, GetContext)(THIS_ HIGLC *higlc) PURE;
  STDMETHOD_(HRESULT, SetContext)(THIS_ HIGLC higlc) PURE;
  STDMETHOD_(HRESULT, SetHints)(THIS_ DWORD hints) PURE;
  STDMETHOD_(HRESULT, GetHints)(THIS_ DWORD *hints) PURE;
};
#endif //__IGLControl_INTERFACE_DEFINED__

// OLE4DM IGLU Library Control Interface
#ifndef __IGLUControl_INTERFACE_DEFINED__
#define __IGLUControl_INTERFACE_DEFINED__
DECLARE_SMART_INTERFACE_(IGLUControl, IUnknown, "0002D284-0000-0000-C000-000000000046")
{
  STDMETHOD(QueryInterface)(THIS_ REFIID riid, LPVOID FAR *ppvObj) PURE;
  STDMETHOD_(ULONG, AddRef)(THIS) PURE;
  STDMETHOD_(ULONG, Release)(THIS) PURE;
  STDMETHOD_(HRESULT, RegisterCallbackInterface)(THIS_ LPUNKNOWN lpcbif) PURE;
  STDMETHOD_(HRESULT, SetStandardCallback)(THIS_ int fid, void *obj, GLenum which) PURE;
};
#endif //__IGLUControl_INTERFACE_DEFINED__

// IGL Graphics Library Interface
#ifndef __IGL_INTERFACE_DEFINED__
#define __IGL_INTERFACE_DEFINED__
DECLARE_SMART_INTERFACE_(IGL, IUnknown, "0002D280-0000-0000-C000-000000000046")
{
  STDMETHOD(QueryInterface)(THIS_ REFIID riid, LPVOID FAR *ppvObj) PURE;
  STDMETHOD_(ULONG, AddRef)(THIS) PURE;
  STDMETHOD_(ULONG, Release)(THIS) PURE;
  STDMETHOD_(void, glAccum)(THIS_ GLenum op, GLfloat value) PURE;
  STDMETHOD_(void, glAlphaFunc)(THIS_ GLenum func, GLclampf ref) PURE;
  STDMETHOD_(void, glBegin)(THIS_ GLenum mode) PURE;
  STDMETHOD_(void, glBitmap)(THIS_ GLsizei width, GLsizei height, GLfloat xorig, GLfloat yorig, GLfloat xmove, GLfloat ymove, const GLubyte *bitmap) PURE;
  STDMETHOD_(void, glBlendFunc)(THIS_ GLenum sfactor, GLenum dfactor) PURE;
  STDMETHOD_(void, glCallList)(THIS_ GLuint list) PURE;
  STDMETHOD_(void, glCallLists)(THIS_ GLsizei n, GLenum type, const GLvoid *lists) PURE;
  STDMETHOD_(void, glClear)(THIS_ GLbitfield mask) PURE;
  STDMETHOD_(void, glClearAccum)(THIS_ GLfloat red, GLfloat green, GLfloat blue, GLfloat alpha) PURE;
  STDMETHOD_(void, glClearColor)(THIS_ GLclampf red, GLclampf green, GLclampf blue, GLclampf alpha) PURE;
  STDMETHOD_(void, glClearDepth)(THIS_ GLclampd depth) PURE;
  STDMETHOD_(void, glClearIndex)(THIS_ GLfloat c) PURE;
  STDMETHOD_(void, glClearStencil)(THIS_ GLint s) PURE;
  STDMETHOD_(void, glClipPlane)(THIS_ GLenum plane, const GLdouble *equation) PURE;
  STDMETHOD_(void, glColor3b)(THIS_ GLbyte red, GLbyte green, GLbyte blue) PURE;
  STDMETHOD_(void, glColor3bv)(THIS_ const GLbyte *v) PURE;
  STDMETHOD_(void, glColor3d)(THIS_ GLdouble red, GLdouble green, GLdouble blue) PURE;
  STDMETHOD_(void, glColor3dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glColor3f)(THIS_ GLfloat red, GLfloat green, GLfloat blue) PURE;
  STDMETHOD_(void, glColor3fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glColor3i)(THIS_ GLint red, GLint green, GLint blue) PURE;
  STDMETHOD_(void, glColor3iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glColor3s)(THIS_ GLshort red, GLshort green, GLshort blue) PURE;
  STDMETHOD_(void, glColor3sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glColor3ub)(THIS_ GLubyte red, GLubyte green, GLubyte blue) PURE;
  STDMETHOD_(void, glColor3ubv)(THIS_ const GLubyte *v) PURE;
  STDMETHOD_(void, glColor3ui)(THIS_ GLuint red, GLuint green, GLuint blue) PURE;
  STDMETHOD_(void, glColor3uiv)(THIS_ const GLuint *v) PURE;
  STDMETHOD_(void, glColor3us)(THIS_ GLushort red, GLushort green, GLushort blue) PURE;
  STDMETHOD_(void, glColor3usv)(THIS_ const GLushort *v) PURE;
  STDMETHOD_(void, glColor4b)(THIS_ GLbyte red, GLbyte green, GLbyte blue, GLbyte alpha) PURE;
  STDMETHOD_(void, glColor4bv)(THIS_ const GLbyte *v) PURE;
  STDMETHOD_(void, glColor4d)(THIS_ GLdouble red, GLdouble green, GLdouble blue, GLdouble alpha) PURE;
  STDMETHOD_(void, glColor4dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glColor4f)(THIS_ GLfloat red, GLfloat green, GLfloat blue, GLfloat alpha) PURE;
  STDMETHOD_(void, glColor4fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glColor4i)(THIS_ GLint red, GLint green, GLint blue, GLint alpha) PURE;
  STDMETHOD_(void, glColor4iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glColor4s)(THIS_ GLshort red, GLshort green, GLshort blue, GLshort alpha) PURE;
  STDMETHOD_(void, glColor4sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glColor4ub)(THIS_ GLubyte red, GLubyte green, GLubyte blue, GLubyte alpha) PURE;
  STDMETHOD_(void, glColor4ubv)(THIS_ const GLubyte *v) PURE;
  STDMETHOD_(void, glColor4ui)(THIS_ GLuint red, GLuint green, GLuint blue, GLuint alpha) PURE;
  STDMETHOD_(void, glColor4uiv)(THIS_ const GLuint *v) PURE;
  STDMETHOD_(void, glColor4us)(THIS_ GLushort red, GLushort green, GLushort blue, GLushort alpha) PURE;
  STDMETHOD_(void, glColor4usv)(THIS_ const GLushort *v) PURE;
  STDMETHOD_(void, glColorMask)(THIS_ GLboolean red, GLboolean green, GLboolean blue, GLboolean alpha) PURE;
  STDMETHOD_(void, glColorMaterial)(THIS_ GLenum face, GLenum mode) PURE;
  STDMETHOD_(void, glCopyPixels)(THIS_ GLint x, GLint y, GLsizei width, GLsizei height, GLenum type) PURE;
  STDMETHOD_(void, glCullFace)(THIS_ GLenum mode) PURE;
  STDMETHOD_(void, glDeleteLists)(THIS_ GLuint list, GLsizei range) PURE;
  STDMETHOD_(void, glDepthFunc)(THIS_ GLenum func) PURE;
  STDMETHOD_(void, glDepthMask)(THIS_ GLboolean flag) PURE;
  STDMETHOD_(void, glDepthRange)(THIS_ GLclampd zNear, GLclampd zFar) PURE;
  STDMETHOD_(void, glDisable)(THIS_ GLenum cap) PURE;
  STDMETHOD_(void, glDrawBuffer)(THIS_ GLenum mode) PURE;
  STDMETHOD_(void, glDrawPixels)(THIS_ GLsizei width, GLsizei height, GLenum format, GLenum type, const GLvoid *pixels) PURE;
  STDMETHOD_(void, glEdgeFlag)(THIS_ GLboolean flag) PURE;
  STDMETHOD_(void, glEdgeFlagv)(THIS_ const GLboolean *flag) PURE;
  STDMETHOD_(void, glEnable)(THIS_ GLenum cap) PURE;
  STDMETHOD_(void, glEnd)(THIS) PURE;
  STDMETHOD_(void, glEndList)(THIS) PURE;
  STDMETHOD_(void, glEvalCoord1d)(THIS_ GLdouble u) PURE;
  STDMETHOD_(void, glEvalCoord1dv)(THIS_ const GLdouble *u) PURE;
  STDMETHOD_(void, glEvalCoord1f)(THIS_ GLfloat u) PURE;
  STDMETHOD_(void, glEvalCoord1fv)(THIS_ const GLfloat *u) PURE;
  STDMETHOD_(void, glEvalCoord2d)(THIS_ GLdouble u, GLdouble v) PURE;
  STDMETHOD_(void, glEvalCoord2dv)(THIS_ const GLdouble *u) PURE;
  STDMETHOD_(void, glEvalCoord2f)(THIS_ GLfloat u, GLfloat v) PURE;
  STDMETHOD_(void, glEvalCoord2fv)(THIS_ const GLfloat *u) PURE;
  STDMETHOD_(void, glEvalMesh1)(THIS_ GLenum mode, GLint i1, GLint i2) PURE;
  STDMETHOD_(void, glEvalMesh2)(THIS_ GLenum mode, GLint i1, GLint i2, GLint j1, GLint j2) PURE;
  STDMETHOD_(void, glEvalPoint1)(THIS_ GLint i) PURE;
  STDMETHOD_(void, glEvalPoint2)(THIS_ GLint i, GLint j) PURE;
  STDMETHOD_(void, glFeedbackBuffer)(THIS_ GLsizei size, GLenum type, GLfloat *buffer) PURE;
  STDMETHOD_(void, glFinish)(THIS) PURE;
  STDMETHOD_(void, glFlush)(THIS) PURE;
  STDMETHOD_(void, glFogf)(THIS_ GLenum pname, GLfloat param) PURE;
  STDMETHOD_(void, glFogfv)(THIS_ GLenum pname, const GLfloat *params) PURE;
  STDMETHOD_(void, glFogi)(THIS_ GLenum pname, GLint param) PURE;
  STDMETHOD_(void, glFogiv)(THIS_ GLenum pname, const GLint *params) PURE;
  STDMETHOD_(void, glFrontFace)(THIS_ GLenum mode) PURE;
  STDMETHOD_(void, glFrustum)(THIS_ GLdouble left, GLdouble right, GLdouble bottom, GLdouble top, GLdouble zNear, GLdouble zFar) PURE;
  STDMETHOD_(GLuint, glGenLists)(THIS_ GLsizei range) PURE;
  STDMETHOD_(void, glGetBooleanv)(THIS_ GLenum pname, GLboolean *params) PURE;
  STDMETHOD_(void, glGetClipPlane)(THIS_ GLenum plane, GLdouble *equation) PURE;
  STDMETHOD_(void, glGetDoublev)(THIS_ GLenum pname, GLdouble *params) PURE;
  STDMETHOD_(GLenum, glGetError)(THIS) PURE;
  STDMETHOD_(void, glGetFloatv)(THIS_ GLenum pname, GLfloat *params) PURE;
  STDMETHOD_(void, glGetIntegerv)(THIS_ GLenum pname, GLint *params) PURE;
  STDMETHOD_(void, glGetLightfv)(THIS_ GLenum light, GLenum pname, GLfloat *params) PURE;
  STDMETHOD_(void, glGetLightiv)(THIS_ GLenum light, GLenum pname, GLint *params) PURE;
  STDMETHOD_(void, glGetMapdv)(THIS_ GLenum target, GLenum query, GLdouble *v) PURE;
  STDMETHOD_(void, glGetMapfv)(THIS_ GLenum target, GLenum query, GLfloat *v) PURE;
  STDMETHOD_(void, glGetMapiv)(THIS_ GLenum target, GLenum query, GLint *v) PURE;
  STDMETHOD_(void, glGetMaterialfv)(THIS_ GLenum face, GLenum pname, GLfloat *params) PURE;
  STDMETHOD_(void, glGetMaterialiv)(THIS_ GLenum face, GLenum pname, GLint *params) PURE;
  STDMETHOD_(void, glGetPixelMapfv)(THIS_ GLenum map, GLfloat *values) PURE;
  STDMETHOD_(void, glGetPixelMapuiv)(THIS_ GLenum map, GLuint *values) PURE;
  STDMETHOD_(void, glGetPixelMapusv)(THIS_ GLenum map, GLushort *values) PURE;
  STDMETHOD_(void, glGetPolygonStipple)(THIS_ GLubyte *mask) PURE;
  STDMETHOD_(const GLubyte*, glGetString)(THIS_ GLenum name) PURE;
  STDMETHOD_(void, glGetTexEnvfv)(THIS_ GLenum target, GLenum pname, GLfloat *params) PURE;
  STDMETHOD_(void, glGetTexEnviv)(THIS_ GLenum target, GLenum pname, GLint *params) PURE;
  STDMETHOD_(void, glGetTexGendv)(THIS_ GLenum coord, GLenum pname, GLdouble *params) PURE;
  STDMETHOD_(void, glGetTexGenfv)(THIS_ GLenum coord, GLenum pname, GLfloat *params) PURE;
  STDMETHOD_(void, glGetTexGeniv)(THIS_ GLenum coord, GLenum pname, GLint *params) PURE;
  STDMETHOD_(void, glGetTexImage)(THIS_ GLenum target, GLint level, GLenum format, GLenum type, GLvoid *pixels) PURE;
  STDMETHOD_(void, glGetTexLevelParameterfv)(THIS_ GLenum target, GLint level, GLenum pname, GLfloat *params) PURE;
  STDMETHOD_(void, glGetTexLevelParameteriv)(THIS_ GLenum target, GLint level, GLenum pname, GLint *params) PURE;
  STDMETHOD_(void, glGetTexParameterfv)(THIS_ GLenum target, GLenum pname, GLfloat *params) PURE;
  STDMETHOD_(void, glGetTexParameteriv)(THIS_ GLenum target, GLenum pname, GLint *params) PURE;
  STDMETHOD_(void, glHint)(THIS_ GLenum target, GLenum mode) PURE;
  STDMETHOD_(void, glIndexMask)(THIS_ GLuint mask) PURE;
  STDMETHOD_(void, glIndexd)(THIS_ GLdouble c) PURE;
  STDMETHOD_(void, glIndexdv)(THIS_ const GLdouble *c) PURE;
  STDMETHOD_(void, glIndexf)(THIS_ GLfloat c) PURE;
  STDMETHOD_(void, glIndexfv)(THIS_ const GLfloat *c) PURE;
  STDMETHOD_(void, glIndexi)(THIS_ GLint c) PURE;
  STDMETHOD_(void, glIndexiv)(THIS_ const GLint *c) PURE;
  STDMETHOD_(void, glIndexs)(THIS_ GLshort c) PURE;
  STDMETHOD_(void, glIndexsv)(THIS_ const GLshort *c) PURE;
  STDMETHOD_(void, glInitNames)(THIS) PURE;
  STDMETHOD_(GLboolean, glIsEnabled)(THIS_ GLenum cap) PURE;
  STDMETHOD_(GLboolean, glIsList)(THIS_ GLuint list) PURE;
  STDMETHOD_(void, glLightModelf)(THIS_ GLenum pname, GLfloat param) PURE;
  STDMETHOD_(void, glLightModelfv)(THIS_ GLenum pname, const GLfloat *params) PURE;
  STDMETHOD_(void, glLightModeli)(THIS_ GLenum pname, GLint param) PURE;
  STDMETHOD_(void, glLightModeliv)(THIS_ GLenum pname, const GLint *params) PURE;
  STDMETHOD_(void, glLightf)(THIS_ GLenum light, GLenum pname, GLfloat param) PURE;
  STDMETHOD_(void, glLightfv)(THIS_ GLenum light, GLenum pname, const GLfloat *params) PURE;
  STDMETHOD_(void, glLighti)(THIS_ GLenum light, GLenum pname, GLint param) PURE;
  STDMETHOD_(void, glLightiv)(THIS_ GLenum light, GLenum pname, const GLint *params) PURE;
  STDMETHOD_(void, glLineStipple)(THIS_ GLint factor, GLushort pattern) PURE;
  STDMETHOD_(void, glLineWidth)(THIS_ GLfloat width) PURE;
  STDMETHOD_(void, glListBase)(THIS_ GLuint base) PURE;
  STDMETHOD_(void, glLoadIdentity)(THIS) PURE;
  STDMETHOD_(void, glLoadMatrixd)(THIS_ const GLdouble *m) PURE;
  STDMETHOD_(void, glLoadMatrixf)(THIS_ const GLfloat *m) PURE;
  STDMETHOD_(void, glLoadName)(THIS_ GLuint name) PURE;
  STDMETHOD_(void, glLogicOp)(THIS_ GLenum opcode) PURE;
  STDMETHOD_(void, glMap1d)(THIS_ GLenum target, GLdouble u1, GLdouble u2, GLint stride, GLint order, const GLdouble *points) PURE;
  STDMETHOD_(void, glMap1f)(THIS_ GLenum target, GLfloat u1, GLfloat u2, GLint stride, GLint order, const GLfloat *points) PURE;
  STDMETHOD_(void, glMap2d)(THIS_ GLenum target, GLdouble u1, GLdouble u2, GLint ustride, GLint uorder, GLdouble v1, GLdouble v2, GLint vstride, GLint vorder, const GLdouble *points) PURE;
  STDMETHOD_(void, glMap2f)(THIS_ GLenum target, GLfloat u1, GLfloat u2, GLint ustride, GLint uorder, GLfloat v1, GLfloat v2, GLint vstride, GLint vorder, const GLfloat *points) PURE;
  STDMETHOD_(void, glMapGrid1d)(THIS_ GLint un, GLdouble u1, GLdouble u2) PURE;
  STDMETHOD_(void, glMapGrid1f)(THIS_ GLint un, GLfloat u1, GLfloat u2) PURE;
  STDMETHOD_(void, glMapGrid2d)(THIS_ GLint un, GLdouble u1, GLdouble u2, GLint vn, GLdouble v1, GLdouble v2) PURE;
  STDMETHOD_(void, glMapGrid2f)(THIS_ GLint un, GLfloat u1, GLfloat u2, GLint vn, GLfloat v1, GLfloat v2) PURE;
  STDMETHOD_(void, glMaterialf)(THIS_ GLenum face, GLenum pname, GLfloat param) PURE;
  STDMETHOD_(void, glMaterialfv)(THIS_ GLenum face, GLenum pname, const GLfloat *params) PURE;
  STDMETHOD_(void, glMateriali)(THIS_ GLenum face, GLenum pname, GLint param) PURE;
  STDMETHOD_(void, glMaterialiv)(THIS_ GLenum face, GLenum pname, const GLint *params) PURE;
  STDMETHOD_(void, glMatrixMode)(THIS_ GLenum mode) PURE;
  STDMETHOD_(void, glMultMatrixd)(THIS_ const GLdouble *m) PURE;
  STDMETHOD_(void, glMultMatrixf)(THIS_ const GLfloat *m) PURE;
  STDMETHOD_(void, glNewList)(THIS_ GLuint list, GLenum mode) PURE;
  STDMETHOD_(void, glNormal3b)(THIS_ GLbyte nx, GLbyte ny, GLbyte nz) PURE;
  STDMETHOD_(void, glNormal3bv)(THIS_ const GLbyte *v) PURE;
  STDMETHOD_(void, glNormal3d)(THIS_ GLdouble nx, GLdouble ny, GLdouble nz) PURE;
  STDMETHOD_(void, glNormal3dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glNormal3f)(THIS_ GLfloat nx, GLfloat ny, GLfloat nz) PURE;
  STDMETHOD_(void, glNormal3fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glNormal3i)(THIS_ GLint nx, GLint ny, GLint nz) PURE;
  STDMETHOD_(void, glNormal3iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glNormal3s)(THIS_ GLshort nx, GLshort ny, GLshort nz) PURE;
  STDMETHOD_(void, glNormal3sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glOrtho)(THIS_ GLdouble left, GLdouble right, GLdouble bottom, GLdouble top, GLdouble zNear, GLdouble zFar) PURE;
  STDMETHOD_(void, glPassThrough)(THIS_ GLfloat token) PURE;
  STDMETHOD_(void, glPixelMapfv)(THIS_ GLenum map, GLint mapsize, const GLfloat *values) PURE;
  STDMETHOD_(void, glPixelMapuiv)(THIS_ GLenum map, GLint mapsize, const GLuint *values) PURE;
  STDMETHOD_(void, glPixelMapusv)(THIS_ GLenum map, GLint mapsize, const GLushort *values) PURE;
  STDMETHOD_(void, glPixelStoref)(THIS_ GLenum pname, GLfloat param) PURE;
  STDMETHOD_(void, glPixelStorei)(THIS_ GLenum pname, GLint param) PURE;
  STDMETHOD_(void, glPixelTransferf)(THIS_ GLenum pname, GLfloat param) PURE;
  STDMETHOD_(void, glPixelTransferi)(THIS_ GLenum pname, GLint param) PURE;
  STDMETHOD_(void, glPixelZoom)(THIS_ GLfloat xfactor, GLfloat yfactor) PURE;
  STDMETHOD_(void, glPointSize)(THIS_ GLfloat size) PURE;
  STDMETHOD_(void, glPolygonMode)(THIS_ GLenum face, GLenum mode) PURE;
  STDMETHOD_(void, glPolygonStipple)(THIS_ const GLubyte *mask) PURE;
  STDMETHOD_(void, glPopAttrib)(THIS) PURE;
  STDMETHOD_(void, glPopMatrix)(THIS) PURE;
  STDMETHOD_(void, glPopName)(THIS) PURE;
  STDMETHOD_(void, glPushAttrib)(THIS_ GLbitfield mask) PURE;
  STDMETHOD_(void, glPushMatrix)(THIS) PURE;
  STDMETHOD_(void, glPushName)(THIS_ GLuint name) PURE;
  STDMETHOD_(void, glRasterPos2d)(THIS_ GLdouble x, GLdouble y) PURE;
  STDMETHOD_(void, glRasterPos2dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glRasterPos2f)(THIS_ GLfloat x, GLfloat y) PURE;
  STDMETHOD_(void, glRasterPos2fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glRasterPos2i)(THIS_ GLint x, GLint y) PURE;
  STDMETHOD_(void, glRasterPos2iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glRasterPos2s)(THIS_ GLshort x, GLshort y) PURE;
  STDMETHOD_(void, glRasterPos2sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glRasterPos3d)(THIS_ GLdouble x, GLdouble y, GLdouble z) PURE;
  STDMETHOD_(void, glRasterPos3dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glRasterPos3f)(THIS_ GLfloat x, GLfloat y, GLfloat z) PURE;
  STDMETHOD_(void, glRasterPos3fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glRasterPos3i)(THIS_ GLint x, GLint y, GLint z) PURE;
  STDMETHOD_(void, glRasterPos3iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glRasterPos3s)(THIS_ GLshort x, GLshort y, GLshort z) PURE;
  STDMETHOD_(void, glRasterPos3sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glRasterPos4d)(THIS_ GLdouble x, GLdouble y, GLdouble z, GLdouble w) PURE;
  STDMETHOD_(void, glRasterPos4dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glRasterPos4f)(THIS_ GLfloat x, GLfloat y, GLfloat z, GLfloat w) PURE;
  STDMETHOD_(void, glRasterPos4fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glRasterPos4i)(THIS_ GLint x, GLint y, GLint z, GLint w) PURE;
  STDMETHOD_(void, glRasterPos4iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glRasterPos4s)(THIS_ GLshort x, GLshort y, GLshort z, GLshort w) PURE;
  STDMETHOD_(void, glRasterPos4sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glReadBuffer)(THIS_ GLenum mode) PURE;
  STDMETHOD_(void, glReadPixels)(THIS_ GLint x, GLint y, GLsizei width, GLsizei height, GLenum format, GLenum type, GLvoid *pixels) PURE;
  STDMETHOD_(void, glRectd)(THIS_ GLdouble x1, GLdouble y1, GLdouble x2, GLdouble y2) PURE;
  STDMETHOD_(void, glRectdv)(THIS_ const GLdouble *v1, const GLdouble *v2) PURE;
  STDMETHOD_(void, glRectf)(THIS_ GLfloat x1, GLfloat y1, GLfloat x2, GLfloat y2) PURE;
  STDMETHOD_(void, glRectfv)(THIS_ const GLfloat *v1, const GLfloat *v2) PURE;
  STDMETHOD_(void, glRecti)(THIS_ GLint x1, GLint y1, GLint x2, GLint y2) PURE;
  STDMETHOD_(void, glRectiv)(THIS_ const GLint *v1, const GLint *v2) PURE;
  STDMETHOD_(void, glRects)(THIS_ GLshort x1, GLshort y1, GLshort x2, GLshort y2) PURE;
  STDMETHOD_(void, glRectsv)(THIS_ const GLshort *v1, const GLshort *v2) PURE;
  STDMETHOD_(GLint, glRenderMode)(THIS_ GLenum mode) PURE;
  STDMETHOD_(void, glRotated)(THIS_ GLdouble angle, GLdouble x, GLdouble y, GLdouble z) PURE;
  STDMETHOD_(void, glRotatef)(THIS_ GLfloat angle, GLfloat x, GLfloat y, GLfloat z) PURE;
  STDMETHOD_(void, glScaled)(THIS_ GLdouble x, GLdouble y, GLdouble z) PURE;
  STDMETHOD_(void, glScalef)(THIS_ GLfloat x, GLfloat y, GLfloat z) PURE;
  STDMETHOD_(void, glScissor)(THIS_ GLint x, GLint y, GLsizei width, GLsizei height) PURE;
  STDMETHOD_(void, glSelectBuffer)(THIS_ GLsizei size, GLuint *buffer) PURE;
  STDMETHOD_(void, glShadeModel)(THIS_ GLenum mode) PURE;
  STDMETHOD_(void, glStencilFunc)(THIS_ GLenum func, GLint ref, GLuint mask) PURE;
  STDMETHOD_(void, glStencilMask)(THIS_ GLuint mask) PURE;
  STDMETHOD_(void, glStencilOp)(THIS_ GLenum fail, GLenum zfail, GLenum zpass) PURE;
  STDMETHOD_(void, glTexCoord1d)(THIS_ GLdouble s) PURE;
  STDMETHOD_(void, glTexCoord1dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glTexCoord1f)(THIS_ GLfloat s) PURE;
  STDMETHOD_(void, glTexCoord1fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glTexCoord1i)(THIS_ GLint s) PURE;
  STDMETHOD_(void, glTexCoord1iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glTexCoord1s)(THIS_ GLshort s) PURE;
  STDMETHOD_(void, glTexCoord1sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glTexCoord2d)(THIS_ GLdouble s, GLdouble t) PURE;
  STDMETHOD_(void, glTexCoord2dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glTexCoord2f)(THIS_ GLfloat s, GLfloat t) PURE;
  STDMETHOD_(void, glTexCoord2fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glTexCoord2i)(THIS_ GLint s, GLint t) PURE;
  STDMETHOD_(void, glTexCoord2iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glTexCoord2s)(THIS_ GLshort s, GLshort t) PURE;
  STDMETHOD_(void, glTexCoord2sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glTexCoord3d)(THIS_ GLdouble s, GLdouble t, GLdouble r) PURE;
  STDMETHOD_(void, glTexCoord3dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glTexCoord3f)(THIS_ GLfloat s, GLfloat t, GLfloat r) PURE;
  STDMETHOD_(void, glTexCoord3fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glTexCoord3i)(THIS_ GLint s, GLint t, GLint r) PURE;
  STDMETHOD_(void, glTexCoord3iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glTexCoord3s)(THIS_ GLshort s, GLshort t, GLshort r) PURE;
  STDMETHOD_(void, glTexCoord3sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glTexCoord4d)(THIS_ GLdouble s, GLdouble t, GLdouble r, GLdouble q) PURE;
  STDMETHOD_(void, glTexCoord4dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glTexCoord4f)(THIS_ GLfloat s, GLfloat t, GLfloat r, GLfloat q) PURE;
  STDMETHOD_(void, glTexCoord4fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glTexCoord4i)(THIS_ GLint s, GLint t, GLint r, GLint q) PURE;
  STDMETHOD_(void, glTexCoord4iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glTexCoord4s)(THIS_ GLshort s, GLshort t, GLshort r, GLshort q) PURE;
  STDMETHOD_(void, glTexCoord4sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glTexEnvf)(THIS_ GLenum target, GLenum pname, GLfloat param) PURE;
  STDMETHOD_(void, glTexEnvfv)(THIS_ GLenum target, GLenum pname, const GLfloat *params) PURE;
  STDMETHOD_(void, glTexEnvi)(THIS_ GLenum target, GLenum pname, GLint param) PURE;
  STDMETHOD_(void, glTexEnviv)(THIS_ GLenum target, GLenum pname, const GLint *params) PURE;
  STDMETHOD_(void, glTexGend)(THIS_ GLenum coord, GLenum pname, GLdouble param) PURE;
  STDMETHOD_(void, glTexGendv)(THIS_ GLenum coord, GLenum pname, const GLdouble *params) PURE;
  STDMETHOD_(void, glTexGenf)(THIS_ GLenum coord, GLenum pname, GLfloat param) PURE;
  STDMETHOD_(void, glTexGenfv)(THIS_ GLenum coord, GLenum pname, const GLfloat *params) PURE;
  STDMETHOD_(void, glTexGeni)(THIS_ GLenum coord, GLenum pname, GLint param) PURE;
  STDMETHOD_(void, glTexGeniv)(THIS_ GLenum coord, GLenum pname, const GLint *params) PURE;
  STDMETHOD_(void, glTexImage1D)(THIS_ GLenum target, GLint level, GLint components, GLsizei width, GLint border, GLenum format, GLenum type, const GLvoid *pixels) PURE;
  STDMETHOD_(void, glTexImage2D)(THIS_ GLenum target, GLint level, GLint components, GLsizei width, GLsizei height, GLint border, GLenum format, GLenum type, const GLvoid *pixels) PURE;
  STDMETHOD_(void, glTexParameterf)(THIS_ GLenum target, GLenum pname, GLfloat param) PURE;
  STDMETHOD_(void, glTexParameterfv)(THIS_ GLenum target, GLenum pname, const GLfloat *params) PURE;
  STDMETHOD_(void, glTexParameteri)(THIS_ GLenum target, GLenum pname, GLint param) PURE;
  STDMETHOD_(void, glTexParameteriv)(THIS_ GLenum target, GLenum pname, const GLint *params) PURE;
  STDMETHOD_(void, glTranslated)(THIS_ GLdouble x, GLdouble y, GLdouble z) PURE;
  STDMETHOD_(void, glTranslatef)(THIS_ GLfloat x, GLfloat y, GLfloat z) PURE;
  STDMETHOD_(void, glVertex2d)(THIS_ GLdouble x, GLdouble y) PURE;
  STDMETHOD_(void, glVertex2dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glVertex2f)(THIS_ GLfloat x, GLfloat y) PURE;
  STDMETHOD_(void, glVertex2fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glVertex2i)(THIS_ GLint x, GLint y) PURE;
  STDMETHOD_(void, glVertex2iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glVertex2s)(THIS_ GLshort x, GLshort y) PURE;
  STDMETHOD_(void, glVertex2sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glVertex3d)(THIS_ GLdouble x, GLdouble y, GLdouble z) PURE;
  STDMETHOD_(void, glVertex3dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glVertex3f)(THIS_ GLfloat x, GLfloat y, GLfloat z) PURE;
  STDMETHOD_(void, glVertex3fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glVertex3i)(THIS_ GLint x, GLint y, GLint z) PURE;
  STDMETHOD_(void, glVertex3iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glVertex3s)(THIS_ GLshort x, GLshort y, GLshort z) PURE;
  STDMETHOD_(void, glVertex3sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glVertex4d)(THIS_ GLdouble x, GLdouble y, GLdouble z, GLdouble w) PURE;
  STDMETHOD_(void, glVertex4dv)(THIS_ const GLdouble *v) PURE;
  STDMETHOD_(void, glVertex4f)(THIS_ GLfloat x, GLfloat y, GLfloat z, GLfloat w) PURE;
  STDMETHOD_(void, glVertex4fv)(THIS_ const GLfloat *v) PURE;
  STDMETHOD_(void, glVertex4i)(THIS_ GLint x, GLint y, GLint z, GLint w) PURE;
  STDMETHOD_(void, glVertex4iv)(THIS_ const GLint *v) PURE;
  STDMETHOD_(void, glVertex4s)(THIS_ GLshort x, GLshort y, GLshort z, GLshort w) PURE;
  STDMETHOD_(void, glVertex4sv)(THIS_ const GLshort *v) PURE;
  STDMETHOD_(void, glViewport)(THIS_ GLint x, GLint y, GLsizei width, GLsizei height) PURE;
  STDMETHOD_(void, glPolygonOffset)(THIS_ GLfloat factor, GLfloat units) PURE;
  STDMETHOD_(void, glBindTexture)(THIS_ GLenum target, GLuint texture) PURE;
};
#endif //__IGL_INTERFACE_DEFINED__

// IWGL Windows Graphics Layer Interface
#ifndef __IWGL_INTERFACE_DEFINED__
#define __IWGL_INTERFACE_DEFINED__
DECLARE_SMART_INTERFACE_(IWGL, IUnknown, "0002D282-0000-0000-C000-000000000046")
{
  STDMETHOD(QueryInterface)(THIS_ REFIID riid, LPVOID FAR *ppvObj) PURE;
  STDMETHOD_(ULONG, AddRef)(THIS) PURE;
  STDMETHOD_(ULONG, Release)(THIS) PURE;
  STDMETHOD_(HGLRC, wglCreateContext)(THIS_ HDC hdc) PURE;
  STDMETHOD_(BOOL, wglDeleteContext)(THIS_ HGLRC hglrc) PURE;
  STDMETHOD_(HGLRC, wglGetCurrentContext)(THIS) PURE;
  STDMETHOD_(HDC, wglGetCurrentDC)(THIS) PURE;
  STDMETHOD_(BOOL, wglMakeCurrent)(THIS_ HDC hdc, HGLRC hglrc) PURE;
  STDMETHOD_(BOOL, wglUseFontBitmapsA)(THIS_ HDC hdc, DWORD first, DWORD count, DWORD listbase) PURE;
  STDMETHOD_(BOOL, wglUseFontBitmapsW)(THIS_ HDC hdc, DWORD first, DWORD count, DWORD listbase) PURE;
  STDMETHOD_(BOOL, SwapBuffers)(THIS_ HDC hdc) PURE;
};
#endif //__IWGL_INTERFACE_DEFINED__

// IGLU Graphics Library Utility Interface
#ifndef __IGLU_INTERFACE_DEFINED__
#define __IGLU_INTERFACE_DEFINED__
DECLARE_SMART_INTERFACE_(IGLU, IUnknown, "0002D283-0000-0000-C000-000000000046")
{
  STDMETHOD(QueryInterface)(THIS_ REFIID riid, LPVOID FAR *ppvObj) PURE;
  STDMETHOD_(ULONG, AddRef)(THIS) PURE;
  STDMETHOD_(ULONG, Release)(THIS) PURE;
  STDMETHOD_(const GLubyte*, gluErrorString)(THIS_ GLenum errCode) PURE;
  STDMETHOD_(const wchar_t*, gluErrorUnicodeStringEXT)(THIS_ GLenum errCode) PURE;
  STDMETHOD_(void, gluOrtho2D)(THIS_ GLdouble left, GLdouble right, GLdouble bottom, GLdouble top) PURE;
  STDMETHOD_(void, gluPerspective)(THIS_ GLdouble fovy, GLdouble aspect, GLdouble zNear, GLdouble zFar) PURE;
  STDMETHOD_(void, gluPickMatrix)(THIS_ GLdouble x, GLdouble y, GLdouble width, GLdouble height, GLint viewport[4]) PURE;
  STDMETHOD_(void, gluLookAt)(THIS_ GLdouble eyex, GLdouble eyey, GLdouble eyez, GLdouble centerx, GLdouble centery, GLdouble centerz, GLdouble upx, GLdouble upy, GLdouble upz) PURE;
  STDMETHOD_(int, gluProject)(THIS_ GLdouble objx, GLdouble objy, GLdouble objz, const GLdouble modelMatrix[16], const GLdouble projMatrix[16], const GLint viewport[4], GLdouble *winx, GLdouble *winy, GLdouble *winz) PURE;
  STDMETHOD_(int, gluUnProject)(THIS_ GLdouble winx, GLdouble winy, GLdouble winz, const GLdouble modelMatrix[16], const GLdouble projMatrix[16], const GLint viewport[4], GLdouble *objx, GLdouble *objy, GLdouble *objz) PURE;
  STDMETHOD_(int, gluScaleImage)(THIS_ GLenum format, GLint widthin, GLint heightin, GLenum typein, const void *datain, GLint widthout, GLint heightout, GLenum typeout, void *dataout) PURE;
  STDMETHOD_(int, gluBuild1DMipmaps)(THIS_ GLenum target, GLint components, GLint width, GLenum format, GLenum type, const void *data) PURE;
  STDMETHOD_(int, gluBuild2DMipmaps)(THIS_ GLenum target, GLint components, GLint width, GLint height, GLenum format, GLenum type, const void *data) PURE;
  STDMETHOD_(GLUquadricObj*, gluNewQuadric)(THIS) PURE;
  STDMETHOD_(void, gluDeleteQuadric)(THIS_ GLUquadricObj *state) PURE;
  STDMETHOD_(void, gluQuadricNormals)(THIS_ GLUquadricObj *quadObject, GLenum normals) PURE;
  STDMETHOD_(void, gluQuadricTexture)(THIS_ GLUquadricObj *quadObject, GLboolean textureCoords) PURE;
  STDMETHOD_(void, gluQuadricOrientation)(THIS_ GLUquadricObj *quadObject, GLenum orientation) PURE;
  STDMETHOD_(void, gluQuadricDrawStyle)(THIS_ GLUquadricObj *quadObject, GLenum drawStyle) PURE;
  STDMETHOD_(void, gluCylinder)(THIS_ GLUquadricObj *qobj, GLdouble baseRadius, GLdouble topRadius, GLdouble height, GLint slices, GLint stacks) PURE;
  STDMETHOD_(void, gluDisk)(THIS_ GLUquadricObj *qobj, GLdouble innerRadius, GLdouble outerRadius, GLint slices, GLint loops) PURE;
  STDMETHOD_(void, gluPartialDisk)(THIS_ GLUquadricObj *qobj, GLdouble innerRadius, GLdouble outerRadius, GLint slices, GLint loops, GLdouble startAngle, GLdouble sweepAngle) PURE;
  STDMETHOD_(void, gluSphere)(THIS_ GLUquadricObj *qobj, GLdouble radius, GLint slices, GLint stacks) PURE;
  STDMETHOD_(void, gluQuadricCallback)(THIS_ GLUquadricObj *qobj, GLenum which, void (CALLBACK* fn)() ) PURE;
  STDMETHOD_(GLUtriangulatorObj*, gluNewTess)(THIS) PURE;
  STDMETHOD_(void, gluTessCallback)(THIS_ GLUtriangulatorObj *tobj, GLenum which, void (CALLBACK* fn)() ) PURE;
  STDMETHOD_(void, gluDeleteTess)(THIS_ GLUtriangulatorObj *tobj) PURE;
  STDMETHOD_(void, gluBeginPolygon)(THIS_ GLUtriangulatorObj *tobj) PURE;
  STDMETHOD_(void, gluEndPolygon)(THIS_ GLUtriangulatorObj *tobj) PURE;
  STDMETHOD_(void, gluNextContour)(THIS_ GLUtriangulatorObj *tobj, GLenum type) PURE;
  STDMETHOD_(void, gluTessVertex)(THIS_ GLUtriangulatorObj *tobj, GLdouble v[3], void *data) PURE;
  STDMETHOD_(GLUnurbsObj*, gluNewNurbsRenderer)(THIS) PURE;
  STDMETHOD_(void, gluDeleteNurbsRenderer)(THIS_ GLUnurbsObj *nobj) PURE;
  STDMETHOD_(void, gluBeginSurface)(THIS_ GLUnurbsObj *nobj) PURE;
  STDMETHOD_(void, gluBeginCurve)(THIS_ GLUnurbsObj *nobj) PURE;
  STDMETHOD_(void, gluEndCurve)(THIS_ GLUnurbsObj *nobj) PURE;
  STDMETHOD_(void, gluEndSurface)(THIS_ GLUnurbsObj *nobj) PURE;
  STDMETHOD_(void, gluBeginTrim)(THIS_ GLUnurbsObj *nobj) PURE;
  STDMETHOD_(void, gluEndTrim)(THIS_ GLUnurbsObj *nobj) PURE;
  STDMETHOD_(void, gluPwlCurve)(THIS_ GLUnurbsObj *nobj, GLint count, GLfloat *array, GLint stride, GLenum type) PURE;
  STDMETHOD_(void, gluNurbsCurve)(THIS_ GLUnurbsObj *nobj, GLint nknots, GLfloat *knot, GLint stride, GLfloat *ctlarray, GLint order, GLenum type) PURE;
  STDMETHOD_(void, gluNurbsSurface)(THIS_ GLUnurbsObj *nobj, GLint sknot_count, GLfloat *sknot, GLint tknot_count, GLfloat *tknot, GLint s_stride, GLint t_stride, GLfloat *ctlarray, GLint sorder, GLint torder, GLenum type) PURE;
  STDMETHOD_(void, gluLoadSamplingMatrices)(THIS_ GLUnurbsObj *nobj, const GLfloat modelMatrix[16], const GLfloat projMatrix[16], const GLint viewport[4]) PURE;
  STDMETHOD_(void, gluNurbsProperty)(THIS_ GLUnurbsObj *nobj, GLenum property, GLfloat value) PURE;
  STDMETHOD_(void, gluGetNurbsProperty)(THIS_ GLUnurbsObj *nobj, GLenum property, GLfloat *value) PURE;
  STDMETHOD_(void, gluNurbsCallback)(THIS_ GLUnurbsObj *nobj, GLenum which, void (CALLBACK* fn)() ) PURE;
};
#endif //__IGLU_INTERFACE_DEFINED__

// IGLExtension Private Extension Interface
#ifndef __IGLExtension_INTERFACE_DEFINED__
#define __IGLExtension_INTERFACE_DEFINED__
DECLARE_SMART_INTERFACE_(IGLExtension, IUnknown, "66EEE92E-5AB5-11D1-A266-08003654E902")
{
	STDMETHOD_(HRESULT, Get)(THIS_ enum IGLExtensionItem eItem, LPVOID *ppvValue) PURE; 
	STDMETHOD_(HRESULT, Set)(THIS_ enum IGLExtensionItem eItem, LPVOID pvValue) PURE;
	STDMETHOD_(HRESULT, Begin)(THIS_ enum IGLExtensionState eState, LPVOID pvValue) PURE;
	STDMETHOD_(HRESULT, End)(THIS_ enum IGLExtensionState eState) PURE;
	STDMETHOD_(HRESULT, DrawGeometry)(THIS_ DWORD dwType,LPVOID pvGraphic, LPVOID pStyle, LPVOID pRange) PURE;
};
#endif //__IGLExtension_INTERFACE_DEFINED__

// IGLExtension Private ViewGLObject Interface
#ifndef __IViewGLObject_INTERFACE_DEFINED__
#define __IViewGLObject_INTERFACE_DEFINED__
DECLARE_SMART_INTERFACE_(IViewGLObject , IUnknown, "0002D201-0000-0000-C000-000000000046")
{
  STDMETHOD (QueryInterface) (THIS_ REFIID riid, LPVOID FAR* ppvObj) PURE;
  STDMETHOD_(ULONG,AddRef) (THIS) PURE;
  STDMETHOD_(ULONG,Release) (THIS) PURE;
  STDMETHOD (Draw) (THIS_ DWORD dwRep, LPGL pIGL)  PURE;
};
#endif //__IViewGLObject_INTERFACE_DEFINED__

#endif

