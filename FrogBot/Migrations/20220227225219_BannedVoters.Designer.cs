﻿// <auto-generated />
using FrogBot.Voting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FrogBot.Migrations
{
    [DbContext(typeof(VoteDbContext))]
    [Migration("20220227225219_BannedVoters")]
    partial class BannedVoters
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("FrogBot.Voting.BannedVoter", b =>
                {
                    b.Property<decimal>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("UserId");

                    b.ToTable("BannedVoters");
                });

            modelBuilder.Entity("FrogBot.Voting.CachedUsername", b =>
                {
                    b.Property<decimal>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("UserId");

                    b.ToTable("CachedUsernames");
                });

            modelBuilder.Entity("FrogBot.Voting.Vote", b =>
                {
                    b.Property<decimal>("ChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("MessageId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ReceiverId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("VoterId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("VoteType")
                        .HasColumnType("integer");

                    b.HasKey("ChannelId", "MessageId", "ReceiverId", "VoterId", "VoteType");

                    b.ToTable("Votes");
                });
#pragma warning restore 612, 618
        }
    }
}
