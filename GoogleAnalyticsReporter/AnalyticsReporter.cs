using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using MongoDB.Driver;

namespace GoogleAnalyticsReporter
{
    static internal class AnalyticsReporter
    {
        public static async Task Run()
        {
            var service = GetAuthenticatedService();
            var requiredMetrics = GetRequiredMetrics();
            var response = GetReport(requiredMetrics, service);
            var metricTotals = GetTotals(requiredMetrics, response);
            WriteToDB(metricTotals);
        }

        private static void WriteToDB(int[] metricTotals)
        {
            var client = new MongoClient(ConfigurationManager.AppSettings["MongoConnectionString"]);
            var database = client.GetDatabase(ConfigurationManager.AppSettings["MongoDatabaseName"]);
            var collection = database.GetCollection<GoogleAnalyticsSnapshot>("GoogleAnalyticsSnapshot");
            collection.InsertOne(new GoogleAnalyticsSnapshot(metricTotals[0], metricTotals[1]));
        }

        private static int[] GetTotals(List<Metric> requiredMetrics, GetReportsResponse response)
        {
            var metricTotals = new int[requiredMetrics.Count];

            foreach (var report in response.Reports)
            {
                var header = report.ColumnHeader;
                var metricHeaders = (List<MetricHeaderEntry>) header.MetricHeader.MetricHeaderEntries;
                var rows = (List<ReportRow>) report.Data.Rows;

                foreach (var metrics in rows.Select(row => (List<DateRangeValues>) row.Metrics))
                {
                    for (var j = 0; j < metrics.Count(); j++)
                    {
                        var values = metrics[j];
                        for (var k = 0; k < values.Values.Count() && k < metricHeaders.Count(); k++)
                        {
                            metricTotals[k] += int.Parse(values.Values[k]);
                        }
                    }
                }
            }
            return metricTotals;
        }

        private static GetReportsResponse GetReport(IList<Metric> requiredMetrics, AnalyticsReportingService service)
        {
            var dateRange = new DateRange { StartDate = "2015-06-15", EndDate = DateTime.UtcNow.ToString("yyyy-MM-dd") };
            var browser = new Dimension {Name = "ga:browser"};
            var reportRequest = new ReportRequest
            {
                ViewId = "113545803",
                DateRanges = new List<DateRange> {dateRange},
                Dimensions = new List<Dimension> {browser},
                Metrics = requiredMetrics
            };
            var requests = new List<ReportRequest> {reportRequest};
            var getReport = new GetReportsRequest {ReportRequests = requests};
            var response = service.Reports.BatchGet(getReport).Execute();
            return response;
        }

        private static List<Metric> GetRequiredMetrics()
        {
            var pageviews = new Metric {Expression = "ga:pageviews", Alias = "Page Views"};
            var uniquePageviews = new Metric {Expression = "ga:uniquePageviews", Alias = "Unique Page Views"};
            var list = new List<Metric> {pageviews, uniquePageviews};
            return list;
        }

        private static AnalyticsReportingService GetAuthenticatedService()
        {
            var credential = GoogleCredential
                .FromStream(new FileStream("service-account.json", FileMode.Open))
                .CreateScoped(AnalyticsReportingService.Scope.Analytics, AnalyticsReportingService.Scope.AnalyticsReadonly);

            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            };
            var service = new AnalyticsReportingService(initializer);
            return service;
        }
    }

    public class GoogleAnalyticsSnapshot
    {
        public DateTime DocumentCreationDate { get; set; }
        public DateTime DocumentModifiedDate { get; set; }
        public int PageViews { get; set; }
        public int UniquePageViews { get; set; }

        public GoogleAnalyticsSnapshot(int pageViews, int uniquePageViews)
        {
            PageViews = pageViews;
            UniquePageViews = uniquePageViews;
            DocumentCreationDate = DateTime.UtcNow;
            DocumentModifiedDate = DateTime.UtcNow;
        }
    }
}