using UnityEngine;

namespace SOLITUDE.GameplayTags
{
    [CreateAssetMenu(menuName = "SOLITUDE/Gameplay Tags/Definition")]
    public sealed class GameplayTagDefinition : ScriptableObject
    {
        [SerializeField] private string tagId;
        [SerializeField] private string segment;
        [Tooltip("Explicit root status distinguishes a root from a child whose parent reference was lost.")]
        [SerializeField] private bool isRoot = true;
        [SerializeField] private GameplayTagDefinition parent;

        public string TagId => tagId;
        public string Segment => segment;
        public bool IsRoot => isRoot;
        public GameplayTagDefinition Parent => parent;

        public void Configure(string nameSegment, GameplayTagDefinition parentDefinition = null)
        {
            segment = nameSegment; parent = parentDefinition; isRoot = parentDefinition == null;
            AssignMissingIdentity();
        }

        private void OnValidate() => AssignMissingIdentity();
        private void AssignMissingIdentity()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(tagId)) return;
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(path)) tagId = UnityEditor.AssetDatabase.AssetPathToGUID(path);
#endif
        }
    }
}
