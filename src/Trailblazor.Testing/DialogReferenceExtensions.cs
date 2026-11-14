namespace Trailblazor.Testing;

/// <summary>Reads the message of a confirm or alert that <see cref="FakeDialogService"/> recorded.</summary>
public static class DialogReferenceExtensions
{
    extension(DialogReference dialog)
    {
        /// <summary>The message of a confirm or alert, or null for any other dialog.</summary>
        public string? Message => dialog.ContentType == typeof(ConfirmDialog)
            ? dialog.Parameters[nameof(ConfirmDialog.Message)] as string : null;
    }
}
