namespace DaggerfallWorkshop.Game.Items
{
    public class ItemListsNStuff
    {
        public enum DyeTargetCombinations
        {
            Basic,              // DyeTargets = 0
            Steel,              // DyeTargets = 2
            Yellow,             // DyeTargets = 3
            Leather,            // DyeTargets = 4
            Red,                // DyeTargets = 5
            Black,              // DyeTargets = 6
            Green,              // DyeTargets = 7
            LightBrown,         // DyeTargets = 8
            Purple,             // DyeTargets = 9
            DarkBrown,          // DyeTargets = 10
            Adamantium,         // DyeTargets = 11
            White,              // DyeTargets = 12
            Basic_Steel,        // Coefficient = 2
            Basic_Yellow,       // Coefficient = 3
            Yellow_Basic,       // Coefficient = 3
            Basic_Red,          // Coefficient = 5
            Basic_Black,        // Coefficient = 6
            Basic_Green,        // Coefficient = 7
            Leather_Yellow,     // Coefficient = 7
            Basic_LightBrown,   // Coefficient = 8
            Black_Yellow,       // Coefficient = 9
            Basic_DarkBrown,    // Coefficient = 10
            LightBrown_Steel,   // Coefficient = 10
            Basic_White,        // Coefficient = 12
            Adamantium_Yellow,  // Coefficient = 14
            LightBrown_White,   // Coefficient = 20
            DarkBrown_LightBrown,       // Coefficient = 18
            Basic_Steel_Yellow,         // Coefficient = 5
            Basic_Red_Yellow,   // Coefficient = 8
            Yellow_Basic_Red,   // Coefficient = 8
            Basic_Yellow_Black, // Coefficient = 9
            Basic_Black_Yellow, // Coefficient = 9
            Basic_Steel_LightBrown,     // Coefficient = 10
            Basic_Yellow_LightBrown,    // Coefficient = 11
            Basic_LightBrown_Yellow,    // Coefficient = 11
            Basic_Green_Red,            // Coefficient = 12
            Basic_Red_LightBrown,       // Coefficient = 13
            Basic_Black_LightBrown,     // Coefficient = 14
            Basic_Yellow_White,         // Coefficient = 15
            Basic_Purple_Black,         // Coefficient = 15
            Basic_DarkBrown_LightBrown, // Coefficient = 18
            Basic_White_LightBrown,     // Coefficient = 20
            Red_Purple_Black,           // Coefficient = 20
            DarkBrown_LightBrown_Yellow,    // Coefficient = 21
            Basic_Steel_Yellow_LightBrown,  // Coefficient = 13
            Basic_Steel_LightBrown_Yellow,  // Coefficient = 13
            Basic_Red_LightBrown_Yellow,    // Coefficient = 16
            Basic_Yellow_Black_LightBrown,  // Coefficient = 17
            Basic_Black_Yellow_LightBrown,  // Coefficient = 17
            Basic_Black_LightBrown_Yellow,  // Coefficient = 17
            Basic_Green_LightBrown_Yellow,  // Coefficient = 18
            Basic_LightBrown_Yellow_Green,  // Coefficient = 18
            Green_Basic_LightBrown_Yellow,  // Coefficient = 18
            Basic_DarkBrown_Yellow_LightBrown,  // Coefficient = 21
            Basic_DarkBrown_LightBrown_Yellow,  // Coefficient = 21
            Basic_White_LightBrown_Yellow,  // Coefficient = 23
            Variable                        // Dependant on item variant
        }

