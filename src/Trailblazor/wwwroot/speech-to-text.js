import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';

// The browser's speech recognition, under its standard name or Safari's and Chrome's prefixed one.
const Recognition = () => window.SpeechRecognition ?? window.webkitSpeechRecognition;

// Unhooks a recognition's handlers before aborting it, so its late events can't touch the next one.
const letGo = recognition => {
    if (recognition) {
        recognition.onresult = recognition.onerror = recognition.onend = null;
        recognition.abort();
    }
};

// Listens while asked to, and tells .NET each phrase it hears and when it stops. Returns null where the browser
// recognizes no speech, so the button stays hidden.
export function attachSpeechToText(root, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!root) {
        return null;
    }

    // Ready either way, so a test can wait for the script to have looked.
    root.dataset.ready = '';
    if (!Recognition()) {
        return null;
    }

    let recognition = null;

    return {
        start(language, continuous, session) {
            letGo(recognition);
            const current = recognition = new (Recognition())();
            current.lang = language || document.documentElement.lang || navigator.language;
            current.continuous = continuous;
            current.interimResults = false;
            current.onresult = event => {
                for (let index = event.resultIndex; index < event.results.length; index++) {
                    const result = event.results[index];
                    if (result.isFinal) {
                        callDotNet(events, 'OnText', result[0].transcript.trim());
                    }
                }
            };
            current.onerror = event => callDotNet(events, 'OnError', event.error);
            current.onend = () => {
                recognition = null;
                callDotNet(events, 'OnStopped', session);
            };
            current.start();
        },
        stop() {
            recognition?.stop();
        },
        dispose() {
            letGo(recognition);
            recognition = null;
        },
    };
}
