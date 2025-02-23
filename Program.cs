using System.Diagnostics;

using OfficeOpenXml;

using CUE4ParseHelper;

using static CUE4ParseHelper.UEAData;

namespace RtM_Parser
{
	internal class Program
	{
		//const string UnrealPackageLocation = @"C:\Users\Jonathan\Desktop\Files\Games\Return to Moria\Paks\Paks_1_4_2_177921";
		const string UnrealPackageLocation = @"C:\Users\Jonathan\Desktop\Files\Games\Return to Moria\Paks\Paks_1_4_3_186305";

		const string OutputPath = @"C:\Users\Jonathan\Desktop\Files\Games\Return to Moria\Data";
		const string OutputFile = "RtM Exported Data.xlsx";
		const float MaxColumnWidth = 60.0f;

		const string StringTablePath = @"/Game/Tech/Data/StringTables/";
		const string StringTableList = "Architecture,CategoryTags,Challenges,Interactables,Items,PlayerStats,ST_AiCharacters,ST_Effects,ST_FoundersDay_Event,ST_LoreEntry_Appendices,ST_LoreEntry_Items,ST_Sleep,ST_TipText,UI,World";

		const string GameItemsPath = @"/Game/Tech/Data/Items/";
		const string GameItemsList = "Items,Armor,Brews,Consumables,ContainerItems,EpicPacks,Ores,RecipeFragments,Runes,Storage,ThresholdEffects,ThrowLights,Tools,Weapons,Fuels,ItemRecipes,ItemSets";
		const string ConstructionsPath = @"/Game/Tech/Data/Building/";
		
		// Items that don't have recipes, but are still obtainable in-game and thus eligible for exporting
		static readonly HashSet<string> GameObjectWhitelist = [
			"RareMushroom",
			"TeaFlower",
			"HarshFlower",
			"IceCabbage",
			"CaveHoney",
			"Chives",
			"Cranberries",
			"DeepMushroom",
			"Eggs",
			"ElvenSecretIngredient",
			"Fennel",
			"Grabapple",
			"DwarfOat",
			"Meat",
			"Mint",
			"MountainPotato",
			"Mushroom",
			"Parsnips",
			"PoisonMushroom",
			"Sunion",
			"HealingFlower",
			"Thyme",
			"WhiteBeans",
		];
		
		// Internal/Abandoned recipes or items that are not obtainable in-game whether in Campaign or Sandbox mode
		// If something is added to the game later, it can simply be removed from this list and it will be exported
		static readonly HashSet<string> GameObjectBlacklist = [
			"ConstructionRecipe.AutoFoundation_Stone_1x1",
			"ConstructionRecipe.AutoFoundation_Stone_3x3",
			"ConstructionRecipe.ContainerProxyChest",
			"ConstructionRecipe.Deco_Carpet_B",
			"ConstructionRecipe.Deco_Carpet_B2",
			"ConstructionRecipe.Deco_Counter_A",
			"ConstructionRecipe.Deco_Rug_E",
			"ConstructionRecipe.Deco_Rug_E2",
			"ConstructionRecipe.Deco_Rug_F",
			"ConstructionRecipe.Deco_Rug_F2",
			"ConstructionRecipe.Deco_Vase_A",
			"ConstructionRecipe.Deco_Vase_B",
			"ConstructionRecipe.Deco_Vase_E",
			"ConstructionRecipe.Deco_Vase_G",
			"ConstructionRecipe.Pallet_Wood_Base",
			"ItemRecipe.Halberd_2h_t3",
			"ItemRecipe.LegForgeDurin_part1",
			"ItemRecipe.LegForgeDurin_part2",
			"ItemRecipe.LegForgeDurin_part3",
			"ItemRecipe.Mattock_1h_t3",
			"ItemRecipe.RangeBonus_Set_GlovesArmor",
			"ItemRecipe.Sword_1h_t5",
			"ItemRecipe.Sword_2h_t3",
			"ItemRecipe.TEST_BlueCarrot",
			"ItemRecipe.WarAxe_1h_t5",
			"Rune.DeepTribePoison", // Used on Deep Orc weapons
			"Rune.RedeyeAntiArmor",
			"Rune.ShadowOrcShadow", // Used on Shadow Orc weapons
			"Rune.Quick"
		];

		// Recipes that are unlocked by some kind of in-game scripting don't have their details in the game files, so we have to hard code their known unlocks or just leave them as "Scripted".
		static readonly HashSet<string> RecipeCampaignUnlocks = [
			// "Battleaxe_2h_t4", // This is the Sandbox-only Mithril weapon
			"BalinCamp_A_Key",
			"BalinCamp_B_Key",
			"BalinCamp_C_Key",
			"BalinCamp_D_Key",
			"BalinCamp_E_Key",
			"Hammer_2h_t2",
			"Mithril_GreatSword_2h_t6",
			"Mithril_Halberd_2h_t6",
			"Mithril_Mattock_1h_t6",
			"Mithril_Set_BootsArmor",
			"Mithril_Set_GlovesArmor",
			"Mithril_Set_HelmetArmor",
			"Mithril_Set_TorsoArmor",
			"Mithril_Sword_1h_t6",
			"Mithril_TBDSet_Shield",
			"Mithril_WarAxe_1h_t6",
			"Restoration_Hammer_Adamant",
			"Restoration_Hammer_Gunmetal",
		];

		// These recipes were made unavailable by the devs for stability bugs or testing, but with Manual unlock requirements instead of the Disabled enum, unfortunately.
		// I'm using a list of them here so that they will automatically show back up if the Devs change the Unlock type, rather than having to find and remove them from the Blacklist.
		static readonly HashSet<string> RecipesDisabled = [
			"Advanced_Bannister_Post_Stone",
			"Advanced_Column_Wood_A",
			"Advanced_Column_Wood_B",
			"Advanced_Column_Wood_D",
			"Advanced_Fence_Wood",
			"Advanced_Fence_Wood_1m",
			"Advanced_Floor_Stone_V2",
			"Advanced_Floor_Stone_1m_V2",
			"Advanced_Stairs_Railing_1m_V2",
			"Crude_Column",
			"Crude_Floor_Stone_V2",
			"Crude_Floor_Wood_V2",
			"Elder_Archway_A",
			"Elder_Archway_C",
			"Elder_Archway_Corner",
			"Elder_Archway_Horizontal_Large",
			"Elder_Archway_Vertical",
			"Elder_Wall_A",
			"Elder_Wall_A_Crown",
			"Elder_Wall_B",
			"Elder_Wall_B_Crown",
			"Elder_Wall_C",
			"Elder_Wall_Corner",
			"Elder_Wall_Corner_Crown",
			"Elder_Wall_D",
			"Elder_Wall_E",
			"Elder_Wall_E_Crown",
			"Elder_Wall_Short_A",
			"Elder_Wall_Short_B",
			"Elder_Wall_Thin_A",
			"Elder_Wall_Thin_A_Crown",
			"Elder_Wall_Thin_B",
			"Elder_Window_A",
			"Elder_Window_B",
			"Elder_Window_C",
			"Scaffolding_Platform_1x1x3",
			"Scaffolding_Platform_1x3x3",
			"Scaffolding_Platform_Open"
		];

