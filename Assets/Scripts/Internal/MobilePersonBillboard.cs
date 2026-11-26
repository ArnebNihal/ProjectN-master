// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    TheLacus
// 
// Notes:
//

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Utility;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Utility.AssetInjection;
using Newtonsoft.Json;
using DaggerfallWorkshop.Utility;
using System.Linq;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// The component that handles graphics for a mobile person.
    /// </summary>
    /// <remarks>
    /// Implementations should be added to a prefab named "MobilePersonAsset" bundled with a mod.
    /// This prefab acts as a provider for all custom npcs graphics. This allows mods to replace classic
    /// <see cref="MobilePersonBillboard"/> with 3d models or other alternatives.
    /// The actual graphic asset for specific npc should be loaded when requested by <see cref="SetPerson"/>.
    /// </remarks>
    public abstract class MobilePersonAsset : MonoBehaviour
    {
        /// <summary>
        /// Trigger collider used for interaction with player. The collider should be altered to enclose the entire npc mesh when
        /// <see cref="SetPerson"/> is called. A sign of badly setup collider is misbehaviour of idle state and talk functionalities.
        /// </summary>
        protected internal CapsuleCollider Trigger { get; internal set; }

        /// <summary>
        /// Gets or sets idle state. Daggerfall NPCs are either in or motion or idle facing player.
        /// This only controls animation state, actual motion is handled by <see cref="MobilePersonMotor"/>.
        /// </summary>
        public abstract bool IsIdle { get; set; }

        /// <summary>
        /// Setup this person based on race, gender and outfit variant. Enitities in a npcs pool can be recycled
        /// when out of range, meaning that this method can be called more than once with different parameters.
        /// </summary>
        /// <param name="race">Race of target npc.</param>
        /// <param name="gender">Gender of target npc.</param>
        /// <param name="personVariant">Which basic outfit does the person wear.</param>
        /// <param name="isGuard">True if this npc is a city watch guard.</param>
        public abstract void SetPerson(Races race, Genders gender, ref int personVariant, bool isGuard, int personFaceVariant, out int faceRecordId);

        /// <summary>
        /// Gets size of asset used by this person (i.e size of bounds). Used to adjust position on terrain.
        /// </summary>
        /// <returns>Size of npc.</returns>
        public abstract Vector3 GetSize();

        /// <summary>
        /// Gets a bitmask that provides all the layers used by this asset.
        /// </summary>
        /// <returns>A layer mask.</returns>
        public virtual int GetLayerMask()
        {
            return 1 << gameObject.layer;
        }
    }

    /// <summary>
    /// Billboard class for classic wandering NPCs found in town environments.
    /// </summary>
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class MobilePersonBillboard : MobilePersonAsset
    {
        #region Fields

        const int numberOrientations = 8;
        const float anglePerOrientation = 360f / numberOrientations;

        Vector3 cameraPosition;
        Camera mainCamera = null;
        MeshFilter meshFilter = null;
        MeshRenderer meshRenderer = null;
        float facingAngle;
        int lastOrientation;
        AnimStates currentAnimState;

        Vector2[] recordSizes;
        int[] recordFrames;
        Rect[] atlasRects;
        RecordIndex[] atlasIndices;
        MobileAnimation[] stateAnims;
        MobileAnimation[] moveAnims;
        MobileAnimation[] idleAnims;
        MobileBillboardImportedTextures importedTextures;
        int currentFrame = 0;

        float animSpeed;
        float animTimer = 0;

        bool isUsingGuardTexture = false;

        #endregion

        #region Textures

        int[] maleBretonTextures = new int[] { 300, 600, 100, 400 };
        int[] maleBretonDesertTextures = new int[] { -1, -1, -1, -1 };
        int[] maleBretonMountainTextures = new int[] { -1, 400, -1, -1 };
        int[] maleBretonColdTextures = new int[] { 100, -1, -1, -1 };

        int[] femaleBretonTextures = new int[] { 0, 100, 200, 300, 400, 500 };  // { 453, 454, 455, 456 }
        int[] femaleBretonDesertTextures = new int[] { };
        int[] femaleBretonMountainTextures = new int[] { 0, 0, -1, 0 };
        int[] femaleBretonColdTextures = new int[] { -1, -1, 0, -1 };

        int[] maleRedguardTextures = new int[] { 0, 300, 400, 600 };
        int[] maleRedguardDesertTextures = new int[] { 0, 100, 800, 900 };    // { 381, 382, 383, 384 }
        int[] maleRedguardMountainTextures = new int[] { 100, 500, 400, 200 };
        int[] maleRedguardColdTextures = new int[] { 400, 500, 400, 400 };

        int[] femaleRedguardTextures = new int[] { 0, 100, 200, 300 };   // { 395, 396, 397, 398 }
        int[] femaleRedguardDesertTextures = new int[] { 0, -1, -1, -1 };  // { 395, 396, 397, 398 }
        int[] femaleRedguardMountainTextures = new int[] { 0, -1, -1, -1 };
        int[] femaleRedguardColdTextures = new int[] { -1, 100, 100, 300 };

        int[] maleNordTextures = new int[] { -1, 800, -1, -1 };  // { 387, 388, 389, 390 }
        int[] maleNordDesertTextures = new int[] { -1, -1, -1, -1 };
        int[] maleNordMountainTextures = new int[] { 100, -1, 300, -1 };
        int[] maleNordColdTextures = new int[] { 700, 400, 500, 900 };

        int[] femaleNordTextures = new int[] { 0, -1, -1, -1 };    // { 392, 393, 451, 452 }
        int[] femaleNordDesertTextures = new int[] { -1, -1, -1, -1 };
        int[] femaleNordMountainTextures = new int[] { -1, 0, 100, 0 };
        int[] femaleNordColdTextures = new int[] { -1, 0, -1, 0 };

        int[] maleDarkElfTextures = new int[] { 0400001,
                                                0400101, 0400102, 0400103 };
        int[] maleDarkElfDesertTextures = new int[] { 0410001,
                                                      0410101, 0410102, 0410103 };
        int[] maleDarkElfMountainTextures = new int[] { };
        int[] maleDarkElfColdTextures = new int[] { };
        int[] femaleDarkElfTextures = new int[] { 5400001,
                                                  5400101, 5400102 };
        int[] femaleDarkElfDesertTextures = new int[] { 5410001,
                                                       5410101, 5410102, 5410103 };
        int[] femaleDarkElfMountainTextures = new int[] { };
        int[] femaleDarkElfColdTextures = new int[] { };

        int[] maleOrcTextures = new int[] { 1000001 };
        int[] femaleOrcTextures = new int[] { 6000001 };

        int[] guardTextures = { 3990100, 3990017, 3990020, 3990023, 3990200, 3990016 };

        int[] maleBretonFaceRecordIndex = new int[] { 192, 216, 288, 240 };     // matching texture 385, 386, 391, 394 from MobilePersonBillboard class texture definition
        int[] femaleBretonFaceRecordIndex = new int[] { 72, 72, 24, 72 };       // matching texture 453, 454, 455, 456 from MobilePersonBillboard class texture definition

        int[] maleRedguardFaceRecordIndex = new int[] { 336, 312, 336, 336 };   // matching textures 381, 382, 383, 384 from MobilePersonBillboard class texture definition
        int[] femaleRedguardFaceRecordIndex = new int[] { 144, 120, 120, 96 };  // matching texture 395, 396, 397, 398 from MobilePersonBillboard class texture definition

        int[] maleNordFaceRecordIndex = new int[] { 240, 264, 168, 192 };       // matching texture 387, 388, 389, 390 from MobilePersonBillboard class texture definition
        int[] femaleNordFaceRecordIndex = new int[] { 72, 0, 48, 0 };          // matching texture 392, 393, 451, 452 from MobilePersonBillboard class texture definition

        int[] maleOrcFaceRecordIndex = new int[] { 0, 5, 10, 0 };
        int[] femaleOrcFaceRecordIndex = new int[] { 15, 20, 25, 15 };

        int[,] femaleBretonFaceBodyIndex = new int[,]
        {
            { 1, 2, 3, 5, 7, 102, 101, 11, 9, 103, 104, 4, 8, 6, 12, 15, 10, 14, 13, 106, 107, 105, 201, 202 },
            { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
            { 1, 2, 3, 5, 7, 102, 101, 11, 9, 103, 104, 4, 8, 6, 12, 18, 10, 17, 14, 15, 106, 105, 13, 16 },
            { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }
        };

        #endregion

        #region Animations

        const int IdleAnimSpeed = 1;
        const int MoveAnimSpeed = 4;
        static MobileAnimation[] IdleAnims = new MobileAnimation[]
        {
            new MobileAnimation() {Record = 5, FramePerSecond = IdleAnimSpeed, FlipLeftRight = false},          // Idle
        };

        static MobileAnimation[] IdleAnimsGuard = new MobileAnimation[]
        {
            new MobileAnimation() {Record = 15, FramePerSecond = IdleAnimSpeed, FlipLeftRight = false},          // Guard idle
        };

        static MobileAnimation[] MoveAnims = new MobileAnimation[]
        {
            new MobileAnimation() {Record = 0, FramePerSecond = MoveAnimSpeed, FlipLeftRight = false},          // Facing south (front facing player)
            new MobileAnimation() {Record = 1, FramePerSecond = MoveAnimSpeed, FlipLeftRight = false},          // Facing south-west
            new MobileAnimation() {Record = 2, FramePerSecond = MoveAnimSpeed, FlipLeftRight = false},          // Facing west
            new MobileAnimation() {Record = 3, FramePerSecond = MoveAnimSpeed, FlipLeftRight = false},          // Facing north-west
            new MobileAnimation() {Record = 4, FramePerSecond = MoveAnimSpeed, FlipLeftRight = false},          // Facing north (back facing player)
            new MobileAnimation() {Record = 3, FramePerSecond = MoveAnimSpeed, FlipLeftRight = true},           // Facing north-east
            new MobileAnimation() {Record = 2, FramePerSecond = MoveAnimSpeed, FlipLeftRight = true},           // Facing east
            new MobileAnimation() {Record = 1, FramePerSecond = MoveAnimSpeed, FlipLeftRight = true},           // Facing south-east
        };

        enum AnimStates
        {
            Idle,           // Idle facing player
            Move,           // Moving
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets idle state.
        /// Daggerfall NPCs are either in or motion or idle facing player.
        /// This only controls anim state, actual motion is handled by MobilePersonMotor.
        /// </summary>
        public sealed override bool IsIdle
        {
            get { return (currentAnimState == AnimStates.Idle); }
            set { SetIdle(value); }
        }

        #endregion

        #region Unity

        private void Start()
        {
            if (Application.isPlaying)
            {
                // Get component references
                mainCamera = GameManager.Instance.MainCamera;
                meshFilter = GetComponent<MeshFilter>();
            }

            // Mobile NPC shadows if enabled
            if (DaggerfallUnity.Settings.MobileNPCShadows)
                GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        }

        private void Update()
        {
            // Rotate to face camera in game
            if (mainCamera && Application.isPlaying)
            {
                // Rotate billboard based on camera facing
                cameraPosition = mainCamera.transform.position;
                Vector3 viewDirection = -new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z);
                transform.LookAt(transform.position + viewDirection);

                // Orient based on camera position
                UpdateOrientation();

                // Tick animations
                animTimer += Time.deltaTime;
                if (animTimer > 1f / animSpeed)
                {
                    if (++currentFrame >= stateAnims[0].NumFrames)
                        currentFrame = 0;

                    UpdatePerson(lastOrientation);
                    animTimer = 0;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Setup this person based on race and gender.
        /// </summary>
        public override void SetPerson(Races race, Genders gender, ref int personVariant, bool isGuard, int personFaceVariant, out int faceRecordId)
        {
            faceRecordId = -1;
            // Must specify a race
            if (race == Races.None)
                return;

            // Get texture range for this race and gender
            int[] textures = null;
            int texture = 0;

            isUsingGuardTexture = isGuard;
            MapsFile.Climates climate = (MapsFile.Climates)GameManager.Instance.PlayerGPS.CurrentClimateIndex;
            DaggerfallDateTime.Seasons season = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.GenericSeasonValue;

            if (isGuard)
            {
                texture = guardTextures[personVariant];
            }
            else
            {
                texture = GetTextureNumber(race, climate, season, gender, ref personVariant, personFaceVariant, out faceRecordId);
            }

            Debug.Log("Step2 -> texture: " + texture);
            // Setup person rendering
            CacheRecordSizesAndFrames(texture);
            AssignMeshAndMaterial(texture);

            // Setup animation state
            moveAnims = GetStateAnims(AnimStates.Move);
            idleAnims = GetStateAnims(AnimStates.Idle);
            stateAnims = moveAnims;
            animSpeed = stateAnims[0].FramePerSecond;
            currentAnimState = AnimStates.Move;
            lastOrientation = -1;
            UpdateOrientation();
        }

        /// <summary>
        /// Get NPC billboard based on race, climate, season and gender.
        /// </summary>
        public int GetTextureNumber(Races race, MapsFile.Climates climate, DaggerfallDateTime.Seasons season, Genders gender, ref int personVariant, int personFaceVariant, out int faceRecordId)
        {
            int texture, resultRace, resultTemp;
            // Races enum is now 0-based, so we modify it for the subsequent operations.
            // Yeah, I know, this will cause me many problems in the near future...
            resultRace = (int)race + 1;

            switch (climate)
            {
                case MapsFile.Climates.Desert:
                case MapsFile.Climates.Savannah:
                    resultTemp = 1;
                    break;

                case MapsFile.Climates.Mountain:
                case MapsFile.Climates.MountainWoods:
                    switch (season)
                    {
                        case DaggerfallDateTime.Seasons.Summer:
                            resultTemp = 0;
                            break;

                        case DaggerfallDateTime.Seasons.Spring:
                        case DaggerfallDateTime.Seasons.Fall:
                            resultTemp = 2;
                            break;

                        case DaggerfallDateTime.Seasons.Winter:
                        default:
                            resultTemp = 3;
                            break;
                    }
                    break;

                case MapsFile.Climates.Rainforest:
                case MapsFile.Climates.Subtropical:
                    switch (season)
                    {
                        case DaggerfallDateTime.Seasons.Spring:
                        case DaggerfallDateTime.Seasons.Summer:
                            resultTemp = 1;
                            break;

                        case DaggerfallDateTime.Seasons.Fall:
                        case DaggerfallDateTime.Seasons.Winter:
                        default:
                            resultTemp = 0;
                            break;
                    }
                    break;

                case MapsFile.Climates.Swamp:
                case MapsFile.Climates.Woodlands:
                case MapsFile.Climates.Steppes:
                case MapsFile.Climates.Maquis:
                    switch (season)
                    {
                        case DaggerfallDateTime.Seasons.Spring:
                        case DaggerfallDateTime.Seasons.Summer:
                            resultTemp = 0;
                            break;

                        case DaggerfallDateTime.Seasons.Fall:
                            resultTemp = 2;
                            break;

                        case DaggerfallDateTime.Seasons.Winter:
                        default:
                            resultTemp = 3;
                            break;
                    }
                    break;

                default:
                    resultTemp = 0;
                    break;
            }

            texture = resultRace * 10 + resultTemp;
            if (gender == Genders.Female) texture += 500;

            int[] textures = GetTextureType(texture);

            if (!TexturesAvailable(textures, out personVariant))
            {
                textures = GetTextureType(texture / 10 * 10);   // if there are no billboards for that particular climate/season, we'll just dish out a regular (i.e.: temperate) townfolk of that race.
                if (!TexturesAvailable(textures, out personVariant))
                    Debug.Log("No usable texture found for temperate version of race " + race);
            }

            faceRecordId = GetFaceRecordArray(race, gender)[personVariant] + personFaceVariant;

            return GetTextureVariant(texture, GetFaceRecordArray(race, gender), textures, faceRecordId);
        }

        public bool TexturesAvailable(int[] textures, out int pickedPersonVariant)
        {
            List<int> availablePersonVariant = new List<int>();
            pickedPersonVariant = -1;
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] >= 0)
                availablePersonVariant.Add(i);
            }
            if (availablePersonVariant.Count < 1)
                return false;
            pickedPersonVariant = availablePersonVariant[UnityEngine.Random.Range(0, availablePersonVariant.Count)];
            return true;
        }

        public int[] GetFaceRecordArray(Races race, Genders gender)
        {
            switch (race)
            {
                case Races.Breton:
                    if (gender == Genders.Female)
                        return femaleBretonFaceRecordIndex;
                    return maleBretonFaceRecordIndex;
                case Races.Redguard:
                    if (gender == Genders.Female)
                        return femaleRedguardFaceRecordIndex;
                    return maleRedguardFaceRecordIndex;
                case Races.Nord:
                    if (gender == Genders.Female)
                        return femaleNordFaceRecordIndex;
                    return maleNordFaceRecordIndex;
                case Races.Orc:
                    if (gender == Genders.Female)
                        return femaleOrcFaceRecordIndex;
                    return maleOrcFaceRecordIndex;
                default:
                    return maleBretonFaceRecordIndex;
            }
        }

        public int[] GetTextureType(int texture)
        {
            switch (texture)
            {
                case 10: return maleBretonTextures;
                case 11: return maleBretonDesertTextures;
                case 12: return maleBretonMountainTextures;
                case 13: return maleBretonColdTextures;

                case 20: return maleRedguardTextures;
                case 21: return maleRedguardDesertTextures;
                case 22: return maleRedguardMountainTextures;
                case 23: return maleRedguardColdTextures;

                case 30: return maleNordTextures;
                case 31: return maleNordDesertTextures;
                case 32: return maleNordMountainTextures;
                case 33: return maleNordColdTextures;

                case 40: return maleDarkElfTextures;
                case 41: return maleDarkElfDesertTextures;
                case 42: return maleDarkElfMountainTextures;
                case 43: return maleDarkElfColdTextures;

                case 100:
                case 101:
                case 102:
                case 103: return maleOrcTextures;

                case 510: return femaleBretonTextures;
                case 511: return femaleBretonDesertTextures;
                case 512: return femaleBretonMountainTextures;
                case 513: return femaleBretonColdTextures;

                case 520: return femaleRedguardTextures;
                case 521: return femaleRedguardDesertTextures;
                case 522: return femaleRedguardMountainTextures;
                case 523: return femaleRedguardColdTextures;

                case 530: return femaleNordTextures;
                case 531: return femaleNordDesertTextures;
                case 532: return femaleNordMountainTextures;
                case 533: return femaleNordColdTextures;

                case 540: return femaleDarkElfTextures;
                case 541: return femaleDarkElfDesertTextures;
                case 542: return femaleDarkElfMountainTextures;
                case 543: return femaleDarkElfColdTextures;

                case 600:
                case 601:
                case 602:
                case 603: return femaleOrcTextures;

                default: return maleBretonTextures;
            }
        }

        public int GetTextureVariant(int raceClimateIndex, int[] faceRecordIndex, int[] textureRecordIndex, int faceRecord)
        {
            Debug.Log("Step1 -> raceClimateIndex: " + raceClimateIndex + ", faceRecord: " + faceRecord);
            int variantMod = 0;
            if ((raceClimateIndex >= 510 && raceClimateIndex <= 512) ||
                (raceClimateIndex >= 530 && raceClimateIndex >= 530))
            {
                int result = CombineComplexFaceBodyCouple(faceRecord, raceClimateIndex);
                if (textureRecordIndex.Contains(result % 1000 / 100 * 100))
                    return result;
                return -1;
            }

            List<int> compatibleComb = new List<int>();
            for (int i = 0; i < faceRecordIndex.Length; i++)
            {
                if (faceRecord >= faceRecordIndex[i] &&
                    faceRecord <= faceRecordIndex[i] + 23)
                {
                    compatibleComb.Add(i);
                }
            }

            int index = compatibleComb[UnityEngine.Random.Range(0, compatibleComb.Count)];
            // Accounting for those beardless redguard faces that have a bearded body.
            if (raceClimateIndex == 20)
            {
                if (faceRecord == 323)
                {
                    faceRecord = 361;
                    index = 0;
                }
                else if (faceRecord == 327)
                {
                    faceRecord = 360;
                    index = 0;
                }
            }
            if (raceClimateIndex == 21)
            {
                if (faceRecord == 323)
                {
                    faceRecord = 361;
                    index = UnityEngine.Random.Range(0, 2);
                    if (index > 0) index++;
                }
                else if (faceRecord == 327)
                {
                    faceRecord = 360;
                    index = UnityEngine.Random.Range(0, 2);
                    if (index > 0) index++;
                }
                else if (index == 3)
                {
                    index = UnityEngine.Random.Range(0, 2);
                    if (index > 0) index++;
                }
                else if (index == 1)
                {
                    index = UnityEngine.Random.Range(1, 3);
                    if (index > 1) index++;
                }
            }
            if (raceClimateIndex == 22)
            {
                if (faceRecord == 323)
                {
                    faceRecord = 361;
                    index = 2;
                }
                else if (faceRecord == 327)
                {
                    faceRecord = 360;
                    index = 2;
                }
            }
            if (raceClimateIndex == 23)
            {
                if (faceRecord == 323)
                {
                    faceRecord = 361;
                    index = 0;
                }
                else if (faceRecord == 327)
                {
                    faceRecord = 360;
                    index = 0;
                }
            }
            if (raceClimateIndex == 521)
            {
                variantMod = UnityEngine.Random.Range(0, 2) == 0 ? 0 : 100;
            }
            if (textureRecordIndex[index] >= 0)
                return raceClimateIndex * 10000 + textureRecordIndex[index] + faceRecord - (faceRecordIndex[index] - 1) + variantMod;
            // If that particular texture doesn't exist, use a temperate one.                        
            else return raceClimateIndex / 10 * 10 * 10000 + textureRecordIndex[index] + faceRecord - (faceRecordIndex[index] - 1);
        }

        public int CombineComplexFaceBodyCouple(int faceRecord, int raceClimateIndex)
        {
            int randomMod = 0;
            if (raceClimateIndex == 510) // Female Breton Temperate
                randomMod = UnityEngine.Random.Range(0, 2) == 0 ? 0 : 300;
            if (raceClimateIndex == 530) // Female Nord Temperate
                randomMod = UnityEngine.Random.Range(0, 3) * 100;

            int result = raceClimateIndex * 10 * 10000 + randomMod;
            int climateIndex = raceClimateIndex % 10;
            if (faceRecord >= 72 && faceRecord <= 95)
                return result + femaleBretonFaceBodyIndex[climateIndex, faceRecord - 72];
            if (faceRecord >= 24 && faceRecord >= 47)
                return result + femaleBretonFaceBodyIndex[climateIndex, faceRecord - 24];
            return -1;
        }

        /// <summary>
        /// Gets billboard size.
        /// </summary>
        /// <returns>Vector2 of billboard width and height.</returns>
        public sealed override Vector3 GetSize()
        {
            if (recordSizes == null || recordSizes.Length == 0)
                return Vector2.zero;

            return recordSizes[0];
        }

        #endregion

        #region Private Methods

        void SetIdle(bool idle)
        {
            if (idle)
            {
                // Switch animation state to idle
                currentAnimState = AnimStates.Idle;
                stateAnims = idleAnims;
                currentFrame = 0;
                lastOrientation = -1;
                animTimer = 1;
                animSpeed = stateAnims[0].FramePerSecond;
            }
            else
            {
                // Switch animation state to move
                currentAnimState = AnimStates.Move;
                stateAnims = moveAnims;
                currentFrame = 0;
                lastOrientation = -1;
                animTimer = 1;
                animSpeed = stateAnims[0].FramePerSecond;
            }
        }

        private void CacheRecordSizesAndFrames(int textureArchive)
        {
            // Open texture file
            string path;
            if (textureArchive < 1000)
                path = Path.Combine(DaggerfallUnity.Instance.Arena2Path, TextureFile.IndexToFileName(textureArchive));
            else path = Path.Combine(DaggerfallUnity.Instance.TexturePath, TextureFile.IndexToFileName(textureArchive));

            TextureReplacement.LoadCustomMobileTexture(path, out CustomMobileTextures textureFile);
            // if (textureArchive >= 1000)
            //     textureFile = new TextureFile(path, FileUsage.UseMemory, true, true);

            // Cache size and scale for each record
            recordSizes = new Vector2[textureFile.Records];
            recordFrames = new int[textureFile.Records];

            for (int i = 0; i < textureFile.Records; i++)
            {
                // Get size and scale of this texture
                DFSize size = new DFSize(textureFile.Size[i * 2], textureFile.Size[i * 2 + 1]);
                DFSize scale = new DFSize((int)textureFile.Scale[i * 2], (int)textureFile.Scale[i * 2 + 1]);

                // Set start size
                Vector2 startSize;
                startSize.x = size.Width;
                startSize.y = size.Height;

                // Apply scale
                Vector2 finalSize;
                int xChange = (int)(size.Width * (scale.Width / BlocksFile.ScaleDivisor));
                int yChange = (int)(size.Height * (scale.Height / BlocksFile.ScaleDivisor));
                finalSize.x = (size.Width + xChange);
                finalSize.y = (size.Height + yChange);

                // Set optional scale
                TextureReplacement.SetBillboardScale(textureArchive, i, ref finalSize);

                // Store final size and frame count
                recordSizes[i] = finalSize * MeshReader.GlobalScale;
                recordFrames[i] = textureFile.Frames[i];
            }
        }

        private void AssignMeshAndMaterial(int textureArchive)
        {
            // Get mesh filter
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            // Vertices for a 1x1 unit quad
            // This is scaled to correct size depending on facing and orientation
            float hx = 0.5f, hy = 0.5f;
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(hx, hy, 0);
            vertices[1] = new Vector3(-hx, hy, 0);
            vertices[2] = new Vector3(hx, -hy, 0);
            vertices[3] = new Vector3(-hx, -hy, 0);

            // Indices
            int[] indices = new int[6]
            {
                0, 1, 2,
                3, 2, 1,
            };

            // Normals
            Vector3 normal = Vector3.Normalize(Vector3.up + Vector3.forward);
            Vector3[] normals = new Vector3[4];
            normals[0] = normal;
            normals[1] = normal;
            normals[2] = normal;
            normals[3] = normal;

            // Create mesh
            Mesh mesh = new Mesh();
            mesh.name = string.Format("MobilePersonMesh");
            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.normals = normals;

            // Assign mesh
            meshFilter.sharedMesh = mesh;

            // Create material
            Material material;
            if (textureArchive >= 100000)
                material = TextureReplacement.GetCustomMobileBillboardMaterial(textureArchive, GetComponent<MeshFilter>(), ref importedTextures);
            else material = TextureReplacement.GetMobileBillboardMaterial(textureArchive, meshFilter, ref importedTextures) ??
                DaggerfallUnity.Instance.MaterialReader.GetMaterialAtlas(
                textureArchive,
                0,
                4,
                1024,
                out atlasRects,
                out atlasIndices,
                4,
                true,
                0,
                false,
                true);

            // Set new person material
            GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void UpdateOrientation()
        {
            Transform parent = transform.parent;
            if (parent == null)
                return;

            // Get direction normal to camera, ignore y axis
            Vector3 dir = Vector3.Normalize(
                new Vector3(cameraPosition.x, 0, cameraPosition.z) -
                new Vector3(transform.position.x, 0, transform.position.z));

            // Get parent forward normal, ignore y axis
            Vector3 parentForward = transform.parent.forward;
            parentForward.y = 0;

            // Get angle and cross product for left/right angle
            facingAngle = Vector3.Angle(dir, parentForward);
            facingAngle = facingAngle * -Mathf.Sign(Vector3.Cross(dir, parentForward).y);

            // Facing index
            int orientation = -Mathf.RoundToInt(facingAngle / anglePerOrientation);
            // Wrap values to 0 .. numberOrientations-1
            orientation = (orientation + numberOrientations) % numberOrientations;

            // Change person to this orientation
            if (orientation != lastOrientation)
                UpdatePerson(orientation);
        }

        private void UpdatePerson(int orientation)
        {
            if (stateAnims == null || stateAnims.Length == 0)
                return;

            // Get mesh filter
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            // Idle only has a single orientation
            if (currentAnimState == AnimStates.Idle && orientation != 0)
                orientation = 0;

            // Get person size and scale for this state
            int record = stateAnims[orientation].Record;
            Vector2 size = recordSizes[record];

            // Set mesh scale for this state
            transform.localScale = new Vector3(size.x, size.y, 1);

            // Check if orientation flip needed
            bool flip = stateAnims[orientation].FlipLeftRight;

            // Update Record/Frame texture
            if (importedTextures.HasImportedTextures)
            {
                if (meshRenderer == null)
                    meshRenderer = GetComponent<MeshRenderer>();

                // Assign imported texture
                meshRenderer.sharedMaterial.mainTexture = importedTextures.Albedo[record][currentFrame];
                if (importedTextures.IsEmissive)
                    meshRenderer.material.SetTexture(Uniforms.EmissionMap, importedTextures.EmissionMaps[record][currentFrame]);

                // Update UVs on mesh
                Vector2[] uvs = new Vector2[4];
                if (flip)
                {
                    uvs[0] = new Vector2(1, 1);
                    uvs[1] = new Vector2(0, 1);
                    uvs[2] = new Vector2(1, 0);
                    uvs[3] = new Vector2(0, 0);
                }
                else
                {
                    uvs[0] = new Vector2(0, 1);
                    uvs[1] = new Vector2(1, 1);
                    uvs[2] = new Vector2(0, 0);
                    uvs[3] = new Vector2(1, 0);
                }
                meshFilter.sharedMesh.uv = uvs;
            }
            else
            {
                // Daggerfall Atlas: Update UVs on mesh
                Rect rect = atlasRects[atlasIndices[record].startIndex + currentFrame];
                Vector2[] uvs = new Vector2[4];
                if (flip)
                {
                    uvs[0] = new Vector2(rect.xMax, rect.yMax);
                    uvs[1] = new Vector2(rect.x, rect.yMax);
                    uvs[2] = new Vector2(rect.xMax, rect.y);
                    uvs[3] = new Vector2(rect.x, rect.y);
                }
                else
                {
                    uvs[0] = new Vector2(rect.x, rect.yMax);
                    uvs[1] = new Vector2(rect.xMax, rect.yMax);
                    uvs[2] = new Vector2(rect.x, rect.y);
                    uvs[3] = new Vector2(rect.xMax, rect.y);
                }
                meshFilter.sharedMesh.uv = uvs;
            }

            // Assign new orientation
            lastOrientation = orientation;
        }

        private MobileAnimation[] GetStateAnims(AnimStates state)
        {
            // Clone static animation state
            MobileAnimation[] anims;
            switch (state)
            {
                case AnimStates.Move:
                    anims = (MobileAnimation[])MoveAnims.Clone();
                    break;
                case AnimStates.Idle:
                    if (isUsingGuardTexture)
                        anims = (MobileAnimation[])IdleAnimsGuard.Clone();
                    else
                        anims = (MobileAnimation[])IdleAnims.Clone();
                    break;
                default:
                    return null;
            }

            // Assign number of frames per anim
            for (int i = 0; i < anims.Length; i++)
                anims[i].NumFrames = recordFrames[anims[i].Record];

            return anims;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        /// <summary>
        /// Rotate person to face editor camera while game not running.
        /// </summary>
        public void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                // Rotate to face camera
                UnityEditor.SceneView sceneView = GetActiveSceneView();
                if (sceneView)
                {
                    // Editor camera stands in for player camera in edit mode
                    cameraPosition = sceneView.camera.transform.position;
                    Vector3 viewDirection = -new Vector3(sceneView.camera.transform.forward.x, 0, sceneView.camera.transform.forward.z);
                    transform.LookAt(transform.position + viewDirection);
                    UpdateOrientation();
                }
            }
        }

        private SceneView GetActiveSceneView()
        {
            // Return the focused window if it is a SceneView
            if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == typeof(SceneView))
                return (SceneView)EditorWindow.focusedWindow;

            return null;
        }
#endif

        #endregion
    }
}