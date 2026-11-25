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

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Serialization;
using Newtonsoft.Json;
using DaggerfallConnect.Arena2;
using System.Linq;

namespace DaggerfallWorkshop.Game.Utility
{
    /// <summary>
    /// Generates names for Daggerfall NPCs and locations.
    /// </summary>
    public class NameHelper
    {
        #region Fields

        const string nameGenFilename = "NameGen.json";
        static string mapPath = WorldMaps.mapPath;

        Dictionary<BankTypes, NameBank> bankDict = null;

        #endregion

        #region Structs & Enums

        /// <summary>
        /// Name banks available for generation.
        /// </summary>
        public enum BankTypes
        {
            Breton,
            Redguard,
            Nord,
            DarkElf,
            HighElf,
            WoodElf,
            Khajiit,
            Imperial,       // Imperial names appear where one would expect Argonian names.
            Monster1,
            Monster2,
            Monster3,
            Argonian,
            BretonModern,
            Orc
        }

        /// <summary>
        /// A bank is an array of sets.
        /// </summary>
        public struct NameBank
        {
            public int setCount;
            public NameSet[] sets;
        }

        /// <summary>
        /// A set is an array of string parts.
        /// </summary>
        public struct NameSet
        {
            public int setIndex;
            public string[] parts;
        }

        #endregion

        #region Constructors

