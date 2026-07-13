# SK_BNCH

_Source: `SK_BNCH.pdf` (5 pages)_


---

## Page 1

1. Scope 
This report presents the Day 2 baseline Chidamber & Kemerer (CK) metrics for the eight 
classes owned by the Desktop GUI and Client Shell work package. These classes 
collectively implement the non-VR user interface of iDaVIE: file/mask loading, rendering 
parameter panels, statistics display, source management, paint mode, histogram controls, 
video recording UI, and tab navigation. 
Classes Analysed 
Class 
File 
Role 
CanvassDesktop 
Assets/Scripts/UI/CanvassDesktop.cs 
Orchestrato
r (O) 
DesktopPaintController 
Assets/Scripts/UI/DesktopPaintContr
oller.cs 
Adapter (A) 
PaintMenuController 
Assets/Scripts/Menu/PaintMenuContro
ller.cs 
Orchestrato
r (O) 
VideoUiManager 
Assets/Scripts/VideoMaker/VideoUiMa
nager.cs 
Adapter (A) 
HistogramMenuControlle
r 
Assets/Scripts/Menu/HistogramMenuCo
ntroller.cs 
Adapter (A) 
HistogramHelper 
Assets/Scripts/Menu/HistogramHelper
.cs 
Domain 
helper (D) 
SourceRow 
Assets/Scripts/Menu/SourceRow.cs 
Domain 
helper (D) 
TabsManager 
Assets/Scripts/Menu/TabsManager.cs 
Domain 
helper (D) 
 
 
 
 
 


---

## Page 2

2. CK Metrics Summary 
Thresholds from the assignment specification (Section 7.1): 
●​ WMC ≤ 20 (domain) / ≤ 40 (orchestrator/adapter) 
●​ DIT ≤ 4 
●​ NOC ≤ 5 
●​ CBO ≤ 14 (domain) / ≤ 25 (orchestrator) 
●​ RFC ≤ 50 
●​ LCOM (Henderson-Sellers) ≤ 0.50 
Class 
Role 
WMC 
DIT 
NOC 
CBO 
RFC 
LCOM_HS 
CanvassDesktop 
O 
63 🔴 
1 ✅ 
0 ✅ 
47 🔴 
118 🔴 
0.955 🔴 
DesktopPaintController 
A 
57 🔴 
1 ✅ 
0 ✅ 
21 ✅ 
99 🔴 
0.940 🔴 
PaintMenuController 
O 
24 ✅ 
1 ✅ 
0 ✅ 
9 ✅ 
56 🔴 
0.919 🔴 
VideoUiManager 
A 
17 ✅ 
1 ✅ 
0 ✅ 
17 ✅ 
64 🔴 
0.863 🔴 
HistogramMenuController 
A 
13 ✅ 
1 ✅ 
0 ✅ 
12 ✅ 
36 ✅ 
0.812 🔴 
HistogramHelper 
D 
3 ✅ 
1 ✅ 
0 ✅ 
13 ⚠ 
23 ✅ 
0.667 🔴 
SourceRow 
D 
3 ✅ 
1 ✅ 
0 ✅ 
3 ✅ 
11 ✅ 
0.667 🔴 
TabsManager 
D 
3 ✅ 
1 ✅ 
0 ✅ 
4 ✅ 
7 ✅ 
0.467 ✅ 
🔴 = violation · ⚠ = at/near threshold · ✅ = within threshold 
 
3. Violation Analysis 
3.1 DIT / NOC — No violations 
All eight classes sit one level above MonoBehaviour (DIT = 1). None have subclasses 
(NOC = 0). Inheritance is not a concern in this subsystem. 
3.2 WMC — 2 violations 
CanvassDesktop (63) and DesktopPaintController (57) both exceed the 
orchestrator/adapter ceiling of 40. These are god classes that absorb file-loading, rendering 
control, statistics display, paint mode, source management, and VR/desktop bridging into a 
single MonoBehaviour. 


---

## Page 3

