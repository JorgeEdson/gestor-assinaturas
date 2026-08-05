namespace GestorAssinaturas.Aplicacao.Portas;

public interface IRelogioDoSistema
{
    DateOnly ObterDataAtual();

    DateTimeOffset ObterInstanteAtual();
}
