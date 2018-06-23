using System.Collections.Generic;
using GraphQL.DataLoader;
using GraphQL.Types;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.Metadata;
using PlumMediaCenter.Business.MetadataProcessing;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class MovieMetadataGraphType : ObjectGraphType<MovieMetadata>
    {
        public MovieMetadataGraphType(
        )
        {
            Field(x => x.BackdropUrls).Description("The list of backdrop urls for this movie");
            //Field(x=> x.Cast)
            //Field(x=>x.Crew)
            Field(x => x.Collection, nullable: true).Description("The name of the collection that this video belongs to (i.e. 'Star Trek', 'Die Hard'). If the movie is not in a collection, this will be null");
            Field(x => x.CollectionOrder, nullable: true).Description("The number in the collection, if it is in one. (i.e. Star Trek: The Wrath of Kahn would be 2, since it's the second star trek movie)");
            Field(x => x.CompletionSeconds, nullable: true).Description("The numer of seconds at which point the video would be considered fully watched");
            Field(x => x.Description, nullable: true).Description("A lengthy description of the movie");
            Field(x => x.ExtraSearchText).Description("Additional phrases to use for searching. For example, sometimes people spell Dalmatians with an o.\"Dalmations\" so adding \"101 Dalmations\" into this list would help with that search");
            Field<ListGraphType<StringGraphType>>().Name("genres")
                .Description("A list of high-level genres for the movie. These would be things like \"Action\", \"Thriller\"")
                .Resolve(x => x.Source.Genres);
            Field<ListGraphType<StringGraphType>>().Name("keywords")
                .Description("A fine-grained list of things related to the movie. Think of keywords like of like micro-genres")
                .Resolve(x => x.Source.Keywords);
            Field<ListGraphType<StringGraphType>>().Name("posterUrls")
                .Description("A list of urls of all of the available posters for this movie")
                .Resolve(x => x.Source.PosterUrls);
            Field(x => x.Rating, nullable: true).Description("The MPAA rating of this video (G, PG, PG-13, R, etc...");
            Field(x => x.ReleaseYear, nullable: true).Description("The year this movie was first released.");
            Field(x => x.RuntimeSeconds, nullable: true).Description("The runtime of the movie in seconds");
            Field(x => x.SortTitle).Description("The title to use to sort the movie with. If omitted, the movie will be sorted by Title");
            Field(x => x.Summary, nullable: true).Description("A short summary of the movie");
            Field(x => x.Title).Description("The formal title of the movie. This is also known as the movie's name.");
            Field(x => x.TmdbId, nullable: true).Description("TMDB (The Movie DataBase) ID for this movie.");
        }
    }
}
