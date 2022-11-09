
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Muc.Addressables;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

public abstract class Master<TSelf> : Master<TSelf, INoneHook>
	where TSelf : Master<TSelf, INoneHook> { }

public abstract partial class Master<TSelf, THook> : Master
	where TSelf : Master<TSelf, THook>
	where THook : IHook {

	[Tooltip("Automatically created modifiers for the Master")]
	public List<RootModifier> baseModifiers;


	public static Type hookType => typeof(THook);

	public ObjectDict<RootModifier> modifiers = new();
	public Hooks<THook> hooks = new();
	public override Hooks rawHooks => hooks;
	public override IObjectDict rawModifiers => modifiers;


	protected override void OnCreate() {
		hooks.Hook(this);
		foreach (var baseModifier in baseModifiers.Where(v => v != null)) {
			RootModifier.Create((TSelf)this, baseModifier);
		}
	}

	protected override void OnRemove() {
		hooks.Unhook(this);
		foreach (var modifier in modifiers.ToList()) {
			modifier.Remove();
		}
	}

	public void Remove() {
		if (removed) return;
		removed = true;

		Game.game.objects.Remove(this);
		Game.game.hooks.Unhook(this);

		OnConfigureNonpersistent(false);
		OnRemove();
	}

	protected void AttachModifier(RootModifier modifier) {
		modifiers.Add(modifier);
		hooks.Hook(modifier);
	}

	protected void DetachModifier(RootModifier modifier) {
		modifiers.Remove(modifier);
		hooks.Unhook(modifier);
	}

	/// <summary> Creates a Master based on the given source. </summary>
	protected static T Create<T>(T source, Action<T> initializer = null) where T : TSelf {
		if (source == null) throw new ArgumentNullException(nameof(source));
		if (!source.isSource) throw new ArgumentException($"{nameof(source)} must be a source", nameof(source));
		var master = Instantiate(source);
		master.source = source;

		Game.game.objects.Add(master);
		Game.game.hooks.Hook(master);

		initializer?.Invoke(master);

		master.OnConfigureNonpersistent(true);
		master.OnCreate();

		return master;
	}

}

public abstract class Master : KalsiumObject {

	public abstract Hooks rawHooks { get; }
	public abstract IObjectDict rawModifiers { get; }

}