/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using System.Globalization;

namespace CoachAiEngine {

    internal static class FloatUtils {

        /// <summary>
        /// <code>float.Parse</code> will use the user's locale for parsing. ie. on a german
        /// phone 1.6 will be parsed as 16 instead of as 1.6. ParseSane does it right.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal static float ParseSane(string str) {
            return float.Parse(str, CultureInfo.InvariantCulture.NumberFormat);
        }

        internal static bool TryParseSane(string str, out float number) {
            return float.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out number);
        }
    }
}
