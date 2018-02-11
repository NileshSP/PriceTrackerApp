using NUnit.Framework;
using System;
using System.Collections.Generic;
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

        [Test]
        public void GetAveragesForNotEmptyTest()
        {
            Dictionary<int,decimal> lstPrices = new Dictionary<int,decimal>() 
            {
                {1, Decimal.Parse("0.001234")},
                {2, Decimal.Parse("0.005678")},
                {3, Decimal.Parse("0.008901")},
                {4, Decimal.Parse("0.00345687")},
                {5, Decimal.Parse("0.002657")},
                {6, Decimal.Parse("0.0089766")},
            };
            var response = _apiClient.GetAverage(lstPrices);
            Assert.IsNotNull(response);
            Assert.IsTrue(response != 0);
        }
    }
}