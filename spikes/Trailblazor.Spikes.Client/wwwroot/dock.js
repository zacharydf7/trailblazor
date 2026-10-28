// Keeps each panel host sized and positioned over the slot of the group that shows it. Hosts never move in the DOM.
export function attachDock(dock, dotNetReference) {
    const layout = dock.querySelector('.dock-layout');

    const resizeObserver = new ResizeObserver(() => positionHosts(dock));
    const mutationObserver = new MutationObserver(() => {
        observeSlots();
        positionHosts(dock);
    });
    resizeObserver.observe(dock);
    mutationObserver.observe(layout, { subtree: true, childList: true, attributes: true });
    observeSlots();
    positionHosts(dock);
    dock.addEventListener('pointerdown', onSplitterPointerDown);

    // Measurement hooks; window-level so Playwright can drive the spike without drag-and-drop UI.
    let sampling = null;
    window.dockSpike = {
        move: (panelId, groupId) => dotNetReference.invokeMethodAsync('Move', panelId, groupId),
        activate: panelId => dotNetReference.invokeMethodAsync('Activate', panelId),
        measureMove: (panelId, groupId) =>
            measureMove(dock, () => dotNetReference.invokeMethodAsync('Move', panelId, groupId), panelId, groupId),
        stats: () => dotNetReference.invokeMethodAsync('GetStats'),
        layoutCallCount: () => dotNetReference.invokeMethodAsync('GetLayoutCallCount'),
        // Observers run in creation order before paint, so this one sees the hosts after positionHosts moved them.
        startSampling: () => {
            sampling = { worst: 0 };
            sampling.observer = new ResizeObserver(() => {
                sampling.worst = Math.max(sampling.worst, measureMisalignment(dock));
            });
            sampling.observer.observe(dock);
            for (const slot of layout.querySelectorAll('.dock-slot')) {
                sampling.observer.observe(slot);
            }
        },
        stopSampling: () => {
            sampling.observer.disconnect();
            return sampling.worst;
        },
    };

    return {
        dispose() {
            resizeObserver.disconnect();
            mutationObserver.disconnect();
            dock.removeEventListener('pointerdown', onSplitterPointerDown);
            delete window.dockSpike;
        },
    };

    function observeSlots() {
        for (const slot of layout.querySelectorAll('.dock-slot')) {
            resizeObserver.observe(slot);
        }
    }
}

function positionHosts(dock) {
    const dockRect = dock.getBoundingClientRect();
    for (const host of dock.querySelectorAll('.dock-host')) {
        const slot = findSlot(dock, host.dataset.panel);
        if (slot && slot.checkVisibility()) {
            const rect = slot.getBoundingClientRect();
            host.style.left = `${rect.left - dockRect.left}px`;
            host.style.top = `${rect.top - dockRect.top}px`;
            host.style.width = `${rect.width}px`;
            host.style.height = `${rect.height}px`;
            host.style.zIndex = slot.closest('.dock-floating, .dock-flyout') ? '3' : '1';
            host.classList.remove('hidden');
        } else {
            host.classList.add('hidden');
        }
    }
}

function findSlot(dock, panelId) {
    return dock.querySelector(`.dock-slot[data-active-panel="${panelId}"]`);
}

// Splitter drag stays in JS; .NET hears nothing until a layout change is committed.
function onSplitterPointerDown(event) {
    const splitter = event.target.closest('.dock-splitter');
    if (!splitter) {
        return;
    }

    const vertical = splitter.parentElement.classList.contains('vertical');
    const before = splitter.previousElementSibling;
    const after = splitter.nextElementSibling;
    const start = vertical ? event.clientY : event.clientX;
    const beforeSize = vertical ? before.offsetHeight : before.offsetWidth;
    const afterSize = vertical ? after.offsetHeight : after.offsetWidth;
    splitter.setPointerCapture(event.pointerId);

    const onMove = moveEvent => {
        const delta = (vertical ? moveEvent.clientY : moveEvent.clientX) - start;
        before.style.flex = `0 0 ${beforeSize + delta}px`;
        after.style.flex = `0 0 ${afterSize - delta}px`;
    };
    const onUp = () => {
        splitter.removeEventListener('pointermove', onMove);
        splitter.removeEventListener('pointerup', onUp);
    };
    splitter.addEventListener('pointermove', onMove);
    splitter.addEventListener('pointerup', onUp);
}

// Largest gap, in px, between any visible host and its slot. A visible host without a slot counts as 9999.
function measureMisalignment(dock) {
    let worst = 0;
    for (const host of dock.querySelectorAll('.dock-host:not(.hidden)')) {
        const slot = findSlot(dock, host.dataset.panel);
        if (!slot) {
            return 9999;
        }

        const a = host.getBoundingClientRect();
        const b = slot.getBoundingClientRect();
        worst = Math.max(worst, Math.abs(a.left - b.left), Math.abs(a.top - b.top), Math.abs(a.width - b.width),
            Math.abs(a.height - b.height));
    }

    return worst;
}

// Resolves after the next frame has been painted.
function afterNextPaint() {
    return new Promise(resolve => requestAnimationFrame(() => {
        const channel = new MessageChannel();
        channel.port1.onmessage = () => resolve();
        channel.port2.postMessage(null);
    }));
}

// Counts painted frames from the move call until the target group shows the panel and every host sits on its slot.
async function measureMove(dock, move, panelId, groupId) {
    const longTasks = [];
    const observer = new PerformanceObserver(list => longTasks.push(...list.getEntries().map(entry => entry.duration)));
    observer.observe({ type: 'longtask' });

    const done = move();
    let frames = 0;
    while (frames < 120) {
        await afterNextPaint();
        frames++;
        const slot = dock.querySelector(`.dock-slot[data-group-slot="${groupId}"]`);
        const host = dock.querySelector(`.dock-host[data-panel="${panelId}"]`);
        const shown = slot?.dataset.activePanel === panelId && !host.classList.contains('hidden');
        if (shown && measureMisalignment(dock) <= 1) {
            break;
        }
    }

    await done;
    observer.disconnect();
    return { frames, longestTask: Math.max(0, ...longTasks) };
}
