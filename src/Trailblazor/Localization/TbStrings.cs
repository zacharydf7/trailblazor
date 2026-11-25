using System.Collections.Frozen;

namespace Trailblazor;

/// <summary>The keys of Trailblazor's built-in strings, for <see cref="ITbLocalizer"/> and
/// <see cref="TrailblazorOptions.Strings"/>. Each summary gives the English text, with what its placeholders hold.
/// </summary>
public static class TbStrings
{
    /// <summary>"Close": names an alert's close button.</summary>
    public const string AlertClose = "Alert.Close";

    /// <summary>"Notice": names a banner with no title for screen readers.</summary>
    public const string BannerLabel = "Banner.Label";

    /// <summary>"Dismiss": names a banner's close button.</summary>
    public const string BannerClose = "Banner.Close";

    /// <summary>"Previous slide": names a carousel's previous button.</summary>
    public const string CarouselPrevious = "Carousel.Previous";

    /// <summary>"Next slide": names a carousel's next button.</summary>
    public const string CarouselNext = "Carousel.Next";

    /// <summary>"Slide {0}": names a carousel's button that shows a slide; {0} is the slide's number.</summary>
    public const string CarouselSlide = "Carousel.Slide";

    /// <summary>"{0} of {1}": names a carousel's slide; {0} is its number and {1} the number of slides.</summary>
    public const string CarouselSlideOf = "Carousel.SlideOf";

    /// <summary>"Stop slide rotation": names the button that stops a carousel turning its slides.</summary>
    public const string CarouselStop = "Carousel.Stop";

    /// <summary>"Start slide rotation": names the button that starts a carousel turning its slides again.</summary>
    public const string CarouselStart = "Carousel.Start";

    /// <summary>"Message": names the box a chat's user types in.</summary>
    public const string ChatMessage = "Chat.Message";

    /// <summary>"Send": the chat composer's send button.</summary>
    public const string ChatSend = "Chat.Send";

    /// <summary>"Attach files": the chat composer's attach button.</summary>
    public const string ChatAttach = "Chat.Attach";

    /// <summary>"{0} is typing…": shown under a chat's messages while one person types; {0} is their name.</summary>
    public const string ChatTyping = "Chat.Typing";

    /// <summary>"{0} and {1} are typing…": shown while several people type; {0} is the list of all but the last
    /// name, and {1} the last name.</summary>
    public const string ChatTypingMany = "Chat.TypingMany";

    /// <summary>"Newest messages": the button that goes back to the newest message after scrolling up.</summary>
    public const string ChatNewest = "Chat.Newest";

    /// <summary>"Generate": an AI prompt's button that sends the prompt.</summary>
    public const string AiPromptGenerate = "AiPrompt.Generate";

    /// <summary>"Stop": an AI prompt's button that stops a response while it generates.</summary>
    public const string AiPromptStop = "AiPrompt.Stop";

    /// <summary>"Suggestions": names an AI prompt's group of suggested prompts.</summary>
    public const string AiPromptSuggestions = "AiPrompt.Suggestions";

    /// <summary>"Responses": names an AI prompt's list of responses.</summary>
    public const string AiPromptResponses = "AiPrompt.Responses";

    /// <summary>"Copy": copies a response's text.</summary>
    public const string AiPromptCopy = "AiPrompt.Copy";

    /// <summary>"Retry": generates a response again for the same prompt.</summary>
    public const string AiPromptRetry = "AiPrompt.Retry";

    /// <summary>"Insert": hands a response's text to the app, such as into the document being written.</summary>
    public const string AiPromptInsert = "AiPrompt.Insert";

    /// <summary>"Generating a response": read out when a response starts.</summary>
    public const string AiPromptGenerating = "AiPrompt.Generating";

    /// <summary>"Response ready": read out when a response is done.</summary>
    public const string AiPromptDone = "AiPrompt.Done";

    /// <summary>"Response stopped": read out when the user stops a response.</summary>
    public const string AiPromptStopped = "AiPrompt.Stopped";

    /// <summary>"Couldn't generate a response. Try again.": shown on a failed response and read out.</summary>
    public const string AiPromptFailed = "AiPrompt.Failed";

    /// <summary>"Copied": read out once a response is copied.</summary>
    public const string AiPromptCopied = "AiPrompt.Copied";

    /// <summary>"{0} of {1}": a Kanban column's count against its limit; {0} is the cards in it and {1} the limit.
    /// </summary>
    public const string KanbanCount = "Kanban.Count";

    /// <summary>"{0} is full: its limit is {1}.": why a card can't move into a full Kanban column; {0} is the
    /// column's title and {1} its limit.</summary>
    public const string KanbanFull = "Kanban.Full";

    /// <summary>"{0}, more choices": names a cascader's choice that opens more choices; {0} is its text.</summary>
    public const string CascaderMore = "Cascader.More";

    /// <summary>"Results: {0}": read out with the number of paths that match what was typed in a cascader.</summary>
    public const string CascaderResults = "Cascader.Results";

    /// <summary>"No matches": shown when no path matches what was typed in a cascader.</summary>
    public const string CascaderNoResults = "Cascader.NoResults";

    /// <summary>"Clear": names a cascader's clear button.</summary>
    public const string CascaderClear = "Cascader.Clear";

    /// <summary>"Suggestions: {0}": read out as a mention input's suggestions come in; {0} is how many.</summary>
    public const string MentionInputSuggestions = "MentionInput.Suggestions";

    /// <summary>"Couldn't load suggestions.": read out when a mention input's search fails.</summary>
    public const string MentionInputSearchFailed = "MentionInput.SearchFailed";

    /// <summary>"Previous image": names a lightbox's previous button.</summary>
    public const string LightboxPrevious = "Lightbox.Previous";

    /// <summary>"Next image": names a lightbox's next button.</summary>
    public const string LightboxNext = "Lightbox.Next";

    /// <summary>"Zoom in": names a lightbox's zoom in button.</summary>
    public const string LightboxZoomIn = "Lightbox.ZoomIn";

    /// <summary>"Zoom out": names a lightbox's zoom out button.</summary>
    public const string LightboxZoomOut = "Lightbox.ZoomOut";

    /// <summary>"Fit to window": names a lightbox's button that undoes zoom and rotation.</summary>
    public const string LightboxFit = "Lightbox.Fit";

    /// <summary>"Rotate left": names a lightbox's button that turns the image a quarter turn counterclockwise.
    /// </summary>
    public const string LightboxRotateLeft = "Lightbox.RotateLeft";

    /// <summary>"Rotate right": names a lightbox's button that turns the image a quarter turn clockwise.</summary>
    public const string LightboxRotateRight = "Lightbox.RotateRight";

    /// <summary>"{0} of {1}": shows which image a lightbox shows; {0} is its number and {1} the number of images.
    /// </summary>
    public const string LightboxPosition = "Lightbox.Position";

    /// <summary>"{0}, {1} of {2}": read out when a lightbox shows another image; {0} is its alt text, {1} its number
    /// and {2} the number of images.</summary>
    public const string LightboxShown = "Lightbox.Shown";

    /// <summary>"{0}%": shows a lightbox's zoom, where 100% fits the image to the window.</summary>
    public const string LightboxZoom = "Lightbox.Zoom";

    /// <summary>"Navigation": names an app shell's menu button and titles its navigation drawer.</summary>
    public const string AppShellMenu = "AppShell.Menu";

    /// <summary>"Main": names an app shell's navigation landmark, heard as "Main navigation".</summary>
    public const string AppShellNavigation = "AppShell.Navigation";

    /// <summary>"Skip to content": the text of an app shell's link that skips to the page's content.</summary>
    public const string AppShellSkipToContent = "AppShell.SkipToContent";

    /// <summary>"Clear": names an autocomplete's clear button.</summary>
    public const string AutocompleteClear = "Autocomplete.Clear";

    /// <summary>"Searching…": shown while an autocomplete waits for suggestions.</summary>
    public const string AutocompleteSearching = "Autocomplete.Searching";

    /// <summary>"No matches": shown when an autocomplete finds nothing.</summary>
    public const string AutocompleteNoResults = "Autocomplete.NoResults";

    /// <summary>"Couldn't load suggestions.": shown and read out when an autocomplete's search fails.</summary>
    public const string AutocompleteSearchFailed = "Autocomplete.SearchFailed";

    /// <summary>"Type to search": shown before the user has typed enough for suggestions.</summary>
    public const string AutocompleteTypeToSearch = "Autocomplete.TypeToSearch";

    /// <summary>"Suggestions: {0}": announced when suggestions arrive; {0} is how many.</summary>
    public const string AutocompleteResults = "Autocomplete.Results";

    /// <summary>"{0} more": names an avatar group's count of the people not shown; {0} is how many.</summary>
    public const string AvatarGroupMore = "AvatarGroup.More";

    /// <summary>"Breadcrumb": names a breadcrumb trail's navigation.</summary>
    public const string BreadcrumbsNavigation = "Breadcrumbs.Navigation";

    /// <summary>"Remove": names the remove button of a chip with no text.</summary>
    public const string ChipRemove = "Chip.Remove";

    /// <summary>"Remove {0}": names a chip's remove button; {0} is the chip's text.</summary>
    public const string ChipRemoveNamed = "Chip.RemoveNamed";

    /// <summary>"Command palette": names a command palette's dialog.</summary>
    public const string CommandPaletteLabel = "CommandPalette.Label";

    /// <summary>"Type a command or search": the placeholder in a command palette's search box.</summary>
    public const string CommandPalettePlaceholder = "CommandPalette.Placeholder";

    /// <summary>"No matching commands": shown, and read out, when nothing in a command palette matches.</summary>
    public const string CommandPaletteNoResults = "CommandPalette.NoResults";

    /// <summary>"Results: {0}": announced when the number of a command palette's results changes; {0} is how many.
    /// </summary>
    public const string CommandPaletteResults = "CommandPalette.Results";

    /// <summary>"Search commands": describes a command palette's shortcut in a hotkeys help.</summary>
    public const string CommandPaletteShortcut = "CommandPalette.Shortcut";

    /// <summary>"Keyboard shortcuts": the title of the dialog that lists the page's hotkeys.</summary>
    public const string HotkeysHelpTitle = "HotkeysHelp.Title";

    /// <summary>"Show keyboard shortcuts": what the hotkey that opens the list of hotkeys does, as the list shows it.
    /// </summary>
    public const string HotkeysHelpShow = "HotkeysHelp.Show";

    /// <summary>"General": the heading of the hotkeys that have no group.</summary>
    public const string HotkeysHelpGeneral = "HotkeysHelp.General";

    /// <summary>"Keys": the heading screen readers hear for the column of keys in the list of hotkeys.</summary>
    public const string HotkeysHelpKeys = "HotkeysHelp.Keys";

    /// <summary>"Action": the heading screen readers hear for the column that says what each hotkey does.</summary>
    public const string HotkeysHelpAction = "HotkeysHelp.Action";

    /// <summary>"Choose a color": names the button that opens a color picker's panel.</summary>
    public const string ColorPickerOpen = "ColorPicker.Open";

    /// <summary>"Pick a color from the screen": names the eyedropper button.</summary>
    public const string ColorPickerEyedropper = "ColorPicker.Eyedropper";

    /// <summary>"Clear": names a color picker's clear button.</summary>
    public const string ColorPickerClear = "ColorPicker.Clear";

    /// <summary>"Swatches": names the list of preset colors.</summary>
    public const string ColorPickerSwatches = "ColorPicker.Swatches";

    /// <summary>"Hue": names the hue slider.</summary>
    public const string ColorPickerHue = "ColorPicker.Hue";

    /// <summary>"Saturation": names the saturation axis of the color area.</summary>
    public const string ColorPickerSaturation = "ColorPicker.Saturation";

    /// <summary>"Brightness": names the brightness axis of the color area.</summary>
    public const string ColorPickerBrightness = "ColorPicker.Brightness";

    /// <summary>"Opacity": names the alpha slider.</summary>
    public const string ColorPickerAlpha = "ColorPicker.Alpha";

    /// <summary>"Red": names the red channel field.</summary>
    public const string ColorPickerRed = "ColorPicker.Red";

    /// <summary>"Green": names the green channel field.</summary>
    public const string ColorPickerGreen = "ColorPicker.Green";

    /// <summary>"Blue": names the blue channel field.</summary>
    public const string ColorPickerBlue = "ColorPicker.Blue";

    /// <summary>"Enter a color as #RRGGBB.": shown when the typed color can't be read.</summary>
    public const string ColorPickerBadColor = "ColorPicker.BadColor";

    /// <summary>"Choose one of the palette's colors.": shown when a palette-only field is given another color.
    /// </summary>
    public const string ColorPickerNotInPalette = "ColorPicker.NotInPalette";

    /// <summary>"Previous month": names a calendar's button that shows the month before.</summary>
    public const string CalendarPreviousMonth = "Calendar.PreviousMonth";

    /// <summary>"Next month": names a calendar's button that shows the month after.</summary>
    public const string CalendarNextMonth = "Calendar.NextMonth";

    /// <summary>"Previous year": names a calendar's button that shows the year before, while months are shown.
    /// </summary>
    public const string CalendarPreviousYear = "Calendar.PreviousYear";

    /// <summary>"Next year": names a calendar's button that shows the year after, while months are shown.</summary>
    public const string CalendarNextYear = "Calendar.NextYear";

    /// <summary>"Previous decade": names a calendar's button that shows the decade before, while years are shown.
    /// </summary>
    public const string CalendarPreviousDecade = "Calendar.PreviousDecade";

    /// <summary>"Next decade": names a calendar's button that shows the decade after, while years are shown.</summary>
    public const string CalendarNextDecade = "Calendar.NextDecade";

    /// <summary>"{0}, choose a month": names a calendar's title button while days are shown, which shows the year's
    /// months; {0} is the month and year.</summary>
    public const string CalendarChooseMonth = "Calendar.ChooseMonth";

    /// <summary>"{0}, choose a year": names a calendar's title button while months are shown, which shows the
    /// decade's years; {0} is the year.</summary>
    public const string CalendarChooseYear = "Calendar.ChooseYear";

    /// <summary>"Choose a date": names a date picker's button that opens the calendar, and the calendar.</summary>
    public const string DatePickerOpen = "DatePicker.Open";

    /// <summary>"Clear": names a date picker's button that clears the date.</summary>
    public const string DatePickerClear = "DatePicker.Clear";

    /// <summary>"Enter the date as {0}.": shown when a date picker can't read the typed date; {0} is the format, such as
    /// MM/DD/YYYY.</summary>
    public const string DatePickerBadDate = "DatePicker.BadDate";

    /// <summary>"D": the letter for a day in a date format hint, such as the D in MM/DD/YYYY; French uses J.</summary>
    public const string DateHintDay = "Date.HintDay";

    /// <summary>"M": the letter for a month in a date format hint, such as the M in MM/DD/YYYY.</summary>
    public const string DateHintMonth = "Date.HintMonth";

    /// <summary>"Y": the letter for a year in a date format hint, such as the Y in MM/DD/YYYY; German uses J.</summary>
    public const string DateHintYear = "Date.HintYear";

    /// <summary>"Not set": what screen readers hear for a value that's missing, shown as a dash.</summary>
    public const string EmptyValue = "Value.Empty";

    /// <summary>"UTC{0}": a timestamp's zone when the app has no abbreviation for it; {0} is the offset, such as "−5",
    /// or nothing at UTC itself.</summary>
    public const string TimestampZone = "Timestamp.Zone";

    /// <summary>"just now": a relative timestamp less than 45 seconds from now, either way.</summary>
    public const string TimestampJustNow = "Timestamp.JustNow";

    /// <summary>"{0} minutes ago": a relative timestamp of a past moment; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="TimestampMinutesAgoOne"/> as its one-form.</summary>
    public const string TimestampMinutesAgo = "Timestamp.MinutesAgo";

    /// <summary>"1 minute ago": the one-form of <see cref="TimestampMinutesAgo"/>.</summary>
    public const string TimestampMinutesAgoOne = "Timestamp.MinutesAgoOne";

    /// <summary>"in {0} minutes": a relative timestamp of a moment ahead; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="TimestampInMinutesOne"/> as its one-form.</summary>
    public const string TimestampInMinutes = "Timestamp.InMinutes";

