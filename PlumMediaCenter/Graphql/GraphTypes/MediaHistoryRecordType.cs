using System.Collections.Generic;
using GraphQL.Types;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class MediaHistoryRecordType : ObjectGraphType<MediaHistoryRecord>
    {
        public MediaHistoryRecordType()
        {
            Field(x => x.Id);
            Field(x => x.DateBegin); 
            Field(x => x.DateEnd); 
            Field(x => x.MediaItemId); 
            Field(x => x.PosterUrl); 
            Field(x => x.ProgressSecondsBegin); 
            Field(x => x.ProgressSecondsEnd); 
            Field(x => x.TotalProgressSeconds.Value); 
            Field(x => x.RuntimeSeconds); 
            Field(x => x.Title); 
        }
    }
}
