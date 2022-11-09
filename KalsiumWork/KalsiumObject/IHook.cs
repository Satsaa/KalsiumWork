
#nullable enable annotations

public interface IHook { }

/// <summary> Dummy IHook type when none are supported </summary>
public interface INoneHook : IHook { }

public interface IGameHook : IHook { }

public interface IOnRefresh : IGameHook { void OnRefresh(); }

public interface IOnModifierCreate : IGameHook { void OnModifierCreate(Modifier modifier); }
public interface IOnModifierRemove : IGameHook { void OnModifierRemove(Modifier modifier); }

public interface IOnGameInit : IGameHook { void OnGameInit(); }
public interface IOnGameStart : IGameHook { void OnGameStart(); }
public interface IOnGameEnd : IGameHook { void OnGameEnd(); }

#nullable disable
