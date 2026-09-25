This is an ATL based ActiveX Control project. It contains an object which
implements the ISolidEdgeAddIn interface which is the primary interface
Solid Edge uses when connecting to an add-in. The component is fully 
self-registering and a post-build step has been added to the project
settings that registers your ActiveX control.

Features included by this object:

You add-in has a command bar.

Microsoft's Component Category Manager interfaces are used to register
the control as implementing ISolidEdgeAddin and by Solid Edge to detect
when an add-in is so registered. During registration a registry value
is set to indicate that the control should be auto-loaded when Solid Edge 
starts up. See RegSeAddin.cpp for the registration details or to add
additional registry entries, or modify the wizard generated settings.

Other than registering what is normally needed for COM objects and
indicating that the control implements ISolidEdgeAddIn, there are no 
installation dependencies that Solid Edge imposes on an add-in.

///////////////////////////////////////////////////////////////////////
Build Notes:

// Note: The Wizard took his best shot at building a path to your
//       Solid Edge Sdk include file directory. If after adding
//       the above project settings, the project fails to build
//       due to missing include files, add the necessary include
//       paths to the c++ (preprocessor) project settings under
//       the addtional include file paths field.
//
// Note: Paths to the Solid Edge SDK include files and typelibs should
//       be defined. Imported typelibs should be resolved by your
//       system path.