		// Not all data sets have relevant information for exporting.
		// Remove InternalName/ConstructionHandle fields when done debugging.
		static readonly Dictionary<ItemType, string> ItemFieldOrder = new() {
			//{ ItemType.None, "Name,InternalName,Tags,Description" },
			{ ItemType.Armor, "Name,InternalName,Tags,Description,Tier,Durability,Armor Protection,Armor Effectiveness,Repair Cost,Damage Modifiers,Effects" },
			{ ItemType.Brew, "Name,InternalName,Description,Effect" },
			{ ItemType.Consumable, "Name,InternalName,Tags,Description,Stack Size,Spoil Time,Restore Hunger,Restore Health,Restore Energy,Meal Time,Effect,Meal Buffs" },
			//{ ItemType.ContainerItem, "Name,InternalName,Tags,Description" },
			//{ ItemType.EpicPack, "Name,InternalName,Tags,Description" },
			{ ItemType.Fuel, "Name,Fuel Value" },
			{ ItemType.ItemRecipe, "Name,InternalName,Quantity,Craft Time,Stations,Materials,Support,Unlock,Materials Sandbox(*),Support Sandbox(*),Unlock Sandbox (* if different)" },
			//{ ItemType.Item, "Name,InternalName,Tags,Description" },
			{ ItemType.ItemSet, "Name,InternalName,Tags,Description" },
			//{ ItemType.Ore, "Name,InternalName,Tags,Description" },
			//{ ItemType.RecipeFragment, "Name,InternalName,Tags,Description" },
			{ ItemType.Rune, "Name,InternalName,Tags,Description" },
			//{ ItemType.Storage, "Name,InternalName,Tags,Description" },
			//{ ItemType.ThresholdEffect, "Name,InternalName,Tags,Description" },
			{ ItemType.ThrowLight, "Name,InternalName,Tags,Description" },
			{ ItemType.Tool, "Name,InternalName,Tags,Description" },
			{ ItemType.Weapon, "Name,InternalName,Tags,Description" },
			{ ItemType.Effect, "Name,InternalName,Tags,Description" },
			//{ ItemType.Construction, "Name,InternalName,Tags,Description" },
			{ ItemType.ConstructionRecipe, "Name,InternalName,ConstructionHandle,Tags,Durability,HorizStabilityDist,Description,Materials,Unlock,Materials Sandbox(*),Unlock Sandbox (* if different)" },
			//{ ItemType.ConstructionStability, "Name,InternalName,Tags,Description" }
		};

		enum ItemType
		{
			None,
			Armor,
			Brew,
			Consumable,
			ContainerItem,
			EpicPack,
			Fuel,
			ItemRecipe,
			Item,
			ItemSet,
			Ore,
			RecipeFragment,
			Rune,
			Storage,
			ThresholdEffect,
			ThrowLight,
			Tool,
			Weapon,
			Effect,
			Construction,
			ConstructionRecipe,
			ConstructionStability
		}

		static readonly Dictionary<string, Dictionary<string, string>> StringTables = [];
		static readonly Dictionary<string, string> CombinedStringTable = [];

		static readonly Dictionary<string, string> CategoryTags = [];

		static readonly Dictionary<string, Dictionary<string, string>> GameObjects = [];
		static readonly Dictionary<ItemType, List<string>> GameObjectsOfType = [];

		static readonly UEGameFiles Game = new();

		const UEGameVersion Rtm_Unreal_Engine_Version = UEGameVersion.UE04_27;

		static int Main()
		{
			// https://github.com/EPPlusSoftware/EPPlus/wiki/Getting-Started

			// Design Note: The DebuggerDisplay code attribute - e.g., [DebuggerDisplay("Count = {count}")] - is the way to display a struct in debugger windows,
			// not CUE4Parse's ToString() mangling like: "StringTableEntry, " + TableId + ", " + Value;

			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

			UEGameFiles.Initialize();

			Console.Write("Opening game files...");
			if (!Game.TryMount(UnrealPackageLocation, Rtm_Unreal_Engine_Version, out var error))
			{
				Console.WriteLine();
				Console.WriteLine("Unable to mount game archives at:\n" + UnrealPackageLocation + "\n\nError: " + (error?.Message ?? "Unknown"));

				return 1;
			}

			Console.WriteLine("Done");

			Console.WriteLine("Loading game data...");

			// Internal objects load their Localized StringTable entries from the appropriate StringTable object automatically, but we need to be able to do our own lookups as well.
			LoadStringTables();
			LoadCategoryTags();
			LoadConstructionStabilities();
			LoadConstructions();

			Console.WriteLine("Loading item data...");
			LoadItems();
			LoadConstructionRecipes();
			Console.WriteLine();
			Console.WriteLine("Finished all parsing activities.");
			Console.WriteLine();

			Console.Write("Writing output to spreadsheet...");
			string spreadsheetPath = Path.Combine(OutputPath, OutputFile);
			WriteSpreadsheet(spreadsheetPath);
			Console.WriteLine(" Done.");
			Console.WriteLine("Spreadsheet can be found at the following location:\n");
			Console.WriteLine(spreadsheetPath);
			Console.WriteLine();
			//Console.ReadKey();

			return 0;
		}

		static void LoadCategoryTags()
		{
			if (Game.GetExportData("/Game/Tech/Data/DT_CategoryTags", out var table))
			{
				Dictionary<string, string> stUI = StringTables["UI"];

				foreach (var category in table ?? NotFound)
				{
					string tag = category["Tag"]?["TagName"] ?? category.Name;

					if (!string.IsNullOrEmpty(tag))
					{
						string displayName = category["DisplayName"] ?? tag;
						CategoryTags.Add(tag, displayName);
						CombinedStringTable[tag] = displayName;
						stUI[tag] = displayName;
					}
				}
			}
			else
			{
				Debugger.Break();
			}
		}

