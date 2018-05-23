using GraphQL.Types;
using PlumMediaCenter.Business.Enums;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class MediaTypeEnumGraphType : EnumerationGraphType<MediaType>
    {
        public MediaTypeEnumGraphType()
        {
            this.Name = "MediaTypeEnum";
        }
    }
}