namespace Trailblazor.Testing;

/// <summary>Thrown by <see cref="TbContract.VerifyAsync{TComponent}"/> with each way the component breaks the rules
/// Trailblazor's components keep.</summary>
public sealed class TbContractException : Exception
{
    /// <summary>Creates the exception for the ways a component breaks the rules.</summary>
    public TbContractException(IReadOnlyList<string> failures)
        : base("The component failed Trailblazor's contract checks:" + Environment.NewLine
            + string.Join(Environment.NewLine, failures.Select(failure => "- " + failure))) => Failures = failures;

    /// <summary>Each way the component breaks a rule.</summary>
    public IReadOnlyList<string> Failures { get; }
}
