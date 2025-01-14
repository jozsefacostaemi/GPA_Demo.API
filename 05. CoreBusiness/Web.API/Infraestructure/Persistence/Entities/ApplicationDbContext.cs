using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attention> Attentions { get; set; }

    public virtual DbSet<AttentionHistory> AttentionHistories { get; set; }

    public virtual DbSet<AttentionState> AttentionStates { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<ConfQueue> ConfQueues { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<GeneratedQueue> GeneratedQueues { get; set; }

    public virtual DbSet<HealthCareStaff> HealthCareStaffs { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PersonState> PersonStates { get; set; }

    public virtual DbSet<Plan> Plans { get; set; }

    public virtual DbSet<Processor> Processors { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-0SDTHPP\\SQLEXPRESS;Database=GPA_Demo;Trusted_Connection=True;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attention>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.AttentionState).WithMany(p => p.Attentions)
                .HasForeignKey(d => d.AttentionStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attentions_AttentionStates");

            entity.HasOne(d => d.HealthCareStaff).WithMany(p => p.Attentions)
                .HasForeignKey(d => d.HealthCareStaffId)
                .HasConstraintName("FK_Attentions_HealthCareStaffs");

            entity.HasOne(d => d.Patient).WithMany(p => p.Attentions)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attentions_Patients");
        });

        modelBuilder.Entity<AttentionHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_AttentionHistorys");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Attention).WithMany(p => p.AttentionHistories)
                .HasForeignKey(d => d.AttentionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttentionHistories_Attentions");

            entity.HasOne(d => d.AttentionStateNavigation).WithMany(p => p.AttentionHistories)
                .HasForeignKey(d => d.AttentionState)
                .HasConstraintName("FK_AttentionHistories_AttentionStates");
        });

        modelBuilder.Entity<AttentionState>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Queue");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(30);
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(30);
            entity.Property(e => e.Name).HasMaxLength(30);

            entity.HasOne(d => d.Department).WithMany(p => p.Cities)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_Cities_Departments");
        });

        modelBuilder.Entity<ConfQueue>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.NOrder).HasColumnName("nOrder");
            entity.Property(e => e.Nprocessor).HasColumnName("NProcessor");
            entity.Property(e => e.QueueDeadLetterExchange).HasMaxLength(50);
            entity.Property(e => e.QueueDeadLetterExchangeRoutingKey).HasMaxLength(50);
            entity.Property(e => e.QueueMode).HasMaxLength(10);

            entity.HasOne(d => d.AttentionState).WithMany(p => p.ConfQueues)
                .HasForeignKey(d => d.AttentionStateId)
                .HasConstraintName("FK_ConfQueues_States");

            entity.HasOne(d => d.City).WithMany(p => p.ConfQueues)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_ConfQueues_Cities");

            entity.HasOne(d => d.Process).WithMany(p => p.ConfQueues)
                .HasForeignKey(d => d.ProcessId)
                .HasConstraintName("FK_ConfQueues_Processor");
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Country");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(30);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_States");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Active)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(30);

            entity.HasOne(d => d.Country).WithMany(p => p.Departments)
                .HasForeignKey(d => d.CountryId)
                .HasConstraintName("FK_Departments_Country");
        });

        modelBuilder.Entity<GeneratedQueue>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.ConfigQueue).WithMany(p => p.GeneratedQueues)
                .HasForeignKey(d => d.ConfigQueueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GeneratedQueues_ConfQueues");
        });

        modelBuilder.Entity<HealthCareStaff>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.City).WithMany(p => p.HealthCareStaffs)
                .HasForeignKey(d => d.CityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HealthCareStaffs_Cities");

            entity.HasOne(d => d.PersonState).WithMany(p => p.HealthCareStaffs)
                .HasForeignKey(d => d.PersonStateId)
                .HasConstraintName("FK_HealthCareStaffs_PersonStates");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Patientss");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Birthday).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.City).WithMany(p => p.Patients)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_Patients_Cities");

            entity.HasOne(d => d.PersonState).WithMany(p => p.Patients)
                .HasForeignKey(d => d.PersonStateId)
                .HasConstraintName("FK_Patients_PersonStates");

            entity.HasOne(d => d.Plan).WithMany(p => p.Patients)
                .HasForeignKey(d => d.PlanId)
                .HasConstraintName("FK_Patients_Plans");
        });

        modelBuilder.Entity<PersonState>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HealthCareStaffStatess");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Plan");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Processor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_AttentionType");

            entity.ToTable("Processor");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(30);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
