// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors: InconsolableCellist
//
// Notes:
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallConnect.FallExe;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallConnect;
using DaggerfallConnect.Utility;

namespace DaggerfallWorkshop.Game.Items
{
    /// <summary>
    /// Generates new items for various game systems.
    /// This helper still under development.
    /// </summary>
    public static class ItemBuilder
    {
        #region Data

        public const int firstFemaleArchive = 245;
        public const int firstMaleArchive = 249;
        private const int chooseAtRandom = -1;

        // This array is used to pick random material values.
        // The array is traversed, subtracting each value from a sum until the sum is less than the next value.
        // Steel through Daedric, or Iron if sum is less than the first value.
        public static readonly byte[] materialsByModifier = { 64, 128, 10, 21, 13, 8, 5, 3, 2, 5 };

        // Weight multipliers by material type. Iron through Daedric. Weight is baseWeight * value / 4.
        // ProjectN: modified every value - still to playtest intensively.
        static readonly short[] weightMultipliersByMaterial = { 0, 5, 4, 4, 2, 3, 7, 3, 6, 2, 12, 1 };

        // Value multipliers by material type. Iron through Daedric. Value is baseValue * ( 3 * value).
        static readonly short[] valueMultipliersByMaterial = { 0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 20 };

        // Condition multipliers by material type. Iron through Daedric. MaxCondition is baseMaxCondition * value / 4.
        // ProjectN: modifiied every value - still to playtest intensively.
        static readonly short[] conditionMultipliersByMaterial = { 0, 4, 6, 4, 6, 12, 16, 7, 20, 12, 16, 5 };

        // Enchantment point/gold value data for item powers
        static readonly int[] extraSpellPtsEnchantPts = { 0x1F4, 0x1F4, 0x1F4, 0x1F4, 0xC8, 0xC8, 0xC8, 0x2BC, 0x320, 0x384, 0x3E8 };
        static readonly int[] potentVsEnchantPts = { 0x320, 0x384, 0x3E8, 0x4B0 };
        static readonly int[] regensHealthEnchantPts = { 0x0FA0, 0x0BB8, 0x0BB8 };
        static readonly int[] vampiricEffectEnchantPts = { 0x7D0, 0x3E8 };
        static readonly int[] increasedWeightAllowanceEnchantPts = { 0x190, 0x258 };
        static readonly int[] improvesTalentsEnchantPts = { 0x1F4, 0x258, 0x258 };
        static readonly int[] goodRepWithEnchantPts = { 0x3E8, 0x3E8, 0x3E8, 0x3E8, 0x3E8, 0x1388 };
        static readonly int[][] enchantmentPtsForItemPowerArrays = { null, null, null, extraSpellPtsEnchantPts, potentVsEnchantPts, regensHealthEnchantPts,
                                                                    vampiricEffectEnchantPts, increasedWeightAllowanceEnchantPts, null, null, null, null, null,
                                                                    improvesTalentsEnchantPts, goodRepWithEnchantPts};
        static readonly ushort[] enchantmentPointCostsForNonParamTypes = { 0, 0x0F448, 0x0F63C, 0x0FF9C, 0x0FD44, 0, 0, 0, 0x384, 0x5DC, 0x384, 0x64, 0x2BC };

        public static int newArmorArchive = 0;

        public enum BodyMorphology
        {
            Argonian = 0,
            Elf = 1,
            Human = 2,
            Khajiit = 3,
        }

        /// <summary>
        /// Every clothing dye.
        /// </summary>
        public static DyeColors[] clothingDyes = {
            DyeColors.Blue,
            DyeColors.Grey,
            DyeColors.Red,
            DyeColors.DarkBrown,
            DyeColors.Purple,
            DyeColors.LightBrown,
            DyeColors.White,
            DyeColors.Aquamarine,
            DyeColors.Yellow,
            DyeColors.Green,
            DyeColors.Olive,
            DyeColors.Amber,
            DyeColors.DarkGrey,
            DyeColors.Iron,
            DyeColors.Steel,
            DyeColors.Leather,
            DyeColors.Silver,
            DyeColors.Elven,
            DyeColors.Glass,
            DyeColors.Dwarven,
            DyeColors.Mithril,
            DyeColors.Adamantium,
            DyeColors.Ebony,
            DyeColors.Orcish,
            DyeColors.Daedric
        };

        /// <summary>
        /// Regular clothing dye.
        /// </summary>
        public static DyeColors[] regularClothingDyes = {
            // DyeColors.Blue,
            DyeColors.Grey,
            // DyeColors.Red,
            DyeColors.DarkBrown,
            // DyeColors.Purple,
            DyeColors.LightBrown,
            DyeColors.White,
            // DyeColors.Aquamarine,
            // DyeColors.Yellow,
            // DyeColors.Green,
            DyeColors.Olive,
            // DyeColors.Amber,
            DyeColors.DarkGrey
        };

        /// <summary>
        /// Every metal dye.
        /// </summary>
        public static DyeColors[] metalDyes = {
            DyeColors.Iron,
            DyeColors.Steel,
            DyeColors.Silver,
            DyeColors.Elven,
            DyeColors.Glass,
            DyeColors.Dwarven,
            DyeColors.Orcish,
            DyeColors.Mithril,
            DyeColors.Adamantium,
            DyeColors.Ebony,
            DyeColors.Daedric
        };

        /// <summary>
        /// Cheap clothing dye.
        /// </summary>
        public static DyeColors[] cheapClothingDyes = {
            DyeColors.Grey,
            DyeColors.DarkBrown,
            DyeColors.LightBrown,
            // DyeColors.White,
            // DyeColors.Olive,
        };

        /// <summary>
        /// Fancy clothing dye.
        /// </summary>
        public static DyeColors[] fancyClothingDyes = {
            DyeColors.Blue,
            DyeColors.Red,
            DyeColors.Purple,
            DyeColors.White,
            DyeColors.Aquamarine,
            DyeColors.Yellow,
            DyeColors.Green,
            DyeColors.Amber,
            DyeColors.DarkGrey
        };

        public static DyeColors[] workClothingDyes = {
            DyeColors.Grey,
            DyeColors.DarkBrown,
            DyeColors.LightBrown,
            DyeColors.DarkGrey,
            DyeColors.Leather
        };

        #endregion

        #region Public Methods

        public static DyeColors RandomClothingDye()
        {
            return clothingDyes[UnityEngine.Random.Range(0, clothingDyes.Length)];
        }

        public static DyeColors RandomRegularClothingDye()
        {
            return regularClothingDyes[UnityEngine.Random.Range(0, regularClothingDyes.Length)];
        }

        public static DyeColors RandomMetalDye()
        {
            return metalDyes[UnityEngine.Random.Range(0, metalDyes.Length)];
        }

        public static DyeColors RandomCheapClothingDye()
        {
            return cheapClothingDyes[UnityEngine.Random.Range(0, cheapClothingDyes.Length)];
        }

        public static DyeColors RandomFancyClothingDye()
        {
            return fancyClothingDyes[UnityEngine.Random.Range(0, fancyClothingDyes.Length)];
        }

        public static DyeColors RandomWorkClothingDye()
        {
            return workClothingDyes[UnityEngine.Random.Range(0, workClothingDyes.Length)];
        }

        /// <summary>
        /// Creates a generic item from group and template index.
        /// </summary>
        /// <param name="itemGroup">Item group.</param>
        /// <param name="templateIndex">Template index.</param>
        /// <returns>DaggerfallUnityItem.</returns>
        public static DaggerfallUnityItem CreateItem(ItemGroups itemGroup, int templateIndex)
        {
            // Handle custom items
            if (templateIndex > ItemHelper.LastDFTemplate)
            {
                // Allow custom item classes to be instantiated when registered
                Type itemClassType;
                if (DaggerfallUnity.Instance.ItemHelper.GetCustomItemClass(templateIndex, out itemClassType))
                    return (DaggerfallUnityItem)Activator.CreateInstance(itemClassType);
                else
                    return new DaggerfallUnityItem(itemGroup, templateIndex);
            }

            // Create classic item
            int groupIndex = DaggerfallUnity.Instance.ItemHelper.GetGroupIndex(itemGroup, templateIndex);
            if (groupIndex == -1)
            {
                Debug.LogErrorFormat("ItemBuilder.CreateItem() encountered an item with an invalid GroupIndex. Check you're passing 'template index' matching a value in ItemEnums - e.g. (int)Weapons.Dagger NOT a 'group index' (e.g. 0).");
                return null;
            }
            DaggerfallUnityItem newItem = new DaggerfallUnityItem(itemGroup, groupIndex);

            return newItem;
        }

        /// <summary>
        /// Super generic item creator: the less is passed, the more random the generation gets.
        /// I added some bool to limit the creation to functional items (e.g.: no furnitures, horses, etc.)
        /// </summary>
        public static DaggerfallUnityItem CreateRandomItem(ItemGroups itemGroup = ItemGroups.None, int minTempIndex = -1, int maxTempIndex = -1, bool filterCrazyStuff = true)
        {
            if (itemGroup == ItemGroups.None)
            {
                do
                {
                    itemGroup = (ItemGroups)UnityEngine.Random.Range(0, Enum.GetNames(typeof(ItemGroups)).Length);
                }
                while (filterCrazyStuff && (itemGroup == ItemGroups.Artifacts ||
                                            itemGroup == ItemGroups.Currency ||
                                            itemGroup == ItemGroups.Deeds ||
                                            itemGroup == ItemGroups.Furniture ||
                                            itemGroup == ItemGroups.MagicItems ||
                                            itemGroup == ItemGroups.MiscItems ||
                                            itemGroup == ItemGroups.QuestItems ||
                                            itemGroup == ItemGroups.Transportation ||
                                            itemGroup == ItemGroups.UselessItems1));    // This could be removed from here when some use is given to these items
            }

            int[] enumArray = (int[])DaggerfallUnity.Instance.ItemHelper.GetEnumArray(itemGroup);
            if (minTempIndex < enumArray[0]) minTempIndex = enumArray[0];
            if (maxTempIndex >= enumArray.Length || maxTempIndex == -1) maxTempIndex = enumArray.Length - 1;

            DaggerfallUnityItem newItem = new DaggerfallUnityItem(itemGroup, UnityEngine.Random.Range(minTempIndex, (maxTempIndex + 1)));

            return newItem;
        }

        /// <summary>
        /// Generates men's clothing.
        /// </summary>
        /// <param name="item">Item type to generate.</param>
        /// <param name="race">Race of player.</param>
        /// <param name="variant">Variant to use. If not set, a random variant will be selected.</param>
        /// <param name="dye">Dye to use</param>
        /// <returns>DaggerfallUnityItem.</returns>
        public static DaggerfallUnityItem CreateMensClothing(MensClothing item, Races race, int qualityMod = 0, int variant = -1, DyeColors dye = DyeColors.Blue)
        {
            // Create item
            int groupIndex = DaggerfallUnity.Instance.ItemHelper.GetGroupIndex(ItemGroups.MensClothing, (int)item);
            DaggerfallUnityItem newItem = new DaggerfallUnityItem(ItemGroups.MensClothing, groupIndex);

            // Random variant
            if (variant < 0)
                variant = UnityEngine.Random.Range(0, newItem.ItemTemplate.variants);

            Debug.Log("groupIndex: " + groupIndex + ", variant: " + variant);

            // Set race, variant, dye
            SetRace(newItem, race);
            SetClothQuality(newItem, qualityMod, out ClothCraftsmanship clothCraftsmanship);
            SetVariant(newItem, variant);
            // newItem.dyeColor = dye;

            return newItem;
        }

        /// <summary>
        /// Generates women's clothing.
        /// </summary>
        /// <param name="item">Item type to generate.</param>
        /// <param name="race">Race of player.</param>
        /// <param name="variant">Variant to use. If not set, a random variant will be selected.</param>
        /// <param name="dye">Dye to use</param>
        /// <returns>DaggerfallUnityItem.</returns>
        public static DaggerfallUnityItem CreateWomensClothing(WomensClothing item, Races race, int qualityMod = 0, int variant = -1, DyeColors dye = DyeColors.Blue)
        {
            // Create item
            int groupIndex = DaggerfallUnity.Instance.ItemHelper.GetGroupIndex(ItemGroups.WomensClothing, (int)item);
            DaggerfallUnityItem newItem = new DaggerfallUnityItem(ItemGroups.WomensClothing, groupIndex);

            // Random variant
            if (variant < 0)
                variant = UnityEngine.Random.Range(0, newItem.ItemTemplate.variants);

            // Set race, variant, dye
            SetRace(newItem, race);
            SetClothQuality(newItem, qualityMod, out ClothCraftsmanship clothCraftsmanship);
            SetVariant(newItem, variant);
            // newItem.dyeColor = dye;

            return newItem;
        }

        /// <summary>
        /// Creates a new item of random clothing.
        /// </summary>
        /// <param name="gender">Gender of player</param>
        /// <returns>DaggerfallUnityItem.</returns>
        public static DaggerfallUnityItem CreateRandomClothing(Genders gender, Races race, int qualityMod = 0)
        {
            // Create random clothing by gender, including any custom items registered as clothes
            ItemGroups genderClothingGroup = (gender == Genders.Male) ? ItemGroups.MensClothing : ItemGroups.WomensClothing;

            ItemHelper itemHelper = DaggerfallUnity.Instance.ItemHelper;
            Array enumArray = itemHelper.GetEnumArray(genderClothingGroup);
            int[] customItemTemplates = itemHelper.GetCustomItemsForGroup(genderClothingGroup);

            int groupIndex = UnityEngine.Random.Range(0, enumArray.Length + customItemTemplates.Length);
            DaggerfallUnityItem newItem;
            if (groupIndex < enumArray.Length)
                newItem = new DaggerfallUnityItem(genderClothingGroup, groupIndex);
            else
                newItem = CreateItem(genderClothingGroup, customItemTemplates[groupIndex - enumArray.Length]);

            SetRace(newItem, race);
            SetClothQuality(newItem, qualityMod, out ClothCraftsmanship clothCraftsmanship);

            // In ProjectN, the colour is chosen with the variant
            // SetClothColors(newItem, clothCraftsmanship);
            // newItem.dyeColor = RandomClothingDye();

            // Random variant
            SetVariant(newItem, UnityEngine.Random.Range(0, newItem.TotalVariants));

            return newItem;
        }

        /// <summary>
        /// Creates a new book.
        /// </summary>
        /// <param name="fileName">The name of the books resource.</param>
        /// <returns>An instance of the book item or null.</returns>
        public static DaggerfallUnityItem CreateBook(string fileName)
        {
            if (!Path.HasExtension(fileName))
                fileName += ".TXT";

            var entry = BookReplacement.BookMappingEntries.Values.FirstOrDefault(x => x.Name.Equals(fileName, StringComparison.Ordinal));
            if (entry.ID != 0)
                return CreateBook(entry.ID);

            int id;
            if (fileName.Length == 12 && fileName.StartsWith("BOK") && int.TryParse(fileName.Substring(3, 5), out id))
                return CreateBook(id);

            return null;
        }

        /// <summary>
        /// Creates a new book.
        /// </summary>
        /// <param name="id">The numeric id of book resource.</param>
        /// <returns>An instance of the book item or null.</returns>
        public static DaggerfallUnityItem CreateBook(int id)
        {
            var bookFile = new BookFile();

            string name = GameManager.Instance.ItemHelper.GetBookFileName(id);
            if (!BookReplacement.TryImportBook(name, bookFile) &&
                !bookFile.OpenBook(DaggerfallUnity.Instance.Arena2Path, name))
                return null;

            return new DaggerfallUnityItem(ItemGroups.Books, 0)
            {
                message = id,
                value = bookFile.Price
            };
        }

        /// <summary>
        /// Creates a new random book
        /// </summary>
        /// <returns>DaggerfallUnityItem.</returns>
        public static DaggerfallUnityItem CreateRandomBook()
        {
            Array enumArray = DaggerfallUnity.Instance.ItemHelper.GetEnumArray(ItemGroups.Books);
            DaggerfallUnityItem book = new DaggerfallUnityItem(ItemGroups.Books, Array.IndexOf(enumArray, Books.Book0));
            book.message = DaggerfallUnity.Instance.ItemHelper.GetRandomBookID();
            book.CurrentVariant = UnityEngine.Random.Range(0, book.TotalVariants);
            // Update item value for this book.
            BookFile bookFile = new BookFile();
            string name = GameManager.Instance.ItemHelper.GetBookFileName(book.message);
            if (!BookReplacement.TryImportBook(name, bookFile))
                bookFile.OpenBook(DaggerfallUnity.Instance.Arena2Path, name);
            book.value = bookFile.Price;
            return book;
        }

