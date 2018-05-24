using GraphQL.Types;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Data;

namespace PlumMediaCenter.Graphql.GraphTypes
{
    public class DatabaseGraphType : ObjectGraphType<Database>
    {
        public DatabaseGraphType(
            DatabaseInstaller databaseInstaller
        )
        {
            this.DatabaseInstaller = databaseInstaller;
            Field(x => x.IsInstalled).Description("Indicates whether the database is installed or not");
        }
        DatabaseInstaller DatabaseInstaller;
        
        public void Register(RootQueryGraphType rootQuery)
        {

            rootQuery.Field<DatabaseGraphType>().Name("database")
                .Description("Information about the database")
                .ResolveAsync(async (ctx) =>
                {
                    var db = new Database();
                    db.IsInstalled = await this.DatabaseInstaller.GetIsInstalled();
                    return db;
                });
        }
    }

    public class Database
    {
        public bool IsInstalled { get; set; }
    }
}