        public NameHelper()
        {
            LoadNameGenData();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets random full name (first name + surname) for an NPC.
        /// Supports Breton, Redguard, Nord, DarkElf, HighElf, WoodElf, Khajiit, Imperial.
        /// All other types return empty string.
        /// </summary>
        public string FullName(BankTypes type, Genders gender)
        {
            // Get parts
            Debug.Log("type: " + type + ", gender: " + gender);
            string firstName = FirstName(type, gender);
            string lastName = Surname(type, (int)gender);

            // Compose full name
            string fullName = firstName;
            if (!string.IsNullOrEmpty(lastName))
                fullName += " " + lastName;

            return fullName;
        }

        /// <summary>
        /// Gets random first name for an NPC.
        /// Supports Breton, Redguard, Nord, DarkElf, HighElf, WoodElf, Khajiit, Imperial, Argonian, Orc.
        /// </summary>
        public string FirstName(BankTypes type, Genders gender)
        {
            // Bank dictionary must be ready
            if (bankDict == null)
                return string.Empty;

            // Generate name by type
            NameBank nameBank = bankDict[type];
            string firstName = string.Empty;
            switch (type)
            {
                // As of today, these still didn't receive the Ibiss treatement
                case BankTypes.Imperial:
                case BankTypes.Orc:
                    firstName = GetRandomFirstName(nameBank, gender);
                    break;

                case BankTypes.Nord:
                    if (DFRandom.rand() % 3 == 0)
                        firstName = GetRandomFirstName(nameBank, gender);
                    else firstName = GetExpandedNordFirstName(nameBank, gender);
                    break;

                case BankTypes.DarkElf:
                case BankTypes.HighElf:
                case BankTypes.WoodElf:
                    uint pickedType = DFRandom.rand() % 7;
                    if (pickedType == 0)
                        firstName = GetRandomFirstName(nameBank, gender);
                    else if (pickedType < 3)
                        firstName = GetRandomFirstName(nameBank, gender);
                    else firstName = GetExpandedFirstName(nameBank, gender);
                    break;

                case BankTypes.Breton:
                    uint pickedBretonType = DFRandom.rand() % 7;
                    if (pickedBretonType == 0)
                        firstName = GetRandomFirstName(bankDict[BankTypes.Breton], gender);
                    else if (pickedBretonType < 3)
                        firstName = GetRandomFirstName(bankDict[BankTypes.BretonModern], gender);
                    else firstName = GetExpandedFirstName(bankDict[BankTypes.BretonModern], gender);
                    break;

                case BankTypes.Khajiit:
                    if (DFRandom.rand() % 3 == 0)
                        firstName = GetRandomFirstName(nameBank, gender);
                    else firstName = GetExpandedKhajiitFirstName(nameBank, gender);
                    break;

                case BankTypes.Redguard:                                                // Redguards have just a single name
                    firstName = GetRandomRedguardName(nameBank, gender);
                    break;

                case BankTypes.Argonian:
                    if (DFRandom.rand() % 3 == 0)
                        firstName = GetRandomArgonianName(nameBank, gender);
                    else firstName = GetExpandedArgonianName(nameBank, gender);
                    break;
            }

            return firstName;
        }

        /// <summary>
        /// Gets random surname for an NPC.
        /// Supports Breton, Nord, DarkElf, HighElf, WoodElf, Khajiit, Imperial.
        /// </summary>
        public string Surname(BankTypes type, int gender = 0)
        {
            // Bank dictionary must be ready
            if (bankDict == null)
                return string.Empty;

            // Generate name by type
            NameBank nameBank = bankDict[type];

            string lastName = string.Empty;
            switch (type)
            {
                case BankTypes.Breton:                         // These banks all work the same
                    uint pickedBretonType = DFRandom.rand() % 7;
                    if (pickedBretonType == 0)
                        lastName = GetRandomSurname(bankDict[BankTypes.Breton]);
                    else if (pickedBretonType < 3)
                        lastName = GetRandomSurname(bankDict[BankTypes.BretonModern]);
                    else lastName = GetExpandedSurname(bankDict[BankTypes.BretonModern]);
                    break;

                case BankTypes.Imperial:
                    lastName = GetRandomSurname(nameBank);
                    break;

                case BankTypes.DarkElf:
                case BankTypes.HighElf:
                case BankTypes.WoodElf:
                    if (DFRandom.rand() % 3 == 0)
                        lastName = GetRandomSurname(nameBank);
                    else lastName = GetExpandedSurname(nameBank);
                    break;

                case BankTypes.Nord:
                    if (DFRandom.rand() % 3 == 0)
                        lastName = GetRandomNordSurname(nameBank, (Genders)gender);
                    else lastName = GetExpandedNordSurname(nameBank, (Genders)gender);

                    break;

                case BankTypes.Khajiit:
                    if (UnityEngine.Random.Range(0, 10) == 0)
                        lastName = GetExpandedKhajiitSurname(nameBank, (Genders)gender);
                    else if (UnityEngine.Random.Range(0, 10) == 0)
                        lastName = GetRandomFirstName(nameBank, (Genders)gender);                        
                    break;

                case BankTypes.Orc:
                    lastName = GetRandomOrcSurname(nameBank, (Genders)gender);
                    break;
            }

            return lastName;
        }

        /// <summary>
        /// Gets random monster name for quests.
        /// </summary>
        public string MonsterName(Genders gender = Genders.Male)
        {
            // Bank dictionary must be ready
            if (bankDict == null)
                return string.Empty;

            return GetRandomMonsterName(gender);
        }

        #endregion

        #region Name Generation

        // Gets random first name by gender for names that follow 0+1 (male), 2+3 (female) pattern
        string GetRandomFirstName(NameBank nameBank, Genders gender)
        {
            // Get set parts
            string[] partsA, partsB;
            if (gender == Genders.Male)
            {
                partsA = nameBank.sets[0].parts;
                partsB = nameBank.sets[1].parts;
            }
            else
            {
                partsA = nameBank.sets[2].parts;
                partsB = nameBank.sets[3].parts;
            }

            // Generate strings
            uint index = DFRandom.rand() % (uint)partsA.Length;
            string stringA = partsA[index];
            Debug.Log("indexA: " + index);

            index = DFRandom.rand() % (uint)partsB.Length;
            string stringB = partsB[index];
            Debug.Log("indexB: " + index);

            return stringA + stringB;
        }

        // Get random first name by gender for names that follow 6 (male), 7 (female) pattern
        string GetExpandedFirstName(NameBank nameBank, Genders gender)
        {
            // Get set parts
            string[] partsA;
            if (gender == Genders.Male)
            {
                partsA = nameBank.sets[6].parts;
            }
            else
            {
                partsA = nameBank.sets[7].parts;
            }

            // Generate strings
            uint index = DFRandom.rand() % (uint)partsA.Length;

            return partsA[index];
        }

        string GetExpandedNordFirstName(NameBank nameBank, Genders gender)
        {
            // Get set parts
            string[] partsA;
            if (gender == Genders.Male)
            {
                partsA = nameBank.sets[4].parts;
            }
            else
            {
                partsA = nameBank.sets[5].parts;
            }

            // Generate strings
            uint index = DFRandom.rand() % (uint)partsA.Length;

            return partsA[index];
        }

        // Gets random surname for names that follow 4+5 pattern
        string GetRandomSurname(NameBank nameBank)
        {
            // Get set parts
            string[] partsA, partsB;
            partsA = nameBank.sets[4].parts;
            partsB = nameBank.sets[5].parts;

            // Generate strings
            uint index = DFRandom.rand() % (uint)partsA.Length;
            string stringA = partsA[index];

            index = DFRandom.rand() % (uint)partsB.Length;
            string stringB = partsB[index];

            return stringA + stringB;
        }

        // Get random surname for those that use the single-word version
        string GetExpandedSurname(NameBank nameBank)
        {
            string[] parts = nameBank.sets[nameBank.sets.Length - 1].parts;
            uint index = DFRandom.rand() % (uint)parts.Length;
            return parts[index];
        }

        // Gets random surname for Nord names that follow 0+1+"sen" pattern
        string GetRandomNordSurname(NameBank nameBank, Genders gender)
        {
            // Get set parts
            string[] partsA, partsB;
            partsA = nameBank.sets[0].parts;
            partsB = nameBank.sets[1].parts;

            // Generate strings
            uint index = DFRandom.rand() % (uint)partsA.Length;
            string stringA = partsA[index];

            index = DFRandom.rand() % (uint)partsB.Length;
            string stringB = partsB[index];

            string stringC = gender == Genders.Male ? "sen" : "dottir";

            if (stringC.Equals("sen") && UnityEngine.Random.Range(0, 5) == 0)
                stringC = "son";

            return stringA + stringB + stringC;
        }

        string GetExpandedNordSurname(NameBank nameBank, Genders gender)
        {
            // Get set parts
            string[] partsA;
            partsA = nameBank.sets[4].parts;

            // Generate strings
            uint index = DFRandom.rand() % (uint)partsA.Length;
            string stringA = partsA[index];

            string stringC = gender == Genders.Male ? "sen" : "dottir";

            if (stringC.Equals("sen") && UnityEngine.Random.Range(0, 5) == 0)
                stringC = "son";

            return stringA + stringC;
        }

        string GetExpandedKhajiitSurname(NameBank nameBank, Genders gender)
        {
            if (gender == (int)Genders.Male) return nameBank.sets[12].parts[UnityEngine.Random.Range(0, nameBank.sets[12].parts.Length)];
            else return nameBank.sets[13].parts[UnityEngine.Random.Range(0, nameBank.sets[13].parts.Length)];
        }

        string GetRandomOrcSurname(NameBank nameBank, Genders gender)
        {
            // Get set parts
            string[] partsA, partsB;
            partsA = nameBank.sets[0].parts;
            partsB = nameBank.sets[1].parts;

            // Generate strings
            uint index = DFRandom.rand() % (uint)partsA.Length;
            string stringA = partsA[index];

            index = DFRandom.rand() % (uint)partsB.Length;
            string stringB = partsB[index];

            string stringC = "gra-";
            string stringD = "gro-";
            string stringE = "gor-";

            if (gender == Genders.Male)
            {
                stringC = "gro-";
                stringD = "gra-";
            }

            int randomRoll = UnityEngine.Random.Range(0, 100);
            if (randomRoll < 85)
                return stringC + stringA + stringB;
            if (randomRoll < 90)
                return stringD + stringA + stringB;
            if (randomRoll < 95)
                return stringE + stringA + stringB;
            return stringA + stringB;
        }

        // Gets random Redguard name which follows 0+1+2+3(75%) (male), 0+1+2+4 (female) pattern
        string GetRandomRedguardName(NameBank nameBank, Genders gender)
        {
            // Get set parts
            string[] partsA, partsB, partsC, partsD;
            if (gender == Genders.Male)
            {
                partsA = nameBank.sets[0].parts;
                partsB = nameBank.sets[1].parts;
                partsC = nameBank.sets[2].parts;
                partsD = nameBank.sets[3].parts;
            }
            else
            {
                partsA = nameBank.sets[0].parts;
                partsB = nameBank.sets[1].parts;
                partsC = nameBank.sets[2].parts;
                partsD = nameBank.sets[4].parts;
            }

            // Generate strings
            uint index = DFRandom.rand() % (uint)partsA.Length;
            string stringA = partsA[index];            

            index = DFRandom.rand() % (uint)partsB.Length;
            string stringB = partsB[index];

            index = DFRandom.rand() % (uint)partsC.Length;
            string stringC = partsC[index];

            string stringD = string.Empty;
            if (gender == Genders.Female || (DFRandom.rand() % 100 < 75))
            {
                index = DFRandom.rand() % (uint)partsD.Length;
                stringD = partsD[index];
            }

            return stringA + stringB + stringC + stringD;
        }

        string GetExpandedKhajiitFirstName(NameBank nameBank, Genders gender)
        {
            // Get set parts
            string[] partsA, partsB;
            string resName;

            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                if (gender == Genders.Male)
                {
                    if (UnityEngine.Random.Range(0, 100) >= 80)
                    {
                        resName = nameBank.sets[6].parts[UnityEngine.Random.Range(0, nameBank.sets[6].parts.Length)];
                        resName = ChangeToFirstUpper(resName);
                    }
                    else
                    {
                        resName = nameBank.sets[6].parts[UnityEngine.Random.Range(0, nameBank.sets[6].parts.Length)] +
                                  nameBank.sets[7].parts[UnityEngine.Random.Range(0, nameBank.sets[7].parts.Length)];
                        resName = ChangeToFirstUpper(resName);
                    }
                }
                else
                {
                    if (UnityEngine.Random.Range(0, 100) >= 50)
                    {
                        resName = nameBank.sets[9].parts[UnityEngine.Random.Range(0, nameBank.sets[9].parts.Length)];
                        resName = ChangeToFirstUpper(resName);
                    }
                    else
                    {
                        resName = nameBank.sets[9].parts[UnityEngine.Random.Range(0, nameBank.sets[9].parts.Length)] +
                                  nameBank.sets[10].parts[UnityEngine.Random.Range(0, nameBank.sets[10].parts.Length)];
                        resName = ChangeToFirstUpper(resName);
                    }
                }
            }
            else
            {
                if (gender == Genders.Male)
                {
                    resName = nameBank.sets[8].parts[UnityEngine.Random.Range(0, nameBank.sets[8].parts.Length)] +
                              nameBank.sets[6].parts[UnityEngine.Random.Range(0, nameBank.sets[6].parts.Length)];
                }
                else
                {
                    resName = nameBank.sets[11].parts[UnityEngine.Random.Range(0, nameBank.sets[11].parts.Length)] +
                              nameBank.sets[9].parts[UnityEngine.Random.Range(0, nameBank.sets[9].parts.Length)];
                }
            }
            return resName;
        }

