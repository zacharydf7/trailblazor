// Matching ignores case and accents, as TextFold does on the server: lowercase first, in the culture the list was
// drawn in, so Turkish "İ" becomes "i", then without accents, so "eagle" finds "Éagle". Only the combining diacritical
// marks and Hebrew's and Arabic's vowel and cantillation marks go, since other scripts' marks are letters. Greek's
// final sigma becomes σ, the lowercase of Σ.
export const fold = (text, locale) => text.toLocaleLowerCase(locale).replace(/ς/g, 'σ').normalize('NFD')
    .replace(/[\u0300-\u036f\u0591-\u05bd\u05bf\u05c1-\u05c2\u05c4-\u05c5\u05c7\u064b-\u065f\u0670]/g, '')
    .replace(/[\u1ab0-\u1aff\u1dc0-\u1dff\u20d0-\u20ff\ufe20-\ufe2f]/g, '');

// The list's culture, or the browser's when it has none or the browser doesn't know it.
const localeOf = listbox => {
    try {
        return Intl.getCanonicalLocales(listbox.dataset.culture || [])[0];
    } catch {
        return undefined;
    }
};

// Hides the options the query leaves out and the groups left empty, and shows the status's no-results text when
// none is left. ruleFor(option) returns true to hide an option or false to show it whatever the text, or null to let
// the text decide; moreLeft() says whether options not drawn yet still match, as in a virtualized list.
export function filterOptions(listbox, query, status, ruleFor = () => null, moreLeft = () => false) {
    const locale = localeOf(listbox);
    const folded = fold(query, locale);
    for (const option of listbox.querySelectorAll('[role="option"]')) {
        const hide = ruleFor(option) ?? !fold(option.dataset.text || option.textContent, locale).includes(folded);
        if (option.hidden !== hide) {
            option.hidden = hide;
        }
    }

    for (const group of listbox.querySelectorAll('[role="group"]')) {
        const hide = !group.querySelector('[role="option"]:not([hidden])');
        if (group.hidden !== hide) {
            group.hidden = hide;
        }
    }

    const text = folded && !listbox.querySelector('[role="option"]:not([hidden])') && !moreLeft()
        ? status?.dataset.text : '';
    if (status && status.textContent !== text) {
        status.textContent = text;
    }
}
