using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;

namespace BirthPlatform.Data;

public class BirthContext : DbContext
{
    public BirthContext(DbContextOptions<BirthContext> options)
        : base(options)
    {
    }

    public DbSet<Child> Children { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.Entity<Child>()
        //    .HasOne(a => a.Booking)
        //    .WithOne(a => a.Child)
        //    .HasForeignKey<Booking>(c => c.ChildId);
    }
}

[PrimaryKey(nameof(Id))]
public class Child
{
    public Guid Id { get; set; }
    public required string BirthMother { get; set; }
    public required string Name { get; set; }
    public required DateOnly BirthDate { get; set; }
    public float? Weight { get; set; }
    public float? Height { get; set; }
    public ICollection<string> Conditions { get; set; } = [];
    public ICollection<string> Allergies { get; set; } = [];
    public Booking? Booking { get; set; }
}

[PrimaryKey(nameof(Id))]
public class Booking
{
    public Guid Id { get; set; }
    public required Guid ChildId { get; set; }
    //public required Child Child { get; set;  }
    public required DateTime Time { get; set; }
    public required string Location { get; set; }
    public string? DoctorHPR { get; set; }
    public bool IsCompleted { get; set; }
}

