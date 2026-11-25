using System;
using System.Collections.Generic;
using System.IO;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using System.Linq;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Serialization;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using BlockRotator;
using Newtonsoft.Json;
using UnityEditor.Localization.Plugins.XLIFF.V12;

// #if UNITY_EDITOR
namespace MapEditor
{
    public class GenerateLocationsWindow : EditorWindow
    {
        static GenerateLocationsWindow generateLocationsWindow;
        const string windowTitle = "Locations Generator";
        public static GenOptions regionOptions;
        public const float distTolerance = 1.5f;
        public static List<ushort> locationIdList = new List<ushort>();
        public static string[][] dungeonTypeBlocks;
        public static Dictionary<int, List<int>> regionTiles;

        public static WorldSettings worldData;

        // How much the major regionalism weigh on the total name pool, as a percentage;
        // minor regionalisms are then (100 - majorRegionalismChance).
        public const int majorRegionalismChance = 90;
        public const int hammerfellTownVowel = 7; // number of "vowels" used in generating the second part of Hammerfell town names
        public const int genericDungeonEpithets = 19;
        public const int normalWallChance = 80; // To modify if I feel there are too many/too few special wall blocks
        public const int lastCommonInd = 2;
        public const int lastUncommonInd = 11;
        public const int lastUnusualInd = 16;
        public static int[] foolRegions = { 2, 3, 4, 6, 7, 8, 10, 12, 13, 14, 15, 24, 25, 27, 28, 29, 30, 31 };
        public static string[] regionalismAreas;
        public static string[,][] availableBlocks;
        public static BlockRotatorWindow.GeoLocations geoLocations;

        public static BlockRotatorWindow.HighRockLocNames highRockLocNames;
        public static BlockRotatorWindow.HammerfellLocNames hammerfellLocNames;
        public static BlockRotatorWindow.SkyrimLocNames skyrimLocNames;
        public static BlockRotatorWindow.ReachLocNames reachLocNames;
        public static BlockRotatorWindow.MorrowindLocNames morrowindLocNames;
        public static BlockRotatorWindow.OrsiniumLocNames orsiniumLocNames;
        public const string BRETIC = "Bretic.json";
        public const string YOKU = "Yoku.json";
        public const string NORD = "Nord.json";
        public const string REACHMEN = "Reachmen.json";
        public const string DUNMER = "Dunmer.json";
        public const string ORSIMER = "Orsimer.json";

        public enum BreticFormulas
        {
            Classic,
            Modern,
            PrefixSuffix,
            NounSuffix,
            AdjectiveNoun,
            Noun_by_the_Geo,
            Noun_of_the_Geo,
            Geo_of_Noun,
            PrefixNoun,
            NounCiv,
            NounGeo,
            AdjectiveNounSuffix,
            NounNoun,
            Noun_upon_Noun,
            NounPrefix,
            AdjectiveCiv,
            AdjectiveGeo
        }

        public enum YokuFormulas
        {
            Classic,
            PrefixSuffix,
            PrefixInterfixSuffix,
            AdjectiveCivNoun,
            AdjectiveGeoNoun,
            AdjectiveSuffix,
            AdjectiveInterfixSuffix,
            PrefixCivNoun,
            PrefixGeoNoun,
            Number_Noun,
            Adjective_CivNoun,
            Adjective_GeoNoun,
            Adjective_PrefixSuffix,
            Prefix_CivNoun,
            Prefix_GeoNoun,
            Adjective_Verb_Noun,
            Name_Of_The_Geo,
            Name_By_The_Geo,
            Civ_By_The_Adjective_Geo,
            Noun_By_The_Geo,
            Noun_Of_Name,
            Adjective_Noun_By_The_Geo,
            Adjective_Noun_Of_Name,
            PrefixSuffix_Civ,
            PrefixSuffix_Geo,
            AdjectiveNoun,
            The_Adjective_Noun_Geo,
            The_Adjective_Noun_Civ,
            The_PrefixSuffix,
            The_PrefixInterfixSuffix,
            PrefixInterfixNoun,
            NounInterfixSuffix,
            NounSuffix,
            NounXPrefixSuffix,
            Noun_PrefixSuffix,
            NounInterfix,
            AdjectiveGeo,
            AdjectiveCiv,
            PrefixInterfix,
            PrefixCiv,
            PrefixGeo,
            NounGeo,
            NounCiv,
            PrefixXCiv,
            PrefixXGeo,
            NameXGeo,
            NameXCiv,
            NameCiv,
            NameGeo,
            NameSuffix,
            PrefixInterfixInterfix,
        }

        public enum NameType
        {
            FirstName,
            Surname,
            MonsterName,
            NameORSurname,
            NameORMonsterName,
            SurnameORMonsterName
        }

        public enum WallType
        {
            None = -1,
            Wall,
            Corner,
            Gate
        }

        // This can be used to give different uses to the "PortAndUnknown" location field.
        public enum TownAttributes
        {
            Port = 1,
            Capital = 2
        }

        public enum TileType
        {
            Climate,
            Politic,
            Heightmap
        }

        // locationDensity lists density for [climate, locationType] of a Kingdom region.
        // Other government types reduce/increase these values by a percentage.
        public static float[,] locationDensity = {
            { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f },                  // Ocean
            { 0.25f, 0.45f, 0.90f, 0.30f, 0.40f, 0.45f, 1.15f, 0.60f, 0.45f, 0.35f, 0.90f, 0.30f, 1.00f, 0.0f },      // Desert
            { 0.20f, 0.40f, 0.95f, 0.25f, 0.45f, 0.40f, 1.10f, 0.70f, 0.40f, 0.30f, 1.05f, 0.25f, 1.15f, 0.0f },      // Savannah
            { 0.15f, 0.40f, 1.00f, 0.90f, 0.45f, 0.25f, 0.70f, 0.70f, 0.30f, 0.15f, 1.05f, 0.30f, 1.15f, 0.0f },      // Mountain
            { 0.25f, 0.55f, 0.95f, 0.70f, 0.40f, 0.45f, 0.75f, 0.60f, 0.35f, 0.30f, 0.90f, 0.40f, 1.00f, 0.0f },      // Rainforest
            { 0.10f, 0.25f, 0.75f, 1.15f, 0.35f, 0.20f, 0.80f, 0.55f, 0.20f, 0.45f, 0.80f, 0.20f, 0.90f, 0.0f },      // Swamp
            { 0.30f, 0.85f, 1.30f, 0.65f, 0.70f, 0.65f, 0.90f, 0.75f, 0.50f, 0.40f, 1.10f, 0.90f, 1.20f, 0.0f },      // Subtropical
            { 0.25f, 0.45f, 0.90f, 1.10f, 0.55f, 0.40f, 0.85f, 0.85f, 0.25f, 0.30f, 1.25f, 0.45f, 1.35f, 0.0f },      // MountainWood
            { 0.30f, 0.90f, 1.35f, 0.95f, 0.80f, 0.60f, 0.75f, 1.20f, 0.30f, 0.95f, 1.60f, 1.10f, 1.70f, 0.0f },      // Woodland
            { 0.20f, 0.75f, 1.15f, 0.75f, 0.70f, 0.50f, 0.85f, 1.05f, 0.15f, 0.95f, 1.40f, 0.90f, 1.50f, 0.0f }       // Steppe
        };                                                                                                        // TODO: Maquis

        // governmentModifier list reduction/increase of densities in a [government, locationType] format.
        // Kingdom doesn't make any change, but it's listed nonetheless for better understanding of future-me.
        public static float[,] governmentModifier = {
            { 0.60f, 0.55f, 0.50f, 0.60f, 1.30f, 0.65f, 0.60f, 1.40f, 0.55f, 0.70f, 1.50f, 0.50f, 0.45f, 1.0f },   // None   
            { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f},                // Kingdom
            { 0.95f, 0.90f, 0.85f, 0.95f, 1.05f, 0.90f, 0.95f, 1.10f, 0.90f, 0.95f, 1.15f, 0.90f, 0.95f, 1.0f },  // Duchy
            { 0.90f, 0.85f, 0.80f, 0.90f, 1.10f, 0.85f, 0.90f, 1.15f, 0.85f, 0.90f, 1.20f, 0.85f, 0.90f, 1.0f },  // March
            { 0.85f, 0.80f, 0.75f, 0.85f, 1.15f, 0.80f, 0.85f, 1.20f, 0.80f, 0.85f, 1.25f, 0.80f, 0.85f, 1.0f },  // County
            { 0.80f, 0.75f, 0.70f, 0.80f, 1.20f, 0.75f, 0.80f, 1.25f, 0.75f, 0.80f, 1.30f, 0.75f, 0.80f, 1.0f },  // Barony
            { 0.75f, 0.70f, 0.65f, 0.75f, 1.25f, 0.70f, 0.75f, 1.30f, 0.70f, 0.75f, 1.35f, 0.70f, 0.75f, 1.0f },  // Fiefdom
            { 1.05f, 1.10f, 1.15f, 1.05f, 0.95f, 1.10f, 1.05f, 0.90f, 1.10f, 1.05f, 0.85f, 1.10f, 1.05f, 1.0f }   // Empire
        };

        public class GenOptions
        {
            public float[] locationDensity = new float[Enum.GetNames(typeof(DFRegion.LocationTypes)).Length];
            public bool includeCapital;
            public string capitalName;
            public (int, int) capitalPosition;
            public (int, int) capitalSize;
            public bool capitalWalled;
            public bool capitalPort;
            public bool[] majorRegionalism = new bool[Enum.GetNames(typeof(LocationNamesList.NameTypes)).Length];
            public bool[] minorRegionalism = new bool[Enum.GetNames(typeof(LocationNamesList.NameTypes)).Length];
            public bool deleteExistingLocations;
            public bool deleteSurplusLocations;
        }

        public class Farm
        {
            public int worldX;
            public int worldY;
            public long locationID;
            public string region;
            public int climate;

        }

        public enum TownShapes
        {
            Regular = 0,
            LShape = 1,
            FourOnSix = 2,
            FiveOnSix = 3,
            SixOnEight = 4,
            MantaShape = 5,
            EightOnNine = 6,
            NineOnTwelve = 7,
            TwelveOnSixteen = 8,
        }

        void Awake()
        {
            regionOptions = new GenOptions();
            regionalismAreas = new string[Enum.GetNames(typeof(LocationNamesList.NameTypes)).Length];
            int counter = 0;
            foreach (string regionalism in Enum.GetNames(typeof(LocationNamesList.NameTypes)))
            {
                regionalismAreas[counter] = regionalism;
                counter++;
            }

            InitializeLocationDensity();
            InitializeAvailableBlocks();
            InitializeWorldData();

            string fileDataPath = Path.Combine(MapEditor.testPath, "mapDict.json");
            regionTiles = JsonConvert.DeserializeObject<Dictionary<int, List<int>>>(File.ReadAllText(fileDataPath));

            dungeonTypeBlocks = new string[Enum.GetNames(typeof(DFRegion.DungeonTypes)).Length][];
            dungeonTypeBlocks = JsonConvert.DeserializeObject<string[][]>(File.ReadAllText(Path.Combine(MapEditor.testPath, "DungeonBlocks.json")));

            locationIdList = InitializeLocationIdList();
        }

