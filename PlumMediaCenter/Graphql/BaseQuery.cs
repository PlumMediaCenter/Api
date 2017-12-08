using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;
using PlumMediaCenter.Business;

namespace PlumMediaCenter.Graphql
{
    public class BaseQuery : ObjectGraphType
    {
        public BaseQuery()
        {
            Field<ListGraphType<MovieType>>("movies", arguments: new QueryArguments(new QueryArgument[]{
                    new QueryArgument<IntGraphType>() { Name = "id" },
                    new QueryArgument<ListGraphType<IntGraphType>>() { Name = "ids" }
                }), resolve: (context) =>
            {
                var manager = (Manager)context.UserContext;
                if (context.Arguments["id"] != null)
                {
                    var id = context.GetArgument<int>("id");
                    return manager.Movies.GetByIds(ids: new int[] { id }, columnNames: this.GetColumnNames(context));
                }
                else if (context.Arguments["ids"] != null)
                {
                    var ids = context.GetArgument<List<int>>("ids");
                    return manager.Movies.GetByIds(ids: ids, columnNames: this.GetColumnNames(context));
                }
                else
                {
                    return manager.Movies.GetMovies(columnNames: this.GetColumnNames(context));
                }
            });
        }

        public List<string> GetColumnNames(ResolveFieldContext<object> context)
        {
            var columnNames = new List<string>();
            foreach (var selection in context.FieldAst.SelectionSet.Children)
            {
                columnNames.AddRange(GetTopLevelFieldNames(selection, context));
            }

            return columnNames;
        }

        public List<string> GetTopLevelFieldNames(GraphQL.Language.AST.INode currentNode, ResolveFieldContext<object> context)
        {
            var columnNames = new List<string>();
            var type = currentNode.GetType();
            //if this is a raw field, use its name
            if (type == typeof(GraphQL.Language.AST.Field))
            {
                var field = (GraphQL.Language.AST.Field)currentNode;
                columnNames.Add(field.Name);
            }
            else if (type == typeof(GraphQL.Language.AST.FragmentSpread))
            {
                var fragmentSpread = (GraphQL.Language.AST.FragmentSpread)currentNode;
                var fragment = context.Fragments.Where(x => x.Name == fragmentSpread.Name).FirstOrDefault();
                if (fragment == null)
                {
                    throw new Exception($"Unable to find fragment with name {fragmentSpread.Name}");
                }
                else
                {
                    foreach (var child in fragment.SelectionSet.Children)
                    {
                        columnNames.AddRange(GetTopLevelFieldNames(child, context));
                    }
                }
            }
            return columnNames;
        }
    }
}
