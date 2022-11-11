
namespace Kalsium.Serialization {

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using Muc.Extensions;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using Newtonsoft.Json.Serialization;
	using UnityEngine;
	using Object = UnityEngine.Object;

	public static class GameSerializer {

		public static string Serialize(Game game) {
			return SerializeGame(game);
		}

		public static void Deserialize(string json, Game target) {
			DeserializeGame(json, target);
		}

		public delegate JToken Tokenizer(object obj);
		public delegate object Detokenizer(Type type, JToken j, object obj);

		class Converter {
			public Tokenizer tokenizer;
			public Detokenizer detokenizer;

			public Converter(Tokenizer serializer, Detokenizer detokenizer) {
				this.tokenizer = serializer;
				this.detokenizer = detokenizer;
			}
		}

		static Dictionary<Type, Converter> unityTypes = new() {
			{
				typeof(Vector2),
				new(
					(o) => { var v = (Vector2)o; return new JArray(v.x, v.y); },
					(t, j, o) => { var a = (JArray)j; return new Vector2((float)a[0], (float)a[1]); }
				)
			},
			{
				typeof(Vector3),
				new(
					(o) => { var v = (Vector3)o; return new JArray(v.x, v.y, v.z); },
					(t, j, o) => { var a = (JArray)j; return new Vector3((float)a[0], (float)a[1], (float)a[2]); }
				)
			},
			{
				typeof(Vector4),
				new(
					(o) => { var v = (Vector4)o; return new JArray(v.x, v.y, v.z, v.w); },
					(t, j, o) => { var a = (JArray)j; return new Vector4((float)a[0], (float)a[1], (float)a[2], (float)a[3]); }
				)
			},
			{
				typeof(Vector2Int),
				new(
					(o) => { var v = (Vector2Int)o; return new JArray(v.x, v.y); },
					(t, j, o) => { var a = (JArray)j; return new Vector2Int((int)a[0], (int)a[1]); }
				)
			},
			{
				typeof(Vector3Int),
				new(
					(o) => { var v = (Vector3Int)o; return new JArray(v.x, v.y, v.z); },
					(t, j, o) => { var a = (JArray)j; return new Vector3Int((int)a[0], (int)a[1], (int)a[2]); }
				)
			},
			{
				typeof(Rect),
				new(
					(o) => { var v = (Rect)o; return new JArray(v.x, v.y, v.width, v.height); },
					(t, j, o) => { var a = (JArray)j; return new Rect((float)a[0], (float)a[1], (float)a[2], (float)a[3]); }
				)
			},
			{
				typeof(RectInt),
				new(
					(o) => { var v = (RectInt)o; return new JArray(v.x, v.y, v.width, v.height); },
					(t, j, o) => { var a = (JArray)j; return new RectInt((int)a[0], (int)a[1], (int)a[2], (int)a[3]); }
				)
			},
			{
				typeof(Quaternion),
				new(
					(o) => { var v = (Quaternion)o; return new JArray(v.x, v.y, v.z, v.w); },
					(t, j, o) => { var a = (JArray)j; return new Quaternion((float)a[0], (float)a[1], (float)a[2], (float)a[3]); }
				)
			},
			{
				typeof(Color),
				new(
					(o) => { var v = (Color)o; return new JArray(v.r, v.g, v.b, v.a); },
					(t, j, o) => { var a = (JArray)j; return new Color((float)a[0], (float)a[1], (float)a[2], (float)a[3]); }
				)
			},
			{
				typeof(Color32),
				new(
					(o) => { var v = (Color32)o; return new JArray(v.r, v.g, v.b, v.a); },
					(t, j, o) => { var a = (JArray)j; return new Color32((byte)a[0], (byte)a[1], (byte)a[2], (byte)a[3]); }
				)
			},
			{
				typeof(LayerMask),
				new(
					(o) => { var v = (LayerMask)o; return new JValue(v.value); },
					(t, j, o) => { var v = (JValue)j; return (LayerMask)(int)v; }
				)
			},
			{
				typeof(RectOffset),
				new(
					(o) => { var v = (RectOffset)o; return new JArray(v.left, v.right, v.top, v.bottom); },
					(t, j, o) => { var a = (JArray)j; return new RectOffset((int)a[0], (int)a[1], (int)a[2], (int)a[3]); }
				)
			},
			{
				typeof(AnimationCurve),
				new(
					null,
					null
				)
			},
			{
				typeof(Matrix4x4),
				new(
					null,
					null
				)
			},
			{
				typeof(Gradient),
				new(
					null,
					null
				)
			},
		};

