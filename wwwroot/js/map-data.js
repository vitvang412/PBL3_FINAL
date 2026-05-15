/**
 * map-data.js — Hiển thị cảnh báo an ninh lên bản đồ Goong Maps
 * Chức năng: Heatmap / Cluster / Individual markers + Time filter + Verify + Resolve
 * Phụ thuộc: window.MapCore (từ map-core.js phải load trước)
 */

// ═══════════════════════════════════════════════════════════
// CONSTANTS
// ═══════════════════════════════════════════════════════════

const ZOOM_HEATMAP_MAX = 22;   // Luôn giữ heatmap (mờ dần theo zoom)
const ZOOM_CLUSTER_MIN = 2;    // Bắt đầu hiện cụm từ zoom 2
const ZOOM_CLUSTER_MAX = 13;   // <=13: cluster
const ZOOM_MARKER_MIN = 14;    // >=14: individual markers

const TIME_PRESETS = { '1': 1, '6': 6, '24': 24, '168': 168, '0': 0 };
const API = '/api/alerts';

const SVG_ICONS = {
    'theft_motorbike': `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="6" cy="18" r="3"/><circle cx="18" cy="18" r="3"/><path d="M12 18h4l2-7h-4l-2 2h-4.5"/><path d="M11 13l-1.5-4h-2.5"/><path d="M14 11l1-5h3"/></svg>`,
    'pickpocket_robbery': `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 16v1a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2h2m4 0h4a2 2 0 0 1 2 2v2m-6 0h4"/><rect x="8" y="9" width="8" height="8" rx="1"/><circle cx="12" cy="13" r="1"/></svg>`,
    'burglary': `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>`,
    'street_racing': `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 8h18M3 12h18M3 16h18"/><path d="M13 8l3-3 3 3m-6 4l3-3 3 3m-6 4l3-3 3 3"/></svg>`,
    'fighting_disorder': `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 11V6a2 2 0 0 0-2-2v0a2 2 0 0 0-2 2v0"/><path d="M14 10V4a2 2 0 0 0-2-2v0a2 2 0 0 0-2 2v2"/><path d="M10 10.5V6a2 2 0 0 0-2-2v0a2 2 0 0 0-2 2v8"/><path d="M18 8a2 2 0 1 1 4 0v6a8 8 0 0 1-8 8h-2c-2.8 0-4.5-.86-5.99-2.34l-3.6-3.6a2 2 0 0 1 2.83-2.82L7 15"/></svg>`,
    'scam_tourist': `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>`,
    'overcharging': `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>`
};

const DEFAULT_ALERT_SVG = `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 9v4"/><path d="M12 17h.01"/><path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/></svg>`;

const ALERT_COLORS = {
    'theft_motorbike': '#ef4444',
    'pickpocket_robbery': '#ef4444',
    'burglary': '#ef4444',
    'street_racing': '#3b82f6',
    'fighting_disorder': '#3b82f6',
    'scam_tourist': '#f59e0b',
    'overcharging': '#f59e0b'
};

const POPUP_ICONS = {
    time: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>`,
    user: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>`,
    check: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>`,
    info: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>`,
    thumbsUp: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 9V5a3 3 0 0 0-3-3l-4 9v11h11.28a2 2 0 0 0 2-1.7l1.38-9a2 2 0 0 0-2-2.3zM7 22H4a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2h3"></path></svg>`,
    thumbsDown: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10 15v4a3 3 0 0 0 3 3l4-9V2H5.72a2 2 0 0 0-2 1.7l-1.38 9a2 2 0 0 0 2 2.3zm7-13h3a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2h-3"></path></svg>`
};
window.SVG_ICONS = SVG_ICONS;

// ═══════════════════════════════════════════════════════════
// STATE
// ═══════════════════════════════════════════════════════════

let currentHours = 24;
let alertsCache = [];
let alertTypesCache = [];
let currentMode = null;       // 'heatmap' | 'cluster' | 'marker'
let activeMarkers = [];       // goongjs.Marker instances
let currentUserId = null;     // từ JWT nếu đăng nhập
let currentUserRole = null;
let showHiddenAlerts = false;
let proximityWatchId = null;
let proximityEnabled = false;
let userLocationMarker = null;
let userLocationLatLng = null;
let proximityToastQueue = [];
let proximityToastActive = false;
let queuedProximityAlertIds = new Set();

const PROXIMITY_RADIUS_METERS = 300;
const PROXIMITY_ALERT_COOLDOWN_MS = 10 * 60 * 1000;
const PROXIMITY_TOAST_DURATION_MS = 8000;
const PROXIMITY_TOAST_GAP_MS = 2000;
const PROXIMITY_SOURCE_ID = 'user-proximity-source';
const PROXIMITY_CIRCLE_LAYER_ID = 'user-proximity-circle';
const PROXIMITY_CIRCLE_STROKE_LAYER_ID = 'user-proximity-circle-stroke';
const PROXIMITY_SESSION_KEY = 'proximityAlertCooldowns';

function isMapUserAuthenticated() {
    return !!localStorage.getItem('token') || !!window.APP_CONFIG?.isAuthenticated;
}

// ═══════════════════════════════════════════════════════════
// KHỞI TẠO
// ═══════════════════════════════════════════════════════════

function init() {
    // Parse JWT để lấy userId và role (nếu đăng nhập)
    const token = localStorage.getItem('token');
    if (token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            currentUserId = parseInt(
                payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
                || payload['sub']
                || payload['nameid']
            ) || null;
            currentUserRole = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
                || payload['role'] || null;
        } catch (_) { }
    }

    if (!currentUserRole && window.APP_CONFIG?.currentUserRole) {
        currentUserRole = window.APP_CONFIG.currentUserRole;
    }

    initHiddenAlertsToggle();
    initProximityToggle();

    // Đợi map sẵn sàng
    waitForMap(() => {
        const map = window.MapCore.getMap();

        map.on('moveend', updateMapView);
        map.on('zoomend', updateMapView);
        map.on('zoom', updateProximityCircleRadius);

        // Load types 1 lần
        fetchAlertTypes().then(() => {
            refresh();
        });

        // Time slider buttons
        document.querySelectorAll('.gm-timeslider__btn').forEach(btn => {
            btn.addEventListener('click', () => {
                document.querySelectorAll('.gm-timeslider__btn').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                setTimeWindow(parseInt(btn.dataset.hrs));
            });
        });
    });
}

function waitForMap(cb, retries = 20) {
    if (window.MapCore && window.MapCore.getMap && window.MapCore.getMap()) {
        cb();
    } else if (retries > 0) {
        setTimeout(() => waitForMap(cb, retries - 1), 300);
    }
}

