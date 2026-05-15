using ErrorOr;

namespace Tailbook.Modules.Catalog.Domain.Aggregates;

public static class CatalogErrors
{
    public static Error CodeRequired => Error.Validation(
        code: "Catalog.CodeRequired",
        description: "Code is required.");

    public static Error UnknownOfferType(string offerType) => Error.Validation(
        code: "Catalog.UnknownOfferType",
        description: $"Unknown offer type '{offerType}'.");

    public static Error UnknownComponentRole(string componentRole) => Error.Validation(
        code: "Catalog.UnknownComponentRole",
        description: $"Unknown component role '{componentRole}'.");

    public static Error OfferVersionImmutable => Error.Conflict(
        code: "Catalog.OfferVersionImmutable",
        description: "Published or archived offer versions are immutable.");

    public static Error OfferVersionNotDraft => Error.Conflict(
        code: "Catalog.OfferVersionNotDraft",
        description: "Only draft offer versions can be published.");

    public static Error OfferVersionNotPackage => Error.Validation(
        code: "Catalog.OfferVersionNotPackage",
        description: "Only package offer versions can have operational components.");

    public static Error OfferVersionNotFound => Error.NotFound(
        code: "Catalog.OfferVersionNotFound",
        description: "Offer version does not exist.");

    public static Error ComponentSequenceExists => Error.Conflict(
        code: "Catalog.ComponentSequenceExists",
        description: "A component with the same sequence number already exists in this version.");

    public static Error ComponentProcedureExists => Error.Conflict(
        code: "Catalog.ComponentProcedureExists",
        description: "The same procedure cannot be added twice to one offer version.");

    public static Error PackageOfferVersionEmpty => Error.Validation(
        code: "Catalog.PackageOfferVersionEmpty",
        description: "Package offer versions must contain at least one component before publication.");

    public static Error CurrencyRequired => Error.Validation(
        code: "Catalog.CurrencyRequired",
        description: "Currency is required.");

    public static Error PriceRuleSetNotDraft => Error.Conflict(
        code: "Catalog.PriceRuleSetNotDraft",
        description: "Price rules can only be added to draft rule sets.");

    public static Error PublishPriceRuleSetNotDraft => Error.Conflict(
        code: "Catalog.PriceRuleSetNotDraft",
        description: "Only draft price rule sets can be published.");

    public static Error DuplicatePriceRule => Error.Conflict(
        code: "Catalog.DuplicatePriceRule",
        description: "An equivalent price rule already exists in this rule set for the same offer and condition combination.");

    public static Error PriceRuleSetEmpty => Error.Validation(
        code: "Catalog.PriceRuleSetEmpty",
        description: "A price rule set must contain at least one rule before publication.");

    public static Error DurationRuleSetNotDraft => Error.Conflict(
        code: "Catalog.DurationRuleSetNotDraft",
        description: "Duration rules can only be added to draft rule sets.");

    public static Error PublishDurationRuleSetNotDraft => Error.Conflict(
        code: "Catalog.DurationRuleSetNotDraft",
        description: "Only draft duration rule sets can be published.");

    public static Error DuplicateDurationRule => Error.Conflict(
        code: "Catalog.DuplicateDurationRule",
        description: "An equivalent duration rule already exists in this rule set for the same offer and condition combination.");

    public static Error DurationRuleSetEmpty => Error.Validation(
        code: "Catalog.DurationRuleSetEmpty",
        description: "A duration rule set must contain at least one rule before publication.");
}
