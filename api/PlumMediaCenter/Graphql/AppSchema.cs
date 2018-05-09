using GraphQL;
using GraphQL.Types;
using PlumMediaCenter.Graphql;
using PlumMediaCenter.Graphql.GraphTypes;

namespace PlumMediaCenter.Graphql
{
    public class AppSchema : Schema
    {
        public AppSchema(RootQueryGraphType query, FuncDependencyResolver resolver, RootMutationGraphType mutation)
        {
            this.Query = query;
            this.Mutation = mutation;
            this.DependencyResolver = resolver;
        }
    }
}