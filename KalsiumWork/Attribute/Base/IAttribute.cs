
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;

public interface IAttribute {

	string identifier { get; }

	int count { get; }

	IReadOnlyList<Attribute.IValueContainer> GetValues();
	Attribute.IValueContainer GetValue(int index);
	Attribute.EnabledContainer GetEnabled();
	Attribute.EnabledContainer SetEnabled(Attribute.EnabledContainer value);


	string Format(bool isSource);
	string TooltipText(IAttribute source);
}