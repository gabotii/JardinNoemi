// JardinNoemi · utilidades mínimas de interfaz (sin dependencias).
// Blazor se encarga de la lógica; aquí solo lo que requiere APIs del navegador.
(() => {
    "use strict";

    window.jn = {
        showModal(dialog) {
            if (dialog && !dialog.open) {
                dialog.showModal();
            }
        },
        closeModal(dialog) {
            if (dialog && dialog.open) {
                dialog.close();
            }
        },
        scrollToTop() {
            window.scrollTo({ top: 0, behavior: "smooth" });
        },
        scrollIntoView(element) {
            element?.scrollIntoView({ behavior: "smooth", block: "start" });
        },
        focus(selector) {
            document.querySelector(selector)?.focus();
        }
    };

    // Sombra de la cabecera al hacer scroll.
    const updateHeader = () => {
        document.querySelector(".site-header")?.classList.toggle("is-scrolled", window.scrollY > 8);
    };
    window.addEventListener("scroll", updateHeader, { passive: true });
    document.addEventListener("DOMContentLoaded", updateHeader);

    document.addEventListener("click", (event) => {
        // Cierra el menú móvil (popover nativo) al navegar dentro de él.
        const link = event.target.closest("#mobile-nav a");
        if (link) {
            const nav = document.getElementById("mobile-nav");
            if (nav?.matches(":popover-open")) {
                nav.hidePopover();
            }
        }

        // "Light dismiss" para <dialog data-light-dismiss>: clic en el fondo cierra.
        const target = event.target;
        if (target instanceof HTMLDialogElement && target.hasAttribute("data-light-dismiss") && target.open) {
            const rect = target.getBoundingClientRect();
            const inside = event.clientX >= rect.left && event.clientX <= rect.right &&
                event.clientY >= rect.top && event.clientY <= rect.bottom;
            if (!inside) {
                target.close();
            }
        }
    });

    // Tras una navegación mejorada (páginas estáticas) se recalcula el estado de la cabecera.
    if (window.Blazor?.addEventListener) {
        window.Blazor.addEventListener("enhancedload", updateHeader);
    }
})();
