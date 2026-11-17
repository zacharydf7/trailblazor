namespace Trailblazor;

/// <summary>Why a <see cref="TbSpeechToText"/> stopped listening without hearing anything.</summary>
public enum SpeechToTextError
{
    /// <summary>The person, or the browser, didn't allow the microphone.</summary>
    NotAllowed,

    /// <summary>Nothing was said before the browser gave up waiting.</summary>
    NoSpeech,

    /// <summary>No microphone was found, or it couldn't be used.</summary>
    NoMicrophone,

    /// <summary>The browser's recognition service couldn't be reached.</summary>
    Network,

    /// <summary>Recognition failed for another reason.</summary>
    Other,
}
