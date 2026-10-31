// Vertical pull-to-refresh for the pane / detail scrollers.
// Lives alongside section-swipe: only arms on a downward pull while at top,
// and never preventDefault on a horizontal lock.
window.pullToRefresh = (function () {
    const key = "__pullToRefresh";
    const lockPx = 10;

    function ignore(el) {
        return !!(el && el.closest && el.closest(
            ".mud-overlay,.mud-dialog,.mud-popover,.mud-picker-popover-paper,[data-no-pull-refresh]"
        ));
    }

    function findScroller(el) {
        let n = el;
        while (n && n !== document.body) {
            if (n.classList && (n.classList.contains("section-pane") || n.classList.contains("app-main-detail")))
                return n;
            n = n.parentElement;
        }
        return el;
    }

    function innerScroller(target, pane) {
        let n = target;
        while (n && n !== pane && n !== document.body) {
            if (n.scrollHeight > n.clientHeight + 1) {
                const oy = getComputedStyle(n).overflowY;
                if (oy === "auto" || oy === "scroll" || oy === "overlay")
                    return n;
            }
            n = n.parentElement;
        }
        return pane;
    }

    function atTop(node) {
        return !node || node.scrollTop <= 1;
    }

    function setHeight(indicator, px) {
        if (!indicator) return;
        indicator.style.height = Math.max(0, px) + "px";
    }

    function attach(root, net, threshold) {
        if (!root) return;
        detach(root);

        const indicator = root.querySelector("[data-ptr-indicator]");
        const scroller = findScroller(root);
        const armAt = threshold || 70;

        const state = {
            net: net,
            scroller: scroller,
            indicator: indicator,
            startX: 0,
            startY: 0,
            tracking: false,
            lock: null,
            pulling: false,
            refreshing: false,
            armed: false,
            dy: 0
        };

        const onStart = function (e) {
            if (state.refreshing || e.touches.length !== 1 || ignore(e.target)) {
                state.tracking = false;
                return;
            }
            const inner = innerScroller(e.target, scroller);
            if (!atTop(scroller) || !atTop(inner)) {
                state.tracking = false;
                return;
            }
            const t = e.touches[0];
            state.startX = t.clientX;
            state.startY = t.clientY;
            state.tracking = true;
            state.lock = null;
            state.pulling = false;
            state.armed = false;
            state.dy = 0;
        };

        const onMove = function (e) {
            if (!state.tracking || state.refreshing || e.touches.length !== 1) return;
            const t = e.touches[0];
            const dx = t.clientX - state.startX;
            const dy = t.clientY - state.startY;
            if (!state.lock) {
                if (Math.abs(dx) < lockPx && Math.abs(dy) < lockPx) return;
                state.lock = Math.abs(dx) > Math.abs(dy) * 1.15 ? "h" : "v";
            }
            if (state.lock !== "v" || dy <= 0) {
                if (state.pulling) {
                    state.pulling = false;
                    state.armed = false;
                    root.classList.remove("is-pulling", "is-armed");
                    setHeight(indicator, 0);
                }
                return;
            }

            const inner = innerScroller(e.target, scroller);
            if (!atTop(scroller) || !atTop(inner)) return;

            state.pulling = true;
            state.dy = dy;
            const height = Math.min(dy * 0.42, 96);
            state.armed = height >= armAt * 0.42;
            root.classList.add("is-pulling");
            root.classList.toggle("is-armed", state.armed);
            if (indicator) {
                indicator.style.setProperty("--ptr-rot", (height / 96 * 360) + "deg");
            }
            setHeight(indicator, height);
            if (e.cancelable) e.preventDefault();
        };

        const onEnd = function () {
            if (!state.tracking) return;
            const shouldRefresh = state.pulling && state.armed && !state.refreshing;
            state.tracking = false;
            state.lock = null;
            state.pulling = false;
            state.dy = 0;

            if (!shouldRefresh) {
                state.armed = false;
                root.classList.remove("is-pulling", "is-armed");
                setHeight(indicator, 0);
                return;
            }

            state.refreshing = true;
            state.armed = false;
            root.classList.remove("is-pulling", "is-armed");
            root.classList.add("is-refreshing");
            setHeight(indicator, 56);
            state.net.invokeMethodAsync("OnJsRefresh").catch(function () {
                state.refreshing = false;
                root.classList.remove("is-refreshing");
                setHeight(indicator, 0);
            });
        };

        scroller.addEventListener("touchstart", onStart, {capture: true, passive: true});
        scroller.addEventListener("touchmove", onMove, {capture: true, passive: false});
        scroller.addEventListener("touchend", onEnd, {capture: true, passive: true});
        scroller.addEventListener("touchcancel", onEnd, {capture: true, passive: true});

        root[key] = {
            onStart: onStart,
            onMove: onMove,
            onEnd: onEnd,
            scroller: scroller,
            indicator: indicator,
            state: state
        };
    }

    function setRefreshing(root, on) {
        const s = root && root[key];
        if (!s) return;
        s.state.refreshing = !!on;
        if (on) {
            root.classList.add("is-refreshing");
            root.classList.remove("is-pulling", "is-armed");
            setHeight(s.indicator, 56);
        } else {
            root.classList.remove("is-refreshing", "is-pulling", "is-armed");
            setHeight(s.indicator, 0);
            s.state.refreshing = false;
        }
    }

    function detach(root) {
        const s = root && root[key];
        if (!s) return;
        s.scroller.removeEventListener("touchstart", s.onStart, true);
        s.scroller.removeEventListener("touchmove", s.onMove, true);
        s.scroller.removeEventListener("touchend", s.onEnd, true);
        s.scroller.removeEventListener("touchcancel", s.onEnd, true);
        delete root[key];
    }

    return {attach: attach, detach: detach, setRefreshing: setRefreshing};
})();