function renderHiddenAlertsToggle() {
    const button = document.getElementById('btnToggleHiddenAlerts');
    if (!button) return;

    if (!isMapUserAuthenticated()) {
        button.style.display = 'none';
        showHiddenAlerts = false;
        return;
    }

    button.style.display = 'flex';
    button.classList.toggle('active', showHiddenAlerts);
    const textEl = button.querySelector('.gm-sidebar-text');
    if (textEl) textEl.textContent = showHiddenAlerts ? 'Tin ẩn: Bật' : 'Tin ẩn';
}

function initHiddenAlertsToggle() {
    const button = document.getElementById('btnToggleHiddenAlerts');
    if (!button) return;

    renderHiddenAlertsToggle();
    button.addEventListener('click', async () => {
        showHiddenAlerts = !showHiddenAlerts;
        renderHiddenAlertsToggle();
        await refresh();
    });
}

function initProximityToggle() {
    const button = document.getElementById('btnToggleProximityAlerts');
    if (!button) return;

    renderProximityToggle();
    button.addEventListener('click', toggleProximityAlerts);
}

function renderProximityToggle() {
    const button = document.getElementById('btnToggleProximityAlerts');
    if (!button) return;

    button.classList.toggle('active', proximityEnabled);
    const textEl = button.querySelector('.gm-sidebar-text');
    if (textEl) {
        textEl.innerHTML = proximityEnabled ? 'Cảnh báo vị trí:<br>Bật' : 'Bật cảnh báo<br>vị trí';
    }
}

function toggleProximityAlerts() {
    if (proximityEnabled) {
        disableProximityAlerts();
        showToast('Đã tắt cảnh báo vị trí', 'info');
        return;
    }

    if (!navigator.geolocation) {
        showToast('Trình duyệt không hỗ trợ định vị', 'error');
        return;
    }

    proximityWatchId = navigator.geolocation.watchPosition(
        handleProximityPosition,
        handleProximityError,
        {
            enableHighAccuracy: true,
            maximumAge: 10000,
            timeout: 15000
        }
    );

    proximityEnabled = true;
    renderProximityToggle();
    showToast('Đang bật cảnh báo vị trí', 'success');
}

function disableProximityAlerts() {
    if (proximityWatchId !== null) {
        navigator.geolocation.clearWatch(proximityWatchId);
        proximityWatchId = null;
    }

    proximityEnabled = false;
    userLocationLatLng = null;
    proximityToastQueue = [];
    proximityToastActive = false;
    queuedProximityAlertIds = new Set();
    removeUserLocationVisuals();
    renderProximityToggle();
}

function handleProximityError(err) {
    disableProximityAlerts();
    const message = err && err.code === err.PERMISSION_DENIED
        ? 'Bạn cần cho phép truy cập vị trí để dùng cảnh báo này'
        : 'Không thể theo dõi vị trí hiện tại';
    showToast(message, 'warning');
}

function handleProximityPosition(position) {
    const lat = position.coords.latitude;
    const lng = position.coords.longitude;
    userLocationLatLng = { lat, lng };

    upsertUserLocationMarker(lng, lat);
    upsertProximityCircle(lng, lat);
    updateProximityCircleRadius();
    queueNearbyAlerts(lat, lng);
}

function upsertUserLocationMarker(lng, lat) {
    if (!userLocationMarker) {
        userLocationMarker = new goongjs.Marker({ color: '#16a34a' })
            .setLngLat([lng, lat])
            .addTo(window.MapCore.getMap());
        return;
    }

    userLocationMarker.setLngLat([lng, lat]);
}

function upsertProximityCircle(lng, lat) {
    const map = window.MapCore.getMap();
    if (!map) return;

    const data = {
        type: 'FeatureCollection',
        features: [{
            type: 'Feature',
            geometry: { type: 'Point', coordinates: [lng, lat] },
            properties: {}
        }]
    };

    if (map.getSource(PROXIMITY_SOURCE_ID)) {
        map.getSource(PROXIMITY_SOURCE_ID).setData(data);
        return;
    }

    map.addSource(PROXIMITY_SOURCE_ID, { type: 'geojson', data });
    map.addLayer({
        id: PROXIMITY_CIRCLE_LAYER_ID,
        type: 'circle',
        source: PROXIMITY_SOURCE_ID,
        paint: {
            'circle-radius': 0,
            'circle-color': '#16a34a',
            'circle-opacity': 0.1
        }
    });
    map.addLayer({
        id: PROXIMITY_CIRCLE_STROKE_LAYER_ID,
        type: 'circle',
        source: PROXIMITY_SOURCE_ID,
        paint: {
            'circle-radius': 0,
            'circle-color': '#16a34a',
            'circle-opacity': 0,
            'circle-stroke-color': '#16a34a',
            'circle-stroke-width': 2,
            'circle-stroke-opacity': 0.4
        }
    });
}

function updateProximityCircleRadius() {
    const map = window.MapCore.getMap();
    if (!map || !userLocationLatLng) return;

    const radius = metersToPixelsAtLatitude(PROXIMITY_RADIUS_METERS, userLocationLatLng.lat, map.getZoom());
    if (map.getLayer(PROXIMITY_CIRCLE_LAYER_ID)) {
        map.setPaintProperty(PROXIMITY_CIRCLE_LAYER_ID, 'circle-radius', radius);
    }
    if (map.getLayer(PROXIMITY_CIRCLE_STROKE_LAYER_ID)) {
        map.setPaintProperty(PROXIMITY_CIRCLE_STROKE_LAYER_ID, 'circle-radius', radius);
    }
}

function metersToPixelsAtLatitude(meters, latitude, zoom) {
    const earthCircumference = 40075016.686;
    const latitudeRadians = latitude * Math.PI / 180;
    return meters / (earthCircumference * Math.cos(latitudeRadians) / Math.pow(2, zoom + 8));
}

function removeUserLocationVisuals() {
    const map = window.MapCore.getMap();

    if (userLocationMarker) {
        userLocationMarker.remove();
        userLocationMarker = null;
    }

    if (!map) return;
    if (map.getLayer(PROXIMITY_CIRCLE_STROKE_LAYER_ID)) map.removeLayer(PROXIMITY_CIRCLE_STROKE_LAYER_ID);
    if (map.getLayer(PROXIMITY_CIRCLE_LAYER_ID)) map.removeLayer(PROXIMITY_CIRCLE_LAYER_ID);
    if (map.getSource(PROXIMITY_SOURCE_ID)) map.removeSource(PROXIMITY_SOURCE_ID);
}

function getNearbyActiveAlerts(lat, lng) {
    return alertsCache
        .filter(alert => alert && (alert.status === 'VISIBLE_UNVERIFIED' || alert.status === 'VISIBLE_VERIFIED'))
        .map(alert => ({
            alert,
            distance: haversineDistanceMeters(lat, lng, Number(alert.latitude), Number(alert.longitude))
        }))
        .filter(item => Number.isFinite(item.distance) && item.distance <= PROXIMITY_RADIUS_METERS)
        .sort((a, b) => a.distance - b.distance);
}

