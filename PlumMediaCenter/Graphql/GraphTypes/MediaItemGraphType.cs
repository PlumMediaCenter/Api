using System.Collections.Generic;
using GraphQL.DataLoader;
using GraphQL.Types;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    /// <summary>
    /// This is a union graph type
    /// </summary>
    public class MediaItemGraphType : UnionGraphType
    {
        public MediaItemGraphType(
        )
        {
            Type<MovieGraphType>();
        }
    }
}
