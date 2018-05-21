using System.Collections.Generic;
using GraphQL.DataLoader;
using GraphQL.Types;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class MediaTypeCountGraphType : ObjectGraphType<Business.MediaTypeCount>
    {
        public MediaTypeCountGraphType(
        )
        {
            Field<MediaTypeEnumGraphType>().Name("mediaType").Description("The media type this count is for").Resolve(x => x.Source.MediaType);
            Field(x => x.Total).Description("The total number of items for this media type that need processed");
            Field(x => x.Remaining).Description("The number of items of this media type that have not yet been processed");
            Field(x => x.Completed).Description("The number of items for this media type that have already been processed");
        }
    }
}