        // Argonian name can be one word Jel, two words Jel with or without hyphen, Tamrielic
        // cfr. https://en.uesp.net/wiki/Lore:Argonian_Names
        string GetRandomArgonianName(NameBank nameBank, Genders gender)
        {
            string[] partsA, partsB, partsC, partsD;
            string stringA, stringB, stringC, stringD;
            string hyphen;
            string resultName;
            int argNameType = (int)DFRandom.rand() % 3;
            int randomSet;
            uint index;

            switch (argNameType)
            {
                // Single word Jel
                case 0:
                    return GetRandomJelName(nameBank, gender);

                // Two words Jel
                case 1:
                    int twoWordStructure = UnityEngine.Random.Range(0, 4);
                    char[] sB;

                    switch (twoWordStructure)
                    {
                        case 0: // Part + Part
                            randomSet = UnityEngine.Random.Range(0, 1);
                            if (randomSet == 1 && gender == Genders.Female)
                                randomSet++;
                            partsA = nameBank.sets[randomSet].parts;
                            if (gender == Genders.Male)
                                partsB = nameBank.sets[1].parts;
                            else partsB = nameBank.sets[2].parts;

                            stringA = string.Empty;

                            do
                            {
                                index = DFRandom.rand() % (uint)partsA.Length;
                                stringA = partsA[index];
                            }
                            while (stringA.Length < 2);

                            index = DFRandom.rand() % (uint)partsB.Length;
                            stringB = partsB[index];

                            sB = stringB.ToCharArray();
                            sB[0] = char.ToUpper(sB[0]);

                            if (DFRandom.rand() % 100 < 75)
                                hyphen = "-";
                            else hyphen = " ";

                            resultName = stringA + hyphen + new string(sB);
                            return resultName;

                        case 1: // Complete + Part
                            stringA = GetRandomJelName(nameBank, gender);
                            stringB = string.Empty;

                            if (gender == Genders.Male)
                                partsB = nameBank.sets[1].parts;
                            else partsB = nameBank.sets[2].parts;

                            do
                            {
                                index = DFRandom.rand() % (uint)partsB.Length;
                                stringB = partsB[index];
                            }
                            while (stringB.Length < 2);

                            sB = stringB.ToCharArray();
                            sB[0] = char.ToUpper(sB[0]);

                            if (DFRandom.rand() % 100 < 75)
                                hyphen = "-";
                            else hyphen = " ";

                            resultName = stringA + hyphen + new string(sB);
                            return resultName;

                        case 2: // Part + Complete
                            partsA = nameBank.sets[0].parts;
                            do
                            {
                                index = DFRandom.rand() % (uint)partsA.Length;
                                stringA = partsA[index];
                            }
                            while (stringA.Length < 2);

                            stringB = GetRandomJelName(nameBank, gender);

                            if (DFRandom.rand() % 100 < 75)
                                hyphen = "-";
                            else hyphen = " ";

                            resultName = stringA + hyphen + stringB;
                            return resultName;

                        case 3: // Complete + Complete
                            stringA = GetRandomJelName(nameBank, gender);
                            stringB = GetRandomJelName(nameBank, gender);

                            if (DFRandom.rand() % 100 < 75)
                                hyphen = "-";
                            else hyphen = " ";

                            resultName = stringA + hyphen + stringB;
                            return resultName;
                    }
                    break;

                case 2: // Tamrielic
                    int tamrielicStructure = UnityEngine.Random.Range(0, 2);
                    stringA = stringD = string.Empty;
                    if (DFRandom.rand() % 100 < 5)
                        stringA = GetRandomJelName(nameBank, gender);

                    if (tamrielicStructure == 0)    // Verb name
                    {
                        int verbNameStructure = UnityEngine.Random.Range(0, (nameBank.sets[3].parts.Length + nameBank.sets[5].parts.Length));
                        if (verbNameStructure <= nameBank.sets[3].parts.Length) // Prefix + Verb
                        {
                            partsB = nameBank.sets[3].parts;
                            partsC = nameBank.sets[4].parts;

                            stringB = partsB[DFRandom.rand() % (uint)partsB.Length];
                            stringC = partsC[DFRandom.rand() % (uint)partsC.Length];

                            if (gender == Genders.Female && stringB.Contains("He-"))
                                stringB = stringB.Replace("He", "She");
                        }
                        else if (verbNameStructure <= nameBank.sets[5].parts.Length)    // Verb + Suffix
                        {
                            partsB = nameBank.sets[4].parts;
                            partsC = nameBank.sets[5].parts;

                            stringB = partsB[DFRandom.rand() % (uint)partsB.Length];
                            stringC = partsC[DFRandom.rand() % (uint)partsC.Length];

                            if (gender == Genders.Female)
                            {
                                if (stringC.Contains("Him"))
                                    stringC = stringC.Replace("Him", "Her");
                                if (stringC.Contains("His"))
                                    stringC = stringC.Replace("His", "Her");
                            }
                        }
                        else    // Prefix + Verb + Suffix
                        {
                            partsB = nameBank.sets[3].parts;
                            partsC = nameBank.sets[4].parts;
                            partsD = nameBank.sets[5].parts;

                            stringB = partsB[DFRandom.rand() % (uint)partsB.Length];
                            stringC = partsC[DFRandom.rand() % (uint)partsC.Length];
                            stringD = partsD[DFRandom.rand() % (uint)partsD.Length];

                            if (gender == Genders.Female && stringB.Contains("He-"))
                                stringB = stringB.Replace("He", "She");
                            if (gender == Genders.Female)
                            {
                                if (stringD.Contains("Him"))
                                    stringD = stringD.Replace("Him", "Her");
                                if (stringD.Contains("His"))
                                    stringD = stringD.Replace("His", "Her");
                            }
                        }

                        if (stringA != "" && stringD != "")
                            return stringA + " " + stringB + stringC + stringD;
                        else if (stringA != "")
                            return stringA + " " + stringB + stringC;
                        else if (stringD != "")
                            return stringB + stringC + stringD;
                        else return stringB + stringC;
                    }
                    else
                    {
                        partsB = nameBank.sets[6].parts;
                        partsC = nameBank.sets[7].parts;

                        stringB = partsB[DFRandom.rand() % (uint)partsB.Length];
                        stringC = partsC[DFRandom.rand() % (uint)partsC.Length];

                        if (stringA != "")
                            return stringA + " " + stringB + "-" + stringC;
                        else return stringB + "-" + stringC;
                    }
            }

            return string.Empty;
        }

