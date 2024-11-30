using UnityEngine;

namespace FPS.Pool
{
	public interface IObjectPool
	{
		T Get<T>() where T : Component;
		T Get<T>(string key) where T : Component;
		public void Return(Component poolable);
	}
}