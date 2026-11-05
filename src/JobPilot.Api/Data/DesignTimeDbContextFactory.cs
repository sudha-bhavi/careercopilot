using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobPilot.Api.Data;

// Used only by `dotnet ef migrations add`. Creating a migration never connects to the
// database, so a placeholder connection string is enough and no secret is needed.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<JobPilotDbContext>
{
    public JobPilotDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<JobPilotDbContext>()
            .UseNpgsql("Host=localhost;Database=jobpilot")
            .Options;
        return new JobPilotDbContext(options);
    }
}
