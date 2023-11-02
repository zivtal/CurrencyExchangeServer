namespace BankOfIsrael.Service
{
    internal readonly struct GetExchangeRateItem
    {
        public string Key { get; init; }
        public double CurrentExchangeRate { get; init; }
        public double CurrentChange { get; init; }
        public int Unit { get; init; }
        public string LastUpdate { get; init; }

        public GetExchangeRateItem(string key, double currentExchangeRate, double currentChange, int unit, string lastUpdate)
        {
            Key = key;
            CurrentExchangeRate = currentExchangeRate;
            CurrentChange = currentChange;
            Unit = unit;
            LastUpdate = lastUpdate;
        }
    }

    internal readonly struct GetExchangeRatesResponse
    {
        public List<GetExchangeRateItem> ExchangeRates { get; }
        
        public GetExchangeRatesResponse(List<GetExchangeRateItem> exchangeRates)
        {
            ExchangeRates = exchangeRates;
        }
    }

    public class BaseExchange
    {
        public required string From { get; init; }
        public required string To { get; init; }
        public double Rate { get; init; }
        public required string UpdatedAt { get; init; }
    }
    
    public class ConvertResult : BaseExchange
    {
        public double Value { get; init; }
        public double Amount { get; init; }

        public override string ToString()
        {
            return $"{Amount} {From} = {Value} {To}";
        }
    }
    
    public class GetRatesResult
    {
        public List<BaseExchange> ExchangeRates { get; init; }
        public string UpdatedAt { get; init; }
        
        public GetRatesResult(List<BaseExchange> rates, string updatedAt)
        {
            ExchangeRates = rates;
            UpdatedAt = updatedAt;
        }
    }

    public class CurrencyExchange : API.Services.ApiClient
    {
        private const int RoundDigits = 2;

        private static readonly GetExchangeRateItem LocalCurrency = new GetExchangeRateItem {
            Key = "ILS",
            CurrentExchangeRate = 1,
            CurrentChange = 1,
            Unit = 1,
            LastUpdate = DateTimeOffset.UtcNow.ToString("o")
        };

        private static HashSet<string>? _currencyCodes;

        private CurrencyExchange() : base("https://boi.org.il/PublicApi") { }

        private static void CheckValidCurrencyCodes(IEnumerable<string> currencyCodesToCheck)
        {
            if (_currencyCodes == null)
                return;

            var invalidCode = currencyCodesToCheck.FirstOrDefault(code => !_currencyCodes.Contains(code));
            
            if (invalidCode != null)
            {
                throw new ArgumentException($"Invalid currency code: {invalidCode}");
            }
        }

        private static async Task<GetExchangeRatesResponse> FetchExchangeRatesAsync()
        {
            var client = new CurrencyExchange();
            
            return await client.GetAsync<GetExchangeRatesResponse>("/GetExchangeRates?asJson=true");
        }

        public static async Task<GetRatesResult> GetRatesAsync()
        {
            var data = await FetchExchangeRatesAsync();
            var exchangeRates = data.ExchangeRates.Append(LocalCurrency).ToArray();
            _currencyCodes = new HashSet<string>(exchangeRates.Select(currency => currency.Key));

            var minLastUpdate = exchangeRates.Min(er => DateTimeOffset.Parse(er.LastUpdate));

            var rates = exchangeRates.Aggregate(new List<BaseExchange>(), (conversationBulk, toCurrency) =>
            {
                conversationBulk.AddRange(
                    exchangeRates
                        .Where(fromCurrency => fromCurrency.Key != toCurrency.Key) // Avoid converting a currency to itself
                        .Select(fromCurrency =>
                        {
                            var updatedAt = DateTimeOffset.Parse(fromCurrency.LastUpdate) < DateTimeOffset.Parse(toCurrency.LastUpdate)
                                ? fromCurrency.LastUpdate
                                : toCurrency.LastUpdate;

                            return new BaseExchange
                            {
                                From = fromCurrency.Key,
                                To = toCurrency.Key,
                                Rate = Math.Round(fromCurrency.CurrentExchangeRate / fromCurrency.Unit /
                                                  (toCurrency.CurrentExchangeRate * toCurrency.Unit),8),
                                UpdatedAt = updatedAt,
                            };
                        }));

                return conversationBulk;
            });
            
            return new GetRatesResult(rates, minLastUpdate.ToString("o"));
        }

        public static async Task<ConvertResult?> ConvertAsync(string fromCurrency, string toCurrency, double value = 1)
        {
            var data = await FetchExchangeRatesAsync();
            var exchangeRates = new HashSet<GetExchangeRateItem>(data.ExchangeRates.Append(LocalCurrency));
            CheckValidCurrencyCodes(new[] { fromCurrency, toCurrency });
            
            var fromCurrencyRate = exchangeRates.FirstOrDefault(currency => currency.Key == fromCurrency);
            var toCurrencyRate = exchangeRates.FirstOrDefault(currency => currency.Key == toCurrency);
            
            var rate = fromCurrencyRate.CurrentExchangeRate / fromCurrencyRate.Unit /
                       (toCurrencyRate.CurrentExchangeRate * toCurrencyRate.Unit);
            
            var updatedAt = DateTimeOffset.Parse(toCurrencyRate.LastUpdate) < DateTimeOffset.Parse(fromCurrencyRate.LastUpdate)
                ? toCurrencyRate.LastUpdate
                : fromCurrencyRate.LastUpdate;
            
            return new ConvertResult
            {
                Amount = value,
                Value = Math.Round(value * rate, RoundDigits),
                Rate = Math.Round(rate, 8),
                From = fromCurrencyRate.Key,
                To = toCurrencyRate.Key,
                UpdatedAt = updatedAt,
            };
        }
    }
}
