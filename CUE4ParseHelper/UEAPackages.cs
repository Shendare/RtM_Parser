using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.UObject;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace CUE4ParseHelper
{
	public class UEAPackage : UEAData
	{
		// Fields

		protected UEGameFiles? _game;

		// Properties

		public IPackage? UEPackage { get; }

		// Constructors
		public UEAPackage(UEGameFiles game, IPackage package)
		{
			UEPackage = package;

			// TODO: Load package properties

			_game = game;
			string name = package.Name;
			string[] parts = name.Split('/');
			name = parts[parts.Length - 1];
			parts = name.Split('.');
			name = parts[0];
			Name = name;
			
			DataType = "Asset Package";

			Add(new("FullPath", "String", package.Name));

			UEAData exports = new UEAData("Exports", "Asset Exports");
			Add(exports);

			foreach (var lazyExport in package.ExportsLazy)
			{
				// Load export from the asset package
				var cue4export = lazyExport.Value;
				var newExport = new UEAExport(this, cue4export);

				exports.Add(newExport);

				// There's also an RF_Standalone flag for single-asset exports, but we don't
				// check for that since we make the first export we find the default anyway
				if (!Data.ContainsKey("DefaultExport") || (cue4export?.Flags & CUE4Parse.UE4.Assets.Exports.EObjectFlags.RF_ClassDefaultObject) != 0)
				{
					this["DefaultExport"] = newExport;
				}
			}
		}

		// Methods
		public UEAData? FindInheritableData(string exportName, string dataPath)
		{
			UEAData? result;

			if (TryGetExportData(out var data, exportName))
			{
				result = data[dataPath];
				if (result != null) { return result; }
			}

			if (TryGetExport(out var blueprint, Name + "_C"))
			{
				// The SuperStruct ObjectReference in a blueprint export refers to the object that the current object inherits properties from.
				// It's an index to the Package Imports, but unfortunately...
				//
				// CUE4Parse hides the actual path or hash for an imported asset behind calls that actually load the darned asset and export,
				// so we end up having to load it twice, first for the ResolvedObject to get the linked export and its parent package's path,
				// then again to actually parse the package and the rest of its exports in a way that we can use them.
				//
				// I've implemented package caching to speed this up, since multiple game objects may inherit from the same parent blueprint.
				// Seems to result in about a 33% speedup, from 9 secs to 6 secs to parse the Construction objects and all of their parent blueprints.
				//
				// If I could figure out a way to turn the SuperStruct's FPackageIndex into a uasset path 
				FPackageIndex? parentRef = (blueprint["SuperStruct"]?.Value.Object as FPackageIndex);				
				ResolvedObject? rObject = parentRef?.ResolvedObject;
				if (_game?.GetPackage(rObject?.GetPathName(true), out var parent, true) ?? false)
				{
					return parent.FindInheritableData(exportName, dataPath);
				}
			}
			
			return null;
		}

		public UEAExport? GetDefaultExport()
		{
			UEAExport? export = this["DefaultExport"] as UEAExport;

			if (export == null)
			{
				// No default. Just find the first one we come across, if any.

				foreach (var entry in this["Exports"] ?? NotFound)
				{
					export = entry as UEAExport;

					if (export != null)
					{
						this["DefaultExport"] = export;

						return export;
					}
				}
			}

			return export;
		}

		public void SetDefaultExport(string name)
		{
			UEAExport? export = this["Exports"]?[name] as UEAExport;

			if (export != null)
			{
				this["DefaultExport"] = export;
			}
		}

		public bool TryGetExport([NotNullWhen(true)] out UEAExport? export, string? name = null)
		{
			if (string.IsNullOrEmpty(name))
			{
				export = GetDefaultExport();
				return (export != null);
			}
			else
			{
				//export = this["Exports"]?[name] as UEAExport;
				export = this["Exports"]?[name] as UEAExport;
				return (export != null);
			}
		}

		public bool TryGetExportData([NotNullWhen(true)] out UEAData? exportData, string? name = null)
		{
			if (TryGetExport(out var export, name))
			{
				exportData = export.GetData();
				return (exportData != null);
			}

			exportData = null;
			return false;
		}

		public override string ToString()
		{
			return Name + ": " + (UEPackage?.GetType().ToString() ?? "Empty IPackage") + " (" + (this["Exports"]?.Count ?? 0) + " Exports)";
		}

	}
}
