namespace GestorAssinaturas.Dominio.Comum;

public abstract class ObjetoDeValor
{
    protected abstract IEnumerable<object?> ObterComponentesDeIgualdade();

    public override bool Equals(object? outroObjeto)
    {
        if (outroObjeto is not ObjetoDeValor outroObjetoDeValor)
        {
            return false;
        }

        if (GetType() != outroObjeto.GetType())
        {
            return false;
        }

        return ObterComponentesDeIgualdade()
            .SequenceEqual(outroObjetoDeValor.ObterComponentesDeIgualdade());
    }

    public override int GetHashCode()
    {
        var codigoAcumulado = new HashCode();

        foreach (var componente in ObterComponentesDeIgualdade())
        {
            codigoAcumulado.Add(componente);
        }

        return codigoAcumulado.ToHashCode();
    }

    public static bool operator ==(ObjetoDeValor? primeiroObjetoDeValor, ObjetoDeValor? segundoObjetoDeValor)
    {
        if (primeiroObjetoDeValor is null)
        {
            return segundoObjetoDeValor is null;
        }

        return primeiroObjetoDeValor.Equals(segundoObjetoDeValor);
    }

    public static bool operator !=(ObjetoDeValor? primeiroObjetoDeValor, ObjetoDeValor? segundoObjetoDeValor)
    {
        return !(primeiroObjetoDeValor == segundoObjetoDeValor);
    }
}
