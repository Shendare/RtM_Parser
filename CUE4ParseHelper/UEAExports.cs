using CUE4Parse;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using System.Collections;
using System.Data;
using System.Diagnostics;

namespace CUE4ParseHelper
{
	public class UEAExport : UEAData
	{
		public enum UEAEType
		{
			Unknown = 0,
			DataTable = 1,
			StringTable = 2,
			Blueprint = 3,
			DataObject = 4
		}

		// Properties
		public UEAEType ExportType { get; set; }

		// Constructors
		public UEAExport(UEAPackage package, UObject uObject) : base(uObject.Name, "UnknownExport")
		{
			Parent = package;
			Value.Object = uObject;

			Add(new UEAData("Flags", "Int", (int)uObject.Flags));
			Add(new UEAData("FlagsDesc", "String", uObject.Flags.ToString()));

			ParseExportType();
		}

		// Indexers

		// Methods
		protected void ParseExportType()
		{
			UEAData? table;

			switch (Value.Object)
			{
				case UStringTable stringTable:
					ExportType = UEAEType.StringTable;
					table = new UEAData("Data", ExportType.ToString());
					DataType = table.DataType + "Export";
					Add(table);

					foreach ((string key, string value) in stringTable.StringTable.KeysToEntries)
					{
						stringTable.StringTable.KeysToMetaData.TryGetValue(key, out var metaData);

						var stringTableEntry = new UEAData(key, "StringTableEntry", value);

						if (metaData != null)
						{
							foreach ((FName metaName, string metaValue) in metaData)
							{
								stringTableEntry.Add(new UEAData(metaName.Text, "StringTableEntryMetadata", metaValue));
							}
						}

						table.Add(stringTableEntry);
					}
					break;
				case UDataTable dataTable:
					ExportType = UEAEType.DataTable;
					table = new UEAData("Data", ExportType.ToString());
					DataType = table.DataType + "Export";
					Add(table);

					string? dataType = null;
					if (dataTable.Properties.Count > 0 && dataTable.Properties[0].Name.Text == "RowStruct")
					{
						dataType = (dataTable.Properties[0].Tag?.GenericValue as FPackageIndex)?.Name;
					}
					dataType ??= "DataTableEntry";

					if (dataTable.RowMap != null)
					{
						foreach ((FName fName, FStructFallback fStruct) in dataTable.RowMap)
						{
							table.Add(new UEAData(fName.Text, dataType, fStruct));
						}
					}
					break;
				case UBlueprintGeneratedClass blueprint:
					ExportType = UEAEType.Blueprint;
					table = new UEAData("Data", ExportType.ToString());
					DataType = table.DataType + "Export";
					Add(table);

					Add(new("IsCooked", "Bool", blueprint.bCooked ? "true" : "false"));
					Add(new("ClassFlags", "String", blueprint.ClassFlags.ToString()));
					Add(new("ClassConfigName", "String", blueprint.ClassConfigName.Text));
					Add(new("SuperStruct", "FPackageIndex", blueprint.SuperStruct));
					Add(new("SSPackageIndex", "Int", blueprint.SuperStruct.Index));
					// More blueprint properties might be available to be added if I want to be able to re-create an asset from helper data
					break;
				default:
					ExportType = UEAEType.DataObject;
					table = new UEAData("Data", ExportType.ToString(), Value.Object!);
					DataType = table.DataType + "Export";
					Add(table);
					break;
			}
		}

		public UEAData? GetData() => Data["Data"];

		public override string ToString()
		{
			//return Name + ": " + ExportType.ToString() + " (" + Data.DataType + ") [" + Data.Count + ']';
			return Name + ": " + ExportType.ToString() + " [" + (Data["Data"]?.Count ?? 0) + ']';
		}

		// Enumerators
	}
}
