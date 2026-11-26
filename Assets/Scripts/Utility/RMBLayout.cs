// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    Arneb
// 
// Notes:
//

using UnityEngine;
using System;
using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game;
using System.Linq;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Formulas;

namespace DaggerfallWorkshop.Utility
{
    /// <summary>
    /// Helper for laying out RMB (city block) data in scene.
    /// </summary>
    public static class RMBLayout
    {

        #region Fields

        public const int RMBTilesPerBlock = 16;
        public const int RMBTilesPerTerrain = 128;
        public const float RMBSide = 4096f * MeshReader.GlobalScale;
        public const float RMBTileSide = 256f * MeshReader.GlobalScale;
        public static int[][] sceneryType = new int[36][];

        const float propsOffsetY = -4f;
        const float blockFlatsOffsetY = -6f;
        const float natureFlatsOffsetY = -2f;
        public const uint CityGateOpenModelID = 446;
        public const uint CityGateClosedModelID = 447;
        public const uint BulletinBoardModelID = 41739;

#if !UNITY_EDITOR
        private static int maxLocationCacheSize = 12;
#endif
        private static List<KeyValuePair<ulong, DFBlock[]>> locationCache = new List<KeyValuePair<ulong, DFBlock[]>>();

        /// <summary>Clear the location cache. Use if block data is changed dynamically.</summary>
        public static void ClearLocationCache()
        {
            locationCache.Clear();
        }

        #endregion

        #region Structures

        class BuildingPoolItem
        {
            public DFLocation.BuildingData buildingData;
            public bool used;
        }

        #endregion

        #region Layout Methods

        /// <summary>
        /// Gets the model scale vector, converting zeros to 1s if needed.
        /// </summary>
        /// <param name="obj">RmbBlock3dObjectRecord structure</param>
        /// <returns>Vector3 with the scaling factors</returns>
        public static Vector3 GetModelScaleVector(DFBlock.RmbBlock3dObjectRecord obj)
        {
            return new Vector3(obj.XScale == 0 ? 1 : obj.XScale, obj.YScale == 0 ? 1 : obj.YScale, obj.ZScale == 0 ? 1 : obj.YScale);
        }

        /// <summary>
        /// Gets block data with validation.
        /// </summary>
        /// <param name="blockName">Block name.</param>
        /// <param name="blockDataOut">DFBlock data out.</param>
        /// <returns>True if validated and successful.</returns>
        public static bool GetBlockData(string blockName, out DFBlock blockDataOut)
        {
            blockDataOut = new DFBlock();

            // Validate
            if (string.IsNullOrEmpty(blockName))
                return false;
            if (!blockName.EndsWith(".RMB", StringComparison.InvariantCultureIgnoreCase))
                return false;
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
                return false;

            // Get block data
            blockDataOut = dfUnity.ContentReader.BlockFileReader.GetBlock(blockName);

            return true;
        }

        /// <summary>
        /// Create base RMB block by name.
        /// </summary>
        /// <param name="blockName">Name of block.</param>
        /// <param name="layoutX">X coordinate in parent map layout.</param>
        /// <param name="layoutY">Y coordinate in parent map layout.</param>
        /// <param name="cloneFrom">Prefab to clone from.</param>
        /// <returns>Block GameObject.</returns>
        public static GameObject CreateBaseGameObject(string blockName, int layoutX, int layoutY, DaggerfallRMBBlock cloneFrom = null)
        {
            DFBlock blockData;
            return CreateBaseGameObject(blockName, layoutX, layoutY, out blockData, cloneFrom);
        }

        /// <summary>
        /// Create base RMB block by name and get back DFBlock data.
        /// </summary>
        /// <param name="blockName">Name of block.</param>
        /// <param name="layoutX">X coordinate in parent map layout.</param>
        /// <param name="layoutY">Y coordinate in parent map layout.</param>
        /// <param name="blockDataOut">DFBlock data out.</param>
        /// <param name="cloneFrom">Prefab to clone from.</param>
        /// <returns>Block GameObject.</returns>
        public static GameObject CreateBaseGameObject(string blockName, int layoutX, int layoutY, out DFBlock blockDataOut, DaggerfallRMBBlock cloneFrom = null)
        {
            // Get block data
            if (!GetBlockData(blockName, out blockDataOut))
                return null;

            return CreateBaseGameObject(ref blockDataOut, layoutX, layoutY, cloneFrom);
        }

        /// <summary>
        /// Instantiate base RMB block by DFBlock data.
        /// </summary>
        /// <param name="blockData">Block data.</param>
        /// <param name="layoutX">X coordinate in parent map layout.</param>
        /// /// <param name="layoutY">Y coordinate in parent map layout.</param>
        /// <param name="cloneFrom">Prefab to clone from.</param>
        /// <returns>Block GameObject.</returns>
        public static GameObject CreateBaseGameObject(ref DFBlock blockData, int layoutX, int layoutY, DaggerfallRMBBlock cloneFrom = null)
        {
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
                return null;

            // Create gameobject
            GameObject go;
            string name = string.Format("DaggerfallBlock [{0}]", blockData.Name);
            if (cloneFrom != null)
                go = GameObjectHelper.InstantiatePrefab(cloneFrom.gameObject, name, null, Vector3.zero);
            else
                go = new GameObject(name);

            // Setup combiner
            ModelCombiner combiner = null;
            if (dfUnity.Option_CombineRMB)
                combiner = new ModelCombiner();

            // Lists to receive any doors found in this block
            List<StaticDoor> modelDoors;
            List<StaticDoor> propDoors;
            List<StaticBuilding> modelBuildings;

            // Add models and static props
            GameObject modelsNode = new GameObject("Models");
            modelsNode.transform.parent = go.transform;
            AddModels(dfUnity, layoutX, layoutY, ref blockData, out modelDoors, out modelBuildings, combiner, modelsNode.transform);
            AddProps(dfUnity, ref blockData, out propDoors, combiner, modelsNode.transform);

            // Combine list of doors found in models and props
            List<StaticDoor> allDoors = new List<StaticDoor>();
            if (modelDoors.Count > 0) allDoors.AddRange(modelDoors);
            if (propDoors.Count > 0) allDoors.AddRange(propDoors);

            // Assign building key to each door
            for (int i = 0; i < allDoors.Count; i++)
            {
                StaticDoor door = allDoors[i];
                door.buildingKey = BuildingDirectory.MakeBuildingKey((byte)layoutX, (byte)layoutY, (byte)door.recordIndex);
                allDoors[i] = door;
            }

            // Assign building key to each building
            for (int i = 0; i < modelBuildings.Count; i++)
            {
                StaticBuilding building = modelBuildings[i];
                building.buildingKey = BuildingDirectory.MakeBuildingKey((byte)layoutX, (byte)layoutY, (byte)building.recordIndex);
                modelBuildings[i] = building;
            }

            // Add static doors component
            if (allDoors.Count > 0)
                AddStaticDoors(allDoors.ToArray(), go);

            // Add static buildings component
            if (modelBuildings.Count > 0)
                AddStaticBuildings(modelBuildings.ToArray(), go);

            // Apply combiner
            if (combiner != null)
            {
                if (combiner.VertexCount > 0)
                {
                    combiner.Apply();
                    GameObjectHelper.CreateCombinedMeshGameObject(
                        combiner,
                        "CombinedModels",
                        modelsNode.transform,
                        dfUnity.Option_SetStaticFlags);
                }
            }

            return go;
        }