		static void LoadItems(string assetFilename = "")
		{
			if (assetFilename == "")
			{
				string[] assetFiles = GameItemsList.Split(',');

				foreach (string assetFile in assetFiles)
				{
					LoadItems(assetFile);
				}
			}
			else
			{
				string itemTypeName = assetFilename;

				if (itemTypeName.EndsWith('s'))
				{
					itemTypeName = itemTypeName.Substring(0, itemTypeName.Length - 1);
				}

				ItemType itemType = Enum.Parse<ItemType>(itemTypeName);
				string itemPrefix = itemTypeName + '.';
				List<string> itemList = [];
				GameObjectsOfType[itemType] = itemList;

				Console.Write("   Loading " + assetFilename + "...");

				if (Game!.GetExportData(GameItemsPath + "DT_" + assetFilename, out var table))
				{
					foreach (UEAData dtItem in table ?? NotFound)
					{
						string itemName = dtItem.Name;
						string itemHandle = itemPrefix + itemName;

						// ERowEnabledStates: Disabled, Live, and Test. Test items are available in-game, so only skip Disabled items.
						// Ugh. Some items are working in-game even set as Disabled (e.g., Battleaxe_2h_t2 - Steel Battleaxe), so we need a Whitelist in addition to a Blacklist.
						if (GameObjectBlacklist.Contains(itemHandle) ||
							itemName.Contains("Unshippable", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}

						itemList.Add(itemName);

						var newItem = GetGameObject(itemHandle);

						newItem["ItemType"] = itemTypeName;
						newItem["InternalName"] = itemName; // (e.g., Scraps)
						newItem["ItemHandle"] = itemHandle;
						
						// I used to filter items out based on this enum, but apparently it isn't used anymore. It doesn't match up with what's available in-game. Useful for some debugging here, though.
						newItem["Disabled"] = ("ERowEnabledState::Disabled".Equals(dtItem["EnabledState"]!) && !GameObjectWhitelist.Contains(itemHandle)) ? "true" : "false";

						if (dtItem == null) continue;

						newItem["Name"] = dtItem["DisplayName"] ?? itemName;
						if (newItem["Name"] == "") { newItem["Name"] = itemName; } // Is this happening?
						newItem["Description"] = dtItem["Description"] ?? "";
						newItem["Stack Size"] = dtItem["MaxStackSize"] ?? "";
						string icon = dtItem["Icon"]?.Value.String ?? "";
						int dot = icon.IndexOf('.');
						if (dot >= 0) { icon = icon.Remove(dot); }
						newItem["Icon"] = icon;

						CombinedStringTable[newItem["ItemHandle"]] = newItem["Name"];
						CombinedStringTable[newItem["InternalName"]] = newItem["Name"];

						bool tierTag;
						List<string> categoryTags = [];
						string tierNum = "";
						foreach (var gameplayTag in dtItem["Tags"] ?? NotFound)
						{
							string catTag = gameplayTag.Value;
							tierTag = false;

							// TODO: Modify to support Tiers higher than 9?
							if (catTag.Substring(catTag.Length - 6, 5).Equals(".Tier"))
							{
								tierNum = catTag[catTag.Length - 1].ToString();
								catTag = catTag.Substring(0, catTag.Length - ".TierX".Length);
								tierTag = true;
							}

							if (CategoryTags.TryGetValue(catTag, out var catTagDisplayName))
							{
								categoryTags.Add(catTagDisplayName);
							}

							if (tierTag)
							{
								categoryTags.Add(LookupString("WeaponTier.Format", "UI")?.Replace("{n}", LookupString("WeaponTier." + tierNum, "UI") ?? "?") ?? ("Tier " + LookupString("WeaponTier." + tierNum, "UI") ?? tierNum));
							}
						}

						newItem["Tier"] = tierNum;
						newItem["Tags"] = string.Join(", ", categoryTags);

						switch (itemType)
						{
							case ItemType.Armor:
								LoadItem_Armor(newItem, dtItem);
								break;
							case ItemType.Brew:
								LoadItem_Brew(newItem, dtItem);
								break;
							case ItemType.Consumable:
								LoadItem_Consumable(newItem, dtItem);
								break;
							case ItemType.ContainerItem:
								LoadItem_ContainerItem(newItem, dtItem);
								break;
							case ItemType.EpicPack:
								LoadItem_EpicPack(newItem, dtItem);
								break;
							case ItemType.Fuel:
								LoadItem_Fuel(newItem, dtItem);
								break;
							case ItemType.Item:
								LoadItem_Item(newItem, dtItem);
								break;
							case ItemType.ItemRecipe:
								LoadItem_ItemRecipe(newItem, dtItem);
								break;
							case ItemType.ItemSet:
								LoadItem_ItemSet(newItem, dtItem);
								break;
							case ItemType.Ore:
								LoadItem_Ore(newItem, dtItem);
								break;
							case ItemType.RecipeFragment:
								LoadItem_RecipeFragment(newItem, dtItem);
								break;
							case ItemType.Rune:
								LoadItem_Rune(newItem, dtItem);
								break;
							case ItemType.Storage:
								LoadItem_Storage(newItem, dtItem);
								break;
							case ItemType.ThresholdEffect:
								LoadItem_ThresholdEffect(newItem, dtItem);
								break;
							case ItemType.ThrowLight:
								LoadItem_ThrowLight(newItem, dtItem);
								break;
							case ItemType.Tool:
								LoadItem_Tool(newItem, dtItem);
								break;
							case ItemType.Weapon:
								LoadItem_Weapon(newItem, dtItem);
								break;
						}
					}

					Console.WriteLine(" Done");
				}
				else
				{
					Console.WriteLine(" Failed.");
				}
			}
		}

		static void LoadItem_Item(Dictionary<string, string> newItem, UEAData fields)
		{
			// Any additional fields to parse here?
		}

		static void LoadItem_Armor(Dictionary<string, string> newItem, UEAData fields)
		{
			newItem["Durability"] = fields["Durability"] ?? "";
			newItem["Armor Effectiveness"] = fields["DamageReduction"] ?? "";
			newItem["Armor Protection"] = fields["DamageProtection"] ?? "";

			var repairField = fields["InitialRepairCost"]?.GetFirst();
			string repairText = repairField?["Count"] ?? "";
			if (repairText != "")
			{
				string repairMaterial = LookupString(repairField?["MaterialHandle.RowName"]!);
				
				if (repairMaterial != "")
				{
					repairText += ' ' + repairMaterial;
				}
			}
			newItem["Repair Cost"] = repairText;

			List<string> itemEffects = [];
			List<string> itemEffect = [];
			bool firstItemEffect = true;

			foreach (var effect in fields["VisibleEffects"] ?? NotFound)
			{
				itemEffect.Add((firstItemEffect ? "Visible: " : "") + RegisterItemEffect(effect["ObjectName"]!, effect["ObjectAsset"]!));
				firstItemEffect = false;
			}
			if (itemEffect.Count > 0) { itemEffects.Add(string.Join(", ", itemEffect)); }
			itemEffect.Clear();

			if (Game.GetExportData(fields["Actor"]!, out var bpData, ""))
			{
				newItem["Armor"] = fields["ArmorValue"] ?? "";

				List<string> tags = [];
				foreach (var tag in bpData["TagsToApplyWhileEquipped"] ?? NotFound)
				{
					tags.Add(tag);
				}
				newItem["EquipTags"] = string.Join(", ", tags);

				firstItemEffect = true;
				itemEffect.Clear();
				foreach (var effect in bpData["Effects"] ?? NotFound)
				{
					itemEffect.Add((firstItemEffect ? "OnEquip: " : "") + RegisterItemEffect(effect["ObjectName"]!, effect["ObjectAsset"]!));
					firstItemEffect = false;
				}
			}

			//itemEffect.Clear();
			//firstItemEffect = true;
			foreach (var effect in fields["EquipEffects"] ?? NotFound)
			{
				itemEffect.Add((firstItemEffect ? "OnEquip: " : "") + RegisterItemEffect(effect["ObjectName"]!, effect["ObjectAsset"]!));
				firstItemEffect = false;
			}
			if (itemEffect.Count > 0) { itemEffects.Add(string.Join(", ", itemEffect)); }

			itemEffect.Clear();
			firstItemEffect = true;
			foreach (var effect in fields["HolsterEffects"] ?? NotFound)
			{
				itemEffect.Add((firstItemEffect ? "OnUnequip: " : "") + RegisterItemEffect(effect["ObjectName"]!, effect["ObjectAsset"]!));
				firstItemEffect = false;
			}
			if (itemEffect.Count > 0) { itemEffects.Add(string.Join(", ", itemEffect)); }

			newItem["Effects"] = string.Join(", ", itemEffects);

			bool hasCorrosionResistance = true;
			string damageModText;
			List<string> damageMods = [];
			foreach (var damageMod in fields["DamageModifiers"] ?? NotFound)
			{
				damageModText = damageMod["RowName"] ?? "";

				if (damageModText.Equals("CorrosiveDamage"))
				{
					hasCorrosionResistance = false;
				}
				else if (damageModText != "")
				{
					damageMods.Add(damageModText);
				}
			}
			if (hasCorrosionResistance)
			{
				damageMods.Add("Corrosion Resistance");
			}
			newItem["Damage Modifiers"] = string.Join(", ", damageMods);
			newItem["ItemSet"] = fields["ItemSetRowHandle.RowName"] ?? "";
		}

		static void LoadItem_Brew(Dictionary<string, string> newItem, UEAData fields)
		{
			newItem["Name"] = fields["DisplayName"] ?? newItem["InternalName"];
			newItem["Description"] = fields["Description"] ?? "";

			List<string> brewEffects = [];
			foreach (var useEffect in fields["UseEffects"] ?? NotFound)
			{
				string brewEffect = useEffect["ObjectName"] ?? "";
				RegisterItemEffect(brewEffect, useEffect["ObjectAsset"]!);
				if (brewEffect.StartsWith("GE_")) { brewEffect = brewEffect.Remove(0, 3); }
				if (brewEffect.EndsWith("_C")) { brewEffect = brewEffect.Remove(brewEffect.Length - 2); }
				brewEffects.Add(brewEffect);
			}

			newItem["Effects"] = string.Join(", ", brewEffects);
		}

		static void LoadItem_Consumable(Dictionary<string, string> newItem, UEAData fields)
		{
			int spoilTime = fields["SpoilageSeconds"]?.Value.Int ?? 0;
			newItem["Spoil Time"] = (spoilTime > 0) ? spoilTime.ToString() : "";
			newItem["Restore Hunger"] = fields["HungerRestore"] ?? "";
			newItem["Restore Health"] = fields["HealthRestore"] ?? "";
			newItem["Restore Energy"] = fields["EnergyRestore"] ?? "";
			newItem["Meal Time"] = (fields["MealTime"]?.ToString() ?? "") switch {
				"EMealTime::Breakfast" => "Breakfast",
				"EMealTime::Lunch" => "Lunch",
				"EMealTime::Dinner" => "Dinner",
				_ => "" };

			// Use Effects
			//if (newItem["InternalName"] == "AntiPoison_Consumable") { Debugger.Break(); }
			List<string> itemEffects = [];
			foreach (UEAData useEffect in fields["UseEffects"] ?? NotFound)
			{
				string itemEffect = useEffect["ObjectName"] ?? "";
				RegisterItemEffect(itemEffect, useEffect["ObjectAsset"]!);
				if (itemEffect.StartsWith("GE_")) { itemEffect = itemEffect.Remove(0, 3); }
				if (itemEffect.EndsWith("_C")) { itemEffect = itemEffect.Remove(itemEffect.Length - 2); }
				itemEffects.Add(itemEffect);
			}
			newItem["Effect"] = string.Join(", ", itemEffects);

			// Meal Buffs
			itemEffects.Clear();
			foreach (UEAData mealEffect in fields["MealTimeUseEffects"] ?? NotFound)
			{
				string itemEffect = mealEffect["ObjectName"] ?? "";
				RegisterItemEffect(itemEffect, mealEffect["ObjectAsset"]!);
				if (itemEffect.StartsWith("GE_")) { itemEffect = itemEffect.Remove(0, 3); }
				if (itemEffect.EndsWith("_C")) { itemEffect = itemEffect.Remove(itemEffect.Length - 2); }
				itemEffects.Add(itemEffect);
			}
			newItem["Meal Buffs"] = string.Join(", ", itemEffects);
		}

		static void LoadItem_ContainerItem(Dictionary<string, string> newItem, UEAData fields)
		{

		}

		static void LoadItem_EpicPack(Dictionary<string, string> newItem, UEAData fields)
		{
		}

		static void LoadItem_Fuel(Dictionary<string, string> newItem, UEAData fields)
		{
			var theItem = GetGameObject(fields["ItemHandle.RowName"]!);

			newItem["Name"] = theItem["Name"];
			newItem["Fuel Value"] = fields["FuelValue"] ?? "";
		}

		static void LoadItem_ItemRecipe(Dictionary<string, string> newItem, UEAData fields)
		{
			string recipeName = newItem["InternalName"];
			Dictionary<string, string>? item = GameObjects.GetValueOrDefault(fields["ResultItemHandle.RowName"] ?? "NotFound");
			newItem["Name"] = item?.GetValueOrDefault("Name") ?? "???"; // So it stands out
			if (item != null)
			{
				// When exporting crafted items, we'll omit any that don't have an available recipe in-game.
				// This avoids having to add a ton of dev/test/abandoned items to the Blacklist.
				item["HasRecipe"] = "true";
			}
			newItem["Quantity"] = fields["ResultItemCount"] ?? "1";
			newItem["Craft Time"] = fields["CraftTimeSeconds"] ?? "";

			List<string> items = [];
			List<string> itemList = [];
			string? itemName = null;
			bool firstItem = true;
			
			int stationType = 0; // 1 = Great Forges, 2 = Great Furnaces, 3 = Hearths
			int stationCount = 0;
			foreach (var station in fields["CraftingStations"] ?? NotFound)
			{
				string craftStation = LookupString(station["RowName"]!);
				if (craftStation == "")
				{
					Debugger.Break();
				}
				else
				{
					items.Add(craftStation);
				}

				if (craftStation.StartsWith("Great "))
				{
					if (craftStation.Contains("Forge"))
					{
						stationType = 1;
						stationCount++;
					}
					else if (craftStation.Contains("Furnace"))
					{
						stationType = 2;
						stationCount++;
					}
					else
					{
						Debugger.Break(); // Great what??
					}
				}
				else if (craftStation.EndsWith(" Hearth"))
				{
					stationType = 3;
					stationCount++;
				}
			}

			// NOTE: The station list culling only works with English localization in this implementation. Would need to rework it to go from construction handles instead to work with other languages.
			if ((stationType == 1 || stationType == 2) && stationCount == 5)
			{
				// All Great Forges or Great Furnaces work with this recipe. Don't list them all out separately.
				for (stationCount = items.Count - 1; stationCount >= 0; stationCount--)
				{
					if (items[stationCount].StartsWith("Great "))
					{
						items.RemoveAt(stationCount);
					}
				}
				items.Add((stationType == 1) ? "Great Forges" : "Great Furnaces");
			}
			else if (stationType == 3 && stationCount == 6)
			{
				// All Hearths work with this recipe. Don't list them all out separately.
				for (stationCount = items.Count - 1; stationCount >= 0; stationCount--)
				{
					if (items[stationCount].EndsWith(" Hearth"))
					{
						items.RemoveAt(stationCount);
					}
				}
				items.Add("All Hearths");
			}

			newItem["Stations"] = (items.Count == 0) ? "Field Crafting" : string.Join(", ", items);
			items.Clear();

			foreach (var material in fields["DefaultRequiredMaterials"] ?? NotFound)
			{
				itemName = LookupString(material["MaterialHandle.RowName"]!);
				if (itemName == "")
				{
					Debugger.Break(); // What happened?
				}
				else
				{
					items.Add(itemName + " (" + (material["Count"] ?? "?") + ")");
				}
			}
			newItem["Materials"] = string.Join(", ", items);
			items.Clear();

			foreach (var support in fields["DefaultRequiredConstructions"] ?? NotFound)
			{
				itemName = LookupString(support["RowName"]!);
				if (itemName == "")
				{
					Debugger.Break(); // What happened?
				}
				else
				{
					items.Add(itemName);
				}
			}
			newItem["Support"] = string.Join(", ", items);
			items.Clear();

			if ("true".Equals(fields["bHasSandboxRequirementsOverride"] ?? ""))
			{
				foreach (var material in fields["SandboxRequiredMaterials"] ?? NotFound)
				{
					itemName = LookupString(material["MaterialHandle.RowName"]!);
					if (itemName == "")
					{
						Debugger.Break(); // What happened?
					}
					else
					{
						items.Add(itemName + " (" + (material["Count"] ?? "?") + ")");
					}
				}
				newItem["Materials Sandbox(*)"] = string.Join(", ", items);
				items.Clear();

				foreach (var support in fields["SandboxRequiredConstructions"] ?? NotFound)
				{
					itemName = LookupString(support["RowName"]!);
					if (itemName == "")
					{
						Debugger.Break(); // What happened?
					}
					else
					{
						items.Add(itemName);
					}
				}
				newItem["Support Sandbox(*)"] = string.Join(", ", items);
				items.Clear();

				if (newItem["Materials"] == newItem["Materials Sandbox(*)"]) { newItem["Materials Sandbox(*)"] = ""; }
				if (newItem["Support"] == newItem["Support Sandbox(*)"]) { newItem["Support Sandbox(*)"] = ""; }
			}

			switch (fields["DefaultUnlocks.UnlockType"] ?? "")
			{
				case "EMorRecipeUnlockType::Manual":

					if (RecipeCampaignUnlocks.Contains(recipeName))
					{
						items.Add("Campaign Progression");
					}
					else if (recipeName.StartsWith("HolidayPack"))
					{
						items.Add("Yule-tide Pack DLC");
					}
					else if (recipeName.StartsWith("RohanPack"))
					{
						items.Add("Rohan Pack DLC");
					}
					else if ("true".Equals(fields["bHasSandboxUnlockOverride"] ?? ""))
					{
						items.Add("Sandbox Only");
					}
					else
					{
						items.Add("Scripted");
					}
					break;
				case "EMorRecipeUnlockType::CollectFragments":
					items.Add("Recipe Fragments (" + (fields["DefaultUnlocks.NumFragments"] ?? "?") + ")");
					break;
				case "EMorRecipeUnlockType::DiscoverDependencies":
					foreach (var station in fields["DefaultUnlocks.UnlockRequiredConstructions"] ?? NotFound)
					{
						itemName = LookupString(station["RowName"]!);
						if (itemName == "")
						{
							Debugger.Break(); // What happened?
						}
						else
						{
							if (firstItem)
							{
								itemName = "Build: " + itemName;
								firstItem = false;
							}
							itemList.Add(itemName);
						}
					}
					if (itemList.Count > 0)
					{
						items.Add(string.Join(", ", itemList));
						itemList.Clear();
						firstItem = true;
					}

					foreach (var material in fields["DefaultUnlocks.UnlockRequiredItems"] ?? NotFound)
					{
						itemName = LookupString(material["RowName"]!);
						if (itemName == "")
						{
							Debugger.Break(); // What happened?
						}
						else
						{
							if (firstItem)
							{
								itemName = "Obtain: " + itemName;
								firstItem = false;
							}
							itemList.Add(itemName);
						}
					}
					if (itemList.Count > 0)
					{
						items.Add(string.Join(", ", itemList));
						itemList.Clear();
						firstItem = true;
					}
					break;
			}
			newItem["Unlock"] = string.Join(", ", items);
			items.Clear();

			if ("true".Equals(fields["bHasSandboxUnlockOverride"] ?? ""))
			{
				switch (fields["SandboxUnlocks.UnlockType"] ?? "")
				{
					case "EMorRecipeUnlockType::Manual":
						items.Add("Manual (Likely DLC)");
						break;
					case "EMorRecipeUnlockType::CollectFragments":
						items.Add("Recipe Fragments (" + (fields["SandboxUnlocks.NumFragments"] ?? "?") + ")");
						break;
					case "EMorRecipeUnlockType::DiscoverDependencies":
						foreach (var station in fields["SandboxUnlocks.UnlockRequiredConstructions"] ?? NotFound)
						{
							itemName = LookupString(station["RowName"]!);
							if (itemName == "")
							{
								Debugger.Break(); // What happened?
							}
							else
							{
								if (firstItem)
								{
									itemName = "Build: " + itemName;
									firstItem = false;
								}
								itemList.Add(itemName);
							}
						}
						if (itemList.Count > 0)
						{
							items.Add(string.Join(", ", itemList));
							itemList.Clear();
							firstItem = true;
						}

						foreach (var material in fields["SandboxUnlocks.UnlockRequiredItems"] ?? NotFound)
						{
							itemName = LookupString(material["RowName"]!);
							if (itemName == "")
							{
								Debugger.Break(); // What happened?
							}
							else
							{
								if (firstItem)
								{
									itemName = "Obtain: " + itemName;
									firstItem = false;
								}
								itemList.Add(itemName);
							}
						}
						if (itemList.Count > 0)
						{
							items.Add(string.Join(", ", itemList));
							itemList.Clear();
							firstItem = true;
						}
						break;
				}
			}
			newItem["Unlock Sandbox (* if different)"] = string.Join(", ", items);

			if ((newItem["Unlock"] == "Campaign Progression") && (newItem["Unlock Sandbox (* if different)"] == ""))
			{
				newItem["Unlock Sandbox (* if different)"] = "Unavailable (Campaign Only)";
			}
		}

		static void LoadItem_ItemSet(Dictionary<string, string> newItem, UEAData fields)
		{
			foreach (var setPiece in fields["SetPieceTags"] ?? NotFound)
			{

			}
		}

		static void LoadItem_Ore(Dictionary<string, string> newItem, UEAData fields)
		{
		}

		static void LoadItem_RecipeFragment(Dictionary<string, string> newItem, UEAData fields)
		{
		}

		static void LoadItem_Rune(Dictionary<string, string> newItem, UEAData fields)
		{
		}

		static void LoadItem_Storage(Dictionary<string, string> newItem, UEAData fields)
		{
		}

		static void LoadItem_ThresholdEffect(Dictionary<string, string> newItem, UEAData fields)
		{
		}

		static void LoadItem_ThrowLight(Dictionary<string, string> newItem, UEAData fields)
		{
		}

		static void LoadItem_Tool(Dictionary<string, string> newItem, UEAData fields)
		{
		}

		static void LoadItem_Weapon(Dictionary<string, string> newItem, UEAData fields)
		{
		}

		static void LoadConstructionStabilities()
		{
			if (!Game.GetExportData("/Game/Tech/Data/Building/DT_ConstructionStabilities", out var table))
			{
				return;
			}

			List<string> itemList = [];
			GameObjectsOfType[ItemType.ConstructionStability] = itemList;
			string itemPrefix = ItemType.ConstructionStability.ToString() + '.';
			string itemType = ItemType.ConstructionStability.ToString();

			foreach (var stability in table)
			{
				string itemName = stability.Name;
				string itemHandle = itemPrefix + itemName;

				itemList.Add(itemName);

				var newItem = GetGameObject(itemHandle);

				newItem["ItemType"] = itemType;
				newItem["InternalName"] = itemName; // (e.g., Scraps)
				newItem["ItemHandle"] = itemHandle;
				newItem["HorizontalDistance"] = stability["HorizontalDistance"] ?? "";
				newItem["VerticalDistance"] = stability["VerticalDistance"] ?? "";
			}
		}

		static void LoadConstructions()
		{
			Console.Write("Loading Constructions (slow)...");

			if (!Game.GetExportData("/Game/Tech/Data/Building/DT_Constructions", out var table))
			{
				Console.WriteLine(" Failed");
				return;
			}

			List<string> itemList = [];
			GameObjectsOfType[ItemType.Construction] = itemList;
			string itemPrefix = ItemType.Construction.ToString() + '.';

			foreach (var building in table)
			{
				string itemName = building.Name;
				string itemHandle = itemPrefix + itemName;

				if (!GameObjectBlacklist.Contains(itemHandle))
				{
					itemList.Add(itemName);

					var newItem = GetGameObject(itemHandle);

					newItem["ItemType"] = ItemType.Construction.ToString();
					newItem["InternalName"] = itemName; // (e.g., Scraps)
					newItem["ItemHandle"] = itemHandle;

					newItem["Name"] = building["DisplayName"] ?? itemName;
					if (newItem["Name"] == "") { newItem["Name"] = itemName; } // Is this happening?
					newItem["Description"] = building["Description"] ?? "";
					string icon = building["Icon"]?.Value.String ?? "";
					int dot = icon.IndexOf('.');
					if (dot >= 0) { icon = icon.Remove(dot); }
					newItem["Icon"] = icon;

					CombinedStringTable[newItem["ItemHandle"]] = newItem["Name"];
					CombinedStringTable[newItem["InternalName"]] = newItem["Name"];

					List<string> categoryTags = [];
					foreach (var gameplayTag in building["Tags"] ?? NotFound)
					{
						string catTag = gameplayTag.Value;

						if (CategoryTags.TryGetValue(catTag, out var catTagDisplayName))
						{
							categoryTags.Add(catTagDisplayName);
						}
					}
					newItem["Tags"] = string.Join(", ", categoryTags);

					if (Game.GetPackage(building["Actor"] ?? "", out var blueprint))
					{
						newItem["Durability"] = blueprint.FindInheritableData("Breakable_GEN_VARIABLE", "MaxHealth") ?? "";
						string stabilityHandle = blueprint.FindInheritableData("MorConstructionSnap_GEN_VARIABLE", "StabilityHandle.RowName") ?? "";
						if (GameObjects.TryGetValue("ConstructionStability." + stabilityHandle, out var stability))
						{
							newItem["HorizStabilityDist"] = stability["HorizontalDistance"];
						}
					}
				}
			}

			Console.WriteLine("Done");
		}

		static void LoadConstructionRecipes()
		{
			if (!Game.GetExportData(ConstructionsPath + "DT_ConstructionRecipes", out var table))
			{
				Console.WriteLine("FAIL: Could not load Construction Recipes.");
				return;
			}

			string itemType = ItemType.ConstructionRecipe.ToString();
			string itemPrefix = itemType +'.';
			List<string> recipeList = [];
			GameObjectsOfType[ItemType.ConstructionRecipe] = recipeList;

			Console.Write("Loading Construction Recipes...");

			List<string> items = [];
			List<string> itemList = [];
			string? itemName = null;
			bool firstItem = true;
			Dictionary<string, string>? building = null;

			foreach (var recipe in table)
			{
				string recipeName = recipe.Name;
				string recipeHandle = itemPrefix + recipeName;

				// ERowEnabledStates: Disabled, Live, and Test. Test items are available in-game, so only skip Disabled items.
				// Ugh. Some items are working in-game even set as Disabled (e.g., Battleaxe_2h_t2 - Steel Battleaxe), so we need a Whitelist in addition to a Blacklist.
				if (GameObjectBlacklist.Contains(recipeHandle))
				{
					continue;
				}

				if ("EMorRecipeUnlockType::Manual".Equals(recipe["DefaultUnlocks.UnlockType"] ?? "") && RecipesDisabled.Contains(recipeName))
				{
					// Recipe temporarily hidden by devs for bugs or testing
					continue;
				}

				string buildingHandle = recipe["ResultConstructionHandle.RowName"] ?? "";
				building = GameObjects.GetValueOrDefault("Construction." + buildingHandle);

				if (building == null)
				{
					// This recipe is for a building that is not available in game.
					continue;
				}

				recipeList.Add(recipeName);

				var newRecipe = GetGameObject(recipeHandle);

				newRecipe["Name"] = building.GetValueOrDefault("Name") ?? "???"; // So it stands out
				if (newRecipe["Name"] == "???") { Debugger.Break(); }

				newRecipe["Disabled"] = ("ERowEnabledState::Disabled".Equals(recipe["EnabledState"]!) && !GameObjectWhitelist.Contains(recipeName)) ? "true" : "false";

				newRecipe["ItemType"] = itemType;
				newRecipe["InternalName"] = recipeName; // (e.g., CraftingStation_BasicFurnace)
				newRecipe["ItemHandle"] = recipeHandle;
				newRecipe["Tags"] = building?.GetValueOrDefault("Tags") ?? "";
				newRecipe["Description"] = building?.GetValueOrDefault("Description") ?? "";
				newRecipe["ConstructionHandle"] = buildingHandle;
				newRecipe["Durability"] = building?.GetValueOrDefault("Durability") ?? "";
				newRecipe["HorizStabilityDist"] = building?.GetValueOrDefault("HorizStabilityDist") ?? "";

				items.Clear();
				foreach (var material in recipe["DefaultRequiredMaterials"] ?? NotFound)
				{
					itemName = LookupString(material["MaterialHandle.RowName"]!);
					if (itemName == "")
					{
						Debugger.Break(); // What happened?
					}
					else
					{
						items.Add(itemName + " (" + (material["Count"] ?? "?") + ")");
					}
				}
				newRecipe["Materials"] = string.Join(", ", items);
				items.Clear();

				// This field is apparently not used. Maybe just a carryover from ItemRecipes.
				foreach (var support in recipe["DefaultRequiredConstructions"] ?? NotFound)
				{
					itemName = LookupString(support["RowName"]!);
					if (itemName == "")
					{
						Debugger.Break(); // What happened?
					}
					else
					{
						items.Add(itemName);
					}
				}
				newRecipe["Supports"] = string.Join(", ", items);
				items.Clear();

				if ("true".Equals(recipe["bHasSandboxRequirementsOverride"] ?? ""))
				{
					foreach (var material in recipe["SandboxRequiredMaterials"] ?? NotFound)
					{
						itemName = LookupString(material["MaterialHandle.RowName"]!);
						if (itemName == "")
						{
							Debugger.Break(); // What happened?
						}
						else
						{
							items.Add(itemName + " (" + (material["Count"] ?? "?") + ")");
						}
					}
					newRecipe["Materials Sandbox(*)"] = string.Join(", ", items);
					items.Clear();

					// This field is apparently not used. Maybe just a carryover from ItemRecipes.
					foreach (var support in recipe["SandboxRequiredConstructions"] ?? NotFound)
					{
						itemName = LookupString(support["RowName"]!);
						if (itemName == "")
						{
							Debugger.Break(); // What happened?
						}
						else
						{
							items.Add(itemName);
						}
					}
					newRecipe["Supports Sandbox(*)"] = string.Join(", ", items);
					items.Clear();

					if (newRecipe["Materials"] == newRecipe["Materials Sandbox(*)"]) { newRecipe["Materials Sandbox(*)"] = ""; }
					if (newRecipe["Supports"] == newRecipe["Supports Sandbox(*)"]) { newRecipe["Supports Sandbox(*)"] = ""; }
				}

				switch (recipe["DefaultUnlocks.UnlockType"] ?? "")
				{
					case "EMorRecipeUnlockType::Manual":
						if (RecipeCampaignUnlocks.Contains(recipeName))
						{
							items.Add("Campaign Progression");
						}
						else if (recipeName.StartsWith("HolidayPack"))
						{
							items.Add("Yule-tide Pack DLC");
						}
						else if (recipeName.StartsWith("RohanPack"))
						{
							items.Add("Rohan Pack DLC");
						}
						else if ("true".Equals(recipe["bHasSandboxUnlockOverride"] ?? ""))
						{
							items.Add("Sandbox Only");
						}
						else
						{
							items.Add("Scripted");
						}
						break;
					case "EMorRecipeUnlockType::CollectFragments":
						items.Add("Recipe Fragments (" + (recipe["DefaultUnlocks.NumFragments"] ?? "?") + ")");
						break;
					case "EMorRecipeUnlockType::DiscoverDependencies":
						foreach (var station in recipe["DefaultUnlocks.UnlockRequiredConstructions"] ?? NotFound)
						{
							itemName = LookupString(station["RowName"]!);
							if (itemName == "")
							{
								Debugger.Break(); // What happened?
							}
							else
							{
								if (firstItem)
								{
									itemName = "Build: " + itemName;
									firstItem = false;
								}
								itemList.Add(itemName);
							}
						}
						if (itemList.Count > 0)
						{
							items.Add(string.Join(", ", itemList));
							itemList.Clear();
							firstItem = true;
						}

						foreach (var material in recipe["DefaultUnlocks.UnlockRequiredItems"] ?? NotFound)
						{
							itemName = LookupString(material["RowName"]!);
							if (itemName == "")
							{
								Debugger.Break(); // What happened?
							}
							else
							{
								if (firstItem)
								{
									itemName = "Obtain: " + itemName;
									firstItem = false;
								}
								itemList.Add(itemName);
							}
						}
						if (itemList.Count > 0)
						{
							items.Add(string.Join(", ", itemList));
							itemList.Clear();
							firstItem = true;
						}
						break;
				}
				newRecipe["Unlock"] = string.Join(", ", items);
				items.Clear();

				if ("true".Equals(recipe["bHasSandboxUnlockOverride"] ?? ""))
				{
					switch (recipe["SandboxUnlocks.UnlockType"] ?? "")
					{
						case "EMorRecipeUnlockType::Manual":
							items.Add("Scripted");
							break;
						case "EMorRecipeUnlockType::CollectFragments":
							items.Add("Recipe Fragments (" + (recipe["SandboxUnlocks.NumFragments"] ?? "?") + ")");
							break;
						case "EMorRecipeUnlockType::DiscoverDependencies":
							foreach (var station in recipe["SandboxUnlocks.UnlockRequiredConstructions"] ?? NotFound)
							{
								itemName = LookupString(station["RowName"]!);
								if (itemName == "")
								{
									Debugger.Break(); // What happened?
								}
								else
								{
									if (firstItem)
									{
										itemName = "Build: " + itemName;
										firstItem = false;
									}
									itemList.Add(itemName);
								}
							}
							if (itemList.Count > 0)
							{
								items.Add(string.Join(", ", itemList));
								itemList.Clear();
								firstItem = true;
							}

							foreach (var material in recipe["SandboxUnlocks.UnlockRequiredItems"] ?? NotFound)
							{
								itemName = LookupString(material["RowName"]!);
								if (itemName == "")
								{
									Debugger.Break(); // What happened?
								}
								else
								{
									if (firstItem)
									{
										itemName = "Obtain: " + itemName;
										firstItem = false;
									}
									itemList.Add(itemName);
								}
							}
							if (itemList.Count > 0)
							{
								items.Add(string.Join(", ", itemList));
								itemList.Clear();
								firstItem = true;
							}
							break;
					}
				}
				newRecipe["Unlock Sandbox (* if different)"] = string.Join(", ", items);

				if ((newRecipe["Unlock"] == "Campaign Progression") && (newRecipe["Unlock Sandbox (* if different)"] == ""))
				{
					newRecipe["Unlock Sandbox (* if different)"] = "Unavailable (Campaign Only)";
				}
			}
		}

		/// <summary>Registers (once) an item effect by asset name and path
		/// </summary>
		/// <param name="assetName"></param>
		/// <param name="assetPath"></param>
		/// <returns>The clean version of the asset name for reference in the Effects list, or an empty string</returns>
		static string RegisterItemEffect(string assetName, string assetPath)
		{
			assetName ??= "";
			string cleanName = assetName;
			if (cleanName.StartsWith("GE_")) { cleanName = cleanName.Remove(0, 3); }
			if (cleanName.EndsWith("_C")) { cleanName = cleanName.Remove(cleanName.Length - 2); }
			
			if (!GameObjects.TryGetValue(assetName, out var effect))
			{
				if (!GameObjectsOfType.TryGetValue(ItemType.Effect, out var list))
				{
					GameObjectsOfType[ItemType.Effect] = list = [];
				}

				Dictionary<string, string> entry = [];
				entry["InternalName"] = assetName;
				entry["Clean Name"] = cleanName;
				entry["AssetPath"] = assetPath;
				
				list.Add(assetName);
				GameObjects.Add(assetName, entry);
			}

			return cleanName;
		}

		/// <summary>Loads effect info from a gameplay effect asset package location
		/// </summary>
		/// <param name="assetName"></param>
		/// <param name="assetLocation"></param>
		/// <returns>The DisplayName of the buff if successful; otherwise, an empty string</returns>
		static string LoadItemEffect(string assetName, string assetLocation)
		{
			if (string.IsNullOrWhiteSpace(assetName)) // We'll accept a blank assetLocation and the effect's name will just be an empty string.
			{
				return "";
			}

			// NOTE: I am breaking up "Buffs" from "Gameplay Effects", as a "buff" is technically just the UI display icon, name, and description.
			// The effects can be happening in the background without a buff being displayed, and with multiple buff options (e.g., Full-Breakfast and then Full-SecondBreakfast when eaten twice).
			// I still need to figure out how to work towards the result of a "Buffs" list with their effects.

			// Locations:
			// Game/Items/ - Misc effects? (GE_WeakPoison_Spider)
			// Game/Character/Shared/Effects/Survival - Consumable effects
			// Game/Character/Shared/Effects/Survival/MealTimeBuffs - Meal buffs
			//
			// GE_WeakPoison_Spider has Duration and Period, with a SuperStruct (parent) in the main export of GE_Poison_Spider_C
			//     GE_Poison_Spider_C has Duration (overruled by the child) with a SuperStruct (parent) in the main export of GE_Poison_Default_C
			//	       GE_Poison_Default_C has Duration and Period (overruled by the child), GE Tag of Debuff.Poison (nvm), Owned Tags of Character.Condition.FoodBuffActive and Character.Condition.Poison, and IgnoreTags of PoisonImunity and Blocking.Left, with a SuperStruct in the main export of GameplayEffect
			//             DT_ThresholdEffects has Poison.RequiredGameplayTag of Character.Condition.Poison, BuildupAttribute of PoisonBuildupRate, MaxoutEffect of GE_Poisoned_C and a reference to the DataTable entry Poisoned, and more
			//				   GCSES/GE_Poisoned has...
			// You know what? Just record the GE asset name for now for item effects. Work on parsing them later.


			// Asset may have effects but no named buffs. I can hard-code a buff name for the effect based on the asset name, with no description.
			// Example: GE_WeakPoison_Spider_C from Meat and Poison Mushrooms can be "Weak Poison" with the effects in the buff entry.

			Dictionary<string, string>? newItem;

			newItem = GetGameObject(assetName);

			if (newItem.TryGetValue("Name", out string? effectName))
			{
				return effectName;
			}

			string itemName = assetName;
			newItem["ItemType"] = ItemType.Effect.ToString();
			newItem["InternalName"] = assetName; // (e.g., Scraps)
			newItem["FileLocation"] = assetLocation;
			newItem["BuffHandle"] = "";
			newItem["Name"] = itemName;
			newItem["Description"] = "";
			newItem["Effects"] = "";

			string effectListString = "";

			List<string> effectList = [];
			if (Game.GetPackage(assetLocation, out var effectPackage))
			{
				float? fBuffDuration = null;
				string buffDuration = "";

				var effectBuffData = effectPackage.GetDefaultExport()?.GetData();
				if ((effectBuffData != null) && "EGameplayEffectDurationType::HasDuration".Equals(effectBuffData["DurationPolicy"]!))
				{
					fBuffDuration = effectBuffData["DurationMagnitude.ScalableFloatMagnitude.Value"]?.Value.Float;
					buffDuration = fBuffDuration?.ToString(UEAData.FloatFormat) ?? "";

					newItem["Duration"] = buffDuration;
				}

				float effectPeriod = effectBuffData?["Period.Value"]?.Value.Float ?? 0.0f;
				string effectPeriodText = effectPeriod.ToString(UEAData.FloatFormat);

				if (effectPackage.TryGetExportData(out var buffUIData, "MorGameplayEffectUIData_0"))
				{
					// This item effect has a buff associated with it
					itemName = buffUIData["DisplayName"] ?? "";
					newItem["Description"] = buffUIData["Description"] ?? "";
				}
				else
				{
					switch (assetName.ToString())
					{
						case "GE_WeakPoison_Spider_C":
							itemName = "Weak Poison"; // Will replace this with actual effects from the Weak Poison
							effectList.Add("Add Poison Buildup");
							break;
						default:
							Debugger.Break();
							break;
					}
				}

				if (effectBuffData != null)
				{
					foreach (UEAData mod in effectBuffData["Modifiers"] ?? NotFound)
					{
						List<string> modText = [];

						string effectStat = mod["Attribute.AttributeName"] ?? "UnknownStat";
						string effectOp = mod["ModifierOp"] ?? "";
						string effectMagType = mod["ModifierMagnitude.MagnitudeCalculationType"] ?? "";
						string effectMagnitude = "";
						switch (effectMagType)
						{
							case "EGameplayEffectMagnitudeCalculation::ScalableFloat":
								effectMagnitude = mod["ModifierMagnitude.ScalableFloatMagnitude.Value"] ?? "";
								break;
							default:
								Debugger.Break();
								break;
						}

						modText.Add(effectStat);
						modText.Add(" ");
						switch (effectOp)
						{
							case "EGameplayModOp::Additive":
								modText.Add(" +");
								break;
							case "EGameplayModOp::Override":
								modText.Add(" = ");
								break;
							default:
								Debugger.Break();
								break;
						}

						modText.Add((effectMagnitude == "") ? "UnknownAmount" : effectMagnitude);

						effectList.Add(string.Join("", modText));
					}
				}

				// TODO: Load more buff data from various fields and asset packages

				//
				// I may have to hard-code a map from the buff tag to the correct node for the duration.
				// DT_Brews\Health_Brew\UseEffects[0] = GE_HealthBrewSip_C
				//     GE_HealthBrewSip_C\Default\InheritableGameplayEffectTags\Added\Added = Buff.Brew.HealthBrew
				//     GE_HealthBrewSip_C hard-coded mapped (?) to GE_HealthBrew_C
				//         GE_HealthBrew_C\Default\DurationMagnitude\ScalableFloatMagnitude\Value = 480.0
				//         GE_HealthBrew_C\MorGameplayEffectUIData\MorGameplayEffectUIData\DisplayName = StringTableLookup for buff name (e.g., HealthBrew -> Revitalized)
				//
				// Can I look up what the buff does, for a Buffs tab?

				effectListString = string.Join(", ", effectList);

				if (effectPeriod > 0.0f)
				{
					if (effectPeriodText == "1.0")
					{
						effectListString += " per second";
					}
					else
					{
						effectListString += " every " + effectPeriodText + " seconds";
					}
				}
				
				// Is this necessary with the Duration field being exported? Could be helpful seen in isolation.
				if ((buffDuration != "") && (effectList.Count > 0))
				{
					effectListString += " for " + buffDuration + " second" + ((buffDuration == "1.0") ? "" : "s");
				}
			}
			else
			{
				// Anything to do if we can't load the asset?
			}

			newItem["Name"] = itemName;
			newItem["Effects"] = effectListString;
			CombinedStringTable[assetName] = itemName;
			return itemName;
		}

		static void LoadStringTables(string Name = "")
		{
			if (Name == "")
			{
				string[] stringTables = StringTableList.Split(',');

				foreach (string stringTable in stringTables)
				{
					LoadStringTables(stringTable);
				}
			}
			else
			{
				string stPath = string.Concat(StringTablePath, Name);
				string stNodeName = string.Concat(stPath, '.', Name);

				if (!StringTables.TryGetValue(stNodeName, out Dictionary<string, string>? stringTable))
				{
					stringTable = [];

					StringTables.Add(stNodeName, stringTable);
					StringTables.Add(Name, stringTable);
				}

				if (Game.GetExportData(stPath, out var table))
				{
					foreach (var entry in table ?? NotFound)
					{
						stringTable[entry.Name] = entry.Value;
						CombinedStringTable[entry.Name] = entry.Value;
					}
				}
			}
		}

		static string LookupString(string? Key, string StringTablePath = "", string Default = "")
		{
			if ((Key == null) || (Key == ""))
			{
				return Default;
			}

			string? _result;

			if (StringTablePath != "")
			{
				if (StringTables.TryGetValue(StringTablePath, out var _stringTable))
				{
					if (_stringTable.TryGetValue(Key, out _result))
					{
						return _result;
					}
				}
			}

			if (CombinedStringTable.TryGetValue(Key, out _result))
			{
				return _result;
			}

			return Default;
		}

		static Dictionary<string, string> GetGameObject(string ObjectName)
		{
			if (GameObjects.TryGetValue(ObjectName, out var _object))
			{
				return _object;
			}

			var _newObject = new Dictionary<string, string>();

			GameObjects.Add(ObjectName, _newObject);

			return _newObject;
		}

		static void WriteSpreadsheet(string path)
		{
			bool ok = false;
			while (ok == false)
			{
				try
				{ 
					File.Delete(path);
					ok = true;
				}
				catch
				{
					Console.Write("\nUnable to write to output file. Close and press any key (Esc to cancel)...");
					if (Console.ReadKey().Key == ConsoleKey.Escape) { return; }
				}
			}

			using (var file = new ExcelPackage(path))
			{
				var headerBG = System.Drawing.Color.FromArgb(0xdd, 0xeb, 0xf7);
				bool recipeRequired;
				
				foreach ((ItemType itemType, string fieldList) in ItemFieldOrder)
				{
					var sheet = file.Workbook.Worksheets.Add(itemType.ToString());

					var fields = fieldList.Split(',');

					switch (itemType)
					{
						case ItemType.Armor:
						case ItemType.Brew:
						case ItemType.Consumable:
						case ItemType.ThrowLight:
						case ItemType.Tool:
						case ItemType.Weapon:
							recipeRequired = true;
							break;
						default:
							recipeRequired = false;
							break;
					}
					
					int row = 1;
					int col = 1;
					int nameCol = 1;

					foreach (string field in fields)
					{
						if (field == "Name") { nameCol = col; }

						var cell = sheet.Cells[row, col++];

						cell.Style.Font.Bold = true;
						cell.Style.Fill.SetBackground(headerBG);

						cell.Value = field;
					}

					if (GameObjectsOfType.TryGetValue(itemType, out var itemKeys))
					{
						string prefix = itemType.ToString() + ".";
						foreach (string key in itemKeys)
						{
							var item = GetGameObject(prefix + key);

#pragma warning disable CS0162 // Unreachable code detected
							if (recipeRequired && !"true".Equals(item.GetValueOrDefault("HasRecipe")) && !GameObjectWhitelist.Contains(item["InternalName"]))
							{
								if (false) // Debugging. Export the items but list them as (Skipped)
								{
									item["Name"] = "(Skipped) " + item["Name"];
								}
								else
								{
									continue;
								}
							}
							else if (false) // ("true".Equals(item.GetValueOrDefault("Disabled")))
							{
								// For debugging purposes only.

								//item["Name"] = "(Disabled) " + item["Name"];
								item["Name"] = item["Name"] + " (Disabled)";
							}
#pragma warning restore CS0162 // Unreachable code detected

							row++;
							col = 0;

							foreach (string field in fields)
							{
								sheet.Cells[row, ++col].Value = item.GetValueOrDefault(field) ?? "";
							}
						}
					}

					sheet.Cells[2, 1, row, col].Sort(sort => sort.SortBy.Column(nameCol - 1));
					sheet.Cells.AutoFitColumns();
					for (int i = 1; i <= col; i++)
					{
						sheet.Column(i).Width += 0.5f;
						if (sheet.Column(i).Width > MaxColumnWidth) { sheet.Column(i).Width = MaxColumnWidth; }
					}
					sheet.View.FreezePanes(2, 2);
				}
				
				file.Save();

				Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
			}
		}
	}
}
