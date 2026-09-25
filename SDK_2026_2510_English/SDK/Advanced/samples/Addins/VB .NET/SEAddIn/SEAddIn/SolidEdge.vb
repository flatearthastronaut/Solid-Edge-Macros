Module SolidEdge

    ' Define constants for various edge environments commonly used by add-ins. More
    ' GUIDs exist in the edge\sdk\include\secatids.h file.
    Public Const CATID_SolidEdgeAddIn As String = "{26B1D2D1-2B03-11d2-B589-080036E8B802}"

    Public Const CATID_SEApplication As String = "{26618394-09D6-11d1-BA07-080036230602}"

    ' Primary document based environments.
    Public Const CATID_SEPart As String = "{26618396-09D6-11d1-BA07-080036230602}"
    Public Const CATID_SESyncPart As String = "{D9B0BB85-3A6C-4086-A0BB-88A1AAD57A58}"
    Public Const CATID_SEAssembly As String = "{26618395-09D6-11d1-BA07-080036230602}"
    Public Const CATID_SESyncAssembly As String = "{2C3C2A72-3A4A-471d-98B5-E3A8CFA4A2BF}"
    Public Const CATID_SESheetMetal As String = "{26618398-09D6-11D1-BA07-080036230602}"
    Public Const CATID_SESyncSheetMetal As String = "{9CBF2809-FF80-4dbc-98F2-B82DABF3530F}"
    Public Const CATID_SEDraft As String = "{08244193-B78D-11D2-9216-00C04F79BE98}"
    Public Const CATID_SEWeldment As String = "{7313526A-276F-11D4-B64E-00C04F79B2BF}"

    ' Environments accessible via a primary document environment

    ' Sketch is a catch-all for the legacy "profile, profile hole, profile pattern, layout" etc. "layout" was sketch in assembly.
    Public Const CATID_SESketch As String = "{0DDABC90-125E-4cfe-9CB7-DC97FB74CCF4}"
    Public Const CATID_FEAResultsPart As String = "{B5965D1C-8819-4902-8252-64841537A16C}"

    Public Const CATID_FEAResultsAssembly As String = "{986B2512-3AE9-4a57-8513-1D2A1E3520DD}"
    Public Const CATID_SEXpresRoute As String = "{1661432A-489C-4714-B1B2-61E85CFD0B71}"
    Public Const CATID_SEHarness As String = "{5337A0AB-23ED-4261-A238-00E2070406FC}"
    Public Const CATID_SEFrame As String = "{D84119E8-F844-4823-B3A0-D4F31793028A}"

    Public Const CATID_SE2DModel As String = "{F6031120-7D99-48a7-95FC-EEE8038D7996}"
    Public Const CATID_SEDrawingViewEdit As String = "{8DBC3B5F-02D6-4241-BE96-B12EAF83FAE6}"


    ' Use this if you want the add-in to load in every environment (including application/no document env)
    Public Const CATID_SEAll As String = "{C484ED57-DBB6-4a83-BEDB-C08600AF07BF}"

    ' Use this if you want the add-in to load in every environment that has a document (excludes application/no document env)
    Public Const CATID_SEAllDocumentEnvrionments = "{BAD41B8D-18FF-42c9-9611-8A00E6921AE8}"

    ' A few of the less often used are in the sdk\include\secatids.h file. Feel free to add your own.

End Module
