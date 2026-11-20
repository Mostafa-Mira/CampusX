(() => {
    const form = document.querySelector("[data-reset-form]");
    if (!form) return;

    const ticket = form.elements.namedItem("Ticket");
    const needsCode = form.elements.namedItem("NeedsUniversityCode");
    const universityCode = form.elements.namedItem("UniversityCode");
    const applyLink = () => {
        const fromEmail = new URLSearchParams(window.location.hash.slice(1));
        const linkTicket = fromEmail.get("ticket");
        if (linkTicket) {
            ticket.value = linkTicket;
            needsCode.value = String(fromEmail.get("proof") === "1");
            universityCode.value = "";
            form.elements.namedItem("Password").value = "";
            form.elements.namedItem("ConfirmPassword").value = "";
            history.replaceState(null, "", window.location.pathname + window.location.search);
        }

        const hasTicket = ticket.value.length > 0 && ticket.value.length <= 4096;
        form.hidden = !hasTicket;
        document.querySelector("[data-reset-missing]").hidden = hasTicket;
        const showCode = needsCode.value.toLowerCase() === "true";
        form.querySelector("[data-reset-university]").hidden = !showCode;
        universityCode.required = showCode;
    };

    applyLink();
    window.addEventListener("hashchange", applyLink);
})();
