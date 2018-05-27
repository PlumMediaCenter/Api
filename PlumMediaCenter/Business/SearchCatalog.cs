using System.Threading.Tasks;
using PlumMediaCenter.Business.Data;
using Dapper;
using PlumMediaCenter.Business.Models;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Index;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using System.Collections.Generic;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers.Classic;
using System.Linq;
using Lucene.Net.Util;
using Lucene.Net.Queries;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;

namespace PlumMediaCenter.Business
{
    public class SearchCatalog
    {

        public SearchCatalog(
            AppSettings appSettings
        )
        {
            this.AppSettings = appSettings;
        }

        AppSettings AppSettings;

        private Analyzer GetAnalyzer()
        {
            var analyzer = new WhitespaceAnalyzer(LuceneVersion.LUCENE_48);
            return analyzer;
        }

        public void GenerateIndexes()
        {
            var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, GetAnalyzer())
            {
                OpenMode = OpenMode.CREATE
            };
            var directory = FSDirectory.Open(new DirectoryInfo(this.AppSettings.SearchIndexesDirectoryPath));
            var indexWriter = new IndexWriter(directory, config);

            using (var connection = ConnectionManager.CreateConnection())
            {
                var movies = connection.Query<Movie>(@"
                    select * from Movies
                ", buffered: false);
                foreach (var movie in movies)
                {
                    var document = new Document();
                    document.Add(new Int32Field("id", movie.Id, Field.Store.YES));
                    if (movie.Description != null)
                    {
                        var field = new TextField("description", movie.Description.ToLower(), Field.Store.YES);

                        document.Add(field);
                    }
                    document.Add(new StringField("rating", movie.Rating.ToLower(), Field.Store.YES));
                    if (movie.SortTitle != null)
                    {
                        document.Add(new TextField("sorttitle", movie.SortTitle.ToLower(), Field.Store.YES));
                    }
                    if (movie.Title != null)
                    {
                        document.Add(new TextField("title", movie.Title.ToLower(), Field.Store.YES));
                    }
                    //add a field that contains ALL of the search properties
                    document.Add(new TextField("text", $"{movie.Title} {movie.SortTitle} {movie.Rating} {movie.Description}".ToLower(), Field.Store.YES));
                    indexWriter.AddDocument(document);
                }
            }
            //save the entire index
            indexWriter.Commit();
            indexWriter.Flush(true, true);
            indexWriter.Commit();

            //clean up resources
            indexWriter.Dispose();
            directory.Dispose();
        }
        public IEnumerable<SearchResult> GetSearchResults(string queryText, int maxResults = 10)
        {
            queryText = queryText.ToLower();
            using (var directory = FSDirectory.Open(new DirectoryInfo(this.AppSettings.SearchIndexesDirectoryPath)))
            {
                var reader = DirectoryReader.Open(directory);
                var searcher = new IndexSearcher(reader);
                var queryParser = new QueryParser(Lucene.Net.Util.LuceneVersion.LUCENE_48, "text", GetAnalyzer());

                queryParser.AllowLeadingWildcard = true;
                queryParser.DefaultOperator = Operator.AND;

                var fullQuery = new BooleanQuery();
                //wrap every term so that we do exact match, fuzzy match, and wildcard match
                {
                    var parsedQuery = queryParser.Parse(queryText);
                    parsedQuery = parsedQuery.Rewrite(reader);

                    var terms = new HashSet<Term>();
                    parsedQuery.ExtractTerms(terms);


                    //for every term, create a boolean query that includes exact match, fuzzy match and wildcard match
                    foreach (var term in terms)
                    {
                        var subquery = new BooleanQuery();
                        var text = term.Text();

                        //add the initial term
                        subquery.Add(new TermQuery(term), Occur.SHOULD);

                        //TODO - remove permanently if this fixes the search issues
                        // var fuzzyQuery = new FuzzyQuery(new Term(term.Field, term.Text()));
                        // fuzzyQuery.Boost = 0.5f;
                        // subquery.Add(fuzzyQuery, Occur.SHOULD);

                        var wildQuery = new WildcardQuery(new Term(term.Field, $"*{term.Text()}*"));
                        wildQuery.Boost = 0.1f;
                        subquery.Add(wildQuery, Occur.SHOULD);

                        fullQuery.Add(subquery, Occur.MUST);
                    }
                }

                var hits = searcher.Search(fullQuery, maxResults);
                var returnValue = new List<SearchResult>();
                foreach (var hit in hits.ScoreDocs)
                {
                    var resultDoc = searcher.Doc(hit.Doc);
                    var id = resultDoc.GetField("id").GetInt32Value().Value;
                    var searchResult = new SearchResult()
                    {
                        MediaItemId = id
                    };
                    returnValue.Add(searchResult);
                }
                return returnValue;
            }
        }

        public class SearchResult
        {
            public int MediaItemId { get; set; }
        }
    }
}