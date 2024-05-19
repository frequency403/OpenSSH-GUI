// File Created by: Oliver Schantz
// Created: 17.05.2024 - 08:05:28
// Last edit: 17.05.2024 - 08:05:29

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DataEncryption;
using Microsoft.EntityFrameworkCore.DataEncryption.Providers;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Lib.Settings;

namespace OpenSSH_GUI.Core.Database.Context;

public class OpenSshGuiDbContext : DbContext
{
    private readonly byte[] _encyptionKey = Convert.FromBase64String("Jk7JqD9mAafdvTvhNESHkXBFdy7phfyDR0FsnyGw2nY=");
    private readonly byte[] _encyptionIV = Convert.FromBase64String("lWXnTKuRPnieE8nzpyl1Gg==");
    private IEncryptionProvider _provider => new AesProvider(_encyptionKey, _encyptionIV);
    
    
    public virtual DbSet<Settings> Settings { get; set; }
    public virtual DbSet<SshKeyDto> KeyDtos { get; set; }
    public virtual DbSet<ConnectionCredentialsDto> ConnectionCredentialsDtos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName);
        var dbFilePath = Path.Combine(appDataPath, $"{AppDomain.CurrentDomain.FriendlyName}.db");
        optionsBuilder.UseSqlite($"DataSource={dbFilePath}").UseLazyLoadingProxies();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseEncryption(_provider);

        modelBuilder.Entity<Settings>().HasKey(e => e.Id);
        modelBuilder.Entity<Settings>()
            .HasIndex(e => e.Version).IsUnique();

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