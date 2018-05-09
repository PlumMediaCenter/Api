using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using PlumMediaCenter.Business;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Graphql
{
    public class BaseQuery : ObjectGraphType
    {
        public BaseQuery()
        {
            FieldAsync<ListGraphType<MovieType>>("movies", arguments: new QueryArguments(new QueryArgument[]{
                    new QueryArgument<IntGraphType>() { Name = "id" },
                    new QueryArgument<ListGraphType<IntGraphType>>() { Name = "ids" }
                }), resolve: async (context) =>
            {
                var manager = (PlumMediaCenter.Business.Manager)context.UserContext;
                //wipe the manager cache before each query so we don't have lingering cache from multiple requests
                Task<IEnumerable<Movie>> moviesTask;
                var columnNames =  Utility.GetColumnNames(context);
                if (context.Arguments["id"] != null)
                {
                    var id = context.GetArgument<int>("id");
                    moviesTask = manager.Movies.GetByIds(ids: new int[] { id }, columnNames:columnNames);
                }
                else if (context.Arguments["ids"] != null)
                {
                    var ids = context.GetArgument<List<int>>("ids");
                    moviesTask = manager.Movies.GetByIds(ids: ids, columnNames: columnNames);
                }
                else
                {
                    moviesTask = manager.Movies.GetMovies(columnNames: columnNames);
                }
                var movies = await moviesTask;
                manager.MovieIds = movies.Select(x=> x.Id); 
                return movies;
            });
        }

    }
}
