using System.Linq.Expressions;
using api.Cryptos.Dtos;
using api.Data;
using api.Models.Cryptos;
using api.Shared;
using MediatR;

namespace api.Cryptos.Queries;
public class ListCryptoAssetsQueryCommand : IRequest<PageList<ViewMinimalCryptoAssetDto>>
{
    public string? CryptoCurrency { get; set; } = string.Empty;
    public string? CurrencyName { get; set; } = string.Empty;
    public string? SortColumn { get; set; } = string.Empty;
    public string? SortOrder { get; set; } = "ASC";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; }
}
public class ListCryptoAssetsQueryCommandHandler : IRequestHandler<ListCryptoAssetsQueryCommand, PageList<ViewMinimalCryptoAssetDto>>
{
    private readonly DataContext _context;

    public ListCryptoAssetsQueryCommandHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<PageList<ViewMinimalCryptoAssetDto>> Handle(ListCryptoAssetsQueryCommand request, CancellationToken cancellationToken)
    {
        IQueryable<CryptoAsset> cryptoAssetQuery = _context.CryptoAssets;

        int maxPageSize = 20;

        if (request.PageSize > maxPageSize)
        {
            request.PageSize = maxPageSize;
        }

        if (!string.IsNullOrEmpty(request.CryptoCurrency))
        {
            request.CryptoCurrency = request.CryptoCurrency?.ToLower().Trim();
            cryptoAssetQuery = cryptoAssetQuery.Where(x => x.CryptoCurrency.ToLower().Contains(request.CryptoCurrency));
        }

        if (!string.IsNullOrEmpty(request.CurrencyName))
        {
            request.CurrencyName = request.CurrencyName?.ToLower().Trim();
            cryptoAssetQuery = cryptoAssetQuery.Where(x => x.CurrencyName.ToLower().Contains(request.CurrencyName));
        }

        if (request.SortOrder?.ToUpper() == "DESC")
        {
            cryptoAssetQuery = cryptoAssetQuery.OrderByDescending(GetSortProperty(request));
        }
        else
        {
            cryptoAssetQuery = cryptoAssetQuery.OrderBy(GetSortProperty(request));
        }

        var collection = cryptoAssetQuery.Select(x => new ViewMinimalCryptoAssetDto(x.Id,
                                                                                    x.CurrencyName,
                                                                                    x.CryptoCurrency,
                                                                                    x.Symbol));

        var pagedCollection = await PageList<ViewMinimalCryptoAssetDto>.CreateAsync(collection,
                                                                                    request.Page,
                                                                                    request.PageSize);
        return pagedCollection;

    }

    private static Expression<Func<CryptoAsset, object>> GetSortProperty(ListCryptoAssetsQueryCommand request)
    {
        return request.SortColumn?.ToLower() switch
        {
            "currency_name" => currency => currency.CurrencyName,
            "crypto_name" => currency => currency.CryptoCurrency,
            "balance" => currency => currency.Balance,
            _ => currency => currency.Id
        };
    }
}
