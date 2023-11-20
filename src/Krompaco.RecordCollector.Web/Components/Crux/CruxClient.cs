using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Krompaco.RecordCollector.Web.Components.Crux;

public class CruxClient
{
    private readonly HttpClient httpClient;

    private readonly ILogger<CruxClient> logger;

    public CruxClient(HttpClient httpClient, ILogger<CruxClient> logger)
    {
        this.httpClient = httpClient;
        this.httpClient.BaseAddress = new Uri("https://chromeuxreport.googleapis.com/v1/");

        this.logger = logger;
    }

    public async Task<Record?> GetRecord(string url, string formFactor)
    {
        var key = Environment.GetEnvironmentVariable("GOOGLE_PAGE_SPEED_API_KEY");

        if (string.IsNullOrWhiteSpace(key) || key == "-")
        {
            return null;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, this.httpClient.BaseAddress + "records:queryRecord?key=" + key);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(new { formFactor, url }), Encoding.UTF8, "application/json");
        var response = await this.httpClient.SendAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            this.logger.LogError($"Error getting CrUX data for {url} ({response.StatusCode})");
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();

        try
        {
            return JsonSerializer.Deserialize<Root>(content)?.Record;
        }
        catch (Exception e)
        {
            this.logger.LogError($"Error parsing CrUX data for {url} ({e.Message})");
            return null;
        }
    }
}

public class CollectionPeriod
{
    [JsonPropertyName("firstDate")]
    public FirstDate FirstDate { get; set; }

    [JsonPropertyName("lastDate")]
    public LastDate LastDate { get; set; }
}

public class CumulativeLayoutShift
{
    [JsonPropertyName("histogram")]
    public List<HistogramCumulativeLayoutShift> Histogram { get; set; }

    [JsonPropertyName("percentiles")]
    public PercentilesCumulativeLayoutShift Percentiles { get; set; }
}

public class ExperimentalTimeToFirstByte
{
    [JsonPropertyName("histogram")]
    public List<Histogram> Histogram { get; set; }

    [JsonPropertyName("percentiles")]
    public Percentiles Percentiles { get; set; }
}

public class FirstContentfulPaint
{
    [JsonPropertyName("histogram")]
    public List<Histogram> Histogram { get; set; }

    [JsonPropertyName("percentiles")]
    public Percentiles Percentiles { get; set; }
}

public class FirstDate
{
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("month")]
    public int? Month { get; set; }

    [JsonPropertyName("day")]
    public int? Day { get; set; }
}

public class FirstInputDelay
{
    [JsonPropertyName("histogram")]
    public List<Histogram> Histogram { get; set; }

    [JsonPropertyName("percentiles")]
    public Percentiles Percentiles { get; set; }
}

public class Histogram
{
    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }

    [JsonPropertyName("density")]
    public double Density { get; set; }
}

public class HistogramCumulativeLayoutShift
{
    [JsonPropertyName("start")]
    public string Start { get; set; }

    [JsonPropertyName("end")]
    public string End { get; set; }

    [JsonPropertyName("density")]
    public double Density { get; set; }
}

public class InteractionToNextPaint
{
    [JsonPropertyName("histogram")]
    public List<Histogram> Histogram { get; set; }

    [JsonPropertyName("percentiles")]
    public Percentiles Percentiles { get; set; }
}

public class Key
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("formFactor")]
    public string FormFactor { get; set; }
}

public class LargestContentfulPaint
{
    [JsonPropertyName("histogram")]
    public List<Histogram> Histogram { get; set; }

    [JsonPropertyName("percentiles")]
    public Percentiles Percentiles { get; set; }
}

public class LastDate
{
    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("day")]
    public int Day { get; set; }
}

public class Metrics
{
    [JsonPropertyName("cumulative_layout_shift")]
    public CumulativeLayoutShift CumulativeLayoutShift { get; set; }

    [JsonPropertyName("experimental_time_to_first_byte")]
    public ExperimentalTimeToFirstByte ExperimentalTimeToFirstByte { get; set; }

    [JsonPropertyName("first_contentful_paint")]
    public FirstContentfulPaint FirstContentfulPaint { get; set; }

    [JsonPropertyName("first_input_delay")]
    public FirstInputDelay FirstInputDelay { get; set; }

    [JsonPropertyName("interaction_to_next_paint")]
    public InteractionToNextPaint InteractionToNextPaint { get; set; }

    [JsonPropertyName("largest_contentful_paint")]
    public LargestContentfulPaint LargestContentfulPaint { get; set; }
}

public class Percentiles
{
    [JsonPropertyName("p75")]
    public int P75 { get; set; }
}

public class PercentilesCumulativeLayoutShift
{
    [JsonPropertyName("p75")]
    public string P75 { get; set; }
}

public class Record
{
    [JsonPropertyName("key")]
    public Key Key { get; set; }

    [JsonPropertyName("metrics")]
    public Metrics Metrics { get; set; }

    [JsonPropertyName("collectionPeriod")]
    public CollectionPeriod CollectionPeriod { get; set; }
}

public class Root
{
    [JsonPropertyName("record")]
    public Record Record { get; set; }
}