        void OnGUI()
        {
            GUILayout.Label(WorldInfo.WorldSetting.RegionNames[RegionManager.currentRegionIndex], EditorStyles.boldLabel);
            EditorGUILayout.Space(50.0f);

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.TownCity)] = EditorGUILayout.FloatField("Cities: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.TownCity)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.TownCity)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.TownHamlet)] = EditorGUILayout.FloatField("Hamlets: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.TownHamlet)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.TownHamlet)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.TownVillage)] = EditorGUILayout.FloatField("Villages: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.TownVillage)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.TownVillage)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.Space(20.0f);

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HomeWealthy)] = EditorGUILayout.FloatField("Wealthy Homes: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HomeWealthy)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HomeWealthy)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HomePoor)] = EditorGUILayout.FloatField("Hovels: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HomePoor)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HomePoor)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.Tavern)] = EditorGUILayout.FloatField("Taverns: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.Tavern)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.Tavern)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HomeFarms)] = EditorGUILayout.FloatField("Farms: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HomeFarms)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HomeFarms)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.Space(20.0f);

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.ReligionTemple)] = EditorGUILayout.FloatField("Temples: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.ReligionTemple)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.ReligionTemple)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.ReligionCult)] = EditorGUILayout.FloatField("Cult Sites: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.ReligionCult)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.ReligionCult)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.Coven)] = EditorGUILayout.FloatField("Witch Covens: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.Coven)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.Coven)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.Space(20.0f);

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonLabyrinth)] = EditorGUILayout.FloatField("Large Dungeons: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonLabyrinth)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonLabyrinth)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonKeep)] = EditorGUILayout.FloatField("Medium Dungeons: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonKeep)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonKeep)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonRuin)] = EditorGUILayout.FloatField("Small Dungeons: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonRuin)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonRuin)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonMinor)] = EditorGUILayout.FloatField("Negligible Dungeons: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonMinor)]);
            EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.DungeonMinor)])).ToString());
            GUILayout.EndHorizontal();

            GUILayout.Space(20.0f);

            regionOptions.deleteExistingLocations = EditorGUILayout.Toggle("Delete Existing Locations", regionOptions.deleteExistingLocations);
            regionOptions.deleteSurplusLocations = EditorGUILayout.Toggle("Delete Surplus Locations", regionOptions.deleteSurplusLocations);

            // GUILayout.BeginHorizontal();
            // regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HiddenLocation)] = EditorGUILayout.FloatField("Hidden Locations: ", regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HiddenLocation)]);
            // EditorGUILayout.LabelField("", (CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HiddenLocation)])).ToString());
            // GUILayout.EndHorizontal();

            GUILayout.Space(50.0f);

            regionOptions.includeCapital = EditorGUILayout.ToggleLeft("Generate Capital", regionOptions.includeCapital);
            GUILayout.BeginHorizontal();
            regionOptions.capitalName = EditorGUILayout.TextField("Name: ", regionOptions.capitalName, GUILayout.MaxWidth(300.0f));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            regionOptions.capitalPosition.Item1 = EditorGUILayout.IntField("Position: ", regionOptions.capitalPosition.Item1, GUILayout.MaxWidth(300.0f));
            GUILayout.Space(20.0f);
            regionOptions.capitalPosition.Item2 = EditorGUILayout.IntField(", ", regionOptions.capitalPosition.Item2, GUILayout.MaxWidth(300.0f));
            // GUILayout.Space(30.0f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            regionOptions.capitalSize.Item1 = EditorGUILayout.IntField("Size: X = ", regionOptions.capitalSize.Item1, GUILayout.MaxWidth(300.0f));
            GUILayout.Space(20.0f);
            regionOptions.capitalSize.Item2 = EditorGUILayout.IntField(", Y = ", regionOptions.capitalSize.Item2, GUILayout.MaxWidth(300.0f));
            GUILayout.Space(30.0f);
            regionOptions.capitalWalled = EditorGUILayout.ToggleLeft("Walled", regionOptions.capitalWalled);
            regionOptions.capitalPort = EditorGUILayout.ToggleLeft("Port", regionOptions.capitalPort);
            GUILayout.EndHorizontal();

            GUILayout.Space(50.0f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Major Regionalisms");
            for (int i = 0; i < Enum.GetNames(typeof(LocationNamesList.NameTypes)).Length; i++)
            {
                if (i % 4 == 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.EndHorizontal();
                }
                regionOptions.majorRegionalism[i] = EditorGUILayout.ToggleLeft(((LocationNamesList.NameTypes)i).ToString(), regionOptions.majorRegionalism[i]);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(30.0f);

            GUILayout.Label("Minor Regionalisms");
            GUILayout.BeginHorizontal();
            for (int j = 0; j < Enum.GetNames(typeof(LocationNamesList.NameTypes)).Length; j++)
            {
                if (j % 4 == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                regionOptions.minorRegionalism[j] = EditorGUILayout.ToggleLeft(((LocationNamesList.NameTypes)j).ToString(), regionOptions.minorRegionalism[j]);
            }
            GUILayout.EndHorizontal();


            if (GUILayout.Button("Generate Locations", GUILayout.MaxWidth(200.0f)))
            {
                GenerateLocations(regionOptions);
            }

            // if (GUILayout.Button("Generate Routes", GUILayout.MaxWidth(200.0f)))
            // {
            //     GenerateRoutes(regionOptions);
            // }

            // if (GUILayout.Button("Generate farms", GUILayout.MaxWidth(200.0f)))
            // {
            //     GenerateFarms();
            // }
        }

        protected int CalculateInteger(int surface, float density)
        {
            float result = density * (float)surface / 100.0f;

            if ((result - (int)result) < 0.5f)
                return (int)result;
            else return (int)result + 1;
        }

        protected void GenerateFarms()
        {
            string text = File.ReadAllText(MapEditor.testPath + "/RoadsideFarms.json");

            List<Farm> farms = new List<Farm>();
            int cursorPos = 0;

            while (cursorPos < text.Length)
            {
                Farm farm = new Farm();
                string textPart;
                int index = 0;
                cursorPos = text.IndexOf("Farm,", cursorPos);
                cursorPos += 5;
                index = text.IndexOf(",", cursorPos);
                textPart = text.Substring(cursorPos, index - cursorPos);
                // Debug.Log("cursorPos (farm.worldX): " + cursorPos + ", textPart: " + textPart);

                farm.worldX = int.Parse(textPart);


                if (farm.worldX < 0)
                {
                    Debug.Log("Error with cursor position while reading worldX! (cursorPos == " + cursorPos + ", textPart: " + textPart + ")");
                }
                // if(!int.TryParse(textPart, out farm.worldX));
                // {
                //     Debug.Log("Error with cursor position while reading worldX! (cursorPos == " + cursorPos + ", textPart: " + textPart + ")");
                //     break;
                // }

                cursorPos += index - cursorPos + 1;
                index = text.IndexOf(",", cursorPos);
                textPart = text.Substring(cursorPos, index - cursorPos);
                // Debug.Log("cursorPos (farm.worldY): " + cursorPos + ", textPart: " + textPart);
                farm.worldY = int.Parse(textPart);

                if (farm.worldY < 0)
                {
                    Debug.Log("Error with cursor position while reading worldY! (cursorPos == " + cursorPos + ", textPart: " + textPart + ")");
                }
                // if(!int.TryParse(textPart, out farm.worldY));
                // {
                //     Debug.Log("Error with cursor position while reading worldY! (cursorPos == " + cursorPos + ", textPart: " + textPart + ")");
                //     break;
                // }

                cursorPos += index - cursorPos + 1;
                index = text.IndexOf(",", cursorPos);
                textPart = text.Substring(cursorPos, index - cursorPos);
                Debug.Log("cursorPos (farm.locationID): " + cursorPos + ", textPart: " + textPart);
                farm.locationID = long.Parse(textPart);

                if (farm.locationID < 0)
                {
                    Debug.Log("Error with cursor position while reading locationID! (cursorPos == " + cursorPos + ", textPart: " + textPart + ")");
                }
                // if(!int.TryParse(textPart, out farm.locationID));
                // {
                //     Debug.Log("Error with cursor position while reading locationID! (cursorPos == " + cursorPos + ", textPart: " + textPart + ")");
                //     break;
                // }

                cursorPos += index - cursorPos + 1;
                index = text.IndexOf(",", cursorPos);
                textPart = text.Substring(cursorPos, index - cursorPos);
                farm.region = textPart;

                cursorPos += index - cursorPos + 1;
                index = text.IndexOf(",", cursorPos);
                textPart = text.Substring(cursorPos, index - cursorPos);
                // Debug.Log("cursorPos (farm.climate): " + cursorPos + ", textPart: " + textPart);
                farm.climate = int.Parse(textPart);

                if (farm.climate < 0)
                {
                    Debug.Log("Error with cursor position while reading climate! (cursorPos == " + cursorPos + ", textPart: " + textPart + ")");
                }
                // if(!int.TryParse(textPart, out farm.climate));
                // {
                //     Debug.Log("Error with cursor position while reading climate! (cursorPos == " + cursorPos + ", textPart: " + textPart + ")");
                //     break;
                // }

                cursorPos += 2;

                farms.Add(farm);
            }

            int[] progressive = new int[62];
            int counter = 0;
            string[][] vanillaLocationNames = new string[62][];
            string[][] randomFarmNames = new string[62][];
            List<string> generatedFarmNames = new List<string>();

            foreach (Farm randomFarm in farms)
            {
                int regionIndex = Array.IndexOf(WorldInfo.WorldSetting.RegionNames, randomFarm.region);

                generatedFarmNames = new List<string>();
                if (randomFarmNames[regionIndex] != null)
                {
                    generatedFarmNames = randomFarmNames[regionIndex].ToList();
                }

                if (vanillaLocationNames[regionIndex] == null)
                {
                    int count = 0;
                    vanillaLocationNames[regionIndex] = new string[Worldmaps.Worldmap[regionIndex].LocationCount];

                    foreach (DFLocation location in Worldmaps.Worldmap[regionIndex].Locations)
                    {
                        vanillaLocationNames[regionIndex][count] = Worldmaps.Worldmap[regionIndex].Locations[count].Name;
                        count++;
                    }
                }

                int regionalism = GetRegionalism(randomFarm.region);
                string farmName;

                do
                {
                    farmName = GenerateHomeName((int)DFRegion.LocationTypes.HomeFarms, regionalism);
                }
                while (vanillaLocationNames[regionIndex].Contains(farmName) || generatedFarmNames.Contains(farmName));

                generatedFarmNames.Add(farmName);
                randomFarmNames[regionIndex] = new string[generatedFarmNames.Count];
                randomFarmNames[regionIndex] = generatedFarmNames.ToArray();

                DFLocation generatedFarm = GenerateFarm((int)DFRegion.LocationTypes.HomeFarms, randomFarm, farmName, regionalism);

                string fileDataPath = Path.Combine(MapEditor.testPath, "Farms", "locationnew-randomfarm" + progressive[regionIndex] + "-" + regionIndex + ".json");

                var json = JsonConvert.SerializeObject(generatedFarm, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                File.WriteAllText(fileDataPath, json);

                progressive[regionIndex]++;
                counter++;

                // if (counter > 30)
                //     break;
            }

            Debug.Log(counter + " farms were generated.");
            // string fileDataPath = Path.Combine(MapEditor.testPath, "RoadsideFarms.json");
            // var json = JsonConvert.SerializeObject(text, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            // File.WriteAllText(fileDataPath, json);
        }

        protected int GetRegionalism(string regionName)
        {
            switch (regionName)
            {
                case "Dwynnen":
                case "Isle of Balfiera":
                case "Wrothgarian Mountains":
                case "Daggerfall":
                case "Glenpoint":
                case "Betony":
                case "Anticlere":
                case "Wayrest":
                case "Orsinium Area":
                case "Northmoor":
                case "Menevia":
                case "Alcaire":
                case "Koegria":
                case "Bhoriane":
                case "Kambria":
                case "Phrygias":
                case "Urvaius":
                case "Ykalon":
                case "Daenia":
                case "Shalgora":
                case "Tulune":
                case "Glenumbra Moors":
                case "Ilessan Hills":
                    return 0;

                case "Alik'r Desert":
                case "DragonTail Mountains":
                case "Dak'fron":
                case "Sentinel":
                case "Lainlyn":
                case "Abibon-Gora":
                case "Kairou":
                case "Pothago":
                case "Myrkwasa":
                case "Ayasofya":
                case "Tigonus":
                case "Kozanset":
                case "Satakalaam":
                case "Totambu":
                case "Mournoth":
                case "Ephesus":
                case "Santaki":
                case "Antiphyllos":
                case "Bergama":
                case "Gavaudon":
                case "Cybiades":
                default:
                    return 2;
            }
        }

        /// <summary>Main locations generator function.</summary>
        protected void GenerateLocations(GenOptions currentOptions)
        {
            bool capitalToBeGenerated = regionOptions.includeCapital;
            Debug.Log("capitalToBeGenerated: " + capitalToBeGenerated);
            DFPosition capitalPosition = new DFPosition(-1, -1);
            ulong[] surfaceArray = new ulong[RegionManager.currentRegionData.surface];
            ulong[][] locationMatrix = new ulong[Enum.GetNames(typeof(DFRegion.LocationTypes)).Length][];

            InitializeLocationNames();

            int pickedIndex = -1;
            surfaceArray = GenerateSurfaceArray(RegionManager.currentRegionIndex, surfaceArray.Length, out MapsFile.Climates[] surfaceClimates);

            for (int k = 0; k < Enum.GetNames(typeof(DFRegion.LocationTypes)).Length; k++)
            {
                Debug.Log("RegionManager.currentRegionData.locations[" + (DFRegion.LocationTypes)k + "]: " + RegionManager.currentRegionData.locations[k] + ", surfaceArray.Length: " + surfaceArray.Length + ", currentOptions.locationDensity[" + (DFRegion.LocationTypes)k + "]: " + currentOptions.locationDensity[k]);
                if (k == (int)DFRegion.LocationTypes.HomeYourShips || k == (int)DFRegion.LocationTypes.None)
                    locationMatrix[k] = new ulong[0];
                else if (!currentOptions.deleteExistingLocations && !currentOptions.deleteSurplusLocations &&
                    RegionManager.currentRegionData.locations[k] > CalculateInteger(surfaceArray.Length, currentOptions.locationDensity[k]))
                    locationMatrix[k] = new ulong[RegionManager.currentRegionData.locations[k]];
                else locationMatrix[k] = new ulong[CalculateInteger(surfaceArray.Length, currentOptions.locationDensity[k])];
                Debug.Log("locationMatrix[" + (DFRegion.LocationTypes)k + "].Length: " + locationMatrix[k].Length);
            }

            List<int> locationAlreadyRandomised = new List<int>();
            // locationAlreadyRandomised.Add((int)DFRegion.LocationTypes.HomeYourShips);
            // locationAlreadyRandomised.Add((int)DFRegion.LocationTypes.None);

            locationMatrix = InitializeLocationMatrix(locationMatrix);

            InsertExistingLocations(surfaceArray, locationMatrix, out surfaceArray, out locationMatrix);

            for (int i = 0; i < Enum.GetNames(typeof(DFRegion.LocationTypes)).Length; i++)
            {
                int randomType;
                int randomIndex;

                do
                {
                    randomType = UnityEngine.Random.Range(0, Enum.GetNames(typeof(DFRegion.LocationTypes)).Length);
                }
                while (locationAlreadyRandomised.Contains(randomType));

                locationAlreadyRandomised.Add(randomType);

                if (randomType == (int)DFRegion.LocationTypes.HomeYourShips ||
                    randomType == (int)DFRegion.LocationTypes.None)
                    continue;

                if (capitalToBeGenerated)
                {
                    if (regionOptions.capitalPosition.Item1 <= 0 || regionOptions.capitalPosition.Item2 <= 0)
                    {
                        Debug.Log("Position for capital city was not provided: will generate it randomly");
                        randomIndex = UnityEngine.Random.Range(0, surfaceArray.Length);
                        locationMatrix[(int)DFRegion.LocationTypes.TownCity][0] = surfaceArray[randomIndex];
                        surfaceArray[randomIndex] = 0;
                    }
                    else
                    {
                        capitalPosition = new DFPosition(regionOptions.capitalPosition.Item1, regionOptions.capitalPosition.Item2);
                        if (surfaceArray.Contains(Worldmaps.ConvertDFPositionToMapId(capitalPosition)))
                        {
                            locationMatrix[(int)DFRegion.LocationTypes.TownCity][0] = surfaceArray[Array.IndexOf(surfaceArray, Worldmaps.ConvertDFPositionToMapId(capitalPosition))];
                            surfaceArray[Array.IndexOf(surfaceArray, Worldmaps.ConvertDFPositionToMapId(capitalPosition))] = 0;
                        }
                        else
                        {
                            locationMatrix[(int)DFRegion.LocationTypes.TownCity][0] = Worldmaps.ConvertDFPositionToMapId(capitalPosition);
                            // surfaceArray[Array.IndexOf(surfaceArray, capitalPosition)] = new DFPosition(-1, -1);
                        }
                    }
                }
                capitalToBeGenerated = false;

                int firstLocToCreate;
                if (regionOptions.deleteExistingLocations)
                {
                    if (randomType != (int)DFRegion.LocationTypes.TownCity || capitalPosition.X.Equals(-1))
                        firstLocToCreate = 0;
                    else firstLocToCreate = 1;
                }
                    
                else firstLocToCreate = RegionManager.currentRegionData.locations[randomType];
                // int firstLocToCreate = Array.IndexOf(locationMatrix[randomType], (ulong)0);
                // if (firstLocToCreate == -1) firstLocToCreate = locationMatrix[randomType].Length;
                // int firstLocToCreate = locationMatrix[randomType].Length;
                for (int j = firstLocToCreate; j < locationMatrix[randomType].Length; j++)
                {
                    bool posOK = false;
                    DFPosition assignedPosition;

                    // if (randomType == (int)DFRegion.LocationTypes.TownCity && j == 0 && locationMatrix[(int)DFRegion.LocationTypes.TownCity].Length > (j + 1) && locationMatrix[(int)DFRegion.LocationTypes.TownCity][0] != (ulong)0)
                    //     j++;

                    do
                    {
                        randomIndex = UnityEngine.Random.Range(0, surfaceArray.Length);

                        if (surfaceArray[randomIndex] == 0)
                            continue;

                        locationMatrix[randomType][j] = surfaceArray[randomIndex];

                        posOK = CheckPosition(ConvertMapIdToDFPosition(surfaceArray[randomIndex]), randomType, currentOptions, locationMatrix);
                    }
                    while (!posOK);

                    pickedIndex = randomIndex;
                    Debug.Log("Assigned value " + surfaceArray[randomIndex] + " to locationMatrix, index: " + j);
                    assignedPosition = ConvertMapIdToDFPosition(surfaceArray[randomIndex]);
                    surfaceArray[randomIndex] = 0;
                }
            }
            // Initialising region
            Worldmap[] modifiedRegion = new Worldmap[regionTiles[RegionManager.currentRegionIndex].Count];

            // Converting what is needed to List, for ease of use
            List<string>[] mapNames = new List<string>[modifiedRegion.Length];
            List<DFRegion.RegionMapTable>[] mapTable = new List<DFRegion.RegionMapTable>[modifiedRegion.Length];
            List<DFLocation>[] locations = new List<DFLocation>[modifiedRegion.Length];
            List<Worldmap> worldList = new List<Worldmap>();

            if (!foolRegions.Contains(RegionManager.currentRegionIndex))
            {
                for (int a = 0; a < modifiedRegion.Length; a++)
                {
                    modifiedRegion[a] = Worldmaps.Worldmap[regionTiles[RegionManager.currentRegionIndex][a]];
                    if (Worldmaps.Worldmap[regionTiles[RegionManager.currentRegionIndex][a]].MapNames != null)
                        mapNames[a] = Worldmaps.Worldmap[regionTiles[RegionManager.currentRegionIndex][a]].MapNames.ToList();
                    else mapNames[a] = new List<string>();
                    if (Worldmaps.Worldmap[regionTiles[RegionManager.currentRegionIndex][a]].MapTable != null)
                        mapTable[a] = Worldmaps.Worldmap[regionTiles[RegionManager.currentRegionIndex][a]].MapTable.ToList();
                    else mapTable[a] = new List<DFRegion.RegionMapTable>();
                    // if (Worldmaps.Worldmap[regionTiles[RegionManager.currentRegionIndex][a]].Locations != null)
                    //     locations[a] = Worldmaps.Worldmap[regionTiles[RegionManager.currentRegionIndex][a]].Locations.ToList();
                    // else 
                    locations[a] = new List<DFLocation>();
                    worldList.Add(Worldmaps.Worldmap[regionTiles[RegionManager.currentRegionIndex][a]]);
                }
            }
            // worldList = Worldmaps.Worldmap.ToList();

            Worldmaps.Worldmap = new Worldmap[WorldInfo.WorldSetting.Regions];

            // for (int z = 0; z < WorldInfo.WorldSetting.Regions; z++)
            // {
            //     string regionName = WorldInfo.WorldSetting.RegionNames[z];

            //     if (foolRegions.Contains(z) || z >= worldList.Count)
            //     {
            //         Worldmaps.Worldmap[z] = new Worldmap();
            //         Worldmaps.Worldmap[z] = new Worldmap();
            //         Worldmaps.Worldmap[z].MapNames = new string[0];
            //     }
            //     else
            //     {
            //         Worldmaps.Worldmap[z] = worldList[z];
            //     }
            // }
            string locationName;
            DFLocation location = new DFLocation();

            for (int l = 0; l < Enum.GetNames(typeof(DFRegion.LocationTypes)).Length; l++)
            {
                int firstLocToCreate;
                if (regionOptions.deleteExistingLocations) firstLocToCreate = 0;
                else firstLocToCreate = RegionManager.currentRegionData.locations[l];
                for (int m = firstLocToCreate; m < locationMatrix[l].Length; m++)
                {
                    Debug.Log("Working on location " + m + " of " + locationMatrix[l].Length + ", location type: " + (DFRegion.LocationTypes)l);
                    if (l == (int)DFRegion.LocationTypes.DungeonLabyrinth ||
                        l == (int)DFRegion.LocationTypes.DungeonKeep ||
                        l == (int)DFRegion.LocationTypes.DungeonRuin ||
                        l == (int)DFRegion.LocationTypes.DungeonMinor)
                    {
                        int dungeonType = (int)BlockRotatorWindow.PickDungeonType(surfaceClimates[pickedIndex], (GovernmentType)((RegionManager.currentRegionData.governType + 1) / 2), CountBorderNum(RegionManager.currentRegionIndex), RegionManager.currentRegionIndex);

                        location = CreateLocation(l, ConvertMapIdToDFPosition(locationMatrix[l][m]), ref mapNames, out locationName, currentOptions, false, dungeonType);
                        locations[Array.IndexOf(regionTiles[RegionManager.currentRegionIndex].ToArray(), Worldmaps.GetTileIndex(locationMatrix[l][m]))].Add(location);
                        // mapTable.Add(location.MapTableData);
                    }
                    // else if (l == (int)DFRegion.LocationTypes.DungeonMinor)
                    // {
                    //     location = CreateLocation(l, locationMatrix[l][m], out locationName, currentOptions, false, (int)BlockRotatorWindow.PickDungeonType(surfaceClimates[pickedIndex], (GovernmentType)((RegionManager.currentRegionData.governType + 1) / 2), BlockRotatorWindow.CountBorderNum(RegionManager.currentRegionIndex), RegionManager.currentRegionIndex));
                    //     locations.Add(location);
                    //     // mapTable.Add(location.MapTableData);
                    // }
                    else if (ConvertMapIdToDFPosition(locationMatrix[l][m]).Equals(capitalPosition))
                    {
                        location = CreateLocation(l, ConvertMapIdToDFPosition(locationMatrix[l][m]), ref mapNames, out locationName, currentOptions, true);
                        locations[Array.IndexOf(regionTiles[RegionManager.currentRegionIndex].ToArray(), Worldmaps.GetTileIndex(locationMatrix[l][m]))].Add(location);
                    }
                    else
                    {
                        Debug.Log("locationMatrix[" + l + "][" + m + "]: " + locationMatrix[l][m] + ", surfaceClimates.Length: " + surfaceClimates.Length + ", pickedIndex: " + pickedIndex);
                        geoLocations = InitializeGeoLocation(ConvertMapIdToDFPosition(locationMatrix[l][m]), surfaceClimates[pickedIndex], (DFRegion.LocationTypes)l);
                        location = CreateLocation(l, ConvertMapIdToDFPosition(locationMatrix[l][m]), ref mapNames, out locationName, currentOptions);
                        locations[Array.IndexOf(regionTiles[RegionManager.currentRegionIndex].ToArray(), Worldmaps.GetTileIndex(locationMatrix[l][m]))].Add(location);
                        Debug.Log("Added new location to locations[" + Array.IndexOf(regionTiles[RegionManager.currentRegionIndex].ToArray(), Worldmaps.GetTileIndex(locationMatrix[l][m])) + "]");
                        // mapTable.Add(location.MapTableData);
                    }

                    // mapNames.Add(location.Name);
                }
            }
            bool everythingDone = true;

            for (int d = 0; d < worldList.Count; d++)
            {
                Debug.Log("worldList[" + d + "].Name: " + worldList[d].Name);
                worldList[d] = RemoveCurrentRegionStuff(worldList[d]);
            }
 

            for (int e = 0; e < worldList.Count; e++)
            {
                worldList[e] = InsertCurrentRegionStuff(worldList[e], locations[e]);
            }

            for (int b = 0; b < worldList.Count; b++)
            {
                locations[b] = worldList[b].Locations.ToList();
                do
                {
                    everythingDone = true;
                    for (int n = 0; n < locations[b].Count; n++)
                    {
                        if (n == 0)
                            continue;

                        if (locations[b][n].MapTableData.MapId < locations[b][n - 1].MapTableData.MapId)
                        {
                            DFLocation swap = new DFLocation();
                            swap = locations[b][n - 1];
                            locations[b].RemoveAt(n - 1);
                            locations[b].Insert(n, swap);
                            everythingDone = false;
                        }
                    }
                }
                while (!everythingDone);
            }

            Dictionary<ulong, int>[] mapIdLookup = new Dictionary<ulong, int>[worldList.Count];
            Dictionary<string, int>[] mapNameLookup = new Dictionary<string, int>[worldList.Count];
            for (int w = 0; w < worldList.Count; w++)
            {
                mapIdLookup[w] = new Dictionary<ulong, int>();
                mapNameLookup[w] = new Dictionary<string, int>();
            }

            for (int c = 0; c < locations.Length; c++)
            {
                mapTable[c] = new List<DFRegion.RegionMapTable>();
                mapNames[c] = new List<string>();
                for (int p = 0; p < locations[c].Count; p++)
                {
                    DFLocation correctIndex = locations[c][p];
                    correctIndex.LocationIndex = p;
                    locations[c].RemoveAt(p);
                    locations[c].Insert(p, correctIndex);
                    mapTable[c].Add(locations[c][p].MapTableData);
                    mapNames[c].Add(locations[c][p].Name);
                }

                worldList[c].Locations = locations[c].ToArray();

                for (int d = 0; d < worldList[c].Locations.Length; d++)
                {
                    if (!mapIdLookup[c].ContainsKey(mapTable[c][d].MapId))
                        mapIdLookup[c].Add(mapTable[c][d].MapId, d);
                    else Debug.Log("Trying to add duplicate item to mapIdLookup: " + mapTable[c][d].MapId + ", " + mapNames[c][d]);
                    if (!mapNameLookup[c].ContainsKey(mapNames[c][d]))
                        mapNameLookup[c].Add(mapNames[c][d], d);
                    else Debug.Log("Trying to add duplicate item to mapNameLookup: " + mapNames[c][d]);
                }
            }

            Debug.Log("worldList.Count: " + worldList.Count);
            for (int f = 0; f < worldList.Count; f++)
            {
                Worldmap worldmap = new Worldmap();
                worldmap.Name = regionTiles[RegionManager.currentRegionIndex][f].ToString();
                worldmap.LocationCount = worldList[f].LocationCount;
                worldmap.MapNames = mapNames[f].ToArray();
                worldmap.MapTable = mapTable[f].ToArray();
                worldmap.MapIdLookup = mapIdLookup[f];
                worldmap.MapNameLookup = mapNameLookup[f];
                worldmap.Locations = locations[f].ToArray();

                string fileDataPath = Path.Combine(MapEditor.testPath, "Locations", "map" + regionTiles[RegionManager.currentRegionIndex][f].ToString("00000") + ".json");
                var json = JsonConvert.SerializeObject(worldmap, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                File.WriteAllText(fileDataPath, json);
            }
        }

        protected ulong[][] InitializeLocationMatrix(ulong[][] locationMatrix)
        {
            for (int i = 0; i < Enum.GetNames(typeof(DFRegion.LocationTypes)).Length; i++)
            {
                for (int j = 0; j < locationMatrix[i].Length; j++)
                {
                    locationMatrix[i][j] = 0;
                }
            }

            return locationMatrix;
        }

        protected int CountBorderNum(int regionInd)
        {
            int counter = 0;
            while (worldData.regionBorders[regionInd * 20 + counter] != 408 &&
                   counter < 20)
            {
                counter++;
            }
            return counter;
        }

        protected Worldmap RemoveCurrentRegionStuff(Worldmap startingMap)
        {
            if (startingMap.LocationCount < 1) return startingMap;
            List<string> nameList = startingMap.MapNames.ToList();
            List<DFRegion.RegionMapTable> mapTableList = startingMap.MapTable.ToList();
            List<DFLocation> locList = startingMap.Locations.ToList();

            // Debug.Log("startingMap.Locations.Length: " + startingMap.Locations.Length);
            for (int i = 0; i < startingMap.Locations.Length; i++)
            {
                if (startingMap.Locations[i].RegionIndex == RegionManager.currentRegionIndex)
                {
                    int indexToRemove = nameList.IndexOf(startingMap.Locations[i].Name);
                    Debug.Log("nameList.Count: " + nameList.Count + ", removing at " + indexToRemove + ", location: " + startingMap.Locations[i].Name + ", mapId: " + startingMap.Locations[i].MapTableData.MapId + "(" + startingMap.Locations[i].Name + ")");
                    if (indexToRemove > -1)
                        nameList.RemoveAt(indexToRemove);
                    else
                    {
                        if (!nameList.Remove(startingMap.Locations[i].Name)) ;
                        Debug.Log("Couldn't find " + startingMap.Locations[i].Name + " in nameList.");
                        indexToRemove = startingMap.MapIdLookup[startingMap.Locations[i].MapTableData.MapId];
                        Debug.Log("New indexToRemove: " + indexToRemove);
                    }

                    mapTableList.Remove(startingMap.Locations[i].MapTableData);
                    startingMap.MapIdLookup.Remove(startingMap.Locations[i].MapTableData.MapId);
                    startingMap.MapNameLookup.Remove(startingMap.Locations[i].Name);
                    if (indexToRemove > -1 && locList.Count > indexToRemove)
                    {
                        Debug.Log("Removing " + locList[indexToRemove].Name + " from data: " + startingMap.Locations[i].Name);
                        locList.RemoveAt(indexToRemove);
                    }
                    else locList.Remove(startingMap.Locations[i]);
                    startingMap.LocationCount--;
                }
            }
            startingMap.MapNames = nameList.ToArray();
            startingMap.MapTable = mapTableList.ToArray();
            startingMap.Locations = locList.ToArray();

            return startingMap;
        }

        protected Worldmap InsertCurrentRegionStuff(Worldmap startingMap, List<DFLocation> locToAdd)
        {
            List<string> nameList = startingMap.MapNames.ToList();
            List<DFRegion.RegionMapTable> mapTableList = startingMap.MapTable.ToList();
            List<DFLocation> locList = startingMap.Locations.ToList();
            Debug.Log("locToAdd.Count: " + locToAdd.Count);

            for (int i = 0; i < locToAdd.Count; i++)
            {
                nameList.Add(locToAdd[i].Name);
                mapTableList.Add(locToAdd[i].MapTableData);
                Debug.Log("Adding location " + locToAdd[i].Name + ", mapId: " + locToAdd[i].MapTableData.MapId + ", location type: " + locToAdd[i].MapTableData.LocationType);
                //if (startingMap.MapIdLookup.Keys.Contains(locToAdd[i].MapTableData.MapId))
                // Debug.Log("MapIdLookup already contains key " + locToAdd[i].MapTableData.MapId + ": " + startingMap.Locations[startingMap.MapIdLookup[locToAdd[i].MapTableData.MapId]].Name);
                startingMap.MapIdLookup.Add(locToAdd[i].MapTableData.MapId, startingMap.LocationCount + 1);
                startingMap.MapNameLookup.Add(locToAdd[i].Name, startingMap.LocationCount + 1);
                locList.Add(locToAdd[i]);
                startingMap.LocationCount++;
            }
            startingMap.MapNames = nameList.ToArray();
            startingMap.MapTable = mapTableList.ToArray();
            startingMap.Locations = locList.ToArray();

            if (startingMap.LocationCount != startingMap.Locations.Length)
                startingMap.LocationCount = startingMap.Locations.Length;

            return startingMap;
        }

        protected void InsertExistingLocations(ulong[] surfaceArray, ulong[][] locationMatrix, out ulong[] outSurfaceArray, out ulong[][] outLocationMatrix)
        {
            outSurfaceArray = surfaceArray;
            outLocationMatrix = locationMatrix;
            int[] locCounter = new int[locationMatrix.Length];
            List<(int, int)>[] locDiscarded = new List<(int, int)>[locationMatrix.Length];
            for (int g = 0; g < locCounter.Length; g++)
            {
                locCounter[g] = 0;
                locDiscarded[g] = new List<(int, int)>();
            }

            for (int h = 0; h < regionTiles[RegionManager.currentRegionIndex].Count; h++)
            {
                DFRegion region = ReadMapTile(regionTiles[RegionManager.currentRegionIndex][h]);
                for (int i = 0; i < region.LocationCount; i++)
                {
                    // int randomChance = UnityEngine.Random.Range(0, CalculateInteger(RegionManager.currentRegionData.surface, RegionManager.currentRegionData.locations[(int)region.MapTable[i].LocationType]));

                    // If surplus locations have to be deleted, "jump" some location on a certain percentage given by locations-to-be-generated/existing-locations
                    // Later we'll fill the eventual gaps.
                    if (regionOptions.deleteSurplusLocations && UnityEngine.Random.Range(0, CalculateInteger(RegionManager.currentRegionData.surface, RegionManager.currentRegionData.locations[(int)region.MapTable[i].LocationType])) >= locationMatrix[(int)region.MapTable[i].LocationType].Length)
                    {
                        locDiscarded[(int)region.MapTable[i].LocationType].Add((regionTiles[RegionManager.currentRegionIndex][h], i));
                        continue;
                    }
                    // If all the locations from the region have to be re-created, jump all locations with that region index
                    if (regionOptions.deleteExistingLocations && PoliticInfo.GetRegionIndexFromMapId(region.MapTable[i].MapId) == RegionManager.currentRegionIndex)
                        continue;

                    // Exclude locations from other regions
                    if (PoliticInfo.GetRegionIndexFromMapId(region.MapTable[i].MapId) != RegionManager.currentRegionIndex)
                        continue;

                    locCounter[(int)region.MapTable[i].LocationType]++;
                    Debug.Log("outSurfaceArray.Length: " + outSurfaceArray.Length + ", surfaceArray.Length: " + surfaceArray.Length + ", ConvertMapIdToDFPosition(region.MapTable[i].MapId): " + ConvertMapIdToDFPosition(region.MapTable[i].MapId) + ", region.MapTable[i].MapId:" + region.MapTable[i].MapId + ", Array.IndexOf(locationMatrix[(int)region.MapTable[i].LocationType], 0): " + Array.IndexOf(locationMatrix[(int)region.MapTable[i].LocationType], (ulong)0));

                    outSurfaceArray[Array.IndexOf(surfaceArray, region.MapTable[i].MapId)] = 0;
                    outLocationMatrix[(int)region.MapTable[i].LocationType][Array.IndexOf(outLocationMatrix[(int)region.MapTable[i].LocationType], (ulong)0)] = region.MapTable[i].MapId;
                    // locCounter[(int)region.MapTable[i].LocationType]++;
                }
            }

            // Removing surplus location or add others if the threshold hasn't been reached
            if (regionOptions.deleteSurplusLocations)
            {
                for (int j = 0; j < locationMatrix.Length; j++)
                {
                    int diff = locCounter[j] - CalculateInteger(RegionManager.currentRegionData.surface, regionOptions.locationDensity[(int)(DFRegion.LocationTypes.HomePoor)]);
                    if (diff < 0)
                    {
                        while (diff < 0)
                        {
                            int ind = UnityEngine.Random.Range(0, locDiscarded[j].Count);
                            (int, int) locPicked = locDiscarded[j][ind];
                            DFRegion region = ReadMapTile(locPicked.Item1);
                            outSurfaceArray[Array.IndexOf(surfaceArray, region.MapTable[locPicked.Item2].MapId)] = 0;
                            outLocationMatrix[(int)region.MapTable[locPicked.Item2].LocationType][Array.IndexOf(locationMatrix[(int)region.MapTable[locPicked.Item2].LocationType], (ulong)0)] = region.MapTable[locPicked.Item2].MapId;
                            diff++;
                        }
                    }
                    if (diff > 0)
                    {
                        while (diff > 0)
                        {
                            int ind = UnityEngine.Random.Range(0, outLocationMatrix[j].Length);
                            DFPosition pos = ConvertMapIdToDFPosition(outLocationMatrix[j][ind]);

                            List<ulong> locList = new List<ulong>(outLocationMatrix[j]);
                            locList.RemoveAt(ind);
                            outLocationMatrix[j] = locList.ToArray();

                            List<ulong> surList = new List<ulong>(outSurfaceArray);
                            surList.Remove(Worldmaps.ConvertDFPositionToMapId(pos));
                            outSurfaceArray = surList.ToArray();

                            diff--;
                        }
                    }
                }
            }
            WriteSurfaceArray(surfaceArray, outSurfaceArray, locationMatrix, outLocationMatrix);
        }

        protected void WriteSurfaceArray(ulong[] surfaceArray, ulong[] outSurfaceArray, ulong[][] locationMatrix, ulong[][] outLocationMatrix)
        {
            string fileDataPath = Path.Combine(MapEditor.testPath, "surfaceArray.json");
            var json = JsonConvert.SerializeObject(surfaceArray, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(fileDataPath, json);

            fileDataPath = Path.Combine(MapEditor.testPath, "outSurfaceArray.json");
            json = JsonConvert.SerializeObject(outSurfaceArray, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(fileDataPath, json);

            fileDataPath = Path.Combine(MapEditor.testPath, "locationMatrix.json");
            json = JsonConvert.SerializeObject(locationMatrix, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(fileDataPath, json);

            fileDataPath = Path.Combine(MapEditor.testPath, "outLocationMatrix.json");
            json = JsonConvert.SerializeObject(outLocationMatrix, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(fileDataPath, json);
        }

        protected DFRegion ReadMapTile(int tileProg)
        {
            DFRegion region = new DFRegion();
            region = JsonConvert.DeserializeObject<DFRegion>(File.ReadAllText(Path.Combine(MapEditor.testPath, "Locations", "map" + tileProg.ToString("00000") + ".json")));
            return region;
        }

        protected DFPosition ConvertMapIdToDFPosition(ulong mapId)
        {
            int x = (int)(mapId % BlockRotatorWindow.MAXXCOORD);
            int y = (int)(mapId / BlockRotatorWindow.MAXXCOORD);
            return new DFPosition(x, y);
        }

        protected DFLocation CreateLocation(int locationType, DFPosition position, ref List<string>[] presentNames, out string locationName, GenOptions currentOptions, bool isCapital = false, int dungeonType = -1)
        {
            // Generate a location name based on LocationType, and major and minor regionalisms
            List<int> majorRegionalism = new List<int>();
            List<int> minorRegionalism = new List<int>();
            DFLocation generatedLocation = new DFLocation();

            for (int i = 0; i < currentOptions.majorRegionalism.Length; i++)
            {
                if (currentOptions.majorRegionalism[i])
                    majorRegionalism.Add(i);
            }

            for (int j = 0; j < currentOptions.minorRegionalism.Length; j++)
            {
                if (currentOptions.minorRegionalism[j])
                    minorRegionalism.Add(j);
            }

            do
            {
                if (isCapital)
                {
                    locationName = regionOptions.capitalName;
                }
                else
                    locationName = GenerateLocationName(locationType, majorRegionalism, minorRegionalism, dungeonType);
            }
            while (!CheckDoubleNames(presentNames, locationName) && locationType != (int)DFRegion.LocationTypes.Tavern);
            // It's not important to put the newly generated name in the correct map list for 2 resons:
            // first, the CheckDoubleNames functions check them all anyway, second, it's better not to have same name locations
            // in the same region. Therefore, adding it to the first array, since it should be always present.
            presentNames[0].Add(locationName);

            generatedLocation = GenerateLocation(locationType, position, dungeonType, locationName, ref presentNames, isCapital);

            return generatedLocation;
        }

        protected bool CheckDoubleNames(List<string>[] presentNames, string pickedName)
        {
            for (int i = 0; i < presentNames.Length; i++)
            {
                if (presentNames[i].Contains(pickedName))
                    return false;
            }
            return true;
        }

        protected string GenerateLocationName(int locationType, List<int> majorReg, List<int> minorReg, int dungeonType)
        {
            // I'm modifying the way to calculate major and minor regionalisms, since I don't think (anymore?) that having more
            // regionalisms of a certain kind (maj or min) should alter the chances of getting one or the other(s). For now, I'll
            // just comment the old code out in case I change my mind. Again.
            int majRegNumber = majorReg.Count;
            int minRegNumber = minorReg.Count;
            int minorRegionalismChance = 100 - majorRegionalismChance;

            // int majChance = majRegNumber * majorRegionalismChance;
            int majChance = majorRegionalismChance;

            // int minChance = minRegNumber * minorRegionalismChance;
            int minChance = minorRegionalismChance;

            int totalChance = majChance + minChance;
            int selectedReg = 0;

            int randomPick = UnityEngine.Random.Range(0, totalChance);

            if (randomPick <= majChance || minorReg.Count < 1)
                selectedReg = majorReg[randomPick % majRegNumber];
            else selectedReg = minorReg[randomPick % minRegNumber];

            string resultingName;
            List<string> nameAlreadyPresent = new List<string>();

            switch (locationType)
            {
                case (int)DFRegion.LocationTypes.TownCity:
                case (int)DFRegion.LocationTypes.TownHamlet:
                case (int)DFRegion.LocationTypes.TownVillage:
                    resultingName = GenerateTownName(locationType, selectedReg, nameAlreadyPresent);
                    nameAlreadyPresent.Add(resultingName);
                    break;

                case (int)DFRegion.LocationTypes.HomeFarms:
                case (int)DFRegion.LocationTypes.HomeWealthy:
                case (int)DFRegion.LocationTypes.HomePoor:
                    resultingName = GenerateHomeName(locationType, selectedReg);
                    break;

                case (int)DFRegion.LocationTypes.Tavern:
                    resultingName = "Waiting for RMB selection";
                    break;

                case (int)DFRegion.LocationTypes.DungeonLabyrinth:
                case (int)DFRegion.LocationTypes.DungeonKeep:
                case (int)DFRegion.LocationTypes.DungeonRuin:
                case (int)DFRegion.LocationTypes.DungeonMinor:
                    resultingName = GenerateDungeonName(locationType, selectedReg, dungeonType);
                    break;


                case (int)DFRegion.LocationTypes.ReligionCult:
                case (int)DFRegion.LocationTypes.ReligionTemple:
                    resultingName = GenerateTempleName(locationType);
                    break;

                default:
                    resultingName = "error";
                    break;
            }

            return resultingName;
        }

        protected string GenerateTownName(int locationType, int regionalism, List<string> nameAlreadyPresent)
        {
            string tempName = "";

            Debug.Log("Regionalism picked: " + (LocationNamesList.NameTypes)regionalism);

            switch (regionalism)
            {
                case (int)LocationNamesList.NameTypes.HighRock:
                    do
                    {
                        tempName = HighRockTownName();
                    }
                    while (nameAlreadyPresent.Contains(tempName));

                    return tempName;

                case (int)LocationNamesList.NameTypes.Hammerfell:
                    do
                    {
                        tempName = HammerfellTownName();
                    }
                    while (nameAlreadyPresent.Contains(tempName));

                    return tempName;

                // numPrefix = UnityEngine.Random.Range(0, LocationNamesList.NamesList[regionalism][(int)LocationNamesList.NameParts.Prefix].Length);
                // numSuffix1 = UnityEngine.Random.Range(0, hammerfellTownVowel);
                // string suffix1 = "";
                // string[] vowelSuffix = new string[LocationNamesList.NamesList[(int)LocationNamesList.NameTypes.Hammerfell][(int)LocationNamesList.NameParts.Suffix1].Length];
                // vowelSuffix = LocationNamesList.NamesList[(int)LocationNamesList.NameTypes.Hammerfell][(int)LocationNamesList.NameParts.Suffix1];
                // string[][] vowelArray = GenerateVowelArray(vowelSuffix);
                // int nameType;

                // if (numSuffix1 != 3)
                //     nameType = UnityEngine.Random.Range(0, 4);
                // else nameType = UnityEngine.Random.Range(0, 2) + 2;
                // int numSuffix2 = UnityEngine.Random.Range(0, LocationNamesList.NamesList[regionalism][(int)LocationNamesList.NameParts.Suffix2].Length);
                // numExtra = UnityEngine.Random.Range(0, LocationNamesList.NamesList[regionalism][(int)LocationNamesList.NameParts.Extra].Length);

                // Debug.Log("numSuffix1: " + numSuffix1);

                // switch (nameType)
                // {
                //     case 0:     // Prefix + vowel + Suffix1 + Suffix2
                //     case 1:     //  Prefix + vowel + Suffix1 + Extra
                //         suffix1 = PickVowel(numSuffix1, vowelSuffix, vowelArray, true);
                //         break;

                //     case 2:     // Prefix + vowel + Suffix2
                //     case 3:     // Prefix + vowel + Extra
                //         suffix1 = PickVowel(numSuffix1, vowelSuffix, vowelArray, false);
                //         break;
                // }

                // switch (nameType)
                // {
                //     case 0:
                //     case 2:
                //         tempName = string.Concat(LocationNamesList.NamesList[regionalism][(int)LocationNamesList.NameParts.Prefix][numPrefix],
                //                     suffix1,
                //                     LocationNamesList.NamesList[regionalism][(int)LocationNamesList.NameParts.Suffix2][numSuffix2]);
                //         break;

                //     case 1:
                //     case 3:
                //         tempName = string.Concat(LocationNamesList.NamesList[regionalism][(int)LocationNamesList.NameParts.Prefix][numPrefix],
                //                     suffix1,
                //                     LocationNamesList.NamesList[regionalism][(int)LocationNamesList.NameParts.Extra][numExtra]);
                //         break;

                //     default:
                //         break;
                // }

                // return tempName;

                case (int)LocationNamesList.NameTypes.Skyrim:
                    do
                    {
                        tempName = SkyrimTownName();
                    }
                    while (nameAlreadyPresent.Contains(tempName));

                    return tempName;

                case (int)LocationNamesList.NameTypes.Reachmen:
                    do
                    {
                        tempName = ReachTownName();
                    }
                    while (nameAlreadyPresent.Contains(tempName));

                    return tempName;

                case (int)LocationNamesList.NameTypes.Morrowind:
                    do
                    {
                        tempName = MorrowindTownName();
                    }
                    while (nameAlreadyPresent.Contains(tempName));

                    return tempName;

                case (int)LocationNamesList.NameTypes.Orsinium:
                    do
                    {
                        tempName = OrsiniumTownName();
                    }
                    while (nameAlreadyPresent.Contains(tempName));

                    return tempName;

                default:
                    break;
            }

            return "Error";
        }

        protected string PickVowel(int vowel, string[] vowelSuffix, string[][] vowelArray, bool suffixPresent = true)
        {
            int counter = 0;
            int startingPoint = -1;
            string resultingString = "";
            int suffix1;
            int vowelCounter = vowel;

            do
            {
                if (counter == vowelSuffix.Length || vowelSuffix[counter].StartsWith("*"))
                {
                    if (vowelCounter == 0)
                    {
                        resultingString = vowelSuffix[counter].Substring(1);
                        if (!suffixPresent)
                        {
                            return resultingString;
                        }
                        else if (vowelArray[vowel].Length > 0 && vowelArray[vowel] != null)
                        {
                            suffix1 = UnityEngine.Random.Range(0, vowelArray[vowel].Length);
                            Debug.Log("vowel: " + vowel + ", suffix1: " + suffix1 + ", vowelArray.Length" + vowelArray.Length + ", vowelArray[vowel].Length: " + vowelArray[vowel].Length);
                            resultingString = string.Concat(resultingString, vowelArray[vowel][suffix1]);
                            return resultingString;
                        }
                        else
                        {
                            return resultingString;
                        }
                    }

                    vowelCounter--;
                }
                counter++;
            }
            while (startingPoint < 0);

            return resultingString;
        }

        protected string[][] GenerateVowelArray(string[] vowels)
        {
            string[][] vowelArray = new string[hammerfellTownVowel][];
            int vowelCounter = -1;
            List<string> vowelList = new List<string>();
            for (int i = 0; i <= vowels.Length; i++)
            {
                if (i == vowels.Length || vowels[i].StartsWith("*"))
                {
                    if (i != 0)
                    {
                        vowelArray[vowelCounter] = vowelList.ToArray();
                        vowelList = new List<string>();
                    }
                    vowelCounter++;
                }
                else
                {
                    vowelList.Add(vowels[i]);
                }
            }

            return vowelArray;
        }

        protected string ConvertToUppercase(string input)
        {
            char[] charExtra = input.ToCharArray();
            Char.ToUpper(charExtra[0]);
            input = charExtra.ToString();

            return input;
        }

        /// <summary>
        /// Checks if an assigned position is too close to a conflicting location,
        /// or already taken.
        /// </summary>
        protected bool CheckPosition(DFPosition position, int locationType, GenOptions currentOptions, ulong[][] locationMatrix)
        {
            if (3 > SmallHeightmap.GetHeightMapValue(position.X, position.Y))
                return false;

            List<int> conflictingLocations = GetConflictingLocations(locationType);

            for (int y = -1; y < 2; y++)
            {
                for (int x = -1; x < 2; x++)
                {
                    bool locFound = false;
                    ulong confMapId = Worldmaps.ConvertDFPositionToMapId(new DFPosition(position.X + x, position.Y + y));
                    if (x == 0 && y == 0) continue;
                    for (int i = 0; i < locationMatrix.Length; i++)
                    {
                        if (locationMatrix[i].Contains(confMapId))
                        {
                            locFound = true;
                            if (conflictingLocations.Contains(i))
                                return false;
                        }
                        if (locFound) break;
                    }
                }
            }

            return true;
        }

        public static bool IsDungeon(int locationType)
        {
            if ((int)DFRegion.LocationTypes.DungeonLabyrinth == locationType ||
                (int)DFRegion.LocationTypes.DungeonKeep == locationType ||
                (int)DFRegion.LocationTypes.DungeonRuin == locationType ||
                (int)DFRegion.LocationTypes.DungeonMinor == locationType)
                return true;
            return false;
        }

        public static float CalculateDistance(DFPosition position1, DFPosition position2)
        {
            double absX = (double)(Math.Abs(position1.X - position2.X));
            double absY = (double)(Math.Abs(position1.Y - position2.Y));

            return (float)(Math.Sqrt((Math.Pow(absX, 2.0f) + Math.Pow(absY, 2.0f))));
        }

        protected float CalculateDistance((int, int) position1, (int, int) position2)
        {
            double absX = (double)(Math.Abs(position1.Item1 - position2.Item1));
            double absY = (double)(Math.Abs(position1.Item2 - position2.Item2));

            return (float)(Math.Sqrt((Math.Pow(absX, 2.0f) + Math.Pow(absY, 2.0f))));
        }

        /// <summary>
        /// Create a list of those location types that can't be positioned near
        /// the location that's being created.
        /// </summary>
        protected List<int> GetConflictingLocations(int locationType)
        {
            List<int> conflictingLocations = new List<int>();
            // conflictingDungeon = new List<int>();

            switch (locationType)
            {
                case (int)DFRegion.LocationTypes.TownCity:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownCity);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownHamlet);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Coven);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonLabyrinth);
                    break;

                case (int)DFRegion.LocationTypes.TownHamlet:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownCity);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownHamlet);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownVillage);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonLabyrinth);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonKeep);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Coven);
                    break;

                case (int)DFRegion.LocationTypes.TownVillage:
                    // conflictingLocations.Add((int)DFRegion.LocationTypes.TownCity);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownHamlet);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownVillage);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonLabyrinth);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.ReligionTemple);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Tavern);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonKeep);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomeWealthy);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonRuin);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Coven);
                    break;

                case (int)DFRegion.LocationTypes.HomeFarms:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonLabyrinth);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonKeep);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonRuin);
                    break;

                case (int)DFRegion.LocationTypes.DungeonLabyrinth:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownCity);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownHamlet);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownVillage);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomeFarms);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonLabyrinth);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.ReligionTemple);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Tavern);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonKeep);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomeWealthy);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonRuin);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomePoor);
                    break;

                case (int)DFRegion.LocationTypes.ReligionTemple:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonLabyrinth);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.ReligionTemple);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonKeep);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonRuin);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.ReligionCult);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Coven);
                    break;

                case (int)DFRegion.LocationTypes.Tavern:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownVillage);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonLabyrinth);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Tavern);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonKeep);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonMinor);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Coven);
                    break;

                case (int)DFRegion.LocationTypes.DungeonKeep:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownVillage);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomeFarms);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonLabyrinth);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.ReligionTemple);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Tavern);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonKeep);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomeWealthy);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonRuin);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomePoor);
                    break;

                case (int)DFRegion.LocationTypes.HomeWealthy:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownVillage);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonLabyrinth);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonKeep);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomeWealthy);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonRuin);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Coven);
                    break;

                case (int)DFRegion.LocationTypes.ReligionCult:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.ReligionTemple);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.ReligionCult);
                    break;

                case (int)DFRegion.LocationTypes.DungeonRuin:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomeFarms);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonLabyrinth);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.ReligionTemple);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonKeep);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomeWealthy);
                    break;

                case (int)DFRegion.LocationTypes.HomePoor:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonLabyrinth);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.DungeonKeep);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomePoor);
                    break;

                case (int)DFRegion.LocationTypes.DungeonMinor:
                    break;

                case (int)DFRegion.LocationTypes.Coven:
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownCity);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownHamlet);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.TownVillage);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.ReligionTemple);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Tavern);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.HomeWealthy);
                    conflictingLocations.Add((int)DFRegion.LocationTypes.Coven);
                    break;

                default:
                    break;
            }

            // switch (locationType)
            // {
            //     case (int)DFRegion.LocationTypes.TownCity:
            //     case (int)DFRegion.LocationTypes.TownHamlet:
            //     case (int)DFRegion.LocationTypes.TownVillage:
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.BarbarianStronghold);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.Coven);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.Crypt);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.DesecratedTemple);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.DragonsDen);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.GiantStronghold);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.HarpyNest);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.Laboratory);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.OrcStronghold);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.NaturalCave);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.ScorpionNest);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.SpiderNest);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.VampireHaunt);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.VolcanicCaves);
            //         break;

            //     case (int)DFRegion.LocationTypes.HomeFarms:
            //     case (int)DFRegion.LocationTypes.ReligionTemple:
            //     case (int)DFRegion.LocationTypes.Tavern:
            //     case (int)DFRegion.LocationTypes.HomeWealthy:
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.BarbarianStronghold);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.DesecratedTemple);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.DragonsDen);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.GiantStronghold);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.HarpyNest);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.NaturalCave);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.OrcStronghold);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.ScorpionNest);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.SpiderNest);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.VolcanicCaves);
            //         break;

            //     case (int)DFRegion.LocationTypes.ReligionCult:
            //     case (int)DFRegion.LocationTypes.HomePoor:
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.BarbarianStronghold);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.DragonsDen);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.GiantStronghold);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.HarpyNest);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.NaturalCave);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.ScorpionNest);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.SpiderNest);
            //         conflictingDungeon.Add((int)DFRegion.DungeonTypes.VolcanicCaves);
            //         break;

            //     default:
            //         break;
            // }

            return conflictingLocations;
        }

        protected ulong[] GenerateSurfaceArray(int index, int surface, out MapsFile.Climates[] surfaceClimates)
        {
            List<DFPosition> surfaceList = new List<DFPosition>();
            List<MapsFile.Climates> surfaceClimatesList = new List<MapsFile.Climates>();

            for (int i = 0; i < regionTiles[RegionManager.currentRegionIndex].Count; i++)
            {
                int[,] politicTile = LoadTile(TileType.Politic, regionTiles[RegionManager.currentRegionIndex][i]);
                for (int x = 0; x < BlockRotatorWindow.TILESIZE; x++)
                {
                    for (int y = 0; y < BlockRotatorWindow.TILESIZE; y++)
                    {
                        DFPosition pos = Worldmaps.GetDFPositionFromRelPosAndTileProg(x, y, regionTiles[RegionManager.currentRegionIndex][i]);
                        if (index == PoliticInfo.ConvertMapPixelToRegionIndex(pos.X, pos.Y))
                        {
                            Debug.Log("index: " + index + ", PoliticInfo.ConvertMapPixelToRegionIndex(pos.X, pos.Y): " + PoliticInfo.ConvertMapPixelToRegionIndex(pos.X, pos.Y) + ", x: " + pos.X + ", y: " + pos.Y);
                            surfaceList.Add(pos);
                            surfaceClimatesList.Add((MapsFile.Climates)ClimateInfo.ConvertMapPixelToClimate(pos.X, pos.Y));
                        }
                    }
                }
            }

            ulong[] surfaceRet = new ulong[surfaceList.Count];
            for (int f = 0; f < surfaceList.Count; f++)
                surfaceRet[f] = Worldmaps.ConvertDFPositionToMapId(surfaceList[f]);
            surfaceClimates = surfaceClimatesList.ToArray();
            return surfaceRet;
        }

        protected int[,] LoadTile(TileType tileType, int mapProg)
        {
            int x = mapProg % (BlockRotatorWindow.MAXXCOORD / BlockRotatorWindow.TILESIZE);
            int y = mapProg / (BlockRotatorWindow.MAXXCOORD / BlockRotatorWindow.TILESIZE);
            Texture2D textureTile = new Texture2D(BlockRotatorWindow.TILESIZE, BlockRotatorWindow.TILESIZE);
            string tilePref = string.Empty;

            switch (tileType)
            {
                case TileType.Climate:
                    tilePref = "climate_";
                    break;
                case TileType.Politic:
                    tilePref = "politic_";
                    break;
                case TileType.Heightmap:
                    tilePref = "woods_";
                    break;
            }

            string tileName = string.Concat(tilePref, x.ToString(), "_", y.ToString(), ".png");
            ImageConversion.LoadImage(textureTile, File.ReadAllBytes(Path.Combine(Worldmaps.tilesPath, tileName)));

            switch (tileType)
            {
                case TileType.Climate:
                    return ClimateInfo.ConvertToMatrix(textureTile);
                case TileType.Politic:
                    return PoliticInfo.ConvertToMatrix(textureTile);
                case TileType.Heightmap:
                    return SmallHeightmap.ConvertToMatrix(textureTile);
            }
            return new int[0, 0];
        }

        protected string GenerateHomeName(int locationType, int regionalism)
        {
            int selectedEpithet;
            int homeNameStructure;
            string epithet;
            string resultingName;

            int bankType = ConvertRegionalismToBankType(regionalism);
            int gender = UnityEngine.Random.Range(0, 2);

            switch (locationType)
            {
                case (int)DFRegion.LocationTypes.HomePoor:
                    selectedEpithet = UnityEngine.Random.Range(0, Enum.GetNames(typeof(LocationNamesList.PoorHomeEpithets)).Length);
                    epithet = ((LocationNamesList.PoorHomeEpithets)selectedEpithet).ToString();
                    break;

                case (int)DFRegion.LocationTypes.HomeWealthy:
                    selectedEpithet = UnityEngine.Random.Range(0, Enum.GetNames(typeof(LocationNamesList.WealthyHomeEpithets)).Length);
                    epithet = ((LocationNamesList.WealthyHomeEpithets)selectedEpithet).ToString();
                    break;

                case (int)DFRegion.LocationTypes.HomeFarms:
                    selectedEpithet = UnityEngine.Random.Range(0, Enum.GetNames(typeof(LocationNamesList.FarmsteadEpithets)).Length);
                    epithet = ((LocationNamesList.FarmsteadEpithets)selectedEpithet).ToString();
                    break;

                default:
                    epithet = "error";
                    break;
            }

            homeNameStructure = UnityEngine.Random.Range(0, 4);

            // Manors use only structures 1 and 2
            if (locationType == (int)DFRegion.LocationTypes.HomeWealthy)
            {
                if (homeNameStructure == 0) homeNameStructure++;
                if (homeNameStructure == 3) homeNameStructure--;
            }

            string name;
            string surname;

            switch (homeNameStructure)
            {
                case 0:     // The + surname + epithet
                    if (regionalism == (int)LocationNamesList.NameTypes.Hammerfell)
                        surname = GetName((int)NameType.FirstName, -1, regionalism);
                    else surname = GetName((int)NameType.Surname, -1, regionalism);

                    resultingName = string.Concat("The ", surname, " ", epithet);
                    break;

                case 1:     // surname + epithet
                    if (regionalism == (int)LocationNamesList.NameTypes.Hammerfell)
                        surname = GetName((int)NameType.FirstName, -1, regionalism);
                    else surname = GetName((int)NameType.Surname, -1, regionalism);

                    resultingName = string.Concat(surname, " ", epithet);
                    break;

                case 2:     // (Old or Lord/Lady) + name + 's + epithet
                    name = GetName((int)NameType.FirstName, gender, regionalism, true);

                    if (locationType == (int)DFRegion.LocationTypes.HomeWealthy)
                        resultingName = string.Concat(gender == 0 ? "Lord " : "Lady ", name, epithet);
                    else resultingName = string.Concat("Old ", name, epithet);
                    break;

                case 3:     // The Old + surname + epithet
                    if (regionalism == (int)LocationNamesList.NameTypes.Hammerfell)
                        surname = GetName((int)NameType.FirstName, -1, regionalism);
                    else surname = GetName((int)NameType.Surname, -1, regionalism);

                    resultingName = string.Concat("The Old ", surname, " ", epithet);
                    break;

                default:
                    resultingName = "error";
                    break;
            }

            return resultingName;
        }

        protected string GenerateDungeonName(int locationType, int regionalism, int dungeonType)
        {
            int selectedEpithet;
            int dungeonNameStructure;
            int title;
            int gender;
            int bankType;
            int nameType;
            string resultingName;
            int dungeonNameType;

            // It seems that I put a one out of ten chance to give the dungeon a random name. Removing it, since I think it could get confusing.
            // if (dungeonType == (int)DFRegion.DungeonTypes.Cemetery || (UnityEngine.Random.Range(0, 10) + 1 < 10))
            dungeonNameType = dungeonType;
            // else dungeonNameType = UnityEngine.Random.Range(0, (Enum.GetNames(typeof(DFRegion.DungeonTypes)).Length - 2));  // Excluding NoDungeon and cemeteries

            // Let's put a 1/100 chance that bankType will pick a random race name
            if (UnityEngine.Random.Range(0, 100) == 0)
            {
                int[] banks = { 0, 1, 2, 3, 4, 5, 6, 7, 11, 13 };
                bankType = banks[UnityEngine.Random.Range(0, banks.Length)];
            }
            else bankType = ConvertRegionalismToBankType(regionalism);

            // 0: The (name)'s (epithet);
            // 1: (title) (name)'s (epithet);
            // 2: The (epithet) of (name);
            // 3: The (epithet) of (title) (name);
            dungeonNameStructure = UnityEngine.Random.Range(0, 4);

            // In case it is needed, choose a title (Lord, Baron, etc...), but excluding "Emperor" and "Empress".
            title = UnityEngine.Random.Range(0, (int)RegionManager.Ruler.Emperor);

            // In case it is left to chance, choose a name type (First Name, Surname or Monster Name);
            nameType = UnityEngine.Random.Range(0, 3);

            // This should generate a random gender and, at the same time, make it coordinated with title
            gender = title % 2;

            selectedEpithet = UnityEngine.Random.Range(0, LocationNamesList.DungeonNamesList[dungeonType].Length);
            string eventualTitle = "";

            switch (dungeonNameType)
            {
                case (int)DFRegion.DungeonTypes.Crypt:

                    if (LocationNamesList.DungeonNamesList[(int)DFRegion.DungeonTypes.Cemetery].Contains(LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]))
                    {
                        if (LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].Equals("Graves"))
                            dungeonNameStructure = 1;

                        else if (LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].Equals("Crypts") ||
                                 LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].Equals("Tombs"))
                            dungeonNameStructure = UnityEngine.Random.Range(0, 2);

                        else dungeonNameStructure = 0;

                        switch (dungeonNameStructure)
                        {
                            case 0:     // The (family name) (epithet) - BG, Cemetery, Crypts, Graveyard, Tombs, Vaults
                                resultingName = string.Concat("The ", GetName((int)NameType.Surname, gender, bankType), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                                return resultingName;

                            case 1:     // The (epithet) of (family name) - Crypts, Graves, Tombs
                            default:
                                resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName((int)NameType.Surname, gender, bankType));
                                return resultingName;
                        }
                    }

                    // I'll change this so first names and surnames can be used too.
                    switch (dungeonNameStructure)
                    {
                        case 0:     // (any name)'s (epithet);
                            resultingName = string.Concat(GetName(nameType, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[(int)DFRegion.DungeonTypes.Crypt][selectedEpithet]);
                            return resultingName;

                        case 1:     // (title) (any name)'s (epithet);
                            resultingName = string.Concat(((RegionManager.Ruler)title).ToString(), " ", GetName(nameType, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[(int)DFRegion.DungeonTypes.Crypt][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (any name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, regionalism));
                            return resultingName;

                        case 3:     // The (epithet) of (title) (any name);
                        default:
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", ((RegionManager.Ruler)title).ToString(), " ", GetName(nameType, gender, regionalism));
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.OrcStronghold:
                case (int)DFRegion.DungeonTypes.HumanStronghold:

                    // If we're generating an Orc Stronghold, change the bankType to orc's
                    if (dungeonType == (int)DFRegion.DungeonTypes.OrcStronghold)
                        bankType = (int)LocationNamesList.NameTypes.Orsinium;

                    eventualTitle = "";
                    if (UnityEngine.Random.Range(0, 10) > 4)
                        eventualTitle = string.Concat(((RegionManager.Ruler)title).ToString(), " ");

                    // "Guard" can use only the last two structures;
                    if (LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].ToString().Equals("Guard"))
                        dungeonNameStructure += 2;

                    // Only "Castle" and "Tower" can use the first structure;
                    else if (!LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].ToString().Equals("Castle") ||
                             !LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].ToString().Equals("Tower"))
                        dungeonNameStructure += 1;

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (epithet) (surname)
                            resultingName = string.Concat(LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].ToString(), " ", GetName((int)NameType.Surname, gender, bankType));
                            return resultingName;

                        case 1:     // The (epithet) of [title] (name)
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].ToString(), " of ", eventualTitle, GetName((int)NameType.FirstName, gender, bankType));
                            return resultingName;

                        case 2:     // [title] (surname) (epithet)
                            resultingName = string.Concat(eventualTitle, GetName((int)NameType.Surname, gender, bankType), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].ToString());
                            return resultingName;

                        case 3:     // [title] (name)'s (epithet)
                        default:
                            resultingName = string.Concat(eventualTitle, GetName(0, gender, bankType, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.Prison:

                    switch (dungeonNameStructure)
                    {
                        case 0:     // The (surname) (epithet)
                            resultingName = string.Concat("The ", GetName((int)NameType.Surname, gender, bankType), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName((int)NameType.FirstName, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (nameORsurname);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName((int)NameType.NameORSurname, gender, regionalism));
                            return resultingName;

                        case 3:     // (name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName((int)NameType.FirstName, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.DesecratedTemple:

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, regionalism));
                            return resultingName;

                        case 3:     // (nameORMonsterName)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName((int)NameType.NameORMonsterName, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.Mine:

                    if ((LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].Equals("Excavation") ||
                         LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].Equals("Lode")) &&
                         nameType == 2)
                        nameType = 0;

                    switch (dungeonNameStructure)
                    {
                        case 0:
                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, regionalism));
                            return resultingName;

                        case 3:     // (firstORMonster)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName((int)NameType.NameORMonsterName, gender, regionalism), "'s ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.NaturalCave:

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                        case 3:
                        default:
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, regionalism));
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.Coven:

                    // For this type of dungeon, names are always first names and feminine.
                    nameType = 0;
                    gender = 0;

                    switch (dungeonNameStructure)
                    {
                        case 0:
                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, regionalism));
                            return resultingName;

                        case 3:     // (name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(nameType, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.VampireHaunt:

                    eventualTitle = "";
                    if (UnityEngine.Random.Range(0, 10) > 7)
                        eventualTitle = string.Concat(((RegionManager.Ruler)title).ToString(), " ");

                    switch (dungeonNameStructure)
                    {
                        case 0:     // [title] (name) (epithet);
                            resultingName = string.Concat(eventualTitle, GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of [title] (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", eventualTitle, GetName(nameType, gender, regionalism));
                            return resultingName;

                        case 3:     // [title] (name)'s (epithet);
                        default:
                            resultingName = string.Concat(eventualTitle, GetName(nameType, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.Laboratory:

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, regionalism));
                            return resultingName;

                        case 3:     // (name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(nameType, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.HarpyNest:

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, regionalism));
                            return resultingName;

                        case 3:     // (name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(nameType, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.RuinedCastle:

                    eventualTitle = "";
                    if (UnityEngine.Random.Range(0, 10) > 4)
                        eventualTitle = string.Concat(((RegionManager.Ruler)title).ToString(), " ");

                    switch (dungeonNameStructure)
                    {
                        case 0:     // [Title] (name) (epithet);
                            resultingName = string.Concat(eventualTitle, GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of [title] (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", eventualTitle, GetName(nameType, gender, regionalism));
                            return resultingName;

                        case 3:     // [title] (name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(nameType, gender, regionalism), "'s ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.SpiderNest:

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName((int)NameType.MonsterName, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName((int)NameType.MonsterName, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName((int)NameType.MonsterName, gender, regionalism));
                            return resultingName;

                        case 3:     // (name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName((int)NameType.MonsterName, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.GiantStronghold:

                    switch (dungeonNameStructure)
                    {
                        case 0:     // The (epithet) of (name)
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet].ToString(), " of ", GetName((int)NameType.MonsterName, gender, bankType));
                            return resultingName;
                        case 1:     // (name)'s (epithet)
                            resultingName = string.Concat(GetName((int)NameType.MonsterName, gender, bankType, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                        default:
                            resultingName = GetName((int)NameType.MonsterName, gender, bankType);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.DragonsDen:

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName((int)NameType.MonsterName, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName((int)NameType.MonsterName, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName((int)NameType.MonsterName, gender, regionalism));
                            return resultingName;

                        case 3:     // (male name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName((int)NameType.MonsterName, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.BarbarianStronghold:

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName((int)NameType.FirstName, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName((int)NameType.FirstName, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName((int)NameType.FirstName, gender, regionalism));
                            return resultingName;

                        case 3:     // (male name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName((int)NameType.FirstName, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.VolcanicCaves:

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName((int)NameType.MonsterName, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName((int)NameType.MonsterName, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName((int)NameType.MonsterName, gender, regionalism));
                            return resultingName;

                        case 3:     // (male name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName((int)NameType.MonsterName, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.ScorpionNest:

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName((int)NameType.MonsterName, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName((int)NameType.MonsterName, gender, regionalism), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet], " of ", GetName((int)NameType.MonsterName, gender, regionalism));
                            return resultingName;

                        case 3:     // (male name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName((int)NameType.MonsterName, gender, regionalism, true), " ", LocationNamesList.DungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                default:
                    selectedEpithet = UnityEngine.Random.Range(0, LocationNamesList.DungeonNamesList[DFRegion.DungeonTypeNames.Length].Length);
                    if (((LocationNamesList.DungeonNamesList[DFRegion.DungeonTypeNames.Length][selectedEpithet]).ToString()).Equals("Ruins"))
                    {
                        resultingName = GenerateRuinsName(regionalism, gender, dungeonType);
                        return resultingName;
                    }
                    else
                    {
                        resultingName = GenerateGenericDungeonName(regionalism, gender, dungeonType);
                        return resultingName;
                    }
            }
        }

        protected string GenerateRuinsName(int regionalism, int gender, int dungeonType)
        {
            // 0: Ruins of (home name);
            // 1: Ruins of (generic dungeon);
            int nameStructure = UnityEngine.Random.Range(0, 2);

            switch (nameStructure)
            {
                case 0:
                    int homeType = UnityEngine.Random.Range(0, 3);

                    switch (homeType)
                    {
                        case 0:
                            return string.Concat("Ruins of ", GenerateHomeName((int)DFRegion.LocationTypes.HomeFarms, regionalism));

                        case 1:
                            return string.Concat("Ruins of ", GenerateHomeName((int)DFRegion.LocationTypes.HomeWealthy, regionalism));

                        case 2:
                        default:
                            return string.Concat("Ruins of ", GenerateHomeName((int)DFRegion.LocationTypes.HomePoor, regionalism));
                    }

                case 1:
                default:
                    return string.Concat("Ruins of ", GenerateGenericDungeonName(regionalism, gender, dungeonType));
            }
        }

        protected string GenerateGenericDungeonName(int regionalism, int gender, int dungeonType)
        {
            int selectedEpithet = UnityEngine.Random.Range(0, LocationNamesList.DungeonNamesList[genericDungeonEpithets].Length);
            int nameStructure = UnityEngine.Random.Range(0, 4);
            string resultingName;

            if ((LocationNamesList.DungeonNamesList[genericDungeonEpithets][selectedEpithet].ToString()).Equals("Tower") &&
                 nameStructure > 3)
                nameStructure = 3;

            else if ((LocationNamesList.DungeonNamesList[genericDungeonEpithets][selectedEpithet].ToString()).Equals("Citadel") ||
                (LocationNamesList.DungeonNamesList[genericDungeonEpithets][selectedEpithet].ToString()).Equals("Fortress") ||
                (LocationNamesList.DungeonNamesList[genericDungeonEpithets][selectedEpithet].ToString()).Equals("Stronghold"))
                nameStructure = 2;

            else if ((LocationNamesList.DungeonNamesList[genericDungeonEpithets][selectedEpithet].ToString()).Equals("Guard"))
                nameStructure = 3;

            else if ((LocationNamesList.DungeonNamesList[genericDungeonEpithets][selectedEpithet].ToString()).Equals("Hold") &&
                      nameStructure < 2)
                nameStructure++;

            switch (nameStructure)
            {
                case 0:     // (family name) (epithet) - tower
                    resultingName = string.Concat((LocationNamesList.DungeonNamesList[genericDungeonEpithets][selectedEpithet]), " ", GetName(1, gender, regionalism));
                    return resultingName;

                case 1:     // (epithet) (family name) - castle, tower
                    resultingName = string.Concat(LocationNamesList.DungeonNamesList[genericDungeonEpithets][selectedEpithet], " ", GetName(1, gender, regionalism));
                    return resultingName;

                case 2:     // The (epithet) of (family name) - castle, citadel, fortress, hold, stronghold, tower
                    resultingName = string.Concat("The ", LocationNamesList.DungeonNamesList[genericDungeonEpithets][selectedEpithet], " of ", GetName(1, gender, regionalism));
                    return resultingName;

                case 3:     // (family name)'s (epithet) - guard, hold
                default:
                    resultingName = string.Concat(GetName(1, gender, regionalism), "'s ", LocationNamesList.DungeonNamesList[genericDungeonEpithets][selectedEpithet]);
                    return resultingName;
            }
        }

        /// <summary>
        /// Provides a random name.
        /// </summary>
        /// <param name="nameType">The kind of name needed:
        /// 0: First Name;
        /// 1: Surname;
        /// 2: Monster Name.</param>
        /// <param name="gender">Gender selected for the name, when relevant.</param>
        /// <param name="regionalism">The name origin.</param>
        protected string GetName(int nameType, int gender = -1, int regionalism = 0, bool saxGen = false)
        {
            int bankType = ConvertRegionalismToBankType(regionalism);
            string result = "";
            if (gender == -1) gender = UnityEngine.Random.Range(0, 2);

            if (nameType > 2)
            {
                int chance = UnityEngine.Random.Range(0, 2);
                switch (nameType)
                {
                    case 3:     // name or surname
                        nameType = chance;
                        break;
                    case 4:     // name or monster name
                        if (chance == 0) nameType = 0;
                        else nameType = 2;
                        break;
                    case 5:     // surname or monster name
                        nameType = chance + 1;
                        break;
                    default:
                        nameType = 0;
                        Debug.Log("Wrong nameType value of " + nameType + " in GetName()");
                        break;
                }
            }

            switch (nameType)
            {
                case 0:
                    result = DaggerfallUnity.Instance.NameHelper.FirstName((NameHelper.BankTypes)bankType, (DaggerfallWorkshop.Game.Entity.Genders)gender);
                    if (saxGen)
                    {
                        if (result.EndsWith("s")) return result + "'";
                        else return result + "'s";
                    }
                    return result;

                case 1:
                    if (bankType != (int)NameHelper.BankTypes.Redguard)
                        result = DaggerfallUnity.Instance.NameHelper.Surname((NameHelper.BankTypes)bankType, gender);
                    else result = DaggerfallUnity.Instance.NameHelper.FirstName((NameHelper.BankTypes)bankType, (DaggerfallWorkshop.Game.Entity.Genders)gender);
                    if (saxGen)
                    {
                        if (result.EndsWith("s")) return result + "'";
                        else return result + "'s";
                    }
                    return result;

                case 2:
                    result = DaggerfallUnity.Instance.NameHelper.MonsterName((DaggerfallWorkshop.Game.Entity.Genders)gender);
                    if (saxGen)
                    {
                        if (result.EndsWith("s")) return result + "'";
                        else return result + "'s";
                    }
                    return result;

                default:
                    return "GetName() error";
            }
        }

        protected string GenerateTempleName(int locationType)
        {
            int relativeDeity = UnityEngine.Random.Range(0, 10);
            int adjectif = UnityEngine.Random.Range(0, LocationNamesList.TempleNamesList[(int)LocationNamesList.TempleParts.Noun].Length);
            int noun = UnityEngine.Random.Range(0, LocationNamesList.TempleNamesList[(int)LocationNamesList.TempleParts.Noun].Length);
            int deity = 0;

            if (locationType == (int)DFRegion.LocationTypes.ReligionTemple)
            {
                // One out of ten chances that a temple won't be dedicated to the official region deity
                if (relativeDeity < 9) // I would have made it "< 8", Jehuty suggested to make it less frequent
                    deity = ConvertReligionToDeity(WorldInfo.WorldSetting.regionTemples[RegionManager.currentRegionIndex]);
                else
                {
                    do
                    {
                        deity = UnityEngine.Random.Range(0, LocationNamesList.TempleNamesList[(int)LocationNamesList.TempleParts.Deity].Length);
                    }
                    while (deity == ConvertReligionToDeity(WorldInfo.WorldSetting.regionTemples[RegionManager.currentRegionIndex]));
                }
            }
            else
                deity = UnityEngine.Random.Range(0, LocationNamesList.TempleNamesList[(int)LocationNamesList.ShrineParts.Deity].Length);

            switch (locationType)
            {
                case (int)DFRegion.LocationTypes.ReligionTemple:
                    return string.Concat(LocationNamesList.TempleNamesList[(int)LocationNamesList.TempleParts.Adjectif][adjectif], " ",
                                         LocationNamesList.TempleNamesList[(int)LocationNamesList.TempleParts.Noun][noun], " of ",
                                         LocationNamesList.TempleNamesList[(int)LocationNamesList.TempleParts.Deity][deity]);

                case (int)DFRegion.LocationTypes.ReligionCult:
                    int randomness = UnityEngine.Random.Range(0, 5);
                    if (randomness == 0)
                    {
                        if (!LocationNamesList.TempleNamesList[(int)LocationNamesList.ShrineParts.SpecificShrine][deity].Equals(""))
                            return LocationNamesList.TempleNamesList[(int)LocationNamesList.ShrineParts.SpecificShrine][deity];
                        else randomness += UnityEngine.Random.Range(1, 5);
                    }
                    string godName = GetMinorDeityNameVersion(deity);
                    if (randomness < 3)
                    {
                        if (godName.StartsWith("The")) godName = string.Concat("the", godName.Substring(3));
                        return string.Concat(LocationNamesList.TempleNamesList[(int)LocationNamesList.ShrineParts.Prefix][UnityEngine.Random.Range(0, LocationNamesList.TempleNamesList[(int)LocationNamesList.ShrineParts.Prefix].Length)], godName);
                    }
                    else return string.Concat(godName, LocationNamesList.TempleNamesList[(int)LocationNamesList.ShrineParts.Suffix][UnityEngine.Random.Range(0, LocationNamesList.TempleNamesList[(int)LocationNamesList.ShrineParts.Suffix].Length)]);

                default:
                    return "Error while generating temple name";
            }
        }

        // TODO: make the function work with any eventual addition to the list of minor deities
        protected string GetMinorDeityNameVersion(int deityProg)
        {
            int randomness = UnityEngine.Random.Range(0, int.MaxValue);
            string wholeString = LocationNamesList.TempleNamesList[(int)LocationNamesList.ShrineParts.Deity][deityProg];
            switch (deityProg)
            {
                case 0:         // I'ric
                case 4:         // Divad
                case 18:
                    // Chice is between first part only or whole
                    if (randomness % 2 == 0) return wholeString.Substring(0, wholeString.IndexOf("*"));
                    else return wholeString.Replace("*", " ");
                case 1:         // Ebonarm
                    // Ebonarm
                    if (randomness % 3 == 0) return wholeString.Substring(wholeString.IndexOf("*") + 1, 7);
                    // Reymon Ebonarm
                    else if (randomness % 3 == 1) return wholeString.Substring(0, wholeString.LastIndexOf("*") - 1).Replace("*", " ");
                    // The Black Knight
                    else return wholeString.Substring(wholeString.LastIndexOf("*") + 1);
                case 5:         // Sage
                    // Sage
                    if (randomness % 2 == 0) return wholeString.Substring(wholeString.LastIndexOf("*") + 1);
                    // The Great Sage
                    else return wholeString.Replace("*", " ");
                case 7:         // Sai
                case 10:        // Jode
                case 11:        // Bandi
                case 13:        // Ephen
                case 17:        // Vigryl
                    if (randomness % 2 == 0) return wholeString.Substring(0, wholeString.IndexOf("*"));
                    else return wholeString.Substring(wholeString.IndexOf("*") + 1);
                case 14:        // Jephre
                    // Jephre
                    if (randomness % 4 == 0) return wholeString.Substring(0, wholeString.IndexOf("*"));
                    // Y'ffre
                    else if (randomness % 4 == 1) return wholeString.Substring(wholeString.IndexOf("*") + 1, 6);
                    // Y'frre
                    else if (randomness % 4 == 2) return wholeString.Substring(wholeString.IndexOf("*", wholeString.IndexOf("*") + 1) + 1, 6);
                    // Y'ffer
                    else return wholeString.Substring(wholeString.LastIndexOf("*") + 1);
                default:
                    return wholeString;
            }
        }

        protected int ConvertRegionalismToBankType(int regionalism)
        {
            switch (regionalism)
            {
                case (int)LocationNamesList.NameTypes.HighRock:
                    if (UnityEngine.Random.Range(0, 3) != 0)
                        return (int)NameHelper.BankTypes.BretonModern;
                    return (int)NameHelper.BankTypes.Breton;

                case (int)LocationNamesList.NameTypes.Hammerfell:
                    return (int)NameHelper.BankTypes.Redguard;

                case (int)LocationNamesList.NameTypes.Skyrim:
                case (int)LocationNamesList.NameTypes.Reachmen:
                    return (int)NameHelper.BankTypes.Nord;

                case (int)LocationNamesList.NameTypes.Morrowind:
                    return (int)NameHelper.BankTypes.DarkElf;

                case (int)LocationNamesList.NameTypes.Cyrodiil:
                    return (int)NameHelper.BankTypes.Imperial;

                case (int)LocationNamesList.NameTypes.SumursetIsle:
                    return (int)NameHelper.BankTypes.HighElf;

                case (int)LocationNamesList.NameTypes.Valenwood:
                    return (int)NameHelper.BankTypes.WoodElf;

                case (int)LocationNamesList.NameTypes.Elsweyr:
                    return (int)NameHelper.BankTypes.Khajiit;

                case (int)LocationNamesList.NameTypes.BlackMarsh:
                    return (int)NameHelper.BankTypes.Argonian;

                case (int)LocationNamesList.NameTypes.Orsinium:
                    return (int)NameHelper.BankTypes.Orc;

                default:
                    return (int)NameHelper.BankTypes.Breton;
            }
        }

        protected string ConvertTempleNameToLetter(string templeName)
        {
            for (int i = 0; i < BlockRotatorWindow.deityCode.Length; i++)
            {
                if (templeName.Contains(BlockRotatorWindow.deityNames[i]))
                    return BlockRotatorWindow.deityCode[i];
            }
            return string.Empty;
        }

        protected int ConvertReligionToDeity(int religion)
        {
            switch (religion)
            {
                case 36:        // Kynareth
                    return 4;

                case 82:        // Arkay
                    return 1;

                case 84:        // Zenithar
                    return 7;

                case 88:        // Mara
                    return 5;

                case 92:        // Akatosh
                    return 0;

                case 94:        // Julianos
                    return 3;

                case 98:        // Dibella
                    return 2;

                case 106:       // Stendarr
                    return 6;

                default:
                    return -1;
            }
        }

        protected DFLocation GenerateLocation(int locationType, DFPosition position, int dungeonType, string locationName, ref List<string>[] presentNames, bool isCapital = false)
        {
            DFLocation generatedLocation = new DFLocation();

            Debug.Log("Generating location type " + (DFRegion.LocationTypes)locationType + " with coordinates " + position.X + ", " + position.Y + " (isCapital: " + isCapital + ")");

            switch (locationType)
            {
                case (int)DFRegion.LocationTypes.TownCity:
                case (int)DFRegion.LocationTypes.TownHamlet:
                case (int)DFRegion.LocationTypes.TownVillage:
                    generatedLocation = GenerateTown(locationType, position, locationName, isCapital);
                    break;

                case (int)DFRegion.LocationTypes.DungeonLabyrinth:
                case (int)DFRegion.LocationTypes.DungeonKeep:
                case (int)DFRegion.LocationTypes.DungeonRuin:
                case (int)DFRegion.LocationTypes.DungeonMinor:
                    generatedLocation = GenerateDungeon(locationType, position, dungeonType, locationName);
                    break;

                case (int)DFRegion.LocationTypes.HomeFarms:
                case (int)DFRegion.LocationTypes.Tavern:
                case (int)DFRegion.LocationTypes.HomeWealthy:
                case (int)DFRegion.LocationTypes.HomePoor:
                    generatedLocation = GenerateHome(locationType, position, locationName, ref presentNames);
                    break;

                case (int)DFRegion.LocationTypes.ReligionCult:
                case (int)DFRegion.LocationTypes.ReligionTemple:
                    generatedLocation = GenerateTemple(locationType, position, locationName);
                    break;
            }

            return generatedLocation;
        }

        protected DFLocation GenerateTown(int locationType, DFPosition position, string townName, bool isCapital = false)
        {
            DFLocation generatedLocation = new DFLocation();

            generatedLocation.Loaded = true;
            generatedLocation.RegionName = WorldInfo.WorldSetting.RegionNames[RegionManager.currentRegionIndex];
            generatedLocation.HasDungeon = false;

            (int, int, int) townShape;

            GenerateTownSize(locationType, out townShape, isCapital);

            generatedLocation.MapTableData.MapId = (ulong)(position.Y * MapsFile.MaxMapPixelX + position.X);
            generatedLocation.MapTableData.Latitude = SetLocationLongitudeLatitude(position.X, position.Y, townShape.Item1, townShape.Item2, false);
            generatedLocation.MapTableData.Longitude = SetLocationLongitudeLatitude(position.X, position.Y, townShape.Item1, townShape.Item2, true);
            generatedLocation.MapTableData.LocationType = (DFRegion.LocationTypes)locationType;
            generatedLocation.MapTableData.DungeonType = DFRegion.DungeonTypes.NoDungeon;
            generatedLocation.MapTableData.Discovered = false;
            generatedLocation.MapTableData.Key = 0;
            generatedLocation.MapTableData.LocationId = GenerateNewLocationId();

            generatedLocation.Exterior.RecordElement.Header.X = SetPixelXY(position.X, position.Y, townShape.Item1, townShape.Item2, true);
            generatedLocation.Exterior.RecordElement.Header.Y = SetPixelXY(position.X, position.Y, townShape.Item1, townShape.Item2, false);
            generatedLocation.Exterior.RecordElement.Header.IsExterior = 32768;
            generatedLocation.Exterior.RecordElement.Header.Unknown2 = 0;
            generatedLocation.Exterior.RecordElement.Header.LocationId = generatedLocation.MapTableData.LocationId;
            generatedLocation.Exterior.RecordElement.Header.IsInterior = 0;
            generatedLocation.Exterior.RecordElement.Header.ExteriorLocationId = generatedLocation.Exterior.RecordElement.Header.LocationId;
            generatedLocation.Name = generatedLocation.Exterior.RecordElement.Header.LocationName = generatedLocation.Exterior.ExteriorData.AnotherName = townName;

            generatedLocation.Exterior.ExteriorData.MapId = generatedLocation.MapTableData.MapId;
            generatedLocation.Exterior.ExteriorData.LocationId = generatedLocation.MapTableData.LocationId;
            generatedLocation.Exterior.ExteriorData.Width = (byte)townShape.Item1;
            generatedLocation.Exterior.ExteriorData.Height = (byte)townShape.Item2;

            if (isCapital)
                if (regionOptions.capitalPort)
                {
                    generatedLocation.Exterior.ExteriorData.PortTownAndUnknown = (byte)TownAttributes.Port & (byte)TownAttributes.Capital;
                }
                else generatedLocation.Exterior.ExteriorData.PortTownAndUnknown = (byte)TownAttributes.Capital;
            else if (locationType != (int)DFRegion.LocationTypes.TownVillage)
                generatedLocation.Exterior.ExteriorData.PortTownAndUnknown = DetermineIfPortTown(position.X, position.Y);
            else generatedLocation.Exterior.ExteriorData.PortTownAndUnknown = 0;

            generatedLocation.Exterior.ExteriorData.BlockNames = new string[townShape.Item1 * townShape.Item2];

            if (locationType == (int)DFRegion.LocationTypes.TownCity && (DetermineIfWalled(townShape.Item1, townShape.Item2) || (isCapital && regionOptions.capitalWalled)))
            {
                string specialCondition = "";
                if (isCapital)
                    specialCondition = "capital";
                generatedLocation.Exterior.ExteriorData.Width += 2;
                generatedLocation.Exterior.ExteriorData.Height += 2;
                townShape.Item1 += 2;
                townShape.Item2 += 2;
                generatedLocation.Exterior.ExteriorData.BlockNames = new string[generatedLocation.Exterior.ExteriorData.Width * generatedLocation.Exterior.ExteriorData.Height];
                generatedLocation.Exterior.ExteriorData.BlockNames = PickBlockNames(locationType, townShape, generatedLocation.Exterior.ExteriorData.BlockNames.Length, position, true, specialCondition);
            }
            else generatedLocation.Exterior.ExteriorData.BlockNames = PickBlockNames(locationType, townShape, generatedLocation.Exterior.ExteriorData.BlockNames.Length, position);

            // generatedLocation.Exterior.BuildingCount = (ushort)CountBuildings(generatedLocation.Exterior.ExteriorData.BlockNames);

            generatedLocation.Exterior.Buildings = BlockRotatorWindow.RegenerateBuildingsArray(
                                                    generatedLocation.Exterior.ExteriorData.BlockNames,
                                                    RegionManager.currentRegionIndex,
                                                    townName,
                                                    (DFRegion.LocationTypes)locationType,
                                                    out generatedLocation.Exterior.BuildingCount,
                                                    out generatedLocation.MapTableData.Key);

            generatedLocation.Dungeon = new DFLocation.LocationDungeon();
            generatedLocation.Climate = MapsFile.GetWorldClimateSettings(ClimateInfo.Climate[position.X, position.Y]);
            generatedLocation.Politic = PoliticInfo.Politic[position.X, position.Y];
            generatedLocation.RegionIndex = RegionManager.currentRegionIndex;
            generatedLocation.LocationIndex = 0; // TODO

            return generatedLocation;
        }

        public static uint GetPartialKey(DFLocation.BuildingData building)
        {
            switch (building.BuildingType)
            {
                case DFLocation.BuildingTypes.Alchemist:
                    return 2048;
                case DFLocation.BuildingTypes.Armorer:
                    return 1024;
                case DFLocation.BuildingTypes.Bank:
                    return 4096;
                case DFLocation.BuildingTypes.Bookseller:
                    return 8192;
                case DFLocation.BuildingTypes.ClothingStore:
                    return 16384;
                case DFLocation.BuildingTypes.GemStore:
                    return 32768;
                case DFLocation.BuildingTypes.Library:
                    return 65536;
                case DFLocation.BuildingTypes.PawnShop:
                    return 4194304;
                case DFLocation.BuildingTypes.Tavern:
                    return 256;
                case DFLocation.BuildingTypes.WeaponSmith:
                    return 512;
                case DFLocation.BuildingTypes.Temple:
                    switch (building.FactionId)
                    {
                        case 82:
                            return 2;
                        case 84:
                            return 128;
                        case 88:
                            return 32;
                        case 92:
                            return 1;
                        case 94:
                            return 8;
                        case 98:
                            return 4;
                        case 106:
                            return 64;
                        case 36:
                            return 16;
                        default:
                            return 0;
                    }
                case DFLocation.BuildingTypes.GuildHall:
                    switch (building.FactionId)
                    {
                        case 40:
                            return 262144;
                        case 41:
                            return 2097152;
                        case 368:
                        case 408:
                        case 409:
                        case 410:
                        case 411:
                        case 413:
                        case 414:
                        case 415:
                        case 416:
                        case 417:
                            return 131072;
                        default:
                            return 0;
                    }
                default:
                    return 0;
            }
        }

        protected void GenerateTownSize(int locationType, out (int, int, int) refinedSize, bool isCapital = false)
        {
            int scalableWidth;
            int scalableHeight;
            int width;
            int height;

            scalableWidth = UnityEngine.Random.Range(0, 100) + 1;
            scalableHeight = UnityEngine.Random.Range(0, 100) + 1;

            if (isCapital)
            {
                if (regionOptions.capitalSize.Item1 == null || regionOptions.capitalSize.Item2 == null)
                {
                    width = ElaborateScalableSize(locationType, scalableWidth);
                    height = ElaborateScalableSize(locationType, scalableHeight);
                }
                else
                {
                    width = regionOptions.capitalSize.Item1;
                    height = regionOptions.capitalSize.Item2;
                }
            }
            else
            {
                width = ElaborateScalableSize(locationType, scalableWidth);
                height = ElaborateScalableSize(locationType, scalableHeight);
            }

            switch (locationType)
            {
                case (int)DFRegion.LocationTypes.TownVillage:
                    refinedSize = RefineVillageSize(width, height);
                    break;

                case (int)DFRegion.LocationTypes.TownHamlet:
                    refinedSize = RefineHamletSize(width, height);
                    break;

                case (int)DFRegion.LocationTypes.TownCity:
                default:
                    refinedSize = (width, height, (int)TownShapes.Regular);
                    break;
            }
        }

        protected (int, int, int) RefineHamletSize(int width, int height)
        {
            int randomChance = UnityEngine.Random.Range(0, 100) + 1;
            switch (width * height)
            {
                case 4:     // 5 blocks is the smallest hamlet possible
                    if (randomChance <= 50)
                        width++;
                    else height++;

                    return (width, height, (int)TownShapes.FiveOnSix);

                case 6:     // There's a small chance a 6 blocks hamlet will have a peculiar shape
                    if (randomChance > 95)
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0)
                            width++;
                        else height++;

                        if (Math.Abs(width - height) == 2)
                            return (width, height, (int)TownShapes.SixOnEight);
                        else return (width, height, (int)TownShapes.MantaShape);
                    }
                    else return (width, height, (int)TownShapes.Regular);

                case 8:     // 4 x 2 blocks hamlet should be rare, 4 out of 5 times it's a 3 x 3 with a FILL
                    if (randomChance <= 80)
                    {
                        return (3, 3, (int)TownShapes.EightOnNine);
                    }
                    else return (width, height, (int)TownShapes.Regular);

                case 9:     // 3 x 3 blocks hamlet are usually just square towns, but there's a small chance it will be a 4 x 3 with 3 FILL
                    if (randomChance <= 80)
                    {
                        return (3, 3, (int)TownShapes.Regular);
                    }
                    else
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0)
                            width++;
                        else height++;

                        return (width, height, (int)TownShapes.NineOnTwelve);
                    }

                case 12:    // 3 x 4 blocks hamlet have a small chance to be wider, but with 4 FILL
                    if (randomChance <= 80)
                    {
                        return (width, height, (int)TownShapes.Regular);
                    }
                    else return (4, 4, (int)TownShapes.TwelveOnSixteen);

                case 16:    // max blocks for hamlets are 12, therefore a 4 x 4 hamlet has always 4 FILL
                    return (width, height, (int)TownShapes.TwelveOnSixteen);

                default:
                    return (width, height, (int)TownShapes.Regular);
            }
        }

        protected (int, int, int) RefineVillageSize(int width, int height)
        {
            switch (width * height)
            {
                case 1:     // 1 x 1 settlements are only homes
                    if (0 == UnityEngine.Random.Range(0, 2))
                        width++;
                    else height++;

                    return (width, height, (int)TownShapes.Regular);

                case 3:     // 3 x 1 settlements should be rare, 4 out of 5 times it's a 2 x 2 with a FILL
                    if (UnityEngine.Random.Range(0, 5) < 4)
                    {
                        return (2, 2, (int)TownShapes.LShape);
                    }
                    else return (width, height, (int)TownShapes.Regular);

                case 6:     // 4 should be the top limit blocks for villages
                    return (width, height, (int)TownShapes.FourOnSix);

                case 9:     // 3 x 3 is reduced to a 3 x 2, as above
                    if (UnityEngine.Random.Range(0, 2) == 0)
                        width--;
                    else height--;

                    return (width, height, (int)TownShapes.FourOnSix);

                default:
                    return (width, height, (int)TownShapes.Regular);
            }
        }

        protected int ElaborateScalableSize(int locationType, int scalableValue)
        {
            switch (locationType)
            {
                // TODO: vanilla cities are present even with a size of 5x7 and 7x5 (walls included).
                // Take that into consideration (maybe for smaller regions?)
                case (int)DFRegion.LocationTypes.TownCity:
                    if (scalableValue <= 80)
                        return 4;
                    else if (scalableValue <= 95)
                        return 5;
                    else return 6;

                case (int)DFRegion.LocationTypes.TownHamlet:
                    if (scalableValue <= 60)
                        return 2;
                    else if (scalableValue <= 90)
                        return 3;
                    else return 4;

                case (int)DFRegion.LocationTypes.TownVillage:
                    if (scalableValue <= 60)
                        return 1;
                    else if (scalableValue <= 90)
                        return 2;
                    else return 3;

                default:
                    return 0;
            }
        }

        protected DFLocation GenerateDungeon(int locationType, DFPosition position, int dungeonType, string dungeonName)
        {
            DFLocation generatedLocation = new DFLocation();

            generatedLocation.Loaded = true;
            generatedLocation.RegionName = WorldInfo.WorldSetting.RegionNames[RegionManager.currentRegionIndex];
            generatedLocation.HasDungeon = true;

            generatedLocation.MapTableData.MapId = (ulong)(position.Y * MapsFile.MaxMapPixelX + position.X);
            generatedLocation.MapTableData.Latitude = SetLocationLongitudeLatitude(position.X, position.Y, 1, 1, false);
            generatedLocation.MapTableData.Longitude = SetLocationLongitudeLatitude(position.X, position.Y, 1, 1, true);
            generatedLocation.MapTableData.LocationType = (DFRegion.LocationTypes)locationType;
            generatedLocation.MapTableData.DungeonType = (DFRegion.DungeonTypes)dungeonType;
            generatedLocation.MapTableData.Discovered = false;
            generatedLocation.MapTableData.Key = 0;
            generatedLocation.MapTableData.LocationId = GenerateNewLocationId();

            generatedLocation.Exterior.RecordElement.Header.X = SetPixelXY(position.X, position.Y, 1, 1, true);
            generatedLocation.Exterior.RecordElement.Header.Y = SetPixelXY(position.X, position.Y, 1, 1, false);
            generatedLocation.Exterior.RecordElement.Header.IsExterior = 32768;
            generatedLocation.Exterior.RecordElement.Header.Unknown2 = 0;
            generatedLocation.Exterior.RecordElement.Header.LocationId = generatedLocation.MapTableData.LocationId;
            generatedLocation.Exterior.RecordElement.Header.IsInterior = 0;
            generatedLocation.Exterior.RecordElement.Header.ExteriorLocationId = generatedLocation.Exterior.RecordElement.Header.LocationId;
            generatedLocation.Name = generatedLocation.Exterior.RecordElement.Header.LocationName = generatedLocation.Exterior.ExteriorData.AnotherName = dungeonName;

            generatedLocation.Exterior.ExteriorData.MapId = generatedLocation.MapTableData.MapId;
            generatedLocation.Exterior.ExteriorData.LocationId = generatedLocation.MapTableData.LocationId;
            generatedLocation.Exterior.ExteriorData.Width = 1;
            generatedLocation.Exterior.ExteriorData.Height = 1;

            generatedLocation.Exterior.ExteriorData.PortTownAndUnknown = 0;

            generatedLocation.Exterior.ExteriorData.BlockNames = new string[1];
            generatedLocation.Exterior.ExteriorData.BlockNames = PickDungeonExterior(locationType, position, dungeonType, dungeonName);

            // Using this because some new dungeon exteriors have buildings, and to get the "key" value,
            // that in dungeons equals to number of entrances - 1;
            generatedLocation.Exterior.Buildings = BlockRotatorWindow.RegenerateBuildingsArray(
                                                    generatedLocation.Exterior.ExteriorData.BlockNames,
                                                    RegionManager.currentRegionIndex,
                                                    dungeonName,
                                                    (DFRegion.LocationTypes)locationType,
                                                    out generatedLocation.Exterior.BuildingCount,
                                                    out generatedLocation.MapTableData.Key);

            generatedLocation.Dungeon.RecordElement.Header.X = generatedLocation.Exterior.RecordElement.Header.X;
            generatedLocation.Dungeon.RecordElement.Header.Y = generatedLocation.Exterior.RecordElement.Header.Y;
            generatedLocation.Dungeon.RecordElement.Header.IsExterior = 0;
            generatedLocation.Dungeon.RecordElement.Header.Unknown2 = 0;
            generatedLocation.Dungeon.RecordElement.Header.LocationId = (ushort)(generatedLocation.Exterior.RecordElement.Header.LocationId + 1);
            generatedLocation.Dungeon.RecordElement.Header.ExteriorLocationId = generatedLocation.Exterior.RecordElement.Header.LocationId;
            generatedLocation.Dungeon.RecordElement.Header.LocationName = generatedLocation.Name;

            generatedLocation.Dungeon.Blocks = GenerateDungeonInterior(locationType, dungeonType, dungeonName);
            Debug.Log("Dungeon: " + dungeonName + "; Blocks generated: " + generatedLocation.Dungeon.Blocks.Length);
            generatedLocation.Dungeon.Header.BlockCount = (ushort)generatedLocation.Dungeon.Blocks.Length;

            generatedLocation.Climate = MapsFile.GetWorldClimateSettings(ClimateInfo.Climate[position.X, position.Y]);
            generatedLocation.Politic = PoliticInfo.Politic[position.X, position.Y];
            generatedLocation.RegionIndex = RegionManager.currentRegionIndex;
            generatedLocation.LocationIndex = 0; // TODO

            return generatedLocation;
        }

        protected DFLocation GenerateFarm(int locationType, Farm randomFarm, string homeName, int climateNumb)
        {
            DFLocation generatedLocation = new DFLocation();

            generatedLocation.Loaded = true;
            generatedLocation.RegionName = randomFarm.region;
            generatedLocation.HasDungeon = false;

            int farmSize = UnityEngine.Random.Range(1, 11);
            if (farmSize < 6)
                farmSize = 1;
            else if (farmSize < 9)
                farmSize = 3;
            else farmSize = 5;

            generatedLocation.MapTableData.MapId = (ulong)(randomFarm.worldY * 1000 + randomFarm.worldX);
            generatedLocation.MapTableData.Latitude = (499 - randomFarm.worldY) * 128;
            generatedLocation.MapTableData.Longitude = randomFarm.worldX * 128;
            generatedLocation.MapTableData.LocationType = DFRegion.LocationTypes.HomeFarms;
            generatedLocation.MapTableData.DungeonType = DFRegion.DungeonTypes.NoDungeon;
            generatedLocation.MapTableData.Discovered = false;
            generatedLocation.MapTableData.Key = 0;
            generatedLocation.MapTableData.LocationId = GenerateNewLocationId();

            generatedLocation.Exterior.RecordElement.Header.X = SetPixelXY(randomFarm.worldX, randomFarm.worldY, farmSize, farmSize, true);
            generatedLocation.Exterior.RecordElement.Header.Y = SetPixelXY(randomFarm.worldX, randomFarm.worldY, farmSize, farmSize, false);
            generatedLocation.Exterior.RecordElement.Header.IsExterior = 32768;
            generatedLocation.Exterior.RecordElement.Header.Unknown2 = 0;
            generatedLocation.Exterior.RecordElement.Header.LocationId = generatedLocation.MapTableData.LocationId;
            generatedLocation.Exterior.RecordElement.Header.IsInterior = 0;
            generatedLocation.Exterior.RecordElement.Header.ExteriorLocationId = generatedLocation.Exterior.RecordElement.Header.LocationId;
            generatedLocation.Name = generatedLocation.Exterior.RecordElement.Header.LocationName = generatedLocation.Exterior.ExteriorData.AnotherName = homeName;

            generatedLocation.Exterior.ExteriorData.MapId = generatedLocation.MapTableData.MapId;
            generatedLocation.Exterior.ExteriorData.LocationId = generatedLocation.MapTableData.LocationId;
            generatedLocation.Exterior.ExteriorData.Width = (byte)farmSize;
            generatedLocation.Exterior.ExteriorData.Height = (byte)farmSize;

            generatedLocation.Exterior.ExteriorData.PortTownAndUnknown = 0;

            generatedLocation.Exterior.ExteriorData.BlockNames = new string[farmSize * farmSize];
            generatedLocation.Exterior.ExteriorData.BlockNames = PickFarmExterior((farmSize * farmSize), new DFPosition(randomFarm.worldX, randomFarm.worldY), randomFarm.climate);

            generatedLocation.Exterior.BuildingCount = (ushort)CountBuildings(generatedLocation.Exterior.ExteriorData.BlockNames);
            generatedLocation.Exterior.Buildings = new DFLocation.BuildingData[generatedLocation.Exterior.BuildingCount];

            int buildingCount = 0;

            for (int j = 0; j < farmSize * farmSize; j++)
            {
                string blockReplacementJson = File.ReadAllText(Path.Combine(MapEditor.testPath, "RMB", string.Concat(generatedLocation.Exterior.ExteriorData.BlockNames[j], ".json")));
                DFBlock block = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), blockReplacementJson);
                // DFBlock block = JsonConvert.DeserializeObject<DFBlock>(File.ReadAllText(Path.Combine(MapEditor.testPath, "RMB", generatedLocation.Exterior.ExteriorData.BlockNames[0])));
                int partialBuildingCount = 0;

                do
                {
                    generatedLocation.Exterior.Buildings[buildingCount].NameSeed = (ushort)UnityEngine.Random.Range(0, ushort.MaxValue);
                    generatedLocation.Exterior.Buildings[buildingCount].FactionId = block.RmbBlock.FldHeader.BuildingDataList[partialBuildingCount].FactionId;
                    generatedLocation.Exterior.Buildings[buildingCount].Sector = block.RmbBlock.FldHeader.BuildingDataList[partialBuildingCount].Sector;
                    generatedLocation.Exterior.Buildings[buildingCount].BuildingType = block.RmbBlock.FldHeader.BuildingDataList[partialBuildingCount].BuildingType;
                    generatedLocation.Exterior.Buildings[buildingCount].Quality = (byte)UnityEngine.Random.Range(1, 21);

                    buildingCount++;
                    partialBuildingCount++;
                }
                while (partialBuildingCount < block.RmbBlock.FldHeader.BuildingDataList.Length);
            }

            if (buildingCount != generatedLocation.Exterior.BuildingCount)
                Debug.Log("Buildings set and BuildingCount for location " + generatedLocation.Exterior.RecordElement.Header.LocationName + " don't correspond: buildingCount: " + buildingCount + ", generatedLocation.Exterior.BuildingCount: " + generatedLocation.Exterior.BuildingCount);

            generatedLocation.Dungeon = new DFLocation.LocationDungeon();
            generatedLocation.Climate = MapsFile.GetWorldClimateSettings(randomFarm.climate + 223);
            generatedLocation.Politic = Array.IndexOf(WorldInfo.WorldSetting.RegionNames, randomFarm.region);
            generatedLocation.RegionIndex = generatedLocation.Politic + 128;
            generatedLocation.LocationIndex = 0; // TODO

            return generatedLocation;
        }

        protected DFLocation GenerateHome(int locationType, DFPosition position, string homeName, ref List<string>[] presentNames)
        {
            DFLocation generatedLocation = new DFLocation();

            generatedLocation.Loaded = true;
            generatedLocation.RegionName = WorldInfo.WorldSetting.RegionNames[RegionManager.currentRegionIndex];
            generatedLocation.HasDungeon = false;

            generatedLocation.MapTableData.MapId = (ulong)(position.Y * MapsFile.MaxMapPixelX + position.X);
            generatedLocation.MapTableData.Latitude = SetLocationLongitudeLatitude(position.X, position.Y, 1, 1, false);
            generatedLocation.MapTableData.Longitude = SetLocationLongitudeLatitude(position.X, position.Y, 1, 1, true);
            generatedLocation.MapTableData.LocationType = (DFRegion.LocationTypes)locationType;
            generatedLocation.MapTableData.DungeonType = DFRegion.DungeonTypes.NoDungeon;
            generatedLocation.MapTableData.Discovered = false;
            generatedLocation.MapTableData.Key = 0;
            generatedLocation.MapTableData.LocationId = GenerateNewLocationId();

            generatedLocation.Exterior.RecordElement.Header.X = SetPixelXY(position.X, position.Y, 1, 1, true);
            generatedLocation.Exterior.RecordElement.Header.Y = SetPixelXY(position.X, position.Y, 1, 1, false);
            generatedLocation.Exterior.RecordElement.Header.IsExterior = 32768;
            generatedLocation.Exterior.RecordElement.Header.Unknown2 = 0;
            generatedLocation.Exterior.RecordElement.Header.LocationId = generatedLocation.MapTableData.LocationId;
            generatedLocation.Exterior.RecordElement.Header.IsInterior = 0;
            generatedLocation.Exterior.RecordElement.Header.ExteriorLocationId = generatedLocation.Exterior.RecordElement.Header.LocationId;
            generatedLocation.Name = generatedLocation.Exterior.RecordElement.Header.LocationName = generatedLocation.Exterior.ExteriorData.AnotherName = homeName;

            generatedLocation.Exterior.ExteriorData.MapId = generatedLocation.MapTableData.MapId;
            generatedLocation.Exterior.ExteriorData.LocationId = generatedLocation.MapTableData.LocationId;

            // generatedLocation.Exterior.ExteriorData.Width = 1;
            // generatedLocation.Exterior.ExteriorData.Height = 1;

            generatedLocation.Exterior.ExteriorData.PortTownAndUnknown = 0;

            generatedLocation.Exterior.ExteriorData.BlockNames = new string[1];
            generatedLocation.Exterior.ExteriorData.BlockNames = PickHomeExterior(locationType, position, homeName, out generatedLocation.Exterior.ExteriorData.Width, out generatedLocation.Exterior.ExteriorData.Height);

            generatedLocation.Exterior.Buildings = BlockRotatorWindow.RegenerateBuildingsArray(
                                                    generatedLocation.Exterior.ExteriorData.BlockNames,
                                                    RegionManager.currentRegionIndex,
                                                    homeName,
                                                    (DFRegion.LocationTypes)locationType,
                                                    out generatedLocation.Exterior.BuildingCount,
                                                    out generatedLocation.MapTableData.Key);

            generatedLocation.Dungeon = new DFLocation.LocationDungeon();
            generatedLocation.Climate = MapsFile.GetWorldClimateSettings(ClimateInfo.Climate[position.X, position.Y]);
            generatedLocation.Politic = PoliticInfo.Politic[position.X, position.Y];
            generatedLocation.RegionIndex = RegionManager.currentRegionIndex;
            generatedLocation.LocationIndex = 0; // TODO

            if (locationType == (int)DFRegion.LocationTypes.Tavern)
            {
                (int, int) selectedTavern = (-1, -1);
                for (int h = 0; h < generatedLocation.Exterior.Buildings.Length; h++)
                {
                    if (generatedLocation.Exterior.Buildings[h].BuildingType == DFLocation.BuildingTypes.Tavern &&
                        selectedTavern.Item1 < generatedLocation.Exterior.Buildings[h].Quality)
                        selectedTavern = (generatedLocation.Exterior.Buildings[h].Quality, generatedLocation.Exterior.Buildings[h].NameSeed);
                }
                string tavernName = TavernNames.GetName(selectedTavern.Item2, DFLocation.BuildingTypes.Tavern, 510, string.Empty, generatedLocation.RegionName);
                int pickedSuffix;
                do
                {
                    pickedSuffix = UnityEngine.Random.Range(0, Enum.GetNames(typeof(LocationNamesList.TavernEpithets)).Length);
                    tavernName = string.Concat(tavernName, " ", ((LocationNamesList.TavernEpithets)pickedSuffix).ToString());
                }
                while (!CheckDoubleNames(presentNames, tavernName));

                generatedLocation.Name = generatedLocation.Exterior.RecordElement.Header.LocationName = generatedLocation.Exterior.ExteriorData.AnotherName = tavernName;
            }

            return generatedLocation;
        }

        protected DFLocation GenerateTemple(int locationType, DFPosition position, string templeName)
        {
            DFLocation generatedLocation = new DFLocation();

            generatedLocation.Loaded = true;
            generatedLocation.RegionName = WorldInfo.WorldSetting.RegionNames[RegionManager.currentRegionIndex];
            generatedLocation.HasDungeon = false;

            generatedLocation.MapTableData.MapId = (ulong)(position.Y * MapsFile.MaxMapPixelX + position.X);
            generatedLocation.MapTableData.Latitude = SetLocationLongitudeLatitude(position.X, position.Y, 1, 1, false);
            generatedLocation.MapTableData.Longitude = SetLocationLongitudeLatitude(position.X, position.Y, 1, 1, true);
            generatedLocation.MapTableData.LocationType = (DFRegion.LocationTypes)locationType;
            generatedLocation.MapTableData.DungeonType = DFRegion.DungeonTypes.NoDungeon;
            generatedLocation.MapTableData.Discovered = false;
            generatedLocation.MapTableData.Key = 0;
            generatedLocation.MapTableData.LocationId = GenerateNewLocationId();

            generatedLocation.Exterior.RecordElement.Header.X = SetPixelXY(position.X, position.Y, 1, 1, true);
            generatedLocation.Exterior.RecordElement.Header.Y = SetPixelXY(position.X, position.Y, 1, 1, false);
            generatedLocation.Exterior.RecordElement.Header.IsExterior = 32768;
            generatedLocation.Exterior.RecordElement.Header.Unknown2 = 0;
            generatedLocation.Exterior.RecordElement.Header.LocationId = generatedLocation.MapTableData.LocationId;
            generatedLocation.Exterior.RecordElement.Header.IsInterior = 0;
            generatedLocation.Exterior.RecordElement.Header.ExteriorLocationId = generatedLocation.Exterior.RecordElement.Header.LocationId;
            generatedLocation.Name = generatedLocation.Exterior.RecordElement.Header.LocationName = generatedLocation.Exterior.ExteriorData.AnotherName = templeName;

            generatedLocation.Exterior.ExteriorData.MapId = generatedLocation.MapTableData.MapId;
            generatedLocation.Exterior.ExteriorData.LocationId = generatedLocation.MapTableData.LocationId;
            // generatedLocation.Exterior.ExteriorData.Width = 1;
            // generatedLocation.Exterior.ExteriorData.Height = 1;

            generatedLocation.Exterior.ExteriorData.PortTownAndUnknown = 0;
            generatedLocation.Exterior.ExteriorData.BlockNames = new string[1];

            generatedLocation.Exterior.ExteriorData.BlockNames = PickTempleExterior(locationType, position, templeName, out generatedLocation.Exterior.ExteriorData.Width, out generatedLocation.Exterior.ExteriorData.Height);

            generatedLocation.Exterior.Buildings = BlockRotatorWindow.RegenerateBuildingsArray(
                                                    generatedLocation.Exterior.ExteriorData.BlockNames,
                                                    RegionManager.currentRegionIndex,
                                                    templeName,
                                                    (DFRegion.LocationTypes)locationType,
                                                    out generatedLocation.Exterior.BuildingCount,
                                                    out generatedLocation.MapTableData.Key);

            generatedLocation.Dungeon = new DFLocation.LocationDungeon();
            generatedLocation.Climate = MapsFile.GetWorldClimateSettings(ClimateInfo.Climate[position.X, position.Y]);
            generatedLocation.Politic = PoliticInfo.Politic[position.X, position.Y];
            generatedLocation.RegionIndex = RegionManager.currentRegionIndex;
            generatedLocation.LocationIndex = 0; // TODO

            return generatedLocation;
        }

        protected int SetLocationLongitudeLatitude(int x, int y, int width, int height, bool isLongitude)
        {
            int size;
            int pixelSize;

            if (isLongitude)
            {
                size = x * MapsFile.WorldMapTileDim;
                pixelSize = width;
            }
            else
            {
                size = (MapsFile.MaxMapPixelY - y) * MapsFile.WorldMapTileDim;
                pixelSize = height;
            }

            switch (pixelSize)
            {
                case 1:
                    size += 40;
                    break;

                case 2:
                    size += 32;
                    break;

                case 3:
                case 5:
                    size += 24;
                    break;

                case 4:
                case 6:
                    size += 16;
                    break;

                case 7:
                    size += 8;
                    break;

                default:
                    break;
            }
            return size;
        }

        protected int SetPixelXY(int x, int y, int width, int height, bool isX)
        {
            int size;

            if (isX)
            {
                size = x * MapsFile.WorldMapTerrainDim + (8 - width) * 2048;
            }
            else
            {
                size = (y * MapsFile.WorldMapTerrainDim + (8 - height) * 2048) + 1;
            }
            return size;
        }

        protected byte DetermineIfPortTown(int x, int y)
        {
            for (int i = -2; i < 3; i++)
            {
                for (int j = -2; j < 3; j++)
                {
                    if (SmallHeightmap.GetHeightMapValue(x + i, y + j) < 3)
                        return 1;
                }
            }

            return 0;
        }

        protected bool DetermineIfWalled(int width, int height)
        {
            int randomChance = UnityEngine.Random.Range(1, 101);

            if (randomChance <= (width * height * 2 + 18))
                return true;

            return false;
        }

        protected bool DetermineIfPalaced(int width, int height, bool walled)
        {
            int randomChance = UnityEngine.Random.Range(1, 101);
            int palaced = width * height * 2 + 18;

            if (walled)
                palaced += 10;

            if (randomChance <= palaced)
                return true;

            return false;
        }

        protected string[] PickBlockNames(int locationTypes, (int, int, int) townShape, int locationSize, DFPosition position, bool walled = false, string specialCondition = "")
        {
            List<string> blocks = new List<string>();
            List<(int, int, string)> cityCorners = new List<(int, int, string)>();
            List<(int, int, string)> specialBlocks = new List<(int, int, string)>();
            List<string> blockToNormalize = new List<string>();

            if (locationTypes == (int)DFRegion.LocationTypes.TownCity && walled)
                cityCorners = CalculateWallPosition(townShape.Item1, townShape.Item2, position);
            specialBlocks = DetermineSpecialBlocksSetting(position, townShape, locationTypes);

            string climateChar = GetClimateChar(position);
            string sizeChar = ConvertSizeToLetter(locationTypes);

            int[] rarityCount = new int[4];
            int[] typeCount = new int[LocationNamesList.RMBNames[(int)LocationNamesList.RMBTypes.Town].Length];

            bool palacePresent = false;
            (int, int, string) palaceBlock = (0, 0, "");

            if (specialCondition.Equals("capital"))
                palacePresent = true;
            else if (locationTypes == (int)DFRegion.LocationTypes.TownCity)
                palacePresent = DetermineIfPalaced(townShape.Item1, townShape.Item2, walled);

            if (palacePresent)
            {
                // Randomising palace position to (width/height 1, -2) should be fine both if the city is walled or it isn't:
                // in the first case, the palace is inside the walls, in the second it isn't on the city borders
                int x = UnityEngine.Random.Range(1, townShape.Item1 - 2);
                int y = UnityEngine.Random.Range(1, townShape.Item2 - 2);

                TryGetRandomBlock(position,
                    Array.IndexOf(BlockRotatorWindow.blockPrefixes, "PAL"),
                    Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, string.Concat(climateChar, sizeChar)),
                    out string pickedBlock,
                    true,
                    true,
                    true);

                palaceBlock = (x, y, pickedBlock);
            }

            for (int i = 0; i < locationSize; i++)
            {
                bool blockPicked = false;

                switch (locationTypes)
                {
                    case (int)DFRegion.LocationTypes.TownCity:
                        if (walled)
                        {
                            foreach ((int, int, string) wallPiece in cityCorners)
                            {
                                if ((wallPiece.Item2 * townShape.Item1 + wallPiece.Item1) == i)
                                {
                                    string correctWallPiece = RotateWallBlock(wallPiece, townShape);
                                    blocks.Add(correctWallPiece);
                                    blockPicked = true;
                                    break;
                                }
                            }
                        }

                        if (palacePresent)
                        {
                            if ((palaceBlock.Item2 * townShape.Item1 + palaceBlock.Item1) == i)
                            {
                                blocks.Add(palaceBlock.Item3);
                                blockPicked = true;
                            }
                        }

                        if (!blockPicked)
                        {
                            int cityPrefix;
                            SetBlockRarityAndTypes(blocks, out rarityCount, out typeCount);
                            do
                            {
                                cityPrefix = UnityEngine.Random.Range(0, LocationNamesList.RMBNames[(int)LocationNamesList.RMBTypes.Town].Length);

                                if (!CheckBlockCompatibility(cityPrefix, rarityCount, typeCount, townShape.Item1 * townShape.Item2, DFRegion.LocationTypes.TownCity))
                                    cityPrefix = -1;
                            }
                            while (cityPrefix < 0);

                            string pickedBlock = string.Empty;
                            Debug.Log("cityPrefix: " + cityPrefix);
                            cityPrefix = (Array.IndexOf(BlockRotatorWindow.blockPrefixes, LocationNamesList.RMBNames[0][cityPrefix]));
                            TryGetRandomBlock(
                                position,
                                cityPrefix,
                                Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, string.Concat(climateChar, sizeChar)),
                                out pickedBlock,
                                true,
                                true
                            );
                            blocks.Add(pickedBlock);
                        }
                        break;

                    case (int)DFRegion.LocationTypes.TownHamlet:
                    case (int)DFRegion.LocationTypes.TownVillage:
                        if (townShape.Item3 != (int)TownShapes.Regular)
                        {
                            foreach ((int, int, string) fillPiece in specialBlocks)
                            {
                                if ((fillPiece.Item2 * townShape.Item1 + fillPiece.Item1) == i)
                                {
                                    blocks.Add(fillPiece.Item3);
                                    blockPicked = true;
                                    break;
                                }
                            }
                        }

                        if (!blockPicked)
                        {
                            int townPrefix;
                            SetBlockRarityAndTypes(blocks, out rarityCount, out typeCount);
                            do
                            {
                                townPrefix = UnityEngine.Random.Range(0, LocationNamesList.RMBNames[(int)LocationNamesList.RMBTypes.Town].Length);

                                if (!CheckBlockCompatibility(townPrefix, rarityCount, typeCount, townShape.Item1 * townShape.Item2, (DFRegion.LocationTypes)locationTypes))
                                    townPrefix = -1;
                            }
                            while (townPrefix < 0);

                            string pickedBlock = string.Empty;
                            Debug.Log("townPrefix: " + townPrefix);
                            townPrefix = (Array.IndexOf(BlockRotatorWindow.blockPrefixes, LocationNamesList.RMBNames[0][townPrefix]));
                            TryGetRandomBlock(
                                position,
                                townPrefix,
                                Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, string.Concat(climateChar, sizeChar)),
                                out pickedBlock,
                                true,
                                true
                            );
                            blocks.Add(pickedBlock);
                        }
                        break;
                }
            }

            // Debug.Log("Normalizing Town RMBs");
            // blocks = NormalizeTownRMBs(blocks);

            return blocks.ToArray();
        }

        protected void SetBlockRarityAndTypes(List<string> blocks, out int[] rarityCount, out int[] typeCount)
        {
            rarityCount = new int[4];
            for (int g = 0; g < rarityCount.Length; g++)
                rarityCount[g] = 0;
            typeCount = new int[LocationNamesList.RMBNames[(int)LocationNamesList.RMBTypes.Town].Length];
            for (int h = 0; h < typeCount.Length; h++)
                typeCount[h] = 0;

            Debug.Log("blocks.Length: " + blocks.Count);
            foreach (string block in blocks)
            {
                Debug.Log("SetBlockRarityAndTypes block: " + block);
                int blockIndex = Array.IndexOf(LocationNamesList.RMBNames[(int)LocationNamesList.RMBTypes.Town], block.Substring(0, 3));
                if (blockIndex < 0) continue;

                typeCount[blockIndex]++;

                if (blockIndex <= lastCommonInd) rarityCount[0]++;
                else if (blockIndex <= lastUncommonInd) rarityCount[1]++;
                else if (blockIndex <= lastUnusualInd) rarityCount[2]++;
                else rarityCount[3]++;
            }
        }

        protected string RandomiseRotation(string block)
        {
            int rotation = UnityEngine.Random.Range(0, 4);
            return string.Concat(block.Substring(0, 3), rotation.ToString(), block.Substring(4));
        }

        protected string RotateWallBlock((int, int, string) wallBlock, (int, int, int) townShape)
        {
            int rotation = 0;
            if (!int.TryParse(wallBlock.Item3.Substring(6, 2), out int wallProg))
            {
                Debug.Log("Problem occured with wallBlock " + wallBlock.Item3);
                return string.Empty;
            }

            int varCounter = 0;
            while (wallProg >= 12)
            {
                wallProg -= 12;
                varCounter++;
            }

            string wallSide = GetWallSide(wallBlock.Item1, wallBlock.Item2, townShape.Item1, townShape.Item2);
            switch (wallProg)
            {
                case 0:
                    switch (wallSide)
                    {
                        case "SW":
                            rotation = 3;
                            break;
                        case "NW":
                            rotation = 0;
                            break;
                        case "SE":
                            rotation = 2;
                            break;
                        case "NE":
                            rotation = 1;
                            break;
                        default:
                            Debug.Log("Wrong combination of wallProg and wallSide!");
                            rotation = -1;
                            break;
                    }
                    break;
                case 1:
                    switch (wallSide)
                    {
                        case "SW":
                            rotation = 2;
                            break;
                        case "NW":
                            rotation = 3;
                            break;
                        case "SE":
                            rotation = 1;
                            break;
                        case "NE":
                            rotation = 0;
                            break;
                        default:
                            Debug.Log("Wrong combination of wallProg and wallSide!");
                            rotation = -1;
                            break;
                    }
                    break;
                case 2:
                    switch (wallSide)
                    {
                        case "SW":
                            rotation = 0;
                            break;
                        case "NW":
                            rotation = 1;
                            break;
                        case "SE":
                            rotation = 3;
                            break;
                        case "NE":
                            rotation = 2;
                            break;
                        default:
                            Debug.Log("Wrong combination of wallProg and wallSide!");
                            rotation = -1;
                            break;
                    }
                    break;
                case 3:
                    switch (wallSide)
                    {
                        case "SW":
                            rotation = 1;
                            break;
                        case "NW":
                            rotation = 2;
                            break;
                        case "SE":
                            rotation = 0;
                            break;
                        case "NE":
                            rotation = 3;
                            break;
                        default:
                            Debug.Log("Wrong combination of wallProg and wallSide!");
                            rotation = -1;
                            break;
                    }
                    break;
                case 4:
                case 8:
                    switch (wallSide)
                    {
                        case "W":
                            rotation = 3;
                            break;
                        case "E":
                            rotation = 1;
                            break;
                        case "S":
                            rotation = 2;
                            break;
                        case "N":
                            rotation = 0;
                            break;
                        default:
                            Debug.Log("Wrong combination of wallProg and wallSide!");
                            rotation = -1;
                            break;
                    }
                    break;
                case 5:
                case 9:
                    switch (wallSide)
                    {
                        case "W":
                            rotation = 2;
                            break;
                        case "E":
                            rotation = 0;
                            break;
                        case "S":
                            rotation = 1;
                            break;
                        case "N":
                            rotation = 3;
                            break;
                        default:
                            Debug.Log("Wrong combination of wallProg and wallSide!");
                            rotation = -1;
                            break;
                    }
                    break;
                case 6:
                case 10:
                    switch (wallSide)
                    {
                        case "W":
                            rotation = 1;
                            break;
                        case "E":
                            rotation = 3;
                            break;
                        case "S":
                            rotation = 0;
                            break;
                        case "N":
                            rotation = 2;
                            break;
                        default:
                            Debug.Log("Wrong combination of wallProg and wallSide!");
                            rotation = -1;
                            break;
                    }
                    break;
                case 7:
                case 11:
                    switch (wallSide)
                    {
                        case "W":
                            rotation = 0;
                            break;
                        case "E":
                            rotation = 2;
                            break;
                        case "S":
                            rotation = 3;
                            break;
                        case "N":
                            rotation = 1;
                            break;
                        default:
                            Debug.Log("Wrong combination of wallProg and wallSide!");
                            rotation = -1;
                            break;
                    }
                    break;
            }
            return string.Concat(wallBlock.Item3.Substring(0, 3), rotation.ToString(), wallBlock.Item3.Substring(4));
        }

        protected string GetWallSide(int x, int y, int width, int height)
        {
            bool xHigh, yHigh, xLow, yLow;
            xHigh = x == width - 1;
            yHigh = y == height - 1;
            xLow = x == 0;
            yLow = y == 0;
            if (xLow)
            {
                if (yLow)
                    return "SW";
                if (yHigh)
                    return "NW";
                return "W";
            }
            if (xHigh)
            {
                if (yLow)
                    return "SE";
                if (yHigh)
                    return "NE";
                return "E";
            }
            if (yLow)
                return "S";
            else return "N";
        }

        protected bool CheckBlockCompatibility(int prefix, int[] rarityCount, int[] typeCount, int locSize, DFRegion.LocationTypes townType)
        {
            string checkedBlock = LocationNamesList.RMBNames[(int)LocationNamesList.RMBTypes.Town][prefix];
            int maxRareTolerated, maxUnusualTolerated, maxUncommonTolerated, maxCommonTolerated;
            int maxUniqueRare, maxUniqueUnusual, maxUniqueUncommon, maxUniqueCommon;

            if (townType.Equals(DFRegion.LocationTypes.TownCity))
            {
                maxRareTolerated = 3;
                maxUnusualTolerated = locSize / 6;
                maxUncommonTolerated = locSize / 5;
                maxCommonTolerated = locSize / 3 * 2;

                maxUniqueRare = 1;
                maxUniqueUnusual = 2;
                maxUniqueUncommon = 2;
                maxUniqueCommon = locSize / 2;
            }
            else if (townType.Equals(DFRegion.LocationTypes.TownHamlet))
            {
                maxRareTolerated = 2;
                maxUnusualTolerated = locSize / 6;
                maxUncommonTolerated = locSize / 5;
                maxCommonTolerated = locSize / 3 * 2;

                maxUniqueRare = 1;
                maxUniqueUnusual = 1;
                maxUniqueUncommon = 2;
                maxUniqueCommon = locSize / 2;
            }
            else // (townType.Equals(DFRegion.LocationTypes.TownVillage))
            {
                maxRareTolerated = 1;
                maxUnusualTolerated = 1;
                maxUncommonTolerated = 1;
                maxCommonTolerated = int.MaxValue;

                maxUniqueRare = 1;
                maxUniqueUnusual = 1;
                maxUniqueUncommon = 1;
                maxUniqueCommon = locSize - 1;
            }

            if (prefix > lastUnusualInd)    // Rare
            {
                if (rarityCount[3] >= maxRareTolerated || typeCount[prefix] >= maxUniqueRare)
                    return false;
            }

            else if (prefix > lastUncommonInd)  // Unusual
            {
                if (rarityCount[2] >= maxUnusualTolerated || typeCount[prefix] >= maxUniqueUnusual)
                    return false;
            }

            else if (prefix > lastCommonInd)    // Uncommon
            {
                if (rarityCount[1] >= maxUncommonTolerated || typeCount[prefix] >= maxUniqueUncommon)
                    return false;
            }

            else                                // Common
            {
                if (rarityCount[0] >= maxCommonTolerated || typeCount[prefix] >= maxUniqueCommon)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Converts the deity or temple faction ID to the letter used in TEM blocks
        /// </summary>
        /// <param name="deityORtemple"> Faction ID of deity or temple </param>
        /// <returns></returns>
        protected string GetTempleCode(int deityORtemple)
        {
            switch (deityORtemple)
            {
                case 21:
                case 82:
                    return "A";

                case 22:
                case 84:
                    return "B";

                case 24:
                case 88:
                    return "C";

                case 26:
                case 92:
                    return "D";

                case 29:
                case 98:
                    return "E";

                case 27:
                case 94:
                    return "F";

                case 33:
                case 106:
                    return "G0";

                case 35:
                case 36:
                    return "H0";

                default:
                    return "Error!!!";
            }
        }

        protected List<string> NormalizeTownRMBs(List<string> RMBList)
        {
            List<string> normalizedRMBList = new List<string>();
            foreach (string block in RMBList)
            {
                string fixedBlock = "notFixed";

                if (!File.Exists(Path.Combine(MapEditor.testPath, "RMB", string.Concat(block, ".json"))))
                {
                    Debug.Log(block + " isn't correct. Going to fix it");

                    fixedBlock = block;

                    if (block.StartsWith("BANK"))       // -g
                        fixedBlock = fixedBlock.Replace('G', 'B');
                    else if (block.StartsWith("BOOK"))  // -g
                        fixedBlock = fixedBlock.Replace('G', 'B');
                    else if (block.StartsWith("DARK"))  // -g -size
                    {
                        fixedBlock = fixedBlock.Replace("AL", "AA");
                        fixedBlock = fixedBlock.Replace("AM", "AA");
                        fixedBlock = fixedBlock.Replace("AS", "AA");
                        fixedBlock = fixedBlock.Replace("BL", "BA");
                        fixedBlock = fixedBlock.Replace("BM", "BA");
                        fixedBlock = fixedBlock.Replace("BS", "BA");
                        fixedBlock = fixedBlock.Replace("GL", "BA");
                        fixedBlock = fixedBlock.Replace("GM", "BA");
                        fixedBlock = fixedBlock.Replace("GS", "BA");
                    }
                    else if (block.StartsWith("FIGH"))  // a: -size; 
                    {
                        fixedBlock = fixedBlock.Replace("AL", "AA");
                        fixedBlock = fixedBlock.Replace("AM", "AA");
                        fixedBlock = fixedBlock.Replace("AS", "AA");
                    }
                    // else if (block.StartsWith("FILL"))  // -b -g -size
                    // {
                    //     fixedBlock = fixedBlock.Replace("AL", "AA");
                    //     fixedBlock = fixedBlock.Replace("AM", "AA");
                    //     fixedBlock = fixedBlock.Replace("AS", "AA");
                    //     fixedBlock = fixedBlock.Replace("BL", "AA");
                    //     fixedBlock = fixedBlock.Replace("BM", "AA");
                    //     fixedBlock = fixedBlock.Replace("BS", "AA");
                    //     fixedBlock = fixedBlock.Replace("GL", "AA");
                    //     fixedBlock = fixedBlock.Replace("GM", "AA");
                    //     fixedBlock = fixedBlock.Replace("GS", "AA");
                    // }
                    else if (block.StartsWith("GRVE"))  // -b -g
                    {
                        fixedBlock = fixedBlock.Replace('S', 'M');    // no "S" blocks for town graveyards (they have entrances)!
                        if (fixedBlock.Contains("VEBL") && UnityEngine.Random.Range(0, 10) != 0) fixedBlock = fixedBlock.Replace("VEBL", "VEAL");
                        if (!fixedBlock.Contains("VEBL")) fixedBlock = fixedBlock.Replace("VEB", "VEA");
                        fixedBlock = fixedBlock.Replace("VEG", "VEA");
                    }
                    else if (block.StartsWith("LIBR"))  // -g
                        fixedBlock = fixedBlock.Replace('G', 'B');
                    else if (block.StartsWith("MAGE"))  // -size
                    {
                        fixedBlock = fixedBlock.Replace("AL", "AA");
                        fixedBlock = fixedBlock.Replace("AM", "AA");
                        fixedBlock = fixedBlock.Replace("AS", "AA");
                        fixedBlock = fixedBlock.Replace("BL", "BA");
                        fixedBlock = fixedBlock.Replace("BM", "BA");
                        fixedBlock = fixedBlock.Replace("BS", "BA");
                        fixedBlock = fixedBlock.Replace("GL", "GA");
                        fixedBlock = fixedBlock.Replace("GM", "GA");
                        fixedBlock = fixedBlock.Replace("GS", "GA");
                    }
                    else if (block.StartsWith("MARK"))  // a: -size; b: +-size
                    {
                        fixedBlock = fixedBlock.Replace("AL", "AA");
                        fixedBlock = fixedBlock.Replace("AM", "AA");
                        fixedBlock = fixedBlock.Replace("AS", "AA");

                        if (UnityEngine.Random.Range(0, 2) == 0)
                            fixedBlock = fixedBlock.Replace("BM", "BA");
                    }
                    // else if (block.StartsWith("PALA"))  // -size
                    // {
                    //     fixedBlock = fixedBlock.Replace("LAAL", "LAAA");
                    //     fixedBlock = fixedBlock.Replace("LAAM", "LAAA");
                    //     fixedBlock = fixedBlock.Replace("LAAS", "LAAA");
                    //     fixedBlock = fixedBlock.Replace("BL", "BA");
                    //     fixedBlock = fixedBlock.Replace("BM", "BA");
                    //     fixedBlock = fixedBlock.Replace("BS", "BA");
                    //     fixedBlock = fixedBlock.Replace("GL", "GA");
                    //     fixedBlock = fixedBlock.Replace("GM", "GA");
                    //     fixedBlock = fixedBlock.Replace("GS", "GA");
                    // }
                    else if (block.StartsWith("TEMP"))  // -size
                    {
                        fixedBlock = fixedBlock.Replace("AL", "AA");
                        fixedBlock = fixedBlock.Replace("AM", "AA");
                        fixedBlock = fixedBlock.Replace("AS", "AA");
                        fixedBlock = fixedBlock.Replace("BL", "BA");
                        fixedBlock = fixedBlock.Replace("BM", "BA");
                        fixedBlock = fixedBlock.Replace("BS", "BA");
                        fixedBlock = fixedBlock.Replace("GL", "GA");
                        fixedBlock = fixedBlock.Replace("GM", "GA");
                        fixedBlock = fixedBlock.Replace("GS", "GA");
                    }
                    else if (block.StartsWith("THIE"))  // b: -s; -g
                    {
                        fixedBlock = fixedBlock.Replace('G', 'B');
                        fixedBlock = fixedBlock.Replace("BS", "BM");
                    }

                    Debug.Log("RMB name changed to " + fixedBlock);
                }

                if (fixedBlock.Equals("notFixed"))
                    normalizedRMBList.Add(block);
                else normalizedRMBList.Add(fixedBlock);
            }

            return normalizedRMBList;
        }

        protected string[] PickFarmExterior(int farmSize, DFPosition position, int climateNumb)
        {
            string[] resultingName = new string[farmSize];
            int pickedRMB;
            string climateChar;
            string sizeChar;

            for (int i = 0; i < farmSize; i++)
            {
                if (climateNumb + 223 == (int)MapsFile.Climates.Desert ||
                    climateNumb + 223 == (int)MapsFile.Climates.Savannah ||
                    climateNumb + 223 == (int)MapsFile.Climates.Subtropical)
                    climateChar = "B";
                else climateChar = "A";

                pickedRMB = UnityEngine.Random.Range(0, 10);
                resultingName[i] = string.Concat("FARM", climateChar, "A", pickedRMB.ToString("00"), ".RMB");
            }

            return resultingName;
        }

        protected string[] PickHomeExterior(int locationType, DFPosition position, string homeName, out byte width, out byte height)
        {
            string climateSize = string.Concat(GetClimateChar(position), ConvertSizeToLetter(locationType));
            string climateChar = GetClimateChar(position);
            string sizeChar = ConvertSizeToLetter(locationType);
            int mainPos = 0;
            int randomValue = UnityEngine.Random.Range(0, int.MaxValue);

            switch (sizeChar)
            {
                case "S":
                    width = 1;
                    height = 1;
                    break;

                case "M":
                    switch (locationType)
                    {
                        case (int)DFRegion.LocationTypes.HomeFarms:
                            if (UnityEngine.Random.Range(0, 2) == 0)
                            {
                                width = 2; height = 1;
                            }
                            else
                            {
                                width = 1; height = 2;
                            }
                            mainPos = randomValue % (width * height);
                            break;
                        case (int)DFRegion.LocationTypes.HomeWealthy:
                            width = 2;
                            height = 2;
                            mainPos = randomValue % (width * height);
                            break;
                        default:
                            width = 1;
                            height = 1;
                            break;
                    }
                    break;

                case "L":
                    switch (locationType)
                    {
                        case (int)DFRegion.LocationTypes.HomeFarms:
                            width = 2;
                            height = 2;
                            mainPos = randomValue % (width * height);
                            break;
                        case (int)DFRegion.LocationTypes.HomeWealthy:
                            if (UnityEngine.Random.Range(0, 2) == 0)
                            {
                                width = 3; height = 2;
                                mainPos = randomValue % 2 == 0 ? 1 : 4;
                            }
                            else
                            {
                                width = 2; height = 3;
                                mainPos = randomValue % 2 + 2;
                            }
                            break;
                        default:
                            width = 1;
                            height = 1;
                            break;
                    }
                    break;
                default:
                    width = 1;
                    height = 1;
                    break;
            }

            string[] pickedRMB = new string[width * height];

            for (int i = 0; i < pickedRMB.Length; i++)
            {
                if (i == mainPos)
                {
                    TryGetRandomBlock(
                        position,
                        Array.IndexOf(BlockRotatorWindow.blockPrefixes, ConvertHomeLocTypeToBlock(locationType)),
                        Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, climateSize),
                        out pickedRMB[i],
                        true,
                        true);
                }
                else
                {
                    TryGetRandomBlock(
                        position,
                        Array.IndexOf(BlockRotatorWindow.blockPrefixes, GetSubBlockForHomes(locationType)),
                        Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, string.Concat(climateChar, "S")),
                        out pickedRMB[i],
                        true,
                        true);
                }
            }

            return pickedRMB;
        }

        protected string ConvertHomeLocTypeToBlock(int locType)
        {
            switch (locType)
            {
                case (int)DFRegion.LocationTypes.HomeFarms:
                    return "FAR";
                case (int)DFRegion.LocationTypes.HomePoor:
                    return "SHC";
                case (int)DFRegion.LocationTypes.HomeWealthy:
                    return "MAN";
                case (int)DFRegion.LocationTypes.Tavern:
                    return "TVR";
                default:
                    Debug.Log("ConvertHomeLocTypeToBlock got wrong location type as input: " + (DFRegion.LocationTypes)locType);
                    return string.Empty;
            }
        }

        protected string GetSubBlockForHomes(int locType)
        {
            int randomValue = UnityEngine.Random.Range(0, int.MaxValue);
            switch (locType)
            {
                case (int)DFRegion.LocationTypes.HomeFarms:
                    if (randomValue % 3 == 0) return "FIL";
                    else return "FAR";

                case (int)DFRegion.LocationTypes.HomeWealthy:
                    randomValue %= 10;
                    switch (randomValue)
                    {
                        case 0:
                        case 8: return "FAR";
                        case 1:
                        case 9: return "RES";
                        case 2: return "GEN";
                        case 3: return "GRV";
                        case 4: return "TEM";
                        case 5: return "FIL";
                        case 6: return "DAR";
                        case 7: return "THI";
                        default: return string.Empty;
                    }
                default:
                    Debug.Log("GetSubBlockForHomes got wrong location type as input: " + (DFRegion.LocationTypes)locType);
                    return string.Empty;
            }
        }

        protected string[] PickTempleExterior(int locationType, DFPosition position, string templeName, out byte width, out byte height)
        {
            string climateSize = string.Concat(GetClimateChar(position), ConvertSizeToLetter(locationType));
            string climateChar = GetClimateChar(position);
            string sizeChar = ConvertSizeToLetter(locationType);
            int mainPos = 0;
            int randomValue = UnityEngine.Random.Range(0, int.MaxValue);
            string deityChar = ConvertTempleNameToLetter(templeName);
            string[] pickedRMB = new string[1];

            if (locationType == (int)DFRegion.LocationTypes.ReligionCult)
            {
                TryGetRandomBlock(
                    position,
                    Array.IndexOf(BlockRotatorWindow.blockPrefixes, "SHR"),
                    Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, climateSize),
                    out pickedRMB[0],
                    true,
                    true);

                width = height = 1;
            }

            else
            {
                switch (sizeChar)
                {
                    case "S":
                        width = 1;
                        height = 1;
                        break;

                    case "M":
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            width = 2; height = 1;
                        }
                        else
                        {
                            width = 1; height = 2;
                        }
                        mainPos = randomValue % (width * height);
                        break;

                    case "L":
                        width = 2;
                        height = 2;
                        mainPos = randomValue % (width * height);
                        break;

                    default:
                        width = 1;
                        height = 1;
                        break;
                }

                pickedRMB = new string[width * height];

                for (int i = 0; i < pickedRMB.Length; i++)
                {
                    if (i == mainPos)
                    {
                        TryGetRandomBlock(
                            position,
                            Array.IndexOf(BlockRotatorWindow.blockPrefixes, "TEM"),
                            Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, climateSize),
                            out pickedRMB[i],
                            true,
                            true,
                            true,
                            ConvertTempleNameToLetter(templeName));
                    }
                    else
                    {
                        TryGetRandomBlock(
                            position,
                            Array.IndexOf(BlockRotatorWindow.blockPrefixes, GetSubBlockForTemples()),
                            Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, string.Concat(climateChar, "S")),
                            out pickedRMB[i],
                            true,
                            true);
                    }
                }
            }
            return pickedRMB;
        }

        protected string GetSubBlockForTemples()
        {
            int randomValue = UnityEngine.Random.Range(0, 10);

            switch (randomValue)
            {
                case 0:
                case 1: return "RES";
                case 2: return "GEN";
                case 3: return "GRV";
                case 4:
                case 5: return "TVR";
                case 6: return "FIL";
                case 7: return "DAR";
                case 8: return "THI";
                case 9: return "FAR";
                default: return string.Empty;
            }
        }

        protected List<(int, int, string)> CalculateWallPosition(int width, int height, DFPosition position)
        {
            List<(int, int, string)> wallPosition = new List<(int, int, string)>();

            int westGate = UnityEngine.Random.Range(1, height - 1);
            int eastGate = UnityEngine.Random.Range(1, height - 1);
            int northGate = UnityEngine.Random.Range(1, width - 1);
            int southGate = UnityEngine.Random.Range(1, width - 1);

            int blockType = Array.IndexOf(BlockRotatorWindow.blockPrefixes, "WAL");
            int climateType;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // DFPosition currentPos = new DFPosition(x, y);
                    if (UnityEngine.Random.Range(1, 101) > normalWallChance)
                        climateType = Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, string.Concat(GetClimateChar(position), "A"));
                    else climateType = Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, "ZA");

                    string pickedBlock = string.Empty;

                    if (((x == 0 && y == 0) || (x == 0 && y == height - 1) || (x == width - 1 && y == 0) || (x == width - 1 && y == height - 1))
                        && TryGetRandomBlock(position, blockType, climateType, out pickedBlock, true, true, false, string.Empty, WallType.Corner))
                        wallPosition.Add((x, y, pickedBlock));
                    else if (((x == 0 && y == westGate) || (x == width - 1 && y == eastGate) || (x == northGate && y == 0) || (x == southGate && y == height - 1))
                            && TryGetRandomBlock(position, blockType, climateType, out pickedBlock, true, true, false, string.Empty, WallType.Gate))
                        wallPosition.Add((x, y, pickedBlock));
                    else if ((x == 0 || x == width - 1 || y == 0 || y == height - 1)
                            && TryGetRandomBlock(position, blockType, climateType, out pickedBlock, true, true, false, string.Empty, WallType.Wall))
                        wallPosition.Add((x, y, pickedBlock));
                }
            }

            return wallPosition;
        }

        protected List<(int, int, string)> DetermineSpecialBlocksSetting(DFPosition position, (int, int, int) townShape, int locationType)
        {
            List<(int, int, string)> specialBlocks = new List<(int, int, string)>();
            int x, y;
            int fillIndex;
            string pickedBlock;

            switch (townShape.Item3)
            {
                case (int)TownShapes.LShape:
                    specialBlocks = PutSomeFillerHereAndThere(position, townShape.Item1, townShape.Item2, 1, locationType);
                    break;

                case (int)TownShapes.FourOnSix:
                    specialBlocks = PutSomeFillerHereAndThere(position, townShape.Item1, townShape.Item2, 2, locationType);
                    break;

                case (int)TownShapes.FiveOnSix:
                    specialBlocks = PutSomeFillerHereAndThere(position, townShape.Item1, townShape.Item2, 1, locationType);
                    break;

                case (int)TownShapes.SixOnEight:
                    specialBlocks = PutSomeFillerHereAndThere(position, townShape.Item1, townShape.Item2, 2, locationType);
                    break;

                case (int)TownShapes.MantaShape:    // This is a bit tricky compared to the other shapes
                    x = UnityEngine.Random.Range(0, 2) * 2;
                    y = UnityEngine.Random.Range(0, 2) * 2;

                    TryGetRandomBlock(
                        position,
                        Array.IndexOf(BlockRotatorWindow.blockPrefixes, "FIL"),
                        Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, string.Concat(GetClimateChar(position), ConvertSizeToLetter(locationType))),
                        out pickedBlock,
                        true,
                        true);

                    specialBlocks.Add((x, y, pickedBlock));
                    Debug.Log("Block " + pickedBlock + " added at position " + x + ", " + y);

                    int x1;
                    int y1;

                    x1 = x;
                    do
                    {
                        y1 = UnityEngine.Random.Range(0, 3);
                    }
                    while (y1 == y);

                    TryGetRandomBlock(
                        position,
                        Array.IndexOf(BlockRotatorWindow.blockPrefixes, "FIL"),
                        Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, string.Concat(GetClimateChar(position), ConvertSizeToLetter(locationType))),
                        out pickedBlock,
                        true,
                        true);

                    specialBlocks.Add((x1, y1, pickedBlock));
                    Debug.Log("Block " + pickedBlock + " added at position " + x1 + ", " + y1);

                    int x2;
                    int y2 = y;

                    do
                    {
                        x2 = UnityEngine.Random.Range(0, 3);
                    }
                    while (x2 == x);

                    TryGetRandomBlock(
                        position,
                        Array.IndexOf(BlockRotatorWindow.blockPrefixes, "FIL"),
                        Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, string.Concat(GetClimateChar(position), ConvertSizeToLetter(locationType))),
                        out pickedBlock,
                        true,
                        true);

                    specialBlocks.Add((x2, y2, pickedBlock));
                    Debug.Log("Block " + pickedBlock + " added at position " + x2 + ", " + y2);
                    break;

                case (int)TownShapes.EightOnNine:
                    specialBlocks = PutSomeFillerHereAndThere(position, townShape.Item1, townShape.Item2, 1, locationType);
                    break;

                case (int)TownShapes.NineOnTwelve:
                    specialBlocks = PutSomeFillerHereAndThere(position, townShape.Item1, townShape.Item2, 3, locationType);
                    break;

                case (int)TownShapes.TwelveOnSixteen:
                    specialBlocks = PutSomeFillerHereAndThere(position, townShape.Item1, townShape.Item2, 4, locationType);
                    break;

                default:
                    break;
            }

            return specialBlocks;
        }

        protected List<(int, int, string)> PutSomeFillerHereAndThere(DFPosition position, int width, int height, int fillToPutIn, int locationType)
        {
            List<(int, int, string)> specialBlocks = new List<(int, int, string)>();
            List<(int, int)> pickedPositions = new List<(int, int)>();
            string fillBlock;

            do
            {
                int x = UnityEngine.Random.Range(0, width);
                int y = UnityEngine.Random.Range(0, height);
                string pickedBlock = string.Empty;

                TryGetRandomBlock(
                    position,
                    Array.IndexOf(BlockRotatorWindow.blockPrefixes, "FIL"),
                    Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, string.Concat(GetClimateChar(position), ConvertSizeToLetter(locationType))),
                    out pickedBlock,
                    true,
                    true);

                if (!pickedPositions.Contains((x, y)))
                {
                    specialBlocks.Add((x, y, pickedBlock));
                    pickedPositions.Add((x, y));
                    Debug.Log("Block " + pickedBlock + " added at position " + x + ", " + y);
                }
            }
            while (specialBlocks.Count < fillToPutIn);

            return specialBlocks;
        }

        protected string ConvertSizeToLetter(int locationType)
        {
            switch (locationType)
            {
                case (int)DFRegion.LocationTypes.TownCity:
                case (int)DFRegion.LocationTypes.DungeonLabyrinth:
                    return "L";
                case (int)DFRegion.LocationTypes.TownHamlet:
                case (int)DFRegion.LocationTypes.DungeonKeep:
                    return "M";
                case (int)DFRegion.LocationTypes.TownVillage:
                case (int)DFRegion.LocationTypes.DungeonRuin:
                    return "S";
                case (int)DFRegion.LocationTypes.DungeonMinor:
                    return "N";
                case (int)DFRegion.LocationTypes.HomeFarms:
                case (int)DFRegion.LocationTypes.HomePoor:
                case (int)DFRegion.LocationTypes.HomeWealthy:
                case (int)DFRegion.LocationTypes.Tavern:
                case (int)DFRegion.LocationTypes.ReligionTemple:
                    int prop = UnityEngine.Random.Range(0, 13);
                    if (prop == 0) return "L";
                    else if (prop < 4) return "M";
                    else return "S";
                default:
                    return "A";
            }
        }

        /// <summary> 
        /// Randomly pick a block between two choices, with a chance of x/10
        /// </summary>
        /// <param name="element1"> First element </param>
        /// <param name="element2"> Second element </param>
        /// <param name="firstElementChance"> Chance out of 10 to pick the first element </param>
        protected string GetRandom(string element1, string element2, int firstElementChance = 5)
        {
            if (UnityEngine.Random.Range(1, 11) <= firstElementChance)
                return element1;

            return element2;
        }

        protected bool TryGetRandomBlock(DFPosition position, int blockType, int climateType, out string pickedBlock, bool tryGenericSize = false, bool trySpecific = false, bool randomiseRotation = true, string deity = "", WallType wallType = WallType.None)
        {
            int avBlocks = -1;
            int randomChance = UnityEngine.Random.Range(0, int.MaxValue);
            bool generic = climateType == 0;
            bool climateGeneric = climateType < 5;
            bool sizeGeneric = climateType % 5 == 0;
            pickedBlock = string.Empty;
            bool isTemple = blockType == Array.IndexOf(BlockRotatorWindow.blockPrefixes, "TEM");
            Debug.Log("TryGetRandomBlock trying to pick blockType: " + blockType + ", climateType: " + climateType + ", randomChance: " + randomChance + ", wallType: " + wallType);

            if (availableBlocks[blockType, climateType].Length > 0)
            {
                Debug.Log("TryGetRandomBlock trying to pick blockType: " + blockType + ", climateType: " + climateType + ", randomChance: " + randomChance + ", wallType: " + wallType);
                avBlocks = availableBlocks[blockType, climateType].Length;
                if (wallType != WallType.None) pickedBlock = availableBlocks[blockType, climateType][GetWallSubgroup(randomChance % avBlocks, blockType, climateType, wallType)];
                else if (isTemple) pickedBlock = GetTempleSubgroup(position, availableBlocks[blockType, climateType][randomChance % avBlocks], deity);
                else pickedBlock = availableBlocks[blockType, climateType][randomChance % avBlocks];
                if (randomiseRotation) pickedBlock = RandomiseRotation(pickedBlock);
                return true;
            }

            // If the required block category is empty, try getting the same version with a generic climate indicator ("Z");
            int genericVersion = 0;
            if (!climateGeneric && TryGetGenericClimate(climateType, out genericVersion) && availableBlocks[blockType, genericVersion].Length > 0)
            {
                Debug.Log("TryGetRandomBlock trying to pick blockType: " + blockType + ", climateType: " + genericVersion);
                randomChance = UnityEngine.Random.Range(0, int.MaxValue);
                avBlocks = availableBlocks[blockType, genericVersion].Length;
                if (wallType != WallType.None) pickedBlock = availableBlocks[blockType, genericVersion][GetWallSubgroup(randomChance % avBlocks, blockType, genericVersion, wallType)];
                else if (isTemple) pickedBlock = GetTempleSubgroup(position, availableBlocks[blockType, genericVersion][randomChance % avBlocks], deity);
                else pickedBlock = availableBlocks[blockType, genericVersion][randomChance % avBlocks];
                if (randomiseRotation) pickedBlock = RandomiseRotation(pickedBlock);
                return true;
            }

            // If the generic climate category is empty too and this type of block allows to, try getting a generic size version ("A")
            if (!sizeGeneric && tryGenericSize && TryGetGenericSize(climateType, out genericVersion) && availableBlocks[blockType, genericVersion].Length > 0)
            {
                Debug.Log("TryGetRandomBlock trying to pick blockType: " + blockType + ", climateType: " + genericVersion);
                randomChance = UnityEngine.Random.Range(0, int.MaxValue);
                avBlocks = availableBlocks[blockType, genericVersion].Length;
                if (wallType != WallType.None) pickedBlock = availableBlocks[blockType, genericVersion][GetWallSubgroup(randomChance % avBlocks, blockType, genericVersion, wallType)];
                else if (isTemple) pickedBlock = GetTempleSubgroup(position, availableBlocks[blockType, genericVersion][randomChance % avBlocks], deity);
                else pickedBlock = availableBlocks[blockType, genericVersion][randomChance % avBlocks];
                if (randomiseRotation) pickedBlock = RandomiseRotation(pickedBlock);
                return true;
            }

            // As a last resort, try the generic-climate, generic-size version ("ZA");
            if (!generic && tryGenericSize && availableBlocks[blockType, 0].Length > 0)
            {
                Debug.Log("TryGetRandomBlock trying to pick blockType: " + blockType + ", climateType: " + genericVersion);
                randomChance = UnityEngine.Random.Range(0, int.MaxValue);
                avBlocks = availableBlocks[blockType, 0].Length;
                if (wallType != WallType.None) pickedBlock = availableBlocks[blockType, 0][GetWallSubgroup(randomChance % avBlocks, blockType, genericVersion, wallType)];
                else if (isTemple) pickedBlock = GetTempleSubgroup(position, availableBlocks[blockType, 0][randomChance % avBlocks], deity);
                else pickedBlock = availableBlocks[blockType, 0][randomChance % avBlocks];
                if (randomiseRotation) pickedBlock = RandomiseRotation(pickedBlock);
                return true;
            }

            // Trying to get the climate specific version
            int specificVersion = 0;
            int requiredClimate = Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, string.Concat(GetClimateChar(position), "A"));
            if (trySpecific && TryGetSpecificClimate(climateType, requiredClimate, out specificVersion) && availableBlocks[blockType, specificVersion].Length > 0)
            {
                Debug.Log("TryGetRandomBlock trying to pick blockType: " + blockType + ", climateType: " + specificVersion);
                randomChance = UnityEngine.Random.Range(0, int.MaxValue);
                avBlocks = availableBlocks[blockType, specificVersion].Length;
                if (wallType != WallType.None) pickedBlock = availableBlocks[blockType, specificVersion][GetWallSubgroup(randomChance % avBlocks, blockType, specificVersion, wallType)];
                else if (isTemple) pickedBlock = GetTempleSubgroup(position, availableBlocks[blockType, specificVersion][randomChance % avBlocks], deity);
                else pickedBlock = availableBlocks[blockType, specificVersion][randomChance % avBlocks];
                if (randomiseRotation) pickedBlock = RandomiseRotation(pickedBlock);
                return true;
            }

            return false;
        }

        protected bool TryGetGenericSize(int climateType, out int genericSize)
        {
            genericSize = 0;
            if (climateType % 5 == 0) return false;

            genericSize = climateType - climateType % 5;
            return true;
        }

        protected bool TryGetGenericClimate(int climateType, out int genericClimate)
        {
            genericClimate = 0;
            if (climateType < 5) return false;

            genericClimate = climateType % 5;
            return true;
        }

        protected bool TryGetSpecificClimate(int climateType, int requiredClimate, out int specificClimate)
        {
            specificClimate = 0;
            if (climateType >= 5) return false;

            specificClimate = climateType + requiredClimate;
            return true;
        }

        protected string GetTempleSubgroup(DFPosition position, string templeBlock, string deity = "")
        {
            int regionInd = PoliticInfo.ConvertMapPixelToRegionIndex(position.X, position.Y);
            int regionDeity = WorldInfo.WorldSetting.regionTemples[regionInd];
            string mainDeity = GetTempleCode(regionDeity);
            bool isSpecific = !int.TryParse(templeBlock.Substring(6, 2), out int templeProg);

            if (!isSpecific)
                return templeBlock;

            if (deity.Equals(string.Empty) && mainDeity.Equals(templeBlock.Substring(6, 1)))
            {
                if (File.Exists(Path.Combine(MapEditor.testPath, "RMB", templeBlock.Substring(0, 8) + "RMB.json")))
                    return templeBlock;
                else return string.Concat(templeBlock.Substring(0, 6), "0", templeBlock.Substring(7));
            }

            if (!deity.Equals(string.Empty))
            {
                if (File.Exists(Path.Combine(MapEditor.testPath, "RMB", string.Concat(templeBlock.Substring(0, 6), deity, templeBlock.Substring(7)) + "RMB.json")))
                    return string.Concat(templeBlock.Substring(0, 6), deity, templeBlock.Substring(7));
                else return string.Concat(templeBlock.Substring(0, 6), "0", templeBlock.Substring(7));
            }


            int diceRoll = UnityEngine.Random.Range(0, 10);
            if (diceRoll == 0)
            {
                if (File.Exists(Path.Combine(MapEditor.testPath, "RMB", templeBlock.Substring(0, 8) + "RMB.json")))
                    return templeBlock;
                else return string.Concat(templeBlock.Substring(0, 6), "0", templeBlock.Substring(7));
            }


            int climateType = Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, templeBlock.Substring(4, 2));
            string modifiedBlock = string.Concat("TEM0", templeBlock.Substring(4, 2), mainDeity, templeBlock.Substring(7, 1), ".RMB");
            if (availableBlocks[Array.IndexOf(BlockRotatorWindow.blockPrefixes, "TEM"), climateType].Contains(modifiedBlock))
                return modifiedBlock;

            bool templeFound = false;
            do
            {
                modifiedBlock = availableBlocks[Array.IndexOf(BlockRotatorWindow.blockPrefixes, "TEM"), climateType][UnityEngine.Random.Range(0, availableBlocks[Array.IndexOf(BlockRotatorWindow.blockPrefixes, "TEM"), climateType].Length)];
                templeFound = int.TryParse(modifiedBlock.Substring(6, 2), out int uselessValue);
            }
            while (!templeFound);

            return modifiedBlock;
        }

        protected int GetWallSubgroup(int wallProg, int blockType, int climateType, WallType wallType)
        {
            // int[] corner = { 0, 2, 3, 1 };
            // int[] straight = { 4, 7, 6, 5 };
            // int[] gate = { 8, 11, 10, 9 };

            int newWallProg;
            int varCounter = 0;

            while (wallProg >= 12)
            {
                wallProg -= 12;
                varCounter++;
            }

            newWallProg = wallProg % 4;

            switch (wallType)
            {
                case WallType.Wall:
                    newWallProg += 4;
                    break;
                case WallType.Gate:
                    newWallProg += 8;
                    break;
                case WallType.Corner:
                default:
                    break;
            }

            if (varCounter > 0 && availableBlocks[blockType, climateType].Length > newWallProg + 12 * varCounter)
                newWallProg += 12 * varCounter;

            return newWallProg;
        }

        protected string GetClimateChar(DFPosition position)
        {
            if (ClimateInfo.Climate[position.X, position.Y] == (int)MapsFile.Climates.Desert ||
                ClimateInfo.Climate[position.X, position.Y] == (int)MapsFile.Climates.Savannah)
                return "B";

            if (ClimateInfo.Climate[position.X, position.Y] == (int)MapsFile.Climates.Subtropical)
                return "G";

            return "A";
        }

        protected int CountBuildings(string[] blockList)
        {
            int buildingCount = 0;
            for (int i = 0; i < blockList.Length; i++)
            {
                string blockReplacementJson = File.ReadAllText(Path.Combine(MapEditor.testPath, "RMB", string.Concat(blockList[i], ".json")));
                DFBlock block = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), blockReplacementJson);
                // DFBlock block = JsonConvert.DeserializeObject<DFBlock>(File.ReadAllText(Path.Combine(MapEditor.testPath, "RMB", string.Concat(blockList[i], ".json"))));
                buildingCount += block.RmbBlock.FldHeader.BuildingDataList.Length;
            }

            return buildingCount;
        }

        protected string[] PickDungeonExterior(int locationType, DFPosition position, int dungeonType, string dungeonName)
        {
            string[] resultingName = new string[1];
            string dungString = GetDungeonRMBType((DFRegion.DungeonTypes)dungeonType);
            string climateSize = string.Concat(GetClimateChar(position), ConvertSizeToLetter(locationType));

            TryGetRandomBlock(
                position,
                Array.IndexOf(BlockRotatorWindow.blockPrefixes, dungString),
                Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, climateSize),
                out resultingName[0],
                true,
                true);

            return resultingName;
        }

        protected string GetDungeonRMBType(DFRegion.DungeonTypes dungeonType)
        {
            switch (dungeonType)
            {
                case DFRegion.DungeonTypes.Crypt:
                    return "CRY";
                case DFRegion.DungeonTypes.OrcStronghold:
                    return "OSH";
                case DFRegion.DungeonTypes.HumanStronghold:
                    return "HSH";
                case DFRegion.DungeonTypes.Prison:
                    return "PRS";
                case DFRegion.DungeonTypes.DesecratedTemple:
                    return "DST";
                case DFRegion.DungeonTypes.Mine:
                    return "MNE";
                case DFRegion.DungeonTypes.NaturalCave:
                    return "NCV";
                case DFRegion.DungeonTypes.Coven:
                    return "CVN";
                case DFRegion.DungeonTypes.VampireHaunt:
                    return "VPH";
                case DFRegion.DungeonTypes.Laboratory:
                    return "LAB";
                case DFRegion.DungeonTypes.HarpyNest:
                    return "HRN";
                case DFRegion.DungeonTypes.RuinedCastle:
                    return "RNC";
                case DFRegion.DungeonTypes.SpiderNest:
                    return "SPN";
                case DFRegion.DungeonTypes.GiantStronghold:
                    return "GSH";
                case DFRegion.DungeonTypes.DragonsDen:
                    return "DGD";
                case DFRegion.DungeonTypes.BarbarianStronghold:
                    return "BBS";
                case DFRegion.DungeonTypes.VolcanicCaves:
                    return "VCV";
                case DFRegion.DungeonTypes.ScorpionNest:
                    return "SCN";
                default:
                    Debug.Log("GetDungeonRMBType couldn't find dungeon type: returning empty");
                    return string.Empty;
            }
        }

        protected DFLocation.DungeonBlock[] GenerateDungeonInterior(int locationType, int dungeonType, string dungeonName)
        {
            bool[,] diagramProject;
            (int, int)[] dungeonDiagram = GenerateDungeonDiagram(locationType, out diagramProject);
            int startingBlock = UnityEngine.Random.Range(0, dungeonDiagram.Length);
            DFLocation.DungeonBlock[] dungeon = new DFLocation.DungeonBlock[dungeonDiagram.Length];
            (int, int) startingPosition;

            for (int i = 0; i < dungeonDiagram.Length; i++)
            {
                int randomChance = UnityEngine.Random.Range(0, 10) + 1;
                string pickedBlock;

                if (startingBlock == i)
                {
                    pickedBlock = dungeonTypeBlocks[dungeonType][UnityEngine.Random.Range(0, dungeonTypeBlocks[dungeonType].Length)];
                    dungeon[i] = GetBlockData(pickedBlock, dungeonDiagram[i]);
                    dungeon[i].IsStartingBlock = true;
                    startingPosition = dungeonDiagram[i];
                }

                else
                {
                    pickedBlock = dungeonTypeBlocks[dungeonType][UnityEngine.Random.Range(0, dungeonTypeBlocks[dungeonType].Length)];
                    dungeon[i] = GetBlockData(pickedBlock, dungeonDiagram[i]);
                }
            }

            List<DFLocation.DungeonBlock> dungeonList = new List<DFLocation.DungeonBlock>();
            dungeonList = dungeon.ToList();
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    string borderBlock;
                    DFLocation.DungeonBlock block;
                    // Corners are excluded
                    if ((x == 0 || x == 5) && (y == 0 || y == 5))
                        continue;

                    if (!diagramProject[x, y] && CheckAdjacentBlocks(diagramProject, (x, y)))
                    {
                        borderBlock = dungeonTypeBlocks[(int)DFRegion.DungeonTypes.ScorpionNest + 1][UnityEngine.Random.Range(0, dungeonTypeBlocks[(int)DFRegion.DungeonTypes.ScorpionNest + 1].Length)];
                        block = GetBlockData(borderBlock, (x - 2, y - 2));
                        dungeonList.Add(block);
                    }
                }
            }

            dungeon = dungeonList.ToArray();

            return dungeon;
        }

        protected (int, int)[] GenerateDungeonDiagram(int locationType, out bool[,] diagramProject)
        {
            int blocksNumber = 0;
            int scalableSize = UnityEngine.Random.Range(0, 100) + 1;
            int[,] dungeonDiagram = new int[6, 6];
            diagramProject = new bool[6, 6];
            (int, int) coordinates;

            // Setting a new diagramProject
            for (int x = 0; x < 6; x++)
                for (int y = 0; y < 6; y++)
                    diagramProject[x, y] = false;

            switch (locationType)
            {
                case (int)DFRegion.LocationTypes.DungeonRuin:
                    if (scalableSize <= 10)
                        blocksNumber = 1;
                    else if (scalableSize <= 40)
                        blocksNumber = 2;
                    else if (scalableSize <= 70)
                        blocksNumber = 3;
                    else blocksNumber = 4;
                    break;

                case (int)DFRegion.LocationTypes.DungeonKeep:
                    blocksNumber = ((scalableSize - 1) / 25) + 5;
                    break;

                case (int)DFRegion.LocationTypes.DungeonLabyrinth:
                    if (scalableSize < 58)          // 1-57
                        blocksNumber = 9;
                    else if (scalableSize < 81)     // 58-69
                        blocksNumber = 10;
                    else if (scalableSize < 86)     // 70-79
                        blocksNumber = 11;
                    else if (scalableSize < 91)     // 80-87
                        blocksNumber = 12;
                    else if (scalableSize < 95)     // 88-93
                        blocksNumber = 13;
                    else if (scalableSize < 98)     // 94-97
                        blocksNumber = 14;
                    else if (scalableSize < 100)    // 98-99
                        blocksNumber = 15;
                    else if (scalableSize == 100)   // 100
                        blocksNumber = 16;

                    break;

                case (int)DFRegion.LocationTypes.DungeonMinor:
                    blocksNumber = 1;
                    break;
                    // (int, int)[] cemetery = new (int, int)[1];
                    // diagramProject[0, 0] = true;
                    // cemetery[0] = (0, 0);
                    // return cemetery;
            }

            (int, int)[] diagram = new (int, int)[blocksNumber];

            for (int i = 0; i < blocksNumber; i++)
            {
                if (i == 0)
                {
                    coordinates = (UnityEngine.Random.Range(-1, 2), UnityEngine.Random.Range(-1, 2));
                    diagram[i] = coordinates;
                    diagramProject[(coordinates.Item1 + 2), (coordinates.Item2 + 2)] = true;
                }

                else
                {
                    bool positionFound = false;

                    do
                    {
                        coordinates = diagram[UnityEngine.Random.Range(0, i)];
                        int[] freeSectors = GetFreeSectors(coordinates, diagramProject);

                        if (freeSectors.Length > 0)
                        {
                            int direction = freeSectors[UnityEngine.Random.Range(0, freeSectors.Length)];
                            (int, int) newSector;

                            switch (direction)
                            {
                                default:
                                case 0:     // North
                                    newSector.Item1 = coordinates.Item1;
                                    newSector.Item2 = coordinates.Item2 - 1;
                                    break;

                                case 1:     // East
                                    newSector.Item1 = coordinates.Item1 + 1;
                                    newSector.Item2 = coordinates.Item2;
                                    break;

                                case 2:     // South
                                    newSector.Item1 = coordinates.Item1;
                                    newSector.Item2 = coordinates.Item2 + 1;
                                    break;

                                case 3:     // West
                                    newSector.Item1 = coordinates.Item1 - 1;
                                    newSector.Item2 = coordinates.Item2;
                                    break;
                            }

                            diagram[i] = (newSector);
                            diagramProject[newSector.Item1 + 2, newSector.Item2 + 2] = true;
                            positionFound = true;
                        }
                    }
                    while (!positionFound);
                }
            }
            return diagram;
        }

        protected int[] GetFreeSectors((int, int) coordinates, bool[,] diagramProject)
        {
            List<int> availableSectors = new List<int>();
            (int, int) modifiedCoordinates;

            for (int i = 0; i < 4; i++)
            {
                switch (i)
                {
                    default:
                    case 0:     // North
                        modifiedCoordinates.Item1 = coordinates.Item1;
                        modifiedCoordinates.Item2 = coordinates.Item2 - 1;
                        break;

                    case 1:     // East
                        modifiedCoordinates.Item1 = coordinates.Item1 + 1;
                        modifiedCoordinates.Item2 = coordinates.Item2;
                        break;

                    case 2:     // South
                        modifiedCoordinates.Item1 = coordinates.Item1;
                        modifiedCoordinates.Item2 = coordinates.Item2 + 1;
                        break;

                    case 3:     // West
                        modifiedCoordinates.Item1 = coordinates.Item1 - 1;
                        modifiedCoordinates.Item2 = coordinates.Item2;
                        break;
                }

                if (modifiedCoordinates.Item1 > -2 && modifiedCoordinates.Item1 < 3 &&
                    modifiedCoordinates.Item2 > -2 && modifiedCoordinates.Item2 < 3 &&
                    !diagramProject[modifiedCoordinates.Item1 + 2, modifiedCoordinates.Item2 + 2])
                    availableSectors.Add(i);
            }

            return availableSectors.ToArray();
        }

        protected int GetRDBType(int dungeonType, string dungeonName)
        {
            if ((dungeonName.Contains("Castle") ||
                 dungeonName.Contains("Citadel") ||
                 dungeonName.Contains("Hold") ||
                 dungeonName.Contains("Guard") ||
                 dungeonName.Contains("Fortress") ||
                 dungeonName.Contains("Stronghold")) &&
                !dungeonName.Contains("Ruins"))
                return (int)LocationNamesList.RDBTypes.Castle;

            else if (dungeonName.Contains("Tower") &&
                    !dungeonName.Contains("Ruins"))
                return (int)LocationNamesList.RDBTypes.Tower;

            else if ((dungeonName.Contains("Aerie") ||
                      dungeonName.Contains("Aviary") ||
                      dungeonName.Contains("Coop") ||
                      dungeonName.Contains("Mews") ||
                      dungeonName.Contains("Nest") ||
                      dungeonName.Contains("Roost")) &&
                      dungeonType == (int)DFRegion.DungeonTypes.HarpyNest)
                return (int)LocationNamesList.RDBTypes.HarpyNest;

            else if ((dungeonName.Contains("Nest") ||
                      dungeonName.Contains("Web")) &&
                      dungeonType != (int)DFRegion.DungeonTypes.HarpyNest)
                return (int)LocationNamesList.RDBTypes.BugsNest;

            else if (dungeonName.Contains("Den") ||
                     dungeonName.Contains("Barrow") ||
                     dungeonName.Contains("Cavern") ||
                     dungeonName.Contains("Cave") ||
                     dungeonName.Contains("Tunnel") ||
                     dungeonName.Contains("Hole") ||
                     dungeonName.Contains("Pit") ||
                     dungeonName.Contains("Hollow") ||
                     dungeonName.Contains("Grotto") ||
                     dungeonName.Contains("Lair"))
                return (int)LocationNamesList.RDBTypes.Cave;

            else if (dungeonName.Contains("Excavation") ||
                     dungeonName.Contains("Mine") ||
                     dungeonName.Contains("Lode") ||
                     dungeonName.Contains("Quarry"))
                return (int)LocationNamesList.RDBTypes.Mine;

            else if ((dungeonName.Contains("Haunt") ||
                      dungeonName.Contains("House")) &&
                      dungeonType != (int)DFRegion.DungeonTypes.Prison)
                return (int)LocationNamesList.RDBTypes.Haunt;

            else if (dungeonName.Contains("Cabal") ||
                     dungeonName.Contains("Coven") ||
                     dungeonName.Contains("Cult") ||
                     dungeonName.Contains("Cultus") ||
                     dungeonName.Contains("Tradition"))
                return (int)LocationNamesList.RDBTypes.Coven;

            else if (dungeonName.Contains("Laboratory") ||
                     dungeonName.Contains("Academy"))
                return (int)LocationNamesList.RDBTypes.Laboratory;

            else if (dungeonName.Contains("Abbey") ||
                     dungeonName.Contains("Cathedral") ||
                     dungeonName.Contains("Cloister") ||
                     dungeonName.Contains("Convent") ||
                     dungeonName.Contains("Friary") ||
                     dungeonName.Contains("Hermitage") ||
                     dungeonName.Contains("Manse") ||
                     dungeonName.Contains("Minster") ||
                     dungeonName.Contains("Monastery") ||
                     dungeonName.Contains("Rectory") ||
                     dungeonName.Contains("Shrine") ||
                     dungeonName.Contains("Temple"))
                return (int)LocationNamesList.RDBTypes.Temple;

            else if ((dungeonName.Contains("Crypt") ||
                     dungeonName.Contains("Grave") ||
                     dungeonName.Contains("Mausoleum") ||
                     dungeonName.Contains("Sepulcher") ||
                     dungeonName.Contains("Tomb") ||
                     dungeonName.Contains("Vault")) &&
                     dungeonType != (int)DFRegion.DungeonTypes.Cemetery)
                return (int)LocationNamesList.RDBTypes.Crypt;

            else if (dungeonName.Contains("Asylum") ||
                     dungeonName.Contains("Dungeon") ||
                     dungeonName.Contains("Gaol") ||
                     dungeonName.Contains("House of Correction") ||
                     dungeonName.Contains("Penitentiary") ||
                     dungeonName.Contains("Prison") ||
                     dungeonName.Contains("Reformatory"))
                return (int)LocationNamesList.RDBTypes.Prison;

            else if (dungeonName.Contains("Ruins"))
                return (int)LocationNamesList.RDBTypes.Ruins;

            else if (dungeonName.Contains("Burial Ground") ||
                     dungeonName.Contains("Cemetery") ||
                     dungeonName.Contains("Crypts") ||
                     dungeonName.Contains("Graveyard") ||
                     dungeonName.Contains("Graves") ||
                     dungeonName.Contains("Tombs") ||
                     dungeonName.Contains("Vaults"))
                return (int)LocationNamesList.RDBTypes.Cemetery;

            else return (int)LocationNamesList.RDBTypes.Castle;
        }

        protected int GetRDBType(int dungeonType)
        {
            switch (dungeonType)
            {
                case (int)DFRegion.DungeonTypes.Crypt:
                    return (int)LocationNamesList.RDBTypes.Crypt;

                default:
                case (int)DFRegion.DungeonTypes.OrcStronghold:
                case (int)DFRegion.DungeonTypes.HumanStronghold:
                case (int)DFRegion.DungeonTypes.GiantStronghold:
                case (int)DFRegion.DungeonTypes.BarbarianStronghold:
                    return (int)LocationNamesList.RDBTypes.Castle;

                case (int)DFRegion.DungeonTypes.Prison:
                    return (int)LocationNamesList.RDBTypes.Prison;

                case (int)DFRegion.DungeonTypes.DesecratedTemple:
                    return (int)LocationNamesList.RDBTypes.Temple;

                case (int)DFRegion.DungeonTypes.Mine:
                    return (int)LocationNamesList.RDBTypes.Mine;

                case (int)DFRegion.DungeonTypes.NaturalCave:
                    return (int)LocationNamesList.RDBTypes.Cave;

                case (int)DFRegion.DungeonTypes.Coven:
                    return (int)LocationNamesList.RDBTypes.Coven;

                case (int)DFRegion.DungeonTypes.VampireHaunt:
                    return (int)LocationNamesList.RDBTypes.Haunt;

                case (int)DFRegion.DungeonTypes.Laboratory:
                    return (int)LocationNamesList.RDBTypes.Laboratory;

                case (int)DFRegion.DungeonTypes.HarpyNest:
                    return (int)LocationNamesList.RDBTypes.HarpyNest;

                case (int)DFRegion.DungeonTypes.RuinedCastle:
                    return (int)LocationNamesList.RDBTypes.Ruins;

                case (int)DFRegion.DungeonTypes.SpiderNest:
                case (int)DFRegion.DungeonTypes.ScorpionNest:
                    return (int)LocationNamesList.RDBTypes.BugsNest;

                case (int)DFRegion.DungeonTypes.DragonsDen:
                    return (int)LocationNamesList.RDBTypes.DragonsDen;

                case (int)DFRegion.DungeonTypes.VolcanicCaves:
                    return (int)LocationNamesList.RDBTypes.VolcanicCaves;

                case (int)DFRegion.DungeonTypes.Cemetery:
                    return (int)LocationNamesList.RDBTypes.Cemetery;
            }
        }

        protected DFLocation.DungeonBlock GetBlockData(string blockName, (int, int) position)
        {
            DFLocation.DungeonBlock block = new DFLocation.DungeonBlock();
            DFBlock rdBlock = new DFBlock();
            string blockReplacementJson = File.ReadAllText(Path.Combine(MapEditor.testPath, "RDB", string.Concat(blockName, ".json")));
            block = (DFLocation.DungeonBlock)SaveLoadManager.Deserialize(typeof(DFLocation.DungeonBlock), blockReplacementJson);
            // block = JsonConvert.DeserializeObject<DFLocation.DungeonBlock>(File.ReadAllText(Path.Combine(MapEditor.testPath, "RDB", string.Concat(blockName, ".json"))));
            rdBlock = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), blockReplacementJson);
            block.X = (sbyte)position.Item1;
            block.Z = (sbyte)position.Item2;
            block.IsStartingBlock = false;
            block.BlockName = blockName;
            block.WaterLevel = 10000;

            // for (int i = 0; i < rdBlock.RdbBlock.ObjectRootList.Length; i++)
            // {
            //     for (int j = 0; j < rdBlock.RdbBlock.ObjectRootList[i].RdbObjects.Length; j++)
            //     {
            //         if (rdBlock.RdbBlock.ObjectRootList[i].RdbObjects[j].Resources.FlatResource.TextureArchive == 199 &&
            //             rdBlock.RdbBlock.ObjectRootList[i].RdbObjects[j].Resources.FlatResource.TextureRecord == 10)
            //         {
            //             block.WaterLevel = rdBlock.RdbBlock.ObjectRootList[i].RdbObjects[j].Resources.FlatResource.SoundIndex;
            //             break;
            //         }
            //     }
            // }
            block.CastleBlock = false;

            return block;
        }

        protected bool CheckAdjacentBlocks(bool[,] dungeonDiagram, (int, int) position)
        {
            if (position.Item2 > 0 && dungeonDiagram[position.Item1, position.Item2 - 1])
                return true;
            if (position.Item1 < 5 && dungeonDiagram[position.Item1 + 1, position.Item2])
                return true;
            if (position.Item2 < 5 && dungeonDiagram[position.Item1, position.Item2 + 1])
                return true;
            if (position.Item1 > 0 && dungeonDiagram[position.Item1 - 1, position.Item2])
                return true;

            return false;
        }

        protected List<ushort> InitializeLocationIdList()
        {
            List<ushort> initializedList = new List<ushort>();
            bool lowestCatched = false;

            ushort lowestValue = 0;
            ushort highestValue = 0;

            int counter = 0;

            while (!lowestCatched)
            {
                if (Worldmaps.mapDict.ContainsKey((ulong)counter))
                {
                    highestValue = lowestValue = (ushort)Worldmaps.mapDict[(ulong)counter].MapID;
                    Debug.Log("Lowest Value set to " + lowestValue + " (at counter" + counter + ")");
                    lowestCatched = true;
                    initializedList.Add((ushort)Worldmaps.mapDict[(ulong)counter].MapID);
                }

                counter++;
            }

            for (int i = counter; i < int.MaxValue; i++)
            {
                if (Worldmaps.mapDict.ContainsKey((ulong)i))
                {
                    if (lowestValue > (ushort)Worldmaps.mapDict[(ulong)i].MapID)
                        lowestValue = (ushort)Worldmaps.mapDict[(ulong)i].MapID;

                    if (highestValue < (ushort)Worldmaps.mapDict[(ulong)i].MapID)
                        highestValue = (ushort)Worldmaps.mapDict[(ulong)i].MapID;

                    initializedList.Add((ushort)Worldmaps.mapDict[(ulong)i].MapID);
                }
            }

            initializedList.Sort();

            Debug.Log("Lowest MapID: " + lowestValue + "(" + initializedList[0] + "); Highest MapID: " + highestValue + "(" + initializedList[initializedList.Count - 1] + ")");

            return initializedList;
        }

        protected void InitializeLocationDensity()
        {
            for (int i = 0; i < (int)DFRegion.LocationTypes.HomeYourShips; i++)
            {
                regionOptions.locationDensity[i] = 0;

                for (int j = 0; j <= ((int)MapsFile.Climates.Steppes - (int)MapsFile.Climates.Ocean); j++)
                {
                    regionOptions.locationDensity[i] += (locationDensity[j, i] * governmentModifier[(RegionManager.currentRegionData.governType + 1) / 2, i] * ((float)RegionManager.currentRegionData.climates[j] * 100.0f / (float)RegionManager.currentRegionData.surface)) / 100.0f;
                }

                regionOptions.locationDensity[i] += (UnityEngine.Random.Range(-0.10f, 0.10f) * regionOptions.locationDensity[i]);
            }
        }

        protected void InitializeAvailableBlocks()
        {
            availableBlocks = new string[BlockRotatorWindow.blockPrefixes.Length, BlockRotatorWindow.rmbBlockClimate.Length][];
            List<string>[] workingList = new List<string>[BlockRotatorWindow.blockPrefixes.Length * BlockRotatorWindow.rmbBlockClimate.Length];
            for (int h = 0; h < BlockRotatorWindow.blockPrefixes.Length * BlockRotatorWindow.rmbBlockClimate.Length; h++)
                workingList[h] = new List<string>();

            DirectoryInfo RMBFolder = new DirectoryInfo(Path.Combine(MapEditor.testPath, "RMB"));
            FileInfo[] RMBfiles = RMBFolder.GetFiles("*.RMB.json");

            foreach (FileInfo RMB in RMBfiles)
            {
                if (!int.TryParse(RMB.Name.Substring(3, 1), out int zeroValue))
                    continue;
                int rmbType = Array.IndexOf(BlockRotatorWindow.blockPrefixes, RMB.Name.Substring(0, 3));
                int rmbCat = Array.IndexOf(BlockRotatorWindow.rmbBlockClimate, RMB.Name.Substring(4, 2));

                workingList[rmbType + BlockRotatorWindow.blockPrefixes.Length * rmbCat].Add(RMB.Name.Substring(0, 12));
            }

            for (int i = 0; i < BlockRotatorWindow.rmbBlockClimate.Length; i++)
            {
                for (int j = 0; j < BlockRotatorWindow.blockPrefixes.Length; j++)
                {
                    availableBlocks[j, i] = workingList[j + BlockRotatorWindow.blockPrefixes.Length * i].ToArray();
                }
            }
        }

        protected void InitializeWorldData()
        {
            worldData = new WorldSettings();
            string filePath = string.Concat(MapEditor.testPath, "WorldData.json");
            worldData = JsonConvert.DeserializeObject<WorldSettings>(File.ReadAllText(filePath));
        }

        public static ushort GenerateNewLocationId()
        {
            bool found = false;
            ushort counter = locationIdList[0];

            do
            {
                counter += 2;

                if (!locationIdList.Contains(counter) && !locationIdList.Contains((ushort)(counter + 1)))
                {
                    found = true;
                }
            }
            while (!found);

            locationIdList.Add(counter);

            return counter;
        }

        protected void InitializeLocationNames()
        {
            string path = Path.Combine(MapEditor.testPath, "LocationNames", BRETIC);
            highRockLocNames = JsonConvert.DeserializeObject<BlockRotatorWindow.HighRockLocNames>(File.ReadAllText(path));

            path = Path.Combine(MapEditor.testPath, "LocationNames", YOKU);
            hammerfellLocNames = JsonConvert.DeserializeObject<BlockRotatorWindow.HammerfellLocNames>(File.ReadAllText(path));

            path = Path.Combine(MapEditor.testPath, "LocationNames", NORD);
            skyrimLocNames = JsonConvert.DeserializeObject<BlockRotatorWindow.SkyrimLocNames>(File.ReadAllText(path));

            path = Path.Combine(MapEditor.testPath, "LocationNames", REACHMEN);
            reachLocNames = JsonConvert.DeserializeObject<BlockRotatorWindow.ReachLocNames>(File.ReadAllText(path));

            path = Path.Combine(MapEditor.testPath, "LocationNames", DUNMER);
            morrowindLocNames = JsonConvert.DeserializeObject<BlockRotatorWindow.MorrowindLocNames>(File.ReadAllText(path));

            path = Path.Combine(MapEditor.testPath, "LocationNames", ORSIMER);
            orsiniumLocNames = JsonConvert.DeserializeObject<BlockRotatorWindow.OrsiniumLocNames>(File.ReadAllText(path));
        }

        protected BlockRotatorWindow.GeoLocations InitializeGeoLocation(DFPosition position, MapsFile.Climates climate, DFRegion.LocationTypes locationType)
        {
            geoLocations = new BlockRotatorWindow.GeoLocations
            {
                City = false,
                Town = false,
                Coastal = false,
                Forest = false,
                Island = false,
                Mountain = false,
                Plains = false,
                River = false,
                Swamp = false,
                Valley = false,
                Cave = false,
                Desert = false,
                Lake = false
            };

            int altitude = SmallHeightmap.GetHeightMapValue(position);
            int region = PoliticInfo.ConvertMapPixelToRegionIndex(position.X, position.Y);

            switch (locationType)
            {
                case DFRegion.LocationTypes.TownCity:
                    geoLocations.City = true;
                    break;
                case DFRegion.LocationTypes.TownHamlet:
                case DFRegion.LocationTypes.TownVillage:
                    geoLocations.Town = true;
                    break;
                default:
                    break;
            }
            switch (climate)
            {
                case MapsFile.Climates.Desert:
                case MapsFile.Climates.Savannah:
                    geoLocations.Desert = true;
                    break;
                case MapsFile.Climates.Mountain:
                    geoLocations.Mountain = true;
                    if (altitude < 20)
                        geoLocations.Valley = true;
                    break;
                case MapsFile.Climates.Swamp:
                    geoLocations.Swamp = true;
                    break;
                case MapsFile.Climates.MountainWoods:
                    geoLocations.Forest = true;
                    geoLocations.Mountain = true;
                    if (altitude < 20)
                        geoLocations.Valley = true;
                    break;
                case MapsFile.Climates.Woodlands:
                case MapsFile.Climates.Steppes:
                    geoLocations.Forest = true;
                    if (altitude < 10)
                        geoLocations.Plains = true;
                    break;
                // Still to do: Island, River and Cave
                default:
                    break;
            }

            for (int y = -2; y <= 2; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    DFPosition posToCheck = new DFPosition(position.X + x, position.Y + y);
                    // Debug.Log("posToCheck: " + posToCheck.X + ", " + posToCheck.Y);
                    altitude = SmallHeightmap.GetHeightMapValue(posToCheck);
                    region = PoliticInfo.ConvertMapPixelToRegionIndex(posToCheck.X, posToCheck.Y);
                    if (altitude < 4)
                    {
                        if (region == 31)
                            geoLocations.Coastal = true;
                        else geoLocations.Lake = true;
                    }
                }
            }

            return geoLocations;
        }

        protected string HighRockTownName()
        {
            BreticFormulas nameStructure = (BreticFormulas)UnityEngine.Random.Range(0, Enum.GetNames(typeof(BreticFormulas)).Length);
            string partA, partB, partC, partD;
            partA = partB = partC = partD = string.Empty;
            int[] randomValues = new int[3];

            for (int i = 0; i < 3; i++)
            {
                randomValues[i] = UnityEngine.Random.Range(0, int.MaxValue);
            }

            switch (nameStructure)
            {
                case BreticFormulas.Classic:
                    partA = highRockLocNames.ClassicPrefix[randomValues[0] % highRockLocNames.ClassicPrefix.Length];
                    partB = highRockLocNames.ClassicSuffix[randomValues[1] % highRockLocNames.ClassicSuffix.Length];
                    if (UnityEngine.Random.Range(0, 10) == 0)
                        partC = " " + highRockLocNames.ClassicExtra[randomValues[2] % highRockLocNames.ClassicExtra.Length];
                    return partA + partB + partC;

                case BreticFormulas.Modern:
                    partA = highRockLocNames.ModernPrefix[randomValues[0] % highRockLocNames.ModernPrefix.Length];
                    partB = highRockLocNames.ModernSuffix[randomValues[1] % highRockLocNames.ModernSuffix.Length];
                    if (UnityEngine.Random.Range(0, 10) == 0)
                        partC = " " + highRockLocNames.ModernExtra[randomValues[2] % highRockLocNames.ModernExtra.Length];
                    return partA + partB + partC;

                case BreticFormulas.PrefixSuffix:
                    partA = highRockLocNames.Prefix[randomValues[0] % highRockLocNames.Prefix.Length];
                    partB = GetSuffix(randomValues[1]);
                    return partA + partB;

                case BreticFormulas.NounSuffix:
                    partA = highRockLocNames.Noun[randomValues[0] % highRockLocNames.Noun.Length];
                    partB = GetSuffix(randomValues[1]);
                    return partA + partB;

                case BreticFormulas.AdjectiveNoun:
                    partA = highRockLocNames.Adjective[randomValues[0] % highRockLocNames.Adjective.Length];
                    partB = highRockLocNames.Noun[randomValues[1] % highRockLocNames.Noun.Length].ToLower();
                    return partA + partB;

                case BreticFormulas.Noun_by_the_Geo:
                    partA = highRockLocNames.Noun[randomValues[0] % highRockLocNames.Noun.Length];
                    partB = highRockLocNames.Extra[0];
                    partC = highRockLocNames.Extra[2];
                    partD = GetGeoSuffix(randomValues[1]);
                    return partA + " " + partB + " " + partC + " " + partD;

                case BreticFormulas.Noun_of_the_Geo:
                    partA = highRockLocNames.Noun[randomValues[0] % highRockLocNames.Noun.Length];
                    partB = highRockLocNames.Extra[1];
                    partC = highRockLocNames.Extra[2];
                    partD = GetGeoSuffix(randomValues[1]);
                    return partA + " " + partB + " " + partC + " " + partD;

                case BreticFormulas.Geo_of_Noun:
                    partA = GetGeoSuffix(randomValues[1]);
                    partB = highRockLocNames.Extra[1];
                    partC = highRockLocNames.Noun[randomValues[0] % highRockLocNames.Noun.Length];
                    return partA + " " + partB + " " + partC;

                case BreticFormulas.PrefixNoun:
                    partA = highRockLocNames.Prefix[randomValues[0] % highRockLocNames.Prefix.Length];
                    partB = highRockLocNames.Noun[randomValues[1] % highRockLocNames.Noun.Length].ToLower();
                    return partA + partB;

                case BreticFormulas.NounCiv:
                    partA = highRockLocNames.Noun[randomValues[0] % highRockLocNames.Noun.Length];
                    partB = GetCivSuffix(randomValues[1]).ToLower();
                    return partA + partB;

                case BreticFormulas.NounGeo:
                    partA = highRockLocNames.Noun[randomValues[0] % highRockLocNames.Noun.Length];
                    partB = GetGeoSuffix(randomValues[1]).ToLower();
                    return partA + partB;

                case BreticFormulas.AdjectiveNounSuffix:
                    partA = highRockLocNames.Adjective[randomValues[0] % highRockLocNames.Adjective.Length];
                    partB = highRockLocNames.Noun[randomValues[1] % highRockLocNames.Noun.Length].ToLower();
                    partC = GetSuffix(randomValues[1]).ToLower();
                    return partA + partB + partC;

                case BreticFormulas.NounNoun:
                    partA = highRockLocNames.Noun[randomValues[0] % highRockLocNames.Noun.Length];
                    partB = highRockLocNames.Noun[randomValues[1] % highRockLocNames.Noun.Length].ToLower();
                    return partA + partB;

                case BreticFormulas.Noun_upon_Noun:
                    partA = highRockLocNames.Noun[randomValues[0] % highRockLocNames.Noun.Length];
                    partB = highRockLocNames.Extra[3];
                    partC = highRockLocNames.Noun[randomValues[1] % highRockLocNames.Noun.Length];
                    return partA + " " + partB + " " + partC;

                case BreticFormulas.NounPrefix:
                    partA = highRockLocNames.Noun[randomValues[0] % highRockLocNames.Noun.Length];
                    partB = highRockLocNames.Prefix[randomValues[0] % highRockLocNames.Prefix.Length].ToLower();
                    return partA + partB;

                case BreticFormulas.AdjectiveCiv:
                    partA = highRockLocNames.Adjective[randomValues[0] % highRockLocNames.Adjective.Length];
                    partB = GetCivSuffix(randomValues[1]).ToLower();
                    return partA + partB;

                case BreticFormulas.AdjectiveGeo:
                    partA = highRockLocNames.Adjective[randomValues[0] % highRockLocNames.Adjective.Length];
                    partB = GetGeoSuffix(randomValues[1]).ToLower();
                    return partA + partB;
            }
            return "Error";
        }

        protected string HammerfellTownName()
        {
            YokuFormulas nameStructure = (YokuFormulas)UnityEngine.Random.Range(0, Enum.GetNames(typeof(YokuFormulas)).Length);
            string partA, partB, partC, partD, partE;
            partA = partB = partC = partD = partE = string.Empty;
            int numSuffix = UnityEngine.Random.Range(0, hammerfellTownVowel);
            int[] randomValues = new int[3];

            for (int i = 0; i < 3; i++)
            {
                randomValues[i] = UnityEngine.Random.Range(0, int.MaxValue);
            }

            switch (nameStructure)
            {
                case YokuFormulas.Classic:
                    int classicStruct = UnityEngine.Random.Range(0, 4);
                    partA = hammerfellLocNames.ClassicPrefix[randomValues[0] % hammerfellLocNames.ClassicPrefix.Length];
                    switch (classicStruct)
                    {
                        case 0:     // Prefix + vowel + Suffix1 + Suffix2
                        case 1:     //  Prefix + vowel + Suffix1 + Extra
                            partB = PickVowel(numSuffix, hammerfellLocNames.ClassicSuffix1, GenerateVowelArray(hammerfellLocNames.ClassicSuffix1), true);
                            break;

                        case 2:     // Prefix + vowel + Suffix2
                        case 3:     // Prefix + vowel + Extra
                            partB = PickVowel(numSuffix, hammerfellLocNames.ClassicSuffix1, GenerateVowelArray(hammerfellLocNames.ClassicSuffix1), false);
                            break;

                    }
                    switch (classicStruct)
                    {
                        case 0:
                        case 2:
                            partC = hammerfellLocNames.ClassicSuffix2[randomValues[1] % hammerfellLocNames.ClassicSuffix2.Length];
                            break;

                        case 1:
                        case 3:
                            partD = hammerfellLocNames.ClassicExtra[randomValues[2] % hammerfellLocNames.ClassicExtra.Length];
                            break;
                    }
                    return partA + partB + partC;

                case YokuFormulas.PrefixSuffix:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = hammerfellLocNames.Suffix[randomValues[1] % hammerfellLocNames.Suffix.Length];
                    return partA + partB;

                case YokuFormulas.PrefixInterfixSuffix:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = hammerfellLocNames.Interfix[randomValues[1] % hammerfellLocNames.Interfix.Length];
                    partC = hammerfellLocNames.Suffix[randomValues[2] % hammerfellLocNames.Suffix.Length];
                    return partA + partB + partC;

                case YokuFormulas.AdjectiveCivNoun:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = GetCivSuffix(randomValues[1]).ToLower();
                    partC = hammerfellLocNames.Noun[randomValues[2] % hammerfellLocNames.Noun.Length].ToLower();
                    return partA + partB + partC;

                case YokuFormulas.AdjectiveGeoNoun:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = GetGeoSuffix(randomValues[1]).ToLower();
                    partC = hammerfellLocNames.Noun[randomValues[2] % hammerfellLocNames.Noun.Length].ToLower();
                    return partA + partB + partC;

                case YokuFormulas.AdjectiveSuffix:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = hammerfellLocNames.Suffix[randomValues[1] % hammerfellLocNames.Suffix.Length];
                    return partA + partB;

                case YokuFormulas.AdjectiveInterfixSuffix:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = hammerfellLocNames.Interfix[randomValues[1] % hammerfellLocNames.Interfix.Length];
                    partC = hammerfellLocNames.Suffix[randomValues[2] % hammerfellLocNames.Suffix.Length];
                    return partA + partB + partC;

                case YokuFormulas.PrefixCivNoun:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = GetCivSuffix(randomValues[1]).ToLower();
                    partC = hammerfellLocNames.Noun[randomValues[2] % hammerfellLocNames.Noun.Length].ToLower();
                    return partA + partB + partC;

                case YokuFormulas.PrefixGeoNoun:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = GetGeoSuffix(randomValues[1]).ToLower();
                    partC = hammerfellLocNames.Noun[randomValues[2] % hammerfellLocNames.Noun.Length].ToLower();
                    return partA + partB + partC;

                case YokuFormulas.Number_Noun:
                    partA = hammerfellLocNames.Number[randomValues[0] % hammerfellLocNames.Number.Length];
                    partB = hammerfellLocNames.Noun[randomValues[1] % hammerfellLocNames.Noun.Length].ToLower();
                    return partA + " " + partB;

                case YokuFormulas.Adjective_CivNoun:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = GetCivSuffix(randomValues[1]);
                    partC = hammerfellLocNames.Noun[randomValues[2] % hammerfellLocNames.Noun.Length].ToLower();
                    return partA + " " + partB + partC;

                case YokuFormulas.Adjective_GeoNoun:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = GetGeoSuffix(randomValues[1]);
                    partC = hammerfellLocNames.Noun[randomValues[2] % hammerfellLocNames.Noun.Length].ToLower();
                    return partA + " " + partB + partC;

                case YokuFormulas.Adjective_PrefixSuffix:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = hammerfellLocNames.Prefix[randomValues[1] % hammerfellLocNames.Prefix.Length];
                    partC = hammerfellLocNames.Suffix[randomValues[2] % hammerfellLocNames.Suffix.Length];
                    return partA + " " + partB + partC;

                case YokuFormulas.Prefix_CivNoun:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = GetCivSuffix(randomValues[1]);
                    partC = hammerfellLocNames.Noun[randomValues[2] % hammerfellLocNames.Noun.Length].ToLower();
                    return partA + " " + partB + partC;

                case YokuFormulas.Prefix_GeoNoun:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = GetGeoSuffix(randomValues[1]);
                    partC = hammerfellLocNames.Noun[randomValues[2] % hammerfellLocNames.Noun.Length].ToLower();
                    return partA + " " + partB + partC;

                case YokuFormulas.Adjective_Verb_Noun:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = hammerfellLocNames.Verb[randomValues[1] % hammerfellLocNames.Verb.Length];
                    partC = hammerfellLocNames.Noun[randomValues[2] % hammerfellLocNames.Noun.Length];
                    return partA + " " + partB + " " + partC;

                case YokuFormulas.Name_Of_The_Geo:
                    partA = hammerfellLocNames.Name[randomValues[0] % hammerfellLocNames.Name.Length];
                    partB = hammerfellLocNames.Extra[1];
                    partC = hammerfellLocNames.Extra[UnityEngine.Random.Range(2, 4)];
                    partD = GetGeoSuffix(randomValues[1]);
                    return partA + " " + partB + " " + partC + " " + partD;

                case YokuFormulas.Name_By_The_Geo:
                    partA = hammerfellLocNames.Name[randomValues[0] % hammerfellLocNames.Name.Length];
                    partB = hammerfellLocNames.Extra[0];
                    partC = hammerfellLocNames.Extra[UnityEngine.Random.Range(2, 4)];
                    partD = GetGeoSuffix(randomValues[1]);
                    return partA + " " + partB + " " + partC + " " + partD;

                case YokuFormulas.Civ_By_The_Adjective_Geo:
                    partA = GetCivSuffix(randomValues[0]);
                    partB = hammerfellLocNames.Extra[0];
                    partC = hammerfellLocNames.Extra[UnityEngine.Random.Range(2, 4)];
                    partD = hammerfellLocNames.Adjective[randomValues[1] % hammerfellLocNames.Adjective.Length];
                    partE = GetGeoSuffix(randomValues[2]);
                    return partA + " " + partB + " " + partC + " " + partD + " " + partE;

                case YokuFormulas.Noun_By_The_Geo:
                    partA = hammerfellLocNames.Noun[randomValues[0] % hammerfellLocNames.Noun.Length];
                    partB = hammerfellLocNames.Extra[0];
                    partC = hammerfellLocNames.Extra[UnityEngine.Random.Range(2, 4)];
                    partD = GetGeoSuffix(randomValues[1]);
                    return partA + " " + partB + " " + partC + " " + partD;

                case YokuFormulas.Noun_Of_Name:
                    partA = hammerfellLocNames.Noun[randomValues[0] % hammerfellLocNames.Noun.Length];
                    partB = hammerfellLocNames.Extra[1];
                    partC = hammerfellLocNames.Name[randomValues[1] % hammerfellLocNames.Name.Length];
                    return partA + " " + partB + " " + partC;

                case YokuFormulas.Adjective_Noun_By_The_Geo:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = hammerfellLocNames.Noun[randomValues[1] % hammerfellLocNames.Noun.Length];
                    partB = hammerfellLocNames.Extra[0];
                    partC = hammerfellLocNames.Extra[UnityEngine.Random.Range(2, 4)];
                    partD = GetGeoSuffix(randomValues[2]);
                    return partA + " " + partB + " " + partC + " " + partD + " " + partE;

                case YokuFormulas.Adjective_Noun_Of_Name:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = hammerfellLocNames.Noun[randomValues[1] % hammerfellLocNames.Noun.Length];
                    partC = hammerfellLocNames.Extra[1];
                    partD = hammerfellLocNames.Name[randomValues[2] % hammerfellLocNames.Name.Length];
                    return partA + " " + partB + " " + partC + " " + partD;

                case YokuFormulas.PrefixSuffix_Civ:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = hammerfellLocNames.Suffix[randomValues[1] % hammerfellLocNames.Suffix.Length];
                    partC = GetCivSuffix(randomValues[2]);
                    return partA + partB + " " + partC;

                case YokuFormulas.PrefixSuffix_Geo:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = hammerfellLocNames.Suffix[randomValues[1] % hammerfellLocNames.Suffix.Length];
                    partC = GetGeoSuffix(randomValues[2]);
                    return partA + partB + " " + partC;

                case YokuFormulas.AdjectiveNoun:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = hammerfellLocNames.Noun[randomValues[1] % hammerfellLocNames.Noun.Length].ToLower();
                    return partA + partB;

                case YokuFormulas.The_Adjective_Noun_Geo:
                    partA = hammerfellLocNames.Extra[UnityEngine.Random.Range(2, 4)];
                    partB = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partC = hammerfellLocNames.Noun[randomValues[1] % hammerfellLocNames.Noun.Length];
                    partD = GetGeoSuffix(randomValues[2]);
                    return partA + " " + partB + " " + partC + " " + partD;

                case YokuFormulas.The_Adjective_Noun_Civ:
                    partA = hammerfellLocNames.Extra[UnityEngine.Random.Range(2, 4)];
                    partB = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partC = hammerfellLocNames.Noun[randomValues[1] % hammerfellLocNames.Noun.Length];
                    partD = GetCivSuffix(randomValues[2]);
                    return partA + " " + partB + " " + partC + " " + partD;

                case YokuFormulas.The_PrefixSuffix:
                    partA = hammerfellLocNames.Extra[UnityEngine.Random.Range(2, 4)];
                    partB = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partC = hammerfellLocNames.Suffix[randomValues[1] % hammerfellLocNames.Suffix.Length];
                    return partA + " " + partB + partC;

                case YokuFormulas.The_PrefixInterfixSuffix:
                    partA = hammerfellLocNames.Extra[UnityEngine.Random.Range(2, 4)];
                    partB = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partC = hammerfellLocNames.Interfix[randomValues[1] % hammerfellLocNames.Interfix.Length];
                    partD = hammerfellLocNames.Suffix[randomValues[2] % hammerfellLocNames.Suffix.Length];
                    return partA + " " + partB + partC + partD;

                case YokuFormulas.PrefixInterfixNoun:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = hammerfellLocNames.Interfix[randomValues[1] % hammerfellLocNames.Interfix.Length];
                    partC = hammerfellLocNames.Noun[randomValues[1] % hammerfellLocNames.Noun.Length].ToLower();
                    return partA + partB + partC;

                case YokuFormulas.NounInterfixSuffix:
                    partA = hammerfellLocNames.Noun[randomValues[0] % hammerfellLocNames.Noun.Length];
                    partB = hammerfellLocNames.Interfix[randomValues[1] % hammerfellLocNames.Interfix.Length];
                    partC = hammerfellLocNames.Suffix[randomValues[2] % hammerfellLocNames.Suffix.Length];
                    return partA + partB + partC;

                case YokuFormulas.NounSuffix:
                    partA = hammerfellLocNames.Noun[randomValues[0] % hammerfellLocNames.Noun.Length];
                    partB = hammerfellLocNames.Suffix[randomValues[1] % hammerfellLocNames.Suffix.Length];
                    return partA + partB;

                case YokuFormulas.NounXPrefixSuffix:
                    partA = hammerfellLocNames.Noun[randomValues[0] % hammerfellLocNames.Noun.Length];
                    partB = hammerfellLocNames.Prefix[randomValues[1] % hammerfellLocNames.Prefix.Length];
                    partC = hammerfellLocNames.Suffix[randomValues[2] % hammerfellLocNames.Suffix.Length];
                    return partA + "-" + partB + partC;

                case YokuFormulas.Noun_PrefixSuffix:
                    partA = hammerfellLocNames.Noun[randomValues[0] % hammerfellLocNames.Noun.Length];
                    partB = hammerfellLocNames.Prefix[randomValues[1] % hammerfellLocNames.Prefix.Length];
                    partC = hammerfellLocNames.Suffix[randomValues[2] % hammerfellLocNames.Suffix.Length];
                    return partA + " " + partB + partC;

                case YokuFormulas.NounInterfix:
                    partA = hammerfellLocNames.Noun[randomValues[0] % hammerfellLocNames.Noun.Length];
                    partB = hammerfellLocNames.Interfix[randomValues[1] % hammerfellLocNames.Interfix.Length];
                    return partA + partB;

                case YokuFormulas.AdjectiveGeo:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = GetGeoSuffix(randomValues[1]).ToLower();
                    return partA + partB;

                case YokuFormulas.AdjectiveCiv:
                    partA = hammerfellLocNames.Adjective[randomValues[0] % hammerfellLocNames.Adjective.Length];
                    partB = GetCivSuffix(randomValues[1]).ToLower();
                    return partA + partB;

                case YokuFormulas.PrefixInterfix:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = hammerfellLocNames.Interfix[randomValues[1] % hammerfellLocNames.Interfix.Length];
                    return partA + partB;

                case YokuFormulas.PrefixCiv:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = GetCivSuffix(randomValues[1]).ToLower();
                    return partA + partB;

                case YokuFormulas.PrefixGeo:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = GetGeoSuffix(randomValues[1]).ToLower();
                    return partA + partB;

                case YokuFormulas.NounGeo:
                    partA = hammerfellLocNames.Noun[randomValues[0] % hammerfellLocNames.Noun.Length];
                    partB = GetGeoSuffix(randomValues[1]).ToLower();
                    return partA + partB;

                case YokuFormulas.NounCiv:
                    partA = hammerfellLocNames.Noun[randomValues[0] % hammerfellLocNames.Noun.Length];
                    partB = GetCivSuffix(randomValues[1]).ToLower();
                    return partA + partB;

                case YokuFormulas.PrefixXCiv:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = GetCivSuffix(randomValues[1]);
                    return partA + "-" + partB;

                case YokuFormulas.PrefixXGeo:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = GetGeoSuffix(randomValues[1]);
                    return partA + "-" + partB;

                case YokuFormulas.NameXGeo:
                    partA = hammerfellLocNames.Name[randomValues[0] % hammerfellLocNames.Name.Length];
                    partB = GetGeoSuffix(randomValues[1]);
                    return partA + "-" + partB;

                case YokuFormulas.NameXCiv:
                    partA = hammerfellLocNames.Name[randomValues[0] % hammerfellLocNames.Name.Length];
                    partB = GetCivSuffix(randomValues[1]);
                    return partA + "-" + partB;

                case YokuFormulas.NameCiv:
                    partA = hammerfellLocNames.Name[randomValues[0] % hammerfellLocNames.Name.Length];
                    partB = GetCivSuffix(randomValues[1]).ToLower();
                    return partA + partB;

                case YokuFormulas.NameGeo:
                    partA = hammerfellLocNames.Name[randomValues[0] % hammerfellLocNames.Name.Length];
                    partB = GetGeoSuffix(randomValues[1]);
                    return partA + partB;

                case YokuFormulas.NameSuffix:
                    partA = hammerfellLocNames.Name[randomValues[0] % hammerfellLocNames.Name.Length];
                    partB = hammerfellLocNames.Suffix[randomValues[1] % hammerfellLocNames.Suffix.Length];
                    return partA + partB;

                case YokuFormulas.PrefixInterfixInterfix:
                    partA = hammerfellLocNames.Prefix[randomValues[0] % hammerfellLocNames.Prefix.Length];
                    partB = hammerfellLocNames.Interfix[randomValues[1] % hammerfellLocNames.Interfix.Length];
                    partC = hammerfellLocNames.Interfix[randomValues[1] % hammerfellLocNames.Interfix.Length];
                    return partA + partB + partC;
            }
            return "Error";
        }

        protected string SkyrimTownName()
        {
            string partA, partB, partC;
            partA = partB = partC = string.Empty;
            int[] randomValues = new int[2];

            for (int i = 0; i < 2; i++)
            {
                randomValues[i] = UnityEngine.Random.Range(0, int.MaxValue);
            }

            partA = skyrimLocNames.Prefix[randomValues[0] % skyrimLocNames.Prefix.Length];
            partB = skyrimLocNames.Suffix[randomValues[1] % skyrimLocNames.Suffix.Length];

            if (UnityEngine.Random.Range(0, 10) == 0)
            {
                do
                {
                    partC = skyrimLocNames.Extra[UnityEngine.Random.Range(0, int.MaxValue) % skyrimLocNames.Extra.Length];
                }
                while (partC.ToLower().Equals(partB));
            }

            return partA + partB + partC;
        }

        protected string ReachTownName()
        {
            string partA, partB, partC;
            partA = partB = partC = string.Empty;
            int[] randomValues = new int[3];

            for (int i = 0; i < 3; i++)
            {
                randomValues[i] = UnityEngine.Random.Range(0, int.MaxValue);
            }

            partA = reachLocNames.Prefix[randomValues[0] % reachLocNames.Prefix.Length];
            partB = reachLocNames.Suffix[randomValues[1] % reachLocNames.Suffix.Length];

            if (UnityEngine.Random.Range(0, 10) > 3)
            {
                partC = reachLocNames.Extra[randomValues[2] % reachLocNames.Extra.Length];
            }

            return partA + partC + partB;
        }

        protected string MorrowindTownName()
        {
            string partA, partB, partC;
            partA = partB = partC = string.Empty;
            int[] randomValues = new int[3];

            for (int i = 0; i < 3; i++)
            {
                randomValues[i] = UnityEngine.Random.Range(0, int.MaxValue);
            }

            partA = morrowindLocNames.Prefix[randomValues[0] % morrowindLocNames.Prefix.Length];
            partB = morrowindLocNames.Suffix[randomValues[1] % morrowindLocNames.Suffix.Length];

            if (UnityEngine.Random.Range(0, 10) > 7)
            {
                partC = morrowindLocNames.Extra[randomValues[2] % morrowindLocNames.Extra.Length];
            }

            return partA + partC + partB;
        }

        protected string OrsiniumTownName()
        {
            string partA, partB, partC;
            partA = partB = partC = string.Empty;
            int[] randomValues = new int[3];

            for (int i = 0; i < 3; i++)
            {
                randomValues[i] = UnityEngine.Random.Range(0, int.MaxValue);
            }

            partA = orsiniumLocNames.Prefix[randomValues[0] % orsiniumLocNames.Prefix.Length];
            partB = orsiniumLocNames.Interfix[randomValues[1] % orsiniumLocNames.Interfix.Length];
            partC = orsiniumLocNames.Suffix[randomValues[2] % orsiniumLocNames.Suffix.Length];
            return partA + partB + partC;
        }

        protected string GetSuffix(int randomNumb)
        {
            int pickRandomly = UnityEngine.Random.Range(0, 3);
            switch (pickRandomly)
            {
                case 0:
                    if (geoLocations.Coastal)
                        return highRockLocNames.SuffixCoastal[randomNumb % highRockLocNames.SuffixCoastal.Length];
                    if (geoLocations.River)
                        return highRockLocNames.SuffixRiver[randomNumb % highRockLocNames.SuffixRiver.Length];
                    if (geoLocations.Plains)
                        return highRockLocNames.SuffixPlains[randomNumb % highRockLocNames.SuffixPlains.Length];
                    if (geoLocations.Valley)
                        return highRockLocNames.SuffixValley[randomNumb % highRockLocNames.SuffixValley.Length];
                    if (geoLocations.Island)
                        return highRockLocNames.SuffixIsland[randomNumb % highRockLocNames.SuffixIsland.Length];
                    if (geoLocations.Forest && geoLocations.Mountain)
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0) return highRockLocNames.SuffixForest[randomNumb % highRockLocNames.SuffixForest.Length];
                        else return highRockLocNames.SuffixMountain[randomNumb % highRockLocNames.SuffixMountain.Length];
                    }
                    if (geoLocations.Forest)
                        return highRockLocNames.SuffixForest[randomNumb % highRockLocNames.SuffixForest.Length];
                    if (geoLocations.Mountain)
                        return highRockLocNames.SuffixMountain[randomNumb % highRockLocNames.SuffixMountain.Length];
                    if (geoLocations.Swamp)
                        return highRockLocNames.SuffixSwamp[randomNumb % highRockLocNames.SuffixSwamp.Length];
                    break;

                case 1:
                    if (geoLocations.City)
                        return highRockLocNames.SuffixCity[randomNumb % highRockLocNames.SuffixCity.Length];
                    if (geoLocations.Town)
                        return highRockLocNames.SuffixTown[randomNumb % highRockLocNames.SuffixTown.Length];
                    break;

                default:
                    break;
            }
            return highRockLocNames.Suffix[randomNumb % highRockLocNames.Suffix.Length];
        }

        protected string GetGeoSuffix(int randomNumb)
        {
            if (geoLocations.Cave)
                return highRockLocNames.GeoCave[randomNumb % highRockLocNames.GeoCave.Length];
            if (geoLocations.Lake)
                return highRockLocNames.GeoLake[randomNumb % highRockLocNames.GeoLake.Length];
            if (geoLocations.Coastal)
                return highRockLocNames.GeoCoastal[randomNumb % highRockLocNames.GeoCoastal.Length];
            if (geoLocations.River)
                return highRockLocNames.GeoRiver[randomNumb % highRockLocNames.GeoRiver.Length];
            if (geoLocations.Island)
                return highRockLocNames.GeoIsland[randomNumb % highRockLocNames.GeoIsland.Length];
            if (geoLocations.Desert)
                return highRockLocNames.GeoDesert[randomNumb % highRockLocNames.GeoDesert.Length];
            if (geoLocations.Forest && geoLocations.Mountain)
            {
                if (UnityEngine.Random.Range(0, 2) == 0) return highRockLocNames.GeoForest[randomNumb % highRockLocNames.GeoForest.Length];
                else return highRockLocNames.GeoMountain[randomNumb % highRockLocNames.GeoMountain.Length];
            }
            if (geoLocations.Forest)
                return highRockLocNames.GeoForest[randomNumb % highRockLocNames.GeoForest.Length];
            if (geoLocations.Mountain)
                return highRockLocNames.GeoMountain[randomNumb % highRockLocNames.GeoMountain.Length];
            if (geoLocations.Swamp)
                return highRockLocNames.GeoSwamp[randomNumb % highRockLocNames.GeoSwamp.Length];
            return highRockLocNames.Geo[randomNumb % highRockLocNames.Geo.Length];
        }

        protected string GetCivSuffix(int randomNumb)
        {
            if (geoLocations.City)
                return highRockLocNames.CivCity[randomNumb % highRockLocNames.CivCity.Length];
            else
                return highRockLocNames.CivTown[randomNumb % highRockLocNames.CivTown.Length];
        }
    }
}
// #endif