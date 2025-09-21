/* Real Leaflet.js-like implementation with OpenStreetMap tiles */
window.L = window.L || {};

// Map implementation with actual OpenStreetMap embed
L.map = function(elementId) {
    return new L.Map(elementId);
};

L.Map = function(elementId) {
    this.element = document.getElementById(elementId);
    this.markers = [];
    this.center = [41.8902, 12.4922];
    this.zoom = 12;
    this.initialized = false;
    this.tileLayer = null;
    
    return this;
};

L.Map.prototype = {
    setView: function(center, zoom) {
        this.center = center;
        this.zoom = zoom;
        if (this.initialized) {
            this._render();
        }
        return this;
    },
    
    // Track whether this is a user-initiated center change vs programmatic
    _updateView: function(center, zoom, isUserInitiated = false) {
        this.center = center;
        this.zoom = zoom;
        if (this.initialized) {
            this._render();
        }
        return this;
    },
    
    addTo: function(map) {
        return this;
    },
    
    fitBounds: function(bounds, options) {
        if (bounds.length === 0) return this;
        
        // Calculate center and zoom from bounds
        let minLat = bounds[0][0], maxLat = bounds[0][0];
        let minLng = bounds[0][1], maxLng = bounds[0][1];
        
        bounds.forEach(([lat, lng]) => {
            minLat = Math.min(minLat, lat);
            maxLat = Math.max(maxLat, lat);
            minLng = Math.min(minLng, lng);
            maxLng = Math.max(maxLng, lng);
        });
        
        const centerLat = (minLat + maxLat) / 2;
        const centerLng = (minLng + maxLng) / 2;
        
        // Calculate zoom level based on bounds size and map dimensions
        const latDiff = maxLat - minLat;
        const lngDiff = maxLng - minLng;
        const maxDiff = Math.max(latDiff, lngDiff);
        
        // Improved zoom calculation
        let zoom = 16;
        if (maxDiff > 0.2) zoom = 9;
        else if (maxDiff > 0.1) zoom = 10;
        else if (maxDiff > 0.05) zoom = 12;
        else if (maxDiff > 0.02) zoom = 13;
        else if (maxDiff > 0.01) zoom = 14;
        else if (maxDiff > 0.005) zoom = 15;
        
        // Apply padding if specified
        if (options && options.padding) {
            zoom = Math.max(zoom - 1, 8); // Reduce zoom by 1 for padding
        }
        
        this.setView([centerLat, centerLng], zoom);
        return this;
    },
    
    _render: function() {
        if (!this.element) return;
        
        // Create map with markers overlay using absolute positioning
        const markersHtml = this.markers.map((marker, index) => {
            const markerLat = marker.latlng[0];
            const markerLng = marker.latlng[1];
            
            // Calculate marker position relative to current iframe bounds
            const pixelX = this._lngToPixel(markerLng);
            const pixelY = this._latToPixel(markerLat);
            
            return `
            <div class="leaflet-marker" data-marker-index="${index}" data-lat="${markerLat}" data-lng="${markerLng}" style="position: absolute; top: ${pixelY}px; left: ${pixelX}px; transform: translate(-50%, -100%); z-index: 100; cursor: pointer;">
                <div class="marker-icon" style="background: #FF6B6B; color: white; width: 20px; height: 20px; border-radius: 50% 50% 50% 0; transform: rotate(-45deg); display: flex; align-items: center; justify-content: center; font-size: 12px; border: 2px solid white; box-shadow: 0 2px 8px rgba(0,0,0,0.3);">
                    <span style="transform: rotate(45deg);">${this._getCategoryIcon(marker.category)}</span>
                </div>
            </div>
        `;
        }).join('');
        
        // Create iframe with OpenStreetMap embed
        const mapHtml = `
            <div style="position: relative; width: 100%; height: 100%; border-radius: 0.375rem; overflow: hidden;">
                <iframe 
                    id="osm-iframe"
                    width="100%" 
                    height="100%" 
                    frameborder="0" 
                    scrolling="no" 
                    marginheight="0" 
                    marginwidth="0" 
                    src="https://www.openstreetmap.org/export/embed.html?bbox=${this._getBbox()}&amp;layer=mapnik&amp;marker=${this.center[0]},${this.center[1]}"
                    style="border: none; pointer-events: none;">
                </iframe>
                ${markersHtml}
                <div class="leaflet-control-zoom" style="position: absolute; top: 10px; right: 10px; z-index: 1000;">
                    <button class="leaflet-control-zoom-in" style="display: block; width: 32px; height: 32px; background: white; border: 1px solid #ccc; cursor: pointer; font-weight: bold; color: #333; margin-bottom: 1px;">+</button>
                    <button class="leaflet-control-zoom-out" style="display: block; width: 32px; height: 32px; background: white; border: 1px solid #ccc; cursor: pointer; font-weight: bold; color: #333;">−</button>
                </div>
                <div style="position: absolute; top: 10px; left: 10px; background: rgba(255,255,255,0.9); padding: 8px; border-radius: 4px; font-size: 12px; font-family: Arial, sans-serif;">
                    <a href="https://www.openstreetmap.org/?mlat=${this.center[0]}&mlon=${this.center[1]}&zoom=${this.zoom}" target="_blank" style="color: #0078A8; text-decoration: none;">View Larger Map</a>
                </div>
                <div style="position: absolute; bottom: 10px; right: 10px; background: rgba(255,255,255,0.9); padding: 4px 8px; border-radius: 4px; font-size: 11px; font-family: Arial, sans-serif;">
                    <a href="https://www.openstreetmap.org/copyright" target="_blank" style="color: #0078A8; text-decoration: none;">© OpenStreetMap</a>
                </div>
                <div id="leaflet-popup" style="position: absolute; display: none; background: white; border: 1px solid #ccc; border-radius: 8px; padding: 0; box-shadow: 0 3px 14px rgba(0,0,0,0.4); z-index: 1000; max-width: 300px; min-width: 200px;">
                    <div style="background: #f8f9fa; padding: 8px 12px; border-bottom: 1px solid #dee2e6; border-radius: 8px 8px 0 0; font-weight: 600; font-size: 14px;">
                        <button id="popup-close" style="float: right; background: none; border: none; font-size: 18px; cursor: pointer; color: #666; line-height: 1;">&times;</button>
                        <span id="popup-title"></span>
                    </div>
                    <div id="popup-content" style="padding: 12px; font-size: 14px; line-height: 1.4;"></div>
                </div>
            </div>
        `;
        
        this.element.innerHTML = mapHtml;
        this.initialized = true;
        
        // Add click handlers for markers and zoom controls
        this._addMarkerClickHandlers();
        this._addZoomControls();
        this._addPanControls();
    },
    
    _addZoomControls: function() {
        const zoomInBtn = this.element.querySelector('.leaflet-control-zoom-in');
        const zoomOutBtn = this.element.querySelector('.leaflet-control-zoom-out');
        
        if (zoomInBtn) {
            zoomInBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                this.setView(this.center, Math.min(this.zoom + 1, 18));
            });
        }
        
        if (zoomOutBtn) {
            zoomOutBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                this.setView(this.center, Math.max(this.zoom - 1, 1));
            });
        }
    },
    
    _addPanControls: function() {
        let isDragging = false;
        let startX, startY;
        let startCenter;
        
        // Add dragging functionality to the map container
        this.element.addEventListener('mousedown', (e) => {
            // Don't start dragging if clicking on a marker or control
            if (e.target.closest('.leaflet-marker') || e.target.closest('.leaflet-control-zoom')) {
                return;
            }
            
            isDragging = true;
            startX = e.clientX;
            startY = e.clientY;
            startCenter = [this.center[0], this.center[1]];
            
            // Prevent text selection during drag
            e.preventDefault();
            this.element.style.cursor = 'grabbing';
        });
        
        document.addEventListener('mousemove', (e) => {
            if (!isDragging) return;
            
            const deltaX = e.clientX - startX;
            const deltaY = e.clientY - startY;
            
            // Calculate the map dimensions and zoom level for proper scaling
            const bbox = this._getBboxAsNumbers();
            const mapWidth = this.element.offsetWidth;
            const mapHeight = this.element.offsetHeight;
            
            // Convert pixel movement to geographic movement
            const lngRange = bbox.maxLng - bbox.minLng;
            const latRange = bbox.maxLat - bbox.minLat;
            
            const deltaLng = -(deltaX / mapWidth) * lngRange;
            const deltaLat = (deltaY / mapHeight) * latRange;
            
            // Update center based on drag
            const newCenter = [
                startCenter[0] + deltaLat,
                startCenter[1] + deltaLng
            ];
            
            this.setView(newCenter, this.zoom);
        });
        
        document.addEventListener('mouseup', () => {
            if (isDragging) {
                isDragging = false;
                this.element.style.cursor = 'grab';
            }
        });
        
        // Set initial cursor
        this.element.style.cursor = 'grab';
    },
    
    _addMarkerClickHandlers: function() {
        const markerElements = this.element.querySelectorAll('.leaflet-marker');
        const popup = this.element.querySelector('#leaflet-popup');
        const popupTitle = this.element.querySelector('#popup-title');
        const popupContent = this.element.querySelector('#popup-content');
        const closeButton = this.element.querySelector('#popup-close');
        
        markerElements.forEach((markerElement, index) => {
            markerElement.addEventListener('click', (e) => {
                e.stopPropagation();
                const marker = this.markers[index];
                if (marker && marker.popup) {
                    // Set popup content first to get accurate dimensions
                    popupTitle.textContent = marker.name || 'Location';
                    popupContent.innerHTML = marker.popup;
                    
                    // Show popup temporarily to measure dimensions
                    popup.style.display = 'block';
                    popup.style.visibility = 'hidden';
                    
                    // Get actual popup dimensions
                    const popupRect = popup.getBoundingClientRect();
                    const popupWidth = popupRect.width;
                    const popupHeight = popupRect.height;
                    
                    // Get marker and container positions
                    const markerRect = markerElement.getBoundingClientRect();
                    const containerRect = this.element.getBoundingClientRect();
                    
                    // Calculate initial popup position relative to marker
                    let popupX = markerRect.left - containerRect.left + 25; // offset to right of marker
                    let popupY = markerRect.top - containerRect.top - popupHeight - 10; // above marker
                    
                    // Get container dimensions for boundary checks
                    const containerWidth = this.element.offsetWidth;
                    const containerHeight = this.element.offsetHeight;
                    
                    // Horizontal boundary checks and adjustments
                    const margin = 10;
                    if (popupX + popupWidth > containerWidth - margin) {
                        // Would go off right edge, position to left of marker
                        popupX = markerRect.left - containerRect.left - popupWidth - 10;
                    }
                    if (popupX < margin) {
                        // Would go off left edge, clamp to margin
                        popupX = margin;
                    }
                    
                    // Vertical boundary checks and adjustments
                    if (popupY < margin) {
                        // Would go off top edge, position below marker
                        popupY = markerRect.bottom - containerRect.top + 10;
                    }
                    if (popupY + popupHeight > containerHeight - margin) {
                        // Would go off bottom edge, adjust upward
                        popupY = containerHeight - popupHeight - margin;
                    }
                    
                    // Apply final position
                    popup.style.left = popupX + 'px';
                    popup.style.top = popupY + 'px';
                    
                    // Make popup visible
                    popup.style.visibility = 'visible';
                }
            });
        });
        
        // Close popup handlers
        if (closeButton) {
            closeButton.addEventListener('click', () => {
                popup.style.display = 'none';
            });
        }
        
        // Close popup when clicking outside
        this.element.addEventListener('click', (e) => {
            if (!e.target.closest('.leaflet-marker') && !e.target.closest('#leaflet-popup')) {
                popup.style.display = 'none';
            }
        });
    },
    
    _getBbox: function() {
        // Calculate bounding box based on center and zoom level
        // Use proper geographic calculations for better accuracy
        const scale = Math.pow(2, this.zoom);
        
        // Calculate degrees per pixel at this zoom level
        // At zoom 0, the world is 256 pixels wide and covers 360 degrees
        const degreesPerPixel = 360 / (256 * scale);
        
        // Get map dimensions
        const mapWidth = this.element ? this.element.offsetWidth : 400;
        const mapHeight = this.element ? this.element.offsetHeight : 400;
        
        // Calculate lat/lng offsets based on map size and zoom
        const lngOffset = (mapWidth / 2) * degreesPerPixel;
        const latOffset = (mapHeight / 2) * degreesPerPixel;
        
        const minLng = this.center[1] - lngOffset;
        const minLat = this.center[0] - latOffset;
        const maxLng = this.center[1] + lngOffset;
        const maxLat = this.center[0] + latOffset;
        
        return `${minLng},${minLat},${maxLng},${maxLat}`;
    },
    
    _getBboxAsNumbers: function() {
        // Get bbox as numbers for coordinate calculations
        const scale = Math.pow(2, this.zoom);
        const degreesPerPixel = 360 / (256 * scale);
        
        const mapWidth = this.element ? this.element.offsetWidth : 400;
        const mapHeight = this.element ? this.element.offsetHeight : 400;
        
        const lngOffset = (mapWidth / 2) * degreesPerPixel;
        const latOffset = (mapHeight / 2) * degreesPerPixel;
        
        return {
            minLng: this.center[1] - lngOffset,
            minLat: this.center[0] - latOffset,
            maxLng: this.center[1] + lngOffset,
            maxLat: this.center[0] + latOffset
        };
    },
    
    _getCategoryIcon: function(category) {
        const icons = {
            'attraction': '🏛️',
            'restaurant': '🍽️',
            'hidden': '💎',
            'cafe': '☕',
            'hotel': '🏨',
            'shop': '🛍️',
            'museum': '🏛️',
            'park': '🌳'
        };
        return icons[category] || '📍';
    },
    
    _latToPixel: function(lat) {
        // Use the exact same coordinate system as the OpenStreetMap iframe
        // This ensures perfect alignment regardless of view changes
        const mapHeight = this.element ? this.element.offsetHeight : 400;
        
        // Get the bbox that the iframe is actually displaying
        const bbox = this._getBboxAsNumbers();
        
        // Convert latitude using simple linear mapping within the visible bounds
        // This matches exactly how the iframe renders coordinates
        const latRange = bbox.maxLat - bbox.minLat;
        if (latRange === 0) return mapHeight / 2;
        
        const normalizedLat = (lat - bbox.minLat) / latRange;
        
        // Invert Y coordinate (north is up, pixels increase downward)
        return mapHeight * (1 - normalizedLat);
    },
    
    _lngToPixel: function(lng) {
        // Use the exact same coordinate system as the OpenStreetMap iframe
        // This ensures perfect alignment regardless of view changes
        const mapWidth = this.element ? this.element.offsetWidth : 400;
        
        // Get the bbox that the iframe is actually displaying
        const bbox = this._getBboxAsNumbers();
        
        // Convert longitude using simple linear mapping within the visible bounds
        // This matches exactly how the iframe renders coordinates
        const lngRange = bbox.maxLng - bbox.minLng;
        if (lngRange === 0) return mapWidth / 2;
        
        const normalizedLng = (lng - bbox.minLng) / lngRange;
        
        return mapWidth * normalizedLng;
    },
};

