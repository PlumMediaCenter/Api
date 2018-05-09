using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;

namespace PlumMediaCenter.Graphql
{
    public class RootMutationGraphType : ObjectGraphType
    {
        public RootMutationGraphType(
        )
        {
            this.Name = "Mutation";
        }
    }
}