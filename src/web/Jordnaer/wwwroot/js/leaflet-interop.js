/**
 * Leaflet.js interop for Blazor map search functionality
 * Provides map initialization, interaction, and radius visualization
 */

window.leafletInterop = {
    maps: {},

    /**
     * Initializes a Leaflet map instance
     * @param {string} mapId - The HTML element ID for the map container
     * @param {number} lat - Initial latitude
     * @param {number} lng - Initial longitude
     * @param {number} zoom - Initial zoom level
     * @returns {boolean} Success status
     */
    initializeMap: function (mapId, lat, lng, zoom) {
        try {
            if (this.maps[mapId]) {
                this.maps[mapId].map.remove();
            }

            const map = L.map(mapId).setView([lat, lng], zoom);

            // Add OpenStreetMap tile layer (free, no API key required)
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                maxZoom: 19
            }).addTo(map);

            this.maps[mapId] = {
                map: map,
                circle: null,
                marker: null
            };

            return true;
        } catch (error) {
            console.error('Error initializing map:', error);
            return false;
        }
    },

    /**
     * Sets up click handler for map that calls back to C#
     * @param {string} mapId - The map instance ID
     * @param {object} dotNetHelper - DotNetObjectReference for callbacks
     * @returns {boolean} Success status
     */
    setupClickHandler: function (mapId, dotNetHelper) {
        try {
            const mapInstance = this.maps[mapId];
            if (!mapInstance) {
                console.error('Map not found:', mapId);
                return false;
            }

            mapInstance.map.on('click', function (e) {
                dotNetHelper.invokeMethodAsync('OnMapClicked', e.latlng.lat, e.latlng.lng);
            });

            return true;
        } catch (error) {
            console.error('Error setting up click handler:', error);
            return false;
        }
    },

    /**
     * Updates or creates a circle to show search radius
     * @param {string} mapId - The map instance ID
     * @param {number} lat - Center latitude
     * @param {number} lng - Center longitude
     * @param {number} radiusKm - Radius in kilometers
     * @returns {boolean} Success status
     */
    updateSearchRadius: function (mapId, lat, lng, radiusKm) {
        try {
            const mapInstance = this.maps[mapId];
            if (!mapInstance) {
                console.error('Map not found:', mapId);
                return false;
            }

            // Remove existing circle if any
            if (mapInstance.circle) {
                mapInstance.circle.remove();
            }

            // Create new circle (radius in meters)
            const radiusMeters = radiusKm * 1000;
            mapInstance.circle = L.circle([lat, lng], {
                color: '#594F8D',      // Primary color from Jordnaer theme
                fillColor: '#594F8D',
                fillOpacity: 0.15,
                radius: radiusMeters
            }).addTo(mapInstance.map);

            // Fit map bounds to show the entire circle
            const bounds = mapInstance.circle.getBounds();
            mapInstance.map.fitBounds(bounds, { padding: [50, 50] });

            return true;
        } catch (error) {
            console.error('Error updating search radius:', error);
            return false;
        }
    },

    /**
     * Centers the map on a specific location
     * @param {string} mapId - The map instance ID
     * @param {number} lat - Latitude
     * @param {number} lng - Longitude
     * @param {number} zoom - Zoom level (optional, uses current if not provided)
     * @returns {boolean} Success status
     */
    centerMap: function (mapId, lat, lng, zoom) {
        try {
            const mapInstance = this.maps[mapId];
            if (!mapInstance) {
                console.error('Map not found:', mapId);
                return false;
            }

            if (zoom !== undefined && zoom !== null) {
                mapInstance.map.setView([lat, lng], zoom);
            } else {
                mapInstance.map.panTo([lat, lng]);
            }

            return true;
        } catch (error) {
            console.error('Error centering map:', error);
            return false;
        }
    },

    /**
     * Adds or updates a marker at the search location
     * @param {string} mapId - The map instance ID
     * @param {number} lat - Latitude
     * @param {number} lng - Longitude
     * @returns {boolean} Success status
     */
    updateMarker: function (mapId, lat, lng) {
        try {
            const mapInstance = this.maps[mapId];
            if (!mapInstance) {
                console.error('Map not found:', mapId);
                return false;
            }

            // Remove existing marker if any
            if (mapInstance.marker) {
                mapInstance.marker.remove();
            }

            // Add new marker
            mapInstance.marker = L.marker([lat, lng]).addTo(mapInstance.map);

            return true;
        } catch (error) {
            console.error('Error updating marker:', error);
            return false;
        }
    },

    /**
     * Removes the search marker
     * @param {string} mapId - The map instance ID
     * @returns {boolean} Success status
     */
    removeMarker: function (mapId) {
        try {
            const mapInstance = this.maps[mapId];
            if (!mapInstance) {
                console.error('Map not found:', mapId);
                return false;
            }

            if (mapInstance.marker) {
                mapInstance.marker.remove();
                mapInstance.marker = null;
            }

            return true;
        } catch (error) {
            console.error('Error removing marker:', error);
            return false;
        }
    },

    /**
     * Removes the search radius circle
     * @param {string} mapId - The map instance ID
     * @returns {boolean} Success status
     */
    removeSearchRadius: function (mapId) {
        try {
            const mapInstance = this.maps[mapId];
            if (!mapInstance) {
                console.error('Map not found:', mapId);
                return false;
            }

            if (mapInstance.circle) {
                mapInstance.circle.remove();
                mapInstance.circle = null;
            }

            return true;
        } catch (error) {
            console.error('Error removing search radius:', error);
            return false;
        }
    },

    /**
     * Disposes of a map instance
     * @param {string} mapId - The map instance ID
     * @returns {boolean} Success status
     */
    disposeMap: function (mapId) {
        try {
            const mapInstance = this.maps[mapId];
            if (!mapInstance) {
                return true; // Already disposed
            }

            if (mapInstance.circle) {
                mapInstance.circle.remove();
            }
            if (mapInstance.marker) {
                mapInstance.marker.remove();
            }
            if (mapInstance.map) {
                mapInstance.map.remove();
            }

            delete this.maps[mapId];
            return true;
        } catch (error) {
            console.error('Error disposing map:', error);
            return false;
        }
    }
};
