﻿using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Web.Configuration;

namespace NuGetGallery
{
    public class EntitiesContext : DbContext, IEntitiesContext
    {
        static string GetDefaultConnectionString()
        {
            Debug.Assert(false, "If you see this during Migrations Ignore it. Otherwise pay attention. The website should be passing in a readonly mode flag");
            return WebConfigurationManager.ConnectionStrings["Gallery.SqlServer"].ConnectionString;
        }

        public EntitiesContext()
            : base(GetDefaultConnectionString())
        {
        }

        public EntitiesContext(string connectionString, bool readOnly)
            : base(connectionString)
        {
            ReadOnly = readOnly;
        }

        public bool ReadOnly { get; private set; }
        public IDbSet<CuratedFeed> CuratedFeeds { get; set; }
        public IDbSet<CuratedPackage> CuratedPackages { get; set; }
        public IDbSet<PackageRegistration> PackageRegistrations { get; set; }
        public IDbSet<User> Users { get; set; }

        IDbSet<T> IEntitiesContext.Set<T>()
        {
            return base.Set<T>();
        }

        public override int SaveChanges()
        {
            if (ReadOnly)
            {
                throw new ReadOnlyModeException("Save changes unavailable: the gallery is currently in read only mode, with limited service. Please try again later.");
            }

            return base.SaveChanges();
        }

        public void DeleteOnCommit<T>(T entity) where T : class
        {
            Set<T>().Remove(entity);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.Key);

            modelBuilder.Entity<User>()
                .HasMany<EmailMessage>(u => u.Messages)
                .WithRequired(em => em.ToUser)
                .HasForeignKey(em => em.ToUserKey);

            modelBuilder.Entity<User>()
                .HasMany<Role>(u => u.Roles)
                .WithMany(r => r.Users)
                .Map(c => c.ToTable("UserRoles")
                           .MapLeftKey("UserKey")
                           .MapRightKey("RoleKey"));

            modelBuilder.Entity<Role>()
                .HasKey(u => u.Key);

            modelBuilder.Entity<EmailMessage>()
                .HasKey(em => em.Key);

            modelBuilder.Entity<EmailMessage>()
                .HasOptional<User>(em => em.FromUser)
                .WithMany()
                .HasForeignKey(em => em.FromUserKey);

            modelBuilder.Entity<PackageRegistration>()
                .HasKey(pr => pr.Key);

            modelBuilder.Entity<PackageRegistration>()
                .HasMany<Package>(pr => pr.Packages)
                .WithRequired(p => p.PackageRegistration)
                .HasForeignKey(p => p.PackageRegistrationKey);

            modelBuilder.Entity<PackageRegistration>()
                .HasMany<User>(pr => pr.Owners)
                .WithMany()
                .Map(c => c.ToTable("PackageRegistrationOwners")
                           .MapLeftKey("PackageRegistrationKey")
                           .MapRightKey("UserKey"));

            modelBuilder.Entity<Package>()
                .HasKey(p => p.Key);

            modelBuilder.Entity<Package>()
                .HasMany<PackageStatistics>(p => p.DownloadStatistics)
                .WithRequired(ps => ps.Package)
                .HasForeignKey(ps => ps.PackageKey);

            modelBuilder.Entity<Package>()
                .HasMany<PackageDependency>(p => p.Dependencies)
                .WithRequired(pd => pd.Package)
                .HasForeignKey(pd => pd.PackageKey);

            modelBuilder.Entity<PackageEdit>()
                .HasKey(pm => pm.Key);

            modelBuilder.Entity<PackageEdit>()
                .HasRequired(pm => pm.User)
                .WithMany()
                .HasForeignKey(pm => pm.UserKey);

            modelBuilder.Entity<PackageEdit>()
                .HasRequired<Package>(pm => pm.Package)
                .WithMany(p => p.PackageEdits)
                .HasForeignKey(pm => pm.PackageKey)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PackageHistory>()
                .HasKey(pm => pm.Key);

            modelBuilder.Entity<PackageHistory>()
                .HasOptional(pm => pm.User)
                .WithMany()
                .HasForeignKey(pm => pm.UserKey);

            modelBuilder.Entity<PackageHistory>()
                .HasRequired<Package>(pm => pm.Package)
                .WithMany(p => p.PackageHistories)
                .HasForeignKey(pm => pm.PackageKey)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PackageStatistics>()
                .HasKey(ps => ps.Key);

            modelBuilder.Entity<PackageDependency>()
                .HasKey(pd => pd.Key);

            modelBuilder.Entity<GallerySetting>()
                .HasKey(gs => gs.Key);

            modelBuilder.Entity<PackageOwnerRequest>()
                .HasKey(por => por.Key);

            modelBuilder.Entity<PackageFramework>()
                .HasKey(pf => pf.Key);
            modelBuilder.Entity<CuratedFeed>()
                .HasKey(cf => cf.Key);

            modelBuilder.Entity<CuratedFeed>()
                .HasMany<CuratedPackage>(cf => cf.Packages)
                .WithRequired(cp => cp.CuratedFeed)
                .HasForeignKey(cp => cp.CuratedFeedKey);

            modelBuilder.Entity<CuratedFeed>()
                .HasMany<User>(cf => cf.Managers)
                .WithMany()
                .Map(c => c.ToTable("CuratedFeedManagers")
                           .MapLeftKey("CuratedFeedKey")
                           .MapRightKey("UserKey"));

            modelBuilder.Entity<CuratedPackage>()
                .HasKey(cp => cp.Key);

            modelBuilder.Entity<CuratedPackage>()
                .HasRequired(cp => cp.PackageRegistration);
        }
    }
}
