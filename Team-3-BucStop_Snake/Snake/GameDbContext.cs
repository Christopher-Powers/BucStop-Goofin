using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Snake
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

        public DbSet<GameInfo> Games { get; set; }
    }

    public class GameInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }
        public string HowTo { get; set; }
        public string Thumbnail { get; set; }
    }
}
