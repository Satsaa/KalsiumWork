
namespace Kalsium {

	using System;
	using System.Reflection;
	using Muc.Addressables;
	using Muc.Data;
	using UnityEngine;
	using Object = UnityEngine.Object;

	[Serializable]
	public class DataFieldSelector<T> {

		[field: SerializeField] private KalsiumObjectFieldName<T> field;

		[field: SerializeField] private T fallbackValue;
		[field: SerializeField] private T fallbackOther;
		[field: SerializeField] private bool fallbackEnabled = true;

		[field: SerializeField, Tooltip("Override value if the attribute is disabled.")] private ToggleValue<T> overrideValue;
		[field: SerializeField, Tooltip("Override other if the attribute is disabled.")] private ToggleValue<T> overrideOther;

		[field: SerializeField, Tooltip("If enabled, getting the primary value returns the secondary value, and vice versa.")]
		private bool swap;

		private object sourceCached;
		private string fieldNameCached;
		private FieldInfo fieldCached;

		private Attribute<T> ac;
		private IAttribute iac;

		private object ar;
		private PropertyInfo arValue;


		public string GetFieldName() => field.attributeName;
		public Type GetFieldType() => field.attributeType.type;

		public object GetFieldValue(object source) {
			UpdateCache(source);
			if (fieldCached != null) return fieldCached.GetValue(source);
			return ac;
		}

		public T GetValue(object source, bool ignoreSwap = false) {
			if (swap && !ignoreSwap) return GetOther(source, true);
			UpdateCache(source);
			if (ar != null) return (T)arValue.GetValue(ar);
			if (fieldCached != null) return (T)fieldCached.GetValue(source);
			return TryOverrideValue(source, ac != null ? ac.current : fallbackValue);
		}
		public T GetRawValue(object source, bool ignoreSwap = false) {
			if (swap && !ignoreSwap) return GetRawOther(source, true);
			UpdateCache(source);
			if (fieldCached != null) return (T)fieldCached.GetValue(source);
			return TryOverrideValue(source, ac != null ? ac.current.raw : fallbackValue);
		}

		public T GetOther(object source, bool ignoreSwap = false) {
			if (swap && !ignoreSwap) return GetValue(source, true);
			UpdateCache(source);
			return TryOverrideOther(source, ac?.count >= 2 ? ac.values[1] : fallbackOther);
		}
		public T GetRawOther(object source, bool ignoreSwap = false) {
			if (swap && !ignoreSwap) return GetRawValue(source, true);
			UpdateCache(source);
			return TryOverrideOther(source, ac?.count >= 2 ? ac.values[1].raw : fallbackOther);
		}

		public bool GetEnabled(object source) {
			UpdateCache(source);
			if (fieldCached != null) {
				var value = fieldCached.GetValue(source);
				if (value is ToggleValue<T> tv) return tv.enabled;
			}
			return iac?.GetEnabled() ?? fallbackEnabled;
		}
		public bool GetRawEnabled(object source) {
			UpdateCache(source);
			if (fieldCached != null) {
				var value = fieldCached.GetValue(source);
				if (value is ToggleValue<T> tv) return tv.enabled;
			}
			return iac?.GetEnabled()?.raw ?? fallbackEnabled;
		}

		protected T TryOverrideValue(object source, T value) {
			return !overrideValue.enabled || GetEnabled(source) ? value : overrideValue.value;
		}

		protected T TryOverrideOther(object source, T value) {
			return !overrideOther.enabled || GetEnabled(source) ? value : overrideOther.value;
		}

		protected void UpdateCache(object source) {
#if UNITY_EDITOR
			if (!Application.isPlaying || (sourceCached != source || fieldNameCached != field.attributeName)) {
#else
		if (sourceCached != source || fieldNameCached != field.attributeName) {
#endif
				sourceCached = source;
				fieldNameCached = field.attributeName;
				ar = iac = ac = null;
				arValue = null;
				fieldCached = null;
				if (!string.IsNullOrEmpty(field.attributeName)) {
					var fieldInfo = source.GetType().GetField(field.attributeName);
					if (fieldInfo != null) {
						var value = fieldInfo.GetValue(source);
						if (value is Attribute<T> attribute) {
							iac = ac = attribute;
						} else {
							var arType = typeof(Object).IsAssignableFrom(typeof(T)) ? typeof(AssetReference<>).MakeGenericType(typeof(T)) : null;
							var crType = typeof(Component).IsAssignableFrom(typeof(T)) ? typeof(ComponentReference<>).MakeGenericType(typeof(T)) : null;
							var type = value.GetType();
							if (arType?.IsAssignableFrom(type) == true || crType?.IsAssignableFrom(type) == true) {
								ar = value;
								arValue = type.GetProperty("value", BindingFlags.Instance | BindingFlags.Public);
							} else {
								fieldCached = fieldInfo;
							}
						}
					}
				}
			}
		}

#if DEBUG // Wow so defensive
		[Obsolete("Pass the containing object instead.")]
		public T GetValue(Attribute<T> attribute) => throw new ArgumentException();
		[Obsolete("Pass the containing object instead.")]
		public T GetRawValue(Attribute<T> attribute) => throw new ArgumentException();
		[Obsolete("Pass the containing object instead.")]
		public T GetOther(Attribute<T> attribute) => throw new ArgumentException();
		[Obsolete("Pass the containing object instead.")]
		public T GetRawOther(Attribute<T> attribute) => throw new ArgumentException();
		[Obsolete("Pass the containing object instead.")]
		public bool GetEnabled(Attribute<T> attribute) => throw new ArgumentException();
		[Obsolete("Pass the containing object instead.")]
		public bool GetRawEnabled(Attribute<T> attribute) => throw new ArgumentException();
#endif
	}

#if UNITY_EDITOR
	namespace Editors {
		using UnityEditor;
		using UnityEngine;
		using static Muc.Editor.EditorUtil;
		using static Muc.Editor.PropertyUtil;

		[CanEditMultipleObjects]
		[CustomPropertyDrawer(typeof(DataFieldSelector<>), true)]
		public class DataFieldSelectorDrawer : PropertyDrawer {

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				return property.isExpanded ? 8 * (lineHeight + spacing) : base.GetPropertyHeight(property, label);
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				using (PropertyScope(position, label, property, out label)) {
					if (property.isExpanded) {
						EditorGUI.PropertyField(position, property, label, true);
					} else {
						var propPos = position;
						propPos.xMax = propPos.xMin + EditorGUIUtility.labelWidth + spacing;
						property.isExpanded = EditorGUI.Foldout(propPos, property.isExpanded, label, true);

						var fieldPos = position;
						fieldPos.xMin += EditorGUIUtility.labelWidth + spacing;
						var field = property.FindPropertyRelative("field");
						EditorGUI.PropertyField(fieldPos, field, GUIContent.none);
					}
				}
			}

		}
	}
#endif
}