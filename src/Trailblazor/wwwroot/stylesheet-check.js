// Trailblazor's rules sit in cascade layers, so any rule outside a layer wins over them however specific they are. A
// reset or framework stylesheet that sets the properties below on every element, or on bare buttons, labels, legends
// and headings, squares buttons, strips spacing and resizes titles and labels.
// Browsers write *:not(dialog) as :not(dialog), so a bare :not() stands for every element.
const element = /^((\*|button|label|legend|h[1-6])(:[\w-]+|:not\([^)]*\))*|(:not\([^)]*\))+)$/i;
const wrapped = /^:(where|is)\((.*)\)$/i;
// A rule's style lists longhands, such as margin-top and border-top-left-radius.
const properties = /^(margin|padding|background|border)(-|$)|^(font-size|line-height|display)$/;

// A selector list split at its own commas, not at those inside parentheses such as :where(), :is() or :not().
// Unwrapping :is(.card) :is(h2) leaves .card) :is(h2, which isn't one list, so a stray ) gives no parts.
function selectors(list) {
    const parts = [];
    let depth = 0;
    let start = 0;
    for (let index = 0; index < list.length; index++) {
        if (list[index] === '(') {
            depth++;
        } else if (list[index] === ')' && --depth < 0) {
            return [];
        } else if (list[index] === ',' && depth === 0) {
            parts.push(list.slice(start, index));
            start = index + 1;
        }
    }

    return [...parts, list.slice(start)].map(part => part.trim());
}

const targets = selector => wrapped.test(selector)
    ? selectors(selector.replace(wrapped, '$2')).some(inner => element.test(inner))
    : element.test(selector);

const overrides = rule => selectors(rule.selectorText).some(targets)
    && [...rule.style].some(name => properties.test(name));

// The stylesheet whose rules override Trailblazor's: this one, or one it imports outside a layer. Layer blocks and
// stylesheets imported into a layer are skipped; media, supports and container blocks are not.
function offending(sheet, rules) {
    for (const rule of rules) {
        if (rule instanceof CSSStyleRule && overrides(rule)) {
            return sheet;
        }

        if (rule instanceof CSSImportRule && rule.layerName === null && rule.styleSheet) {
            const imported = offending(rule.styleSheet, readable(rule.styleSheet));
            if (imported) {
                return imported;
            }
        }

        if (rule instanceof CSSConditionRule) {
            const nested = offending(sheet, rule.cssRules);
            if (nested) {
                return nested;
            }
        }
    }

    return null;
}

// A stylesheet from another origin can't be read, so it can't be checked.
function readable(sheet) {
    try {
        return sheet.cssRules;
    } catch {
        return [];
    }
}

export function checkStylesheets() {
    const found = [...document.styleSheets].map(sheet => offending(sheet, readable(sheet))).filter(Boolean);
    if (found.length > 0) {
        const names = found.map(sheet => sheet.href ?? 'a <style> element').join(', ');
        console.warn('Trailblazor: rules outside a cascade layer style every element, or buttons, labels, legends or '
            + "headings, and win over Trailblazor's. Move their stylesheets into a layer that comes before "
            + `Trailblazor's (https://trailblazor.dev/guides/theming#other-stylesheets). Stylesheets: ${names}`);
    }
}
