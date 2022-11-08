
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using static Serialization.GameSerializer;

namespace Serialization {

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Enum)]
	public class TokenizeAttribute : System.Attribute {

		public bool applyToList { get; private set; }

		public TokenizeAttribute(bool applyToList = false) => this.applyToList = applyToList;

		/// <summary> If this returns false, the field won't be serialized by default. </summary>
		public virtual bool IsTokenized(Type type) => true;

		/// <summary> Return a Detokenizer to override the default one. </summary>
		public virtual Detokenizer GetDetokenizer(Type type) => null;
		/// <summary> Return a Tokenizer to override the default one. </summary>
		public virtual Tokenizer GetTokenizer(Type type) => null;

	}

	public sealed class DoNotTokenizeAttribute : TokenizeAttribute {
		public DoNotTokenizeAttribute(bool applyToList = false) : base(applyToList) { }
		public override bool IsTokenized(Type type) => false;
	}


	[AttributeUsage(AttributeTargets.Class)]
	public class RefTokenAttribute : TokenizeAttribute {

		public RefTokenAttribute() : base(false) { }

		public sealed override bool IsTokenized(Type type) => true;

		/// <summary>
		/// If true, the existing value of an Object serialized by reference is used instead of always creating a new one.
		/// This will make some objects retain UnityObject references that won't otherwise be kept.
		/// </summary>
		public virtual bool KeepRef(Object remainingValue) => false;

		/// <summary> Return an object to override the default object creation. </summary>
		public virtual UnityEngine.Object CreateObject(JToken jToken) => null;

	}

	public class KeepRefToken : RefTokenAttribute {
		public sealed override bool KeepRef(Object remainingValue) => true;
	}

}