using System.Collections.Generic;
using GraphQL.DataLoader;
using GraphQL.Types;
using PlumMediaCenter.Attributues;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class LibraryGeneratorStatusGraphType : ObjectGraphType<Business.LibraryGeneratorStatus>
    {
        public LibraryGeneratorStatusGraphType(
             IDataLoaderContextAccessor dlca,
             UserRepository userRepository
        )
        {
            Field<ListGraphType<StringGraphType>>().Name("activeFiles")
                .Description("The list of items currently being processed")
                .Resolve((ctx) =>
                {
                    return ctx.Source.ActiveFiles;
                });
            Field(x => x.CountCompleted).Description("The number of items that have already been processed during the current library generation cycle");
            Field(x => x.CountRemaining).Description("The number of items that have not yet been fully processed during the current library generation cycle");
            Field(x => x.CountTotal).Description("The total number of items to be processed during the current library generation cycle");
            Field<PrettyErrorGraphType>().Name("error")
                .Description("The error encountered during the current library generation cycle. This will be null if no errors were encountered")
                .Resolve((ctx) =>
                {
                    if (ctx.Source.Error != null)
                    {
                        return new PrettyError(ctx.Source.Error);
                    }
                    else
                    {
                        return null;
                    }
                });
            Field<ListGraphType<StringGraphType>>().Name("failedItems")
                .Description("The list of failed items encountered during the current library generation cycle")
                .Resolve((ctx) =>
                {
                    return ctx.Source.FailedItems;
                });
            Field(x => x.IsProcessing).Description("Indicates whether the library is currently processing");
            Field(x => x.LastGeneratedDate, nullable: true).Description("The date of the last time the library was generated");
            Field<ListGraphType<StringGraphType>>().Name("log")
                .Description("The list of items currently being processed")
                .Resolve((ctx) =>
                {
                    return ctx.Source.Log;
                });
            Field(x => x.SecondsRemaining).Description("An estimate of how many seconds are remaining until the current library generation cycle completes");
            Field(x => x.StartTime, nullable: true).Description("The time the library generator started its current library generation cycle");
            Field(x => x.State).Description("The current state of the library generator");
            Field<ListGraphType<MediaTypeCountGraphType>>().Name("mediaTypeCounts")
                .Description("The list of completed and total counts for all media types")
                .Resolve((ctx) =>
                {
                    return ctx.Source.MediaTypeCounts;
                });
        }
    }
}
