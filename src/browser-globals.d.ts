// What the library's scripts use that TypeScript's DOM types leave out: newer browser APIs they check for before
// using, and Blazor's JS interop global.

declare function showSaveFilePicker(options?: { suggestedName?: string }): Promise<FileSystemFileHandle>;

declare class EyeDropper {
    open(): Promise<{ sRGBHex: string }>;
}

interface Navigator {
    readonly userAgentData?: { readonly platform: string };
}

interface Window {
    SpeechRecognition?: new () => any;
    webkitSpeechRecognition?: new () => any;
}

declare const DotNet: {
    createJSObjectReference(value: object): object;
};
