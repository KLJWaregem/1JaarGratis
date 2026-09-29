window.scoreboardAnim = (() => {
    let before = new Map();

    function prepare(containerId) {
        before = new Map();
        const container = document.getElementById(containerId);
        if (!container) return;
        for (const el of container.querySelectorAll("[data-id]")) {
            before.set(el.dataset.id, el.getBoundingClientRect());
        }
    }

    function play(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;
        for (const el of container.querySelectorAll("[data-id]")) {
            const from = before.get(el.dataset.id);
            if (!from) continue;

            const to = el.getBoundingClientRect();
            const dx = from.left - to.left;
            const dy = from.top - to.top;
            if (Math.abs(dx) < 1 && Math.abs(dy) < 1) continue;

            if (Math.abs(dx) < 1) slideVertically(el, dy);
            else exitAndReenter(el, dx, dy, to.height);
        }
    }

    function slideVertically(el, dy) {
        el.style.transition = "none";
        el.style.transform = `translateY(${dy}px)`;
        el.getBoundingClientRect();
        requestAnimationFrame(() => {
            el.style.transition = "transform .5s ease";
            el.style.transform = "";
        });
    }

    function exitAndReenter(el, dx, dy, rowHeight) {
        el.style.transition = "none";
        el.style.transform = `translate(${dx}px, ${dy}px)`;
        el.style.opacity = "1";
        el.getBoundingClientRect();

        el.addEventListener("transitionend", function reenter() {
            el.removeEventListener("transitionend", reenter);
            el.style.transition = "none";
            el.style.transform = `translateY(${rowHeight}px)`;
            el.getBoundingClientRect();
            requestAnimationFrame(() => {
                el.style.transition = "transform .3s ease, opacity .3s ease";
                el.style.transform = "";
                el.style.opacity = "1";
            });
        }, { once: true });

        requestAnimationFrame(() => {
            el.style.transition = "transform .25s ease, opacity .25s ease";
            el.style.transform = `translate(${dx}px, ${dy - rowHeight}px)`;
            el.style.opacity = "0";
        });
    }

    return { prepare, play };
})();
