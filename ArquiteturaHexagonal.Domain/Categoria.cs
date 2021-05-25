using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ArquiteturaHexagonal.Domain
{
    public class Categoria
    {
            public int CategoriaId { get; set; }
            public int Nome { get; set; }
            public string ImagemUrl { get; set; }
            
            public ICollection<Produto> Produtos { get; set; }

            public Categoria()
            {
                Produtos = new Collection<Produto>();
            }
    }
}