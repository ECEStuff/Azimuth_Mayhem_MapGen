using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azimuth
{
    public static class Extensions
    {
        public static List<List<T>> ToList<T>(this T[,] array)
        {
            var result = new List<List<T>>();
            var lengthX = array.GetLength(0);
            var lengthY = array.GetLength(1);

            // the reason why we get lengths of dimensions before looping through
            // is because we would like to use `List<T>(int length)` overload
            // this will prevent constant resizing of its underlying array and improve performance
            for (int i = 0; i < lengthX;  i++)
            {
                var listToAdd = new List<T>(lengthY);

                for (int i2 = 0; i2 < lengthY; i2++)
                {
                    listToAdd.Add(array[i, i2]);
                }

                result.Add(listToAdd);
            }

            return result;
        }

        public static bool Between(this int num, int lower, int upper, bool inclusive = false)
        {
            if (lower < upper)
            {
                return inclusive
                    ? lower <= num && num <= upper
                    : lower < num && num < upper;
            }
            else
            {
                return inclusive
                    ? upper <= num && num <= lower
                    : upper < num && num < lower;
            }
        }

        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }


        /// <summary>
        /// Provides an enumerable list of strings for an Enum, so that you can bind an enum to a combobox or other element that needs the list.
        /// </summary>
        public class EnumList
        {
            public static IEnumerable<KeyValuePair<T, string>> Of<T>()
            {
                return Enum.GetValues(typeof(T))
                    .Cast<T>()
                    .Select(p => new KeyValuePair<T, string>(p, p.ToString()))
                    .ToList();
            }
        }
    }
}
