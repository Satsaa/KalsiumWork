
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

	public void ExecuteOnUpdate() {
		using (var scope = new Hooks.Scope()) hooks.ForEach<IOnUpdate>(scope, v => v.OnUpdate());
	}

	public void ExecuteOnLateUpdate() {
		using (var scope = new Hooks.Scope()) hooks.ForEach<IOnLateUpdate>(scope, v => v.OnLateUpdate());
	}

	/// <summary> Removes removed KalsiumObjects from the cache and destroys them </summary>
	private void Flush() {
		foreach (var obj in objects.Get<Master>().Where(v => v.removed).ToList()) {
			objects.Remove(obj);
			ObjectUtil.Destroy(obj);
		}
	}

}

#if UNITY_EDITOR
namespace Editors {

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using UnityEngine;
	using static Muc.Editor.EditorUtil;
	using static Muc.Editor.PropertyUtil;
	using Object = UnityEngine.Object;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(Game), true)]
	public class GameEditor : Editor {

		Game t => (Game)target;

		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			if (ButtonField(new("Show all"))) {
				foreach (var dataObject in Game.game.objects.Get()) {
					dataObject.Show();
				}
			}
			if (ButtonField(new("Hide all"))) {
				foreach (var dataObject in Game.game.objects.Get()) {
					dataObject.Hide();
				}
			}
		}
	}
}
#endif