function haversineDistanceMeters(lat1, lng1, lat2, lng2) {
    const toRadians = deg => deg * Math.PI / 180;
    const earthRadius = 6371000;
    const dLat = toRadians(lat2 - lat1);
    const dLng = toRadians(lng2 - lng1);
    const a = Math.sin(dLat / 2) ** 2
        + Math.cos(toRadians(lat1)) * Math.cos(toRadians(lat2)) * Math.sin(dLng / 2) ** 2;
    return 2 * earthRadius * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

function queueNearbyAlerts(lat, lng) {
    const nearbyAlerts = getNearbyActiveAlerts(lat, lng);
    nearbyAlerts.forEach(({ alert, distance }) => {
        if (!shouldNotifyProximityAlert(alert.id) || queuedProximityAlertIds.has(alert.id)) return;
        queuedProximityAlertIds.add(alert.id);
        proximityToastQueue.push({ alert, distance: Math.round(distance) });
    });

    processProximityToastQueue();
}

function shouldNotifyProximityAlert(alertId) {
    const cooldowns = readProximityCooldowns();
    const lastShownAt = cooldowns[String(alertId)] || 0;
    return Date.now() - lastShownAt >= PROXIMITY_ALERT_COOLDOWN_MS;
}

function markProximityAlertShown(alertId) {
    const cooldowns = readProximityCooldowns();
    cooldowns[String(alertId)] = Date.now();
    sessionStorage.setItem(PROXIMITY_SESSION_KEY, JSON.stringify(cooldowns));
}

function readProximityCooldowns() {
    try {
        const raw = sessionStorage.getItem(PROXIMITY_SESSION_KEY);
        const parsed = raw ? JSON.parse(raw) : {};
        const now = Date.now();
        Object.keys(parsed).forEach(key => {
            if (now - parsed[key] >= PROXIMITY_ALERT_COOLDOWN_MS) delete parsed[key];
        });
        return parsed;
    } catch (_) {
        return {};
    }
}

function processProximityToastQueue() {
    if (proximityToastActive || proximityToastQueue.length === 0) return;

    proximityToastActive = true;
    const nextItem = proximityToastQueue.shift();
    if (!nextItem) {
        proximityToastActive = false;
        return;
    }

    markProximityAlertShown(nextItem.alert.id);
    showProximityToast(nextItem.alert, nextItem.distance, () => {
        proximityToastActive = false;
        queuedProximityAlertIds.delete(nextItem.alert.id);
        setTimeout(processProximityToastQueue, PROXIMITY_TOAST_GAP_MS);
    });
}

function showProximityToast(alert, distance, onDone) {
    const toast = document.createElement('div');
    toast.className = 'gm-proximity-toast';
    toast.innerHTML = `
        <div class="gm-proximity-toast__icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="20" height="20">
                <path d="M12 2l9 16H3L12 2z"></path>
                <path d="M12 9v4"></path>
                <path d="M12 17h.01"></path>
            </svg>
        </div>
        <div class="gm-proximity-toast__body">
            <div class="gm-proximity-toast__message">Cảnh báo: ${escHtml(alert.alertTypeName || 'Sự cố')} cách bạn khoảng ${distance} mét. Hãy cẩn thận.</div>
            <div class="gm-proximity-toast__actions">
                <button class="gm-proximity-toast__focus">Xem trên bản đồ</button>
                <button class="gm-proximity-toast__close" aria-label="Đóng">Đóng</button>
            </div>
        </div>`;

    let isClosed = false;
    const finish = () => {
        if (isClosed) return;
        isClosed = true;
        if (toast.parentNode) toast.remove();
        onDone();
    };

    toast.querySelector('.gm-proximity-toast__focus')?.addEventListener('click', () => {
        focusAlertOnMap(alert.id);
        finish();
    });
    toast.querySelector('.gm-proximity-toast__close')?.addEventListener('click', finish);

    document.body.appendChild(toast);
    setTimeout(finish, PROXIMITY_TOAST_DURATION_MS);
}

function focusAlertOnMap(alertId) {
    const alert = alertsCache.find(item => item.id === alertId);
    if (!alert) {
        openAlert(alertId);
        return;
    }

    const lng = Number(alert.longitude);
    const lat = Number(alert.latitude);
    const map = window.MapCore.getMap();
    if (!map) return;

    map.flyTo({ center: [lng, lat], zoom: Math.max(map.getZoom(), 16), duration: 800, offset: [0, 150] });
    setTimeout(() => {
        window.MapCore.closePopup();
        window.MapCore.showPopup([lng, lat], buildMarkerPopup(alert));
    }, 450);
}

// ═══════════════════════════════════════════════════════════
// FETCH FUNCTIONS
// ═══════════════════════════════════════════════════════════

async function fetchMapAlerts(bounds, fromTime, toTime, includeHidden = false) {
    const token = localStorage.getItem('token');
    const params = new URLSearchParams({
        southLat: bounds.getSouth(),
        northLat: bounds.getNorth(),
        westLng: bounds.getWest(),
        eastLng: bounds.getEast(),
        fromTime: fromTime.toISOString(),
        toTime: toTime.toISOString(),
        includeHidden: includeHidden ? 'true' : 'false'
    });
    const res = await fetch(`${API}/map?${params}`, {
        headers: token ? { 'Authorization': `Bearer ${token}` } : undefined,
        credentials: 'same-origin'
    });

    if (includeHidden && (res.status === 401 || res.status === 403)) {
        showHiddenAlerts = false;
        renderHiddenAlertsToggle();
        showToast('Vui lòng đăng nhập lại để xem tin đã ẩn', 'warning');
        return await fetchMapAlerts(bounds, fromTime, toTime, false);
    }

    if (!res.ok) return [];
    return await res.json();
}

async function fetchHeatmapData(fromTime, toTime) {
    const params = new URLSearchParams({
        fromTime: fromTime.toISOString(),
        toTime: toTime.toISOString()
    });
    const res = await fetch(`${API}/heatmap?${params}`);
    if (!res.ok) return [];
    return await res.json();
}

async function fetchAlertDetail(id) {
    const res = await fetch(`${API}/${id}`);
    if (!res.ok) return null;
    return await res.json();
}

async function fetchAlertTypes() {
    if (alertTypesCache.length > 0) return alertTypesCache;
    try {
        const res = await fetch(`${API}/types`);
        if (res.ok) alertTypesCache = await res.json();
    } catch (_) { }
    return alertTypesCache;
}

async function sendVerification(alertId, type, lat, lng, comment) {
    const token = localStorage.getItem('token');
    if (!token) {
        showToast('Vui lòng đăng nhập để xác nhận sự cố', 'warning');
        return;
    }
    const res = await fetch(`${API}/${alertId}/verify`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify({ verificationType: type, latitude: lat, longitude: lng, comment })
    });
    const data = await res.json();
    if (data.success) {
        showToast(data.message, 'success');
        setTimeout(() => refresh(), 1000);
    } else {
        showToast(data.message, 'error');
    }
}

