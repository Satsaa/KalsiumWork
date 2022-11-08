
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Shows a dropdown to select an identifier of a KalsiumObject.
/// </summary>
public class IdentifierAttribute : PropertyAttribute {

	public Type objectType { get; }

	public IdentifierAttribute(Type objectType = null) {
		if (objectType != null && !typeof(KalsiumObject).IsAssignableFrom(objectType)) throw new ArgumentException($"Type must be assignable to the type {nameof(KalsiumObject)}", nameof(objectType));
		this.objectType = objectType;
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
	[CustomPropertyDrawer(typeof(IdentifierAttribute), true)]
	public class IdentifierAttributeDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			using (PropertyScope(position, label, property, out label)) {

				const int dropDownWidth = 20;

				var propertyPos = position;
				propertyPos.width -= dropDownWidth;

				EditorGUI.PropertyField(propertyPos, property, label);

				var dropDownPos = position;
				dropDownPos.xMin += position.width - dropDownWidth;

				if (EditorGUI.DropdownButton(dropDownPos, GUIContent.none, FocusType.Keyboard)) {
					var a = attribute as IdentifierAttribute;
					var v = GetFirstValue<string>(property);
					var menu = new GenericMenu();
					var sorted = Game.game.library.dict.Values.ToList();
					sorted.Sort((a, b) => a.identifier.CompareTo(b.identifier));
					foreach (var data in sorted) {
						if (a.objectType == null || a.objectType.IsAssignableFrom(data.GetType())) {
							menu.AddItem(new GUIContent(data.identifier), data.identifier == v, () => SetValue(property, data.identifier));
						}
					}
					menu.ShowAsContext();
				}
			}
		}

	}
}
#endif