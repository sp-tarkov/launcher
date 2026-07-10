// Loader for the launcher's front-end scripts. Each file below owns a single, targeted concern; add new scripts to this list.
// Paths are absolute from the site root (wwwroot/), so the targeted scripts live in wwwroot/js/scripts/.
const scriptsToLoad = [
    '/js/scripts/externalLinks.js',
    '/js/scripts/navigateBack.js',
    '/js/scripts/navFade.js',
    '/js/scripts/userTabset.js',
];
scriptsToLoad.forEach(function (path) {
    const script = document.createElement('script');
    script.src = path;
    script.async = false; // Execute in listed order.
    document.head.appendChild(script);
});