        public static void InitialiseSceneryType()
        {
            sceneryType[0] = new int[] { 1, 2, 5, 6, 7, 8, 9, 10, 11, 20, 21, 22, 23, 24, 26, 27, 29, 31 };     // Rainforest plants
            sceneryType[1] = new int[] { 4, 17, 19, 28 };                                                       // Rainforest stones
            sceneryType[2] = new int[] { 3, 12, 13, 14, 15, 16, 18, 25, 30 };                                   // Rainforest trees

            sceneryType[3] = new int[] { 1, 2, 7, 8, 9, 14, 18, 20, 21, 22, 25, 26, 27, 28, 29, 31 };           // Sub Tropical plants
            sceneryType[4] = new int[] { 3, 4, 5, 6, 10, 23 };                                                  // Sub Tropical stones
            sceneryType[5] = new int[] { 11, 12, 13, 15, 16, 17, 19, 24, 30 };                                  // Sub Tropical trees

            sceneryType[6] = new int[] { 1, 7, 8, 9, 11, 14, 20, 21, 22, 23, 26, 27, 28, 29, 31 };              // Swamp plants
            sceneryType[7] = new int[] { 2, 3, 4, 5, 6, 10 };                                                   // Swamp stones
            sceneryType[8] = new int[] { 12, 13, 15, 16, 17, 18, 19, 24, 25, 30 };                              // Swamp trees

            sceneryType[9] = new int[] { 1, 7, 8, 9, 10, 14, 15, 16, 17, 23, 24, 25, 26, 27, 29, 31 };          // Desert plants
            sceneryType[10] = new int[] { 2, 3, 4, 6, 18, 19, 20, 22 };                                         // Desert stones
            sceneryType[11] = new int[] { 5, 11, 12, 13, 28, 30 };                                              // Desert trees

            sceneryType[12] = new int[] { 1, 2, 7, 8, 9, 10, 21, 22, 23, 24, 26, 27, 28, 29, 31 };                     // Temperate Woodland plants
            sceneryType[13] = new int[] { 3, 4, 5, 6 };                                                         // Temperate Woodland stones
            sceneryType[14] = new int[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 25, 30 };                 // Temperate Woodland trees

            sceneryType[15] = new int[] { 1, 2, 7, 8, 9, 10, 21, 22, 23, 24, 26, 27, 28, 29 };                     // Snow Woodland plants
            sceneryType[16] = new int[] { 3, 4, 5, 6 };                                                         // Snow Woodland stones
            sceneryType[17] = new int[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 25, 30, 31 };                 // Snow Woodland trees

            sceneryType[18] = new int[] { 2, 7, 8, 9, 21, 22, 23, 26, 27, 29, 31 };                             // Woodland Hills plants
            sceneryType[19] = new int[] { 1, 3, 4, 6, 17, 18, 28 };                                             // Woodland Hills stones
            sceneryType[20] = new int[] { 5, 10, 11, 12, 13, 14, 15, 16, 19, 20, 24, 25, 30 };                  // Woodland Hills trees

            sceneryType[21] = new int[] { 7, 8, 9, 21, 22, 23, 26, 27, 29, 30, 31 };                            // Snow Woodland Hills plants
            sceneryType[22] = new int[] { 1, 3, 4, 6, 17, 18, 28 };                                             // Snow Woodland Hills stones
            sceneryType[23] = new int[] { 2, 5, 10, 11, 12, 13, 14, 15, 16, 19, 20, 24, 25 };                   // Snow Woodland Hills trees

            sceneryType[24] = new int[] { 2, 7, 8, 9, 11, 14, 21, 22, 23, 26, 27, 28, 29 };                     // Haunted Woodland plants
            sceneryType[25] = new int[] { 1, 3, 4, 5, 6, 10, 12, 17 };                                          // Haunted Woodland stones
            sceneryType[26] = new int[] { 13, 15, 16, 18, 19, 20, 24, 25, 30, 31 };                             // Haunted Woodland trees

            sceneryType[27] = new int[] { 2, 7, 8, 9, 11, 14, 21, 22, 23, 26, 27, 28, 29 };                     // Snow Haunted Woodland plants
            sceneryType[28] = new int[] { 1, 3, 4, 5, 6, 10, 12, 17 };                                          // Snow Haunted Woodland stones
            sceneryType[29] = new int[] { 13, 15, 16, 18, 19, 20, 24, 25, 30, 31 };                             // Snow Haunted Woodland trees

            sceneryType[30] = new int[] { 2, 7, 8, 9, 10, 21, 22, 23, 26, 29 };                                 // Mountain plants
            sceneryType[31] = new int[] { 1, 3, 4, 6, 14, 17, 18, 27, 28, 31 };                                 // Mountain stones
            sceneryType[32] = new int[] { 5, 11, 12, 13, 15, 16, 19, 20, 24, 25, 30 };                          // Mountain trees

            sceneryType[33] = new int[] { 7, 8, 9, 10, 21, 22, 23, 26, 29, 30 };                                // Snow Mountain plants
            sceneryType[34] = new int[] { 1, 3, 4, 6, 12, 14, 17, 18, 27, 28, 31 };                             // Snow Mountain stones
            sceneryType[35] = new int[] { 2, 5, 11, 13, 15, 16, 19, 20, 24, 25 };                               // Snow Mountain trees
        }