        public static int[][] DyeTargetElements = new int[][] {
            new int[] { 153, 154, 162, 163, 164, 173, 182, 184, 191, 199, 200, 201, 210, 
                        369, 370, 374, 375, 376, 377, 378, 380, 381, 392, 404, 405, 406, 408, 417, 420, 422, 424, 426,
                        471, 472, 476, 477, 478, 479, 480, 482, 483, 494, 506, 507, 508, 510, 519, 522, 524, 526, 528, },       // Basic (blue)
            new int[] {},       // Steel
            new int[] { 142, 144, 146, 185, },       // Yellow
            new int[] { 147, 148, 149, 151, 186, 187, 188, },       // Leather
            new int[] { 145, 150, 189, },       // Red
            new int[] { 156, 193, },       // Black
            new int[] {},       // Green
            new int[] { 152, 409, 412, 413, 511, 514, 515, },       // LightBrown
            new int[] {},       // Purple
            new int[] {},       // DarkBrown
            new int[] {},       // Adamantium
            new int[] {},       // White
            new int[] {},       // Basic_Steel
            new int[] { 159, 371, 372, 373, 379, 411, 416, 473, 474, 475, 513, 481, 518, },       // Basic_Yellow
            new int[] {},       // Yellow_Basic
            new int[] { 160, 174, 211, },       // Basic_Red
            new int[] { 407, 509, },                       // Basic_Black
            new int[] {},       // Basic_Green
            new int[] { 177, 181, },       // Leather_Yellow
            new int[] { 155, 157, 192, 333, 335, 348, 349, 352, 353, 437, 435, 450, 451, 454, 455, },       // Basic_LightBrown
            new int[] {},       // Black_Yellow
            new int[] {},       // Basic_DarkBrown
            new int[] { 414, 415, 516, 517, },        // LightBrown_Steel
            new int[] { 382, 410, 418, 421, 423, 425, 484, 512, 520, 523, 525, 527, },       // Basic_White
            new int[] { 180, },       // Adamantium_Yellow
            new int[] {},       // LightBrown_White
            new int[] { 419, 521, },    // DarkBrown_LightBrown
            new int[] {},       // Basic_Steel_Yellow
            new int[] {},       // Basic_Red_Yellow
            new int[] { 161, },       // Yellow_Basic_Red
            new int[] { 383, 485, },       // Basic_Yellow_Black
            new int[] { 393, 394, 403, 495, 496, 505, },       // Basic_Black_Yellow
            new int[] {},       // Basic_Steel_LightBrown
            new int[] {},       // Basic_Yellow_LightBrown
            new int[] { 175, 178, 179, 197, 198, 212, 214, 215, },       // Basic_LightBrown_Yellow
            new int[] {},       // Basic_Green_Red
            new int[] {},       // Basic_Red_LightBrown
            new int[] { 330, 331, 432, 433, },       // Basic_Black_LightBrown
            new int[] { 362, 363, 464, 465, },       // Basic_Yellow_White
            new int[] {},       // Basic_Purple_Black
            new int[] {},       // Basic_DarkBrown_LightBrown
            new int[] { 332, 434, 334, 436, },       // Basic_White_LightBrown
            new int[] {},       // Red_Purple_Black
            new int[] { 143, },       // DarkBrown_LightBrown_Yellow
            new int[] {},       // Basic_Steel_Yellow_LightBrown
            new int[] {},       // Basic_Steel_LightBrown_Yellow
            new int[] {},       // Basic_Red_LightBrown_Yellow
            new int[] {},       // Basic_Yellow_Black_LightBrown
            new int[] { 396, 498, },       // Basic_Black_Yellow_LightBrown
            new int[] {},       // Basic_Black_LightBrown_Yellow
            new int[] {},       // Basic_Green_LightBrown_Yellow
            new int[] {},       // Basic_LightBrown_Yellow_Green
            new int[] { 176, },       // Green_Basic_LightBrown_Yellow
            new int[] { 397, 499, },       // Basic_DarkBrown_Yellow_LightBrown
            new int[] {},       // Basic_DarkBrown_LightBrown_Yellow
            new int[] {},       // Basic_White_LightBrown_Yellow
            new int[] { 141, 158, 165, 166, 167, 168, 169, 170, 171, 172, 183, 190, 194, 195, 196, 202, 203, 204, 205, 206, 207, 208, 209, 213, 216, 316, 317, 318, 319, 320, 321, 322, 323, 324, 
                        325, 326, 327, 328, 329, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 350, 351, 354, 355, 356, 357, 358, 359, 360, 361, 364, 365, 366, 367, 368, 384, 385, 386, 387, 388, 389, 390, 391, 395, 398, 399, 400, 401, 402,
                        427, 428, 429, 430, 431, 438, 439, 440, 441, 442, 443, 444, 445, 446, 447, 448, 449, 452, 453, 456, 457, 458, 459, 460, 461, 462, 463, 466, 467, 468, 469, 470, 486, 487, 488, 489, 490, 491, 492, 493, 497, 500, 501, 502, 503, 504, }         // Variable
        };

        public static DyeTargets[][] DyeCombArrays = new DyeTargets[][] {
            new DyeTargets[] { DyeTargets.BasicClothing },
            new DyeTargets[] { DyeTargets.SteelClothing },
            new DyeTargets[] { DyeTargets.YellowClothing},
            new DyeTargets[] { DyeTargets.LeatherClothing },
            new DyeTargets[] { DyeTargets.RedClothing },
            new DyeTargets[] { DyeTargets.BlackClothing },
            new DyeTargets[] { DyeTargets.GreenClothing },
            new DyeTargets[] { DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.PurpleClothing },
            new DyeTargets[] { DyeTargets.DarkBrownClothing },
            new DyeTargets[] { DyeTargets.AdamantiumClothing },
            new DyeTargets[] { DyeTargets.WhiteClothing},
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.SteelClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.YellowClothing, DyeTargets.BasicClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.RedClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.BlackClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.GreenClothing },
            new DyeTargets[] { DyeTargets.LeatherClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.BlackClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.DarkBrownClothing },
            new DyeTargets[] { DyeTargets.LightBrownClothing, DyeTargets.SteelClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.WhiteClothing },
            new DyeTargets[] { DyeTargets.AdamantiumClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.LightBrownClothing, DyeTargets.WhiteClothing },
            new DyeTargets[] { DyeTargets.DarkBrownClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.SteelClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.RedClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.YellowClothing, DyeTargets.BasicClothing, DyeTargets.RedClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.YellowClothing, DyeTargets.BlackClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.BlackClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.SteelClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.YellowClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.LightBrownClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.GreenClothing, DyeTargets.RedClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.RedClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.BlackClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.YellowClothing, DyeTargets.WhiteClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.PurpleClothing, DyeTargets.BlackClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.DarkBrownClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.WhiteClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.RedClothing, DyeTargets.PurpleClothing, DyeTargets.BlackClothing },
            new DyeTargets[] { DyeTargets.DarkBrownClothing, DyeTargets.LightBrownClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.SteelClothing, DyeTargets.YellowClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.SteelClothing, DyeTargets.LightBrownClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.RedClothing, DyeTargets.LightBrownClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.YellowClothing, DyeTargets.BlackClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.BlackClothing, DyeTargets.YellowClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.BlackClothing, DyeTargets.LightBrownClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.GreenClothing, DyeTargets.LightBrownClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.LightBrownClothing, DyeTargets.YellowClothing, DyeTargets.GreenClothing },
            new DyeTargets[] { DyeTargets.GreenClothing, DyeTargets.BasicClothing, DyeTargets.LightBrownClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.DarkBrownClothing, DyeTargets.YellowClothing, DyeTargets.LightBrownClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.DarkBrownClothing, DyeTargets.LightBrownClothing, DyeTargets.YellowClothing },
            new DyeTargets[] { DyeTargets.BasicClothing, DyeTargets.WhiteClothing, DyeTargets.LightBrownClothing, DyeTargets.YellowClothing },
        };
    }
}