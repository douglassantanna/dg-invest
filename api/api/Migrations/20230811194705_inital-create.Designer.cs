﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using api.Data;

#nullable disable

namespace api.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20230811194705_inital-create")]
    partial class initalcreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.21")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("api.Models.Cryptos.CryptoTransaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<decimal>("Amount")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<string>("ExchangeName")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<decimal>("Price")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<DateTimeOffset>("PurchaseDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("TransactionType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("CryptoTransactions");
                });

            modelBuilder.Entity("api.Models.Cryptos.CryptoWallet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<decimal>("AveragePrice")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<decimal>("Balance")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("CryptoCurrency")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("CurrencyName")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Symbol")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("CryptoWallets");
                });

            modelBuilder.Entity("api.SpotSolar.Models.Proposal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("ExcecutionTime")
                        .HasColumnType("int");

                    b.Property<decimal>("LabourValue")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<string>("Notes")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("PaymentMethods")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Power")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<int>("ServiceType")
                        .HasColumnType("int");

                    b.Property<decimal>("TotalPrice")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<decimal>("TotalPriceProducts")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<int>("WarrantyQtd")
                        .HasColumnType("int");

                    b.Property<int>("WarrantyType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Proposals");
                });

            modelBuilder.Entity("api.SpotSolar.Models.Proposal", b =>
                {
                    b.OwnsOne("api.SpotSolar.Models.Address", "Address", b1 =>
                        {
                            b1.Property<int>("ProposalId")
                                .HasColumnType("int");

                            b1.Property<string>("City")
                                .HasMaxLength(255)
                                .HasColumnType("varchar(255)");

                            b1.Property<string>("Number")
                                .HasMaxLength(255)
                                .HasColumnType("varchar(255)");

                            b1.Property<string>("State")
                                .HasMaxLength(255)
                                .HasColumnType("varchar(255)");

                            b1.Property<string>("Street")
                                .HasMaxLength(255)
                                .HasColumnType("varchar(255)");

                            b1.Property<string>("ZipCode")
                                .HasMaxLength(255)
                                .HasColumnType("varchar(255)");

                            b1.HasKey("ProposalId");

                            b1.ToTable("Proposals");

                            b1.WithOwner()
                                .HasForeignKey("ProposalId");
                        });

                    b.OwnsOne("api.SpotSolar.Models.Customer", "Customer", b1 =>
                        {
                            b1.Property<int>("ProposalId")
                                .HasColumnType("int");

                            b1.Property<string>("Email")
                                .HasMaxLength(255)
                                .HasColumnType("varchar(255)");

                            b1.Property<string>("Name")
                                .HasMaxLength(255)
                                .HasColumnType("varchar(255)");

                            b1.Property<string>("Phone")
                                .HasMaxLength(255)
                                .HasColumnType("varchar(255)");

                            b1.HasKey("ProposalId");

                            b1.ToTable("Proposals");

                            b1.WithOwner()
                                .HasForeignKey("ProposalId");
                        });

                    b.OwnsMany("api.SpotSolar.Models.Product", "Products", b1 =>
                        {
                            b1.Property<int>("ProposalId")
                                .HasColumnType("int");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b1.Property<int>("Id"), 1L, 1);

                            b1.Property<string>("Name")
                                .HasMaxLength(255)
                                .HasColumnType("varchar(255)");

                            b1.Property<int>("Quantity")
                                .HasColumnType("int");

                            b1.HasKey("ProposalId", "Id");

                            b1.ToTable("Product");

                            b1.WithOwner()
                                .HasForeignKey("ProposalId");
                        });

                    b.Navigation("Address");

                    b.Navigation("Customer");

                    b.Navigation("Products");
                });
#pragma warning restore 612, 618
        }
    }
}