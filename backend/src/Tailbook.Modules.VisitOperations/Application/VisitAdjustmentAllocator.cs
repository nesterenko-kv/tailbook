namespace Tailbook.Modules.VisitOperations.Application;

public static class VisitAdjustmentAllocator
{
    public static IReadOnlyDictionary<Guid, decimal> AllocateAdjustmentsPerItem(
        IReadOnlyCollection<VisitItemAdjustment> items,
        IReadOnlyCollection<VisitAdjustmentFlat> adjustments)
    {
        var itemTotals = items.ToDictionary(x => x.ItemId, x => x.TotalAmount);
        var grandTotal = itemTotals.Values.Sum();

        var result = new Dictionary<Guid, decimal>();
        foreach (var itemId in itemTotals.Keys)
        {
            result[itemId] = 0m;
        }

        foreach (var adj in adjustments)
        {
            if (adj.TargetItemId.HasValue && result.ContainsKey(adj.TargetItemId.Value))
            {
                result[adj.TargetItemId.Value] += adj.Sign * adj.Amount;
            }
            else if (grandTotal > 0)
            {
                foreach (var itemId in itemTotals.Keys)
                {
                    var share = itemTotals[itemId] / grandTotal;
                    var allocated = Math.Round(share * adj.Sign * adj.Amount, 2, MidpointRounding.AwayFromZero);
                    result[itemId] += allocated;
                }
            }
        }

        return result;
    }
}

public sealed record VisitItemAdjustment(Guid ItemId, decimal TotalAmount);

public sealed record VisitAdjustmentFlat(Guid AdjustmentId, int Sign, decimal Amount, Guid? TargetItemId);
