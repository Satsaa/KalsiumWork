
namespace Kalsium {

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Muc;
	using Muc.Collections;
	using Muc.Data;
	using Muc.Extensions;
	using Serialization;
	using UnityEngine;
	using Object = UnityEngine.Object;

	public abstract class Hooks {

		public class Scope : IDisposable {
			public Scope() => active = true;

			public Action onFinish;
			public bool active;

			public void Dispose() {
				if (!active) throw new InvalidOperationException("The scope has already been disposed.");
				onFinish?.Invoke();
				current = null;
				active = false;
			}
		}

		protected static Scope current;

		public static bool executing => current != null;

		/// <summary> This event is Invoked ONCE after the execution of IHooks in the current scope ends. </summary>
		/// <exception cref="InvalidOperationException">asd</exception>
		public static event Action onFinishEvent {
			remove { if (current?.active == true) current.onFinish -= value; else throw new InvalidOperationException("No active scope."); }
			add { if (current?.active == true) current.onFinish += value; else throw new InvalidOperationException("No active scope."); }
		}


		/// <summary> Adds an OnEvents to the cache. </summary>
		public abstract void Hook(Object obj);

		/// <summary> Adds an OnEvents to the cache, if not already present. </summary>
		public abstract void TryHook(Object obj);

		/// <summary> Removes an OnEvents from the cache. </summary>
		public abstract void Unhook(Object obj);


		public class HooksList<T> : SafeList<T> {

			public List<int> orders = new();

			public void AddOrdered(Type hookerType, Type hookType, T item) {
				var order = HookOrders.instance.GetOrder(hookerType, hookType);
				for (int i = 0; i < orders.Count; i++) {
					var current = orders[i];
					if (current > order) {
						orders.Insert(i, order);
						Insert(i, item);
						return;
					}
				}
				orders.Add(order);
				Add(item);
			}

			public bool RemoveOrdered(T item) {
				var index = IndexOf(item);
				if (index == -1) return false;
				RemoveAt(index);
				orders.RemoveAt(index);
				return true;
			}

		}
	}

	/// <summary>
	/// Stores IHooks in collections based on their types.
	/// </summary>
	/// <typeparam name="TBase">Required base type for IHooks</typeparam>
	[Serializable]
	public class Hooks<TBase> : Hooks, ISerializationCallbackReceiver where TBase : IHook {

		Dictionary<Type, IList> dict = new();


		/// <summary> Returns IHooks of type T inside a new List. </summary>
		public IEnumerable<T> Get<T>() where T : TBase {
			if (dict.TryGetValue(typeof(T), out var val)) {
				return new List<T>(val as SafeList<T>);
			} else {
				return new List<T>();
			}
		}

		/// <summary> Invokes action on all IHooks of type T. </summary>
		public void ForEach<T>(Scope scope, Action<T> action) where T : TBase {
			if (dict.TryGetValue(typeof(T), out var val)) {
				var list = val as SafeList<T>;
				foreach (var v in list) {
					current = scope;
					action(v);
				}
			}
		}

		public override void Hook(Object obj) {
			var hookerType = obj.GetType();
			foreach (var iType in obj.GetType().GetInterfaces().Where(v => v.GetInterfaces().Contains(typeof(IHook)))) {
				var listType = typeof(HooksList<>).MakeGenericType(new[] { iType });
				if (!dict.TryGetValue(iType, out var list)) {
					dict[iType] = list = (IList)Activator.CreateInstance(listType);
				}
				var add = listType.GetMethod(nameof(HooksList<int>.AddOrdered));
				add.Invoke(list, new object[] { hookerType, iType, obj });
#if DEBUG // Ensure no duplicates are created
				var total = 0;
				foreach (var item in list) {
					if (obj.Equals(item)) total++;
				}
				if (total > 1) Debug.Log($"Added {obj} to {iType.Name}. ${total} duplicates exist!");
#endif
			}
		}

		public override void TryHook(Object obj) {
			var hookerType = obj.GetType();
			foreach (var iType in obj.GetType().GetInterfaces().Where(v => v.GetInterfaces().Contains(typeof(IHook)))) {
				var listType = typeof(HooksList<>).MakeGenericType(new[] { iType });
				if (!dict.TryGetValue(iType, out var list)) {
					dict[iType] = list = (IList)Activator.CreateInstance(listType);
				}
				if (!list.Cast<object>().Any(v => obj.Equals(v))) {
					var add = listType.GetMethod(nameof(HooksList<int>.AddOrdered));
					add.Invoke(list, new object[] { hookerType, iType, obj });
				}
			}
		}

		public override void Unhook(Object obj) {
			foreach (var iType in obj.GetType().GetInterfaces().Where(v => v.GetInterfaces().Contains(typeof(IHook)))) {
				var listType = typeof(HooksList<>).MakeGenericType(new[] { iType });
				if (dict.TryGetValue(iType, out var list)) {
					var remove = listType.GetMethod(nameof(HooksList<int>.RemoveOrdered));
					remove.Invoke(list, new object[] { obj });
				} else {
					Debug.LogWarning($"Couldn't remove {obj} from {iType.Name} because the list for that type was missing!");
				}
			}
		}

		#region Serialization

		[Serializable]
		private class SrContainer {
			public Object obj;
			public int order;
			public SrContainer(Object obj, int order) {
				this.obj = obj;
				this.order = order;
			}
		}


		[SerializeField] SerializedDictionary<string, SrContainer[]> sr_dict;

		void ISerializationCallbackReceiver.OnBeforeSerialize() {
			sr_dict = new(dict.Select(kv => {
				var orders = (kv.Value.GetType().GetField(nameof(HooksList<int>.orders)).GetValue(kv.Value) as List<int>);
				var i = 0;
				return new KeyValuePair<string, SrContainer[]>(
					kv.Key.GetShortQualifiedName(),
					kv.Value.Cast<Object>().Select(v => new SrContainer(v as Object, orders[i++])).ToArray()
				);
			}));
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize() {
			dict.Clear();
			foreach (var kv in sr_dict) {
				var type = Type.GetType(kv.Key);
				var objs = kv.Value.Select(v => v.obj).ToList();
				var orders = kv.Value.Select(v => v.order).ToList();
				Type listType = typeof(HooksList<>).MakeGenericType(new[] { type });
				var list = dict[type] = (IList)Activator.CreateInstance(listType);

				foreach (var srCont in kv.Value) {
					list.Add(srCont.obj);
				}

				listType.GetField(nameof(HooksList<int>.orders)).SetValue(list, orders);
			}
			sr_dict = null;
		}

		#endregion

	}

}