// Marker implementation
L.marker = function(latlng) {
    return new L.Marker(latlng);
};

L.Marker = function(latlng) {
    this.latlng = latlng;
    this.popup = null;
    this.name = '';
    this.category = '';
    this.description = '';
    return this;
};

L.Marker.prototype = {
    addTo: function(map) {
        map.markers.push(this);
        return this;
    },
    
    bindPopup: function(content, options) {
        this.popup = content;
        // Extract name and category from popup content
        const nameMatch = content.match(/<h5>(.*?)<\/h5>/);
        const categoryMatch = content.match(/<p class="small text-muted">(.*?)<\/p>/);
        const descMatch = content.match(/<p>(.*?)<\/p>/);
        
        if (nameMatch) this.name = nameMatch[1];
        if (categoryMatch) this.category = categoryMatch[1];
        if (descMatch) this.description = descMatch[1];
        
        return this;
    }
};

// Tile layer implementation
L.tileLayer = function(url, options) {
    return new L.TileLayer(url, options);
};

L.TileLayer = function(url, options) {
    this.url = url;
    this.options = options || {};
    return this;
};

L.TileLayer.prototype = {
    addTo: function(map) {
        map.tileLayer = this;
        // Trigger rendering after tile layer is added
        if (!map.initialized) {
            map._render();
        }
        return this;
    }
};