async function markResolved(alertId) {
    const token = localStorage.getItem('token');
    if (!token) { showToast('Vui lòng đăng nhập', 'warning'); return; }
    const res = await fetch(`${API}/${alertId}/resolve`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    const data = await res.json();
    if (data.success) {
        showToast('Đã đánh dấu sự cố đã xử lý', 'success');
        window.MapCore.closePopup();
        setTimeout(() => refresh(), 1000);
    } else {
        showToast(data.message, 'error');
    }
}

// ═══════════════════════════════════════════════════════════
// RENDER — HEATMAP
// ═══════════════════════════════════════════════════════════

function renderHeatmapLayer(points) {
    const map = window.MapCore.getMap();
    if (!map || !points.length) return;

    const geojson = {
        type: 'FeatureCollection',
        features: points.map(p => ({
            type: 'Feature',
            geometry: { type: 'Point', coordinates: [p.lng, p.lat] },
            properties: { intensity: p.intensity || 1 }
        }))
    };

    if (!map.getSource('alerts-heat')) {
        map.addSource('alerts-heat', { type: 'geojson', data: geojson });
    } else {
        map.getSource('alerts-heat').setData(geojson);
        return;
    }

    map.addLayer({
        id: 'alerts-heat-layer',
        type: 'heatmap',
        source: 'alerts-heat',
        maxzoom: ZOOM_HEATMAP_MAX + 1,
        paint: {
            // Weight theo intensity của điểm dữ liệu
            'heatmap-weight': ['interpolate', ['linear'], ['get', 'intensity'], 0, 0, 10, 1],

            // Intensity tăng mạnh khi zoom vào để bù đắp cho density giảm
            'heatmap-intensity': [
                'interpolate', ['linear'], ['zoom'],
                0,  1,
                10, 2,
                13, 4,
                16, 8,
                20, 12
            ],

            // Màu sắc gradient từ trong suốt → xanh → vàng → cam → đỏ
            'heatmap-color': [
                'interpolate', ['linear'], ['heatmap-density'],
                0,   'rgba(33,102,172,0)',
                0.15, 'rgb(103,169,207)',
                0.35, 'rgb(209,229,240)',
                0.5, 'rgb(255,201,101)',
                0.7, 'rgb(253,128,30)',
                0.85,'rgb(220,60,20)',
                1,   'rgb(178,24,43)'
            ],

            // Radius tăng mạnh theo zoom để điểm dữ liệu vẫn "phủ" đủ diện tích
            'heatmap-radius': [
                'interpolate', ['linear'], ['zoom'],
                0,  15,
                10, 40,
                13, 70,
                16, 130,
                20, 300
            ],

            // Opacity không đổi
            'heatmap-opacity': 0.85
        }
    });
}

// ═══════════════════════════════════════════════════════════
// RENDER — CLUSTER
// ═══════════════════════════════════════════════════════════

function renderClusterLayer(alerts) {
    const map = window.MapCore.getMap();
    if (!map) return;

    const geojson = {
        type: 'FeatureCollection',
        features: alerts.map(a => ({
            type: 'Feature',
            geometry: { type: 'Point', coordinates: [parseFloat(a.longitude), parseFloat(a.latitude)] },
            properties: { id: a.id, title: a.title, status: a.status, alertTypeSlug: a.alertTypeSlug || '' }
        }))
    };

    // Luôn đảm bảo Source được xóa và tạo mới để Layer nằm trên cùng
    if (map.getLayer('cluster-count')) map.removeLayer('cluster-count');
    if (map.getLayer('unclustered-point')) map.removeLayer('unclustered-point');
    if (map.getLayer('clusters-ring')) map.removeLayer('clusters-ring');
    if (map.getLayer('clusters-bg')) map.removeLayer('clusters-bg');
    if (map.getSource('alerts-cluster')) map.removeSource('alerts-cluster');

    map.addSource('alerts-cluster', {
        type: 'geojson',
        data: geojson,
        cluster: true,
        clusterMaxZoom: 13,
        clusterRadius: 60
    });

    // 1. Vòng tròn chính (Solid Circle với viền trắng dày)
    map.addLayer({
        id: 'clusters-bg',
        type: 'circle',
        source: 'alerts-cluster',
        filter: ['has', 'point_count'],
        paint: {
            'circle-color': [
                'step', ['get', 'point_count'],
                '#51bbd6', 10,  // Xanh dương nhạt (<10)
                '#f1f075', 30,  // Vàng (10-30)
                '#f28cb1'       // Hồng/Đỏ (>30)
            ],
            // Chỉnh lại màu theo ý bạn: Xanh lá -> Cam -> Đỏ
            'circle-color': [
                'step', ['get', 'point_count'],
                '#4ade80', 5,   // Xanh lá (<5)
                '#fb923c', 15,  // Cam (5-15)
                '#ef4444'       // Đỏ (>15)
            ],
            'circle-radius': [
                'step', ['get', 'point_count'],
                20, 5, 25, 15, 30
            ],
            'circle-stroke-width': 4,
            'circle-stroke-color': '#ffffff'
        }
    });

    // 2. Con số (Bold Black Text)
    map.addLayer({
        id: 'cluster-count',
        type: 'symbol',
        source: 'alerts-cluster',
        filter: ['has', 'point_count'],
        layout: {
            'text-field': '{point_count_abbreviated}',
            'text-size': 14,
            'text-allow-overlap': true,
            'text-ignore-placement': true
        },
        paint: {
            'text-color': '#000000',
            'text-halo-color': '#ffffff',
            'text-halo-width': 0.5
        }
    });

    // 3. Điểm lẻ tẻ (Small solid dot)
    map.addLayer({
        id: 'unclustered-point',
        type: 'circle',
        source: 'alerts-cluster',
        filter: ['!', ['has', 'point_count']],
        paint: {
            'circle-color': '#3b82f6',
            'circle-radius': 8,
            'circle-stroke-width': 3,
            'circle-stroke-color': '#ffffff'
        }
    });

    // Sự kiện click
    map.on('click', 'clusters-bg', (e) => {
        const features = map.queryRenderedFeatures(e.point, { layers: ['clusters-bg'] });
        const clusterId = features[0].properties.cluster_id;
        map.getSource('alerts-cluster').getClusterLeaves(clusterId, 100, 0, (err, leaves) => {
            if (err || !leaves || leaves.length === 0) return;
            const bounds = new window.goongjs.LngLatBounds();
            leaves.forEach(leaf => bounds.extend(leaf.geometry.coordinates));
            map.fitBounds(bounds, { padding: 80, maxZoom: 16 });
        });
    });

    map.on('click', 'unclustered-point', (e) => {
        const props = e.features[0].properties;
        const alert = alertsCache.find(a => a.id === props.id);
        if (alert) {
            map.flyTo({ center: e.features[0].geometry.coordinates, duration: 600, offset: [0, 150] });
            window.MapCore.showPopup(e.features[0].geometry.coordinates, buildMarkerPopup(alert));
        }
    });

    map.on('mouseenter', 'clusters-bg', () => map.getCanvas().style.cursor = 'pointer');
    map.on('mouseleave', 'clusters-bg', () => map.getCanvas().style.cursor = '');
    map.on('mouseenter', 'unclustered-point', () => map.getCanvas().style.cursor = 'pointer');
    map.on('mouseleave', 'unclustered-point', () => map.getCanvas().style.cursor = '');
}

// ═══════════════════════════════════════════════════════════
// RENDER — INDIVIDUAL MARKERS
// ═══════════════════════════════════════════════════════════

function renderIndividualMarkers(alerts) {
    const map = window.MapCore.getMap();
    if (!map) return;

    alerts.forEach(alert => {
        const el = buildMarkerElement(alert);
        const marker = new goongjs.Marker({ element: el })
            .setLngLat([parseFloat(alert.longitude), parseFloat(alert.latitude)])
            .addTo(map);

        // Lắng nghe sự kiện click trực tiếp lên element của marker
        el.addEventListener('click', (e) => {
            e.stopPropagation();

            // 1. Căn giữa màn hình: Bay tới vị trí marker để popup luôn hiện ở trung tâm
            map.flyTo({
                center: [parseFloat(alert.longitude), parseFloat(alert.latitude)],
                duration: 600,
                // Tăng offset lên 150px để đẩy marker xuống thấp, nhường chỗ cho popup cao
                offset: [0, 150]
            });

            // 2. Hiển thị popup
            const html = buildMarkerPopup(alert);
            window.MapCore.showPopup(
                [parseFloat(alert.longitude), parseFloat(alert.latitude)],
                html
            );
        });

        activeMarkers.push(marker);
    });
}

function buildMarkerElement(alert) {
    const el = document.createElement('div');
    el.className = 'gm-alert-marker';

    const state = getAlertState(alert);
    const isNew = alert.createdAt && (Date.now() - new Date(alert.createdAt).getTime()) < 30 * 60 * 1000;
    const accentColor = ALERT_COLORS[alert.alertTypeSlug] || '#64748b';
    const slugSvg = window.SVG_ICONS ? window.SVG_ICONS[alert.alertTypeSlug] : (typeof SVG_ICONS !== 'undefined' ? SVG_ICONS[alert.alertTypeSlug] : null);
    const iconSvg = (slugSvg || DEFAULT_ALERT_SVG).replace('<svg ', `<svg style="color:${accentColor}" `);

    if (state.isResolved) el.classList.add('gm-alert-marker--resolved');
    else if (state.isHidden) el.classList.add('gm-alert-marker--hidden');
    else if (state.isUnverified) el.classList.add('gm-alert-marker--unverified');
    if (isNew && !state.isResolved) el.classList.add('gm-alert-marker--pulse');

    let inner = '';
    if (alert.iconUrl) {
        inner += `<img src="${alert.iconUrl}" class="gm-marker-icon-svg" style="width:24px;height:24px;" />`;
    } else {
        inner += `<div class="gm-marker-svg-icon" style="width:24px;height:24px;display:flex;align-items:center;justify-content:center;color:${accentColor};">${iconSvg}</div>`;
    }

    if (state.isResolved) {
        inner += `<span class="gm-marker-label gm-marker-label--resolved">Đã xử lý</span>`;
    } else if (state.isHidden) {
        inner += `<span class="gm-marker-label gm-marker-label--hidden">Đã ẩn</span>`;
    } else if (state.isUnverified) {
        inner += `<span class="gm-marker-label gm-marker-label--info">Chưa xác thực</span>`;
    } else if (state.needsInfo) {
        inner += `<span class="gm-marker-label gm-marker-label--info">Cần bổ sung</span>`;
    }

    if (state.isHidden) {
        el.style.opacity = '0.5';
    } else if (alert.opacity) {
        el.style.opacity = (alert.opacity / 100).toString();
    }

    el.innerHTML = inner;
    el.title = alert.title;
    return el;
}

function isVideo(url) {
    if (!url) return false;
    const videoExtensions = ['.mp4', '.webm', '.ogg', '.mov', '.m4v'];
    return videoExtensions.some(ext => url.toLowerCase().includes(ext));
}

function getAlertState(alert) {
    const status = alert.status || '';
    return {
        isResolved: status === 'RESOLVED',
        isVerified: status === 'VISIBLE_VERIFIED',
        isUnverified: status === 'VISIBLE_UNVERIFIED',
        isPending: status === 'PENDING_REVIEW',
        needsInfo: status === 'NEEDS_MORE_INFO' || status === 'NOT_ENOUGH_EVIDENCE',
        isHidden: status === 'REJECTED' || status === 'EXPIRED'
    };
}

function buildMarkerPopup(alert) {
    const timeStr = alert.createdAt
        ? new Date(alert.createdAt).toLocaleString('vi-VN')
        : 'Không rõ';
    const desc = alert.description
        ? (alert.description.length > 150 ? alert.description.slice(0, 150) + '...' : alert.description)
        : '';
    const state = getAlertState(alert);

    const warningHtml = state.needsInfo
        ? `<div class="gm-popup-warning">
            <div class="warning-ico">${POPUP_ICONS.info}</div>
            <div><b>Cảnh báo:</b> Cần thêm bằng chứng để xác thực báo cáo này.</div>
           </div>`
        : state.isPending
            ? `<div class="gm-popup-warning">
            <div class="warning-ico">${POPUP_ICONS.info}</div>
            <div><b>Trạng thái:</b> Báo cáo đang chờ kiểm duyệt trước khi hiển thị công khai.</div>
           </div>`
            : '';

    const mediaHtml = (alert.mediaUrls && alert.mediaUrls.length > 0)
        ? `<div class="gm-popup-media">
            ${alert.mediaUrls.map(url => {
            if (isVideo(url)) {
                return `<div class="gm-popup-media-item gm-popup-video-wrap">
                        <video src="${url}" controls muted></video>
                    </div>`;
            }
            return `<div class="gm-popup-media-item">
                    <img src="${url}" onclick="window.open('${url}','_blank')" alt="Bằng chứng"/>
                </div>`;
        }).join('')}
           </div>`
        : '';

    const token = localStorage.getItem('token');
    const isLoggedIn = !!token;
    const isOwner = currentUserId && alert.userId === currentUserId;
    const isAdmin = currentUserRole === 'Admin';

    const verifyBtns = isLoggedIn && !state.isResolved && !state.isPending && !state.isHidden ? `
        <div class="gm-alert-popup__verify-section">
            <div class="verify-label">Bạn có ghi nhận sự cố này không?</div>
            <div class="gm-alert-popup__verify-btns">
                <button class="gm-btn-confirm" onclick="window.MapData._verify(${alert.id},'CONFIRM')" title="Xác nhận">
                    <span class="btn-icon">${POPUP_ICONS.thumbsUp}</span>
                    Có <span>(${alert.confirmCount || 0})</span>
                </button>
                <button class="gm-btn-deny" onclick="window.MapData._verify(${alert.id},'DENY')" title="Phủ nhận">
                    <span class="btn-icon">${POPUP_ICONS.thumbsDown}</span>
                    Không <span>(${alert.denyCount || 0})</span>
                </button>
            </div>
        </div>` : !isLoggedIn ? `
        <div class="gm-popup-login-hint">
            ${POPUP_ICONS.info} <a href="/Auth/Login">Đăng nhập</a> để tham gia xác thực
        </div>` : '';

    const resolveBtn = (isOwner || isAdmin) && !state.isResolved && !state.isHidden ? `
        <button class="gm-btn-resolve" onclick="window.MapData._resolve(${alert.id})">
            <span class="btn-icon">${POPUP_ICONS.check}</span> Đã xử lý xong
        </button>` : '';

    const reportSection = `
        <div class="gm-alert-popup__report-section">
            <button class="gm-btn-report" onclick="window.MapData._toggleReportForm(${alert.id})">
                <span class="btn-icon">${POPUP_ICONS.info}</span> Báo cáo vi phạm
            </button>
            <div class="gm-report-form" id="report-form-${alert.id}" style="display:none">
                <label class="gm-report-label" for="report-reason-${alert.id}">Lý do báo cáo</label>
                <select id="report-reason-${alert.id}" class="gm-report-select">
                    <option value="">Chọn lý do</option>
                    <option value="THONG_TIN_SAI_SU_THAT">Thông tin sai sự thật</option>
                    <option value="NOI_DUNG_XUC_PHAM">Nội dung xúc phạm</option>
                    <option value="SPAM">Spam</option>
                    <option value="NGUOI_DUNG_GIA_MAO">Người dùng giả mạo</option>
                    <option value="KHAC">Khác</option>
                </select>
                <label class="gm-report-label" for="report-description-${alert.id}">Mô tả thêm</label>
                <textarea id="report-description-${alert.id}" class="gm-report-textarea" maxlength="300" placeholder="Mô tả thêm nếu cần (tối đa 300 ký tự)"></textarea>
                <div class="gm-report-actions">
                    <button class="gm-btn-report-submit" onclick="window.MapData._submitReport(${alert.id})">Gửi</button>
                    <button class="gm-btn-report-cancel" onclick="window.MapData._cancelReportForm(${alert.id})">Hủy</button>
                </div>
            </div>
        </div>`;

    const statusBadge = state.isResolved
        ? `<span class="gm-popup-status gm-popup-status--resolved">${POPUP_ICONS.check} Đã xử lý</span>`
        : state.needsInfo
            ? `<span class="gm-popup-status gm-popup-status--info">${POPUP_ICONS.info} Thiếu kiểm chứng</span>`
            : state.isPending
                ? `<span class="gm-popup-status gm-popup-status--info">${POPUP_ICONS.info} Chờ kiểm duyệt</span>`
                : state.isHidden
                    ? `<span class="gm-popup-status gm-popup-status--info">${POPUP_ICONS.info} Đã ẩn</span>`
                    : state.isUnverified
                        ? `<span class="gm-popup-status gm-popup-status--unverified">${POPUP_ICONS.info} Chưa xác thực</span>`
                        : `<span class="gm-popup-status gm-popup-status--verified">${POPUP_ICONS.check} Đã xác thực</span>`;

    const accentColor = ALERT_COLORS[alert.alertTypeSlug] || '#64748b';
    const slugSvg = window.SVG_ICONS ? window.SVG_ICONS[alert.alertTypeSlug] : (typeof SVG_ICONS !== 'undefined' ? SVG_ICONS[alert.alertTypeSlug] : null);
    const iconSvg = (slugSvg || DEFAULT_ALERT_SVG).replace('<svg ', `<svg style="color:${accentColor}" `);

    const iconHtml = alert.iconUrl
        ? `<div class="gm-popup-icon-box" style="background:${accentColor}15; color:${accentColor}; border-color:${accentColor}30;">
            <img src="${alert.iconUrl}" style="width:20px;height:20px;" />
        </div>`
        : `<div class="gm-popup-icon-box" style="background:${accentColor}15; color:${accentColor}; border-color:${accentColor}30;">${iconSvg}</div>`;

    // Trust score badge
    const trustLabel = alert.trustScore >= 30 ? 'Cao' : alert.trustScore >= 10 ? 'Trung bình' : 'Thấp';
    const trustColor = alert.trustScore >= 30 ? '#16a34a' : alert.trustScore >= 10 ? '#f59e0b' : '#ef4444';

    // User reputation
    const repScore = alert.userReputationScore ?? 5;
    const repLabel = repScore >= 10 ? 'Uy tín cao' : repScore >= 5 ? 'Bình thường' : 'Mới';

    // Comment section (only for visible alerts)
    const commentSection = (!state.isHidden) ? `
        <div class="gm-popup-comments-section" id="comments-section-${alert.id}">
            <div class="gm-popup-comments-header" onclick="window.MapData._toggleComments(${alert.id})">
                <span>Bình luận</span>
                <span class="gm-comments-toggle-ico">&#9660;</span>
            </div>
            <div class="gm-popup-comments-body" id="comments-body-${alert.id}" style="display:none">
                <div class="gm-comments-list" id="comments-list-${alert.id}">
                    <div class="gm-comments-loading">Đang tải...</div>
                </div>
                ${isLoggedIn ? `
                <div class="gm-comment-form">
                    <input type="text" id="comment-input-${alert.id}" placeholder="Viết bình luận..." maxlength="500" class="gm-comment-input" />
                    <label class="gm-comment-attach" title="Đính kèm ảnh/video">
                        <input type="file" id="comment-file-${alert.id}" accept="image/*,video/*" style="display:none" onchange="window.MapData._previewCommentFile(${alert.id})" />
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16">
                            <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48"></path>
                        </svg>
                    </label>
                    <button class="gm-comment-send" onclick="window.MapData._postComment(${alert.id})">Gửi</button>
                </div>
                <div class="gm-comment-file-preview" id="comment-file-preview-${alert.id}" style="display:none">
                    <span id="comment-file-name-${alert.id}"></span>
                    <button class="gm-comment-file-remove" onclick="window.MapData._removeCommentFile(${alert.id})">x</button>
                </div>` : `
                <div class="gm-popup-login-hint">
                    <a href="/Auth/Login">Đăng nhập</a> để bình luận
                </div>`}
            </div>
        </div>` : '';

    return `
        <div class="gm-alert-popup">
            <div class="gm-popup-header">
                ${iconHtml}
                <div class="header-main">
                    <div class="gm-popup-type" style="color:${accentColor}">${escHtml(alert.alertTypeName || '')}</div>
                    <div class="gm-popup-title">${escHtml(alert.title)}</div>
                </div>
                ${statusBadge}
            </div>

            <div class="gm-popup-content-body">
                <div class="gm-popup-meta">
                    <div class="meta-item"><span class="meta-ico">${POPUP_ICONS.time}</span> ${timeStr}</div>
                    <div class="meta-item"><span class="meta-ico">${POPUP_ICONS.user}</span> ${escHtml(alert.userName || 'Ẩn danh')} <span class="gm-rep-badge" title="Điểm uy tín: ${repScore}">${repLabel}</span></div>
                    <div class="meta-item"><span class="meta-ico">${POPUP_ICONS.check}</span> Độ tin cậy: <span style="color:${trustColor};font-weight:700">${alert.trustScore ?? 0} (${trustLabel})</span></div>
                </div>

                ${warningHtml}
                ${desc ? `<div class="gm-popup-desc">${escHtml(desc)}</div>` : ''}
                ${mediaHtml}

                ${verifyBtns}
                ${resolveBtn}
                ${reportSection}
                ${commentSection}
            </div>
        </div>`;
}

function escHtml(str) {
    if (!str) return '';
    const d = document.createElement('div');
    d.textContent = str;
    return d.innerHTML;
}

// ═══════════════════════════════════════════════════════════
// CONTROL FUNCTIONS
// ═══════════════════════════════════════════════════════════

function clearAllLayers() {
    const map = window.MapCore.getMap();
    if (!map) return;

    // Xóa markers
    activeMarkers.forEach(m => m.remove());
    activeMarkers = [];

    // Xóa cluster layers/source
    ['cluster-count', 'unclustered-point', 'clusters-ring', 'clusters-bg', 'clusters-glow'].forEach(id => {
        if (map.getLayer(id)) map.removeLayer(id);
    });
    if (map.getSource('alerts-cluster')) map.removeSource('alerts-cluster');

    // Xóa heatmap layers/source
    if (map.getLayer('alerts-heat-layer')) map.removeLayer('alerts-heat-layer');
    if (map.getSource('alerts-heat')) map.removeSource('alerts-heat');
}

async function updateMapView() {
    const map = window.MapCore.getMap();
    if (!map) return;

    const zoom = map.getZoom();
    let newMode;
    if (zoom <= 10) newMode = 'heatmap';
    else if (zoom <= ZOOM_CLUSTER_MAX) newMode = 'cluster';
    else newMode = 'marker';

    currentMode = newMode;
    clearAllLayers();
    await refreshData();
}

async function refreshData() {
    const map = window.MapCore.getMap();
    if (!map) return;

    const now = new Date();
    const fromTime = currentHours === 0
        ? new Date(0)
        : new Date(now.getTime() - currentHours * 3600 * 1000);
    const bounds = map.getBounds();

    clearAllLayers();

    // 1. Luôn vẽ Heatmap ở lớp nền
    const points = await fetchHeatmapData(fromTime, now);
    renderHeatmapLayer(points);

    // 2. Vẽ thêm các lớp tương tác (Cluster hoặc Marker) tùy theo mức Zoom
    const zoom = map.getZoom();
    const alerts = await fetchMapAlerts(bounds, fromTime, now, showHiddenAlerts);
    alertsCache = alerts;

    if (zoom >= ZOOM_MARKER_MIN) {
        renderIndividualMarkers(alerts);
    } else {
        // Chế độ Cluster (hiện số lượng) — chạy từ zoom 2 trở lên
        renderClusterLayer(alerts);
    }
    console.log(`[MapData] Fetched ${alerts.length} alerts. Zoom: ${zoom}`);

    // Update alert count badge
    updateAlertCount();
}

function setTimeWindow(hours) {
    currentHours = hours;
    refresh();
}

function updateAlertCount() {
    const el = document.getElementById('gmAlertCount');
    if (!el) return;
    const count = alertsCache.length;
    el.textContent = count > 0 ? `${count} báo cáo` : '';
    el.style.display = count > 0 ? 'inline-flex' : 'none';
}

async function refresh() {
    const map = window.MapCore.getMap();
    if (!map) return;

    const zoom = map.getZoom();
    if (zoom <= 10) currentMode = 'heatmap';
    else if (zoom <= ZOOM_CLUSTER_MAX) currentMode = 'cluster';
    else currentMode = 'marker';

    await refreshData();
}

async function openAlert(id) {
    const alert = await fetchAlertDetail(id);
    if (!alert) { showToast('Không tìm thấy sự cố', 'error'); return; }
    const html = buildMarkerPopup(alert);
    window.MapCore.showPopup(
        [parseFloat(alert.longitude), parseFloat(alert.latitude)],
        html
    );
}

// ═══════════════════════════════════════════════════════════
// TOAST NOTIFICATION
// ═══════════════════════════════════════════════════════════

function showToast(msg, type = 'info') {
    const colors = { success: '#22c55e', error: '#ef4444', warning: '#f59e0b', info: '#3b82f6' };
    const el = document.createElement('div');
    el.style.cssText = `
        position:fixed;bottom:80px;right:24px;z-index:9999;
        padding:12px 20px;border-radius:12px;font-size:14px;font-weight:600;
        background:${colors[type] || colors.info};color:#fff;
        box-shadow:0 4px 20px rgba(0,0,0,.25);
        animation:gmSlideIn .3s ease;max-width:320px;word-break:break-word;
    `;
    el.textContent = msg;
    document.body.appendChild(el);
    setTimeout(() => { el.style.opacity = '0'; el.style.transition = 'opacity .3s'; setTimeout(() => el.remove(), 300); }, 3500);
}

// CSS animation cho toast
const toastStyle = document.createElement('style');
toastStyle.textContent = `@keyframes gmSlideIn{from{transform:translateX(100%);opacity:0}to{transform:translateX(0);opacity:1}}`;
document.head.appendChild(toastStyle);

function toggleReportForm(alertId) {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = '/Auth/Login';
        return;
    }

    const form = document.getElementById(`report-form-${alertId}`);
    if (!form) return;
    form.style.display = form.style.display === 'none' ? 'flex' : 'none';
}

