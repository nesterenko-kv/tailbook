using ErrorOr;

namespace Tailbook.Modules.VisitOperations.Domain.Aggregates;

public static class VisitErrors
{
    public static Error IdRequired => Error.Validation(
        code: "VisitOperations.VisitIdRequired",
        description: "Visit id is required.");

    public static Error AppointmentRequired => Error.Validation(
        code: "VisitOperations.AppointmentRequired",
        description: "Visit must reference an appointment.");

    public static Error ExecutionItemsRequired => Error.Validation(
        code: "VisitOperations.ExecutionItemsRequired",
        description: "Visit execution items are required.");

    public static Error AtLeastOneExecutionItemRequired => Error.Validation(
        code: "VisitOperations.ExecutionItemsRequired",
        description: "Visit must include at least one execution item.");

    public static Error ExecutionItemIdRequired => Error.Validation(
        code: "VisitOperations.ExecutionItemIdRequired",
        description: "Visit execution item id is required.");

    public static Error ExecutionItemNotFound => Error.NotFound(
        code: "VisitOperations.ExecutionItemNotFound",
        description: "Visit execution item does not exist.");

    public static Error CompletionFailed => Error.Conflict(
        code: "VisitOperations.VisitCompletionFailed",
        description: "Visit is not eligible for completion.");

    public static Error CloseFailed => Error.Conflict(
        code: "VisitOperations.VisitCloseFailed",
        description: "Visit is not eligible for closure.");

    public static Error PriceAdjustmentRequired => Error.Validation(
        code: "VisitOperations.PriceAdjustmentRequired",
        description: "Visit price adjustment is required.");

    public static Error InvalidAdjustmentAmount => Error.Validation(
        code: "VisitOperations.InvalidAdjustmentAmount",
        description: "Adjustment amount must be greater than zero.");

    public static Error NegativeFinalTotal => Error.Validation(
        code: "VisitOperations.NegativeFinalTotal",
        description: "Visit final total cannot be negative.");

    public static Error ExecutionItemRequired => Error.Validation(
        code: "VisitOperations.ExecutionItemRequired",
        description: "Visit execution item is required.");

    public static Error ExecutionItemAlreadyExists => Error.Conflict(
        code: "VisitOperations.ExecutionItemAlreadyExists",
        description: "Visit execution item already exists for this appointment item.");

    public static Error NotEditable => Error.Conflict(
        code: "VisitOperations.VisitNotEditable",
        description: "Visit is not editable in its current status.");

    public static Error DefaultComponentIncomplete(string procedureName) => Error.Validation(
        code: "VisitOperations.DefaultComponentIncomplete",
        description: $"Default expected component '{procedureName}' must be performed or skipped before completion.");
}
