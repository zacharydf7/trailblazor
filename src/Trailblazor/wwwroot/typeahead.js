// Quick typing finds the next option by prefix, a repeated letter steps through matches, and a pause starts over.
export function createTypeahead() {
    let typed = '';
    let typedAt = 0;
    const type = key => {
        const now = Date.now();
        typed = now - typedAt > 500 ? key.toLowerCase() : typed + key.toLowerCase();
        typedAt = now;
        return typed;
    };
    return {
        // Whether a word is still being typed, in which Space is a letter, as in "New York".
        typing: () => typed !== '' && Date.now() - typedAt <= 500,
        // Adds a key to what is being typed and returns it all, for options searched somewhere else.
        type,
        find(options, current, key) {
            type(key);
            const index = options.indexOf(current);
            const firstFrom = (start, text) => options.map((_, step) => options[(start + step) % options.length])
                .find(option => (option.dataset.text ?? option.textContent).trim().toLowerCase().startsWith(text));
            const match = firstFrom(typed.length === 1 ? index + 1 : Math.max(index, 0), typed);
            // A real double letter, as in "Aaron", takes priority over stepping, unless the search is already on it.
            return [...typed].every(letter => letter === typed[0]) && (!match || match === current)
                ? firstFrom(index + 1, typed[0]) : match;
        },
    };
}