    /// <summary>"in 1 minute": the one-form of <see cref="TimestampInMinutes"/>.</summary>
    public const string TimestampInMinutesOne = "Timestamp.InMinutesOne";

    /// <summary>"{0} hours ago": a relative timestamp of a past moment; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="TimestampHoursAgoOne"/> as its one-form.</summary>
    public const string TimestampHoursAgo = "Timestamp.HoursAgo";

    /// <summary>"1 hour ago": the one-form of <see cref="TimestampHoursAgo"/>.</summary>
    public const string TimestampHoursAgoOne = "Timestamp.HoursAgoOne";

    /// <summary>"in {0} hours": a relative timestamp of a moment ahead; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="TimestampInHoursOne"/> as its one-form.</summary>
    public const string TimestampInHours = "Timestamp.InHours";

    /// <summary>"in 1 hour": the one-form of <see cref="TimestampInHours"/>.</summary>
    public const string TimestampInHoursOne = "Timestamp.InHoursOne";

    /// <summary>"{0} days ago": a relative timestamp of a past moment; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="TimestampDaysAgoOne"/> as its one-form.</summary>
    public const string TimestampDaysAgo = "Timestamp.DaysAgo";

    /// <summary>"1 day ago": the one-form of <see cref="TimestampDaysAgo"/>.</summary>
    public const string TimestampDaysAgoOne = "Timestamp.DaysAgoOne";

    /// <summary>"in {0} days": a relative timestamp of a moment ahead; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="TimestampInDaysOne"/> as its one-form.</summary>
    public const string TimestampInDays = "Timestamp.InDays";

    /// <summary>"in 1 day": the one-form of <see cref="TimestampInDays"/>.</summary>
    public const string TimestampInDaysOne = "Timestamp.InDaysOne";

    /// <summary>"{0} months ago": a relative timestamp of a past moment; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="TimestampMonthsAgoOne"/> as its one-form.</summary>
    public const string TimestampMonthsAgo = "Timestamp.MonthsAgo";

    /// <summary>"1 month ago": the one-form of <see cref="TimestampMonthsAgo"/>.</summary>
    public const string TimestampMonthsAgoOne = "Timestamp.MonthsAgoOne";

    /// <summary>"in {0} months": a relative timestamp of a moment ahead; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="TimestampInMonthsOne"/> as its one-form.</summary>
    public const string TimestampInMonths = "Timestamp.InMonths";

    /// <summary>"in 1 month": the one-form of <see cref="TimestampInMonths"/>.</summary>
    public const string TimestampInMonthsOne = "Timestamp.InMonthsOne";

    /// <summary>"{0} years ago": a relative timestamp of a past moment; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="TimestampYearsAgoOne"/> as its one-form.</summary>
    public const string TimestampYearsAgo = "Timestamp.YearsAgo";

    /// <summary>"1 year ago": the one-form of <see cref="TimestampYearsAgo"/>.</summary>
    public const string TimestampYearsAgoOne = "Timestamp.YearsAgoOne";

    /// <summary>"in {0} years": a relative timestamp of a moment ahead; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="TimestampInYearsOne"/> as its one-form.</summary>
    public const string TimestampInYears = "Timestamp.InYears";

    /// <summary>"in 1 year": the one-form of <see cref="TimestampInYears"/>.</summary>
    public const string TimestampInYearsOne = "Timestamp.InYearsOne";

    /// <summary>"Go now": the button that runs a countdown's action at once.</summary>
    public const string CountdownGoNow = "Countdown.GoNow";

    /// <summary>"Stay on this page": the button that stops a countdown.</summary>
    public const string CountdownStay = "Countdown.Stay";

    /// <summary>"Stayed on this page": shown in place of a countdown that was stopped.</summary>
    public const string CountdownStayed = "Countdown.Stayed";

    /// <summary>"{0} seconds": the time a countdown has left, under a minute; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="CountdownSecondsOne"/> as its one-form.</summary>
    public const string CountdownSeconds = "Countdown.Seconds";

    /// <summary>"1 second": the one-form of <see cref="CountdownSeconds"/>.</summary>
    public const string CountdownSecondsOne = "Countdown.SecondsOne";

    /// <summary>"{0} minutes": the time a countdown has left, a minute or more, rounded up; {0} is how many. Asked
    /// for through <see cref="ITbLocalizer.Plural"/>, with <see cref="CountdownMinutesOne"/> as its one-form.</summary>
    public const string CountdownMinutes = "Countdown.Minutes";

    /// <summary>"1 minute": the one-form of <see cref="CountdownMinutes"/>.</summary>
    public const string CountdownMinutesOne = "Countdown.MinutesOne";

    /// <summary>"Copy": a copy button's words, and its name when it shows only its icon.</summary>
    public const string CopyButtonCopy = "CopyButton.Copy";

    /// <summary>"Copied": shown and announced for a moment after a copy button copies.</summary>
    public const string CopyButtonCopied = "CopyButton.Copied";

    /// <summary>"Couldn't copy": shown and announced for a moment when the browser refuses a copy button's copy.
    /// </summary>
    public const string CopyButtonFailed = "CopyButton.Failed";

    /// <summary>"{0} can't be chosen.": shown when a typed date is outside Min and Max, or IsDateDisabled rules it
    /// out; {0} is the date as typed.</summary>
    public const string DatePickerUnavailable = "DatePicker.Unavailable";

    /// <summary>"Choose a time from {0} to {1}.": shown when a time is outside a time picker's Min and Max; {0} and
    /// {1} are the earliest and latest times.</summary>
    public const string TimePickerOutOfRange = "TimePicker.OutOfRange";

    /// <summary>"Choose {0} or later.": shown when a time, or a date and time, is before a picker's Min and there is no
    /// Max; {0} is the earliest.</summary>
    public const string TimePickerAtLeast = "TimePicker.AtLeast";

    /// <summary>"Choose {0} or earlier.": shown when a time, or a date and time, is after a picker's Max and there is
    /// no Min; {0} is the latest.</summary>
    public const string TimePickerAtMost = "TimePicker.AtMost";

    /// <summary>"Date": names a date-time picker's date box.</summary>
    public const string DateTimePickerDate = "DateTimePicker.Date";

    /// <summary>"Time": names a date-time picker's time box.</summary>
    public const string DateTimePickerTime = "DateTimePicker.Time";

    /// <summary>"Choose a date and time from {0} to {1}.": shown when a date-time picker's value is outside its Min
    /// and Max; {0} and {1} are the earliest and latest.</summary>
    public const string DateTimePickerOutOfRange = "DateTimePicker.OutOfRange";

    /// <summary>"Start date": names a date range's start field, and is added to the start day's name in the calendar.
    /// </summary>
    public const string DateRangePickerStart = "DateRangePicker.Start";

    /// <summary>"End date": names a date range's end field, and is added to the end day's name in the calendar.
    /// </summary>
    public const string DateRangePickerEnd = "DateRangePicker.End";

    /// <summary>"Choose dates": names the button that opens the calendar.</summary>
    public const string DateRangePickerOpen = "DateRangePicker.Open";

    /// <summary>"Previous month": names the calendar's previous-month button.</summary>
    public const string DateRangePickerPreviousMonth = "DateRangePicker.PreviousMonth";

    /// <summary>"Next month": names the calendar's next-month button.</summary>
    public const string DateRangePickerNextMonth = "DateRangePicker.NextMonth";

    /// <summary>"Presets": names the list of preset ranges.</summary>
    public const string DateRangePickerPresets = "DateRangePicker.Presets";

    /// <summary>"Clear": names a date range's clear button.</summary>
    public const string DateRangePickerClear = "DateRangePicker.Clear";

    /// <summary>"Now choose the end date.": announced once the start date is chosen.</summary>
    public const string DateRangePickerChooseEnd = "DateRangePicker.ChooseEnd";

    /// <summary>"{0} to {1}": announced once the end date is chosen; {0} and {1} are the two dates.</summary>
    public const string DateRangePickerChosen = "DateRangePicker.Chosen";

    /// <summary>"Enter the start date as {0}.": shown when the start can't be read; {0} is the date format.</summary>
    public const string DateRangePickerBadStart = "DateRangePicker.BadStart";

    /// <summary>"Enter the end date as {0}.": shown when the end can't be read; {0} is the date format.</summary>
    public const string DateRangePickerBadEnd = "DateRangePicker.BadEnd";

    /// <summary>"{0} can't be chosen.": shown for a date the rules leave out; {0} is the date.</summary>
    public const string DateRangePickerUnavailable = "DateRangePicker.Unavailable";

    /// <summary>"The end date is before the start date.": shown for a range that runs backwards.</summary>
    public const string DateRangePickerEndBeforeStart = "DateRangePicker.EndBeforeStart";

    /// <summary>"Close": names a dialog's close button.</summary>
    public const string DialogClose = "Dialog.Close";

    /// <summary>"Minimize": names a dialog's minimize button.</summary>
    public const string DialogMinimize = "Dialog.Minimize";

    /// <summary>"Maximize": names a dialog's maximize button.</summary>
    public const string DialogMaximize = "Dialog.Maximize";

    /// <summary>"Restore": names the minimize or maximize button of a dialog that is minimized or maximized.</summary>
    public const string DialogRestore = "Dialog.Restore";

    /// <summary>"Move": names a dialog's Move button and menu.</summary>
    public const string DialogMove = "Dialog.Move";

    /// <summary>"Arrow keys move the dialog.": describes the Move button.</summary>
    public const string DialogMoveHint = "Dialog.MoveHint";

    /// <summary>"Resize": names a dialog's Resize button and menu.</summary>
    public const string DialogResize = "Dialog.Resize";

    /// <summary>"Arrow keys resize the dialog.": describes the Resize button.</summary>
    public const string DialogResizeHint = "Dialog.ResizeHint";

    /// <summary>"Center": the Move menu's item for <see cref="DialogPosition.Center"/>.</summary>
    public const string DialogPositionCenter = "Dialog.Position.Center";

    /// <summary>"Top": the Move menu's item for <see cref="DialogPosition.Top"/>.</summary>
    public const string DialogPositionTop = "Dialog.Position.Top";

    /// <summary>"Bottom": the Move menu's item for <see cref="DialogPosition.Bottom"/>.</summary>
    public const string DialogPositionBottom = "Dialog.Position.Bottom";

    /// <summary>"Start": the Move menu's item for <see cref="DialogPosition.Start"/>.</summary>
    public const string DialogPositionStart = "Dialog.Position.Start";

    /// <summary>"End": the Move menu's item for <see cref="DialogPosition.End"/>.</summary>
    public const string DialogPositionEnd = "Dialog.Position.End";

    /// <summary>"Top start": the Move menu's item for <see cref="DialogPosition.TopStart"/>.</summary>
    public const string DialogPositionTopStart = "Dialog.Position.TopStart";

    /// <summary>"Top end": the Move menu's item for <see cref="DialogPosition.TopEnd"/>.</summary>
    public const string DialogPositionTopEnd = "Dialog.Position.TopEnd";

    /// <summary>"Bottom start": the Move menu's item for <see cref="DialogPosition.BottomStart"/>.</summary>
    public const string DialogPositionBottomStart = "Dialog.Position.BottomStart";

    /// <summary>"Bottom end": the Move menu's item for <see cref="DialogPosition.BottomEnd"/>.</summary>
    public const string DialogPositionBottomEnd = "Dialog.Position.BottomEnd";

    /// <summary>"Small": the Resize menu's item for <see cref="DialogSize.Small"/>.</summary>
    public const string DialogSizeSmall = "Dialog.Size.Small";

    /// <summary>"Medium": the Resize menu's item for <see cref="DialogSize.Medium"/>.</summary>
    public const string DialogSizeMedium = "Dialog.Size.Medium";

    /// <summary>"Large": the Resize menu's item for <see cref="DialogSize.Large"/>.</summary>
    public const string DialogSizeLarge = "Dialog.Size.Large";

    /// <summary>"Extra large": the Resize menu's item for <see cref="DialogSize.ExtraLarge"/>.</summary>
    public const string DialogSizeExtraLarge = "Dialog.Size.ExtraLarge";

    /// <summary>"OK": the confirm button of <see cref="IDialogService.ConfirmAsync"/> and
    /// <see cref="IDialogService.PromptAsync"/>.</summary>
    public const string DialogConfirm = "Dialog.Confirm";

    /// <summary>"Cancel": the cancel button of <see cref="IDialogService.ConfirmAsync"/> and
    /// <see cref="IDialogService.PromptAsync"/>.</summary>
    public const string DialogCancel = "Dialog.Cancel";

    /// <summary>"{0} isn't valid.": shown when a field's text can't be read as its type; {0} is the field's label.
    /// </summary>
    public const string FieldInvalid = "Field.Invalid";

    /// <summary>"{0} is required.": shown when a <c>Required</c> field is empty; {0} is the field's label.</summary>
    public const string FieldRequired = "Field.Required";

    /// <summary>"The field": stands in for the label in a field's messages when it has none.</summary>
    public const string FieldUnnamed = "Field.Unnamed";

    /// <summary>"required": describes a required group or calendar to screen readers, since it can't carry
    /// <c>aria-required</c>.</summary>
    public const string FieldRequiredMark = "Field.RequiredMark";

    /// <summary>"Checking…": shown under a field that shows its validation status while its <c>ValidationAsync</c>
    /// check runs.</summary>
    public const string FieldChecking = "Field.Checking";

    /// <summary>"Drop files here, or click to choose them.": the file upload's drop zone.</summary>
    public const string FileUploadZone = "FileUpload.Zone";

    /// <summary>"Choose files": names the file upload's choose button.</summary>
    public const string FileUploadChoose = "FileUpload.Choose";

    /// <summary>"Remove {0}": names a chosen file's remove button; {0} is the file's name.</summary>
    public const string FileUploadRemove = "FileUpload.Remove";

    /// <summary>"Clear": names the button that removes every chosen file.</summary>
    public const string FileUploadClear = "FileUpload.Clear";

    /// <summary>"{0} files chosen.": announced after choosing; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="FileUploadChosenOne"/> as its one-form.</summary>
    public const string FileUploadChosen = "FileUpload.Chosen";

    /// <summary>"1 file chosen.": announced after choosing one file; {0}, if used, is the count.</summary>
    public const string FileUploadChosenOne = "FileUpload.ChosenOne";

    /// <summary>"{0} isn't one of {1}.": shown for a file of the wrong type; {0} is its name, {1} the allowed types.
    /// </summary>
    public const string FileUploadWrongType = "FileUpload.WrongType";

    /// <summary>"{0} is larger than {1}.": shown for a file over the size limit; {0} is its name, {1} the limit.
    /// </summary>
    public const string FileUploadTooLarge = "FileUpload.TooLarge";

    /// <summary>"{0} B": a file's size in bytes; {0} is the number.</summary>
    public const string FileUploadBytes = "FileUpload.Bytes";

    /// <summary>"{0} KB": a file's size in kilobytes; {0} is the number.</summary>
    public const string FileUploadKilobytes = "FileUpload.Kilobytes";

    /// <summary>"{0} MB": a file's size in megabytes; {0} is the number.</summary>
    public const string FileUploadMegabytes = "FileUpload.Megabytes";

    /// <summary>"{0} GB": a file's size in gigabytes; {0} is the number.</summary>
    public const string FileUploadGigabytes = "FileUpload.Gigabytes";

    /// <summary>"Choose at most {0} files.": shown when too many files are chosen; {0} is the limit. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="FileUploadTooManyOne"/> as its one-form.</summary>
    public const string FileUploadTooMany = "FileUpload.TooMany";

    /// <summary>"Choose only one file.": shown when several files are chosen for a single-file upload; {0}, if used, is
    /// the limit.</summary>
    public const string FileUploadTooManyOne = "FileUpload.TooManyOne";

    /// <summary>"Information about {0}": names a label's info button; {0} is the label's text.</summary>
    public const string LabelInfo = "Label.Info";

    /// <summary>"Nothing to choose from": shown in a list box when it has no options.</summary>
    public const string ListBoxEmpty = "ListBox.Empty";

    /// <summary>"Available": titles a transfer list's list of items not chosen.</summary>
    public const string TransferListAvailable = "TransferList.Available";

