using CHBackend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<AppUser>

{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Contractor> Contractors { get; set; }
    public DbSet<Issue> Issues { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<Protocol> Protocols { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Konfiguracja relacji między Issue a Contractor
        modelBuilder.Entity<Issue>()
            .HasOne(i => i.Contractor)
            .WithMany(c => c.Issues)
            .HasForeignKey(i => i.ContractorId)
            .OnDelete(DeleteBehavior.Cascade); // Jeśli wykonawca zostanie usunięty, ContractorId w Issue zostanie ustawione na null

        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Contractor)
            .WithMany(cn => cn.Contracts)
            .HasForeignKey(c => c.ContractorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Contractor)
            .WithMany()
            .HasForeignKey(u => u.ContractorId)
            .OnDelete(DeleteBehavior.SetNull); // Jeśli usuniesz Wykonawcę, użytkownik nie zostanie usunięty,tylko jego pole ContractorId ustawi się na NULL.
            
    }

}

