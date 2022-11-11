
namespace Kalsium {

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using Object = UnityEngine.Object;

	[Serializable]
	[AttributeLabels("V", "Min", "Max")]
	public class MinMaxAttribute<T> : MaxAttribute<T> where T : IComparable {

		public MinMaxAttribute() : base() { }
		public MinMaxAttribute(T value = default, T min = default, T max = default) => InitValues(true, value, min, max);

		public override int count => 3;

		public virtual ValueContainer min => values[1];
		public override ValueContainer max => values[2];

		/// <summary> Value is set to min if it's smaller. </summary>
		public override void Floor() {
			if (current.value.CompareTo(min.value) < 0) current.value = min;
		}

		/// <summary> Value is set to min. </summary>
		public void Min() {
			current.value = min;
		}
	}

	[Serializable]
	public class ToggleMinMaxAttribute<T> : MinMaxAttribute<T>, IAttribute where T : IComparable {

		public ToggleMinMaxAttribute() : base() { }
		public ToggleMinMaxAttribute(T value = default, T min = default, T max = default, bool enabled = true) => InitValues(enabled, value, min, max);
		public ToggleMinMaxAttribute(bool enabled = true) => InitValues(enabled);

		[field: SerializeField] public EnabledContainer enabled { get; private set; }
		EnabledContainer IAttribute.GetEnabled() => enabled;
		EnabledContainer IAttribute.SetEnabled(EnabledContainer value) => enabled = value;

	}

}