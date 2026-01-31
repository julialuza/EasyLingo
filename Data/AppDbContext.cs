using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EasyLingo.Data.Entities;
using System.IO;
using EasyLingo.Services;

namespace EasyLingo.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Set> Sets { get; set; }
        public DbSet<Term> Terms { get; set; }
        public DbSet<UserSetProgress> UserSetProgresses { get; set; }
        public DbSet<UserSetCategory> UserSetCategories { get; set; }
        public DbSet<UserTermStatus> UserTermStatuses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EasyLingo",
                "EasyLingo.db"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            optionsBuilder.UseSqlite($"Data Source={path}");
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Set - Language
            modelBuilder.Entity<Set>()
                .HasOne(s => s.Language)
                .WithMany(l => l.Sets)
                .HasForeignKey(s => s.LangId);

            modelBuilder.Entity<Set>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sets)
                .HasForeignKey(s => s.UserId);


            // Term - Set
            modelBuilder.Entity<Term>()
                .HasOne(t => t.Set)
                .WithMany(s => s.Terms)
                .HasForeignKey(t => t.SetId);

            // Set - UserSetCategory
            modelBuilder.Entity<UserSetCategory>()
                .HasOne(c => c.User)
                .WithMany(u => u.UserSetCategories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserSetCategory - User
            modelBuilder.Entity<UserSetCategory>()
                .HasOne(c => c.User)
                .WithMany(u => u.UserSetCategories)
                .HasForeignKey(c => c.UserId);


            // UserSetProgress - User i Set
            modelBuilder.Entity<UserSetProgress>()
                .HasOne(p => p.User)
                .WithMany(u => u.UserSetProgresses)
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<UserSetProgress>()
                .HasOne(p => p.Set)
                .WithMany(s => s.UserSetProgresses)
                .HasForeignKey(p => p.SetId);

            // UserTermStatus - User i Term
            modelBuilder.Entity<UserTermStatus>()
                .HasOne(s => s.User)
                .WithMany(u => u.UserTermStatuses)
                .HasForeignKey(s => s.UserId);

            modelBuilder.Entity<UserTermStatus>()
                .HasOne(s => s.Term)
                .WithMany(t => t.UserTermStatuses)
                .HasForeignKey(s => s.TermId);

            // Dodanie początkowych wartości do bazy
            modelBuilder.Entity<Language>().HasData(
                new Language { LangId = 1, Name = "Angielski", Code = "EN" },
                new Language { LangId = 2, Name = "Niemiecki", Code = "DE" }
            );

            modelBuilder.Entity<User>().HasData(
                new User { UserId = 1, Username = "admin", PasswordHash = PasswordHasher.HashPassword("admin123") }
            );

        }
    }
}
