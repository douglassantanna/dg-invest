﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using api.Data;

#nullable disable

namespace api.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.11");

            modelBuilder.Entity("api.Cryptos.Models.Address", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AddressName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("AddressValue")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("CryptoAssetId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Deleted")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("CryptoAssetId");

                    b.ToTable("Address");
                });

            modelBuilder.Entity("api.Cryptos.Models.Crypto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CoinMarketCapId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Image")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar");

                    b.HasKey("Id");

                    b.ToTable("Cryptos");
                });

            modelBuilder.Entity("api.Models.Cryptos.CryptoAsset", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("AveragePrice")
                        .HasPrecision(18, 8)
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Balance")
                        .HasPrecision(18, 8)
                        .HasColumnType("TEXT");

                    b.Property<int>("CoinMarketCapId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("CryptoCurrency")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar");

                    b.Property<string>("CurrencyName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar");

                    b.Property<bool>("Deleted")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar");

                    b.Property<decimal>("TotalInvested")
                        .HasPrecision(18, 8)
                        .HasColumnType("TEXT");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CryptoAssets");
                });

            modelBuilder.Entity("api.Models.Cryptos.CryptoTransaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Amount")
                        .HasPrecision(18, 8)
                        .HasColumnType("TEXT");

                    b.Property<int?>("CryptoAssetId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Enabled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ExchangeName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar");

                    b.Property<decimal>("Price")
                        .HasPrecision(18, 8)
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("PurchaseDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("TransactionType")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("CryptoAssetId");

                    b.ToTable("CryptoTransactions");
                });

            modelBuilder.Entity("api.Users.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar");

                    b.Property<int>("Role")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("api.Cryptos.Models.Address", b =>
                {
                    b.HasOne("api.Models.Cryptos.CryptoAsset", "CryptoAsset")
                        .WithMany("Addresses")
                        .HasForeignKey("CryptoAssetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CryptoAsset");
                });

            modelBuilder.Entity("api.Models.Cryptos.CryptoAsset", b =>
                {
                    b.HasOne("api.Users.Models.User", null)
                        .WithMany("CryptoAssets")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("api.Models.Cryptos.CryptoTransaction", b =>
                {
                    b.HasOne("api.Models.Cryptos.CryptoAsset", null)
                        .WithMany("Transactions")
                        .HasForeignKey("CryptoAssetId");
                });

            modelBuilder.Entity("api.Models.Cryptos.CryptoAsset", b =>
                {
                    b.Navigation("Addresses");

                    b.Navigation("Transactions");
                });

            modelBuilder.Entity("api.Users.Models.User", b =>
                {
                    b.Navigation("CryptoAssets");
                });
#pragma warning restore 612, 618
        }
    }
}