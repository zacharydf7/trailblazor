// The browser may still be reading a download's blob once the link's click returns, so it is let go well after.
const revokeDelay = 40_000;

// Reports once resizing pauses, so dragging a window's edge doesn't send a size for every frame.
const resizeSettleDelay = 100;

export async function copyText(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        // There's no clipboard on an insecure page, and the browser refuses it without a recent user action.
        return false;
    }
}

export async function download(streamRef, fileName, contentType) {
    const blob = new Blob([await streamRef.arrayBuffer()], { type: contentType ?? '' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), revokeDelay);
}

export async function save(streamRef, fileName, contentType) {
    if (!('showSaveFilePicker' in window)) {
        await download(streamRef, fileName, contentType);
        return true;
    }

    let file;
    try {
        file = await showSaveFilePicker({ suggestedName: fileName });
    } catch (error) {
        if (error.name === 'AbortError') {
            return false;
        }

        // The browser opens the dialog only soon after a user action, but it still allows a download.
        if (error.name === 'SecurityError') {
            await download(streamRef, fileName, contentType);
            return true;
        }

        throw error;
    }

    await (await streamRef.stream()).pipeTo(await file.createWritable());
    return true;
}

export function canPickColor() {
    return 'EyeDropper' in window;
}

export async function pickColor() {
    if (!canPickColor()) {
        return null;
    }

    try {
        return (await new EyeDropper().open()).sRGBHex;
    } catch (error) {
        if (error.name === 'AbortError') {
            return null;
        }

        throw error;
    }
}

export function scrollIntoView(element, block, smooth) {
    const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
    element.scrollIntoView({ block, inline: 'nearest', behavior: smooth && !reduced ? 'smooth' : 'instant' });
}

export function getViewport() {
    return { width: innerWidth, height: innerHeight };
}

export function observeViewport(listener) {
    let timer;
    const report = () => listener.invokeMethodAsync('OnChange', getViewport());
    const onResize = () => {
        clearTimeout(timer);
        timer = setTimeout(report, resizeSettleDelay);
    };

    addEventListener('resize', onResize);
    report();
    return {
        dispose() {
            clearTimeout(timer);
            removeEventListener('resize', onResize);
        },
    };
}

// The breakpoint is the number of minimum widths the viewport reaches, which matches the enum's order.
export function observeBreakpoint(listener, minWidths) {
    const queries = minWidths.map(width => matchMedia(`(min-width: ${width}px)`));
    let reported;
    // Crossing several breakpoints at once fires a change for each, so only a new breakpoint is reported.
    const report = () => {
        const breakpoint = queries.filter(query => query.matches).length;
        if (breakpoint !== reported) {
            reported = breakpoint;
            listener.invokeMethodAsync('OnChange', breakpoint);
        }
    };

    queries.forEach(query => query.addEventListener('change', report));
    report();
    return {
        dispose() {
            queries.forEach(query => query.removeEventListener('change', report));
        },
    };
}
