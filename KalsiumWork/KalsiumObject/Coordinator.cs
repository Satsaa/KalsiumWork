
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
[KeepRefToken]
public class Coordinator : Singleton<Coordinator> {

	/// <summary> Currently active Game instance </summary>
	public static Game game => Coordinator.instance.activeGame;
	public static Coordinator coordinator => Coordinator.instance;


	[SerializeField]
	private Game activeGame;

}
