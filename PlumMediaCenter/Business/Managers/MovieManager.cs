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
        public MovieManager(Manager manager) : base(manager)
        {
        }

        public async Task<IEnumerable<Models.Movie>> GetAll()
        {
            var models = await this.QueryAsync<Models.Movie>(@"
                select *, backdropGuids as _backdropGuids 
                from Movies
                order by sortTitle asc;
            ");
            return models;
        }

        /// <summary>
        /// Get a list of movies by id
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Models.Movie>> GetByIds(IEnumerable<int> ids)
        {
            var models = (await this.QueryAsync<Models.Movie>($@"
                select *, backdropGuids as _backdropGuids, {(int)MediaTypeId.Movie} as mediaTypeId 
                from Movies
                where id in @ids
            ", new { ids = ids }));
            return models;
        }

        public async Task<IEnumerable<Models.Movie>> GetSearchResults(string text)
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
            var ids = await this.QueryAsync<int>($@"
                select id 
                from Movies
                where {sql.ToString()}
            ", dbParams);
            var movies = await this.GetByIds(ids);

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
        public async Task<Models.Movie> GetById(int id)
        {
            var movie = (await this.GetByIds(new List<int> { id })).FirstOrDefault();
            return movie;
        }
    }
}