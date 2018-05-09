using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;

namespace PlumMediaCenter.Business
{
    public static class Extensions
    {

        /// <summary>
        /// Add a column to the ienumerable if it is missing
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static IEnumerable<string> AddIfMissing(this IEnumerable<string> items, string value)
        {
            if (items.Contains(value) == false)
            {
                items = items.Append(value);
            }
            return items;
        }

        public static List<string> AddIfMissing(this List<string> items, string value)
        {
            if (items.Contains(value) == false)
            {
                items.Add(value);
            }
            return items;
        }

        public static IEnumerable<string> RemoveIfPresent(this IEnumerable<string> items, string value)
        {
            if (items.Contains(value))
            {
                items = items.Where(x => x != value);
            }
            return items;
        }

        public static List<string> RemoveIfPresent(this List<string> items, string value)
        {
            if (items.Contains(value))
            {
                items.Remove(value);
            }
            return items;
        }

        public static Dictionary<TKey, TValue> AddIfMissing<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key) == false)
            {
                dictionary.Add(key, value);
            }
            return dictionary;
        }

        public static IEnumerable<IEnumerable<T>> Bucketize<T>(this IEnumerable<T> items, int bucketSize)
        {
            var enumerator = items.GetEnumerator();
            while (enumerator.MoveNext())
                yield return GetNextBucket(enumerator, bucketSize);
        }

        private static IEnumerable<T> GetNextBucket<T>(IEnumerator<T> enumerator, int maxItems)
        {
            int count = 0;
            do
            {
                yield return enumerator.Current;

                count++;
                if (count == maxItems)
                    yield break;

            } while (enumerator.MoveNext());
        }

        /// <summary>
        /// Get an argument from the context, or a default value
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argumentName"></param>
        /// <returns></returns>
        public static ArgumentType GetArgumentOrDefault<ContextType, ArgumentType>(this ResolveFieldContext<ContextType> context, string argumentName, ArgumentType defaultValue = default(ArgumentType))
        {
            if (context.Arguments.ContainsKey(argumentName) && context.Arguments[argumentName] != null)
            {
                return context.GetArgument<ArgumentType>(argumentName);
            }
            else
            {
                return defaultValue;
            }
        }
    }
}