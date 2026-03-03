namespace Bangkok.Application.Configuration;

public class AttachmentSettings
{
    public const string SectionName = "Attachments";

    /// <summary>Max file size in bytes. Default 10 MB.</summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Allowed MIME types (e.g. image/*, application/pdf). Empty = allow all.</summary>
    public string[] AllowedContentTypes { get; set; } = new[]
    {
        "image/*",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain",
        "text/csv"
    };
}
