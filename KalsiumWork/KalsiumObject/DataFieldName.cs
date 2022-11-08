
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Muc.Addressables;
using Muc.Data;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class DataFieldName {

	private static IEnumerable<(string, Type, Type)> cache;

	[field: SerializeField] public string attributeName { get; private set; }
	[field: SerializeField] public SerializedType attributeType { get; private set; }

	public virtual IEnumerable<(string, Type, Type)> GetFieldNames() {
		if (cache == null) {
			var list = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(v => v.GetTypes())
					.Where(v
						=> (v.IsClass || (v.IsValueType && !v.IsPrimitive))
						&& v.IsSubclassOf(typeof(KalsiumObject))
					).SelectMany(v => v.GetFields()
						.Select(v => (v.Name, GetAttributeType(v.FieldType), v.FieldType))).Distinct().ToList();
			list.Sort((a, b) => a.Name.CompareTo(b.Name));
			cache = list;

		}
		return cache;
		Type GetAttributeType(Type fieldType) {
			var current = fieldType;
			while (current != null) {
				if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Attribute<>)) {
					return current.GenericTypeArguments[0];
				}
				current = current.BaseType;
			}
			return fieldType;
		}
	}

}

[Serializable]
public class KalsiumObjectFieldName<T> : DataFieldName {

	private static IEnumerable<(string, Type, Type)> cache;

	public override IEnumerable<(string, Type, Type)> GetFieldNames() {
		if (cache == null) {
			var arType = typeof(Object).IsAssignableFrom(typeof(T)) ? typeof(AssetReference<>).MakeGenericType(typeof(T)) : null;
			var crType = typeof(Component).IsAssignableFrom(typeof(T)) ? typeof(ComponentReference<>).MakeGenericType(typeof(T)) : null;
			var list = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(v => v.GetTypes())
					.Where(v
						=> (v.IsClass || (v.IsValueType && !v.IsPrimitive))
						&& v.IsSubclassOf(typeof(KalsiumObject))
					).SelectMany(v => v.GetFields()
						.Where(f =>
							f.FieldType == typeof(T) || f.FieldType.IsSubclassOf(typeof(T))
							|| typeof(Attribute<T>).IsAssignableFrom(f.FieldType)
							|| arType?.IsAssignableFrom(f.FieldType) == true
							|| crType?.IsAssignableFrom(f.FieldType) == true
						)
						.Select(v => (v.Name, GetAttributeType(v.FieldType), v.FieldType))).Distinct().ToList();
			list.Sort((a, b) => a.Name.CompareTo(b.Name));
			cache = list;

		}
		return cache;
		Type GetAttributeType(Type fieldType) {
			var current = fieldType;
			while (current != null) {
				if (current.IsGenericType) {
					if (current.GetGenericTypeDefinition() == typeof(Attribute<>)) {
						return current.GenericTypeArguments[0];
					}
					if (current.GetGenericTypeDefinition() == typeof(AssetReference<>)) {
						return current.GenericTypeArguments[0];
					}
					if (current.GetGenericTypeDefinition() == typeof(ComponentReference<>)) {
						return current.GenericTypeArguments[0];
					}
				}
				current = current.BaseType;
			}
			return fieldType;
		}
	}

}

[Serializable]
public class NumericDataFieldName : DataFieldName {

	private static IEnumerable<(string, Type, Type)> cache;

	public override IEnumerable<(string, Type, Type)> GetFieldNames() {
		if (cache == null) {
			var list = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(v => v.GetTypes())
					.Where(v
						=> (v.IsClass || (v.IsValueType && !v.IsPrimitive))
						&& v.IsSubclassOf(typeof(KalsiumObject))
					).SelectMany(v => v.GetFields()
						.Where(f => f.FieldType == typeof(float) || typeof(Attribute<float>).IsAssignableFrom(f.FieldType) || f.FieldType == typeof(int) || typeof(Attribute<int>).IsAssignableFrom(f.FieldType))
						.Select(v => (v.Name, GetAttributeType(v.FieldType), v.FieldType))).Distinct().ToList();
			list.Sort((a, b) => a.Name.CompareTo(b.Name));
			cache = list;

		}
		return cache;
		Type GetAttributeType(Type fieldType) {
			var current = fieldType;
			while (current != null) {
				if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Attribute<>)) {
					return current.GenericTypeArguments[0];
				}
				current = current.BaseType;
			}
			return fieldType;
		}
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
	[CustomPropertyDrawer(typeof(DataFieldName), true)]
	public class DataFieldNameDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			var noLabel = label.text is "" && label.image is null;

			using (PropertyScope(position, label, property, out label)) {

				const int dropDownWidth = 20;

				var propertyPos = position;
				propertyPos.width -= dropDownWidth;

				var attributeName = property.FindPropertyRelative(GetBackingFieldName(nameof(DataFieldName.attributeName)));
				EditorGUI.PropertyField(propertyPos, attributeName, label);

				var dropDownPos = position;
				dropDownPos.xMin += position.width - dropDownWidth;

				if (EditorGUI.DropdownButton(dropDownPos, GUIContent.none, FocusType.Keyboard)) {
					var attributeType = property.FindPropertyRelative(GetBackingFieldName(nameof(DataFieldName.attributeType)));
					var type = GetFirstValue<SerializedType>(attributeType);
					var fnVal = GetFirstValue<DataFieldName>(property);
					var pairs = fnVal.GetFieldNames();
					var menu = new GenericMenu();
					foreach (var pair in pairs) {
						menu.AddItem(
							new GUIContent($"{pair.Item1} ({GetTypeName(pair.Item3)})"),
							attributeName.stringValue == pair.Item1,
							() => {
								attributeName.stringValue = pair.Item1;
								var types = GetValues<SerializedType>(attributeType);
								SetValue(attributeType, new SerializedType() { type = pair.Item2 });
								attributeName.serializedObject.ApplyModifiedProperties();
							}
						);
					}
					menu.DropDown(dropDownPos);
				}

			}
		}

		static string GetTypeName(Type type) {
			if (type.IsArray) return GetTypeName(type.GetElementType()) + "[]";
			if (type.IsGenericType) {
				return string.Format(
					"{0}<{1}>",
					type.Name[..type.Name.LastIndexOf("`", StringComparison.InvariantCulture)],
					string.Join(", ", type.GetGenericArguments().Select(GetTypeName))
				);
			}

			return type.Name;
		}

	}
}
#endif