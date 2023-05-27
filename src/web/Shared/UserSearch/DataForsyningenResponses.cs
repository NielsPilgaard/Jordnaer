namespace Jordnaer.Shared.UserSearch;

// ReSharper disable InconsistentNaming
public readonly record struct AddressAutoCompleteResponse(string? Tekst, Adresse? Adresse)
{
    public override string ToString() => Tekst ?? string.Empty;
}

public readonly record struct Adresse(
    string? Id,
    int Status,
    int Darstatus,
    string? Vejkode,
    string? Vejnavn,
    string? Adresseringsvejnavn,
    string? Husnr,
    object? Etage,
    object? Dør,
    object? Supplerendebynavn,
    string? Postnr,
    string? Postnrnavn,
    object? Stormodtagerpostnr,
    object? Stormodtagerpostnrnavn,
    string? Kommunekode,
    string? Adgangsadresseid,
    float X,
    float Y,
    string? Href
);

public readonly record struct ZipCodeSearchResponse(
    string? Href,
    string? Nr,
    string? Navn,
    object? Stormodtageradresser,
    float[]? Bbox,
    float[]? Visueltcenter,
    Kommuner[]? Kommuner,
    DateTime Ændret,
    DateTime Geo_Ændret,
    int Geo_Version,
    string? Dagi_Id
);

public readonly record struct Kommuner(
    string? Href,
    string? Kode,
    string? Navn
);
