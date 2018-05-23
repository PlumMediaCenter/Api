using System.Collections.Generic;
using GraphQL.Types;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class MediaHistoryRecordGraphType : ObjectGraphType<MediaHistoryRecord>
    {
        public MediaHistoryRecordGraphType()
        {
            Field(x => x.Id);
            Field(x => x.DateBegin);
            Field(x => x.DateEnd);
            Field(x => x.MediaItemId);
            Field(x => x.PosterUrl);
            Field(x => x.ProfileId);
            Field(x => x.ProgressSecondsBegin);
            Field(x => x.ProgressSecondsEnd);
            Field<IntGraphType>().Name("totalProgressSeconds")
                .Resolve((ctx) =>
                {
                    return ctx.Source.TotalProgressSeconds.Value;
                });

            Field(x => x.RuntimeSeconds);

            Field<MediaTypeEnumGraphType>().Name("mediaType")
                .Description("The media type for this item.")
                .Resolve(x => x.Source.MediaType);

            Field(x => x.Title);
        }
    }
}
