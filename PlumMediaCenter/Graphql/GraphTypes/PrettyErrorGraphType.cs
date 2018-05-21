using System.Collections.Generic;
using GraphQL.Types;
using PlumMediaCenter.Attributues;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class PrettyErrorGraphType : ObjectGraphType<PrettyError>
    {
        public PrettyErrorGraphType()
        {
            Field(x => x.message);
            Field<ListGraphType<StringGraphType>>().Name("stackTrace").Resolve(x => x.Source.stackTrace);
        }
    }
}
