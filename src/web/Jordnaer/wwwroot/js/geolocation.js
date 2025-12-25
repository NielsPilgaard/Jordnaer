// Browser Geolocation API integration
window.geolocationService = {
    // Request user's current location
    // Returns: { latitude: number, longitude: number } or null if denied/unavailable
    getCurrentPosition: function () {
        return new Promise((resolve) => {
            if (!navigator.geolocation) {
                console.log('Geolocation is not supported by this browser');
                resolve(null);
                return;
            }

            navigator.geolocation.getCurrentPosition(
                // Success callback
                (position) => {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude
                    });
                },
                // Error callback
                (error) => {
                    console.log('Geolocation error:', error.message);
                    resolve(null);
                },
                // Options
                {
                    enableHighAccuracy: false,
                    timeout: 5000,
                    maximumAge: 300000 // Cache for 5 minutes
                }
            );
        });
    },

    // Check if geolocation permission is already granted
    checkPermission: async function () {
        if (!navigator.permissions || !navigator.permissions.query) {
            return 'prompt'; // Assume we need to prompt
        }

        try {
            const result = await navigator.permissions.query({ name: 'geolocation' });
            return result.state; // 'granted', 'denied', or 'prompt'
        } catch (error) {
            console.log('Permission check error:', error);
            return 'prompt';
        }
    }
};
