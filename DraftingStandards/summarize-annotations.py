import xml.etree.ElementTree as E
from collections import Counter
r=E.parse('DraftingStandards/annotation-property-audit.xml').getroot()
for doc in r:
 s=doc.find("Sheet[@section='igWorkingSection']")
 print('\n'+doc.tag)
 for kind in ['CenterLine','CenterMark','Balloon','Leader','TextBox','CuttingPlane','SurfaceFinishSymbol','FeatureControlFrame']:
  arr=s.findall(kind)
  if not arr: continue
  print(kind,len(arr))
  for path in ['Property','Style/Property','Edit/Property']:
   names=sorted(set(p.get('name') for a in arr for p in a.findall(path)))
   if path!='Property': names=[n for n in names if n in ['Name','Height','Font','TextSize','Color','DrivenColor','DrivingColor','TerminatorSize','LineWidth','CenterLineType','CenterMarkSize','CenterMarkExtension','AutoCenterLine','AutoCenterProjectionLine','SmartDepthThreadFinite','TextWeight']]
   for n in names:
    if path=='Property' and n in ['Index','ZOrder','Key','Type','Name','IsAttributeSetPresent','Layer','PMIName','KeyPointCount','VertexCount']:continue
    v=Counter(a.find(path+"[@name='%s']"%n).get('value') for a in arr if a.find(path+"[@name='%s']"%n)is not None)
    print(' ',path,n,dict(v))
