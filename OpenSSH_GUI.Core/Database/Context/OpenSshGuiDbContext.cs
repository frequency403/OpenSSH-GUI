// File Created by: Oliver Schantz
// Created: 17.05.2024 - 08:05:28
// Last edit: 17.05.2024 - 08:05:29

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DataEncryption;
using Microsoft.EntityFrameworkCore.DataEncryption.Providers;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Lib.Settings;
using Serilog.Extensions.Logging;
using SQLitePCL;

namespace OpenSSH_GUI.Core.Database.Context;

/// <summary>
/// Represents the database context for the OpenSSH GUI application.
/// </summary>
public class OpenSshGuiDbContext : DbContext
{
    /// <summary>
    /// Represents the encryption key used for data encryption in the OpenSshGuiDbContext class.
    /// </summary>
    private readonly byte[] _encyptionKey = Convert.FromBase64String("Jk7JqD9mAafdvTvhNESHkXBFdy7phfyDR0FsnyGw2nY=");

    /// <summary>
    /// Represents the initialization vector (IV) used for data encryption in the OpenSshGuiDbContext class.
    /// </summary>
    private readonly byte[] _encyptionIV = Convert.FromBase64String("lWXnTKuRPnieE8nzpyl1Gg==");

    /// <summary>
    /// Represents an encryption provider used by the OpenSshGuiDbContext.
    /// </summary>
    private IEncryptionProvider _provider => new AesProvider(_encyptionKey, _encyptionIV);


    /// <summary>
    /// Represents a settings file for the application.
    /// </summary>
    public virtual DbSet<Settings> Settings { get; set; }

    /// <summary>
    /// Represents a SSH key data transfer object (DTO).
    /// </summary>
    public virtual DbSet<SshKeyDto> KeyDtos { get; set; }

    /// <summary>
    /// Represents a data transfer object (DTO) for connection credentials.
    /// </summary>
    public virtual DbSet<ConnectionCredentialsDto> ConnectionCredentialsDtos { get; set; }

    /// <summary>
    /// Configures the database connection for the OpenSshGuiDbContext.
    /// </summary>
    /// <param name="optionsBuilder">The options builder used to configure the database connection.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName);
        if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);
        var dbFilePath = Path.Combine(appDataPath, $"{AppDomain.CurrentDomain.FriendlyName}");
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbFilePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true
        };
        optionsBuilder.UseSqlite(builder.ToString())
            .UseLazyLoadingProxies();
        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    /// This method is called when the model for a derived context has been initialized, but before
    /// the model has been locked down and used to initialize the context. It allows further
    /// modification of the model before it is locked down.
    /// </summary>
    /// <param name="modelBuilder">The builder that defines the model for the context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseEncryption(_provider);
        
        modelBuilder.Entity<Settings>().HasKey(e => e.Version);

        modelBuilder.Entity<SshKeyDto>().HasKey(e => e.Id);
        modelBuilder.Entity<SshKeyDto>()
            .HasIndex(e => e.AbsolutePath).IsUnique();
        
        modelBuilder.Entity<SshKeyDto>()
            .HasMany<ConnectionCredentialsDto>(e => e.ConnectionCredentialsDto)
            .WithMany(e => e.KeyDtos);

        modelBuilder.Entity<ConnectionCredentialsDto>().HasKey(e => e.Id);
        modelBuilder.Entity<ConnectionCredentialsDto>()
            .HasMany<SshKeyDto>(e => e.KeyDtos)
            .WithMany(e => e.ConnectionCredentialsDto);
    }
}