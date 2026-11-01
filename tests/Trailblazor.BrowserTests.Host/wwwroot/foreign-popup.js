// Stands in for another library's popup, such as an editor's menu or a date picker: it appends itself to the page's
// body and places itself by the page's coordinates, above most of the page.
export function attach(button, output) {
    button.addEventListener('click', () => {
        const box = button.getBoundingClientRect();
        const popup = document.createElement('div');
        popup.className = 'foreign-popup';
        popup.style.cssText = `position: absolute; z-index: 2000; left: ${box.left + scrollX}px;`
            + ` top: ${box.bottom + scrollY}px; padding: 8px; background: white; border: 1px solid gray;`;
        const red = document.createElement('button');
        red.type = 'button';
        red.textContent = 'Red';
        red.addEventListener('click', () => {
            output.textContent = 'Red';
            popup.remove();
        });
        popup.append(red);
        document.body.append(popup);
    });
}

// Stands in for a date picker that makes its calendar once, as the page loads, and for Blazor's error bar.
export function createPageParts() {
    const popup = document.createElement('div');
    popup.className = 'reused-popup';
    popup.hidden = true;
    popup.style.cssText = 'position: absolute; z-index: 2000; padding: 8px; background: white; border: 1px solid gray;';
    const large = document.createElement('button');
    large.type = 'button';
    large.textContent = 'Large';
    popup.append(large);
    const errorBar = document.createElement('div');
    errorBar.id = 'blazor-error-ui';
    document.body.append(popup, errorBar);
}

// Shows the calendar made as the page loaded, by the button.
export function attachReused(button, output) {
    button.addEventListener('click', () => {
        const popup = document.querySelector('.reused-popup');
        const box = button.getBoundingClientRect();
        popup.style.left = `${box.left + scrollX}px`;
        popup.style.top = `${box.bottom + scrollY}px`;
        popup.hidden = false;
        popup.querySelector('button').onclick = () => {
            output.textContent = 'Large';
            popup.hidden = true;
        };
    });
}
