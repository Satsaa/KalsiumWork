
namespace Kalsium {

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Muc.Components.Extended;
	using Muc.Editor;
	using Serialization;
	using UnityEngine;
	using UnityEngine.AddressableAssets;
	using Object = UnityEngine.Object;

	/// <summary> Coordinates the execution of Game Hooks. </summary>
	public class Coordinator : Singleton<Coordinator> {

		/// <summary> Currently active Game instance </summary>
		public static Game game => Coordinator.instance.activeGame;
		public static Coordinator coordinator => Coordinator.instance;

		[SerializeField]
		private Game activeGame;

		public virtual void ActivateGame(Game game) {
			if (activeGame == game) return;
			activeGame = game;
		}

		public virtual void DeactivateGame() {
			activeGame = null;
		}

		protected virtual void Update() {
			if (activeGame) {
				if (activeGame.state == Game.State.Ready) activeGame.Refresh();
			}
		}

	}

}