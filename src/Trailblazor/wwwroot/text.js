import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { watchSizes } from '../Trailblazor.Primitives/pages.js';

// Tells .NET whether the text's box cuts it off, each time that changes as the box or the text does. A tooltip's
// bubble inside the text shows outside the box and takes no room in it.
export function watchTextCut(text, events) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!text) {
        return null;
    }

    let cut = null;
    const check = () => {
        const now = text.scrollHeight > text.clientHeight || text.scrollWidth > text.clientWidth;
        if (now !== cut) {
            cut = now;
            callDotNet(events, 'OnCut', now);
        }
    };
    const sizes = watchSizes(check);
    sizes.observe(text);
    const changes = new MutationObserver(check);
    changes.observe(text, { childList: true, characterData: true, subtree: true });
    check();

    return {
        dispose() {
            sizes.disconnect();
            changes.disconnect();
        },
    };
}
