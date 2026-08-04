using App.Application.Common;
using App.Application.Contracts.Erp;

namespace App.Application.Abstractions.Erp;

public interface IErpBatchReader
{
    Task<Result<ErpBatchSnapshot>> ReadAsync(string orderReference, CancellationToken cancellationToken = default);
}
