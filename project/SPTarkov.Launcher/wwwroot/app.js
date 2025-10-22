// Because Photino starts links to external pages inside the app
// Add this script to check a tag has a link to an external site and start it externally ourself.
document.addEventListener('DOMContentLoaded', () => {
    document.body.addEventListener('click', function (e) {
        const target = e.target.closest('a');
        if (!target || !target.href) {
            return;
        }

        if (target.origin !== window.location.origin) {
            console.log('Opening external link:', target.href);
            e.preventDefault();
            window.external.sendMessage('open-external:' + target.href);
        }
    });
});


