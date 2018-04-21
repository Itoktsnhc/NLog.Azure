using System;
using System.Collections.Generic;

namespace NLog.Azure
{
    public static class Helper
    {
        public static IEnumerable<List<T>> SplitList<T>(List<T> locations, Int32 nSize = 30)
        {
            for (var i = 0; i < locations.Count; i += nSize)
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
        }
    }
}