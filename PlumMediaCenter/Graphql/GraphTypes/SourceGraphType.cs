using System.Collections.Generic;
using GraphQL.DataLoader;
using GraphQL.Types;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class SourceGraphType : ObjectGraphType<Source>
    {
        public SourceGraphType(
             IDataLoaderContextAccessor dlca,
             MediaRepository mediaRepository,
             UserRepository userRepository
        )
        {
            Field(x => x.Id).Description("The ID of the source");
            Field(x => x.FolderPath).Description("The full path to the folder for this source");
            
            Field<MediaTypeEnumGraphType>()
                .Name("mediaType")
                .Description("The type of media that this source contains")
                .Resolve((ctx) =>
                {
                    return ctx.Source.MediaTypeId;
                });
        }
    }
}
