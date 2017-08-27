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
                connection.Execute(@"
                    create table sources(
                        id integer AUTO_INCREMENT primary key comment 'id of source',
                        folderPath varchar(4000) not null comment 'full path to source folder',
                        sourceType tinyint not null comment 'the type of media such as movies, shows, etc...'
                    );
                ");

                connection.Execute(@"
                    create table movies(
                        id integer AUTO_INCREMENT primary key comment 'id of movie',
                        folderPath varchar(4000) not null comment 'full path to folder for movie',
                        videoPath varchar(4000) not null comment 'full path to video file',
                        title varchar(200) not null comment 'title of movie',
                        summary varchar(100) comment 'short explanation of movie plot',
                        description varchar(4000) comment 'long explanation of movie plot',
                        rating varchar(10) comment 'MPAA rating for movie',
                        releaseDate date comment 'Date the movie was first released',
                        runtime integer comment 'Runtime of movie in minutes',
                        tmdbId integer comment 'The tmdb id for this movie, if one exists',
                        sourceId integer not null comment 'fk for sources table',
                        backdropGuids varchar(4000) not null comment 'comma separated list of backdrop guids',
                        foreign key (sourceId) references sources(id)
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