function cancelReportForm(alertId) {
    const form = document.getElementById(`report-form-${alertId}`);
    const reason = document.getElementById(`report-reason-${alertId}`);
    const description = document.getElementById(`report-description-${alertId}`);

    if (reason) reason.value = '';
    if (description) description.value = '';
    if (form) form.style.display = 'none';
}

async function submitReport(alertId) {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = '/Auth/Login';
        return;
    }

    const reasonEl = document.getElementById(`report-reason-${alertId}`);
    const descriptionEl = document.getElementById(`report-description-${alertId}`);
    const reason = reasonEl ? reasonEl.value : '';
    const description = descriptionEl ? descriptionEl.value.trim() : '';

    if (!reason) {
        showToast('Vui lòng chọn lý do báo cáo', 'warning');
        if (reasonEl) reasonEl.focus();
        return;
    }

    if (description.length > 300) {
        showToast('Mô tả thêm tối đa 300 ký tự', 'warning');
        return;
    }

    try {
        const res = await fetch(`${API}/${alertId}/report`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            credentials: 'same-origin',
            body: JSON.stringify({ reason, description })
        });

        const data = await res.json();
        if (!res.ok || !data.success) throw new Error(data.message || 'Không thể gửi báo cáo vi phạm');

        cancelReportForm(alertId);
        showToast(data.message || 'Đã gửi báo cáo vi phạm', 'success');
    } catch (err) {
        showToast(err.message || 'Không thể gửi báo cáo vi phạm', 'error');
    }
}

