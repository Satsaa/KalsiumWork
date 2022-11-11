
namespace Kalsium {

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using Object = UnityEngine.Object;

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

}