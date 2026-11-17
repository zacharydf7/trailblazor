// The clipboard's text, or null where the browser won't share it: the user refused, the page isn't secure, or the
// browser has no clipboard to read.
export async function readClipboard() {
    try {
        return await navigator.clipboard.readText();
    } catch {
        return null;
    }
}
