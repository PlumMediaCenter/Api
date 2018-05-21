using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using System;
using PlumMediaCenter.Graphql.InputGraphTypes;
using PlumMediaCenter.Graphql.GraphTypes;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;
using System.Collections.Generic;
using PlumMediaCenter.Business.MetadataProcessing;

namespace PlumMediaCenter.Graphql.Mutations
{
    public class MovieMetadataMutations
    {
        public MovieMetadataMutations(
            MovieMetadataProcessor movieMetadataProcessor
        )
        {
            this.MovieMetadataProcessor = movieMetadataProcessor;
        }
        MovieMetadataProcessor MovieMetadataProcessor;

        public void Register(RootMutationGraphType mutation)
        {
            mutation.Field<BooleanGraphType>().Name("saveMovieMetadata")
                .Description("Update the metadata file and image folders for the specified movie. This also updates the database with the latest metadata.")
                .Argument<NonNullGraphType<IntGraphType>>("movieId", "The id of the movie to have its metadata updated")
                .Argument<NonNullGraphType<MovieMetadataInputGraphType>>("metadata", "The metadata to use for this movie")
                .ResolveAsync(async (ctx) =>
                {
                    var movieId = ctx.GetArgument<int>("movieId");
                    var metadata = ctx.GetArgument<MovieMetadata>("metadata");
                    await MovieMetadataProcessor.SaveAsync(movieId, metadata);
                    return true;
                }
            );
        }
    }
}