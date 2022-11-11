
namespace Kalsium {

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using Object = UnityEngine.Object;

	/// <summary> A hooker automatically hooks to the global hooks in Awake and OnDestroy. </summary>
	public abstract class Hooker : MonoBehaviour {

		protected virtual void Awake() {
			Game.game.hooks.Hook(this);
		}

		protected virtual void OnDestroy() {
			if (Game.game) Game.game.hooks.Unhook(this);
		}

	}

}