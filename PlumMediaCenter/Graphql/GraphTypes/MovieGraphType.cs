using System.Collections.Generic;
using GraphQL.DataLoader;
using GraphQL.Types;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class MovieGraphType : ObjectGraphType<Movie>
    {
        public MovieGraphType(
            IDataLoaderContextAccessor dlca,
            MediaItemRepository mediaItemRepository,
            UserRepository userRepository,
            IDataLoaderContextAccessor dla
        )
        {
            this.Name = "Movie";
            Field(x => x.Id).Description("The ID of the video.");
            Field(x => x.Title).Description("The formal title of the movie. This is also known as the movie's name.");
            Field(x => x.SortTitle).Description("The title used to sort this movie.");
            Field(x => x.ShortSummary, nullable: true).Description("A short summary of the movie. This is shorter than the standard summary field.");
            Field(x => x.Summary, nullable: true).Description("The summary of the movie.");
            Field(x => x.Rating, nullable: true).Description("This is the MPAA rating for the video (i.e. 'PG', 'R', etc...");
            Field(x => x.ReleaseYear, nullable: true).Description("The year this movie was first released.");
            Field(x => x.RuntimeSeconds, nullable: true).Description("How long the movie is, in seconds.");
            Field(x => x.TmdbId, nullable: true).Description("TMDB (The Movie DataBase) ID for this movie.");
            Field(x => x.SourceId, nullable: true).Description("The Source ID for this movie.");
            Field(x => x.VideoUrl).Description("The full url to the video file. This is the file that will be used when streaming");
            Field<MediaTypeEnumGraphType>().Name("mediaType")
                .Description("The media type for this movie. Will always be the same value since all movies have the same media type")
                .Resolve(x => x.Source.MediaType);

            Field<ListGraphType<StringGraphType>>("posterUrls", resolve: (context) =>
            {
                return context.Source.PosterUrls;
            });

            Field<ListGraphType<StringGraphType>>("backdropUrls", resolve: (context) =>
            {
                return context.Source.BackdropUrls;
            });

            Field(x => x.CompletionSeconds).Description("The number of seconds at which time this video would be considered fully watched (i.e. the number of seconds at which time the credits start rolling).");
            Field<ListGraphType<MediaHistoryRecordGraphType>>().Name("history")
                .Description("The viewing history for this movie")
                .ResolveAsync(async (ctx) =>
                {
                    var columnNames = ctx.SubFields.Keys;
                    //manager.Media.PrefetchHistoryForMediaItem(manager.Users.CurrentProfileId, context.Source.Id);
                    return await mediaItemRepository.GetHistoryForMediaItem(userRepository.CurrentProfileId, ctx.Source.Id);
                });

            Field(x => x.ResumeSeconds).Description("If the user stopped in the middle of watching this video, resumeSeconds will be the number of seconds where playback should start back up. If the user has never watched this movie, or if they have already completely watched the movie, this field will be set to 0");
            Field(x => x.ProgressPercentage).Description("The percentage of the movie that the user watched");
        }
    }
}