3.3 CBO — 1 critical violation 
CanvassDesktop (47) nearly doubles the orchestrator threshold of 25. It directly couples to: 
●​ 23 project classes (e.g. VolumeDataSetRenderer, FitsReader, DataAnalysis, 
FeatureMapping) 
●​ 13 Unity/TMPro UI types 
●​ 7 System library types (IntPtr, StringBuilder, Marshal, etc.) 
●​ 4 Valve.VR types (SteamVR, OpenVR) 
Any change to FitsReader, VolumeCommandController, DataAnalysis, 
FeatureMapping, or MenuBarBehaviour ripples directly into this class. 
HistogramHelper (13) is borderline against the domain threshold of 14, driven by its 
OxyPlot charting dependency. 
3.4 RFC — 5 violations 
Class 
RFC 
Threshold Over by 
CanvassDesktop 
~118 
50 
+68 
DesktopPaintController 
~99 
50 
+49 
VideoUiManager 
64 
50 
+14 
PaintMenuController 
56 
50 
+6 
High RFC correlates directly with high CBO and WMC — these classes call out to too many 
external types because they own too many responsibilities. 
3.5 LCOM — 7 of 8 classes violate (≤ 0.50) 
TabsManager (0.467) is the only clean class. 
Critical cases: 
●​ CanvassDesktop (0.955): 63 methods, 67 fields, but total field–method access 
count is only 189. Methods operate on small, largely disjoint subsets of the field set 
— the textbook signature of a class that should be split. Three fields 
(_restFrequency, inPaintMode, _tabsManager) are declared but never 
accessed by any method. 
●​ DesktopPaintController (0.940): 57 methods, 66 fields, access count 226. Same 
disjoint-access pattern. The axis field is touched by 20 methods while 
firstEnable, colormapHeight, and minZoom are dead. 
●​ PaintMenuController (0.919): Deceptively bad. _volumeInputController is 
accessed by 16 of 24 methods, but 5 fields (cropstatus, featureStatus, 


---

## Page 4

oldSaveText, paintMenu, savePopup) are declared and never touched — dead 
weight inflating LCOM. 
●​ VideoUiManager (0.863): _isPaused is declared but never accessed. 
Low-priority cases: 
●​ HistogramHelper (0.667) and SourceRow (0.667): Trivially small classes. Their 
LCOM violations are artefacts of empty Unity lifecycle stubs (Start/Update with no 
bodies) and publicly-set data fields that the class never reads internally. These inflate 
LCOM mathematically but do not indicate a design problem. 
 
4. Dead Code Inventory 
Class 
Dead Fields 
CanvassDesktop 
_restFrequency, inPaintMode, _tabsManager 
DesktopPaintController 
firstEnable, colormapHeight, minZoom 
PaintMenuController 
cropstatus, featureStatus, oldSaveText, 
paintMenu, savePopup 
VideoUiManager 
_isPaused 
HistogramMenuController 
editMinScale, editMaxScale 
12 dead fields across 5 classes. These should be removed in the refactoring proposal. 
 
5. Key Coupling Dependencies (CanvassDesktop) 
CanvassDesktop's 47-type coupling breaks down as: 
Category 
Coun
t 
Examples 
Project 
types 
23 
VolumeDataSetRenderer, VolumeInputController, 
FitsReader, DataAnalysis, FeatureSetManager, 
FeatureMapping, Config, ColorMapUtils 
Unity / 
TMPro 
13 
TMP_Dropdown, TMP_InputField, Toggle, Slider, Button, 
Coroutine, PlayerPrefs 


---

## Page 5

System 
7 
IntPtr, StringBuilder, FileInfo, Marshal, CultureInfo 
Third-party 
4 
StandaloneFileBrowser, ExtensionFilter, SteamVR, 
OpenVR 
 
6. Implications for Refactoring 
The two primary refactoring targets are CanvassDesktop and DesktopPaintController. 
Both are god classes with extreme WMC, RFC, and LCOM violations. 
The assignment specification (Section 6.6) directs the following splits: 
1.​ CanvassDesktop → MVVM decomposition: View (Unity 6 UI Toolkit), ViewModel 
(pure C#), Service Gateway (server communication). Menu structure, panel state, file 
dialogs, and configuration become separate, composable responsibilities. 
2.​ File tab: From direct native-plugin call to ViewModel command via service gateway 
(Worked Example 1). 
3.​ Debug tab: From inline GUI logic to Observer of a structured logging stream 
(Worked Example 2). 
The projected Day 13 metrics for these refactored classes should bring WMC, CBO, RFC, 
and LCOM within their respective thresholds. 
 
Report prepared by Sub-team 6 Quality Champion · Sprint 1 · iDaVIE Refactoring 
Assessment 2026 
 
