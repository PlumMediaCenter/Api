
using GraphQL.Types;

namespace PlumMediaCenter.Graphql
{
    public static class ResolveFieldContextExtensions
    {
        /// <summary>
        /// Generate a key based on the prefix and the list of subfields that can be used 
        /// to identify similar data in the data loader
        /// </summary>
        /// <param name="context"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string GetDataLoaderKey<T>(this ResolveFieldContext<T> context, string prefix)
        {
            return $"{prefix}-{string.Join(",", context.SubFields.Keys)}";
        }
    }
}