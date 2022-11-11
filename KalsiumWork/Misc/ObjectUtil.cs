
namespace Kalsium {

#if UNITY_EDITOR
	using UnityEditor;
#endif

	using System;
	using UnityEngine;
	using Object = UnityEngine.Object;
	using System.Reflection;

	public static class ObjectUtil {

		/// <summary> Uses Destroy or DestroyImmediate depending on the player state. </summary>
		public static void Destroy(Object obj) {
#if UNITY_EDITOR
			if (!Application.isPlaying)
				EditorApplication.delayCall += () => { if (obj && !Application.isPlaying) Object.DestroyImmediate(obj); };
			else
#endif
				Object.Destroy(obj);
		}

		/// <summary> Instantiates gameObject without triggering it's Awake and disabling it. </summary>
		/// <param name="wasActive">Whether the cloned object was active.</param>
		public static GameObject UnawokenGameObject(GameObject gameObject) {
			var wasActive = gameObject.activeSelf;
			if (wasActive) gameObject.SetActive(false);
			var go = ObjectUtil.Instantiate(gameObject);
			Debug.Assert(!go.activeSelf);
			if (wasActive) gameObject.SetActive(true);
			return go;
		}

		/// <summary>
		/// Instatiates a GameObject and maintains prefab links if appropriate.
		/// </summary>
		public static GameObject Instantiate(GameObject gameObject) {
#if UNITY_EDITOR
			if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(gameObject))
				return (GameObject)PrefabUtility.InstantiatePrefab(gameObject);
#endif
			return GameObject.Instantiate(gameObject);
		}

		/// <summary>
		/// Instatiates a GameObject and maintains prefab links if appropriate.
		/// </summary>
		public static GameObject Instantiate(GameObject gameObject, Transform parent) {
#if UNITY_EDITOR
			if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(gameObject))
				return (GameObject)PrefabUtility.InstantiatePrefab(gameObject, parent);
#endif
			return GameObject.Instantiate(gameObject, parent);
		}

		/// <summary>
		/// Finds an Object based on an instance id. Instance ids can be obtained with <c>Object.GetInstanceId()</c>.
		/// </summary>
		/// <param name="instanceId">Integer returned by Object.GetInstanceId().</param>
		/// <returns>Returns the Object with the provided instance id or null if none was found.</returns>
		public static UnityEngine.Object FindObjectFromInstanceID(int instanceId) => findObjectFromInstanceID(instanceId);
		private static Func<int, UnityEngine.Object> findObjectFromInstanceID = null;

		static ObjectUtil() {
			var methodInfo = typeof(UnityEngine.Object).GetMethod("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static);
			findObjectFromInstanceID = (Func<int, UnityEngine.Object>)Delegate.CreateDelegate(typeof(Func<int, UnityEngine.Object>), methodInfo);
		}

	}

}