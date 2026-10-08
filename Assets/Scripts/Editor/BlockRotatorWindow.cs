using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallConnect.Utility;
using Newtonsoft.Json;
using MapEditor;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Entity;
using System.Xml.Linq;

namespace BlockRotator
{
    public class BlockRotatorWindow : EditorWindow
    {
        static BlockRotatorWindow blockRotatorWindow;
        const string windowTitle = "Block Rotator";
        const string menuPath = "Daggerfall Tools/Block Rotator";
        public const string testPath = "D:/da_shiny_oneBU/daggerfall/DaggerfallGameFiles/arena2/Maps";
        public const string secondPath = "D:/da_shiny_oneBU/daggerfall/DaggerfallGameFiles/arena2/Maps/Tamriel/Data";
        public const int regionForUnique = 62;  // To update as more regions get unique blocks
        public const int normalWallChance = 80; // To modify if I feel there are too many/too few special wall blocks
        public const int MAXXCOORD = 7680;
        public const int MAXYCOORD = 6144;
        public const int TILESIZE = 64;

        // Dungeon coefficients for the PickDungeonType func
        public const int rare = 7;
        public const int rarer = 4;
        public const int rarest = 1;
        public const int common = 13;
        public const int veryCommon = 16;
        public const int ordinary = 19;
        public const int normal = 10;

        public static int provinceIndex;
        public static int regionIndex;
        public static WorldSettings worldData;

        public bool blockIntegration = false;
        public static string[][] dungeonNamesList;
        public static string[][] dungeonTypeBlocks;

        public static string[] blockPrefixes = {
            "TVR", "GEN", "RES", "WEA", "ARM", "ALC", "BAN", "BOO",
            "CLO", "FUR", "GEM", "LIB", "PAW", "TEM", "PAL", "FAR",
            "DUN", "CAS", "CAR", "MAN", "SHR", "RUI", "SHC", "GRV",
            "FIL", "KRA", "KDR", "KOW", "KMO", "KCA", "KFL", "KHO",
            "KRO", "KWH", "KSC", "KHA", "MAG", "THI", "DAR", "FIG",
            "CUS", "WAL", "MAR", "SHI", "WIT", "BBS", "CVN", "CRY",
            "DST", "DGD", "GSH", "HRN", "HSH", "LAB", "MNE", "NCV",
            "OSH", "PRS", "RNC", "SCN", "SPN", "VPH", "VCV",
            "B", "N", "W", "S", "M"
        };

        public static string[] blockNotToRotate = {
            "CUS", "WAL", "SHI"
        };

        public static string[] rmbBlockClimate = {
            "ZA", "ZL", "ZM", "ZS", "ZN",     // Good for every region
            "AA", "AL", "AM", "AS", "AN",     // High Rock, Rainforest, Swamp, Desert2...
            "BA", "BL", "BM", "BS", "BN",     // Desert
            "GA", "GL", "GM", "GS", "GN"      // Subtropical, but to re-design, since it's a copy/paste of the "B" group
        };

        // For reference:
        // A: Arkay
        // B: Zenithar
        // C: Mara
        // D: Akatosh
        // E: Dibella
        // F: Julianos
        // G: Stendarr
        // H: Kynareth
        public static string[] deityCode = {
            "A", "B", "C", "D", "E", "F", "G", "H"
        };

        public static string[] deityNames = {
            "Arkay",
            "Zenithar",
            "Mara",
            "Akatosh",
            "Dibella",
            "Julianos",
            "Stendarr",
            "Kynareth"
        };

        // Progressive identifiers for unique blocks
        public static string[] uniqueCode = {
            "!", "#", "$", "%", "&", "'", "(", ")", "+", ",", "-", "=", "@", "[", "]", "^", "_", "'", "{"
        };

        public static int[] templeModels = {
            100, 101, 102, 257, 258, 261, 362, 363, 400, 402, 406, 433, 436
        };

        public static int[][] orcishCountries =
        {
            new int[] { 26 },
            new int[] { 16, 23, 33 },
            new int[] { 34, 50, 52, 57}
        };

        public static readonly string[] rotationNames = { "90", "180", "270" };

        public static readonly DFLocation.BuildingTypes[] merchantBuilding = {
            DFLocation.BuildingTypes.Alchemist,
            DFLocation.BuildingTypes.Armorer,
            DFLocation.BuildingTypes.Bank,
            DFLocation.BuildingTypes.Bookseller,
            DFLocation.BuildingTypes.ClothingStore,
            DFLocation.BuildingTypes.FurnitureStore,
            DFLocation.BuildingTypes.GemStore,
            DFLocation.BuildingTypes.GeneralStore,
            DFLocation.BuildingTypes.Library,
            DFLocation.BuildingTypes.PawnShop,
            DFLocation.BuildingTypes.Tavern,
            DFLocation.BuildingTypes.WeaponSmith
        };

        public static bool typeToggle;
        public static bool blockToggle;
        public static bool rotationToggle;
        public static bool handleUniques;

        public static int blockType = 0;
        public static int climateType = 0;
        public static int selectedBlock = 0;
        public static int selectedRotation = 0;
        public static string[][] availableBlocks;
        public static int textureArchive = 0;

        public static DFRegion.DungeonTypes[] rareDungeons = { DFRegion.DungeonTypes.HarpyNest, DFRegion.DungeonTypes.DragonsDen, DFRegion.DungeonTypes.VolcanicCaves };

        private class TextureFormat
        {
            public FileHeader fileHeader;
            public RecordHeader[] recordHeader;
            public Record[] record;
        }

        private struct FileHeader
        {
            public long Position;
            public Int16 RecordCount;
            public String Name;
            public int TextureWidth;
            public int TextureHeight;
        }

        /// <summary>
        /// Record header.
        /// </summary>
        private struct RecordHeader
        {
            public long Position;
            public Int16 Type1;
            public Int32 RecordPosition;
            public Int16 Type2;
            public Int32 Unknown1;
            public Int64 NullValue1;
        }

        /// <summary>
        /// Record data.
        /// </summary>
        private struct Record
        {
            public long Position;
            public Int16 OffsetX;
            public Int16 OffsetY;
            public Int16 Width;
            public Int16 Height;
            public Int16 Compression;
            public UInt32 RecordSize;
            public UInt32 DataOffset;
            public Boolean IsNormal;
            public UInt16 FrameCount;
            public Int16 Unknown1;
            public Int16 ScaleX;
            public Int16 ScaleY;
            public DFBitmap[] Frames;
        }

        public struct HighRockLocNames
        {
            public string[] ClassicPrefix;
            public string[] ClassicSuffix;
            public string[] ClassicExtra;
            public string[] ModernPrefix;
            public string[] ModernSuffix;
            public string[] ModernExtra;
            public string[] Prefix;
            public string[] Suffix;
            public string[] SuffixCity;
            public string[] SuffixCoastal;
            public string[] SuffixForest;
            public string[] SuffixIsland;
            public string[] SuffixMountain;
            public string[] SuffixPlains;
            public string[] SuffixRiver;
            public string[] SuffixSwamp;
            public string[] SuffixTown;
            public string[] SuffixValley;
            public string[] Noun;
            public string[] Adjective;
            public string[] Geo;
            public string[] GeoCave;
            public string[] GeoCoastal;
            public string[] GeoDesert;
            public string[] GeoForest;
            public string[] GeoIsland;
            public string[] GeoLake;
            public string[] GeoMountain;
            public string[] GeoRiver;
            public string[] GeoSwamp;
            public string[] CivCity;
            public string[] CivTown;
            public string[] Extra;
        }

        public struct HammerfellLocNames
        {
            public string[] ClassicPrefix;
            public string[] ClassicSuffix1;
            public string[] ClassicSuffix2;
            public string[] ClassicExtra;
            public string[] Prefix;
            public string[] Suffix;
            public string[] Interfix;
            public string[] Noun;
            public string[] Adjective;
            public string[] Name;
            public string[] Number;
            public string[] Verb;
            public string[] Geo;
            public string[] GeoCave;
            public string[] GeoCoastal;
            public string[] GeoDesert;
            public string[] GeoIsland;
            public string[] GeoLake;
            public string[] GeoMountain;
            public string[] GeoRiver;
            public string[] CivCity;
            public string[] CivTown;
            public string[] Extra;
        }

        public struct SkyrimLocNames
        {
            public string[] Prefix;
            public string[] Suffix;
            public string[] Extra;
        }

        public struct ReachLocNames
        {
            public string[] Prefix;
            public string[] Suffix;
            public string[] Extra;
        }

        public struct MorrowindLocNames
        {
            public string[] Prefix;
            public string[] Suffix;
            public string[] Extra;
        }

        public struct OrsiniumLocNames
        {
            public string[] Prefix;
            public string[] Interfix;
            public string[] Suffix;
        }

        public struct DwemeriLocNames
        {
            public string[] Prefix;
            public string[] Interfix;
            public string[] Suffix;
            public string[] Noun;
        }

        public struct GeoLocations
        {
            public bool City;
            public bool Town;
            public bool Coastal;
            public bool Forest;
            public bool Island;
            public bool Mountain;
            public bool Plains;
            public bool River;
            public bool Swamp;
            public bool Valley;
            public bool Cave;
            public bool Desert;
            public bool Lake;
        }

        [MenuItem(menuPath)]
        static void Init()
        {
            blockRotatorWindow = (BlockRotatorWindow)EditorWindow.GetWindow(typeof(BlockRotatorWindow));
            blockRotatorWindow.titleContent = new GUIContent(windowTitle);
        }

        void Awake()
        {
            typeToggle = false;
            blockToggle = false;
            rotationToggle = false;
            handleUniques = false;

            availableBlocks = new string[blockPrefixes.Length][];

            worldData = JsonConvert.DeserializeObject<WorldSettings>(File.ReadAllText(Path.Combine(testPath, "WorldData.json")));

            for (int i = 0; i < blockPrefixes.Length; i++)
            {
                List<string> blockList = new List<string>();

                if (blockPrefixes[i].Equals("N") || blockPrefixes[i].Equals("W") || blockPrefixes[i].Equals("B") || blockPrefixes[i].Equals("S") || blockPrefixes[i].Equals("M"))
                {
                    int counter = 0;
                    string blockName = ElaborateBlockName(blockPrefixes[i], string.Empty, string.Empty, "Z", counter);
                    while (File.Exists(Path.Combine(testPath, "RDB", blockName + ".RDB.json")))
                    {
                        blockList.Add(blockName);
                        counter++;
                        blockName = ElaborateBlockName(blockPrefixes[i], string.Empty, string.Empty, "Z", counter);
                    }
                }
                else
                {
                    bool genericTemDone = false;
                    for (int j = 0; j < rmbBlockClimate.Length; j++)
                    {
                        int counter;
                        if (blockPrefixes[i].Equals("TEM") && !genericTemDone)
                        {
                            counter = 0;
                            string blockName = ElaborateBlockName(blockPrefixes[i], "0", rmbBlockClimate[j], "Z", counter);
                            while (CheckBlockExistance(blockName))
                            {
                                blockList.Add(blockName);
                                counter++;
                                blockName = ElaborateBlockName(blockPrefixes[i], "0", rmbBlockClimate[j], "Z", counter);
                            }
                        }
                    }
                    genericTemDone = true;

                    for (int l = 0; l < rmbBlockClimate.Length; l++)
                    {
                        int counter = 0;
                        if (blockPrefixes[i].Equals("TEM") && genericTemDone)
                        {
                            for (int k = 0; k < deityCode.Length; k++)
                            {
                                counter = 0;
                                string blockName = ElaborateBlockName(blockPrefixes[i], "0", rmbBlockClimate[l], deityCode[k], counter);
                                while (CheckBlockExistance(blockName))
                                {
                                    blockList.Add(blockName);
                                    counter++;
                                    blockName = ElaborateBlockName(blockPrefixes[i], "0", rmbBlockClimate[l], deityCode[k], counter);
                                }
                            }
                        }
                        else
                        {
                            string blockName = ElaborateBlockName(blockPrefixes[i], "0", rmbBlockClimate[l], "Z", counter);
                            while (CheckBlockExistance(blockName))
                            {
                                blockList.Add(blockName);
                                counter++;
                                blockName = ElaborateBlockName(blockPrefixes[i], "0", rmbBlockClimate[l], "Z", counter);
                            }
                        }
                    }
                }
                availableBlocks[i] = blockList.ToArray();
            }
        }

