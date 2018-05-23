using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Dapper;
using PlumMediaCenter.Business.Data;

namespace PlumMediaCenter.Business
{
    public abstract class BaseRepository<ModelType> where ModelType : class, IHasId
    {
        public BaseRepository()
        {
        }

        /// <summary>
        /// The name of the db table for this manager
        /// </summary>
        public string TableName;

        /// <summary>
        /// The list of column names for this manager
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> AllColumnNames;

        /// <summary>
        /// The list of column names that will always be retrieved from the database, regardless of what columns are requested
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> AlwaysIncludedColumnNames = new List<string>();

        /// <summary>
        /// A set of not-physical columns that are derived based on subqueries. 
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Func<DynamicParameters, string>> DerivedColumns = new Dictionary<string, Func<DynamicParameters, string>>();

        /// <summary>
        /// Column names can be called other things sometimes from outside, so map those names back to real column names.
        /// Key is the alias, value is the actual column names
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> Aliases = new Dictionary<string, string>();

        /// <summary>
        /// A computed column may depend on other fields being present
        /// Key is the alias, value is the list of actual column names (1-1 when used for aliases, possibly 1-many when used for computed columns)
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, IEnumerable<string>> ComputedColumnDependencies = new Dictionary<string, IEnumerable<string>>();

        /// <summary>
        /// A set of functions that are executed once a query is finished. This can be used to run bulk loads of certain computed properties.
        /// </summary>
        /// <returns></returns>
        public List<PostQueryProcessor<ModelType>> PostQueryProcessors = new List<PostQueryProcessor<ModelType>>();

        /// <summary>
        /// Perform a query to retrieve a set of results
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="bindings"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ModelType>> Query(string sql = null, object bindings = null, IEnumerable<string> initialColumnNames = null)
        {
            var dynamicParams = bindings != null ? new DynamicParameters(bindings) : new DynamicParameters();

            var columnNames = this.SanitizeColumnNames(initialColumnNames).ToList();
            //ensure that every query includes the ID column
            columnNames.AddIfMissing("id");

            //prepend the table name to each column
            columnNames = columnNames.Select(columnName => $"{this.TableName}.{columnName}").ToList();

            //add derived columns
            {
                var columns = columnNames.ToList();
                foreach (var derivedColumn in DerivedColumns)
                {
                    var qualifiedColumnName = $"{this.TableName}.{derivedColumn.Key}";
                    if (columns.Contains(qualifiedColumnName))
                    {
                        columns.Remove(qualifiedColumnName);
                        var derivedColumnSql = derivedColumn.Value(dynamicParams);
                        columns.Add(derivedColumnSql);
                    }
                }
                columnNames = columns;
            }

            IEnumerable<ModelType> records;
            using (var connection = ConnectionManager.CreateConnection())
            {
                var fullSql = $@"
                    select {string.Join(',', columnNames)}
                    from {this.TableName}
                    {sql}
                ";
                records = await connection.QueryAsync<ModelType>(fullSql, dynamicParams);
            }
            foreach (var processor in PostQueryProcessors)
            {
                if (processor.PropertyNames.Where(x => initialColumnNames.Contains(x)).Count() > 0)
                {
                    records = await processor.Callback(records);
                }
            }
            return records;
        }

        /// <summary>
        /// Filter the list of reords by only the ones that the current user is authorized to access.
        /// By default, this method simply returns all provided records. It is the responsibility of the child class
        /// to filter the records accordingly.
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        protected virtual async Task<IEnumerable<ModelType>> FilterAuthorizedOnly(IEnumerable<ModelType> records)
        {
            return await Task.FromResult(records);
        }

        protected virtual IEnumerable<string> SanitizeColumnNames(IEnumerable<string> columnNames)
        {
            //default to an empty list if null
            var cols = columnNames != null ? columnNames.ToList() : new List<string>();

            //exchange any aliases with the actual column names
            foreach (var aliasKvp in this.Aliases)
            {
                var idx = cols.IndexOf(aliasKvp.Key);
                if (idx > -1)
                {
                    cols[idx] = aliasKvp.Value;
                }
            }
            //exchange any computed columns with their dependencies
            foreach (var computedColumnKvp in this.ComputedColumnDependencies)
            {
                var idx = cols.IndexOf(computedColumnKvp.Key);
                if (idx > -1)
                {
                    //remove the computed column name
                    cols.RemoveAt(idx);
                    //add the columns the computed column depends on
                    cols.AddRange(computedColumnKvp.Value);
                }
            }
            //include the AlwaysIncluded columns 
            cols.AddRange(this.AlwaysIncludedColumnNames);

            //remove post processor column names
            cols.RemoveIfPresent(this.PostQueryProcessors.SelectMany(x => x.PropertyNames));
            //add columns required for post processors
            cols.AddIfMissing(this.PostQueryProcessors.SelectMany(x => x.RequiredColumns));

            //throw away anything that is not a known column name
            var result = cols.Where(x => this.AllColumnNames.Contains(x)).Distinct().ToList();
            return result;
        }

