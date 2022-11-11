
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

	/// <summary> The game. </summary>
	[KeepRefToken]
	public sealed class Game : Master<Game, IGameHook> {

		/// <summary> Currently active Game instance </summary>
		public static Game game => Coordinator.game;
		public bool active => game == this;

		[field: SerializeField] public Library library { get; private set; }

		[field: SerializeField] public ObjectDict<KalsiumObject> objects { get; private set; } = new();

		protected override void OnCreate() {
			base.OnCreate();
			hooks.Hook(this);
		}

		public void Init() {
			using (var scope = new Hooks.Scope()) hooks.ForEach<IOnGameInit>(scope, v => v.OnGameInit());
		}

		public void Refresh() {
			using (var scope = new Hooks.Scope()) hooks.ForEach<IOnRefresh>(scope, v => v.OnRefresh());
		}

		public void End() {
			using (var scope = new Hooks.Scope()) hooks.ForEach<IOnGameEnd>(scope, v => v.OnGameEnd());
		}

		/// <summary> Removes removed KalsiumObjects from the cache and destroys them </summary>
		private void Flush() {
			foreach (var obj in objects.Get<Master>().Where(v => v.removed).ToList()) {
				objects.Remove(obj);
				ObjectUtil.Destroy(obj);
			}
		}

	}

}