        /// <summary>
        /// Creates a new random religious item.
        /// </summary>
        /// <returns>DaggerfallUnityItem.</returns>
        public static DaggerfallUnityItem CreateRandomReligiousItem()
        {
            Array enumArray = DaggerfallUnity.Instance.ItemHelper.GetEnumArray(ItemGroups.ReligiousItems);
            int groupIndex = UnityEngine.Random.Range(0, enumArray.Length);
            DaggerfallUnityItem newItem = new DaggerfallUnityItem(ItemGroups.ReligiousItems, groupIndex);

            return newItem;
        }

        public static DaggerfallUnityItem CreateRandomlyFilledSoulTrap()
        {
            // Create a trapped soul type and filter invalid creatures
            MobileTypes soul = MobileTypes.None;
            while (soul == MobileTypes.None)
            {
                MobileTypes randomSoul = (MobileTypes)UnityEngine.Random.Range((int)MobileTypes.Rat, (int)MobileTypes.Lamia + 1);
                if (randomSoul == MobileTypes.Horse_Invalid ||
                    randomSoul == MobileTypes.Dragonling)       // NOTE: Dragonling (34) is soulless, only soul of Dragonling_Alternate (40) from B0B70Y16 has a soul
                    continue;
                else
                    soul = randomSoul;
            }

            // Generate item
            DaggerfallUnityItem newItem = CreateItem(ItemGroups.MiscItems, (int)MiscItems.Soul_trap);
            newItem.TrappedSoulType = soul;
            MobileEnemy mobileEnemy = GameObjectHelper.EnemyDict[(int)soul];
            newItem.value = 5000 + mobileEnemy.SoulPts;

            return newItem;
        }

        /// <summary>
        /// Creates a new random gem.
        /// </summary>
        /// <returns>DaggerfallUnityItem.</returns>
        public static DaggerfallUnityItem CreateRandomGem()
        {
            Array enumArray = DaggerfallUnity.Instance.ItemHelper.GetEnumArray(ItemGroups.Gems);
            int groupIndex = UnityEngine.Random.Range(0, enumArray.Length);
            DaggerfallUnityItem newItem = new DaggerfallUnityItem(ItemGroups.Gems, groupIndex);

            return newItem;
        }

        /// <summary>
        /// Creates a new random jewellery.
        /// </summary>
        /// <returns>DaggerfallUnityItem.</returns>
        public static DaggerfallUnityItem CreateRandomJewellery()
        {
            Array enumArray = DaggerfallUnity.Instance.ItemHelper.GetEnumArray(ItemGroups.Jewellery);
            int groupIndex = UnityEngine.Random.Range(0, enumArray.Length);
            DaggerfallUnityItem newItem = new DaggerfallUnityItem(ItemGroups.Jewellery, groupIndex);

            return newItem;
        }

        /// <summary>
        /// Creates a new random drug.
        /// </summary>
        /// <returns>DaggerfallUnityItem.</returns>
        public static DaggerfallUnityItem CreateRandomDrug()
        {
            Array enumArray = DaggerfallUnity.Instance.ItemHelper.GetEnumArray(ItemGroups.Drugs);
            int groupIndex = UnityEngine.Random.Range(0, enumArray.Length);
            DaggerfallUnityItem newItem = new DaggerfallUnityItem(ItemGroups.Drugs, groupIndex);

            return newItem;
        }

        /// <summary>
        /// Generates a weapon.
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="material">Ignored for arrows</param>
        /// <returns></returns>
        public static DaggerfallUnityItem CreateWeapon(Weapons weapon, MaterialTypes material)
        {
            // Create item
            int groupIndex = DaggerfallUnity.Instance.ItemHelper.GetGroupIndex(ItemGroups.Weapons, (int)weapon);
            DaggerfallUnityItem newItem = new DaggerfallUnityItem(ItemGroups.Weapons, groupIndex);

            if (weapon == Weapons.Arrow)
            {   // Handle arrows
                newItem.stackCount = UnityEngine.Random.Range(1, 20 + 1);
                newItem.currentCondition = 0; // not sure if this is necessary, but classic does it
            }

            ApplyWeaponMaterial(newItem, material);
            int variant = GetWeaponVariant(newItem, material);
            SetVariant(newItem, variant);

            return newItem;
        }

        public static int GetWeaponVariant(DaggerfallUnityItem weapon, MaterialTypes material)
        {
            // For the moment, there's no Glass Exotic variant
            if (material == MaterialTypes.Glass || WeaponHasNoVariants(weapon.TemplateIndex))
                return 0;

            if (!GameManager.HasInstance)
                return 0;

            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            int chances = 1;

            if (IsFromThisArea(weapon))
                chances = 900;
            else if (playerGPS.IsPlayerInTown(true) && playerGPS.CurrentLocation.Exterior.ExteriorData.PortTownAndUnknown != 0)
                chances = 500;

            Debug.Log("Getting weapon variant");
            if (DFRandom.random_range_inclusive(1, 1000) <= chances)
                return 1;

            return 0;
        }

        public static bool IsFromThisArea(DaggerfallUnityItem weapon)
        {
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

            // At the moment, it's not possible to visit a place where daedric exotic weapons are "common"
            if (weapon.nativeMaterialValue == (int)MaterialTypes.Daedric)
                return false;

            if (weapon.nativeMaterialValue == (int)MaterialTypes.Iron)
            {
                if (IsFromUpperCraglorn(playerGPS.CurrentRegionIndex) &&
                   (weapon.TemplateIndex == (int)Weapons.Claymore ||
                    weapon.TemplateIndex == (int)Weapons.Warhammer ||
                    weapon.TemplateIndex == (int)Weapons.War_Axe))
                    return true;
            }

            if (weapon.nativeMaterialValue == (int)MaterialTypes.Orcish)
            {
                if (playerGPS.CurrentRegionIndex == Array.IndexOf(WorldData.WorldSetting.RegionNames, "Orsinium Area") &&
                   (weapon.TemplateIndex == (int)Weapons.Dagger ||
                    weapon.TemplateIndex == (int)Weapons.Tanto ||
                    weapon.TemplateIndex == (int)Weapons.Staff ||
                    weapon.TemplateIndex == (int)Weapons.Shortsword ||
                    weapon.TemplateIndex == (int)Weapons.Wakazashi ||
                    weapon.TemplateIndex == (int)Weapons.Longsword ||
                    weapon.TemplateIndex == (int)Weapons.Katana ||
                    weapon.TemplateIndex == (int)Weapons.Dai_Katana))
                    return true;
                if (IsFromHollowWastes(playerGPS.CurrentRegionIndex) &&
                   (weapon.TemplateIndex == (int)Weapons.Broadsword ||
                    weapon.TemplateIndex == (int)Weapons.Saber ||
                    weapon.TemplateIndex == (int)Weapons.Claymore ||
                    weapon.TemplateIndex == (int)Weapons.Mace ||
                    weapon.TemplateIndex == (int)Weapons.Flail))
                    return true;
                if (IsFromValusMountains(playerGPS.CurrentRegionIndex) &&
                   (weapon.TemplateIndex == (int)Weapons.Warhammer ||
                    weapon.TemplateIndex == (int)Weapons.Battle_Axe ||
                    weapon.TemplateIndex == (int)Weapons.War_Axe))
                    return true;
                if (IsFromGreenshade(playerGPS.CurrentRegionIndex) &&
                    weapon.TemplateIndex == (int)Weapons.Short_Bow)
                    return true;
                if (IsFromGrathwood(playerGPS.CurrentRegionIndex) &&
                    weapon.TemplateIndex == (int)Weapons.Long_Bow)
                    return true;
            }

            return RandomWeaponOrigin(playerGPS.CurrentRegionIndex, weapon);
        }

        public static bool RandomWeaponOrigin(int regionIndex, DaggerfallUnityItem weapon)
        {
            int factor = weapon.TemplateIndex * weapon.nativeMaterialValue * 7;
            int regionNumber = WorldData.WorldSetting.RegionNames.Length;
            while (factor >= regionNumber)
            {
                factor -= regionNumber;
            }
            return regionIndex == factor;
        }

        public static int RandomWeaponOrigin(DaggerfallUnityItem weapon)
        {
            int factor = weapon.TemplateIndex * weapon.nativeMaterialValue * 7;
            int regionNumber = WorldData.WorldSetting.RegionNames.Length;
            while (factor >= regionNumber)
            {
                factor -= regionNumber;
            }
            return factor;
        }

