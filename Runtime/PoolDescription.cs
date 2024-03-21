using UnityEngine;

namespace FPS.Pool
{
    public class PoolDescription : ScriptableObject
    {
        [SerializeField] private PoolableSetup[] poolables = { new() };

        public PoolableSetup[] Setup => poolables;

#if UNITY_EDITOR
        [SerializeField] private bool renamePoolables;

        [ContextMenu(nameof(RenamePrefabs))]
        private void ForceRename()
        {
            renamePoolables = true;
            RenamePrefabs();
        }

        public void RenamePrefabs()
        {
            if (!renamePoolables)
                return;

            foreach (PoolableSetup description in poolables)
            {
                if (description.Prefab == null)
                    continue;

                string newName = description.Prefab.gameObject.name;

                if (newName.Contains(')'))
                    newName = newName.Split(')')[^1];
                newName = $"({description.Prefab.Type.GetType().Name}){newName}";

                string path = UnityEditor.AssetDatabase.GetAssetPath(description.Prefab);
                UnityEditor.AssetDatabase.RenameAsset(path, newName);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
#endif
    }
}