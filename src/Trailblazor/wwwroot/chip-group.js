// Remembers where a chip sits among the group's chips, so focus can land on its neighbor once it is gone.
export function rememberPlace(group, chip) {
    group.tbChipPlace = [...group.querySelectorAll('.tb-chip')].indexOf(chip);
}

// Focuses the chip now in the remembered place, the one before it if the last chip went, or the group if none is left.
export function focusPlace(group) {
    const place = group.tbChipPlace;
    delete group.tbChipPlace;
    if (place === undefined || place < 0) {
        return;
    }

    const chips = [...group.querySelectorAll('.tb-chip')];
    const chip = chips[Math.min(place, chips.length - 1)];
    (chip?.querySelector('button:not(:disabled), a[href]') ?? group).focus();
}