// ═══════════════════════════════════════════════════════════
// COMMENTS
// ═══════════════════════════════════════════════════════════

async function toggleComments(alertId) {
    const body = document.getElementById(`comments-body-${alertId}`);
    if (!body) return;

    const isHidden = body.style.display === 'none';
    body.style.display = isHidden ? 'block' : 'none';

    if (isHidden) {
        await loadComments(alertId);
    }
}

async function loadComments(alertId) {
    const list = document.getElementById(`comments-list-${alertId}`);
    if (!list) return;

    list.innerHTML = '<div class="gm-comments-loading">Đang tải...</div>';

    try {
        const res = await fetch(`${API}/${alertId}/comments`);
        if (!res.ok) throw new Error('Loi tai binh luan');
        const comments = await res.json();
        renderComments(alertId, comments);
    } catch (err) {
        list.innerHTML = '<div class="gm-comments-loading">Không thể tải bình luận</div>';
    }
}

function renderComments(alertId, comments) {
    const list = document.getElementById(`comments-list-${alertId}`);
    if (!list) return;

    if (!comments || comments.length === 0) {
        list.innerHTML = '<div class="gm-comments-empty">Chưa có bình luận nào</div>';
        return;
    }

    list.innerHTML = comments.map(c => {
        const repLabel = c.userReputation >= 10 ? 'Uy tín' : '';
        const timeAgo = getTimeAgo(c.createdAt);

        let mediaHtml = '';
        if (c.mediaUrl) {
            if (isVideo(c.mediaUrl)) {
                mediaHtml = `<div class="gm-comment-media"><video src="${c.mediaUrl}" controls muted></video></div>`;
            } else {
                mediaHtml = `<div class="gm-comment-media"><img src="${c.mediaUrl}" onclick="window.open('${c.mediaUrl}','_blank')" /></div>`;
            }
        }

        return `<div class="gm-comment-item">
            <div class="gm-comment-header">
                <span class="gm-comment-user">${escHtml(c.userName)}</span>
                ${repLabel ? `<span class="gm-rep-badge">${repLabel}</span>` : ''}
                <span class="gm-comment-time">${timeAgo}</span>
            </div>
            <div class="gm-comment-content">${escHtml(c.content)}</div>
            ${mediaHtml}
        </div>`;
    }).join('');
}

