using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using System;
using PlumMediaCenter.Graphql.InputGraphTypes;
using PlumMediaCenter.Graphql.GraphTypes;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;
using System.Collections.Generic;
using PlumMediaCenter.Business;

namespace PlumMediaCenter.Graphql.Mutations
{
    public class LibraryMutations
    {
        public LibraryMutations(
            LibraryGenerator libraryGenerator,
            AppSettings appSettings
        )
        {
            this.LibraryGenerator = libraryGenerator;
            this.AppSettings = appSettings;
        }
        LibraryGenerator LibraryGenerator;
        AppSettings AppSettings;

        public void Register(RootMutationGraphType mutation)
        {
            mutation.Field<ListGraphType<IntGraphType>>().Name("processItems")
                .Argument<ListGraphType<IntGraphType>>("ids", "The ids of the media items to process")
                .ResolveAsync(async (ctx) =>
                {
                    var mediaItemIds = ctx.GetArgument<IEnumerable<int>>("ids");
                    await LibraryGenerator.ProcessItems(mediaItemIds);
                    return mediaItemIds;
                });

            mutation.Field<LibraryGeneratorStatusGraphType>()
                .Name("generateLibrary")
                .Description("Generate the library. ")
                .Resolve((ctx) =>
                {
                    var initialStatus = LibraryGenerator.GetStatus();
                    //temporarily delete all movies
                    //Data.ConnectionManager.GetConnection().Execute("truncate movies");
                    var baseUrl = AppSettings.GetBaseUrl();
                    var generateOnSeparateThreadTask = Task.Run(() =>
                    {
                        var libGenTask = LibraryGenerator.Generate(baseUrl);
                    });

                    var startDate = DateTime.UtcNow;
                    //spin until we get a new status
                    var status = LibraryGenerator.GetStatus();
                    while (status == null || status == initialStatus)
                    {
                        status = LibraryGenerator.GetStatus();
                        var time = DateTime.UtcNow - startDate;
                        if (time.TotalSeconds > 20)
                        {
                            throw new Exception("Generator status hasn't changed within the expected time");
                        }
                    }
                    return status;
                }
            );
        }
    }
}