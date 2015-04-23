using System;
using System.Collections.Generic;
using System.Linq;

namespace LivePercentiles.Tests.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var random = new Random();
            var buffer = source.ToList();
            for (var i = 0; i < buffer.Count; i++)
            {
                var randomIndex = random.Next(i, buffer.Count);
                yield return buffer[randomIndex];

                buffer[randomIndex] = buffer[i];
            }
        } 
    }
}