    /// <summary>"Chosen": titles a transfer list's list of chosen items.</summary>
    public const string TransferListChosen = "TransferList.Chosen";

    /// <summary>"Move to {0}": names the button that moves the marked items to the other list; {0} is that list's
    /// title.</summary>
    public const string TransferListMove = "TransferList.Move";

    /// <summary>"Move all to {0}": names the button that moves every item shown to the other list; {0} is that list's
    /// title.</summary>
    public const string TransferListMoveAll = "TransferList.MoveAll";

    /// <summary>"Moved {0} to {1}": read out after a move; {0} is how many items moved and {1} the list's title. Asked
    /// for through <see cref="ITbLocalizer.Plural"/>, so a translation can make the verb agree with the count.
    /// </summary>
    public const string TransferListMoved = "TransferList.Moved";

    /// <summary>"{0} needs all {1} characters.": the error a code field shows while its code is only partly typed;
    /// {0} is the field's label and {1} how many characters the code has. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="CodeFieldIncompleteOne"/> as its one-form.</summary>
    public const string CodeFieldIncomplete = "CodeField.Incomplete";

    /// <summary>"{0} needs its 1 character.": the same error for a code of one character; {0} is the field's label
    /// and {1}, if used, the count.</summary>
    public const string CodeFieldIncompleteOne = "CodeField.IncompleteOne";

    /// <summary>"{0} digits": describes a code field of digits by its length, which only its boxes show; {0} is how
    /// many digits the code has. Asked for through <see cref="ITbLocalizer.Plural"/>, with
    /// <see cref="CodeFieldDigitsOne"/> as its one-form.</summary>
    public const string CodeFieldDigits = "CodeField.Digits";

    /// <summary>"1 digit": the same description for a code of one digit.</summary>
    public const string CodeFieldDigitsOne = "CodeField.DigitsOne";

    /// <summary>"{0} characters": describes a code field of letters and digits by its length; {0} is how many
    /// characters the code has. Asked for through <see cref="ITbLocalizer.Plural"/>, with
    /// <see cref="CodeFieldCharactersOne"/> as its one-form.</summary>
    public const string CodeFieldCharacters = "CodeField.Characters";

    /// <summary>"1 character": the same description for a code of one character.</summary>
    public const string CodeFieldCharactersOne = "CodeField.CharactersOne";

    /// <summary>"Filter {0}": labels the filter box above a list; {0} is the list's title.</summary>
    public const string TransferListFilter = "TransferList.Filter";

    /// <summary>"opens in a new tab": read after the text of a link or menu item that opens a new tab.</summary>
    public const string NewTabHint = "NewTabHint";

    /// <summary>"Decrease": names a number field's step-down button.</summary>
    public const string NumberFieldDecrease = "NumberField.Decrease";

    /// <summary>"Increase": names a number field's step-up button.</summary>
    public const string NumberFieldIncrease = "NumberField.Increase";

    /// <summary>"{0} must be a number.": shown for text that isn't a number; {0} is the field's label.</summary>
    public const string NumberFieldNotANumber = "NumberField.NotANumber";

    /// <summary>"{0} must be from {1} to {2}.": shown for a number out of range; {1} and {2} are the limits.</summary>
    public const string NumberFieldRange = "NumberField.Range";

    /// <summary>"{0} must be at least {1}.": shown for a number below the minimum, {1}.</summary>
    public const string NumberFieldAtLeast = "NumberField.AtLeast";

    /// <summary>"{0} must be at most {1}.": shown for a number above the maximum, {1}.</summary>
    public const string NumberFieldAtMost = "NumberField.AtMost";

    /// <summary>"{0} removed": announced when a multi-select's chip is removed; {0} is its option.</summary>
    public const string MultiSelectRemoved = "MultiSelect.Removed";

    /// <summary>"Select all": names the option that chooses every option of a multi-select.</summary>
    public const string MultiSelectSelectAll = "MultiSelect.SelectAll";

    /// <summary>"Pagination": names the pagination's navigation landmark.</summary>
    public const string PaginationNavigation = "Pagination.Navigation";

    /// <summary>"Page {0}": names a page button; {0} is its number.</summary>
    public const string PaginationPage = "Pagination.Page";

    /// <summary>"Page": names the page box in compact mode.</summary>
    public const string PaginationPageBox = "Pagination.PageBox";

    /// <summary>"of {0}": shown after the page box in compact mode; {0} is the page count.</summary>
    public const string PaginationOf = "Pagination.Of";

    /// <summary>"Previous page": names the previous-page button.</summary>
    public const string PaginationPrevious = "Pagination.Previous";

    /// <summary>"Next page": names the next-page button.</summary>
    public const string PaginationNext = "Pagination.Next";

    /// <summary>"First page": names the first-page button.</summary>
    public const string PaginationFirst = "Pagination.First";

    /// <summary>"Last page": names the last-page button.</summary>
    public const string PaginationLast = "Pagination.Last";

    /// <summary>"Match": names a query builder group's list that picks whether an item must meet all of its conditions or any of them.</summary>
    public const string QueryBuilderMatch = "QueryBuilder.Match";

    /// <summary>"All of these": the choice that makes a query builder group match only items that meet all of its conditions.</summary>
    public const string QueryBuilderAll = "QueryBuilder.All";

    /// <summary>"Any of these": the choice that makes a query builder group match items that meet any of its conditions.</summary>
    public const string QueryBuilderAny = "QueryBuilder.Any";

    /// <summary>"Field": names a query builder condition's list of fields.</summary>
    public const string QueryBuilderField = "QueryBuilder.Field";

    /// <summary>"Comparison": names a query builder condition's list of comparisons, such as "Contains".</summary>
    public const string QueryBuilderComparison = "QueryBuilder.Comparison";

    /// <summary>"Condition {0}": names one condition of a query builder group; {0} is its number in the group.</summary>
    public const string QueryBuilderCondition = "QueryBuilder.Condition";

    /// <summary>"Group {0}": names a group inside a query builder group; {0} is its number among the inner groups.</summary>
    public const string QueryBuilderGroup = "QueryBuilder.Group";

    /// <summary>"Add condition": a query builder group's button that adds a condition to it.</summary>
    public const string QueryBuilderAddCondition = "QueryBuilder.AddCondition";

    /// <summary>"Add group": a query builder group's button that adds a group inside it.</summary>
    public const string QueryBuilderAddGroup = "QueryBuilder.AddGroup";

    /// <summary>"Remove condition {0}": names a query builder condition's remove button; {0} is its number.</summary>
    public const string QueryBuilderRemoveCondition = "QueryBuilder.RemoveCondition";

    /// <summary>"Remove group {0}": names a query builder group's remove button; {0} is its number.</summary>
    public const string QueryBuilderRemoveGroup = "QueryBuilder.RemoveGroup";

    /// <summary>"Is any of": the comparison that matches any of several chosen values.</summary>
    public const string QueryBuilderIsAnyOf = "QueryBuilder.IsAnyOf";

    /// <summary>"Before": the comparison that matches dates before a day.</summary>
    public const string QueryBuilderBefore = "QueryBuilder.Before";

    /// <summary>"On or before": the comparison that matches dates on or before a day.</summary>
    public const string QueryBuilderOnOrBefore = "QueryBuilder.OnOrBefore";

    /// <summary>"After": the comparison that matches dates after a day.</summary>
    public const string QueryBuilderAfter = "QueryBuilder.After";

    /// <summary>"On or after": the comparison that matches dates on or after a day.</summary>
    public const string QueryBuilderOnOrAfter = "QueryBuilder.OnOrAfter";

    /// <summary>"Minimum": names a range slider's lower thumb, after the field's label.</summary>
    public const string RangeSliderStart = "RangeSlider.Start";

    /// <summary>"Maximum": names a range slider's upper thumb, after the field's label.</summary>
    public const string RangeSliderEnd = "RangeSlider.End";

    /// <summary>"1 star": what screen readers hear for a rating's first star; {0}, if used, is its number.</summary>
    public const string RatingStar = "Rating.Star";

    /// <summary>"{0} stars": what screen readers hear for a rating's stars; {0} is the star's number. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="RatingStar"/> as its one-form.</summary>
    public const string RatingStars = "Rating.Stars";

    /// <summary>"Undo": the signature pad's button that takes back the last stroke.</summary>
    public const string SignaturePadUndo = "SignaturePad.Undo";

    /// <summary>"Clear": the signature pad's button that clears the signature.</summary>
    public const string SignaturePadClear = "SignaturePad.Clear";

    /// <summary>"Type your name": the signature pad's toggle that switches to typing a name instead of drawing.
    /// </summary>
    public const string SignaturePadTypeName = "SignaturePad.TypeName";

    /// <summary>"Your full name": the label of the signature pad's field for a typed name.</summary>
    public const string SignaturePadTypedName = "SignaturePad.TypedName";

    /// <summary>"{0}, nothing drawn": what screen readers hear for an empty signature pad; {0} is its label.</summary>
    public const string SignaturePadBlank = "SignaturePad.Blank";

    /// <summary>"{0}, signed": what screen readers hear for a signature pad with something drawn; {0} is its label.
    /// </summary>
    public const string SignaturePadDrawn = "SignaturePad.Drawn";

    /// <summary>"Filter": names a select's or a list box's filter box.</summary>
    public const string SelectFilter = "Select.Filter";

    /// <summary>"No matches": shown when a select's or a list box's filter finds nothing.</summary>
    public const string SelectNoResults = "Select.NoResults";

    /// <summary>"Nothing to choose from": shown in a select's list when it has no options.</summary>
    public const string SelectEmpty = "Select.Empty";

    /// <summary>"Clear": names a select's clear button.</summary>
    public const string SelectClear = "Select.Clear";

    /// <summary>"Panel group {0}": names the tab list of a dock's group; {0} is the group's number.</summary>
    public const string DockGroup = "Dock.Group";

    /// <summary>"Resize {0}": names a dock's separator, where {0} is the title of the panel showing before it, and
    /// a slid-out panel's inner edge, where it is that panel's title.</summary>
    public const string DockResize = "Dock.Resize";

    /// <summary>"Press Delete to close": tells screen readers how to close a dock's closable tab.</summary>
    public const string DockCloseHint = "Dock.CloseHint";

    /// <summary>"Unsaved changes": tells screen readers that a dock panel holds unsaved changes.</summary>
    public const string DockModified = "Dock.Modified";

    /// <summary>"Options for {0}": names a dock group's menu button; {0} is the title of its showing panel.</summary>
    public const string DockMenu = "Dock.Menu";

    /// <summary>"Tab options": names the menu that opens on a dock's tab with a right-click, a long press, Shift+F10
    /// or the context menu key.</summary>
    public const string DockTabMenu = "Dock.TabMenu";

    /// <summary>"Move tab back": a dock menu item that moves the tab one place toward the start of its row.</summary>
    public const string DockMoveTabBack = "Dock.MoveTabBack";

    /// <summary>"Move tab forward": a dock menu item that moves the tab one place toward the end of its row.</summary>
    public const string DockMoveTabForward = "Dock.MoveTabForward";

    /// <summary>"Move to group": a dock submenu that lists the other groups.</summary>
    public const string DockMoveToGroup = "Dock.MoveToGroup";

    /// <summary>"Group {0}: {1}": names a group in that submenu; {0} is its number and {1} the title of its showing
    /// panel.</summary>
    public const string DockGroupItem = "Dock.GroupItem";

    /// <summary>"New group beside this one": a dock submenu that gives the panel a new group on one side of its
    /// own.</summary>
    public const string DockNewGroup = "Dock.NewGroup";

    /// <summary>"New group at the dock's edge": a dock submenu that gives the panel a new group along an edge of the
    /// dock.</summary>
    public const string DockNewGroupAtEdge = "Dock.NewGroupAtEdge";

    /// <summary>"At the start": the side where text starts, in a dock's new-group submenus.</summary>
    public const string DockAtStart = "Dock.AtStart";

    /// <summary>"At the end": the side where text ends, in a dock's new-group submenus.</summary>
    public const string DockAtEnd = "Dock.AtEnd";

    /// <summary>"Above": above the panel's own group, in a dock's menu.</summary>
    public const string DockAbove = "Dock.Above";

    /// <summary>"Below": below the panel's own group, in a dock's menu.</summary>
    public const string DockBelow = "Dock.Below";

    /// <summary>"At the top": the dock's top edge, in a dock's menu.</summary>
    public const string DockAtTop = "Dock.AtTop";

    /// <summary>"At the bottom": the dock's bottom edge, in a dock's menu.</summary>
    public const string DockAtBottom = "Dock.AtBottom";

    /// <summary>"Close": a dock menu item that closes the panel.</summary>
    public const string DockClose = "Dock.Close";

    /// <summary>"{0} moved to panel group {1}.": said after a dock's menu moves a panel; {0} is its title and {1} the
    /// number of its group.</summary>
    public const string DockMoved = "Dock.Moved";

    /// <summary>"Size of {0}": names the menu button on a dock's separator, where {0} is the title of the panel
    /// showing before it, and the one on a slid-out panel's inner edge, where it is that panel's title.</summary>
    public const string DockSizeMenu = "Dock.SizeMenu";

    /// <summary>"Make {0} larger": a dock separator's menu item that grows the part before it by 10%, and the item
    /// on a slid-out panel's inner edge that grows the panel by 10% of the dock.</summary>
    public const string DockLarger = "Dock.Larger";

    /// <summary>"Make {0} smaller": a dock separator's menu item that shrinks the part before it by 10%, and the
    /// item on a slid-out panel's inner edge that shrinks the panel by 10% of the dock.</summary>
    public const string DockSmaller = "Dock.Smaller";

    /// <summary>"Maximize": a dock menu item that has the panel's group fill the dock.</summary>
    public const string DockMaximize = "Dock.Maximize";

    /// <summary>"Restore": a dock menu item, and the name of the button, that show every group again.</summary>
    public const string DockRestore = "Dock.Restore";

    /// <summary>"Panel group {0} fills the dock.": said when a dock's group is maximized; {0} is its number.</summary>
    public const string DockMaximized = "Dock.Maximized";

    /// <summary>"All panel groups show again.": said when a dock's maximized group is restored.</summary>
    public const string DockRestored = "Dock.Restored";

    /// <summary>"Floating panel group {0}": names the tab list of a dock's floating window; {0} is its number.
    /// </summary>
    public const string DockFloatingGroup = "Dock.FloatingGroup";

    /// <summary>"Float": a dock menu item that takes a panel out of its group, or out of a window it shares, into a
    /// floating window of its own.</summary>
    public const string DockFloat = "Dock.Float";

    /// <summary>"Dock": a dock menu item that docks a floating panel.</summary>
    public const string DockDock = "Dock.Dock";

    /// <summary>"{0} floats in a window.": said when a dock's panel ends up in a floating window; {0} is its title.
    /// </summary>
    public const string DockFloated = "Dock.Floated";

    /// <summary>"Open in new window": a dock menu item that opens a panel in a browser window of its own.</summary>
    public const string DockPopOut = "Dock.PopOut";

    /// <summary>"Return to the dock": the button in a dock panel's own browser window that brings the panel back.
    /// </summary>
    public const string DockReturn = "Dock.Return";

    /// <summary>"{0} opened in a new window.": said when a dock's panel opens in a browser window of its own; {0} is
    /// its title.</summary>
    public const string DockPoppedOut = "Dock.PoppedOut";

    /// <summary>"{0} is back in the dock.": said when a dock's panel comes back from its own browser window; {0} is
    /// its title.</summary>
    public const string DockReturned = "Dock.Returned";

    /// <summary>"The browser blocked the new window for {0}.": said when the browser refuses to open a window for a
    /// dock's panel; {0} is its title.</summary>
    public const string DockPopOutBlocked = "Dock.PopOutBlocked";

    /// <summary>"The new window for {0} closed before the panel was ready. Try again.": said when a dock panel's
    /// window closed because the dock didn't ask for it within ten seconds; {0} is the panel's title.</summary>
    public const string DockPopOutLapsed = "Dock.PopOutLapsed";

    /// <summary>"The page lost its connection to the server. This panel works again when the page reconnects.":
    /// shown in a dock panel's own browser window, over the panel, while the connection is lost.</summary>
    public const string DockConnectionLost = "Dock.ConnectionLost";

