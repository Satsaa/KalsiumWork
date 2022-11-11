
namespace Kalsium {

	using System;
	using System.Reflection;
	using Muc.Data;
	using UnityEngine;

	// Supports floats and ints simultaneously

	[Serializable]
	public class NumericAttributeSelector {

		[field: SerializeField] private NumericAttributeFieldName field;

		[field: SerializeField] private float fallbackValue;
		[field: SerializeField] private float fallbackOther;
		[field: SerializeField] private bool fallbackEnabled = true;

		[field: SerializeField, Tooltip("Override value if the attribute is disabled.")] private ToggleValue<float> overrideValue;
		[field: SerializeField, Tooltip("Override other if the attribute is disabled.")] private ToggleValue<float> overrideOther;

		private object sourceCached;
		private string fieldNameCached;
		private IAttribute iac;

		public IAttribute GetAttribute(object source) {
			UpdateCache(source);
			return iac;
		}

		public float GetValue(object source) {
			UpdateCache(source);
			return TryOverrideValue(source, iac != null ? (float)iac.GetValue(0).value : fallbackValue);
		}
		public float GetRawValue(object source) {
			UpdateCache(source);
			return TryOverrideValue(source, iac != null ? (float)iac.GetValue(0).raw : fallbackValue);
		}

		public float GetOther(object source) {
			UpdateCache(source);
			return TryOverrideOther(source, iac?.count >= 2 ? (float)iac.GetValue(1).value : fallbackOther);
		}
		public float GetRawOther(object source) {
			UpdateCache(source);
			return TryOverrideOther(source, iac?.count >= 2 ? (float)iac.GetValue(1).raw : fallbackOther);
		}

		public bool GetEnabled(object source) {
			UpdateCache(source);
			return iac?.GetEnabled() ?? fallbackEnabled;
		}
		public bool GetRawEnabled(object source) {
			UpdateCache(source);
			return iac?.GetEnabled()?.raw ?? fallbackEnabled;
		}

		protected float TryOverrideValue(object source, float value) {
			return !overrideValue.enabled || GetEnabled(source) ? value : overrideValue.value;
		}

		protected float TryOverrideOther(object source, float value) {
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
				iac = null;
				if (!String.IsNullOrEmpty(field.attributeName)) {
					var fieldInfo = source.GetType().GetField(field.attributeName);
					if (fieldInfo != null) {
						var value = fieldInfo.GetValue(source);
						this.iac = value switch {
							Attribute<float> att => att,
							Attribute<int> att => att,
							_ => null,
						};
					}
				}
			}
		}

#if DEBUG // Wow so defensive
		[Obsolete("Pass the containing object instead.")]
		public float GetValue(Attribute attribute) => throw new ArgumentException();
		[Obsolete("Pass the containing object instead.")]
		public float GetRawValue(Attribute attribute) => throw new ArgumentException();
		[Obsolete("Pass the containing object instead.")]
		public float GetOther(Attribute attribute) => throw new ArgumentException();
		[Obsolete("Pass the containing object instead.")]
		public float GetRawOther(Attribute attribute) => throw new ArgumentException();
		[Obsolete("Pass the containing object instead.")]
		public bool GetEnabled(Attribute attribute) => throw new ArgumentException();
		[Obsolete("Pass the containing object instead.")]
		public bool GetRawEnabled(Attribute attribute) => throw new ArgumentException();
#endif
	}

#if UNITY_EDITOR
	namespace Editors {
		using UnityEditor;
		using UnityEngine;
		using static Muc.Editor.EditorUtil;
		using static Muc.Editor.PropertyUtil;

		[CanEditMultipleObjects]
		[CustomPropertyDrawer(typeof(NumericAttributeSelector), true)]
		public class NumericAttributeSelectorDrawer : PropertyDrawer {

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				return property.isExpanded ? 7 * (lineHeight + spacing) : base.GetPropertyHeight(property, label);
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