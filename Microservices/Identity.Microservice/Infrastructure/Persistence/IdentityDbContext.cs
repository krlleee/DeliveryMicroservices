using Identity.Microservice.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace Identity.Microservice.Infrastructure.Persistence
{
    public class IdentityDbContext : IdentityDbContext<ApplicationUser>
    {
        public IdentityDbContext(DbContextOptions options)
            : base(options) { }

        public DbSet<OpenIddictEntityFrameworkCoreApplication> OpenIddictApplications { get; set; }
        public DbSet<OpenIddictEntityFrameworkCoreAuthorization> OpenIddictAuthorizations { get; set; }
        public DbSet<OpenIddictEntityFrameworkCoreScope> OpenIddictScopes { get; set; }
        public DbSet<OpenIddictEntityFrameworkCoreToken> OpenIddictTokens { get; set; }
    }
}
