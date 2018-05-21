using System.Collections.Generic;
using GraphQL.DataLoader;
using GraphQL.Types;
using PlumMediaCenter.Business;
using PlumMediaCenter.Business.MetadataProcessing;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class MovieMetadataComparisonGraphType : ObjectGraphType<MovieMetadataComparison>
    {
        public MovieMetadataComparisonGraphType(
        )
        {
           Field<MovieMetadataGraphType>().Name("current").Resolve(x=> x.Source.Current);
           Field<MovieMetadataGraphType>().Name("incoming").Resolve(x=> x.Source.Incoming);
        }
    }
}
