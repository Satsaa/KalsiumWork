
namespace Kalsium {

	using System;
	using System.Linq;
	using UnityEngine;

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
	public class AttributeLabelsAttribute : PropertyAttribute {

		public readonly GUIContent[] labels;

		public AttributeLabelsAttribute(params string[] labels) {
			this.labels = labels.Select(v => new GUIContent(v)).ToArray();
		}
	}

}