using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>Stands in for the browser's speech recognition, which a test browser can't use without a microphone, so a
/// test can hand a <c>TbSpeechToText</c> what it hears with <see cref="TbSpeechToTextLocator.SayAsync"/>.</summary>
public static class FakeSpeech
{
    // Like the browser's, one recognition listens at a time, and it ends after one phrase unless continuous.
    // data-tb-fake-listening on <html> is 1 while one listens and 0 otherwise.
    private const string Script = """
        (() => {
            const listening = [];
            const mark = () => document.documentElement.dataset.tbFakeListening = String(listening.length);
            class FakeSpeechRecognition {
                start() {
                    listening.forEach(other => other.end());
                    listening.push(this);
                    mark();
                }
                stop() { this.end(); }
                abort() { this.end(); }
                end() {
                    const at = listening.indexOf(this);
                    if (at >= 0) {
                        listening.splice(at, 1);
                        mark();
                        this.onend?.();
                    }
                }
            }
            window.SpeechRecognition = window.webkitSpeechRecognition = FakeSpeechRecognition;
            window.trailblazorFakeSpeech = {
                say(text) {
                    const recognition = listening[0];
                    recognition.onresult?.({ resultIndex: 0,
                        results: [Object.assign([{ transcript: text, confidence: 1 }], { isFinal: true })] });
                    const continuous = recognition.continuous;
                    if (!continuous) {
                        recognition.end();
                    }
                    return continuous;
                },
            };
        })();
        """;

    /// <summary>Replaces the browser's speech recognition on this page, in every browser, with one that hears only
    /// what <see cref="TbSpeechToTextLocator.SayAsync"/> hands it. Call it before the page opens the app, or reload
    /// the page after.</summary>
    public static Task UseFakeSpeechAsync(this IPage page)
    {
        // Playwright later changed AddInitScriptAsync's return type, so a direct call compiled against this
        // package's minimum version throws MissingMethodException on newer ones; reflection works on both.
        var add = typeof(IPage).GetMethod(nameof(IPage.AddInitScriptAsync), [typeof(string), typeof(string)])!;
        return (Task)add.Invoke(page, [Script, null])!;
    }
}
