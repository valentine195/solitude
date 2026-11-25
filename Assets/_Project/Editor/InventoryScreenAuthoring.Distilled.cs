using System;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Player;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SOLITUDE.Containers;
using TMPro;
using UnityEditor;
using SOLITUDE.UI;
using UnityEngine;

namespace SOLITUDE.Editor
{
    public static partial class InventoryScreenAuthoring
    {
        [MenuItem("SOLITUDE/UI/Install Weapon, Tool and Quick Use")]
        public static void InstallCarriedEquipment()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScrubberEncounterBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved Scrubber scene in Edit Mode.");
            var bootstrap = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<GameCompositionRoot>(true)).Single();
            var config = (GameRuntimeConfig)new SerializedObject(bootstrap).FindProperty("config").objectReferenceValue;
            if (config == null || !AssetDatabase.GetAssetPath(config).EndsWith("/ScrubberRuntimeConfig.asset", StringComparison.Ordinal))
                throw new InvalidOperationException("The Scrubber scene requires its own runtime configuration.");
            AuthorCarriedEquipmentPanel();
            config.useCarriedEquipment = true;
            EditorUtility.SetDirty(config);
            ConfigureScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
        }

        private static void LoadInventoryTypography()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/Exo_2/Exo2-Regular SDF.asset");
            textMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Systems/Containers/Materials/InventoryText.mat");
        }

        private static void AuthorCarriedEquipmentPanel()
        {
            LoadInventoryTypography();
            var root = PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var panel = (RectTransform)root.transform.Find("Panel");
                var screen = root.GetComponent<InventoryScreenView>();
                panel.sizeDelta = new Vector2(1520, 666);
                root.transform.Find("WorldDimmer").GetComponent<UnityEngine.UI.Image>().color = new Color(.025f, .036f, .043f, .96f);
                StyleVitalsSurface(panel, new Color32(24, 41, 50, 255), Color.clear, 24);
                panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;

                var header = Place(panel, "HeaderFace", 0, 0, 1520, 81);
                StyleVitalsSurface(header, ShipboardUiColors.PanelInk, ShipboardUiColors.PanelRim, 24);
                header.SetAsFirstSibling();
                foreach (var obsolete in new[] { header.Find("ShipElevation"), panel.Find("FooterFace"),
                             panel.Find("InventoryFooterRule"), panel.Find("InventoryNavigation") })
                    if (obsolete != null) UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
                StyleEquipmentBacking(panel);

                Place(panel, "Title", 36, 21, 620, 54).GetComponent<TMP_Text>().fontSize = 36;
                var back = Place(panel, "Back", 1284, 22, 200, 44);
                Set(screen, "backLabel", Place(back, "Label", 0, 5, 190, 38).GetComponent<TMP_Text>());
                Corners(back, "FocusCorners", 200, 44, Text);
                Paint(Place(panel, "HeaderDivider", 36, 80, 1448, 1), VitalsInsetRule);
                DistilledLabel("EquipmentHeading", panel, "EQUIPMENT", 23, Text, 36, 102, 268, 36);
                DistilledLabel("CarriedHeading", panel, "CARRIED", 23, Text, 352, 102, 330, 36);
                var capacityBacking = Place(panel, "CapacityBacking", 816, 102, 190, 35);
                StyleVitalsSurface(capacityBacking, ShipboardUiColors.PanelInk, VitalsInsetRule, 10);
                capacityBacking.SetAsFirstSibling();
                var count = Place(panel, "Capacity", 820, 103, 170, 35).GetComponent<TMP_Text>();
                count.fontSize = 21;
                count.enableAutoSizing = true;
                count.fontSizeMin = 15;
                count.fontSizeMax = 21;
                DistilledLabel("DetailsHeading", panel, "DETAILS", 23, Text, 1054, 102, 430, 36);
                Paint(Place(panel, "EquipmentRule", 36, 139, 268, 1), Line);
                Paint(Place(panel, "CarriedRule", 352, 139, 654, 1), Line);
                Paint(Place(panel, "DetailsRule", 1054, 139, 430, 1), Line);
                Paint(Place(panel, "EquipmentDivider", 327, 103, 1, 526), Line);
                Paint(Place(panel, "InspectorDivider", 1029, 103, 1, 526), Line).gameObject.SetActive(true);
                Place(panel, "CarriedScroll", 352, 156, 654, 434);
                var context = Place(panel, "Context", 352, 602, 654, 54).GetComponent<TMP_Text>();
                context.fontSize = 19;
                context.color = ShipboardUiColors.Amber;

                var weapon = Place(panel, "WeaponSlot", 36, 156, 268, 127);
                Paint(Place(weapon, "DestinationResponse", 0, 0, 268, 127), Color.clear);
                StyleEquipmentCard(weapon, "WEAPON", "ItemName", screen, "weaponName", "weaponIcon", "weaponStatus");
                var tool = Place(panel, "SuitSlot", 36, 295, 268, 127);
                tool.Find("Reason").gameObject.SetActive(false);
                StyleEquipmentCard(tool, "TOOL", "Unavailable", screen, "toolName", "toolIcon", "toolStatus");
                AuthorEquipmentCardSelection(weapon, screen, "weaponCard");
                AuthorEquipmentCardSelection(tool, screen, "toolCard");
                AuthorInventoryQuickUse(panel, screen);

                foreach (string name in new[] { "QuickAccessDock", "HotbarTitle", "HotbarDivider" })
                    panel.Find(name)?.gameObject.SetActive(false);

                var details = Place(panel, "ItemPopover", 1054, 156, 430, 471);
                var itemTitle = Place(details, "ItemTitle", 0, 1, 430, 42).GetComponent<TMP_Text>();
                itemTitle.fontSize = 29;
                itemTitle.enableAutoSizing = true;
                itemTitle.fontSizeMin = 22;
                itemTitle.fontSizeMax = 29;
                var metadata = Place(details, "Metadata", 0, 48, 430, 28).GetComponent<TMP_Text>();
                metadata.fontSize = 19;
                metadata.color = Muted;
                var artField = Place(details, "ArtField", 0, 86, 430, 144);
                Paint(artField, new Color32(23, 38, 47, 255));
                var plate = Place(artField, "RegistrationGrid", 0, 0, 430, 144);
                var drawing = plate.GetComponent<InventoryDraftingGraphic>() ?? plate.gameObject.AddComponent<InventoryDraftingGraphic>();
                drawing.raycastTarget = false;
                PlaceCenteredArt(details, "ItemArt", 142, 91, 146, 134);
                Place(details, "Description", 0, 242, 430, 88).GetComponent<TMP_Text>().fontSize = 20;
                Paint(Place(details, "ActionDivider", 0, 334, 430, 1), Line);
                Place(details, "Replacement", 0, 330, 430, 38).gameObject.SetActive(false);
                var actions = Place(details, "Actions", 0, 347, 430, 130);
                foreach (var button in actions.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    button.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 40;
                    var label = Place(button.transform, "Label", 14, 6, 338, 28).GetComponent<TMP_Text>();
                    label.fontSize = 21;
                    label.fontStyle = FontStyles.Normal;
                    label.characterSpacing = 0;
                    var keycap = Place(button.transform, "Keycap", 384, 7, 32, 26);
                    if (keycap.GetComponent<UnityEngine.UI.Image>() != null)
                    {
                        Place(keycap, "Glyph", 0, 2, 32, 23);
                        Paint(keycap, ShipboardUiColors.PanelInk);
                    }
                    Corners(button.transform, "FocusCorners", 430, 40, Text);
                }
                Set(screen, "actionDivider", details.Find("ActionDivider").gameObject);
                panel.Find("VitalsSurface").SetAsFirstSibling();
                PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            var slotPath = Folder + "/InventoryCarriedSlot.prefab";
            var slot = PrefabUtility.LoadPrefabContents(slotPath);
            try
            {
                var selection = new SerializedObject(slot.GetComponent<ContainerSlotView>())
                    .FindProperty("selectionHighlight").objectReferenceValue as GameObject;
                foreach (var mark in selection.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    mark.color = ShipboardUiColors.Amber;
                AuthorCarriedEquippedLabel(slot.transform);
                PrefabUtility.SaveAsPrefabAsset(slot, slotPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(slot); }
        }

        internal static void StyleEquipmentBacking(Transform panel)
        {
            bool newFace = panel.Find("EquipmentFace") == null;
            var face = Place(panel, "EquipmentFace", 0, 82, 327, 363);
            Paint(face, new Color32(17, 32, 41, 255));
            if (newFace) face.SetAsFirstSibling();
        }

        // Match the exploration Vitals HUD's face and top edge.
        private static readonly Color VitalsInsetRule = new Color32(75, 96, 103, 255);

        private static void StyleVitalsSurface(RectTransform rect, Color face, Color edge, float cut)
        {
            Paint(rect, Color.clear);
            var backing = Place(rect, "VitalsSurface", 0, 0, rect.sizeDelta.x, rect.sizeDelta.y);
            Fill(backing);
            backing.SetAsFirstSibling();
            var surface = backing.GetComponent<InventoryPanelGraphic>() ?? backing.gameObject.AddComponent<InventoryPanelGraphic>();
            surface.raycastTarget = false;
            var data = new SerializedObject(surface);
            data.FindProperty("top").colorValue = face;
            data.FindProperty("bottom").colorValue = face;
            data.FindProperty("cut").floatValue = cut;
            data.FindProperty("topRule").colorValue = edge;
            data.FindProperty("squareLowerCorner").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform PlaceCenteredArt(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = Place(parent, name, x, y, width, height);
            rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = new Vector2(x + width * .5f, -y - height * .5f);
            return rect;
        }

        internal static void AuthorInventoryQuickUse(Transform panel, InventoryScreenView screen)
        {
            LoadInventoryTypography();
            panel.Find("QuickUse")?.gameObject.SetActive(false);
            panel.Find("QuickUseRule")?.gameObject.SetActive(false);
            var row = Place(panel, "QuickUseSlots", 352, 595, 654, 74);
            DistilledLabel("Heading", row, "QUICK USE", 20, Text, 0, 26, 126, 28);
            var data = new SerializedObject(screen);
            data.FindProperty("inventoryQuickUse").objectReferenceValue = row.gameObject;
            var slots = data.FindProperty("inventoryQuickUseSlots");
            slots.arraySize = SOLITUDE.SaveLoad.PlayerEquipmentRecord.QuickUseCount;
            for (int i = 0; i < slots.arraySize; i++)
            {
                var slot = slots.GetArrayElementAtIndex(i);
                float x = 134 + 67 * i;
                var number = DistilledLabel($"Number{i + 1}", row, (i + 1).ToString(), 15, Text, x, 0, 55, 20);
                number.alignment = TextAlignmentOptions.Center;
                var cell = Place(row, $"Slot{i + 1}", x, 21, 55, 49);
                if (cell.Find("Rim") is Transform previousRim)
                    UnityEngine.Object.DestroyImmediate(previousRim.gameObject);
                var rim = ExplorationHudPrototypeAuthoring.Bevel("Rim", cell, 0, 0, 55, 49, 7,
                    ShipboardUiColors.PanelInk, ShipboardUiColors.QuickUseRim, 2);
                var face = rim.transform.Find("Face").GetComponent<UnityEngine.UI.Graphic>();
                face.raycastTarget = true;
                var button = cell.GetComponent<UnityEngine.UI.Button>() ?? cell.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = face;
                button.transition = UnityEngine.UI.Selectable.Transition.None;
                if (!cell.TryGetComponent<ContainerSlotView>(out _)) cell.gameObject.AddComponent<ContainerSlotView>();
                var icon = Paint(PlaceCenteredArt(cell, "Icon", 10, 5, 34, 34), Color.white);
                icon.preserveAspect = true;
                icon.enabled = false;
                var quantity = DistilledLabel("Quantity", cell, "", 14, Text, 18, 30, 33, 17);
                quantity.alignment = TextAlignmentOptions.MidlineRight;
                quantity.textWrappingMode = TextWrappingModes.NoWrap;
                quantity.overflowMode = TextOverflowModes.Overflow;
                icon.transform.SetSiblingIndex(rim.transform.GetSiblingIndex() + 1);
                quantity.transform.SetAsLastSibling();
                slot.FindPropertyRelative("button").objectReferenceValue = button;
                slot.FindPropertyRelative("icon").objectReferenceValue = icon;
                slot.FindPropertyRelative("quantity").objectReferenceValue = quantity;
                slot.FindPropertyRelative("rim").objectReferenceValue = rim;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            AuthorInventoryDropFeedback(panel);
        }

        internal static void AuthorCarriedEquippedLabel(Transform slot)
        {
            LoadInventoryTypography();
            var label = DistilledLabel("EquippedLabel", slot, "EQUIPPED", 12,
                new Color32(188, 213, 223, 255), 6, 4, 78, 17);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.transform.SetAsLastSibling();
            label.gameObject.SetActive(false);
            var view = slot.GetComponent<ContainerSlotView>();
            Set(view, "equippedLabel", label.gameObject);
            Set(view, "equippedIconFrame", Place(slot, "EquippedIconFrame", 31, 19, 42, 40));
        }

        internal static void AuthorInventoryDropFeedback(Transform panel)
        {
            var context = Place(panel, "Context", 455, 104, 350, 34).GetComponent<TMP_Text>();
            context.fontSize = 16;
            context.alignment = TextAlignmentOptions.MidlineLeft;
            context.textWrappingMode = TextWrappingModes.Normal;
            context.overflowMode = TextOverflowModes.Ellipsis;
            context.color = ShipboardUiColors.Amber;

            var row = panel.Find("QuickUseSlots");
            if (row == null) return;
            for (int i = 1; i <= SOLITUDE.SaveLoad.PlayerEquipmentRecord.QuickUseCount; i++)
            {
                var slot = row.Find($"Slot{i}") as RectTransform;
                if (slot == null) continue;
                var view = slot.GetComponent<ContainerSlotView>();
                var focus = Corners(slot, "FocusCorners", 55, 49, ShipboardUiColors.Amber);
                focus.transform.SetAsLastSibling();
                Set(view, "navigationFocus", focus);
                AuthorDropMarker(slot);
            }
            AuthorDropMarker((RectTransform)panel.Find("WeaponSlot"));
            AuthorDropMarker((RectTransform)panel.Find("SuitSlot"));
        }

        private static void AuthorDropMarker(RectTransform target)
        {
            var view = target.GetComponent<ContainerSlotView>();
            var marker = Paint(Place(target, "DropDestination", 5, target.sizeDelta.y - 5,
                target.sizeDelta.x - 10, 2), new Color32(112, 171, 192, 255));
            marker.transform.SetAsLastSibling();
            marker.gameObject.SetActive(false);
            Set(view, "dropDestination", marker.gameObject);
        }

        internal static void AuthorEquipmentCardSelection(Transform card, InventoryScreenView screen, string field)
        {
            var rect = (RectTransform)card;
            var image = card.GetComponent<UnityEngine.UI.Image>();
            image.raycastTarget = true;
            var button = card.GetComponent<UnityEngine.UI.Button>() ?? card.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            button.interactable = false;
            if (!card.TryGetComponent<ContainerSlotView>(out _)) card.gameObject.AddComponent<ContainerSlotView>();
            var marker = Paint(Place(card, "EquippedSelection", 0, 0, 3, rect.sizeDelta.y), ShipboardUiColors.Amber);
            marker.gameObject.SetActive(false);
            Set(screen, field, button);
        }

        private static void StyleEquipmentCard(Transform card, string heading, string name, InventoryScreenView screen,
            string nameField, string iconField, string statusField)
        {
            var rect = (RectTransform)card;
            StyleVitalsSurface(rect, ShipboardUiColors.PanelInk, VitalsInsetRule, 15);
            if (card.Find("CardEdge") is Transform oldEdge)
                UnityEngine.Object.DestroyImmediate(oldEdge.gameObject);
            var well = Place(card, "IconWell", 11, 12, 70, 86);
            Paint(well, new Color32(24, 41, 50, 255));
            well.SetSiblingIndex(1);
            var icon = Paint(PlaceCenteredArt(card, "Icon", 17, 19, 58, 72), Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;
            icon.transform.SetAsLastSibling();
            DistilledLabel("SlotLabel", card, heading, 17, Muted, 94, 13, 162, 25);
            var item = DistilledLabel(name, card, "Empty", 21, Text, 94, 40, 162, 45);
            item.enableAutoSizing = true;
            item.fontSizeMin = 16;
            item.fontSizeMax = 21;
            item.overflowMode = TextOverflowModes.Ellipsis;
            var status = DistilledLabel("Status", card, "", 17, Muted, 94, 88, 162, 27);
            Set(screen, nameField, item);
            Set(screen, iconField, icon);
            Set(screen, statusField, status);
        }

        private static void ConfigureEquipmentHud(Scene scene, InventoryScreenView screen, Transform hotbarCanvas)
        {
            var bootstrap = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<GameCompositionRoot>(true)).Single();
            var config = (GameRuntimeConfig)new SerializedObject(bootstrap).FindProperty("config").objectReferenceValue;
            if (config == null || !config.useCarriedEquipment) return;
            var roots = scene.GetRootGameObjects();
            PlayerVitalsAuthoring.ConfigureComposition(
                roots.SelectMany(root => root.GetComponentsInChildren<SceneBindings>(true)).Single(),
                roots.SelectMany(root => root.GetComponentsInChildren<PlayerVitalsController>(true)).Single());
            LoadInventoryTypography();
            hotbarCanvas.Find("Tray").gameObject.SetActive(false);
            Set(screen, "inHandHud", EquipmentHudLabel(hotbarCanvas, "InHandHud", -222));
            Set(screen, "quickUseHud", EquipmentHudLabel(hotbarCanvas, "QuickUseHud", 222));
        }

        private static TMP_Text EquipmentHudLabel(Transform parent, string name, float x)
        {
            var text = parent.Find(name)?.GetComponent<TMP_Text>() ?? Label(name, parent, "", 22, Text, 0, 0, 420, 42);
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(x, 24);
            text.alignment = TextAlignmentOptions.Center;
            text.gameObject.SetActive(false);
            return text;
        }

        private static TMP_Text DistilledLabel(string name, Transform parent, string value, float size, Color color, float x, float y, float width, float height)
        {
            var existing = parent.Find(name)?.GetComponent<TMP_Text>();
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name == name && existing != null && child != existing.transform)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            if (existing == null) return Label(name,parent,value,size,color,x,y,width,height);
            Place(parent,name,x,y,width,height); existing.text = value; existing.fontSize = size; existing.color = color;
            return existing;
        }
        /// <summary>Explicit migration only. Saved prefab remains editable; ConfigureScene never reapplies this layout.</summary>
        [MenuItem("SOLITUDE/UI/Apply Distilled Inventory")]
        public static void DistilledInventory()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/Exo_2/Exo2-Regular SDF.asset");
            textMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Systems/Containers/Materials/InventoryText.mat");
            var root = PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var screen = root.GetComponent<InventoryScreenView>();
                var panel = (RectTransform)root.transform.Find("Panel");
                panel.sizeDelta = new Vector2(1600,880); panel.anchoredPosition = Vector2.zero;
                Paint(panel,new Color(.10f,.14f,.155f,1)).raycastTarget = true;
                var face = panel.GetComponent<InventoryPanelGraphic>();
                if (face != null) { var data = new SerializedObject(face); data.FindProperty("top").colorValue = new Color(.10f,.14f,.155f); data.FindProperty("bottom").colorValue = new Color(.10f,.14f,.155f); data.FindProperty("cut").floatValue = 0; data.ApplyModifiedPropertiesWithoutUndo(); }
                foreach (string name in new[] { "HousingShadow", "Housing", "Paused", "All", "Tools", "Consumables", "Miscellaneous", "ControlHints", "NavigationHints", "TitleUnderline", "FooterRule", "CommandDivider", "Equipped", "HotbarSlots" })
                    panel.Find(name)?.gameObject.SetActive(false);
                root.transform.Find("WorldDimmer").GetComponent<UnityEngine.UI.Image>().color = new Color(.018f,.035f,.04f,.6f);
                var title = Place(panel,"Title",40,22,620,62).GetComponent<TMP_Text>(); title.fontSize = 40; title.characterSpacing = 3;
                Paint(Place(panel,"HeaderDivider",40,96,1520,1),Line);
                var close = Place(panel,"Back",1330,26,230,52);
                close.Find("Keycap")?.gameObject.SetActive(false);
                var closeText = Place(close,"Label",0,8,230,36).GetComponent<TMP_Text>(); closeText.text = "ESC  Close"; closeText.fontSize = 24; closeText.alignment = TextAlignmentOptions.Right;
                Corners(close,"FocusCorners",230,52,Text);
                DistilledLabel("EquipmentHeading",panel,"EQUIPMENT",27,Text,40,119,310,40);
                DistilledLabel("CarriedHeading",panel,"CARRIED",27,Text,400,119,400,40);
                Place(panel,"Capacity",900,121,154,38).GetComponent<TMP_Text>().fontSize = 24;
                Paint(Place(panel,"EquipmentRule",40,166,310,1),Line);
                Paint(Place(panel,"CarriedRule",400,166,654,1),Line);
                Paint(Place(panel,"EquipmentDivider",378,118,1,572),Line);
                Paint(Place(panel,"InspectorDivider",1090,118,1,572),Line);
                var scroll = Place(panel,"CarriedScroll",400,194,654,434);
                var layout = scroll.Find("Viewport/Slots").GetComponent<UnityEngine.UI.GridLayoutGroup>(); layout.cellSize = new Vector2(104,104); layout.spacing = new Vector2(6,6);
                var cv = new SerializedObject(root.GetComponent<ContainerView>()); cv.FindProperty("maximumViewportHeight").floatValue = 434; cv.ApplyModifiedPropertiesWithoutUndo();
                var weapon = Place(panel,"WeaponSlot",40,194,310,150); Paint(weapon,new Color(.065f,.10f,.115f));
                var response = Paint(Place(weapon,"DestinationResponse",0,0,310,150),Color.clear); Set(screen,"weaponResponse",response);
                var icon = Paint(PlaceCenteredArt(weapon,"Icon",12,21,96,106),Text); icon.preserveAspect = true; icon.enabled = false; Set(screen,"weaponIcon",icon);
                DistilledLabel("SlotLabel",weapon,"WEAPON / TOOL",18,Muted,122,16,180,26);
                Set(screen,"weaponName",DistilledLabel("ItemName",weapon,"Nothing in hand",25,Text,122,52,174,86));
                var suit = Place(panel,"SuitSlot",40,370,310,146); Paint(suit,new Color(.065f,.10f,.115f));
                DistilledLabel("SlotLabel",suit,"SUIT / ARMOR",19,Text,20,19,270,32);
                DistilledLabel("Unavailable",suit,"Not available",23,Muted,20,61,270,36);
                DistilledLabel("Reason",suit,"Equipment support pending",17,Muted,20,104,270,28);
                var pop = (RectTransform)root.transform.Find("ItemPopover"); if (pop == null) pop = (RectTransform)panel.Find("ItemPopover");
                pop.SetParent(panel,false); Place(panel,"ItemPopover",1130,124,430,584);
                foreach (string name in new[] { "SoftShadow", "Face", "SurfaceLight", "SelectionTab", "Metadata" }) pop.Find(name)?.gameObject.SetActive(false);
                var itemTitle = Place(pop,"ItemTitle",0,0,430,66).GetComponent<TMP_Text>(); itemTitle.fontSize = 32;
                var artField = Place(pop,"ArtField",78,78,274,214); Paint(artField,new Color(.065f,.10f,.115f)); Set(screen,"itemArtField",artField.gameObject);
                PlaceCenteredArt(pop,"ItemArt",102,80,226,210).SetAsLastSibling();
                Place(pop,"Description",0,306,430,92).GetComponent<TMP_Text>().fontSize = 22;
                Paint(Place(pop,"ActionDivider",0,410,430,1),Line);
                Set(screen,"replacement",DistilledLabel("Replacement",pop,"",21,Text,0,425,430,54));
                var actions = Place(pop,"Actions",0,490,430,108);
                var horizontal = actions.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>(); if (horizontal != null) UnityEngine.Object.DestroyImmediate(horizontal);
                var vertical = actions.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() ?? actions.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                vertical.spacing = 6; vertical.childControlWidth = vertical.childControlHeight = true; vertical.childForceExpandHeight = false; vertical.childForceExpandWidth = true;
                foreach (var button in actions.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    var element = button.GetComponent<UnityEngine.UI.LayoutElement>(); element.preferredHeight = 48; element.flexibleWidth = 0;
                    button.transition = UnityEngine.UI.Selectable.Transition.None;
                    Place(button.transform,"Label",54,8,360,36).GetComponent<TMP_Text>().fontSize = 25;
                    Corners(button.transform,"FocusCorners",430,48,Text);
                }
                var equip = actions.Find("Equip"); equip.Find("Label").GetComponent<TMP_Text>().text = "Equip";
                var drop = actions.Find("Drop"); drop.Find("Label").GetComponent<TMP_Text>().text = "Drop (unavailable)";
                drop.Find("Label").GetComponent<TMP_Text>().fontSize = 21;
                DistilledLabel("UnavailableReason",pop,"World dropping unavailable",17,Muted,54,598,370,28).gameObject.SetActive(false);
                Place(panel,"Context",400,648,654,54).GetComponent<TMP_Text>().fontSize = 19;
                Paint(Place(panel,"HotbarDivider",40,730,1520,1),Line).gameObject.SetActive(true);
                var quick = Place(panel,"HotbarTitle",40,753,290,48); quick.gameObject.SetActive(true); quick.GetComponent<TMP_Text>().text = "QUICK ACCESS"; quick.GetComponent<TMP_Text>().fontSize = 25;
                var dock = Place(panel,"QuickAccessDock",340,750,1000,100); Set(screen,"quickAccessDock",dock);
                var dataScreen = new SerializedObject(screen); dataScreen.FindProperty("distilledLayout").boolValue = true; dataScreen.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,ScreenPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            var slot = PrefabUtility.LoadPrefabContents(Folder+"/InventoryCarriedSlot.prefab");
            try
            {
                slot.transform.Find("ItemName").gameObject.SetActive(true);
                var name = Place(slot.transform,"ItemName",6,60,92,42).GetComponent<TMP_Text>();
                name.fontSize = 16; name.enableAutoSizing = true; name.fontSizeMin = 12; name.fontSizeMax = 16;
                name.textWrappingMode = TextWrappingModes.Normal; name.overflowMode = TextOverflowModes.Ellipsis;
                Place(slot.transform,"Icon",26,6,52,52); Place(slot.transform,"Quantity",67,38,30,24);
                slot.transform.Find("SocketCorners")?.gameObject.SetActive(false);
                var border = slot.transform.Find("RestBorder"); if (border != null) border.gameObject.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(slot,Folder+"/InventoryCarriedSlot.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(slot); }
            AssetDatabase.SaveAssets();
        }
    }
}
