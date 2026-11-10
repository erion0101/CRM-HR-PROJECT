using HrManagement.Infrastructure;
using HrManagement.Infrastructure.Files;
using HrManagement.Infrastructure.Files.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace HrManagement.IntegrationTests;

public sealed class PersistenceAndStorageTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg17")
        .WithDatabase("hr_tests").WithUsername("hr_test").WithPassword("hr_test_password").Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Initial_migration_creates_the_model()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), npgsql => npgsql.UseVector()).Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        Assert.True(await db.Database.CanConnectAsync());
        var tables = await db.Database.SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public'").ToListAsync();
        Assert.Contains("Employees", tables);
        Assert.Contains("KnowledgeArticles", tables);
        Assert.Contains("Projects", tables);
        Assert.Contains("OnboardingAssignments", tables);
        Assert.Contains("OnboardingStepProgresses", tables);
        Assert.Contains("OnboardingReadingCycles", tables);
        Assert.Contains("OnboardingReadingProgresses", tables);
        Assert.Contains("OnboardingQuizzes", tables);
        Assert.Contains("OnboardingQuizQuestions", tables);
        Assert.Contains("OnboardingQuizAnswerOptions", tables);
        Assert.Contains("OnboardingQuizAttempts", tables);
        Assert.Contains("OnboardingQuizAttemptQuestions", tables);
        Assert.Contains("OnboardingQuizAnswers", tables);
        Assert.Contains("DateOfBirth", (await db.Database.SqlQueryRaw<string>("SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_name = 'Employees'").ToListAsync()));
        Assert.Contains("CurrentReadingCycleId", (await db.Database.SqlQueryRaw<string>("SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_name = 'OnboardingAssignments'").ToListAsync()));
    }

    [Fact]
    public async Task Local_storage_rejects_path_traversal_and_round_trips_quarantine_file()
    {
        var root = Path.Combine(Path.GetTempPath(), $"hr-storage-{Guid.NewGuid():N}");
        try
        {
            var storage = new LocalDocumentStorage(Options.Create(new StorageOptions { RootPath = root }));
            await using var input = new MemoryStream("%PDF-test"u8.ToArray());
            var key = await storage.SaveQuarantineAsync(input, CancellationToken.None);
            await using var output = await storage.OpenReadAsync(key, CancellationToken.None);
            using var reader = new StreamReader(output);
            Assert.Equal("%PDF-test", await reader.ReadToEndAsync());
            Assert.Throws<InvalidOperationException>(() => storage.GetAbsolutePath("../escape"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
