import xml.etree.ElementTree as E
from collections import Counter
r=E.parse('DraftingStandards/annotation-property-audit.xml').getroot().find('Reference');ws=r.find('Sheet');std=E.parse('DraftingStandards/dimension-presentation-standard.xml').getroot();root=E.Element('AnnotationStandard',source='detailing example 1.dft')
def cat(name,api,arr,direct=(),style=True):
 c=E.SubElement(root,'Category',name=name,api=api)
 for sub,names in [('Object',direct),('Style',[p.get('name') for p in std] if style else [])]:
  dest=E.SubElement(c,sub)
  for n in names:
   ps=[a.find(('Style/' if sub=='Style' else '')+"Property[@name='%s']"%n) for a in arr];ps=[p for p in ps if p is not None]
   if not ps: continue
   v=Counter(p.get('value') for p in ps).most_common(1)[0][0]
   E.SubElement(dest,'Property',name=n,value=v,type=ps[0].get('type'))
 return c
for kind in ['CenterLine','CenterMark','SurfaceFinishSymbol']:cat(kind,kind,ws.findall(kind),('TextScale',) if kind!='CenterLine' else ())
cat('Balloon','Balloon',ws.findall('Balloon'),('BalloonSize','BreakLineDistance','CalloutBorder','CalloutBorderHorizontalGap','CalloutBorderVerticalGap','CalloutTextAspectRatio','CalloutTextWidthMode','TextScale'))
cat('Leader','Leader',r.findall('.//Leader'),('BreakLineDistance','VerticalBreakLine'))
cat('FeatureControlFrame','FeatureControlFrame',[ws.findall('FeatureControlFrame')[0]],('TextScale','BreakLineDistance'))
cat('ToleranceNote','FeatureControlFrame',ws.findall('FeatureControlFrame')[1:],('TextScale','BreakLineDistance'))
cat('CuttingPlane','CuttingPlane',ws.findall('CuttingPlane'),('DisplayCaption','DisplayType','TextFont','TextFontStyle','TextHeight','TextRatio','TextColor','TerminatorType','TerminatorRatio','LineWidth','LineType','LineDisplayStyle','ThickLineLengthRatio','ThickLineArrowOffset'),False)
notes=r.find("Sheet[@name='Notes To Copy']").findall('TextBox')[:5]
textprops=('BorderType','BorderLineStyle','BorderLineWidth','BorderLineColor','BorderOffset','VerticalAlignment','Justification','Fill','Shadow','FillColor','ShadowOffset','FlowDirection','FlowOrientation','TextScale','HorizontalAlignment','TabStopWidth','LineSpacingType','LineSpacing','FillBackgroundColor')
c=cat('TextNote','TextBox',notes,textprops,False); edit=E.SubElement(c,'Edit')
for p in notes[0].find('Edit'):
 if p.get('name') in ['Name','Language','Characters','InsertIndex','TabCount','IsEmpty','NumberingStartValue']:continue
 E.SubElement(edit,'Property',name=p.get('name'),value=p.get('value'),type=p.get('type'))
for name in ['TextCaption','TextField']:
 d=E.fromstring(E.tostring(c));d.set('name',name);d.find("Edit/Property[@name='Bold']").set('value','False');d.find("Edit/Property[@name='Color']").set('value','8421376');root.append(d)
for i,b in enumerate(r.find("Sheet[@name='B-Size']").findall('Balloon'),1):cat('BackgroundBalloon'+str(i),'Balloon',[b],(),True)
cat('BackgroundSurfaceFinishSymbol','SurfaceFinishSymbol',r.find("Sheet[@name='B-Size']").findall('SurfaceFinishSymbol'))

# The fitting has inch-only units. Use the reference's inch callout recipe,
# rather than its dual-unit recipe, which would print the inch depth twice.
for name in ['SmartDepthHoleFinite','SmartDepthThreadFinite','SmartDepthSlotFinite']:
 source=ws.findall('Balloon')[0].find("Style/Property[@name='%s']"%name)
 target=root.find("Category[@name='Balloon']/Style/Property[@name='%s']"%name)
 target.set('value',source.get('value'))

root.find("Category[@name='TextField']/Edit/Property[@name='Font']").set('value', r.find("Sheet[@name='B-Size']").findall('Balloon')[14].find("Style/Property[@name='Font']").get('value'))
E.indent(root);E.ElementTree(root).write('DraftingStandards/annotation-presentation-standard.xml',encoding='utf-8',xml_declaration=True)
print(len(root),'annotation roles saved')
