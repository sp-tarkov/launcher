// Opens external links in the system browser and guards webview-specific click behaviour.
//
// Photino would otherwise open links to other pages inside the app window. We intercept those clicks and route the URL to the .NET
// BrowserBridge (registered by MainLayout once the app has rendered), which hands it to the OS browser instead.

// Bridge to the .NET BrowserBridge — a DotNetObjectReference set by MainLayout.
let browserBridge = null;

window.registerBrowserBridge = function (bridge) {
    browserBridge = bridge;
};

function openExternal(url) {
    browserBridge?.invokeMethodAsync('OpenExternal', url);
}

// Open clicks on links to another origin in the system browser instead of navigating the webview to them.
document.body.addEventListener('click', function (e) {
    const target = e.target.closest('a');
    if (!target || !target.href) {
        return;
    }

    if (target.origin !== window.location.origin) {
        e.preventDefault();
        openExternal(target.href);
    }
});

// Middle-clicking used to open a new webview window that couldn't connect back to the app, so suppress it.
document.addEventListener('auxclick', function (e) {
    if (e.button === 1) { // Middle button
        e.preventDefault();
        e.stopPropagation();
    }
}, true);
