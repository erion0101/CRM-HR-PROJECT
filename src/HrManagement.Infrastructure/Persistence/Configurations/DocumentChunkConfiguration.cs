using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class DocumentChunkConfiguration : EntityConfigurationBase<DocumentChunk>
{
    public override void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        base.Configure(builder);
        var comparer = new ValueComparer<float[]>(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            value => value.ToArray());
        builder.Property(x => x.Text).HasMaxLength(20_000);
        builder.Property(x => x.Embedding)
            .HasConversion(value => new Vector(value), value => value.ToArray())
            .Metadata.SetValueComparer(comparer);
        builder.Property(x => x.Embedding).HasColumnType("vector(1536)");
        builder.Property<NpgsqlTypes.NpgsqlTsVector>("SearchVector")
            .HasComputedColumnSql("to_tsvector('simple', coalesce(\"Text\", ''))", stored: true);
        builder.HasIndex("SearchVector").HasMethod("GIN");
        builder.HasIndex(x => new { x.DocumentVersionId, x.ChunkIndex }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.DocumentId });
        builder.HasIndex(x => x.Embedding).HasMethod("hnsw").HasOperators("vector_cosine_ops");
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<DocumentVersion>().WithMany().HasForeignKey(x => x.DocumentVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}