		static void DeserializeGame(string json, Game game) {
			using (new Muc.Stopwatch("Deserialization took")) {
				var objs = new Dictionary<uint, Object>() { { 1, game } };
				var cbList = new HashSet<ISerializationCallbackReceiver>();
				var stack = new Stack<uint>();
				stack.Push(1);

				var data = JObject.Parse(json);
				var props = data.Properties().ToDictionary(v => uint.Parse(v.Name), v => v.Value as JObject);

				while (stack.Count > 0) {
					var id = stack.Pop();
					var jo = props[id];
					Deserialize(id, jo);
				}

				foreach (var icb in cbList) {
					try {
						icb.OnAfterDeserialize();
					} catch (System.Exception e) {
						Debug.LogError($"Exception occurred during `{icb.GetType().Name} ISerializationCallbackReceiver.OnAfterDeserialize`: ${e}", icb as Object);
					}
				}

				Object GetObj(uint id, Object remainingValue) {
					if (objs.TryGetValue(id, out var obj)) return obj;
					var type = Type.GetType((string)props[id]["$type"]);
					var refTok = type.GetCustomAttribute<RefTokenAttribute>();
					if (refTok == null) throw new InvalidOperationException($"Type is not legal ({type.FullName})");
					if (remainingValue != null && refTok.KeepRef(remainingValue)) return objs[id] = remainingValue;
#pragma warning disable UNT0007
					var res = objs[id] = refTok.CreateObject(props[id]) ?? ScriptableObject.CreateInstance(type);
#pragma warning restore UNT0007
					res.name = id.ToString();
					return res;
				}

				void Deserialize(uint id, JObject jo) {
					var obj = GetObj(id, null);
					if (DefaultDetokenizer(obj.GetType(), jo, obj) is ISerializationCallbackReceiver isg) {
						cbList.Add(isg);
					}


					object DefaultDetokenizer(Type type, JToken j, object obj) {

						var jo = (JObject)j;

						obj ??= InstantiateType(type);

						foreach (var d in GetFields(type)) {
							var detokenizer = d.detokenizer ?? DefaultDetokenizer;
							var value = d.field.GetValue(obj);

							// List
							if (d.asList) {
								var list = (IList)(value ?? InstantiateType(d.field.FieldType));
								var ja = (JArray)jo[d.field.Name];

								if (list.Count != ja.Count) {
									if (list is Array arr) list = Resize(arr, ja.Count);
									else ResizeList(ref list, d.itemType, ja.Count);
								}

								if (d.isUnityObject) {
									for (int i = 0; i < ja.Count; i++) {
										var jt = ja[i];
										if (jt.Type == JTokenType.Null) {
											d.field.SetValue(obj, null);
										} else {
											var id = (uint)jt;
											if (!objs.TryGetValue(id, out var objValue)) {
												objValue = GetObj(id, list[i] as Object);
												stack.Push(id);
											}
											QueueOnAfterDeserialize(list[i] = objValue, d);
										}
									}
								} else {
									if (d.serializeReference) {
										// { "$type": ... , "$value": ... }
										for (int i = 0; i < ja.Count; i++) {
											var jt = ja[i];
											if (
												jt is JObject jto
												&& jto["$value"] is JToken jval
												&& jto["$type"] is JValue jv
												&& jv.Value is string str
												&& Type.GetType(str) is Type jtype
												&& d.itemType.IsAssignableFrom(jtype)
											) {
												if (jval is JValue jvalval && jvalval.Value == null) {
													list[i] = null;
												} else {
													QueueOnAfterDeserialize(list[i] = detokenizer(jtype, jval, list[i]), d);
												}
											}
										}
									} else {
										for (int i = 0; i < ja.Count; i++) {
											var jt = ja[i];
											var val = detokenizer(d.itemType, jt, list[i]);
											QueueOnAfterDeserialize(list[i] = val, d);
										}
									}
								}

								d.field.SetValue(obj, list);
								QueueOnAfterDeserialize(list, d);
								continue;
							}

							// SerializeReference
							if (d.serializeReference) {
								// { "$type": ... , "$value": ... }
								if (
									jo["$value"] is JToken jval
									&& jo["$type"] is JValue jv
									&& jv.Value is string str
									&& Type.GetType(str) is Type jtype
									&& d.field.FieldType.IsAssignableFrom(jtype)
								) {
									if (jval is JValue jvalval && jvalval.Value == null) {
										d.field.SetValue(obj, null);
									} else {
										var value1 = detokenizer(jtype, jval, value);
										d.field.SetValue(obj, value1);
										QueueOnAfterDeserialize(value1, d);
									}
									continue;
								} else {
									d.field.SetValue(obj, null);
									continue;
								}
							}

							// Ref
							if (d.isUnityObject) {
								var jref = (JValue)jo[d.field.Name];
								if (jref.Type == JTokenType.Null) {
									d.field.SetValue(obj, null);
								} else {
									var id = (uint)jref;
									if (!objs.TryGetValue(id, out var objValue)) {
										objValue = GetObj(id, value as Object);
										stack.Push(id);
									}
									d.field.SetValue(obj, objValue);
									QueueOnAfterDeserialize(objValue, d);
								}
								continue;
							}

							// Default
							var value2 = detokenizer(d.field.FieldType, jo[d.field.Name], value);
							d.field.SetValue(obj, value2);
							QueueOnAfterDeserialize(value2, d);
						}

						return obj;
					}

					void QueueOnAfterDeserialize(object obj, FieldData d) {
						if (d.isCallbackReceiver && !Object.Equals(obj, null)) {
							cbList.Add((ISerializationCallbackReceiver)obj);
						}
					}
				}
			}
		}