    /// <summary>"{0} isn't open right now. This window shows it again when it is.": shown in a dock panel's own
    /// browser window that stays open after the app took the panel away; {0} is the panel's title.</summary>
    public const string DockEmptyWindow = "Dock.EmptyWindow";

    /// <summary>"Panels in their own windows": names the row of buttons a dock keeps for the panels that show in
    /// browser windows of their own.</summary>
    public const string DockAway = "Dock.Away";

    /// <summary>"In its own browser window": describes the button a dock keeps for a panel that shows in a browser
    /// window of its own.</summary>
    public const string DockAwayHint = "Dock.AwayHint";

    /// <summary>"Show its window": a dock menu item that brings a panel's own browser window to the front.
    /// </summary>
    public const string DockShowWindow = "Dock.ShowWindow";

    /// <summary>"Auto-hide": a dock submenu that puts a panel away on a strip along an edge of the dock.</summary>
    public const string DockAutoHide = "Dock.AutoHide";

    /// <summary>"Auto-hidden panels ({0})": names a strip of a dock's auto-hidden panels; {0} is the words of its
    /// edge, such as "At the start".</summary>
    public const string DockStrip = "Dock.Strip";

    /// <summary>"Auto-hidden panel": names the tab list of a dock's auto-hidden panel that is slid out.</summary>
    public const string DockRevealedGroup = "Dock.RevealedGroup";

    /// <summary>"{0} is auto-hidden.": said when a dock's panel is put away on a strip; {0} is its title.</summary>
    public const string DockAutoHidden = "Dock.AutoHidden";

    /// <summary>"Move the {0} window": names the button that moves a dock's floating window; {0} is the title of
    /// its showing panel.</summary>
    public const string DockMoveWindow = "Dock.MoveWindow";

    /// <summary>"Arrow keys move the window.": describes that button to screen readers.</summary>
    public const string DockMoveWindowHint = "Dock.MoveWindowHint";

    /// <summary>"Resize the {0} window": names the grip that resizes a dock's floating window; {0} is the title
    /// of its showing panel.</summary>
    public const string DockResizeWindow = "Dock.ResizeWindow";

    /// <summary>"Arrow keys resize the window.": describes that grip to screen readers.</summary>
    public const string DockResizeWindowHint = "Dock.ResizeWindowHint";

    /// <summary>"The window is {0}% across and {1}% down.": said when a dock's floating window has moved; {0} and
    /// {1} are how far across and down the dock it starts, in percent.</summary>
    public const string DockMovedWindow = "Dock.MovedWindow";

    /// <summary>"The window is {0}% wide and {1}% tall.": said when a dock's floating window has been resized; {0}
    /// and {1} are its width and height in percent of the dock.</summary>
    public const string DockResizedWindow = "Dock.ResizedWindow";

    /// <summary>"Left": a dock menu item that moves a floating window toward the left of the screen.</summary>
    public const string DockLeft = "Dock.Left";

    /// <summary>"Right": a dock menu item that moves a floating window toward the right of the screen.</summary>
    public const string DockRight = "Dock.Right";

    /// <summary>"Up": a dock menu item that moves a floating window up.</summary>
    public const string DockUp = "Dock.Up";

    /// <summary>"Down": a dock menu item that moves a floating window down.</summary>
    public const string DockDown = "Dock.Down";

    /// <summary>"Wider": a dock menu item that makes a floating window wider.</summary>
    public const string DockWider = "Dock.Wider";

    /// <summary>"Narrower": a dock menu item that makes a floating window narrower.</summary>
    public const string DockNarrower = "Dock.Narrower";

    /// <summary>"Taller": a dock menu item that makes a floating window taller.</summary>
    public const string DockTaller = "Dock.Taller";

    /// <summary>"Shorter": a dock menu item that makes a floating window shorter.</summary>
    public const string DockShorter = "Dock.Shorter";

    /// <summary>"Resize {0}": names a splitter's separator; {0} is the pane before it.</summary>
    public const string SplitterResize = "Splitter.Resize";

    /// <summary>"pane {0}": stands in for a pane's label in a separator's name and its grip menu when the pane has
    /// none; {0} is its number.</summary>
    public const string SplitterPane = "Splitter.Pane";

    /// <summary>"Size of {0}": names the menu on a splitter's grip; {0} is the pane before it.</summary>
    public const string SplitterMenu = "Splitter.Menu";

    /// <summary>"Make {0} larger": a grip menu item that grows the pane before the grip by 10%.</summary>
    public const string SplitterLarger = "Splitter.Larger";

    /// <summary>"Make {0} smaller": a grip menu item that shrinks the pane before the grip by 10%.</summary>
    public const string SplitterSmaller = "Splitter.Smaller";

    /// <summary>"Collapse {0}": a grip menu item that collapses a collapsible pane.</summary>
    public const string SplitterCollapse = "Splitter.Collapse";

    /// <summary>"Restore {0}": a grip menu item that restores a collapsed pane.</summary>
    public const string SplitterRestore = "Splitter.Restore";

    /// <summary>"Completed": tells screen readers a step of a stepper is done.</summary>
    public const string StepperCompleted = "Stepper.Completed";

    /// <summary>"Current step: {0}": announced as a stepper moves to another step; {0} is the step's title.</summary>
    public const string StepperCurrent = "Stepper.Current";

    /// <summary>"Progress": names a stepper's list of steps.</summary>
    public const string StepperLabel = "Stepper.Label";

    /// <summary>"Optional": shown under the title of a step that can be skipped.</summary>
    public const string StepperOptional = "Stepper.Optional";

    /// <summary>"OK": the confirm button of a <see cref="TbPopconfirm"/>.</summary>
    public const string PopconfirmConfirm = "Popconfirm.Confirm";

    /// <summary>"Cancel": the cancel button of a <see cref="TbPopconfirm"/>.</summary>
    public const string PopconfirmCancel = "Popconfirm.Cancel";

    /// <summary>"Ribbon commands": names the button that collapses a <see cref="TbRibbon"/> to its tabs and shows
    /// its commands again.</summary>
    public const string RibbonCollapse = "Ribbon.Collapse";

    /// <summary>"{0}: {1} tab available": announces a ribbon tab that appeared for a context; {0} is the context,
    /// such as "Table tools", and {1} the tab's title.</summary>
    public const string RibbonContextTab = "Ribbon.ContextTab";

    /// <summary>"{0}: {1} tabs available": announces several ribbon tabs that appeared for one context; {0} is the
    /// context and {1} the tabs' titles, separated by commas.</summary>
    public const string RibbonContextTabs = "Ribbon.ContextTabs";

    /// <summary>"Key tip: {0}": describes a ribbon control that a key tip reaches; {0} is the keys to press in
    /// order, separated by commas, such as "Alt, H, B".</summary>
    public const string RibbonKeyTip = "Ribbon.KeyTip";

    /// <summary>"On this page": names a table of contents' navigation landmark.</summary>
    public const string TableOfContentsNavigation = "TableOfContents.Navigation";

    /// <summary>"Add tab": names the add button that follows a tab list.</summary>
    public const string TabsAdd = "Tabs.Add";

    /// <summary>"Press Delete to close": describes a tab that can be closed, since nothing else says how.</summary>
    public const string TabsCloseHint = "Tabs.CloseHint";

    /// <summary>"Scroll to earlier tabs": names the button that scrolls an overflowing tab row toward its start.
    /// </summary>
    public const string TabsScrollStart = "Tabs.ScrollStart";

    /// <summary>"Scroll to later tabs": names the button that scrolls an overflowing tab row toward its end.</summary>
    public const string TabsScrollEnd = "Tabs.ScrollEnd";

    /// <summary>"Enter adds a tag. Left Arrow moves to the tags.": describes a tag input's text box to screen
    /// readers. Right-to-left languages should say Right Arrow.</summary>
    public const string TagInputHint = "TagInput.Hint";

    /// <summary>"Remove {0}": names a tag's remove button; {0} is the tag.</summary>
    public const string TagInputRemove = "TagInput.Remove";

    /// <summary>"{0} added": announced when a tag is added; {0} is the tag.</summary>
    public const string TagInputAdded = "TagInput.Added";

    /// <summary>"{0} removed": announced when a tag is removed; {0} is the tag.</summary>
    public const string TagInputRemoved = "TagInput.Removed";

    /// <summary>"{0} is already added.": shown when the typed tag is already there; {0} is the tag.</summary>
    public const string TagInputDuplicate = "TagInput.Duplicate";

    /// <summary>"You can add up to {0} tags.": shown when a tag is typed once the limit is reached; {0} is the limit.
    /// Asked for through <see cref="ITbLocalizer.Plural"/>, with <see cref="TagInputTooManyOne"/> as its one-form.
    /// </summary>
    public const string TagInputTooMany = "TagInput.TooMany";

    /// <summary>"You can add only one tag.": shown when a tag is typed once a limit of one is reached; {0}, if used, is
    /// the limit.</summary>
    public const string TagInputTooManyOne = "TagInput.TooManyOne";

    /// <summary>"Clear": names a text field's clear button.</summary>
    public const string TextFieldClear = "TextField.Clear";

    /// <summary>"{0} can be at most {1} characters.": the error for a text field's value that is longer than its
    /// MaxLength; {0} is the field's label and {1} the limit. Asked for through <see cref="ITbLocalizer.Plural"/>, with
    /// <see cref="TextFieldTooLongOne"/> as its one-form.</summary>
    public const string TextFieldTooLong = "TextField.TooLong";

    /// <summary>"{0} can be at most {1} character.": the same error for a limit of one; {0} is the field's label and
    /// {1} the limit.</summary>
    public const string TextFieldTooLongOne = "TextField.TooLongOne";

    /// <summary>"{0} characters": what screen readers hear for a text field's counter without a limit; {0} is how many
    /// characters the field holds. Asked for through <see cref="ITbLocalizer.Plural"/>, with
    /// <see cref="TextFieldCountOne"/> as its one-form.</summary>
    public const string TextFieldCount = "TextField.Count";

    /// <summary>"{0} character": the same for one character; {0} is the count.</summary>
    public const string TextFieldCountOne = "TextField.CountOne";

    /// <summary>"{0} of {1} characters": what screen readers hear for a text field's counter with a limit; {0} is how
    /// many characters the field holds and {1} the limit. Asked for through <see cref="ITbLocalizer.Plural"/> with the
    /// limit, with <see cref="TextFieldCountOfLimitOne"/> as its one-form.</summary>
    public const string TextFieldCountOfLimit = "TextField.CountOfLimit";

    /// <summary>"{0} of {1} character": the same for a limit of one.</summary>
    public const string TextFieldCountOfLimitOne = "TextField.CountOfLimitOne";

    /// <summary>"{0} characters left.": announced as a text field with a counter and a limit nears it; {0} is how many
    /// characters are left. Asked for through <see cref="ITbLocalizer.Plural"/>, with
    /// <see cref="TextFieldCharactersLeftOne"/> as its one-form.</summary>
    public const string TextFieldCharactersLeft = "TextField.CharactersLeft";

    /// <summary>"{0} character left.": the same for one character; {0} is the count.</summary>
    public const string TextFieldCharactersLeftOne = "TextField.CharactersLeftOne";

    /// <summary>"Show password": names a password field's button that shows and hides the password; the button is
    /// pressed while the password shows.</summary>
    public const string TextFieldShowPassword = "TextField.ShowPassword";

    /// <summary>"{0} isn't complete.": shown when a masked text field has empty slots; {0} is the field's label.
    /// </summary>
    public const string TextFieldMaskIncomplete = "TextField.MaskIncomplete";

    /// <summary>"Formatting": names a rich text editor's toolbar.</summary>
    public const string RichTextEditorToolbar = "RichTextEditor.Toolbar";

    /// <summary>"Bold": names a rich text editor's bold button.</summary>
    public const string RichTextEditorBold = "RichTextEditor.Bold";

    /// <summary>"Italic": names a rich text editor's italic button.</summary>
    public const string RichTextEditorItalic = "RichTextEditor.Italic";

    /// <summary>"Underline": names a rich text editor's underline button.</summary>
    public const string RichTextEditorUnderline = "RichTextEditor.Underline";

    /// <summary>"Strikethrough": names a rich text editor's strikethrough button.</summary>
    public const string RichTextEditorStrikethrough = "RichTextEditor.Strikethrough";

    /// <summary>"Subscript": names a rich text editor's subscript button.</summary>
    public const string RichTextEditorSubscript = "RichTextEditor.Subscript";

    /// <summary>"Superscript": names a rich text editor's superscript button.</summary>
    public const string RichTextEditorSuperscript = "RichTextEditor.Superscript";

    /// <summary>"Code": names a rich text editor's inline code button.</summary>
    public const string RichTextEditorCode = "RichTextEditor.Code";

    /// <summary>"Text color": names a rich text editor's text color menu.</summary>
    public const string RichTextEditorTextColor = "RichTextEditor.TextColor";

    /// <summary>"Default": the text color menu's item that takes the color off.</summary>
    public const string RichTextEditorDefaultColor = "RichTextEditor.DefaultColor";

    /// <summary>"Primary": names the primary tone's color in a rich text editor's text color menu.</summary>
    public const string RichTextEditorColorPrimary = "RichTextEditor.ColorPrimary";

    /// <summary>"Secondary": names the secondary tone's color in a rich text editor's text color menu.</summary>
    public const string RichTextEditorColorSecondary = "RichTextEditor.ColorSecondary";

    /// <summary>"Gray": names the neutral tone's color in a rich text editor's text color menu.</summary>
    public const string RichTextEditorColorNeutral = "RichTextEditor.ColorNeutral";

    /// <summary>"Green": names the success tone's color in a rich text editor's text color menu.</summary>
    public const string RichTextEditorColorSuccess = "RichTextEditor.ColorSuccess";

    /// <summary>"Amber": names the warning tone's color in a rich text editor's text color menu.</summary>
    public const string RichTextEditorColorWarning = "RichTextEditor.ColorWarning";

    /// <summary>"Red": names the danger tone's color in a rich text editor's text color menu.</summary>
    public const string RichTextEditorColorDanger = "RichTextEditor.ColorDanger";

    /// <summary>"Blue": names the info tone's color in a rich text editor's text color menu.</summary>
    public const string RichTextEditorColorInfo = "RichTextEditor.ColorInfo";

    /// <summary>"Highlight": names a rich text editor's highlight button.</summary>
    public const string RichTextEditorHighlight = "RichTextEditor.Highlight";

    /// <summary>"Paragraph": names a rich text editor's slash command for a plain paragraph.</summary>
    public const string RichTextEditorParagraph = "RichTextEditor.Paragraph";

    /// <summary>"Heading 1": names a rich text editor's top-level heading button.</summary>
    public const string RichTextEditorHeading1 = "RichTextEditor.Heading1";

    /// <summary>"Heading 2": names a rich text editor's second-level heading button.</summary>
    public const string RichTextEditorHeading2 = "RichTextEditor.Heading2";

    /// <summary>"Heading 3": names a rich text editor's third-level heading button.</summary>
    public const string RichTextEditorHeading3 = "RichTextEditor.Heading3";

    /// <summary>"Quote": names a rich text editor's block quote button.</summary>
    public const string RichTextEditorQuote = "RichTextEditor.Quote";

    /// <summary>"Bulleted list": names a rich text editor's bulleted list button.</summary>
    public const string RichTextEditorBulletedList = "RichTextEditor.BulletedList";

    /// <summary>"Numbered list": names a rich text editor's numbered list button.</summary>
    public const string RichTextEditorNumberedList = "RichTextEditor.NumberedList";

    /// <summary>"Outdent": names a rich text editor's outdent button.</summary>
    public const string RichTextEditorOutdent = "RichTextEditor.Outdent";

    /// <summary>"Indent": names a rich text editor's indent button.</summary>
    public const string RichTextEditorIndent = "RichTextEditor.Indent";

    /// <summary>"Horizontal line": names a rich text editor's button that inserts a rule.</summary>
    public const string RichTextEditorHorizontalRule = "RichTextEditor.HorizontalRule";

    /// <summary>"Clear formatting": names a rich text editor's button that takes the formats off the selection.
    /// </summary>
    public const string RichTextEditorClearFormatting = "RichTextEditor.ClearFormatting";

