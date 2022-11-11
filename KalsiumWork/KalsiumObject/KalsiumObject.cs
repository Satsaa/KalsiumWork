
namespace Kalsium {

	using System.Linq;
	using System.Text.RegularExpressions;
	using Serialization;
	using UnityEngine;

	[RefToken]
	public abstract class KalsiumObject : ScriptableObject, IIdentifiable {

		[Tooltip("String identifier of this KalsiumObject. (\"Unit_Oracle\")")]
		public string identifier;
		string IIdentifiable.identifier => identifier;

		private static Regex removeData = new(@"Data$");

		[field: Tooltip("Source instance."), SerializeField]
		public KalsiumObject source { get; protected set; }
		public bool isSource => source == null;

		[field: SerializeField]
		public bool removed { get; protected set; }

		protected virtual void OnCreate() { }
		protected virtual void OnRemove() { }

		/// <summary>
		/// Modifier is created or the scripts are reloaded.
		/// Also when the Modifier is removed but with add = false.
		/// Conditionally add or remove non-persistent things here.
		/// </summary>
		/// <param name="add"></param>
		protected virtual void OnConfigureNonpersistent(bool add) { }

#if UNITY_EDITOR
		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnReloadScripts() {
			if (Application.isPlaying && Game.game) {
				foreach (var dobj in Game.game.objects.Get<KalsiumObject>().Where(v => v)) {
					dobj.OnConfigureNonpersistent(true);
				}
			}
		}
#endif

	}

#if UNITY_EDITOR
	namespace Editors {

		using System;
		using System.Collections.Generic;
		using System.Linq;
		using Muc.Data;
		using UnityEditor;
		using UnityEditorInternal;
		using static Muc.Editor.EditorUtil;
		using static Muc.Editor.PropertyUtil;

		[CanEditMultipleObjects]
		[CustomEditor(typeof(KalsiumObject), true)]
		public class KalsiumObjectEditor : Editor {

			KalsiumObject t => (KalsiumObject)target;

			static string[] excludes = { script };
			static List<bool> expands = new() { true };
			ReorderableList list;
			Type listType;

			public override void OnInspectorGUI() {
				serializedObject.Update();

				Type decType = null;
				int expandI = 0;

				var property = serializedObject.GetIterator();
				var expanded = true;
				while (property.NextVisible(expanded)) {
					expanded = false;
					if (excludes.Contains(property.name)) continue;
					var fi = GetFieldInfo(property);
					var prev = decType;
					if (decType != (decType = fi.DeclaringType)) {
						if (expandI != 0) {
							EditorGUI.indentLevel--;
						}
						expands[expandI] = EditorGUILayout.Foldout(expands[expandI], ObjectNames.NicifyVariableName(decType.Name), true, EditorStyles.foldoutHeader);
						EditorGUI.indentLevel++;
						expandI++;
						if (expands.Count <= expandI) {
							expands.Add(true);
						}
					}
					if (expands[expandI - 1]) {
						EditorGUILayout.PropertyField(property, true);
					}
				}
				if (expandI != 0) {
					EditorGUI.indentLevel--;
				}

				serializedObject.ApplyModifiedProperties();
			}

			private static void OnSelect(SerializedProperty property, Type type) {
				var values = GetValues<SerializedType>(property);
				Undo.RecordObjects(property.serializedObject.targetObjects, $"Set {property.name}");
				foreach (var value in values) value.type = type;
				foreach (var target in property.serializedObject.targetObjects) EditorUtility.SetDirty(target);
				property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
			}


			private static List<Type> createTypes;

			private static IEnumerable<Type> GetCompatibleTypes(Type dataType, Type createType) {
				createTypes ??= AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(v => v.GetTypes())
					.Where(v =>
						v.IsClass
						&& !v.IsAbstract
						&& (v == typeof(KalsiumObject) || typeof(KalsiumObject).IsAssignableFrom(v))
					).ToList();
				return createTypes.Where(v => v == createType || createType.IsAssignableFrom(v));
			}

		}
	}
#endif
}