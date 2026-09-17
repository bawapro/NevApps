// Capture-phase horizontal swipe for the section shell.
// MudSwipeArea uses bubbling pointer events, which iOS WKWebView drops over
// inputs/expansion panels/calendar cells, and which Android inner tabs (MudTabs
// overflow, MudToggleGroup) consume before the shell sees them.
window.sectionSwipe = (function () {
    const key = "__sectionSwipe";
    const lockPx = 10;

    function ignore(el) {
        return !!(el && el.closest && el.closest(
            ".mud-overlay,.mud-dialog,.mud-popover,.mud-picker-popover-paper,[data-no-section-swipe]"
        ));
    }

    function attach(el, net, sensitivity) {
        if (!el) return;
        detach(el);

        const state = {
            net: net,
            sensitivity: sensitivity || 80,
            startX: 0,
            startY: 0,
            tracking: false,
            lock: null,
            swiped: false
        };

        const onStart = function (e) {
            if (e.touches.length !== 1 || ignore(e.target)) {
                state.tracking = false;
                return;
            }
            const t = e.touches[0];
            state.startX = t.clientX;
            state.startY = t.clientY;
            state.tracking = true;
            state.lock = null;
            state.swiped = false;
        };

        const onMove = function (e) {
            if (!state.tracking || e.touches.length !== 1) return;
            const t = e.touches[0];
            const dx = t.clientX - state.startX;
            const dy = t.clientY - state.startY;
            if (!state.lock) {
                if (Math.abs(dx) < lockPx && Math.abs(dy) < lockPx) return;
                // Prefer vertical when close so scrolling is not stolen.
                state.lock = Math.abs(dx) > Math.abs(dy) * 1.15 ? "h" : "v";
            }
            if (state.lock === "h" && e.cancelable) e.preventDefault();
        };

        const onEnd = function (e) {
            if (!state.tracking) return;
            const wasH = state.lock === "h";
            state.tracking = false;
            state.lock = null;
            if (!wasH) return;
            const t = e.changedTouches && e.changedTouches[0];
            if (!t) return;
            const dx = t.clientX - state.startX;
            if (Math.abs(dx) < state.sensitivity) return;
            state.swiped = true;
            state.net.invokeMethodAsync("OnJsSwipe", dx < 0 ? 1 : -1);
        };

        const onClick = function (e) {
            if (!state.swiped) return;
            e.preventDefault();
            e.stopPropagation();
            state.swiped = false;
        };

        el.addEventListener("touchstart", onStart, { capture: true, passive: true });
        el.addEventListener("touchmove", onMove, { capture: true, passive: false });
        el.addEventListener("touchend", onEnd, { capture: true, passive: true });
        el.addEventListener("touchcancel", onEnd, { capture: true, passive: true });
        el.addEventListener("click", onClick, true);

        el[key] = { onStart: onStart, onMove: onMove, onEnd: onEnd, onClick: onClick };
    }

    function detach(el) {
        const s = el && el[key];
        if (!s) return;
        el.removeEventListener("touchstart", s.onStart, true);
        el.removeEventListener("touchmove", s.onMove, true);
        el.removeEventListener("touchend", s.onEnd, true);
        el.removeEventListener("touchcancel", s.onEnd, true);
        el.removeEventListener("click", s.onClick, true);
        delete el[key];
    }

    return { attach: attach, detach: detach };
})();
