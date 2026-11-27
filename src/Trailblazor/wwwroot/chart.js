import { isComposing } from '../Trailblazor.Primitives/composing.js';
import { callDotNet } from '../Trailblazor.Primitives/dotnet-call.js';
import { frames, viewOf } from '../Trailblazor.Primitives/pages.js';

// Follows the pointer and the keys over a chart that .NET has already drawn. Every place, value and position is read
// from the markup, so moving over the chart costs the server nothing; .NET hears only that a point was activated.
export function attachChart(plot, events, point, noValue, activates) {
    // Blazor passes null for an element that was removed before this call arrived.
    if (!plot) {
        return null;
    }

    // How a point is named for screen readers, what a missing value reads, and whether .NET hears a point activated.
    let options = { point, noValue, activates };

    const page = plot.ownerDocument;
    const crosshair = plot.querySelector(':scope > .tb-chart__crosshair');
    const tooltip = plot.querySelector(':scope > .tb-chart__tooltip');
    const proxies = [...plot.querySelectorAll(':scope > .tb-chart__focus')];
    // A pie's places are its slices, set around a circle and not in order along the chart.
    const round = plot.hasAttribute('data-round');
    // A horizontal chart's places run down it. The app may turn a chart while the page is up.
    const horizontal = () => plot.hasAttribute('data-horizontal');
    let model = null;
    let active = null;
    let pointer = null;

    // What the chart shows now: its places, and each series' values and points as shares of the plot. The bars are
    // drawn before the lines, and the series are read in the order the legend and the table list them.
    function read() {
        if (model) {
            return model;
        }

        const places = plot.dataset.places === undefined ? [] : plot.dataset.places.split('\n');
        // Where each place is along the chart, as a share from its start: evenly spaced unless the plot has data-at.
        const at = plot.dataset.at === undefined ? places.map((_, place) => (place + 0.5) / places.length)
            : plot.dataset.at.split(' ').map(x => x / 10000);
        const series = [...plot.querySelectorAll('.tb-chart__marks [data-series]')].map(element => ({
            element,
            number: Number(element.dataset.number),
            name: element.dataset.series,
            color: viewOf(plot).getComputedStyle(element).getPropertyValue('--_tb-chart-series'),
            values: element.dataset.values.split('\n'),
            // The tone of each place's mark, where the series gives single marks one.
            tones: element.dataset.tones?.split(' '),
            points: element.dataset.points.split(' ').map(point => {
                const [x, y] = point.split(',');
                return point === '-' ? null : { x: x / 10000, y: y / 10000 };
            }),
        })).sort((one, other) => one.number - other.number);
        model = { places, series, at };
        return model;
    }

    // Right to left, the marks are mirrored, so a share from the left of the chart is one from the right of the box.
    const mirrored = () => viewOf(plot).getComputedStyle(plot).direction === 'rtl';
    const across = x => (mirrored() ? 1 - x : x);

    const element = (name, className, text) => {
        const made = page.createElement(name);
        made.className = className;
        made.textContent = text ?? '';
        return made;
    };

    // The line at a place and every series' value there.
    function show(place, seriesIndex) {
        const { places, series } = read();
        if (!series.length || place < 0 || place >= places.length) {
            hide();
            return;
        }

        // How far along the chart the place is: across the plot, or down the plot of a horizontal chart.
        const where = horizontal() ? read().at[place] : across(read().at[place]);
        crosshair.style.left = horizontal() ? '' : `${where * 100}%`;
        crosshair.style.top = horizontal() ? `${where * 100}%` : '';
        crosshair.hidden = false;
        const rows = series.map((one, index) => {
            const row = element('div', 'tb-chart__tooltip-row');
            row.toggleAttribute('data-active', index === seriesIndex);
            const key = element('span', 'tb-chart__tooltip-key');
            // The key is the color of the mark it stands for: its own tone's, a pie slice's own, or else its
            // series'.
            const tone = one.tones?.[place];
            const slice = round && one.element.querySelector(`[data-place="${place}"]`);
            if (tone && tone !== '-') {
                key.classList.add(`tb-chart__tone--${tone}`);
            } else {
                key.style.setProperty('--_tb-chart-series', slice
                    ? viewOf(plot).getComputedStyle(slice).getPropertyValue('--_tb-chart-series') : one.color);
            }
            row.append(key, element('span', 'tb-chart__tooltip-name', one.name),
                element('strong', 'tb-chart__tooltip-value', one.values[place] || options.noValue));
            return row;
        });
        tooltip.replaceChildren(element('div', 'tb-chart__tooltip-place', places[place]), ...rows);
        // The tooltip sits on the side of the line that has more room: beside it, or under or over the line of a
        // horizontal chart.
        const [before, after] = horizontal() ? ['top', 'bottom'] : ['left', 'right'];
        for (const edge of ['left', 'right', 'top', 'bottom']) {
            tooltip.style[edge] = '';
        }

        tooltip.style[where < 0.5 ? before : after] = `calc(${(where < 0.5 ? where : 1 - where) * 100}% + 0.75rem)`;
        // A pie's tooltip is level with the middle of its slice.
        const middle = round && series[0].points[place];
        if (middle) {
            tooltip.style.top = `${middle.y * 100}%`;
        }

        tooltip.hidden = false;
    }

    function hide() {
        crosshair.hidden = true;
        tooltip.hidden = true;
    }

    const placeAt = event => {
        const box = plot.getBoundingClientRect();
        const share = horizontal() ? (event.clientY - box.top) / box.height
            : across((event.clientX - box.left) / box.width);
        const { at } = read();
        let nearest = 0;
        at.forEach((x, place) => {
            if (Math.abs(x - share) < Math.abs(at[nearest] - share)) {
                nearest = place;
            }
        });
        return nearest;
    };

    // The place a press or the pointer is for: that of the scatter mark under it, since two marks may share a spot
    // along the chart, or else the nearest place along the chart.
    const placeOf = event => {
        const mark = event.target.closest?.('.tb-chart__marks [data-place]');
        return mark ? Number(mark.dataset.place) : round ? -1 : placeAt(event);
    };

    // A press within this many pixels of a point is on that point: a marker's size.
    const onPoint = 6;

    // The series a press at the place is for: the bar under the pointer, or else the one whose point there is
    // nearest, so a thin line or a short bar isn't hard to hit. In a stack of areas, a press inside a band is for
    // that band unless it is on a point. A line takes no press itself, so what is on a point is a matter of
    // distance, the same in every browser.
    const seriesAt = (place, event) => {
        const { series } = read();
        const numbered = selector => {
            const under = event.target.closest?.(selector);
            const found = series.findIndex(one => under && one.number === Number(under.dataset.number));
            return found >= 0 && series[found].points[place] ? found : -1;
        };
        const mark = numbered('.tb-chart__marks [data-series]');
        if (mark >= 0) {
            return mark;
        }

        const box = plot.getBoundingClientRect();
        const away = point => Math.hypot(box.left + across(point.x) * box.width - event.clientX,
            box.top + point.y * box.height - event.clientY);
        let nearest = -1;
        series.forEach((one, index) => {
            const point = one.points[place];
            if (point && (nearest < 0 || away(point) < away(series[nearest].points[place]))) {
                nearest = index;
            }
        });
        const band = numbered('.tb-chart__area[data-number]');
        return band >= 0 && away(series[nearest].points[place]) > onPoint ? band : nearest;
    };

    // One update a frame, for the last place the pointer was at. Off a pie's slices the pointer is at no place, and
    // the tooltip goes back to the slice that has focus, if one does.
    const frame = frames(plot, () => {
        if (pointer && read().places.length) {
            const place = placeOf(pointer);
            if (place < 0 && active && plot.contains(page.activeElement)) {
                show(active.place, active.series);
            } else {
                show(place, null);
            }
        }
    });

    const onPointerMove = event => {
        pointer = event;
        frame.request();
    };

    // A finger leaves the plot as it lifts, before the frame that shows its tooltip, and the tooltip stays until a
    // tap elsewhere.
    const onPointerLeave = event => {
        if (event.pointerType === 'touch') {
            return;
        }

        pointer = null;
        if (!plot.contains(page.activeElement)) {
            hide();
        }
    };

    const onPagePointerDown = event => {
        if (!plot.contains(event.target) && !plot.contains(page.activeElement)) {
            hide();
        }
    };

    const onClick = event => {
        if (!options.activates || !read().places.length) {
            return;
        }

        // The elements focus moves between ignore the pointer, so a click on one comes from a screen reader, has no
        // position, and is for the point that has focus.
        if (proxies.includes(event.target)) {
            if (active) {
                callDotNet(events, 'OnActivate', read().series[active.series].number, active.place, 'keyboard');
            }

            return;
        }

        const place = placeOf(event);
        if (place < 0) {
            return;
        }

        const seriesIndex = seriesAt(place, event);
        if (seriesIndex >= 0) {
            callDotNet(events, 'OnActivate', read().series[seriesIndex].number, place, 'pointer');
        }
    };

    const nameOf = (seriesIndex, place) => {
        const { places, series } = read();
        const one = series[seriesIndex];
        return options.point.replace('{0}', places[place]).replace('{1}', one.name)
            .replace('{2}', one.values[place] || options.noValue).replace('{3}', place + 1)
            .replace('{4}', places.length);
    };

    const putOn = (proxy, seriesIndex, placeIndex) => {
        const point = read().series[seriesIndex].points[placeIndex];
        proxy.setAttribute('role', 'img');
        proxy.setAttribute('aria-label', nameOf(seriesIndex, placeIndex));
        proxy.style.left = `${across(point.x) * 100}%`;
        proxy.style.top = `${point.y * 100}%`;
    };

    // Focus goes back and forth between two elements, each named for its point before it takes focus, so a screen
    // reader hears a point as it would any newly focused element, and a chart needs no element for each point.
    function focusPoint(seriesIndex, placeIndex, proxy) {
        active = { series: seriesIndex, place: placeIndex };
        putOn(proxy, seriesIndex, placeIndex);
        for (const other of proxies) {
            other.tabIndex = other === proxy ? 0 : -1;
        }

        // Taking focus shows the point's tooltip. The element focus left stands for no point now.
        proxy.focus();
        for (const other of proxies) {
            if (other !== proxy) {
                other.removeAttribute('role');
                other.removeAttribute('aria-label');
            }
        }
    }

    // The point focus starts from or returns to: the last one visited if it is still there, or else the first.
    const startPoint = () => {
        const { series } = read();
        if (active && series[active.series]?.points[active.place]) {
            return active;
        }

        for (let seriesIndex = 0; seriesIndex < series.length; seriesIndex++) {
            const first = series[seriesIndex].points.findIndex(point => point);
            if (first >= 0) {
                return { series: seriesIndex, place: first };
            }
        }

        return null;
    };

    // The tab stop is on a point only while there is one, and is named for it even before it takes focus. An element
    // with no point to stand on has no role or name, so nothing unnamed is left for a screen reader to find.
    function settle() {
        const start = startPoint();
        const stop = proxies.find(proxy => proxy.tabIndex === 0) ?? proxies[0];
        for (const proxy of proxies) {
            if (proxy !== stop || !start) {
                proxy.removeAttribute('tabindex');
                proxy.removeAttribute('role');
                proxy.removeAttribute('aria-label');
            }
        }

        active = start;
        if (start) {
            putOn(stop, start.series, start.place);
            stop.tabIndex = 0;
            if (plot.contains(page.activeElement)) {
                show(start.place, start.series);
            }
        } else {
            hide();
        }
    }

    // The next place along the series that has a value, since a gap has nothing to stand on.
    const along = (seriesIndex, from, step) => {
        const points = read().series[seriesIndex].points;
        for (let at = from + step; at >= 0 && at < points.length; at += step) {
            if (points[at]) {
                return at;
            }
        }

        return from;
    };

    // The next series up or down that has a value at the place.
    const upOrDown = (from, placeIndex, step) => {
        const { series } = read();
        for (let at = from + step; at >= 0 && at < series.length; at += step) {
            if (series[at].points[placeIndex]) {
                return at;
            }
        }

        return from;
    };

    // The point is found again as focus arrives, since the chart may have changed size or direction meanwhile.
    const onFocusIn = event => {
        if (proxies.includes(event.target) && active) {
            putOn(event.target, active.series, active.place);
            show(active.place, active.series);
        }
    };

    const onFocusOut = event => {
        if (!plot.contains(event.relatedTarget)) {
            hide();
        }
    };

    const onKeyDown = event => {
        if (!active || !proxies.includes(event.target)) {
            return;
        }

        // Right and Left move along the chart and Down and Up between its series, or the other way around on a
        // horizontal chart, whose places run down it.
        const [right, left] = mirrored() ? ['ArrowLeft', 'ArrowRight'] : ['ArrowRight', 'ArrowLeft'];
        const [forward, back, next, previous] = horizontal() ? ['ArrowDown', 'ArrowUp', right, left]
            : [right, left, 'ArrowDown', 'ArrowUp'];
        let { series: seriesIndex, place: placeIndex } = active;
        // A pie has one series, so Down and Up move around it as the other arrows do.
        if (event.key === forward || (round && event.key === next)) {
            placeIndex = along(seriesIndex, placeIndex, 1);
        } else if (event.key === back || (round && event.key === previous)) {
            placeIndex = along(seriesIndex, placeIndex, -1);
        } else if (event.key === next) {
            seriesIndex = upOrDown(seriesIndex, placeIndex, 1);
        } else if (event.key === previous) {
            seriesIndex = upOrDown(seriesIndex, placeIndex, -1);
        } else if (event.key === 'Home') {
            placeIndex = along(seriesIndex, -1, 1);
        } else if (event.key === 'End') {
            placeIndex = along(seriesIndex, read().places.length, -1);
        } else if ((event.key === 'Enter' || event.key === ' ') && options.activates) {
            event.preventDefault();
            callDotNet(events, 'OnActivate', read().series[seriesIndex].number, placeIndex, 'keyboard');
            return;
        } else {
            return;
        }

        event.preventDefault();
        if (seriesIndex !== active.series || placeIndex !== active.place) {
            focusPoint(seriesIndex, placeIndex, proxies.find(proxy => proxy !== event.target));
        } else {
            show(placeIndex, seriesIndex);
        }
    };

    // Escape hides the tooltip wherever focus is, since the pointer may be what shows it (WCAG 1.4.13). Heard on the
    // plot or on the dialog around the chart, its default is prevented too, so that dialog, or a layer around the
    // plot, stays open. Whatever has focus inside the dialog hears the key first, and may keep it.
    const around = plot.closest('dialog');
    const onEscape = event => {
        if (event.key !== 'Escape' || tooltip.hidden || isComposing(event)) {
            return;
        }

        if (event.currentTarget !== page) {
            event.preventDefault();
        }

        hide();
    };

    // .NET draws the marks again when the data changes, and the tooltip and the tab stop follow. Changes inside the
    // crosshair, the tooltip and the focus elements are this script's own, so they're passed over.
    const own = [crosshair, tooltip, ...proxies];
    const observer = new (viewOf(plot).MutationObserver)(changes => {
        if (changes.some(change => !own.some(mine => mine.contains(change.target)))) {
            // A series hidden or shown moves the ones after it, and the tab stop stays with its own series.
            const number = active && model?.series[active.series]?.number;
            model = null;
            if (active) {
                active.series = read().series.findIndex(one => one.number === number);
            }

            settle();
        }
    });
    observer.observe(plot, {
        subtree: true, childList: true, attributes: true,
        attributeFilter: ['data-places', 'data-at', 'data-points', 'data-values', 'data-tones', 'class', 'style'],
    });

    plot.addEventListener('pointermove', onPointerMove);
    plot.addEventListener('pointerdown', onPointerMove);
    plot.addEventListener('pointerleave', onPointerLeave);
    plot.addEventListener('click', onClick);
    plot.addEventListener('focusin', onFocusIn);
    plot.addEventListener('focusout', onFocusOut);
    plot.addEventListener('keydown', onKeyDown);
    page.addEventListener('pointerdown', onPagePointerDown);
    for (const heard of [plot, around, page]) {
        heard?.addEventListener('keydown', onEscape);
    }

    settle();
    // Marks that the pointer and the keys are followed, which tests wait for.
    plot.toggleAttribute('data-ready', true);

    return {
        update(point, noValue, activates) {
            options = { point, noValue, activates };
            model = null;
            settle();
        },
        dispose() {
            observer.disconnect();
            frame.cancel();
            plot.removeEventListener('pointermove', onPointerMove);
            plot.removeEventListener('pointerdown', onPointerMove);
            plot.removeEventListener('pointerleave', onPointerLeave);
            plot.removeEventListener('click', onClick);
            plot.removeEventListener('focusin', onFocusIn);
            plot.removeEventListener('focusout', onFocusOut);
            plot.removeEventListener('keydown', onKeyDown);
            page.removeEventListener('pointerdown', onPagePointerDown);
            for (const heard of [plot, around, page]) {
                heard?.removeEventListener('keydown', onEscape);
            }

            hide();
            plot.removeAttribute('data-ready');
        },
    };
}
