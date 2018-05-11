using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Graphql.GraphTypes;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql
{
    public class RootQueryGraphType : ObjectGraphType
    {
        public RootQueryGraphType(
            MovieRepository movieRepository
        )
        {
            Field<ListGraphType<MovieGraphType>, IEnumerable<Movie>>()
                .Name("movies")
                .Description("A list of movies")
                .Argument<ListGraphType<IntGraphType>>("ids", "A list of ids of the polls to fetch")
                .Argument<IntGraphType>("top", "Pick the top N results")
                .Argument<IntGraphType>("skip", "skip the first N results")
                .ResolveAsync(async (ctx) =>
                {
                    var filters = movieRepository.GetArgumentFilters(ctx);
                    var results = await movieRepository.Query(filters, ctx.SubFields.Keys);
                    return results;
                });
        }

    }
}
