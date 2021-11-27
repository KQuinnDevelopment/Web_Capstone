using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           18-09-2021
*/
namespace WBSAlpha.Data
{
    public class ApplicationDbContext : IdentityDbContext<CoreUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // additional tables
        public DbSet<Standing> Standings { get; set; }
        public DbSet<Chatroom> Chatrooms { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Report> Reports { get; set; }
        // game-specific
        public DbSet<Rune> Runes { get; set; }
        public DbSet<Weapon> Weapons { get; set; }
        public DbSet<GameOneBuild> GameOneBuilds { get; set; }
    }
}