        /// <summary>
        /// Checks if the passed region index belongs to a region in the Hollow Wastes area
        /// (https://en.uesp.net/wiki/Lore:Hollow_Wastes)
        /// </summary>
        public static bool IsFromHollowWastes(int regionIndex)
        {
            switch (regionIndex)
            {
                case 11:    // Dak'fron
                case 20:    // Sentinel
                case 47:    // Ayasofya
                case 48:    // Tigonus
                case 54:    // Santaki
                case 55:    // Antiphyllos
                case 56:    // Bergama
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if the passed region index belongs to a region in the Valus Mountains area
        /// (https://en.uesp.net/wiki/Lore:Valus_Mountains)
        /// </summary>
        public static bool IsFromValusMountains(int regionIndex)
        {
            switch (regionIndex)
            {
                case 153:   // Kragenmoor
                case 154:   // Andrethis
                case 351:   // Cheydinhal
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if the passed region index belongs to a region in the Grathwood area
        /// (https://en.uesp.net/wiki/Lore:Grahtwood)
        /// </summary>
        public static bool IsFromGrathwood(int regionIndex)
        {
            switch (regionIndex)
            {
                case 244:   // Tarlain Heights
                case 245:   // Cormount
                case 246:   // Stonesquare
                case 247:   // Southpoint
                case 251:   // Haven
                case 299:   // Greenhall
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if the passed region index belongs to a region in the Greenshade area
        /// (https://en.uesp.net/wiki/Lore:Greenshade)
        /// </summary>
        public static bool IsFromGreenshade(int regionIndex)
        {
            switch (regionIndex)
            {
                case 239:   // Woodheart
                case 240:   // Vullen Haven
                case 241:   // Longhaven
                case 242:   // Marbruk Field
                case 243:   // Greenheart
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if the passed region index belongs to a region in the Upper Craglorn area
        /// (https://en.uesp.net/wiki/Lore:Craglorn)
        /// </summary>
        public static bool IsFromUpperCraglorn(int regionIndex)
        {
            switch (regionIndex)
            {
                case 104:   // Belkarth
                case 105:   // Dragon Gate
                case 106:   // Dragonstar
                    return true;

                default:
                    return false;
            }
        }

        public static bool WeaponHasNoVariants(int weaponIndex)
        {
            switch (weaponIndex)
            {
                case 288:   // Hatchet
                case 289:   // Flail
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates random weapon.
        /// </summary>
        /// <returns>DaggerfallUnityItem</returns>
        public static DaggerfallUnityItem CreateRandomWeapon(int luck, int levelModifier = 1, bool isTownGuard = false)
        {
            // Create a random weapon type, including any custom items registered as weapons
            ItemHelper itemHelper = DaggerfallUnity.Instance.ItemHelper;
            Array enumArray = itemHelper.GetEnumArray(ItemGroups.Weapons);
            int[] customItemTemplates = itemHelper.GetCustomItemsForGroup(ItemGroups.Weapons);

            int groupIndex = UnityEngine.Random.Range(0, enumArray.Length + customItemTemplates.Length);
            DaggerfallUnityItem newItem;
            if (groupIndex < enumArray.Length)
                newItem = new DaggerfallUnityItem(ItemGroups.Weapons, groupIndex);
            else
                newItem = CreateItem(ItemGroups.Weapons, customItemTemplates[groupIndex - enumArray.Length]);

            // Random weapon material
            MaterialTypes material = FormulaHelper.RandomMaterial(luck, levelModifier, isTownGuard);

            if (groupIndex == 18)
            {   // Handle arrows
                newItem.stackCount = UnityEngine.Random.Range(1, 20 + 1);
                newItem.currentCondition = 0; // not sure if this is necessary, but classic does it
            }

            ApplyWeaponMaterial(newItem, material);
            int variant = GetWeaponVariant(newItem, material);
            SetVariant(newItem, variant);

            return newItem;
        }

        /// <summary>Set material and adjust weapon stats accordingly</summary>
        public static void ApplyWeaponMaterial(DaggerfallUnityItem weapon, MaterialTypes material)
        {
            weapon.nativeMaterialValue = (int)material;
            weapon = SetItemPropertiesByMaterial(weapon, (int)material);
            weapon.dyeColor = DaggerfallUnity.Instance.ItemHelper.GetWeaponDyeColor(material);

            // Female characters use archive - 1 (i.e. 233 rather than 234) for weapons
            if (GameManager.Instance.PlayerEntity.Gender == Genders.Female)
            {
                if (!GameManager.Instance.ItemHelper.IsNewWeapon(weapon) || weapon.TemplateIndex == (int)Weapons.Crossbow)
                    weapon.PlayerTextureArchive -= 1;
            }
        }

        /// <summary>
        /// Generates armour.
        /// </summary>
        /// <param name="gender">Gender armor is created for.</param>
        /// <param name="race">Race armor is created for.</param>
        /// <param name="armor">Type of armor item to create.</param>
        /// <param name="armorType"> Base type of armour.</param>
        /// <param name="material">Material of armor.</param>
        /// <param name="variant">Visual variant of armor. If -1, a random variant is chosen.</param>
        /// <returns>DaggerfallUnityItem</returns>
        public static DaggerfallUnityItem CreateArmor(Genders gender, Races race, Armor armor, ArmorMaterialTypes material, int variant = -1)
        {
            // Create item
            int groupIndex = DaggerfallUnity.Instance.ItemHelper.GetGroupIndex(ItemGroups.Armor, (int)armor);
            Debug.Log("Creating armour with group index " + groupIndex);
            DaggerfallUnityItem newItem = new DaggerfallUnityItem(ItemGroups.Armor, groupIndex);

            ApplyArmorSettings(newItem, gender, race, material, variant);

            return newItem;
        }

        /// <summary>
        /// Creates random armor.
        /// </summary>
        /// <param name="luck">Luck stat for material type.</param>
        /// <param name="gender">Gender armor is created for.</param>
        /// <param name="race">Race armor is created for.</param>
        /// <returns>DaggerfallUnityItem</returns>
        public static DaggerfallUnityItem CreateRandomArmor(int luck, Genders gender, Races race, int levelModifier = 1, bool isTownGuard = false)
        {
            // Create a random armor type, including any custom items registered as armor
            ItemHelper itemHelper = DaggerfallUnity.Instance.ItemHelper;
            Array enumArray = itemHelper.GetEnumArray(ItemGroups.Armor);
            // int[] customItemTemplates = itemHelper.GetCustomItemsForGroup(ItemGroups.Armor);

            int groupIndex = GetArmorIndex(enumArray.Length);
            Debug.Log("enumArray.Length: " + enumArray.Length + ", groupIndex: " + groupIndex);
            DaggerfallUnityItem newItem;
            // if (groupIndex < enumArray.Length)
            newItem = new DaggerfallUnityItem(ItemGroups.Armor, groupIndex);
            // else
            //     newItem = CreateItem(ItemGroups.Armor, customItemTemplates[groupIndex - enumArray.Length]);

            ApplyArmorSettings(newItem, gender, race, FormulaHelper.RandomArmorMaterial((Armor)enumArray.GetValue(groupIndex), luck, levelModifier, isTownGuard));

            return newItem;
        }

        // Here we try to keep vanilla armor type proportions (Plate 1/10, Chain 2/10, Leather 7/10)
        // despite the new armor types. Works in tandem with chances in RandomArmorMaterial.
        public static int GetArmorIndex(int arrayLength)
        {
            int diceRoll = UnityEngine.Random.Range(0, 245);
            if (diceRoll < 35)
                return diceRoll % 7;
            if (diceRoll < 70)
                return diceRoll % 5 + 11;
            if (diceRoll < 210)
                return diceRoll % 7 + 16;
            // Shields
            if (diceRoll < 228)
                return 7;
            if (diceRoll < 238)
                return 8;
            if (diceRoll < 243)
                return 9;
            else return 10;
        }

        /// <summary> Set gender, body morphology and material of armor </summary>
        public static void ApplyArmorSettings(DaggerfallUnityItem armor, Genders gender, Races race, ArmorMaterialTypes material, int variant = 0)
        {
            // Keep original archive in case a new type armor will be generated.
            Debug.Log("armor.PlayerTextureArchive: " + armor.PlayerTextureArchive);
            newArmorArchive = armor.PlayerTextureArchive;

            // Adjust for gender
            if (gender == Genders.Female)
                armor.PlayerTextureArchive = firstFemaleArchive;
            else
                armor.PlayerTextureArchive = firstMaleArchive;

            // Adjust for body morphology
            SetRace(armor, race);

            // Adjust material
            ApplyArmorMaterial(armor, material);

            if (IsNewArmor(armor))
            {
                armor.PlayerTextureArchive = newArmorArchive;

                if (gender == Genders.Male)
                    armor.PlayerTextureArchive += 4;

                if (armor.IsOfTemplate(ItemGroups.Armor, (int)Armor.Helmet))
                {
                    if ((gender == Genders.Male && (race == Races.Khajiit || race == Races.DarkElf || race == Races.HighElf || race == Races.WoodElf)) ||
                        gender == Genders.Female && race == Races.Argonian)
                        armor.PlayerTextureArchive--;
                }
                Debug.Log("Second passage - armor.PlayerTextureArchive: " + armor.PlayerTextureArchive);
            }

            // Adjust for variant
            if (variant >= 0)
                SetVariant(armor, variant);
            else
                RandomizeArmorVariant(armor);
        }

        /// <summary>Set material and adjust armor stats accordingly</summary>
        public static void ApplyArmorMaterial(DaggerfallUnityItem armor, ArmorMaterialTypes material)
        {
            armor.nativeMaterialValue = (int)material;

            if (armor.nativeMaterialValue == (int)ArmorMaterialTypes.Leather) // Managing standard leather
            {
                armor.weightInKg /= 2;
            }
            else if (armor.nativeMaterialValue == (int)ArmorMaterialTypes.Fur)
            {
                armor.weightInKg -= 2;
            }
            else if (armor.nativeMaterialValue >= (int)ArmorMaterialTypes.Chain)    // Managing standard chain
            {
                armor.value *= 2;
            }

            if (GetArmorMaterialType((int)material) >= MaterialTypes.Iron)
            {
                Debug.Log("materia: " + material + ", Armor material type: " + GetArmorMaterialType((int)material));
                armor = SetItemPropertiesByMaterial(armor, (int)material);
            }

            Debug.Log("armor.PlayerTextureArchive: " + armor.PlayerTextureArchive);

            armor.dyeColor = DaggerfallUnity.Instance.ItemHelper.GetArmorDyeColor(material);
        }

        public static bool IsNewArmor(DaggerfallUnityItem armor)
        {
            // ArmorMaterialTypes armorType = GetArmorMaterialType(armor.nativeMaterialValue);

            if ((armor.nativeMaterialValue >= (int)ArmorMaterialTypes.LeatherIron && armor.nativeMaterialValue <= (int)ArmorMaterialTypes.LeatherDaedric) ||
                 armor.nativeMaterialValue == (int)ArmorMaterialTypes.Fur ||
                 newArmorArchive >= 10000)
                return true;

            return false;
        }

        /// <summary>
        /// Creates random magic item in same manner as classic.
        /// </summary>
        /// <returns>DaggerfallUnityItem</returns>
        public static DaggerfallUnityItem CreateRandomMagicItem(int playerLuck, Genders gender, Races race, int levelModifier = 1)
        {
            return CreateRegularMagicItem(chooseAtRandom, playerLuck, gender, race, levelModifier);
        }

        /// <summary>
        /// Create a regular non-artifact magic item.
        /// </summary>
        /// <param name="chosenItem">An integer index of the item to create, or -1 for a random one.</param>
        /// <param name="playerLuck">The player luck live stat.</param>
        /// <param name="gender">The gender to create an item for.</param>
        /// <param name="race">The race to create an item for.</param>
        /// <returns>DaggerfallUnityItem</returns>
        /// <exception cref="Exception">When a base item cannot be created.</exception>
        public static DaggerfallUnityItem CreateRegularMagicItem(int chosenItem, int playerLuck, Genders gender, Races race, int levelModifier = 1)
        {
            byte[] itemGroups0 = { 2, 3, 6, 10, 12, 14, 25 };
            byte[] itemGroups1 = { 2, 3, 6, 12, 25 };

            DaggerfallUnityItem newItem = null;

            // Get the list of magic item templates read from MAGIC.DEF
            MagicItemTemplate[] magicItems = DaggerfallUnity.Instance.ItemHelper.MagicItemTemplates;

            // Reduce the list to only the regular magic items.
            MagicItemTemplate[] regularMagicItems = magicItems.Where(template => template.type == MagicItemTypes.RegularMagicItem).ToArray();
            if (chosenItem > regularMagicItems.Length)
                throw new Exception(string.Format("Magic item subclass {0} does not exist", chosenItem));

            // Pick a random one if needed.
            if (chosenItem == chooseAtRandom)
            {
                chosenItem = UnityEngine.Random.Range(0, regularMagicItems.Length);
            }

            // Get the chosen template
            MagicItemTemplate magicItem = regularMagicItems[chosenItem];

            // Get the item group. The possible groups are determined by the 33rd byte (magicItem.group) of the MAGIC.DEF template being used.
            ItemGroups group = 0;
            if (magicItem.group == 0)
                group = (ItemGroups)itemGroups0[UnityEngine.Random.Range(0, 7)];
            else if (magicItem.group == 1)
                group = (ItemGroups)itemGroups1[UnityEngine.Random.Range(0, 5)];
            else if (magicItem.group == 2)
                group = ItemGroups.Weapons;

            // Create the base item
            if (group == ItemGroups.Weapons)
            {
                newItem = CreateRandomWeapon(playerLuck, levelModifier);

                // No arrows as enchanted items
                while (newItem.GroupIndex == 18)
                    newItem = CreateRandomWeapon(playerLuck, levelModifier);
            }
            else if (group == ItemGroups.Armor)
                newItem = CreateRandomArmor(playerLuck, gender, race, levelModifier);
            else if (group == ItemGroups.MensClothing || group == ItemGroups.WomensClothing)
                newItem = CreateRandomClothing(gender, race);
            else if (group == ItemGroups.ReligiousItems)
                newItem = CreateRandomReligiousItem();
            else if (group == ItemGroups.Gems)
                newItem = CreateRandomGem();
            else // Only other possibility is jewellery
                newItem = CreateRandomJewellery();

            if (newItem == null)
                throw new Exception("CreateRegularMagicItem() failed to create an item.");

            // Replace the regular item name with the magic item name
            newItem.shortName = magicItem.name;

            // Add the enchantments
            newItem.legacyMagic = new DaggerfallEnchantment[magicItem.enchantments.Length];
            for (int i = 0; i < magicItem.enchantments.Length; ++i)
                newItem.legacyMagic[i] = magicItem.enchantments[i];

            // Set the condition/magic uses
            newItem.maxCondition = magicItem.uses;
            newItem.currentCondition = magicItem.uses;

            // Set the value of the item. This is determined by the enchantment point cost/spell-casting cost
            // of the enchantments on the item.
            int value = 0;
            for (int i = 0; i < magicItem.enchantments.Length; ++i)
            {
                if (magicItem.enchantments[i].type != EnchantmentTypes.None
                    && magicItem.enchantments[i].type < EnchantmentTypes.ItemDeteriorates)
                {
                    switch (magicItem.enchantments[i].type)
                    {
                        case EnchantmentTypes.CastWhenUsed:
                        case EnchantmentTypes.CastWhenHeld:
                        case EnchantmentTypes.CastWhenStrikes:
                            // Enchantments that cast a spell. The parameter is the spell index in SPELLS.STD.
                            value += Formulas.FormulaHelper.GetSpellEnchantPtCost(magicItem.enchantments[i].param);
                            break;
                        case EnchantmentTypes.RepairsObjects:
                        case EnchantmentTypes.AbsorbsSpells:
                        case EnchantmentTypes.EnhancesSkill:
                        case EnchantmentTypes.FeatherWeight:
                        case EnchantmentTypes.StrengthensArmor:
                            // Enchantments that provide an effect that has no parameters
                            value += enchantmentPointCostsForNonParamTypes[(int)magicItem.enchantments[i].type];
                            break;
                        case EnchantmentTypes.SoulBound:
                            // Bound soul
                            MobileEnemy mobileEnemy = GameObjectHelper.EnemyDict[magicItem.enchantments[i].param];
                            value += mobileEnemy.SoulPts; // TODO: Not sure about this. Should be negative? Needs to be tested.
                            break;
                        default:
                            // Enchantments that provide a non-spell effect with a parameter (parameter = when effect applies, what enemies are affected, etc.)
                            value += enchantmentPtsForItemPowerArrays[(int)magicItem.enchantments[i].type][magicItem.enchantments[i].param];
                            break;
                    }
                }
            }

            newItem.value = value;

            return newItem;
        }

        /// <summary>
        /// Sets properties for a weapon or piece of armor based on its material.
        /// </summary>
        /// <param name="item">Item to have its properties modified.</param>
        /// <param name="material">Material to use to apply properties.</param>
        /// <returns>DaggerfallUnityItem</returns>
        public static DaggerfallUnityItem SetItemPropertiesByMaterial(DaggerfallUnityItem item, int nativeMaterialValue)
        {
            int multiplier = 1;
            MaterialTypes material = MaterialTypes.Iron;
            Debug.Log("nativeMaterialValue: " + nativeMaterialValue);

            if (item.ItemGroup == ItemGroups.Weapons)
            {
                multiplier = 4;
                material = (MaterialTypes)nativeMaterialValue;
            }
            else
            {
                ArmorTypes armor = GetArmorType(nativeMaterialValue);
                material = GetArmorMaterialType(nativeMaterialValue);
                switch (armor)
                {
                    case ArmorTypes.Leather:
                        multiplier = 2;
                        break;
                    case ArmorTypes.Chain:
                        multiplier = 6;
                        break;
                    case ArmorTypes.Plate:
                        multiplier = 10;
                        break;
                    default:
                        break;
                }
            }

            Debug.Log("(int)GetArmorMaterialType(item.nativeMaterialValue): " + (int)GetArmorMaterialType((int)material));
            item.value *= multiplier * valueMultipliersByMaterial[(int)GetArmorMaterialType((int)material)];
            item.weightInKg += (CalculateWeightForMaterial(item, GetArmorMaterialType((int)material)) - item.weightInKg) * multiplier / 6;
            item.maxCondition = item.maxCondition * conditionMultipliersByMaterial[(int)GetArmorMaterialType((int)material)] / 4;
            item.currentCondition = item.maxCondition;

            return item;
        }

        static float CalculateWeightForMaterial(DaggerfallUnityItem item, MaterialTypes material)
        {
            int quarterKgs = (int)(item.weightInKg * 4);
            Debug.Log("(int)material: " + (int)material);
            float matQuarterKgs = (float)(quarterKgs * weightMultipliersByMaterial[(int)material]) / 4;
            return Mathf.Round(matQuarterKgs) / 4;
        }

        /// <summary>
        /// Creates a random ingredient from any of the ingredient groups.
        /// Passing a non-ingredient group will return null.
        /// </summary>
        /// <param name="ingredientGroup">Ingredient group.</param>
        /// <returns>DaggerfallUnityItem</returns>
        public static DaggerfallUnityItem CreateRandomIngredient(ItemGroups ingredientGroup)
        {
            int groupIndex;
            Array enumArray;
            switch (ingredientGroup)
            {
                case ItemGroups.CreatureIngredients1:
                case ItemGroups.CreatureIngredients2:
                case ItemGroups.CreatureIngredients3:
                case ItemGroups.MetalIngredients:
                case ItemGroups.MiscellaneousIngredients1:
                case ItemGroups.MiscellaneousIngredients2:
                case ItemGroups.PlantIngredients1:
                case ItemGroups.PlantIngredients2:
                    enumArray = DaggerfallUnity.Instance.ItemHelper.GetEnumArray(ingredientGroup);
                    groupIndex = UnityEngine.Random.Range(0, enumArray.Length);
                    break;
                default:
                    return null;
            }

            // Create item
            DaggerfallUnityItem newItem = new DaggerfallUnityItem(ingredientGroup, groupIndex);

            return newItem;
        }

        /// <summary>
        /// Creates a random ingredient from a random ingredient group.
        /// </summary>
        /// <returns>DaggerfallUnityItem</returns>
        public static DaggerfallUnityItem CreateRandomIngredient()
        {
            // Randomise ingredient group
            ItemGroups itemGroup;
            int group = UnityEngine.Random.Range(0, 8);
            Array enumArray;
            switch (group)
            {
                case 0:
                    itemGroup = ItemGroups.CreatureIngredients1;
                    break;
                case 1:
                    itemGroup = ItemGroups.CreatureIngredients2;
                    break;
                case 2:
                    itemGroup = ItemGroups.CreatureIngredients3;
                    break;
                case 3:
                    itemGroup = ItemGroups.MetalIngredients;
                    break;
                case 4:
                    itemGroup = ItemGroups.MiscellaneousIngredients1;
                    break;
                case 5:
                    itemGroup = ItemGroups.MiscellaneousIngredients2;
                    break;
                case 6:
                    itemGroup = ItemGroups.PlantIngredients1;
                    break;
                case 7:
                    itemGroup = ItemGroups.PlantIngredients2;
                    break;
                default:
                    return null;
            }

            // Randomise ingredient within group
            enumArray = DaggerfallUnity.Instance.ItemHelper.GetEnumArray(itemGroup);
            int groupIndex = UnityEngine.Random.Range(0, enumArray.Length);

            // Create item
            DaggerfallUnityItem newItem = new DaggerfallUnityItem(itemGroup, groupIndex);

            return newItem;
        }

        /// <summary>
        /// Creates a potion.
        /// </summary>
        /// <param name="recipe">Recipe index for the potion</param>
        /// <returns>Potion DaggerfallUnityItem</returns>
        public static DaggerfallUnityItem CreatePotion(int recipeKey, int stackSize = 1)
        {
            return new DaggerfallUnityItem(ItemGroups.UselessItems1, 1) { PotionRecipeKey = recipeKey, stackCount = stackSize };
        }

        /// <summary>
        /// Creates a random potion from all registered recipes.
        /// </summary>
        /// <returns>Potion DaggerfallUnityItem</returns>
        public static DaggerfallUnityItem CreateRandomPotion(int stackSize = 1)
        {
            List<int> recipeKeys = GameManager.Instance.EntityEffectBroker.GetPotionRecipeKeys();
            int recipeIdx = UnityEngine.Random.Range(0, recipeKeys.Count);
            return CreatePotion(recipeKeys[recipeIdx]);
        }

        /// <summary>
        /// Creates a random (classic) potion
        /// </summary>
        /// <returns>Potion DaggerfallUnityItem</returns>
        public static DaggerfallUnityItem CreateRandomClassicPotion()
        {
            int recipeIdx = UnityEngine.Random.Range(0, MagicAndEffects.PotionRecipe.classicRecipeKeys.Length);
            return CreatePotion(MagicAndEffects.PotionRecipe.classicRecipeKeys[recipeIdx]);
        }

        /// <summary>
        /// Generates gold pieces.
        /// </summary>
        /// <param name="amount">Total number of gold pieces in stack.</param>
        /// <returns></returns>
        public static DaggerfallUnityItem CreateGoldPieces(int amount)
        {
            DaggerfallUnityItem newItem = CreateItem(ItemGroups.Currency, (int)Currency.Gold_pieces);
            newItem.stackCount = amount;
            newItem.value = 1;

            return newItem;
        }

        /// <summary>
        /// Sets a random variant of clothing item.
        /// </summary>
        /// <param name="item">Item to randomize variant.</param>
        public static void RandomizeClothingVariant(DaggerfallUnityItem item)
        {
            int totalVariants = item.ItemTemplate.variants;
            SetVariant(item, UnityEngine.Random.Range(0, totalVariants));
        }

        /// <summary>
        /// Sets a random variant of armor item.
        /// </summary>
        /// <param name="item">Item to randomize variant.</param>
        public static void RandomizeArmorVariant(DaggerfallUnityItem item)
        {
            int variant = 0;

            // We only need to pick randomly where there is more than one possible variant. Otherwise we can just pass in 0 to SetVariant and
            // the correct variant will still be chosen.
            if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Cuirass))
            {
                variant = UnityEngine.Random.Range(1, 4);
            }
            // else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Jerkin))
            // {
            //     if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Fur)
            //         variant = 11;
            //     else
            //         variant = (int)GetArmorMaterialType(item.nativeMaterialValue);
            // }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Greaves))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    variant = UnityEngine.Random.Range(0, 2);
                else if (item.nativeMaterialValue >= (int)ArmorMaterialTypes.PlateIron)
                    variant = UnityEngine.Random.Range(2, 6);
            }
            // else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Cuisse))
            // {
            //     if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Fur)
            //         variant = 11;
            //     else
            //         variant = (int)GetArmorMaterialType(item.nativeMaterialValue);
            // }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Left_Pauldron) || item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Right_Pauldron))
            {
                if (item.nativeMaterialValue >= (int)ArmorMaterialTypes.PlateIron)
                    variant = UnityEngine.Random.Range(1, 4);
            }
            // else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Left_Vambrace) || item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Right_Vambrace))
            // {
            //     if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Fur)
            //         variant = 11;
            //     else
            //         variant = (int)GetArmorMaterialType(item.nativeMaterialValue);
            // }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Boots) && (item.nativeMaterialValue >= (int)ArmorMaterialTypes.PlateIron))
            {
                variant = UnityEngine.Random.Range(1, 3);
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Helm))
            {
                variant = UnityEngine.Random.Range(0, item.ItemTemplate.variants);
            }
            // else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Light_Boots))
            // {
            //     variant = UnityEngine.Random.Range(0, 2);
            // }
            // else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Gloves))
            // {
            //     variant = UnityEngine.Random.Range(0, 2);
            // }
            SetVariant(item, variant);
        }

        /// <summary>
        /// If no location is passed, a map from a random settlement location is generated.
        /// Take into account that, although maps closer to the player current position are
        /// going to be generated much more frequently, the randomisation can get some pretty
        /// crazy results.
        /// </summary>
        /// <param name="location">Location to generate a town map of.</param>
        /// <param name="exoticism">Value to roll to get a map from "far away".</param>
        /// <param name="anyRegion">If a random map can be created of locations in other regions in this province.</param>
        /// <param name="anyProvince">If a random map can be created of locations in other provinces in this continent.</param>
        /// <param name="anyContinent">If a random map can be created of locations in other continents (on this planet).</param>
        public static DaggerfallUnityItem CreateTownMap(int exoticism = 100, bool anyRegion = false, bool anyProvince = false, bool anyContinent = false, DFLocation location = new DFLocation())
        {
            DaggerfallUnityItem townMap = new DaggerfallUnityItem(ItemGroups.Maps, 1);
            int diceRoll = Dice100.Roll();

            if (location.Exterior.ExteriorData.BlockNames == null && diceRoll < exoticism)
            {
                PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
                ProvinceNames pickedProvince = playerGPS.GetProvinceFromRegion();
                int pickedRegion = playerGPS.CurrentPoliticIndex;
                (int, int) currentTile = playerGPS.CurrentTile;
                List<(int, int)> locIdxs = new List<(int, int)>();

                if (anyRegion)
                {
                    diceRoll = Dice100.Roll();
                    if (diceRoll >= exoticism && anyProvince)
                    {
                        diceRoll = Dice100.Roll();
                        if (diceRoll >= exoticism && anyContinent)
                        {
                            // TODO: expand this
                        }
                        else
                        {
                            pickedProvince = (ProvinceNames)UnityEngine.Random.Range(1, Enum.GetNames(typeof(ProvinceNames)).Length + 1);
                            townMap.value *= 10;
                        }
                    }
                    pickedRegion = WorldData.WorldSetting.regionInProvince[(int)pickedProvince][UnityEngine.Random.Range(0, WorldData.WorldSetting.regionInProvince[(int)pickedProvince].Length)];
                    townMap.value *= 5;
                }

                if (diceRoll > 50) // town map from the same region OR exotic map
                {
                    Debug.Log("Trying to create town map from " + WorldData.WorldSetting.RegionNames[pickedRegion]);
                    for (int t = 0; t < WorldMaps.regionTiles[pickedRegion].Count; t++)
                    {
                        DFRegion tile = WorldMaps.ConvertWorldMapsToDFRegion(WorldMaps.regionTiles[pickedRegion][t], true);
                        for (int r = 0; r < tile.LocationCount; r++)
                        {
                            if (tile.MapTable[r].LocationType == DFRegion.LocationTypes.TownCity ||
                                tile.MapTable[r].LocationType == DFRegion.LocationTypes.TownHamlet ||
                                tile.MapTable[r].LocationType == DFRegion.LocationTypes.TownVillage)
                                locIdxs.Add((MapsFile.MapPixelToTile(MapsFile.GetPixelFromPixelID(tile.MapTable[r].MapId)), r));
                        }
                    }
                }
                else
                {   // town map from the same geographical area (9x9 tiles)
                    Debug.Log("Trying to create local town map.");
                    for (int gX = 0; gX < 3; gX++)
                    {
                        for (int gY = 0; gY < 3; gY++)
                        {
                            DFRegion localTile = WorldMaps.ConvertWorldMapsToDFRegion(gY * 3 + gX);
                            for (int gr = 0; gr < localTile.LocationCount; gr++)
                            {
                                if (localTile.MapTable[gr].LocationType == DFRegion.LocationTypes.TownCity ||
                                    localTile.MapTable[gr].LocationType == DFRegion.LocationTypes.TownHamlet ||
                                    localTile.MapTable[gr].LocationType == DFRegion.LocationTypes.TownVillage)
                                    locIdxs.Add((MapsFile.MapPixelToTile(MapsFile.GetPixelFromPixelID(localTile.MapTable[gr].MapId)), gr));
                            }
                        }
                    }
                }

                int locIndex = UnityEngine.Random.Range(0, locIdxs.Count);
                MapSummary locSummary;
                if (locIdxs.Count < 1)
                    return null;
                DFRegion finalTile = WorldMaps.ConvertWorldMapsToDFRegion(locIdxs[locIndex].Item1, true);
                DFPosition pos = MapsFile.GetPixelFromPixelID(finalTile.MapTable[locIdxs[locIndex].Item2].MapId);
                townMap.message = locIdxs[locIndex].Item1 * 10000 + locIdxs[locIndex].Item2;
                townMap.shortName = townMap.shortName + " " + finalTile.MapNames[locIdxs[locIndex].Item2] + " (" + WorldData.WorldSetting.RegionNames[PoliticData.GetAbsPoliticValue(pos.X, pos.Y)] + ")";
            }
            else
            {
                townMap.message = location.AbsTileIndex * 10000 + location.LocationIndex;
                townMap.shortName = townMap.shortName + " " + location.Name + " (" + location.RegionName + ")";
            }
            Debug.Log("townMap.ItemName: " + townMap.ItemName);

            return townMap;
        }

        public static DaggerfallUnityItem CreateLocationMap(int exoticism = 100, bool anyRegion = false, bool anyProvince = false, bool anyContinent = false, DFLocation location = new DFLocation(), DFRegion.LocationTypes locType = DFRegion.LocationTypes.None)
        {
            DaggerfallUnityItem locationMap = new DaggerfallUnityItem(ItemGroups.Maps, 0);
            int variant = 0;

            if (location.Exterior.ExteriorData.BlockNames == null)
            {
                int diceRoll;
                PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
                ProvinceNames pickedProvince = playerGPS.GetProvinceFromRegion();
                int pickedRegion = playerGPS.CurrentPoliticIndex;
                (int, int) currentTile = playerGPS.CurrentTile;
                List<(int, int)> locIdxs = new List<(int, int)>();

                if (locType == DFRegion.LocationTypes.None)
                {
                    diceRoll = Dice100.Roll();

                    if (diceRoll <= 30)
                        locType = DFRegion.LocationTypes.TownCity;
                    else if (diceRoll <= 40)
                        locType = DFRegion.LocationTypes.TownHamlet;
                    else if (diceRoll <= 45)
                        locType = DFRegion.LocationTypes.DungeonLabyrinth;
                    else if (diceRoll <= 55)
                        locType = DFRegion.LocationTypes.DungeonKeep;
                    else if (diceRoll <= 70)
                        locType = DFRegion.LocationTypes.DungeonRuin;
                    else if (diceRoll <= 80)
                        locType = DFRegion.LocationTypes.ReligionTemple;
                    else if (diceRoll <= 98)
                        locType = DFRegion.LocationTypes.ReligionCult;
                    else if (diceRoll <= 100)
                        locType = DFRegion.LocationTypes.Coven;
                }

                switch (locType)
                {
                    case DFRegion.LocationTypes.DungeonRuin:
                    case DFRegion.LocationTypes.DungeonKeep:
                    case DFRegion.LocationTypes.DungeonLabyrinth:
                        locationMap.value *= (10 - (int)locType);
                        variant = 3;
                        break;
                    case DFRegion.LocationTypes.ReligionTemple:
                    case DFRegion.LocationTypes.ReligionCult:
                        locationMap.value /= 2;
                        variant = 1;
                        break;
                    case DFRegion.LocationTypes.Coven:
                        locationMap.value *= 100000;
                        variant = 2;
                        break;
                    case DFRegion.LocationTypes.TownCity:
                    case DFRegion.LocationTypes.TownHamlet:
                    default:
                        break;
                }

                diceRoll = Dice100.Roll();

                if (diceRoll >= exoticism && anyRegion)
                {
                    diceRoll = Dice100.Roll();
                    if (diceRoll >= exoticism && anyProvince)
                    {
                        diceRoll = Dice100.Roll();
                        if (diceRoll >= exoticism && anyContinent)
                        {
                            // TODO: expand this
                        }
                        else
                        {
                            pickedProvince = (ProvinceNames)UnityEngine.Random.Range(1, Enum.GetNames(typeof(ProvinceNames)).Length + 1);
                            locationMap.value *= 10;
                        }
                    }
                    pickedRegion = WorldData.WorldSetting.regionInProvince[(int)pickedProvince][UnityEngine.Random.Range(0, WorldData.WorldSetting.regionInProvince[(int)pickedProvince].Length)];
                    locationMap.value *= 5;
                }

                if (diceRoll > 50) // map from the same region OR exotic map
                {
                    for (int t = 0; t < WorldMaps.regionTiles[pickedRegion].Count; t++)
                    {
                        DFRegion tile = WorldMaps.ConvertWorldMapsToDFRegion(WorldMaps.regionTiles[pickedRegion][t], true);
                        for (int r = 0; r < tile.LocationCount; r++)
                        {
                            if (tile.MapTable[r].LocationType == locType)
                                locIdxs.Add((MapsFile.MapPixelToTile(MapsFile.GetPixelFromPixelID(tile.MapTable[r].MapId)), r));
                        }
                    }
                }
                else
                {   // map from the same geographical area (9x9 tiles)
                    for (int gX = 0; gX < 3; gX++)
                    {
                        for (int gY = 0; gY < 3; gY++)
                        {
                            DFRegion localTile = WorldMaps.ConvertWorldMapsToDFRegion(gY * 3 + gX);
                            for (int gr = 0; gr < localTile.LocationCount; gr++)
                            {
                                if (localTile.MapTable[gr].LocationType == locType)
                                    locIdxs.Add((MapsFile.MapPixelToTile(MapsFile.GetPixelFromPixelID(localTile.MapTable[gr].MapId)), gr));
                            }
                        }
                    }
                }

                if (locIdxs.Count == 0)
                {
                    Debug.Log("No suitable location found.");
                    return null;
                }

                int locIndex = UnityEngine.Random.Range(0, locIdxs.Count);
                MapSummary locSummary;
                DFRegion finalTile = WorldMaps.ConvertWorldMapsToDFRegion(locIdxs[locIndex].Item1, true);
                DFPosition pos = MapsFile.GetPixelFromPixelID(finalTile.MapTable[locIdxs[locIndex].Item2].MapId);
                locationMap.message = locIdxs[locIndex].Item1 * 10000 + locIdxs[locIndex].Item2;
                locationMap.shortName = locationMap.shortName + " " + finalTile.MapNames[locIdxs[locIndex].Item2] + " (" + WorldData.WorldSetting.RegionNames[PoliticData.GetAbsPoliticValue(pos.X, pos.Y)] + ")";
            }
            else
            {
                locationMap.message = location.AbsTileIndex * 10000 + location.LocationIndex;
                locationMap.shortName = locationMap.shortName + " " + location.Name + " (" + location.RegionName + ")";
            }
            SetVariant(locationMap, variant);
            return locationMap;
        }

        #endregion

        #region Static Utility Methods

        public static void SetRace(DaggerfallUnityItem item, Races race)
        {
            if (item.PlayerTextureArchive > 9999) return;
            int offset = (int)GetBodyMorphology(race);
            item.PlayerTextureArchive += offset;
        }

        public static void SetClothQuality(DaggerfallUnityItem item, int quality, out ClothCraftsmanship clothCraftsmanship)
        {
            int[] tier = new int[4];
            clothCraftsmanship = ClothCraftsmanship.Cheap;
            for (int i = 0; i < 4; i++)
            {
                tier[i] = UnityEngine.Random.Range(0, 100) + quality;
                if (tier[i] >= 80)
                    clothCraftsmanship++;
            }

            // Calling "Tall Sandals" by their name
            if ((item.TemplateIndex == (int)WomensClothing.Sandals ||
                 item.TemplateIndex == (int)MensClothing.Sandals) &&
                 item.CurrentVariant == 0)
                item.shortName = "Tall " + item.shortName;

            item.craftsmanship = clothCraftsmanship;
            switch (clothCraftsmanship)
            {
                case ClothCraftsmanship.Cheap:
                    item.shortName = "Cheap " + item.shortName;
                    break;
                case ClothCraftsmanship.Normal:
                    item.value *= 2;
                    item.maxCondition *= 3;
                    item.enchantmentPoints *= 2;
                    break;
                case ClothCraftsmanship.Fancy:
                    item.shortName = "Fancy " + item.shortName;
                    item.value *= 5;
                    item.maxCondition *= 2;
                    item.enchantmentPoints *= 5;
                    break;
                case ClothCraftsmanship.Extravagant:
                    item.shortName = "Extravagant " + item.shortName;
                    item.value *= 10;
                    item.maxCondition *= 2;
                    item.enchantmentPoints *= 7;
                    break;
                case ClothCraftsmanship.Exquisite:
                    item.shortName = "Exquisite " + item.shortName;
                    item.value *= 20;
                    item.maxCondition *= 3;
                    item.enchantmentPoints *= 10;
                    break;
            }
        }

        public static void SetClothDyeData(DaggerfallUnityItem item)
        {
            if (ItemListsNStuff.DyeTargetElements[(int)ItemListsNStuff.DyeTargetCombinations.Variable].Contains(item.TemplateIndex))
                SetVariableDyeData(item, ref item.dyeTargets, ref item.dyeLevel);
            else
            {
                bool found = false;
                int counter = 0;
                do
                {
                    if (counter == (int)ItemListsNStuff.DyeTargetCombinations.Variable)
                    {
                        Debug.Log("SetClothDyeData: couldn't find dyeTargets for item index " + item.TemplateIndex);
                        return;
                    }
                    if (ItemListsNStuff.DyeTargetElements[counter].Contains(item.TemplateIndex))
                    {
                        item.dyeTargets = ItemListsNStuff.DyeCombArrays[counter];
                        item.dyeLevel = item.dyeTargets.Length;
                        found = true;
                    }
                    counter++;
                }
                while (!found);
            }
        }

        public static void SetVariableDyeData(DaggerfallUnityItem cloth, ref DyeTargets[] dyeTargets, ref int dyeLevel)
        {
            int templateVariant = cloth.TemplateIndex * 10 + cloth.CurrentVariant;
            switch (templateVariant)
            {
                case 1580:
                case 1650:
                case 1651:
                case 1654:
                case 1670:
                case 1671:
                case 1674:
                case 1690:
                case 1691:
                case 1710:
                case 1711:
                case 1904:
                case 1950:
                case 2020:
                case 2021:
                case 2024:
                case 2040:
                case 2041:
                case 2044:
                case 2060:
                case 2061:
                case 2080:
                case 2081:
                case 2130:
                case 3200:
                case 3460:
                case 3470:
                case 3540:
                case 3640:
                case 3650:
                case 3660:
                case 3840:
                case 3990:
                case 4000:
                case 4480:
                case 4490:
                case 4560:
                case 4660:
                case 4670:
                case 4680:
                case 4860:
                case 5010:
                case 5020:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic];
                    break;
                case 1831:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Steel];
                    break;
                case 1941:
                case 1942:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Yellow];
                    break;
                case 1520:
                case 1900:
                case 1901:
                case 1902:
                case 2131:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Leather];
                    break;
                case 1834:
                case 2161:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Red];
                    break;
                case 2160:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Green];
                    break;
                case 1521:
                case 1903:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.LightBrown];
                    break;
                case 4010:
                case 4020:
                case 5030:
                case 5040:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Steel];
                    break;
                case 1830:
                case 1833:
                case 3611:
                case 3850:
                case 3950:
                case 4631:
                case 4870:
                case 4970:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Yellow];
                    break;
                case 1940:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Yellow_Basic];
                    break;
                case 3160:
                case 3161:
                case 3170:
                case 3171:
                case 3180:
                case 3181:
                case 3190:
                case 3191:
                case 3210:
                case 3211:
                case 3220:
                case 3221:
                case 3230:
                case 3231:
                case 3240:
                case 3241:
                case 3250:
                case 3260:
                case 4270:
                case 1335:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Red];
                    break;
                case 3280:
                case 3290:
                case 3360:
                case 3380:
                case 3500:
                case 3503:
                case 3680:
                case 1355:
                case 1365:
                case 4380:
                case 4400:
                case 4520:
                case 4523:
                case 4700:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Black];
                    break;
                case 1581:
                case 1652:
                case 1653:
                case 1672:
                case 1673:
                case 1692:
                case 1693:
                case 1712:
                case 1713:
                case 2022:
                case 2023:
                case 2042:
                case 2043:
                case 2062:
                case 2063:
                case 2082:
                case 2083:
                case 3201:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Green];
                    break;
                case 1410:
                case 1411:
                case 1412:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Leather_Yellow];
                    break;
                case 1951:
                case 3251:
                case 3261:
                case 3392:
                case 3461:
                case 3471:
                case 3542:
                case 3641:
                case 3652:
                case 3662:
                case 3842:
                case 3880:
                case 3882:
                case 3884:
                case 3992:
                case 4002:
                case 4271:
                case 1336:
                case 4412:
                case 4481:
                case 4491:
                case 4562:
                case 4661:
                case 4672:
                case 4682:
                case 4862:
                case 4900:
                case 4902:
                case 4904:
                case 5012:
                case 5022:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_LightBrown];
                    break;
                case 1413:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Black_Yellow];
                    break;
                case 3860:
                case 4880:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_DarkBrown];
                    break;
                case 3890:
                case 3900:
                case 4910:
                case 4920:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_White];
                    break;
                case 1832:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.LightBrown_White];
                    break;
                case 3271:
                case 3273:
                case 1346:
                case 1348:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Red_LightBrown];
                    break;
                case 3281:
                case 3291:
                case 3362:
                case 3372:
                case 3382:
                case 3501:
                case 3502:
                case 3511:
                case 3671:
                case 3681:
                case 1356:
                case 1366:
                case 4382:
                case 4392:
                case 4402:
                case 4521:
                case 4522:
                case 4531:
                case 4691:
                case 4701:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Black_LightBrown];
                    break;
                case 1960:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Purple_Black];
                    break;
                case 3862:
                case 3864:
                case 4882:
                case 4884:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_DarkBrown_LightBrown];
                    break;
                case 3891:
                case 3901:
                case 3911:
                case 4911:
                case 4921:
                case 4931:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_White_LightBrown];
                    break;
                case 1961:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Red_Purple_Black];
                    break;
                case 3400:
                case 3410:
                case 3420:
                case 3430:
                case 3440:
                case 3450:
                case 3610:
                case 4420:
                case 4430:
                case 4440:
                case 4450:
                case 4460:
                case 4470:
                case 4630:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Steel_Yellow];
                    break;
                case 3853:
                case 3856:
                case 4873:
                case 4876:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Red_LightBrown];
                    break;
                case 3870:
                case 4890:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Yellow_Black];
                    break;
                case 3550:
                case 3551:
                case 3560:
                case 3570:
                case 3580:
                case 3581:
                case 3590:
                case 3600:
                case 3980:
                case 4570:
                case 4571:
                case 4580:
                case 4590:
                case 4600:
                case 4601:
                case 4610:
                case 4620:
                case 5000:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Black_Yellow];
                    break;
                case 4012:
                case 4022:
                case 5032:
                case 5042:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Steel_LightBrown];
                    break;
                case 3851:
                case 3852:
                case 3854:
                case 3855:
                case 4871:
                case 4872:
                case 4874:
                case 4875:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Yellow_LightBrown];
                    break;
                case 1660:
                case 1661:
                case 1680:
                case 1681:
                case 1700:
                case 1701:
                case 1720:
                case 1721:
                case 2030:
                case 2031:
                case 2050:
                case 2051:
                case 2070:
                case 2071:
                case 2090:
                case 2091:
                case 3390:
                case 3391:
                case 3541:
                case 3651:
                case 3661:
                case 3841:
                case 3881:
                case 3883:
                case 3951:
                case 3991:
                case 4001:
                case 4410:
                case 4411:
                case 4561:
                case 4671:
                case 4681:
                case 4861:
                case 4901:
                case 4903:
                case 4971:
                case 5011:
                case 5021:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_LightBrown_Yellow];
                    break;
                case 3162:
                case 3163:
                case 3172:
                case 3173:
                case 3182:
                case 3183:
                case 3192:
                case 3193:
                case 3212:
                case 3213:
                case 3222:
                case 3223:
                case 3232:
                case 3233:
                case 3242:
                case 3243:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Green_Red];
                    break;
                case 3401:
                case 3402:
                case 3411:
                case 3412:
                case 3421:
                case 3422:
                case 3431:
                case 3432:
                case 3441:
                case 3442:
                case 3451:
                case 3452:
                case 4421:
                case 4422:
                case 4431:
                case 4432:
                case 4441:
                case 4442:
                case 4451:
                case 4452:
                case 4461:
                case 4462:
                case 4471:
                case 4472:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Steel_Yellow_LightBrown];
                    break;
                case 4011:
                case 4021:
                case 5031:
                case 5041:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Steel_LightBrown_Yellow];
                    break;
                case 3270:
                case 3272:
                case 1345:
                case 1347:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Red_LightBrown_Yellow];
                    break;
                case 3871:
                case 4891:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Yellow_Black_LightBrown];
                    break;
                case 3552:
                case 3553:
                case 3561:
                case 3562:
                case 3571:
                case 3572:
                case 3582:
                case 3583:
                case 3591:
                case 3592:
                case 3601:
                case 3602:
                case 3981:
                case 3982:
                case 4572:
                case 4573:
                case 4581:
                case 4582:
                case 4591:
                case 4592:
                case 4602:
                case 4603:
                case 4611:
                case 4612:
                case 4621:
                case 4622:
                case 5001:
                case 5002:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Black_Yellow_LightBrown];
                    break;
                case 3361:
                case 3370:
                case 3371:
                case 3381:
                case 3510:
                case 3670:
                case 4381:
                case 4390:
                case 4391:
                case 4401:
                case 4530:
                case 4690:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Black_LightBrown_Yellow];
                    break;
                case 1662:
                case 1663:
                case 1682:
                case 1683:
                case 1702:
                case 1703:
                case 1722:
                case 1723:
                case 2032:
                case 2033:
                case 2052:
                case 2053:
                case 2072:
                case 2073:
                case 2092:
                case 2093:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_Green_LightBrown_Yellow];
                    break;
                case 3861:
                case 3863:
                case 4881:
                case 4883:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_DarkBrown_LightBrown_Yellow];
                    break;
                case 3910:
                case 4930:
                    dyeTargets = ItemListsNStuff.DyeCombArrays[(int)ItemListsNStuff.DyeTargetCombinations.Basic_White_LightBrown_Yellow];
                    break;
            }
            dyeLevel = dyeTargets.Length;
        }

        public static void SetClothColors(DaggerfallUnityItem item, ClothCraftsmanship clothCraftsmanship)
        {
            // Primary colour
            if (item.TemplateIndex == (int)MensClothing.Sash ||
                item.TemplateIndex == (int)MensClothing.Eodoric ||
                item.TemplateIndex == (int)MensClothing.Casual_pants ||
                item.TemplateIndex == (int)MensClothing.Breeches ||
                item.TemplateIndex == (int)MensClothing.Short_skirt ||
                item.TemplateIndex == (int)MensClothing.Casual_cloak ||
                item.TemplateIndex == (int)MensClothing.Khajiit_suit ||
                item.TemplateIndex == (int)MensClothing.Loincloth ||
                item.TemplateIndex == (int)MensClothing.Plain_robes ||
                item.TemplateIndex == (int)MensClothing.Priest_robes ||
                item.TemplateIndex == (int)MensClothing.Open_Tunic ||
                item.TemplateIndex == (int)WomensClothing.Brassier ||
                item.TemplateIndex == (int)WomensClothing.Peasant_blouse ||
                item.TemplateIndex == (int)WomensClothing.Eodoric ||
                item.TemplateIndex == (int)WomensClothing.Casual_pants ||
                item.TemplateIndex == (int)WomensClothing.Casual_cloak ||
                item.TemplateIndex == (int)WomensClothing.Khajiit_suit ||
                item.TemplateIndex == (int)WomensClothing.Loincloth ||
                item.TemplateIndex == (int)WomensClothing.Plain_robes ||
                item.TemplateIndex == (int)WomensClothing.Priestess_robes ||
                item.TemplateIndex == (int)WomensClothing.Open_tunic ||
                item.TemplateIndex == (int)WomensClothing.Tights ||
                item.TemplateIndex == (int)WomensClothing.Vest ||
                item.TemplateIndex == (int)WomensClothing.Copper_circlet ||
                item.TemplateIndex == (int)MensClothing.Copper_circlet ||
                item.TemplateIndex == (int)WomensClothing.Silver_circlet ||
                item.TemplateIndex == (int)MensClothing.Silver_circlet ||
                item.TemplateIndex == (int)WomensClothing.Gold_circlet ||
                item.TemplateIndex == (int)MensClothing.Gold_circlet ||
                item.TemplateIndex == (int)WomensClothing.Ebony_circlet ||
                item.TemplateIndex == (int)MensClothing.Ebony_circlet ||
                item.TemplateIndex == (int)WomensClothing.Robes3 ||
                item.TemplateIndex == (int)MensClothing.Robes3 ||
                item.TemplateIndex == (int)WomensClothing.Hat ||
                item.TemplateIndex == (int)MensClothing.Hat ||
                item.TemplateIndex == (int)WomensClothing.Long_gloves ||
                item.TemplateIndex == (int)MensClothing.Long_gloves ||
                item.TemplateIndex == (int)WomensClothing.Gloves ||
                item.TemplateIndex == (int)MensClothing.Gloves ||
                item.TemplateIndex == (int)WomensClothing.Short_gloves ||
                item.TemplateIndex == (int)MensClothing.Short_gloves ||
                item.TemplateIndex == (int)WomensClothing.Fingerless_gloves ||
                item.TemplateIndex == (int)MensClothing.Fingerless_gloves ||
              ((item.TemplateIndex == (int)MensClothing.Short_tunic ||
                item.TemplateIndex == (int)WomensClothing.Formal_eodoric ||
                item.TemplateIndex == (int)WomensClothing.Evening_gown ||
                item.TemplateIndex == (int)MensClothing.Short_tunic_fit ||
                item.TemplateIndex == (int)WomensClothing.Puff_sleeved_dress0 ||
                item.TemplateIndex == (int)MensClothing.Puff_sleeved_dress0 ||
                item.TemplateIndex == (int)WomensClothing.Sleeveless_dress ||
                item.TemplateIndex == (int)MensClothing.Sleeveless_dress ||
                item.TemplateIndex == (int)WomensClothing.Ribbed_velvet_blouse ||
                item.TemplateIndex == (int)MensClothing.Ribbed_velvet_blouse ||
                item.TemplateIndex == (int)WomensClothing.Tights0 ||
                item.TemplateIndex == (int)MensClothing.Tights0 ||
                item.TemplateIndex == (int)WomensClothing.Long_skirt0 ||
                item.TemplateIndex == (int)MensClothing.Long_skirt0 ||
                item.TemplateIndex == (int)WomensClothing.Revealing_long_skirt ||
                item.TemplateIndex == (int)MensClothing.Revealing_long_skirt ||
                item.TemplateIndex == (int)WomensClothing.Quilted_top ||
                item.TemplateIndex == (int)MensClothing.Quilted_top ||
                item.TemplateIndex == (int)WomensClothing.Long_puff_pants ||
                item.TemplateIndex == (int)MensClothing.Long_puff_pants ||
                item.TemplateIndex == (int)WomensClothing.Short_puff_pants ||
                item.TemplateIndex == (int)MensClothing.Short_puff_pants) &&
                item.CurrentVariant == 0) ||
              ((item.TemplateIndex == (int)MensClothing.Short_shirt ||
                item.TemplateIndex == (int)MensClothing.Long_shirt ||
                item.TemplateIndex == (int)WomensClothing.Short_shirt ||
                item.TemplateIndex == (int)WomensClothing.Long_shirt) &&
               (item.CurrentVariant == 0 || item.CurrentVariant == 1 || item.CurrentVariant == 4)) ||
              ((item.TemplateIndex == (int)MensClothing.Short_shirt_closed_top ||
                item.TemplateIndex == (int)MensClothing.Long_shirt_closed_top ||
                item.TemplateIndex == (int)WomensClothing.Short_shirt_closed ||
                item.TemplateIndex == (int)WomensClothing.Long_shirt_closed) &&
               (item.CurrentVariant == 0 || item.CurrentVariant == 1)) ||
              ((item.TemplateIndex == (int)WomensClothing.Formal_brassier) &&
               (item.CurrentVariant == 4)))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        break;
                }
            }
            // Primary colour (footwear)
            else if (item.TemplateIndex == (int)WomensClothing.Shoes ||
                     item.TemplateIndex == (int)WomensClothing.Tall_boots ||
                     item.TemplateIndex == (int)WomensClothing.Boots ||
                     item.TemplateIndex == (int)WomensClothing.Sandals ||
                     item.TemplateIndex == (int)MensClothing.Shoes ||
                     item.TemplateIndex == (int)MensClothing.Tall_Boots ||
                     item.TemplateIndex == (int)MensClothing.Boots ||
                     item.TemplateIndex == (int)MensClothing.Sandals ||
                     item.TemplateIndex == (int)WomensClothing.Shoes0 ||
                     item.TemplateIndex == (int)MensClothing.Shoes0 ||
                     item.TemplateIndex == (int)WomensClothing.Shoes1 ||
                     item.TemplateIndex == (int)MensClothing.Shoes1 ||
                     item.TemplateIndex == (int)WomensClothing.Boots1 ||
                     item.TemplateIndex == (int)MensClothing.Boots1 ||
                     item.TemplateIndex == (int)WomensClothing.Boots2 ||
                     item.TemplateIndex == (int)MensClothing.Boots2 ||
                     item.TemplateIndex == (int)WomensClothing.Boots3 ||
                     item.TemplateIndex == (int)MensClothing.Boots3 ||
                     item.TemplateIndex == (int)WomensClothing.Shoes3 ||
                     item.TemplateIndex == (int)MensClothing.Shoes3)
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = DyeColors.Leather;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        break;
                }
            }
            // Primary colour (metal)
            else if (item.TemplateIndex == (int)MensClothing.Armbands ||
                     item.TemplateIndex == (int)MensClothing.Fancy_Armbands ||
                     item.TemplateIndex == (int)WomensClothing.Headband ||
                     item.TemplateIndex == (int)MensClothing.Headband ||
                   ((item.TemplateIndex == (int)WomensClothing.Formal_brassier) &&
                     item.CurrentVariant == 1))
            {
                int randomMetal = UnityEngine.Random.Range(0, 12);
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = (randomMetal % 2 == 0) ? DyeColors.Iron : DyeColors.Steel;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = (randomMetal % 2 == 0) ? DyeColors.Silver : DyeColors.Elven;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = (randomMetal % 2 == 0) ? DyeColors.Glass : DyeColors.Dwarven;
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = (randomMetal % 2 == 0) ? DyeColors.Orcish : DyeColors.Mithril;
                        break;
                    case ClothCraftsmanship.Exquisite:
                        if (randomMetal % 3 == 0) item.dyeColor = DyeColors.Adamantium;
                        else if (randomMetal % 3 == 1) item.dyeColor = DyeColors.Ebony;
                        else item.dyeColor = DyeColors.Daedric;
                        break;
                }
            }
            // Primary colour (fur)
            else if (item.TemplateIndex == (int)WomensClothing.Wolf_pelt ||
                     item.TemplateIndex == (int)MensClothing.Wolf_pelt ||
                     item.TemplateIndex == (int)WomensClothing.Wolf_pelt_shawl ||
                     item.TemplateIndex == (int)MensClothing.Wolf_pelt_shawl ||
                     item.TemplateIndex == (int)WomensClothing.Wolf_pelt_hood ||
                     item.TemplateIndex == (int)MensClothing.Wolf_pelt_hood)
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomWorkClothingDye();
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomCheapClothingDye();
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        break;
                }
            }
            // Primary colour + belt with buckle
            else if (item.TemplateIndex == (int)MensClothing.Kimono ||
                     item.TemplateIndex == (int)MensClothing.Long_Skirt ||
                     item.TemplateIndex == (int)MensClothing.Short_shirt_unchangeable ||
                     item.TemplateIndex == (int)MensClothing.Long_shirt_unchangeable ||
                     item.TemplateIndex == (int)WomensClothing.Casual_dress ||
                     item.TemplateIndex == (int)WomensClothing.Long_skirt ||
                     item.TemplateIndex == (int)WomensClothing.Strapless_dress ||
                     item.TemplateIndex == (int)WomensClothing.Short_shirt_unchangeable ||
                     item.TemplateIndex == (int)WomensClothing.Long_shirt_unchangeable ||
                   ((item.TemplateIndex == (int)WomensClothing.Ribbed_velvet_blouse ||
                     item.TemplateIndex == (int)MensClothing.Ribbed_velvet_blouse ||
                     item.TemplateIndex == (int)WomensClothing.Long_skirt0 ||
                     item.TemplateIndex == (int)MensClothing.Long_skirt0 ||
                     item.TemplateIndex == (int)WomensClothing.Revealing_long_skirt ||
                     item.TemplateIndex == (int)MensClothing.Revealing_long_skirt ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_top ||
                     item.TemplateIndex == (int)MensClothing.Quilted_top ||
                     item.TemplateIndex == (int)WomensClothing.Long_puff_pants ||
                     item.TemplateIndex == (int)MensClothing.Long_puff_pants ||
                     item.TemplateIndex == (int)WomensClothing.Short_puff_pants ||
                     item.TemplateIndex == (int)MensClothing.Short_puff_pants) &&
                     item.CurrentVariant == 1) ||
                   ((item.TemplateIndex == (int)WomensClothing.Puff_sleeved_top ||
                     item.TemplateIndex == (int)MensClothing.Puff_sleeved_top ||
                     item.TemplateIndex == (int)MensClothing.Short_shirt_with_belt ||
                     item.TemplateIndex == (int)MensClothing.Long_shirt_with_belt ||
                     item.TemplateIndex == (int)MensClothing.Short_shirt_closed_top2 ||
                     item.TemplateIndex == (int)MensClothing.Long_shirt_closed_top2 ||
                     item.TemplateIndex == (int)WomensClothing.Short_shirt_belt ||
                     item.TemplateIndex == (int)WomensClothing.Long_shirt_belt ||
                     item.TemplateIndex == (int)WomensClothing.Short_shirt_closed_belt ||
                     item.TemplateIndex == (int)WomensClothing.Long_shirt_closed_belt) &&
                    (item.CurrentVariant == 0 || item.CurrentVariant == 1)))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.LightBrown;
                        item.additionalColors[1] = DyeColors.Iron;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = DyeColors.LightBrown;
                        item.additionalColors[1] = DyeColors.Yellow;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        break;
                }
            }
            // Primary colour + belt (without buckle)
            else if (item.TemplateIndex == (int)WomensClothing.Sleeved_corseted_blouse ||
                     item.TemplateIndex == (int)MensClothing.Sleeved_corseted_blouse ||
                     item.TemplateIndex == (int)WomensClothing.Sleeveless_corseted_blouse ||
                     item.TemplateIndex == (int)MensClothing.Sleeveless_corseted_blouse ||
                     item.TemplateIndex == (int)WomensClothing.Puff_sleeved_corseted_top ||
                     item.TemplateIndex == (int)MensClothing.Puff_sleeved_corseted_top ||
                     item.TemplateIndex == (int)WomensClothing.Sleeveless_corseted_top ||
                     item.TemplateIndex == (int)MensClothing.Sleeveless_corseted_top ||
                     item.TemplateIndex == (int)WomensClothing.Long_sleeved_corseted_top ||
                     item.TemplateIndex == (int)MensClothing.Long_sleeved_corseted_top ||
                     item.TemplateIndex == (int)WomensClothing.Short_sleeved_corseted_top ||
                     item.TemplateIndex == (int)MensClothing.Short_sleeved_corseted_top  ||
                   ((item.TemplateIndex == (int)WomensClothing.Puff_sleeved_dress0 ||
                     item.TemplateIndex == (int)MensClothing.Puff_sleeved_dress0 ||
                     item.TemplateIndex == (int)WomensClothing.Sleeveless_dress ||
                     item.TemplateIndex == (int)MensClothing.Sleeveless_dress ||
                     item.TemplateIndex == (int)WomensClothing.Long_toga ||
                     item.TemplateIndex == (int)MensClothing.Long_toga ||
                     item.TemplateIndex == (int)WomensClothing.Toga_top ||
                     item.TemplateIndex == (int)MensClothing.Toga_top ||
                     item.TemplateIndex == (int)WomensClothing.Tights0 ||
                     item.TemplateIndex == (int)MensClothing.Tights0 ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_top ||
                     item.TemplateIndex == (int)MensClothing.Quilted_top) &&
                     item.CurrentVariant == 1) ||
                   ((item.TemplateIndex == (int)WomensClothing.Puff_sleeved_top ||
                     item.TemplateIndex == (int)MensClothing.Puff_sleeved_top ||
                     item.TemplateIndex == (int)WomensClothing.Ribbed_velvet_blouse ||
                     item.TemplateIndex == (int)MensClothing.Ribbed_velvet_blouse ||
                     item.TemplateIndex == (int)WomensClothing.Long_skirt0 ||
                     item.TemplateIndex == (int)MensClothing.Long_skirt0 ||
                     item.TemplateIndex == (int)WomensClothing.Revealing_long_skirt ||
                     item.TemplateIndex == (int)MensClothing.Revealing_long_skirt ||
                     item.TemplateIndex == (int)WomensClothing.Long_puff_pants ||
                     item.TemplateIndex == (int)MensClothing.Long_puff_pants ||
                     item.TemplateIndex == (int)WomensClothing.Short_puff_pants ||
                     item.TemplateIndex == (int)MensClothing.Short_puff_pants) &&
                     item.CurrentVariant == 2))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomRegularClothingDye();
                        item.additionalColors[0] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomMetalDye();
                        break;
                }
            }
            // Primary colour + sash
            else if (((item.TemplateIndex == (int)WomensClothing.Short_shirt_sash ||
                       item.TemplateIndex == (int)WomensClothing.Long_shirt_sash ||
                       item.TemplateIndex == (int)WomensClothing.Short_shirt_closed_sash ||
                       item.TemplateIndex == (int)WomensClothing.Long_shirt_closed_sash ||
                       item.TemplateIndex == (int)MensClothing.Short_shirt_with_sash ||
                       item.TemplateIndex == (int)MensClothing.Long_shirt_with_sash ||
                       item.TemplateIndex == (int)MensClothing.Short_shirt_closed_top3 ||
                       item.TemplateIndex == (int)MensClothing.Long_shirt_closed_top3) &&
                      (item.CurrentVariant == 0 || item.CurrentVariant == 1)) ||
                     ((item.TemplateIndex == (int)WomensClothing.Long_toga ||
                       item.TemplateIndex == (int)MensClothing.Long_toga||
                       item.TemplateIndex == (int)WomensClothing.Toga_top ||
                       item.TemplateIndex == (int)MensClothing.Toga_top) &&
                       item.CurrentVariant == 0))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomRegularClothingDye();
                        item.additionalColors[0] = DyeColors.Red;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary colour
            else if (item.TemplateIndex == (int)MensClothing.Dwynnen_surcoat ||
                     item.TemplateIndex == (int)MensClothing.Reversible_tunic ||
                     item.TemplateIndex == (int)MensClothing.Toga ||
                     item.TemplateIndex == (int)MensClothing.Wrap ||
                     item.TemplateIndex == (int)MensClothing.Vest ||
                     item.TemplateIndex == (int)WomensClothing.Wrap ||
                     item.TemplateIndex == (int)WomensClothing.Witch_hat ||
                     item.TemplateIndex == (int)MensClothing.Witch_hat ||
                     item.TemplateIndex == (int)WomensClothing.Noble_horned_headdress ||
                     item.TemplateIndex == (int)MensClothing.Noble_horned_headdress ||
                     item.TemplateIndex == (int)WomensClothing.Noble_hennin ||
                     item.TemplateIndex == (int)MensClothing.Noble_hennin ||
                     item.TemplateIndex == (int)WomensClothing.Cowl ||
                     item.TemplateIndex == (int)MensClothing.Cowl ||
                     item.TemplateIndex == (int)WomensClothing.Puff_bolero ||
                     item.TemplateIndex == (int)MensClothing.Puff_bolero ||
                     item.TemplateIndex == (int)WomensClothing.Puff_bolero0 ||
                     item.TemplateIndex == (int)MensClothing.Puff_bolero0 ||
                     item.TemplateIndex == (int)WomensClothing.Jester_hat ||
                     item.TemplateIndex == (int)MensClothing.Jester_hat ||
                   ((item.TemplateIndex == (int)WomensClothing.Sleeved_maiden_dress ||
                     item.TemplateIndex == (int)MensClothing.Sleeved_maiden_dress||
                     item.TemplateIndex == (int)WomensClothing.Sleeveless_maiden_dress ||
                     item.TemplateIndex == (int)MensClothing.Sleeveless_maiden_dress ||
                     item.TemplateIndex == (int)WomensClothing.Sleeved_dress ||
                     item.TemplateIndex == (int)MensClothing.Sleeved_dress ||
                     item.TemplateIndex == (int)WomensClothing.Puff_sleeved_dress ||
                     item.TemplateIndex == (int)MensClothing.Puff_sleeved_dress ||
                     item.TemplateIndex == (int)WomensClothing.Long_skirt2 ||
                     item.TemplateIndex == (int)MensClothing.Long_skirt2 ||
                     item.TemplateIndex == (int)WomensClothing.Lined_tunic0 ||
                     item.TemplateIndex == (int)MensClothing.Lined_tunic0 ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_doublet0 ||
                     item.TemplateIndex == (int)MensClothing.Quilted_doublet0 ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_doublet1 ||
                     item.TemplateIndex == (int)MensClothing.Quilted_doublet1 ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_doublet2 ||
                     item.TemplateIndex == (int)MensClothing.Quilted_doublet2) &&
                     item.CurrentVariant == 0) ||
                   ((item.TemplateIndex == (int)MensClothing.Short_tunic ||
                     item.TemplateIndex == (int)MensClothing.Short_tunic_fit ||
                     item.TemplateIndex == (int)WomensClothing.Ornate_bolero ||
                     item.TemplateIndex == (int)MensClothing.Ornate_bolero) &&
                     item.CurrentVariant == 1) ||
                   ((item.TemplateIndex == (int)WomensClothing.Formal_brassier ||
                     item.TemplateIndex == (int)WomensClothing.Wide_collared_blouse ||
                     item.TemplateIndex == (int)MensClothing.Wide_collared_blouse) &&
                    (item.CurrentVariant == 0 || item.CurrentVariant == 3)) ||
                   ((item.TemplateIndex == (int)WomensClothing.Formal_eodoric) &&
                    (item.CurrentVariant == 1 || item.CurrentVariant == 2)) ||
                   ((item.TemplateIndex == (int)MensClothing.Short_shirt ||
                     item.TemplateIndex == (int)MensClothing.Long_shirt ||
                     item.TemplateIndex == (int)MensClothing.Short_shirt_closed_top ||
                     item.TemplateIndex == (int)MensClothing.Long_shirt_closed_top ||
                     item.TemplateIndex == (int)WomensClothing.Short_shirt ||
                     item.TemplateIndex == (int)WomensClothing.Long_shirt ||
                     item.TemplateIndex == (int)WomensClothing.Short_shirt_closed ||
                     item.TemplateIndex == (int)WomensClothing.Long_shirt_closed) &&
                    (item.CurrentVariant == 2 || item.CurrentVariant == 3)))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomRegularClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary (steel)
            else if (item.TemplateIndex == (int)WomensClothing.Antlers ||
                     item.TemplateIndex == (int)MensClothing.Antlers ||
                     item.TemplateIndex == (int)WomensClothing.Horned_headpiece ||
                     item.TemplateIndex == (int)MensClothing.Horned_headpiece ||
                   ((item.TemplateIndex == (int)WomensClothing.Long_puff_pants0 ||
                     item.TemplateIndex == (int)MensClothing.Long_puff_pants0 ||
                     item.TemplateIndex == (int)WomensClothing.Short_puff_pants0 ||
                     item.TemplateIndex == (int)MensClothing.Short_puff_pants0) &&
                     item.CurrentVariant == 0) ||
                    (item.TemplateIndex == (int)MensClothing.Straps && item.CurrentVariant == 3))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.Iron;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = DyeColors.Steel;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomClothingDye();
                        break;
                }
            }
            // Primary (leather) + secondary (steel)
            else if (item.TemplateIndex == (int)MensClothing.Challenger_Straps ||
                     item.TemplateIndex == (int)MensClothing.Champion_straps ||
                   ((item.TemplateIndex == (int)MensClothing.Straps) && 
                     item.CurrentVariant < 3))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = DyeColors.DarkBrown;
                        item.additionalColors[0] = DyeColors.Iron;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = DyeColors.LightBrown;
                        item.additionalColors[0] = DyeColors.Steel;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomRegularClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomClothingDye();
                        break;
                }
            }
            // Primary + secondary (fur)
            else if (item.TemplateIndex == (int)MensClothing.Formal_cloak ||
                     item.TemplateIndex == (int)WomensClothing.Formal_cloak ||
                     item.TemplateIndex == (int)WomensClothing.Fur_trimmed_capelet ||
                     item.TemplateIndex == (int)MensClothing.Fur_trimmed_capelet ||
                     item.TemplateIndex == (int)WomensClothing.Colovian_fur_hat ||
                     item.TemplateIndex == (int)MensClothing.Colovian_fur_hat ||
                     item.TemplateIndex == (int)WomensClothing.Fur_cuffs ||
                     item.TemplateIndex == (int)MensClothing.Fur_cuffs ||
                     item.TemplateIndex == (int)WomensClothing.Long_fur_gloves ||
                     item.TemplateIndex == (int)MensClothing.Long_fur_gloves ||
                     item.TemplateIndex == (int)WomensClothing.Fur_gloves ||
                     item.TemplateIndex == (int)MensClothing.Fur_gloves ||
                     item.TemplateIndex == (int)WomensClothing.Short_fur_gloves ||
                     item.TemplateIndex == (int)MensClothing.Short_fur_gloves ||
                   ((item.TemplateIndex == (int)WomensClothing.Formal_brassier) &&
                    (item.CurrentVariant == 2)))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary (leather)
            else if (((item.TemplateIndex == (int)WomensClothing.Long_sleeved_leather_lined_tunic ||
                       item.TemplateIndex == (int)MensClothing.Long_sleeved_leather_lined_tunic ||
                       item.TemplateIndex == (int)WomensClothing.Short_sleeved_leather_lined_tunic ||
                       item.TemplateIndex == (int)MensClothing.Short_sleeved_leather_lined_tunic) &&
                       item.CurrentVariant == 0) ||
                     ((item.TemplateIndex == (int)WomensClothing.Evening_gown) &&
                       item.CurrentVariant == 1))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomRegularClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        break;
                }
            }
            // Primary + secondary colour (footwear)
            else if (item.TemplateIndex == (int)WomensClothing.Shoes2 ||
                     item.TemplateIndex == (int)MensClothing.Shoes2 ||
                     item.TemplateIndex == (int)WomensClothing.Boots0 ||
                     item.TemplateIndex == (int)MensClothing.Boots0 ||
                     item.TemplateIndex == (int)WomensClothing.Boots4 ||
                     item.TemplateIndex == (int)MensClothing.Boots4)
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = DyeColors.LightBrown;
                        item.additionalColors[0] = RandomCheapClothingDye();
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = DyeColors.Leather;
                        item.additionalColors[0] = RandomWorkClothingDye();
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = DyeColors.DarkBrown;
                        item.additionalColors[0] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomRegularClothingDye();
                        item.additionalColors[0] = RandomMetalDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomClothingDye();
                        break;
                }
            }
            // Primary + secondary + belt with buckle
            else if (item.TemplateIndex == (int)MensClothing.Anticlere_Surcoat ||
                   ((item.TemplateIndex == (int)WomensClothing.Fur_trimmed_top ||
                     item.TemplateIndex == (int)MensClothing.Fur_trimmed_top ||
                     item.TemplateIndex == (int)WomensClothing.Long_skirt1 ||
                     item.TemplateIndex == (int)MensClothing.Long_skirt1 ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_top0 ||
                     item.TemplateIndex == (int)MensClothing.Quilted_top0) &&
                     item.CurrentVariant == 0) ||
                   ((item.TemplateIndex == (int)WomensClothing.Lined_tunic ||
                     item.TemplateIndex == (int)MensClothing.Lined_tunic) &&
                    (item.CurrentVariant == 0 || item.CurrentVariant == 2)) ||
                   ((item.TemplateIndex == (int)WomensClothing.Sleeved_dress ||
                     item.TemplateIndex == (int)MensClothing.Sleeved_dress ||
                     item.TemplateIndex == (int)WomensClothing.Puff_sleeved_dress ||
                     item.TemplateIndex == (int)MensClothing.Puff_sleeved_dress ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_doublet2 ||
                     item.TemplateIndex == (int)MensClothing.Quilted_doublet2) &&
                     item.CurrentVariant == 1) ||
                   ((item.TemplateIndex == (int)WomensClothing.Sleeved_top ||
                     item.TemplateIndex == (int)MensClothing.Sleeved_top) &&
                    (item.CurrentVariant == 0 || item.CurrentVariant == 1)) ||
                   ((item.TemplateIndex == (int)WomensClothing.Lined_tunic0 ||
                     item.TemplateIndex == (int)MensClothing.Lined_tunic0) &&
                    (item.CurrentVariant == 1 || item.CurrentVariant == 4)) ||
                   ((item.TemplateIndex == (int)MensClothing.Short_shirt_with_belt ||
                     item.TemplateIndex == (int)MensClothing.Long_shirt_with_belt ||
                     item.TemplateIndex == (int)MensClothing.Short_shirt_closed_top2 ||
                     item.TemplateIndex == (int)MensClothing.Long_shirt_closed_top2 ||
                     item.TemplateIndex == (int)WomensClothing.Short_shirt_belt ||
                     item.TemplateIndex == (int)WomensClothing.Long_shirt_belt ||
                     item.TemplateIndex == (int)WomensClothing.Short_shirt_closed_belt ||
                     item.TemplateIndex == (int)WomensClothing.Long_shirt_closed_belt) &&
                    (item.CurrentVariant == 2 || item.CurrentVariant == 3)))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        item.additionalColors[1] = DyeColors.LightBrown;
                        item.additionalColors[2] = DyeColors.Iron;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = RandomCheapClothingDye();
                        item.additionalColors[1] = DyeColors.LightBrown;
                        item.additionalColors[2] = DyeColors.Yellow;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        item.additionalColors[2] = RandomMetalDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomMetalDye();
                        item.additionalColors[1] = RandomFancyClothingDye();
                        item.additionalColors[2] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomMetalDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        item.additionalColors[2] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary (steel) + belt with buckle
            else if ((item.TemplateIndex == (int)WomensClothing.Long_puff_pants0 ||
                      item.TemplateIndex == (int)MensClothing.Long_puff_pants0 ||
                      item.TemplateIndex == (int)WomensClothing.Short_puff_pants0 ||
                      item.TemplateIndex == (int)MensClothing.Short_puff_pants0) &&
                      item.CurrentVariant == 1)
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.Iron;
                        item.additionalColors[1] = DyeColors.LightBrown;
                        item.additionalColors[2] = DyeColors.Iron;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = DyeColors.Steel;
                        item.additionalColors[1] = DyeColors.LightBrown;
                        item.additionalColors[2] = DyeColors.Yellow;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        item.additionalColors[2] = RandomMetalDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomFancyClothingDye();
                        item.additionalColors[2] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        item.additionalColors[2] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary (leather) + belt with buckle
            else if ((item.TemplateIndex == (int)WomensClothing.Long_sleeved_leather_lined_tunic ||
                      item.TemplateIndex == (int)MensClothing.Long_sleeved_leather_lined_tunic ||
                      item.TemplateIndex == (int)WomensClothing.Short_sleeved_leather_lined_tunic ||
                      item.TemplateIndex == (int)MensClothing.Short_sleeved_leather_lined_tunic) &&
                     (item.CurrentVariant == 1 || item.CurrentVariant == 3))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.DarkBrown;
                        item.additionalColors[1] = DyeColors.LightBrown;
                        item.additionalColors[2] = DyeColors.Iron;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = DyeColors.LightBrown;
                        item.additionalColors[1] = DyeColors.DarkBrown;
                        item.additionalColors[2] = DyeColors.Yellow;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomRegularClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        item.additionalColors[2] = RandomMetalDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomFancyClothingDye();
                        item.additionalColors[2] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        item.additionalColors[2] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary colour + belt (without buckle)
            else if (item.TemplateIndex == (int)WomensClothing.Sleeved_maiden_corset ||
                     item.TemplateIndex == (int)MensClothing.Sleeved_maiden_corset ||
                     item.TemplateIndex == (int)WomensClothing.Sleeveless_maiden_corset ||
                     item.TemplateIndex == (int)MensClothing.Sleeveless_maiden_corset ||
                     item.TemplateIndex == (int)WomensClothing.Sleeved_corseted_gown ||
                     item.TemplateIndex == (int)MensClothing.Sleeved_corseted_gown || 
                     item.TemplateIndex == (int)WomensClothing.Sleeveless_corseted_gown ||
                     item.TemplateIndex == (int)MensClothing.Sleeveless_corseted_gown ||
                   ((item.TemplateIndex == (int)WomensClothing.Sleeved_maiden_dress ||
                     item.TemplateIndex == (int)MensClothing.Sleeved_maiden_dress ||
                     item.TemplateIndex == (int)WomensClothing.Sleeveless_maiden_dress ||
                     item.TemplateIndex == (int)MensClothing.Sleeveless_maiden_dress ||
                     item.TemplateIndex == (int)WomensClothing.Fur_trimmed_top ||
                     item.TemplateIndex == (int)MensClothing.Fur_trimmed_top ||
                     item.TemplateIndex == (int)WomensClothing.Long_skirt1 ||
                     item.TemplateIndex == (int)MensClothing.Long_skirt1 ||
                     item.TemplateIndex == (int)WomensClothing.Long_skirt2 ||
                     item.TemplateIndex == (int)MensClothing.Long_skirt2 ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_doublet0 ||
                     item.TemplateIndex == (int)MensClothing.Quilted_doublet0 ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_doublet1 ||
                     item.TemplateIndex == (int)MensClothing.Quilted_doublet1 ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_top0 ||
                     item.TemplateIndex == (int)MensClothing.Quilted_top0) &&
                     item.CurrentVariant == 1) ||
                   ((item.TemplateIndex == (int)WomensClothing.Sleeved_dress ||
                     item.TemplateIndex == (int)MensClothing.Sleeved_dress ||
                     item.TemplateIndex == (int)WomensClothing.Sleeved_top ||
                     item.TemplateIndex == (int)MensClothing.Sleeved_top ||
                     item.TemplateIndex == (int)WomensClothing.Puff_sleeved_dress ||
                     item.TemplateIndex == (int)MensClothing.Puff_sleeved_dress) &&
                     item.CurrentVariant == 2) ||
                   ((item.TemplateIndex == (int)WomensClothing.Wide_collared_blouse ||
                     item.TemplateIndex == (int)MensClothing.Wide_collared_blouse) &&
                    (item.CurrentVariant == 1 || item.CurrentVariant == 2)) ||
                   ((item.TemplateIndex == (int)WomensClothing.Lined_tunic0 ||
                     item.TemplateIndex == (int)MensClothing.Lined_tunic0) &&
                    (item.CurrentVariant == 2 || item.CurrentVariant == 5)))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        item.additionalColors[1] = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomRegularClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        item.additionalColors[1] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomWorkClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomMetalDye();
                        item.additionalColors[1] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary (steel) + belt (without buckle)
            else if ((item.TemplateIndex == (int)WomensClothing.Long_puff_pants0 ||
                      item.TemplateIndex == (int)MensClothing.Long_puff_pants0 ||
                      item.TemplateIndex == (int)WomensClothing.Short_puff_pants0 ||
                      item.TemplateIndex == (int)MensClothing.Short_puff_pants0) &&
                      item.CurrentVariant == 2)
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.Iron;
                        item.additionalColors[1] = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomRegularClothingDye();
                        item.additionalColors[0] = DyeColors.Steel;
                        item.additionalColors[1] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomWorkClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary (leather) + belt (without buckle)
            else if ((item.TemplateIndex == (int)WomensClothing.Long_sleeved_leather_lined_tunic ||
                      item.TemplateIndex == (int)MensClothing.Long_sleeved_leather_lined_tunic ||
                      item.TemplateIndex == (int)WomensClothing.Short_sleeved_leather_lined_tunic ||
                      item.TemplateIndex == (int)MensClothing.Short_sleeved_leather_lined_tunic) &&
                     (item.CurrentVariant == 2 || item.CurrentVariant == 4))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.DarkBrown;
                        item.additionalColors[1] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = DyeColors.LightBrown;
                        item.additionalColors[1] = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomRegularClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        break;
                }
            }
            // Primary + secondary colour + sash
            else if (((item.TemplateIndex == (int)WomensClothing.Short_shirt_sash ||
                       item.TemplateIndex == (int)WomensClothing.Long_shirt_sash ||
                       item.TemplateIndex == (int)WomensClothing.Short_shirt_closed_sash ||
                       item.TemplateIndex == (int)WomensClothing.Long_shirt_closed_sash ||
                       item.TemplateIndex == (int)MensClothing.Short_shirt_with_sash ||
                       item.TemplateIndex == (int)MensClothing.Long_shirt_with_sash ||
                       item.TemplateIndex == (int)MensClothing.Short_shirt_closed_top3 ||
                       item.TemplateIndex == (int)MensClothing.Long_shirt_closed_top3) &&
                      (item.CurrentVariant == 2 || item.CurrentVariant == 3)) ||
                     ((item.TemplateIndex == (int)WomensClothing.Lined_tunic0 ||
                       item.TemplateIndex == (int)MensClothing.Lined_tunic0) &&
                      (item.CurrentVariant == 3 || item.CurrentVariant == 6)))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        item.additionalColors[1] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomRegularClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        item.additionalColors[0] = DyeColors.Red;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomMetalDye();
                        item.additionalColors[0] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary + tertiary colour
            else if (item.TemplateIndex == (int)MensClothing.Formal_tunic ||
                     item.TemplateIndex == (int)WomensClothing.Day_gown ||
                     item.TemplateIndex == (int)WomensClothing.Prince_top ||
                     item.TemplateIndex == (int)MensClothing.Prince_top ||
                     item.TemplateIndex == (int)WomensClothing.Velvet_ballgown ||
                     item.TemplateIndex == (int)MensClothing.Velvet_ballgown ||
                     item.TemplateIndex == (int)WomensClothing.Velvet_ballgown_top ||
                     item.TemplateIndex == (int)MensClothing.Velvet_ballgown_top ||
                     item.TemplateIndex == (int)WomensClothing.Velvet_ballgown_skirt ||
                     item.TemplateIndex == (int)MensClothing.Velvet_ballgown_skirt ||
                   ((item.TemplateIndex == (int)WomensClothing.Long_sleeved_velvet_tunic ||
                     item.TemplateIndex == (int)MensClothing.Long_sleeved_velvet_tunic ||
                     item.TemplateIndex == (int)WomensClothing.Long_sleeved_velvet_top ||
                     item.TemplateIndex == (int)MensClothing.Long_sleeved_velvet_top ||
                     item.TemplateIndex == (int)WomensClothing.Short_sleeved_velvet_tunic ||
                     item.TemplateIndex == (int)MensClothing.Short_sleeved_velvet_tunic ||
                     item.TemplateIndex == (int)WomensClothing.Short_sleeved_velvet_top ||
                     item.TemplateIndex == (int)MensClothing.Short_sleeved_velvet_top ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_doublet ||
                     item.TemplateIndex == (int)MensClothing.Quilted_doublet ||
                     item.TemplateIndex == (int)WomensClothing.Doublet ||
                     item.TemplateIndex == (int)MensClothing.Doublet) &&
                    (item.CurrentVariant == 0)) ||
                   ((item.TemplateIndex == (int)WomensClothing.Long_sleeved_velvet_dress ||
                     item.TemplateIndex == (int)MensClothing.Long_sleeved_velvet_dress ||
                     item.TemplateIndex == (int)WomensClothing.Short_sleeved_velvet_dress ||
                     item.TemplateIndex == (int)MensClothing.Short_sleeved_velvet_dress) &&
                    (item.CurrentVariant == 0 || item.CurrentVariant == 1)))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomCheapClothingDye();
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary (steel) + tertiary colour
            else if ((item.TemplateIndex == (int)WomensClothing.Robes ||
                      item.TemplateIndex == (int)MensClothing.Robes ||
                      item.TemplateIndex == (int)WomensClothing.Robes0 ||
                      item.TemplateIndex == (int)MensClothing.Robes0 ||
                      item.TemplateIndex == (int)WomensClothing.Long_sleeved_tunic ||
                      item.TemplateIndex == (int)MensClothing.Long_sleeved_tunic ||
                      item.TemplateIndex == (int)WomensClothing.Robes1 ||
                      item.TemplateIndex == (int)MensClothing.Robes1 ||
                      item.TemplateIndex == (int)WomensClothing.Robes2 ||
                      item.TemplateIndex == (int)MensClothing.Robes2 ||
                      item.TemplateIndex == (int)WomensClothing.Long_sleeved_tunic0 ||
                      item.TemplateIndex == (int)MensClothing.Long_sleeved_tunic0 ||
                      item.TemplateIndex == (int)WomensClothing.Ornate_bolero ||
                      item.TemplateIndex == (int)MensClothing.Ornate_bolero) &&
                      item.CurrentVariant == 0)
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.Iron;
                        item.additionalColors[1] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = DyeColors.Steel;
                        item.additionalColors[1] = RandomCheapClothingDye();
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary + tertiary (fur)
            else if (item.TemplateIndex == (int)WomensClothing.Fur_trimmed_dress ||
                     item.TemplateIndex == (int)MensClothing.Fur_trimmed_dress ||
                     item.TemplateIndex == (int)WomensClothing.Fur_trimmed_top0 ||
                     item.TemplateIndex == (int)MensClothing.Fur_trimmed_top0)
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        item.additionalColors[1] = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        break;
                }
            }
            // Primary + secondary + tertiary colour + belt with buckle
            else if (item.TemplateIndex == (int)WomensClothing.Velvet_robe ||
                     item.TemplateIndex == (int)MensClothing.Velvet_robe ||
                   ((item.TemplateIndex == (int)WomensClothing.Gold_laced_tunic ||
                     item.TemplateIndex == (int)MensClothing.Gold_laced_tunic) &&
                     item.CurrentVariant == 0) ||
                   ((item.TemplateIndex == (int)WomensClothing.Long_sleeved_velvet_tunic ||
                     item.TemplateIndex == (int)MensClothing.Long_sleeved_velvet_tunic ||
                     item.TemplateIndex == (int)WomensClothing.Long_sleeved_velvet_top ||
                     item.TemplateIndex == (int)MensClothing.Long_sleeved_velvet_top ||
                     item.TemplateIndex == (int)WomensClothing.Short_sleeved_velvet_tunic ||
                     item.TemplateIndex == (int)MensClothing.Short_sleeved_velvet_tunic ||
                     item.TemplateIndex == (int)WomensClothing.Short_sleeved_velvet_top ||
                     item.TemplateIndex == (int)MensClothing.Short_sleeved_velvet_top ||
                     item.TemplateIndex == (int)WomensClothing.Quilted_doublet ||
                     item.TemplateIndex == (int)MensClothing.Quilted_doublet ||
                     item.TemplateIndex == (int)WomensClothing.Doublet ||
                     item.TemplateIndex == (int)MensClothing.Doublet) &&
                    (item.CurrentVariant == 1)) ||
                   ((item.TemplateIndex == (int)WomensClothing.Long_sleeved_velvet_dress ||
                     item.TemplateIndex == (int)MensClothing.Long_sleeved_velvet_dress ||
                     item.TemplateIndex == (int)WomensClothing.Short_sleeved_velvet_dress ||
                     item.TemplateIndex == (int)MensClothing.Short_sleeved_velvet_dress) &&
                     item.CurrentVariant == 2))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        item.additionalColors[2] = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomCheapClothingDye();
                        item.additionalColors[2] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        item.additionalColors[2] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomClothingDye();
                        item.additionalColors[2] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        item.additionalColors[2] = RandomFancyClothingDye();
                        break;
                }
            }
            // Primary + secondary (steel) + tertiary colour + belt with buckle
            else if ((item.TemplateIndex == (int)WomensClothing.Robes ||
                      item.TemplateIndex == (int)MensClothing.Robes ||
                      item.TemplateIndex == (int)WomensClothing.Robes0 ||
                      item.TemplateIndex == (int)MensClothing.Robes0 ||
                      item.TemplateIndex == (int)WomensClothing.Long_sleeved_tunic ||
                      item.TemplateIndex == (int)MensClothing.Long_sleeved_tunic ||
                      item.TemplateIndex == (int)WomensClothing.Robes1 ||
                      item.TemplateIndex == (int)MensClothing.Robes1 ||
                      item.TemplateIndex == (int)WomensClothing.Robes2 ||
                      item.TemplateIndex == (int)MensClothing.Robes2 ||
                      item.TemplateIndex == (int)WomensClothing.Long_sleeved_tunic0 ||
                      item.TemplateIndex == (int)MensClothing.Long_sleeved_tunic0) &&
                      item.CurrentVariant == 1)
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.Iron;
                        item.additionalColors[1] = RandomWorkClothingDye();
                        item.additionalColors[2] = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = DyeColors.Steel;
                        item.additionalColors[1] = RandomCheapClothingDye();
                        item.additionalColors[2] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        item.additionalColors[2] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        item.additionalColors[2] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        item.additionalColors[2] = RandomFancyClothingDye();
                        break;
                }
            }
            // Primary + secondary + tertiary colour + belt (without buckle)
            else if (((item.TemplateIndex == (int)WomensClothing.Gold_laced_tunic ||
                       item.TemplateIndex == (int)MensClothing.Gold_laced_tunic) &&
                       item.CurrentVariant == 1) ||
                     ((item.TemplateIndex == (int)WomensClothing.Long_sleeved_velvet_tunic ||
                       item.TemplateIndex == (int)MensClothing.Long_sleeved_velvet_tunic ||
                       item.TemplateIndex == (int)WomensClothing.Long_sleeved_velvet_top ||
                       item.TemplateIndex == (int)MensClothing.Long_sleeved_velvet_top ||
                       item.TemplateIndex == (int)WomensClothing.Short_sleeved_velvet_tunic ||
                       item.TemplateIndex == (int)MensClothing.Short_sleeved_velvet_tunic ||
                       item.TemplateIndex == (int)WomensClothing.Short_sleeved_velvet_top ||
                       item.TemplateIndex == (int)MensClothing.Short_sleeved_velvet_top ||
                       item.TemplateIndex == (int)WomensClothing.Doublet ||
                       item.TemplateIndex == (int)MensClothing.Doublet) &&
                      (item.CurrentVariant == 2)) ||
                     ((item.TemplateIndex == (int)WomensClothing.Long_sleeved_velvet_dress ||
                       item.TemplateIndex == (int)MensClothing.Long_sleeved_velvet_dress ||
                       item.TemplateIndex == (int)WomensClothing.Short_sleeved_velvet_dress ||
                       item.TemplateIndex == (int)MensClothing.Short_sleeved_velvet_dress) &&
                       item.CurrentVariant == 3))
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = RandomWorkClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        item.additionalColors[2] = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomCheapClothingDye();
                        item.additionalColors[2] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        item.additionalColors[2] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomClothingDye();
                        item.additionalColors[2] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        item.additionalColors[2] = RandomFancyClothingDye();
                        break;
                }
            }
            // Primary + secondary (steel) + tertiary colour + belt (without buckle)
            else if ((item.TemplateIndex == (int)WomensClothing.Robes ||
                      item.TemplateIndex == (int)MensClothing.Robes ||
                      item.TemplateIndex == (int)WomensClothing.Robes0 ||
                      item.TemplateIndex == (int)MensClothing.Robes0 ||
                      item.TemplateIndex == (int)WomensClothing.Long_sleeved_tunic ||
                      item.TemplateIndex == (int)MensClothing.Long_sleeved_tunic ||
                      item.TemplateIndex == (int)WomensClothing.Robes1 ||
                      item.TemplateIndex == (int)MensClothing.Robes1 ||
                      item.TemplateIndex == (int)WomensClothing.Robes2 ||
                      item.TemplateIndex == (int)MensClothing.Robes2 ||
                      item.TemplateIndex == (int)WomensClothing.Long_sleeved_tunic0 ||
                      item.TemplateIndex == (int)MensClothing.Long_sleeved_tunic0) &&
                      item.CurrentVariant == 2)
            {
                switch (clothCraftsmanship)
                {
                    case ClothCraftsmanship.Cheap:
                        item.dyeColor = RandomCheapClothingDye();
                        item.additionalColors[0] = DyeColors.Iron;
                        item.additionalColors[1] = RandomWorkClothingDye();
                        item.additionalColors[2] = DyeColors.DarkBrown;
                        break;
                    case ClothCraftsmanship.Normal:
                        item.dyeColor = RandomWorkClothingDye();
                        item.additionalColors[0] = DyeColors.Steel;
                        item.additionalColors[1] = RandomCheapClothingDye();
                        item.additionalColors[2] = DyeColors.LightBrown;
                        break;
                    case ClothCraftsmanship.Fancy:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        item.additionalColors[2] = RandomRegularClothingDye();
                        break;
                    case ClothCraftsmanship.Extravagant:
                        item.dyeColor = RandomFancyClothingDye();
                        item.additionalColors[0] = RandomFancyClothingDye();
                        item.additionalColors[1] = RandomRegularClothingDye();
                        item.additionalColors[2] = RandomFancyClothingDye();
                        break;
                    case ClothCraftsmanship.Exquisite:
                        item.dyeColor = RandomMetalDye();
                        item.additionalColors[0] = RandomRegularClothingDye();
                        item.additionalColors[1] = RandomMetalDye();
                        item.additionalColors[2] = RandomFancyClothingDye();
                        break;
                }
            }
            else item.dyeColor = RandomClothingDye();
        }

        public static void SetVariant(DaggerfallUnityItem item, int variant)
        {
            // Range check
            int totalVariants = item.ItemTemplate.variants;
            if (variant < 0 || variant >= totalVariants)
                return;

            // Clamp to appropriate variant based on material family
            if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Cuirass))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    variant = 0;
                else if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                    variant = 4;
                else
                    variant = Mathf.Clamp(variant, 1, 3);
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Hauberk))
            {
                variant = 0;
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Jerkin))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Fur)
                    variant = 12;
                else
                    variant = (int)GetArmorMaterialType(item.nativeMaterialValue);
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Greaves))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    variant = Mathf.Clamp(variant, 0, 1);
                else if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                    variant = 6;
                else
                    variant = Mathf.Clamp(variant, 2, 5);
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Chausses))
            {
                variant = 0;
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Cuisse))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Fur)
                    variant = 12;
                else
                    variant = (int)GetArmorMaterialType(item.nativeMaterialValue);
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Left_Pauldron) || item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Right_Pauldron))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    variant = 0;
                else if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                    variant = 4;
                else
                    variant = Mathf.Clamp(variant, 1, 3);
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Left_Spaulder) || item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Right_Spaulder))
            {
                variant = 0;
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Left_Vambrace) || item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Right_Vambrace))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Fur)
                    variant = 12;
                else
                    variant = (int)GetArmorMaterialType(item.nativeMaterialValue);
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Gauntlets))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    variant = 0;
                else
                    variant = 1;
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Gloves))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Fur)
                    variant = 12;
                else
                    variant = (int)GetArmorMaterialType(item.nativeMaterialValue);
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Boots))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    variant = 0;
                else
                    variant = Mathf.Clamp(variant, 1, 2);
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Sollerets))
            {
                variant = 0;
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Light_Boots))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Fur)
                    variant = 12;
                else
                    variant = (int)GetArmorMaterialType(item.nativeMaterialValue);
            }
            else if (item.IsOfTemplate(ItemGroups.Armor, (int)Armor.Helmet))
            {
                if (item.nativeMaterialValue == (int)ArmorMaterialTypes.Fur)
                    variant = 12;
                else
                    variant = (int)GetArmorMaterialType(item.nativeMaterialValue);
            }
            // Women's clothing variants
            else if (item.ItemGroup == ItemGroups.WomensClothing ||
                     item.ItemGroup == ItemGroups.MensClothing)
            {
                Debug.Log("Setting variant and color for " + item.ItemName);
                SetClothDyeData(item);
                SetClothColors(item, item.craftsmanship);
            }
            else if (item.ItemGroup == ItemGroups.Weapons && variant != 0)
            {
                string prefix = GetExoticWeaponPrefix(item);
                item.shortName = prefix + " " + item.shortName;
                item.value *= 10;
                item.maxCondition = item.maxCondition * 15 / 10;
                item.enchantmentPoints = item.enchantmentPoints * 15 / 10;
            }

            // Store variant
            item.CurrentVariant = variant;
        }

        public static DaggerfallUnityItem GetItemCraftsmanshipStats(DaggerfallUnityItem item, ClothCraftsmanship craftsmanship)
        {
            switch (craftsmanship)
            {
                case ClothCraftsmanship.Normal:
                    item.value *= 2;
                    item.maxCondition *= 3;
                    item.enchantmentPoints *= 2;
                    return item;
                case ClothCraftsmanship.Fancy:
                    item.value *= 5;
                    item.maxCondition *= 2;
                    item.enchantmentPoints *= 5;
                    return item;
                case ClothCraftsmanship.Extravagant:
                    item.value *= 10;
                    item.maxCondition *= 2;
                    item.enchantmentPoints *= 7;
                    return item;
                case ClothCraftsmanship.Exquisite:
                    item.value *= 20;
                    item.maxCondition *= 3;
                    item.enchantmentPoints *= 10;
                    return item;
                case ClothCraftsmanship.Cheap:
                default:
                    return item;
            }
        }

        public static string GetExoticWeaponPrefix(DaggerfallUnityItem weapon)
        {
            if (weapon.nativeMaterialValue == (int)MaterialTypes.Daedric)
            {
                switch (weapon.TemplateIndex)
                {
                    case (int)Weapons.Dagger:
                        return "Deadlands'";
                    case (int)Weapons.Tanto:
                        return "Mirrormoor's";
                    case (int)Weapons.Staff:
                        return "Apocrypha's";
                    case (int)Weapons.Shortsword:
                        return "Shivering Isle's";
                    case (int)Weapons.Wakazashi:
                        return "Void's";
                    case (int)Weapons.Broadsword:
                        return "Pit's";
                    case (int)Weapons.Saber:
                        return "Moonshadow's";
                    case (int)Weapons.Longsword:
                        return "Auroran's";
                    case (int)Weapons.Katana:
                        return "Spiral Skein's";
                    case (int)Weapons.Claymore:
                        return "Attribution's";
                    case (int)Weapons.Dai_Katana:
                        return "Revelry's";
                    case (int)Weapons.Mace:
                        return "Ashpit's";
                    case (int)Weapons.Flail:
                        return "Quagmire's";
                    case (int)Weapons.Warhammer:
                        return "Coldharbour's";
                    case (int)Weapons.Battle_Axe:
                        return "Regret's";
                    case (int)Weapons.War_Axe:
                        return "Oblivion's";
                    case (int)Weapons.Short_Bow:
                        return "Hunting Ground's";
                    case (int)Weapons.Long_Bow:
                        return "Evergloam's";
                }
            }
            if (weapon.nativeMaterialValue == (int)MaterialTypes.Orcish)
            {
                switch (weapon.TemplateIndex)
                {
                    case (int)Weapons.Dagger:
                    case (int)Weapons.Tanto:
                    case (int)Weapons.Staff:
                    case (int)Weapons.Shortsword:
                    case (int)Weapons.Wakazashi:
                    case (int)Weapons.Longsword:
                    case (int)Weapons.Katana:
                    case (int)Weapons.Dai_Katana:
                        return "Orsinium's";
                    case (int)Weapons.Broadsword:
                    case (int)Weapons.Saber:
                    case (int)Weapons.Claymore:
                    case (int)Weapons.Mace:
                    case (int)Weapons.Flail:
                        return "Hollow Wastes'";
                    case (int)Weapons.Warhammer:
                    case (int)Weapons.Battle_Axe:
                    case (int)Weapons.War_Axe:
                        return "Valus'";
                    case (int)Weapons.Short_Bow:
                        return "Greenshade's";
                    case (int)Weapons.Long_Bow:
                        return "Grathwood's";
                }
            }
            if (weapon.nativeMaterialValue == (int)MaterialTypes.Iron)
            {
                if (weapon.TemplateIndex == (int)Weapons.Claymore ||
                    weapon.TemplateIndex == (int)Weapons.Warhammer ||
                    weapon.TemplateIndex == (int)Weapons.War_Axe)
                    return "Craglorn's";
            }

            string randomPrefix = WorldData.WorldSetting.RegionNames[RandomWeaponOrigin(weapon)];

            if (randomPrefix.EndsWith("s")) randomPrefix += "'";
            else randomPrefix += "'s";

            return randomPrefix;
        }

        public static BodyMorphology GetBodyMorphology(Races race)
        {
            switch (race)
            {
                case Races.Argonian:
                    return BodyMorphology.Argonian;

                case Races.DarkElf:
                case Races.HighElf:
                case Races.WoodElf:
                    return BodyMorphology.Elf;

                case Races.Breton:
                case Races.Nord:
                case Races.Redguard:
                case Races.Imperial:
                case Races.Orc:
                    return BodyMorphology.Human;

                case Races.Khajiit:
                    return BodyMorphology.Khajiit;

                default:
                    throw new Exception("GetBodyMorphology() encountered unsupported race value.");
            }
        }

        public static ArmorTypes GetArmorType(int nativeMaterialValue)
        {
            return (ArmorTypes)(nativeMaterialValue / 100 * 100);
        }

        public static MaterialTypes GetArmorMaterialType(int nativeMaterialValue)
        {
            if (nativeMaterialValue == 0)
                return MaterialTypes.Leather;
            Debug.Log(nativeMaterialValue + " (nativeMaterialValue) % 0x0010 = " + (nativeMaterialValue % 0x0010));
            return (MaterialTypes)(nativeMaterialValue % 0x0010);
        }

        #endregion
    }
}