/* SV22T1020176.Shop – Custom JavaScript
   ====================================== */

// ── Scroll to top button ────────────────────────────────
(function() {
    var btn = document.createElement('button');
    btn.id = 'scrollToTop';
    btn.innerHTML = '<i class="bi bi-chevron-up"></i>';
    btn.title = 'Cuộn lên đầu trang';
    btn.style.display = 'none';
    document.body.appendChild(btn);

    btn.addEventListener('click', function() {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });

    window.addEventListener('scroll', function() {
        btn.style.display = window.scrollY > 200 ? 'flex' : 'none';
    });
})();

// ── Bootstrap validation enhancement ───────────────────
(function() {
    'use strict';
    var forms = document.querySelectorAll('.needs-validation');
    Array.prototype.slice.call(forms).forEach(function(form) {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });
})();

// ── Utility: parse JSON safely ────────────────────────
function safeJson(res) {
    if (!res.ok) throw new Error('Server error: ' + res.status);
    return res.json();
}
