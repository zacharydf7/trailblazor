namespace Trailblazor;

/// <summary>Stands for one message an <see cref="IAnnouncer"/> announced, so whoever announced it can withdraw that
/// message alone with <see cref="IAnnouncer.Withdraw(AnnouncementHandle)"/>. Each message gets its own handle, even
/// when another message has the same text.</summary>
public sealed class AnnouncementHandle;
