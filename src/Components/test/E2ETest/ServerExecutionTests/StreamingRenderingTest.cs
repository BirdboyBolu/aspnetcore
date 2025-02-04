// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.E2ETesting;
using TestServer;
using Xunit.Abstractions;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests;

public class StreamingRenderingTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup>>
{
    public StreamingRenderingTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void CanRenderNonstreamingPageWithoutInjectingStreamingMarkers()
    {
        Navigate(ServerPathBase);

        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Assert.DoesNotContain("<blazor-ssr", Browser.PageSource);
    }

    [Fact]
    public void CanPerformStreamingRendering()
    {
        Navigate($"{ServerPathBase}/streaming");

        // Initial "waiting" state
        Browser.Equal("Streaming Rendering", () => Browser.Exists(By.TagName("h1")).Text);
        var getStatusText = () => Browser.Exists(By.Id("status"));
        var getDisplayedItems = () => Browser.FindElements(By.TagName("li"));
        Assert.Equal("Waiting for more...", getStatusText().Text);
        Assert.Empty(getDisplayedItems());

        // Can add items
        for (var i = 1; i <= 3; i++)
        {
            // Each time we click, there's another streaming render batch and the UI is updated
            Browser.FindElement(By.Id("add-item-link")).Click();
            Browser.Collection(getDisplayedItems, Enumerable.Range(1, i).Select<int, Action<IWebElement>>(index =>
            {
                return actualItem => Assert.Equal($"Item {index}", actualItem.Text);
            }).ToArray());
            Assert.Equal("Waiting for more...", getStatusText().Text);

            // These are insta-removed so they don't pollute anything
            Browser.DoesNotExist(By.TagName("blazor-ssr"));
        }

        // Can finish the response
        Browser.FindElement(By.Id("end-response-link")).Click();
        Browser.Equal("Finished", () => getStatusText().Text);
    }
}
