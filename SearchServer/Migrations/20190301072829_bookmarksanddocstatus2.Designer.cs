﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SearchServer.Models;

namespace SearchServer.Migrations
{
    [DbContext(typeof(UserContext))]
    [Migration("20190301072829_bookmarksanddocstatus2")]
    partial class bookmarksanddocstatus2
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.2-servicing-10034")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<int>("RoleId");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<int>("UserId");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<int>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("SearchServer.Models.Bookmark", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("DocumentId");

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("Page")
                        .HasMaxLength(256);

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("DocumentId", "UserId");

                    b.ToTable("Bookmark");
                });

            modelBuilder.Entity("SearchServer.Models.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("Category");
                });

            modelBuilder.Entity("SearchServer.Models.Comment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("DateTime");

                    b.Property<string>("DocumentId");

                    b.Property<int?>("GroupId");

                    b.Property<string>("Text")
                        .HasMaxLength(2048);

                    b.Property<int>("UserId");

                    b.Property<int>("nDislikes");

                    b.Property<int>("nLikes");

                    b.HasKey("Id");

                    b.HasIndex("DocumentId");

                    b.HasIndex("GroupId");

                    b.HasIndex("UserId");

                    b.ToTable("Comment");
                });

            modelBuilder.Entity("SearchServer.Models.Document", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("Access");

                    b.Property<string>("Additional");

                    b.Property<DateTime>("DateTime");

                    b.Property<string>("Description");

                    b.Property<byte>("DocStatus");

                    b.Property<int>("Downloads");

                    b.Property<string>("File")
                        .IsRequired();

                    b.Property<int?>("GroupId");

                    b.Property<short>("Pages");

                    b.Property<int>("ProcessedState");

                    b.Property<long>("Rating");

                    b.Property<long>("Size");

                    b.Property<string>("Tags");

                    b.Property<string>("Title");

                    b.Property<byte>("TitlePage");

                    b.Property<string>("Url");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("GroupId");

                    b.HasIndex("UserId");

                    b.ToTable("Document");
                });

            modelBuilder.Entity("SearchServer.Models.DocumentLike", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("DocumentId");

                    b.Property<int?>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("DocumentId");

                    b.HasIndex("UserId");

                    b.ToTable("DocumentLike");
                });

            modelBuilder.Entity("SearchServer.Models.DocumentRead", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("DateTime");

                    b.Property<string>("DocumentId")
                        .IsRequired();

                    b.Property<string>("Host")
                        .HasMaxLength(256);

                    b.Property<int?>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("DocumentId");

                    b.HasIndex("UserId");

                    b.ToTable("DocumentRead");
                });

            modelBuilder.Entity("SearchServer.Models.Group", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Access");

                    b.Property<int?>("CategoryId");

                    b.Property<int?>("CreatorId");

                    b.Property<string>("Description");

                    b.Property<string>("ImageUrl");

                    b.Property<string>("Name")
                        .IsRequired();

                    b.Property<long>("Rating");

                    b.Property<string>("Tags");

                    b.Property<int>("Type");

                    b.Property<string>("Url");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.HasIndex("CreatorId");

                    b.ToTable("Group");
                });

            modelBuilder.Entity("SearchServer.Models.GroupAdmin", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<int>("GroupId");

                    b.HasKey("UserId", "GroupId");

                    b.HasAlternateKey("GroupId", "UserId");

                    b.ToTable("GroupAdmin");
                });

            modelBuilder.Entity("SearchServer.Models.GroupSubscriber", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<int>("GroupId");

                    b.HasKey("UserId", "GroupId");

                    b.HasAlternateKey("GroupId", "UserId");

                    b.ToTable("GroupSubscriber");
                });

            modelBuilder.Entity("SearchServer.Models.GroupUser", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<int>("GroupId");

                    b.HasKey("UserId", "GroupId");

                    b.HasAlternateKey("GroupId", "UserId");

                    b.ToTable("GroupUser");
                });

            modelBuilder.Entity("SearchServer.Models.Message", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("DateTime");

                    b.Property<byte>("Type");

                    b.Property<int?>("fromUserId");

                    b.Property<int?>("groupId");

                    b.Property<string>("text")
                        .HasMaxLength(1024);

                    b.Property<int>("toUserId");

                    b.HasKey("Id");

                    b.HasIndex("fromUserId");

                    b.HasIndex("groupId");

                    b.HasIndex("toUserId");

                    b.ToTable("Message");
                });

            modelBuilder.Entity("SearchServer.Models.PayedSubscribe", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("DocumentId");

                    b.Property<DateTime>("EndTime");

                    b.Property<DateTime>("StartTime");

                    b.Property<int>("Type");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("DocumentId");

                    b.HasIndex("UserId");

                    b.ToTable("PayedSubscribe");
                });

            modelBuilder.Entity("SearchServer.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AccessFailedCount");

                    b.Property<string>("Banned");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<DateTime>("DateTime");

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<string>("ImageUrl");

                    b.Property<string>("Info");

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<long>("Rating");

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("Url");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.HasIndex("UserName")
                        .IsUnique();

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("SearchServer.Models.UserSubscriber", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("ToUserId");

                    b.Property<int?>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ToUserId");

                    b.HasIndex("UserId");

                    b.ToTable("UserSubscriber");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<int>")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.HasOne("SearchServer.Models.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.HasOne("SearchServer.Models.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<int>")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SearchServer.Models.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.HasOne("SearchServer.Models.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SearchServer.Models.Bookmark", b =>
                {
                    b.HasOne("SearchServer.Models.Document", "Document")
                        .WithMany()
                        .HasForeignKey("DocumentId");

                    b.HasOne("SearchServer.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SearchServer.Models.Comment", b =>
                {
                    b.HasOne("SearchServer.Models.Document", "Document")
                        .WithMany("Comments")
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SearchServer.Models.Group", "Group")
                        .WithMany("Comments")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SearchServer.Models.User", "User")
                        .WithMany("Comments")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SearchServer.Models.Document", b =>
                {
                    b.HasOne("SearchServer.Models.Group", "Group")
                        .WithMany("Documents")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("SearchServer.Models.User", "User")
                        .WithMany("Documents")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("SearchServer.Models.DocumentLike", b =>
                {
                    b.HasOne("SearchServer.Models.Document", "Document")
                        .WithMany("Likes")
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SearchServer.Models.User", "User")
                        .WithMany("Likes")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SearchServer.Models.DocumentRead", b =>
                {
                    b.HasOne("SearchServer.Models.Document", "Document")
                        .WithMany("Reads")
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SearchServer.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("SearchServer.Models.Group", b =>
                {
                    b.HasOne("SearchServer.Models.Category", "Category")
                        .WithMany()
                        .HasForeignKey("CategoryId");

                    b.HasOne("SearchServer.Models.User", "Creator")
                        .WithMany("CreatedGroups")
                        .HasForeignKey("CreatorId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("SearchServer.Models.GroupAdmin", b =>
                {
                    b.HasOne("SearchServer.Models.Group", "Group")
                        .WithMany("Admins")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("SearchServer.Models.User", "User")
                        .WithMany("AdminOfGroups")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("SearchServer.Models.GroupSubscriber", b =>
                {
                    b.HasOne("SearchServer.Models.Group", "Group")
                        .WithMany("Subscribers")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("SearchServer.Models.User", "User")
                        .WithMany("SubscribesToGroups")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SearchServer.Models.GroupUser", b =>
                {
                    b.HasOne("SearchServer.Models.Group", "Group")
                        .WithMany("Participants")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("SearchServer.Models.User", "User")
                        .WithMany("Participate")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SearchServer.Models.Message", b =>
                {
                    b.HasOne("SearchServer.Models.User", "fromUser")
                        .WithMany()
                        .HasForeignKey("fromUserId");

                    b.HasOne("SearchServer.Models.Group", "group")
                        .WithMany()
                        .HasForeignKey("groupId");

                    b.HasOne("SearchServer.Models.User", "toUser")
                        .WithMany("Messages")
                        .HasForeignKey("toUserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SearchServer.Models.PayedSubscribe", b =>
                {
                    b.HasOne("SearchServer.Models.Document", "Document")
                        .WithMany()
                        .HasForeignKey("DocumentId");

                    b.HasOne("SearchServer.Models.User", "User")
                        .WithMany("PayedSubscribes")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SearchServer.Models.UserSubscriber", b =>
                {
                    b.HasOne("SearchServer.Models.User", "ToUser")
                        .WithMany("Subscribers")
                        .HasForeignKey("ToUserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("SearchServer.Models.User", "User")
                        .WithMany("SubscribesToUsers")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);
                });
#pragma warning restore 612, 618
        }
    }
}
