using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SOLITUDE.Editor.RoomAuthoring
{
    public static class RoomFloorPanelAssetStore
    {
        public const string Root = "Assets/_Project/Features/World/RoomAuthoring/GeneratedFloorPanels";
        // Explicit isolated-test injection points; normal callers leave both null.
        public static string VerificationRoot
        {
            get => SessionState.GetString("SOLITUDE.PanelVerificationRoot", "") is var value && value.Length != 0 ? value : null;
            set => SessionState.SetString("SOLITUDE.PanelVerificationRoot", value ?? "");
        }
        public static Action<string> PublicationCheckpoint { get; set; }
        public static int PublicationCount { get; private set; }
        private static string Store => VerificationRoot ?? Root;
        public static int RetainedRevisionCount => !AssetDatabase.IsValidFolder(Store) ? 0 : AssetDatabase.FindAssets("t:RoomFloorPanelOutput", new[] { Store })
            .Length;

        private static string Digest(Action<BinaryWriter> write)
        {
            using var bytes = new MemoryStream();
            using (var writer = new BinaryWriter(bytes, System.Text.Encoding.UTF8, true)) write(writer);
            using var hash = SHA256.Create();
            bytes.Position = 0;
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        private static void Pixels(BinaryWriter writer, Color32[] pixels)
        {
            // Preserve the persisted RGBA byte stream without four BinaryWriter calls per pixel.
            var rgba = new byte[checked(pixels.Length * 4)];
            for (var i = 0; i < pixels.Length; i++)
            {
                var offset = i * 4; var pixel = pixels[i];
                rgba[offset] = pixel.r; rgba[offset + 1] = pixel.g;
                rgba[offset + 2] = pixel.b; rgba[offset + 3] = pixel.a;
            }
            writer.Write(rgba);
        }
        public static string ContentHash(RoomFloorPanelOutput output) => Digest(w =>
        {
            w.Write(output.OwnerId); w.Write(output.Version);
            foreach (var c in output.Chunks)
            {
                w.Write(c.Origin.x); w.Write(c.Origin.y); w.Write(c.Texture.width);
                Pixels(w, c.Texture.GetPixels32());
            }
        });
        private static string ContentHash(string owner, int version, RoomFloorPanelRaster raster) => Digest(w =>
        {
            w.Write(owner); w.Write(version);
            foreach (var c in raster.Chunks)
            { w.Write(c.Origin.x); w.Write(c.Origin.y); w.Write(c.Size); Pixels(w, c.CopyPixels()); }
        });
        private static string OwnerKey(string owner) => Digest(w => w.Write(owner));
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path))))
                throw new IOException("Could not create managed panel folder.");
        }
        public static bool IsVerified(RoomFloorPanelOutput output) => IsVerified(output, null);

        // Only call with a hash computed in this same assessment; never cache texture bytes across edits.
        internal static bool IsVerified(RoomFloorPanelOutput output, string currentContentHash)
        {
            if (output == null || string.IsNullOrEmpty(output.OwnerId) || output.Version != RoomFloorPanelSettings.CurrentVersion) return false;
            try
            {
                var path = AssetDatabase.GetAssetPath(output);
                if (!path.StartsWith(Store + "/", StringComparison.Ordinal) ||
                    Path.GetFileNameWithoutExtension(path) != OwnerKey(output.OwnerId) + "-" + output.ContentId) return false;
                foreach (var c in output.Chunks)
                    if (c.Texture == null || c.Sprite == null || c.Texture.format != TextureFormat.RGBA32 ||
                        c.Texture.filterMode != FilterMode.Point || c.Texture.mipmapCount != 1 ||
                        c.Texture.wrapMode != TextureWrapMode.Clamp || c.Sprite.texture != c.Texture ||
                        c.Sprite.pixelsPerUnit != 32 || c.Sprite.pivot != Vector2.zero ||
                        c.Sprite.rect != new Rect(0, 0, c.Texture.width, c.Texture.height) ||
                        AssetDatabase.GetAssetPath(c.Sprite) != path || AssetDatabase.GetAssetPath(c.Texture) != path) return false;
                return output.ContentId == (currentContentHash ?? ContentHash(output));
            }
            catch { return false; }
        }

        public static RoomFloorPanelOutput Prepare(ShipRoomAuthoring room, RoomPlacement placement,
            out RoomGenerationIssue[] issues)
        {
            issues = Array.Empty<RoomGenerationIssue>();
            if (!room.FloorPanels.Enabled || placement.Floor == null || placement.Geometry.Cells.Length == 0) return null;
            var rendering = placement.Rendering;
            if (rendering == null || rendering.DetailOrder <= rendering.FloorOrder || rendering.DetailOrder >= rendering.ArchitectureOrder)
            {
                var invalid = new RoomGenerationIssue(RoomIssueKind.Asset, "FloorPanels/Publication", "Detail sorting must be strictly between base floor and architecture.");
                invalid.LocateInRoom(room.RoomId); issues = new[] { invalid }; return null;
            }
            var domain = RoomFloorPanelDomain.Build(room);
            if (!domain.CanGenerate) return null; // Domain diagnostics already belong to assessment.
            var seams = RoomFloorPanelSeams.Extract(room.RoomId, RoomFloorPanelPartitioner.Partition(domain));
            var raster = RoomFloorPanelRasterizer.Rasterize(room.RoomId, seams, domain.PixelRegions, domain.Settings);
            if (!raster.Success) { issues = raster.Issues.ToArray(); return null; }
            var settings = domain.Settings;
            var id = ContentHash(room.RoomId, settings.Version, raster);
            var path = Store + "/" + OwnerKey(room.RoomId) + "-" + id + ".asset";
            var allocated = new List<Object>(); var created = false;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Panel publication requires Edit Mode.");
                if (!Store.StartsWith("Assets/_Project/", StringComparison.Ordinal) || Store.Contains(".."))
                    throw new InvalidOperationException("Managed resources must remain under Assets/_Project.");
                if (File.Exists(path))
                {
                    var old = AssetDatabase.LoadAssetAtPath<RoomFloorPanelOutput>(path);
                    if (!IsVerified(old)) throw new InvalidOperationException("Existing immutable revision is modified or incomplete; preserve it and inspect its references.");
                    return old;
                }
                EnsureFolder(Store);
                if (!AssetDatabase.IsOpenForEdit(Store)) throw new IOException("Managed panel folder is not writable.");
                var chunks = new List<RoomFloorPanelOutput.Chunk>();
                foreach (var c in raster.Chunks)
                {
                    PublicationCheckpoint?.Invoke("allocation");
                    var texture = new Texture2D(c.Size, c.Size, TextureFormat.RGBA32, false)
                    { name = "Pixels " + c.Origin, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    allocated.Add(texture); texture.SetPixels32(c.CopyPixels()); texture.Apply(false, false);
                    var sprite = Sprite.Create(texture, new Rect(0, 0, c.Size, c.Size), Vector2.zero, 32, 0, SpriteMeshType.FullRect);
                    allocated.Add(sprite); sprite.name = "Seams " + c.Origin;
                    chunks.Add(new RoomFloorPanelOutput.Chunk { Origin = c.Origin, Texture = texture, Sprite = sprite });
                }
                var revision = ScriptableObject.CreateInstance<RoomFloorPanelOutput>(); allocated.Add(revision);
                revision.name = "Floor Panels " + id; revision.Initialize(room.RoomId, settings.Version, id, chunks.ToArray());
                AssetDatabase.CreateAsset(revision, path); created = true;
                foreach (var asset in allocated.Where(a => a != revision)) AssetDatabase.AddObjectToAsset(asset, revision);
                EditorUtility.SetDirty(revision); AssetDatabase.SaveAssetIfDirty(revision);
                PublicationCheckpoint?.Invoke("import");
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                revision = AssetDatabase.LoadAssetAtPath<RoomFloorPanelOutput>(path);
                if (!IsVerified(revision)) throw new IOException("Prepared resource validation failed after import.");
                PublicationCheckpoint?.Invoke("publication");
                PublicationCount++;
                return revision;
            }
            catch (Exception error)
            {
                // No scene reference has been published. Only this transaction's newly created revision can be removed.
                if (created) AssetDatabase.DeleteAsset(path);
                foreach (var asset in allocated) if (asset != null && !EditorUtility.IsPersistent(asset)) Object.DestroyImmediate(asset);
                var issue = new RoomGenerationIssue(RoomIssueKind.Asset, "FloorPanels/Publication", error.Message);
                issue.LocateInRoom(room.RoomId); issues = new[] { issue }; return null;
            }
        }

        // Caller must explicitly consent to ending ALL editor Undo history. Never invoked by Rebuild or import.
        public static bool CleanupUnused(RoomFloorPanelOutput revision, bool endUndoRetention, out string issue)
        {
            issue = null;
            if (!endUndoRetention) { issue = "Recoverable Undo may reference this revision. Explicitly end Undo retention before cleanup."; return false; }
            if (EditorApplication.isPlayingOrWillChangePlaymode || RoomAuthoringOperations.HasActiveEdit || !IsVerified(revision))
            { issue = "Cleanup requires an idle editor and a verified managed revision."; return false; }
            var path = AssetDatabase.GetAssetPath(revision);
            // Check saved scenes, prefabs, assets and packages, including unloaded scenes and binary serialization.
            foreach (var asset in AssetDatabase.GetAllAssetPaths().Where(p => p != path && !AssetDatabase.IsValidFolder(p)))
                if (AssetDatabase.GetDependencies(asset, true).Contains(path))
                { issue = "Retained saved reference: " + asset; return false; }
            Undo.ClearAll(); // Explicit retention boundary: destroyed Undo-only objects must no longer count as loaded references.
            var loadedRoots = Resources.FindObjectsOfTypeAll<GameObject>().Where(g => !EditorUtility.IsPersistent(g)).Cast<Object>().ToArray();
            if (EditorUtility.CollectDependencies(loadedRoots).Any(o => AssetDatabase.GetAssetPath(o) == path))
            { issue = "Retained reference in loaded scene or editor object."; return false; }
            if (!AssetDatabase.DeleteAsset(path)) { issue = "Revision could not be deleted."; return false; }
            return true;
        }
    }
}
