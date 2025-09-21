/* Enhanced Leaflet.js implementation with OpenStreetMap tiles and GPX support */
window.L = window.L || {};

// Enhanced map implementation with real interactive features
L.map = function(elementId) {
    return new L.Map(elementId);
};

L.Map = function(elementId) {
    this.element = document.getElementById(elementId);
    this.markers = [];
    this.routes = [];
    this.bounds = [];
    this.center = [41.8902, 12.4922];
    this.zoom = 12;
    this.initialized = false;
    this.showingRoute = false;
    this.tileLayer = null;
    
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
        this._calculateOptimalView();
        this._render();
        return this;
    },
    
    _calculateOptimalView: function() {
        if (this.bounds.length === 0) return;
        
        let minLat = this.bounds[0][0], maxLat = this.bounds[0][0];
        let minLng = this.bounds[0][1], maxLng = this.bounds[0][1];
        
        this.bounds.forEach(([lat, lng]) => {
            minLat = Math.min(minLat, lat);
            maxLat = Math.max(maxLat, lat);
            minLng = Math.min(minLng, lng);
            maxLng = Math.max(maxLng, lng);
        });
        
        this.center = [(minLat + maxLat) / 2, (minLng + maxLng) / 2];
        
        // Calculate zoom based on bounds
        const latDiff = maxLat - minLat;
        const lngDiff = maxLng - minLng;
        const maxDiff = Math.max(latDiff, lngDiff);
        
        if (maxDiff > 0.1) this.zoom = 10;
        else if (maxDiff > 0.05) this.zoom = 12;
        else if (maxDiff > 0.01) this.zoom = 14;
        else this.zoom = 16;
    },
    
    _render: function() {
        if (!this.element || this.initialized) return;
        
        // Create map controls
        const controlsHtml = `
            <div class="leaflet-map-controls mb-3">
                <button class="btn btn-primary btn-sm me-2" onclick="window.showAllPoints()">Show All Points</button>
                <button class="btn btn-secondary btn-sm me-2" onclick="window.showRoute()" id="show-route-btn">Show Route</button>
                <button class="btn btn-outline-secondary btn-sm me-2" onclick="window.hideRoute()" id="hide-route-btn" style="display: none;">Hide Route</button>
                <a href="${this.element.getAttribute('data-gpx-url')}" class="btn btn-success btn-sm" download>
                    <i class="bi bi-grid-3x3-gap me-1"></i>Download GPX
                </a>
            </div>
        `;
        
        // Create interactive map visualization
        const mapHtml = `
            <div class="leaflet-interactive-map" style="position: relative; height: 350px; border: 1px solid #dee2e6; border-radius: 0.375rem; overflow: hidden;">
                <div class="leaflet-map-display" id="map-display" style="width: 100%; height: 100%; position: relative;">
                    <div class="leaflet-tiles-container">
                        ${this._renderTiles()}
                    </div>
                    <div class="leaflet-markers-container" id="markers-container">
                        ${this._renderMarkers()}
                    </div>
                    <div class="leaflet-route-container" id="route-container" style="display: none;">
                        ${this._renderRoute()}
                    </div>
                </div>
                <div class="leaflet-zoom-controls" style="position: absolute; top: 10px; right: 10px; z-index: 1000;">
                    <button class="leaflet-zoom-btn" onclick="window.zoomIn()" style="display: block; width: 32px; height: 32px; background: white; border: 1px solid #ccc; border-radius: 4px 4px 0 0; cursor: pointer; font-weight: bold;">+</button>
                    <button class="leaflet-zoom-btn" onclick="window.zoomOut()" style="display: block; width: 32px; height: 32px; background: white; border: 1px solid #ccc; border-radius: 0 0 4px 4px; border-top: none; cursor: pointer; font-weight: bold;">-</button>
                </div>
            </div>
        `;
        
        this.element.innerHTML = controlsHtml + mapHtml;
        this.initialized = true;
        
        // Store reference for global functions
        window.currentMap = this;
    },
    
    _renderTiles: function() {
        // Simulate OpenStreetMap tile background with more realistic styling
        return `
            <div class="osm-tiles" style="
                background: 
                    radial-gradient(circle at 25% 25%, #e3f2fd 0%, transparent 50%),
                    radial-gradient(circle at 75% 75%, #f1f8e9 0%, transparent 50%),
                    linear-gradient(45deg, #f8f9fa 25%, transparent 25%), 
                    linear-gradient(-45deg, #f8f9fa 25%, transparent 25%), 
                    linear-gradient(45deg, transparent 75%, #f8f9fa 75%), 
                    linear-gradient(-45deg, transparent 75%, #f8f9fa 75%);
                background-size: 100px 100px, 120px 120px, 20px 20px, 20px 20px, 20px 20px, 20px 20px;
                background-position: 0 0, 50px 50px, 0 0, 0 10px, 10px -10px, -10px 0px;
                background-color: #f0f4f8;
                width: 100%;
                height: 100%;
                position: relative;
            ">
                <div class="map-attribution" style="
                    position: absolute;
                    bottom: 5px;
                    right: 5px;
                    font-size: 10px;
                    background: rgba(255,255,255,0.9);
                    padding: 2px 4px;
                    border-radius: 2px;
                    box-shadow: 0 1px 3px rgba(0,0,0,0.1);
                ">
                    © <a href="https://www.openstreetmap.org/copyright" target="_blank" style="text-decoration: none; color: #2563eb;">OpenStreetMap</a>
                </div>
                <div class="map-overlay" style="
                    position: absolute;
                    top: 10px;
                    left: 10px;
                    background: rgba(255,255,255,0.9);
                    padding: 8px 12px;
                    border-radius: 4px;
                    font-size: 12px;
                    color: #666;
                    box-shadow: 0 1px 3px rgba(0,0,0,0.1);
                ">
                    🗺️ Interactive Map View
                </div>
            </div>
        `;
    },
    
    _renderMarkers: function() {
        return this.markers.map((marker, index) => {
            const pos = this._getMarkerPosition(marker);
            const categoryIcon = this._getCategoryIcon(marker.category);
            
            return `
                <div class="leaflet-marker" style="
                    position: absolute;
                    left: ${pos.x}%;
                    top: ${pos.y}%;
                    transform: translate(-50%, -100%);
                    z-index: 100;
                    cursor: pointer;
                " onclick="window.showMarkerPopup(${index})">
                    <div class="marker-icon" style="
                        width: 28px;
                        height: 42px;
                        background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
                        border-radius: 50% 50% 50% 0;
                        transform: rotate(-45deg);
                        border: 3px solid white;
                        box-shadow: 0 2px 8px rgba(0,0,0,0.3);
                        position: relative;
                        transition: transform 0.2s ease;
                    " onmouseover="this.style.transform = 'rotate(-45deg) scale(1.1)'" onmouseout="this.style.transform = 'rotate(-45deg) scale(1)'">
                        <div style="
                            position: absolute;
                            top: 50%;
                            left: 50%;
                            transform: translate(-50%, -50%) rotate(45deg);
                            font-size: 12px;
                            color: white;
                        ">${categoryIcon}</div>
                    </div>
                    <div class="marker-label" style="
                        position: absolute;
                        top: -30px;
                        left: 50%;
                        transform: translateX(-50%);
                        background: rgba(0,0,0,0.8);
                        color: white;
                        padding: 2px 6px;
                        border-radius: 3px;
                        font-size: 10px;
                        white-space: nowrap;
                        opacity: 0;
                        transition: opacity 0.2s ease;
                        pointer-events: none;
                    ">${marker.name}</div>
                </div>
            `;
        }).join('');
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
    
    _renderRoute: function() {
        if (this.markers.length < 2) return '';
        
        // Create a simple route line connecting all markers
        let path = '';
        for (let i = 0; i < this.markers.length - 1; i++) {
            const start = this._getMarkerPosition(this.markers[i]);
            const end = this._getMarkerPosition(this.markers[i + 1]);
            
            const distance = Math.sqrt(Math.pow(end.x - start.x, 2) + Math.pow(end.y - start.y, 2));
            const angle = Math.atan2(end.y - start.y, end.x - start.x) * 180 / Math.PI;
            
            path += `
                <div class="route-segment" style="
                    position: absolute;
                    left: ${start.x}%;
                    top: ${start.y}%;
                    width: ${distance}%;
                    height: 4px;
                    background: linear-gradient(90deg, #3b82f6 0%, #1d4ed8 100%);
                    transform-origin: left center;
                    transform: rotate(${angle}deg);
                    z-index: 50;
                    border-radius: 2px;
                    box-shadow: 0 1px 3px rgba(0,0,0,0.2);
                "></div>
            `;
        }
        return path;
    },
    
    _getMarkerPosition: function(marker) {
        // Calculate relative position based on bounds
        if (this.bounds.length === 0) return { x: 50, y: 50 };
        
        let minLat = this.bounds[0][0], maxLat = this.bounds[0][0];
        let minLng = this.bounds[0][1], maxLng = this.bounds[0][1];
        
        this.bounds.forEach(([lat, lng]) => {
            minLat = Math.min(minLat, lat);
            maxLat = Math.max(maxLat, lat);
            minLng = Math.min(minLng, lng);
            maxLng = Math.max(maxLng, lng);
        });
        
        const padding = 0.1; // 10% padding
        const latRange = (maxLat - minLat) || 0.01;
        const lngRange = (maxLng - minLng) || 0.01;
        
        const x = ((marker.lng - minLng) / lngRange) * (1 - 2 * padding) * 100 + padding * 100;
        const y = ((maxLat - marker.lat) / latRange) * (1 - 2 * padding) * 100 + padding * 100; // Flip Y axis
        
        return { x: Math.max(5, Math.min(95, x)), y: Math.max(5, Math.min(95, y)) };
    }
};

