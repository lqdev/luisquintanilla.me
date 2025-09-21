/* Minimal Leaflet.js-like implementation for travel maps */
window.L = window.L || {};

// Simple map implementation
L.map = function(elementId) {
    return new L.Map(elementId);
};

L.Map = function(elementId) {
    this.element = document.getElementById(elementId);
    this.markers = [];
    this.bounds = [];
    this.center = [41.8902, 12.4922];
    this.zoom = 12;
    this.initialized = false;
    
    return this;
};

L.Map.prototype = {
    setView: function(center, zoom) {
        this.center = center;
        this.zoom = zoom;
        return this;
    },
    
    addTo: function(map) {
        return this;
    },
    
    fitBounds: function(bounds, options) {
        this.bounds = bounds;
        this._render();
        return this;
    },
    
    _render: function() {
        if (!this.element || this.initialized) return;
        
        this.element.innerHTML = `
            <div class="leaflet-map-placeholder">
                <h4>🗺️ Interactive Travel Map</h4>
                <p>${this.markers.length} places marked</p>
                <div class="leaflet-places-list">
                    ${this.markers.map(marker => `
                        <div class="leaflet-place-item" onclick="window.openLocation(${marker.lat}, ${marker.lng})">
                            <div class="leaflet-place-name">${marker.name}</div>
                            <div class="leaflet-place-category">${marker.category}</div>
                            <div class="leaflet-place-coords">${marker.lat.toFixed(6)}, ${marker.lng.toFixed(6)}</div>
                        </div>
                    `).join('')}
                </div>
                <div class="mt-3">
                    <small class="text-muted">Click on any place above to view on external map</small>
                </div>
            </div>
        `;
        
        this.initialized = true;
    }
};

// Simple marker implementation
L.marker = function(latlng) {
    return new L.Marker(latlng);
};

L.Marker = function(latlng) {
    this.lat = latlng[0];
    this.lng = latlng[1];
    this.popup = null;
    this.name = '';
    this.category = '';
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
        
        if (nameMatch) this.name = nameMatch[1];
        if (categoryMatch) this.category = categoryMatch[1];
        
        return this;
    }
};

// Simple tile layer implementation (placeholder)
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
        return this;
    }
};

// Global function to open location in external map
window.openLocation = function(lat, lng) {
    const url = `https://maps.google.com/?q=${lat},${lng}`;
    window.open(url, '_blank');
};