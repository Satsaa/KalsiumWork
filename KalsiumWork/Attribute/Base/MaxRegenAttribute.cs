
namespace Kalsium {

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using UnityEngine;
	using Object = UnityEngine.Object;

	[Serializable]
	[AttributeLabels("V", "Max", "Regen")]
	public class MaxRegenAttribute<T> : MaxAttribute<T> where T : IComparable {

		public MaxRegenAttribute() : base() { }
		public MaxRegenAttribute(T value = default, T max = default, T regen = default) => InitValues(true, value, max, regen);

		public override int count => 3;

		public override ValueContainer max => values[1];
		public virtual ValueContainer regen => values[2];


		static Func<T, T, T> _add;
		protected static Func<T, T, T> add {
			get {
				if (_add != null) return _add;
				var p1 = Expression.Parameter(typeof(T));
				var p2 = Expression.Parameter(typeof(T));
				return _add = Expression.Lambda<Func<T, T, T>>(Expression.Add(p1, p2), p1, p2).Compile();
			}
		}

		/// <summary> Value is increased by regen and clamped. </summary>
		public void Regen(bool clamp = true) {
			current.value = add(current, regen);
			if (clamp) Clamp();
		}

	}

	[Serializable]
	public class ToggleMaxRegenAttribute<T> : MaxRegenAttribute<T>, IAttribute where T : IComparable {

		public ToggleMaxRegenAttribute() : base() { }
		public ToggleMaxRegenAttribute(T value = default, T max = default, T regen = default, bool enabled = true) => InitValues(enabled, value, max, regen);
		public ToggleMaxRegenAttribute(bool enabled = true) => InitValues(enabled);

		[field: SerializeField] public EnabledContainer enabled { get; private set; }
		EnabledContainer IAttribute.GetEnabled() => enabled;
		EnabledContainer IAttribute.SetEnabled(EnabledContainer value) => enabled = value;

	}

}