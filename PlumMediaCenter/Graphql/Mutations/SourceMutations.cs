using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using System;
using PlumMediaCenter.Graphql.InputGraphTypes;
using PlumMediaCenter.Graphql.GraphTypes;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;
using System.Collections.Generic;

namespace PlumMediaCenter.Graphql.Mutations
{
    public class SourceMutations
    {
        public SourceMutations(
            SourceRepository sourceRepository
        )
        {
            this.SourceRepository = sourceRepository;
        }
        SourceRepository SourceRepository;

        public void Register(RootMutationGraphType mutation)
        {
            mutation.Field<ListGraphType<SourceGraphType>>()
                .Name("setAllSources")
                .Description("Replace the entire list of sources with the specified list")
                .Argument<NonNullGraphType<ListGraphType<SourceInputGraphType>>>("sources", "The list of all sources")
                .ResolveAsync(async (ctx) =>
                {
                    var sources = ctx.GetArgument<IEnumerable<Source>>("sources");
                    await SourceRepository.SetAll(sources);
                    return await SourceRepository.GetAll();
                }
            );
        }
    }
}