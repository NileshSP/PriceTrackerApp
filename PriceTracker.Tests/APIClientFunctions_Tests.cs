using NUnit.Framework;
using PriceTracker;

namespace PriceTracker.Tests
{
    [TestFixture]
    public class APIClientFunctions_Tests
    {
        private readonly APIClient _apiClient;

        public APIClientFunctions_Tests()
        {
            _apiClient = new APIClient();
        }

        [Test]
        public void GetPriceForNotEmptyTest()
        {
            var response = _apiClient.GetPriceAsync();
            Assert.That(response.Result, Is.Not.Empty);
        }
    }
}