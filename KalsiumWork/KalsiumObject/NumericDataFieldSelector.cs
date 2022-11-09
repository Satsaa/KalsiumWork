
using System;
using System.Reflection;
using Muc.Data;
using UnityEngine;

// Supports floats and ints simultaneously

[Serializable]
public class NumericDataFieldSelector {

	[field: SerializeField] private NumericDataFieldName field;

	[field: SerializeField] private float fallbackValue;
	[field: SerializeField] private float fallbackOther;
	[field: SerializeField] private bool fallbackEnabled = true;

	[field: SerializeField, Tooltip("Override value if the attribute is disabled.")] private ToggleValue<float> overrideValue;
	[field: SerializeField, Tooltip("Override other if the attribute is disabled.")] private ToggleValue<float> overrideOther;

	[field: SerializeField, Tooltip("If enabled, getting the primary value returns the secondary value, and vice versa.")]
	private bool swap;

	private object sourceCached;
	private string fieldNameCached;
	private IAttribute iac;
	private FieldInfo fieldCached;

	public string GetFieldName() => field.attributeName;
	public Type GetFieldType() => field.attributeType.type;

	public object GetFieldValue(object source) {
		UpdateCache(source);
		if (fieldCached != null) return fieldCached.GetValue(source);
		return iac;
	}

	public float GetValue(object source, bool ignoreSwap = false) {
		if (swap && !ignoreSwap) return GetOther(source, true);
		UpdateCache(source);
		if (fieldCached != null) return AsFloat(fieldCached.GetValue(source));
		return TryOverrideValue(source, iac != null ? AsFloat(iac.GetValue(0).value) : fallbackValue);
	}
	public float GetRawValue(object source, bool ignoreSwap = false) {
		if (swap && !ignoreSwap) return GetRawOther(source, true);
		UpdateCache(source);
		if (fieldCached != null) return AsFloat(fieldCached.GetValue(source));
		return TryOverrideValue(source, iac != null ? AsFloat(iac.GetValue(0).raw) : fallbackValue);
	}

	public float GetOther(object source, bool ignoreSwap = false) {
		if (swap && !ignoreSwap) return GetValue(source, true);
		UpdateCache(source);
		return TryOverrideOther(source, iac?.count >= 2 ? AsFloat(iac.GetValue(1).value) : fallbackOther);
	}
	public float GetRawOther(object source, bool ignoreSwap = false) {
		if (swap && !ignoreSwap) return GetRawValue(source, true);
		UpdateCache(source);
		return TryOverrideOther(source, iac?.count >= 2 ? AsFloat(iac.GetValue(1).raw) : fallbackOther);
	}

	public bool GetEnabled(object source) {
		UpdateCache(source);
		if (fieldCached != null) {
			var value = fieldCached.GetValue(source);
			if (value is ToggleValue<float> tv) return tv.enabled;
		}
		return iac?.GetEnabled() ?? fallbackEnabled;
	}
	public bool GetRawEnabled(object source) {
		UpdateCache(source);
		if (fieldCached != null) {
			var value = fieldCached.GetValue(source);
			if (value is ToggleValue<float> tv) return tv.enabled;
		}
		return iac?.GetEnabled()?.raw ?? fallbackEnabled;
	}

	protected float AsFloat(object value) {
		return value switch {
			float v => v,
			int v => (float)v,
			_ => default,
		};
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
			fieldCached = null;
			if (!String.IsNullOrEmpty(field.attributeName)) {
				var fieldInfo = source.GetType().GetField(field.attributeName);
				if (fieldInfo != null) {
					var value = fieldInfo.GetValue(source);
					if (value is Attribute) {
						this.iac = value switch {
							Attribute<float> att => att,
							Attribute<int> att => att,
							_ => null,
						};
					} else {
						fieldCached = fieldInfo;
					}

				}
			}
		}
	}

#if DEBUG // Wow so defensive
	[Obsolete("Pass the containing object instead.", true)]
	public float GetValue(Attribute attribute) => throw new ArgumentException();
	[Obsolete("Pass the containing object instead.", true)]
	public float GetRawValue(Attribute attribute) => throw new ArgumentException();
	[Obsolete("Pass the containing object instead.", true)]
	public float GetOther(Attribute attribute) => throw new ArgumentException();
	[Obsolete("Pass the containing object instead.", true)]
	public float GetRawOther(Attribute attribute) => throw new ArgumentException();
	[Obsolete("Pass the containing object instead.", true)]
	public bool GetEnabled(Attribute attribute) => throw new ArgumentException();
	[Obsolete("Pass the containing object instead.", true)]
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
	[CustomPropertyDrawer(typeof(NumericDataFieldSelector), true)]
	public class NumericDataFieldSelectorDrawer : PropertyDrawer {

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