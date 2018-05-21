using System.Collections.Generic;
using GraphQL.DataLoader;
using GraphQL.Types;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.MetadataProcessing;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class MovieMetadataSearchResultGraphType : ObjectGraphType<MovieMetadataSearchResult>
    {
        public MovieMetadataSearchResultGraphType(
        )
        {
            Field(x => x.Title).Description("The formal title of the movie. This is also known as the movie's name.");
            Field(x => x.ReleaseDate, nullable: true).Description("The date this movie was first released.");
            Field(x => x.TmdbId, nullable: true).Description("TMDB (The Movie DataBase) ID for this movie.");
            Field(x => x.PosterUrl).Description("The full url to the poster for this movie.");
            Field(x => x.Overview).Description("The overview (or summary) of the movie");
        }
    }
}
