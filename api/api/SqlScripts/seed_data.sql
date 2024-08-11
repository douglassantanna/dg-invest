-- INSER USER
INSERT INTO [dbo].[Users]
( 
 [FullName], [Email], [Password], [Role], [EmailConfirmed]
)
VALUES
( 
 'Theo', 'theo@gmail.com', 'any-password', 1, 0
)


-- INSERT DEFAULT ACCOUNT
INSERT INTO [dbo].[Accounts]
(
  [Id], [UserId], [Balance]
)
VALUES
(
  1, 1, 500 
)

-- INSERT DEFAULT ACCOUNT TRANSACTIONS
INSERT INTO [dbo].[AccountTransactions]
(
  [Id], [Date], [TransactionType], [Amount], [ExchangeName], [Notes], [CryptoCurrentPrice], [AccountId], [CryptoAssetId]
)
VALUES
(
  1, "2024-01-01 00:00:00", 1, 1000, "", "", 0, 1, 0  
),
(
  2, "2024-05-24 22:06:12", 5, 100, "Binance", "", 0.3754, 1, 2  
)
(
  3, "2024-05-24 22:06:12", 5, 100, "Binance", "", 0.3754, 1, 2  
)


-- INSERT DEFAULT CRYPTOS
INSERT INTO [dbo].[Cryptos]
( 
 [Name], [Symbol], [Image], [CoinMarketCapId]
)
VALUES
( 
 'Bitcoin', 'BTC', '', 1
),
( 
 'Chainlink', 'LINK', '', 1975
),
( 
 'Secret', 'SCRT', '', 5604
),
( 
 'Solana', 'SOL', '', 5426
)

-- INSERT DEFAULT ACCOUNT TRANSACTIONS
INSERT INTO [dbo].[CryptoAssets]
(
  [Id], [UserId], [CryptoCurrency], [Balance], [AveragePrice], [Symbol], [CurrencyName], [CreatedAt], [Deleted], [CoinMarketCapId], [TotalInvested]
)
VALUES
(
  1, 1, "BTC", 3.0436, 0, "BTCUSD", "USD", "2024-05-24 22:06:12", 0, 1, 178326.10
),
(
  2, 1, "SCRT", 2877.00, 0, "SCRTUSD", "USD", "2024-05-25 13:06:12", 0, 5604, 823.73
),
GO