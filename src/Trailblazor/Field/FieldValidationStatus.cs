namespace Trailblazor;

/// <summary>Where a field's validation stands, as its <c>ValidationStatus</c> reports it.</summary>
public enum FieldValidationStatus
{
    /// <summary>Nothing to report: the field shows no error, and no <c>ValidationAsync</c> check has passed its value.
    /// </summary>
    None,

    /// <summary>Its <c>ValidationAsync</c> check is running.</summary>
    Checking,

    /// <summary>Its <c>ValidationAsync</c> check passed the value it holds.</summary>
    Valid,

    /// <summary>It shows an error.</summary>
    Invalid,
}
