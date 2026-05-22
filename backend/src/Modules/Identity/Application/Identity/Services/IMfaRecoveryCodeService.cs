using ErrorOr;

namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IMfaRecoveryCodeService
{
    Task<ErrorOr<MfaRecoveryCodeGenerationResult>> GenerateRecoveryCodesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<MfaRecoveryCodeStatus> GetRecoveryCodeStatusAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<ErrorOr<MfaRecoveryCodeConsumptionResult>> ConsumeRecoveryCodeAsync(
        Guid userId,
        string code,
        Guid? challengeId,
        CancellationToken cancellationToken);

    Task<ErrorOr<MfaRecoveryResetResult>> ResetMfaRecoveryAsync(
        Guid userId,
        Guid actorUserId,
        CancellationToken cancellationToken);
}
