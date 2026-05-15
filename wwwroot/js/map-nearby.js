// Nearby helper panel for Map. Isolated from alert/report/route markers.
(function () {
    const OVERPASS_URL = 'https://overpass-api.de/api/interpreter';
    const DEFAULT_RADIUS = 3000;
    const EXPANDED_RADIUS = 10000;
    const VALID_TYPES = new Set([
        'fuel',
        'hospital',
        'pharmacy',
        'atm',
        'police',
        'fire_station',
        'motorcycle_repair'
    ]);

    const GOONG_KEYWORDS = {
        fuel: ['cây xăng', 'trạm xăng', 'petrolimex'],
        hospital: ['bệnh viện', 'phòng khám'],
        pharmacy: ['nhà thuốc', 'quầy thuốc', 'pharmacy'],
        atm: ['ATM', 'máy rút tiền', 'ngân hàng'],
        police: ['công an', 'đồn công an'],
        fire_station: ['cứu hỏa', 'phòng cháy chữa cháy'],
        motorcycle_repair: ['sửa xe máy', 'tiệm sửa xe', 'vá xe']
    };

    const OVERPASS_TAGS = {
        fuel: [{ key: 'amenity', value: 'fuel' }],
        hospital: [
            { key: 'amenity', value: 'hospital' },
            { key: 'healthcare', value: 'hospital' }
        ],
        pharmacy: [
            { key: 'amenity', value: 'pharmacy' },
            { key: 'healthcare', value: 'pharmacy' }
        ],
        atm: [
            { key: 'amenity', value: 'atm' },
            { key: 'atm', value: 'yes' }
        ],
        police: [{ key: 'amenity', value: 'police' }],
        fire_station: [{ key: 'amenity', value: 'fire_station' }],
        motorcycle_repair: [
            { key: 'shop', value: 'motorcycle_repair' },
            { key: 'shop', value: 'motorcycle' },
            { key: 'service:motorcycle:repair', value: 'yes' }
        ]
    };

    const emergencyNumbers = [
        { label: 'Công an', number: '113' },
        { label: 'Cứu hỏa', number: '114' },
        { label: 'Cấp cứu', number: '115' },
        { label: 'Tổng đài hỗ trợ Đà Nẵng', number: '1022' }
    ];

    let nearbyMarkers = [];
    let currentPosition = null;
    let lastSearch = null;
    let isOpen = false;

    document.addEventListener('DOMContentLoaded', initNearbyPanel);

    function initNearbyPanel() {
        const btnOpen = byId('btnNearbyPanel');
        const btnClose = byId('btnNearbyClose');
        const btnRefresh = byId('btnRefreshNearbyLocation');

        if (!btnOpen || !byId('gmNearbyPanel')) return;

        btnOpen.addEventListener('click', () => {
            if (isOpen) {
                closeNearbyPanel();
                return;
            }
            openNearbyPanel();
        });

        btnClose?.addEventListener('click', closeNearbyPanel);
        btnRefresh?.addEventListener('click', refreshLocation);

        document.querySelectorAll('.gm-nearby-option').forEach(btn => {
            btn.addEventListener('click', () => {
                const type = btn.dataset.nearbyType;
                const label = btn.dataset.nearbyLabel || btn.textContent.trim();
                console.log(`[Nearby] ${label}`, type);

                if (type === 'emergency') {
                    showEmergencyNumbers();
                    return;
                }

                searchNearby(type, label, DEFAULT_RADIUS);
            });
        });
    }

    function openNearbyPanel() {
        isOpen = true;
        setSidebarActive(true);
        hideOtherMapPanels();
        showChooser();
        byId('gmNearbyPanel').style.display = 'block';
        byId('btnRefreshNearbyLocation').style.display = 'inline-flex';
    }

    function closeNearbyPanel() {
        isOpen = false;
        setSidebarActive(false);
        clearNearbyMarkers();
        hideLoading();
        byId('gmNearbyPanel').style.display = 'none';
        byId('btnRefreshNearbyLocation').style.display = 'none';
        const search = byId('stateSearch');
        if (search) search.style.display = 'block';
    }

    function showChooser() {
        const picker = byId('gmNearbyPicker');
        const status = byId('gmNearbyStatus');
        const results = byId('gmNearbyResults');

        if (picker) picker.style.display = 'grid';
        if (status) {
            status.style.display = 'none';
            status.innerHTML = '';
        }
        if (results) {
            results.style.display = 'none';
            results.innerHTML = '';
        }
    }

    async function searchNearby(type, label, radius) {
        if (!VALID_TYPES.has(type)) return;

        lastSearch = { type, label, radius };
        setPickerBusy(type);
        showLoading();

        try {
            const pos = await getCurrentPosition();
            currentPosition = {
                lat: pos.coords.latitude,
                lon: pos.coords.longitude
            };

            const places = await fetchNearby(currentPosition.lat, currentPosition.lon, type, radius);
            hideLoading();

            if (!places.length) {
                clearNearbyMarkers();
                showEmptyState(type, label, radius);
                return;
            }

            renderNearbyResults(label, places, radius);
            drawNearbyMarkers(places);
            fitResults(places);
        } catch (err) {
            hideLoading();
            clearNearbyMarkers();
            showErrorState(err, type, label, radius);
        } finally {
            clearPickerBusy();
        }
    }

    async function fetchNearby(lat, lon, type, radius = DEFAULT_RADIUS) {
        if (!VALID_TYPES.has(type)) throw new Error('Unsupported nearby type');

        const settled = await Promise.allSettled([
            fetchGoongNearby(lat, lon, type, radius),
            fetchOverpassNearby(lat, lon, type, radius)
        ]);

        const places = settled.flatMap(result => result.status === 'fulfilled' ? result.value : []);
        const hasAnySuccess = settled.some(result => result.status === 'fulfilled');
        if (!hasAnySuccess) throw settled[0].reason || new Error('Nearby lookup failed');

        return mergePlaces(places)
            .sort((a, b) => a.distance - b.distance)
            .slice(0, 5);
    }

    async function fetchGoongNearby(lat, lon, type, radius) {
        const apiKey = window.APP_CONFIG?.goongRestApiKey;
        const keywords = GOONG_KEYWORDS[type] || [];
        if (!apiKey || !keywords.length) return [];

        const predictions = [];
        for (const keyword of keywords) {
            const url = 'https://rsapi.goong.io/Place/AutoComplete'
                + `?input=${encodeURIComponent(keyword)}`
                + `&location=${lat},${lon}`
                + `&radius=${Math.max(radius, 5000)}`
                + `&more_compound=true`
                + `&api_key=${encodeURIComponent(apiKey)}`;

            try {
                const res = await fetch(url);
                if (!res.ok) continue;
                const data = await res.json();
                (data.predictions || []).slice(0, 5).forEach(item => predictions.push(item));
            } catch (_) {
                continue;
            }
        }

        const seenPlaceIds = new Set();
        const detailCandidates = predictions
            .filter(item => item.place_id && !seenPlaceIds.has(item.place_id) && seenPlaceIds.add(item.place_id))
            .slice(0, 10);

        const details = await Promise.allSettled(detailCandidates.map(item => fetchGoongDetail(item, apiKey)));
        return details
            .filter(result => result.status === 'fulfilled' && result.value)
            .map(result => {
                const place = result.value;
                return {
                    ...place,
                    distance: haversine(lat, lon, place.lat, place.lon),
                    source: 'goong'
                };
            })
            .filter(place => place.distance <= radius * 1.25);
    }

    async function fetchGoongDetail(prediction, apiKey) {
        const url = `https://rsapi.goong.io/Place/Detail?place_id=${encodeURIComponent(prediction.place_id)}&api_key=${encodeURIComponent(apiKey)}`;
        const res = await fetch(url);
        if (!res.ok) return null;

        const data = await res.json();
        const result = data.result || {};
        const loc = result.geometry?.location;
        if (!loc || !Number.isFinite(loc.lat) || !Number.isFinite(loc.lng)) return null;

        const mainText = prediction.structured_formatting?.main_text || prediction.description;
        return {
            name: result.name || mainText || 'Không có tên',
            lat: loc.lat,
            lon: loc.lng,
            phone: result.formatted_phone_number || result.international_phone_number || '',
            address: result.formatted_address || prediction.description || ''
        };
    }

    async function fetchOverpassNearby(lat, lon, type, radius) {
        const tags = OVERPASS_TAGS[type] || [];
        if (!tags.length) return [];

        const blocks = tags.flatMap(tag => [
            `node["${tag.key}"="${tag.value}"](around:${radius},${lat},${lon});`,
            `way["${tag.key}"="${tag.value}"](around:${radius},${lat},${lon});`,
            `relation["${tag.key}"="${tag.value}"](around:${radius},${lat},${lon});`
        ]).join('');
        const query = `[out:json][timeout:25];(${blocks});out center;`;
        const url = `${OVERPASS_URL}?data=${encodeURIComponent(query)}`;
        const res = await fetch(url, { method: 'GET' });
        if (!res.ok) throw new Error(`Overpass ${res.status}`);

        const data = await res.json();
        const elements = Array.isArray(data.elements) ? data.elements : [];

        return elements
            .map(item => {
                const coord = getOsmCoord(item);
                if (!coord) return null;
                const tags = item.tags || {};
                return {
                    name: getOsmName(tags, type),
                    lat: coord.lat,
                    lon: coord.lon,
                    phone: tags.phone || tags['contact:phone'] || tags['contact:mobile'] || '',
                    address: buildOsmAddress(tags),
                    distance: haversine(lat, lon, coord.lat, coord.lon),
                    source: 'osm'
                };
            })
            .filter(Boolean);
    }

    function mergePlaces(places) {
        const byKey = new Map();
        places.forEach(place => {
            if (!Number.isFinite(place.lat) || !Number.isFinite(place.lon)) return;
            const key = `${Math.round(place.lat * 10000)}:${Math.round(place.lon * 10000)}`;
            const existing = byKey.get(key);
            if (!existing) {
                byKey.set(key, place);
                return;
            }

            const existingHasName = existing.name && existing.name !== 'Không có tên';
            const placeHasName = place.name && place.name !== 'Không có tên';
            if ((!existingHasName && placeHasName) || (place.source === 'goong' && existing.source !== 'goong')) {
                byKey.set(key, { ...existing, ...place, distance: Math.min(existing.distance, place.distance) });
            }
        });
        return Array.from(byKey.values());
    }

    function getOsmCoord(item) {
        if (Number.isFinite(item.lat) && Number.isFinite(item.lon)) return { lat: item.lat, lon: item.lon };
        if (item.center && Number.isFinite(item.center.lat) && Number.isFinite(item.center.lon)) {
            return { lat: item.center.lat, lon: item.center.lon };
        }
        return null;
    }

    function getOsmName(tags, type) {
        const rawName = tags.name
            || tags['name:vi']
            || tags.official_name
            || tags.brand
            || tags.operator
            || tags.network
            || tags.short_name
            || tags.ref
            || tags['addr:housename'];

        if (rawName) return rawName;

        if (type === 'atm' && tags.operator) return `ATM ${tags.operator}`;
        if (type === 'fuel' && (tags.brand || tags.operator)) return `Cây xăng ${tags.brand || tags.operator}`;
        if (type === 'pharmacy' && (tags.brand || tags.operator)) return `Nhà thuốc ${tags.brand || tags.operator}`;
        return 'Không có tên';
    }

    function buildOsmAddress(tags) {
        return [
            tags['addr:housenumber'],
            tags['addr:street'],
            tags['addr:ward'],
            tags['addr:district']
        ].filter(Boolean).join(', ');
    }

    function showEmergencyNumbers() {
        openNearbyPanel();
        clearNearbyMarkers();
        const picker = byId('gmNearbyPicker');
        const status = byId('gmNearbyStatus');
        const results = byId('gmNearbyResults');
        if (picker) picker.style.display = 'none';
        if (results) results.style.display = 'none';
        if (!status) return;

        status.className = 'gm-nearby-status gm-nearby-status--plain';
        status.style.display = 'block';
        status.innerHTML = `
            <div class="gm-nearby-state-head">
                <button class="gm-nearby-back" type="button" data-nearby-back>Chọn lại</button>
                <div class="gm-nearby-state-title">Số khẩn cấp tại Đà Nẵng</div>
            </div>
            <div class="gm-emergency-list">
                ${emergencyNumbers.map(item => `
                    <div class="gm-emergency-row">
                        <span>${esc(item.label)}</span>
                        <strong>${esc(item.number)}</strong>
                    </div>
                `).join('')}
            </div>
        `;
        status.querySelector('[data-nearby-back]')?.addEventListener('click', showChooser);
    }

    function renderNearbyResults(label, places, radius) {
        const picker = byId('gmNearbyPicker');
        const status = byId('gmNearbyStatus');
        const results = byId('gmNearbyResults');
        if (picker) picker.style.display = 'none';
        if (status) {
            status.style.display = 'none';
            status.innerHTML = '';
        }
        if (!results) return;

        results.style.display = 'block';
        results.innerHTML = `
            <div class="gm-nearby-results__head">
                <div>
                    <div class="gm-nearby-results__title">${esc(label)}</div>
                    <div class="gm-nearby-results__meta">${places.length} địa điểm trong ${radius >= 10000 ? '10km' : '3km'}</div>
                </div>
                <button class="gm-nearby-close-list" type="button" title="Đóng danh sách" data-nearby-back>
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="14" height="14">
                        <line x1="18" y1="6" x2="6" y2="18"></line>
                        <line x1="6" y1="6" x2="18" y2="18"></line>
                    </svg>
                </button>
            </div>
            <div class="gm-nearby-list">
                ${places.map((place, index) => buildResultCard(place, index)).join('')}
            </div>
        `;

        results.querySelector('[data-nearby-back]')?.addEventListener('click', () => {
            clearNearbyMarkers();
            showChooser();
        });
        results.querySelectorAll('[data-nearby-dir]').forEach(btn => {
            btn.addEventListener('click', () => {
                const lat = parseFloat(btn.dataset.lat);
                const lon = parseFloat(btn.dataset.lon);
                const index = parseInt(btn.dataset.index || '-1', 10);
                const place = places[index] || { lat, lon, name: 'Điểm đến' };
                startInternalRoute(place);
            });
        });
    }

    function buildResultCard(place, index) {
        const phone = normalizePhone(place.phone);
        return `
            <div class="gm-nearby-card" data-nearby-index="${index}">
                <div class="gm-nearby-card__pin">${index + 1}</div>
                <div class="gm-nearby-card__body">
                    <div class="gm-nearby-card__name">${esc(place.name || 'Không có tên')}</div>
                    <div class="gm-nearby-card__dist">${formatDistance(place.distance)}</div>
                    ${place.address ? `<div class="gm-nearby-card__addr">${esc(place.address)}</div>` : ''}
                    <div class="gm-nearby-card__actions">
                        <button class="gm-nearby-card__btn gm-nearby-card__btn--primary" type="button" data-nearby-dir data-index="${index}" data-lat="${place.lat}" data-lon="${place.lon}">
                            Chỉ đường
                        </button>
                        ${phone ? `<a class="gm-nearby-card__btn gm-nearby-card__btn--phone" href="tel:${escAttr(phone)}">Gọi ngay</a>` : ''}
                    </div>
                </div>
            </div>
        `;
    }

    async function startInternalRoute(place) {
        const routeCard = byId('gmRouteCard');
        const search = byId('stateSearch');
        const placeCard = byId('gmPlaceCard');
        const nearbyPanel = byId('gmNearbyPanel');
        const startInput = byId('routeStart');
        const endInput = byId('routeEnd');
        const findButton = byId('btnFindRoute');

        if (!routeCard || !startInput || !endInput || !findButton) return;

        let start = currentPosition;
        if (!start) {
            showLoading();
            try {
                const pos = await getCurrentPosition();
                start = currentPosition = { lat: pos.coords.latitude, lon: pos.coords.longitude };
            } catch (err) {
                hideLoading();
                showErrorState(err, lastSearch?.type || 'fuel', lastSearch?.label || 'Gần tôi', lastSearch?.radius || DEFAULT_RADIUS);
                return;
            }
            hideLoading();
        }

        clearNearbyMarkers();
        if (nearbyPanel) nearbyPanel.style.display = 'none';
        if (search) search.style.display = 'none';
        if (placeCard) placeCard.style.display = 'none';
        routeCard.style.display = 'block';
        isOpen = false;
        setSidebarActive(false);
        const refreshButton = byId('btnRefreshNearbyLocation');
        if (refreshButton) refreshButton.style.display = 'none';

        window.RouteStartCoord = { lat: start.lat, lng: start.lon };
        window.RouteEndCoord = { lat: place.lat, lng: place.lon };
        window.SelectedPlace = {
            name: place.name || 'Không có tên',
            lat: place.lat,
            lng: place.lon
        };

        startInput.value = 'Vị trí hiện tại của bạn';
        startInput.dataset.prefilledLocation = 'true';
        endInput.value = place.name || 'Không có tên';

        setTimeout(() => findButton.click(), 40);
    }

    function drawNearbyMarkers(places) {
        const map = getMap();
        if (!map) return;

        clearNearbyMarkers();
        places.forEach(place => {
            const marker = new goongjs.Marker({ color: '#10B981' })
                .setLngLat([place.lon, place.lat])
                .setPopup(new goongjs.Popup({ offset: 28, closeButton: true })
                    .setHTML(`
                        <div class="lp">
                            <div class="lp__top">
                                <svg class="lp__pin" viewBox="0 0 20 24" fill="none">
                                    <path d="M10 0C6.13 0 3 3.13 3 7c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7z" fill="#10B981"/>
                                    <circle cx="10" cy="7" r="2.5" fill="white"/>
                                </svg>
                                <div>
                                    <div class="lp__addr">${esc(place.name || 'Không có tên')}</div>
                                    <div class="lp__sub">${formatDistance(place.distance)}</div>
                                    ${place.address ? `<div class="lp__sub">${esc(place.address)}</div>` : ''}
                                </div>
                            </div>
                        </div>
                    `))
                .addTo(map);
            nearbyMarkers.push(marker);
        });
    }

    function clearNearbyMarkers() {
        nearbyMarkers.forEach(marker => marker.remove());
        nearbyMarkers = [];
    }

    function fitResults(places) {
        const map = getMap();
        if (!map || !places.length || !window.goongjs) return;

        const bounds = new goongjs.LngLatBounds();
        if (currentPosition) bounds.extend([currentPosition.lon, currentPosition.lat]);
        places.forEach(p => bounds.extend([p.lon, p.lat]));
        map.fitBounds(bounds, { padding: 90, maxZoom: 16, duration: 900 });
    }

    function showEmptyState(type, label, radius) {
        showState(`
            <div class="gm-nearby-state-title">Không tìm thấy địa điểm nào trong vòng ${radius >= 10000 ? '10km' : '3km'}.</div>
            <div class="gm-nearby-state-text">Thử mở rộng phạm vi?</div>
            ${radius < EXPANDED_RADIUS ? `<button class="gm-nearby-state-btn" type="button" data-expand>Tìm trong 10km</button>` : ''}
            <button class="gm-nearby-back" type="button" data-nearby-back>Chọn lại</button>
        `);
        byId('gmNearbyStatus')?.querySelector('[data-expand]')?.addEventListener('click', () => searchNearby(type, label, EXPANDED_RADIUS));
        byId('gmNearbyStatus')?.querySelector('[data-nearby-back]')?.addEventListener('click', showChooser);
    }

    function showErrorState(err, type, label, radius) {
        const isPermission = err && err.code === 1;
        const message = isPermission
            ? 'Bạn cần bật quyền vị trí để tìm địa điểm gần mình.'
            : 'Không kết nối được. Kiểm tra mạng và thử lại.';
        showState(`
            <div class="gm-nearby-state-title">${esc(message)}</div>
            <button class="gm-nearby-state-btn" type="button" data-retry>Thử lại</button>
            <button class="gm-nearby-back" type="button" data-nearby-back>Chọn lại</button>
        `);
        byId('gmNearbyStatus')?.querySelector('[data-retry]')?.addEventListener('click', () => searchNearby(type, label, radius));
        byId('gmNearbyStatus')?.querySelector('[data-nearby-back]')?.addEventListener('click', showChooser);
    }

    function showState(html) {
        const picker = byId('gmNearbyPicker');
        const results = byId('gmNearbyResults');
        const status = byId('gmNearbyStatus');
        if (picker) picker.style.display = 'none';
        if (results) {
            results.style.display = 'none';
            results.innerHTML = '';
        }
        if (status) {
            status.className = 'gm-nearby-status';
            status.style.display = 'block';
            status.innerHTML = html;
        }
    }

    async function refreshLocation() {
        if (!navigator.geolocation) {
            showState('<div class="gm-nearby-state-title">Trình duyệt không hỗ trợ định vị.</div>');
            return;
        }

        if (lastSearch && lastSearch.type !== 'emergency') {
            await searchNearby(lastSearch.type, lastSearch.label, lastSearch.radius || DEFAULT_RADIUS);
            return;
        }

        showLoading();
        try {
            const pos = await getCurrentPosition();
            currentPosition = { lat: pos.coords.latitude, lon: pos.coords.longitude };
            hideLoading();
            const map = getMap();
            map?.flyTo({ center: [currentPosition.lon, currentPosition.lat], zoom: 15, duration: 900 });
        } catch (err) {
            hideLoading();
            showErrorState(err, lastSearch?.type || 'fuel', lastSearch?.label || 'Gần tôi', lastSearch?.radius || DEFAULT_RADIUS);
        }
    }

    function getCurrentPosition() {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error('Geolocation unsupported'));
                return;
            }
            navigator.geolocation.getCurrentPosition(resolve, reject, {
                enableHighAccuracy: true,
                maximumAge: 15000,
                timeout: 15000
            });
        });
    }

    function hideOtherMapPanels() {
        ['stateSearch', 'gmPlaceCard', 'gmRouteCard'].forEach(id => {
            const el = byId(id);
            if (el) el.style.display = 'none';
        });
    }

    function setPickerBusy(type) {
        document.querySelectorAll('.gm-nearby-option').forEach(btn => {
            btn.disabled = true;
            btn.classList.toggle('is-loading', btn.dataset.nearbyType === type);
        });
    }

    function clearPickerBusy() {
        document.querySelectorAll('.gm-nearby-option').forEach(btn => {
            btn.disabled = false;
            btn.classList.remove('is-loading');
        });
    }

    function setSidebarActive(active) {
        const btn = byId('btnNearbyPanel');
        if (!btn) return;
        btn.classList.toggle('active', active);
    }

    function showLoading() {
        const el = byId('gmNearbyLoading');
        if (el) el.style.display = 'flex';
    }

    function hideLoading() {
        const el = byId('gmNearbyLoading');
        if (el) el.style.display = 'none';
    }

    function getMap() {
        return window.MapCore?.getMap?.() || window.MapInstance || null;
    }

    function haversine(lat1, lon1, lat2, lon2) {
        const r = 6371000;
        const dLat = toRad(lat2 - lat1);
        const dLon = toRad(lon2 - lon1);
        const a = Math.sin(dLat / 2) ** 2
            + Math.cos(toRad(lat1)) * Math.cos(toRad(lat2))
            * Math.sin(dLon / 2) ** 2;
        return 2 * r * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }

    function toRad(value) {
        return value * Math.PI / 180;
    }

    function formatDistance(meters) {
        if (!Number.isFinite(meters)) return '';
        if (meters < 1000) return `${Math.round(meters)}m`;
        return `${(meters / 1000).toFixed(1)}km`;
    }

    function normalizePhone(phone) {
        if (!phone) return '';
        return String(phone).split(';')[0].trim().replace(/[^\d+]/g, '');
    }

    function byId(id) {
        return document.getElementById(id);
    }

    function esc(value) {
        const div = document.createElement('div');
        div.textContent = value == null ? '' : String(value);
        return div.innerHTML;
    }

    function escAttr(value) {
        return esc(value).replace(/"/g, '&quot;');
    }

    window.MapNearby = {
        fetchNearby,
        open: openNearbyPanel,
        close: closeNearbyPanel,
        clear: clearNearbyMarkers
    };
})();
