using System.Buffers;
using System.Collections;
using System.Diagnostics;

using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.Niagara;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4ParseHelper
{
	// UEAData has a type and value, and can contain its own data properties or array contents
	//[DebuggerDisplay("{Name}")]
	public class UEAData : IEnumerable<UEAData>
	{
		// Statics

		public static readonly IEnumerable<UEAData> NotFound = [];

		public static string FloatFormat { get; set; } = "0.0#######";

		// Fields

		protected long _highestKeyNum = -1;

		public readonly Dictionary<string, UEAData> Data = [];

		// Properties

		public int Count { get => Data.Count; }

		public string DataType { get; set; }

		public bool IsArray { get; set; }

		public string Name { get; set; }

		public UEAData? Parent { get; set; }

		public UEADataValue Value { get; set; }

		// Constructors

		public UEAData(string name = "", string dataType = "")
		{
			Name = name;
			DataType = dataType;
			Value = new();
		}

		public UEAData(string name, string dataType, string value) : this(name, dataType) { Value.String = value; }

		public UEAData(string name, string dataType, int value) : this(name, dataType) { Value.Int = value; }

		public UEAData(string name, string dataType, uint value) : this(name, dataType) { Value.UInt = value; }

		public UEAData(string name, string dataType, float value) : this(name, dataType) { Value.Float = value; }

		public UEAData(string name, string dataType, object value) : this(name, dataType)
		{ 
			Value.Object = value;
			ParseCUE4Object(name, dataType, value);
		}

		// Indexers

		public UEAData? this[string name]
		{
			get
			{
				if (string.IsNullOrEmpty(name)) { return null; }

				if (Data.TryGetValue(name, out var result)) { return result; }

				if (name.Contains('.'))
				{
					// It's a path to traverse

					return Find(name);
				}

				return null;
			}
			set
			{
				bool emptyName = string.IsNullOrWhiteSpace(name);
				if (!emptyName) { name = name.Trim(); } // Remove any leading or trailing whitespace
				if (value == null)
				{
					if (!emptyName)
					{
						Data.Remove(name);
					}
				}
				else
				{
					bool emptyValueName = string.IsNullOrWhiteSpace(value.Name);
					if (!emptyValueName) { value.Name = value.Name.Trim(); } // Remove any leading or trailing whitespace

					if (IsArray)
					{
						if (emptyName)
						{
							_highestKeyNum++;
							name = _highestKeyNum.ToString();
							emptyName = false;
						}
						else if (long.TryParse(name, out var index))
						{
							if (index > _highestKeyNum) { _highestKeyNum = index; }
						}

						if (emptyValueName)
						{
							value.Name = name;
							emptyValueName = false;
						}

						// NOTE: Unchecked situation - Non-int key being supplied to an Array via indexer
					}
					else
					{
						if (emptyName && emptyValueName)
						{
							// We have no key to add this data under, and we're not an array for adding a new index to.
							return;
						}
						else if (emptyName)
						{
							name = value.Name;
							emptyName = false;
						}
						else if (emptyValueName)
						{
							value.Name = name;
							emptyValueName = false;
						}
					}

					value.Parent = this;

					Data[name] = value;
				}
			}
		}

		// Methods

		public void Add(UEAData data) { if (data.Name != null) { this[data.Name] = data; } }

		/// <summary>Traverses a node path to find the descendant Data
		/// </summary>
		/// <param name="dataPath">Node path (e.g., "Field.Child.ArrayChild.0.Descendant")</param>
		/// <example>Find("Field.Child.ArrayChild.0.Descendant")</example>
		/// <returns>The descendant Data if found; otherwise null</returns>
		public UEAData? Find(string dataPath) => Find(dataPath.Split('.'), 0);

		public UEAData? Find(string[] dataPath, int position)
		{
			if ((position < 0) || (position >= dataPath.Length))
			{
				return null;
			}

			UEAData? next;
			string name = dataPath[position++];
			next = Data.GetValueOrDefault(name);

			if (next == null)
			{
				// We didn't find a Data with the exact name
				if (name.EndsWith(']'))
				{
					// Looks like they sent an array-style reference (e.g., "Field.ArrayChild[5].Property"). Need to parse it into a child path.

					int indexStart = name.IndexOf('[');
					string childName = name.Substring(indexStart + 1, name.Length - indexStart - 2);
					name = name.Remove(indexStart);

					next = Data.GetValueOrDefault(name);
					if (next != null)
					{
						// We found the parent node, now continue traversing with the child node (if found).
						next = next[childName];
					}
					else
					{
						return null;
					}
				}
				else
				{
					return null;
				}
			}

			if (position >= dataPath.Length)
			{
				return next;
			}
			else
			{
				return next?.Find(dataPath, position);
			}
		}

		public UEAExport? GetExport()
		{
			if (this is UEAExport me) { return me; }
			
			UEAData? parent = Parent;

			while (parent != null)
			{
				if (parent is UEAExport export)
				{
					return export;
				}
				else
				{
					parent = parent.Parent;
				}
			}

			return null;
		}

		public UEAData? GetFirst() => Data.Values.FirstOrDefault();

		public UEAPackage? GetPackage()
		{
			if (this is UEAPackage me) { return me; }

			UEAData? parent = Parent;

			while (parent != null)
			{
				if (parent is UEAPackage package)
				{
					return package;
				}
				else
				{
					parent = parent.Parent;
				}
			}

			return null;
		}

		public string[] GetSortedDataNames()
		{
			string[] keys = Data.Keys.ToArray();
			Array.Sort(keys);
			return keys;
		}

		public bool HasData() => (Data.Count > 0);

		public bool HasValue() => (Value.ValueType != UEADataValue.Type.None);

		protected void ParseCUE4Object(string name, string? propertyDataType, object? property)
		{
			if (property == null) { return; }
			string propertyType = propertyDataType ?? DataType;

			switch (property)
			{
				case StructProperty structData:
					// StructProperties are objects with their own data
					// [DebuggerDisplay("{Name,nq}: Struct")]
					IUStruct? child = structData.Value?.StructType;
					ParseCUE4Object(name, propertyType, child);
					break;
				case NameProperty nameData:
					// NameProperties contain text that's often repeated, so it gets stored as a pointer to an entry in the package's Name Map
					// [DebuggerDisplay("{Name,nq}: {Value.String} (N#{_nameIndex,d})")]
					Add(new("NameIndex", "Int", nameData.Value.Index));
					Value.String = nameData.Value.Text;
					break;
				case TextProperty textData:
					// TextProperties contain text that can have format codes or keys to external string tables
					// [DebuggerDisplay("{Name,nq}: {Value.String} (?????)")]
					if (textData.Value != null)
					{
						FText fText = textData.Value;
						Add(new("Type", "String", fText.HistoryType.ToString()));

						switch (fText.HistoryType)
						{
							case ETextHistoryType.StringTableEntry:
								Add(new("Flags", "UInt", fText.Flags));
								FTextHistory.StringTableEntry stHist = (FTextHistory.StringTableEntry)fText.TextHistory;
								Add(new("TableId", "String", stHist.TableId.Text)); // FName
								Add(new("Key", "String", stHist.Key));
								break;
							case ETextHistoryType.None:
								Add(new("Flags", "UInt", fText.Flags));
								break;
							case ETextHistoryType.Base:
								Add(new("Flags", "UInt", fText.Flags));
								FTextHistory.Base baseHist = (FTextHistory.Base)fText.TextHistory;
								Add(new("Namespace", "String", baseHist.Namespace));
								Add(new("Key", "String", baseHist.Key));
								break;
							default:
								Debugger.Break();
								break;
						}

						Value.String = fText.Text ?? "";
					}
					break;
				case ObjectProperty objectProp:
					ParseCUE4Object(name, propertyType, objectProp.Value);
					break;
				case FPackageIndex objectRef:
					Add(new("PackageIndex", "Int", objectRef.Index));
					Add(new("PackageIndexType", "String", objectRef.IsImport ? "Import" : objectRef.IsExport ? "Export" : "Unknown"));

					// Unfortunately, CUE4Parse appears to hide the ImportMap and other necessary information to parse the FPackageIndex without resolving the object.
					// So we have to resolve it first, which means it gets loaded and CUE4Parsed from whatever package it's in elsewhere in the game files, just to get the asset filename and import/export name.

					// Note: Don't try to step into or over this line. It tries to load packages on another thread and crashes with an exception.
					// Have to make a Breakpoint on the next line and Continue.
					var refObject = objectRef.ResolvedObject;
					if (refObject != null)
					{
						Add(new("ObjectName", "String", refObject.Name.Text));
						string refObjectClassName = refObject.Class?.Name.Text ?? "";
						string refObjectOuterClassName = refObject.Class?.Outer?.Name.Text ?? "";
						if (!string.IsNullOrEmpty(refObjectClassName) && !string.IsNullOrEmpty(refObjectOuterClassName))
						{
							refObjectClassName = refObjectOuterClassName + '.' + refObjectClassName;
						}
						Add(new("ObjectClass", "String", refObjectClassName));
						Add(new("ObjectAsset", "String", refObject.Outer?.Name.Text ?? ""));
					}
					break;
				case ByteProperty byteProp:
					Value.UInt = byteProp.Value;
					break;
				case IntProperty intProp:
					Value.Int = intProp.Value;
					break;
				case UInt16Property uint16Prop:
					Value.UInt = uint16Prop.Value;
					break;
				case byte ibyte:
					Value.UInt = ibyte;
					break;
				case FloatProperty floatProp:
					Value.Float = floatProp.Value;
					break;
				case StrProperty stringProp:
					Value.String = stringProp.Value ?? "";
					break;
				case BoolProperty boolProp:
					Value.String = boolProp.Value ? "true" : "false";
					break;
				case EnumProperty enumProp:
					Value.String = enumProp.Value.Text;
					break;
				case ArrayProperty arrayProp:
					UScriptArray? array = arrayProp.Value;
					ParseCUE4Object(name, propertyType, array);
					break;
				case UScriptArray scriptArrayProp:
					IsArray = true;
					string arrayElementType = (scriptArrayProp.InnerTagData?.StructType ?? scriptArrayProp.InnerType ?? "Unknown");
					if (arrayElementType == "Unknown") { Debugger.Break(); }
					foreach (FPropertyTagType element in scriptArrayProp.Properties)
					{
						// Leaving as "" and generating the index-based name at array insertion doesn't work when sub-properties need a name for this object.
						Add(new(Count.ToString(), arrayElementType, element));
					}
					break;
				case SoftObjectProperty softobjProp:
					Value.String = softobjProp.Value.AssetPathName.Text;
					break;
				case MapProperty mapProp:
					var map = mapProp.Value;
					if (map != null)
					{
						foreach (var mapEntry in map.Properties)
						{
							Add(new(mapEntry.Key.ToString(), "MapValue<" + mapEntry.Key.GetType().ToString() + "," + (mapEntry.Value?.GetType().ToString() ?? null) + ">", mapEntry.Value?.ToString() ?? ""));
						}
					}
					break;
				case FieldPathProperty fieldPathProp:
					FFieldPath? path = fieldPathProp.Value;
					if (path != null)
					{
						List<string> pathList = [];
						foreach (FName pathName in path.Path)
						{
							pathList.Add(pathName.Text);
						}
						Add(new(name, propertyType, string.Join('.', pathList)));
					}
					break;
				case SetProperty setProp:
					IsArray = true;
					UScriptSet? set = setProp.Value;
					if (set?.Properties.Count > 0)
					{
						string setElementType = propertyDataType ?? "SetElement";
						var bracketPos = setElementType.IndexOf('<');
						if (bracketPos >= 0)
						{
							setElementType = setElementType.Substring(bracketPos + 1, setElementType.Length - bracketPos - 2);
						}
						foreach (FPropertyTagType element in set.Properties)
						{
							Add(new("", setElementType, element));
						}
					}
					break;
				case AbstractPropertyHolder fStruct:
					foreach (var structProperty in fStruct.Properties)
					{
						if (structProperty.Tag != null)
						{
							Add(new(structProperty.Name.Text, structProperty.TagData?.ToString() ?? "StructProperty", structProperty.Tag));
						}
					}
					break;
				case FPropertyTag fProperty:
					ParseCUE4Object(fProperty.Name.Text, fProperty.TagData?.ToString(), fProperty.Tag);
					break;
				case FPropertyTagType fPropertyType:
					ParseCUE4Object(name, propertyDataType, fPropertyType.GenericValue);
					break;
				case FScriptStruct fsStruct:
					ParseCUE4Object(name, propertyDataType, fsStruct.StructType);
					break;
				case FGameplayTagContainer gameTags:
					IsArray = true;
					foreach (FGameplayTag gtag in gameTags.GameplayTags)
					{
						Add(new("", "GameplayTag", gtag.TagName.Text));
					}
					break;
				case FLinearColor linearColor:
					Add(new("R", "Float", linearColor.R));
					Add(new("G", "Float", linearColor.G));
					Add(new("B", "Float", linearColor.B));
					Add(new("A", "Float", linearColor.A));
					Value.String = linearColor.Hex;
					break;
				case FSoftObjectPath softObjProp:
					Value.String = softObjProp.AssetPathName.Text;
					break;
				case FName fName:
					Add(new("NameIndex", "Int", fName.Index));
					Value.String = fName.Text;
					break;
				case FGuid guid:
					Value.String = Value.Object?.ToString() ?? "";
					break;
				case FRotator rotator:
					Add(new("Pitch", "Float", rotator.Pitch));
					Add(new("Roll", "Float", rotator.Roll));
					Add(new("Yaw", "Float", rotator.Yaw));
					break;
				case FNiagaraVariableWithOffset nvwo:
					Add(new("Name", "String", nvwo.Name.Text)); //FName
					Add(new("Offset", "Int", nvwo.Offset));
					Add(new("TypeDef", "Struct", nvwo.TypeDef));
					break;
				case FVector fVector:
					Add(new("X", "Float", fVector.X));
					Add(new("Y", "Float", fVector.Y));
					Add(new("Z", "Float", fVector.Z));
					break;
				case FColor fColor:
					Add(new("A", "Byte", fColor.A));
					Add(new("R", "Byte", fColor.R));
					Add(new("G", "Byte", fColor.G));
					Add(new("B", "Byte", fColor.B));
					//Add(new("Hex", "String", fColor.Hex));
					Value.String = fColor.Hex;
					break;
				case FIntPoint fIntPoint:
					Add(new("X", "UInt", fIntPoint.X));
					Add(new("Y", "UInt", fIntPoint.Y));
					break;
				case FQuat fQuat:
				case FRichCurveKey fRichCurveKey:
					// TODO: Break these out if desired in the future. We're not actually using them for our purpose.
					break;
				default:
					Debugger.Break();
					break;
			}
		}

		public void Remove(string name) => Data.Remove(name ?? "");

		public string SafeText(string dataName) => this[dataName]?.Value.String ?? "";

		public int SafeInt(string dataName) => this[dataName]?.Value.Int ?? 0;

		public uint SafeUInt(string dataName) => this[dataName]?.Value.UInt ?? 0u;

		public float SafeFloat(string dataName) => this[dataName]?.Value.Float ?? 0.0f;

		public override string ToString()
		{
			switch (Value.ValueType)
			{
				case UEADataValue.Type.String:
				case UEADataValue.Type.Int:
				case UEADataValue.Type.UInt:
				case UEADataValue.Type.Float:
					return Value.String;
			}

			// Special handling for various data types, if desired.
			switch (DataType)
			{
				case "ObjectReference":
					string? objectRef = this["PackageIndex"]?.Value.String;
					return (objectRef == null) ? DataType + " (Empty)" : (DataType + ':' + objectRef);
			}

			return DataType + " [" + Count.ToString() + "]";
		}

		// Enumerators

		public IEnumerator<UEAData> GetEnumerator() => Data.Values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => Data.Values.GetEnumerator();

		// Conversions

		public static implicit operator string(UEAData Data) => Data?.ToString() ?? "";
	}

	[DebuggerDisplay("{ToString(),nq} ({ValueType})")]
	public class UEADataValue
	{
		public static readonly UEADataValue Empty = new();

		public enum Type
		{
			None = 0,
			Int = 1,
			UInt = 2,
			Float = 3,
			String = 4,
			Object = 5
		}

		// Fields
		protected int _valueInt;
		protected uint _valueUInt;
		protected float _valueFloat;
		protected string? _valueString;
		protected object? _valueObject;

		protected bool _hasInt;
		protected bool _hasUInt;
		protected bool _hasFloat;

		protected Type _type;

		// Properties
		public Type ValueType { get => _type; }

		public int Int
		{
			get
			{
				if (_hasInt) return _valueInt;
				_hasInt = true;
				if (_hasUInt) return _valueInt = (int)_valueUInt;
				if (_hasFloat) return _valueInt = (int)_valueFloat;
				
				// TryParse handles nulls fine
				if (int.TryParse(_valueString, out _valueInt)) return _valueInt;

				return _valueInt = default;
			}
			set
			{
				_type = Type.Int;
				_valueInt = value;
				_hasInt = true;
				_hasUInt = false;
				_hasFloat = false;
				_valueString = null;
				_valueObject = null;
			}
		}

		public uint UInt
		{
			get
			{
				if (_hasUInt) return _valueUInt;
				_hasUInt = true;
				if (_hasInt) return _valueUInt = (uint)_valueInt;
				if (_hasFloat) return _valueUInt = (uint)_valueFloat;

				// TryParse handles nulls fine
				if (uint.TryParse(_valueString, out _valueUInt)) return _valueUInt;

				return _valueUInt = default;
			}
			set
			{
				_type = Type.UInt;
				_valueUInt = value;
				_hasUInt = true;
				_hasInt = false;
				_hasFloat = false;
				_valueString = null;
				_valueObject = null;
			}
		}

		public float Float
		{
			get
			{
				if (_hasFloat) return _valueFloat;
				_hasFloat = true;
				if (_hasInt) return _valueFloat = _valueInt;
				if (_hasUInt) return _valueFloat = _valueUInt;

				// TryParse handles nulls fine
				if (float.TryParse(_valueString, out _valueFloat)) return _valueFloat;

				return _valueFloat = default;
			}
			set
			{
				_type = Type.Float;
				_valueFloat = value;
				_hasFloat = true;
				_hasInt = false;
				_hasUInt = false;
				_valueString = null;
				_valueObject = null;
			}
		}

		public string String
		{
			get
			{
				if (_valueString != null) return _valueString;

				_valueString = _type switch
				{
					Type.String => "", // How did we get a Type.String without a _valueString?
					Type.Int => _valueInt.ToString(),
					Type.UInt => _valueUInt.ToString(),
					Type.Float => _valueFloat.ToString(UEAData.FloatFormat),
					Type.Object => _valueObject?.ToString() ?? "(null)",
					_ => "",
				};
				return _valueString;
			}
			set
			{
				_type = Type.String;
				_valueString = value ?? "";
				_hasInt = _hasUInt = _hasFloat = false;
				_valueObject = null;
			}
		}

		public object? Object
		{
			get
			{
				if (_type == Type.Object) return _valueObject;

				switch (_type)
				{
					case Type.String:
						return _valueString;
					case Type.Int:
						return _valueInt;
					case Type.UInt:
						return _valueUInt;
					case Type.Float:
						return _valueFloat;
				}

				return null;
			}
			set
			{
				_type = Type.Object;
				_valueObject = value;
				_valueString = null;
				_hasInt = _hasUInt = _hasFloat = false;
			}
		}

		// Methods

		public override string ToString()
		{
			switch (_type)
			{
				case Type.String:
				case Type.Int:
				case Type.UInt:
				case Type.Float:
				case Type.Object:
					return String;
				default:
					return "";
			}
		}

		// Conversions
		public static implicit operator string(UEADataValue dataValue) => dataValue.ToString();
	}
}