using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.MetadataProcessing;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Graphql.GraphTypes;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql
{
    public class RootQueryGraphType : ObjectGraphType
    {
        public RootQueryGraphType(
            MovieRepository movieRepository,
            SourceRepository sourceRepository,
            LibraryGenerator libraryGenerator,
            MovieMetadataProcessor movieMetadataProcessor,
            MediaRepository mediaRepository,
            UserRepository userRepository,
            MovieGraphType movieGraphType,
            DatabaseGraphType databaseGraphType
        )
        {
            this.Name = "Query";
            databaseGraphType.Register(this);

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

            Field<ListGraphType<SourceGraphType>>()
                .Name("sources")
                .Description("The sources of media for this library")
                .ResolveAsync(async (ctx) =>
                {
                    var results = await sourceRepository.GetAll();
                    return results;
                });

            Field<LibraryGeneratorStatusGraphType>()
                .Name("libraryGeneratorStatus")
                .Description("The status of the library generator")
                .Resolve((ResolveFieldContext<object> ctx) =>
                {
                    return libraryGenerator.GetStatus();
                });

            Field<ListGraphType<MovieMetadataSearchResultGraphType>>()
                .Name("movieMetadataSearchResults")
                .Description("A list of TMDB movie search results")
                .Argument<StringGraphType>("searchText", "The text to use to search for results")
                .ResolveAsync(async (ctx) =>
                {
                    var searchText = ctx.GetArgument<string>("searchText");
                    return await movieMetadataProcessor.GetSearchResultsAsync(searchText);
                });

            Field<MovieMetadataComparisonGraphType>()
                .Name("movieMetadataComparison")
                .Description("A comparison of a metadata search result to what is currently in the system")
                .Argument<IntGraphType>("tmdbId", "The TMDB of the incoming movie")
                .Argument<IntGraphType>("movieId", "The id of the current movie in the system")
                .ResolveAsync(async (ctx) =>
                {
                    var tmdbId = ctx.GetArgument<int>("tmdbId");
                    var movieId = ctx.GetArgument<int>("movieId");
                    return await movieMetadataProcessor.GetComparisonAsync(tmdbId, movieId);
                });

            Field<ListGraphType<MediaHistoryRecordGraphType>>().Name("mediaHistory")
                .Description("A list of media items consumed and their current progress and duration of viewing")
                .ResolveAsync(async (ctx) =>
                {
                    return await mediaRepository.GetHistory(userRepository.CurrentProfileId);
                });

            Field<ListGraphType<MediaItemGraphType>>().Name("mediaItems")
                .Description("A list of media items. This is a union graph type, so you must specify inline fragments ")
                .Argument<ListGraphType<IntGraphType>>("mediaItemIds", "A list of mediaItem IDs")
                .ResolveAsync(async (ctx) =>
                {
                    var mediaItemIds = ctx.GetArgument<IEnumerable<int>>("mediaItemIds");
                    return await mediaRepository.GetMediaItems(mediaItemIds);
                });

        }

    }
}
