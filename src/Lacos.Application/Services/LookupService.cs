using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Exceptions;
using Lacos.Domain.ValueObjects;

namespace Lacos.Application.Services;

/// <summary>
/// Consultas auxiliares a serviços externos (CEP e CID), usadas pelo front-end para preenchimento assistido.
/// </summary>
public interface ILookupService
{
    Task<AddressLookupResult> LookupCepAsync(string cep, CancellationToken ct = default);
    Task<IReadOnlyList<CidLookupResult>> SearchCidAsync(string term, CancellationToken ct = default);
}

public sealed class LookupService : ILookupService
{
    private readonly IAddressLookupService _address;
    private readonly ICidLookupService _cid;

    public LookupService(IAddressLookupService address, ICidLookupService cid)
    {
        _address = address;
        _cid = cid;
    }

    /// <summary>Valida o formato do CEP localmente antes de chamar o serviço externo (evita chamadas desnecessárias).</summary>
    public async Task<AddressLookupResult> LookupCepAsync(string cep, CancellationToken ct = default)
    {
        if (!Cep.TryCreate(cep, out var parsed)) throw new ValidationFailedException(nameof(cep), "CEP inválido.");
        return await _address.LookupAsync(parsed!.Value, ct) ?? throw new NotFoundException("CEP", parsed.Formatted);
    }

    public async Task<IReadOnlyList<CidLookupResult>> SearchCidAsync(string term, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            throw new ValidationFailedException(nameof(term), "Informe ao menos 2 caracteres.");
        return await _cid.SearchAsync(term.Trim(), ct);
    }
}
