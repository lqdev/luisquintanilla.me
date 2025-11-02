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
        
        // Simple zoom calculation based on bounds size
        const latDiff = maxLat - minLat;
        const lngDiff = maxLng - minLng;
        const maxDiff = Math.max(latDiff, lngDiff);
        
        let zoom = 16;
        if (maxDiff > 0.1) zoom = 10;
        else if (maxDiff > 0.05) zoom = 12;
        else if (maxDiff > 0.01) zoom = 14;
        
        this.setView([centerLat, centerLng], zoom);
        return this;
    },
    
    _render: function() {
        if (!this.element) return;
        
        // Create map with markers overlay
        const markersHtml = this.markers.map(marker => `
            <div style="position: absolute; top: ${this._latToPixel(marker.latlng[0])}px; left: ${this._lngToPixel(marker.latlng[1])}px; transform: translate(-50%, -100%); z-index: 100; cursor: pointer;" onclick="alert('${marker.name}\\n\\n${marker.description || 'Click for details'}')">
                <div style="background: #FF6B6B; color: white; width: 20px; height: 20px; border-radius: 50% 50% 50% 0; transform: rotate(-45deg); display: flex; align-items: center; justify-content: center; font-size: 12px; border: 2px solid white; box-shadow: 0 2px 8px rgba(0,0,0,0.3);">
                    <span style="transform: rotate(45deg);">${this._getCategoryIcon(marker.category)}</span>
                </div>
            </div>
        `).join('');
        
        // Create iframe with OpenStreetMap embed
        const mapHtml = `
            <div style="position: relative; width: 100%; height: 100%; border-radius: 0.375rem; overflow: hidden;">
                <iframe 
                    width="100%" 
                    height="100%" 
                    frameborder="0" 
                    scrolling="no" 
                    marginheight="0" 
                    marginwidth="0" 
                    src="https://www.openstreetmap.org/export/embed.html?bbox=${this._getBbox()}&amp;layer=mapnik&amp;marker=${this.center[0]},${this.center[1]}"
                    style="border: none;">
                </iframe>
                ${markersHtml}
                <div style="position: absolute; top: 10px; left: 10px; background: rgba(255,255,255,0.9); padding: 8px; border-radius: 4px; font-size: 12px; font-family: Arial, sans-serif;">
                    <a href="https://www.openstreetmap.org/?mlat=${this.center[0]}&mlon=${this.center[1]}&zoom=${this.zoom}" target="_blank" style="color: #0078A8; text-decoration: none;">View Larger Map</a>
                </div>
                <div style="position: absolute; bottom: 10px; right: 10px; background: rgba(255,255,255,0.9); padding: 4px 8px; border-radius: 4px; font-size: 11px; font-family: Arial, sans-serif;">
                    <a href="https://www.openstreetmap.org/copyright" target="_blank" style="color: #0078A8; text-decoration: none;">© OpenStreetMap</a>
                </div>
            </div>
        `;
        
        this.element.innerHTML = mapHtml;
        this.initialized = true;
    },
    
    _getBbox: function() {
        // Calculate bounding box based on center and zoom
        const zoomFactor = Math.pow(2, 15 - this.zoom) * 0.01;
        const latOffset = zoomFactor;
        const lngOffset = zoomFactor;
        
        const minLng = this.center[1] - lngOffset;
        const minLat = this.center[0] - latOffset;
        const maxLng = this.center[1] + lngOffset;
        const maxLat = this.center[0] + latOffset;
        
        return `${minLng},${minLat},${maxLng},${maxLat}`;
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
        // Simple projection for visualization (not geographically accurate)
        const mapHeight = this.element ? this.element.offsetHeight : 400;
        const latRange = 0.1; // Approximate range for Rome area
        const centerLat = this.center[0];
        const relativePos = (lat - centerLat) / latRange;
        return mapHeight / 2 - (relativePos * mapHeight * 0.8);
    },
    
    _lngToPixel: function(lng) {
        // Simple projection for visualization (not geographically accurate)
        const mapWidth = this.element ? this.element.offsetWidth : 400;
        const lngRange = 0.1; // Approximate range for Rome area
        const centerLng = this.center[1];
        const relativePos = (lng - centerLng) / lngRange;
        return mapWidth / 2 + (relativePos * mapWidth * 0.8);
    }
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