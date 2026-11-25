namespace Trailblazor;

/// <summary>A failed load of a <see cref="TbAsyncContent"/>, given to its <see cref="TbAsyncContent.FailedContent"/>.
/// </summary>
/// <param name="Exception">What the load threw.</param>
/// <param name="RetryAsync">Loads again, keeping the failure in view until the load finishes. Hand it to a button's
/// <c>OnClick</c>, so the button shows that it's busy meanwhile.</param>
public sealed record AsyncContentFailure(Exception Exception, Func<Task> RetryAsync);
