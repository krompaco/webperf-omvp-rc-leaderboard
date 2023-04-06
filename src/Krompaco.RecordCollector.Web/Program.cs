using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Krompaco.RecordCollector.Web;
using Krompaco.RecordCollector.Web.Extensions;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container
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

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline
app.Logger.LogInformation("\r\nRecord Collector Version 2.0\r\n");

app.UseRequestLocalization();

app.UseDeveloperExceptionPage();

app.UseStaticFiles();

app.UseRouting();

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
            }

            await context.Response.WriteAsync(doc.DocumentNode.OuterHtml);
        }
        else
        {
            await next();
        }
    });
}

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

        foreach (var node in doc.DocumentNode.Descendants())
        {
            if (node.NodeType != HtmlNodeType.Element)
            {
                continue;
            }

            if (node.Name == "p" && node.Attributes.Contains("class") && node.Attributes["class"].Value == "inline-block my-3 px-2 py-1 rounded-md")
            {
                node.Attributes["class"].Value += " font-bold " + GetRatingClassName(node.InnerText);
            }

            if (node.Name == "p" && node.Attributes.Contains("class") && node.Attributes["class"].Value == "inline-block mt-4 text-xl tracking-tight font-bold md:text-2xl px-2 py-1 rounded-md")
            {
                node.Attributes["class"].Value += " font-bold " + GetRatingClassName(node.InnerText);
            }

            if (node.Name == "li" && node.Attributes.Contains("class") && node.Attributes["class"].Value == "text-xs")
            {
                var content = node.InnerHtml
                    .Replace("<p>", string.Empty)
                    .Replace("</p>", string.Empty)
                    .Replace("!!", "!");

                node.InnerHtml = "<span class=\"flex items-center p-[3px] min-h-[31px]\"><span>" + content + "</span></span>";

                var match = Regex.Match(node.InnerHtml, @"\( (.*) rating \)");

                if (match.Success && match.Groups.Count > 0)
                {
                    var rating = match.Groups[1].Value;
                    node.InnerHtml = node.InnerHtml.Replace(match.Groups[0].Value, "</span><span class=\"flex-none ml-3 " + GetRatingClassName(rating) + " px-[3px] py-[1px] rounded-md font-bold\">" + rating);
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

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "rc-content-report",
        pattern: "rc-content-report",
        defaults: new { controller = "Content", action = "Report" });

    endpoints.MapControllerRoute(
        name: "rc-content-properties",
        pattern: "rc-content-properties",
        defaults: new { controller = "Content", action = "Properties" });

    endpoints.MapControllerRoute(
        name: "files",
        pattern: "{**path}",
        defaults: new { controller = "Content", action = "Files" });
});

app.Logger.LogInformation($"In {app.Environment.EnvironmentName} using {builder.Configuration.GetAppSettingsContentRootPath()}");

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
            ratingClass = "bg-green-100";
        }

        return ratingClass;
    }
}
