using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using PlumMediaCenter.Business.Enums;

namespace PlumMediaCenter.Business.Managers
{
    public class MovieManager : BaseManager
    {
        private static List<string> ColumnNameWhitelist = new List<string>{
            "id",
            "folderPath".ToLower(),
            "videoPath".ToLower(),
            "title",
            "sortTitle".ToLower(),
            "summary",
            "description",
            "rating",
            "releaseDate".ToLower(),
            "runtimeSeconds".ToLower(),
            "tmdbId".ToLower(),
            "sourceId".ToLower(),
            "backdropGuids".ToLower(),
            "completionSeconds".ToLower(),
        };

        private static Dictionary<string, string[]> ComputedPropertyRequirements = new Dictionary<string, string[]>{
            {"posterUrl".ToLower(), new string[]{"id"}},
            {"backdropUrls".ToLower(), new string[]{"backdropGuids".ToLower()}}
        };

        public MovieManager(Manager manager) : base(manager)
        {
        }

        private List<string> SanitizeColumnNames(List<string> columnNames)
        {
            //force the column names to lower case for comparisons
            var lowerColumnNames = columnNames.Select(x => x.ToLower()).ToList();

            //if column names is wildcard, use entire column name list
            bool wasWildcard = false;
            if (columnNames.First() == "*")
            {
                columnNames = ColumnNameWhitelist.ToList();
                wasWildcard = true;
            }

            //certain properties are computed and don't exist in the db. Those properties require that certain columns are loaded
            //so the properties can be calculated. Get the list of columns required for the requested properties
            var columnRequirements = ComputedPropertyRequirements
                .Where(x => lowerColumnNames.Contains(x.Key))
                .SelectMany(x => x.Value)
                .Distinct();

            //throw away anything that is not a known column name
            var result = columnNames.Where(x => ColumnNameWhitelist.Contains(x.ToLower())).ToList();
            //add the column requirements
            result.AddRange(columnRequirements);
            return result;
        }

        /// <summary>
        /// Get a list of movies filtered by the provided where clause
        /// </summary>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Models.Movie>> GetMovies(string sql = null, object bindings = null, List<string> columnNames = null)
        {
            columnNames = columnNames ?? new List<string> { "*" };

            columnNames = SanitizeColumnNames(columnNames);

            var fullSql = $@"
                select {string.Join(',', columnNames)}
                from Movies
                {sql ?? ""}
            ";

            IEnumerable<Models.Movie> models;
            if (bindings == null)
            {
                models = await this.QueryAsync<Models.Movie>(fullSql);
            }
            else
            {
                models = await this.QueryAsync<Models.Movie>(fullSql, bindings);
            }
            return models;
        }

        /// <summary>
        /// Get a list of movies by id
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Models.Movie>> GetByIds(IEnumerable<int> ids, List<string> columnNames = null)
        {
            return await this.GetMovies("where id in @ids", new { ids = ids }, columnNames);
        }

        public async Task<IEnumerable<Models.Movie>> GetSearchResults(string text, List<string> columnNames = null)
        {
            text = LibraryGeneration.Movie.NormalizeTitle(text);
            //split the text by spaces
            var parts = text.Split(" ");
            var i = 0;
            var sql = new StringBuilder();
            var or = "";
            //construct a where clause with all of the parts
            var dbParams = new DynamicParameters();
            foreach (var part in parts)
            {
                sql.Append($"{or} title like @part{i}");
                //add wildcards around the part
                dbParams.Add($"part{i++}", $"%{part}%");
                or = " or ";
            }
            //we need title to handle the sorting, so make sure it's included
            if (columnNames.Contains("title") == false)
            {
                columnNames.Add("title");
            }
            var movies = await this.GetMovies($"where {sql.ToString()}", dbParams, columnNames);

            //sort the movies by how many times each part appears
            movies = movies.OrderByDescending(movie =>
            {
                var count = 0;
                foreach (var part in parts)
                {
                    if (movie.Title.IndexOf(part) > 0)
                    {
                        count++;
                    }
                }
                return count;
            });
            return movies;
        }

        /// <summary>
        /// Get a movie by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Models.Movie> GetById(int id, List<string> columnNames = null)
        {
            var movie = (await this.GetByIds(new List<int> { id }, columnNames)).FirstOrDefault();
            return movie;
        }
    }
}