using Amazon;
using Amazon.CostExplorer;
using Amazon.CostExplorer.Model;
using Amazon.Lambda.Core;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using BillingReportLambda.Parameters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BillingReportLambda
{
    public class Function
    {
        private static readonly RegionEndpoint region = RegionEndpoint.APNortheast1;

        // 環境変数
        private readonly string WEBHOOK_URL = Environment.GetEnvironmentVariable("WEBHOOK_URL");
        private readonly string SLACK_CHANNEL = Environment.GetEnvironmentVariable("SLACK_CHANNEL");

        private const int RATE = 110;
        private const string METRICS = "UnblendedCost";

        public async Task<bool> FunctionHandler(ILambdaContext context)
        {
            // AWS側の集計に時間を要するため、2日前の利用量を集計する
            var start = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd");
            var end = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            var ceRequest = new GetCostAndUsageRequest
            {
                TimePeriod = new DateInterval()
                {
                    Start = start,
                    End = end
                },
                Granularity = "DAILY",
                Metrics = new List<string>() { METRICS },
                GroupBy = new List<GroupDefinition>()
                {
                    new GroupDefinition
                    {
                        Type = "DIMENSION",
                        Key = "SERVICE"
                    }
                }
            };

            using var ceClient = new AmazonCostExplorerClient(region);
            var ceResponse = await ceClient.GetCostAndUsageAsync(ceRequest);

            decimal total = 0;
            var fields = new List<Field>();
            foreach (var result in ceResponse.ResultsByTime)
            {
                foreach (var group in result.Groups)
                {
                    var cost = Math.Round(Convert.ToDecimal(group.Metrics[METRICS].Amount) * RATE, 0);

                    fields.Add(new Field()
                    {
                        title = group.Keys[0],
                        value = String.Format(":moneybag: {0:#,0} 円", cost),
                        @short = true
                    });

                    total += cost;
                }
            }

            var color = total == 0 ? "good" : "danger";
            var attachment = new Attachment()
            {
                fallback = "Required plain-text summery of the attachment.",
                color = color,
                pretext = String.Format("*{0} のAWS利用料は {1:#,0}円 です*", start, total),
                fields = fields,
                channel = SLACK_CHANNEL,
                username = "Daily Report"
            };

            var jsonSting = JsonConvert.SerializeObject(attachment);
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "payload",  jsonSting }
            });

            var webhookUrl = await GetWebhookUrl();
            try
            {
                using var httpClient = new HttpClient();
                await httpClient.PostAsync(webhookUrl, content);
            }
            catch (Exception e)
            {
                context.Logger.LogLine("Exception: " + e.Message);
            }

            return true;
        }

        /// <summary>
        /// パラメータストアにあるWebhook URLを取得
        /// </summary>
        /// <returns>Webhook URL</returns>
        private async Task<string> GetWebhookUrl()
        {
            var request = new GetParameterRequest()
            {
                Name = WEBHOOK_URL,
                WithDecryption = true
            };

            using var ssmClient = new AmazonSimpleSystemsManagementClient(region);
            var response = await ssmClient.GetParameterAsync(request);

            return response.Parameter.Value;
        }
    }
}
