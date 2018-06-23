using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using GraphQL.Types;
using PlumMediaCenter.Business.Data;
using PlumMediaCenter.Business.Enums;
using PlumMediaCenter.Business.Models;
using PlumMediaCenter.Models;

namespace PlumMediaCenter.Business.Repositories
{
    public class MovieRepository : BaseRepository<Movie>
    {
        public MovieRepository(
            LibGenMovieRepository libGenMovieRepository,
            MediaItemRepository mediaItemRepository,
            UserRepository userRepository,
            Utility utility
        ) : base()
        {
            this.LibGenMovieRepository = libGenMovieRepository;
            this.Utility = utility;
            this.TableName = "Movies";
            this.AllColumnNames = new[]{
                //actual columns
                "id",
                "folderPath",
                "videoPath",
                "title",
                "sortTitle",
                "summary",
                "description",
                "rating",
                "releaseYear",
                "runtimeSeconds",
                "tmdbId",
                "sourceId",
                "completionSeconds",
                "posterCount",
                "backdropCount",

                //derived columns
                "resumeSeconds",
                "progressPercentage",
                "posterUrls",
                "backdropUrls"
            };
            this.AlwaysIncludedColumnNames = new[] {
                "id",
                "videoPath"
            };
            this.Aliases.Add("posterUrls", "posterCount");
            this.Aliases.Add("backdropUrls", "backdropCount");

            this.PostQueryProcessors.Add(new PostQueryProcessor<Movie>(new[] { "resumeSeconds", "progressPercentage" }, new[] { "runtimeSeconds" }, async (models) =>
              {
                  await mediaItemRepository.FetchProgressSeconds(userRepository.CurrentProfileId, models);
                  return models;
              }));


        }
        LibGenMovieRepository LibGenMovieRepository;
        Utility Utility;

        public async Task<IEnumerable<Movie>> Query(
               MovieFilters filters,
               IEnumerable<string> columnNames = null)
        {
            var whereFilters = new List<string>();
            var parameters = new DynamicParameters();

            //Filter by specific ids
            if (filters.MovieIds != null)
            {
                //remove dupes
                filters.MovieIds = filters.MovieIds.Distinct();

                whereFilters.Add("Movies.id in @ids");
                parameters.Add("ids", filters.MovieIds);
            }
            var sql = "";
            if (whereFilters.Count > 0)
            {
                //append the where clause
                sql = $@"where {string.Join(" and ", whereFilters)}";
            }

            // sql = ApplyOrderToQuery(sql, filters.SortField, filters.SortDirection);
            sql = ApplyLimitersToQuery(sql, filters.Top, filters.Skip);
            return await this.Query(sql, parameters, columnNames);
        }

        public async Task<IEnumerable<Movie>> GetSearchResults(string text, List<string> columnNames = null)
        {
            text = Utility.NormalizeTitle(text);
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
            var movies = await this.Query($"where {sql.ToString()}", dbParams, columnNames);

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
        public async Task<Movie> GetById(int id, List<string> columnNames = null)
        {
            var movie = (await this.GetByIds(new[] { id }, columnNames)).FirstOrDefault();
            return movie;
        }

        public MovieFilters GetArgumentFilters<T>(ResolveFieldContext<T> ctx)
        {
            var filters = new MovieFilters();
            filters.MovieIds = ctx.GetArgumentOrDefault("ids", (IEnumerable<int>)null);
            filters.Top = ctx.GetArgumentOrDefault("top", (int?)null);
            filters.Skip = ctx.GetArgumentOrDefault("skip", (int?)null);
            return filters;
        }

    }

    public class MovieFilters
    {
        public IEnumerable<int> MovieIds;
        /// <summary>
        /// Return the first N elements from the list
        /// </summary>
        public int? Top;
        /// <summary>
        /// Skip the first N elements the first N elements from the list. Normally used in conjunction with Top
        /// </summary>
        public int? Skip;
    }

}