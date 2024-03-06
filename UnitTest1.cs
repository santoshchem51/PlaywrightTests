using System.Text.RegularExpressions;
using Allure.Net.Commons;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Allure.Attributes;
using NUnit.Allure.Core;

namespace PlaywrightTests;

[TestFixture]
[AllureNUnit]
public class Tests : PageTest
{

    private IBrowserContext _context;
    private IPage _page;

    [Test]
    [AllureFeature("PlaywrightTest")]        
    public async Task HomepageHasPlaywrightInTitleAndGetStartedLinkLinkingtoTheIntroPage()
    {
        await Context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true
        });
        _context = Context;
        _page = Page;
        await Page.GotoAsync("https://playwright.dev");

        // Expect a title "to contain" a substring.
        await Expect(Page).ToHaveTitleAsync(new Regex("Playwright"));

        // create a locator
        var getStarted = Page.GetByRole(AriaRole.Link, new() { Name = "Get started" });

        // Expect an attribute "to be strictly equal" to the value.
        await Expect(getStarted).ToHaveAttributeAsync("href", "/docs/intro");

        // Click the get started link.
        await getStarted.ClickAsync();

        // Expects the URL to contain intro.
        await Expect(Page).ToHaveURLAsync(new Regex(".*intro"));
    }

    [Test]
    [AllureFeature("OUP Search")]
    public async Task SearchResultsPageTest()
    {        
        var headers = new Dictionary<string, string>
        {
            { "User-Agent", "SilverchairSQA" }
        };        
        var credentials = new HttpCredentials { Username = "scm6user", Password = "Scm6pwd!" };

        // Create a new browser instance with headless set to false.
        var browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false // This will run the browser in headless mode.
        });

        // Create a new context with the specified credentials and headers.       

        var context = await browser.NewContextAsync(new BrowserNewContextOptions()
        {
            HttpCredentials = credentials,
            ExtraHTTPHeaders = headers            
        });

        await context.Tracing.StartAsync(new TracingStartOptions()
        {
            Screenshots = true,
            Snapshots = true
        });
        _context = context;

        // Use the new context to create a new page.
        var page = await context.NewPageAsync();
        _page = page;

        await page.Context.SetExtraHTTPHeadersAsync(headers);

        await page.GotoAsync("https://academic.oupdev.silverchair.com/search-results?page=1&q=*&fl_SiteID=191&SearchSourceType=1");

        await page.GetByRole(AriaRole.Textbox, new() { Name = "Enter search term" }).ClickAsync();
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Enter search term" }).FillAsync("endocrinology");
        await page.GetByRole(AriaRole.Link, new() { Name = " Search" }).ClickAsync();


        await page.WaitForLoadStateAsync(LoadState.NetworkIdle,new PageWaitForLoadStateOptions()
        {
            Timeout = 60000
        });
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "endocrinology", Exact = true })).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
        {
            Timeout = 60000
        });

       // await browser.CloseAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        // Define the path for the trace file
        var tracePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"trace-{TestContext.CurrentContext.Test.Name}.zip");

        // Stop tracing and save the trace to a file
        await _context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });

        // Attach the trace file to the Allure report
        if (File.Exists(tracePath))
        {
            AllureApi.AddAttachment("Trace", "application/zip", File.ReadAllBytes(tracePath), "zip");
        }
        
        var video = _page.Video;
        
        // Close the context        
        await _page.CloseAsync();
        await Page.CloseAsync();
        await _context.CloseAsync();

        if (video != null)        
        {
            // Stop the video recording before closing the context.            
            var videoPath = await video.PathAsync();
            if (File.Exists(videoPath))
            {
                AllureApi.AddAttachment("Video", "video/webm", File.ReadAllBytes(videoPath), $"{TestContext.CurrentContext.Test.Name}_webm");
            }
        }         
        // Optionally, add a link to the trace file in the Allure report
        AllureApi.AddLink("Trace File",$"https://trace.playwright.dev?trace={tracePath}");
    }
}