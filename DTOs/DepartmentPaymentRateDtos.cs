using System;

namespace AttendVisionReportsApi.DTOs
{
    public record DepartmentPaymentRateInput(
        string RateType,
        decimal Amount,
        string? MatchKey,
        string? OtherLabel,
        string AppliesTo
    );

    public record DepartmentPaymentRate(
        Guid Id,
        Guid DepartmentId,
        string RateType,
        decimal Amount,
        string? MatchKey,
        string? OtherLabel,
        string AppliesTo
    );
}
