using UnityEditor;

namespace FPS.Pool.Editor
{
    public class PoolDescriptionWindow
    {
        [MenuItem("FPS/Pool Description")]
        private static void ShowWindow()
        {
#if CORE_DEV
            var description = Utils.Editor.GetAllInstances<PoolDescription>()[0];
#else
            var description = UnityEngine.Resources.Load<PoolDescription>(nameof(PoolDescription));
#endif
            description.RenamePrefabs();
            EditorUtility.OpenPropertyEditor(description);
        }
    }
}