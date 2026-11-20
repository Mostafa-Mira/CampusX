(() => {
    // Display aids only: every limit and expiry is enforced again on the server.
    const cooldown = document.querySelector("[data-cooldown]");
    if (cooldown) {
        const started = performance.now();
        const duration = Number(cooldown.dataset.cooldown);
        const update = () => {
            const seconds = Math.max(0, duration - Math.floor((performance.now() - started) / 1000));
            cooldown.disabled = seconds > 0;
            cooldown.textContent = seconds > 0 ? "Request code in " + seconds + "s" : cooldown.dataset.readyLabel;
        };
        update();
        if (duration > 0) {
            const timer = setInterval(() => { update(); if (!cooldown.disabled) clearInterval(timer); }, 1000);
        }
    }
    const expiry = document.getElementById("code-expiry");
    if (expiry?.dataset.expires) {
        const deadline = Date.parse(expiry.dataset.expires);
        const update = () => {
            if (Date.now() >= deadline) {
                expiry.textContent = "Your code has expired. Request a new code.";
                document.getElementById("verify-code-button").disabled = true;
                return true;
            }
            return false;
        };
        if (!update()) {
            const timer = setInterval(() => { if (update()) clearInterval(timer); }, 1000);
        }
    }
})();

