using Dapper;
using System;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using PlumMediaCenter.Business.Enums;

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
            var connection = ConnectionManager.GetNewConnection();
            VersionRun("0.1.0", connection, () =>
            {
                connection.Execute(@"
                    create table MediaTypes(
                        id tinyint not null primary key comment 'id of media type',
                        name varchar(10) not null comment 'the name of the media type'
                    );
                ");

                connection.Execute($@"
                    insert into MediaTypes(id,name)
                    values 
                        ({(int)MediaTypeId.Movie}, 'Movie'),
                        ({(int)MediaTypeId.TvShow}, 'TvShow'),
                        ({(int)MediaTypeId.TvEpisode}, 'TvEpisode')
                ");

                connection.Execute(@"
                    create table Sources(
                        id int unsigned not null AUTO_INCREMENT primary key comment 'id of source',
                        folderPath varchar(4000) not null comment 'full path to source folder',
                        mediaTypeId tinyint not null comment 'the id of the mediaType of the type of media the source contains (i.e. movies, tvshows, etc...)',
                        foreign key (mediaTypeId) references MediaTypes(id)
                    );
                ");

                // Used to generate an ID that is unique between all media types
                connection.Execute(@"
                    create table MediaItemIds(
                        id integer unsigned not null AUTO_INCREMENT primary key comment 'id of',
                        mediaTypeId tinyint not null comment 'the type of media this ID was created for'
                    );
                ");

                connection.Execute(@"
                    create table Movies(
                        id int unsigned not null primary key comment 'mediaItemId of movie',
                        folderPath varchar(4000) not null comment 'full path to folder for movie',
                        videoPath varchar(4000) not null comment 'full path to video file',
                        title varchar(200) not null comment 'title of movie',
                        sortTitle varchar(200) not null comment 'title to use for sorting movies',
                        summary varchar(100) comment 'short explanation of movie plot',
                        description varchar(4000) comment 'long explanation of movie plot',
                        rating varchar(10) comment 'MPAA rating for movie',
                        releaseDate date comment 'Date the movie was first released',
                        runtimeMinutes integer comment 'Runtime of movie in minutes',
                        tmdbId integer comment 'The tmdb id for this movie, if one exists',
                        sourceId int unsigned not null comment 'fk for sources table',
                        backdropGuids varchar(4000) not null comment 'comma separated list of backdrop guids',
                        foreign key(sourceId) references Sources(id),
                        foreign key(id) references MediaItemIds(id)
                    );
                ");

                connection.Execute(@"
                    create table MediaProgress(
                        id int unsigned not null AUTO_INCREMENT primary key comment 'Unique identifier for this table',
                        profileId int unsigned not null comment 'id of the profile that interacted with this media item',
                        mediaItemId int unsigned not null comment 'id of the media item',
                        progressSecondsBegin int not null comment 'the second count when the media interaction began',
                        progressSecondsEnd int not null comment 'the second count when the media interaction ended',
                        dateBegin datetime not null,
                        dateEnd datetime not null,
                        foreign key(mediaItemId) references MediaItemIds(id)
                    );
                ");
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
                using (var connection = ConnectionManager.GetNewConnection())
                {
                    var versionString = connection.Query<string>(@"
                    select version
                    from version
                ").ToList().First();
                    var version = new Version(versionString);
                    connection.Close();
                    return version;
                }
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
            using (var connection = ConnectionManager.GetNewConnection(this.RootUsername, this.RootPassword, false))
            {
                connection.Execute(@"
                    create database `pmc`;
                    GRANT ALL ON `pmc`.* TO 'pmc'@'localhost' identified by 'pmc';                
                    GRANT ALL ON `pmc`.* TO 'pmc'@'127.0.0.1' identified by 'pmc';                
                    GRANT ALL ON `pmc`.* TO 'pmc'@'%' identified by 'pmc';
                    FLUSH PRIVILEGES;
                ");
            }
            using (var connection = ConnectionManager.GetNewConnection())
            {
                connection.Execute("create table version(version text)");
                connection.Execute("insert into version(version) values('0.0.0')");
                connection.Close();
            }
        }

        /// <summary>
        /// Determine if the database is instaled. This does not check to see if the db is up to date. 
        /// It only validates that there is a pmc db created
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> IsInstalled()
        {
            try
            {
                using (var connection = ConnectionManager.GetNewConnection())
                {
                    var rows = await connection.QueryAsync(@"
                        select version
                        from version
                    ");
                    if (rows.FirstOrDefault() != null)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}