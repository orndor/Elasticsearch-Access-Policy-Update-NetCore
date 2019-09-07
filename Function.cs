using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Elasticsearch;
using Amazon.Elasticsearch.Model;

namespace Elasticsearch_Access_Policy_Update_NetCore
{
    public class Function
    {
        private static async Task Main(string[] args)
        {
            Action<FunctionInput, ILambdaContext> func = FunctionHandler;
            using(var handlerWrapper = HandlerWrapper.GetHandlerWrapper(func, new JsonSerializer()))
            using(var bootstrap = new LambdaBootstrap(handlerWrapper))
            {
                await bootstrap.RunAsync();
            }
        }

        public static void FunctionHandler(FunctionInput input, ILambdaContext context)
        {
            var es_domain = "Insert ES Domain Name Here";
            var es_arn = "Insert ES full ARN Here";
            var ec2client = new AmazonEC2Client();
            var result = ec2client.DescribeInstancesAsync(new DescribeInstancesRequest
            {
                InstanceIds = new List<string>
                {
                    input.Instanceid
                }
            });
            var publicIP = result.Result.Reservations[0].Instances[0].PublicIpAddress;
            var newAccessPolicy = $"{{\"Version\":\"2012-10-17\",\"Statement\":[{{\"Effect\":\"Allow\",\"Principal\":{{\"AWS\":\"*\"}},\"Action\":\"es:*\",\"Resource\":\"{es_arn}\",\"Condition\":{{\"IpAddress\":{{\"aws:SourceIp\":[\"{publicIP}\"]}}}}}}]}}";
            var esclient = new AmazonElasticsearchClient();
            var updateAccessPolicy = esclient.UpdateElasticsearchDomainConfigAsync(new UpdateElasticsearchDomainConfigRequest
            {
                DomainName = es_domain,
                AccessPolicies = newAccessPolicy
            }).Result;
        }

        public class FunctionInput
        {
            public string Instanceid { get; set; }
        }
    }
}
