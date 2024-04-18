using UnityEditor;

namespace FPS.Pool.Editor
{
    public class PoolDescriptionWindow
    {
        [MenuItem("FPS/Pool Description")]
        private static void ShowWindow()
        {
            var poolDescription = Utils.Editor.GetAllInstances<PoolDescription>()[0];
            poolDescription.RenamePrefabs();
            EditorUtility.OpenPropertyEditor(poolDescription);
        }
    }
}