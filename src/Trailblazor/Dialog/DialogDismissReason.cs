using System.Text.Json.Serialization;

namespace Trailblazor;

/// <summary>How the user asked to dismiss a dialog, as <see cref="TbDialogContent{TResult}.CanDismissAsync"/> and
/// <see cref="TbDialog.CanDismiss"/> are told.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DialogDismissReason>))]
public enum DialogDismissReason
{
    /// <summary>The user pressed Escape, or made another close request such as a back gesture.</summary>
    Escape,

    /// <summary>The user clicked the backdrop outside a modal dialog.</summary>
    Backdrop,

    /// <summary>The user pressed the close button in the dialog's header.</summary>
    CloseButton,
}
