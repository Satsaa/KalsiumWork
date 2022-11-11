
namespace Kalsium {

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Muc.Components.Extended;
	using Muc.Data;
	using UnityEngine;
	using Object = UnityEngine.Object;

	public class HookOrders : Singleton<HookOrders> {

		[SerializeField, Tooltip("The item with nulls will dictate default order.\n\nAn item with only the first type will define the default order for objects of that type.\n\nAn item with both will define the order only for that type of hooks of that type of object.")]
		protected List<DoubleValueField<SerializedHookerType, HookType>> orders;

		protected Dictionary<(Type, Type), int> cache;
		protected int defaultOrder;


		public int GetOrder(Type hookerType, Type hookType) {
			if (cache == null) RebuildCache();
			if (cache.TryGetValue((hookerType, hookType), out var res1)) return res1;
			if (cache.TryGetValue((hookerType, null), out var res2)) return res2;
			return defaultOrder;
		}

		public void RebuildCache() {
			cache = new();
			defaultOrder = 0;
			for (int i = 0; i < orders.Count; i++) {
				var order = orders[i];
				if (order.value1.type == null) {
					defaultOrder = i;
				} else {
					cache.Add((order.value1, order.value2), i);
				}
			}
		}

#if UNITY_EDITOR
		public void _EditorRefresh() {
			cache = null;
			foreach (var order in orders) {
				if (order.value2 != null) {
					order.value2.hookerType = order.value1;
				}
			}
		}
#endif


		[Serializable]
		public class SerializedHookerType : SerializedType {

			public override IEnumerable<Type> GetValidTypes() {
				return AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(v => v.GetTypes())
					.Where(v
						=> (v.IsClass || v.IsValueType)
						&& !v.IsAbstract
						&& v.GetInterfaces().Any(v => v == typeof(IHook))
					);
			}
		}

		[Serializable]
		public class HookType : SerializedType {

			public Type hookerType;

			public override IEnumerable<Type> GetValidTypes() {
				return AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(v => v.GetTypes())
					.Where(v
						=> v.IsInterface
						&& v.GetInterfaces().Any(v => v == typeof(IHook))
						&& v.GetMethods().Length > 0
						&& hookerType?.GetInterfaces().Contains(v) != false
					);
			}
		}

		[Serializable]
		public class DoubleValueField<T1, T2> {
			public T1 value1;
			public T2 value2;
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
		[CustomPropertyDrawer(typeof(HookOrders.DoubleValueField<,>), true)]
		public class DoubleValueFieldDrawer : PropertyDrawer {

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				return lineHeight;
			}

			private static GUIContent[] contents = new GUIContent[] { new("V1"), new("V2") };
			private static SerializedProperty[] props = new SerializedProperty[2];

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				using (PropertyScope(position, label, property, out label)) {
					props[0] = property.FindPropertyRelative("value1");
					props[1] = property.FindPropertyRelative("value2");

					MultiPropertyField(position, contents, props);
				}
			}

		}
	}
#endif

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
		[CustomEditor(typeof(HookOrders), true)]
		public class HookOrdersEditor : Editor {

			HookOrders t => (HookOrders)target;

			void OnEnable() {
				foreach (HookOrders target in targets) {
					target._EditorRefresh();
				}
			}

			public override void OnInspectorGUI() {
				serializedObject.Update();

				DrawDefaultInspector();

				serializedObject.ApplyModifiedProperties();

				foreach (HookOrders target in targets) {
					target._EditorRefresh();
				}
			}
		}
	}
#endif
}