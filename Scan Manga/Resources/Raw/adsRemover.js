(function() {
    const selectors = [
        'div.BDPFGA[data-type="_mgwidget"]',
        'div.PUBFUTURE',
        'div[data-unit]',
        'div#teads_inread',
        'script[src*="richardghain.com"]',
        'script[src*="adschill.com"]',
        'script[src*="acscdn.com"]'
    ];

    // --- 1. Suppression des publicités ---
    function removeAds() {
        selectors.forEach(sel => {
            document.querySelectorAll(sel).forEach(el => el.remove());
        });

        const container = document.querySelector('.reader_container');

        while (container.firstElementChild) {
            const first = container.firstElementChild;

            if (first.tagName.toLowerCase() === 'div' && first.classList.contains('reader_view')) {
                break;
            } else {
                container.removeChild(first);
            }
        }

        const html = document.documentElement;
        Array.from(html.children).forEach(child => {
            if (child.tagName.toLowerCase() !== 'head' && child.tagName.toLowerCase() !== 'body') {
                html.removeChild(child);
            }
        });

        document.querySelectorAll('in-page-message, iframe').forEach(e => {
            if (e.shadowRoot) e.shadowRoot.innerHTML = '';
            e.remove();
        });
    }

    removeAds();
    const adObserver = new MutationObserver(removeAds);
    adObserver.observe(document.body, { childList: true, subtree: true });

    // --- 2. Mise en couleur des chapitres visités ---
    const style = document.createElement('style');
    style.textContent = `
        span.i a.visited,
        a.l_read.visited,
        div.top a.atop.visited {
            color: #e0a19d !important;
        }
    `;
    document.head.appendChild(style);

    var visited = {visitedJoined};
    var anchors = document.querySelectorAll('span.i a, a.l_read, div.top a.atop');
    anchors.forEach(function(link) {
        if (visited.includes(link.href)) {
            link.classList.add('visited');
        }
    });
})();