using MockErp.Api.Models;

namespace MockErp.Api.Validation;

public static class RoutingValidator
{
    public static void Validate(MockErpRouting routing)
    {
        ArgumentNullException.ThrowIfNull(routing);

        EnsureUnique(
            routing.Operations.Select(operation => operation.OperationReference),
            "operation reference",
            routing.RoutingReference);
        EnsureUnique(
            routing.Operations.Select(operation => operation.OperationSequence),
            "operation sequence",
            routing.RoutingReference);

        var knownOperationReferences = new HashSet<string>(
            routing.Operations.Select(operation => operation.OperationReference),
            StringComparer.Ordinal);

        foreach (var operation in routing.Operations)
        {
            if (operation.StandardDurationMinutes <= 0)
            {
                throw new InvalidOperationException(
                    $"Routing '{routing.RoutingReference}' operation '{operation.OperationReference}' " +
                    $"has an invalid StandardDurationMinutes ({operation.StandardDurationMinutes}); " +
                    "it must be positive.");
            }

            if (operation.OperationSequence <= 0)
            {
                throw new InvalidOperationException(
                    $"Routing '{routing.RoutingReference}' operation '{operation.OperationReference}' " +
                    $"has an invalid OperationSequence ({operation.OperationSequence}); " +
                    "it must be positive.");
            }

            foreach (var predecessor in operation.PredecessorOperationReferences)
            {
                if (string.Equals(
                        predecessor,
                        operation.OperationReference,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Routing '{routing.RoutingReference}' operation '{operation.OperationReference}' " +
                        "cannot list itself as a predecessor.");
                }

                if (!knownOperationReferences.Contains(predecessor))
                {
                    throw new InvalidOperationException(
                        $"Routing '{routing.RoutingReference}' operation '{operation.OperationReference}' " +
                        $"references predecessor '{predecessor}', which is not part of the same routing.");
                }
            }
        }
    }

    private static void EnsureUnique<T>(
        IEnumerable<T> identifiers,
        string identifierType,
        string routingReference)
        where T : notnull
    {
        var duplicate = identifiers
            .GroupBy(identifier => identifier)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Routing '{routingReference}' contains duplicate {identifierType} '{duplicate.Key}'.");
        }
    }
}
