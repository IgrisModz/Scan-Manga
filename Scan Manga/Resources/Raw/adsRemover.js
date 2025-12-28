(function () {
    const enableAdBlock = {isAdBlockEnabled};
    const enableColoration = {isHistoryEnabled};

    // Le {visitedJoined} sera remplacé par le tableau JSON brut, ex: ["url1", "url2"]
    var visitedSet = new Set({visitedJoined});

    if (enableAdBlock) {
        // Sélecteurs pour masquage CSS immédiat (Visuel)
        const adSelectors = [
            'div.BDPFGA[data-type="_mgwidget"]',
            'div.PUBFUTURE',
            'div[data-unit]',
            'div#teads_inread',
            'div#ayads-html',
            'script[src*="richardghain.com"]',
            'script[src*="adschill.com"]',
            'script[src*="acscdn.com"]'
        ];


        const styleAds = document.createElement('style');
        styleAds.textContent = adSelectors.join(',') + ' { display: none !important; }';
        document.head.appendChild(styleAds);
    }

    if (enableColoration) {
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
    }


    function cleanDOM() {
        if (!enableAdBlock) return;

        document.body.className = "";

        const adJsSelectors = [
            'div.BDPFGA', 'div.PUBFUTURE', 'div[data-unit]', 'div#teads_inread', 'div#ayads-html',
            'script[src*="richardghain.com"]', 'script[src*="adschill.com"]', 'script[src*="acscdn.com"]'
        ].join(',');

        document.querySelectorAll(adJsSelectors).forEach(el => el.remove());

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

    function colorizeLinks() {
        if (!enableColoration) return;

        const selectors = 'span.i a:not(.visited), a.l_read:not(.visited), div.top a.atop:not(.visited)';
        var anchors = document.querySelectorAll(selectors);
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
            if (enableAdBlock) cleanDOM();
            if (enableColoration) colorizeLinks(); // On relance la coloration au cas où de nouveaux liens (infinite scroll) apparaissent
        }, 60);
    });

    // Exécution initiale
    if (enableAdBlock) cleanDOM();
    if (enableColoration) colorizeLinks();

    // Démarrage de la surveillance si l'une des options est active
    if (enableAdBlock || enableColoration) {
        adObserver.observe(document.body || document.documentElement, {
            childList: true,
            subtree: true,
            attributes: true // Ajouté pour surveiller les changements de classe sur le body
        });
    }
})();