// WHAT IT DOES: Hamburger toggles the sidebar.
// - Desktop: collapse/expand by toggling .t-collapsed on #t-shell
// - Mobile (<=1100px): overlay open/close by toggling .is-open on #t-sidebar
document.addEventListener('DOMContentLoaded', () => {
    const shell = document.getElementById('t-shell');
    const sidebar = document.getElementById('t-sidebar');
    const btn = document.getElementById('t-menu');

    const isMobile = () => window.matchMedia('(max-width: 1100px)').matches;

    const toggleNav = () => {
        if (isMobile()) {
            sidebar?.classList.toggle('is-open');
            btn?.setAttribute('aria-expanded', sidebar?.classList.contains('is-open') ? 'true' : 'false');
        } else {
            shell?.classList.toggle('t-collapsed');
            btn?.setAttribute('aria-expanded', shell?.classList.contains('t-collapsed') ? 'false' : 'true');
        }
    };

    btn?.addEventListener('click', toggleNav);

    // Close overlay if resizing from mobile -> desktop
    window.addEventListener('resize', () => {
        if (!isMobile()) sidebar?.classList.remove('is-open');
    });
});
