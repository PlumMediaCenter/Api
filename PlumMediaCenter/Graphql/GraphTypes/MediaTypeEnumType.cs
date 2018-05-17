using GraphQL.Types;
using PlumMediaCenter.Business.Enums;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class MediaTypeEnumGraphType : EnumerationGraphType<MediaTypeId>
    {
        public MediaTypeEnumGraphType()
        {
            this.Name = "MediaTypeEnum";
        }
    }
}