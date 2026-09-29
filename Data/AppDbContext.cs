using Microsoft.EntityFrameworkCore;

namespace EenJaarGratis.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionGroup> QuestionGroups => Set<QuestionGroup>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Question>().Property(q => q.PointsToShare).HasDefaultValue(100);
        b.Entity<Player>().HasIndex(p => p.Code).IsUnique();
    }
}
