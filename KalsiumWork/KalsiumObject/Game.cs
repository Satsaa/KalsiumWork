
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
	[CreateAssetMenu(fileName = nameof(Game), menuName = nameof(Kalsium) + "/" + nameof(Master) + "/" + nameof(Game))]
	public sealed class Game : Master<Game, IGameHook> {

		/// <summary> Currently active Game instance </summary>
		public static Game game => Coordinator.game;
		public bool active => game == this;

		[field: SerializeField] public Library library { get; private set; }

		[field: SerializeField] public ObjectDict<KalsiumObject> objects { get; private set; } = new();

		[field: SerializeField] public State state { get; private set; }

		[Serializable]
		public enum State {
			Uninited,
			Initing,
			Ready,
			Ending,
			Ended,
		}

		public static Game CreateGame(Game source) {
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (!source.isSource) throw new ArgumentException($"{nameof(source)} must be a source", nameof(source));
			var game = Instantiate(source);
			game.source = source;
			return game;
		}

		public void Init() {
			if (state >= State.Initing) throw new InvalidOperationException("Already inited");
			state = State.Initing;

			game.objects.Add(game);
			game.hooks.Hook(game);

			game.OnConfigureNonpersistent(true);
			game.OnCreate();

			using (var scope = new Hooks.Scope()) hooks.ForEach<IOnGameInit>(scope, v => v.OnGameInit());
			state = State.Ready;
		}

		public void Refresh() {
			if (state != State.Ready) throw new InvalidOperationException("Not ready");
			using (var scope = new Hooks.Scope()) hooks.ForEach<IOnEarlyRefresh>(scope, v => v.OnEarlyRefresh());
			using (var scope = new Hooks.Scope()) hooks.ForEach<IOnRefresh>(scope, v => v.OnRefresh());
			using (var scope = new Hooks.Scope()) hooks.ForEach<IOnLateRefresh>(scope, v => v.OnLateRefresh());
		}

		public void End() {
			if (state >= State.Ending) throw new InvalidOperationException("Already ended");
			if (state < State.Ready) throw new InvalidOperationException("Not inited");
			state = State.Ending;
			using (var scope = new Hooks.Scope()) hooks.ForEach<IOnGameEnd>(scope, v => v.OnGameEnd());
			state = State.Ended;
		}

		/// <summary> Removes removed KalsiumObjects from the cache and destroys them </summary>
		private void Flush() {
			foreach (var obj in objects.Get<Master>().Where(v => v.removed).ToList()) {
				objects.Remove(obj);
				ObjectUtil.Destroy(obj);
			}
		}

		#region Misc

		[Obsolete("Don't try to remove the game.")]
		new public void Remove() { }

		protected override void OnRemove() {
			base.OnRemove();
			Debug.LogWarning("The Game was removed. Very bad.");
		}

		#endregion

	}

}