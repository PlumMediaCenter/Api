
using GraphQL.Types;
using PlumMediaCenter.Graphql.GraphTypes;

namespace PlumMediaCenter.Graphql.InputGraphTypes
{
    public class SourceInputGraphType : InputObjectGraphType
    {
        public SourceInputGraphType()
        {
            this.Name = "SourceInput";
            Field<IntGraphType>("id", "The id of an already-existing source. Do not specify for new items");
            Field<StringGraphType>("folderPath", "The full path to the folder for this source");
            Field<MediaTypeEnumGraphType>("mediaType", "The type of media this source contains");
        }
    }
}