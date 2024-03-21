using UnityEditor;
using UnityEngine;

namespace FPS.Pool
{
    public class PoolDescriptionWindow
    {
        [MenuItem("FPS/Pool Description")]
        private static void ShowWindow()
        {
            var poolDescription = Resources.Load<PoolDescription>(nameof(PoolDescription));
            poolDescription.RenamePrefabs();
            EditorUtility.OpenPropertyEditor(poolDescription);
        }
    }
}