    /// <summary>"HTML source": names a rich text editor's button that shows its value's HTML to edit.</summary>
    public const string RichTextEditorSource = "RichTextEditor.Source";

    /// <summary>"Find and replace": names a rich text editor's find and replace button and its bar.</summary>
    public const string RichTextEditorFindAndReplace = "RichTextEditor.FindAndReplace";

    /// <summary>"Find": names the field for the text to find in a rich text editor.</summary>
    public const string RichTextEditorFind = "RichTextEditor.Find";

    /// <summary>"{0} of {1}": says which match a rich text editor's find bar is on, of how many.</summary>
    public const string RichTextEditorFindCount = "RichTextEditor.FindCount";

    /// <summary>"No results": says that the text to find isn't in a rich text editor.</summary>
    public const string RichTextEditorFindNoResults = "RichTextEditor.FindNoResults";

    /// <summary>"Previous match": names the button that goes to the previous match in a rich text editor.</summary>
    public const string RichTextEditorFindPrevious = "RichTextEditor.FindPrevious";

    /// <summary>"Next match": names the button that goes to the next match in a rich text editor.</summary>
    public const string RichTextEditorFindNext = "RichTextEditor.FindNext";

    /// <summary>"Match case": names the button that makes a rich text editor's search tell capital letters from
    /// small ones.</summary>
    public const string RichTextEditorMatchCase = "RichTextEditor.MatchCase";

    /// <summary>"Whole word": names the button that makes a rich text editor's search find whole words only.</summary>
    public const string RichTextEditorWholeWord = "RichTextEditor.WholeWord";

    /// <summary>"Close": names the button that closes a rich text editor's find bar.</summary>
    public const string RichTextEditorCloseFind = "RichTextEditor.CloseFind";

    /// <summary>"Replace with": names the field for the text that replaces a match in a rich text editor.</summary>
    public const string RichTextEditorReplaceWith = "RichTextEditor.ReplaceWith";

    /// <summary>"Replace": names the button that replaces the current match in a rich text editor.</summary>
    public const string RichTextEditorReplace = "RichTextEditor.Replace";

    /// <summary>"Replace all": names the button that replaces every match in a rich text editor.</summary>
    public const string RichTextEditorReplaceAll = "RichTextEditor.ReplaceAll";

    /// <summary>"Replaced {0}": says how many matches a rich text editor replaced.</summary>
    public const string RichTextEditorReplaced = "RichTextEditor.Replaced";

    /// <summary>"Selection formatting": names the toolbar that shows over selected text in a rich text
    /// editor.</summary>
    public const string RichTextEditorFloatingToolbar = "RichTextEditor.FloatingToolbar";

    /// <summary>"Link": names a rich text editor's link button.</summary>
    public const string RichTextEditorLink = "RichTextEditor.Link";

    /// <summary>"Add link": titles a rich text editor's link dialog for text without a link.</summary>
    public const string RichTextEditorAddLink = "RichTextEditor.AddLink";

    /// <summary>"Edit link": titles a rich text editor's link dialog for a link.</summary>
    public const string RichTextEditorEditLink = "RichTextEditor.EditLink";

    /// <summary>"Text": labels the link dialog's field for the text to insert.</summary>
    public const string RichTextEditorLinkText = "RichTextEditor.LinkText";

    /// <summary>"Link": labels the link dialog's field for the URL.</summary>
    public const string RichTextEditorLinkAddress = "RichTextEditor.LinkAddress";

    /// <summary>"This kind of link isn't allowed here. Allowed: {0}": shown for a URL whose scheme isn't allowed; {0}
    /// lists the allowed schemes, such as "https:, mailto:".</summary>
    public const string RichTextEditorLinkNotAllowed = "RichTextEditor.LinkNotAllowed";

    /// <summary>"Use a link within this site.": shown for a URL with a scheme where only relative links are allowed.
    /// </summary>
    public const string RichTextEditorRelativeLinkOnly = "RichTextEditor.RelativeLinkOnly";

    /// <summary>"Apply": the link dialog's button that sets the link.</summary>
    public const string RichTextEditorApplyLink = "RichTextEditor.ApplyLink";

    /// <summary>"Remove link": the link dialog's button that takes the link off the text.</summary>
    public const string RichTextEditorRemoveLink = "RichTextEditor.RemoveLink";

    /// <summary>"Open link": the link dialog's link to the URL, which opens in a new tab.</summary>
    public const string RichTextEditorOpenLink = "RichTextEditor.OpenLink";

    /// <summary>"Image": the rich text editor's button that inserts an image.</summary>
    public const string RichTextEditorImage = "RichTextEditor.Image";

    /// <summary>"Insert image": the title of the dialog that inserts an image.</summary>
    public const string RichTextEditorInsertImage = "RichTextEditor.InsertImage";

    /// <summary>"Image file": labels the image dialog's file picker.</summary>
    public const string RichTextEditorImageFile = "RichTextEditor.ImageFile";

    /// <summary>"Alt text": labels the image dialog's field for the image's alt text.</summary>
    public const string RichTextEditorImageAlt = "RichTextEditor.ImageAlt";

    /// <summary>"Describes the image for people who can't see it.": the help under the image dialog's alt text
    /// field.</summary>
    public const string RichTextEditorImageAltHelp = "RichTextEditor.ImageAltHelp";

    /// <summary>"Decorative": labels the image dialog's checkbox for an image with no alt text.</summary>
    public const string RichTextEditorImageDecorative = "RichTextEditor.ImageDecorative";

    /// <summary>"It adds nothing the text doesn't say, so screen readers skip it.": the help under the image dialog's
    /// Decorative checkbox.</summary>
    public const string RichTextEditorImageDecorativeHelp = "RichTextEditor.ImageDecorativeHelp";

    /// <summary>"Insert": the image dialog's button that stores the image and puts it in.</summary>
    public const string RichTextEditorApplyImage = "RichTextEditor.ApplyImage";

    /// <summary>"The image couldn't be stored. Try again.": shown in the image dialog when the app's upload
    /// fails.</summary>
    public const string RichTextEditorImageFailed = "RichTextEditor.ImageFailed";

    /// <summary>"The image's address isn't one this editor allows.": shown in the image dialog when the app's upload
    /// returns an address the editor's ImageSchemes don't allow.</summary>
    public const string RichTextEditorImageNotAllowed = "RichTextEditor.ImageNotAllowed";

    /// <summary>"Table": the button that puts a table in.</summary>
    public const string RichTextEditorTable = "RichTextEditor.Table";

    /// <summary>"Table options": the menu button with commands for the table the caret is in.</summary>
    public const string RichTextEditorTableMenu = "RichTextEditor.TableMenu";

    /// <summary>"Add row above": the table menu's command that adds a row above the caret's row.</summary>
    public const string RichTextEditorAddRowAbove = "RichTextEditor.AddRowAbove";

    /// <summary>"Add row below": the table menu's command that adds a row below the caret's row.</summary>
    public const string RichTextEditorAddRowBelow = "RichTextEditor.AddRowBelow";

    /// <summary>"Add column left": the table menu's command that adds a column left of the caret's column.</summary>
    public const string RichTextEditorAddColumnLeft = "RichTextEditor.AddColumnLeft";

    /// <summary>"Add column right": the table menu's command that adds a column right of the caret's column.</summary>
    public const string RichTextEditorAddColumnRight = "RichTextEditor.AddColumnRight";

    /// <summary>"Delete row": the table menu's command that deletes the caret's row.</summary>
    public const string RichTextEditorDeleteRow = "RichTextEditor.DeleteRow";

    /// <summary>"Delete column": the table menu's command that deletes the caret's column.</summary>
    public const string RichTextEditorDeleteColumn = "RichTextEditor.DeleteColumn";

    /// <summary>"Delete table": the table menu's command that deletes the table.</summary>
    public const string RichTextEditorDeleteTable = "RichTextEditor.DeleteTable";

    /// <summary>"Header row": the table menu's checkbox that makes the first row the table's header row.</summary>
    public const string RichTextEditorHeaderRow = "RichTextEditor.HeaderRow";

    /// <summary>"Next": a tour step's button to the next step.</summary>
    public const string TourNext = "Tour.Next";

    /// <summary>"Back": a tour step's button to the step before.</summary>
    public const string TourBack = "Tour.Back";

    /// <summary>"Done": the last tour step's button, which ends the tour.</summary>
    public const string TourDone = "Tour.Done";

    /// <summary>"Skip tour": a tour step's button that ends the tour early.</summary>
    public const string TourSkip = "Tour.Skip";

    /// <summary>"{0} of {1}": shows which tour step shows; {0} is its number and {1} the number of steps.</summary>
    public const string TourPosition = "Tour.Position";

    /// <summary>"({0})": the seconds left shown after a button's text while its <c>Cooldown</c> runs; {0} is how
    /// many.</summary>
    public const string ButtonCooldownLeft = "Button.CooldownLeft";

    /// <summary>"Available again in {0} seconds": describes a button whose <c>Cooldown</c> has started, and is read
    /// out once; {0} is how many. Asked for through <see cref="ITbLocalizer.Plural"/>, with
    /// <see cref="ButtonCooldownOne"/> as its one-form.</summary>
    public const string ButtonCooldown = "Button.Cooldown";

    /// <summary>"Available again in 1 second": describes a button whose one-second <c>Cooldown</c> has started; {0},
    /// if used, is the count.</summary>
    public const string ButtonCooldownOne = "Button.CooldownOne";

    /// <summary>"Dictate": the name of the button that fills a field from speech.</summary>
    public const string SpeechToTextLabel = "SpeechToText.Label";

    /// <summary>"Listening": said when the dictation button starts listening.</summary>
    public const string SpeechToTextListening = "SpeechToText.Listening";

    /// <summary>"Stopped listening": said when the dictation button stops listening.</summary>
    public const string SpeechToTextStopped = "SpeechToText.Stopped";

    /// <summary>"Smart paste": the button that fills a form from text on the clipboard.</summary>
    public const string SmartPasteLabel = "SmartPaste.Label";

    /// <summary>"Paste text to fill the form": the title of the box that asks for the text when the clipboard can't be
    /// read.</summary>
    public const string SmartPasteTitle = "SmartPaste.Title";

    /// <summary>"Text to fill from": the label of that box's text area.</summary>
    public const string SmartPasteText = "SmartPaste.Text";

    /// <summary>"Fill": that box's button that fills the form.</summary>
    public const string SmartPasteFill = "SmartPaste.Fill";

    /// <summary>"Cancel": that box's button that closes it without filling.</summary>
    public const string SmartPasteCancel = "SmartPaste.Cancel";

    /// <summary>"Filling the form": shown and said while the model reads the text.</summary>
    public const string SmartPasteFilling = "SmartPaste.Filling";

    /// <summary>"Filled {0} fields": shown and said once fields are filled; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>, with <see cref="SmartPasteFilledOne"/> as its one-form.</summary>
    public const string SmartPasteFilled = "SmartPaste.Filled";

    /// <summary>"Filled 1 field": shown and said once one field is filled; {0}, if used, is the count.</summary>
    public const string SmartPasteFilledOne = "SmartPaste.FilledOne";

    /// <summary>"Found nothing to fill": shown and said when the text held no value for any field.</summary>
    public const string SmartPasteNothing = "SmartPaste.Nothing";

    /// <summary>"Couldn't fill the form": shown and said when the model failed.</summary>
    public const string SmartPasteFailed = "SmartPaste.Failed";

    /// <summary>"Loading {0}": shown and said while an async content region's first load is slow; {0} is its
    /// <see cref="TbAsyncContent.Label"/>, such as "groups".</summary>
    public const string AsyncContentLoading = "AsyncContent.Loading";

    /// <summary>"Unable to load {0}.": shown and said when an async content region's load fails; {0} is its
    /// <see cref="TbAsyncContent.Label"/>.</summary>
    public const string AsyncContentLoadFailed = "AsyncContent.LoadFailed";

    /// <summary>"Retry": the button that loads an async content region again after a failure.</summary>
    public const string AsyncContentRetry = "AsyncContent.Retry";

    /// <summary>"Back to top": the name of the button that takes a scrolled page back to its top.</summary>
    public const string BackToTopLabel = "BackToTop.Label";

    /// <summary>"Edit": the start of the name of an in-place editor's button that shows its value and starts editing
    /// it, before the label and the value.</summary>
    public const string InPlaceEditorEdit = "InPlaceEditor.Edit";

    /// <summary>"Save": an in-place editor's button that saves the edit.</summary>
    public const string InPlaceEditorSave = "InPlaceEditor.Save";

    /// <summary>"Cancel": an in-place editor's button that throws the edit away.</summary>
    public const string InPlaceEditorCancel = "InPlaceEditor.Cancel";

    /// <summary>"Preview": names a markdown editor's Preview button and the preview it shows.</summary>
    public const string MarkdownEditorPreview = "MarkdownEditor.Preview";

    /// <summary>"Theme": names a theme picker.</summary>
    public const string ThemePicker = "Theme.Picker";

    /// <summary>"Light": names the theme picker's light choice.</summary>
    public const string ThemeLight = "Theme.Light";

    /// <summary>"Dark": names the theme picker's dark choice.</summary>
    public const string ThemeDark = "Theme.Dark";

    /// <summary>"Match the device": names the theme picker's choice that follows the device's setting.</summary>
    public const string ThemeSystem = "Theme.System";

    /// <summary>"{0} available, press F8.": read out after a toast's message when it has an action; {0} is the
    /// action's text.</summary>
    public const string ToastActionAvailable = "Toast.ActionAvailable";

    /// <summary>"Dismiss": names a toast's close button.</summary>
    public const string ToastDismiss = "Toast.Dismiss";

    /// <summary>"Notifications": names the region the toasts appear in.</summary>
    public const string ToastRegion = "Toast.Region";

    /// <summary>"More": names a toolbar's overflow menu.</summary>
    public const string ToolbarMore = "Toolbar.More";

    /// <summary>"Loading…": shown while a data grid loads its rows.</summary>
    public const string DataGridLoading = "DataGrid.Loading";

    /// <summary>"Couldn't load the rows.": shown when a data grid's rows fail to load.</summary>
    public const string DataGridLoadFailed = "DataGrid.LoadFailed";

    /// <summary>"Couldn't export the rows.": read out when a data grid's export fails.</summary>
    public const string DataGridExportFailed = "DataGrid.ExportFailed";

    /// <summary>"Retry": names the button that loads a data grid's rows again after a failure.</summary>
    public const string DataGridRetry = "DataGrid.Retry";

    /// <summary>"Nothing to show.": shown by a data grid with no rows.</summary>
    public const string DataGridEmpty = "DataGrid.Empty";

    /// <summary>"Select all rows": names the header checkbox that selects every row on a data grid's page.</summary>
    public const string DataGridSelectAll = "DataGrid.SelectAll";

    /// <summary>"Select": names the column of a data grid's selection buttons.</summary>
    public const string DataGridSelect = "DataGrid.Select";

    /// <summary>"Select {0}": names a row's selection box; {0} is the row's label.</summary>
    public const string DataGridSelectRow = "DataGrid.SelectRow";

    /// <summary>"Details": names the column of a data grid that holds each row's details button.</summary>
    public const string DataGridDetails = "DataGrid.Details";

    /// <summary>"Details for {0}": names a row's button that opens and closes its details; {0} is the row's label.
    /// </summary>
    public const string DataGridDetailsFor = "DataGrid.DetailsFor";

    /// <summary>"Changed": tells screen readers a batch grid's cell holds a change not yet saved.</summary>
    public const string DataGridChanged = "DataGrid.Changed";

    /// <summary>"Unsaved changes: {0}": says how many changes a batch grid holds; {0} is the count.</summary>
    public const string DataGridUnsavedChanges = "DataGrid.UnsavedChanges";

    /// <summary>"Save changes": the button that saves a batch grid's changes.</summary>
    public const string DataGridSaveChanges = "DataGrid.SaveChanges";

    /// <summary>"Discard changes": the button that drops a batch grid's changes.</summary>
    public const string DataGridDiscardChanges = "DataGrid.DiscardChanges";

    /// <summary>"Changes saved": read out once a batch grid's changes are saved.</summary>
    public const string DataGridChangesSaved = "DataGrid.ChangesSaved";

