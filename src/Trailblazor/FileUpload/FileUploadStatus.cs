namespace Trailblazor;

/// <summary>Where a chosen file stands in a <see cref="TbFileUpload"/>.</summary>
public enum FileUploadStatus
{
    /// <summary>Chosen and not sent yet, or the field has no <see cref="TbFileUpload.Upload"/>.</summary>
    Chosen,

    /// <summary>Being sent by the field's <see cref="TbFileUpload.Upload"/>.</summary>
    Uploading,

    /// <summary>Sent: <see cref="TbFileUpload.Upload"/> finished without an error.</summary>
    Uploaded,

    /// <summary><see cref="TbFileUpload.Upload"/> threw. The file can be tried again.</summary>
    Failed,

    /// <summary>Its upload was canceled. The file can be tried again.</summary>
    Canceled,
}
