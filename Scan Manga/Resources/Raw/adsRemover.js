(function () {
    // Sélecteurs pour masquage CSS immédiat (Visuel)
    const adSelectors = [
        'div.BDPFGA[data-type="_mgwidget"]',
        'div.PUBFUTURE',
        'div[data-unit]',
        'div#teads_inread',
        'script[src*="richardghain.com"]',
        'script[src*="adschill.com"]',
        'script[src*="acscdn.com"]'
    ];


    const styleAds = document.createElement('style');
    styleAds.textContent = adSelectors.join(',') + ' { display: none !important; }';
    document.head.appendChild(styleAds);

    // Sélecteurs supplémentaires pour suppression JS (Scripts, etc.)
    const scriptsToRemove = [
        'script[src*="richardghain.com"]',
        'script[src*="adschill.com"]',
        'script[src*="acscdn.com"]'
    ];

    // Style pour les liens visités
    const styleColors = document.createElement('style');
    styleColors.textContent = `
        span.i a.visited,
        a.l_read.visited,
        div.top a.atop.visited {
            color: #e0a19d !important;
        }
    `;
    document.head.appendChild(styleColors);

    const allJsSelectors = [...adSelectors, ...scriptsToRemove].join(',');

    function cleanDOM() {
        // Suppression simple des pubs ciblées
        const elements = document.querySelectorAll(allJsSelectors).forEach(el => el.remove());

        // Nettoyage intelligent du reader_container
        const container = document.querySelector('.reader_container');
        if (container) {
            let child = container.firstElementChild;
            while (child) {
                const next = child.nextElementSibling;
                // Si ce n'est pas la div de lecture, on supprime
                if (!(child.tagName === 'DIV' && child.classList.contains('reader_view'))) {
                    child.remove();
                } else {
                    // On a trouvé le contenu, on peut arrêter si les pubs sont toujours avant
                    break;
                }
                child = next;
            }
        }

        // Nettoyage racine HTML (hors head/body)
        const html = document.documentElement;
        let htmlChild = html.lastElementChild; // On part de la fin pour éviter les bugs
        while (htmlChild) {
            const prev = htmlChild.previousElementSibling;
            const tag = htmlChild.tagName;
            if (tag !== 'HEAD' && tag !== 'BODY') {
                htmlChild.remove();
            }
            htmlChild = prev;
        }

        // Nettoyage Shadow DOM
        const shadowHosts = document.querySelectorAll('in-page-message, iframe');
        shadowHosts.forEach(host => {
            if (host.shadowRoot) {
                host.shadowRoot.innerHTML = '';
            }
            host.remove();
        });
    }

    // Le {visitedJoined} sera remplacé par le tableau JSON brut, ex: ["url1", "url2"]
    var visitedSet = new Set({visitedJoined});

    function colorizeLinks() {
        var anchors = document.querySelectorAll('span.i a:not(.visited), a.l_read:not(.visited), div.top a.atop:not(.visited)');
        anchors.forEach(function (link) {
            if (visitedSet.has(link.href)) {
                link.classList.add('visited');
            }
        });
    }

    let timeout;
    const adObserver = new MutationObserver((mutations) => {
        if (timeout) clearTimeout(timeout);
        timeout = setTimeout(() => {
            cleanDOM();
            colorizeLinks(); // On relance la coloration au cas où de nouveaux liens (infinite scroll) apparaissent
        }, 50);
    });

    // Exécution initiale
    cleanDOM();
    colorizeLinks();

    // Démarrage de l'observer
    adObserver.observe(document.body, { childList: true, subtree: true });
})();