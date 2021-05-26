using Migration = FluentMigrator.Migration;

namespace ArquiteturaHexagonal.Migrations._2021.Configuration.Tables.InitialCreate
{
    [FluentMigrator.Migration(202125052146)]
    public class CriacaoTabelaCategoria : Migration
    {
        public override void Up()
        {
            if (Schema.Table("TB_Categoria").Exists()) return;

            var tabela = Create.Table("TB_Categoria")
                .WithDescription("Tabela de Cadastro de Categoria");

            tabela.WithColumn("CDCadastro").AsInt16().PrimaryKey("PK_Categoria").NotNullable()
                .WithColumnDescription("Codigo da Categoria");

            tabela.WithColumn("DSNome").AsAnsiString(80).NotNullable()
                .WithColumnDescription("Nome da Categoria");
            
            tabela.WithColumn("DSImg").AsAnsiString(300).NotNullable()
                .WithColumnDescription("URL da Imagem");
        }

        public override void Down()
        {
            if (Schema.Table("TB_Categoria").Exists())
                Delete.Table("TB_Categoria");
        }
    }
}