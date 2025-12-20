// Lazy loader for Quill.js and dependencies
window.QuillLoader = {
    isLoaded: false,
    loadingPromise: null,

    async ensureLoaded() {
        if (this.isLoaded) {
            return;
        }

        // If already loading, wait for that promise
        if (this.loadingPromise) {
            return this.loadingPromise;
        }

        this.loadingPromise = this.loadResources();
        await this.loadingPromise;
        this.isLoaded = true;
    },

    async loadResources() {
        // Load CSS files
        await Promise.all([
            this.loadStylesheet('https://cdn.quilljs.com/1.3.6/quill.snow.css'),
            this.loadStylesheet('https://cdn.quilljs.com/1.3.6/quill.bubble.css')
        ]);

        // Load JS files sequentially (order matters)
        await this.loadScript('https://cdn.quilljs.com/1.3.6/quill.js');
        await this.loadScript('_content/WYSIWYGTextEditor/quill-blot-formatter.min.js');
        await this.loadScript('_content/WYSIWYGTextEditor/BlazorQuill.js');
    },

    loadStylesheet(href) {
        return new Promise((resolve, reject) => {
            const existing = document.querySelector(`link[href="${href}"]`);
            if (existing) {
                resolve();
                return;
            }

            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = href;
            link.onload = resolve;
            link.onerror = reject;
            document.head.appendChild(link);
        });
    },

    loadScript(src) {
        return new Promise((resolve, reject) => {
            const existing = document.querySelector(`script[src="${src}"]`);
            if (existing) {
                resolve();
                return;
            }

            const script = document.createElement('script');
            script.src = src;
            script.onload = resolve;
            script.onerror = reject;
            document.body.appendChild(script);
        });
    }
};
