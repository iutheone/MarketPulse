using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarketPulse.Infrastructure.Persistence;

public sealed class MarketPulseDbContextFactory : IDesignTimeDbContextFactory<MarketPulseDbContext>
{
    public MarketPulseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MarketPulseDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=marketpulse;Username=marketpulse;Password=marketpulse")
            .Options;

        return new MarketPulseDbContext(options);
    }
}
