// Travel Map Initialization Script
// Uses real Leaflet.js library for interactive maps

document.addEventListener('DOMContentLoaded', function() {
    const mapContainer = document.getElementById('travel-map');
    
    if (!mapContainer) {
        return; // No map container
    }

    // Load Leaflet CSS and JS if not already loaded
    if (!window.L) {
        // Load Leaflet CSS
        const linkElement = document.createElement('link');
        linkElement.rel = 'stylesheet';
        linkElement.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
        document.head.appendChild(linkElement);
        
        // Load Leaflet JS
        const scriptElement = document.createElement('script');
        scriptElement.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
        scriptElement.onload = () => {
            console.log('Leaflet loaded, initializing map...');
            initializeMap();
        };
        document.head.appendChild(scriptElement);
    } else {
        // Leaflet already loaded
        initializeMap();
    }

    function initializeMap() {
        try {
            // Get places data from the container's data attribute
            const placesData = mapContainer.getAttribute('data-places');
            if (!placesData) {
                showMapError('No location data found');
                return;
            }

            const places = JSON.parse(placesData);
            if (!places || places.length === 0) {
                showMapError('No places to display');
                return;
            }

            // Clear any fallback content
            mapContainer.innerHTML = '';

            // Create real Leaflet map
            const map = L.map('travel-map').setView([41.8781, -87.6298], 12);

            // Add OpenStreetMap tiles
            L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
                maxZoom: 19,
                attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            }).addTo(map);

            // Calculate bounds for all places
            const bounds = [];
            const markers = [];

            places.forEach(place => {
                const lat = parseFloat(place.lat);
                const lng = parseFloat(place.lon);

                if (isNaN(lat) || isNaN(lng)) {
                    console.warn('Invalid coordinates for place:', place.name);
                    return;
                }

                bounds.push([lat, lng]);

                // Create popup content
                const popupContent = createPopupContent(place);

                // Create marker with proper Leaflet API
                const marker = L.marker([lat, lng]).addTo(map);
                marker.bindPopup(popupContent);
                
                markers.push(marker);
            });

            // Fit map to show all markers
            if (bounds.length > 0) {
                map.fitBounds(bounds, { padding: [20, 20] });
            }

            console.log(`Initialized travel map with ${markers.length} places using real Leaflet`);

        } catch (error) {
            console.error('Error initializing travel map:', error);
            showMapError('Error loading map');
        }
    }

    function createPopupContent(place) {
        // Create consistent popup HTML that matches the card design
        let content = `<h5>${escapeHtml(place.name)}</h5>`;
        
        if (place.category) {
            content += `<p class="small text-muted">${escapeHtml(place.category)}</p>`;
        }
        
        if (place.description) {
            content += `<p>${escapeHtml(place.description)}</p>`;
        }
        
        if (place.personalNote) {
            content += `
                <div class="alert alert-info" style="margin: 0.5rem 0; padding: 0.5rem; border-radius: 0.25rem; background-color: #d1ecf1; border: 1px solid #bee5eb; color: #0c5460;">
                    <strong>💡 Personal tip:</strong> ${escapeHtml(place.personalNote)}
                </div>
            `;
        }
        
        // Add practical info if available
        if (place.practicalInfo) {
            const info = place.practicalInfo;
            const details = [];
            
            if (info.price) details.push(`💰 ${escapeHtml(info.price)}`);
            if (info.hours) details.push(`🕒 ${escapeHtml(info.hours)}`);
            if (info.phone) details.push(`📞 <a href="tel:${escapeHtml(info.phone)}">${escapeHtml(info.phone)}</a>`);
            if (info.website) details.push(`🌐 <a href="${escapeHtml(info.website)}" target="_blank">Website</a>`);
            
            if (details.length > 0) {
                content += `<div class="mt-2">${details.map(d => `<span class="badge bg-secondary me-2">${d}</span>`).join('')}</div>`;
            }
        }
        
        // Add coordinate links
        content += `
            <div class="mt-2">
                <a href="geo:${place.lat},${place.lon}" class="btn btn-sm btn-primary me-1">📱 Open in App</a>
                <a href="https://www.openstreetmap.org/?mlat=${place.lat}&mlon=${place.lon}&zoom=18" target="_blank" class="btn btn-sm btn-outline-secondary">🗺️ Map</a>
            </div>
        `;
        
        return content;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function showMapError(message) {
        if (mapContainer) {
            mapContainer.innerHTML = `
                <div class="text-center text-muted d-flex align-items-center justify-content-center h-100">
                    <div>
                        <i class="bi bi-exclamation-triangle display-4 mb-2 text-warning"></i><br>
                        ${escapeHtml(message)}<br>
                        <small>Please try refreshing the page</small>
                    </div>
                </div>
            `;
        }
    }
});