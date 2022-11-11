
namespace Kalsium {

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using UnityEngine;
	using Object = UnityEngine.Object;

	public interface IObjectDict {
		void Add(Object dataObject);
		void Remove(Object dataObject);
		IEnumerable<Object> Get();
	}

	/// <summary>
	/// Stores Objects in collections based on their types.
	/// </summary>
	[Serializable]
	public class ObjectDict<TObj> : IEnumerable<TObj>, ISerializationCallbackReceiver, IObjectDict where TObj : Object {

		Dictionary<Type, IList> dict = new();
#if DEBUG
		HashSet<object> duplicateCheckMap = new();
#endif
		/// <summary> Enumerates Objects of the root type. </summary>
		public IEnumerable<TObj> Get() => Get<TObj>();
		/// <summary> Enumerates Objects of type T. </summary>
		public IEnumerable<T> Get<T>() where T : TObj {
			var type = typeof(T);
			if (dict.TryGetValue(type, out var val)) {
				foreach (T item in (List<T>)val) {
					yield return item;
				}
			}
		}

		/// <summary> Adds the Object to the cache. </summary>
		public void Add<T>(T obj) where T : TObj {
			var type = obj.GetType();
#if DEBUG
			if (!duplicateCheckMap.Add(obj)) Debug.LogWarning($"Trying to add {obj} to {type.Name} which already contains it!");
#endif
			while (true) {
				Type listType = typeof(List<>).MakeGenericType(new[] { type });
				if (!dict.TryGetValue(type, out var list)) {
					dict[type] = list = (IList)Activator.CreateInstance(listType);
				}
				list.Add(obj);
				if (type == typeof(TObj)) break;
				type = type.BaseType;
			}
		}

		/// <summary> Removes the Object from the cache. </summary>
		public void Remove(TObj obj) {
			var type = obj.GetType();
#if DEBUG
			duplicateCheckMap.Remove(obj);
#endif
			while (true) {
				Type listType = typeof(List<>).MakeGenericType(new[] { type });
				if (dict.TryGetValue(type, out var list)) {
					list.Remove(obj);
				} else {
					Debug.LogWarning($"Couldn't remove {obj} from {type.Name} because the list for that type was missing!");
				}
				if (type == typeof(TObj)) break;
				type = type.BaseType;
			}
		}

		/// <summary> Returns the first Object of type T. </summary>
		public T First<T>() where T : TObj {
			var type = typeof(T);
			if (dict.TryGetValue(type, out var val)) {
				var res = ((List<T>)val).FirstOrDefault();
				if (res != null) return (T)res;
			}
			throw new InvalidOperationException($"No {nameof(Modifier)} of type {typeof(T).Name}.");
		}
		/// <summary> Returns the first Object of type T or null if none exist. </summary>
		public T FirstOrDefault<T>() where T : TObj {
			var type = typeof(T);
			if (dict.TryGetValue(type, out var val)) {
				return ((List<T>)val).FirstOrDefault();
			}
			return default;
		}

		public int IndexOf<T>(TObj obj) where T : TObj {
			var type = typeof(T);
			if (dict.TryGetValue(type, out var val)) {
				return ((List<T>)val).IndexOf((T)obj);
			}
			return -1;
		}

		/// <summary> Returns whether a list of items of type T exists. </summary>
		public bool Contains<T>() where T : TObj {
			var type = typeof(T);
			if (dict.TryGetValue(type, out var val)) {
				var list = (List<T>)val;
				return list.Count > 0;
			}
			return false;
		}

		public void Clear() {
			foreach (var kv in dict) {
				kv.Value.Clear();
			}
			dict.Clear();
#if DEBUG
			duplicateCheckMap.Clear();
#endif
		}

		public void Clear<T>() where T : TObj {
			if (dict.TryGetValue(typeof(T), out var list)) {
				list.Clear();
			}
		}

		#region IEnumerable

		public IEnumerator<TObj> GetEnumerator() => Get().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => Get().GetEnumerator();

		#endregion

		#region IObjectDict

		void IObjectDict.Add(Object dataObject) => Add((TObj)dataObject);
		void IObjectDict.Remove(Object dataObject) => Remove((TObj)dataObject);
		IEnumerable<Object> IObjectDict.Get() => Get<TObj>();

		#endregion


		#region Serialization

		[SerializeField] TObj[] sr_values;

		void ISerializationCallbackReceiver.OnBeforeSerialize() {
			sr_values = Get().ToArray();
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize() {
			if (sr_values == null) return;
			Clear();
			foreach (var value in sr_values) {
				Add(value);
			}
		}

		#endregion
	}

}