
using System;
using UnityEngine;


public abstract partial class Master<TSelf, THook> : Master
	where TSelf : Master<TSelf, THook>
	where THook : IHook {

	/// <summary>
	/// Base class for all Modifiers of Masters.
	/// </summary>
	public abstract class RootModifier : Modifier {

		[field: Tooltip("Master component for this Modifier."), SerializeField]
		public TSelf master { get; private set; }

		/// <summary> A virtual Modifier is wrapped by a Virtualizer which acts as a layer (WIP). </summary>
		[HideInInspector] public bool virtualized;

		/// <summary> Removes this Modifier from the Master and the game. </summary>
		public void Remove() {
			if (removed) return;
			removed = true;
			Hide();

			master.DetachModifier(this);
			Game.game.objects.Remove(this);
			Game.game.hooks.Unhook(this);

			using (var scope = new Hooks.Scope()) Game.game.hooks.ForEach<IOnModifierRemove>(scope, v => v.OnModifierRemove(this));

			OnConfigureNonpersistent(false);
			OnRemove();
		}

		/// <summary> Creates a Modifier based on the given source and attaches it to the master. </summary>
		public static T Create<T>(TSelf master, T source, Action<T> initializer = null) where T : RootModifier {
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (!source.isSource) throw new ArgumentException($"{nameof(source)} must be a source", nameof(source));

			var modifier = Instantiate(source);
			modifier.master = master;
			modifier.source = source;

			master.AttachModifier(modifier);
			Game.game.objects.Add(modifier);
			Game.game.hooks.Hook(modifier);

			initializer?.Invoke(modifier);

			modifier.OnConfigureNonpersistent(true);
			modifier.OnCreate();
			modifier.Show();

			using (var scope = new Hooks.Scope()) Game.game.hooks.ForEach<IOnModifierCreate>(scope, v => v.OnModifierCreate(modifier));

			return modifier;
		}

		protected override void OnShow() {
			//
			base.OnShow();
		}

		protected override void OnHide() {
			//
			base.OnHide();
		}

	}

}

public abstract class Modifier : KalsiumObject {

}
