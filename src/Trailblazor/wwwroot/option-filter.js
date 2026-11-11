// Matching ignores case and accents, so "eagle" finds "Éagle".
export const fold = text => text.normalize('NFD').replace(/\p{M}/gu, '').toLocaleLowerCase();

// Hides the options the query leaves out and the groups left empty, and shows the status's no-results text when
// none is left. ruleFor(option) returns true to hide an option or false to show it whatever the text, or null to let
// the text decide; moreLeft() says whether options not drawn yet still match, as in a virtualized list.
export function filterOptions(listbox, query, status, ruleFor = () => null, moreLeft = () => false) {
    const folded = fold(query);
    for (const option of listbox.querySelectorAll('[role="option"]')) {
        const hide = ruleFor(option) ?? !fold(option.dataset.text || option.textContent).includes(folded);
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