    /// <summary>"Rows under {0}": names a tree grid row's button that shows and hides its child rows; {0} is the row's
    /// label.</summary>
    public const string DataGridRowsUnder = "DataGrid.RowsUnder";

    /// <summary>"Export": names the data grid's menu that downloads its rows as a file.</summary>
    public const string DataGridExport = "DataGrid.Export";

    /// <summary>"CSV": the Export menu's item that downloads the rows as a CSV file.</summary>
    public const string DataGridExportCsv = "DataGrid.ExportCsv";

    /// <summary>"Excel": the Export menu's item that downloads the rows as an Excel workbook.</summary>
    public const string DataGridExportExcel = "DataGrid.ExportExcel";

    /// <summary>"PDF": the Export menu's item that downloads the rows as a PDF.</summary>
    public const string DataGridExportPdf = "DataGrid.ExportPdf";

    /// <summary>"Order": names the column of a data grid whose rows can be moved, which holds each row's move
    /// handle.</summary>
    public const string DataGridOrder = "DataGrid.Order";

    /// <summary>"{0}–{1} of {2}": the rows a data grid's page shows; {0} and {1} are the first and last, {2} the
    /// count of all rows.</summary>
    public const string DataGridRange = "DataGrid.Range";

    /// <summary>"Pages of {0}": names a data grid's pages; {0} is the grid's name.</summary>
    public const string DataGridPages = "DataGrid.Pages";

    /// <summary>"Rows per page": names a data grid's list of page sizes.</summary>
    public const string DataGridPageSize = "DataGrid.PageSize";

    /// <summary>"Filter {0}": names a data grid's filter for a column; {0} is the column.</summary>
    public const string DataGridFilter = "DataGrid.Filter";

    /// <summary>"Filter {0}, on": names a data grid's filter menu button while its column is filtered; {0} is the
    /// column.</summary>
    public const string DataGridFilterOn = "DataGrid.FilterOn";

    /// <summary>"Any": shown in a data grid's choice filter when it filters nothing.</summary>
    public const string DataGridFilterAny = "DataGrid.FilterAny";

    /// <summary>"Yes": how a data grid writes true, in a yes-or-no column's cells and its filter.</summary>
    public const string DataGridFilterYes = "DataGrid.FilterYes";

    /// <summary>"No": how a data grid writes false, in a yes-or-no column's cells and its filter.</summary>
    public const string DataGridFilterNo = "DataGrid.FilterNo";

    /// <summary>"Apply": the button that applies a data grid's filter menu.</summary>
    public const string DataGridFilterApply = "DataGrid.FilterApply";

    /// <summary>"Clear": the button that clears a data grid column's filter.</summary>
    public const string DataGridFilterClear = "DataGrid.FilterClear";

    /// <summary>"Condition": names the list of conditions in a data grid's filter menu.</summary>
    public const string DataGridFilterCondition = "DataGrid.FilterCondition";

    /// <summary>"Value": names the value field in a data grid's filter menu.</summary>
    public const string DataGridFilterValue = "DataGrid.FilterValue";

    /// <summary>"Contains": the condition that the column's text contains the filter's value.</summary>
    public const string DataGridFilterContains = "DataGrid.FilterContains";

    /// <summary>"Doesn't contain": the condition that the column's text doesn't contain the filter's value.</summary>
    public const string DataGridFilterDoesNotContain = "DataGrid.FilterDoesNotContain";

    /// <summary>"Starts with": the condition that the column's text starts with the filter's value.</summary>
    public const string DataGridFilterStartsWith = "DataGrid.FilterStartsWith";

    /// <summary>"Ends with": the condition that the column's text ends with the filter's value.</summary>
    public const string DataGridFilterEndsWith = "DataGrid.FilterEndsWith";

    /// <summary>"Is": the condition that the column's value equals the filter's value.</summary>
    public const string DataGridFilterEqualTo = "DataGrid.FilterEqualTo";

    /// <summary>"Isn't": the condition that the column's value differs from the filter's value.</summary>
    public const string DataGridFilterNotEqualTo = "DataGrid.FilterNotEqualTo";

    /// <summary>"Less than": the condition that the column's value is less than the filter's value.</summary>
    public const string DataGridFilterLessThan = "DataGrid.FilterLessThan";

    /// <summary>"At most": the condition that the column's value is at most the filter's value.</summary>
    public const string DataGridFilterAtMost = "DataGrid.FilterAtMost";

    /// <summary>"More than": the condition that the column's value is more than the filter's value.</summary>
    public const string DataGridFilterGreaterThan = "DataGrid.FilterGreaterThan";

    /// <summary>"At least": the condition that the column's value is at least the filter's value.</summary>
    public const string DataGridFilterAtLeast = "DataGrid.FilterAtLeast";

    /// <summary>"Is empty": the condition that the column has no value.</summary>
    public const string DataGridFilterIsEmpty = "DataGrid.FilterIsEmpty";

    /// <summary>"Isn't empty": the condition that the column has a value.</summary>
    public const string DataGridFilterIsNotEmpty = "DataGrid.FilterIsNotEmpty";

    /// <summary>"Rows shown: {0}.": read out when a data grid's filters change; {0} is how many rows meet them.
    /// </summary>
    public const string DataGridRowsShown = "DataGrid.RowsShown";

    /// <summary>"{0} can't hold that value.": shown when an edited data grid cell's value doesn't fit its field, such
    /// as a number too large; {0} is the column.</summary>
    public const string DataGridEditInvalid = "DataGrid.EditInvalid";

    /// <summary>"Save": the button that saves a data grid row's edit.</summary>
    public const string DataGridSave = "DataGrid.Save";

    /// <summary>"Save {0}": names the button that saves a data grid row's edit; {0} is the row's label.</summary>
    public const string DataGridSaveRow = "DataGrid.SaveRow";

    /// <summary>"Cancel": the button that cancels a data grid row's edit.</summary>
    public const string DataGridCancel = "DataGrid.Cancel";

    /// <summary>"Cancel editing {0}": names the button that cancels a data grid row's edit; {0} is the row's label.
    /// </summary>
    public const string DataGridCancelRow = "DataGrid.CancelRow";

    /// <summary>"Group by": labels the list of columns a data grid's rows can be grouped by.</summary>
    public const string DataGridGroupBy = "DataGrid.GroupBy";

    /// <summary>"Options for {0}": names a data grid column's menu; {0} is the column.</summary>
    public const string DataGridColumnMenu = "DataGrid.ColumnMenu";

    /// <summary>"Move {0} earlier": a column menu item that moves the column one place toward the start of the row;
    /// {0} is the column.</summary>
    public const string DataGridEarlier = "DataGrid.Earlier";

    /// <summary>"Move {0} later": a column menu item that moves the column one place toward the end of the row; {0} is
    /// the column.</summary>
    public const string DataGridLater = "DataGrid.Later";

    /// <summary>"{0} moved to column {1} of {2}": read out after a column moves from its menu; {0} is the column, {1}
    /// its new place, counting from 1, and {2} how many columns there are.</summary>
    public const string DataGridColumnMoved = "DataGrid.ColumnMoved";

    /// <summary>"Columns": names the menu above a data grid that shows and hides its columns.</summary>
    public const string DataGridColumns = "DataGrid.Columns";

    /// <summary>"Hide {0}": a column menu item that hides the column; {0} is the column.</summary>
    public const string DataGridHide = "DataGrid.Hide";

    /// <summary>"{0} hidden": read out after a column is hidden from its own menu; {0} is the column.</summary>
    public const string DataGridColumnHidden = "DataGrid.ColumnHidden";

    /// <summary>"Totals": names a data grid's totals row for screen readers.</summary>
    public const string DataGridTotals = "DataGrid.Totals";

    /// <summary>"Totals for {0}": names a group's totals row for screen readers; {0} is the group's heading.</summary>
    public const string DataGridGroupTotals = "DataGrid.GroupTotals";

    /// <summary>"Sum": names a column's total in a data grid's totals row when it adds up the values.</summary>
    public const string DataGridSum = "DataGrid.Sum";

    /// <summary>"Average": names a column's total in a data grid's totals row when it is the mean of the values.
    /// </summary>
    public const string DataGridAverage = "DataGrid.Average";

    /// <summary>"Count": names a column's total in a data grid's totals row when it counts the rows with a value.
    /// </summary>
    public const string DataGridCount = "DataGrid.Count";

    /// <summary>"Min": names a column's total in a data grid's totals row when it is the smallest value.</summary>
    public const string DataGridMin = "DataGrid.Min";

    /// <summary>"Max": names a column's total in a data grid's totals row when it is the largest value.</summary>
    public const string DataGridMax = "DataGrid.Max";

    /// <summary>"Make {0} wider": a column menu item that widens the column; {0} is the column.</summary>
    public const string DataGridWider = "DataGrid.Wider";

    /// <summary>"Make {0} narrower": a column menu item that narrows the column; {0} is the column.</summary>
    public const string DataGridNarrower = "DataGrid.Narrower";

    /// <summary>"Reset column widths": a column menu item that gives every column back the width it started with.
    /// </summary>
    public const string DataGridResetWidths = "DataGrid.ResetWidths";

    /// <summary>"{0}: {1}": a group's heading; {0} is the column and {1} the group's value.</summary>
    public const string DataGridGroupHeading = "DataGrid.GroupHeading";

    /// <summary>"Rows: {0}": how many rows a group holds; {0} is the count.</summary>
    public const string DataGridGroupCount = "DataGrid.GroupCount";

    /// <summary>"None": the value of a group whose rows have no value in its column.</summary>
    public const string DataGridGroupNone = "DataGrid.GroupNone";

    /// <summary>"Sorted by {0}, ascending.": read out when a column sorts its rows smallest first; {0} is the column.
    /// </summary>
    public const string DataGridSortedAscending = "DataGrid.SortedAscending";

    /// <summary>"Sorted by {0}, descending.": read out when a column sorts its rows largest first; {0} is the column.
    /// </summary>
    public const string DataGridSortedDescending = "DataGrid.SortedDescending";

    /// <summary>"No longer sorted by {0}.": read out when a column stops sorting the rows; {0} is the column.
    /// </summary>
    public const string DataGridUnsorted = "DataGrid.Unsorted";

    /// <summary>"sort {0} of {1}, {2}": tells which of several sorts a column is; {0} is its place, {1} the count and
    /// {2} its direction.</summary>
    public const string DataGridSortPosition = "DataGrid.SortPosition";

    /// <summary>"ascending": a sort from smallest to largest.</summary>
    public const string DataGridAscending = "DataGrid.Ascending";

    /// <summary>"descending": a sort from largest to smallest.</summary>
    public const string DataGridDescending = "DataGrid.Descending";

    /// <summary>"Loading…": shown while a tree row loads its children.</summary>
    public const string TreeViewLoading = "TreeView.Loading";

    /// <summary>"Couldn't load these items.": shown when a tree row's children fail to load.</summary>
    public const string TreeViewLoadFailed = "TreeView.LoadFailed";

    /// <summary>"Retry": names the button that loads a failed tree row's children again.</summary>
    public const string TreeViewRetry = "TreeView.Retry";

    /// <summary>"Nothing to show.": shown by a tree with no rows.</summary>
    public const string TreeViewEmpty = "TreeView.Empty";

    /// <summary>"Alt+Shift and the arrow keys move this item.": describes a tree row that can be moved.</summary>
    public const string TreeViewMoveHint = "TreeView.MoveHint";

    /// <summary>"{0} moved before {1}.": said after a tree move, with what moved and its new neighbor.</summary>
    public const string TreeViewMovedBefore = "TreeView.MovedBefore";

    /// <summary>"{0} moved after {1}.": said after a tree move, with what moved and its new neighbor.</summary>
    public const string TreeViewMovedAfter = "TreeView.MovedAfter";

    /// <summary>"{0} moved into {1}.": said after a tree move, with what moved and its new parent.</summary>
    public const string TreeViewMovedInside = "TreeView.MovedInside";

    /// <summary>"{0} items": names several tree rows moved together; {0} is how many. Asked for through
    /// <see cref="ITbLocalizer.Plural"/>; one row is named by its text instead.</summary>
    public const string TreeViewItemCount = "TreeView.ItemCount";

    /// <summary>"Move {0}": the name of the handle that drags or moves an item of a sortable list or a grid row, with
    /// the item's text.</summary>
    public const string SortableHandle = "Sortable.Handle";

    /// <summary>"Arrow keys move it, and Home and End move it to the start or end. Enter opens a menu of moves.":
    /// describes a move handle.</summary>
    public const string SortableHint = "Sortable.Hint";

    /// <summary>"Move up": a move handle's menu item that moves its item one place toward the start.</summary>
    public const string SortableMoveUp = "Sortable.MoveUp";

    /// <summary>"Move down": a move handle's menu item that moves its item one place toward the end.</summary>
    public const string SortableMoveDown = "Sortable.MoveDown";

    /// <summary>"Move to the start": a move handle's menu item that moves its item first.</summary>
    public const string SortableMoveToStart = "Sortable.MoveToStart";

    /// <summary>"Move to the end": a move handle's menu item that moves its item last.</summary>
    public const string SortableMoveToEnd = "Sortable.MoveToEnd";

    /// <summary>"{0} moved to position {1} of {2}.": said after an item of a sortable list or a grid row moves, with
    /// its text, new position and the number of items.</summary>
    public const string SortableMoved = "Sortable.Moved";

    /// <summary>"Up and Down move it, and Home and End move it to the start or end. Left and Right move it to the
    /// previous or next list. Enter opens a menu of moves.": describes the move handle of a sortable list that shares a
    /// group.</summary>
    public const string SortableGroupHint = "Sortable.GroupHint";

    /// <summary>"Move to {0}": a move handle's menu item that moves its item to the end of another list in its group,
    /// with that list's name.</summary>
    public const string SortableMoveToList = "Sortable.MoveToList";

    /// <summary>"{0} moved to {3}, position {1} of {2}.": said after an item moves to another sortable list, with its
    /// text, new position, the number of items in that list and the list's name.</summary>
    public const string SortableMovedToList = "Sortable.MovedToList";

    /// <summary>"An item can't move into itself.": why a tree move into an item's own branch is refused.</summary>
    public const string TreeViewCantMoveIntoItself = "TreeView.CantMoveIntoItself";

    /// <summary>"Filter": names a tree select's filter box and is its placeholder.</summary>
    public const string TreeSelectFilter = "TreeSelect.Filter";

    /// <summary>"Clear": names a tree select's clear button.</summary>
    public const string TreeSelectClear = "TreeSelect.Clear";

    /// <summary>"{0} more": ends a multi tree select's field when it lists only some of the checked items; {0} is how
    /// many it leaves out.</summary>
    public const string TreeSelectMore = "TreeSelect.More";

    /// <summary>"Zoom in": names a viewport's button that zooms in.</summary>
    public const string ViewportZoomIn = "Viewport.ZoomIn";

    /// <summary>"Zoom out": names a viewport's button that zooms out.</summary>
    public const string ViewportZoomOut = "Viewport.ZoomOut";

    /// <summary>"Zoom {0}": names a viewport's zoom menu, and is read out as the keys, the buttons or the menu zoom; {0} is the zoom,
    /// such as "125%".</summary>
    public const string ViewportZoom = "Viewport.Zoom";

    /// <summary>"Fit width": the zoom menu item that fits a viewport's content to its width.</summary>
    public const string ViewportFitWidth = "Viewport.FitWidth";

    /// <summary>"Fit page": the zoom menu item that fits a viewport's whole content in view.</summary>
    public const string ViewportFitPage = "Viewport.FitPage";

    /// <summary>"Actual size": the zoom menu item that shows a viewport's content at 100%.</summary>
    public const string ViewportActualSize = "Viewport.ActualSize";