		static void ResizeList(ref IList list, Type type, int newSize) {
			while (list.Count < newSize) {
				list.Add(type.IsValueType ? Activator.CreateInstance(type) : null);
			}
			while (list.Count > newSize) {
				list.RemoveAt(list.Count - 1);
			}
		}

		static Array Resize(Array array, int newSize) {
			Type elementType = array.GetType().GetElementType();
			Array newArray = Array.CreateInstance(elementType, newSize);
			Array.Copy(array, newArray, Math.Min(array.Length, newArray.Length));
			return newArray;
		}

		static object InstantiateType(Type type, bool allowAbstract = false) {
			if (type == typeof(string)) return null;
			if (allowAbstract && type.IsAbstract) return null;
			var ci = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
			if (ci != null) return ci.Invoke(null);
			return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
		}

		static string SerializeGame(Game game) {
			using (new Muc.Stopwatch("Serialization took")) {
				const int maxDepth = 7;
				var depthReached = 0;
				var stack = new Stack<(uint, Object)>();
				uint topId = 1;
				stack.Push((topId, game));
				var tokens = new Dictionary<uint, JToken>() { { topId, null } };
				var toId = new Dictionary<object, uint>() { { game, topId } };
				while (stack.Count > 0) {
					var pair = stack.Pop();
					Serialize(pair.Item1, pair.Item2);
				}
				if (depthReached > 0) Debug.LogWarning($"Max depth reached {depthReached} times.");
				var res = JsonConvert.SerializeObject(tokens, Formatting.Indented);
				return res;

				void Serialize(uint id, Object obj) {
					var depth = 0;
					if (obj is ISerializationCallbackReceiver irc) irc.OnBeforeSerialize();
					tokens[id] = DefaultTokenizer(obj);

					JToken DefaultTokenizer(object obj) {
						if (++depth > maxDepth) {
							depthReached++;
							return JValue.CreateNull();
						}
						var jo = new JObject();
						var type = obj.GetType();
						if (depth == 1) { // First item is IGameSerializable and needs a type field
							jo.Add("$type", type.GetShortQualifiedName());
						}

						foreach (var d in GetFields(type)) {
							var value = d.field.GetValue(obj);
							var tokenizer = d.tokenizer ?? DefaultTokenizer;

							if (d.isCallbackReceiver && !Object.Equals(value, null)) ((ISerializationCallbackReceiver)value).OnBeforeSerialize();

							// List
							if (d.asList) {
								var arrayToken = new JArray();
								if (d.isUnityObject) {
									if (value != null) {
										foreach (var e in (IList)value) {
											if (e is null) {
												arrayToken.Add(null);
											} else if (toId.TryGetValue(e, out var otherId)) {
												arrayToken.Add(otherId);
											} else {
												stack.Push((++topId, (Object)e));
												toId[e] = topId;
												tokens[topId] = null;
												arrayToken.Add(topId);
											}
											continue;
										}
									}
								} else {
									if (d.serializeReference) {
										foreach (var v in (IList)value) {
											if (v is null) {
												arrayToken.Add(JValue.CreateNull());
											} else {
												arrayToken.Add(new JObject(
													new JProperty("$type", v.GetType().GetShortQualifiedName()),
													new JProperty("$value", tokenizer(v))
												));
											}
										}
									} else {
										if (value != null) {
											foreach (var v in (IList)value) {
												arrayToken.Add(tokenizer(v));
											}
										}
									}
								}
								jo.Add(d.field.Name, arrayToken);
								continue;
							}

							// Null?
							if (Object.Equals(value, null)) {
								jo.Add(d.field.Name, null);
								continue;
							}

							// Ref
							if (d.isUnityObject) {
								if (toId.TryGetValue(value, out var otherId)) {
									jo.Add(d.field.Name, otherId);
									continue;
								} else {
									stack.Push((++topId, (Object)value));
									toId[value] = topId;
									tokens[topId] = null;
									jo.Add(d.field.Name, topId);
									continue;
								}
							}

							// SerializeReference
							if (d.serializeReference) {
								jo.Add(d.field.Name, new JObject(
									new JProperty("$type", value.GetType().GetShortQualifiedName()),
									new JProperty("$value", tokenizer(value))
								));
								continue;
							}

							// Default
							jo.Add(d.field.Name, tokenizer(value));
						}
						depth--;
						return jo;
					}
				}
			}
		}

