// The docs site's direction switch. The page's head script restores a remembered right-to-left direction before the
// first paint.
export const isRightToLeft = () => document.documentElement.dir === 'rtl';

export function set(rightToLeft) {
    if (rightToLeft) {
        document.documentElement.dir = 'rtl';
    } else {
        document.documentElement.removeAttribute('dir');
    }

    try {
        localStorage.setItem('docs-direction', rightToLeft ? 'rtl' : 'ltr');
    } catch {
        // Storage can be off, as in a private window; the direction then lasts until the page is left.
    }
}