    // Each Plural key's one-form, which the default Plural gives for a count of 1.
    private static readonly FrozenDictionary<string, string> OneForms = new Dictionary<string, string>
    {
        [CodeFieldIncomplete] = CodeFieldIncompleteOne,
        [TextFieldTooLong] = TextFieldTooLongOne,
        [TextFieldCount] = TextFieldCountOne,
        [TextFieldCountOfLimit] = TextFieldCountOfLimitOne,
        [TextFieldCharactersLeft] = TextFieldCharactersLeftOne,
        [CodeFieldDigits] = CodeFieldDigitsOne,
        [CodeFieldCharacters] = CodeFieldCharactersOne,
        [FileUploadChosen] = FileUploadChosenOne,
        [ButtonCooldown] = ButtonCooldownOne,
        [FileUploadTooMany] = FileUploadTooManyOne,
        [RatingStars] = RatingStar,
        [TagInputTooMany] = TagInputTooManyOne,
        [SmartPasteFilled] = SmartPasteFilledOne,
        [TimestampMinutesAgo] = TimestampMinutesAgoOne,
        [TimestampInMinutes] = TimestampInMinutesOne,
        [TimestampHoursAgo] = TimestampHoursAgoOne,
        [TimestampInHours] = TimestampInHoursOne,
        [TimestampDaysAgo] = TimestampDaysAgoOne,
        [TimestampInDays] = TimestampInDaysOne,
        [TimestampMonthsAgo] = TimestampMonthsAgoOne,
        [TimestampInMonths] = TimestampInMonthsOne,
        [TimestampYearsAgo] = TimestampYearsAgoOne,
        [TimestampInYears] = TimestampInYearsOne,
        [CountdownSeconds] = CountdownSecondsOne,
        [CountdownMinutes] = CountdownMinutesOne,
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, string> EnglishText = new Dictionary<string, string>
    {
        [AlertClose] = "Close",
        [BannerLabel] = "Notice",
        [BannerClose] = "Dismiss",
        [CarouselPrevious] = "Previous slide",
        [CarouselNext] = "Next slide",
        [CarouselSlide] = "Slide {0}",
        [CarouselSlideOf] = "{0} of {1}",
        [CarouselStop] = "Stop slide rotation",
        [CarouselStart] = "Start slide rotation",
        [ChatMessage] = "Message",
        [ChatSend] = "Send",
        [ChatAttach] = "Attach files",
        [ChatTyping] = "{0} is typing…",
        [ChatTypingMany] = "{0} and {1} are typing…",
        [ChatNewest] = "Newest messages",
        [AiPromptGenerate] = "Generate",
        [AiPromptStop] = "Stop",
        [AiPromptSuggestions] = "Suggestions",
        [AiPromptResponses] = "Responses",
        [AiPromptCopy] = "Copy",
        [AiPromptRetry] = "Retry",
        [AiPromptInsert] = "Insert",
        [AiPromptGenerating] = "Generating a response",
        [AiPromptDone] = "Response ready",
        [AiPromptStopped] = "Response stopped",
        [AiPromptFailed] = "Couldn't generate a response. Try again.",
        [AiPromptCopied] = "Copied",
        [KanbanCount] = "{0} of {1}",
        [KanbanFull] = "{0} is full: its limit is {1}.",
        [CascaderMore] = "{0}, more choices",
        [CascaderResults] = "Results: {0}",
        [CascaderNoResults] = "No matches",
        [CascaderClear] = "Clear",
        [MentionInputSuggestions] = "Suggestions: {0}",
        [MentionInputSearchFailed] = "Couldn't load suggestions.",
        [LightboxPrevious] = "Previous image",
        [LightboxNext] = "Next image",
        [LightboxZoomIn] = "Zoom in",
        [LightboxZoomOut] = "Zoom out",
        [LightboxFit] = "Fit to window",
        [LightboxRotateLeft] = "Rotate left",
        [LightboxRotateRight] = "Rotate right",
        [LightboxPosition] = "{0} of {1}",
        [LightboxShown] = "{0}, {1} of {2}",
        [LightboxZoom] = "{0}%",
        [AppShellMenu] = "Navigation",
        [AppShellNavigation] = "Main",
        [AppShellSkipToContent] = "Skip to content",
        [AutocompleteClear] = "Clear",
        [AutocompleteSearching] = "Searching…",
        [AutocompleteNoResults] = "No matches",
        [AutocompleteSearchFailed] = "Couldn't load suggestions.",
        [AutocompleteTypeToSearch] = "Type to search",
        [AutocompleteResults] = "Suggestions: {0}",
        [AvatarGroupMore] = "{0} more",
        [BreadcrumbsNavigation] = "Breadcrumb",
        [ChipRemove] = "Remove",
        [ChipRemoveNamed] = "Remove {0}",
        [CommandPaletteLabel] = "Command palette",
        [CommandPalettePlaceholder] = "Type a command or search",
        [CommandPaletteNoResults] = "No matching commands",
        [CommandPaletteResults] = "Results: {0}",
        [CommandPaletteShortcut] = "Search commands",
        [HotkeysHelpTitle] = "Keyboard shortcuts",
        [HotkeysHelpShow] = "Show keyboard shortcuts",
        [HotkeysHelpGeneral] = "General",
        [HotkeysHelpKeys] = "Keys",
        [HotkeysHelpAction] = "Action",
        [ColorPickerOpen] = "Choose a color",
        [ColorPickerEyedropper] = "Pick a color from the screen",
        [ColorPickerClear] = "Clear",
        [ColorPickerSwatches] = "Swatches",
        [ColorPickerHue] = "Hue",
        [ColorPickerSaturation] = "Saturation",
        [ColorPickerBrightness] = "Brightness",
        [ColorPickerAlpha] = "Opacity",
        [ColorPickerRed] = "Red",
        [ColorPickerGreen] = "Green",
        [ColorPickerBlue] = "Blue",
        [ColorPickerBadColor] = "Enter a color as #RRGGBB.",
        [ColorPickerNotInPalette] = "Choose one of the palette's colors.",
        [CalendarPreviousMonth] = "Previous month",
        [CalendarNextMonth] = "Next month",
        [CalendarPreviousYear] = "Previous year",
        [CalendarNextYear] = "Next year",
        [CalendarPreviousDecade] = "Previous decade",
        [CalendarNextDecade] = "Next decade",
        [CalendarChooseMonth] = "{0}, choose a month",
        [CalendarChooseYear] = "{0}, choose a year",
        [DatePickerOpen] = "Choose a date",
        [DatePickerClear] = "Clear",
        [DatePickerBadDate] = "Enter the date as {0}.",
        [DateHintDay] = "D",
        [DateHintMonth] = "M",
        [DateHintYear] = "Y",
        [EmptyValue] = "Not set",
        [TimestampZone] = "UTC{0}",
        [TimestampJustNow] = "just now",
        [TimestampMinutesAgo] = "{0} minutes ago",
        [TimestampMinutesAgoOne] = "1 minute ago",
        [TimestampInMinutes] = "in {0} minutes",
        [TimestampInMinutesOne] = "in 1 minute",
        [TimestampHoursAgo] = "{0} hours ago",
        [TimestampHoursAgoOne] = "1 hour ago",
        [TimestampInHours] = "in {0} hours",
        [TimestampInHoursOne] = "in 1 hour",
        [TimestampDaysAgo] = "{0} days ago",
        [TimestampDaysAgoOne] = "1 day ago",
        [TimestampInDays] = "in {0} days",
        [TimestampInDaysOne] = "in 1 day",
        [TimestampMonthsAgo] = "{0} months ago",
        [TimestampMonthsAgoOne] = "1 month ago",
        [TimestampInMonths] = "in {0} months",
        [TimestampInMonthsOne] = "in 1 month",
        [TimestampYearsAgo] = "{0} years ago",
        [TimestampYearsAgoOne] = "1 year ago",
        [TimestampInYears] = "in {0} years",
        [TimestampInYearsOne] = "in 1 year",
        [CountdownGoNow] = "Go now",
        [CountdownStay] = "Stay on this page",
        [CountdownStayed] = "Stayed on this page",
        [CountdownSeconds] = "{0} seconds",
        [CountdownSecondsOne] = "1 second",
        [CountdownMinutes] = "{0} minutes",
        [CountdownMinutesOne] = "1 minute",
        [CopyButtonCopy] = "Copy",
        [CopyButtonCopied] = "Copied",
        [CopyButtonFailed] = "Couldn't copy",
        [DatePickerUnavailable] = "{0} can't be chosen.",
        [TimePickerOutOfRange] = "Choose a time from {0} to {1}.",
        [TimePickerAtLeast] = "Choose {0} or later.",
        [TimePickerAtMost] = "Choose {0} or earlier.",
        [DateTimePickerDate] = "Date",
        [DateTimePickerTime] = "Time",
        [DateTimePickerOutOfRange] = "Choose a date and time from {0} to {1}.",
        [DateRangePickerStart] = "Start date",
        [DateRangePickerEnd] = "End date",
        [DateRangePickerOpen] = "Choose dates",
        [DateRangePickerPreviousMonth] = "Previous month",
        [DateRangePickerNextMonth] = "Next month",
        [DateRangePickerPresets] = "Presets",
        [DateRangePickerClear] = "Clear",
        [DateRangePickerChooseEnd] = "Now choose the end date.",
        [DateRangePickerChosen] = "{0} to {1}",
        [DateRangePickerBadStart] = "Enter the start date as {0}.",
        [DateRangePickerBadEnd] = "Enter the end date as {0}.",
        [DateRangePickerUnavailable] = "{0} can't be chosen.",
        [DateRangePickerEndBeforeStart] = "The end date is before the start date.",
        [DialogClose] = "Close",
        [DialogMinimize] = "Minimize",
        [DialogMaximize] = "Maximize",
        [DialogRestore] = "Restore",
        [DialogMove] = "Move",
        [DialogMoveHint] = "Arrow keys move the dialog.",
        [DialogResize] = "Resize",
        [DialogResizeHint] = "Arrow keys resize the dialog.",
        [DialogPositionCenter] = "Center",
        [DialogPositionTop] = "Top",
        [DialogPositionBottom] = "Bottom",
        [DialogPositionStart] = "Start",
        [DialogPositionEnd] = "End",
        [DialogPositionTopStart] = "Top start",
        [DialogPositionTopEnd] = "Top end",
        [DialogPositionBottomStart] = "Bottom start",
        [DialogPositionBottomEnd] = "Bottom end",
        [DialogSizeSmall] = "Small",
        [DialogSizeMedium] = "Medium",
        [DialogSizeLarge] = "Large",
        [DialogSizeExtraLarge] = "Extra large",
        [DialogConfirm] = "OK",
        [DialogCancel] = "Cancel",
        [FieldInvalid] = "{0} isn't valid.",
        [FieldRequired] = "{0} is required.",
        [FieldRequiredMark] = "required",
        [FieldChecking] = "Checking…",
        [FieldUnnamed] = "The field",
        [FileUploadZone] = "Drop files here, or click to choose them.",
        [FileUploadChoose] = "Choose files",
        [FileUploadRemove] = "Remove {0}",
        [FileUploadClear] = "Clear",
        [FileUploadChosen] = "{0} files chosen.",
        [FileUploadChosenOne] = "1 file chosen.",
        [FileUploadWrongType] = "{0} isn't one of {1}.",
        [FileUploadTooLarge] = "{0} is larger than {1}.",
        [FileUploadBytes] = "{0} B",
        [FileUploadKilobytes] = "{0} KB",
        [FileUploadMegabytes] = "{0} MB",
        [FileUploadGigabytes] = "{0} GB",
        [FileUploadTooMany] = "Choose at most {0} files.",
        [FileUploadTooManyOne] = "Choose only one file.",
        [LabelInfo] = "Information about {0}",
        [ListBoxEmpty] = "Nothing to choose from",
        [TransferListAvailable] = "Available",
        [TransferListChosen] = "Chosen",
        [TransferListMove] = "Move to {0}",
        [TransferListMoveAll] = "Move all to {0}",
        [TransferListMoved] = "Moved {0} to {1}",
        [CodeFieldIncomplete] = "{0} needs all {1} characters.",
        [CodeFieldIncompleteOne] = "{0} needs its 1 character.",
        [CodeFieldDigits] = "{0} digits",
        [CodeFieldDigitsOne] = "1 digit",
        [CodeFieldCharacters] = "{0} characters",
        [CodeFieldCharactersOne] = "1 character",
        [TransferListFilter] = "Filter {0}",
        [NewTabHint] = "opens in a new tab",
        [NumberFieldDecrease] = "Decrease",
        [NumberFieldIncrease] = "Increase",
        [NumberFieldNotANumber] = "{0} must be a number.",
        [NumberFieldRange] = "{0} must be from {1} to {2}.",
        [NumberFieldAtLeast] = "{0} must be at least {1}.",
        [NumberFieldAtMost] = "{0} must be at most {1}.",
        [MultiSelectRemoved] = "{0} removed",
        [MultiSelectSelectAll] = "Select all",
        [PaginationNavigation] = "Pagination",
        [PaginationPage] = "Page {0}",
        [PaginationPageBox] = "Page",
        [PaginationOf] = "of {0}",
        [PaginationPrevious] = "Previous page",
        [PaginationNext] = "Next page",
        [PaginationFirst] = "First page",
        [PaginationLast] = "Last page",
        [RangeSliderStart] = "Minimum",
        [RangeSliderEnd] = "Maximum",
        [RatingStar] = "1 star",
        [RatingStars] = "{0} stars",
        [SignaturePadUndo] = "Undo",
        [SignaturePadClear] = "Clear",
        [SignaturePadTypeName] = "Type your name",
        [SignaturePadTypedName] = "Your full name",
        [SignaturePadBlank] = "{0}, nothing drawn",
        [SignaturePadDrawn] = "{0}, signed",
        [SelectFilter] = "Filter",
        [SelectNoResults] = "No matches",
        [SelectEmpty] = "Nothing to choose from",
        [SelectClear] = "Clear",
        [DockGroup] = "Panel group {0}",
        [DockResize] = "Resize {0}",
        [DockCloseHint] = "Press Delete to close",
        [DockModified] = "Unsaved changes",
        [DockMenu] = "Options for {0}",
        [DockTabMenu] = "Tab options",
        [DockMoveTabBack] = "Move tab back",
        [DockMoveTabForward] = "Move tab forward",
        [DockMoveToGroup] = "Move to group",
        [DockGroupItem] = "Group {0}: {1}",
        [DockNewGroup] = "New group beside this one",
        [DockNewGroupAtEdge] = "New group at the dock's edge",
        [DockAtStart] = "At the start",
        [DockAtEnd] = "At the end",
        [DockAbove] = "Above",
        [DockBelow] = "Below",
        [DockAtTop] = "At the top",
        [DockAtBottom] = "At the bottom",
        [DockClose] = "Close",
        [DockMoved] = "{0} moved to panel group {1}.",
        [DockSizeMenu] = "Size of {0}",
        [DockLarger] = "Make {0} larger",
        [DockSmaller] = "Make {0} smaller",
        [DockMaximize] = "Maximize",
        [DockRestore] = "Restore",
        [DockMaximized] = "Panel group {0} fills the dock.",
        [DockRestored] = "All panel groups show again.",
        [DockFloatingGroup] = "Floating panel group {0}",
        [DockFloat] = "Float",
        [DockDock] = "Dock",
        [DockFloated] = "{0} floats in a window.",
        [DockPopOut] = "Open in new window",
        [DockReturn] = "Return to the dock",
        [DockPoppedOut] = "{0} opened in a new window.",
        [DockReturned] = "{0} is back in the dock.",
        [DockPopOutBlocked] = "The browser blocked the new window for {0}.",
        [DockPopOutLapsed] = "The new window for {0} closed before the panel was ready. Try again.",
        [DockConnectionLost] = "The page lost its connection to the server. This panel works again when the page "
            + "reconnects.",
        [DockEmptyWindow] = "{0} isn't open right now. This window shows it again when it is.",
        [DockAway] = "Panels in their own windows",
        [DockAwayHint] = "In its own browser window",
        [DockShowWindow] = "Show its window",
        [DockAutoHide] = "Auto-hide",
        [DockStrip] = "Auto-hidden panels ({0})",
        [DockRevealedGroup] = "Auto-hidden panel",
        [DockAutoHidden] = "{0} is auto-hidden.",
        [DockMoveWindow] = "Move the {0} window",
        [DockMoveWindowHint] = "Arrow keys move the window.",
        [DockResizeWindow] = "Resize the {0} window",
        [DockResizeWindowHint] = "Arrow keys resize the window.",
        [DockMovedWindow] = "The window is {0}% across and {1}% down.",
        [DockResizedWindow] = "The window is {0}% wide and {1}% tall.",
        [DockLeft] = "Left",
        [DockRight] = "Right",
        [DockUp] = "Up",
        [DockDown] = "Down",
        [DockWider] = "Wider",
        [DockNarrower] = "Narrower",
        [DockTaller] = "Taller",
        [DockShorter] = "Shorter",
        [SplitterResize] = "Resize {0}",
        [SplitterPane] = "pane {0}",
        [SplitterMenu] = "Size of {0}",
        [SplitterLarger] = "Make {0} larger",
        [SplitterSmaller] = "Make {0} smaller",
        [SplitterCollapse] = "Collapse {0}",
        [SplitterRestore] = "Restore {0}",
        [StepperCompleted] = "Completed",
        [StepperCurrent] = "Current step: {0}",
        [StepperLabel] = "Progress",
        [StepperOptional] = "Optional",
        [PopconfirmConfirm] = "OK",
        [PopconfirmCancel] = "Cancel",
        [RibbonCollapse] = "Ribbon commands",
        [RibbonContextTab] = "{0}: {1} tab available",
        [RibbonContextTabs] = "{0}: {1} tabs available",
        [RibbonKeyTip] = "Key tip: {0}",
        [TableOfContentsNavigation] = "On this page",
        [TabsAdd] = "Add tab",
        [TabsCloseHint] = "Press Delete to close",
        [TabsScrollStart] = "Scroll to earlier tabs",
        [TabsScrollEnd] = "Scroll to later tabs",
        [TagInputHint] = "Enter adds a tag. Left Arrow moves to the tags.",
        [TagInputRemove] = "Remove {0}",
        [TagInputAdded] = "{0} added",
        [TagInputRemoved] = "{0} removed",
        [TagInputDuplicate] = "{0} is already added.",
        [TagInputTooMany] = "You can add up to {0} tags.",
        [TagInputTooManyOne] = "You can add only one tag.",
        [TextFieldClear] = "Clear",
        [TextFieldShowPassword] = "Show password",
        [TextFieldTooLong] = "{0} can be at most {1} characters.",
        [TextFieldTooLongOne] = "{0} can be at most {1} character.",
        [TextFieldCount] = "{0} characters",
        [TextFieldCountOne] = "{0} character",
        [TextFieldCountOfLimit] = "{0} of {1} characters",
        [TextFieldCountOfLimitOne] = "{0} of {1} character",
        [TextFieldCharactersLeft] = "{0} characters left.",
        [TextFieldCharactersLeftOne] = "{0} character left.",
        [TextFieldMaskIncomplete] = "{0} isn't complete.",
        [RichTextEditorToolbar] = "Formatting",
        [RichTextEditorBold] = "Bold",
        [RichTextEditorItalic] = "Italic",
        [RichTextEditorUnderline] = "Underline",
        [RichTextEditorStrikethrough] = "Strikethrough",
        [RichTextEditorSubscript] = "Subscript",
        [RichTextEditorSuperscript] = "Superscript",
        [RichTextEditorCode] = "Code",
        [RichTextEditorHighlight] = "Highlight",
        [RichTextEditorParagraph] = "Paragraph",
        [RichTextEditorTextColor] = "Text color",
        [RichTextEditorDefaultColor] = "Default",
        [RichTextEditorColorPrimary] = "Primary",
        [RichTextEditorColorSecondary] = "Secondary",
        [RichTextEditorColorNeutral] = "Gray",
        [RichTextEditorColorSuccess] = "Green",
        [RichTextEditorColorWarning] = "Amber",
        [RichTextEditorColorDanger] = "Red",
        [RichTextEditorColorInfo] = "Blue",
        [RichTextEditorHeading1] = "Heading 1",
        [RichTextEditorHeading2] = "Heading 2",
        [RichTextEditorHeading3] = "Heading 3",
        [RichTextEditorQuote] = "Quote",
        [RichTextEditorBulletedList] = "Bulleted list",
        [RichTextEditorNumberedList] = "Numbered list",
        [RichTextEditorOutdent] = "Outdent",
        [RichTextEditorIndent] = "Indent",
        [RichTextEditorHorizontalRule] = "Horizontal line",
        [RichTextEditorClearFormatting] = "Clear formatting",
        [RichTextEditorSource] = "HTML source",
        [RichTextEditorFindAndReplace] = "Find and replace",
        [RichTextEditorFind] = "Find",
        [RichTextEditorFindCount] = "{0} of {1}",
        [RichTextEditorFindNoResults] = "No results",
        [RichTextEditorFindPrevious] = "Previous match",
        [RichTextEditorFindNext] = "Next match",
        [RichTextEditorMatchCase] = "Match case",
        [RichTextEditorWholeWord] = "Whole word",
        [RichTextEditorCloseFind] = "Close",
        [RichTextEditorReplaceWith] = "Replace with",
        [RichTextEditorReplace] = "Replace",
        [RichTextEditorReplaceAll] = "Replace all",
        [RichTextEditorReplaced] = "Replaced {0}",
        [RichTextEditorFloatingToolbar] = "Selection formatting",
        [RichTextEditorLink] = "Link",
        [RichTextEditorAddLink] = "Add link",
        [RichTextEditorEditLink] = "Edit link",
        [RichTextEditorLinkText] = "Text",
        [RichTextEditorLinkAddress] = "Link",
        [RichTextEditorLinkNotAllowed] = "This kind of link isn't allowed here. Allowed: {0}",
        [RichTextEditorRelativeLinkOnly] = "Use a link within this site.",
        [RichTextEditorApplyLink] = "Apply",
        [RichTextEditorRemoveLink] = "Remove link",
        [RichTextEditorOpenLink] = "Open link",
        [RichTextEditorImage] = "Image",
        [RichTextEditorInsertImage] = "Insert image",
        [RichTextEditorImageFile] = "Image file",
        [RichTextEditorImageAlt] = "Alt text",
        [RichTextEditorImageAltHelp] = "Describes the image for people who can't see it.",
        [RichTextEditorImageDecorative] = "Decorative",
        [RichTextEditorImageDecorativeHelp] = "It adds nothing the text doesn't say, so screen readers skip it.",
        [RichTextEditorApplyImage] = "Insert",
        [RichTextEditorImageFailed] = "The image couldn't be stored. Try again.",
        [RichTextEditorImageNotAllowed] = "The image's address isn't one this editor allows.",
        [RichTextEditorTable] = "Table",
        [RichTextEditorTableMenu] = "Table options",
        [RichTextEditorAddRowAbove] = "Add row above",
        [RichTextEditorAddRowBelow] = "Add row below",
        [RichTextEditorAddColumnLeft] = "Add column left",
        [RichTextEditorAddColumnRight] = "Add column right",
        [RichTextEditorDeleteRow] = "Delete row",
        [RichTextEditorDeleteColumn] = "Delete column",
        [RichTextEditorDeleteTable] = "Delete table",
        [RichTextEditorHeaderRow] = "Header row",
        [MarkdownEditorPreview] = "Preview",
        [TourNext] = "Next",
        [TourBack] = "Back",
        [TourDone] = "Done",
        [TourSkip] = "Skip tour",
        [TourPosition] = "{0} of {1}",
        [ButtonCooldownLeft] = "({0})",
        [ButtonCooldown] = "Available again in {0} seconds",
        [ButtonCooldownOne] = "Available again in 1 second",
        [SpeechToTextLabel] = "Dictate",
        [SpeechToTextListening] = "Listening",
        [SpeechToTextStopped] = "Stopped listening",
        [SmartPasteLabel] = "Smart paste",
        [SmartPasteTitle] = "Paste text to fill the form",
        [SmartPasteText] = "Text to fill from",
        [SmartPasteFill] = "Fill",
        [SmartPasteCancel] = "Cancel",
        [SmartPasteFilling] = "Filling the form",
        [SmartPasteFilled] = "Filled {0} fields",
        [SmartPasteFilledOne] = "Filled 1 field",
        [SmartPasteNothing] = "Found nothing to fill",
        [SmartPasteFailed] = "Couldn't fill the form",
        [AsyncContentLoading] = "Loading {0}",
        [AsyncContentLoadFailed] = "Unable to load {0}.",
        [AsyncContentRetry] = "Retry",
        [BackToTopLabel] = "Back to top",
        [InPlaceEditorEdit] = "Edit",
        [InPlaceEditorSave] = "Save",
        [InPlaceEditorCancel] = "Cancel",
        [ThemePicker] = "Theme",
        [ThemeLight] = "Light",
        [ThemeDark] = "Dark",
        [ThemeSystem] = "Match the device",
        [ToastActionAvailable] = "{0} available, press F8.",
        [ToastDismiss] = "Dismiss",
        [ToastRegion] = "Notifications",
        [ToolbarMore] = "More",
        [DataGridLoading] = "Loading…",
        [DataGridLoadFailed] = "Couldn't load the rows.",
        [DataGridExportFailed] = "Couldn't export the rows.",
        [DataGridRetry] = "Retry",
        [DataGridEmpty] = "Nothing to show.",
        [DataGridSelectAll] = "Select all rows",
        [DataGridSelect] = "Select",
        [DataGridSelectRow] = "Select {0}",
        [DataGridDetails] = "Details",
        [DataGridDetailsFor] = "Details for {0}",
        [DataGridChanged] = "Changed",
        [DataGridUnsavedChanges] = "Unsaved changes: {0}",
        [DataGridSaveChanges] = "Save changes",
        [DataGridDiscardChanges] = "Discard changes",
        [DataGridChangesSaved] = "Changes saved",
        [DataGridRowsUnder] = "Rows under {0}",
        [DataGridExport] = "Export",
        [DataGridExportCsv] = "CSV",
        [DataGridExportExcel] = "Excel",
        [DataGridExportPdf] = "PDF",
        [DataGridOrder] = "Order",
        [DataGridRange] = "{0}–{1} of {2}",
        [DataGridPages] = "Pages of {0}",
        [DataGridPageSize] = "Rows per page",
        [DataGridFilter] = "Filter {0}",
        [DataGridFilterOn] = "Filter {0}, on",
        [DataGridFilterAny] = "Any",
        [DataGridFilterYes] = "Yes",
        [DataGridFilterNo] = "No",
        [DataGridRowsShown] = "Rows shown: {0}.",
        [DataGridEditInvalid] = "{0} can't hold that value.",
        [DataGridSave] = "Save",
        [DataGridSaveRow] = "Save {0}",
        [DataGridCancel] = "Cancel",
        [DataGridCancelRow] = "Cancel editing {0}",
        [DataGridGroupBy] = "Group by",
        [DataGridColumnMenu] = "Options for {0}",
        [DataGridEarlier] = "Move {0} earlier",
        [DataGridLater] = "Move {0} later",
        [DataGridColumnMoved] = "{0} moved to column {1} of {2}",
        [DataGridColumns] = "Columns",
        [DataGridHide] = "Hide {0}",
        [DataGridColumnHidden] = "{0} hidden",
        [DataGridTotals] = "Totals",
        [DataGridGroupTotals] = "Totals for {0}",
        [DataGridSum] = "Sum",
        [DataGridAverage] = "Average",
        [DataGridCount] = "Count",
        [DataGridMin] = "Min",
        [DataGridMax] = "Max",
        [DataGridWider] = "Make {0} wider",
        [DataGridNarrower] = "Make {0} narrower",
        [DataGridResetWidths] = "Reset column widths",
        [DataGridGroupHeading] = "{0}: {1}",
        [DataGridGroupCount] = "Rows: {0}",
        [DataGridGroupNone] = "None",
        [DataGridFilterApply] = "Apply",
        [DataGridFilterClear] = "Clear",
        [DataGridFilterCondition] = "Condition",
        [DataGridFilterValue] = "Value",
        [DataGridFilterContains] = "Contains",
        [DataGridFilterDoesNotContain] = "Doesn't contain",
        [DataGridFilterStartsWith] = "Starts with",
        [DataGridFilterEndsWith] = "Ends with",
        [DataGridFilterEqualTo] = "Is",
        [DataGridFilterNotEqualTo] = "Isn't",
        [DataGridFilterLessThan] = "Less than",
        [DataGridFilterAtMost] = "At most",
        [DataGridFilterGreaterThan] = "More than",
        [DataGridFilterAtLeast] = "At least",
        [DataGridFilterIsEmpty] = "Is empty",
        [DataGridFilterIsNotEmpty] = "Isn't empty",
        [DataGridSortedAscending] = "Sorted by {0}, ascending.",
        [DataGridSortedDescending] = "Sorted by {0}, descending.",
        [DataGridUnsorted] = "No longer sorted by {0}.",
        [DataGridSortPosition] = "sort {0} of {1}, {2}",
        [DataGridAscending] = "ascending",
        [DataGridDescending] = "descending",
        [TreeViewLoading] = "Loading…",
        [TreeViewLoadFailed] = "Couldn't load these items.",
        [TreeViewRetry] = "Retry",
        [TreeViewEmpty] = "Nothing to show.",
        [TreeViewMoveHint] = "Alt+Shift and the arrow keys move this item.",
        [TreeViewMovedBefore] = "{0} moved before {1}.",
        [TreeViewMovedAfter] = "{0} moved after {1}.",
        [TreeViewMovedInside] = "{0} moved into {1}.",
        [TreeViewItemCount] = "{0} items",
        [TreeViewCantMoveIntoItself] = "An item can't move into itself.",
        [TreeSelectFilter] = "Filter",
        [TreeSelectClear] = "Clear",
        [TreeSelectMore] = "{0} more",
        [QueryBuilderMatch] = "Match",
        [QueryBuilderAll] = "All of these",
        [QueryBuilderAny] = "Any of these",
        [QueryBuilderField] = "Field",
        [QueryBuilderComparison] = "Comparison",
        [QueryBuilderCondition] = "Condition {0}",
        [QueryBuilderGroup] = "Group {0}",
        [QueryBuilderAddCondition] = "Add condition",
        [QueryBuilderAddGroup] = "Add group",
        [QueryBuilderRemoveCondition] = "Remove condition {0}",
        [QueryBuilderRemoveGroup] = "Remove group {0}",
        [QueryBuilderIsAnyOf] = "Is any of",
        [QueryBuilderBefore] = "Before",
        [QueryBuilderOnOrBefore] = "On or before",
        [QueryBuilderAfter] = "After",
        [QueryBuilderOnOrAfter] = "On or after",
        [ViewportZoomIn] = "Zoom in",
        [ViewportZoomOut] = "Zoom out",
        [ViewportZoom] = "Zoom {0}",
        [ViewportFitWidth] = "Fit width",
        [ViewportFitPage] = "Fit page",
        [ViewportActualSize] = "Actual size",
        [SortableHandle] = "Move {0}",
        [SortableHint] =
            "Arrow keys move it, and Home and End move it to the start or end. Enter opens a menu of moves.",
        [SortableMoveUp] = "Move up",
        [SortableMoveDown] = "Move down",
        [SortableMoveToStart] = "Move to the start",
        [SortableMoveToEnd] = "Move to the end",
        [SortableMoved] = "{0} moved to position {1} of {2}.",
        [SortableGroupHint] = "Up and Down move it, and Home and End move it to the start or end. Left and Right move "
            + "it to the previous or next list. Enter opens a menu of moves.",
        [SortableMoveToList] = "Move to {0}",
        [SortableMovedToList] = "{0} moved to {3}, position {1} of {2}.",
    }.ToFrozenDictionary();

    /// <summary>The key of <paramref name="key"/>'s one-form, such as <see cref="FileUploadChosenOne"/> for
    /// <see cref="FileUploadChosen"/>, or <paramref name="key"/> itself when it has none. An
    /// <see cref="ITbLocalizer"/> with its own <see cref="ITbLocalizer.Plural"/> can fall back on it.</summary>
    public static string OneFormOf(string key) => OneForms.TryGetValue(key, out var one) ? one : key;

    /// <summary>The English text for <paramref name="key"/>, for a localizer of your own to fall back on.</summary>
    /// <exception cref="KeyNotFoundException">The key isn't one of these.</exception>
    public static string English(string key) => EnglishText.TryGetValue(key, out var text) ? text
        : throw new KeyNotFoundException($"{key} isn't a Trailblazor string; the keys are in {nameof(TbStrings)}.");
}