        void OnGUI()
        {
            typeToggle = EditorGUILayout.BeginToggleGroup("Choose Block Type", typeToggle);

            blockType = EditorGUILayout.Popup("Block Type: ", blockType, blockPrefixes, GUILayout.MaxWidth(600.0f));
            blockToggle = EditorGUILayout.BeginToggleGroup("Choose Specific Block", blockToggle);
            selectedBlock = EditorGUILayout.Popup("Selected: ", selectedBlock, availableBlocks[blockType], GUILayout.MaxWidth(600.0f));
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.EndToggleGroup();

            rotationToggle = EditorGUILayout.BeginToggleGroup("Choose Rotation", rotationToggle);
            selectedRotation = EditorGUILayout.Popup("Rotation: ", selectedRotation, rotationNames, GUILayout.MaxWidth(600.0f));
            EditorGUILayout.EndToggleGroup();

            handleUniques = EditorGUILayout.Toggle("Handle Unique Blocks", handleUniques);

            if (GUILayout.Button("ROTATE", GUILayout.MaxWidth(200.0f)))
            {
                ActivateRotation();
            }

            if (GUILayout.Button("ConvertToClassic", GUILayout.MaxWidth(200.0f)))
            {
                ConvertToClassic(blockType, selectedBlock);
            }

            if (GUILayout.Button("ConvertToPN", GUILayout.MaxWidth(200.0f)))
            {
                ConvertToPN();
            }

            if (GUILayout.Button("Create BLOCKS.BSA", GUILayout.MaxWidth(200.0f)))
            {
                CreateBlocksBsa();
            }

            if (GUILayout.Button("Convert to new Block Format", GUILayout.MaxWidth(200.0f)))
            {
                ConvertToNewBlockFormat();
            }

            if (GUILayout.Button("Calculate Block Limits", GUILayout.MaxWidth(200.0f)))
            {
                CalculateBlockLimits();
            }

            blockIntegration = EditorGUILayout.Foldout(blockIntegration, "Block Integration");
            if (blockIntegration)
            {
                int[][] oldBlockLimits = JsonConvert.DeserializeObject<int[][]>(File.ReadAllText(Path.Combine(testPath, "oldBlockLimits.json")));
                int[][] newBlockLimits = JsonConvert.DeserializeObject<int[][]>(File.ReadAllText(Path.Combine(testPath, "newBlockLimits.json")));

                EditorGUILayout.BeginHorizontal();
                int colCounter = 0;
                for (int i = 0; i < blockPrefixes.Length - 5; i++)
                {
                    for (int j = 0; j < rmbBlockClimate.Length; j++)
                    {
                        if (oldBlockLimits[i][j] < newBlockLimits[i][j])
                        {
                            string blockName = blockPrefixes[i] + "*" + rmbBlockClimate[j] + "##";
                            EditorGUILayout.LabelField(blockName + ": ", "+" + (newBlockLimits[i][j] - oldBlockLimits[i][j]).ToString());
                            colCounter++;
                        }
                        if (colCounter % 5 == 0)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Integrate new blocks", GUILayout.MaxWidth(200.0f)))
                {
                    IntegrateNewBlocks();
                }
            }

            if (GUILayout.Button("Calculate Population Proportions", GUILayout.MaxWidth(200.0f)))
            {
                CalculatePopulationProportion();
            }

            if (GUILayout.Button("ListSimpleBorders", GUILayout.MaxWidth(200.0f)))
            {
                ListSimpleBorders();
            }

            if (GUILayout.Button("Analyse Dungeon Collocation", GUILayout.MaxWidth(200.0f)))
            {
                AnalyseDungeonCollocation();
            }

            if (GUILayout.Button("Correct Region Indexes", GUILayout.MaxWidth(200.0f)))
            {
                CorrectRegionIndexes();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Texture File", GUILayout.MaxWidth(200.0f)))
            {
                CreateTextureFiles(textureArchive);
            }
            textureArchive = EditorGUILayout.IntField("Archive: ", textureArchive);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create LocName Blank File", GUILayout.MaxWidth(200.0f)))
            {
                DwemeriLocNames dwemerLocNames = new DwemeriLocNames();
                string path = Path.Combine(testPath, "LocationNames", "Dwemer.json");
                var jsonBlock = JsonConvert.SerializeObject(dwemerLocNames, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                File.WriteAllText(path, jsonBlock);
            }

            if (GUILayout.Button("Calculate stuff regarding maps", GUILayout.MaxWidth(200.0f)))
            {
                CalculateStuffRegardingMaps();
            }
        }

        public static string ElaborateBlockName(string prefix, string rotation, string climate, string deity, int progressive)
        {
            if (prefix.Equals("TEM") && !deity.Equals("Z"))
                return prefix + rotation + climate + deity + progressive.ToString();
            else if (prefix.Equals("N") || prefix.Equals("W") || prefix.Equals("B") || prefix.Equals("S") || prefix.Equals("M"))
                return prefix + progressive.ToString("0000000");
            else
                return prefix + rotation + climate + progressive.ToString("00");
        }

        public static void ActivateRotation()
        {
            if (!typeToggle)
            {
                for (int i = 0; i < availableBlocks.Length; i++)
                {
                    for (int j = 0; j < availableBlocks[i].Length; j++)
                    {
                        if (!rotationToggle)
                        {
                            for (int k = 0; k < 3; k++)
                            {
                                RotateBlock(availableBlocks[i][j], k);
                            }
                        }
                        else
                        {
                            RotateBlock(availableBlocks[i][j], selectedRotation);
                        }
                    }
                }
            }
            else
            {
                if (!blockToggle)
                {
                    for (int l = 0; l < availableBlocks[blockType].Length; l++)
                    {
                        if (!rotationToggle)
                        {
                            for (int m = 0; m < 3; m++)
                            {
                                RotateBlock(availableBlocks[blockType][l], m);
                            }
                        }
                        else
                        {
                            RotateBlock(availableBlocks[blockType][l], selectedRotation);
                        }
                    }
                }
                else
                {
                    if (!rotationToggle)
                    {
                        for (int n = 0; n < 3; n++)
                        {
                            RotateBlock(availableBlocks[blockType][selectedBlock], n);
                        }
                    }
                    else
                        RotateBlock(availableBlocks[blockType][selectedBlock], selectedRotation);
                }
            }
        }

        public static void ConvertToClassic(int type, int selected)
        {
            if (!typeToggle)
                return;

            DFBlock block = new DFBlock();
            DFBlockRotation convBlock = new DFBlockRotation();
            bool isRmb = false;
            string path = string.Empty;
            // string correctName;

            if (blockToggle)
            {
                path = Path.Combine(testPath, "RMB", availableBlocks[type][selected] + ".RMB.json");
                if (File.Exists(path))
                    isRmb = true;
                else
                    path = Path.Combine(testPath, "RDB", availableBlocks[type][selected] + ".RDB.json");

                // if (isRmb)
                //     correctName = availableBlocks[type][selected] + ".RMB";
                // else
                //     correctName = availableBlocks[type][selected] + ".RDB";

                block = JsonConvert.DeserializeObject<DFBlock>(File.ReadAllText(path));

                convBlock = ConvertBlock(block);

                if (isRmb)
                    path = Path.Combine(testPath, "RMB", "converted", convBlock.Name + ".json");
                else
                    path = Path.Combine(testPath, "RDB", "converted", convBlock.Name + ".json");
                var jsonBlock = JsonConvert.SerializeObject(convBlock, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                File.WriteAllText(path, jsonBlock);
            }
            else
            {
                for (int i = 0; i < availableBlocks[type].Length; i++)
                {
                    path = Path.Combine(testPath, "RMB", availableBlocks[type][i] + ".RMB.json");
                    if (File.Exists(path))
                        isRmb = true;
                    else
                        path = Path.Combine(testPath, "RDB", availableBlocks[type][selected] + ".RDB.json");
                    // Debug.Log("Deserializing block " + path);
                    block = JsonConvert.DeserializeObject<DFBlock>(File.ReadAllText(path));

                    convBlock = ConvertBlock(block);

                    if (isRmb)
                        path = Path.Combine(testPath, "RMB", "converted", convBlock.Name + ".json");
                    else
                        path = Path.Combine(testPath, "RDB", "converted", convBlock.Name + ".json");
                    var jsonBlock = JsonConvert.SerializeObject(convBlock, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                    File.WriteAllText(path, jsonBlock);
                }
            }
        }

        public static void ConvertToPN()
        {
            List<string> blockList = new List<string>();

            if (handleUniques)
            {
                for (int h = 0; h < blockPrefixes.Length; h++)
                {
                    for (int g = 0; g < uniqueCode.Length; g++)
                    {
                        for (int f = 0; f < regionForUnique; f++)
                        {
                            string blockName = blockPrefixes[h] + uniqueCode[g] + f.ToString("0000");
                            if (!CheckBlockExistance(blockName))
                                continue;
                            blockList.Add(blockName);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < blockPrefixes.Length; i++)
                {
                    if (blockPrefixes[i].Equals("N") || blockPrefixes[i].Equals("W") || blockPrefixes[i].Equals("B") || blockPrefixes[i].Equals("S") || blockPrefixes[i].Equals("M"))
                    {
                        for (int a = 0; a < availableBlocks[i].Length; a++)
                        {
                            string blockName = availableBlocks[i][a];
                            if (File.Exists(Path.Combine(testPath, "RDB", "converted", blockName + ".RDB.json")))
                                blockList.Add(blockName);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < rmbBlockClimate.Length; j++)
                        {
                            if (blockPrefixes[i].Equals("TEMP"))
                            {
                                for (int b = 0; b < availableBlocks[i].Length; b++)
                                {
                                    string blockName = availableBlocks[i][b];
                                    if (File.Exists(Path.Combine(testPath, "RMB", "converted", GetGenericBlockName(blockName, true))))
                                        blockList.Add(blockName);
                                }
                            }
                            else
                            {
                                for (int c = 0; c < availableBlocks[i].Length; c++)
                                {
                                    string blockName = availableBlocks[i][c];
                                    if (File.Exists(Path.Combine(testPath, "RMB", "converted", GetGenericBlockName(blockName, true))))
                                        blockList.Add(blockName);
                                }
                            }
                        }
                    }
                }
            }

            foreach (string blockName in blockList)
            {
                // Debug.Log("Converting " + blockName);
                DFBlockRotation block = new DFBlockRotation();
                DFBlock convBlock = new DFBlock();
                bool isRmb = false;
                string path = Path.Combine(testPath, "RMB", "converted", blockName + ".RMB.json");
                if (File.Exists(path))
                    isRmb = true;
                else
                    path = Path.Combine(testPath, "RDB", "converted", blockName + ".RDB.json");

                block = JsonConvert.DeserializeObject<DFBlockRotation>(File.ReadAllText(path));
                if (isRmb)
                    block.Name = blockName + ".RMB";

                convBlock = ConvertBlock(block);

                if (convBlock.Type == DFBlock.BlockTypes.Rmb)
                    path = Path.Combine(testPath, "RMB", convBlock.Name + ".json");
                else if (convBlock.Type == DFBlock.BlockTypes.Rdb)
                    path = Path.Combine(testPath, "RDB", convBlock.Name + ".json");
                else break;
                var jsonBlock = JsonConvert.SerializeObject(convBlock, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                File.WriteAllText(path, jsonBlock);
            }
        }

        public static void CreateBlocksBsa()
        {
            List<long> positionList = new List<long>();
            List<int> indexList = new List<int>();
            List<(string, int)> blocksBsa = new List<(string, int)>();
            string path;

            long positionCounter;
            int indexCounter = 0;
            bool blockModified;

            for (int i = 0; i < availableBlocks.Length; i++)
            {
                for (int j = 0; j < availableBlocks[i].Length; j++)
                {
                    blockModified = false;
                    bool positionModified = false;
                    // bool indexModified = false;

                    long positionCounterCom;
                    // int indexCounterCom = 0;

                    DFBlock block = new DFBlock();
                    // Debug.Log("Working on " + availableBlocks[i][j] + " (i: " + i + ", j: " + j + ")");

                    string correctName;
                    bool isRmb = true;

                    if (i > 62)
                    {
                        path = Path.Combine(testPath, "RDB", availableBlocks[i][j] + ".RDB.json");
                        block = JsonConvert.DeserializeObject<DFBlock>(File.ReadAllText(path));
                        correctName = availableBlocks[i][j] + ".RDB";
                        isRmb = false;
                    }
                    else
                    {
                        path = Path.Combine(testPath, "RMB", availableBlocks[i][j] + ".RMB.json");
                        block = JsonConvert.DeserializeObject<DFBlock>(File.ReadAllText(path));
                        correctName = availableBlocks[i][j] + ".RMB";
                    }

                    if (correctName != block.Name)
                    {
                        // Debug.Log("Converting " + block.Name + " to " + correctName);
                        block.Name = correctName;
                        blockModified = true;
                    }
                    if (correctName != block.RmbBlock.FldHeader.Name)
                    {
                        // Debug.Log("Converting " + block.RmbBlock.FldHeader.Name + " to " + correctName);
                        block.RmbBlock.FldHeader.Name = correctName;
                        blockModified = true;
                    }

                    positionCounterCom = block.Position;
                    while (positionList.Contains(positionCounterCom))
                    {
                        positionCounterCom++;
                        positionModified = true;
                    }
                    if (positionModified)
                    {
                        block.Position = positionCounterCom;
                        blockModified = true;
                    }

                    // indexCounterCom = block.Index;
                    while (indexList.Contains(indexCounter))
                    {
                        indexCounter++;
                    }

                    // indexModified = true;
                    if (block.Index != indexCounter)
                    {
                        block.Index = indexCounter;
                        blockModified = true;
                    }

                    positionList.Add(block.Position);
                    indexList.Add(block.Index);
                    blocksBsa.Add((block.Name, block.Index));

                    // DFBlock convBlock = ConvertBlock(block);

                    if (blockModified)
                    {
                        if (isRmb)
                            path = Path.Combine(testPath, "RMB", availableBlocks[i][j] + ".RMB.json");
                        else
                            path = Path.Combine(testPath, "RDB", availableBlocks[i][j] + ".RDB.json");
                        var jsonBlock = JsonConvert.SerializeObject(block, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                        File.WriteAllText(path, jsonBlock);
                    }
                    // positionCounter = positionCounterCom;
                    // indexCounter = indexCounterCom;
                }

                for (int h = 0; h < regionForUnique; h++)
                {
                    int g = 0;
                    path = Path.Combine(testPath, "RMB", blockPrefixes[i] + uniqueCode[g] + h.ToString("0000") + ".RMB.json");
                    while (File.Exists(path))
                    {
                        bool positionModified = false;
                        blockModified = false;
                        DFBlock block = JsonConvert.DeserializeObject<DFBlock>(File.ReadAllText(path));
                        string correctName = blockPrefixes[i] + uniqueCode[g] + h.ToString("0000") + ".RMB";

                        if (correctName != block.Name)
                        {
                            // Debug.Log("Converting " + block.Name + " to " + correctName);
                            block.Name = correctName;
                            blockModified = true;
                        }
                        if (correctName != block.RmbBlock.FldHeader.Name)
                        {
                            // Debug.Log("Converting " + block.RmbBlock.FldHeader.Name + " to " + correctName);
                            block.RmbBlock.FldHeader.Name = correctName;
                            blockModified = true;
                        }
                        positionCounter = block.Position;
                        while (positionList.Contains(positionCounter))
                        {
                            positionCounter++;
                            positionModified = true;
                        }
                        if (positionModified)
                        {
                            block.Position = positionCounter;
                            blockModified = true;
                        }

                        while (indexList.Contains(indexCounter))
                        {
                            indexCounter++;
                        }

                        // indexModified = true;
                        if (block.Index != indexCounter)
                        {
                            block.Index = indexCounter;
                            blockModified = true;
                        }

                        positionList.Add(block.Position);
                        indexList.Add(block.Index);
                        blocksBsa.Add((block.Name, block.Index));
                        if (blockModified)
                        {
                            // path = Path.Combine(testPath, "RMB", availableBlocks[i][j] + ".RMB.json");
                            var jsonBlock = JsonConvert.SerializeObject(block, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                            File.WriteAllText(path, jsonBlock);
                        }

                        if ((g + 1) < uniqueCode.Length)
                        {
                            g++;
                            path = Path.Combine(testPath, "RMB", blockPrefixes[i] + uniqueCode[g] + h.ToString("0000") + ".RMB.json");
                        }
                        else break;
                    }
                }
            }

            string resultingPath = Path.Combine(testPath, "BLOCKS.BSA");
            var json = JsonConvert.SerializeObject(blocksBsa, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(resultingPath, json);
        }

        public static DFBlock ConvertBlock(DFBlockRotation block)
        {
            DFBlock convBlock = new DFBlock();

            convBlock.Position = block.Position;
            convBlock.Index = block.Index;
            if (int.TryParse(block.Name.Substring(3, 1), out int rotIndex))
            {
                convBlock.Name = block.Name;
                convBlock.RmbBlock.FldHeader.Name = block.Name;
            }
            else if (uniqueCode.Contains(block.Name.Substring(3, 1)))
            {
                convBlock.Name = block.Name;
                convBlock.RmbBlock.FldHeader.Name = block.Name;
            }
            else
            {
                convBlock.Name = block.Name.Substring(0, 3) + "0" + block.Name.Substring(4, 8);
                convBlock.RmbBlock.FldHeader.Name = convBlock.Name;
            }
            convBlock.Type = (DFBlock.BlockTypes)block.Type;

            convBlock.RmbBlock = GetRmbBlockDesc(convBlock.Type, block.RmbBlock);
            convBlock.RdbBlock = GetRdbBlockDesc(convBlock.Type, block.RdbBlock);

            convBlock.RdiBlock = new DFBlock.RdiBlockDesc();

            return convBlock;
        }

        public static DFBlockRotation ConvertBlock(DFBlock block)
        {
            DFBlockRotation convBlock = new DFBlockRotation();

            convBlock.Position = block.Position;
            convBlock.Index = block.Index;
            if (int.TryParse(block.Name.Substring(3, 1), out int rotIndex))
            {
                convBlock.Name = block.Name;
                convBlock.RmbBlock.FldHeader.Name = block.Name;
            }
            else
            {
                convBlock.Name = block.Name.Substring(0, 3) + "0" + block.Name.Substring(4, 8);
                convBlock.RmbBlock.FldHeader.Name = convBlock.Name;
            }
            convBlock.Type = (DFBlockRotation.BlockTypes)block.Type;

            convBlock.RmbBlock = GetRmbBlockDesc(convBlock.Type, block.RmbBlock);
            convBlock.RdbBlock = GetRdbBlockDesc(convBlock.Type, block.RdbBlock);

            convBlock.RdiBlock = new DFBlockRotation.RdiBlockDesc();

            return convBlock;
        }

        public static DFBlock.RmbBlockDesc GetRmbBlockDesc(DFBlock.BlockTypes type, DFBlockRotation.RmbBlockDesc block)
        {
            DFBlock.RmbBlockDesc convBlock = new DFBlock.RmbBlockDesc();

            switch (type)
            {
                case DFBlock.BlockTypes.Rmb:
                    convBlock = GetRmbBDForRmb(block);
                    break;
                case DFBlock.BlockTypes.Rdb:
                    convBlock = new DFBlock.RmbBlockDesc();
                    // convBlock = GetRmbBDForRdb(block);
                    break;
                default:
                    Debug.Log("GetRmbBlockDesc = Unknown block type");
                    break;
            }

            return convBlock;
        }

        public static DFBlockRotation.RmbBlockDesc GetRmbBlockDesc(DFBlockRotation.BlockTypes type, DFBlock.RmbBlockDesc block)
        {
            DFBlockRotation.RmbBlockDesc convBlock = new DFBlockRotation.RmbBlockDesc();

            switch (type)
            {
                case DFBlockRotation.BlockTypes.Rmb:
                    convBlock = GetRmbBDForRmb(block);
                    break;
                case DFBlockRotation.BlockTypes.Rdb:
                    convBlock = new DFBlockRotation.RmbBlockDesc();
                    // convBlock = GetRmbBDForRdb(block);
                    break;
                default:
                    // Debug.Log("GetRmbBlockDesc = Unknown block type");
                    break;
            }

            return convBlock;
        }

        public static DFBlock.RdbBlockDesc GetRdbBlockDesc(DFBlock.BlockTypes type, DFBlockRotation.RdbBlockDesc block)
        {
            DFBlock.RdbBlockDesc convBlock = new DFBlock.RdbBlockDesc();

            switch (type)
            {
                case DFBlock.BlockTypes.Rmb:
                    convBlock = new DFBlock.RdbBlockDesc();
                    // convBlock = GetRdbBDForRmb(block);
                    break;
                case DFBlock.BlockTypes.Rdb:
                    convBlock = GetRdbBDForRdb(block);
                    break;
                default:
                    // Debug.Log("GetRdbBlockDesc = Unknown block type");
                    break;
            }

            return convBlock;
        }

        public static DFBlockRotation.RdbBlockDesc GetRdbBlockDesc(DFBlockRotation.BlockTypes type, DFBlock.RdbBlockDesc block)
        {
            DFBlockRotation.RdbBlockDesc convBlock = new DFBlockRotation.RdbBlockDesc();

            switch (type)
            {
                case DFBlockRotation.BlockTypes.Rmb:
                    convBlock = new DFBlockRotation.RdbBlockDesc();
                    // convBlock = GetRdbBDForRmb(block);
                    break;
                case DFBlockRotation.BlockTypes.Rdb:
                    convBlock = GetRdbBDForRdb(block);
                    break;
                default:
                    // Debug.Log("GetRdbBlockDesc = Unknown block type");
                    break;
            }

            return convBlock;
        }

        public static float GetScale(DFBlock.RmbBlock3dObjectRecord objectRecord)
        {
            float scale = 1.0f;

            if (objectRecord.ZScale != 0.0f && objectRecord.ZScale != 1.0f)
                scale = objectRecord.ZScale;
            if (objectRecord.YScale != 0.0f && objectRecord.YScale != 1.0f)
                scale = objectRecord.YScale;
            if (objectRecord.XScale != 0.0f && objectRecord.XScale != 1.0f)
                scale = objectRecord.XScale;
            return scale;
        }

        public static float GetScale(DFBlockRotation.RmbBlock3dObjectRecord objectRecord)
        {
            float scale = 1.0f;

            if (objectRecord.ZScale != 0.0f && objectRecord.ZScale != 1.0f)
                scale = objectRecord.ZScale;
            if (objectRecord.YScale != 0.0f && objectRecord.YScale != 1.0f)
                scale = objectRecord.YScale;
            if (objectRecord.XScale != 0.0f && objectRecord.XScale != 1.0f)
                scale = objectRecord.XScale;
            return scale;
        }

        public static DFBlock.RmbBlockDesc GetRmbBDForRmb(DFBlockRotation.RmbBlockDesc block)
        {
            DFBlock.RmbBlockDesc convBlock = new DFBlock.RmbBlockDesc();

            convBlock.FldHeader.NumBlockDataRecords = block.FldHeader.NumBlockDataRecords;
            convBlock.FldHeader.NumMisc3dObjectRecords = block.FldHeader.NumMisc3dObjectRecords;
            convBlock.FldHeader.NumMiscFlatObjectRecords = block.FldHeader.NumMiscFlatObjectRecords;

            convBlock.FldHeader.BlockPositions = new DFBlock.RmbFldBlockPositions[block.FldHeader.BlockPositions.Length];
            for (int i = 0; i < block.FldHeader.BlockPositions.Length; i++)
            {
                convBlock.FldHeader.BlockPositions[i].XPos = block.FldHeader.BlockPositions[i].XPos;
                convBlock.FldHeader.BlockPositions[i].ZPos = block.FldHeader.BlockPositions[i].ZPos;
                convBlock.FldHeader.BlockPositions[i].YRotation = block.FldHeader.BlockPositions[i].YRotation;
            }

            convBlock.FldHeader.BuildingDataList = new DFLocation.BuildingData[block.FldHeader.BuildingDataList.Length];
            for (int j = 0; j < block.FldHeader.BuildingDataList.Length; j++)
            {
                convBlock.FldHeader.BuildingDataList[j].NameSeed = block.FldHeader.BuildingDataList[j].NameSeed;
                convBlock.FldHeader.BuildingDataList[j].FactionId = block.FldHeader.BuildingDataList[j].FactionId;
                convBlock.FldHeader.BuildingDataList[j].Sector = block.FldHeader.BuildingDataList[j].Sector;
                convBlock.FldHeader.BuildingDataList[j].LocationId = block.FldHeader.BuildingDataList[j].LocationId;
                convBlock.FldHeader.BuildingDataList[j].BuildingType = block.FldHeader.BuildingDataList[j].BuildingType;
                convBlock.FldHeader.BuildingDataList[j].Quality = block.FldHeader.BuildingDataList[j].Quality;
            }

            convBlock.FldHeader.BlockDataSizes = block.FldHeader.BlockDataSizes;

            if (block.FldHeader.GroundData.Header == null)
                convBlock.FldHeader.GroundData.Header = new int[] { 86, 9, 60, 130, 94, 42, 60, 130 };
            else convBlock.FldHeader.GroundData.Header = block.FldHeader.GroundData.Header;
            convBlock.FldHeader.GroundData.GroundTiles = null;
            convBlock.FldHeader.GroundData.GroundTiles1d = new DFBlock.RmbGroundTiles[16 * 16];
            for (int a = 0; a < block.FldHeader.GroundData.GroundTiles.Length; a++)
            {
                convBlock.FldHeader.GroundData.GroundTiles1d[a].TileBitfield = block.FldHeader.GroundData.GroundTiles[a].TileBitfield;
                convBlock.FldHeader.GroundData.GroundTiles1d[a].TextureRecord = block.FldHeader.GroundData.GroundTiles[a].TextureRecord;
                convBlock.FldHeader.GroundData.GroundTiles1d[a].IsRotated = block.FldHeader.GroundData.GroundTiles[a].IsRotated;
                convBlock.FldHeader.GroundData.GroundTiles1d[a].IsFlipped = block.FldHeader.GroundData.GroundTiles[a].IsFlipped;
            }

            convBlock.FldHeader.GroundData.GroundScenery = null;
            convBlock.FldHeader.GroundData.GroundScenery1d = new DFBlock.RmbGroundScenery[16 * 16];
            for (int b = 0; b < block.FldHeader.GroundData.GroundScenery.Length; b++)
            {
                convBlock.FldHeader.GroundData.GroundScenery1d[b].TextureRecord = block.FldHeader.GroundData.GroundScenery[b].TextureRecord;
            }

            convBlock.FldHeader.AutoMapData = block.FldHeader.AutoMapData;
            convBlock.FldHeader.Name = block.FldHeader.Name;
            convBlock.FldHeader.OtherNames = block.FldHeader.OtherNames;

            convBlock.SubRecords = new DFBlock.RmbSubRecord[block.SubRecords.Length];
            for (int k = 0; k < block.SubRecords.Length; k++)
            {
                convBlock.SubRecords[k].XPos = block.SubRecords[k].XPos;
                convBlock.SubRecords[k].ZPos = block.SubRecords[k].ZPos;
                convBlock.SubRecords[k].YRotation = block.SubRecords[k].YRotation;
                convBlock.SubRecords[k].Exterior.Header.Position = block.SubRecords[k].Exterior.Header.Position;
                convBlock.SubRecords[k].Exterior.Header.Num3dObjectRecords = block.SubRecords[k].Exterior.Header.Num3dObjectRecords;
                convBlock.SubRecords[k].Exterior.Header.NumFlatObjectRecords = block.SubRecords[k].Exterior.Header.NumFlatObjectRecords;
                convBlock.SubRecords[k].Exterior.Header.NumSection3Records = block.SubRecords[k].Exterior.Header.NumSection3Records;
                convBlock.SubRecords[k].Exterior.Header.NumPeopleRecords = block.SubRecords[k].Exterior.Header.NumPeopleRecords;
                convBlock.SubRecords[k].Exterior.Header.NumDoorRecords = block.SubRecords[k].Exterior.Header.NumDoorRecords;

                convBlock.SubRecords[k].Exterior.Block3dObjectRecords = new DFBlock.RmbBlock3dObjectRecord[block.SubRecords[k].Exterior.Block3dObjectRecords.Length];
                for (int l = 0; l < block.SubRecords[k].Exterior.Block3dObjectRecords.Length; l++)
                {
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ModelId = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ModelId;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ModelIdNum = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ModelIdNum;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ObjectType = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ObjectType;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].XPos = block.SubRecords[k].Exterior.Block3dObjectRecords[l].XPos;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].YPos = block.SubRecords[k].Exterior.Block3dObjectRecords[l].YPos;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ZPos = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ZPos;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].XScale = block.SubRecords[k].Exterior.Block3dObjectRecords[l].XScale;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].YScale = block.SubRecords[k].Exterior.Block3dObjectRecords[l].YScale;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ZScale = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ZScale;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].XRotation = block.SubRecords[k].Exterior.Block3dObjectRecords[l].XRotation;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].YRotation = block.SubRecords[k].Exterior.Block3dObjectRecords[l].YRotation;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ZRotation = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ZRotation;
                }

                convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords = new DFBlock.RmbBlockFlatObjectRecord[block.SubRecords[k].Exterior.BlockFlatObjectRecords.Length];
                for (int m = 0; m < block.SubRecords[k].Exterior.BlockFlatObjectRecords.Length; m++)
                {
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].Position = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].Position;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].XPos = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].XPos;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].YPos = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].YPos;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].ZPos = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].ZPos;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].TextureArchive = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].TextureArchive;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].TextureRecord = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].TextureRecord;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].FactionID = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].FactionID;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].Flags = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].Flags;
                }

                convBlock.SubRecords[k].Exterior.BlockSection3Records = new DFBlock.RmbBlockSection3Record[block.SubRecords[k].Exterior.BlockSection3Records.Length];
                for (int n = 0; n < block.SubRecords[k].Exterior.BlockSection3Records.Length; n++)
                {
                    convBlock.SubRecords[k].Exterior.BlockSection3Records[n].XPos = block.SubRecords[k].Exterior.BlockSection3Records[n].XPos;
                    convBlock.SubRecords[k].Exterior.BlockSection3Records[n].YPos = block.SubRecords[k].Exterior.BlockSection3Records[n].YPos;
                    convBlock.SubRecords[k].Exterior.BlockSection3Records[n].ZPos = block.SubRecords[k].Exterior.BlockSection3Records[n].ZPos;
                }

                convBlock.SubRecords[k].Exterior.BlockPeopleRecords = new DFBlock.RmbBlockPeopleRecord[block.SubRecords[k].Exterior.BlockPeopleRecords.Length];
                for (int o = 0; o < block.SubRecords[k].Exterior.BlockPeopleRecords.Length; o++)
                {
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].Position = block.SubRecords[k].Exterior.BlockPeopleRecords[o].Position;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].XPos = block.SubRecords[k].Exterior.BlockPeopleRecords[o].XPos;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].YPos = block.SubRecords[k].Exterior.BlockPeopleRecords[o].YPos;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].ZPos = block.SubRecords[k].Exterior.BlockPeopleRecords[o].ZPos;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].TextureArchive = block.SubRecords[k].Exterior.BlockPeopleRecords[o].TextureArchive;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].TextureRecord = block.SubRecords[k].Exterior.BlockPeopleRecords[o].TextureRecord;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].FactionID = block.SubRecords[k].Exterior.BlockPeopleRecords[o].FactionID;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].Flags = block.SubRecords[k].Exterior.BlockPeopleRecords[o].Flags;
                }

                convBlock.SubRecords[k].Exterior.BlockDoorRecords = new DFBlock.RmbBlockDoorRecord[block.SubRecords[k].Exterior.BlockDoorRecords.Length];
                for (int p = 0; p < block.SubRecords[k].Exterior.BlockDoorRecords.Length; p++)
                {
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].Position = block.SubRecords[k].Exterior.BlockDoorRecords[p].Position;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].XPos = block.SubRecords[k].Exterior.BlockDoorRecords[p].XPos;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].YPos = block.SubRecords[k].Exterior.BlockDoorRecords[p].YPos;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].ZPos = block.SubRecords[k].Exterior.BlockDoorRecords[p].ZPos;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].YRotation = block.SubRecords[k].Exterior.BlockDoorRecords[p].YRotation;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].OpenRotation = block.SubRecords[k].Exterior.BlockDoorRecords[p].OpenRotation;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].DoorModelIndex = block.SubRecords[k].Exterior.BlockDoorRecords[p].DoorModelIndex;
                }

                convBlock.SubRecords[k].Interior.Header.Position = block.SubRecords[k].Interior.Header.Position;
                convBlock.SubRecords[k].Interior.Header.Num3dObjectRecords = block.SubRecords[k].Interior.Header.Num3dObjectRecords;
                convBlock.SubRecords[k].Interior.Header.NumFlatObjectRecords = block.SubRecords[k].Interior.Header.NumFlatObjectRecords;
                convBlock.SubRecords[k].Interior.Header.NumSection3Records = block.SubRecords[k].Interior.Header.NumSection3Records;
                convBlock.SubRecords[k].Interior.Header.NumPeopleRecords = block.SubRecords[k].Interior.Header.NumPeopleRecords;
                convBlock.SubRecords[k].Interior.Header.NumDoorRecords = block.SubRecords[k].Interior.Header.NumDoorRecords;

                convBlock.SubRecords[k].Interior.Block3dObjectRecords = new DFBlock.RmbBlock3dObjectRecord[block.SubRecords[k].Interior.Block3dObjectRecords.Length];
                for (int q = 0; q < block.SubRecords[k].Interior.Block3dObjectRecords.Length; q++)
                {
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ModelId = block.SubRecords[k].Interior.Block3dObjectRecords[q].ModelId;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ModelIdNum = block.SubRecords[k].Interior.Block3dObjectRecords[q].ModelIdNum;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ObjectType = block.SubRecords[k].Interior.Block3dObjectRecords[q].ObjectType;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].XPos = block.SubRecords[k].Interior.Block3dObjectRecords[q].XPos;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].YPos = block.SubRecords[k].Interior.Block3dObjectRecords[q].YPos;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ZPos = block.SubRecords[k].Interior.Block3dObjectRecords[q].ZPos;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].XScale = block.SubRecords[k].Interior.Block3dObjectRecords[q].XScale;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].YScale = block.SubRecords[k].Interior.Block3dObjectRecords[q].YScale;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ZScale = block.SubRecords[k].Interior.Block3dObjectRecords[q].ZScale;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].XRotation = block.SubRecords[k].Interior.Block3dObjectRecords[q].XRotation;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].YRotation = block.SubRecords[k].Interior.Block3dObjectRecords[q].YRotation;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ZRotation = block.SubRecords[k].Interior.Block3dObjectRecords[q].ZRotation;
                }

                convBlock.SubRecords[k].Interior.BlockFlatObjectRecords = new DFBlock.RmbBlockFlatObjectRecord[block.SubRecords[k].Interior.BlockFlatObjectRecords.Length];
                for (int r = 0; r < block.SubRecords[k].Interior.BlockFlatObjectRecords.Length; r++)
                {
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].Position = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].Position;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].XPos = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].XPos;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].YPos = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].YPos;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].ZPos = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].ZPos;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].TextureArchive = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].TextureArchive;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].TextureRecord = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].TextureRecord;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].FactionID = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].FactionID;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].Flags = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].Flags;
                }

                convBlock.SubRecords[k].Interior.BlockSection3Records = new DFBlock.RmbBlockSection3Record[block.SubRecords[k].Interior.BlockSection3Records.Length];
                for (int s = 0; s < block.SubRecords[k].Interior.BlockSection3Records.Length; s++)
                {
                    convBlock.SubRecords[k].Interior.BlockSection3Records[s].XPos = block.SubRecords[k].Interior.BlockSection3Records[s].XPos;
                    convBlock.SubRecords[k].Interior.BlockSection3Records[s].YPos = block.SubRecords[k].Interior.BlockSection3Records[s].YPos;
                    convBlock.SubRecords[k].Interior.BlockSection3Records[s].ZPos = block.SubRecords[k].Interior.BlockSection3Records[s].ZPos;
                }

                convBlock.SubRecords[k].Interior.BlockPeopleRecords = new DFBlock.RmbBlockPeopleRecord[block.SubRecords[k].Interior.BlockPeopleRecords.Length];
                for (int t = 0; t < block.SubRecords[k].Interior.BlockPeopleRecords.Length; t++)
                {
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].Position = block.SubRecords[k].Interior.BlockPeopleRecords[t].Position;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].XPos = block.SubRecords[k].Interior.BlockPeopleRecords[t].XPos;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].YPos = block.SubRecords[k].Interior.BlockPeopleRecords[t].YPos;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].ZPos = block.SubRecords[k].Interior.BlockPeopleRecords[t].ZPos;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].TextureArchive = block.SubRecords[k].Interior.BlockPeopleRecords[t].TextureArchive;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].TextureRecord = block.SubRecords[k].Interior.BlockPeopleRecords[t].TextureRecord;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].FactionID = block.SubRecords[k].Interior.BlockPeopleRecords[t].FactionID;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].Flags = block.SubRecords[k].Interior.BlockPeopleRecords[t].Flags;
                }

                convBlock.SubRecords[k].Interior.BlockDoorRecords = new DFBlock.RmbBlockDoorRecord[block.SubRecords[k].Interior.BlockDoorRecords.Length];
                for (int u = 0; u < block.SubRecords[k].Interior.BlockDoorRecords.Length; u++)
                {
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].Position = block.SubRecords[k].Interior.BlockDoorRecords[u].Position;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].XPos = block.SubRecords[k].Interior.BlockDoorRecords[u].XPos;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].YPos = block.SubRecords[k].Interior.BlockDoorRecords[u].YPos;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].ZPos = block.SubRecords[k].Interior.BlockDoorRecords[u].ZPos;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].YRotation = block.SubRecords[k].Interior.BlockDoorRecords[u].YRotation;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].OpenRotation = block.SubRecords[k].Interior.BlockDoorRecords[u].OpenRotation;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].DoorModelIndex = block.SubRecords[k].Interior.BlockDoorRecords[u].DoorModelIndex;
                }
            }

            convBlock.Misc3dObjectRecords = new DFBlock.RmbBlock3dObjectRecord[block.Misc3dObjectRecords.Length];
            for (int v = 0; v < block.Misc3dObjectRecords.Length; v++)
            {
                convBlock.Misc3dObjectRecords[v].ModelId = block.Misc3dObjectRecords[v].ModelId;
                convBlock.Misc3dObjectRecords[v].ModelIdNum = block.Misc3dObjectRecords[v].ModelIdNum;
                convBlock.Misc3dObjectRecords[v].ObjectType = block.Misc3dObjectRecords[v].ObjectType;
                convBlock.Misc3dObjectRecords[v].XPos = block.Misc3dObjectRecords[v].XPos;
                convBlock.Misc3dObjectRecords[v].YPos = block.Misc3dObjectRecords[v].YPos;
                convBlock.Misc3dObjectRecords[v].ZPos = block.Misc3dObjectRecords[v].ZPos;
                convBlock.Misc3dObjectRecords[v].XScale = block.Misc3dObjectRecords[v].XScale;
                convBlock.Misc3dObjectRecords[v].YScale = block.Misc3dObjectRecords[v].YScale;
                convBlock.Misc3dObjectRecords[v].ZScale = block.Misc3dObjectRecords[v].ZScale;
                convBlock.Misc3dObjectRecords[v].XRotation = block.Misc3dObjectRecords[v].XRotation;
                convBlock.Misc3dObjectRecords[v].YRotation = block.Misc3dObjectRecords[v].YRotation;
                convBlock.Misc3dObjectRecords[v].ZRotation = block.Misc3dObjectRecords[v].ZRotation;
            }

            convBlock.MiscFlatObjectRecords = new DFBlock.RmbBlockFlatObjectRecord[block.MiscFlatObjectRecords.Length];
            for (int w = 0; w < block.MiscFlatObjectRecords.Length; w++)
            {
                convBlock.MiscFlatObjectRecords[w].Position = block.MiscFlatObjectRecords[w].Position;
                convBlock.MiscFlatObjectRecords[w].XPos = block.MiscFlatObjectRecords[w].XPos;
                convBlock.MiscFlatObjectRecords[w].YPos = block.MiscFlatObjectRecords[w].YPos;
                convBlock.MiscFlatObjectRecords[w].ZPos = block.MiscFlatObjectRecords[w].ZPos;
                convBlock.MiscFlatObjectRecords[w].TextureArchive = block.MiscFlatObjectRecords[w].TextureArchive;
                convBlock.MiscFlatObjectRecords[w].TextureRecord = block.MiscFlatObjectRecords[w].TextureRecord;
                convBlock.MiscFlatObjectRecords[w].FactionID = block.MiscFlatObjectRecords[w].FactionID;
                convBlock.MiscFlatObjectRecords[w].Flags = block.MiscFlatObjectRecords[w].Flags;
            }

            return convBlock;
        }

        public static DFBlockRotation.RmbBlockDesc GetRmbBDForRmb(DFBlock.RmbBlockDesc block)
        {
            DFBlockRotation.RmbBlockDesc convBlock = new DFBlockRotation.RmbBlockDesc();

            convBlock.FldHeader.NumBlockDataRecords = block.FldHeader.NumBlockDataRecords;
            convBlock.FldHeader.NumMisc3dObjectRecords = block.FldHeader.NumMisc3dObjectRecords;
            convBlock.FldHeader.NumMiscFlatObjectRecords = block.FldHeader.NumMiscFlatObjectRecords;

            convBlock.FldHeader.BlockPositions = new DFBlockRotation.RmbFldBlockPositions[block.FldHeader.BlockPositions.Length];
            for (int i = 0; i < block.FldHeader.BlockPositions.Length; i++)
            {
                convBlock.FldHeader.BlockPositions[i].XPos = block.FldHeader.BlockPositions[i].XPos;
                convBlock.FldHeader.BlockPositions[i].ZPos = block.FldHeader.BlockPositions[i].ZPos;
                convBlock.FldHeader.BlockPositions[i].YRotation = block.FldHeader.BlockPositions[i].YRotation;
            }

            convBlock.FldHeader.BuildingDataList = new DFLocation.BuildingData[block.FldHeader.BuildingDataList.Length];
            for (int j = 0; j < block.FldHeader.BuildingDataList.Length; j++)
            {
                convBlock.FldHeader.BuildingDataList[j].NameSeed = block.FldHeader.BuildingDataList[j].NameSeed;
                if (merchantBuilding.Contains(block.FldHeader.BuildingDataList[j].BuildingType))
                {
                    convBlock.FldHeader.BuildingDataList[j].FactionId = 510;
                }
                else convBlock.FldHeader.BuildingDataList[j].FactionId = block.FldHeader.BuildingDataList[j].FactionId;
                convBlock.FldHeader.BuildingDataList[j].Sector = block.FldHeader.BuildingDataList[j].Sector;
                convBlock.FldHeader.BuildingDataList[j].LocationId = block.FldHeader.BuildingDataList[j].LocationId;
                convBlock.FldHeader.BuildingDataList[j].BuildingType = block.FldHeader.BuildingDataList[j].BuildingType;
                convBlock.FldHeader.BuildingDataList[j].Quality = block.FldHeader.BuildingDataList[j].Quality;
            }

            convBlock.FldHeader.BlockDataSizes = block.FldHeader.BlockDataSizes;

            if (block.FldHeader.GroundData.Header == null)
                convBlock.FldHeader.GroundData.Header = new int[] { 86, 9, 60, 130, 94, 42, 60, 130 };
            else convBlock.FldHeader.GroundData.Header = block.FldHeader.GroundData.Header;
            convBlock.FldHeader.GroundData.GroundTiles = new DFBlockRotation.RmbGroundTiles[16 * 16];
            for (int a = 0; a < block.FldHeader.GroundData.GroundTiles1d.Length; a++)
            {
                convBlock.FldHeader.GroundData.GroundTiles[a].TileBitfield = block.FldHeader.GroundData.GroundTiles1d[a].TileBitfield;
                convBlock.FldHeader.GroundData.GroundTiles[a].TextureRecord = block.FldHeader.GroundData.GroundTiles1d[a].TextureRecord;
                convBlock.FldHeader.GroundData.GroundTiles[a].IsRotated = block.FldHeader.GroundData.GroundTiles1d[a].IsRotated;
                convBlock.FldHeader.GroundData.GroundTiles[a].IsFlipped = block.FldHeader.GroundData.GroundTiles1d[a].IsFlipped;
            }

            convBlock.FldHeader.GroundData.GroundScenery = new DFBlockRotation.RmbGroundScenery[16 * 16];
            for (int b = 0; b < block.FldHeader.GroundData.GroundScenery1d.Length; b++)
            {
                convBlock.FldHeader.GroundData.GroundScenery[b].TextureRecord = block.FldHeader.GroundData.GroundScenery1d[b].TextureRecord;
            }

            convBlock.FldHeader.AutoMapData = block.FldHeader.AutoMapData;
            convBlock.FldHeader.Name = block.FldHeader.Name;
            convBlock.FldHeader.OtherNames = block.FldHeader.OtherNames;

            convBlock.SubRecords = new DFBlockRotation.RmbSubRecord[block.SubRecords.Length];
            for (int k = 0; k < block.SubRecords.Length; k++)
            {
                convBlock.SubRecords[k].XPos = block.SubRecords[k].XPos;
                convBlock.SubRecords[k].ZPos = block.SubRecords[k].ZPos;
                convBlock.SubRecords[k].YRotation = block.SubRecords[k].YRotation;
                convBlock.SubRecords[k].Exterior.Header.Position = block.SubRecords[k].Exterior.Header.Position;
                convBlock.SubRecords[k].Exterior.Header.Num3dObjectRecords = block.SubRecords[k].Exterior.Header.Num3dObjectRecords;
                convBlock.SubRecords[k].Exterior.Header.NumFlatObjectRecords = block.SubRecords[k].Exterior.Header.NumFlatObjectRecords;
                convBlock.SubRecords[k].Exterior.Header.NumSection3Records = block.SubRecords[k].Exterior.Header.NumSection3Records;
                convBlock.SubRecords[k].Exterior.Header.NumPeopleRecords = block.SubRecords[k].Exterior.Header.NumPeopleRecords;
                convBlock.SubRecords[k].Exterior.Header.NumDoorRecords = block.SubRecords[k].Exterior.Header.NumDoorRecords;

                convBlock.SubRecords[k].Exterior.Block3dObjectRecords = new DFBlockRotation.RmbBlock3dObjectRecord[block.SubRecords[k].Exterior.Block3dObjectRecords.Length];
                for (int l = 0; l < block.SubRecords[k].Exterior.Block3dObjectRecords.Length; l++)
                {
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ModelId = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ModelId;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ModelIdNum = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ModelIdNum;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ObjectType = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ObjectType;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].XPos = block.SubRecords[k].Exterior.Block3dObjectRecords[l].XPos;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].YPos = block.SubRecords[k].Exterior.Block3dObjectRecords[l].YPos;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ZPos = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ZPos;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].XScale = block.SubRecords[k].Exterior.Block3dObjectRecords[l].XScale;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].YScale = block.SubRecords[k].Exterior.Block3dObjectRecords[l].YScale;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ZScale = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ZScale;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].XRotation = block.SubRecords[k].Exterior.Block3dObjectRecords[l].XRotation;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].YRotation = block.SubRecords[k].Exterior.Block3dObjectRecords[l].YRotation;
                    convBlock.SubRecords[k].Exterior.Block3dObjectRecords[l].ZRotation = block.SubRecords[k].Exterior.Block3dObjectRecords[l].ZRotation;
                }

                convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords = new DFBlockRotation.RmbBlockFlatObjectRecord[block.SubRecords[k].Exterior.BlockFlatObjectRecords.Length];
                for (int m = 0; m < block.SubRecords[k].Exterior.BlockFlatObjectRecords.Length; m++)
                {
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].Position = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].Position;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].XPos = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].XPos;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].YPos = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].YPos;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].ZPos = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].ZPos;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].TextureArchive = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].TextureArchive;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].TextureRecord = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].TextureRecord;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].FactionID = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].FactionID;
                    convBlock.SubRecords[k].Exterior.BlockFlatObjectRecords[m].Flags = block.SubRecords[k].Exterior.BlockFlatObjectRecords[m].Flags;
                }

                convBlock.SubRecords[k].Exterior.BlockSection3Records = new DFBlockRotation.RmbBlockSection3Record[block.SubRecords[k].Exterior.BlockSection3Records.Length];
                for (int n = 0; n < block.SubRecords[k].Exterior.BlockSection3Records.Length; n++)
                {
                    convBlock.SubRecords[k].Exterior.BlockSection3Records[n].XPos = block.SubRecords[k].Exterior.BlockSection3Records[n].XPos;
                    convBlock.SubRecords[k].Exterior.BlockSection3Records[n].YPos = block.SubRecords[k].Exterior.BlockSection3Records[n].YPos;
                    convBlock.SubRecords[k].Exterior.BlockSection3Records[n].ZPos = block.SubRecords[k].Exterior.BlockSection3Records[n].ZPos;
                }

                convBlock.SubRecords[k].Exterior.BlockPeopleRecords = new DFBlockRotation.RmbBlockPeopleRecord[block.SubRecords[k].Exterior.BlockPeopleRecords.Length];
                for (int o = 0; o < block.SubRecords[k].Exterior.BlockPeopleRecords.Length; o++)
                {
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].Position = block.SubRecords[k].Exterior.BlockPeopleRecords[o].Position;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].XPos = block.SubRecords[k].Exterior.BlockPeopleRecords[o].XPos;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].YPos = block.SubRecords[k].Exterior.BlockPeopleRecords[o].YPos;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].ZPos = block.SubRecords[k].Exterior.BlockPeopleRecords[o].ZPos;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].TextureArchive = block.SubRecords[k].Exterior.BlockPeopleRecords[o].TextureArchive;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].TextureRecord = block.SubRecords[k].Exterior.BlockPeopleRecords[o].TextureRecord;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].FactionID = block.SubRecords[k].Exterior.BlockPeopleRecords[o].FactionID;
                    convBlock.SubRecords[k].Exterior.BlockPeopleRecords[o].Flags = block.SubRecords[k].Exterior.BlockPeopleRecords[o].Flags;
                }

                convBlock.SubRecords[k].Exterior.BlockDoorRecords = new DFBlockRotation.RmbBlockDoorRecord[block.SubRecords[k].Exterior.BlockDoorRecords.Length];
                for (int p = 0; p < block.SubRecords[k].Exterior.BlockDoorRecords.Length; p++)
                {
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].Position = block.SubRecords[k].Exterior.BlockDoorRecords[p].Position;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].XPos = block.SubRecords[k].Exterior.BlockDoorRecords[p].XPos;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].YPos = block.SubRecords[k].Exterior.BlockDoorRecords[p].YPos;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].ZPos = block.SubRecords[k].Exterior.BlockDoorRecords[p].ZPos;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].YRotation = block.SubRecords[k].Exterior.BlockDoorRecords[p].YRotation;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].OpenRotation = block.SubRecords[k].Exterior.BlockDoorRecords[p].OpenRotation;
                    convBlock.SubRecords[k].Exterior.BlockDoorRecords[p].DoorModelIndex = block.SubRecords[k].Exterior.BlockDoorRecords[p].DoorModelIndex;
                }

                convBlock.SubRecords[k].Interior.Header.Position = block.SubRecords[k].Interior.Header.Position;
                convBlock.SubRecords[k].Interior.Header.Num3dObjectRecords = block.SubRecords[k].Interior.Header.Num3dObjectRecords;
                convBlock.SubRecords[k].Interior.Header.NumFlatObjectRecords = block.SubRecords[k].Interior.Header.NumFlatObjectRecords;
                convBlock.SubRecords[k].Interior.Header.NumSection3Records = block.SubRecords[k].Interior.Header.NumSection3Records;
                convBlock.SubRecords[k].Interior.Header.NumPeopleRecords = block.SubRecords[k].Interior.Header.NumPeopleRecords;
                convBlock.SubRecords[k].Interior.Header.NumDoorRecords = block.SubRecords[k].Interior.Header.NumDoorRecords;

                convBlock.SubRecords[k].Interior.Block3dObjectRecords = new DFBlockRotation.RmbBlock3dObjectRecord[block.SubRecords[k].Interior.Block3dObjectRecords.Length];
                for (int q = 0; q < block.SubRecords[k].Interior.Block3dObjectRecords.Length; q++)
                {
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ModelId = block.SubRecords[k].Interior.Block3dObjectRecords[q].ModelId;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ModelIdNum = block.SubRecords[k].Interior.Block3dObjectRecords[q].ModelIdNum;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ObjectType = block.SubRecords[k].Interior.Block3dObjectRecords[q].ObjectType;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].XPos = block.SubRecords[k].Interior.Block3dObjectRecords[q].XPos;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].YPos = block.SubRecords[k].Interior.Block3dObjectRecords[q].YPos;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ZPos = block.SubRecords[k].Interior.Block3dObjectRecords[q].ZPos;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].XScale = block.SubRecords[k].Interior.Block3dObjectRecords[q].XScale;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].YScale = block.SubRecords[k].Interior.Block3dObjectRecords[q].YScale;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ZScale = block.SubRecords[k].Interior.Block3dObjectRecords[q].ZScale;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].XRotation = block.SubRecords[k].Interior.Block3dObjectRecords[q].XRotation;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].YRotation = block.SubRecords[k].Interior.Block3dObjectRecords[q].YRotation;
                    convBlock.SubRecords[k].Interior.Block3dObjectRecords[q].ZRotation = block.SubRecords[k].Interior.Block3dObjectRecords[q].ZRotation;
                }

                convBlock.SubRecords[k].Interior.BlockFlatObjectRecords = new DFBlockRotation.RmbBlockFlatObjectRecord[block.SubRecords[k].Interior.BlockFlatObjectRecords.Length];
                for (int r = 0; r < block.SubRecords[k].Interior.BlockFlatObjectRecords.Length; r++)
                {
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].Position = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].Position;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].XPos = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].XPos;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].YPos = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].YPos;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].ZPos = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].ZPos;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].TextureArchive = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].TextureArchive;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].TextureRecord = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].TextureRecord;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].FactionID = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].FactionID;
                    convBlock.SubRecords[k].Interior.BlockFlatObjectRecords[r].Flags = block.SubRecords[k].Interior.BlockFlatObjectRecords[r].Flags;
                }

                convBlock.SubRecords[k].Interior.BlockSection3Records = new DFBlockRotation.RmbBlockSection3Record[block.SubRecords[k].Interior.BlockSection3Records.Length];
                for (int s = 0; s < block.SubRecords[k].Interior.BlockSection3Records.Length; s++)
                {
                    convBlock.SubRecords[k].Interior.BlockSection3Records[s].XPos = block.SubRecords[k].Interior.BlockSection3Records[s].XPos;
                    convBlock.SubRecords[k].Interior.BlockSection3Records[s].YPos = block.SubRecords[k].Interior.BlockSection3Records[s].YPos;
                    convBlock.SubRecords[k].Interior.BlockSection3Records[s].ZPos = block.SubRecords[k].Interior.BlockSection3Records[s].ZPos;
                }

                convBlock.SubRecords[k].Interior.BlockPeopleRecords = new DFBlockRotation.RmbBlockPeopleRecord[block.SubRecords[k].Interior.BlockPeopleRecords.Length];
                for (int t = 0; t < block.SubRecords[k].Interior.BlockPeopleRecords.Length; t++)
                {
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].Position = block.SubRecords[k].Interior.BlockPeopleRecords[t].Position;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].XPos = block.SubRecords[k].Interior.BlockPeopleRecords[t].XPos;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].YPos = block.SubRecords[k].Interior.BlockPeopleRecords[t].YPos;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].ZPos = block.SubRecords[k].Interior.BlockPeopleRecords[t].ZPos;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].TextureArchive = block.SubRecords[k].Interior.BlockPeopleRecords[t].TextureArchive;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].TextureRecord = block.SubRecords[k].Interior.BlockPeopleRecords[t].TextureRecord;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].FactionID = block.SubRecords[k].Interior.BlockPeopleRecords[t].FactionID;
                    convBlock.SubRecords[k].Interior.BlockPeopleRecords[t].Flags = block.SubRecords[k].Interior.BlockPeopleRecords[t].Flags;
                }

                convBlock.SubRecords[k].Interior.BlockDoorRecords = new DFBlockRotation.RmbBlockDoorRecord[block.SubRecords[k].Interior.BlockDoorRecords.Length];
                for (int u = 0; u < block.SubRecords[k].Interior.BlockDoorRecords.Length; u++)
                {
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].Position = block.SubRecords[k].Interior.BlockDoorRecords[u].Position;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].XPos = block.SubRecords[k].Interior.BlockDoorRecords[u].XPos;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].YPos = block.SubRecords[k].Interior.BlockDoorRecords[u].YPos;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].ZPos = block.SubRecords[k].Interior.BlockDoorRecords[u].ZPos;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].YRotation = block.SubRecords[k].Interior.BlockDoorRecords[u].YRotation;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].OpenRotation = block.SubRecords[k].Interior.BlockDoorRecords[u].OpenRotation;
                    convBlock.SubRecords[k].Interior.BlockDoorRecords[u].DoorModelIndex = block.SubRecords[k].Interior.BlockDoorRecords[u].DoorModelIndex;
                }
            }

            convBlock.Misc3dObjectRecords = new DFBlockRotation.RmbBlock3dObjectRecord[block.Misc3dObjectRecords.Length];
            for (int v = 0; v < block.Misc3dObjectRecords.Length; v++)
            {
                convBlock.Misc3dObjectRecords[v].ModelId = block.Misc3dObjectRecords[v].ModelId;
                convBlock.Misc3dObjectRecords[v].ModelIdNum = block.Misc3dObjectRecords[v].ModelIdNum;
                convBlock.Misc3dObjectRecords[v].ObjectType = block.Misc3dObjectRecords[v].ObjectType;
                convBlock.Misc3dObjectRecords[v].XPos = block.Misc3dObjectRecords[v].XPos;
                convBlock.Misc3dObjectRecords[v].YPos = block.Misc3dObjectRecords[v].YPos;
                convBlock.Misc3dObjectRecords[v].ZPos = block.Misc3dObjectRecords[v].ZPos;
                convBlock.Misc3dObjectRecords[v].XScale = block.Misc3dObjectRecords[v].XScale;
                convBlock.Misc3dObjectRecords[v].YScale = block.Misc3dObjectRecords[v].YScale;
                convBlock.Misc3dObjectRecords[v].ZScale = block.Misc3dObjectRecords[v].ZScale;
                convBlock.Misc3dObjectRecords[v].XRotation = block.Misc3dObjectRecords[v].XRotation;
                convBlock.Misc3dObjectRecords[v].YRotation = block.Misc3dObjectRecords[v].YRotation;
                convBlock.Misc3dObjectRecords[v].ZRotation = block.Misc3dObjectRecords[v].ZRotation;
            }

            convBlock.MiscFlatObjectRecords = new DFBlockRotation.RmbBlockFlatObjectRecord[block.MiscFlatObjectRecords.Length];
            for (int w = 0; w < block.MiscFlatObjectRecords.Length; w++)
            {
                convBlock.MiscFlatObjectRecords[w].Position = block.MiscFlatObjectRecords[w].Position;
                convBlock.MiscFlatObjectRecords[w].XPos = block.MiscFlatObjectRecords[w].XPos;
                convBlock.MiscFlatObjectRecords[w].YPos = block.MiscFlatObjectRecords[w].YPos;
                convBlock.MiscFlatObjectRecords[w].ZPos = block.MiscFlatObjectRecords[w].ZPos;
                convBlock.MiscFlatObjectRecords[w].TextureArchive = block.MiscFlatObjectRecords[w].TextureArchive;
                convBlock.MiscFlatObjectRecords[w].TextureRecord = block.MiscFlatObjectRecords[w].TextureRecord;
                convBlock.MiscFlatObjectRecords[w].FactionID = block.MiscFlatObjectRecords[w].FactionID;
                convBlock.MiscFlatObjectRecords[w].Flags = block.MiscFlatObjectRecords[w].Flags;
            }

            return convBlock;
        }

        public static DFBlock.RdbBlockDesc GetRdbBDForRdb(DFBlockRotation.RdbBlockDesc block)
        {
            DFBlock.RdbBlockDesc convBlock = new DFBlock.RdbBlockDesc();

            convBlock.ModelReferenceList = new DFBlock.RdbModelReference[block.ModelReferenceList.Length];
            for (int i = 0; i < block.ModelReferenceList.Length; i++)
            {
                convBlock.ModelReferenceList[i].ModelId = block.ModelReferenceList[i].ModelId;
                convBlock.ModelReferenceList[i].ModelIdNum = block.ModelReferenceList[i].ModelIdNum;
                convBlock.ModelReferenceList[i].Description = block.ModelReferenceList[i].Description;
            }

            convBlock.ObjectRootList = new DFBlock.RdbObjectRoot[block.ObjectRootList.Length];
            for (int j = 0; j < block.ObjectRootList.Length; j++)
            {
                if (block.ObjectRootList[j].RdbObjects == null)
                    continue;

                convBlock.ObjectRootList[j].RdbObjects = new DFBlock.RdbObject[block.ObjectRootList[j].RdbObjects.Length];
                for (int k = 0; k < convBlock.ObjectRootList[j].RdbObjects.Length; k++)
                {
                    convBlock.ObjectRootList[j].RdbObjects[k].Position = block.ObjectRootList[j].RdbObjects[k].Position;
                    convBlock.ObjectRootList[j].RdbObjects[k].Index = block.ObjectRootList[j].RdbObjects[k].Index;
                    convBlock.ObjectRootList[j].RdbObjects[k].XPos = block.ObjectRootList[j].RdbObjects[k].XPos;
                    convBlock.ObjectRootList[j].RdbObjects[k].YPos = block.ObjectRootList[j].RdbObjects[k].YPos;
                    convBlock.ObjectRootList[j].RdbObjects[k].ZPos = block.ObjectRootList[j].RdbObjects[k].ZPos;
                    convBlock.ObjectRootList[j].RdbObjects[k].Type = (DFBlock.RdbResourceTypes)block.ObjectRootList[j].RdbObjects[k].Type;

                    switch (convBlock.ObjectRootList[j].RdbObjects[k].Type)
                    {
                        case DFBlock.RdbResourceTypes.Model:
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.XRotation = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.XRotation;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.YRotation = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.YRotation;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ZRotation = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ZRotation;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ModelIndex = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ModelIndex;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.TriggerFlag_StartingLock = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.TriggerFlag_StartingLock;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.SoundIndex = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.SoundIndex;

                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Position = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Position;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Axis = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Axis;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Duration = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Duration;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Magnitude = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Magnitude;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.NextObjectOffset = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.NextObjectOffset;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.PreviousObjectOffset = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.PreviousObjectOffset;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.NextObjectIndex = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.NextObjectIndex;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Flags = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Flags;
                            break;

                        case DFBlock.RdbResourceTypes.Light:
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Unknown1 = block.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Unknown1;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Unknown2 = block.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Unknown2;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Radius = block.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Radius;
                            break;

                        case DFBlock.RdbResourceTypes.Flat:
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Position = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Position;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.TextureArchive = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.TextureArchive;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.TextureRecord = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.TextureRecord;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Flags = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Flags;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Magnitude = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Magnitude;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.SoundIndex = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.SoundIndex;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.FactionOrMobileId = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.FactionOrMobileId;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.NextObjectOffset = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.NextObjectOffset;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Action = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Action;
                            break;

                        default:
                            // Debug.Log("GetRdbBDForRdb = unknown RdbResourceTypes");
                            break;
                    }
                }
            }

            return convBlock;
        }

        public static DFBlockRotation.RdbBlockDesc GetRdbBDForRdb(DFBlock.RdbBlockDesc block)
        {
            DFBlockRotation.RdbBlockDesc convBlock = new DFBlockRotation.RdbBlockDesc();

            convBlock.ModelReferenceList = new DFBlockRotation.RdbModelReference[block.ModelReferenceList.Length];
            for (int i = 0; i < block.ModelReferenceList.Length; i++)
            {
                convBlock.ModelReferenceList[i].ModelId = block.ModelReferenceList[i].ModelId;
                convBlock.ModelReferenceList[i].ModelIdNum = block.ModelReferenceList[i].ModelIdNum;
                convBlock.ModelReferenceList[i].Description = block.ModelReferenceList[i].Description;
            }

            convBlock.ObjectRootList = new DFBlockRotation.RdbObjectRoot[block.ObjectRootList.Length];
            for (int j = 0; j < block.ObjectRootList.Length; j++)
            {
                if (block.ObjectRootList[j].RdbObjects == null)
                    continue;

                convBlock.ObjectRootList[j].RdbObjects = new DFBlockRotation.RdbObject[block.ObjectRootList[j].RdbObjects.Length];
                for (int k = 0; k < convBlock.ObjectRootList[j].RdbObjects.Length; k++)
                {
                    convBlock.ObjectRootList[j].RdbObjects[k].Position = block.ObjectRootList[j].RdbObjects[k].Position;
                    convBlock.ObjectRootList[j].RdbObjects[k].Index = block.ObjectRootList[j].RdbObjects[k].Index;
                    convBlock.ObjectRootList[j].RdbObjects[k].XPos = block.ObjectRootList[j].RdbObjects[k].XPos;
                    convBlock.ObjectRootList[j].RdbObjects[k].YPos = block.ObjectRootList[j].RdbObjects[k].YPos;
                    convBlock.ObjectRootList[j].RdbObjects[k].ZPos = block.ObjectRootList[j].RdbObjects[k].ZPos;
                    convBlock.ObjectRootList[j].RdbObjects[k].Type = (DFBlockRotation.RdbResourceTypes)block.ObjectRootList[j].RdbObjects[k].Type;

                    switch (convBlock.ObjectRootList[j].RdbObjects[k].Type)
                    {
                        case DFBlockRotation.RdbResourceTypes.Model:
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.XRotation = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.XRotation;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.YRotation = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.YRotation;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ZRotation = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ZRotation;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ModelIndex = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ModelIndex;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.TriggerFlag_StartingLock = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.TriggerFlag_StartingLock;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.SoundIndex = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.SoundIndex;

                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Position = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Position;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Axis = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Axis;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Duration = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Duration;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Magnitude = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Magnitude;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.NextObjectOffset = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.NextObjectOffset;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.PreviousObjectOffset = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.PreviousObjectOffset;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.NextObjectIndex = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.NextObjectIndex;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Flags = block.ObjectRootList[j].RdbObjects[k].Resources.ModelResource.ActionResource.Flags;
                            break;

                        case DFBlockRotation.RdbResourceTypes.Light:
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Unknown1 = block.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Unknown1;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Unknown2 = block.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Unknown2;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Radius = block.ObjectRootList[j].RdbObjects[k].Resources.LightResource.Radius;
                            break;

                        case DFBlockRotation.RdbResourceTypes.Flat:
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Position = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Position;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.TextureArchive = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.TextureArchive;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.TextureRecord = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.TextureRecord;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Flags = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Flags;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Magnitude = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Magnitude;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.SoundIndex = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.SoundIndex;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.FactionOrMobileId = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.FactionOrMobileId;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.NextObjectOffset = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.NextObjectOffset;
                            convBlock.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Action = block.ObjectRootList[j].RdbObjects[k].Resources.FlatResource.Action;
                            break;

                        default:
                            Debug.Log("GetRdbBDForRdb = unknown RdbResourceTypes");
                            break;
                    }
                }
            }

            return convBlock;
        }

        public static void RotateBlock(string blockName, int rotation)
        {
            DFBlock block = new DFBlock();
            DFBlock rotatedBlock = new DFBlock();
            string path = Path.Combine(testPath, "RMB", blockName + ".RMB.json");
            block = JsonConvert.DeserializeObject<DFBlock>(File.ReadAllText(path));

            rotatedBlock.Position = block.Position;
            rotatedBlock.Index = block.Index;
            rotatedBlock.Name = block.Name.Substring(0, 3) + (rotation + 1).ToString() + block.Name.Substring(4, 4) + ".RMB";
            rotatedBlock.Type = (DFBlock.BlockTypes)block.Type;

            switch (rotatedBlock.Type)
            {
                case DFBlock.BlockTypes.Rmb:
                    rotatedBlock.RmbBlock = RotateRMBBlock(rotatedBlock.Name, block.RmbBlock, rotation);
                    break;

                default:
                    break;
            }
            path = Path.Combine(testPath, "RMB", rotatedBlock.Name + ".json");
            var json = JsonConvert.SerializeObject(rotatedBlock, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(path, json);
        }

        public static DFBlock.RmbBlockDesc RotateRMBBlock(string rotatedName, DFBlock.RmbBlockDesc rmbBlock, int rotation)
        {
            DFBlock.RmbBlockDesc rotatedRmb = new DFBlock.RmbBlockDesc();

            rotatedRmb.FldHeader.NumBlockDataRecords = rmbBlock.FldHeader.NumBlockDataRecords;
            rotatedRmb.FldHeader.NumMisc3dObjectRecords = rmbBlock.FldHeader.NumMisc3dObjectRecords;
            rotatedRmb.FldHeader.NumMiscFlatObjectRecords = rmbBlock.FldHeader.NumMiscFlatObjectRecords;

            rotatedRmb.FldHeader.BlockPositions = new DFBlock.RmbFldBlockPositions[rmbBlock.FldHeader.BlockPositions.Length];
            for (int i = 0; i < rmbBlock.FldHeader.BlockPositions.Length; i++)
            {
                rotatedRmb.FldHeader.BlockPositions[i] = RotateBlockPositions(rmbBlock.FldHeader.BlockPositions[i], rotation);
                rotatedRmb.FldHeader.BlockPositions[i].YRotation = rmbBlock.FldHeader.BlockPositions[i].YRotation - (512 * (rotation + 1));
                if (rotatedRmb.FldHeader.BlockPositions[i].YRotation >= 2048)
                    rotatedRmb.FldHeader.BlockPositions[i].YRotation -= 2048;
                if (rotatedRmb.FldHeader.BlockPositions[i].YRotation <= -2048)
                    rotatedRmb.FldHeader.BlockPositions[i].YRotation += 2048;
            }

            rotatedRmb.FldHeader.BuildingDataList = rmbBlock.FldHeader.BuildingDataList;
            rotatedRmb.FldHeader.BlockDataSizes = rmbBlock.FldHeader.BlockDataSizes;
            rotatedRmb.FldHeader.GroundData.Header = rmbBlock.FldHeader.GroundData.Header;

            rotatedRmb.FldHeader.GroundData.GroundTiles = null;
            rotatedRmb.FldHeader.GroundData.GroundScenery = null;
            rotatedRmb.FldHeader.GroundData.GroundTiles1d = new DFBlock.RmbGroundTiles[256];
            rotatedRmb.FldHeader.GroundData.GroundScenery1d = new DFBlock.RmbGroundScenery[256];

            for (int k = 0; k < 16; k++)
            {
                for (int j = 0; j < 16; j++)
                {
                    (int, int) rotPos = RotateCoord(j, k, rotation, 16);
                    rotatedRmb.FldHeader.GroundData.GroundTiles1d[j + k * 16] = RotateGroundTiles(rmbBlock.FldHeader.GroundData.GroundTiles1d[rotPos.Item1 + rotPos.Item2 * 16], rotation);

                    (int, int) gsMod = GetGSMod(rotation);
                    int gsIndex = (j + gsMod.Item1) + (k + gsMod.Item2) * 16;
                    if (gsIndex >= 0 && gsIndex < 256)
                        rotatedRmb.FldHeader.GroundData.GroundScenery1d[gsIndex] = rmbBlock.FldHeader.GroundData.GroundScenery1d[rotPos.Item1 + rotPos.Item2 * 16];
                }
            }

            rotatedRmb.FldHeader.AutoMapData = new uint[64 * 64];
            for (int l = 0; l < (64 * 64); l++)
            {
                int x = l % 64;
                int y = l / 64;
                (int, int) rotPos = RotateCoord(x, y, rotation, 64);
                rotatedRmb.FldHeader.AutoMapData[l] = rmbBlock.FldHeader.AutoMapData[rotPos.Item1 + rotPos.Item2 * 64];
            }

            rotatedRmb.FldHeader.Name = rotatedName;
            rotatedRmb.FldHeader.OtherNames = rmbBlock.FldHeader.OtherNames;

            rotatedRmb.SubRecords = new DFBlock.RmbSubRecord[rmbBlock.SubRecords.Length];
            for (int m = 0; m < rmbBlock.SubRecords.Length; m++)
            {
                rotatedRmb.SubRecords[m] = RotateSubRecordPositions(rmbBlock.SubRecords[m], rotation);
                rotatedRmb.SubRecords[m].YRotation = rmbBlock.SubRecords[m].YRotation - (512 * (rotation + 1));
                if (rotatedRmb.SubRecords[m].YRotation >= 2048)
                    rotatedRmb.SubRecords[m].YRotation -= 2048;
                if (rotatedRmb.SubRecords[m].YRotation <= -2048)
                    rotatedRmb.SubRecords[m].YRotation += 2048;

                rotatedRmb.SubRecords[m].Exterior = rmbBlock.SubRecords[m].Exterior;
                // rotatedRmb.SubRecords[m].Exterior = RotateExteriorInterior(rmbBlock.SubRecords[m].Exterior, rotation);
                rotatedRmb.SubRecords[m].Interior = rmbBlock.SubRecords[m].Interior;
                // rotatedRmb.SubRecords[m].Interior = RotateExteriorInterior(rmbBlock.SubRecords[m].Interior, rotation);
            }

            rotatedRmb.Misc3dObjectRecords = new DFBlock.RmbBlock3dObjectRecord[rmbBlock.Misc3dObjectRecords.Length];
            for (int n = 0; n < rmbBlock.Misc3dObjectRecords.Length; n++)
            {
                rotatedRmb.Misc3dObjectRecords[n] = RotateBlock3dObjectRecord(rmbBlock.Misc3dObjectRecords[n], rotation, true);
            }

            rotatedRmb.MiscFlatObjectRecords = new DFBlock.RmbBlockFlatObjectRecord[rmbBlock.MiscFlatObjectRecords.Length];
            for (int o = 0; o < rmbBlock.MiscFlatObjectRecords.Length; o++)
            {
                rotatedRmb.MiscFlatObjectRecords[o] = RotateBlockFlatObjectRecord(rmbBlock.MiscFlatObjectRecords[o], rotation, true);
            }
            return rotatedRmb;
        }

        public static (int, int) GetGSMod(int rotation)
        {
            (int, int) modifier;

            switch (rotation)
            {
                case 0:
                    return (1, 0);
                case 1:
                    return (1, 1);
                case 2:
                    return (0, 1);
                default:
                    Debug.Log("GetGSMod = wrong rotation value!");
                    return (0, 0);
            }
        }

        public static DFBlock.RmbBlockData RotateExteriorInterior(DFBlock.RmbBlockData blockData, int rotation)
        {
            DFBlock.RmbBlockData resultingBlockData = new DFBlock.RmbBlockData();
            resultingBlockData.Header = blockData.Header;

            resultingBlockData.Block3dObjectRecords = new DFBlock.RmbBlock3dObjectRecord[blockData.Block3dObjectRecords.Length];
            for (int i = 0; i < blockData.Block3dObjectRecords.Length; i++)
            {
                resultingBlockData.Block3dObjectRecords[i] = RotateBlock3dObjectRecord(blockData.Block3dObjectRecords[i], rotation, false);
            }

            resultingBlockData.BlockFlatObjectRecords = new DFBlock.RmbBlockFlatObjectRecord[blockData.BlockFlatObjectRecords.Length];
            for (int j = 0; j < blockData.BlockFlatObjectRecords.Length; j++)
            {
                resultingBlockData.BlockFlatObjectRecords[j] = RotateBlockFlatObjectRecord(blockData.BlockFlatObjectRecords[j], rotation, false);
            }

            resultingBlockData.BlockSection3Records = new DFBlock.RmbBlockSection3Record[blockData.BlockSection3Records.Length];
            for (int k = 0; k < blockData.BlockSection3Records.Length; k++)
            {
                resultingBlockData.BlockSection3Records[k] = RotateBlockSection3Record(blockData.BlockSection3Records[k], rotation);
            }

            resultingBlockData.BlockPeopleRecords = new DFBlock.RmbBlockPeopleRecord[blockData.BlockPeopleRecords.Length];
            for (int l = 0; l < blockData.BlockPeopleRecords.Length; l++)
            {
                resultingBlockData.BlockPeopleRecords[l] = RotateBlockPeopleRecord(blockData.BlockPeopleRecords[l], rotation);
            }

            resultingBlockData.BlockDoorRecords = new DFBlock.RmbBlockDoorRecord[blockData.BlockDoorRecords.Length];
            for (int m = 0; m < blockData.BlockDoorRecords.Length; m++)
            {
                resultingBlockData.BlockDoorRecords[m] = RotateBlockDoorRecord(blockData.BlockDoorRecords[m], rotation);
            }
            return resultingBlockData;
        }

        public static DFBlock.RmbFldBlockPositions RotateBlockPositions(DFBlock.RmbFldBlockPositions blockPos, int rotation)
        {
            DFBlock.RmbFldBlockPositions rotatedBlockPos = new DFBlock.RmbFldBlockPositions();
            int size = 4096;
            bool zNegative = blockPos.ZPos < 0;
            if (zNegative)
                blockPos.ZPos *= -1;

            switch (rotation)
            {
                case 0: // 90 degrees
                    rotatedBlockPos.XPos = size - blockPos.ZPos;
                    rotatedBlockPos.ZPos = blockPos.XPos;
                    break;
                case 1: // 180 degrees
                    rotatedBlockPos.XPos = size - blockPos.XPos;
                    rotatedBlockPos.ZPos = size - blockPos.ZPos;
                    break;
                case 2: // 270 degrees
                    rotatedBlockPos.XPos = blockPos.ZPos;
                    rotatedBlockPos.ZPos = size - blockPos.XPos;
                    break;
                default:
                    Debug.Log("RotateBlockPosition = wrong rotation value!");
                    break;
            }
            if (zNegative)
                rotatedBlockPos.ZPos *= -1;
            rotatedBlockPos.YRotation = blockPos.YRotation; // YRotation get calculated outside this method, since it's the same calculation for every block.
            return rotatedBlockPos;
        }

        public static DFBlock.RmbSubRecord RotateSubRecordPositions(DFBlock.RmbSubRecord subRecord, int rotation)
        {
            DFBlock.RmbSubRecord rotatedSubPos = new DFBlock.RmbSubRecord();
            int size = 4096;

            switch (rotation)
            {
                case 0: // 90 degrees
                    rotatedSubPos.XPos = size - subRecord.ZPos;
                    rotatedSubPos.ZPos = subRecord.XPos;
                    break;
                case 1: // 180 degrees
                    rotatedSubPos.XPos = size - subRecord.XPos;
                    rotatedSubPos.ZPos = size - subRecord.ZPos;
                    break;
                case 2: // 270 degrees
                    rotatedSubPos.XPos = subRecord.ZPos;
                    rotatedSubPos.ZPos = size - subRecord.XPos;
                    break;
                default:
                    Debug.Log("RotateSubRecordPosition = wrong rotation value!");
                    break;
            }
            rotatedSubPos.YRotation = subRecord.YRotation; // YRotation get calculated outside this method, since it's the same calculation for every block.
            return rotatedSubPos;
        }

        public static DFBlock.RmbGroundTiles RotateGroundTiles(DFBlock.RmbGroundTiles tile, int rotation)
        {
            DFBlock.RmbGroundTiles resultingTile = new DFBlock.RmbGroundTiles();
            resultingTile.TileBitfield = (byte)tile.TextureRecord;
            resultingTile.TextureRecord = tile.TextureRecord;

            switch (rotation)
            {
                case 0:
                    // 1st version
                    resultingTile.IsRotated = !tile.IsRotated;
                    if (!tile.IsRotated)
                        resultingTile.IsFlipped = !tile.IsFlipped;
                    else resultingTile.IsFlipped = tile.IsFlipped;
                    break;
                case 1:
                    // Flipped 1st
                    resultingTile.IsRotated = tile.IsRotated;
                    resultingTile.IsFlipped = !tile.IsFlipped;
                    break;
                case 2:
                    // 1st version
                    resultingTile.IsRotated = !tile.IsRotated;
                    if (!tile.IsRotated)
                        resultingTile.IsFlipped = tile.IsFlipped;
                    else resultingTile.IsFlipped = !tile.IsFlipped;
                    break;
                default:
                    Debug.Log("RotateGroundTiles = wrong rotation value!");
                    break;
            }

            if (resultingTile.IsRotated)
                resultingTile.TileBitfield += 64;
            if (resultingTile.IsFlipped)
                resultingTile.TileBitfield += 128;

            return resultingTile;
        }

        public static DFBlock.RmbBlock3dObjectRecord RotateBlock3dObjectRecord(DFBlock.RmbBlock3dObjectRecord objRec, int rotation, bool isExterior)
        {
            DFBlock.RmbBlock3dObjectRecord resulting3dObj = new DFBlock.RmbBlock3dObjectRecord();
            resulting3dObj = objRec;
            (int, int) rotatedCoord;
            if (isExterior) rotatedCoord = RotateCoord3dObj(objRec.XPos, objRec.ZPos, rotation, 4096);
            else rotatedCoord = RotateCoordInt(objRec.XPos, objRec.ZPos, rotation);
            resulting3dObj.XPos = rotatedCoord.Item1;
            resulting3dObj.ZPos = rotatedCoord.Item2;
            // rotatedRmb.SubRecords[m].Exterior.Block3dObjectRecords[n].XPos = rmbBlock.SubRecords[m].Exterior.Block3dObjectRecords[n].XPos;
            // rotatedRmb.SubRecords[m].Exterior.Block3dObjectRecords[n].YPos = rmbBlock.SubRecords[m].Exterior.Block3dObjectRecords[n].YPos;
            // rotatedRmb.SubRecords[m].Exterior.Block3dObjectRecords[n].ZPos = rmbBlock.SubRecords[m].Exterior.Block3dObjectRecords[n].ZPos;
            // rotatedRmb.SubRecords[m].Exterior.Block3dObjectRecords[n].XRotation = rmbBlock.SubRecords[m].Exterior.Block3dObjectRecords[n].XRotation;
            resulting3dObj.YRotation = (short)(objRec.YRotation - (512 * (rotation + 1)));
            // if (isExterior) resulting3dObj.YRotation = (short)(objRec.YRotation - (512 * (rotation + 1)));
            // else resulting3dObj.YRotation = (short)(objRec.YRotation + (512 * (rotation + 1)));
            if (resulting3dObj.YRotation >= 2048)
                resulting3dObj.YRotation -= 2048;
            if (resulting3dObj.YRotation <= -2048)
                resulting3dObj.YRotation += 2048;
            // rotatedRmb.SubRecords[m].Exterior.Block3dObjectRecords[n].ZRotation = rmbBlock.SubRecords[m].Exterior.Block3dObjectRecords[n].ZRotation;
            return resulting3dObj;
        }

        // public static DFBlockRotation.RmbBlock3dObjectRecord RotateBlock3dObjectRecordInt(DFBlockRotation.RmbBlock3dObjectRecord objRec, int rotation, int area)
        // {
        //     DFBlockRotation.RmbBlock3dObjectRecord resulting3dObj = new DFBlockRotation.RmbBlock3dObjectRecord();
        //     resulting3dObj = objRec;
        //     (int, int) rotatedCoord = RotateCoordInt(objRec.XPos, objRec.ZPos, rotation);
        //     resulting3dObj.XPos = rotatedCoord.Item1;
        //     resulting3dObj.ZPos = rotatedCoord.Item2;
        //     // rotatedRmb.SubRecords[m].Exterior.Block3dObjectRecords[n].XPos = rmbBlock.SubRecords[m].Exterior.Block3dObjectRecords[n].XPos;
        //     // rotatedRmb.SubRecords[m].Exterior.Block3dObjectRecords[n].YPos = rmbBlock.SubRecords[m].Exterior.Block3dObjectRecords[n].YPos;
        //     // rotatedRmb.SubRecords[m].Exterior.Block3dObjectRecords[n].ZPos = rmbBlock.SubRecords[m].Exterior.Block3dObjectRecords[n].ZPos;
        //     // rotatedRmb.SubRecords[m].Exterior.Block3dObjectRecords[n].XRotation = rmbBlock.SubRecords[m].Exterior.Block3dObjectRecords[n].XRotation;
        //     resulting3dObj.YRotation = (short)(objRec.YRotation + (512 * (rotation + 1)));
        //     if (resulting3dObj.YRotation >= 2048)
        //         resulting3dObj.YRotation -= 2048;
        //      if (resulting3dObj.YRotation <= -2048)
        //         resulting3dObj.YRotation += 2048;
        //     // rotatedRmb.SubRecords[m].Exterior.Block3dObjectRecords[n].ZRotation = rmbBlock.SubRecords[m].Exterior.Block3dObjectRecords[n].ZRotation;
        //     return resulting3dObj;
        // }

        public static DFBlock.RmbBlockFlatObjectRecord RotateBlockFlatObjectRecord(DFBlock.RmbBlockFlatObjectRecord objRec, int rotation, bool isExterior)
        {
            DFBlock.RmbBlockFlatObjectRecord resultingFlatObj = new DFBlock.RmbBlockFlatObjectRecord();
            resultingFlatObj = objRec;
            (int, int) rotatedCoord;
            if (isExterior) rotatedCoord = RotateCoord(objRec.XPos, objRec.ZPos, rotation, 4096);
            else rotatedCoord = RotateCoordInt(objRec.XPos, objRec.ZPos, rotation);
            resultingFlatObj.XPos = rotatedCoord.Item1;
            resultingFlatObj.ZPos = rotatedCoord.Item2;
            return resultingFlatObj;
        }

        public static DFBlock.RmbBlockSection3Record RotateBlockSection3Record(DFBlock.RmbBlockSection3Record objRec, int rotation)
        {
            DFBlock.RmbBlockSection3Record resultingSec3Rec = new DFBlock.RmbBlockSection3Record();
            resultingSec3Rec = objRec;
            return resultingSec3Rec;
        }

        public static DFBlock.RmbBlockPeopleRecord RotateBlockPeopleRecord(DFBlock.RmbBlockPeopleRecord objRec, int rotation)
        {
            DFBlock.RmbBlockPeopleRecord resultingPeopleRec = new DFBlock.RmbBlockPeopleRecord();
            resultingPeopleRec = objRec;
            return resultingPeopleRec;
        }

        public static DFBlock.RmbBlockDoorRecord RotateBlockDoorRecord(DFBlock.RmbBlockDoorRecord objRec, int rotation)
        {
            DFBlock.RmbBlockDoorRecord resultingDoorRec = new DFBlock.RmbBlockDoorRecord();
            resultingDoorRec = objRec;
            return resultingDoorRec;
        }

        public static (int, int) RotateCoord(int x, int z, int rotation, int size)
        {
            (int, int) rotatedCoord;
            bool zNegative = z < 0;
            size--;
            if (zNegative)
                z *= -1;



            switch (rotation)
            {
                case 0: // 90 degrees
                    if (size == 4095) rotatedCoord = (size - z, x);
                    else rotatedCoord = (z, size - x);
                    break;
                case 1: // 180 degrees
                    rotatedCoord = (size - x, size - z);
                    break;
                case 2: // 270 degrees
                    if (size == 4095) rotatedCoord = (z, size - x);
                    else rotatedCoord = (size - z, x);
                    break;
                default:
                    Debug.Log("RotateCoord = wrong rotation value!");
                    rotatedCoord = (-1, -1);
                    break;
            }

            if (zNegative)
                rotatedCoord = (rotatedCoord.Item1, rotatedCoord.Item2 * -1);

            return rotatedCoord;
        }

        public static (int, int) RotateCoord3dObj(int x, int z, int rotation, int size)
        {
            (int, int) rotatedCoord;
            bool zNegative = z < 0;
            size--;
            if (zNegative)
                z *= -1;

            switch (rotation)
            {
                case 0: // 90 degrees
                    rotatedCoord = (size - z, x);
                    break;
                case 1: // 180 degrees
                    rotatedCoord = (size - x, size - z);
                    break;
                case 2: // 270 degrees
                    rotatedCoord = (z, size - x);
                    break;
                default:
                    Debug.Log("RotateCoord = wrong rotation value!");
                    rotatedCoord = (-1, -1);
                    break;
            }

            if (zNegative)
                rotatedCoord = (rotatedCoord.Item1, rotatedCoord.Item2 * -1);

            return rotatedCoord;
        }

        public static (int, int) RotateCoordInt(int x, int z, int rotation)
        {
            (int, int) rotatedCoord;

            switch (rotation)
            {
                case 0: // 90 degrees
                    rotatedCoord = (z, x * -1);
                    break;
                case 1: // 180 degrees
                    rotatedCoord = (x * -1, z * -1);
                    break;
                case 2: // 270 degrees
                    rotatedCoord = (z * -1, x);
                    break;
                default:
                    Debug.Log("RotateCoord = wrong rotation value!");
                    rotatedCoord = (-1, -1);
                    break;
            }
            return rotatedCoord;
        }

        public static void ConvertToNewBlockFormat()
        {
            Worldmap mapTile = new Worldmap();

            dungeonNamesList = new string[Enum.GetNames(typeof(DFRegion.DungeonTypes)).Length][];
            dungeonNamesList = JsonConvert.DeserializeObject<string[][]>(File.ReadAllText(Path.Combine(testPath, "Tamriel", "DungeonEpithets.json")));

            dungeonTypeBlocks = new string[Enum.GetNames(typeof(DFRegion.DungeonTypes)).Length][];
            dungeonTypeBlocks = JsonConvert.DeserializeObject<string[][]>(File.ReadAllText(Path.Combine(testPath, "DungeonBlocks.json")));

            int randomChance = UnityEngine.Random.Range(int.MaxValue, -1);
            int randomRotation;
            string sizeChar;
            string provChar;
            bool blockChanged;

            for (int y = 0; y < MAXYCOORD / TILESIZE; y++)
            {
                for (int x = 0; x < MAXXCOORD / TILESIZE; x++)
                {
                    mapTile = JsonConvert.DeserializeObject<Worldmap>(File.ReadAllText(Path.Combine(testPath, "Locations", "map" + (x + y * MapsFile.TileX).ToString("00000") + ".json")));
                    // Debug.Log("Working on mapTile " + x + ", " + y);

                    if (mapTile.LocationCount <= 0)
                    {
                        mapTile.DiscardMapTile();
                        continue;
                    }

                    for (int i = 0; i < mapTile.Locations.Length; i++)
                    {
                        bool subDone = false;
                        provinceIndex = GetProvinceIndex(mapTile.Locations[i].RegionIndex);
                        sizeChar = GetBlockSize(mapTile.Locations[i].MapTableData.LocationType);
                        provChar = GetArchitecturalStyle((MapsFile.Climates)mapTile.Locations[i].Climate.WorldClimate);

                        for (int j = 0; j < mapTile.Locations[i].Exterior.ExteriorData.BlockNames.Length; j++)
                        {
                            string blockName = mapTile.Locations[i].Exterior.ExteriorData.BlockNames[j];
                            randomRotation = UnityEngine.Random.Range(0, 4);

                            // OG "CUST" blocks have to be substituted with the new RMB format
                            if (blockName.StartsWith("CUS"))
                            {
                                blockName = SubstituteUniqueBlockNames(blockName);
                            }

                            // Don't rotate ships, just change the name to the new format
                            else if (blockName.StartsWith("SHI"))
                            {
                                blockName = blockName.Substring(0, 3) + "0" + blockName.Substring(4, 8);
                            }

                            // Walls can rotate, but it has meaning only for Wayton's new wall blocks
                            else if (blockName.StartsWith("WAL"))
                            {
                                int wallProg = int.Parse(blockName.Substring(6, 2));
                                wallProg = GetWallSubgroup(wallProg, randomRotation);

                                blockName = blockName.Substring(0, 3) + randomRotation.ToString() + "ZA" + wallProg.ToString("00");
                            }

                            else if (mapTile.Locations[i].MapTableData.LocationType == DFRegion.LocationTypes.TownVillage &&
                                     (HasBlocks("RES", mapTile.Locations[i].Exterior.ExteriorData.BlockNames, 2, out int viilageChance) ||
                                      HasBlocks("CLO", mapTile.Locations[i].Exterior.ExteriorData.BlockNames, 1, out viilageChance) ||
                                      HasBlocks("GEN", mapTile.Locations[i].Exterior.ExteriorData.BlockNames, 2, out viilageChance)) &&
                                     UnityEngine.Random.Range(0, 10) <= (--viilageChance) && !subDone)
                            {
                                blockName = SubstituteVillageBlock() + randomRotation.ToString() + provChar + "S" + "00.RMB";
                                subDone = true;
                            }

                            else if (mapTile.Locations[i].MapTableData.LocationType == DFRegion.LocationTypes.TownHamlet &&
                                     (HasBlocks("RES", mapTile.Locations[i].Exterior.ExteriorData.BlockNames, 3, out int hamletChance) ||
                                      HasBlocks("FIL", mapTile.Locations[i].Exterior.ExteriorData.BlockNames, 1, out hamletChance) ||
                                      HasBlocks("GEN", mapTile.Locations[i].Exterior.ExteriorData.BlockNames, 2, out hamletChance)) &&
                                     UnityEngine.Random.Range(0, 10) <= (--hamletChance) && !subDone)
                            {
                                blockName = SubstituteHamletBlock() + randomRotation.ToString() + provChar + "M" + "00.RMB";
                                subDone = true;
                            }

                            else if (mapTile.Locations[i].MapTableData.LocationType == DFRegion.LocationTypes.TownCity &&
                                     mapTile.Locations[i].Name != "Sentinel" &&
                                     mapTile.Locations[i].Name != "Daggerfall" &&
                                     mapTile.Locations[i].Name != "Wayrest" &&
                                     (HasBlocks("RES", mapTile.Locations[i].Exterior.ExteriorData.BlockNames, 3, out int cityChance) ||
                                      HasBlocks("FIL", mapTile.Locations[i].Exterior.ExteriorData.BlockNames, 1, out cityChance) ||
                                      HasBlocks("GEN", mapTile.Locations[i].Exterior.ExteriorData.BlockNames, 2, out cityChance)) &&
                                     UnityEngine.Random.Range(0, 10) <= (--cityChance) && !subDone)
                            {
                                blockName = SubstituteCityBlock() + randomRotation.ToString() + provChar + "L" + "00.RMB";
                                subDone = true;
                            }

                            // This first time, I need to insert FG blocks with the new size indicators;
                            // From next time, FIG blocks should behave like any other.
                            else if (blockName.StartsWith("FIG"))
                            {
                                int randomFIGChance = UnityEngine.Random.Range(0, int.MaxValue);

                                if (blockName.Substring(4, 1).Equals("A"))
                                {
                                    blockName = "FIG" + randomRotation.ToString() + "A" + sizeChar + (randomFIGChance % 2).ToString("00") + ".RMB";
                                }
                                else
                                {
                                    if (!sizeChar.Equals("S"))
                                        blockName = "FIG" + randomRotation.ToString() + blockName.Substring(4, 1) + sizeChar + "00.RMB";
                                    else
                                        blockName = "FIG" + randomRotation.ToString() + blockName.Substring(4, 1) + sizeChar + (randomChance % 3).ToString("00") + ".RMB";
                                }
                            }

                            // Only S sized WEAP block exists in the original game, I need to insert the others
                            // Since ARM blocks are the opposite (just L and M blocks), I could re-randomize their distribution
                            // whenever I encounter one or the other.
                            else if (blockName.StartsWith("WEA") || blockName.StartsWith("ARM"))
                            {
                                randomChance = UnityEngine.Random.Range(0, 2);
                                if (randomChance % 2 == 1)
                                {
                                    if (blockName.StartsWith("WEA"))
                                        blockName = blockName.Replace("WEA", "ARM");

                                    else // if (blockName.StartsWith("ARM"))                                    
                                        blockName = blockName.Replace("ARM", "WEA");
                                }
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 5, sizeChar);
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                            }

                            // No S sized ALC, BAN, BOO, GEM, PAW block exists in the original game. Where could I insert those?
                            // What about a system based on internal routes? Too complicated?
                            else if (blockName.StartsWith("ALC") ||
                                     blockName.StartsWith("BAN") ||
                                     blockName.StartsWith("BOO") ||
                                     blockName.StartsWith("GEM") ||
                                     blockName.StartsWith("PAW"))
                            {
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 5, sizeChar);
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                            }

                            // Only S sized CLOT block exists in the original game.
                            else if (blockName.StartsWith("CLO"))
                            {
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 5, sizeChar);
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                            }

                            // // Only L sized LIB block exists in the original game. Actually, this could make sense
                            else if (blockName.StartsWith("LIB"))
                            {
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 5, sizeChar);
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                            }

                            // TEM block now have both a generic and a specialized version;
                            // for now, I'm substituting OG blocks with the specialized versions,
                            // thus I can keep track of the OG distribution of deities.
                            else if (blockName.StartsWith("TEM"))
                            {
                                bool genericTemp = false;
                                bool randTemp = false;
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());

                                int flip = UnityEngine.Random.Range(0, 2);
                                int flop = UnityEngine.Random.Range(0, 2);
                                if (flip == 1)
                                {
                                    genericTemp = true;
                                }
                                if (flop == 1 && sizeChar != "L")
                                {
                                    randTemp = true;
                                }

                                int randDeity = UnityEngine.Random.Range(0, 8);

                                if (mapTile.Locations[i].MapTableData.LocationType == DFRegion.LocationTypes.ReligionTemple)
                                {
                                    int prop = UnityEngine.Random.Range(0, 13);
                                    if (prop == 0) sizeChar = "L";
                                    else if (prop < 4) sizeChar = "M";
                                    else sizeChar = "S";

                                    string deityToSubstitute = string.Empty;
                                    int counter = 0;
                                    do
                                    {
                                        if (mapTile.Locations[i].Name.Contains(deityNames[counter]))
                                            deityToSubstitute = deityNames[counter];
                                        counter++;
                                    }
                                    while (deityToSubstitute.Equals(string.Empty));


                                    if (randTemp)
                                    {
                                        mapTile.Locations[i].Name =
                                        mapTile.Locations[i].Exterior.RecordElement.Header.LocationName =
                                        mapTile.Locations[i].Exterior.ExteriorData.AnotherName =
                                        mapTile.Locations[i].Name.Replace(deityToSubstitute, deityNames[randDeity]);

                                        if (!genericTemp) blockName = ModifyBlockName(blockName, 6, deityCode[Array.IndexOf(deityNames, deityToSubstitute)]);
                                    }
                                }
                                blockName = ModifyBlockName(blockName, 4, provChar);
                                blockName = ModifyBlockName(blockName, 5, sizeChar);

                                if (genericTemp) blockName = blockName.Substring(0, 6) + "00.RMB";
                                if (randTemp) blockName = blockName.Substring(0, 6) + deityCode[randDeity] + "0.RMB";

                                if (!CheckBlockExistance(blockName))
                                    blockName = blockName.Substring(0, 6) + "00.RMB";

                                // Debug.Log("Checking temple block " + blockName);
                                // if (!File.Exists(Path.Combine(testPath, "RMB", blockName + ".json")))
                                // {
                                //     blockName = blockName.Substring(0, 6) + "00.RMB";
                                // }
                            }

                            // Only S sized MANR block exists in the original game.
                            // For now, I'm putting a generic proportion of L-M-S MAN blocks.
                            // I leave to calculate modfied proportions based on gov type
                            // to the location generator.
                            else if (blockName.StartsWith("MAN"))
                            {
                                int prop = UnityEngine.Random.Range(0, 13);
                                if (prop == 0) sizeChar = "L";
                                else if (prop < 4) sizeChar = "M";
                                else sizeChar = "S";

                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 5, sizeChar);
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                            }

                            // No FURN blocks in OG. Where to put them?
                            // else if (blockName.StartsWith("FUR"))
                            // {

                            // }

                            else if (blockName.StartsWith("PAL"))
                            {
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());

                                if (blockName.Substring(4, 1).Equals("B") ||
                                    blockName.Substring(4, 1).Equals("G"))
                                {
                                    int newProg = UnityEngine.Random.Range(0, 4);
                                    blockName = ModifyBlockName(blockName, 4, "Z");
                                    blockName = blockName.Replace(blockName.Substring(6, 2), newProg.ToString("00"));
                                }
                                else
                                {
                                    int newClimate = UnityEngine.Random.Range(0, 11);
                                    if (newClimate < 4) blockName = ModifyBlockName(blockName, 4, "Z");
                                }
                            }

                            else if (blockName.StartsWith("DUN") ||
                                     blockName.StartsWith("CAS") ||
                                     blockName.StartsWith("DUN") ||
                                     blockName.StartsWith("CAR") ||
                                     blockName.StartsWith("RUI"))
                            {
                                blockName = blockName.Replace(blockName.Substring(0, 3), GetDungeonPrefix(mapTile.Locations[i].MapTableData.DungeonType));
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                if (provChar.Equals("G"))
                                    blockName = ModifyBlockName(blockName, 4, "B");
                                else blockName = ModifyBlockName(blockName, 4, provChar);
                                blockName = ModifyBlockName(blockName, 5, sizeChar);
                                blockName = blockName.Remove(6, 2);
                                blockName = blockName.Insert(6, "00");

                                if (!CheckBlockExistance(blockName))
                                    blockName = ModifyBlockName(blockName, 4, "Z");

                                bool dungeonOk = VerifyDungeonCorrectness(mapTile.Locations[i].MapTableData.LocationType, mapTile.Locations[i].MapTableData.DungeonType, mapTile.Locations[i].Dungeon.Blocks);

                                if (!dungeonOk)
                                    mapTile.Locations[i].Dungeon.Blocks = GenerateDungeonInterior((int)mapTile.Locations[i].MapTableData.LocationType, (int)mapTile.Locations[i].MapTableData.DungeonType);

                                bool correctName = false;
                                for (int r = 0; r < dungeonNamesList[(int)mapTile.MapTable[i].DungeonType].Length; r++)
                                {
                                    // Debug.Log("Working on dungeonNamesList with " + (int)mapTile.MapTable[i].DungeonType + ", and index " + r);
                                    if (mapTile.MapNames[i].Contains(dungeonNamesList[(int)mapTile.MapTable[i].DungeonType][r]))
                                    {
                                        correctName = true;
                                        break;
                                    }
                                }
                                if (!correctName)
                                {
                                    // Debug.Log("Original " + mapTile.MapTable[i].DungeonType + " name: " + mapTile.MapNames[i]);

                                    int index = mapTile.MapIdLookup[mapTile.MapTable[i].MapId];

                                    mapTile.MapIdLookup.Remove(mapTile.MapTable[i].MapId);
                                    mapTile.MapIdLookup.Add(mapTile.MapTable[i].MapId, index);

                                    mapTile.MapNameLookup.Remove(mapTile.MapNames[i]);

                                    do
                                    {
                                        mapTile.MapNames[i] =
                                        mapTile.Locations[i].Name = mapTile.Locations[i].Exterior.RecordElement.Header.LocationName =
                                        mapTile.Locations[i].Exterior.ExteriorData.AnotherName =
                                        mapTile.Locations[i].Dungeon.RecordElement.Header.LocationName = ReGenerateDungeonName(provChar, (int)mapTile.MapTable[i].DungeonType);
                                    }
                                    while (mapTile.MapNameLookup.ContainsKey(mapTile.MapNames[i]));

                                    mapTile.MapNameLookup.Add(mapTile.MapNames[i], index);
                                    // Debug.Log(mapTile.MapTable[i].DungeonType + " new name: " + mapTile.MapNames[i]);
                                }
                            }

                            else if (blockName.StartsWith("WIT"))
                            {
                                blockName = ModifyBlockName(blockName, 3, "0");
                                blockName = ModifyBlockName(blockName, 4, "Z");
                            }

                            // both FARM and SHCK don't have size or climate variants, integrating them should work the same.
                            else if (blockName.StartsWith("SHC") ||
                                     blockName.StartsWith("FAR"))
                            {
                                int prop = UnityEngine.Random.Range(0, 13);
                                if (prop == 0) sizeChar = "L";
                                else if (prop < 4) sizeChar = "M";
                                else sizeChar = "S";

                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 5, sizeChar);
                                blockName = ModifyBlockName(blockName, 4, GetArchitecturalStyle((MapsFile.Climates)mapTile.Locations[i].Climate.WorldClimate));
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                            }

                            // All those small graveyards need to be changed to random minor dungeons;
                            // For the moment I'm applying a flat randomisation, later I'll maybe develop
                            // different dungeon type proportions for different regions.
                            else if (blockName.StartsWith("GRV"))
                            {
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 4, "Z");
                                blockName = ModifyBlockName(blockName, 5, sizeChar);
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                                if (mapTile.Locations[i].MapTableData.DungeonType == DFRegion.DungeonTypes.Cemetery)
                                {

                                    DFRegion.DungeonTypes dType = PickDungeonType((MapsFile.Climates)(mapTile.Locations[i].Climate.WorldClimate - 223), GetGovernmentType(mapTile.Locations[i].MapTableData.MapId), CountBorderNum(mapTile.Locations[i].RegionIndex), mapTile.Locations[i].RegionIndex);

                                    mapTile.Locations[i].MapTableData.DungeonType = dType;
                                    blockName = blockName.Replace(blockName.Substring(0, 3), GetDungeonPrefix(dType));
                                    blockName = blockName.Remove(6, 2); blockName = blockName.Insert(6, "00");
                                    if (dType != (int)DFRegion.DungeonTypes.Crypt)
                                    {
                                        // Debug.Log("Original Graveyard name: " + mapTile.MapNames[i]);

                                        int index = mapTile.MapIdLookup[mapTile.MapTable[i].MapId];

                                        mapTile.MapIdLookup.Remove(mapTile.MapTable[i].MapId);
                                        mapTile.MapIdLookup.Add(mapTile.MapTable[i].MapId, index);

                                        mapTile.MapNameLookup.Remove(mapTile.MapNames[i]);

                                        do
                                        {
                                            mapTile.MapNames[i] =
                                            mapTile.Locations[i].Name = mapTile.Locations[i].Exterior.RecordElement.Header.LocationName =
                                            mapTile.Locations[i].Exterior.ExteriorData.AnotherName =
                                            mapTile.Locations[i].Dungeon.RecordElement.Header.LocationName = ReGenerateDungeonName(provChar, (int)dType);
                                        }
                                        while (mapTile.MapNameLookup.ContainsKey(mapTile.MapNames[i]));

                                        mapTile.MapNameLookup.Add(mapTile.MapNames[i], index);
                                        // Debug.Log("Graveyard changed to " + dType + ", new name: " + mapTile.MapNames[i]);
                                    }
                                }
                            }

                            // It would be cool to have different filler for different town sizes.
                            else if (blockName.StartsWith("FIL"))
                            {
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 4, "Z");
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                            }

                            else if (blockName.StartsWith("KRA") ||
                                     blockName.StartsWith("KDR") ||
                                     blockName.StartsWith("KOW") ||
                                     blockName.StartsWith("KMO") ||
                                     blockName.StartsWith("KCA") ||
                                     blockName.StartsWith("KFL") ||
                                     blockName.StartsWith("KHO") ||
                                     blockName.StartsWith("KRO") ||
                                     blockName.StartsWith("KWH") ||
                                     blockName.StartsWith("KSC") ||
                                     blockName.StartsWith("KHA"))
                            {
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 4, provChar);
                                blockName = ModifyBlockName(blockName, 5, sizeChar);
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                            }

                            else if (blockName.StartsWith("THI") ||
                                     blockName.StartsWith("DAR") ||
                                     blockName.StartsWith("MAR"))
                            {
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 4, provChar);
                                blockName = ModifyBlockName(blockName, 5, sizeChar);
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                            }

                            else if (blockName.StartsWith("TVR"))
                            {
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());

                                if (mapTile.Locations[i].MapTableData.LocationType == DFRegion.LocationTypes.Tavern)
                                {
                                    int prop = UnityEngine.Random.Range(0, 13);
                                    if (prop == 0) sizeChar = "L";
                                    else if (prop < 4) sizeChar = "M";
                                    else sizeChar = "S";
                                }
                                else
                                {
                                    blockName = ModifyBlockName(blockName, 5, sizeChar);
                                }
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                            }

                            else if (blockName.StartsWith("GEN") ||
                                     blockName.StartsWith("RES") ||
                                     blockName.StartsWith("MAG"))
                            {
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 5, sizeChar);
                                blockName = blockName.Remove(6);
                                blockName += "00.RMB";
                            }

                            // Shrines need to be incremented *badly*...
                            else if (blockName.StartsWith("SHR"))
                            {
                                blockName = ModifyBlockName(blockName, 3, randomRotation.ToString());
                                blockName = ModifyBlockName(blockName, 4, "Z");
                                // blockName = ModifyBlockName(blockName, 5, "A");
                            }
                            else
                            {
                                Debug.Log("BlockName " + blockName + "not found!");
                            }

                            if (!blockName.EndsWith(".RMB"))
                                blockName += ".RMB";

                            if (!CheckBlockExistance(blockName))
                            {
                                if (blockName.Substring(4, 1).Equals("G"))
                                    blockName = ModifyBlockName(blockName, 4, "B");
                                else Debug.Log("Created block with name " + blockName + " that does not exist!");
                            }


                            mapTile.Locations[i].Exterior.ExteriorData.BlockNames[j] = blockName;
                        }

                        mapTile.Locations[i].Exterior.Buildings = RegenerateBuildingsArray(mapTile.Locations[i].Exterior.ExteriorData.BlockNames, mapTile.Locations[i].RegionIndex, mapTile.Locations[i].Name, mapTile.Locations[i].MapTableData.LocationType, out mapTile.Locations[i].Exterior.BuildingCount, out mapTile.Locations[i].MapTableData.Key);

                        // Roadside Tavern needs to change their name to one of the tavern's (picking the one with highest quality)
                        if (mapTile.Locations[i].MapTableData.LocationType == DFRegion.LocationTypes.Tavern)
                        {
                            (int, int) selectedTavern = (-1, -1);
                            for (int h = 0; h < mapTile.Locations[i].Exterior.Buildings.Length; h++)
                            {
                                if (mapTile.Locations[i].Exterior.Buildings[h].BuildingType == DFLocation.BuildingTypes.Tavern &&
                                    selectedTavern.Item1 < mapTile.Locations[i].Exterior.Buildings[h].Quality)
                                    selectedTavern = (mapTile.Locations[i].Exterior.Buildings[h].Quality, mapTile.Locations[i].Exterior.Buildings[h].NameSeed);
                            }
                            string tavernName = TavernNames.GetName(selectedTavern.Item2, DFLocation.BuildingTypes.Tavern, 510, string.Empty, mapTile.Locations[i].RegionName);
                            int pickedSuffix = UnityEngine.Random.Range(0, Enum.GetNames(typeof(LocationNamesList.TavernEpithets)).Length);
                            tavernName = string.Concat(tavernName, " ", ((LocationNamesList.TavernEpithets)pickedSuffix).ToString());

                            mapTile.Locations[i].Name = mapTile.Locations[i].Exterior.RecordElement.Header.LocationName = mapTile.Locations[i].Exterior.ExteriorData.AnotherName = tavernName;
                        }
                    }

                    var json = JsonConvert.SerializeObject(mapTile, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                    File.WriteAllText(Path.Combine(testPath, "Locations", "map" + (x + y * MapsFile.TileX).ToString("00000") + ".json"), json);
                }
            }
        }

        /// <summary>
        /// Record the highest progressive for each block type and subgroup.
        /// This is later used to insert new blocks with the right proportions.
        /// </summary>
        /// <returns></returns>
        public static void CalculateBlockLimits()
        {
            int[][] oldBlockLimits = new int[blockPrefixes.Length - 5][];
            int[][] newBlockLimits = new int[blockPrefixes.Length - 5][];
            List<string>[][] blockUsed = new List<string>[blockPrefixes.Length - 5][];

            for (int i = 0; i < blockPrefixes.Length - 5; i++)
            {
                if (i == Array.IndexOf(blockPrefixes, "TEM"))
                {
                    oldBlockLimits[i] = new int[rmbBlockClimate.Length * (deityCode.Length + 1)];
                    newBlockLimits[i] = new int[rmbBlockClimate.Length * (deityCode.Length + 1)];
                    blockUsed[i] = new List<string>[rmbBlockClimate.Length * (deityCode.Length + 1)];
                    for (int j = 0; j < (rmbBlockClimate.Length * (deityCode.Length + 1)); j++)
                    {
                        oldBlockLimits[i][j] = -1;
                        newBlockLimits[i][j] = -1;
                        blockUsed[i][j] = new List<string>();
                    }
                }
                else
                {
                    oldBlockLimits[i] = new int[rmbBlockClimate.Length];
                    newBlockLimits[i] = new int[rmbBlockClimate.Length];
                    blockUsed[i] = new List<string>[rmbBlockClimate.Length];
                    for (int j = 0; j < rmbBlockClimate.Length; j++)
                    {
                        oldBlockLimits[i][j] = -1;
                        newBlockLimits[i][j] = -1;
                        blockUsed[i][j] = new List<string>();
                    }
                }
            }

            Worldmap mapTile = new Worldmap();

            for (int y = 0; y < MAXYCOORD / TILESIZE; y++)
            {
                for (int x = 0; x < MAXXCOORD / TILESIZE; x++)
                {
                    mapTile = JsonConvert.DeserializeObject<Worldmap>(File.ReadAllText(Path.Combine(testPath, "Locations", "map" + (x + y * MapsFile.TileX).ToString("00000") + ".json")));

                    if (mapTile.LocationCount <= 0)
                    {
                        mapTile.DiscardMapTile();
                        continue;
                    }

                    for (int k = 0; k < mapTile.Locations.Length; k++)
                    {
                        for (int l = 0; l < mapTile.Locations[k].Exterior.ExteriorData.BlockNames.Length; l++)
                        {
                            string blockName = mapTile.Locations[k].Exterior.ExteriorData.BlockNames[l];

                            // Skipping unique blocks
                            if (!int.TryParse(blockName.Substring(3, 1), out int uselessValue))
                                continue;

                            int prefixIndex = Array.IndexOf(blockPrefixes, blockName.Substring(0, 3));
                            int climateIndex = Array.IndexOf(rmbBlockClimate, blockName.Substring(4, 2));
                            // int prog;
                            int deityProg = 0;

                            if (prefixIndex == Array.IndexOf(blockPrefixes, "TEM"))
                            {
                                // prog = int.Parse(blockName.Substring(7, 1));
                                deityProg = Array.IndexOf(deityCode, blockName.Substring(6, 1)) + 1;
                                // Debug.Log("blockName: " + blockName + ", climateIndex: " + climateIndex + ", deityProg: " + deityProg);
                                // if (oldBlockLimits[prefixIndex][climateIndex * deityCode.Length + deityProg] <= prog)
                                // {
                                //     oldBlockLimits[prefixIndex][climateIndex * deityCode.Length + deityProg] = prog;
                                // }
                                if (!blockUsed[prefixIndex][climateIndex * (deityCode.Length + 1) + deityProg].Exists(el => el.Substring(4).Equals(blockName.Substring(4))))
                                    blockUsed[prefixIndex][climateIndex * (deityCode.Length + 1) + deityProg].Add(blockName);
                            }
                            else if (prefixIndex < Array.IndexOf(blockPrefixes, "B"))
                            {
                                // prog = int.Parse(blockName.Substring(6, 2));

                                // Debug.Log("blockName: " + blockName + ", prefixIndex: " + prefixIndex + ", climateIndex: " + climateIndex);
                                // if (oldBlockLimits[prefixIndex][climateIndex] <= prog)
                                // {
                                //     oldBlockLimits[prefixIndex][climateIndex] = prog;
                                // }
                                // Debug.Log("Evaluating " + blockName + " substring: " + blockName.Substring(4));
                                if (!blockUsed[prefixIndex][climateIndex].Exists(el => el.Substring(4).Equals(blockName.Substring(4))))
                                    blockUsed[prefixIndex][climateIndex].Add(GetGenericBlockName(blockName));
                            }
                            else continue;
                        }
                    }
                }
            }

            for (int p = 0; p < blockPrefixes.Length - 5; p++)
            {
                for (int q = 0; q < rmbBlockClimate.Length; q++)
                {
                    oldBlockLimits[p][q] = blockUsed[p][q].Count() - 1;
                }
            }

            string rmbPath = Path.Combine(testPath, "RMB");
            for (int m = 0; m < blockPrefixes.Length; m++)
            {
                for (int n = 0; n < rmbBlockClimate.Length; n++)
                {
                    int counter;

                    if (m == Array.IndexOf(blockPrefixes, "TEM"))
                    {
                        for (int o = 0; o < (deityCode.Length + 1); o++)
                        {
                            counter = 0;
                            if (o == 0)
                            {
                                while (File.Exists(Path.Combine(rmbPath, blockPrefixes[m] + "0" + rmbBlockClimate[n] + counter.ToString("00") + ".RMB.json")))
                                {
                                    newBlockLimits[m][n * (deityCode.Length + 1) + o]++;
                                    counter++;
                                }
                            }
                            else while (File.Exists(Path.Combine(rmbPath, blockPrefixes[m] + "0" + rmbBlockClimate[n] + deityCode[o - 1] + counter.ToString() + ".RMB.json")))
                            {
                                newBlockLimits[m][n * (deityCode.Length + 1) + o]++;
                                counter++;
                            }
                        }
                    }
                    else if (m < Array.IndexOf(blockPrefixes, "B"))
                    {
                        counter = 0;
                        while (File.Exists(Path.Combine(rmbPath, blockPrefixes[m] + "0" + rmbBlockClimate[n] + counter.ToString("00") + ".RMB.json")))
                        {
                            newBlockLimits[m][n]++;
                            counter++;
                        }
                    }
                    else continue;
                }
            }

            string path = Path.Combine(testPath, "oldBlockLimits.json");
            var jsonOldLimits = JsonConvert.SerializeObject(oldBlockLimits, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(path, jsonOldLimits);

            path = Path.Combine(testPath, "newBlockLimits.json");
            var jsonNewLimits = JsonConvert.SerializeObject(newBlockLimits, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(path, jsonNewLimits);

            path = Path.Combine(testPath, "BlockUsed.json");
            var jsonBlockUsed = JsonConvert.SerializeObject(blockUsed, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(path, jsonBlockUsed);
        }

        public static void IntegrateNewBlocks()
        {
            Debug.Log("Integrating New Blocks");
            Worldmap mapTile = new Worldmap();
            worldData = JsonConvert.DeserializeObject<WorldSettings>(File.ReadAllText(Path.Combine("D:/da_shiny_oneBU/daggerfall/DaggerfallGameFiles/arena2/Maps", "WorldData.json")));
            int[][] oldBlockLimits = JsonConvert.DeserializeObject<int[][]>(File.ReadAllText(Path.Combine(testPath, "oldBlockLimits.json")));
            int[][] newBlockLimits = JsonConvert.DeserializeObject<int[][]>(File.ReadAllText(Path.Combine(testPath, "newBlockLimits.json")));
            List<string>[][] blocksUsed = JsonConvert.DeserializeObject<List<string>[][]>(File.ReadAllText(Path.Combine(testPath, "BlockUsed.json")));

            for (int y = 0; y < MAXYCOORD / TILESIZE; y++)
            {
                for (int x = 0; x < MAXXCOORD / TILESIZE; x++)
                {
                    mapTile = JsonConvert.DeserializeObject<Worldmap>(File.ReadAllText(Path.Combine(testPath, "Locations", "map" + (x + y * MapsFile.TileX).ToString("00000") + ".json")));
                    // Debug.Log("Working on mapTile " + x + ", " + y);

                    if (mapTile.LocationCount <= 0)
                    {
                        mapTile.DiscardMapTile();
                        continue;
                    }

                    for (int i = 0; i < mapTile.Locations.Length; i++)
                    {
                        bool somethingChanged = false;
                        bool templeBlock = false;
                        bool wallBlock = false;
                        provinceIndex = GetProvinceIndex(mapTile.Locations[i].RegionIndex);
                        for (int j = 0; j < mapTile.Locations[i].Exterior.ExteriorData.BlockNames.Length; j++)
                        {
                            string blockName = mapTile.Locations[i].Exterior.ExteriorData.BlockNames[j];

                            // Skipping unique blocks
                            if (!int.TryParse(blockName.Substring(3, 1), out int uselessValue))
                                continue;

                            // Determining block size
                            string blockSize = GetBlockSize(mapTile.Locations[i].MapTableData.LocationType);
                            if (blockSize.Equals("")) blockSize = blockName.Substring(5, 1);

                            // Determining block prefix and if it's a wall or temple block (thus requiring additional calculations)
                            int blockPref = Array.IndexOf(blockPrefixes, blockName.Substring(0, 3));
                            templeBlock = blockPref == Array.IndexOf(blockPrefixes, "TEM");
                            wallBlock = blockPref == Array.IndexOf(blockPrefixes, "WAL");

                            // Determining if temple block is specific to a deity or generic
                            int deitySpecific = -1;
                            if (templeBlock)
                            {
                                if (int.TryParse(blockName.Substring(6, 1), out int uselessInt))
                                    deitySpecific = 0;
                                else deitySpecific = Array.IndexOf(deityCode, blockName.Substring(6, 1)) + 1;
                            }

                            // Determing second index
                            int preference = PickPrefixToUse(GetArchitecturalStyle((MapsFile.Climates)mapTile.Locations[i].Climate.WorldClimate), blockSize, blockName.Substring(4, 2), GetBlockTypePreference(blockName.Substring(0, 3)));
                            int blockUsedIndex = preference;
                            if (templeBlock)
                                blockUsedIndex = preference * (deityCode.Length + 1) + deitySpecific;

                            int chance = UnityEngine.Random.Range(0, newBlockLimits[blockPref][blockUsedIndex] + 1);
                            if (wallBlock)
                            {
                                int.TryParse(blockName.Substring(3, 1), out int rotation);
                                chance = GetWallSubgroup(chance, rotation);
                            }

                            string potentialBN = string.Empty;
                            if (templeBlock)
                            {
                                if (deitySpecific == 0)
                                    potentialBN = string.Concat(blockName.Substring(0, 4), rmbBlockClimate[preference], chance.ToString("00"));
                                else potentialBN = string.Concat(blockName.Substring(0, 4), rmbBlockClimate[preference], deityCode[deitySpecific - 1], chance.ToString("0"));
                            }
                            else
                                potentialBN = string.Concat(blockName.Substring(0, 4), rmbBlockClimate[preference], chance.ToString("00"));

                            Debug.Log("potentialBN: " + potentialBN);
                            if (!blocksUsed[blockPref][blockUsedIndex].Contains(GetGenericBlockName(potentialBN)) &&
                                CheckBlockExistance(potentialBN))
                            {
                                mapTile.Locations[i].Exterior.ExteriorData.BlockNames[j] = GetGenericBlockName(potentialBN);
                                somethingChanged = true;
                            }
                        }
                        if (somethingChanged)
                            mapTile.Locations[i].Exterior.Buildings = RegenerateBuildingsArray(mapTile.Locations[i].Exterior.ExteriorData.BlockNames, mapTile.Locations[i].RegionIndex, mapTile.Locations[i].Name, mapTile.Locations[i].MapTableData.LocationType, out mapTile.Locations[i].Exterior.BuildingCount, out mapTile.Locations[i].MapTableData.Key);
                    }

                    var json = JsonConvert.SerializeObject(mapTile, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                    File.WriteAllText(Path.Combine(testPath, "Locations", "map" + (x + y * MapsFile.TileX).ToString("00000") + ".json"), json);
                }
            }
        }

        public static string RandomizeWallVariant(string wallName)
        {
            int wallProg = int.Parse(wallName.Substring(6, 2));
            int normWall = UnityEngine.Random.Range(1, 101);

            int diceRoll = UnityEngine.Random.Range(0, 4);
            wallProg = GetWallSubgroup(wallProg, diceRoll);

            // "normalWallChance" chances the wall block will be a standard one (i.e.: no external structures)
            // For now, wall variants are available only in HighRock version
            if (diceRoll <= normalWallChance)
                return "WAL" + diceRoll.ToString() + "AA" + wallProg.ToString("00") + ".RMB";
            // return wallName.Substring(0, 3) + "0" + wallName.Substring(4, 8);
            else return "WAL" + diceRoll.ToString() + "AA" + wallProg.ToString("00") + ".RMB";
        }

        public static void CalculatePopulationProportion()
        {
            WorldSettings worldData = JsonConvert.DeserializeObject<WorldSettings>(File.ReadAllText(Path.Combine("D:/da_shiny_oneBU/daggerfall/DaggerfallGameFiles/arena2/Maps", "WorldData.json")));

            for (int i = 0; i < worldData.RegionNames.Length; i++)
            {
                // Excluding unused regions
                if (worldData.regionInProvince[(int)ProvinceNames.Unused].Contains(i))
                    continue;
                int resPop = 0;

                // Setting the main population as the province dominant race
                for (int l = 1; l < Enum.GetNames(typeof(ProvinceNames)).Length; l++)
                    if (worldData.regionInProvince[l].Contains(i))
                        resPop += ConvertProvinceToRace((ProvinceNames)l) * 100;

                if (resPop < 100)
                    Debug.Log(worldData.RegionNames[i] + " not present in Tamriel Provinces.");

                int[] provinceCount = new int[Enum.GetNames(typeof(ProvinceNames)).Length];
                (int, int) secondPop = (0, 0);
                (int, int) thirdPop = (0, 0);

                for (int j = 1; j < Enum.GetNames(typeof(ProvinceNames)).Length; j++)
                {
                    for (int k = 0; k < 20; k++)
                    {
                        if (worldData.regionInProvince[j].Contains(worldData.regionBorders[i * 20 + k]))
                        {
                            provinceCount[j]++;
                            continue;
                        }
                    }
                }

                for (int j = 1; j < Enum.GetNames(typeof(ProvinceNames)).Length; j++)
                {
                    if (provinceCount[j] > 0)
                    {
                        if (provinceCount[j] > secondPop.Item2)
                        {
                            thirdPop = secondPop;
                            secondPop = (j, provinceCount[j]);
                        }
                        else if (provinceCount[j] > thirdPop.Item2)
                        {
                            thirdPop = (j, provinceCount[j]);
                        }
                    }
                }

                if (secondPop.Item2 == thirdPop.Item2)
                    Debug.Log(worldData.RegionNames[i] + " population proportion needs some manual assesment.");

                if (thirdPop.Item1 == 0)
                    thirdPop = secondPop;

                worldData.regionRaces[i] = resPop + (ConvertProvinceToRace((ProvinceNames)secondPop.Item1) * 10) + ConvertProvinceToRace((ProvinceNames)thirdPop.Item1);
            }

            var json = JsonConvert.SerializeObject(worldData, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(Path.Combine(testPath, "WorldData.json"), json);
        }

        public static void AnalyseDungeonCollocation()
        {
            // Worldmap mapTile = new Worldmap();

            int climateCount = Enum.GetNames(typeof(MapsFile.Climates)).Length;
            int dungTypeCount = Enum.GetNames(typeof(DFRegion.DungeonTypes)).Length;

            int[,] dungeonCount = new int[climateCount, dungTypeCount];

            // Initializing 2d matrix
            for (int cl = 0; cl < climateCount; cl++)
            {
                for (int dtc = 0; dtc < dungTypeCount; dtc++)
                {
                    dungeonCount[cl, dtc] = 0;
                }
            }

            for (int y = 0; y < MAXYCOORD / TILESIZE; y++)
            {
                for (int x = 0; x < MAXXCOORD / TILESIZE; x++)
                {
                    Worldmap mapTile = JsonConvert.DeserializeObject<Worldmap>(File.ReadAllText(Path.Combine(testPath, "Locations", "map" + (x + y * MapsFile.TileX).ToString("00000") + ".json")));

                    if (mapTile.MapTable == null)
                        continue;

                    for (int i = 0; i < mapTile.MapTable.Length; i++)
                    {
                        if (mapTile.MapTable[i].LocationType != DFRegion.LocationTypes.DungeonLabyrinth &&
                            mapTile.MapTable[i].LocationType != DFRegion.LocationTypes.DungeonKeep &&
                            mapTile.MapTable[i].LocationType != DFRegion.LocationTypes.DungeonRuin)
                            continue;

                        dungeonCount[mapTile.Locations[i].Climate.WorldClimate - 223, (int)mapTile.Locations[i].MapTableData.DungeonType]++;
                    }
                }
            }

            var json = JsonConvert.SerializeObject(dungeonCount, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(Path.Combine(testPath, "DungeonCount.json"), json);
        }

        public static void ListSimpleBorders()
        {
            // Debug.Log("Listing simple borders");
            worldData = JsonConvert.DeserializeObject<WorldSettings>(File.ReadAllText(Path.Combine("D:/da_shiny_oneBU/daggerfall/DaggerfallGameFiles/arena2/Maps", "WorldData.json")));

            List<int>[] borders = new List<int>[408];
            for (int h = 0; h < 408; h++)
                borders[h] = new List<int>();

            for (int y = 0; y < MAXYCOORD / TILESIZE; y++)
            {
                for (int x = 0; x < MAXXCOORD / TILESIZE; x++)
                {
                    for (int i = 0; i < TILESIZE; i++)
                    {
                        for (int j = 0; j < TILESIZE; j++)
                        {
                            int ics = j + x * TILESIZE;
                            int ipsilon = i + y * TILESIZE;
                            int currentReg = PoliticInfo.Politic[ics, ipsilon];
                            if (currentReg == 0) currentReg = 31;
                            else currentReg -= 128;

                            int eastReg = 0;
                            if (ics + 1 >= MAXXCOORD)
                                eastReg = currentReg;
                            else
                            {
                                eastReg = PoliticInfo.Politic[ics + 1, ipsilon];
                                if (eastReg == 0) eastReg = 31;
                                else eastReg -= 128;
                            }

                            int southReg = 0;
                            if (ipsilon + 1 >= MAXYCOORD)
                                southReg = currentReg;
                            else
                            {
                                southReg = PoliticInfo.Politic[ics, ipsilon + 1];
                                if (southReg == 0) southReg = 31;
                                else southReg -= 128;
                            }

                            if (worldData.regionInProvince[(int)ProvinceNames.Unused].Contains(currentReg) ||
                                worldData.regionInProvince[(int)ProvinceNames.Unused].Contains(eastReg))
                                continue;

                            if (currentReg != eastReg)
                            {
                                // Debug.Log("Working with " + worldData.RegionNames[currentReg] + " and " + worldData.RegionNames[southReg]);
                                if (!borders[currentReg].Contains(eastReg))
                                    borders[currentReg].Add(eastReg);
                                if (!borders[eastReg].Contains(currentReg))
                                    borders[eastReg].Add(currentReg);
                            }

                            if (worldData.regionInProvince[(int)ProvinceNames.Unused].Contains(southReg))
                                continue;

                            if (currentReg != southReg)
                            {
                                // Debug.Log("Working with " + worldData.RegionNames[currentReg] + " and " + worldData.RegionNames[southReg]);
                                if (!borders[currentReg].Contains(southReg))
                                    borders[currentReg].Add(southReg);
                                if (!borders[southReg].Contains(currentReg))
                                    borders[southReg].Add(currentReg);
                            }
                        }
                    }
                }
            }

            for (int k = 0; k < 408; k++)
            {
                for (int l = 0; l < 20; l++)
                {
                    int rightPoint = l + k * 20;
                    if (l < borders[k].Count)
                        worldData.regionBorders[rightPoint] = borders[k][l];
                    else worldData.regionBorders[rightPoint] = 408;
                }
            }

            var json = JsonConvert.SerializeObject(worldData, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(Path.Combine(testPath, "WorldData.json"), json);
        }

        public static void CreateTextureFiles(int archive)
        {
            string fileName = string.Empty;
            string frameFN = string.Empty;
            string xmlFN = string.Empty;

            int lastRecordIndex = 0;

            bool hasFrame = false;
            int frameNum = 0;

            List<Record> recordList = new List<Record>();

            for (int i = 0; i < 100; i++)
            {
                fileName = archive.ToString("0000000") + "_" + i.ToString() + "-0.png";
                if (!File.Exists(Path.Combine(testPath, "Textures", archive.ToString(), fileName)))
                {
                    recordList.Add(new Record());
                    continue;
                }

                frameFN = archive.ToString("0000000") + "_" + i.ToString() + "-1.png";
                if (File.Exists(Path.Combine(testPath, "Textures", archive.ToString(), frameFN)))
                    hasFrame = true;

                if (hasFrame)
                {
                    do
                    {
                        frameNum++;
                        frameFN = archive.ToString("0000000") + "_" + i.ToString() + "-" + (frameNum + 1).ToString() + ".png";
                    }
                    while (File.Exists(Path.Combine(testPath, "Textures", archive.ToString(), frameFN)));
                }

                byte[] fileBytes = File.ReadAllBytes(Path.Combine(testPath, "Textures", archive.ToString(), fileName));
                Texture2D texture = new Texture2D(1, 1);
                texture.LoadImage(fileBytes);

                Record record = new Record();
                record.Position = 1;
                record.OffsetX = 100;
                record.OffsetY = 100;
                record.Width = (short)texture.width;
                record.Height = (short)texture.height;
                record.Compression = 0;
                record.RecordSize = (uint)(record.Width * record.Height);
                record.DataOffset = 0;
                record.IsNormal = false;
                record.FrameCount = (ushort)frameNum;
                record.Unknown1 = 0;

                xmlFN = archive.ToString("0000000") + "_" + i.ToString() + "-0.xml";
                if (File.Exists(Path.Combine(testPath, "Textures", archive.ToString(), xmlFN)))
                {
                    XElement xml = XElement.Load(Path.Combine(testPath, "Textures", archive.ToString(), xmlFN));
                    Debug.Log("Reading xml for archive " + archive + ", record " + i);
                    record.ScaleX = (short)((float)xml.Element("scaleX") * 256.0f);
                    record.ScaleY = (short)((float)xml.Element("scaleY") * 256.0f);
                }
                else
                {
                    record.ScaleX = 0;
                    record.ScaleY = 0;
                }
                record.Frames = null;

                recordList.Add(record);

                lastRecordIndex = i;
            }
            ;

            List<RecordHeader> recordHeaderList = new List<RecordHeader>();
            for (int j = 0; j <= lastRecordIndex; j++)
            {
                recordHeaderList.Add(new RecordHeader());
            }
            recordList.RemoveRange(lastRecordIndex + 1, recordList.Count - (lastRecordIndex + 1));

            TextureFormat textureFormat = new TextureFormat();
            textureFormat.fileHeader.Position = 0;
            textureFormat.fileHeader.RecordCount = (short)(lastRecordIndex + 1);
            textureFormat.fileHeader.Name = "Insert description for your own good";
            textureFormat.fileHeader.TextureWidth = 100;
            textureFormat.fileHeader.TextureHeight = 100;

            textureFormat.recordHeader = recordHeaderList.ToArray();
            textureFormat.record = recordList.ToArray();

            string path = Path.Combine(testPath, "Textures", "TEXTURE." + archive.ToString("0000000"));
            var json = JsonConvert.SerializeObject(textureFormat, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(path, json);
        }

        public static void CalculateStuffRegardingMaps()
        {
            Worldmap map = new Worldmap();
            worldData = JsonConvert.DeserializeObject<WorldSettings>(File.ReadAllText(Path.Combine(testPath, "WorldData.json")));

            for (int y = 0; y < MAXYCOORD; y++)
            {
                for (int x = 0; x < MAXXCOORD; x++)
                {
                    map = JsonConvert.DeserializeObject<Worldmap>(File.ReadAllText(Path.Combine(testPath, "Locations", "map" + (x + y * MapsFile.TileX).ToString("00000") + ".json")));
                    // Debug.Log("Working on mapTile " + x + ", " + y);

                    if (map.LocationCount <= 0)
                    {
                        map.DiscardMapTile();
                        continue;
                    }
                }
            }
        }

        public static void CorrectRegionIndexes()
        {
            Worldmap mapTile = new Worldmap();
            worldData = JsonConvert.DeserializeObject<WorldSettings>(File.ReadAllText(Path.Combine("D:/da_shiny_oneBU/daggerfall/DaggerfallGameFiles/arena2/Maps", "WorldData.json")));

            for (int y = 0; y < MAXYCOORD / TILESIZE; y++)
            {
                for (int x = 0; x < MAXXCOORD / TILESIZE; x++)
                {
                    mapTile = JsonConvert.DeserializeObject<Worldmap>(File.ReadAllText(Path.Combine(testPath, "Locations", "map" + (x + y * MapsFile.TileX).ToString("00000") + ".json")));

                    if (mapTile.LocationCount <= 0)
                    {
                        mapTile.DiscardMapTile();
                        continue;
                    }

                    bool modified = false;

                    for (int i = 0; i < mapTile.Locations.Length; i++)
                    {
                        if (mapTile.Locations[i].Politic - 128 != mapTile.Locations[i].RegionIndex)
                        {
                            Debug.Log("Found discrepancy between Politic (" + mapTile.Locations[i].Politic + ") and RegionIndex (" + mapTile.Locations[i].RegionIndex + "). Correcting.");
                            int regionIndex = Array.IndexOf(worldData.RegionNames, mapTile.Locations[i].RegionName);

                            if (regionIndex + 128 == mapTile.Locations[i].Politic)
                            {
                                Debug.Log("RegionIndex was incorrect for location " + mapTile.Locations[i].Name + " in " + mapTile.Locations[i].RegionName);
                                mapTile.Locations[i].RegionIndex = regionIndex;
                                modified = true;
                            }
                            else
                            {
                                Debug.Log("Modifying both Politic and RegionIndex.");
                                mapTile.Locations[i].RegionIndex = regionIndex;
                                mapTile.Locations[i].Politic = regionIndex + 128;
                                modified = true;
                                Debug.Log("RegionIndex set to " + mapTile.Locations[i].RegionIndex + ", Politic set to " + mapTile.Locations[i].Politic + " in " + mapTile.Locations[i].RegionName);
                            }
                        }
                    }

                    if (modified)
                    {
                        var json = JsonConvert.SerializeObject(mapTile, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                        File.WriteAllText(Path.Combine(testPath, "Locations", "map" + (x + y * MapsFile.TileX).ToString("00000") + ".json"), json);
                    }
                }
            }
        }

        public static int GetWallSubgroup(int wallProg, int diceRoll)
        {
            int[] corner = { 0, 2, 3, 1 };
            int[] straight = { 4, 7, 6, 5 };
            int[] gate = { 8, 11, 10, 9 };

            int newWallProg = 99;
            int varCounter = 0;

            while (wallProg >= 12)
            {
                wallProg -= 12;
                varCounter++;
            }

            switch (wallProg)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    int cIndex = Array.IndexOf(corner, wallProg);
                    diceRoll += cIndex;
                    if (diceRoll >= 4) diceRoll -= 4;
                    newWallProg = corner[diceRoll];
                    break;

                case 4:
                case 5:
                case 6:
                case 7:
                    int sIndex = Array.IndexOf(straight, wallProg);
                    diceRoll += sIndex;
                    if (diceRoll >= 4) diceRoll -= 4;
                    newWallProg = straight[diceRoll];
                    break;

                case 8:
                case 9:
                case 10:
                case 11:
                    int gIndex = Array.IndexOf(gate, wallProg);
                    diceRoll += gIndex;
                    if (diceRoll >= 4) diceRoll -= 4;
                    newWallProg = gate[diceRoll];
                    break;

                default:
                    // Debug.Log("wallProg: " + wallProg + ", diceRoll: " + diceRoll + " not found!");
                    return 99;
            }

            if (varCounter > 0)
                newWallProg += 12 * varCounter;

            return newWallProg;
        }

        public static string SubstituteUniqueBlockNames(string ogName)
        {
            switch (ogName)
            {
                case "CUSTAA00.RMB": return "GEN!0017.RMB";
                case "CUSTAA01.RMB": return "GEN#0017.RMB";
                case "CUSTAA02.RMB": return "RES!0017.RMB";
                case "CUSTAA03.RMB": return "RES#0017.RMB";
                case "CUSTAA04.RMB": return "BOO!0017.RMB";
                case "CUSTAA05.RMB": return "PAL!0017.RMB";
                case "CUSTAA06.RMB": return "VPH!0009.RMB";
                case "CUSTAA07.RMB": return "HSH!0040.RMB";
                case "CUSTAA08.RMB": return "CRY!0033.RMB";
                case "CUSTAA09.RMB": return "OSH!0026.RMB";
                case "CUSTAA10.RMB": return "CRY!0001.RMB";
                case "CUSTAA19.RMB": return "LAB!0016.RMB";
                case "CUSTAA29.RMB": return "HSH!0023.RMB";
                case "CUSTAA30.RMB": return "HSH!0017.RMB";
                case "CUSTAA34.RMB": return "FIL!0023.RMB";
                case "CUSTAA35.RMB": return "FIL#0023.RMB";
                case "CUSTAA36.RMB": return "FIL$0023.RMB";
                case "CUSTAA42.RMB": return "TVR!0023.RMB";
                case "CUSTAA43.RMB": return "RES!0023.RMB";
                case "CUSTAA44.RMB": return "FIL%0023.RMB";
                case "CUSTAA45.RMB": return "PAL!0023.RMB";
                case "CUSTAA46.RMB": return "FIL&0023.RMB";
                case "CUSTAA54.RMB": return "FIL'0023.RMB";
                case "CUSTAA55.RMB": return "FIL(0023.RMB";
                case "CUSTAA56.RMB": return "FIL)0023.RMB";

                case "CUSTGA00.RMB": return "MAR!0020.RMB";
                case "CUSTGA01.RMB": return "RES!0020.RMB";
                case "CUSTGA02.RMB": return "GEN!0020.RMB";
                case "CUSTGA03.RMB": return "RES#0020.RMB";
                case "CUSTGA04.RMB": return "WEA!0020.RMB";
                case "CUSTGA05.RMB": return "RES$0020.RMB";
                case "CUSTGA06.RMB": return "WEA#0020.RMB";
                case "CUSTGA07.RMB": return "WEA$0020.RMB";
                case "CUSTGA08.RMB": return "RES%0020.RMB";
                case "CUSTGA09.RMB": return "TVR!0020.RMB";
                case "CUSTGA10.RMB": return "ALC!0020.RMB";
                case "CUSTGA11.RMB": return "RES&0020.RMB";
                case "CUSTGA12.RMB": return "RES'0020.RMB";
                case "CUSTGA13.RMB": return "RES(0020.RMB";
                case "CUSTGA14.RMB": return "RES)0020.RMB";
                case "CUSTGA15.RMB": return "FIL!0020.RMB";
                case "CUSTGA16.RMB": return "FIL#0020.RMB";
                case "CUSTGA17.RMB": return "KCA!0020.RMB";
                case "CUSTGA18.RMB": return "RES+0020.RMB";
                case "CUSTGA19.RMB": return "TVR#0020.RMB";
                case "CUSTGA20.RMB": return "FIL$0020.RMB";
                case "CUSTGA21.RMB": return "PAL!0020.RMB";
                case "CUSTGA22.RMB": return "FIL%0020.RMB";
                case "CUSTGA23.RMB": return "TVR$0020.RMB";
                case "CUSTGA24.RMB": return "RES,0020.RMB";
                case "CUSTGA25.RMB": return "MAG!0020.RMB";
                case "CUSTGA26.RMB": return "RES-0020.RMB";
                case "CUSTGA27.RMB": return "FIL&0020.RMB";
                case "CUSTGA28.RMB": return "RES=0020.RMB";
                case "CUSTGA29.RMB": return "RES@0020.RMB";
                case "CUSTGA30.RMB": return "TEM!0020.RMB";
                case "CUSTGA31.RMB": return "RES[0020.RMB";
                case "CUSTGA32.RMB": return "RES]0020.RMB";
                case "CUSTGA33.RMB": return "TVR%0020.RMB";
                case "CUSTGA34.RMB": return "RES^0020.RMB";
                case "CUSTGA35.RMB": return "RES_0020.RMB";

                default:
                    Debug.Log("Block " + ogName + " not listed!");
                    return "";
            }
        }

        public static int GetProvinceIndex(int regInd)
        {
            for (int i = 0; i < worldData.regionInProvince.Length; i++)
            {
                if (worldData.regionInProvince[i].Contains(regInd))
                    return i;
            }

            Debug.Log("Region Index " + regInd + " doesn't appear in any region!");
            return -1;
        }

        public static string GetBlockSize(DFRegion.LocationTypes locType)
        {
            switch (locType)
            {
                case DFRegion.LocationTypes.TownCity:
                case DFRegion.LocationTypes.DungeonLabyrinth:
                    return "L";
                case DFRegion.LocationTypes.TownHamlet:
                case DFRegion.LocationTypes.DungeonKeep:
                    return "M";
                case DFRegion.LocationTypes.TownVillage:
                case DFRegion.LocationTypes.DungeonRuin:
                    return "S";
                case DFRegion.LocationTypes.DungeonMinor:
                    return "N";
                default:
                    return "";
            }
        }

        public static string ModifyBlockName(string bName, int index, string sub)
        {
            // Debug.Log("Modifying block " + bName + " at index " + index + ", sub " + sub);
            bName = bName.Remove(index, 1);
            bName = bName.Insert(index, sub);
            // Debug.Log("bName after modification: " + bName);
            return bName;
        }

        public static string GetDungeonPrefix(DFRegion.DungeonTypes dungeon)
        {
            switch (dungeon)
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
                    Debug.Log("Dungeon Type " + dungeon + " not recognised");
                    return "---";
            }
        }

        public static bool HasBlocks(string blockInit, string[] blockNames, int threshold, out int chanceOfSub)
        {
            int counter = 0;
            switch (blockInit)
            {
                case "RES":
                case "CLO":
                case "FIL":
                    chanceOfSub = 1;
                    break;
                case "GEN":
                    chanceOfSub = 8;
                    break;
                default:
                    chanceOfSub = 0;
                    break;
            }
            for (int i = 0; i < blockNames.Length; i++)
            {
                if (blockNames[i].StartsWith(blockInit))
                    counter++;
                if (counter >= threshold)
                    return true;
            }
            return false;
        }

        public static string SubstituteVillageBlock()
        {
            string[] avBlocks = { "FIG", "ALC", "BAN", "BOO", "GEM", "PAW", "FUR", "LIB", "THI", "DAR", "MAR", "MAG" };
            int chance = UnityEngine.Random.Range(0, avBlocks.Length);
            return avBlocks[chance];
        }

        public static string SubstituteHamletBlock()
        {
            string[] avBlocks = { "CLO", "LIB", "FUR" };
            int chance = UnityEngine.Random.Range(0, avBlocks.Length);
            return avBlocks[chance];
        }

        public static string SubstituteCityBlock()
        {
            string[] avBlocks = { "CLO", "FUR" };
            int chance = UnityEngine.Random.Range(0, avBlocks.Length);
            return avBlocks[chance];
        }

        public static string ReGenerateDungeonName(string provChar, int dungeonType)
        {
            int selectedEpithet;
            int dungeonNameStructure;
            int title;
            int gender;
            int bankType;
            int nameType;
            string resultingName;

            // Let's put a 1/10 chance that bankType will pick a random race name
            if (UnityEngine.Random.Range(0, 10) == 0)
            {
                int[] banks = { 0, 1, 2, 3, 4, 5, 6, 7, 11, 13 };
                bankType = banks[UnityEngine.Random.Range(0, banks.Length)];
            }
            else if (provinceIndex == (int)ProvinceNames.HighRock)
            {
                bankType = 0;
            }
            else bankType = 1;

            if (bankType == 0 && UnityEngine.Random.Range(0, 2) == 1)
                bankType = 12;

            // 0: The (name)'s (epithet);
            // 1: (title) (name)'s (epithet);
            // 2: The (epithet) of (name);
            // 3: The (epithet) of (title) (name);
            dungeonNameStructure = UnityEngine.Random.Range(0, 4);

            // In case it is needed, choose a title (Lord, Baron, etc...);
            title = UnityEngine.Random.Range(0, Enum.GetNames(typeof(RegionManager.Ruler)).Length);

            // In case it is left to chance, choose a name type (First Name, Surname or Monster Name);
            // I need to add some more cases, like first name OR surname, first name OR monster name, etc...
            nameType = UnityEngine.Random.Range(0, 3);

            // This should generate a random gender and, at the same time, make it coordinated with title
            gender = title % 2;

            // Debug.Log("Picking name for dungeonType " + (DFRegion.DungeonTypes)dungeonType);
            switch (dungeonType)
            {
                case (int)DFRegion.DungeonTypes.Crypt:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    // I'll change this so first names and surnames can be used too.

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (any name)'s (epithet);
                            resultingName = string.Concat(GetName(nameType, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // (title) (any name)'s (epithet);
                            resultingName = string.Concat(((RegionManager.Ruler)title).ToString(), " ", GetName(nameType, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (any name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, bankType));
                            return resultingName;

                        case 3:     // The (epithet) of (title) (any name);
                        default:
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", ((RegionManager.Ruler)title).ToString(), " ", GetName(nameType, gender, bankType));
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.OrcStronghold:
                case (int)DFRegion.DungeonTypes.HumanStronghold:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);
                    // Debug.Log("selectedEpithet: " + dungeonNamesList[dungeonType][selectedEpithet] + " (" + selectedEpithet + ")");

                    // If we're generating an Orc Stronghold, change the bankType to orc's
                    if (dungeonType == (int)DFRegion.DungeonTypes.OrcStronghold)
                        bankType = 13;

                    // "Guard" can use only the last two structures;
                    if ((dungeonNamesList[dungeonType][selectedEpithet].ToString()).Equals("Guard"))
                        dungeonNameStructure += 2;

                    // Only "Castle" and "Tower" can use the first structure;
                    else if (!((dungeonNamesList[dungeonType][selectedEpithet]).ToString()).Equals("Castle") ||
                             !((dungeonNamesList[dungeonType][selectedEpithet]).ToString()).Equals("Tower"))
                        dungeonNameStructure += 1;

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (epithet) (surname)
                            resultingName = string.Concat((dungeonNamesList[dungeonType][selectedEpithet]).ToString(), " ", GetName(1, gender, bankType));
                            return resultingName;

                        case 1:     // The (epithet) of (name)
                            resultingName = string.Concat("The ", (dungeonNamesList[dungeonType][selectedEpithet]).ToString(), " of ", GetName(0, gender, bankType));
                            return resultingName;

                        case 2:     // (surname) (epithet)
                            resultingName = string.Concat("The ", (dungeonNamesList[dungeonType][selectedEpithet]).ToString(), " ", GetName(1, gender, bankType));
                            return resultingName;

                        case 3:     // (name)'s (epithet)
                        default:
                            resultingName = string.Concat(GetName(0, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.Prison:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // The (surname) (epithet)
                            resultingName = string.Concat("The ", GetName(1, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(0, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (nameORsurname);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(3, gender, bankType));
                            return resultingName;

                        case 3:     // (name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(0, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.DesecratedTemple:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, bankType));
                            return resultingName;

                        case 3:     // (male name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(0, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.Mine:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    if ((dungeonNamesList[dungeonType][selectedEpithet].Equals("Excavation") ||
                         dungeonNamesList[dungeonType][selectedEpithet].Equals("Lode")) &&
                         nameType == 2)
                        nameType = 0;

                    switch (dungeonNameStructure)
                    {
                        case 0:
                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, bankType));
                            return resultingName;

                        case 3:     // (male OR monster)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(4, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.NaturalCave:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                        case 3:
                        default:
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, bankType));
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.Coven:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    // For this type of dungeon, names are always first names and feminine.
                    nameType = 0;
                    gender = 0;

                    switch (dungeonNameStructure)
                    {
                        case 0:
                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, bankType));
                            return resultingName;

                        case 3:     // (name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(4, gender, bankType), "'s ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.VampireHaunt:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    string eventualTitle = "";
                    if (UnityEngine.Random.Range(0, 10) + 1 > 8)
                        eventualTitle = string.Concat(((RegionManager.Ruler)title).ToString(), " ");

                    switch (dungeonNameStructure)
                    {
                        case 0:     // [title] (name) (epithet);
                            resultingName = string.Concat(eventualTitle, GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of [title] (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", eventualTitle, GetName(nameType, gender, bankType));
                            return resultingName;

                        case 3:     // [title] (name)'s (epithet);
                        default:
                            resultingName = string.Concat(eventualTitle, GetName(2, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.Laboratory:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, bankType));
                            return resultingName;

                        case 3:     // (name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(4, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.HarpyNest:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, bankType));
                            return resultingName;

                        case 3:     // (name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(4, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.RuinedCastle:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(nameType, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(nameType, gender, bankType));
                            return resultingName;

                        case 3:     // (male name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(4, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.SpiderNest:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(2, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(2, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(2, gender, bankType));
                            return resultingName;

                        case 3:     // (male name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(2, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.GiantStronghold:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // The (epithet) of (name)
                            resultingName = string.Concat("The ", (dungeonNamesList[dungeonType][selectedEpithet]).ToString(), " of ", GetName(2, gender, bankType));
                            return resultingName;
                        case 1:     // (name)'s (epithet)
                            resultingName = string.Concat(GetName(2, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                        default:
                            resultingName = GetName(2, gender, bankType);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.DragonsDen:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(2, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(2, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(2, gender, bankType));
                            return resultingName;

                        case 3:     // (male name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(2, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.BarbarianStronghold:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(0, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(0, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(0, gender, bankType));
                            return resultingName;

                        case 3:     // (male name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(0, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.VolcanicCaves:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(2, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(2, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(2, gender, bankType));
                            return resultingName;

                        case 3:     // (male name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(2, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.ScorpionNest:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    switch (dungeonNameStructure)
                    {
                        case 0:     // (name) (epithet);
                            resultingName = string.Concat(GetName(2, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (name) (epithet);
                            resultingName = string.Concat("The ", GetName(2, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 2:     // The (epithet) of (name);
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(2, gender, bankType));
                            return resultingName;

                        case 3:     // (male name)'s (epithet);
                        default:
                            resultingName = string.Concat(GetName(2, gender, bankType, true), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;
                    }

                case (int)DFRegion.DungeonTypes.Cemetery:

                    selectedEpithet = UnityEngine.Random.Range(0, dungeonNamesList[dungeonType].Length);

                    if (dungeonNamesList[dungeonType][selectedEpithet].Equals("Graves"))
                        dungeonNameStructure = 1;

                    else if (dungeonNamesList[dungeonType][selectedEpithet].Equals("Crypts") ||
                             dungeonNamesList[dungeonType][selectedEpithet].Equals("Tombs"))
                        dungeonNameStructure = UnityEngine.Random.Range(0, 2);

                    else dungeonNameStructure = 0;

                    switch (dungeonNameStructure)
                    {
                        case 0:     // The (family name) (epithet) - BG, Cemetery, Crypts, Graveyard, Tombs, Vaults
                            resultingName = string.Concat("The ", GetName(1, gender, bankType), " ", dungeonNamesList[dungeonType][selectedEpithet]);
                            return resultingName;

                        case 1:     // The (epithet) of (family name) - Crypts, Graves, Tombs
                        default:
                            resultingName = string.Concat("The ", dungeonNamesList[dungeonType][selectedEpithet], " of ", GetName(1, gender, bankType));
                            return resultingName;
                    }


                default:
                    return "";
            }
        }

        public static string GetName(int nameType, int gender = 0, int regionalism = 0, bool saxGen = false)
        {
            int bankType = regionalism;
            // Debug.Log("nameType: " + nameType + ", bankType: " + bankType);
            string result = "";

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
                        result = DaggerfallUnity.Instance.NameHelper.Surname((NameHelper.BankTypes)bankType, UnityEngine.Random.Range(0, 2));
                    else result = DaggerfallUnity.Instance.NameHelper.FirstName((NameHelper.BankTypes)bankType, (DaggerfallWorkshop.Game.Entity.Genders)(UnityEngine.Random.Range(0, 2)));
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

        // return a block type climate preference:
        // 0: Specific climate
        // 1: Generic ("Z")
        // 2: No preference
        // 3: Original climate and size
        public static int GetBlockTypePreference(string blockType)
        {
            switch (blockType)
            {
                case "FIL":
                    return 3;
                case "PAL":
                    return 2;
                case "WAL":
                    return 1;
                default:
                    return 0;
            }
        }

        public static int PickPrefixToUse(string provinceChar, string blockSize, string orClimSize, int preference)
        {
            int climPref = Array.IndexOf(rmbBlockClimate, string.Concat(provinceChar, blockSize));
            int crossPref = Array.IndexOf(rmbBlockClimate, string.Concat("Z", blockSize));
            int genPref = Array.IndexOf(rmbBlockClimate, string.Concat(provinceChar, "A"));
            int orPref = Array.IndexOf(rmbBlockClimate, orClimSize);

            int diceRoll = UnityEngine.Random.Range(0, 100);

            switch (preference)
            {
                case 0:     // specific climate
                    if (diceRoll < 1) return 0;
                    if (diceRoll < 3) return orPref;
                    if (diceRoll < 7) return crossPref;
                    if (diceRoll < 15) return genPref;
                    return climPref;

                case 1:     // generic climate
                    if (diceRoll < 1) return climPref;
                    if (diceRoll < 3) return orPref;
                    if (diceRoll < 7) return genPref;
                    if (diceRoll < 15) return 0;
                    return crossPref;

                case 2:     // No preference
                    if (diceRoll < 1) return 0;
                    if (diceRoll < 5) return genPref;
                    if (diceRoll < 10) return crossPref;
                    if (diceRoll < 15) return climPref;
                    return orPref;

                case 3:     // Original climate and size
                    if (diceRoll < 1) return 0;
                    if (diceRoll < 3) return genPref;
                    if (diceRoll < 7) return crossPref;
                    if (diceRoll < 15) return climPref;
                    return orPref;

                default:
                    return orPref;
            }
        }

        public static DFLocation.BuildingData[] RegenerateBuildingsArray(string[] blocks, int regionInd, string locName, DFRegion.LocationTypes locType, out ushort buildingCount, out uint locationKey)
        {
            List<DFLocation.BuildingData> regBuildData = new List<DFLocation.BuildingData>();
            List<int> nameseedUsed = new List<int>();
            buildingCount = 0;
            short sectorCounter = 0;
            locationKey = 0;

            for (int i = 0; i < blocks.Length; i++)
            {
                Debug.Log("Regeneratin building list for block " + blocks[i]);
                string path = Path.Combine(testPath, "RMB", GetGenericBlockName(blocks[i], true));
                DFBlock block = JsonConvert.DeserializeObject<DFBlock>(File.ReadAllText(path));

                // This is used for one of the many useless things I thought about:
                // if in a block two buildings have the same type AND quality, they
                // get the same name. This was to make those Hammerfell "double taverns"
                // make a little more sense, but maybe will have some other kind of future
                // use? Who knows...
                List<(int, int)> sameNameBuilding = new List<(int, int)>();

                for (int j = 0; j < block.RmbBlock.FldHeader.BuildingDataList.Length; j++)
                {
                    DFLocation.BuildingData building = new DFLocation.BuildingData();
                    sectorCounter += (short)UnityEngine.Random.Range(3, 10);
                    buildingCount++;

                    if (sameNameBuilding.Contains(((int)block.RmbBlock.FldHeader.BuildingDataList[j].BuildingType, block.RmbBlock.FldHeader.BuildingDataList[j].Quality)))
                    {
                        building.NameSeed = regBuildData[sameNameBuilding.IndexOf(((int)block.RmbBlock.FldHeader.BuildingDataList[j].BuildingType, block.RmbBlock.FldHeader.BuildingDataList[j].Quality))].NameSeed;
                        building.Quality = regBuildData[sameNameBuilding.IndexOf(((int)block.RmbBlock.FldHeader.BuildingDataList[j].BuildingType, block.RmbBlock.FldHeader.BuildingDataList[j].Quality))].Quality;

                        sameNameBuilding.Add((-1, -1));
                    }
                    else
                    {
                        sameNameBuilding.Add(((int)block.RmbBlock.FldHeader.BuildingDataList[j].BuildingType, block.RmbBlock.FldHeader.BuildingDataList[j].Quality));

                        // Making sure the nameseed is unique for every building;
                        // chances of two buidlngs of the same type with the same nameseed
                        // are probably really close to 0, but with a world this big you never know.
                        do
                        {
                            building.NameSeed = (ushort)UnityEngine.Random.Range(0, ushort.MaxValue);
                        }
                        while (nameseedUsed.Contains(building.NameSeed));
                        nameseedUsed.Add(building.NameSeed);

                        building.Quality = (byte)UnityEngine.Random.Range(1, 21);
                    }

                    if (building.BuildingType == DFLocation.BuildingTypes.Temple)
                    {
                        if (!int.TryParse(blocks[i].Substring(6, 1), out int useless))
                            building.FactionId = (ushort)GetTempleFactionId(Array.IndexOf(deityCode, blocks[i].Substring(6, 1)));
                        else if (locType == DFRegion.LocationTypes.ReligionTemple)
                        {
                            building.FactionId = (ushort)GetTempleFactionId(Array.IndexOf(deityNames, GetDeityName(locName)));
                        }
                        else
                        {
                            if (UnityEngine.Random.Range(0, 10) != 0)
                            {
                                building.FactionId = (ushort)worldData.regionTemples[regionInd];
                            }
                            else building.FactionId = (ushort)GetTempleFactionId(UnityEngine.Random.Range(0, 8));
                        }
                    }
                    else building.FactionId = block.RmbBlock.FldHeader.BuildingDataList[j].FactionId;
                    building.Sector = sectorCounter;
                    building.LocationId = 0;
                    building.BuildingType = block.RmbBlock.FldHeader.BuildingDataList[j].BuildingType;

                    regBuildData.Add(building);

                    locationKey |= GenerateLocationsWindow.GetPartialKey(building);
                }

                if (locType == DFRegion.LocationTypes.DungeonLabyrinth ||
                locType == DFRegion.LocationTypes.DungeonKeep ||
                locType == DFRegion.LocationTypes.DungeonRuin ||
                locType == DFRegion.LocationTypes.DungeonMinor)
                {
                    short entrances = -1;
                    for (int e = 0; e < block.RmbBlock.MiscFlatObjectRecords.Length; e++)
                    {
                        if (block.RmbBlock.MiscFlatObjectRecords[e].TextureArchive == 199 &&
                            block.RmbBlock.MiscFlatObjectRecords[e].TextureRecord == 10)
                            entrances++;
                    }
                    locationKey = (uint)entrances;
                }
            }

            return regBuildData.ToArray();
        }

        public static string GetGenericBlockName(string blockName, bool inFileFormat = false)
        {
            string result;
            if (!int.TryParse(blockName.Substring(3, 1), out int uselessValue))
                result = blockName;
            else result = blockName.Substring(0, 3) + "0" + blockName.Substring(4);
            if (inFileFormat)
            {
                return result.Substring(0, 8) + ".RMB.json";
            }
            return result.Substring(0, 8) + ".RMB";
        }

        public static DFRegion.DungeonTypes PickDungeonType(MapsFile.Climates climate, GovernmentType government, int borderNum, int regionInd)
        {
            int[] dungCoeff = new int[(int)DFRegion.DungeonTypes.Cemetery];
            for (int h = 0; h < dungCoeff.Length; h++)
                dungCoeff[h] = 0;
            for (int i = 0; i < (int)DFRegion.DungeonTypes.Cemetery; i++)
            {
                if ((DFRegion.DungeonTypes)i == DFRegion.DungeonTypes.ScorpionNest &&
                     !worldData.regionInProvince[(int)ProvinceNames.Hammerfell].Contains(regionInd))
                {
                    dungCoeff[i] = 0;
                    continue;
                }

                dungCoeff[i] += GetClimateCoeff((DFRegion.DungeonTypes)i, climate);
                dungCoeff[i] += GetGovCoeff((DFRegion.DungeonTypes)i, government);
                dungCoeff[i] += GetBordersCoeff((DFRegion.DungeonTypes)i, borderNum);
                dungCoeff[i] += GetMiscCoeff((DFRegion.DungeonTypes)i, regionInd);

                if (rareDungeons.Contains((DFRegion.DungeonTypes)i))
                    dungCoeff[i] = dungCoeff[i] / 2;
            }

            int totalChance = 0;
            for (int j = 0; j < dungCoeff.Length; j++)
                totalChance += dungCoeff[j];
            int diceRoll = UnityEngine.Random.Range(0, totalChance);
            DFRegion.DungeonTypes pickedDung = DFRegion.DungeonTypes.NoDungeon;
            int counter = 0;

            while (diceRoll >= 0)
            {
                pickedDung = (DFRegion.DungeonTypes)counter;
                diceRoll -= dungCoeff[counter];
                counter++;
            }
            return pickedDung;
        }

        public static int GetClimateCoeff(DFRegion.DungeonTypes dungType, MapsFile.Climates climate)
        {
            switch (climate)
            {
                case MapsFile.Climates.Desert:
                    switch (dungType)
                    {
                        case DFRegion.DungeonTypes.BarbarianStronghold:
                            return rare;
                        case DFRegion.DungeonTypes.VolcanicCaves:
                            return rarer;
                        default:
                            return normal;
                    }

                case MapsFile.Climates.Desert2:
                    switch (dungType)
                    {
                        case DFRegion.DungeonTypes.BarbarianStronghold:
                        case DFRegion.DungeonTypes.VolcanicCaves:
                            return rare;
                        default:
                            return normal;
                    }

                case MapsFile.Climates.Mountain:
                    switch (dungType)
                    {
                        case DFRegion.DungeonTypes.OrcStronghold:
                        case DFRegion.DungeonTypes.GiantStronghold:
                            return common;
                        case DFRegion.DungeonTypes.Mine:
                        case DFRegion.DungeonTypes.HarpyNest:
                            return veryCommon;
                        default:
                            return normal;
                    }
                case MapsFile.Climates.Rainforest:
                case MapsFile.Climates.Subtropical:
                    switch (dungType)
                    {
                        case DFRegion.DungeonTypes.DragonsDen:
                        case DFRegion.DungeonTypes.VolcanicCaves:
                            return rarest;
                        default:
                            return normal;
                    }
                case MapsFile.Climates.Swamp:
                    switch (dungType)
                    {
                        case DFRegion.DungeonTypes.Mine:
                            return rare;
                        case DFRegion.DungeonTypes.DragonsDen:
                        case DFRegion.DungeonTypes.VolcanicCaves:
                            return rarest;
                        default:
                            return normal;
                    }
                case MapsFile.Climates.MountainWoods:
                    switch (dungType)
                    {
                        case DFRegion.DungeonTypes.OrcStronghold:
                        case DFRegion.DungeonTypes.Mine:
                        case DFRegion.DungeonTypes.VampireHaunt:
                        case DFRegion.DungeonTypes.HarpyNest:
                        case DFRegion.DungeonTypes.SpiderNest:
                            return common;
                        case DFRegion.DungeonTypes.VolcanicCaves:
                            return rare;
                        default:
                            return normal;
                    }
                case MapsFile.Climates.Woodlands:
                    switch (dungType)
                    {
                        case DFRegion.DungeonTypes.HarpyNest:
                        case DFRegion.DungeonTypes.SpiderNest:
                        case DFRegion.DungeonTypes.GiantStronghold:
                            return common;
                        case DFRegion.DungeonTypes.VolcanicCaves:
                            return rarer;
                        default:
                            return normal;
                    }
                case MapsFile.Climates.HauntedWoodlands:
                    switch (dungType)
                    {
                        case DFRegion.DungeonTypes.HarpyNest:
                            return veryCommon;
                        case DFRegion.DungeonTypes.SpiderNest:
                            return common;
                        case DFRegion.DungeonTypes.VolcanicCaves:
                            return rarer;
                        default:
                            return normal;
                    }
                default:
                    return 10;
            }
        }

        public static int GetGovCoeff(DFRegion.DungeonTypes dungType, GovernmentType government)
        {
            int lowMod = 0;
            int highMod = 0;
            switch (government)
            {
                case GovernmentType.None:
                    lowMod = ordinary;
                    highMod = rarest;
                    break;
                case GovernmentType.Fiefdom:
                    lowMod = veryCommon;
                    highMod = rarer;
                    break;
                case GovernmentType.Barony:
                    lowMod = common;
                    highMod = rare;
                    break;
                // case GovernmentType.County:
                //     lowMod = highMod = normal;
                //     break;
                case GovernmentType.March:
                    lowMod = rare;
                    highMod = common;
                    break;
                case GovernmentType.Duchy:
                    lowMod = rarer;
                    highMod = veryCommon;
                    break;
                case GovernmentType.Kingdom:
                case GovernmentType.Empire:
                    lowMod = rarest;
                    highMod = ordinary;
                    break;
                default:
                    lowMod = highMod = normal;
                    break;
            }

            switch (dungType)
            {
                case DFRegion.DungeonTypes.Crypt:
                case DFRegion.DungeonTypes.OrcStronghold:
                case DFRegion.DungeonTypes.DesecratedTemple:
                case DFRegion.DungeonTypes.Coven:
                case DFRegion.DungeonTypes.Laboratory:
                case DFRegion.DungeonTypes.HarpyNest:
                case DFRegion.DungeonTypes.SpiderNest:
                case DFRegion.DungeonTypes.GiantStronghold:
                case DFRegion.DungeonTypes.BarbarianStronghold:
                case DFRegion.DungeonTypes.ScorpionNest:
                    return lowMod;
                case DFRegion.DungeonTypes.HumanStronghold:
                case DFRegion.DungeonTypes.Prison:
                case DFRegion.DungeonTypes.RuinedCastle:
                    return highMod;
                case DFRegion.DungeonTypes.VampireHaunt:
                    if (highMod == ordinary) return ordinary;
                    else return lowMod;
                default:
                    return normal;
            }
        }

        public static int GetBordersCoeff(DFRegion.DungeonTypes dungType, int borderNum)
        {
            int coeff = 1 + (borderNum * 3);
            switch (dungType)
            {
                case DFRegion.DungeonTypes.HumanStronghold:
                case DFRegion.DungeonTypes.RuinedCastle:
                    return coeff;
                default:
                    return normal;
            }
        }

        public static int GetMiscCoeff(DFRegion.DungeonTypes dungType, int regionInd)
        {
            switch (dungType)
            {
                case DFRegion.DungeonTypes.OrcStronghold:
                    if (orcishCountries[0].Contains(regionInd))
                        return ordinary;
                    else if (orcishCountries[1].Contains(regionInd))
                        return veryCommon;
                    else if (orcishCountries[2].Contains(regionInd))
                        return common;
                    else return normal;
                default:
                    return normal;
            }
        }

        public static GovernmentType GetGovernmentType(ulong mapId)
        {
            int x = (int)mapId % MAXXCOORD;
            int y = (int)mapId / MAXXCOORD;
            // Debug.Log("x: " + x + ", y: " + y + ", regionIndex: " + PoliticInfo.ConvertMapPixelToRegionIndex(x, y) + ", region name: " + WorldInfo.WorldSetting.RegionNames[PoliticInfo.ConvertMapPixelToRegionIndex(x, y)]);
            FactionFile.FactionData region = FactionAtlas.FactionDictionary[FactionAtlas.FactionToId[ConvertFactionName(WorldInfo.WorldSetting.RegionNames[PoliticInfo.ConvertMapPixelToRegionIndex(x, y)])]];
            int governmentType = (region.ruler + 1) / 2;
            return (GovernmentType)governmentType;
        }

        public static int CountBorderNum(int regionInd)
        {
            int counter = 0;
            while (worldData.regionBorders[regionInd * 20 + counter] != 408 &&
                   counter < 20)
            {
                counter++;
            }
            return counter;
        }

        public static string ConvertFactionName(string name)
        {
            switch (name)
            {
                case "Alik'r Desert":
                    return "Alik'ra";

                case "Dragontail Mountains":
                    return "Dragontail";

                case "Wrothgarian Mountains":
                    return "Wrothgaria";

                case "Orsinium Area":
                    return "Orsinium";

                default:
                    break;
            }
            return name;
        }

        public static string GetArchitecturalStyle(MapsFile.Climates climate)
        {
            switch (climate)
            {
                case MapsFile.Climates.Desert:
                    return "B";
                case MapsFile.Climates.Subtropical:
                    return "G";
                default:
                    return "A";
            }
        }

        public static int GetTempleFactionId(int deityCodeProg)
        {
            switch (deityCodeProg)
            {
                case 0:
                    return 82;
                case 1:
                    return 84;
                case 2:
                    return 88;
                case 3:
                    return 92;
                case 4:
                    return 98;
                case 5:
                    return 94;
                case 6:
                    return 106;
                case 7:
                    return 36;
                default:
                    return -1;
            }
        }

        public static string GetDeityName(string locName)
        {
            string deityName = string.Empty;
            int counter = 0;

            do
            {
                if (locName.Contains(deityNames[counter]))
                    deityName = deityNames[counter];
                counter++;
            }
            while (deityName.Equals(string.Empty));

            return deityName;
        }

        public static bool CheckBlockExistance(string blockName)
        {
            return File.Exists(Path.Combine(testPath, "RMB", GetGenericBlockName(blockName, true)));
        }

        public static int ConvertRaceToProvince(Races race)
        {
            switch (race)
            {
                case Races.Breton:
                    return (int)ProvinceNames.HighRock;
                case Races.Redguard:
                    return (int)ProvinceNames.Hammerfell;
                case Races.Nord:
                    return (int)ProvinceNames.Skyrim;
                case Races.DarkElf:
                    return (int)ProvinceNames.Morrowind;
                case Races.HighElf:
                    return (int)ProvinceNames.Sumurset;
                case Races.WoodElf:
                    return (int)ProvinceNames.Valenwood;
                case Races.Khajiit:
                    return (int)ProvinceNames.Elsweyr;
                case Races.Argonian:
                    return (int)ProvinceNames.BlackMarsh;
                case Races.Imperial:
                    return (int)ProvinceNames.Cyrodiil;
                default:
                    return (int)ProvinceNames.Unused;
            }
        }

        public static int ConvertProvinceToRace(ProvinceNames province)
        {
            switch (province)
            {
                case ProvinceNames.HighRock:
                    return (int)Races.Breton;
                case ProvinceNames.Hammerfell:
                    return (int)Races.Redguard;
                case ProvinceNames.Skyrim:
                    return (int)Races.Nord;
                case ProvinceNames.Morrowind:
                    return (int)Races.DarkElf;
                case ProvinceNames.Cyrodiil:
                    return (int)Races.Imperial;
                case ProvinceNames.Sumurset:
                    return (int)Races.HighElf;
                case ProvinceNames.Valenwood:
                    return (int)Races.WoodElf;
                case ProvinceNames.Elsweyr:
                    return (int)Races.Khajiit;
                case ProvinceNames.BlackMarsh:
                    return (int)Races.Argonian;
                default:
                    return (int)Races.Undetermined;
            }
        }

        public static bool VerifyDungeonCorrectness(DFRegion.LocationTypes locType, DFRegion.DungeonTypes dType, DFLocation.DungeonBlock[] dBlocks)
        {
            int blockCount = 0;
            for (int i = 0; i < dBlocks.Length; i++)
            {
                if (!dBlocks[i].BlockName.StartsWith("B"))
                {
                    if (!dungeonTypeBlocks[(int)dType].Contains(dBlocks[i].BlockName.Substring(0, 8)))
                        return false;
                    blockCount++;
                }
            }

            switch (locType)
            {
                case DFRegion.LocationTypes.DungeonLabyrinth:
                    if (blockCount < 9)
                        return false;
                    break;
                case DFRegion.LocationTypes.DungeonKeep:
                    if (blockCount < 5 || blockCount > 8)
                        return false;
                    break;
                case DFRegion.LocationTypes.DungeonRuin:
                    if (blockCount > 4)
                        return false;
                    break;
                default:
                    Debug.Log("VerifyDungeonCorrectness() got a wrong location type!");
                    return true;
            }

            return true;
        }

        public static DFLocation.DungeonBlock[] GenerateDungeonInterior(int locationType, int dungeonType)
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

        public static (int, int)[] GenerateDungeonDiagram(int locationType, out bool[,] diagramProject)
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

                            diagram[i] = newSector;
                            diagramProject[newSector.Item1 + 2, newSector.Item2 + 2] = true;
                            positionFound = true;
                        }
                    }
                    while (!positionFound);
                }
            }
            return diagram;
        }

        public static DFLocation.DungeonBlock GetBlockData(string blockName, (int, int) position)
        {
            DFLocation.DungeonBlock block = new DFLocation.DungeonBlock();
            DFBlock rdBlock = new DFBlock();
            if (!blockName.EndsWith(".RDB.json"))
                blockName = blockName.Substring(0, 8) + ".RDB.json";
            string blockReplacementJson = File.ReadAllText(Path.Combine(testPath, "RDB", blockName));
            block = (DFLocation.DungeonBlock)SaveLoadManager.Deserialize(typeof(DFLocation.DungeonBlock), blockReplacementJson);
            // block = JsonConvert.DeserializeObject<DFLocation.DungeonBlock>(File.ReadAllText(Path.Combine(MapEditor.testPath, "RDB", string.Concat(blockName, ".json"))));
            rdBlock = (DFBlock)SaveLoadManager.Deserialize(typeof(DFBlock), blockReplacementJson);
            block.X = (sbyte)position.Item1;
            block.Z = (sbyte)position.Item2;
            block.IsStartingBlock = false;
            block.BlockName = blockName;
            block.WaterLevel = 10000;
            block.CastleBlock = false;

            return block;
        }

        public static bool CheckAdjacentBlocks(bool[,] dungeonDiagram, (int, int) position)
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

        public static int[] GetFreeSectors((int, int) coordinates, bool[,] diagramProject)
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

        public static float CalculateDistance((int, int) position1, (int, int) position2)
        {
            double absX = (double)(Math.Abs(position1.Item1 - position2.Item1));
            double absY = (double)(Math.Abs(position1.Item2 - position2.Item2));

            return (float)(Math.Sqrt((Math.Pow(absX, 2.0f) + Math.Pow(absY, 2.0f))));
        }
    }
}