using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using CUE4Parse;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CUE4ParseHelper
{
	// I don't know if this is actually helpful, but the CUE4Helper practice of putting every single supported different game in their asset version enum looks ridiculous to me.
	public enum UEGameVersion
	{
		Unknown = 0,
		UE04_00 = 400,
		UE04_01 = 401,
		UE04_02 = 402,
		UE04_03 = 403,
		UE04_04 = 404,
		UE04_05 = 405,
		UE04_06 = 406,
		UE04_07 = 407,
		UE04_08 = 408,
		UE04_09 = 409,
		UE04_10 = 410,
		UE04_11 = 411,
		UE04_12 = 412,
		UE04_13 = 413,
		UE04_14 = 414,
		UE04_15 = 415,
		UE04_16 = 416,
		UE04_17 = 417,
		UE04_18 = 418,
		UE04_19 = 419,
		UE04_20 = 420,
		UE04_21 = 421,
		UE04_22 = 422,
		UE04_23 = 423,
		UE04_24 = 424,
		UE04_25 = 425,
		UE04_26 = 426,
		UE04_27 = 427,
		UE04_28 = 428,
		UE04_Unknown = 499,
		UE05_00 = 500,
		UE05_01 = 501,
		UE05_02 = 502,
		UE05_03 = 503,
		UE05_04 = 504,
		UE05_05 = 505,
		UE05_06 = 506,
		UE05_Unknown = 599
	}

	public class UEGameFiles
	{
		// Statics

		public static readonly HashSet<string> FileExtensions = new("uasset,uexp,ubulk,uptnl,uplugin,upluginmanifest,bin,exe,ini,locres,locmeta".Split(','));
		
		public static void Initialize()
		{
			CUE4Parse.Compression.ZlibHelper.Initialize("zlib-ng2.dll");
		}

		// Fields

		protected string _path = "";
		protected DefaultFileProvider? _files;
		protected EGame _version;
		protected Dictionary<string, UEAPackage> _cachedPackages = [];

		// Properties
		
		public string Path { get => _path; }

		// Methods

		public void ClearCachedPackages() => _cachedPackages.Clear();
		
		protected static UEGameVersion ConvertEnum(EGame CUE4ParseValue)
		{
			int _value = (int)CUE4ParseValue;

			return (UEGameVersion)((((_value / GameUtils.GameUe4Base) + 3) * 100) + ((_value % GameUtils.GameUe4Base) >> 4));
		}

		protected static EGame ConvertEnum(UEGameVersion CUE4ParseHelperValue)
		{
			int _value = (int)CUE4ParseHelperValue;

			return (EGame)(((((_value / 100) - 3) * GameUtils.GameUe4Base) + (_value % 100)) << 4);
		}

		public DefaultFileProvider? GetGameFiles() => _files;

		public bool TryMount(string gameFilePath, UEGameVersion version, [NotNullWhen(false)] out Exception? error, bool caseInsensitivePaths = false)
		{
			DefaultFileProvider? archive = null;

			try
			{
				_version = ConvertEnum(version);
				archive = new DefaultFileProvider(gameFilePath, SearchOption.AllDirectories, caseInsensitivePaths, new VersionContainer(_version));
				archive.Initialize();
				archive.Mount();
				
				/*
				archive.SubmitKey(new CUE4Parse.UE4.Objects.Core.Misc.FGuid(), new CUE4Parse.Encryption.Aes.FAesKey("0x" + new string('0', 64)));
				archive.PostMount();
				archive.LoadLocalization();
				archive.LoadVirtualPaths(_version.GetVersion());
				*/

				_path = gameFilePath;
				_files = archive;

				error = null;
				return true;
			}
			catch (Exception ex)
			{
				// Release any unlikely but potential file locks immediately, since our mounting failed.
				archive?.Dispose();

				_path = "";
				_files = null;
				_version = 0;
				error = ex;

				return false;
			}
		}

		/// <summary>Tries to loads an asset package at the specified path 
		/// </summary>
		/// <param name="packagePath"></param>
		/// <param name="package"></param>
		/// <returns></returns>
		public bool GetPackage(string? packagePath, [NotNullWhen(true)] out UEAPackage? package, bool cachePackage = false)
		{
			if (!string.IsNullOrEmpty(packagePath) && (_files != null))
			{
				// Separate an Export name or file extension from the packagePath, if present.
				var pathParts = packagePath.Split('.');

				if (_cachedPackages.TryGetValue(pathParts[0], out var cachedPackage))
				{
					package = cachedPackage;
					return package != null;
				}

				if (_files.TryLoadPackage(pathParts[0], out IPackage? ioPackage))
				{
					package = new UEAPackage(this, (IoPackage)ioPackage);
					if (cachePackage)
					{
						_cachedPackages.Add(pathParts[0], package);
					}
					return true;
				}
			}

			package = null;
			return false;
		}

		/// <summary>Tries to load an asset export at the specified path
		/// </summary>
		/// <param name="packagePath"></param>
		/// <param name="export"></param>
		/// <param name="exportName"></param>
		/// <returns></returns>
		public bool GetExport(string? packagePath, [NotNullWhen(true)] out UEAExport? export, string? exportName = null)
		{
			if (!string.IsNullOrEmpty(packagePath))
			{
				// Separate an Export name from the packagePath, if present.
				var pathParts = packagePath.Split('.');
				if ((pathParts.Length > 1) && (exportName == null))
				{
					exportName = pathParts[1];
				}

				if (GetPackage(pathParts[0], out var package))
				{
					return package.TryGetExport(out export, exportName ?? "");
				}
				Debugger.Break();
			}

			export = null;
			return false;
		}

		/// <summary>Tries to load the data from an asset export at the specified path
		/// </summary>
		/// <param name="packagePath"></param>
		/// <param name="data"></param>
		/// <param name="exportName"></param>
		/// <returns></returns>
		public bool GetExportData(string? packagePath, [NotNullWhen(true)] out UEAData? data, string? exportName = null)
		{
			if (GetExport(packagePath, out var export, exportName))
			{
				data = export.Data["Data"];
				return (data != null);
			}

			data = null;
			return false;
		}
	}
}