        string GetRandomJelName(NameBank nameBank, Genders gender)
        {
            string[] partsA, partsB, partsC, partsD;
            string resultName;

            partsA = nameBank.sets[0].parts;
            if (gender == Genders.Male)
                partsB = nameBank.sets[1].parts;
            else partsB = nameBank.sets[2].parts;

            do
            {
                uint index = DFRandom.rand() % (uint)partsA.Length;
                string stringA = partsA[index];

                index = DFRandom.rand() % (uint)partsB.Length;
                string stringB = partsB[index];

                resultName = stringA + stringB;
            }
            while (resultName.Length < 3);

            return resultName;
        }

        string GetExpandedArgonianName(NameBank nameBank, Genders gender)
        {
            if (gender == Genders.Male)
                return nameBank.sets[8].parts[UnityEngine.Random.Range(0, nameBank.sets[8].parts.Length)];
            else return nameBank.sets[9].parts[UnityEngine.Random.Range(0, nameBank.sets[9].parts.Length)];
        }

        // Get random monster name.
        // Monster1: 0+(50% +1)+2
        // Monster2: 0+(50% +1)+2+(if female, +3)
        // Monster3: (if male, 25% +3 + " ")+0+1+2
        string GetRandomMonsterName(Genders gender)
        {
            BankTypes type = (BankTypes)UnityEngine.Random.Range(8, 9 + 1); // Get random Monster1 or Monster2 for now.
            NameBank nameBank = bankDict[type];

            // Get set parts
            string[] partsA, partsB, partsC, partsD;
            partsA = nameBank.sets[0].parts;
            partsB = nameBank.sets[1].parts;
            partsC = nameBank.sets[2].parts;
            partsD = null;

            string stringA = string.Empty;
            string stringB = string.Empty;
            string stringC = string.Empty;
            string stringD = string.Empty;

            uint index = 0;

            // Additional set for Monster2 and Monster3
            if (nameBank.sets.Length >= 4)
                partsD = nameBank.sets[3].parts;

            // Generate strings
            if (type != BankTypes.Monster3) // Monster1 or Monster2
            {
                index = DFRandom.rand() % (uint)partsA.Length;
                stringA = partsA[index];

                stringB = string.Empty;
                if (DFRandom.rand() % 50 < 25)
                {
                    index = DFRandom.rand() % (uint)partsB.Length;
                    stringB = partsB[index];
                }

                index = DFRandom.rand() % (uint)partsC.Length;
                stringC = partsC[index];

                // Additional set for Monster2 female
                if (partsD != null && gender == Genders.Female)
                {
                    index = DFRandom.rand() % (uint)partsD.Length;
                    stringD = partsD[index];
                }
            }
            else // Monster3
            {
                if (gender == Genders.Female || DFRandom.rand() % 100 >= 25)
                {
                    index = DFRandom.rand() % (uint)partsA.Length;
                    stringA = partsA[index];
                }
                else
                {
                    index = DFRandom.rand() % (uint)partsD.Length;
                    stringA = partsD[index] + " ";

                    index = DFRandom.rand() % (uint)partsA.Length;
                    stringB = partsA[index];
                }

                index = DFRandom.rand() % (uint)partsB.Length;
                stringC = partsB[index];

                index = DFRandom.rand() % (uint)partsC.Length;
                stringD = partsC[index];
            }

            return stringA + stringB + stringC + stringD;
        }

        string ChangeToFirstUpper(string nameToChange)
        {
            return string.Concat(nameToChange[0].ToString().ToUpper(), nameToChange.Substring(1));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads namegen data from JSON file.
        /// </summary>
        void LoadNameGenData()
        {
            try
            {
                bankDict = JsonConvert.DeserializeObject<Dictionary<BankTypes, NameBank>>(File.ReadAllText(Path.Combine(mapPath, nameGenFilename)));
            }
            catch
            {
                Debug.Log("Could not load NameGen database from Resources. Check file exists and is in correct format.");
            }
        }

        #endregion
    }
}