		private class FieldData {

			public FieldInfo field;
			public Tokenizer tokenizer;
			public Detokenizer detokenizer;
			public Type itemType;
			public bool hasTypeAttribute;
			public bool hasFieldAttribute;
			public bool isUnityObject;
			public bool isCallbackReceiver;
			public bool serializeReference;
			public bool asList;
		}

		static Dictionary<Type, List<FieldData>> fieldDataListCache = new();

		static IEnumerable<FieldData> GetFields(Type type) {

			if (fieldDataListCache.TryGetValue(type, out var res)) {
				return res;
			}

			var fields = new List<FieldData>();

			foreach (var field in GetAllFields(type)) {
				var data = ValidateField(field);
				if (data != null) {
					fields.Add(data);
				}
			}

			return fieldDataListCache[type] = fields;
		}

		static Dictionary<FieldInfo, FieldData> fieldDataCache = new();

		static FieldData ValidateField(FieldInfo field) {
			if (fieldDataCache.TryGetValue(field, out var cached)) {
				return cached;
			}
			var d = new FieldData {
				field = field
			};
			if (field.IsPublic) {
				if (System.Attribute.IsDefined(field, typeof(NonSerializedAttribute))) {
					return fieldDataCache[field] = null;
				}
				d.serializeReference = System.Attribute.IsDefined(field, typeof(SerializeReference));
			} else {
				d.serializeReference = System.Attribute.IsDefined(field, typeof(SerializeReference));
				if (!d.serializeReference && !System.Attribute.IsDefined(field, typeof(SerializeField))) {
					return fieldDataCache[field] = null;
				}
			}

			var fieldAttribute = field.GetCustomAttribute<TokenizeAttribute>();
			d.hasFieldAttribute = fieldAttribute != null;

			var isArray = typeof(Array).IsAssignableFrom(field.FieldType);
			var listType = field.FieldType.GetGenericTypeOf(typeof(List<>));
			if ((isArray || listType != null) && !(d.hasFieldAttribute && fieldAttribute.applyToList)) {
				d.asList = true;
				d.itemType = isArray ? field.FieldType.GetElementType() : listType;
			} else {
				d.itemType = field.FieldType;
			}

			d.isUnityObject = typeof(Object).IsAssignableFrom(d.itemType);
			d.isCallbackReceiver = typeof(ISerializationCallbackReceiver).IsAssignableFrom(d.itemType);

			if (d.isUnityObject) {
				var refAttribute = d.itemType.GetCustomAttribute<RefTokenAttribute>();
				if (refAttribute != null) {
					if (!refAttribute.IsTokenized(d.itemType)) return fieldDataCache[field] = null;
					d.tokenizer = refAttribute.GetTokenizer(d.itemType);
					d.detokenizer = refAttribute.GetDetokenizer(d.itemType);
					d.hasTypeAttribute = true;
					return fieldDataCache[field] = d;
				}
				Debug.LogWarning($"Attempting to serialize UnityObject without {nameof(RefTokenAttribute)}. {d.field.DeclaringType.Name}.{d.field.Name}");
				return fieldDataCache[field] = null;
			}

			if (d.hasFieldAttribute) {
				if (!fieldAttribute.IsTokenized(d.itemType)) return fieldDataCache[field] = null;
				d.tokenizer = fieldAttribute.GetTokenizer(d.itemType);
				d.detokenizer = fieldAttribute.GetDetokenizer(d.itemType);
				if (d.tokenizer != null || d.detokenizer != null) return fieldDataCache[field] = d;
			}

			if (d.asList) {
				var rawTypeAttribute = field.FieldType.GetCustomAttribute<TokenizeAttribute>(true);
				d.hasTypeAttribute = rawTypeAttribute != null;
				if (rawTypeAttribute != null) {
					if (!rawTypeAttribute.IsTokenized(d.itemType)) return fieldDataCache[field] = null;
					d.tokenizer = rawTypeAttribute.GetTokenizer(d.itemType);
					d.detokenizer = rawTypeAttribute.GetDetokenizer(d.itemType);
					if (d.tokenizer != null || d.detokenizer != null) {
						d.asList = false;
						d.itemType = field.FieldType;
						return fieldDataCache[field] = d;
					}
				}
			}

			var typeAttribute = d.itemType.GetCustomAttribute<TokenizeAttribute>(true);
			d.hasTypeAttribute |= typeAttribute != null;
			if (typeAttribute != null) {
				if (!typeAttribute.IsTokenized(d.itemType)) return fieldDataCache[field] = null;
				d.tokenizer = typeAttribute.GetTokenizer(d.itemType);
				d.detokenizer = typeAttribute.GetDetokenizer(d.itemType);
				if (d.tokenizer != null || d.detokenizer != null) return fieldDataCache[field] = d;
			}

			return fieldDataCache[field] = ValidateType(d.itemType, d, out d.tokenizer, out d.detokenizer) ? d : null;
		}

