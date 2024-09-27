using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Krompaco.RecordCollector.Web;
using Krompaco.RecordCollector.Web.Components;
using Krompaco.RecordCollector.Web.Extensions;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// Add services to the container
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
builder.Services.AddHostedService<FileSystemWatcherService>();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

    // TODO: This is the place to set the default culture
    options.DefaultRequestCulture = new RequestCulture(culture: "en-US", uiCulture: "en-US");

    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new SiteRequestCultureProvider(builder.Configuration));
});

builder.Services.AddLocalization();

builder.Services.AddRazorComponents();
builder.Services.AddControllersWithViews();

using var loggerFactory = LoggerFactory.Create(builderInside =>
{
    builderInside.AddSimpleConsole(i => i.ColorBehavior = LoggerColorBehavior.Default);
});
var logger = loggerFactory.CreateLogger<Program>();

var app = builder.Build();

logger.LogInformation("Record Collector Version 3.0 with Blazor SSR!");

// Configure the HTTP request pipeline
app.UseRequestLocalization();

app.UseDeveloperExceptionPage();

app.UseStaticFiles();

app.UseRouting();

app.UseAntiforgery();

var frontendSetup = app.Configuration.GetAppSettingsFrontendSetup();

if (frontendSetup == "simplecss")
{
    // Trying out a way to strip class attributes from HTML if Simple.css is used
    app.Use(async (context, next) =>
    {
        // Way that should work to only process HTML output
        if (context.Request.Path.HasValue && context.Request.Path.Value.EndsWith("/"))
        {
            var responseBody = context.Response.Body;

            await using var newResponseBody = new MemoryStream();
            context.Response.Body = newResponseBody;
            await next();

            context.Response.Body = new MemoryStream();

            newResponseBody.Seek(0, SeekOrigin.Begin);
            context.Response.Body = responseBody;

            var html = new StreamReader(newResponseBody).ReadToEnd();

            // Using HtmlAgilityPack to modify whole document
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var paginationLinkNodes = new List<HtmlNode>();

            foreach (var node in doc.DocumentNode.Descendants())
            {
                if (node.NodeType != HtmlNodeType.Element)
                {
                    continue;
                }

                if (node.Attributes.Contains("class"))
                {
                    node.Attributes["class"].Remove();
                }

                if (node.NodeType == HtmlNodeType.Element
                    && node.Attributes.Contains("data-id")
                    && node.GetDataAttribute("id").Value == "pagination")
                {
                    var links = node.Descendants().Where(x => x.NodeType == HtmlNodeType.Element && x.Name == "a").ToList();

                    if (links.Any())
                    {
                        paginationLinkNodes.AddRange(links);
                    }
                }
            }

            foreach (var node in paginationLinkNodes)
            {
                node.Attributes.Add("class", "button");
            }

            await context.Response.WriteAsync(doc.DocumentNode.OuterHtml);
        }
        else
        {
            await next();
        }
    });
}

// Fix markup in 
app.Use(async (context, next) =>
{
    // Way that should work to only process HTML output
    if (context.Request.Path.HasValue && context.Request.Path.Value.EndsWith("/"))
    {
        var responseBody = context.Response.Body;

        await using var newResponseBody = new MemoryStream();
        context.Response.Body = newResponseBody;
        await next();

        context.Response.Body = new MemoryStream();

        newResponseBody.Seek(0, SeekOrigin.Begin);
        context.Response.Body = responseBody;

        var html = new StreamReader(newResponseBody).ReadToEnd();

        // Using HtmlAgilityPack to modify whole document
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var ratingRegex = new Regex(@"\( (.*?) rating \)", RegexOptions.Compiled);

        foreach (var node in doc.DocumentNode.Descendants())
        {
            if (node.NodeType != HtmlNodeType.Element)
            {
                continue;
            }

            if (node.Name == "p" && node.Attributes.Contains("class") && node.Attributes["class"].Value == "inline-block my-3 px-2 pt-1.5 pb-1 rounded-md")
            {
                node.Attributes["class"].Value += " font-bold " + GetRatingClassName(node.InnerText);
            }

            if (node.Name == "p" && node.Attributes.Contains("class") && node.Attributes["class"].Value == "inline-block mt-4 text-xl tracking-tight font-bold md:text-2xl px-2 pt-1.5 pb-1 rounded-md")
            {
                node.Attributes["class"].Value += " font-bold " + GetRatingClassName(node.InnerText);
            }

            if (node.Name == "a" && !node.Attributes.Contains("class"))
            {
                node.Attributes.Add("class", "link-primary outline-primary");
            }

            if (node.Name == "div" && node.Attributes.Contains("class") && node.Attributes["class"].Value.StartsWith("webperf-md "))
            {
                foreach (Match match in ratingRegex.Matches(node.InnerHtml))
                {
                    if (match.Success && match.Groups.Count > 0)
                    {
                        var rating = match.Groups[1].Value;
                        node.InnerHtml = node.InnerHtml
                            .Replace(match.Groups[0].Value, "<span class=\"inline-block ml-3 " + GetRatingClassName(rating) + " px-[3px] pt-[2px] pb-[1px] my-[1px] rounded-md font-bold\">" + rating + "</span>")
                            .Replace("<br> <span", " <span");
                    }
                }
            }
        }

        await context.Response.WriteAsync(doc.DocumentNode.OuterHtml);
    }
    else
    {
        await next();
    }
});

app.MapControllerRoute(
    name: "rc-content-report",
    pattern: "rc-content-report",
    defaults: new { controller = "Content", action = "Report" });

app.MapControllerRoute(
    name: "rc-content-properties",
    pattern: "rc-content-properties",
    defaults: new { controller = "Content", action = "Properties" });

// This is the catch all action used to serve the correct content, image or document
app.MapControllerRoute(
    name: "files",
    pattern: "{**path}",
    defaults: new { controller = "Content", action = "Files" });

logger.LogInformation($"In {app.Environment.EnvironmentName} using content from {builder.Configuration.GetAppSettingsContentRootPath()}");

app.Run();

public partial class Program
{
    public const string ToBeVisibleInTestProjects = "To be visible in test projects.";

    public static string GetRatingClassName(string rating)
    {
        var ratingClass = "bg-green-200";

        if (rating.StartsWith("0") || rating.StartsWith("1"))
        {
            ratingClass = "bg-red-300";
        }
        else if (rating.StartsWith("2"))
        {
            ratingClass = "bg-pink-200";
        }
        else if (rating.StartsWith("3"))
        {
            ratingClass = "bg-orange-200";
        }
        else if (rating.StartsWith("4"))
        {
            ratingClass = "bg-yellow-200";
        }

        return ratingClass;
    }
}
