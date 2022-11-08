
using UnityEngine;

public abstract class Actor<TMaster> : Actor where TMaster : Master {

	new public TMaster master => (TMaster)base.master;

	public virtual void Attach(TMaster master) => base.Attach(master);
	public sealed override void Attach(Master master) => base.Attach((TMaster)master);

}

public class Actor : Hooker {

	[field: SerializeField, HideInInspector]
	public Master master { get; private set; }
	public bool attached => master;

	[field: SerializeField]
	public Animator animator { get; private set; }

	protected override void Awake() {
		base.Awake();
		if (!transform.parent) transform.parent = Game.game.transform;
		if (!animator) animator = GetComponent<Animator>();
	}

	protected override void OnDestroy() {
		base.OnDestroy();
		if (master) Detach();
	}


	public virtual void Attach(Master master) {
		this.master = master;
		master.rawHooks.Hook(this);
		master.OnActorAttach();
	}
	public virtual void Detach() {
		master.OnActorDetach();
		master.rawHooks.Unhook(this);
		master = null;
	}


	public virtual void EndAnimations() {
		if (animator) animator.SetTrigger("Idle");
	}

}
