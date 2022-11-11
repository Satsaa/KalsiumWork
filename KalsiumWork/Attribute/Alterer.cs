
namespace Kalsium {

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using Object = UnityEngine.Object;

	public abstract class Alterer<T1> {

		public Object creator { get; protected set; }

		public abstract T1 Apply(T1 value);

		public abstract void Update();

	}


	public sealed class Alterer<T1, T2> : Alterer<T1> {

		public T2 alterant { get; private set; }
		public Func<T2> updater { get; private set; }
		public Func<T1, T2, T1> applier { get; private set; }
		public Action changeInvoker { get; private set; }

		public Alterer(Object creator, Func<T2> updater, Func<T1, T2, T1> applier, Action changeInvoker) {
			this.creator = creator;
			this.updater = updater;
			this.alterant = updater();
			this.changeInvoker = changeInvoker;
			this.applier = applier;
		}

		public override T1 Apply(T1 value) {
			return applier(value, alterant);
		}

		public override void Update() {
			var old = alterant;
			alterant = updater();
			if (old.Equals(alterant)) return;
			changeInvoker();
		}

	}

}