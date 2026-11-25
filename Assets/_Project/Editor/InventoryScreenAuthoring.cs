using System;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Editor
{
    /// <summary>One-time authoring/migration. Runtime uses saved, editable prefabs.</summary>
    public static partial class InventoryScreenAuthoring
    {
        public const string Folder = "Assets/_Project/Systems/Containers/Prefabs";
        public const string ScreenPath = Folder + "/InventoryScreen.prefab";
        private static TMP_FontAsset font;
        private static Material textMaterial;
        private static readonly Color Text = new Color(.906f,.925f,.902f), Muted = new Color(.663f,.718f,.702f), Amber = new Color(.937f,.733f,.431f), Line = new Color(.278f,.325f,.310f);
        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rt = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent,false);
            rt.anchorMin = rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0,1);
            rt.anchoredPosition = new Vector2(x,-y); rt.sizeDelta = new Vector2(width,height); return rt;
        }
        private static void Fill(RectTransform rect)
        { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
        private static UnityEngine.UI.Image Image(RectTransform rect, Color color, bool raycast = false)
        { var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=color; image.raycastTarget=raycast; return image; }
        private static TextMeshProUGUI Label(string name, Transform parent, string value, float size, Color color, float x, float y, float width, float height)
        {
            var text=Rect(name,parent,x,y,width,height).gameObject.AddComponent<TextMeshProUGUI>();
            text.font=font; text.fontSharedMaterial=textMaterial; text.fontSize=size; text.text=value; text.color=color; text.richText=false; text.raycastTarget=false;
            text.textWrappingMode=TextWrappingModes.Normal; text.overflowMode=TextOverflowModes.Truncate;
            return text;
        }
        private static UnityEngine.UI.Button Button(string name, Transform parent, string title, float x, float y, float width, float height, bool primary=false)
        {
            var rect=Rect(name,parent,x,y,width,height);
            var image=Image(rect,primary?new Color(.827f,.667f,.424f):new Color(0,0,0,0),true);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=image; button.transition=UnityEngine.UI.Selectable.Transition.None;
            var colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1,.88f,.64f); colors.selectedColor=new Color(1,.88f,.64f); colors.disabledColor=new Color(.4f,.4f,.4f); button.colors=colors;
            var label=Label("Label",rect,title,18,primary?new Color(.086f,.125f,.102f):Text,12,6,width-24,height-6);
            return button;
        }
        private static void Set(UnityEngine.Object owner, string field, UnityEngine.Object value)
        { var so=new SerializedObject(owner); so.FindProperty(field).objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Number(UnityEngine.Object owner,string field,int value)
        { var so=new SerializedObject(owner); so.FindProperty(field).intValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Array(UnityEngine.Object owner,string field,UnityEngine.Object[] values)
        { var so=new SerializedObject(owner); var p=so.FindProperty(field); p.arraySize=values.Length; for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static ContainerSlotView Slot(bool hotbar)
        {
            var root=Rect(hotbar?"InventoryHotbarSlot":"InventoryCarriedSlot",null,0,0,144,hotbar?80:108);
            root.gameObject.AddComponent<CanvasGroup>();
            var surface=Image(root,new Color(.157f,.204f,.188f),true);
            var button=root.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=surface;
            var colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1.3f,1.25f,1.1f); colors.selectedColor=new Color(1.5f,1.35f,1.1f); colors.disabledColor=Color.white; button.colors=colors;
            var view=root.gameObject.AddComponent<ContainerSlotView>();
            Set(view,"surface",surface);
            var icon=Image(Rect("Icon",root,50,hotbar?17:8,44,44),Text); icon.preserveAspect=true; Set(view,"icon",icon);
            var count=Label("Quantity",root,"",16,Text,96,4,40,24); count.alignment=TextAlignmentOptions.TopRight; Set(view,"count",count);
            if(!hotbar) { var name=Label("ItemName",root,"",18,Text,6,58,132,46); name.alignment=TextAlignmentOptions.Top; Set(view,"itemName",name); }
            var selected=Rect("Selected",root,0,0,144,hotbar?80:108); Fill(selected);
            foreach(var edge in new[]{Rect("Top",selected,0,0,144,2),Rect("Bottom",selected,0,hotbar?78:106,144,2),Rect("Left",selected,0,0,2,hotbar?80:108),Rect("Right",selected,142,0,2,hotbar?80:108)}) Image(edge,Amber);
            Set(view,"selectionHighlight",selected.gameObject); selected.gameObject.SetActive(false);
            if(hotbar)
            {
                var decorator=root.gameObject.AddComponent<SOLITUDE.Hotbar.HotbarSlotDecorator>();
                Set(decorator,"numberLabel",Label("Number",root,"",16,Muted,6,4,40,24));
                var active=Rect("Equipped",root,0,77,144,3); Image(active,new Color(.663f,.78f,.667f)); Set(decorator,"activeHighlight",active.gameObject); active.gameObject.SetActive(false);
            }
            var path=Folder+(hotbar?"/InventoryHotbarSlot.prefab":"/InventoryCarriedSlot.prefab");
            var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,path); UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<ContainerSlotView>();
        }
        [MenuItem("SOLITUDE/UI/Author Inventory Screen")]
        public static void Author()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring.");
            if(AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath)!=null) return; // Preserve subsequent manual prefab edits.
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/Exo_2/Exo2-Regular SDF.asset");
            const string materials = "Assets/_Project/Systems/Containers/Materials";
            if(!AssetDatabase.IsValidFolder(materials))AssetDatabase.CreateFolder("Assets/_Project/Systems/Containers","Materials");
            textMaterial=AssetDatabase.LoadAssetAtPath<Material>(materials+"/InventoryText.mat");
            if(textMaterial==null) { textMaterial=new Material(font.material); textMaterial.SetColor("_FaceColor",Color.white); AssetDatabase.CreateAsset(textMaterial,materials+"/InventoryText.mat"); }
            var carriedSlot=Slot(false); var hotbarSlot=Slot(true);
            var root=Rect("InventoryScreen",null,0,0,1920,1080); Fill(root);
            try
            {
                var canvas=root.gameObject.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingLayerName="Inventory"; canvas.sortingOrder=30000;
                var scaler=root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
                root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                var shade=Rect("WorldDimmer",root,0,0,1920,1080); Fill(shade); Image(shade,new Color(.027f,.067f,.071f,.65f),true);
                var panel=Rect("Panel",root,0,0,1400,870); panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f); panel.anchoredPosition=new Vector2(0,9); Image(panel,new Color(.078f,.125f,.118f,.98f),true);
                var view=root.gameObject.AddComponent<ContainerView>(); var controller=root.gameObject.AddComponent<ContainerController>(); var modal=root.gameObject.AddComponent<ContainerModalView>(); var screen=root.gameObject.AddComponent<InventoryScreenView>();
                Set(controller,"view",view); Set(modal,"root",root.gameObject); Set(modal,"controller",controller); Number(modal,"type",1); Set(screen,"carriedView",view);
                Label("Title",panel,"INVENTORY",36,Text,36,30,320,48);
                var capacity=Label("Capacity",panel,"0 / 24",16,Muted,1120,40,100,24); capacity.alignment=TextAlignmentOptions.TopRight; Set(screen,"capacity",capacity);
                var close=Button("Back",panel,"ESC  Back",1236,30,128,44); Set(screen,"closeButton",close); Set(screen,"backLabel",close.GetComponentInChildren<TMP_Text>());
                Image(Rect("HeaderDivider",panel,36,88,1328,1),Line);
                string[] names={"All","Tools","Consumables","Miscellaneous"}; float[] xs={36,82,150,286}; float[] widths={36,60,128,164};
                var filters=new UnityEngine.UI.Button[4]; var underlines=new UnityEngine.UI.Image[4];
                for(int i=0;i<4;i++) { filters[i]=Button(names[i],panel,names[i],xs[i],114,widths[i],40); var text=filters[i].GetComponentInChildren<TMP_Text>(); text.rectTransform.offsetMin=new Vector2(0,text.rectTransform.offsetMin.y); text.rectTransform.sizeDelta=new Vector2(widths[i],34); underlines[i]=Image(Rect("SelectedTab",filters[i].transform,0,37,widths[i]-12,2),Amber); }
                Array(screen,"filters",filters); Array(screen,"filterUnderlines",underlines);
                var scroll=Rect("CarriedScroll",panel,36,167,896,450).gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); scroll.horizontal=false; scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=32;
                scroll.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                var viewport=Rect("Viewport",scroll.transform,0,0,896,450); Fill(viewport); Image(viewport,Color.white); var mask=viewport.gameObject.AddComponent<UnityEngine.UI.Mask>(); mask.showMaskGraphic=false;
                var grid=Rect("Slots",viewport,0,0,896,450); grid.anchorMin=new Vector2(0,1); grid.anchorMax=Vector2.one; grid.sizeDelta=new Vector2(0,450);
                var layout=grid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>(); layout.cellSize=new Vector2(144,108); layout.spacing=new Vector2(6,6); layout.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount=6;
                var fitter=grid.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>(); fitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                scroll.viewport=viewport; scroll.content=grid; Set(view,"slotViewPrefab",carriedSlot); Set(view,"slotsParent",grid); Set(view,"gridLayoutGroup",layout); Set(view,"scrollRect",scroll); Number(view,"columns",6);
                var vso=new SerializedObject(view); vso.FindProperty("maximumViewportHeight").floatValue=450; vso.ApplyModifiedPropertiesWithoutUndo();
                Set(screen,"context",Label("Context",panel,"",16,Amber,36,627,896,56));
                Image(Rect("InspectorDivider",panel,972,114,1,570),Line);
                var art=Image(Rect("ItemArt",panel,1144,120,80,80),Text); art.preserveAspect=true; Set(screen,"itemIcon",art);
                Set(screen,"itemTitle",Label("ItemTitle",panel,"Select an item",30,Text,1005,225,359,38));
                Set(screen,"metadata",Label("Metadata",panel,"",16,Muted,1005,263,359,28));
                Set(screen,"description",Label("Description",panel,"",18,Text,1005,294,359,100));
                var actions=Rect("Actions",panel,1005,403,359,240).gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); actions.childControlWidth=actions.childControlHeight=true; actions.childForceExpandHeight=false; actions.childForceExpandWidth=true;
                string[] fields={"equipButton","useButton","assignButton","moveButton","splitButton","dropButton"}; string[] titles={"Equip","Use","Assign to hotbar","Move","Split stack","Drop"};
                for(int i=0;i<fields.Length;i++) { var button=Button(titles[i],actions.transform,titles[i],0,0,359,40,i<2); var le=button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); le.preferredHeight=40; Set(screen,fields[i],button); }
                Image(Rect("HotbarDivider",panel,36,691,1328,1),Line);
                Label("HotbarTitle",panel,"HOTBAR",16,Text,36,708,200,24);
                var equipped=Label("Equipped",panel,"",16,new Color(.663f,.78f,.667f),736,708,628,24); equipped.alignment=TextAlignmentOptions.TopRight; Set(screen,"equipped",equipped);
                var hotgrid=Rect("HotbarSlots",panel,36,736,1328,80); var hl=hotgrid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>(); hl.cellSize=new Vector2(144,80); hl.spacing=new Vector2(4,0); hl.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; hl.constraintCount=9;
                var hv=hotgrid.gameObject.AddComponent<ContainerView>(); Set(hv,"slotViewPrefab",hotbarSlot); Set(hv,"slotsParent",hotgrid); Set(hv,"gridLayoutGroup",hl); Number(hv,"columns",9); Set(screen,"hotbarView",hv);
                Label("NavigationHints",panel,"Arrows / D-pad   Navigate       Enter / A   Select       Esc / B   Back",16,Muted,36,831,1328,28);
                root.gameObject.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root.gameObject,ScreenPath); AssetDatabase.SaveAssets();
                ApplyBolderPrefabStyle();
            }
            finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
        }
        private static RectTransform Place(Transform parent, string name, float x, float y, float width, float height)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing == null) return Rect(name,parent,x,y,width,height);
            existing.anchorMin = existing.anchorMax = existing.pivot = new Vector2(0,1);
            existing.anchoredPosition = new Vector2(x,-y); existing.sizeDelta = new Vector2(width,height); return existing;
        }
        private static UnityEngine.UI.Image Paint(RectTransform rect, Color color)
        {
            var image = rect.GetComponent<UnityEngine.UI.Image>();
            if(image == null) image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        private static GameObject Corners(Transform parent, string name, float width, float height, Color color)
        {
            var group=Place(parent,name,0,0,width,height); Fill(group);
            foreach(var edge in new[]{
                Place(group,"TLH",0,0,14,3),Place(group,"TLV",0,0,3,14),
                Place(group,"TRH",width-14,0,14,3),Place(group,"TRV",width-3,0,3,14),
                Place(group,"BLH",0,height-3,14,3),Place(group,"BLV",0,height-14,3,14),
                Place(group,"BRH",width-14,height-3,14,3),Place(group,"BRV",width-3,height-14,3,14)}) Paint(edge,color);
            group.gameObject.SetActive(false); return group.gameObject;
        }
        private static void SlotStyle(string path, bool hotbar)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                float height=hotbar?80:108;
                var icon=Place(root.transform,"Icon",hotbar?46:44,hotbar?14:5,hotbar?52:56,hotbar?52:56);
                var label=root.transform.Find("ItemName")?.GetComponent<TMP_Text>();
                if(label!=null) { Place(root.transform,"ItemName",6,54,132,52); label.fontSize=18; label.fontStyle=FontStyles.Bold; }
                var count=root.transform.Find("Quantity").GetComponent<TMP_Text>();count.fontSize=18;count.fontStyle=FontStyles.Bold;
                var border=Place(root.transform,"RestBorder",0,0,144,height);Fill(border);border.SetAsFirstSibling();
                foreach(var edge in new[]{Place(border,"Top",0,0,144,1),Place(border,"Bottom",0,height-1,144,1),Place(border,"Left",0,0,1,height),Place(border,"Right",143,0,1,height)})Paint(edge,Line);
                var selected=root.transform.Find("Selected");
                foreach(var edge in new[]{Place(selected,"Top",0,0,144,1),Place(selected,"Bottom",0,height-1,144,1),Place(selected,"Left",0,0,1,height),Place(selected,"Right",143,0,1,height)})Paint(edge,Amber);
                Set(root.GetComponent<ContainerSlotView>(),"navigationFocus",Corners(root.transform,"FocusCorners",144,height,Amber));
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void Hint(Transform parent,string name,string key,string action,float x,float width)
        {
            var cap=Place(parent,name,x,0,width,28);Paint(cap,new Color(.157f,.204f,.188f));
            var glyph=cap.Find("Glyph")?.GetComponent<TMP_Text>();
            if(glyph==null)glyph=Label("Glyph",cap,key,16,Text,0,2,width,24);
            glyph.text=key;glyph.alignment=TextAlignmentOptions.Center;glyph.fontStyle=FontStyles.Bold;
            var caption=parent.Find(name+"Action")?.GetComponent<TMP_Text>();
            if(caption==null)caption=Label(name+"Action",parent,action,16,Muted,x+width+10,3,100,26);
            caption.text=action;
        }
        private static void ApplyBolderPrefabStyle()
        {
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/Exo_2/Exo2-Regular SDF.asset");
            textMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Systems/Containers/Materials/InventoryText.mat");
            SlotStyle(Folder+"/InventoryCarriedSlot.prefab",false); SlotStyle(Folder+"/InventoryHotbarSlot.prefab",true);
            var root=PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var panel=root.transform.Find("Panel");
                Paint(panel as RectTransform,new Color(.078f,.125f,.118f,1));
                var title=panel.Find("Title").GetComponent<TMP_Text>();title.fontSize=44;title.fontStyle=FontStyles.Bold;
                Place(panel,"Title",36,23,500,58);
                var capacity=panel.Find("Capacity").GetComponent<TMP_Text>();capacity.fontSize=20;capacity.fontStyle=FontStyles.Bold;Place(panel,"Capacity",1094,39,126,30);
                Paint(Place(panel,"HeaderDivider",36,88,1328,2),Amber);
                string[] names={"All","Tools","Consumables","Miscellaneous"};float[] x={36,126,224,396};float[] widths={84,92,166,196};
                for(int i=0;i<names.Length;i++)
                {
                    var tab=Place(panel,names[i],x[i],114,widths[i],40);var text=tab.Find("Label").GetComponent<TMP_Text>();
                    Place(tab,"Label",12,6,widths[i]-24,28);text.fontSize=18;text.fontStyle=FontStyles.Bold;
                    Paint(Place(tab,"SelectedTab",12,37,widths[i]-24,3),Amber);Corners(tab,"FocusCorners",widths[i],40,Text);
                }
                Paint(Place(panel,"ArtField",1005,114,359,144),new Color(.067f,.11f,.10f));
                panel.Find("ArtField").SetSiblingIndex(panel.Find("ItemArt").GetSiblingIndex());
                panel.Find("ItemArt").SetSiblingIndex(panel.Find("ArtField").GetSiblingIndex()+1);
                Set(root.GetComponent<InventoryScreenView>(),"itemArtField",panel.Find("ArtField").gameObject);
                Place(panel,"ItemArt",1116,118,136,136);
                var itemTitle=panel.Find("ItemTitle").GetComponent<TMP_Text>();itemTitle.fontSize=32;itemTitle.fontStyle=FontStyles.Bold;Place(panel,"ItemTitle",1005,264,359,44);
                Place(panel,"Metadata",1005,312,359,26);
                var description=panel.Find("Description").GetComponent<TMP_Text>();description.lineSpacing=25;Place(panel,"Description",1005,348,359,86);
                var actions=panel.Find("Actions");Place(panel,"Actions",1005,448,359,236);actions.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing=4;
                foreach(var button in actions.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    button.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight=44;
                    var label=button.transform.Find("Label").GetComponent<TMP_Text>();label.fontSize=18;label.fontStyle=FontStyles.Bold;Place(button.transform,"Label",12,9,335,30);
                    Corners(button.transform,"FocusCorners",359,44,Text);
                }
                var back=panel.Find("Back");Corners(back,"FocusCorners",128,44,Text);
                var backText=back.Find("Label").GetComponent<TMP_Text>();backText.text="Back";Place(back,"Label",64,9,64,28);
                var cap=Place(back,"Keycap",0,7,54,28);Paint(cap,new Color(.157f,.204f,.188f));
                var key=cap.Find("Glyph")?.GetComponent<TMP_Text>();if(key==null)key=Label("Glyph",cap,"ESC",16,Text,0,2,54,24);key.alignment=TextAlignmentOptions.Center;key.fontStyle=FontStyles.Bold;
                Set(root.GetComponent<InventoryScreenView>(),"backLabel",key);
                panel.Find("HotbarTitle").GetComponent<TMP_Text>().fontStyle=FontStyles.Bold;
                panel.Find("NavigationHints").gameObject.SetActive(false);
                var hints=Place(panel,"ControlHints",36,828,1328,30);
                Hint(hints,"Navigate","ARROWS / D-PAD","Navigate",0,166);
                Hint(hints,"Select","ENTER / A","Select",304,106);
                Hint(hints,"Back","ESC / B","Back",534,92);
                PrefabUtility.SaveAsPrefabAsset(root,ScreenPath);AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            ApplyDistilledPrefabStyle();
        }
        private static void ApplyDistilledPrefabStyle()
        {
            var root=PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var panel=root.transform.Find("Panel");
                panel.Find("Paused")?.gameObject.SetActive(false);
                panel.Find("ControlHints/Back")?.gameObject.SetActive(false);
                panel.Find("ControlHints/BackAction")?.gameObject.SetActive(false);
                Place(panel,"Capacity",1014,39,126,30);
                var back=Place(panel,"Back",1176,30,188,44);
                Corners(back,"FocusCorners",188,44,Text);
                Place(back,"Label",120,9,68,28);
                Place(back,"Keycap",0,7,104,28);
                Place(back.Find("Keycap"),"Glyph",0,2,104,24).GetComponent<TMP_Text>().text="ESC / B";
                foreach(var name in new[]{"Use","Split stack","Drop"})panel.Find("Actions/"+name)?.gameObject.SetActive(false);
                panel.Find("Context").GetComponent<TMP_Text>().text=string.Empty;
                panel.Find("Metadata").GetComponent<TMP_Text>().text=string.Empty;
                Place(panel,"Description",1005,348,359,86);
                PrefabUtility.SaveAsPrefabAsset(root,ScreenPath);AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [MenuItem("SOLITUDE/UI/Distill Inventory Presentation")]
        public static void DistillInventory()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before authoring.");
            Author();ApplyDistilledPrefabStyle();
        }
        [MenuItem("SOLITUDE/UI/Amplify Inventory Presentation")]
        public static void AmplifyInventory()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before authoring.");
            Author();ApplyBolderPrefabStyle();
        }

        [MenuItem("SOLITUDE/UI/Inventory Selection Popover")]
        public static void SelectionPopover()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before authoring.");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/Exo_2/Exo2-Regular SDF.asset");
            textMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Systems/Containers/Materials/InventoryText.mat");
            var root=PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var panel=(RectTransform)root.transform.Find("Panel");panel.sizeDelta=new Vector2(718,680);panel.anchoredPosition=new Vector2(-200,36);
                Paint(panel,new Color(.072f,.112f,.118f));
                Place(panel,"Title",32,20,340,54);
                Place(panel,"Capacity",374,33,108,30);
                Place(panel,"Back",498,24,188,44);
                Place(panel,"HeaderDivider",32,84,654,2);
                Paint(Place(panel,"SurfaceLight",0,0,718,1),new Color(.31f,.38f,.37f));
                foreach(var name in new[]{"InspectorDivider","CommandDivider","ControlHints"})panel.Find(name)?.gameObject.SetActive(false);
                Place(panel,"Context",32,610,654,44);
                var popover=Place(root.transform,"ItemPopover",1100,320,400,428);
                var face=Paint(popover,Color.clear);face.raycastTarget=true;
                var shadow=Place(popover,"SoftShadow",0,10,400,428);shadow.SetAsFirstSibling();
                var graphic=shadow.GetComponent<InventorySoftShadow>()??shadow.gameObject.AddComponent<InventorySoftShadow>();graphic.raycastTarget=false;
                var fill=Paint(Place(popover,"Face",0,0,400,428),new Color(.16f,.215f,.207f));fill.transform.SetSiblingIndex(1);Fill((RectTransform)fill.transform);
                Fill(shadow);shadow.offsetMin=new Vector2(0,-10);shadow.offsetMax=new Vector2(0,-10);
                Paint(Place(popover,"SurfaceLight",0,0,400,1),new Color(.39f,.46f,.42f));
                foreach(var name in new[]{"ItemArt","ArtField","ItemTitle","Metadata","Description","Actions"})
                {
                    var child=panel.Find(name);if(child!=null)child.SetParent(popover,false);
                }
                Place(popover,"ArtField",240,-24,160,160).GetComponent<UnityEngine.UI.Image>().color=Color.clear;
                Place(popover,"ItemArt",240,-24,160,160).GetComponent<UnityEngine.UI.Image>().color=Color.white;
                var title=Place(popover,"ItemTitle",24,28,224,80).GetComponent<TMP_Text>();title.fontSize=28;
                Place(popover,"Metadata",24,120,352,26);
                Place(popover,"Description",24,164,352,82);
                Paint(Place(popover,"ActionDivider",24,250,352,1),new Color(.30f,.36f,.33f));
                var actions=Place(popover,"Actions",24,268,352,140);
                UnityEngine.Object.DestroyImmediate(actions.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>());
                var stack=actions.GetComponent<UnityEngine.UI.VerticalLayoutGroup>()??actions.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                stack.spacing=4;stack.childControlWidth=stack.childControlHeight=true;stack.childForceExpandWidth=true;stack.childForceExpandHeight=false;
                foreach(var button in actions.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {button.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth=352;Place(button.transform,"Label",54,9,286,28);Corners(button.transform,"FocusCorners",352,44,Text);}
                Set(root.GetComponent<InventoryScreenView>(),"itemPopover",popover);
                PrefabUtility.SaveAsPrefabAsset(root,ScreenPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            var slot=PrefabUtility.LoadPrefabContents(Folder+"/InventoryCarriedSlot.prefab");
            try
            {
                Place(slot.transform,"Icon",4,4,96,96).GetComponent<UnityEngine.UI.Image>().color=Color.white;
                var border=slot.transform.Find("RestBorder");foreach(var edge in border.GetComponentsInChildren<UnityEngine.UI.Image>())edge.color=new Color(.16f,.22f,.22f,.55f);
                Paint(Place(slot.transform,"RecessTop",1,1,102,2),new Color(.018f,.039f,.045f));
                Paint(Place(slot.transform,"RecessFoot",1,102,102,1),new Color(.26f,.33f,.31f,.42f));
                slot.transform.Find("Selected").SetAsLastSibling();slot.transform.Find("FocusCorners").SetAsLastSibling();
                PrefabUtility.SaveAsPrefabAsset(slot,Folder+"/InventoryCarriedSlot.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(slot); }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("SOLITUDE/UI/Refine Inventory Housing")]
        public static void RefineHousing()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/Exo_2/Exo2-Regular SDF.asset");
            textMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Systems/Containers/Materials/InventoryText.mat");
            var root=PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var panel=root.transform.Find("Panel");panel.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
                var housing=Place(panel,"Housing",0,0,718,680);housing.SetAsFirstSibling();
                if(housing.GetComponent<CanvasRenderer>()==null)housing.gameObject.AddComponent<CanvasRenderer>();
                var face=housing.GetComponent<InventoryPanelGraphic>()??housing.gameObject.AddComponent<InventoryPanelGraphic>();face.raycastTarget=false;
                var shadow=Place(panel,"HousingShadow",0,12,718,680);shadow.SetAsFirstSibling();
                var shade=shadow.GetComponent<InventorySoftShadow>()??shadow.gameObject.AddComponent<InventorySoftShadow>();shade.raycastTarget=false;
                panel.Find("SurfaceLight").gameObject.SetActive(false);
                var title=Place(panel,"Title",32,20,340,60).GetComponent<TMP_Text>();title.fontSize=42;title.characterSpacing=5;title.fontStyle=FontStyles.Bold;
                Paint(Place(panel,"HeaderDivider",32,88,654,1),new Color(.36f,.43f,.44f));
                Paint(Place(panel,"TitleUnderline",32,87,64,3),Amber);
                Paint(Place(panel,"FooterRule",32,650,654,1),new Color(.24f,.31f,.33f));
                var pop=root.transform.Find("ItemPopover");var bg=pop.Find("Face");if(bg.GetComponent<UnityEngine.UI.Image>()!=null)UnityEngine.Object.DestroyImmediate(bg.GetComponent<UnityEngine.UI.Image>());
                var popupFace=bg.GetComponent<InventoryPanelGraphic>()??bg.gameObject.AddComponent<InventoryPanelGraphic>();popupFace.raycastTarget=false;
                var so=new SerializedObject(popupFace);so.FindProperty("top").colorValue=new Color(.17f,.23f,.25f);so.FindProperty("bottom").colorValue=new Color(.08f,.12f,.14f);so.FindProperty("cut").floatValue=16;so.ApplyModifiedPropertiesWithoutUndo();
                pop.Find("SurfaceLight").gameObject.SetActive(false);
                Paint(Place(pop,"SelectionTab",0,24,4,48),Amber);
                pop.Find("ItemTitle").GetComponent<TMP_Text>().characterSpacing=1;
                foreach(var button in pop.Find("Actions").GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    var cap=button.transform.Find("Keycap");if(cap!=null)cap.GetComponent<UnityEngine.UI.Image>().color=new Color(.27f,.33f,.34f);
                }
                root.transform.Find("WorldDimmer").GetComponent<UnityEngine.UI.Image>().color=new Color(.015f,.03f,.04f,.66f);
                PrefabUtility.SaveAsPrefabAsset(root,ScreenPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            var slot=PrefabUtility.LoadPrefabContents(Folder+"/InventoryCarriedSlot.prefab");
            try
            {
                slot.transform.Find("RestBorder").gameObject.SetActive(false);
                slot.transform.Find("RecessTop").gameObject.SetActive(false);slot.transform.Find("RecessFoot").gameObject.SetActive(false);
                var corners=Corners(slot.transform,"SocketCorners",104,104,new Color(.30f,.37f,.38f,.5f));corners.SetActive(true);
                foreach(var edge in corners.GetComponentsInChildren<UnityEngine.UI.Image>())
                {var r=edge.rectTransform;var size=r.sizeDelta;if(size.x==3)size.x=1;if(size.y==3)size.y=1;r.sizeDelta=size;}
                slot.transform.Find("Selected").SetAsLastSibling();slot.transform.Find("FocusCorners").SetAsLastSibling();
                PrefabUtility.SaveAsPrefabAsset(slot,Folder+"/InventoryCarriedSlot.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(slot);}
            AssetDatabase.SaveAssets();
        }

        private static void StyleActionKeycaps(GameObject root)
        {
            var panel=root.transform.Find("Panel");
            panel.Find("ControlHints")?.gameObject.SetActive(false);
            panel.Find("NavigationHints")?.gameObject.SetActive(false);
            string[] names={"Equip","Assign to hotbar","Move"};string[] keys={"E","R","M"};float[] widths={184,260,184};
            for(int i=0;i<names.Length;i++)
            {
                var button=panel.Find("Actions/"+names[i]);var width=widths[i];
                button.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth=width;
                Place(button,"Label",54,9,width-66,28);Corners(button,"FocusCorners",width,44,Text);
                var cap=Place(button,"Keycap",12,8,32,28);Paint(cap,new Color(.067f,.11f,.10f));
                var glyph=cap.Find("Glyph")?.GetComponent<TMP_Text>();
                if(glyph==null)glyph=Label("Glyph",cap,keys[i],16,Text,0,2,32,24);
                glyph.text=keys[i];glyph.fontStyle=FontStyles.Bold;glyph.alignment=TextAlignmentOptions.Center;
            }
        }
        [MenuItem("SOLITUDE/UI/Inventory Action Keycaps")]
        public static void ActionKeycaps()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before authoring.");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/Exo_2/Exo2-Regular SDF.asset");
            textMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Systems/Containers/Materials/InventoryText.mat");
            var root=PrefabUtility.LoadPrefabContents(ScreenPath);
            try { StyleActionKeycaps(root);PrefabUtility.SaveAsPrefabAsset(root,ScreenPath); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        public const string PersistentPath = Folder + "/PersistentHotbar.prefab";
        private static void CompactSlot(string path, bool hotbar)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                float size=hotbar?64:104;
                root.GetComponent<UnityEngine.UI.Button>().transition=UnityEngine.UI.Selectable.Transition.None;
                ((RectTransform)root.transform).sizeDelta=new Vector2(size,size);
                root.transform.Find("ItemName")?.gameObject.SetActive(false);
                Place(root.transform,"Icon",hotbar?12:20,hotbar?12:20,hotbar?40:64,hotbar?40:64);
                Place(root.transform,"Quantity",size-46,size-27,40,24);
                foreach(var name in new[]{"Selected","RestBorder"})
                {
                    var edge=root.transform.Find(name);
                    Place(edge,"Top",0,0,size,1);Place(edge,"Bottom",0,size-1,size,1);
                    Place(edge,"Left",0,0,1,size);Place(edge,"Right",size-1,0,1,size);
                }
                Corners(root.transform,"FocusCorners",size,size,Amber);
                if(hotbar) { Place(root.transform,"Number",5,2,22,22);Place(root.transform,"Equipped",0,size-3,size,3); }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [MenuItem("SOLITUDE/UI/Compact Game Inventory")]
        public static void CompactInventory()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before authoring.");
            Author();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/Exo_2/Exo2-Regular SDF.asset");
            textMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Systems/Containers/Materials/InventoryText.mat");
            CompactSlot(Folder+"/InventoryCarriedSlot.prefab",false);
            CompactSlot(Folder+"/InventoryHotbarSlot.prefab",true);
            var root=PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var panel=(RectTransform)root.transform.Find("Panel");panel.sizeDelta=new Vector2(1120,720);panel.anchoredPosition=new Vector2(0,36);
                root.transform.Find("WorldDimmer").GetComponent<UnityEngine.UI.Image>().color=new Color(.027f,.067f,.071f,.52f);
                Place(panel,"Title",32,20,500,54).GetComponent<TMP_Text>().fontSize=40;
                Place(panel,"Capacity",750,33,126,30);
                var back=Place(panel,"Back",900,24,188,44);Corners(back,"FocusCorners",188,44,Text);
                Place(panel,"HeaderDivider",32,84,1056,2);
                string[] names={"All","Tools","Consumables","Miscellaneous"};float[] x={32,112,208,374};float[] widths={74,90,160,202};
                for(int i=0;i<4;i++) { var tab=Place(panel,names[i],x[i],104,widths[i],40);Place(tab,"Label",10,6,widths[i]-20,28);Place(tab,"SelectedTab",10,37,widths[i]-20,3);Corners(tab,"FocusCorners",widths[i],40,Text); }
                var scroll=Place(panel,"CarriedScroll",32,160,654,434);
                var layout=scroll.Find("Viewport/Slots").GetComponent<UnityEngine.UI.GridLayoutGroup>();layout.cellSize=new Vector2(104,104);
                var so=new SerializedObject(root.GetComponent<ContainerView>());so.FindProperty("maximumViewportHeight").floatValue=434;so.ApplyModifiedPropertiesWithoutUndo();
                Place(panel,"InspectorDivider",714,104,1,490);
                // Item art belongs directly to the inspection area, without a nested display card.
                panel.Find("ArtField").GetComponent<UnityEngine.UI.Image>().color=Color.clear;
                Place(panel,"ArtField",744,104,344,208);Place(panel,"ItemArt",816,108,192,192);
                Place(panel,"ItemTitle",744,324,344,88).GetComponent<TMP_Text>().fontSize=32;
                Place(panel,"Metadata",744,416,344,26);
                Place(panel,"Description",744,456,344,124);
                Place(panel,"Context",32,602,654,32).GetComponent<TMP_Text>().fontSize=16;
                var actions=Place(panel,"Actions",32,642,654,44);
                UnityEngine.Object.DestroyImmediate(actions.GetComponent<UnityEngine.UI.VerticalLayoutGroup>());
                var horizontal=actions.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>()??actions.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                horizontal.spacing=6;horizontal.childControlWidth=horizontal.childControlHeight=true;horizontal.childForceExpandHeight=false;horizontal.childForceExpandWidth=false;
                foreach(var button in actions.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    var le=button.GetComponent<UnityEngine.UI.LayoutElement>();le.preferredWidth=214;le.preferredHeight=44;le.flexibleWidth=0;
                    Place(button.transform,"Label",12,9,190,28);Corners(button.transform,"FocusCorners",214,44,Text);
                }
                Paint(Place(panel,"CommandDivider",32,630,1056,1),Line);
                foreach(var name in new[]{"HotbarSlots","HotbarTitle","Equipped","HotbarDivider"})panel.Find(name).gameObject.SetActive(false);
                // The scene supplies the one persistent hotbar endpoint.
                Set(root.GetComponent<InventoryScreenView>(),"hotbarView",null);
                var hints=Place(panel,"ControlHints",744,602,344,84);
                Hint(hints,"Navigate","ARROWS / D-PAD","Navigate",0,166);
                Place(hints,"Navigate",0,0,166,28);Place(hints.Find("Navigate"),"Glyph",0,2,166,24);
                Place(hints,"NavigateAction",176,3,120,26);
                Hint(hints,"Select","ENTER / A","Select",0,106);
                Place(hints,"Select",0,48,106,28);Place(hints.Find("Select"),"Glyph",0,2,106,24);
                Place(hints,"SelectAction",116,51,120,26);
                StyleActionKeycaps(root);
                PrefabUtility.SaveAsPrefabAsset(root,ScreenPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            if(AssetDatabase.LoadAssetAtPath<GameObject>(PersistentPath)==null)
            {
                var bar=Rect("PersistentHotbar",null,0,0,1920,1080);Fill(bar);
                try
                {
                    var canvas=bar.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30010;
                    var scaler=bar.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
                    bar.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();var group=bar.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
                    var tray=Rect("Tray",bar,0,0,640,80);tray.anchorMin=tray.anchorMax=tray.pivot=new Vector2(.5f,0);tray.anchoredPosition=new Vector2(0,24);Image(tray,new Color(.078f,.125f,.118f,1));
                    var grid=Rect("Slots",tray,8,8,624,64);var layout=grid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();layout.cellSize=new Vector2(64,64);layout.spacing=new Vector2(6,0);layout.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;layout.constraintCount=9;
                    var view=bar.gameObject.AddComponent<ContainerView>();Set(view,"slotViewPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/InventoryHotbarSlot.prefab").GetComponent<ContainerSlotView>());Set(view,"slotsParent",grid);Set(view,"gridLayoutGroup",layout);Number(view,"columns",9);
                    var controller=bar.gameObject.AddComponent<ContainerController>();Set(controller,"view",view);
                    PrefabUtility.SaveAsPrefabAsset(bar.gameObject,PersistentPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(bar.gameObject); }
            }
            ConfigureScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        private static void ConfigurePersistentHotbar(Scene scene, SceneBindings bindings, InventoryScreenView screen)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PersistentPath);if(prefab==null)return;
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ContainerController>(true)).ToArray();
            var controller=all.FirstOrDefault(c=>c.name=="PersistentHotbar");
            if(controller==null)
            {
                var legacy=all.First(c=>c.DefaultSource is SOLITUDE.Hotbar.Hotbar);
                controller=((GameObject)PrefabUtility.InstantiatePrefab(prefab,scene)).GetComponent<ContainerController>();
                Set(controller,"defaultContainerSource",(MonoBehaviour)legacy.DefaultSource);
                var so=new SerializedObject(bindings);var list=so.FindProperty("controllers");for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==legacy)list.GetArrayElementAtIndex(i).objectReferenceValue=controller;so.ApplyModifiedPropertiesWithoutUndo();
                legacy.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(legacy.gameObject);
            }
            Set(screen,"hotbarView",controller.GetComponent<ContainerView>());Set(screen,"hotbarInputGroup",controller.GetComponent<CanvasGroup>());
            var quiet=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true))
                .Where(t=>t.name=="HUDCanvas"||t.name=="RoomAtmosphereWarning"||t.name=="EquipmentStatus").Select(t=>(UnityEngine.Object)t.gameObject).ToArray();
            Array(screen,"quietWhileOpen",quiet);
            ConfigureEquipmentHud(scene, screen, controller.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(screen);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        public static void ConfigureScene(Scene scene)
        {
            Author();
            var roots=scene.GetRootGameObjects(); var bindings=roots.SelectMany(r=>r.GetComponentsInChildren<SceneBindings>(true)).Single();
            var screen=roots.SelectMany(r=>r.GetComponentsInChildren<InventoryScreenView>(true)).SingleOrDefault();
            if(screen!=null) { ConfigurePersistentHotbar(scene,bindings,screen); return; }
            var data=new SerializedObject(bindings); var old=(ContainerModalView)data.FindProperty("inventoryModal").objectReferenceValue;
            var legacy=old.Controller;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath),scene);
            screen=go.GetComponent<InventoryScreenView>(); data.FindProperty("inventoryModal").objectReferenceValue=go.GetComponent<ContainerModalView>();
            var controllers=data.FindProperty("controllers"); for(int i=0;i<controllers.arraySize;i++) if(controllers.GetArrayElementAtIndex(i).objectReferenceValue==legacy)controllers.GetArrayElementAtIndex(i).objectReferenceValue=go.GetComponent<ContainerController>();
            var oldbar=roots.SelectMany(r=>r.GetComponentsInChildren<ContainerController>(true)).FirstOrDefault(c=>c.DefaultSource is SOLITUDE.Hotbar.Hotbar);

            data.ApplyModifiedPropertiesWithoutUndo(); old.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);
            ConfigurePersistentHotbar(scene,bindings,screen);bindings.Validate(); EditorSceneManager.MarkSceneDirty(scene);
        }
        public static string CaptureForReview(string path)
        {
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Scene camera is missing.");
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var oldCameras = canvases.Select(c => c.worldCamera).ToArray();
            var oldPlanes = canvases.Select(c => c.planeDistance).ToArray();
            var oldLayers = canvases.Select(c => c.sortingLayerID).ToArray();
            var oldOrders = canvases.Select(c => c.sortingOrder).ToArray();
            var previous = camera.targetTexture; var active = RenderTexture.active;
            var target = new RenderTexture(1920,1080,24); var pixels = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture = target;
                for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = 1; canvases[i].sortingLayerName = "Inventory"; canvases[i].sortingOrder = canvases[i].name == "PersistentHotbar" ? 32761 : canvases[i].GetComponent<InventoryScreenView>() != null ? 32760 : 30000; }
                Canvas.ForceUpdateCanvases();
                foreach(var canvas in canvases)canvas.GetComponent<InventoryScreenView>()?.SendMessage("PositionPopover",SendMessageOptions.DontRequireReceiver);
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)); System.IO.File.WriteAllBytes(path,pixels.EncodeToPNG()); return path;
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceOverlay; canvases[i].worldCamera = oldCameras[i]; canvases[i].planeDistance = oldPlanes[i]; canvases[i].sortingLayerID = oldLayers[i]; canvases[i].sortingOrder = oldOrders[i]; }
                camera.targetTexture = previous; RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(target); Canvas.ForceUpdateCanvases();
            }
        }
        [MenuItem("SOLITUDE/UI/Install Inventory In Scrubber Encounter")]
        public static void InstallInScrubber()
        {
            var scene=SceneManager.GetActiveScene(); if(scene.path!=ScrubberEncounterBuilder.ScenePath)throw new InvalidOperationException("Open AtmosphericScrubberEncounter first.");
            ConfigureScene(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
    }
}
