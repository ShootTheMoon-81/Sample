using System;
using System.Collections.Generic;

public static class GenericExtensions
{
    private static Random random = new Random();

    public static void AddSorted<TSource, T>(this List<TSource> list, TSource item, Func<TSource, T> selector) where T : IComparable
    {
        int min = 0;
        int max = list.Count;

        while (max - min >= 1)
        {
            int center = (min + max) / 2;
            int compare = selector(list[center]).CompareTo(selector(item));

            if (compare > 0)
                max = center;
            else if (compare <= 0)
                min = center + 1;
        }

        list.Insert(max, item);
    }

    public static void Shuffle<T>(this List<T> list)
    {
        for (int i = list.Count - 1; i >= 0; --i)
        {
            int j = random.Next(i + 1);
            T temp = list[j];

            list[j] = list[i];
            list[i] = temp;
        }
    }

    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    {
        TValue value;

        return dictionary.TryGetValue(key, out value) ? value : defaultValue;
    }
}
