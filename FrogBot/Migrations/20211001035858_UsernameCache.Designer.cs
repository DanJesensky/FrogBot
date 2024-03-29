﻿// <auto-generated />
using FrogBot.Voting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace FrogBot.Migrations
{
    [DbContext(typeof(VoteDbContext))]
    [Migration("20211001035858_UsernameCache")]
    partial class UsernameCache
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.9")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

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
