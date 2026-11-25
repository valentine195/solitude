using System;
using System.Linq;
using SOLITUDE.Containers;
using SOLITUDE.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SOLITUDE.Editor
{
    public static partial class InventoryScreenAuthoring
    {
        [MenuItem("SOLITUDE/UI/Author Inventory Transfer")]
        public static void AuthorTransfer()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath)?.GetComponent<InventoryScreenView>();
            if (prefab == null) throw new InvalidOperationException("Author the Inventory screen first.");
            LoadInventoryTypography();
            var lockerSlot = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/InventoryCarriedSlot.prefab")?.GetComponent<ContainerSlotView>();
            if (lockerSlot == null) throw new InvalidOperationException("Author the Carried slot first.");
            var root = PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var panel = (RectTransform)root.transform.Find("Panel");
                if (panel == null || panel.Find("EquipmentHeading") == null)
                    throw new InvalidOperationException("Transfer requires the current carried-equipment Inventory layout.");
                var screen = root.GetComponent<InventoryScreenView>();
                foreach (var old in new[] { panel.Find("LockerContents"), panel.Find("ItemPopover/TransferStack"), panel.Find("ItemPopover/TransferNote") })
                    if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

                const float lockerWidth = 434;
                var column = Rect("LockerContents", panel, 36, 102, lockerWidth, 488);
                Label("LockerHeading", column, "CONTENTS", 23, Text, 0, 0, 280, 36);
                StyleVitalsSurface(Rect("LockerCountBacking", column, lockerWidth - 101, 0, 101, 35), ShipboardUiColors.PanelInk, VitalsInsetRule, 10);
                var count = Label("LockerCapacity", column, "", 21, Muted, lockerWidth - 96, 0, 81, 35);
                count.alignment = TextAlignmentOptions.MidlineRight;
                var empty = Label("LockerEmpty", column, "Container empty", 19, Muted, 0, 254, lockerWidth, 32);

                var scroll = Rect("LockerScroll", column, 0, 54, lockerWidth, 434).gameObject.AddComponent<ScrollRect>();
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 44;
                scroll.gameObject.AddComponent<LayoutElement>();
                var viewport = Rect("Viewport", scroll.transform, 0, 0, lockerWidth, 434);
                Fill(viewport);
                viewport.gameObject.AddComponent<RectMask2D>();
                var slots = Rect("Slots", viewport, 0, 0, lockerWidth, 434);
                slots.anchorMax = Vector2.one;
                slots.sizeDelta = new Vector2(0, 434);
                var grid = slots.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(104, 104);
                grid.spacing = new Vector2(6, 6);
                var fit = slots.gameObject.AddComponent<ContentSizeFitter>();
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.viewport = viewport;
                scroll.content = slots;
                var locker = column.gameObject.AddComponent<ContainerView>();
                Set(locker, "slotViewPrefab", lockerSlot);
                Set(locker, "slotsParent", slots);
                Set(locker, "gridLayoutGroup", grid);
                Set(locker, "scrollRect", scroll);
                Number(locker, "columns", 4);
                var settings = new SerializedObject(locker);
                settings.FindProperty("maximumViewportHeight").floatValue = 434;
                settings.ApplyModifiedPropertiesWithoutUndo();

                var details = panel.Find("ItemPopover");
                var face = Place(details, "TransferPopoverFace", -16, -16, 462, 419);
                StyleVitalsSurface(face, new Color32(20, 35, 44, 255), VitalsInsetRule, 16);
                face.GetComponent<Image>().raycastTarget = true;
                face.SetAsFirstSibling();
                var action = Button("TransferStack", details, "Take to Carried", 0, 347, 430, 40);
                action.navigation = new Navigation { mode = Navigation.Mode.None };
                var label = action.GetComponentInChildren<TMP_Text>(true);
                label.fontSize = 21;
                var arrow = Label("Direction", action.transform, "→", 21, Text, 388, 6, 27, 28);
                arrow.alignment = TextAlignmentOptions.MidlineRight;
                var note = panel.Find("TransferNote")?.GetComponent<TMP_Text>() ??
                    Label("TransferNote", panel, "", 19, Muted, 36, 607, 1130, 32);
                var title = panel.Find("Title").GetComponent<TMP_Text>();
                title.overflowMode = TextOverflowModes.Ellipsis;
                title.textWrappingMode = TextWrappingModes.NoWrap;

                Set(screen, "lockerView", locker);
                Set(screen, "lockerCapacity", count);
                Set(screen, "lockerEmpty", empty);
                Set(screen, "transferTitle", title);
                Set(screen, "transferPanel", panel);
                Set(screen, "transferDimmer", root.transform.Find("WorldDimmer").GetComponent<UnityEngine.UI.Image>());
                Set(screen, "transferAction", action);
                Set(screen, "transferLabel", label);
                Set(screen, "transferDirection", arrow);
                Set(screen, "transferNote", note);
                Array(screen, "transferCarriedLayout", new[] { "CarriedHeading", "CarriedScroll", "Capacity", "CapacityBacking" }
                    .Select(name => panel.Find(name)).ToArray());
                Array(screen, "transferEquipmentLayout", new[] { "EquipmentHeading", "WeaponSlot", "SuitSlot" }
                    .Select(name => panel.Find(name)).ToArray());
                Array(screen, "transferShow", new[] { column.gameObject, note.gameObject, face.gameObject }
                    .Concat(new[] { "HeaderFace", "HeaderDivider", "Title", "Back", "VitalsSurface" }
                        .Select(name => panel.Find(name).gameObject)).ToArray());
                Array(screen, "transferHide", new[] { "EquipmentFace", "EquipmentRule", "EquipmentDivider", "CarriedRule", "DetailsHeading",
                        "DetailsRule", "InspectorDivider", "Context", "QuickUseRule", "QuickUse" }
                    .Select(name => panel.Find(name)).Where(element => element != null).Select(element => element.gameObject)
                    .Concat(new[] { details.Find("Replacement").gameObject }).ToArray());
                column.gameObject.SetActive(false);
                face.gameObject.SetActive(false);
                action.gameObject.SetActive(false);
                note.gameObject.SetActive(false);
                locker.ValidateAuthoring();
                ApplyTransferPolish(screen, panel);
                PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void PolishTransfer()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring.");
            var root = PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                ApplyTransferPolish(root.GetComponent<InventoryScreenView>(), root.transform.Find("Panel"));
                PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ApplyTransferPolish(InventoryScreenView screen, Transform panel)
        {
            var details = panel.Find("ItemPopover");
            var face = Place(details, "TransferPopoverFace", -12, -12, 454, 243);
            var art = details.Find("ArtField");
            var capacity = panel.Find("Capacity").GetComponent<TMP_Text>();
            var lockerCount = panel.Find("LockerContents/LockerCapacity").GetComponent<TMP_Text>();
            lockerCount.fontStyle = capacity.fontStyle;
            lockerCount.characterSpacing = capacity.characterSpacing;
            Place(panel, "TransferNote", 36, 628, 1130, 24);
            Place(details, "TransferStack", 0, 179, 430, 40);

            var serialized = new SerializedObject(screen);
            serialized.FindProperty("transferPopoverFace").objectReferenceValue = face;
            var layout = serialized.FindProperty("transferPopoverLayout");
            var placements = new (Transform target, Rect rect)[]
            {
                (details.Find("ItemTitle"), new Rect(0, 0, 319, 42)),
                (details.Find("Metadata"), new Rect(0, 43, 319, 28)),
                (art, new Rect(334, 0, 96, 82)),
                (details.Find("ItemArt"), new Rect(342, 4, 80, 74)),
                (details.Find("Description"), new Rect(0, 93, 430, 73)),
                (details.Find("ActionDivider"), new Rect(0, 169, 430, 1))
            };
            layout.arraySize = placements.Length;
            for (int i = 0; i < placements.Length; i++)
            {
                var (target, rect) = placements[i];
                var transform = (RectTransform)target;
                var position = new Vector2(rect.x + rect.width * transform.pivot.x,
                    -rect.y - rect.height * (1 - transform.pivot.y));
                var entry = layout.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("target").objectReferenceValue = target;
                entry.FindPropertyRelative("bounds").rectValue = new Rect(position, rect.size);
            }

            var draftingGrid = art.Find("RegistrationGrid").gameObject;
            var hidden = serialized.FindProperty("transferHide");
            if (!Enumerable.Range(0, hidden.arraySize).Any(i => hidden.GetArrayElementAtIndex(i).objectReferenceValue == draftingGrid))
                hidden.GetArrayElementAtIndex(hidden.arraySize++).objectReferenceValue = draftingGrid;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

    }
}
