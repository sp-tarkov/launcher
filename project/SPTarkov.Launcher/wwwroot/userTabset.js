function setTabContent() {
    // Find all elements with the "tabset" class within the user-markdown container.
    const userMarkdownArea = document.querySelector(".user-markdown");
    if (!userMarkdownArea) {
        return;
    }

    const tabsets = userMarkdownArea.querySelectorAll(".tabset");

    tabsets.forEach((tabset, tabsetIndex) => {
        const panels = tabset.querySelectorAll(".tab-panel");
        if (panels.length === 0) return;

        // Create a container for the navigation links.
        const navigation = document.createElement("div");
        navigation.className = "tab-navigation";
        navigation.setAttribute("role", "tablist");

        // Add a unique identifier for potentially multiple tabsets on a page.
        navigation.setAttribute("aria-label", `Tab Set ${tabsetIndex + 1}`);

        // Generate Links and Prepare Panels
        panels.forEach((panel, panelIndex) => {
            const panelId = panel.id;
            const titleElement = panel.querySelector(".tab-title");
            const contentElement = panel.querySelector(".tab-content");

            // Basic validation
            if (!panelId) {
                console.warn("Tab panel is missing an ID, skipping:", panel);
                return;
            }
            if (!titleElement) {
                console.warn(`Tab panel with ID "${panelId}" is missing a .tab-title, skipping:`, panel);
                return;
            }
            if (!contentElement) {
                console.warn(`Tab panel with ID "${panelId}" is missing a .tab-content, skipping:`, panel);
                return;
            }

            // Create the "tab" button
            const link = document.createElement("button");
            link.textContent = titleElement.textContent.trim();
            link.className = "tab-link";
            link.setAttribute("role", "tab");
            link.setAttribute("aria-controls", panelId); // Links button to panel.
            link.setAttribute("data-target-id", panelId); // Store target ID for easier access.
            // Ensure unique ID for the link itself for aria-labelledby.
            const linkId = `tab-link-${panelId}-${tabsetIndex}`;
            link.id = linkId;

            navigation.appendChild(link);

            // Setup Panel and Content Accessibility & Initial State.
            panel.setAttribute("role", "tabpanel");
            panel.setAttribute("aria-labelledby", linkId); // Links panel back to button.

            if (panelIndex === 0) {
                // First tab is active by default.
                link.classList.add("active");
                link.setAttribute("aria-selected", "true");
                contentElement.classList.remove("hidden"); // Ensure it's visible.
            } else {
                // Hide other tabs' content initially.
                link.setAttribute("aria-selected", "false");
                contentElement.classList.add("hidden");
            }
        });

        // Insert Navigation and Add Event Listener

        // Insert the generated navigation links *before* the tabset element.
        tabset.parentNode.insertBefore(navigation, tabset);

        // Add a single event listener to the navigation container (event delegation).
        navigation.addEventListener("click", (event) => {
            // Find the button that was clicked, even if a child element was the direct target.
            const clickedLink = event.target.closest(".tab-link");

            // Ignore clicks that are not on a tab link or if it's already active.
            if (!clickedLink || clickedLink.classList.contains("active")) {
                return;
            }

            const targetId = clickedLink.dataset.targetId;
            if (!targetId) return; // Should not happen :)

            // Update Tab States within this specific tabset.

            // Deactivate all links and hide all content within this tabset
            navigation.querySelectorAll(".tab-link").forEach((link) => {
                link.classList.remove("active");
                link.setAttribute("aria-selected", "false");
            });
            // Only hide content within the *current* tabset associated with this navigation.
            tabset.querySelectorAll(".tab-content").forEach((content) => {
                content.classList.add("hidden");
            });

            // Activate the clicked link.
            clickedLink.classList.add("active");
            clickedLink.setAttribute("aria-selected", "true");

            // Show the target panel's content.
            const targetPanel = tabset.querySelector(`#${targetId}`); // Find panel within the correct tabset
            if (targetPanel) {
                const targetContent = targetPanel.querySelector(".tab-content");
                if (targetContent) {
                    targetContent.classList.remove("hidden");
                } else {
                    console.error(`Could not find .tab-content within target panel #${targetId}`);
                }
            } else {
                console.error(`Could not find target panel #${targetId} within its tabset`);
            }
        });
    });
}
