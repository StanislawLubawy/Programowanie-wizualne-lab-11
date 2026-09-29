using System;

namespace probkibiologiczne.Models;

public enum TypProbki
{
    DNA,
    RNA,
    Bialko,
    Inny
}

public class Probka
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nazwa { get; set; } = string.Empty;
    public TypProbki Typ { get; set; } = TypProbki.Inny;
    public DateTime DataPobrania { get; set; } = DateTime.Now;
    public string Opis { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Nazwa} ({Typ}) - {DataPobrania:d}";
    }
}
