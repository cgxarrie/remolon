(function () {
    try {
        var raw = localStorage.getItem('retro-color-mode');
        var mode = 'system';
        if (raw) {
            var parsed = JSON.parse(raw);
            if (parsed && parsed.state && parsed.state.mode) mode = parsed.state.mode;
            else if (parsed === 'light' || parsed === 'dark' || parsed === 'system') mode = parsed;
        }
        var dark = mode === 'dark' || (mode !== 'light' && window.matchMedia('(prefers-color-scheme: dark)').matches);
        document.documentElement.classList.toggle('dark', dark);
        document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
    } catch (e) { }
})();
