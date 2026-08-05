namespace GestorAssinaturas.Dominio.Comum;

public abstract class Entidade
{
    protected Entidade(Guid identificador)
    {
        Identificador = identificador;
    }

    public Guid Identificador { get; }

    protected static Resultado ValidarIdentificador(Guid identificador)
    {
        return Resultado.FalhaQuando(
            identificador == Guid.Empty,
            "O identificador da entidade não pode ser vazio.");
    }

    public override bool Equals(object? outroObjeto)
    {
        if (outroObjeto is not Entidade outraEntidade)
        {
            return false;
        }

        if (ReferenceEquals(this, outraEntidade))
        {
            return true;
        }

        if (GetType() != outraEntidade.GetType())
        {
            return false;
        }

        return Identificador == outraEntidade.Identificador;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Identificador);
    }
}
