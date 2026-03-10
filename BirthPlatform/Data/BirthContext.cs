using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BirthPlatform.Data;

public class BirthContext : DbContext
{
    public DbSet<Child> Children { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
}

public class Child
{
    [Key]
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

public class Booking
{
    [Key]
    public Guid Id { get; set; }
    public required Child Child { get; set;  }
    public required DateTime Time { get; set; }
    public required string Location { get; set; }
    public Doctor? Doctor { get; set; }
    public bool IsCompleted { get; set; }
}

public class Doctor
{
    [Key]
    public required string HPRNumber;
    public ICollection<Booking> Bookings = [];
}
