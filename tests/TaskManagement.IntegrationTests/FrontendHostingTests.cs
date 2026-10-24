using System.Net;
using TaskManagement.IntegrationTests.Infrastructure;

namespace TaskManagement.IntegrationTests;

[TestFixture]
public class FrontendHostingTests : ApiTestBase
{
    [Test]
    public async Task Root_ServesTheTaskManagerPage()
    {
        var response = await Client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            Assert.That(html, Does.Contain("<title>Task Manager</title>"));
        });
    }

    [TestCase("/css/styles.css", "text/css")]
    [TestCase("/js/app.js", "text/javascript")]
    [TestCase("/js/api.js", "text/javascript")]
    public async Task StaticAssets_AreServedWithCorrectContentType(string path, string mediaType)
    {
        var response = await Client.GetAsync(path);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(mediaType));
        });
    }
}