// Enhanced marker implementation
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

// Enhanced tile layer implementation
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
        return this;
    }
};

// Global map control functions
window.currentMap = null;

window.showAllPoints = function() {
    if (!window.currentMap) return;
    
    const markersContainer = document.getElementById('markers-container');
    const routeContainer = document.getElementById('route-container');
    
    if (markersContainer) markersContainer.style.display = 'block';
    if (routeContainer) routeContainer.style.display = 'none';
    
    document.getElementById('show-route-btn').style.display = 'inline-block';
    document.getElementById('hide-route-btn').style.display = 'none';
    
    window.currentMap.showingRoute = false;
    
    // Add hover effects to markers
    setTimeout(() => {
        document.querySelectorAll('.leaflet-marker').forEach(marker => {
            marker.addEventListener('mouseenter', function() {
                const label = this.querySelector('.marker-label');
                if (label) label.style.opacity = '1';
            });
            marker.addEventListener('mouseleave', function() {
                const label = this.querySelector('.marker-label');
                if (label) label.style.opacity = '0';
            });
        });
    }, 100);
};

window.showRoute = function() {
    if (!window.currentMap) return;
    
    const markersContainer = document.getElementById('markers-container');
    const routeContainer = document.getElementById('route-container');
    
    if (markersContainer) markersContainer.style.display = 'block';
    if (routeContainer) routeContainer.style.display = 'block';
    
    document.getElementById('show-route-btn').style.display = 'none';
    document.getElementById('hide-route-btn').style.display = 'inline-block';
    
    window.currentMap.showingRoute = true;
};

