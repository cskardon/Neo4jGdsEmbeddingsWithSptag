namespace SptagTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Extensions
    {
        public static T[] Concat<T>(this IList<T[]> arrays)
        {
            var size = arrays.Sum(e => e.Length);
            var output = new T[size];

            var insertionPoint = 0;
            foreach (var array in arrays)
            {
                Array.Copy(array, 0, output, insertionPoint, array.Length);
                insertionPoint += array.Length;
            }

            return output;
        }
    }
}