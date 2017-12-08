using System.Collections.Generic;
using GraphQL.Types;
using PlumMediaCenter.Business;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql
{
    public class MovieType : ObjectGraphType<Movie>
    {
        public MovieType()
        {
            Field(x => x.Id).Description("The ID of the video.");
            Field(x => x.Title).Description("The formal title of the movie. This is also known as the movie's name.");
            Field(x => x.SortTitle).Description("The title used to sort this movie.");
            Field(x => x.Summary, nullable: true).Description("The summary of the movie. This is shorter than the description.");
            Field(x => x.Description, nullable: true).Description("The description of the video.");
            Field(x => x.Rating, nullable: true).Description("This is the MPAA rating for the video (i.e. 'PG', 'R', etc...");
            Field(x => x.ReleaseDate, nullable: true).Description("The date this movie was first released.");
            Field(x => x.RuntimeSeconds, nullable: true).Description("How long the movie is, in seconds.");
            Field(x => x.TmdbId, nullable: true).Description("TMDB (The Movie DataBase) ID for this movie.");
            Field(x => x.SourceId, nullable: true).Description("The Source ID for this movie.");
            Field<ListGraphType<StringGraphType>>("backdropUrls", resolve: (context) =>
            {
                return context.Source.BackdropUrls;
            });

            Field(x => x.PosterUrl).Description("The url to the poster for this movie");
            Field(x => x.CompletionSeconds).Description("The number of seconds at which time this video would be considered fully watched (i.e. the number of seconds at which time the credits start rolling).");
            Field<ListGraphType<MediaHistoryRecordType>>("history", resolve: (context) =>
            {
                var manager = (Manager)context.UserContext;
                return manager.Media.GetHistoryForMediaItem(manager.Users.CurrentProfileId, context.Source.Id).Result;
            });
        }
    }
}
