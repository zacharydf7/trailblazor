// Typing letters quickly finds the next option that starts with them; a pause starts the search over.
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
            const start = index < 0 ? 0 : index + (typed.length === 1 ? 1 : 0);
            return options.map((_, step) => options[(start + step) % options.length])
                .find(option => (option.dataset.text ?? option.textContent).trim().toLowerCase().startsWith(typed));
        },
    };
}
