// Travel Map Initialization Script
// Initializes interactive maps for travel collection pages

document.addEventListener('DOMContentLoaded', function() {
    const mapContainer = document.getElementById('travel-map');
    
    if (!mapContainer || !window.L) {
        return; // No map container or Leaflet not loaded
    }
    
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
        
        // Create map instance
        const map = L.map('travel-map');
        
        // Create markers for all places
        const markers = [];
        const bounds = [];
        
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
            
            // Create marker
            const marker = L.marker([lat, lng]).bindPopup(popupContent);
            marker.name = place.name;
            marker.category = place.category || 'attraction';
            
            markers.push(marker);
            marker.addTo(map);
        });
        
        // Fit map to show all markers with padding
        if (bounds.length > 0) {
            map.fitBounds(bounds, { padding: true });
        } else {
            // Fallback to default Rome location
            map.setView([41.8902, 12.4922], 12);
        }
        
        // Trigger rendering of the map
        if (typeof map._render === 'function') {
            map._render();
        }
        
        console.log(`Initialized travel map with ${markers.length} places`);
        
    } catch (error) {
        console.error('Error initializing travel map:', error);
        showMapError('Error loading map');
    }
});

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
        if (info.phone) details.push(`📞 ${escapeHtml(info.phone)}`);
        
        if (details.length > 0) {
            content += `<div class="mt-2">${details.map(d => `<span class="badge bg-secondary me-1">${d}</span>`).join('')}</div>`;
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
    const mapContainer = document.getElementById('travel-map');
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