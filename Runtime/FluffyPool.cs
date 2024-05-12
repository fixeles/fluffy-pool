using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPS.Pool
{
    public static class FluffyPool
    {
        private static readonly HashSet<Component> ActivePoolablesByType = new();
        private static readonly Dictionary<Component, string> ActivePoolablesByString = new();

        private static readonly Dictionary<Type, Component> PrefabsByType = new();
        private static readonly Dictionary<string, Component> PrefabsByString = new();
        private static readonly Dictionary<Type, Transform> ParentsByType = new();
        private static readonly Dictionary<string, Transform> ParentsByString = new();

        private static readonly Dictionary<Type, HashSet<Component>> InactivePoolablesByType = new();
        private static readonly Dictionary<string, HashSet<Component>> InactivePoolablesByString = new();

        public static T Get<T>() where T : Component
        {
            Type type = typeof(T);
            if (!InactivePoolablesByType.TryGetValue(type, out var pool))
                throw new Exception($"FluffyPool: Pool with type {type} does not exist");

            Component poolable;
            if (pool.Count > 0)
            {
                poolable = pool.First();
                pool.Remove(poolable);
                poolable.gameObject.SetActive(true);
            }
            else
                poolable = CreateNew(type);

            ActivePoolablesByType.Add(poolable);
            return poolable as T;
        }

        public static T Get<T>(string key) where T : Component
        {
            if (!InactivePoolablesByString.TryGetValue(key, out var pool))
                throw new Exception($"FluffyPool: Pool with key \"{key}\" does not exist");

            Component poolable;
            if (pool.Count > 0)
            {
                poolable = pool.First();
                pool.Remove(poolable);
                poolable.gameObject.SetActive(true);
            }
            else
                poolable = CreateNew(key);

            ActivePoolablesByString.Add(poolable, key);
            if (poolable is T component)
                return component;

            throw new Exception($"FluffyPool: Poolable with key {key} is not {typeof(T)} ");
        }

        public static void Return(Component poolable)
        {
            if (ActivePoolablesByString.ContainsKey(poolable))
            {
                string key = ActivePoolablesByString[poolable];
                InactivePoolablesByString[key].Add(poolable);
                ActivePoolablesByString.Remove(poolable);
                poolable.transform.SetParent(ParentsByString[key]);
                poolable.gameObject.SetActive(false);
            }
            else if (ActivePoolablesByType.Contains(poolable))
            {
                Type type = poolable.GetType();
                InactivePoolablesByType[type].Add(poolable);
                ActivePoolablesByType.Remove(poolable);
                poolable.transform.SetParent(ParentsByType[type]);
                poolable.gameObject.SetActive(false);
            }
            else
                Debug.LogWarning($"FluffyPool: Can't return \"{poolable.gameObject.name}\", because it hasn't been registered before");
        }

        public static void Init(CancellationToken token)
        {
            var poolDescription = Resources.Load<PoolDescription>(nameof(PoolDescription));
#if UNITY_EDITOR
            poolDescription.RenamePrefabs();
#endif
            CreateParents(poolDescription.Setup);

            foreach (var setup in poolDescription.Setup)
            {
                switch (setup.PrewarmType)
                {
                    case PoolableSetup.PoolablePrewarmType.Lazy:
                        PrewarmPoolablesAsync(setup, token);
                        break;

                    case PoolableSetup.PoolablePrewarmType.OneFrame:
                        PrewarmPoolables(setup);
                        break;
                }
            }
            Resources.UnloadAsset(poolDescription);
        }

        private static void PrewarmPoolables(PoolableSetup setup)
        {
            Component newPoolable;
            if (string.IsNullOrEmpty(setup.Key))
            {
                Type type = setup.Prefab.Type.GetType();
                for (var i = 0; i < setup.Count; i++)
                {
                    newPoolable = CreateNew(type);
                    newPoolable.gameObject.SetActive(false);
                    InactivePoolablesByType[type].Add(newPoolable);
                }
            }
            else
                for (var i = 0; i < setup.Count; i++)
                {
                    newPoolable = CreateNew(setup.Key);
                    newPoolable.gameObject.SetActive(false);
                    InactivePoolablesByString[setup.Key].Add(newPoolable);
                }
        }

        private static async void PrewarmPoolablesAsync(PoolableSetup setup, CancellationToken token)
        {
            Component newPoolable;
            if (string.IsNullOrEmpty(setup.Key))
            {
                Type type = setup.Prefab.Type.GetType();
                for (var i = 0; i < setup.Count; i++)
                {
                    newPoolable = CreateNew(type);
                    newPoolable.gameObject.SetActive(false);
                    InactivePoolablesByType[type].Add(newPoolable);
                    await Task.Yield();
                    if (token.IsCancellationRequested)
                        return;
                }
            }
            else
                for (var i = 0; i < setup.Count; i++)
                {
                    newPoolable = CreateNew(setup.Key);
                    newPoolable.gameObject.SetActive(false);
                    InactivePoolablesByString[setup.Key].Add(newPoolable);
                    await Task.Yield();
                    if (token.IsCancellationRequested)
                        return;
                }
        }

        private static Component CreateNew(Type type)
        {
            var newPoolable = Object.Instantiate(PrefabsByType[type], ParentsByType[type]);
            return newPoolable;
        }

        private static Component CreateNew(string key)
        {
            var newPoolable = Object.Instantiate(PrefabsByString[key], ParentsByString[key]);
            return newPoolable;
        }

        private static void CreateParents(IEnumerable<PoolableSetup> poolablesSetup)
        {
            var root = new GameObject
            {
                name = nameof(FluffyPool)
            };
            var rootTransform = root.transform;

            foreach (var setup in poolablesSetup)
            {
                Type type = setup.Prefab.Type.GetType();
                if (string.IsNullOrEmpty(setup.Key))
                {
                    InactivePoolablesByType.Add(type, new HashSet<Component>(setup.Count * 2));
                    var parent = new GameObject($"<{type.Name}>").transform;
                    parent.SetParent(rootTransform);
                    ParentsByType.Add(type, parent);

                    if (!PrefabsByType.TryAdd(type, setup.Prefab.Type))
                        Debug.LogError($"FluffyPool: Type {type} already exists and additional key is empty");
                }
                else
                {
                    InactivePoolablesByString.Add(setup.Key, new HashSet<Component>(setup.Count * 2));
                    var parent = new GameObject($"\"{setup.Key}\"<{type.Name}>").transform;
                    parent.SetParent(rootTransform);
                    ParentsByString.Add(setup.Key, parent);

                    if (!PrefabsByString.TryAdd(setup.Key, setup.Prefab.Type))
                        Debug.LogError($"FluffyPool: Key {setup.Key} already exists");
                }
            }
        }
    }
}