		static bool ValidateType(Type type, FieldData d, out Tokenizer tokenizer, out Detokenizer detokenizer) {
			tokenizer = null;
			detokenizer = null;

			var gsa = type.GetCustomAttribute<TokenizeAttribute>(true);
			if (gsa != null) {
				if (!gsa.IsTokenized(type)) return false;
				tokenizer = gsa.GetTokenizer(d.itemType);
				detokenizer = gsa.GetDetokenizer(d.itemType);
				if (tokenizer != null && detokenizer != null) return true;
			}

			if (type == typeof(string) || type.IsPrimitive || type.IsEnum) {
				tokenizer = value => new JValue(value);
				if (type.IsEnum) {
					detokenizer = (type, j, obj) => Enum.ToObject(type, (Int64)((JValue)j).Value);
				} else {
					detokenizer = (type, j, obj) => Convert.ChangeType(((JValue)j).Value, type);
				}
			} else {
				if (gsa != null) return true;
				if (type.IsClass || type.IsValueType) {
					if (!d.hasTypeAttribute && !d.hasFieldAttribute) {
						if (!d.serializeReference && type.IsAbstract) return false;
						if (unityTypes.TryGetValue(type, out var converter)) {
							tokenizer = converter.tokenizer;
							detokenizer = converter.detokenizer;
							if (tokenizer == null || detokenizer == null) return false;
						} else {
							if (d.isUnityObject) return false;
							if (!System.Attribute.IsDefined(type, typeof(SerializableAttribute), false)) return false;
							if (System.Attribute.IsDefined(type, typeof(NonSerializedAttribute), false)) return false;
						}
					}
				}
			}
			return true;
		}

		static IEnumerable<FieldInfo> GetAllFields(Type type) {
			if (type == null) return Enumerable.Empty<FieldInfo>();
			return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Concat(GetAllFields(type.BaseType));
		}

	}

}