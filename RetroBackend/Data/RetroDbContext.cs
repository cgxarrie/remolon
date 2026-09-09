using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RetroBackend.Models;

namespace RetroBackend.Data;

public class RetroDbContext : IdentityDbContext<AppUser>
{
      public RetroDbContext(DbContextOptions<RetroDbContext> options) : base(options) { }

      public DbSet<Retrospective> Retrospectives => Set<Retrospective>();
      public DbSet<Organization> Organizations => Set<Organization>();
      public DbSet<Item> Items => Set<Item>();
      public DbSet<Column> Columns => Set<Column>();
      public DbSet<UserRetrospective> UserRetrospectives => Set<UserRetrospective>();
      public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Organization>(entity =>
            {
                  entity.HasKey(o => o.Id);
                  entity.Property(o => o.Name).IsRequired().HasMaxLength(200);
                  entity.Property(o => o.ThemeKey).IsRequired().HasMaxLength(20).HasDefaultValue("default");
                  entity.Property(o => o.ThemeHeaderColor).HasMaxLength(7);
                  entity.Property(o => o.ThemeHeaderHoverColor).HasMaxLength(7);
                  entity.Property(o => o.ThemeAccentColor).HasMaxLength(7);
                  entity.Property(o => o.ThemeAccentHoverColor).HasMaxLength(7);
                  entity.Property(o => o.ThemeFocusColor).HasMaxLength(7);
                  entity.HasIndex(o => o.Name)
                    .HasDatabaseName("IX_Organizations_Name_CaseInsensitive")
                    .IsUnique();
            });

            modelBuilder.Entity<AppUser>(entity =>
            {
                  entity.HasIndex(u => u.Nickname)
                    .HasDatabaseName("IX_AspNetUsers_Nickname_CaseInsensitive")
                    .IsUnique();
                  entity.HasOne(u => u.Organization)
                    .WithMany(o => o.Users)
                    .HasForeignKey(u => u.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Item>(entity =>
            {
                  entity.HasKey(i => i.Id);
                  entity.Property(i => i.Description)
                    .HasField("_description")
                    .UsePropertyAccessMode(PropertyAccessMode.Field)
                    .HasMaxLength(ItemLimits.DescriptionMaxLength);
                  entity.Property(i => i.Position)
                    .HasField("_position")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.Property(i => i.GroupId)
                    .HasField("_groupId")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.Property(i => i.CreatedByNickname)
                    .HasField("_createdByNickname")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.HasDiscriminator<string>("ItemType")
                    .HasValue<Item>("Item")
                    .HasValue<ActionItem>("ActionItem");
            });

            modelBuilder.Entity<ActionItem>(entity =>
            {
                  entity.Property(a => a.IsCompleted)
                    .HasField("_isCompleted")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.PrimitiveCollection(a => a.Assignees)
                    .HasField("_assignees")
                    .UsePropertyAccessMode(PropertyAccessMode.Field)
                    .HasColumnType("text[]");
                  entity.Property(a => a.Iterations)
                    .HasField("_iterations")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.Property(a => a.ClosedBy)
                    .HasField("_closedBy")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.Property(a => a.ClosedAt)
                    .HasField("_closedAt")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
            });

            modelBuilder.Entity<Retrospective>(entity =>
            {
                  entity.HasKey(r => r.Id);
                  entity.Property(r => r.Title)
                    .HasField("_title")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.Property(r => r.IsClosed)
                    .HasField("_isClosed")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.Property(r => r.IsRevealed)
                    .HasField("_isRevealed")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.Property(r => r.RetrospectiveDate)
                    .HasField("_retrospectiveDate")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.HasMany(r => r.Columns)
                    .WithOne()
                    .HasForeignKey(c => c.RetrospectiveId)
                    .OnDelete(DeleteBehavior.Cascade);
                  entity.HasOne(r => r.Organization)
                    .WithMany(o => o.Retrospectives)
                    .HasForeignKey(r => r.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Column>(entity =>
            {
                  entity.HasKey(c => c.Id);
                  entity.Property(c => c.Title)
                    .HasField("_title")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.Property(c => c.Position)
                    .HasField("_position")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.Property(c => c.HeaderColor)
                    .HasField("_headerColor")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                  entity.Ignore(c => c.IsEditable);
                  entity.HasDiscriminator<string>("ColumnType")
                    .HasValue<ActionColumn>("Action")
                    .HasValue<Column>("Column");
                  entity.HasMany(c => c.Items)
                    .WithOne()
                    .HasForeignKey(i => i.ColumnId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserRetrospective>(entity =>
            {
                  entity.HasKey(ur => new { ur.UserId, ur.RetrospectiveId });
                  entity.HasOne(ur => ur.User)
                    .WithMany()
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                  entity.HasOne(ur => ur.Retrospective)
                    .WithMany()
                    .HasForeignKey(ur => ur.RetrospectiveId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                  entity.HasKey(t => t.Id);
                  entity.Property(t => t.UserId).IsRequired();
                  entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
                  entity.HasIndex(t => t.TokenHash).IsUnique();
                  entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
      }
}
