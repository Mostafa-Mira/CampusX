(() => {
    const storageKey = "campusx-theme";
    const toggle = document.getElementById("theme-toggle");

    if (!toggle) return;

    const applyTheme = (theme) => {
        const isDark = theme === "dark";
        if (isDark) document.documentElement.dataset.theme = "dark";
        else delete document.documentElement.dataset.theme;

        toggle.setAttribute("aria-pressed", String(isDark));
        toggle.setAttribute("aria-label", isDark ? "Switch to light mode" : "Switch to dark mode");
    };

    applyTheme(document.documentElement.dataset.theme);
    toggle.addEventListener("click", () => {
        const nextTheme = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
        try { localStorage.setItem(storageKey, nextTheme); } catch { }
        applyTheme(nextTheme);
    });
})();