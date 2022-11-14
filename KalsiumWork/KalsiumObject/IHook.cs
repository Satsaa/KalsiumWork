
namespace Kalsium {

#nullable enable annotations

	public interface IHook { }

	/// <summary> Dummy IHook type when none are supported </summary>
	[System.Obsolete]
	public interface INoneHook : IHook { }

	public interface IGameHook : IHook { }

	public interface IOnEarlyRefresh : IGameHook { void OnEarlyRefresh(); }
	public interface IOnRefresh : IGameHook { void OnRefresh(); }
	public interface IOnLateRefresh : IGameHook { void OnLateRefresh(); }

	public interface IOnModifierCreate : IGameHook { void OnModifierCreate(Modifier modifier); }
	public interface IOnModifierRemove : IGameHook { void OnModifierRemove(Modifier modifier); }

	public interface IOnGameInit : IGameHook { void OnGameInit(); }
	public interface IOnGameStart : IGameHook { void OnGameStart(); }
	public interface IOnGameEnd : IGameHook { void OnGameEnd(); }

#nullable disable

}