using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.CoinMarketCap;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace unit_tests.Extensions
{
    public static class MockDbSetExtensions
    {
        public static Mock<DbSet<T>> BuildMockDbSet<T>(this IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            return mockSet;
        }
        public static GetQuoteResponse CreateFakeGetQuoteResponse()
        {
            var status = new Status(
                Error_code: 0,
                Error_message: null
            );

            var data = new Dictionary<string, Coin>
            {
                {
                    "1", new Coin(
                        Id: 1,
                        Name: "Bitcoin",
                        Symbol: "BTC",
                        Last_updated: DateTime.UtcNow,
                        Quote: new Quote(
                            new USD(
                                Price: 45000.00m,
                                Last_updated: DateTime.UtcNow,
                                Percent_change_24h: 2.0m
                            )
                        )
                    )
                },
                {
                    "1027", new Coin(
                        Id: 1027,
                        Name: "Ethereum",
                        Symbol: "ETH",
                        Last_updated: DateTime.UtcNow,
                        Quote: new Quote(
                            new USD(
                                Price: 3000.00m,
                                Last_updated: DateTime.UtcNow,
                                Percent_change_24h: 1.5m
                            )
                        )
                    )
                }
            };

            return new GetQuoteResponse(status, data);
        }
    }
}
