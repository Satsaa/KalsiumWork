
namespace Kalsium {

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Muc.Data;
	using UnityEngine;
	using Object = UnityEngine.Object;

	[Serializable]
	public class Library : ScriptableObject {

		[SerializeField] List<KalsiumObject> sources;
		public IReadOnlyDictionary<string, KalsiumObject> dict => _dict == null ? _dict ??= BuildDict() : _dict.Count == sources.Count ? _dict : _dict ??= BuildDict();
		private SerializedDictionary<string, KalsiumObject> _dict;

		private SerializedDictionary<string, KalsiumObject> BuildDict() {
			var res = new SerializedDictionary<string, KalsiumObject>();
			sources.RemoveAll(v => v == null);
			foreach (var source in sources) {
				Debug.Assert(source.identifier != "", source);
				Debug.Assert(!res.ContainsKey(source.identifier), source);
				res.Add(source.identifier, source);
			}
			return res;
		}

		public bool TryGetById(string id, out KalsiumObject result) => TryGetById<KalsiumObject>(id, out result);
		public bool TryGetById<T>(string id, out T result) where T : KalsiumObject {
			if (dict.TryGetValue(id, out var res) && res is T _result) {
				result = _result;
				return true;
			}
			result = default;
			return false;
		}

		public KalsiumObject GetById(string id) => GetById<KalsiumObject>(id);
		public T GetById<T>(string id) where T : KalsiumObject {
			return (T)dict[id];
		}

		public IEnumerable<KalsiumObject> GetByType(Type type) {
			foreach (var v in dict.Values) {
				if (type.IsAssignableFrom(v.GetType())) yield return v;
			}
		}
		public IEnumerable<T> GetByType<T>() where T : KalsiumObject {
			foreach (var v in dict.Values) {
				if (v is T r) yield return r;
			}
		}

		public void SetSources(List<KalsiumObject> sources) {
			this.sources = sources;
		}

	}

#if UNITY_EDITOR
	namespace Editors {

		using System;
		using System.Collections.Generic;
		using System.Linq;
		using UnityEditor;
		using UnityEngine;
		using static Muc.Editor.EditorUtil;
		using static Muc.Editor.PropertyUtil;
		using Object = UnityEngine.Object;

		[CanEditMultipleObjects]
		[CustomEditor(typeof(Library), true)]
		public class LibraryEditor : Editor {

			Library t => (Library)target;

			public override void OnInspectorGUI() {
				serializedObject.Update();

				DrawDefaultInspector();

				if (GUILayout.Button("Add All Sources In Project")) {
					var datas = new List<KalsiumObject>();
					var guids = AssetDatabase.FindAssets($"t:{nameof(KalsiumObject)}");
					foreach (var guid in guids) {
						var path = AssetDatabase.GUIDToAssetPath(guid);
						var data = AssetDatabase.LoadAssetAtPath<KalsiumObject>(path);
						datas.Add(data);
					}
					t.SetSources(datas);
				}

				serializedObject.ApplyModifiedProperties();
			}
		}
	}
#endif
}