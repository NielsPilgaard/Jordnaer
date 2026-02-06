/**
 * Leaflet.js interop for Blazor map search functionality
 * Provides map initialization, interaction, and radius visualization
 */

window.leafletInterop = {
    maps: {},

    // Primary color from Jordnaer theme
    primaryColor: '#594F8D',

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
                marker: null,
                markerClusterGroup: null
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
            if (mapInstance.markerClusterGroup) {
                mapInstance.markerClusterGroup.clearLayers();
                mapInstance.map.removeLayer(mapInstance.markerClusterGroup);
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
    },

    /**
     * Creates a custom icon for a group marker
     * @param {object} group - Group data with profilePictureUrl and name
     * @returns {L.DivIcon} Custom Leaflet icon
     */
    createGroupIcon: function (group) {
        const size = 40;
        let iconHtml;

        if (group.profilePictureUrl) {
            iconHtml = `<img src="${this.escapeAttribute(group.profilePictureUrl)}" alt="${this.escapeAttribute(group.name)}" />`;
        } else {
            const initial = group.name ? group.name.charAt(0).toUpperCase() : '?';
            iconHtml = `<span class="group-marker-initial">${this.escapeHtml(initial)}</span>`;
        }

        return L.divIcon({
            className: 'group-marker-icon',
            html: iconHtml,
            iconSize: [size, size],
            iconAnchor: [size / 2, size / 2],
            popupAnchor: [0, -size / 2]
        });
    },

    /**
     * Creates popup content HTML for a group
     * @param {object} group - Group data
     * @returns {string} HTML string for popup content
     */
    createGroupPopupContent: function (group) {
        // Build the avatar section
        let avatarContent;
        if (group.profilePictureUrl) {
            avatarContent = `<img src="${this.escapeAttribute(group.profilePictureUrl)}" alt="${this.escapeAttribute(group.name)}" />`;
        } else {
            const initial = group.name ? group.name.charAt(0).toUpperCase() : '?';
            avatarContent = `<span class="group-initial">${this.escapeHtml(initial)}</span>`;
        }

        // Build the location string
        let locationHtml = '';
        if (group.zipCode || group.city) {
            const locationParts = [];
            if (group.zipCode) locationParts.push(group.zipCode);
            if (group.city) locationParts.push(group.city);
            const locationText = locationParts.join(', ');
            locationHtml = `
                <div class="group-popup-location">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/>
                    </svg>
                    <span>${this.escapeHtml(locationText)}</span>
                </div>
            `;
        }

        // Build description
        let descriptionHtml = '';
        if (group.shortDescription) {
            descriptionHtml = `<div class="group-popup-description">${this.escapeHtml(group.shortDescription)}</div>`;
        }

        // Build the full popup
        const groupUrl = `/groups/${encodeURIComponent(group.name)}`;

        return `
            <div class="group-popup-content">
                <div class="group-popup-header">
                    <div class="group-popup-avatar">
                        ${avatarContent}
                    </div>
                    <h3 class="group-popup-title">${this.escapeHtml(group.name)}</h3>
                </div>
                <div class="group-popup-body">
                    ${locationHtml}
                    ${descriptionHtml}
                    <a href="${groupUrl}" class="group-popup-link">
                        <span>Se gruppe</span>
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                            <path d="M12 4l-1.41 1.41L16.17 11H4v2h12.17l-5.58 5.59L12 20l8-8z"/>
                        </svg>
                    </a>
                </div>
            </div>
        `;
    },

    /**
     * Escapes HTML special characters to prevent XSS
     * @param {string} text - Text to escape
     * @returns {string} Escaped text
     */
    escapeHtml: function (text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    /**
     * Escapes a string for safe use in HTML attributes
     * @param {string} text - Text to escape for attribute context
     * @returns {string} Escaped text safe for use in attributes
     */
    escapeAttribute: function (text) {
        if (!text) return '';
        return text
            .replace(/&/g, '&amp;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    },

    /**
     * Updates group markers on the map with clustering
     * @param {string} mapId - The map instance ID
     * @param {Array} groups - Array of group objects with lat, lng, and group info
     * @returns {boolean} Success status
     */
    updateGroupMarkers: function (mapId, groups) {
        try {
            const mapInstance = this.maps[mapId];
            if (!mapInstance) {
                console.error('Map not found:', mapId);
                return false;
            }

            // Remove existing cluster group if any
            if (mapInstance.markerClusterGroup) {
                mapInstance.markerClusterGroup.clearLayers();
                mapInstance.map.removeLayer(mapInstance.markerClusterGroup);
            }

            // Create marker cluster group with custom options
            mapInstance.markerClusterGroup = L.markerClusterGroup({
                showCoverageOnHover: false,
                maxClusterRadius: 60,
                spiderfyOnMaxZoom: true,
                disableClusteringAtZoom: 18,
                iconCreateFunction: function (cluster) {
                    const count = cluster.getChildCount();
                    let sizeClass = 'marker-cluster-small';
                    let size = 40;

                    if (count >= 100) {
                        sizeClass = 'marker-cluster-large';
                        size = 44;
                    } else if (count >= 10) {
                        sizeClass = 'marker-cluster-medium';
                        size = 40;
                    }

                    return L.divIcon({
                        html: '<div>' + count + '</div>',
                        className: 'marker-cluster ' + sizeClass,
                        iconSize: L.point(size, size)
                    });
                }
            });

            // Add markers for each group
            if (groups && groups.length > 0) {
                groups.forEach(group => {
                    // Skip groups without valid coordinates
                    if (group.latitude == null || group.longitude == null ||
                        isNaN(group.latitude) || isNaN(group.longitude)) {
                        return;
                    }

                    const marker = L.marker([group.latitude, group.longitude], {
                        icon: this.createGroupIcon(group)
                    });

                    // Create and bind popup
                    const popupContent = this.createGroupPopupContent(group);
                    marker.bindPopup(popupContent, {
                        className: 'group-popup',
                        maxWidth: 300,
                        minWidth: 250
                    });

                    mapInstance.markerClusterGroup.addLayer(marker);
                });
            }

            // Add cluster group to map
            mapInstance.map.addLayer(mapInstance.markerClusterGroup);

            return true;
        } catch (error) {
            console.error('Error updating group markers:', error);
            return false;
        }
    },

    /**
     * Clears all group markers from the map
     * @param {string} mapId - The map instance ID
     * @returns {boolean} Success status
     */
    clearGroupMarkers: function (mapId) {
        try {
            const mapInstance = this.maps[mapId];
            if (!mapInstance) {
                console.error('Map not found:', mapId);
                return false;
            }

            if (mapInstance.markerClusterGroup) {
                mapInstance.markerClusterGroup.clearLayers();
            }

            return true;
        } catch (error) {
            console.error('Error clearing group markers:', error);
            return false;
        }
    },

    /**
     * Fits the map view to show all group markers
     * @param {string} mapId - The map instance ID
     * @param {number} padding - Padding around bounds in pixels
     * @returns {boolean} Success status
     */
    fitBoundsToMarkers: function (mapId, padding) {
        try {
            const mapInstance = this.maps[mapId];
            if (!mapInstance || !mapInstance.markerClusterGroup) {
                console.error('Map or marker cluster group not found:', mapId);
                return false;
            }

            const bounds = mapInstance.markerClusterGroup.getBounds();
            if (bounds.isValid()) {
                const p = padding ?? 50;
                mapInstance.map.fitBounds(bounds, {
                    padding: [p, p],
                    maxZoom: 15
                });
            }

            return true;
        } catch (error) {
            console.error('Error fitting bounds to markers:', error);
            return false;
        }
    }
};
