using System;
using UnityEngine;

namespace FPS.Pool
{
    [Serializable]
    public class PoolableSetup
    {
        [field: SerializeField] public Poolable Prefab { get; private set; }
        [field: SerializeField, Min(1)] public int Count { get; private set; } = 1;
        [field: SerializeField] public string Key { get; private set; }
        [field: SerializeField] public PoolablePrewarmType PrewarmType { get; private set; }

        public enum PoolablePrewarmType
        {
            Lazy,
            OneFrame
        }
    }
}