// Because Photino starts links to external pages inside the app
// Add this script to check a tag has a link to an external site and start it externally ourselves.
document.addEventListener('DOMContentLoaded', () => {
    document.body.addEventListener('click', function (e) {
        const target = e.target.closest('a');
        if (!target || !target.href) {
            return;
        }

        if (target.origin !== window.location.origin) {
            console.log('Opening external link:', target.href);
            e.preventDefault();

            // This complains about sendMessage, it does work, I currently don't know an alternative
            window.external.sendMessage('open-external:' + target.href);
        }
    });
});

// Stop allowing middle mouse click from opening windows etc.
// It didn't open them in desktop browser,
// It was a webview that cant connect to the app
document.addEventListener('auxclick', function (e) {
        // 1 = middle mouse button
        // 3 = Back button and forward
        // console.log(e.button);
        if (e.button === 1 || e.button === 0) {
            e.preventDefault();
            e.stopPropagation();
        }
    }, true
);

// Navigates to the previous page in history obviously
function navigateBack() {
    window.history.back();
}
