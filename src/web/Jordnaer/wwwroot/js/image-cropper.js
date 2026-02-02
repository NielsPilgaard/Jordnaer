// Image cropper functionality using Cropper.js
// Requires Cropper.js library to be loaded

window.imageCropper = {
    cropper: null,
    componentId: null,

    /**
     * Wait for Cropper.js library to be loaded
     * @returns {Promise<boolean>} Promise that resolves when Cropper is available
     */
    waitForCropper: function () {
        return new Promise((resolve, reject) => {
            // If Cropper is already available, resolve immediately
            if (typeof Cropper !== 'undefined') {
                resolve(true);
                return;
            }

            // Otherwise, wait up to 3 seconds for it to load
            let attempts = 0;
            const maxAttempts = 30; // 30 attempts * 100ms = 3 seconds

            const checkInterval = setInterval(() => {
                attempts++;

                if (typeof Cropper !== 'undefined') {
                    clearInterval(checkInterval);
                    resolve(true);
                } else if (attempts >= maxAttempts) {
                    clearInterval(checkInterval);
                    reject(new Error('Cropper.js library failed to load within 3 seconds'));
                }
            }, 100);
        });
    },

    /**
     * Initialize Cropper.js on an image element
     * @param {string} imageElementId - The ID of the image element to crop
     * @param {string} componentId - The component ID for finding preview elements
     * @returns {Promise<void>} Promise that resolves when initialization is complete
     */
    initializeCropper: async function (imageElementId, componentId) {
        // Wait for Cropper.js to be available
        await this.waitForCropper();

        const image = document.getElementById(imageElementId);
        if (!image) {
            console.error('Image element not found:', imageElementId);
            return;
        }

        // Store the component ID for preview updates
        this.componentId = componentId;

        // Destroy existing cropper if any
        if (this.cropper) {
            this.cropper.destroy();
        }

        // Initialize new cropper with square aspect ratio
        this.cropper = new Cropper(image, {
            aspectRatio: 1, // Square (1:1)
            viewMode: 1, // Restrict crop box to canvas
            dragMode: 'move',
            autoCropArea: 0.8,
            restore: false,
            guides: true,
            center: true,
            highlight: false,
            cropBoxMovable: true,
            cropBoxResizable: true,
            toggleDragModeOnDblclick: false,
            responsive: true,
            crop: (event) => {
                // Update preview on crop change
                this.updatePreviews();
            }
        });
    },

    /**
     * Update all preview images with the current crop
     */
    updatePreviews: function () {
        if (!this.cropper || !this.componentId) return;

        // Build preview IDs based on the component ID
        const previewSuffixes = ['40', '150', '250'];
        const previewIds = previewSuffixes.map(suffix => `preview-${suffix}-${this.componentId}`);

        const canvas = this.cropper.getCroppedCanvas();

        if (!canvas) return;

        const dataUrl = canvas.toDataURL();

        previewIds.forEach(id => {
            const preview = document.getElementById(id);
            if (preview) {
                preview.src = dataUrl;
            }
        });
    },

    /**
     * Get the cropped image as a data URL
     * @param {number} width - Target width for the cropped image
     * @param {number} height - Target height for the cropped image
     * @returns {string} Data URL of the cropped image
     */
    getCroppedImage: function (width, height) {
        if (!this.cropper) {
            console.error('Cropper not initialized');
            return '';
        }

        const canvas = this.cropper.getCroppedCanvas({
            width: width,
            height: height,
            imageSmoothingEnabled: true,
            imageSmoothingQuality: 'high'
        });

        if (!canvas) {
            console.error('Failed to get cropped canvas');
            return '';
        }

        return canvas.toDataURL('image/jpeg', 0.9);
    },

    /**
     * Destroy the cropper instance
     */
    destroyCropper: function () {
        if (this.cropper) {
            this.cropper.destroy();
            this.cropper = null;
        }
    },

    /**
     * Get image dimensions from a data URL
     * @param {string} dataUrl - The data URL of the image
     * @returns {Promise<number[]>} Promise resolving to [width, height]
     */
    getImageDimensions: function (dataUrl) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.onload = function () {
                resolve([img.width, img.height]);
            };
            img.onerror = function () {
                reject(new Error('Failed to load image'));
            };
            img.src = dataUrl;
        });
    }
};
