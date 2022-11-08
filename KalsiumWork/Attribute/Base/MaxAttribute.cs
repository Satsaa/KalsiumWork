
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;

[AttributeLabels("V", "Max")]
public class MaxAttribute<T> : Attribute<T> where T : IComparable {

	public MaxAttribute() : base() { }
	public MaxAttribute(T value = default, T max = default) => InitValues(true, value, max);

	public override int count => 2;

	public virtual ValueContainer max => values[1];

	/// <summary> Value is set to max if it's larger. </summary>
	public void Ceil() {
		if (current.value.CompareTo(max.value) > 0) current.value = max;
	}

	/// <summary> Value is set to default if it's smaller. </summary>
	public virtual void Floor() {
		if (current.value.CompareTo(default(T)) < 0) current.value = default;
	}

	/// <summary> Value is set to default if it's smaller. Value is set to max if it's larger. </summary>
	public void Clamp() {
		Floor();
		Ceil(); // Being less than max is quaranteed
	}

	/// <summary> Value is set to max. </summary>
	public void Max() {
		base.current.value = max;
	}

}

[Serializable]
public class ToggleMaxAttribute<T> : MaxAttribute<T>, IAttribute where T : IComparable {

	public ToggleMaxAttribute() : base() { }
	public ToggleMaxAttribute(T value = default, T max = default, bool enabled = true) => InitValues(enabled, value, max);
	public ToggleMaxAttribute(bool enabled = true) => InitValues(enabled);

	[field: SerializeField] public EnabledContainer enabled { get; private set; }
	EnabledContainer IAttribute.GetEnabled() => enabled;
	EnabledContainer IAttribute.SetEnabled(EnabledContainer value) => enabled = value;

}