function getTimeAgo(dateStr) {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'Vừa xong';
    if (mins < 60) return `${mins} phút trước`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs} giờ trước`;
    const days = Math.floor(hrs / 24);
    return `${days} ngày trước`;
}

function previewCommentFile(alertId) {
    const fileInput = document.getElementById(`comment-file-${alertId}`);
    const preview = document.getElementById(`comment-file-preview-${alertId}`);
    const nameEl = document.getElementById(`comment-file-name-${alertId}`);
    if (!fileInput || !preview || !nameEl) return;

    const file = fileInput.files[0];
    if (file) {
        nameEl.textContent = file.name.length > 20 ? file.name.slice(0, 17) + '...' : file.name;
        preview.style.display = 'flex';
    }
}

function removeCommentFile(alertId) {
    const fileInput = document.getElementById(`comment-file-${alertId}`);
    const preview = document.getElementById(`comment-file-preview-${alertId}`);
    if (fileInput) fileInput.value = '';
    if (preview) preview.style.display = 'none';
}

async function postComment(alertId) {
    const input = document.getElementById(`comment-input-${alertId}`);
    const fileInput = document.getElementById(`comment-file-${alertId}`);
    const content = input ? input.value.trim() : '';
    const file = fileInput ? fileInput.files[0] : null;

    if (!content && !file) return;

    const token = localStorage.getItem('token');
    if (!token) { showToast('Vui lòng đăng nhập', 'warning'); return; }

    if (input) input.disabled = true;

    try {
        const formData = new FormData();
        formData.append('content', content);
        if (file) formData.append('mediaFile', file);

        const res = await fetch(`${API}/${alertId}/comments`, {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${token}` },
            credentials: 'same-origin',
            body: formData
        });

        const data = await res.json();
        if (!res.ok || !data.success) throw new Error(data.message || 'Lỗi gửi bình luận');

        if (input) input.value = '';
        removeCommentFile(alertId);
        await loadComments(alertId);
        showToast('Đã gửi bình luận', 'success');
    } catch (err) {
        showToast(err.message || 'Không thể gửi bình luận', 'error');
    } finally {
        if (input) { input.disabled = false; input.focus(); }
    }
}

// ═══════════════════════════════════════════════════════════
// EXPORT
// ═══════════════════════════════════════════════════════════

window.MapData = {
    refresh,
    setTimeWindow,
    openAlert,
    _verify: sendVerification,
    _resolve: markResolved,
    _toggleReportForm: toggleReportForm,
    _cancelReportForm: cancelReportForm,
    _submitReport: submitReport,
    _toggleComments: toggleComments,
    _postComment: postComment,
    _previewCommentFile: previewCommentFile,
    _removeCommentFile: removeCommentFile
};

// Helpers
function isVideo(url) {
    if (!url) return false;
    const ext = url.split('.').pop().toLowerCase();
    return ['mp4', 'webm', 'ogg', 'mov'].includes(ext);
}

function escHtml(str) {
    if (!str) return '';
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

// Khởi động khi DOM sẵn sàng
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
} else {
    init();
}

