using Microsoft.AspNetCore.Components.Forms;

namespace Trailblazor;

/// <summary>A file a <see cref="TbFileUpload"/> lists, with its upload's status and progress, for
/// <see cref="TbFileUpload.FileTemplate"/> to show.</summary>
public sealed class FileUploadItem
{
    internal FileUploadItem(IBrowserFile file) => File = file;

    /// <summary>The chosen file.</summary>
    public IBrowserFile File { get; }

    /// <summary>Where the file stands.</summary>
    public FileUploadStatus Status { get; internal set; }

    /// <summary>How many bytes <see cref="TbFileUpload.Upload"/> last reported sent.</summary>
    public long Sent { get; internal set; }

    // Stops this file's upload when it is canceled, removed or the field goes away.
    internal CancellationTokenSource? Stop { get; set; }
}
