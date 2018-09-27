using System.IO;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Linq;

namespace Company.Function
{
    public class RatingModel
    {
        public Guid id { get; set; }
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public DateTime TimeStamp { get; set; }
        public string LocationName { get; set; }
        public int Rating { get; set; }
        public string UserNotes { get; set; }
        // public int magicNumber { get; set; }

        // public double sentimentScore { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

    }
    public static class GetRatings
    {
        [FunctionName("GetRatings")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            TraceWriter log
        )
        {
            var option = new FeedOptions { EnableCrossPartitionQuery = true };
            var limit = 100;
            var userIdParam = req.Query["userId"];
            var limitQueryParameter = req.Query["limit"];
            
            if (!string.IsNullOrWhiteSpace(limitQueryParameter))
                limit = int.Parse(limitQueryParameter);

            var collectionUri = UriFactory.CreateDocumentCollectionUri("RatingsDB", "Ratings");
            var endpointUrl = new Uri("https://hacker12.documents.azure.com:443/");
            var cosmosKey = "kMox9lT5nplspflT4HDRKttMj3M8GUZlsMIjqFnmPcoMMhP3sqVJfbIVt3bum3boU83QWfT0wq4p4yhHj4nMvg==";

            DocumentClient dClient = new DocumentClient(endpointUrl, cosmosKey);


            var results = dClient.CreateDocumentQuery<RatingModel>(collectionUri, option)
                .Where(x => x.UserId == new Guid(userIdParam))
                .AsEnumerable().FirstOrDefault();

            return new OkObjectResult(results);
        }
    }
}