        /// <summary>
        /// Add nature billboards.
        /// </summary>
        public static void AddNatureFlats(
            ref DFBlock blockData,
            Transform flatsParent,
            ulong randSeed,
            DaggerfallBillboardBatch billboardBatch = null,
            ClimateNatureSets climateNature = ClimateNatureSets.TemperateWoodland,
            ClimateSeason climateSeason = ClimateSeason.Summer)
        {
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;

            if (!dfUnity.IsReady)
                return;
            bool randomiseStuff = int.TryParse(blockData.Name.Substring(3, 1), out int uselessValue);
            InitialiseSceneryType();

            UnityEngine.Random.InitState(Convert.ToInt32(randSeed));
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    // Get scenery item - ignore indices -1 (empty) and 0 (marker/waypoint of some kind)
                    if (blockData.RmbBlock.FldHeader.GroundData.GroundScenery[x, 15 - y].TextureRecord < 1)
                        continue;
                    DFBlock.RmbGroundScenery scenery;
                    scenery = randomiseStuff ? GetRandomScenery(billboardBatch.TextureArchive, billboardBatch.TextureArchive, blockData.RmbBlock.FldHeader.GroundData.GroundScenery[x, 15 - y].TextureRecord) : blockData.RmbBlock.FldHeader.GroundData.GroundScenery[x, 15 - y];

                    // Calculate position
                    Vector3 billboardPosition = new Vector3(
                        x * BlocksFile.TileDimension,
                        natureFlatsOffsetY,
                        y * BlocksFile.TileDimension + BlocksFile.TileDimension) * MeshReader.GlobalScale;

                    // Get Archive
                    int natureArchive = ClimateSwaps.GetNatureArchive(climateNature, climateSeason);

                    // Import custom 3d gameobject instead of flat
                    if (MeshReplacement.ImportCustomFlatGameobject(natureArchive, scenery.TextureRecord, billboardPosition, flatsParent) != null)
                        continue;

                    // Add billboard to batch or standalone
                    if (billboardBatch != null)
                    {
                        billboardBatch.AddItem(scenery.TextureRecord, billboardPosition);
                    }
                    else
                    {
                        GameObject go = GameObjectHelper.CreateDaggerfallBillboardGameObject(natureArchive, scenery.TextureRecord, flatsParent);
                        go.transform.position = billboardPosition;
                        AlignBillboardToBase(go);
                    }
                }
            }
        }

        public static DFBlock.RmbGroundScenery GetRandomScenery(int oldArchive, int archive, int texture)
        {
            int sceneryTypeIndex = GetScenerySubType(oldArchive, texture);
            // Debug.Log("Scenery Sub Type: " + sceneryTypeIndex + ", Scenery Type Index: " + ((archive - 500) * 3 + sceneryTypeIndex));
            DFBlock.RmbGroundScenery rmbGroundScenery = new DFBlock.RmbGroundScenery
            {
                TextureRecord = sceneryType[(archive - 500) * 3 + sceneryTypeIndex][UnityEngine.Random.Range(0, sceneryType[(archive - 500) * 3 + sceneryTypeIndex].Length)]
            };

            return rmbGroundScenery;
        }

        public static int GetScenerySubType(int archive, int record)
        { 
            int index = (archive - 500) * 3;

            for (int i = 0; i < 3; i++)
            {
                if (sceneryType[index + i].Contains(record))
                    return i;
            }

            Debug.Log("Scenery Type Index not found for archive " + archive + ", record " + record + ". Returning default index 0.");
            return 0;
        }

        /// <summary>
        /// Adds light flats and prefabs.
        /// </summary>
        public static void AddLights(
            ref DFBlock blockData,
            Transform flatsParent,
            Transform lightsParent,
            DaggerfallBillboardBatch billboardBatch = null)
        {
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
                return;

            // Do nothing if import option not enabled or missing prefab
            if (!dfUnity.Option_ImportLightPrefabs || dfUnity.Option_CityLightPrefab == null)
                return;

            // Iterate block flats for lights
            foreach (DFBlock.RmbBlockFlatObjectRecord obj in blockData.RmbBlock.MiscFlatObjectRecords)
            {
                // Add point lights
                if (obj.TextureArchive == TextureReader.LightsTextureArchive)
                {
                    // Calculate position
                    Vector3 billboardPosition = new Vector3(
                        obj.XPos,
                        -obj.YPos + blockFlatsOffsetY,
                        obj.ZPos + BlocksFile.RMBDimension) * MeshReader.GlobalScale;

                    // Import custom 3d gameobject instead of flat
                    if (MeshReplacement.ImportCustomFlatGameobject(obj.TextureArchive, obj.TextureRecord, billboardPosition, flatsParent) != null)
                        continue;

                    // Add billboard to batch or standalone
                    if (billboardBatch != null)
                    {
                        billboardBatch.AddItem(obj.TextureRecord, billboardPosition);
                    }
                    else
                    {
                        GameObject go = GameObjectHelper.CreateDaggerfallBillboardGameObject(obj.TextureArchive, obj.TextureRecord, flatsParent);
                        go.transform.position = billboardPosition;
                        AlignBillboardToBase(go);
                    }

                    // Import light prefab
                    AddLight(dfUnity, obj, lightsParent);
                }
            }
        }

        /// <summary>
        /// Add misc block flats.
        /// Batching is conditionally supported.
        /// </summary>
        public static void AddMiscBlockFlats(
            ref DFBlock blockData,
            Transform flatsParent,
            ulong mapId,
            int randSeed,
            int locationIndex,
            DaggerfallBillboardBatch animalsBillboardBatch = null,
            TextureAtlasBuilder miscBillboardsAtlas = null,
            DaggerfallBillboardBatch miscBillboardsBatch = null)
        {
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
                return;

            bool randomiseStuff = int.TryParse(blockData.Name.Substring(3, 1), out int uselessValue);
            UnityEngine.Random.InitState(Convert.ToInt32(randSeed));
            int counter = 0;
            DFBlock.RmbBlockFlatObjectRecord[] swapObj = new DFBlock.RmbBlockFlatObjectRecord[blockData.RmbBlock.MiscFlatObjectRecords.Length];

            // Add block flats
            foreach (DFBlock.RmbBlockFlatObjectRecord obj in blockData.RmbBlock.MiscFlatObjectRecords)
            {
                // Ignore lights as they are handled by AddLights()
                if (obj.TextureArchive == TextureReader.LightsTextureArchive)
                    continue;

                (int, int, int, int) flatCategories = GetBillboardParameters(obj.TextureArchive, obj.TextureRecord);
                swapObj[counter] = ApplyFlags(obj, mapId, locationIndex, flatCategories, randomiseStuff, out bool isPresent);
                if (!isPresent) continue;

                // Calculate position
                Vector3 billboardPosition = new Vector3(
                    swapObj[counter].XPos,
                    -swapObj[counter].YPos + blockFlatsOffsetY,
                    swapObj[counter].ZPos + BlocksFile.RMBDimension) * MeshReader.GlobalScale;
                
                GameObject go = MeshReplacement.ImportCustomFlatGameobject(swapObj[counter].TextureArchive, swapObj[counter].TextureRecord, billboardPosition, flatsParent);
                if (go == null)
                {
                    // Add standalone billboard gameobject
                    go = GameObjectHelper.CreateDaggerfallBillboardGameObject(swapObj[counter].TextureArchive, swapObj[counter].TextureRecord, flatsParent);
                    go.transform.position = billboardPosition;
                    AlignBillboardToBase(go);
                }

                // Add animal sound
                if (swapObj[counter].TextureArchive == TextureReader.AnimalsTextureArchive)
                    GameObjectHelper.AddAnimalAudioSource(go, swapObj[counter].TextureRecord);

                // If flat record has a non-zero faction id, then it's an exterior NPC
                if (swapObj[counter].FactionID != 0)
                {
                    // Add RMB data to billboard
                    Billboard dfBillboard = go.GetComponent<Billboard>();
                    if (dfBillboard != null)
                        dfBillboard.SetRMBPeopleData(swapObj[counter].FactionID, swapObj[counter].Flags, swapObj[counter].Position);

                    // Add StaticNPC behaviour
                    StaticNPC npc = go.AddComponent<StaticNPC>();
                    npc.SetLayoutData(swapObj[counter], mapId, locationIndex);

                    QuestMachine.Instance.SetupIndividualStaticNPC(go, swapObj[counter].FactionID);
                }
                counter++;
            }
            blockData.RmbBlock.MiscFlatObjectRecords = swapObj;
        }

        public static (int, int, int, int) GetBillboardParameters(int archive, int record)
        {
            if (archive == 199) return (-1, -1, -1, -1);
            int key = archive * 10000 + record * 10;
            Debug.Log("key: " + key);
            int value = GameObjectHelper.billboardDict[key];
            int item4 = value % 100;                
            int item3 = value / 100 % 100;
            int item2 = value / 10000 % 100;
            int item1 = value / 1000000 % 100;
            return (item1, item2, item3, item4);
        }

        public static DFBlock.RmbBlockFlatObjectRecord ApplyFlags(DFBlock.RmbBlockFlatObjectRecord flat, ulong mapId, int locationIndex, (int, int, int, int) flatParameters, bool randomiseStuff, out bool isPresent)
        {
            int regionIndex = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            isPresent = true;
            switch (flatParameters.Item1)
            {
                case 0:     // People
                    return ApplyFlagsToPeople(flat, flatParameters, regionIndex, mapId, locationIndex, randomiseStuff, false, out isPresent);
                case 1:     // Interior
                    return ApplyFlagsToInterior(flat, flatParameters, regionIndex, mapId, randomiseStuff, false, out isPresent);
                case 2:     // Fauna
                    return ApplyFlagsToFauna(flat, flatParameters, regionIndex, mapId, randomiseStuff, false, out isPresent);
                case 3:     // Misc exterior stuff
                    return ApplyFlagsToExterior(flat, flatParameters, regionIndex, mapId, randomiseStuff, out isPresent);
                case 4:     // Plants and other stuff
                    switch (flatParameters.Item2)
                    {
                        case 0:
                            return ApplyFlagsToCrops(flat, flatParameters, regionIndex, mapId, randomiseStuff, out isPresent);
                        case 1:
                        case 2:
                        case 3:
                            return ApplyFlagsToFlora(flat, flatParameters, regionIndex, mapId, randomiseStuff, false, out isPresent);
                        case 4:
                        case 5:
                            // Seasonal crops and trees - to implement
                            return flat;
                    }
                    break;
            }
            return flat;
        }

        public static DFBlock.RmbBlockFlatObjectRecord ConvertTupleBillboardToFlatObject(DFBlock.RmbBlockFlatObjectRecord flat, (int, int) billboard)
        {
            flat.TextureArchive = billboard.Item1;
            flat.TextureRecord = billboard.Item2;
            return flat;
        }

        public static DFBlock.RmbBlockFlatObjectRecord ApplyFlagsToPeople(DFBlock.RmbBlockFlatObjectRecord flat, (int, int, int, int) flatParameters, int regionIndex, ulong mapId, int locationIndex, bool randomiseStuff, bool isInterior, out bool isPresent)
        {
            ProcessFlagValues(
                flat.Flags, 
                out bool randomNPC,
                out bool daytimePres,
                out bool nighttimePres,
                out bool rainPres,
                out bool isFemale,
                out bool randomPres,
                out bool randomRace
            );
            
            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            if (!CheckIfPresent(daytimePres, nighttimePres, rainPres, false, randomPres, false, regionIndex, mapId, isInterior))
            {
                Debug.Log("People " + flat.TextureArchive + "." + flat.TextureRecord + " with flag " + flat.Flags + " isn't present.");
                isPresent = false;
                return ConvertTupleBillboardToFlatObject(flat, (0, 0));
            }
            isPresent = true;
            if (randomPres)
            {
                randomiseStuff = true;
                randomNPC = randomRace = true;
                isFemale = UnityEngine.Random.Range(0, 2) == 0 ? false : true;
            }
            UnityEngine.Random.state = oldState;

            if (!randomNPC || !randomiseStuff)
                return flat;

            int races = WorldData.WorldSetting.regionRaces[PoliticData.ConvertMapIdToRegionIndex(mapId)];
            if (randomRace && randomiseStuff)            
            {                
                int selectedRace = ConvertWorldDataRaceToFlatRace(GetNPCRace(races, PoliticData.GetLocTypeFromMapId(mapId, locationIndex, out byte townAttributes), townAttributes));
                // Debug.Log("flatParameters.Item1: " + flatParameters.Item1 + ", flatParameters.Item2: " + flatParameters.Item2 + "Selected race: " + selectedRace + "original race value: " + races);
                if (GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][selectedRace].Length > 0)
                    return ConvertTupleBillboardToFlatObject(flat, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][selectedRace][UnityEngine.Random.Range(0, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][selectedRace].Length)]);
            }

            if (GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][flatParameters.Item3].Length > 0)
                return ConvertTupleBillboardToFlatObject(flat, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][flatParameters.Item3][UnityEngine.Random.Range(0, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][flatParameters.Item3].Length)]);
            else return flat;
        }

        public static int ConvertWorldDataRaceToFlatRace(int worldDataRace)
        {
            switch (worldDataRace)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                default:
                    return worldDataRace - 1;                
            }
        }

        // Apply flag values to interior items
        public static DFBlock.RmbBlockFlatObjectRecord ApplyFlagsToInterior(DFBlock.RmbBlockFlatObjectRecord flat, (int, int, int, int) flatParameters, int regionIndex, ulong mapId, bool randomiseStuff, bool isInterior, out bool isPresent)
        {
            Debug.Log("Applying flags to Interior...");
            ProcessFlagValues(
                flat.Flags, 
                out bool randomItem,
                out bool daytimePres,
                out bool nighttimePres,
                out bool rainPres,
                out bool holidayOnly,
                out bool randomPres,
                out bool uncommonFreq
            );

            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            if (!CheckIfPresent(daytimePres, nighttimePres, rainPres, holidayOnly, randomPres, uncommonFreq, regionIndex, mapId, isInterior))
            {
                isPresent = false;
                return ConvertTupleBillboardToFlatObject(flat, (0, 0));
            }
            isPresent = true;
            UnityEngine.Random.state = oldState;

            if (!randomItem || !randomiseStuff) return flat;            
            return ConvertTupleBillboardToFlatObject(flat, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][flatParameters.Item3][UnityEngine.Random.Range(0, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][flatParameters.Item3].Length)]);
        }

        public static DFBlock.RmbBlockFlatObjectRecord ApplyFlagsToFauna(DFBlock.RmbBlockFlatObjectRecord flat, (int, int, int, int) flatParameters, int regionIndex, ulong mapId, bool randomiseStuff, bool isInterior, out bool isPresent)
        {
            Debug.Log("Applying flags to Fauna...");
            ProcessFlagValues(
                flat.Flags, 
                out bool randomFauna,
                out bool daytimePres,
                out bool nighttimePres,
                out bool rainPres,
                out bool randomType,
                out bool randomPres,
                out bool uselessValue
            );
            
            int region = flatParameters.Item2;
            int faunaType = flatParameters.Item3;

            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            if (!CheckIfPresent(daytimePres, nighttimePres, rainPres, false, randomPres, false, regionIndex, mapId, isInterior))
            {
                Debug.Log("Fauna " + flat.TextureArchive + "." + flat.TextureRecord + " with flag " + flat.Flags + " isn't present.");
                isPresent = false;
                return ConvertTupleBillboardToFlatObject(flat, (0, 0));
            }
            isPresent = true;
            UnityEngine.Random.state = oldState;

            if (!randomFauna || !randomiseStuff)
                return flat;

            FlatCategories.IndexStartType startType = GetFaunaType(flatParameters.Item2, flatParameters.Item3, out bool petAndLiveStock);
            if (randomType)
            {
                if (startType != FlatCategories.IndexStartType.NotToRandomise)
                {
                    region = GetRegionFaunaType(regionIndex, out bool peculiarRegion);
                    faunaType = PickFaunaType(region, startType, petAndLiveStock, peculiarRegion);
                }
            }
            // Debug.Log("randomType: " + randomType + ", faunaType: " + faunaType + ", flatParameters: " + flatParameters);
            // if (startType == FlatCategories.IndexStartType.Livestock)
            // {
            //     UnityEngine.Random.InitState(Convert.ToInt32(mapId + (ulong)flat.TextureRecord));
            //     return ConvertTupleBillboardToFlatObject(flat, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][randomType][UnityEngine.Random.Range(0, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][randomType].Length)]);
            // }
            return ConvertTupleBillboardToFlatObject(flat, GameObjectHelper.billboardType[flatParameters.Item1][region][faunaType][UnityEngine.Random.Range(0, GameObjectHelper.billboardType[flatParameters.Item1][region][faunaType].Length)]);
        }

        public static void ProcessFlagValues(byte flag, out bool byte0, out bool byte1, out bool byte2, out bool byte3, out bool byte4, out bool byte5, out bool byte6)
        {
            byte0 = flag % 2 > 0;
            byte1 = (flag >> 1) % 2 > 0;
            byte2 = (flag >> 2) % 2 > 0;
            byte3 = (flag >> 3) % 2 > 0;
            byte4 = (flag >> 4) % 2 > 0;
            byte5 = (flag >> 5) % 2 > 0;
            byte6 = (flag >> 6) % 2 > 0;
        }

        public static bool CheckIfPresent(bool daytimePres, bool nighttimePres, bool rainPres, bool holidayOnly, bool randomPres, bool uncommonFreq, int regionIndex, ulong mapId, bool isInterior = false)
        {
            DaggerfallDateTime dateTime = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime;
            WeatherManager weather = GameManager.Instance.WeatherManager;

            if (isInterior)
            {
                if (!daytimePres && (dateTime.MinuteOfDay >= dateTime.ActivityStart - 30  &&
                                     dateTime.MinuteOfDay < dateTime.ActivityEnd + 30))
                    return false;
                if (!nighttimePres && (dateTime.MinuteOfDay < dateTime.ActivityStart - 30 ||
                                      dateTime.MinuteOfDay >= dateTime.ActivityEnd + 30))
                    return false;
            }
            if (!isInterior)
            {
                if (!daytimePres && dateTime.SettlementIsActive) return false;
                if (!nighttimePres && !dateTime.SettlementIsActive) return false;
            }
            
            if (!rainPres && weather.IsRaining) return false;
            if (holidayOnly && FormulaHelper.GetHolidayId(dateTime.ToClassicDaggerfallTime(), regionIndex) == 0) return false;

            UnityEngine.Random.InitState(dateTime.DayOfYear + (int)mapId);
            if (randomPres)
            {
                int randomChance = UnityEngine.Random.Range(1, 101);
                if ((uncommonFreq && randomChance > 50) || (!uncommonFreq && randomChance > 75)) return false;
            }
            return true;
        }

        public static int GetRegionFaunaType(int regionIndex, out bool peculiarRegion)
        {
            peculiarRegion = FlatCategories.regionsWithPeculiarStuff.Contains(regionIndex);
            for (int i = 0; i < Enum.GetNames(typeof(ProvinceNames)).Length; i++)
            {
                if (WorldData.WorldSetting.regionInProvince[i].Contains(regionIndex))
                    return i - 1;
            }
            return -1;            
        }

        // Get the type of fauna (pet, livestock, wild or "not to randomise")
        public static FlatCategories.IndexStartType GetFaunaType(int region, int faunaType, out bool isLivestockToo)
        {
            FlatCategories.IndexStartType startType = FlatCategories.IndexStartType.NotToRandomise;
            isLivestockToo = false;
            for (int i = 0; i < Enum.GetNames(typeof(FlatCategories.IndexStartType)).Length; i++)
            {
                if (GameObjectHelper.provincialStartEnd[region][i].Item1 <= faunaType &&
                    GameObjectHelper.provincialStartEnd[region][i].Item2 >= faunaType)
                {
                    startType = (FlatCategories.IndexStartType)i;
                    if (startType == FlatCategories.IndexStartType.Pet)
                    {
                        if (GameObjectHelper.provincialStartEnd[region][i + 1].Item1 <= faunaType &&
                            GameObjectHelper.provincialStartEnd[region][i + 1].Item2 >= faunaType)
                            isLivestockToo = true;
                    }
                    break;
                }
            }
            return startType;
        }

        // Randomise or pick the fauna type to utilise based on fauna categories
        public static int PickFaunaType(int region, FlatCategories.IndexStartType faunaType, bool petAndLiveStock = false, bool peculiarRegion = false)
        {
            bool isPickable = peculiarRegion && GameObjectHelper.regionalStartEnd[region][(int)faunaType].Item1 > -1;
            int randomIndex;
            int rangeIncrease = 0;
            if (isPickable)
                rangeIncrease = GameObjectHelper.regionalStartEnd[region][(int)faunaType].Item2 - GameObjectHelper.regionalStartEnd[region][(int)faunaType].Item1 + 1;
            switch (faunaType)
            {
                case FlatCategories.IndexStartType.Pet:
                    if (petAndLiveStock)
                    {
                        if (isPickable)
                        {
                            rangeIncrease = GameObjectHelper.regionalStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item2 - GameObjectHelper.regionalStartEnd[region][(int)FlatCategories.IndexStartType.Livestock].Item1 + 1;
                            randomIndex = UnityEngine.Random.Range(GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Livestock].Item1, GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item2 + rangeIncrease);
                            if (randomIndex <= GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item2)
                                return randomIndex;
                            else
                                return randomIndex - GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item2 + (GameObjectHelper.regionalStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item2 - 1);
                        }
                        else return UnityEngine.Random.Range(GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Livestock].Item1, GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item2 + 1);
                    }
                    else if (isPickable)
                    {
                        randomIndex = UnityEngine.Random.Range(GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item1, GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item2 + rangeIncrease);
                        if (randomIndex <= GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item2)
                            return randomIndex;
                        else return randomIndex - GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item2 + (GameObjectHelper.regionalStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item2 - 1);
                    }
                    else return UnityEngine.Random.Range(GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item1, GameObjectHelper.provincialStartEnd[region][(int)FlatCategories.IndexStartType.Pet].Item2 + 1);
                default:
                    if (isPickable)
                    {
                        randomIndex = UnityEngine.Random.Range(GameObjectHelper.provincialStartEnd[region][(int)faunaType].Item1, GameObjectHelper.provincialStartEnd[region][(int)faunaType].Item2 + rangeIncrease);
                        if (randomIndex <= GameObjectHelper.provincialStartEnd[region][(int)faunaType].Item2)
                            return randomIndex;
                        else return randomIndex - GameObjectHelper.provincialStartEnd[region][(int)faunaType].Item2 + (GameObjectHelper.regionalStartEnd[region][(int)faunaType].Item2 - 1);
                    }
                    else return UnityEngine.Random.Range(GameObjectHelper.provincialStartEnd[region][(int)faunaType].Item1, GameObjectHelper.provincialStartEnd[region][(int)faunaType].Item2 + 1);
            }
        }

        public static DFBlock.RmbBlockFlatObjectRecord ApplyFlagsToExterior(DFBlock.RmbBlockFlatObjectRecord flat, (int, int, int, int) flatParameters, int regionIndex, ulong mapId, bool randomiseStuff, out bool isPresent)
        {
            Debug.Log("Applying flags to Exterior...");
            ProcessFlagValues(
                flat.Flags, 
                out bool randomExterior,
                out bool daytimePres,
                out bool nighttimePres,
                out bool rainPres,
                out bool holidayOnly,
                out bool randomPres,
                out bool uncommonFreq
            );

            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            if (!CheckIfPresent(daytimePres, nighttimePres, rainPres, holidayOnly, randomPres, uncommonFreq, regionIndex, mapId))
            {
                isPresent = false;
                return ConvertTupleBillboardToFlatObject(flat, (0, 0));
            }
            isPresent = true;
            UnityEngine.Random.state = oldState;

            if (!randomExterior || randomiseStuff) return flat;
            return ConvertTupleBillboardToFlatObject(flat, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][flatParameters.Item3][UnityEngine.Random.Range(0, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][flatParameters.Item3].Length)]);
        }

        public static DFBlock.RmbBlockFlatObjectRecord ApplyFlagsToCrops(DFBlock.RmbBlockFlatObjectRecord flat, (int, int, int, int) flatParameters, int regionIndex, ulong mapId, bool randomiseStuff, out bool isPresent)
        {
            Debug.Log("Applying flags to Crops...");
            ProcessFlagValues(
                flat.Flags, 
                out bool randomCrop,
                out bool daytimePres,
                out bool nighttimePres,
                out bool rainPres,
                out bool uselessValue0,
                out bool randomPres,
                out bool uselessValue1
            );

            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            if (!CheckIfPresent(daytimePres, nighttimePres, rainPres, false, randomPres, false, regionIndex, mapId))
            {
                isPresent = false;
                return ConvertTupleBillboardToFlatObject(flat, (0, 0));
            }
            isPresent = true;
            UnityEngine.Random.state = oldState;
            
            if (!randomCrop || !randomiseStuff)
            {
                return ConvertTupleBillboardToFlatObject(flat, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][flatParameters.Item3][UnityEngine.Random.Range(0, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][flatParameters.Item3].Length)]);
            }

            // Re-initialising the seed should make all the crops of the same type to change to different types, but still being homogeneous. Test required.
            UnityEngine.Random.InitState(Convert.ToInt32(mapId + (ulong)flat.TextureRecord));
            int randomType = UnityEngine.Random.Range(0, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2].Length);
            return ConvertTupleBillboardToFlatObject(flat, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][randomType][UnityEngine.Random.Range(0, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][randomType].Length)]);
        }

        public static DFBlock.RmbBlockFlatObjectRecord ApplyFlagsToFlora(DFBlock.RmbBlockFlatObjectRecord flat, (int, int, int, int) flatParameters, int regionIndex, ulong mapId, bool randomiseStuff, bool isInterior, out bool isPresent)
        {
            Debug.Log("Applying flags to Crops...");
            ProcessFlagValues(
                flat.Flags, 
                out bool randomFlora,
                out bool daytimePres,
                out bool nighttimePres,
                out bool rainPres,
                out bool randomType,
                out bool randomPres,
                out bool uselessValue0
            );
            
            int floraType = flatParameters.Item2;

            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            if (!CheckIfPresent(daytimePres, nighttimePres, rainPres, false, randomPres, false, regionIndex, mapId, isInterior))
            {
                isPresent = false;
                return ConvertTupleBillboardToFlatObject(flat, (0, 0));
            }
            isPresent = true;
            UnityEngine.Random.state = oldState;

            if (!randomFlora || !randomiseStuff) return flat;
            
            if (randomType)
                floraType = UnityEngine.Random.Range(1, 4);

            return ConvertTupleBillboardToFlatObject(flat, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][floraType][UnityEngine.Random.Range(0, GameObjectHelper.billboardType[flatParameters.Item1][flatParameters.Item2][floraType].Length)]);
        }

        public static int GetNPCRace(int races, DFRegion.LocationTypes settlement, byte townAttributes)
        {
            int diceRoll = UnityEngine.Random.Range(0, 100);
            if (townAttributes % 2 > 0) diceRoll += 10;        // Port town
            if ((townAttributes >> 1) % 2 > 0) diceRoll += 10; // Capital

            switch (settlement)
            {
                case DFRegion.LocationTypes.TownCity:
                    if (diceRoll < 50)
                        return races / 100;
                    else if (diceRoll < 75)
                        return races / 10 % 10;
                    else if (diceRoll < 90)
                        return races % 10;
                    break;

                case DFRegion.LocationTypes.TownHamlet:
                    if (diceRoll < 75)
                        return races / 100;
                    else if (diceRoll < 90)
                        return races / 10 % 10;
                    else if (diceRoll < 98)
                        return races % 10;
                    break;

                default:
                    if (diceRoll < 90)
                        return races / 100;
                    else if (diceRoll < 96)
                        return races / 10 % 10;
                    else if (diceRoll < 99)
                        return races % 10;
                    break;
            }
            return UnityEngine.Random.Range(1, 11);
        }

        /// <summary>
        /// Add subrecord (building) exterior block flats.
        /// </summary>
        public static void AddExteriorBlockFlats(
            ref DFBlock blockData,
            Transform flatsParent,
            Transform lightsParent,
            ulong mapId,
            int locationIndex,
            ClimateNatureSets climateNature = ClimateNatureSets.TemperateWoodland,
            ClimateSeason climateSeason = ClimateSeason.Summer)
        {
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
                return;

            // Get Nature Archive
            int natureArchive = ClimateSwaps.GetNatureArchive(climateNature, climateSeason);

            foreach (DFBlock.RmbSubRecord subRecord in blockData.RmbBlock.SubRecords)
            {
                Vector3 subRecordPosition = new Vector3(subRecord.XPos, 0, -subRecord.ZPos) * MeshReader.GlobalScale;

                foreach (DFBlock.RmbBlockFlatObjectRecord obj in subRecord.Exterior.BlockFlatObjectRecords)
                {
                    // Don't add building exterior editor flats since they can't be used by any DFU systems
                    int archive = obj.TextureArchive;
                    if (archive == TextureReader.EditorFlatsTextureArchive)
                        continue;

                    // Calculate position
                    Vector3 billboardPosition = new Vector3(
                        obj.XPos,
                        -obj.YPos + blockFlatsOffsetY,
                        obj.ZPos + BlocksFile.RMBDimension) * MeshReader.GlobalScale;

                    billboardPosition += subRecordPosition;

                    // Add natures using correct climate set archive
                    if (archive >= (int)DFLocation.ClimateTextureSet.Nature_RainForest && archive <= (int)DFLocation.ClimateTextureSet.Nature_Maquis)
                    {
                        archive = natureArchive;
                        billboardPosition.z = natureFlatsOffsetY;
                    }

                    GameObject go = MeshReplacement.ImportCustomFlatGameobject(archive, obj.TextureRecord, billboardPosition, flatsParent);
                    bool isImported = go != null;
                    if (!isImported)
                    {
                        // Add standalone billboard gameobject
                        go = GameObjectHelper.CreateDaggerfallBillboardGameObject(archive, obj.TextureRecord, flatsParent);
                        go.transform.position = billboardPosition;
                        AlignBillboardToBase(go);
                    }

                    // Add animal sound
                    if (archive == TextureReader.AnimalsTextureArchive)
                        GameObjectHelper.AddAnimalAudioSource(go, obj.TextureRecord);

                    // If flat record has a non-zero faction id, then it's an exterior NPC
                    if (obj.FactionID != 0)
                    {
                        // Add RMB data to billboard
                        Billboard dfBillboard = go.GetComponent<Billboard>();
                        if (dfBillboard != null)
                            dfBillboard.SetRMBPeopleData(obj.FactionID, obj.Flags, obj.Position);

                        // Add StaticNPC behaviour
                        StaticNPC npc = go.AddComponent<StaticNPC>();
                        npc.SetLayoutData(obj, mapId, locationIndex);

                        QuestMachine.Instance.SetupIndividualStaticNPC(go, obj.FactionID);
                    }

                    // If this is a light flat, import light prefab
                    if (archive == TextureReader.LightsTextureArchive && !isImported)
                    {
                        if (dfUnity.Option_CityLightPrefab == null)
                            return;

                        Vector2 size = dfUnity.MeshReader.GetScaledBillboardSize(210, obj.TextureRecord);
                        Vector3 position = new Vector3(
                            obj.XPos,
                            -obj.YPos + size.y,
                            obj.ZPos + BlocksFile.RMBDimension) * MeshReader.GlobalScale;
                        position += subRecordPosition;

                        GameObjectHelper.InstantiatePrefab(dfUnity.Option_CityLightPrefab.gameObject, string.Empty, lightsParent, position);
                    }
                }
            }
        }

        /// <summary>
        /// Aligns billboard GameObject to centre of base.
        /// This is required for exterior billboard.
        /// </summary>
        /// <param name="go">GameObject with DaggerfallBillboard component.</param>
        public static void AlignBillboardToBase(GameObject go)
        {
            Billboard c = go.GetComponent<Billboard>();
            if (c)
            {
                c.AlignToBase();
            }
        }

        /// <summary>
        /// Add simple ground plane to block layout.
        /// </summary>
        public static GameObject AddGroundPlane(
            ref DFBlock blockData,
            Transform parent = null,
            ClimateBases climateBase = ClimateBases.Temperate,
            ClimateSeason climateSeason = ClimateSeason.Summer)
        {
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
                return null;

            GameObject go = new GameObject("Ground");
            if (parent != null)
                go.transform.parent = parent;

            // Assign components
            DaggerfallGroundPlane dfGround = go.AddComponent<DaggerfallGroundPlane>();
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();

            // Assign climate and mesh
            Color32[] tileMap;
            Mesh mesh = dfUnity.MeshReader.GetSimpleGroundPlaneMesh(
                ref blockData,
                out tileMap,
                dfUnity.MeshReader.AddMeshTangents,
                dfUnity.MeshReader.AddMeshLightmapUVs);
            if (mesh)
            {
                meshFilter.sharedMesh = mesh;
            }

            // Assign tileMap and climate
            dfGround.tileMap = tileMap;
            dfGround.SetClimate(dfUnity, climateBase, climateSeason);

            // Assign collider
            if (dfUnity.Option_AddMeshColliders)
                go.AddComponent<BoxCollider>();

            // Assign static
            if (dfUnity.Option_SetStaticFlags)
                GameObjectHelper.TagStaticGeometry(go);

            return go;
        }

        #endregion

        #region Building Methods

        /// <summary>
        /// Gets BuildingSummary array generated from DFBlock data.
        /// DFBlock data should be provided from RMBLayout.GetLocationBuildingData() output.
        /// Otherwise not all building data will be present.
        /// </summary>
        /// <param name="blockData">DFBlock data.</param>
        /// <param name="layoutX">X coordindate in map layout used to generate building key.</param>
        /// <param name="layoutY">Y coordindate in map layout used to generate building key.</param>
        /// <returns>BuildingSummary.</returns>
        public static BuildingSummary[] GetBuildingData(in DFBlock blockData, int layoutX = -1, int layoutY = -1)
        {
            // Store building information
            int buildingCount = blockData.RmbBlock.SubRecords.Length;
            BuildingSummary[] buildings = new BuildingSummary[buildingCount];
            for (int i = 0; i < buildingCount; i++)
            {
                // Create building summary
                buildings[i] = new BuildingSummary();

                // Set building data
                ref readonly DFLocation.BuildingData buildingData = ref blockData.RmbBlock.FldHeader.BuildingDataList[i];
                buildings[i].buildingKey = BuildingDirectory.MakeBuildingKey((byte)layoutX, (byte)layoutY, (byte)i);
                buildings[i].NameSeed = buildingData.NameSeed;
                buildings[i].FactionId = buildingData.FactionId;
                buildings[i].BuildingType = buildingData.BuildingType;
                buildings[i].Quality = buildingData.Quality;

                // Set building transform info
                ref readonly DFBlock.RmbSubRecord subRecord = ref blockData.RmbBlock.SubRecords[i];
                buildings[i].Position = new Vector3(subRecord.XPos, 0, BlocksFile.RMBDimension - subRecord.ZPos) * MeshReader.GlobalScale;
                buildings[i].Rotation = new Vector3(0, -subRecord.YRotation / BlocksFile.RotationDivisor, 0);
                buildings[i].Matrix = Matrix4x4.TRS(buildings[i].Position, Quaternion.Euler(buildings[i].Rotation), Vector3.one);

                // First model of record is building model
                if (subRecord.Exterior.Block3dObjectRecords.Length > 0)
                {
                    buildings[i].ModelID = subRecord.Exterior.Block3dObjectRecords[0].ModelIdNum;
                }
            }

            return buildings;
        }

        /// <summary>
        /// Gets all RMB blocks from a location populated with building data from MAPS.BSA.
        /// This method is using "best effort" process at this point in time.
        /// However, it does yield very accurate results most of the time.
        /// Please use exception handling when calling this method for now.
        /// It will be progressed over time.
        /// </summary>
        /// <param name="location">Location to use.</param>
        public static DFBlock[] GetLocationBuildingData(in DFLocation location)
        {
            ulong mapId = location.MapTableData.MapId;
            DFBlock[] blocksArray = locationCache.FirstOrDefault(l => l.Key == mapId).Value;
            if (blocksArray != null)
                return blocksArray;

            List<BuildingPoolItem> namedBuildingPool = new List<BuildingPoolItem>();
            List<DFBlock> blocks = new List<DFBlock>();

            // Get content reader
            ContentReader contentReader = DaggerfallUnity.Instance.ContentReader;
            if (contentReader == null)
                throw new Exception("GetCompleteBuildingData() could not find ContentReader.");

            // Store named buildings in pool for distribution
            for (int i = 0; i < location.Exterior.BuildingCount; i++)
            {
                DFLocation.BuildingData building = location.Exterior.Buildings[i];
                if (IsNamedBuilding(building.BuildingType))
                {
                    BuildingPoolItem bpi = new BuildingPoolItem();
                    bpi.buildingData = building;
                    bpi.used = false;
                    namedBuildingPool.Add(bpi);
                }
            }

            // Get building summary of all blocks in this location
            int width = location.Exterior.ExteriorData.Width;
            int height = location.Exterior.ExteriorData.Height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Get block name
                    string blockName = contentReader.BlockFileReader.CheckName(WorldMaps.GetRmbBlockName(location, x, y));

                    // Get block data
                    DFBlock block;
                    if (!contentReader.BlockFileReader.GetBlock(blockName, out block))
                        throw new Exception("GetCompleteBuildingData() could not read block " + blockName);

                    // Make a copy of the building data array for our block copy since we're modifying it
                    DFLocation.BuildingData[] buildingArray = new DFLocation.BuildingData[block.RmbBlock.FldHeader.BuildingDataList.Length];
                    Array.Copy(block.RmbBlock.FldHeader.BuildingDataList, buildingArray, block.RmbBlock.FldHeader.BuildingDataList.Length);
                    block.RmbBlock.FldHeader.BuildingDataList = buildingArray;

                    // Assign building data for this block
                    BuildingReplacementData buildingReplacementData;
                    for (int i = 0; i < block.RmbBlock.SubRecords.Length; i++)
                    {
                        ref DFLocation.BuildingData building = ref block.RmbBlock.FldHeader.BuildingDataList[i];
                        if (IsNamedBuilding(building.BuildingType))
                        {
                            // Try to find next building and merge data
                            BuildingPoolItem item;
                            if (!GetNextBuildingFromPool(namedBuildingPool, building.BuildingType, out item))
                            {
                                Debug.LogFormat("End of city building list reached without finding building type {0} in location {1}.{2}", building.BuildingType, location.RegionName, location.Name);
                            }

                            // Copy found city building data to block level
                            building.NameSeed = item.buildingData.NameSeed;
                            building.FactionId = item.buildingData.FactionId;
                            building.Sector = item.buildingData.Sector;
                            building.LocationId = item.buildingData.LocationId;
                            building.Quality = item.buildingData.Quality;

                            // Check for replacement building data and use it if found
                            // if (WorldDataReplacement.GetBuildingReplacementData(blockName, block.Index, i, out buildingReplacementData))
                            // {
                            //     // Use custom building values from replacement data, but only use up pool item if factionId is zero
                            //     if (buildingReplacementData.FactionId != 0)
                            //     {
                            //         // Don't use up pool item and set factionId, NameSeed, Quality from replacement data
                            //         item.used = false;
                            //         building.FactionId = buildingReplacementData.FactionId;
                            //         building.Quality = buildingReplacementData.Quality;
                            //         building.NameSeed = (ushort)(buildingReplacementData.NameSeed + location.LocationIndex);    // Vary name seed by location
                            //     }
                            //     // Always override type
                            //     building.BuildingType = (DFLocation.BuildingTypes)buildingReplacementData.BuildingType;
                            // }

                            // Matched to classic: special handling for some Order of the Raven buildings
                            if (block.RmbBlock.FldHeader.OtherNames != null &&
                                block.RmbBlock.FldHeader.OtherNames[i] == "KRAVE01.HS2")
                            {
                                building.BuildingType = DFLocation.BuildingTypes.GuildHall;
                                building.FactionId = 414;
                            }
                        }
                    }

                    // Save block data
                    blocks.Add(block);
                }
            }

            blocksArray = blocks.ToArray();

