using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace PlumMediaCenter.Business
{
    public static class Extensions
    {

        public static IEnumerable<string> AddIfMissing(this IEnumerable<string> items, IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                if (items.Contains(value) == false)
                {
                    items = items.Append(value);
                }
            }
            return items;
        }
        /// <summary>
        /// Add a column to the ienumerable if it is missing
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static IEnumerable<string> AddIfMissing(this IEnumerable<string> items, string value)
        {
            return items.AddIfMissing(new[] { value });
        }

        public static List<string> AddIfMissing(this List<string> items, IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                if (items.Contains(value) == false)
                {
                    items.Add(value);
                }
            }
            return items;
        }


        public static List<string> AddIfMissing(this List<string> items, string value)
        {
            return items.AddIfMissing(new[] { value });
        }


        public static IEnumerable<string> RemoveIfPresent(this IEnumerable<string> items, IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                if (items.Contains(value))
                {
                    items = items.Where(x => x != value);
                }
            }
            return items;
        }

        public static IEnumerable<string> RemoveIfPresent(this IEnumerable<string> items, string value)
        {
            return items.RemoveIfPresent(new[] { value });
        }

        public static List<string> RemoveIfPresent(this List<string> items, IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                if (items.Contains(value))
                {
                    items.Remove(value);
                }
            }
            return items;
        }

        public static List<string> RemoveIfPresent(this List<string> items, string value)
        {
            return items.RemoveIfPresent(new[] { value });
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

        // public static IDictionary<string, Field> GetSubFields<ContextType>(this ResolveFieldContext<ContextType> context, IGraphType graphType)
        // {
        //     //  public static Dictionary<string, Field> CollectFields(
        //     //             ExecutionContext context,
        //     //             IGraphType specificType,
        //     //             SelectionSet selectionSet,
        //     //             Dictionary<string, Field> fields,
        //     //             List<string> visitedFragmentNames)
        //     //         {
        //     var fields = new Dictionary<string, Field>();
        //     // return ExecutionHelper.CollectFields(context, graphType, context.FieldAst.SelectionSet, fields, new List<string>());
        //     return new Dictionary<string, Field>();
        // }

    }
}