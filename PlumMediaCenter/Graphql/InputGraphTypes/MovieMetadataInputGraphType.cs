
using GraphQL.Types;
using PlumMediaCenter.Business.Metadata;
using PlumMediaCenter.Business.MetadataProcessing;
using PlumMediaCenter.Graphql.GraphTypes;

namespace PlumMediaCenter.Graphql.InputGraphTypes
{
    public class MovieMetadataInputGraphType : InputObjectGraphType<MovieMetadata>
    {
        public MovieMetadataInputGraphType()
        {
            this.Name = "MovieMetadataInput";
            Field(x => x.BackdropUrls);
            // Field(x => x.Cast);
            Field(x => x.Collection);
            Field(x => x.CollectionOrder, nullable: true);
            Field(x => x.CompletionSeconds, nullable: true);
            // Field(x => x.Crew);
            Field(x => x.Summary);
            Field(x => x.ExtraSearchText);
            Field(x => x.Genres);
            Field(x => x.Keywords);
            Field(x => x.PosterUrls);
            Field(x => x.Rating);
            Field(x => x.ReleaseYear, nullable: true);
            Field(x => x.RuntimeSeconds, nullable: true);
            Field(x => x.SortTitle);
            Field(x => x.ShortSummary);
            Field(x => x.Title);
            Field(x => x.TmdbId, nullable: true);
        }
    }
}