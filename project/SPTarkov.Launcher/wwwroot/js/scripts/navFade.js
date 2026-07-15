// Fades the top and bottom edges of the nav rail's scroll region to signal overflow, since its native scrollbar is hidden.
//
// Toggles `is-scrollable-up` / `is-scrollable-down` on the `.nav-scroll` container based on its scroll position. The CSS turns those
// classes into the corresponding edge-fade mask. The rail mounts after Blazor renders and its tile list changes as mod pages load, so we
// watch the DOM and bind to whichever `.nav-scroll` element is currently present.

(function () {
    const SELECTOR = '.nav-scroll';

    function update(el) {
        const max = el.scrollHeight - el.clientHeight;
        // A 1px tolerance keeps the fades from flickering at the edges.
        el.classList.toggle('is-scrollable-up', el.scrollTop > 1);
        el.classList.toggle('is-scrollable-down', el.scrollTop < max - 1);
    }

    // The element we've wired listeners to. Re-bind whenever Blazor swaps in a fresh one.
    let bound = null;
    let resizeObserver = null;

    function bind(el) {
        // Same element still present. Just refresh the fades.
        if (el === bound) {
            if (el) {
                update(el);
            }
            return;
        }

        bound = el;
        resizeObserver?.disconnect();
        resizeObserver = null;

        if (!el) {
            return;
        }

        el.addEventListener('scroll', () => update(el), { passive: true });

        // Fires when the rail's height changes (window resize) and overflow appears or disappears.
        resizeObserver = new ResizeObserver(() => update(el));
        resizeObserver.observe(el);

        update(el);
    }

    // Rebind on any DOM change so we catch the rail mounting and its tiles being added or removed.
    new MutationObserver(() => bind(document.querySelector(SELECTOR))).observe(document.body, {
        childList: true,
        subtree: true,
    });

    bind(document.querySelector(SELECTOR));
})();
