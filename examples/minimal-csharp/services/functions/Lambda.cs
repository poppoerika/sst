using System.Collections.Generic;
using System.Net;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Momento.Sdk;
using Momento.Sdk.Auth;
using Momento.Sdk.Config;
using Momento.Sdk.Exceptions;
using Momento.Sdk.Responses;
using System;
using System.Threading.Tasks;


// Assembly attribute to concert the Lambda function's JSON input to a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Api
{
    public class Handlers
    {
        static ICredentialProvider authProvider = new EnvMomentoTokenProvider("MOMENTO_AUTH_TOKEN");
        const string CACHE_NAME = "erika-cache";
        const string KEY = "MyKey";
        const string VALUE = "MyData";
        static TimeSpan DEFAULT_TTL = TimeSpan.FromSeconds(60);

        readonly SimpleCacheClient client = createCacheClient();
        static private SimpleCacheClient createCacheClient()
        {
            Console.WriteLine("CONSTRUCTING CACHE CLIENT");
            return new SimpleCacheClient(Configurations.Laptop.Latest(), authProvider, DEFAULT_TTL);
        }

        private async Task<string> getCacheValueAsync(SimpleCacheClient client)
        {
            Console.WriteLine($"\nGetting value for key: {KEY} for the first time");
            CacheGetResponse getResponse = await client.GetAsync(CACHE_NAME, KEY);
            if (getResponse is CacheGetResponse.Hit getHit)
            {
                Console.WriteLine($"Looked up value: {getHit.ValueString}, Stored value: {VALUE} first time");
                return getHit.ValueString;
            }

            Console.WriteLine($"\nSetting value: {VALUE} for key: {KEY}");
            var setResponse = await client.SetAsync(CACHE_NAME, KEY, VALUE);
            if (setResponse is CacheSetResponse.Error setError)
            {
                // Warn the user of the error and exit.
                Console.WriteLine($"Error setting value: {setError.Message}. Exiting.");
                Environment.Exit(1);
            }

            Console.WriteLine($"\nGetting value for key: {KEY} for the second time");
            CacheGetResponse getResponseTwo = await client.GetAsync(CACHE_NAME, KEY);
            if (getResponseTwo is CacheGetResponse.Hit getHitTwo)
            {
                Console.WriteLine($"Looked up value: {getHitTwo.ValueString}, Stored value: {VALUE} for the second time");
                return getHitTwo.ValueString;
            }
            else if (getResponseTwo is CacheGetResponse.Miss)
            {
                // This shouldn't be fatal but should be reported.
                Console.WriteLine($"Error: got a cache miss for {KEY}!");
            }
            else if (getResponseTwo is CacheGetResponse.Error getError)
            {
                // Also not considered fatal.
                Console.WriteLine($"Error getting value: {getError.Message}!");
            }
            return null;
        }

        public async Task<string> Handler(APIGatewayHttpApiV2ProxyRequest request)
        {
            return await getCacheValueAsync(client);
            //return new APIGatewayHttpApiV2ProxyResponse
            //{
            //    StatusCode = (int)HttpStatusCode.OK,
            //    Body = $"Hello, Bye! Your request was received at {request.RequestContext.Time}.",
            //    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            //};
        }
    }
}
