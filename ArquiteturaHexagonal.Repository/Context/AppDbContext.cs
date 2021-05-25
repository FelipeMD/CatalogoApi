using ArquiteturaHexagonal.Domain;
using Microsoft.EntityFrameworkCore;

namespace ArquiteturaHexagonal.Repository.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options ) 
            : base(options){ }
        
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Produto> Produtos { get; set; }
    }
}