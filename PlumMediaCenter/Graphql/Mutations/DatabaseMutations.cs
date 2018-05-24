using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using System;
using PlumMediaCenter.Graphql.InputGraphTypes;
using PlumMediaCenter.Graphql.GraphTypes;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;
using System.Collections.Generic;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Graphql.Mutations
{
    public class DatabaseMutations
    {
        public DatabaseMutations(
            DatabaseInstaller databaseInstaller
        )
        {
            this.DatabaseInstaller = databaseInstaller;
        }
        DatabaseInstaller DatabaseInstaller;

        public void Register(RootMutationGraphType mutation)
        {
            mutation.Field<BooleanGraphType>().Name("installDatabase")
                .Description("Install the database if it is not yet installed")
                .Argument<StringGraphType>("rootUsername", "The username for the root database user")
                .Argument<StringGraphType>("rootPassword", "The password for the root database user")
                .Resolve((ctx) =>
                {
                    this.DatabaseInstaller.Install(ctx.GetArgument<string>("rootUsername"), ctx.GetArgument<string>("rootPassword"));
                    return true;
                }
            );
        }
    }
}