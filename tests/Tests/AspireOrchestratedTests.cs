using System.Threading.Tasks;

namespace Moser.Archetype.Tests;

[Collection("DistributedAppTestCollection")]
public class AspireOrchestratedTests(DistributedAppTestFixture fixture)
{
    [Fact]
    public async Task HealthCheck_ValidRequest_ShouldReturnResponse()
    {
        // Act
        //var response = await fixture._httpClient!.GetAsync("/");
        var response2 = await fixture.HttpClient!.GetAsync("/");

        // Assert
        //Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }
}
