window.scrollToTopSmooth = function () {
    window.scrollTo({
        top: 0,
        behavior: 'smooth'
    });
}

window.toggleScrollButton = function () {
    var scrollButton = document.getElementById('scrollTopBtn');
    if (!scrollButton) {
        return;
    }
    if (document.body.scrollTop > 20 || document.documentElement.scrollTop > 20) {
        scrollButton.style.display = 'flex';
    } else {
        scrollButton.style.display = 'none';
    }
};

window.onscroll = function () {
    window.toggleScrollButton();
};

window.toggleScrollButton();

// Because Photino starts links to external pages inside the app
// Add this script to check a tag has a link to an external site and start it externally ourself.
document.addEventListener('DOMContentLoaded', () => {
    document.body.addEventListener('click', function (e) {
        const target = e.target.closest('a');
        if (!target || !target.href) {
            return;
        }

        if (!target.href.startsWith('#') && !target.href.startsWith('https://localhost') && !target.href.startsWith('http://localhost')) {
            console.log('Opening external link:', target.href);
            e.preventDefault();
            window.external.sendMessage('open-external:' + target.href);
        }
    });
});