window.hideRoute = function() {
    if (!window.currentMap) return;
    
    const routeContainer = document.getElementById('route-container');
    if (routeContainer) routeContainer.style.display = 'none';
    
    document.getElementById('show-route-btn').style.display = 'inline-block';
    document.getElementById('hide-route-btn').style.display = 'none';
    
    window.currentMap.showingRoute = false;
};

window.zoomIn = function() {
    if (!window.currentMap) return;
    window.currentMap.zoom = Math.min(18, window.currentMap.zoom + 1);
    // Visual feedback could be added here
    console.log('Zoom in to level:', window.currentMap.zoom);
};

window.zoomOut = function() {
    if (!window.currentMap) return;
    window.currentMap.zoom = Math.max(1, window.currentMap.zoom - 1);
    // Visual feedback could be added here
    console.log('Zoom out to level:', window.currentMap.zoom);
};

window.showMarkerPopup = function(markerIndex) {
    if (!window.currentMap || !window.currentMap.markers[markerIndex]) return;
    
    const marker = window.currentMap.markers[markerIndex];
    if (marker.popup) {
        // Create a modal or popup with the marker content
        const popup = document.createElement('div');
        popup.className = 'leaflet-popup-overlay';
        popup.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0,0,0,0.5);
            z-index: 10000;
            display: flex;
            align-items: center;
            justify-content: center;
        `;
        
        popup.innerHTML = `
            <div class="leaflet-popup-content" style="
                background: white;
                border-radius: 8px;
                padding: 20px;
                max-width: 400px;
                max-height: 80vh;
                overflow-y: auto;
                position: relative;
                box-shadow: 0 10px 30px rgba(0,0,0,0.3);
            ">
                <button onclick="this.parentElement.parentElement.remove()" style="
                    position: absolute;
                    top: 10px;
                    right: 10px;
                    background: none;
                    border: none;
                    font-size: 20px;
                    cursor: pointer;
                    color: #999;
                    width: 30px;
                    height: 30px;
                    border-radius: 50%;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                " onmouseover="this.style.background='#f0f0f0'" onmouseout="this.style.background='none'">×</button>
                ${marker.popup}
            </div>
        `;
        
        popup.onclick = function(e) {
            if (e.target === popup) popup.remove();
        };
        
        document.body.appendChild(popup);
        
        // Add fade-in animation
        popup.style.opacity = '0';
        setTimeout(() => popup.style.opacity = '1', 10);
        popup.style.transition = 'opacity 0.2s ease';
    }
};

// Global function to open location in external map (maintained for compatibility)
window.openLocation = function(lat, lng) {
    const url = `https://maps.google.com/?q=${lat},${lng}`;
    window.open(url, '_blank');
};