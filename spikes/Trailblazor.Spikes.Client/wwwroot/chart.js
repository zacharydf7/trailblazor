// Spike E: follows the pointer and the keys over a chart that .NET has already drawn. It reads every point from the
// markup, so nothing here asks the server for anything; .NET hears only Enter, Space or a click on a point.
export function attachChart(plot, dotNetReference) {
    const view = plot.ownerDocument.defaultView;
    const marks = plot.querySelector('.spike-chart__marks');
    const crosshair = plot.querySelector('.spike-chart__crosshair');
    const tooltip = plot.querySelector('.spike-chart__tooltip');
    const proxies = [...plot.querySelectorAll('.spike-chart__focus')];
    let model = null;
    let active = null;
    let frame = 0;
    let pointer = null;
    let firstMove = 0;
    const timings = [];

    // .NET redraws the marks when the data changes; the next move reads them again.
    const observer = new view.MutationObserver(() => { model = null; });
    observer.observe(marks, { subtree: true, childList: true, attributes: true });

    function read() {
        if (model) {
            return model;
        }

        const series = [...marks.querySelectorAll('[data-series]')].map(element => ({
            index: Number(element.dataset.series),
            name: element.dataset.name,
            color: view.getComputedStyle(element).getPropertyValue('--_series'),
            values: element.dataset.values.split('|'),
            points: element.localName === 'path'
                ? pathPoints(element.getAttribute('d'))
                : [...element.children].map(bar => ({
                    x: (parseFloat(bar.getAttribute('x')) + parseFloat(bar.getAttribute('width')) / 2) / 100,
                    y: parseFloat(bar.getAttribute('y')) / 100,
                })),
        }));
        model = { labels: plot.dataset.x.split('|'), series };
        return model;
    }

    // A line's path starts at a point and gives each later one as its distance from the one before.
    function pathPoints(path) {
        let x = 0;
        let y = 0;
        return [...path.matchAll(/[Ml](-?\d+) (-?\d+)/g)].map(match => {
            x = match[0][0] === 'M' ? Number(match[1]) : x + Number(match[1]);
            y = match[0][0] === 'M' ? Number(match[2]) : y + Number(match[2]);
            return { x: x / 10000, y: y / 10000 };
        });
    }

    const mirrored = () => view.getComputedStyle(plot).direction === 'rtl';
    const across = x => `${(mirrored() ? 1 - x : x) * 100}%`;

    function show(place, seriesIndex) {
        const { labels, series } = read();
        if (!series.length) {
            return;
        }

        // Side-by-side bars sit beside the middle of their place, so the line is drawn at the place, not the bar.
        const x = (place + 0.5) / labels.length;
        crosshair.style.left = across(x);
        crosshair.hidden = false;

        const heading = plot.ownerDocument.createElement('div');
        heading.className = 'spike-chart__tooltip-heading';
        heading.textContent = labels[place];
        const rows = series.map((one, index) => {
            const row = plot.ownerDocument.createElement('div');
            row.className = 'spike-chart__tooltip-row';
            row.toggleAttribute('data-active', index === seriesIndex);
            const key = plot.ownerDocument.createElement('span');
            key.className = 'spike-chart__tooltip-key';
            key.style.background = one.color;
            const name = plot.ownerDocument.createElement('span');
            name.textContent = one.name;
            const value = plot.ownerDocument.createElement('strong');
            value.textContent = one.values[place];
            row.append(key, name, value);
            return row;
        });
        tooltip.replaceChildren(heading, ...rows);
        const shownAt = mirrored() ? 1 - x : x;
        tooltip.style.left = shownAt < 0.5 ? `calc(${shownAt * 100}% + 12px)` : '';
        tooltip.style.right = shownAt < 0.5 ? '' : `calc(${(1 - shownAt) * 100}% + 12px)`;
        tooltip.hidden = false;
    }

    function hide() {
        crosshair.hidden = true;
        tooltip.hidden = true;
    }

    function nearestPlace(clientX) {
        const box = plot.getBoundingClientRect();
        const fraction = (clientX - box.left) / box.width;
        const { labels } = read();
        const place = Math.floor((mirrored() ? 1 - fraction : fraction) * labels.length);
        return Math.max(0, Math.min(labels.length - 1, place));
    }

    function nearestSeries(place, clientY) {
        const box = plot.getBoundingClientRect();
        const y = (clientY - box.top) / box.height;
        let nearest = 0;
        read().series.forEach((one, index, all) => {
            if (Math.abs(one.points[place].y - y) < Math.abs(all[nearest].points[place].y - y)) {
                nearest = index;
            }
        });
        return nearest;
    }

    const onPointerMove = event => {
        pointer = event;
        firstMove ||= event.timeStamp;
        frame ||= view.requestAnimationFrame(frameTime => {
            frame = 0;
            const started = view.performance.now();
            show(nearestPlace(pointer.clientX), null);
            const ended = view.performance.now();
            // From the first move this update answers: to the frame's start, to the end of the work, and the work.
            timings.push([frameTime - firstMove, ended - firstMove, ended - started]);
            firstMove = 0;
        });
    };

    const onPointerLeave = () => {
        if (!plot.contains(plot.ownerDocument.activeElement)) {
            hide();
        }
    };

    const onClick = event => {
        const place = nearestPlace(event.clientX);
        const nearest = read().series[nearestSeries(place, event.clientY)];
        dotNetReference.invokeMethodAsync('Activate', nearest.index, place);
    };

    // Focus goes back and forth between two elements, each named for its point before it takes focus, so a screen
    // reader hears a point as it would any newly focused element, and the chart needs no element per point.
    function focusPoint(seriesIndex, place, proxy) {
        const { labels, series } = read();
        const one = series[seriesIndex];
        active = { series: seriesIndex, place };
        proxy.setAttribute('aria-label',
            `${labels[place]}, ${one.name}: ${one.values[place]}, ${place + 1} of ${labels.length}`);
        proxy.style.left = across(one.points[place].x);
        proxy.style.top = `${one.points[place].y * 100}%`;
        for (const other of proxies) {
            other.tabIndex = other === proxy ? 0 : -1;
        }

        if (plot.ownerDocument.activeElement !== proxy) {
            proxy.focus();
        }

        show(place, seriesIndex);
    }

    const onFocusIn = event => {
        if (proxies.includes(event.target) && !proxies.includes(event.relatedTarget)) {
            const { series } = read();
            if (series.length) {
                const at = active && active.series < series.length ? active : { series: 0, place: 0 };
                focusPoint(at.series, Math.min(at.place, series[at.series].points.length - 1), event.target);
            }
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

        const { labels, series } = read();
        const forward = mirrored() ? 'ArrowLeft' : 'ArrowRight';
        const back = mirrored() ? 'ArrowRight' : 'ArrowLeft';
        let { series: seriesIndex, place } = active;
        if (event.key === forward) {
            place = Math.min(labels.length - 1, place + 1);
        } else if (event.key === back) {
            place = Math.max(0, place - 1);
        } else if (event.key === 'ArrowDown') {
            seriesIndex = Math.min(series.length - 1, seriesIndex + 1);
        } else if (event.key === 'ArrowUp') {
            seriesIndex = Math.max(0, seriesIndex - 1);
        } else if (event.key === 'Home') {
            place = 0;
        } else if (event.key === 'End') {
            place = labels.length - 1;
        } else if (event.key === 'Escape') {
            hide();
            return;
        } else if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            dotNetReference.invokeMethodAsync('Activate', series[seriesIndex].index, place);
            return;
        } else {
            return;
        }

        event.preventDefault();
        if (seriesIndex !== active.series || place !== active.place) {
            focusPoint(seriesIndex, place, proxies.find(proxy => proxy !== event.target));
        } else {
            show(place, seriesIndex);
        }
    };

    proxies[0].tabIndex = 0;
    plot.addEventListener('pointermove', onPointerMove);
    plot.addEventListener('pointerleave', onPointerLeave);
    plot.addEventListener('click', onClick);
    plot.addEventListener('focusin', onFocusIn);
    plot.addEventListener('focusout', onFocusOut);
    plot.addEventListener('keydown', onKeyDown);

    // Measurement hook for the spike's tests.
    plot.spikeTimings = timings;

    return {
        dispose() {
            observer.disconnect();
            view.cancelAnimationFrame(frame);
            plot.removeEventListener('pointermove', onPointerMove);
            plot.removeEventListener('pointerleave', onPointerLeave);
            plot.removeEventListener('click', onClick);
            plot.removeEventListener('focusin', onFocusIn);
            plot.removeEventListener('focusout', onFocusOut);
            plot.removeEventListener('keydown', onKeyDown);
        },
    };
}