#if !UNITY_EDITOR // Cache blocks for this location if not in editor
            if (locationCache.Count == maxLocationCacheSize)
                locationCache.RemoveAt(0);
            locationCache.Add(new KeyValuePair<int, DFBlock[]>(mapId, blocksArray));
#endif

            return blocksArray;
        }

        /// <summary>
        /// Draws and consume building from pool.
        /// Checking if buildings are ordered by special type.
        /// </summary>
        static bool GetNextBuildingFromPool(List<BuildingPoolItem> namedBuildingPool, DFLocation.BuildingTypes buildingType, out BuildingPoolItem itemOut)
        {
            itemOut = new BuildingPoolItem();
            for (int i = 0; i < namedBuildingPool.Count; i++)
            {
                if (!namedBuildingPool[i].used && namedBuildingPool[i].buildingData.BuildingType == buildingType)
                {
                    itemOut = namedBuildingPool[i];
                    itemOut.used = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if building type is a special named building.
        /// </summary>
        public static bool IsNamedBuilding(DFLocation.BuildingTypes buildingType)
        {
            switch (buildingType)
            {
                case DFLocation.BuildingTypes.Alchemist:
                case DFLocation.BuildingTypes.Armorer:
                case DFLocation.BuildingTypes.Bank:
                case DFLocation.BuildingTypes.Bookseller:
                case DFLocation.BuildingTypes.ClothingStore:
                case DFLocation.BuildingTypes.FurnitureStore:
                case DFLocation.BuildingTypes.GemStore:
                case DFLocation.BuildingTypes.GeneralStore:
                case DFLocation.BuildingTypes.Library:
                case DFLocation.BuildingTypes.GuildHall:
                case DFLocation.BuildingTypes.PawnShop:
                case DFLocation.BuildingTypes.WeaponSmith:
                case DFLocation.BuildingTypes.Temple:
                case DFLocation.BuildingTypes.Tavern:
                case DFLocation.BuildingTypes.Palace:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if building is a residence.
        /// </summary>
        public static bool IsResidence(DFLocation.BuildingTypes buildingType)
        {
            // Only House1-House4 seem to ID as "Residence" (to confirm)
            if (buildingType >= DFLocation.BuildingTypes.House1 && buildingType <= DFLocation.BuildingTypes.House4)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Checks if building is a shop with quality text.
        /// </summary>
        public static bool IsShop(DFLocation.BuildingTypes buildingType)
        {
            switch (buildingType)
            {
                case DFLocation.BuildingTypes.Alchemist:
                case DFLocation.BuildingTypes.Armorer:
                case DFLocation.BuildingTypes.Bookseller:
                case DFLocation.BuildingTypes.ClothingStore:
                case DFLocation.BuildingTypes.FurnitureStore:
                case DFLocation.BuildingTypes.GemStore:
                case DFLocation.BuildingTypes.GeneralStore:
                case DFLocation.BuildingTypes.PawnShop:
                case DFLocation.BuildingTypes.WeaponSmith:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if building is a shop which offers repair service.
        /// </summary>
        public static bool IsRepairShop(DFLocation.BuildingTypes buildingType)
        {
            switch (buildingType)
            {
                case DFLocation.BuildingTypes.Armorer:
                case DFLocation.BuildingTypes.GeneralStore:
                case DFLocation.BuildingTypes.WeaponSmith:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if building is a tavern.
        /// </summary>
        public static bool IsTavern(DFLocation.BuildingTypes buildingType) => buildingType == DFLocation.BuildingTypes.Tavern;

        public static DFBlock.RmbBlockPeopleRecord ConvertFromFlatObjectToPeople(DFBlock.RmbBlockFlatObjectRecord flatPeople)
        {
            DFBlock.RmbBlockPeopleRecord converted = new DFBlock.RmbBlockPeopleRecord
            {
                Position = flatPeople.Position,
                XPos = flatPeople.XPos,
                YPos = flatPeople.YPos,
                ZPos = flatPeople.ZPos,
                TextureBitfield = flatPeople.TextureBitfield,
                TextureArchive = flatPeople.TextureArchive,
                TextureRecord = flatPeople.TextureRecord,
                FactionID = flatPeople.FactionID,
                Flags = flatPeople.Flags
            };
            return converted;
        }

        public static DFBlock.RmbBlockFlatObjectRecord ConvertFromPeopleToFlatObject(DFBlock.RmbBlockPeopleRecord flatPeople)
        {
            DFBlock.RmbBlockFlatObjectRecord converted = new DFBlock.RmbBlockFlatObjectRecord
            {
                Position = flatPeople.Position,
                XPos = flatPeople.XPos,
                YPos = flatPeople.YPos,
                ZPos = flatPeople.ZPos,
                TextureBitfield = flatPeople.TextureBitfield,
                TextureArchive = flatPeople.TextureArchive,
                TextureRecord = flatPeople.TextureRecord,
                FactionID = flatPeople.FactionID,
                Flags = flatPeople.Flags
            };
            return converted;
        }

        #endregion

        #region Private Methods

        private static void AddModels(
            DaggerfallUnity dfUnity,
            int layoutX,
            int layoutY,
            ref DFBlock blockData,
            out List<StaticDoor> doorsOut,
            out List<StaticBuilding> buildingsOut,
            ModelCombiner combiner = null,
            Transform parent = null)
        {
            doorsOut = new List<StaticDoor>();
            buildingsOut = new List<StaticBuilding>();

            // Iterate through all subrecords
            int recordCount = 0;
            foreach (DFBlock.RmbSubRecord subRecord in blockData.RmbBlock.SubRecords)
            {
                // Get subrecord transform
                Vector3 subRecordPosition = new Vector3(subRecord.XPos, 0, BlocksFile.RMBDimension - subRecord.ZPos) * MeshReader.GlobalScale;
                Vector3 subRecordRotation = new Vector3(0, -subRecord.YRotation / BlocksFile.RotationDivisor, 0);
                Matrix4x4 subRecordMatrix = Matrix4x4.TRS(subRecordPosition, Quaternion.Euler(subRecordRotation), Vector3.one);

                // Iterate through models in this subrecord
                bool firstModel = true;
                foreach (DFBlock.RmbBlock3dObjectRecord obj in subRecord.Exterior.Block3dObjectRecords)
                {
                    // Get model transform
                    Vector3 modelPosition = new Vector3(obj.XPos, -obj.YPos, obj.ZPos) * MeshReader.GlobalScale;
                    Vector3 modelRotation = new Vector3(-obj.XRotation / BlocksFile.RotationDivisor, -obj.YRotation / BlocksFile.RotationDivisor, -obj.ZRotation / BlocksFile.RotationDivisor);
                    Vector3 modelScale = GetModelScaleVector(obj);
                    Matrix4x4 modelMatrix = subRecordMatrix * Matrix4x4.TRS(modelPosition, Quaternion.Euler(modelRotation), modelScale);

                    // Get model data
                    ModelData modelData;
                    dfUnity.MeshReader.GetModelData(obj.ModelIdNum, out modelData);

                    // Does this model have doors?
                    StaticDoor[] staticDoors = null;
                    if (modelData.Doors != null)
                        staticDoors = GameObjectHelper.GetStaticDoors(ref modelData, blockData.Index, recordCount, modelMatrix);

                    // Store building information for first model of record
                    // First model is main record structure, others are attachments like posts
                    // Only main structure is needed to resolve building after hit-test
                    int buildingKey = 0;
                    if (firstModel)
                    {
                        // Create building key for this record - considered experimental for now
                        buildingKey = BuildingDirectory.MakeBuildingKey((byte)layoutX, (byte)layoutY, (byte)recordCount);

                        StaticBuilding staticBuilding = new StaticBuilding();
                        staticBuilding.modelMatrix = modelMatrix;
                        staticBuilding.recordIndex = recordCount;
                        staticBuilding.centre = new Vector3(modelData.DFMesh.Centre.X, modelData.DFMesh.Centre.Y, modelData.DFMesh.Centre.Z) * MeshReader.GlobalScale;
                        staticBuilding.size = new Vector3(modelData.DFMesh.Size.X, modelData.DFMesh.Size.Y, modelData.DFMesh.Size.Z) * MeshReader.GlobalScale;
                        buildingsOut.Add(staticBuilding);
                        firstModel = false;
                    }

                    bool dontCreateStaticDoors = false;

                    // Import custom GameObject or use Daggerfall Model
                    GameObject go;
                    if (go = MeshReplacement.ImportCustomGameobject(obj.ModelIdNum, parent, modelMatrix))
                    {
                        // Find doors
                        if (staticDoors != null && staticDoors.Length > 0)
                            CustomDoor.InitDoors(go, staticDoors, buildingKey, out dontCreateStaticDoors);
                    }
                    else if (combiner == null || IsCityGate(obj.ModelIdNum) || IsBulletinBoard(obj.ModelIdNum) || PlayerActivate.HasCustomActivation(obj.ModelIdNum))
                        AddStandaloneModel(dfUnity, ref modelData, modelMatrix, parent);
                    else
                        combiner.Add(ref modelData, modelMatrix);

                    if (modelData.Doors != null && !dontCreateStaticDoors)
                        doorsOut.AddRange(staticDoors);
                }

                // Increment record count
                recordCount++;
            }
        }

        private static void AddProps(
            DaggerfallUnity dfUnity,
            ref DFBlock blockData,
            out List<StaticDoor> doorsOut,
            ModelCombiner combiner = null,
            Transform parent = null)
        {
            doorsOut = new List<StaticDoor>();

            // Iterate through all misc records
            foreach (DFBlock.RmbBlock3dObjectRecord obj in blockData.RmbBlock.Misc3dObjectRecords)
            {
                // Get model transform
                Vector3 modelPosition = new Vector3(obj.XPos, -obj.YPos + propsOffsetY, obj.ZPos + BlocksFile.RMBDimension) * MeshReader.GlobalScale;
                Vector3 modelRotation = new Vector3(-obj.XRotation / BlocksFile.RotationDivisor, -obj.YRotation / BlocksFile.RotationDivisor, -obj.ZRotation / BlocksFile.RotationDivisor);
                Vector3 modelScale = GetModelScaleVector(obj);
                Matrix4x4 modelMatrix = Matrix4x4.TRS(modelPosition, Quaternion.Euler(modelRotation), modelScale);

                // Get model data
                ModelData modelData;
                dfUnity.MeshReader.GetModelData(obj.ModelIdNum, out modelData);

                // Does this model have doors?
                if (modelData.Doors != null)
                    doorsOut.AddRange(GameObjectHelper.GetStaticDoors(ref modelData, blockData.Index, 0, modelMatrix));

                // Import custom GameObject
                if (MeshReplacement.ImportCustomGameobject(obj.ModelIdNum, parent, modelMatrix) != null)
                    continue;

                // Use Daggerfall Model
                // Add or combine
                if (combiner == null || IsBulletinBoard(obj.ModelIdNum) || PlayerActivate.HasCustomActivation(obj.ModelIdNum))
                {
                    AddStandaloneModel(dfUnity, ref modelData, modelMatrix, parent);
                }
                else
                {
                    combiner.Add(ref modelData, modelMatrix);
                }
            }
        }

        private static GameObject AddStandaloneModel(
            DaggerfallUnity dfUnity,
            ref ModelData modelData,
            Matrix4x4 matrix,
            Transform parent)
        {
            uint modelID = (uint)modelData.DFMesh.ObjectId;

            // Add GameObject
            GameObject go = GameObjectHelper.CreateDaggerfallMeshGameObject(modelID, parent, dfUnity.Option_SetStaticFlags);
            go.transform.position = matrix.GetColumn(3);
            go.transform.rotation = matrix.rotation;
            go.transform.localScale = matrix.lossyScale;

            // Is this a city gate?
            if (IsCityGate(modelID))
            {
                go.AddComponent<DaggerfallCityGate>();
            }

            // Is this a bulletin board?
            if (IsBulletinBoard(modelID))
            {
                go.AddComponent<DaggerfallBulletinBoard>();
            }

            return go;
        }

        private static void AddLight(DaggerfallUnity dfUnity, DFBlock.RmbBlockFlatObjectRecord obj, Transform parent = null)
        {
            if (dfUnity.Option_CityLightPrefab == null)
                return;

            Vector2 size = dfUnity.MeshReader.GetScaledBillboardSize(210, obj.TextureRecord);
            Vector3 position = new Vector3(
                obj.XPos,
                -obj.YPos + size.y,
                obj.ZPos + BlocksFile.RMBDimension) * MeshReader.GlobalScale;

            GameObjectHelper.InstantiatePrefab(dfUnity.Option_CityLightPrefab.gameObject, string.Empty, parent, position);
        }

        private static void AddStaticDoors(StaticDoor[] doors, GameObject target)
        {
            DaggerfallStaticDoors c = target.GetComponent<DaggerfallStaticDoors>();
            if (c == null)
                c = target.AddComponent<DaggerfallStaticDoors>();
            if (doors != null && target != null)
                c.Doors = doors;
        }

        private static void AddStaticBuildings(StaticBuilding[] buildings, GameObject target)
        {
            DaggerfallStaticBuildings c = target.GetComponent<DaggerfallStaticBuildings>();
            if (c == null)
                c = target.AddComponent<DaggerfallStaticBuildings>();
            if (buildings != null && target != null)
                c.Buildings = buildings;
        }

        private static bool IsCityGate(uint modelID)
        {
            // Two variants of City Gate model known
            return modelID == CityGateOpenModelID || modelID == CityGateClosedModelID;
        }

        private static bool IsBulletinBoard(uint modelID)
        {
            // Only a single variant of Bulletin Board model known
            return modelID == BulletinBoardModelID;
        }


        #endregion
    }
}