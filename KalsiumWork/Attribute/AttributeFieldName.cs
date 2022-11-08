
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Reflection;

[Serializable]
public class AttributeFieldName {

	private static IEnumerable<string> cache;

	[field: SerializeField] public string attributeName { get; private set; }

	public virtual IEnumerable<string> GetFieldNames() {
		if (cache == null) {
			var list = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(v => v.GetTypes())
					.Where(v
						=> (v.IsClass || (v.IsValueType && !v.IsPrimitive))
						&& (v.IsSubclassOf(typeof(Object)) || v.CustomAttributes.Any(v => v.AttributeType == typeof(SerializableAttribute)))
					).SelectMany(v => v.GetFields()
						.Where(f => typeof(Attribute).IsAssignableFrom(f.FieldType))
						.Select(v => v.Name)).Distinct().ToList();
			list.Sort();
			cache = list;

		}
		return cache;
	}

}

[Serializable]
public class AttributeFieldName<T> : AttributeFieldName {

	protected static IEnumerable<string> cache;

	public override IEnumerable<string> GetFieldNames() {
		if (cache == null) {
			var list = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(v => v.GetTypes())
					.Where(v
						=> (v.IsClass || (v.IsValueType && !v.IsPrimitive))
						&& (v.IsSubclassOf(typeof(Object)) || v.CustomAttributes.Any(v => v.AttributeType == typeof(SerializableAttribute)))
					).SelectMany(v => v.GetFields()
						.Where(f => typeof(Attribute<T>).IsAssignableFrom(f.FieldType))
						.Select(v => v.Name)).Distinct().ToList();
			list.Sort();
			cache = list;

		}
		return cache;
	}

}

[Serializable]
public class NumericAttributeFieldName : AttributeFieldName {

	protected static IEnumerable<string> cache;

	public override IEnumerable<string> GetFieldNames() {
		if (cache == null) {
			var list = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(v => v.GetTypes())
					.Where(v
						=> (v.IsClass || (v.IsValueType && !v.IsPrimitive))
						&& (v.IsSubclassOf(typeof(Object)) || v.CustomAttributes.Any(v => v.AttributeType == typeof(SerializableAttribute)))
					).SelectMany(v => v.GetFields()
						.Where(f => typeof(Attribute<float>).IsAssignableFrom(f.FieldType) || typeof(Attribute<int>).IsAssignableFrom(f.FieldType))
						.Select(v => v.Name)).ToList();
			list.Sort();
			cache = list;

		}
		return cache;
	}

}



#if UNITY_EDITOR
namespace Editors {

	using System;
	using System.Linq;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using Object = UnityEngine.Object;
	using static Muc.Editor.PropertyUtil;
	using static Muc.Editor.EditorUtil;

	[CanEditMultipleObjects]
	[CustomPropertyDrawer(typeof(AttributeFieldName), true)]
	public class AttributeFieldNameDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			var noLabel = label.text is "" && label.image is null;

			using (PropertyScope(position, label, property, out label)) {

				const int dropDownWidth = 20;

				var propertyPos = position;
				propertyPos.width -= dropDownWidth;

				var attributeName = property.FindPropertyRelative(GetBackingFieldName("attributeName"));
				EditorGUI.PropertyField(propertyPos, attributeName, label);

				var dropDownPos = position;
				dropDownPos.xMin += position.width - dropDownWidth;

				if (EditorGUI.DropdownButton(dropDownPos, GUIContent.none, FocusType.Keyboard)) {
					var fnVal = GetFirstValue<AttributeFieldName>(property);
					var names = fnVal.GetFieldNames();
					var menu = new GenericMenu();
					foreach (var name in names) {
						menu.AddItem(new GUIContent(name), attributeName.stringValue == name, () => OnSelect(attributeName, name));
					}
					menu.DropDown(dropDownPos);
				}

			}
		}


		protected void OnSelect(SerializedProperty property, string value) {
			property.stringValue = value;
			property.serializedObject.ApplyModifiedProperties();
		}

	}
}
#endif