using Dapper;
using System;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Business.Data;

namespace PlumMediaCenter.Data
{
    public class DatabaseInstaller
    {
        public DatabaseInstaller()
        {

        }

        public void Install(string rootUsername, string rootPassword)
        {
            CreateDbIfNotExist(rootUsername, rootPassword);
            var connection = ConnectionManager.CreateConnection();
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
                        ({(int)MediaType.MOVIE}, 'MOVIE'),
                        ({(int)MediaType.TV_SHOW}, 'TV_SHOW'),
                        ({(int)MediaType.TV_EPISODE}, 'TV_EPISODE')
                ");

                connection.Execute(@"
                    create table Sources(
                        id int not null AUTO_INCREMENT primary key comment 'id of source',
                        folderPath varchar(4000) not null comment 'full path to source folder',
                        mediaTypeId tinyint not null comment 'the id of the mediaType of the type of media the source contains (i.e. movies, tvshows, etc...)',
                        foreign key (mediaTypeId) references MediaTypes(id)
                    );
                ");

                // Used to generate an ID that is unique between all media types
                connection.Execute(@"
                    create table MediaItemIds(
                        id int not null AUTO_INCREMENT primary key comment 'id of',
                        mediaTypeId tinyint not null comment 'the type of media this ID was created for'
                    );
                ");

                connection.Execute(@"
                    create table Movies(
                        id int not null primary key comment 'mediaItemId of movie',
                        folderPath varchar(4000) not null comment 'full path to folder for movie',
                        videoPath varchar(4000) not null comment 'full path to video file',
                        title varchar(200) not null comment 'title of movie',
                        sortTitle varchar(200) not null comment 'title to use for sorting movies',
                        shortSummary varchar(100) comment 'short explanation of movie plot',
                        summary varchar(4000) comment 'longer explanation of movie plot',
                        rating varchar(10) comment 'MPAA rating for movie',
                        releaseYear int(4) comment 'Year the movie was first released',
                        runtimeSeconds int comment 'Runtime of movie in seconds',
                        tmdbId int comment 'The tmdb id for this movie, if one exists',
                        sourceId int not null comment 'fk for Sources table',
                        completionSeconds int comment 'the number of seconds at which time the movie would be considered watched',
                        posterCount int comment 'The number of posters this movie has',
                        backdropCount int comment 'The number of backdrops this movie has',
                        foreign key(sourceId) references Sources(id),
                        foreign key(id) references MediaItemIds(id)
                    );
                ");

                connection.Execute(@"
                    create table TvShows(
                        id int not null primary key comment 'mediaItemId of tv show',
                        folderPath varchar(4000) not null comment 'full path to folder for tv show',
                        title varchar(200) not null comment 'title of tv show',
                        sortTitle varchar(200) not null comment 'title to use for sorting',
                        summary varchar(100) comment 'short explanation of tv show',
                        description varchar(4000) comment 'longer explanation of tv show',
                        rating varchar(10) comment 'MPAA rating for tv show',
                        releaseYear int(4) comment 'Year the tv show was first released',
                        runtimeSeconds int comment 'Average runtime of the episodes in the tv show',
                        tmdbId int comment 'The tmdb id for this tv show, if one exists',
                        sourceId int not null comment 'fk for Sources table',
                        foreign key(sourceId) references Sources(id),
                        foreign key(id) references MediaItemIds(id)
                    );
                ");

                connection.Execute(@"
                    create table MediaItemProgress(
                        id int not null AUTO_INCREMENT primary key comment 'Unique identifier for this table',
                        profileId int not null comment 'id of the profile that interacted with this media item',
                        mediaItemId int not null comment 'id of the media item',
                        progressSecondsBegin int not null comment 'the second count when the media interaction began',
                        progressSecondsEnd int not null comment 'the second count when the media interaction ended',
                        dateBegin datetime not null,
                        dateEnd datetime not null,
                        foreign key(mediaItemId) references MediaItemIds(id)
                    );
                ");

                connection.Execute(@"
                    create table Image(
                        id int not null AUTO_INCREMENT primary key comment 'Unique identifier for this table',
                        sourceUrl varchar(2048) comment 'The url where the image originated from. Will be null if a user manually uploaded an image',
                        data blob not null
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
                connection.Execute("update Version set version = @version", new { version = version.ToString() });
            }
        }

        public Version GetVersion()
        {
            try
            {
                using (var connection = ConnectionManager.CreateConnection())
                {
                    var versionString = connection.Query<string>(@"
                    select version
                    from Version
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
        public void CreateDbIfNotExist(string rootUsername, string rootPassword)
        {
            var version = GetVersion();
            if (version != null)
            {
                return;
            }
            //the db has not yet been created. create it
            using (var connection = ConnectionManager.CreateConnection(rootUsername, rootPassword, false))
            {
                connection.Execute(@"
                    create database `pmc`;
                    GRANT ALL ON `pmc`.* TO 'pmc'@'localhost' identified by 'pmc';                
                    GRANT ALL ON `pmc`.* TO 'pmc'@'127.0.0.1' identified by 'pmc';                
                    GRANT ALL ON `pmc`.* TO 'pmc'@'%' identified by 'pmc';
                    FLUSH PRIVILEGES;
                ");
            }
            using (var connection = ConnectionManager.CreateConnection())
            {
                connection.Execute("create table Version(version text)");
                connection.Execute("insert into Version(version) values('0.0.0')");
                connection.Close();
            }
        }

        /// <summary>
        /// Determine if the database is instaled. This does not check to see if the db is up to date. 
        /// It only validates that there is a pmc db created
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GetIsInstalled()
        {
            try
            {
                using (var connection = ConnectionManager.CreateConnection())
                {
                    var rows = await connection.QueryAsync(@"
                        select version
                        from Version
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