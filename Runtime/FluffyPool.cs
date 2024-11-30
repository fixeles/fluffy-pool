using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FPS.Pool
{
	public class FluffyPool : IObjectPool
	{
		private readonly IObjectResolver _resolver;
		private readonly HashSet<Component> _activePoolablesByType = new();
		private readonly Dictionary<Component, string> _activePoolablesByString = new();

		private readonly Dictionary<Type, Component> _prefabsByType = new();
		private readonly Dictionary<string, Component> _prefabsByString = new();
		private readonly Dictionary<Type, Transform> _parentsByType = new();
		private readonly Dictionary<string, Transform> _parentsByString = new();

		private readonly Dictionary<Type, HashSet<Component>> _inactivePoolablesByType = new();
		private readonly Dictionary<string, HashSet<Component>> _inactivePoolablesByString = new();

		[Inject]
		public FluffyPool(IObjectResolver resolver)
		{
			_resolver = resolver;
		}

		public T Get<T>() where T : Component
		{
			Type type = typeof(T);
			if (!_inactivePoolablesByType.TryGetValue(type, out var pool))
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

			_activePoolablesByType.Add(poolable);
			return poolable as T;
		}

		public T Get<T>(string key) where T : Component
		{
			if (!_inactivePoolablesByString.TryGetValue(key, out var pool))
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

			_activePoolablesByString.Add(poolable, key);
			if (poolable is T component)
				return component;

			throw new Exception($"FluffyPool: Poolable with key {key} is not {typeof(T)} ");
		}

		public void Return(Component poolable)
		{
			if (_activePoolablesByString.ContainsKey(poolable))
			{
				string key = _activePoolablesByString[poolable];
				_inactivePoolablesByString[key].Add(poolable);
				_activePoolablesByString.Remove(poolable);
				poolable.transform.SetParent(_parentsByString[key]);
				poolable.gameObject.SetActive(false);
			}
			else if (_activePoolablesByType.Contains(poolable))
			{
				Type type = poolable.GetType();
				_inactivePoolablesByType[type].Add(poolable);
				_activePoolablesByType.Remove(poolable);
				poolable.transform.SetParent(_parentsByType[type]);
				poolable.gameObject.SetActive(false);
			}
			else
				Debug.LogWarning(
					$"FluffyPool: Can't return \"{poolable.gameObject.name}\", because it hasn't been registered before");
		}

		public void Init(CancellationToken token)
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

		private void PrewarmPoolables(PoolableSetup setup)
		{
			Component newPoolable;
			if (string.IsNullOrEmpty(setup.Key))
			{
				Type type = setup.Prefab.Type.GetType();
				for (var i = 0; i < setup.Count; i++)
				{
					newPoolable = CreateNew(type);
					newPoolable.gameObject.SetActive(false);
					_inactivePoolablesByType[type].Add(newPoolable);
				}
			}
			else
				for (var i = 0; i < setup.Count; i++)
				{
					newPoolable = CreateNew(setup.Key);
					newPoolable.gameObject.SetActive(false);
					_inactivePoolablesByString[setup.Key].Add(newPoolable);
				}
		}

		private async void PrewarmPoolablesAsync(PoolableSetup setup, CancellationToken token)
		{
			Component newPoolable;
			if (string.IsNullOrEmpty(setup.Key))
			{
				Type type = setup.Prefab.Type.GetType();
				for (var i = 0; i < setup.Count; i++)
				{
					newPoolable = CreateNew(type);
					newPoolable.gameObject.SetActive(false);
					_inactivePoolablesByType[type].Add(newPoolable);
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
					_inactivePoolablesByString[setup.Key].Add(newPoolable);
					await Task.Yield();
					if (token.IsCancellationRequested)
						return;
				}
		}

		private Component CreateNew(Type type)
		{
			return _resolver.Instantiate(_prefabsByType[type], _parentsByType[type]);
		}

		private Component CreateNew(string key)
		{
			return _resolver.Instantiate(_prefabsByString[key], _parentsByString[key]);
		}

		private void CreateParents(IEnumerable<PoolableSetup> poolablesSetup)
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
					_inactivePoolablesByType.Add(type, new HashSet<Component>(setup.Count * 2));
					var parent = new GameObject($"<{type.Name}>").transform;
					parent.SetParent(rootTransform);
					_parentsByType.Add(type, parent);

					if (!_prefabsByType.TryAdd(type, setup.Prefab.Type))
						Debug.LogError($"FluffyPool: Type {type} already exists and additional key is empty");
				}
				else
				{
					_inactivePoolablesByString.Add(setup.Key, new HashSet<Component>(setup.Count * 2));
					var parent = new GameObject($"\"{setup.Key}\"<{type.Name}>").transform;
					parent.SetParent(rootTransform);
					_parentsByString.Add(setup.Key, parent);

					if (!_prefabsByString.TryAdd(setup.Key, setup.Prefab.Type))
						Debug.LogError($"FluffyPool: Key {setup.Key} already exists");
				}
			}
		}
	}
}