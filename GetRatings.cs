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
using System.Net;

namespace Company.Function
{

    public static class CreateRating
    {
        [FunctionName("CreateRating")]
        public static async System.Threading.Tasks.Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            var id = new System.Guid().ToString();
            DateTime timestamp = DateTime.Now;

            //string name = req.Query["name"];

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject<RatingPayload>(requestBody);

            RatingPayload rating = (RatingPayload)data;

            var error = String.Empty;
            bool isValidUser = IsValidUser(rating.UserId);
            bool isValidProduct = IsValidProduct(rating.ProductId);
            bool isValidRating = IsValidRating(rating.Rating);

            ActionResult result;

            error = isValidUser ? error : error + $"Rating Must have valid User\n";
            error = isValidProduct ? error : error + $"Rating Must have valid Product\n";
            error = isValidRating ? error : error + $"Rating Must have valid Rating i.e. 0-5\n";

            if (isValidUser && isValidProduct && isValidRating)
            {
                var rslt = new RatingResponse();
                rslt.id = System.Guid.NewGuid().ToString();
                rslt.TimeStamp = DateTime.Now;
                rslt.UserId = rating.UserId;
                rslt.ProductId = rating.ProductId;
                rslt.LocationName = rating.LocationName;
                rslt.Rating = rating.Rating;
                rslt.UserNotes = rating.UserNotes;

                //rating.id = System.Guid.NewGuid().ToString();
                //rating.TimeStamp = DateTime.Now;

                WriteToCosmos(rslt);
                result = (ActionResult)new OkObjectResult(rating);
            }
            else
            {
                result = new BadRequestObjectResult($"{error}");
            }

            return result;
        }


        public static void WriteToCosmos(RatingResponse ratingExample)
        {
            // var cosmosUrl = new Uri()
            var collectionUri = UriFactory.CreateDocumentCollectionUri("RatingsDB", "Ratings");
            var endpointUrl = new Uri("https://hacker12.documents.azure.com:443/");
            var cosmosKey = "kMox9lT5nplspflT4HDRKttMj3M8GUZlsMIjqFnmPcoMMhP3sqVJfbIVt3bum3boU83QWfT0wq4p4yhHj4nMvg==";

            DocumentClient dClient = new DocumentClient(endpointUrl, cosmosKey);
            IDocumentQuery<RatingResponse> query = dClient.CreateDocumentQuery<RatingResponse>(collectionUri)
                .AsDocumentQuery();
            var idanIds = dClient.CreateDocumentQuery(collectionUri);
            using (dClient)
            {

                dClient.CreateDatabaseIfNotExistsAsync(new Database() { Id = "RatingsDB" }).GetAwaiter().GetResult();

                dClient.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri("RatingsDB"),
                new DocumentCollection { Id = "Ratings" }).
                GetAwaiter()
                .GetResult();


                dClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri("RatingsDB", "Ratings"), ratingExample).GetAwaiter().GetResult();


            }
            OkObjectResult results = new OkObjectResult(ratingExample);

            var idan = "blabbla";

        }


        private static bool IsValidUser(string user)
        {
            var url = "https://serverlessohuser.trafficmanager.net/api/GetUser?userId=" + user;

            try
            {
                var userData = Get(url);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public static String Get(String url)
        {
            WebRequest request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            WebResponse response = request.GetResponse();
            // Display the status.  
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            using (Stream dataStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(dataStream))
            {
                string responseFromServer = reader.ReadToEnd();
                return responseFromServer;
            }
        }

        private static bool IsValidProduct(string productId)
        {
            var url = "https://serverlessohproduct.trafficmanager.net/api/GetProduct?productId=" + productId;

            try
            {
                var productData = Get(url);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        private static bool IsValidRating(int rating)
        {
            var rslt = (rating > 0 && rating <= 5) ? true : false;
            return rslt;
        }

    }

    public class RatingPayload
    {
        public string UserId { get; set; }
        public string ProductId { get; set; }
        public string LocationName { get; set; }
        public int Rating { get; set; }
        public string UserNotes { get; set; }
    }

    public class RatingResponse
    {
        public string id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string UserId { get; set; }
        public string ProductId { get; set; }
        public string LocationName { get; set; }
        public int Rating { get; set; }
        public string UserNotes { get; set; }
    }




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