        /// <summary>
        /// Get a list of records by their IDs
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ModelType>> GetByIds(IEnumerable<int> ids, IEnumerable<string> columnNames)
        {
            return await this.Query(@"where id in @ids", new { ids = ids }, columnNames);
        }

        public async Task<Dictionary<int, ModelType>> GetDictionaryByIds(IEnumerable<int> ids, IEnumerable<string> columnNames)
        {
            ids = ids.Distinct();
            var models = await this.GetByIds(ids, columnNames);
            var result = new Dictionary<int, ModelType>();
            //add a null default value for each id
            foreach (var id in ids)
            {
                result.Add(id, null);
            }
            //populate all fetched models
            foreach (var model in models)
            {
                result[model.Id] = model;
            }
            return result;
        }

        /// <summary>
        /// Get a record by its id, or null if not found
        /// </summary>
        /// <param name="id"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public async Task<ModelType> GetById(int id, IEnumerable<string> columnNames)
        {
            var models = await this.GetByIds(new int[] { id }, columnNames);
            return models.FirstOrDefault();
        }

        /// <summary>
        /// Insert a new record into the table
        /// </summary>
        /// <param name="model"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public async Task<ModelType> Insert(ModelType model, IEnumerable<string> columnNames)
        {
            var result = await this.InsertMany(new ModelType[] { model }, columnNames);
            return result.FirstOrDefault();
        }


