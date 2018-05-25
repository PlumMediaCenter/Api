
using GraphQL.Types;
using PlumMediaCenter.Graphql.GraphTypes;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.InputGraphTypes
{
    public class SourceInputGraphType : InputObjectGraphType<Source>
    {
        public SourceInputGraphType()
        {
            this.Name = "SourceInput";
            Field(x => x.Id, nullable: true).Description("The id of an already-existing source. Do not specify for new items");
            Field(x => x.FolderPath).Description("The full path to the folder for this source");
            Field<MediaTypeEnumGraphType>().Name("mediaType").Description("The type of media this source contains");
        }
    }
}