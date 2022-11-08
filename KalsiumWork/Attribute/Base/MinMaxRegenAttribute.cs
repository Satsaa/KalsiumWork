
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;

[Serializable]
[AttributeLabels("V", "Min", "Max", "Regen")]
public class MinMaxRegenAttribute<T> : MinMaxAttribute<T> where T : IComparable {

	public MinMaxRegenAttribute() : base() { }
	public MinMaxRegenAttribute(T value = default, T min = default, T max = default, T regen = default) => InitValues(true, value, min, max, regen);

	public override int count => 4;

	public override ValueContainer min => values[1];
	public override ValueContainer max => values[2];
	public virtual ValueContainer regen => values[3];

}

[Serializable]
public class ToggleMinMaxRegenAttribute<T> : MinMaxRegenAttribute<T>, IAttribute where T : IComparable {

	public ToggleMinMaxRegenAttribute() : base() { }
	public ToggleMinMaxRegenAttribute(T value = default, T min = default, T max = default, T regen = default, bool enabled = true) => InitValues(enabled, value, min, max, regen);
	public ToggleMinMaxRegenAttribute(bool enabled = true) => InitValues(enabled);

	[field: SerializeField] public EnabledContainer enabled { get; private set; }
	EnabledContainer IAttribute.GetEnabled() => enabled;
	EnabledContainer IAttribute.SetEnabled(EnabledContainer value) => enabled = value;

}
