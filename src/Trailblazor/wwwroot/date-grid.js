// Date arithmetic for the calendars' day grids, on UTC dates so a clock change never skips or repeats a day.
const dayMs = 24 * 60 * 60 * 1000;

export const toDate = iso => new Date(`${iso}T00:00:00Z`);
export const toIso = date => date.toISOString().slice(0, 10);

// The browser's own date, which the server's clock in another time zone may not share.
export function browserToday() {
    const now = new Date();
    return toIso(new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate())));
}

// Moves by months the way a calendar does: from January 31, one month on is the last day of February. Dates are built
// with setUTCFullYear, since Date.UTC reads the years 0 to 99 as 1900 to 1999.
export function addMonths(date, months) {
    const moved = new Date(0);
    moved.setUTCFullYear(date.getUTCFullYear(), date.getUTCMonth() + months, 1);
    const last = new Date(0);
    last.setUTCFullYear(moved.getUTCFullYear(), moved.getUTCMonth() + 1, 0);
    moved.setUTCDate(Math.min(date.getUTCDate(), last.getUTCDate()));
    return moved;
}

// The day a key moves to from the given one, or null for a key the grid leaves alone.
export function dayTarget(key, shift, date, firstDayOfWeek) {
    const weekday = (date.getUTCDay() - firstDayOfWeek + 7) % 7;
    switch (key) {
        case 'ArrowLeft': return new Date(date - dayMs);
        case 'ArrowRight': return new Date(+date + dayMs);
        case 'ArrowUp': return new Date(date - 7 * dayMs);
        case 'ArrowDown': return new Date(+date + 7 * dayMs);
        case 'Home': return new Date(date - weekday * dayMs);
        case 'End': return new Date(+date + (6 - weekday) * dayMs);
        case 'PageUp': return addMonths(date, shift ? -12 : -1);
        case 'PageDown': return addMonths(date, shift ? 12 : 1);
        default: return null;
    }
}
