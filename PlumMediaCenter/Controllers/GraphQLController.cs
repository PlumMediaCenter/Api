using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PlumMediaCenter.Attributues;
using Newtonsoft.Json.Linq;
using PlumMediaCenter.Graphql;

namespace PlumMediaCenter.Controllers
{
    [ExceptionHandlerFilter()]
    [Route("[controller]")]
    public class GraphQLController : Controller
    {
        public GraphQLController(
            AppSchema schema,
            IDataLoaderContextAccessor accessor
        )
        {
            this.Schema = schema;
            //init a new DataLoader context on each request
            accessor.Context = new DataLoaderContext();
            DataLoaderDocumentListener = new DataLoaderDocumentListener(accessor);
        }
        private AppSchema Schema;
        private DataLoaderDocumentListener DataLoaderDocumentListener;

        [HttpGet()]
        [HttpPost()]
        [HttpPut()]
        [HttpDelete()]
        public async Task<ActionResult> GetPostPutDelete()
        {
            Body body = null;
            try
            {
                body = await GetBody();
            }
            catch (Exception e)
            {
                throw new Exception("Unable to deserialize body", e);
            }

            var graphqlResult = await new DocumentExecuter().ExecuteAsync(_ =>
                {
                    _.Schema = this.Schema;
                    _.Query = body.Query;
                    _.Inputs = body.Variables?.ToInputs();
                    _.OperationName = body.OperationName;
                    _.Listeners.Add(DataLoaderDocumentListener);

                    // options.UserContext = userContext;
                }).ConfigureAwait(false);


            var json = new DocumentWriter(indent: true).Write(graphqlResult);

            var result = new ContentResult();
            result.ContentType = "application/json";
            //if there were no errors
            if (graphqlResult.Errors == null || graphqlResult.Errors.Count == 0)
            {
                result.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                //mark this response as an error
                result.StatusCode = (int)HttpStatusCode.InternalServerError;
                var o = JsonConvert.DeserializeObject<JObject>(json);
                var errorsArray = (JArray)o.SelectToken("errors");
                errorsArray.RemoveAll();
                //append the actual errors into the list for more helpful debugging
                foreach (var error in graphqlResult.Errors)
                {
                    var prettyError = new PrettyError(error);
                    errorsArray.Add(JObject.Parse(JsonConvert.SerializeObject(prettyError)));
                }
                json = JsonConvert.SerializeObject(o);
            }
            result.Content = json;
            return result;
        }


        private async Task<Body> GetBody()
        {
            Body body = new Body();
            if (Request.Method.ToUpper() != "GET")
            {
                string bodyText = await (new StreamReader(Request.Body, Encoding.UTF8)).ReadToEndAsync();

                if (Request.ContentType == "application/graphql")
                {
                    body.Query = bodyText;
                }
                else
                {
                    var requestBody = JsonConvert.DeserializeObject<RequestBody>(bodyText);
                    body.Query = requestBody.query;
                    body.OperationName = requestBody.operationName;

                    if (requestBody.variables != null)
                    {
                        body.Variables = JsonConvert.SerializeObject(requestBody.variables);
                    }
                }
            }
            //override body values with querystring values
            if (Request.Query.ContainsKey("query"))
            {
                body.Query = Request.Query["query"];
            }
            if (Request.Query.ContainsKey("variables"))
            {
                body.Variables = Request.Query["variables"];
            }
            if (Request.Query.ContainsKey("operationName"))
            {
                body.OperationName = Request.Query["operationName"];
            }

            return body;
        }

        public class Body
        {
            public string Query;
            public string Variables;
            public string OperationName;
        }
        public class RequestBody
        {
            public string query;
            public Dictionary<string, object> variables;
            public string operationName;
        }

    }
}


class ScriptController
{
    [HttpGet]
    [Route("script")]
    public HttpResponse GetScript()
    {
        var fileContents = File.ReadAllText("dist/app.min.js");
        var edrsSettings = new
        {
            someSetting = AppSettings["someSetting"],
            someOtherSetting = AppSettings["someOtherSetting"]
        };
        fileContents += ";window.edrsSettings = " + Newtonsoft.Json.JsonConvert.SerializeObject(edrsSettings);

        var response = Request.CreateResponse(System.Net.HttpStatusCode.OK, fileContents, new TextPlainFormatter());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-javascript");
        return response;
    }
}