        /// <summary>
        /// Insert a set new records into the table. They are inserted in the order provided.
        /// If one insert fails, they are all rolled back.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ModelType>> InsertMany(IEnumerable<ModelType> models, IEnumerable<string> columnNames)
        {
            var columnNamesList = columnNames.ToList();
            columnNamesList = columnNamesList ?? this.AllColumnNames.ToList();
            //remove the ID column because this is an insert
            columnNamesList.RemoveIfPresent("id");

            await BeforeInsert(models, columnNamesList);

            using (var connection = ConnectionManager.CreateConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var model in models)
                    {
                        var columnNameString = string.Join(",", columnNamesList);
                        var valuesNameString = "@" + string.Join(",@", columnNamesList);
                        var sql = $@"
                            insert into {this.TableName}({columnNameString})
                            values({valuesNameString});
                            select LAST_INSERT_ID();
                        ";
                        var queryParams = this.GetQueryParams(model, columnNamesList);
                        model.Id = await connection.ExecuteScalarAsync<int>(sql, queryParams);
                    }
                    transaction.Commit();
                }
            }
            await AfterInsert(models, columnNamesList);
            return models;
        }

        public virtual async Task BeforeInsert(IEnumerable<ModelType> models, List<string> columnNames)
        {
            await Task.CompletedTask;
        }


        public virtual async Task AfterInsert(IEnumerable<ModelType> models, List<string> columnNames)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Insert a new record into the table
        /// </summary>
        /// <param name="model"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public async Task<ModelType> Update(ModelType model, IEnumerable<string> columnNames)
        {
            var response = await this.UpdateMany(new ModelType[] { model }, columnNames);
            return response.FirstOrDefault();
        }

        /// <summary>
        /// Update a set of records. If one update fails, the entire transaction is rolled back (meaning all updates fail).
        /// </summary>
        /// <param name="models"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ModelType>> UpdateMany(IEnumerable<ModelType> models, IEnumerable<string> columnNames)
        {
            columnNames = columnNames ?? this.AllColumnNames.ToList();
            //the id column is required because this is an update
            if (columnNames.Contains("id") == false)
            {
                throw new Exception($"Cannot perform update on {typeof(ModelType).Name}: id column is missing and is required");
            }
            await this.BeforeUpdate(models, columnNames);

            using (var connection = ConnectionManager.CreateConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var model in models)
                    {
                        //remove the id column name because we won't ever set that value through code
                        columnNames = columnNames.ToList();
                        ((List<string>)columnNames).Remove("id");

                        var columnNameString = string.Join(",", columnNames);
                        var valuesNameString = "@" + string.Join(",@", columnNames);

                        var valuesString = string.Join(",", columnNames.Select(columnName => $"{columnName}=@{columnName}"));
                        var sql = $@"
                            update {this.TableName}
                            set {valuesString}
                            where id=@id
                        ";
                        var queryParams = this.GetQueryParams(model, columnNames);

                        //add the parameter
                        queryParams.Add("id", model.Id);

                        await connection.ExecuteAsync(sql, queryParams);
                    }
                    transaction.Commit();
                    return models;
                }
            }
        }

        /// <summary>
        /// An overridable method called before updates occur
        /// </summary>
        /// <param name="models"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public virtual async Task BeforeUpdate(IEnumerable<ModelType> models, IEnumerable<string> columnNames)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Delete a record by its id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task DeleteById(int id)
        {
            await this.DeleteByIds(new int[] { id });
        }

        /// <summary>
        /// Delete a set of records by ids
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task DeleteByIds(IEnumerable<int> ids)
        {
            using (var connection = ConnectionManager.CreateConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    //run the child's beforeDelete function
                    await this.BeforeDelete(ids, transaction);

                    //delete the records
                    await connection.ExecuteAsync($@"
                        delete from {this.TableName} 
                        where id in @ids"
                    , new { ids = ids });

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Given a model and a list of column names, get the parameters used for a dynamic query in dapper
        /// </summary>
        /// <param name="model"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        private Dapper.DynamicParameters GetQueryParams(ModelType model, IEnumerable<string> columnNames)
        {
            var queryParams = new Dapper.DynamicParameters();
            var type = model.GetType();
            foreach (var columnName in columnNames)
            {
                var propertyName = columnName.Substring(0, 1).ToUpper() + columnName.Substring(1);

                System.Reflection.FieldInfo field;
                System.Reflection.PropertyInfo property;

                if ((field = type.GetField(propertyName)) != null)
                {
                    queryParams.Add(columnName, field.GetValue(model));
                }
                else if ((property = type.GetProperty(propertyName)) != null)
                {
                    queryParams.Add(columnName, property.GetValue(model));

                }
                else
                {
                    throw new Exception($"Unable to find property '{propertyName}' on type {type.FullName}");
                }
            }
            return queryParams;
        }

        public void Clear()
        {
        }

        /// <summary>
        /// A function to be called before a delete call. this allows the child to pre-delete other records first
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        protected virtual async Task BeforeDelete(IEnumerable<int> ids, IDbTransaction transaction)
        {
            await Task.FromResult(false);
        }

        /// <summary>
        /// Apply the limit and offset parts of the query.
        /// </summary>
        /// <returns></returns>
        public string ApplyLimitersToQuery(string sql, int? top, int? skip)
        {
            if (top == null && skip == null)
            {
                //apply no limit
                return sql;
            }
            else
            {
                top = top ?? int.MaxValue;
                skip = skip ?? 0;
                return $"{sql} limit {top} offset {skip}";
            }
        }

        /// <summary>
        /// Add an orderby statement to the query
        /// </summary>
        /// <returns></returns>
        public string ApplyOrderToQuery(string sql, string sortField, string sortDirection)
        {
            //apply no order if a field was not provided
            if (sortField == null)
            {
                return sql;
            }
            else
            {
                var sanitized = this.SanitizeColumnNames(new[] { sortField }).FirstOrDefault();
                if (sanitized == null)
                {
                    throw new Exception($"Cannot sort by {sortField}");
                }
                //default to ascending unless descending explicitly provided
                sortDirection = sortDirection.ToLower() == "desc" ? "desc" : "asc";
                return $"{sql} order by {sortField} {sortDirection}";
            }
        }
    }
    public class PostQueryProcessor<ModelType>
    {
        public PostQueryProcessor(string propertyName, IEnumerable<string> requiredColumns, Func<IEnumerable<ModelType>, Task<IEnumerable<ModelType>>> callback)
        {
            this.PropertyNames = new[] { propertyName };
            this.RequiredColumns = requiredColumns;
            this.Callback = callback;
        }

        public PostQueryProcessor(IEnumerable<string> propertyNames, IEnumerable<string> requiredColumns, Func<IEnumerable<ModelType>, Task<IEnumerable<ModelType>>> callback)
        {
            this.PropertyNames = propertyNames;
            this.RequiredColumns = requiredColumns;
            this.Callback = callback;
        }
        public IEnumerable<string> PropertyNames;
        public IEnumerable<string> RequiredColumns;
        public Func<IEnumerable<ModelType>, Task<IEnumerable<ModelType>>> Callback;
    }

}