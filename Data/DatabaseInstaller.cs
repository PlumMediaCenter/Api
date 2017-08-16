using Dapper;
using System;
using System.Linq;
using System.Data;

namespace PlumMediaCenter.Data
{
    class DatabaseInstaller
    {
        public DatabaseInstaller(string rootUsername, string rootPassword)
        {
            this.RootUsername = rootUsername;
            this.RootPassword = rootPassword;
        }

        private string RootUsername;
        private string RootPassword;
        public void Install()
        {
            CreateDbIfNotExist();
            var connection = ConnectionManager.GetConnection();
            VersionRun("0.1.0", connection, () =>
            {
                connection.ExecuteAsync(@"
                    create table movies(
                        id integer AUTO_INCREMENT primary key,
                        folderPath varchar(4000) not null,
                        videoPath varchar(4000) not null,
                        title varchar(200) not null,
                        summary varchar(100),
                        description varchar(4000)
                    );
                ");

                connection.Execute(@"
                    create table sources(
                        id integer AUTO_INCREMENT primary key,
                        folderPath varchar(4000) not null,
                        sourceType tinyint not null
                    );
                ");
                //temporarily insert some hardcoded video sources
                connection.Execute(@"
                    insert into sources(folderPath, sourceType)
                    values(@a,@b)
                ", new { a = @"C:\videos\movies", b = 0 });

                connection.Execute(@"
                    insert into sources(folderPath, sourceType)
                    values(@a,@b)
                ", new { a = @"C:\videos\shows", b = 1 });
            });

            connection.Close();
        }

        /// <summary>
        /// Execute the callback code if the current version is less than the provided version
        /// </summary>
        /// <param name="versionString"></param>
        /// <param name="connection"></param>
        /// <param name="callback"></param>
        public void VersionRun(string versionString, IDbConnection connection, Action callback)
        {
            var version = new Version(versionString);
            var currentVersion = GetVersion();
            if (currentVersion < version)
            {
                callback();
                connection.Execute("update version set version = @version", new { version = version.ToString() });
            }
        }

        public Version GetVersion()
        {
            try
            {
                var connection = ConnectionManager.GetConnection();
                var versionString = connection.Query<string>(@"
                    select version
                    from version
                ").ToList().First();
                var version = new Version(versionString);
                connection.Close();
                return version;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public void CreateDbIfNotExist()
        {
            var version = GetVersion();
            if (version != null)
            {
                return;
            }
            //the db has not yet been created. create it
            var connection = ConnectionManager.GetConnection(this.RootUsername, this.RootPassword, false);
            connection.Execute(@"
                create database `pmc`;
                GRANT ALL ON `pmc`.* TO 'pmc'@'localhost' identified by 'pmc';                
                GRANT ALL ON `pmc`.* TO 'pmc'@'127.0.0.1' identified by 'pmc';                
                GRANT ALL ON `pmc`.* TO 'pmc'@'%' identified by 'pmc';
                FLUSH PRIVILEGES;
            ");
            connection.Close();

            connection = ConnectionManager.GetConnection();
            connection.Execute("create table version(version text)");
            connection.Execute("insert into version(version) values('0.0.0')");
            connection.Close();
        }
    }
}