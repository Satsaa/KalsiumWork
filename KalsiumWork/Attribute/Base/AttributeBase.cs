
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;

public abstract class Attribute {

	public interface IValueContainer {
		object cache { get; }
		bool isCached { get; }
		object value { get; }
		object raw { get; }
		object original { get; }
		bool isModified { get; }
		System.Collections.IList alterers { get; }
		Muc.Data.Event onChanged { get; }
	}

	public interface IValueContainer<T> {
		T cache { get; }
		bool isCached { get; }
		T value { get; }
		T raw { get; }
		T original { get; }
		bool isModified { get; }
		IReadOnlyList<Alterer<T>> alterers { get; }
		Muc.Data.Event onChanged { get; }
	}

	public class ValueContainer<T> : IValueContainer, IValueContainer<T> {

		public static implicit operator T(ValueContainer<T> v) => v.value;

		public ValueContainer() { }
		public ValueContainer(T raw) => this.raw = raw;

		List<Alterer<T>> _alterers = new();
		public IReadOnlyList<Alterer<T>> alterers => _alterers;
		public Muc.Data.Event onChanged { get; private set; } = new();
		protected internal Muc.Data.Event onAttributeChanged { protected get; set; }

		[SerializeField] T cache;
		[SerializeField] bool isCached;
		[field: SerializeField] public T raw { get; private set; }
		[field: SerializeField] public bool isModified { get; private set; }

		public T value {
			get => Update();
			set {
				if (raw.Equals(value)) return;
				Update();
				raw = value;
				Update(true);
			}
		}


		[SerializeField] T _original;

		public T original {
			get {
				Update();
				return _original;
			}
		}


		private T Update(bool forceUpdate = false) {
#if UNITY_EDITOR
			if (!Application.isPlaying) return raw;
#endif
			if (!forceUpdate && isCached)
				return cache;
			if (!isCached) {
				cache = raw;
				isCached = true;
			}
			var prev = cache;
			cache = alterers.Aggregate(raw, (v, alt) => alt.Apply(v));
			if (!prev.Equals(cache)) {
				if (!isModified) {
					_original = raw;
					isModified = true;
				}
				onChanged.Invoke();
				onAttributeChanged?.Invoke();
			}
			return cache;
		}


		/// <summary> Adds or removes Alterers. When add is false all Alterers are removed by the creator regardless of the alterers argument.</summary>
		public Alterer<T, T2> ConfigureAlterer<T2>(bool add, Object creator, Func<T, T2, T> applier, Func<T2> updater, params Muc.Data.Event[] updateEvents) {
			return ConfigureAlterer(add, creator, applier, updater, updateEvents as IEnumerable<Muc.Data.Event>);
		}

		/// <summary> Adds or removes Alterers. When add is false all Alterers are removed by the creator regardless of the other arguments.</summary>
		public Alterer<T, T2> ConfigureAlterer<T2>(bool add, Object creator, Func<T, T2, T> applier, Func<T2> updater, IEnumerable<Muc.Data.Event> updateEvents = null) {
			Alterer<T, T2> res = null;
			if (add) {
				res = new(creator, updater, applier, () => Update(true));
				_alterers.Add(res);
				if (updateEvents != null) {
					foreach (var upEvent in updateEvents) {
						upEvent.ConfigureListener(add, res.Update);
					}
				}
			} else {
				if (updateEvents != null) {
					foreach (var alt in _alterers.Where(v => v.creator == creator)) {
						foreach (var upEvent in updateEvents) {
							upEvent.ConfigureListener(add, alt.Update);
						}
					}
				}
				_alterers.RemoveAll(v => v.creator == creator);
				Update(true);
			}
			return res;
		}

		#region Interfaces
		object IValueContainer.cache => cache;
		bool IValueContainer.isCached => isCached;
		object IValueContainer.value => value;
		object IValueContainer.raw => raw;
		object IValueContainer.original => original;
		bool IValueContainer.isModified => isModified;
		IList IValueContainer.alterers => _alterers;
		Muc.Data.Event IValueContainer.onChanged => onChanged;

		T IValueContainer<T>.cache => cache;
		bool IValueContainer<T>.isCached => isCached;
		T IValueContainer<T>.value => value;
		T IValueContainer<T>.raw => raw;
		T IValueContainer<T>.original => original;
		bool IValueContainer<T>.isModified => isModified;
		IReadOnlyList<Alterer<T>> IValueContainer<T>.alterers => alterers;
		Muc.Data.Event IValueContainer<T>.onChanged => onChanged;
		#endregion
	}

	[Serializable]
	public class EnabledContainer : ValueContainer<bool> {
		public static implicit operator bool(EnabledContainer v) => v.value;
		public EnabledContainer() : base(true) { }
		public EnabledContainer(bool enabled) : base(enabled) { }
	}
}

