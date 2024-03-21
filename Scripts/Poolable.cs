using UnityEngine;

namespace FPS.Pool
{
    public class Poolable : MonoBehaviour
    {
        [field: SerializeField, ComponentSelector] public Component Type { get; private set; }

        public void Release()
        {
            FluffyPool.Return(Type);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            Type ??= transform;
        }
  #endif
    }
}