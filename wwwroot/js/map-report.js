/**
 * map-report.js — Form báo cáo sự cố
 * Giao diện chuyên nghiệp, không dùng emoji
 * Chức năng: Modal nhập sự cố, ghim vị trí bản đồ, tìm kiếm địa chỉ, upload ảnh/video, submit API
 */

// ═══════════════════════════════════════════════════════════
// INJECT CSS
// ═══════════════════════════════════════════════════════════

(function injectStyles() {
    if (document.getElementById('gm-report-styles')) return;
    const style = document.createElement('style');
    style.id = 'gm-report-styles';
    style.textContent = `
        /* ── Fonts ── */
        @import url('https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:wght@400;500;600;700&display=swap');

        /* ── Reset & Base ── */
        .gm-report-modal { 
            box-sizing: border-box; 
            font-family: 'Be Vietnam Pro', 'Inter', system-ui, -apple-system, sans-serif;
            color: #1e293b;
        }
        .gm-report-modal *, .gm-report-modal *:before, .gm-report-modal *:after { box-sizing: inherit; }

        /* ── Overlay ── */
        .gm-report-modal {
            display: none;
            position: fixed; inset: 0;
            z-index: 2000; /* Tăng z-index để luôn trên cùng */
            align-items: center;
            justify-content: center;
        }
        .gm-report-modal__overlay {
            position: absolute; inset: 0;
            background: rgba(15, 23, 42, 0.75);
            backdrop-filter: blur(8px);
        }

        /* ── Dialog ── */
        .gm-report-modal__content {
            position: relative;
            width: 100%;
            max-width: 560px;
            max-height: 85vh;
            background: #ffffff;
            border-radius: 24px;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            overflow: hidden;
            display: flex;
            flex-direction: column;
            animation: reportSlideIn 0.35s cubic-bezier(0.34, 1.56, 0.64, 1);
        }
        @keyframes reportSlideIn {
            from { opacity: 0; transform: translateY(30px) scale(0.95); }
            to   { opacity: 1; transform: translateY(0) scale(1); }
        }

        /* ── Header ── */
        .gm-report-modal__head {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 24px 28px;
            background: #ffffff;
            border-bottom: 1px solid #f1f5f9;
            flex-shrink: 0;
        }
        .gm-report-modal__head-left {
            display: flex;
            align-items: center;
            gap: 16px;
        }
        .gm-report-modal__icon {
            width: 44px; height: 44px;
            background: #ffffff;
            border: 1px solid #e2e8f0;
            border-radius: 12px;
            display: flex; align-items: center; justify-content: center;
            flex-shrink: 0;
            box-shadow: 0 1px 2px rgba(0,0,0,0.02);
        }
        .gm-report-modal__icon svg {
            width: 20px; height: 20px;
            stroke: #334155;
            fill: none;
            stroke-width: 2;
        }
        .gm-report-modal__head h3 {
            font-size: 18px;
            font-weight: 800;
            color: #0f172a;
            margin: 0;
            letter-spacing: -0.01em;
        }
        .gm-report-modal__head p {
            font-size: 13px;
            color: #64748b;
            margin: 4px 0 0 0;
            font-weight: 400;
        }
        .gm-report-modal__close {
            width: 36px; height: 36px;
            background: #f1f5f9;
            border: none;
            border-radius: 50%;
            cursor: pointer;
            display: flex; align-items: center; justify-content: center;
            color: #475569;
            transition: all 0.2s;
            flex-shrink: 0;
        }
        .gm-report-modal__close:hover { 
            background: #e2e8f0; 
            color: #0f172a; 
        }
        .gm-report-modal__close svg { width: 18px; height: 18px; stroke: currentColor; fill: none; stroke-width: 2.5; }

        /* ── Scroll body ── */
        .gm-report-modal__body {
            overflow-y: auto;
            padding: 24px 28px;
            display: flex;
            flex-direction: column;
            gap: 24px;
            flex: 1;
        }

        /* ── Type selector ── */
        .gm-type-group { margin-bottom: 16px; }
        .gm-type-group__label {
            font-size: 12px;
            font-weight: 700;
            color: #64748b;
            text-transform: uppercase;
            letter-spacing: 0.05em;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            gap: 8px;
        }
        .gm-type-grid {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 12px;
        }
        .gm-type-option {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            gap: 12px;
            padding: 20px 12px;
            border: 2px solid #f1f5f9;
            border-radius: 16px;
            cursor: pointer;
            background: #ffffff;
            transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
            user-select: none;
            text-align: center;
            position: relative;
        }
        .gm-type-option:hover {
            border-color: #e2e8f0;
            transform: translateY(-2px);
            box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.05);
        }

        /* Trộm cắp - Đỏ */
        .gm-type-option.red { background: #fffbff; }
        .gm-type-option.red .gm-type-dot { background: #fef2f2; color: #ef4444; }
        .gm-type-option.red.selected { border-color: #ef4444; background: #fef2f2; box-shadow: 0 0 0 1px #ef4444; }
        .gm-type-option.red.selected .gm-type-dot { background: #ef4444; color: #fff; }

        /* Giao thông - Xanh */
        .gm-type-option.blue { background: #f7fbff; }
        .gm-type-option.blue .gm-type-dot { background: #eff6ff; color: #3b82f6; }
        .gm-type-option.blue.selected { border-color: #3b82f6; background: #eff6ff; box-shadow: 0 0 0 1px #3b82f6; }
        .gm-type-option.blue.selected .gm-type-dot { background: #3b82f6; color: #fff; }

        /* Lừa đảo - Vàng */
        .gm-type-option.amber { background: #fffdf5; }
        .gm-type-option.amber .gm-type-dot { background: #fffbeb; color: #f59e0b; }
        .gm-type-option.amber.selected { border-color: #f59e0b; background: #fffbeb; box-shadow: 0 0 0 1px #f59e0b; }
        .gm-type-option.amber.selected .gm-type-dot { background: #f59e0b; color: #fff; }

        .gm-type-dot {
            width: 44px; height: 44px;
            border-radius: 12px;
            display: flex; align-items: center; justify-content: center;
            transition: all 0.2s;
        }
        .gm-type-dot svg { width: 22px; height: 22px; stroke-width: 2; }

        .gm-type-name {
            font-size: 13px;
            font-weight: 600;
            color: #475569;
            line-height: 1.25;
        }
        .gm-type-option.selected .gm-type-name { color: #0f172a; }

        /* ── Inputs ── */
        .gm-report-input {
            width: 100%;
            padding: 10px 14px;
            border: 1.5px solid #e2e8f0;
            border-radius: 10px;
            font-size: 14px;
            color: #0f172a;
            background: #fafafa;
            transition: border-color 0.15s, background 0.15s, box-shadow 0.15s;
            outline: none;
            resize: none;
            font-family: 'Be Vietnam Pro', sans-serif;
        }
        .gm-report-input:focus {
            border-color: #3b82f6;
            background: #ffffff;
            box-shadow: 0 0 0 3px rgba(59,130,246,0.10);
        }
        .gm-report-input::placeholder { color: #94a3b8; }
        .gm-hint {
            margin-top: 6px;
            font-size: 11px;
            color: #64748b;
            line-height: 1.4;
        }

        /* ── Location row ── */
        .gm-location-row {
            display: flex;
            gap: 8px;
            margin-top: 8px;
        }
        .gm-btn-location {
            flex: 1;
            padding: 10px 14px;
            border: 1.5px solid #e2e8f0;
            border-radius: 10px;
            font-size: 13px;
            font-weight: 600;
            cursor: pointer;
            background: #fafafa;
            color: #334155;
            transition: all 0.15s;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            font-family: 'Be Vietnam Pro', sans-serif;
        }
        .gm-btn-location:hover {
            border-color: #3b82f6;
            background: #eff6ff;
            color: #1d4ed8;
        }
        .gm-btn-location svg {
            width: 15px; height: 15px;
            stroke: currentColor;
            fill: none;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
            flex-shrink: 0;
        }
        .gm-btn-location--icon {
            flex: none;
            width: 42px;
            padding: 10px;
        }
        .gm-btn-location--active {
            border-color: #16a34a !important;
            background: #f0fdf4 !important;
            color: #15803d !important;
        }

        /* ── Search dropdown ── */
        .gm-search-wrap { position: relative; }
        .gm-search-input-wrap {
            position: relative;
            display: flex;
            align-items: center;
        }
        .gm-search-ico-left {
            position: absolute;
            left: 12px;
            width: 18px; height: 18px;
            color: #94a3b8;
            pointer-events: none;
            display: flex; align-items: center; justify-content: center;
        }
        .gm-search-ico-left svg { width: 100%; height: 100%; }
        .gm-search-input-wrap .gm-report-input {
            padding-left: 40px;
            padding-right: 36px;
        }
        .gm-search-clear {
            position: absolute;
            right: 10px;
            width: 24px; height: 24px;
            background: transparent;
            border: none;
            border-radius: 50%;
            display: flex; align-items: center; justify-content: center;
            color: #94a3b8;
            cursor: pointer;
            transition: all 0.15s;
        }
        .gm-search-clear:hover { background: #f1f5f9; color: #0f172a; }
        .gm-search-clear svg { width: 14px; height: 14px; }
        .gm-search-spinner {
            position: absolute;
            right: 12px;
            width: 18px; height: 18px;
            border: 2.5px solid #e2e8f0;
            border-top-color: #3b82f6;
            border-radius: 50%;
            animation: gm-spin 0.7s linear infinite;
            display: none;
        }
        @keyframes gm-spin { to { transform: rotate(360deg); } }
        .gm-search-dropdown {
            display: none;
            margin-top: 6px;
            background: #ffffff;
            border: 1.5px solid #e2e8f0;
            border-radius: 12px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.06);
            overflow: hidden;
            max-height: 220px;
            overflow-y: auto;
        }
        .gm-search-dropdown::-webkit-scrollbar { width: 4px; }
        .gm-search-dropdown::-webkit-scrollbar-thumb { background: #e2e8f0; border-radius: 4px; }
        .gm-search-item {
            padding: 10px 14px;
            cursor: pointer;
            border-bottom: 1px solid #f8fafc;
            display: flex;
            align-items: flex-start;
            gap: 10px;
            transition: background 0.1s;
        }
        .gm-search-item:last-child { border-bottom: none; }
        .gm-search-item:hover { background: #f8fafc; }
        .gm-search-item__icon {
            width: 28px; height: 28px;
            background: #f1f5f9;
            border-radius: 7px;
            display: flex; align-items: center; justify-content: center;
            flex-shrink: 0;
            margin-top: 1px;
        }
        .gm-search-item__icon svg {
            width: 14px; height: 14px;
            stroke: #64748b;
            fill: none;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
        }
        .gm-search-item__main { font-size: 13px; font-weight: 600; color: #0f172a; line-height: 1.3; }
        .gm-search-item__sub { font-size: 12px; color: #64748b; margin-top: 2px; line-height: 1.3; }
        .gm-search-empty {
            padding: 16px;
            text-align: center;
            font-size: 13px;
            color: #94a3b8;
        }

        /* ── Picked coords ── */
        .gm-picked-coords {
            display: none;
            margin-top: 8px;
            padding: 8px 12px;
            background: #f0fdf4;
            border: 1px solid #bbf7d0;
            border-radius: 8px;
            font-size: 12px;
            font-weight: 600;
            color: #15803d;
            display: flex;
            align-items: center;
            gap: 8px;
        }
        .gm-picked-coords svg {
            width: 14px; height: 14px;
            stroke: #16a34a;
            fill: none;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
            flex-shrink: 0;
        }

        /* ── Char count ── */
        .gm-char-count {
            font-size: 11px;
            color: #94a3b8;
            text-align: right;
            margin-top: 5px;
        }

        /* ── File upload ── */
        .gm-upload-label {
            display: flex;
            align-items: center;
            gap: 10px;
            padding: 12px 16px;
            border: 1.5px dashed #cbd5e1;
            border-radius: 10px;
            cursor: pointer;
            background: #fafafa;
            transition: border-color 0.15s, background 0.15s;
        }
        .gm-upload-label:hover {
            border-color: #3b82f6;
            background: #eff6ff;
        }
        .gm-upload-label__icon {
            width: 32px; height: 32px;
            background: #e2e8f0;
            border-radius: 8px;
            display: flex; align-items: center; justify-content: center;
            flex-shrink: 0;
        }
        .gm-upload-label__icon svg {
            width: 16px; height: 16px;
            stroke: #64748b;
            fill: none;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
        }
        .gm-upload-label__text span {
            display: block;
            font-size: 13px;
            font-weight: 600;
            color: #334155;
        }
        .gm-upload-label__text small {
            font-size: 11px;
            color: #94a3b8;
        }

        .gm-file-preview {
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
            margin-top: 10px;
        }
        .gm-preview-item {
            width: 72px;
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 4px;
        }
        .gm-preview-item img {
            width: 72px; height: 72px;
            object-fit: cover;
            border-radius: 8px;
            border: 1.5px solid #e2e8f0;
        }
        .gm-preview-item span {
            font-size: 10px;
            color: #64748b;
            text-align: center;
            word-break: break-all;
        }
        .gm-preview-video {
            width: 72px; height: 72px;
            background: #f1f5f9;
            border-radius: 8px;
            border: 1.5px solid #e2e8f0;
            display: flex; align-items: center; justify-content: center;
        }
        .gm-upload-label.is-dragover {
            border-color: #2563eb;
            background: #dbeafe;
            box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.12);
        }
        .gm-preview-item video {
            width: 72px; height: 72px;
            object-fit: cover;
            border-radius: 8px;
            border: 1.5px solid #e2e8f0;
            background: #0f172a;
        }
        .gm-preview-item small {
            font-size: 9px;
            color: #94a3b8;
            text-align: center;
        }
        .gm-upload-progress-list {
            display: flex;
            flex-direction: column;
            gap: 8px;
            margin-top: 10px;
        }
        .gm-upload-progress-item {
            padding: 10px;
            border: 1px solid #e2e8f0;
            border-radius: 10px;
            background: #f8fafc;
        }
        .gm-upload-progress-head {
            display: flex;
            justify-content: space-between;
            gap: 10px;
            font-size: 12px;
            color: #334155;
            font-weight: 600;
        }
        .gm-upload-progress-head small { color: #64748b; font-weight: 500; white-space: nowrap; }
        .gm-upload-progress-bar {
            height: 6px;
            border-radius: 999px;
            background: #e2e8f0;
            overflow: hidden;
            margin-top: 8px;
        }
        .gm-upload-progress-bar div {
            height: 100%;
            width: 0;
            background: #2563eb;
            transition: width 0.15s ease;
        }
        .gm-upload-progress-status {
            margin-top: 6px;
            font-size: 11px;
            color: #64748b;
        }

        /* ── Confirm checkbox ── */
        .gm-confirm-check {
            display: flex;
            align-items: flex-start;
            gap: 10px;
            cursor: pointer;
        }
        .gm-confirm-check input[type="checkbox"] {
            width: 16px; height: 16px;
            border-radius: 4px;
            flex-shrink: 0;
            margin-top: 1px;
            accent-color: #dc2626;
            cursor: pointer;
        }
        .gm-confirm-check span {
            font-size: 13px;
            color: #475569;
            line-height: 1.5;
        }

        /* ── Errors ── */
        .gm-error {
            font-size: 12px;
            color: #dc2626;
            font-weight: 500;
            margin-top: 5px;
            min-height: 16px;
        }

        /* ── Footer actions ── */
        .gm-report-modal__actions {
            display: flex;
            gap: 10px;
            padding: 16px 24px 20px;
            border-top: 1px solid #f0f2f5;
            flex-shrink: 0;
        }
        .gm-btn-cancel {
            flex: 1;
            padding: 11px;
            border: 1.5px solid #e2e8f0;
            border-radius: 10px;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            background: #fafafa;
            color: #64748b;
            transition: all 0.15s;
            font-family: 'Be Vietnam Pro', sans-serif;
        }
        .gm-btn-cancel:hover { background: #f1f5f9; color: #334155; border-color: #cbd5e1; }
        .gm-btn-submit {
            flex: 2;
            padding: 11px;
            border: none;
            border-radius: 10px;
            font-size: 14px;
            font-weight: 700;
            cursor: pointer;
            background: #dc2626;
            color: #ffffff;
            transition: all 0.15s;
            font-family: 'Be Vietnam Pro', sans-serif;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }
        .gm-btn-submit:hover { background: #b91c1c; }
        .gm-btn-submit:disabled { opacity: 0.6; cursor: not-allowed; }
        .gm-btn-submit svg {
            width: 16px; height: 16px;
            stroke: currentColor;
            fill: none;
            stroke-width: 2.5;
            stroke-linecap: round;
            stroke-linejoin: round;
        }

        /* ── Context menu ── */
        .gm-context-menu {
            background: #ffffff;
            border-radius: 12px;
            box-shadow: 0 8px 32px rgba(0,0,0,0.14), 0 2px 8px rgba(0,0,0,0.06);
            overflow: hidden;
            min-width: 210px;
            border: 1px solid #f0f2f5;
        }
        .gm-context-menu__item {
            width: 100%;
            padding: 12px 16px;
            border: none;
            background: transparent;
            font-size: 13px;
            font-weight: 600;
            color: #0f172a;
            cursor: pointer;
            text-align: left;
            transition: background 0.12s;
            font-family: 'Be Vietnam Pro', sans-serif;
            display: flex;
            align-items: center;
            gap: 10px;
        }
        .gm-context-menu__item:hover { background: #fef2f2; color: #dc2626; }
        .gm-context-menu__item svg {
            width: 15px; height: 15px;
            stroke: currentColor;
            fill: none;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
        }

        /* ── Toast ── */
        .gm-toast {
            position: fixed;
            bottom: 80px;
            right: 24px;
            z-index: 9999;
            padding: 12px 20px;
            border-radius: 12px;
            font-size: 13px;
            font-weight: 600;
            color: #fff;
            box-shadow: 0 4px 20px rgba(0,0,0,0.15);
            max-width: 320px;
            animation: toastIn 0.2s ease;
            font-family: 'Be Vietnam Pro', sans-serif;
        }
        @keyframes toastIn { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }

        /* ── Picking mode overlay ── */
        .gm-picking-banner {
            position: fixed;
            top: 16px;
            left: 50%;
            transform: translateX(-50%);
            z-index: 1300;
            background: #0f172a;
            color: #fff;
            padding: 10px 20px;
            border-radius: 10px;
            font-size: 13px;
            font-weight: 600;
            font-family: 'Be Vietnam Pro', sans-serif;
            box-shadow: 0 4px 16px rgba(0,0,0,0.2);
            display: flex;
            align-items: center;
            gap: 10px;
            pointer-events: none;
        }
        .gm-picking-banner svg {
            width: 16px; height: 16px;
            stroke: #94a3b8;
            fill: none;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
        }

        /* ── Responsive ── */
        @media (max-width: 600px) {
            .gm-report-modal__content { max-width: 100%; border-radius: 20px 20px 0 0; margin-top: auto; }
            .gm-report-modal { align-items: flex-end; }
        }
    `;
    document.head.appendChild(style);
})();

// ═══════════════════════════════════════════════════════════
// SVG ICONS
// ═══════════════════════════════════════════════════════════

const ICONS = {
    alert: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>`,
    close: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>`,
    pin: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0118 0z"/><circle cx="12" cy="10" r="3"/></svg>`,
    target: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><circle cx="12" cy="12" r="6"/><circle cx="12" cy="12" r="2"/></svg>`,
    search: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>`,
    check: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>`,
    location: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0118 0z"/><circle cx="12" cy="10" r="3"/></svg>`,
    camera: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M23 19a2 2 0 01-2 2H3a2 2 0 01-2-2V8a2 2 0 012-2h4l2-3h6l2 3h4a2 2 0 012 2z"/><circle cx="12" cy="13" r="4"/></svg>`,
    video: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="23 7 16 12 23 17 23 7"/><rect x="1" y="5" width="15" height="14" rx="2" ry="2"/></svg>`,
    send: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/></svg>`,

    // Type-specific icons based on real slugs from DB
    theft_motorbike: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="6" cy="18" r="3"/><circle cx="18" cy="18" r="3"/><path d="M12 18h4l2-7h-4l-2 2h-4.5"/><path d="M11 13l-1.5-4h-2.5"/><path d="M14 11l1-5h3"/></svg>`,
    pickpocket_robbery: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 16v1a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2h2m4 0h4a2 2 0 0 1 2 2v2m-6 0h4"/><rect x="8" y="9" width="8" height="8" rx="1"/><circle cx="12" cy="13" r="1"/></svg>`,
    burglary: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>`,
    street_racing: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 8h18M3 12h18M3 16h18"/><path d="M13 8l3-3 3 3m-6 4l3-3 3 3m-6 4l3-3 3 3"/></svg>`,
    fighting_disorder: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 11V6a2 2 0 0 0-2-2v0a2 2 0 0 0-2 2v0"/><path d="M14 10V4a2 2 0 0 0-2-2v0a2 2 0 0 0-2 2v2"/><path d="M10 10.5V6a2 2 0 0 0-2-2v0a2 2 0 0 0-2 2v8"/><path d="M18 8a2 2 0 1 1 4 0v6a8 8 0 0 1-8 8h-2c-2.8 0-4.5-.86-5.99-2.34l-3.6-3.6a2 2 0 0 1 2.83-2.82L7 15"/></svg>`,
    scam_tourist: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>`,
    overcharging: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>`,
};

// ═══════════════════════════════════════════════════════════
// STATE
// ═══════════════════════════════════════════════════════════

let alertTypes = [];
let pickedLatLng = null;
let pickingMode = false;
let selectedTypeId = null;
let pickMarker = null;
let pickingBanner = null;
let selectedFiles = [];
let previewObjectUrls = [];

const CLIENT_MAX_MEDIA_SIZE = 500 * 1024 * 1024;
const CLIENT_ALLOWED_MEDIA_TYPES = new Set([
    'image/jpeg',
    'image/png',
    'image/webp',
    'video/mp4',
    'video/webm'
]);
const CLIENT_ALLOWED_MEDIA_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp', '.mp4', '.webm'];

// ═══════════════════════════════════════════════════════════
// KHỞI TẠO
// ═══════════════════════════════════════════════════════════

async function initReportForm() {
    try {
        const res = await fetch('/api/alerts/types');
        if (res.ok) alertTypes = await res.json();
    } catch (_) { }

    injectModalHtml();
    bindEvents();
    setupContextMenu();
}

function injectModalHtml() {
    if (document.getElementById('gmReportModal')) return;
    const modal = document.createElement('div');
    modal.id = 'gmReportModal';
    modal.className = 'gm-report-modal';
    modal.style.display = 'none';
    modal.innerHTML = `
        <div class="gm-report-modal__overlay" onclick="window.MapReport.close()"></div>
        <div class="gm-report-modal__content" role="dialog" aria-modal="true" aria-labelledby="reportModalTitle">

            <!-- Header -->
            <div class="gm-report-modal__head">
                <div class="gm-report-modal__head-left">
                    <div class="gm-report-modal__icon">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
                            <line x1="12" y1="9" x2="12" y2="13"></line>
                            <line x1="12" y1="17" x2="12.01" y2="17"></line>
                        </svg>
                    </div>
                    <div>
                        <h3 id="reportModalTitle">Báo cáo sự cố</h3>
                        <p>Thông tin sẽ được kiểm duyệt trước khi hiển thị</p>
                    </div>
                </div>
                <button class="gm-report-modal__close" onclick="window.MapReport.close()" title="Đóng" aria-label="Đóng">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                        <line x1="18" y1="6" x2="6" y2="18"></line>
                        <line x1="6" y1="6" x2="18" y2="18"></line>
                    </svg>
                </button>
            </div>

            <!-- Body -->
            <div class="gm-report-modal__body">

                <!-- Bước 1: Loại sự cố -->
                <div class="gm-report-step">
                    <div class="gm-report-step__label">Loại sự cố <span class="required">*</span></div>
                    <div id="reportTypeSelector" class="gm-type-selector"></div>
                    <div class="gm-error" id="errType"></div>
                </div>

                <!-- Bước 2: Vị trí -->
                <div class="gm-report-step">
                    <div class="gm-report-step__label">Vị trí sự cố <span class="required">*</span></div>

                    <div class="gm-search-wrap">
                        <div class="gm-search-input-wrap">
                            <div class="gm-search-ico-left">${ICONS.search}</div>
                            <input type="text" id="reportLocationSearch" class="gm-report-input"
                                placeholder="Tìm kiếm địa chỉ, tên đường, địa điểm..." autocomplete="off" aria-label="Tìm kiếm địa điểm">
                            <button id="btnReportClearSearch" class="gm-search-clear" style="display:none" title="Xóa tìm kiếm">${ICONS.close}</button>
                            <div class="gm-search-spinner" id="reportSearchSpinner"></div>
                        </div>
                        <div id="reportSearchDropdown" class="gm-search-dropdown"></div>
                    </div>

                    <div class="gm-location-row">
                        <button class="gm-btn-location" id="btnPickLocation" onclick="window.MapReport.startPicking()">
                            ${ICONS.pin}
                            <span>Ghim trên bản đồ</span>
                        </button>
                        <button class="gm-btn-location gm-btn-location--icon" id="btnMyLocReport"
                            onclick="window.MapReport.useMyLocation()" title="Dùng vị trí hiện tại" aria-label="Dùng vị trí hiện tại">
                            ${ICONS.target}
                        </button>
                    </div>

                    <div id="pickedCoords" class="gm-picked-coords" style="display:none">
                        ${ICONS.check}
                        <span id="pickedCoordsText"></span>
                    </div>
                    <div class="gm-error" id="errLocation"></div>
                </div>

                <!-- Bước 3: Mô tả -->
                <div class="gm-report-step">
                    <div class="gm-report-step__label">Mô tả sự cố <span class="required">*</span></div>
                    <textarea id="reportTitle" class="gm-report-input"
                        placeholder="Tiêu đề ngắn gọn..." maxlength="200" rows="1"
                        style="margin-bottom:8px"></textarea>
                    <textarea id="reportDesc" class="gm-report-input"
                        placeholder="Mô tả chi tiết (tối thiểu 20 ký tự)..." maxlength="2000" rows="4"></textarea>
                    <div class="gm-char-count"><span id="descCharCount">0</span> / 2000</div>
                    <div class="gm-error" id="errDesc"></div>
                </div>

                <!-- Thời gian xảy ra -->
                <div class="gm-report-step">
                    <div class="gm-report-step__label">Thời gian xảy ra</div>
                    <input type="datetime-local" id="reportIncidentTime" class="gm-report-input" aria-label="Thời gian xảy ra">
                    <div class="gm-hint">Có thể bỏ trống nếu bạn không chắc thời điểm chính xác.</div>
                </div>

                <!-- Bước 4: Upload -->
                <div class="gm-report-step">
                    <div class="gm-report-step__label">Ảnh / Video bằng chứng</div>
                    <label class="gm-upload-label" id="reportDropZone" for="reportFiles">
                        <div class="gm-upload-label__icon">${ICONS.camera}</div>
                        <div class="gm-upload-label__text">
                            <span>Kéo thả hoặc chọn ảnh/video</span>
                            <small>JPG, PNG, WEBP, MP4, WEBM — tối đa 500MB mỗi file</small>
                        </div>
                    </label>
                    <input type="file" id="reportFiles"
                        accept="image/jpeg,image/png,image/webp,video/mp4,video/webm"
                        multiple style="display:none">
                    <div id="filePreview" class="gm-file-preview"></div>
                    <div id="uploadProgressList" class="gm-upload-progress-list"></div>
                </div>

                <!-- Xác nhận -->
                <div class="gm-report-step">
                    <label class="gm-confirm-check">
                        <input type="checkbox" id="reportConfirm">
                        <span>Tôi xác nhận thông tin trên là đúng sự thật và chịu trách nhiệm về báo cáo này</span>
                    </label>
                    <div class="gm-error" id="errConfirm"></div>
                </div>

            </div>

            <!-- Footer -->
            <div class="gm-report-modal__actions">
                <button class="gm-btn-cancel" onclick="window.MapReport.close()">Hủy</button>
                <button class="gm-btn-submit" id="btnSubmitReport" onclick="submitReport()">
                    ${ICONS.send}
                    Gửi báo cáo
                </button>
            </div>

        </div>
    `;
    document.body.appendChild(modal);
}

// ═══════════════════════════════════════════════════════════
// BIND EVENTS
// ═══════════════════════════════════════════════════════════

function bindEvents() {
    const fab = document.getElementById('btnOpenReport');
    if (fab) fab.onclick = openReportModal;

    const descEl = document.getElementById('reportDesc');
    if (descEl) {
        descEl.addEventListener('input', () => {
            document.getElementById('descCharCount').textContent = descEl.value.length;
        });
    }

    const filesInput = document.getElementById('reportFiles');
    const dropZone = document.getElementById('reportDropZone');
    if (filesInput) {
        filesInput.addEventListener('change', () => setSelectedFiles(Array.from(filesInput.files || [])));
    }
    if (dropZone) {
        ['dragenter', 'dragover'].forEach(eventName => {
            dropZone.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
                dropZone.classList.add('is-dragover');
            });
        });
        ['dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
                dropZone.classList.remove('is-dragover');
            });
        });
        dropZone.addEventListener('drop', (e) => {
            const files = Array.from(e.dataTransfer?.files || []);
            setSelectedFiles(files);
        });
    }

    renderTypeSelector();

    const searchInput = document.getElementById('reportLocationSearch');
    const dropdown = document.getElementById('reportSearchDropdown');
    const btnClear = document.getElementById('btnReportClearSearch');
    let searchTimer = null;

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const q = searchInput.value.trim();
            if (btnClear) btnClear.style.display = q ? 'block' : 'none';
            if (q.length < 2) {
                if (dropdown) dropdown.style.display = 'none';
                return;
            }
            clearTimeout(searchTimer);
            searchTimer = setTimeout(() => searchReportLocation(q), 380);
        });

        searchInput.addEventListener('focus', function () {
            if (dropdown && dropdown.children.length > 0) dropdown.style.display = 'block';
        });

        if (btnClear) {
            btnClear.onclick = () => {
                searchInput.value = '';
                btnClear.style.display = 'none';
                if (dropdown) { dropdown.style.display = 'none'; dropdown.innerHTML = ''; }
                searchInput.focus();
            };
        }

        document.addEventListener('click', (e) => {
            if (searchInput && dropdown && !searchInput.contains(e.target) && !dropdown.contains(e.target)) {
                dropdown.style.display = 'none';
            }
        });
    }

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') closeReportModal();
    });
}

function setupContextMenu() {
    waitForMapReport(() => {
        const map = window.MapCore.getMap();
        if (!map) return;

        let contextMenu = null;

        map.on('contextmenu', (e) => {
            if (contextMenu) { contextMenu.remove(); contextMenu = null; }

            const menuEl = document.createElement('div');
            menuEl.className = 'gm-context-menu';

            const btn = document.createElement('button');
            btn.className = 'gm-context-menu__item';
            btn.innerHTML = `${ICONS.alert} Báo cáo sự cố tại đây`;
            menuEl.appendChild(btn);

            menuEl.style.cssText = `
                position:fixed;
                left:${e.originalEvent.clientX}px;
                top:${e.originalEvent.clientY}px;
                z-index:1500;
            `;
            document.body.appendChild(menuEl);
            contextMenu = menuEl;

            btn.onclick = () => {
                menuEl.remove();
                contextMenu = null;
                pickedLatLng = { lat: e.lngLat.lat, lng: e.lngLat.lng };
                openReportModal();
                showPickedLocation();
            };

            setTimeout(() => {
                document.addEventListener('click', function removeMenu() {
                    if (contextMenu) { contextMenu.remove(); contextMenu = null; }
                    document.removeEventListener('click', removeMenu);
                }, { once: true });
            }, 100);
        });
    });
}

function waitForMapReport(cb, retries = 20) {
    if (window.MapCore && window.MapCore.getMap && window.MapCore.getMap()) {
        cb();
    } else if (retries > 0) {
        setTimeout(() => waitForMapReport(cb, retries - 1), 300);
    }
}

// ═══════════════════════════════════════════════════════════
// MODAL CONTROL
// ═══════════════════════════════════════════════════════════

function openReportModal() {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
        return;
    }
    const modal = document.getElementById('gmReportModal');
    if (!modal) return;
    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';
    if (pickedLatLng) showPickedLocation();
}

function closeReportModal() {
    if (pickingMode) {
        cancelPickingLocation();
        return;
    }
    const modal = document.getElementById('gmReportModal');
    if (modal) modal.style.display = 'none';
    document.body.style.overflow = '';
    resetForm();
}

function resetForm() {
    selectedTypeId = null;
    pickedLatLng = null;
    pickingMode = false;

    if (pickMarker) { pickMarker.remove(); pickMarker = null; }

    document.querySelectorAll('.gm-type-option').forEach(el => el.classList.remove('selected'));

    ['reportTitle', 'reportDesc', 'reportIncidentTime', 'reportFiles'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = '';
    });

    clearSelectedFiles();

    const confirm = document.getElementById('reportConfirm');
    if (confirm) confirm.checked = false;

    const preview = document.getElementById('filePreview');
    if (preview) preview.innerHTML = '';

    const progressList = document.getElementById('uploadProgressList');
    if (progressList) progressList.innerHTML = '';

    const charCount = document.getElementById('descCharCount');
    if (charCount) charCount.textContent = '0';

    const searchInput = document.getElementById('reportLocationSearch');
    if (searchInput) searchInput.value = '';

    const dropdown = document.getElementById('reportSearchDropdown');
    if (dropdown) { dropdown.style.display = 'none'; dropdown.innerHTML = ''; }

    const coordsEl = document.getElementById('pickedCoords');
    if (coordsEl) coordsEl.style.display = 'none';

    const pickBtn = document.getElementById('btnPickLocation');
    if (pickBtn) pickBtn.classList.remove('gm-btn-location--active');

    clearErrors();
}

// ═══════════════════════════════════════════════════════════
// LOCATION SEARCH
// ═══════════════════════════════════════════════════════════

async function searchReportLocation(query) {
    const key = window.APP_CONFIG?.goongRestApiKey;
    if (!key) return;

    const spinner = document.getElementById('reportSearchSpinner');
    if (spinner) spinner.style.display = 'block';

    try {
        const map = window.MapCore.getMap();
        const c = map ? map.getCenter() : { lat: 16.0544, lng: 108.2022 };
        const url = `https://rsapi.goong.io/Place/AutoComplete?input=${encodeURIComponent(query)}&location=${c.lat},${c.lng}&radius=50000&more_compound=true&api_key=${key}`;

        const res = await fetch(url);
        if (!res.ok) return;
        const data = await res.json();

        const dropdown = document.getElementById('reportSearchDropdown');
        if (!dropdown) return;

        if (!data.predictions?.length) {
            dropdown.innerHTML = `<div class="gm-search-empty">Không tìm thấy địa điểm phù hợp</div>`;
        } else {
            dropdown.innerHTML = data.predictions.slice(0, 5).map(p => `
                <div class="gm-search-item"
                     onclick="window.MapReport.selectSearchedLocation('${escAttr(p.place_id)}', '${escAttr(p.description)}', '${escAttr(p.structured_formatting?.main_text || p.description)}')">
                    <div class="gm-search-item__icon">${ICONS.location}</div>
                    <div>
                        <div class="gm-search-item__main">${escHtml(p.structured_formatting?.main_text || p.description)}</div>
                        <div class="gm-search-item__sub">${escHtml(p.structured_formatting?.secondary_text || '')}</div>
                    </div>
                </div>
            `).join('');
        }
        dropdown.style.display = 'block';
    } catch (e) {
        console.error('[REPORT] Search error:', e);
    } finally {
        if (spinner) spinner.style.display = 'none';
    }
}

async function selectSearchedLocation(placeId, description, mainText) {
    const dropdown = document.getElementById('reportSearchDropdown');
    const searchInput = document.getElementById('reportLocationSearch');
    if (dropdown) dropdown.style.display = 'none';
    if (searchInput) searchInput.value = mainText || description;

    const key = window.APP_CONFIG?.goongRestApiKey;
    if (!key) return;

    try {
        const res = await fetch(`https://rsapi.goong.io/Place/Detail?place_id=${placeId}&api_key=${key}`);
        if (!res.ok) return;
        const data = await res.json();
        const loc = data.result?.geometry?.location;
        if (!loc) return;

        pickedLatLng = { lat: loc.lat, lng: loc.lng };
        showPickedLocation();
        placePickMarker();
        document.getElementById('errLocation').textContent = '';

        const map = window.MapCore?.getMap();
        if (map) map.flyTo({ center: [loc.lng, loc.lat], zoom: 17, duration: 1000 });
    } catch (e) {
        console.error('[REPORT] Detail error:', e);
    }
}

function useMyLocation() {
    if (!navigator.geolocation) {
        showToastReport('Trình duyệt không hỗ trợ định vị.', 'warning');
        return;
    }
    const btn = document.getElementById('btnMyLocReport');
    const origContent = btn ? btn.innerHTML : '';
    if (btn) {
        btn.innerHTML = '<div class="gm-search-spinner" style="display:block;position:static;margin:auto"></div>';
        btn.disabled = true;
    }

    navigator.geolocation.getCurrentPosition(pos => {
        if (btn) { btn.innerHTML = origContent; btn.disabled = false; }
        pickedLatLng = { lat: pos.coords.latitude, lng: pos.coords.longitude };

        const searchInput = document.getElementById('reportLocationSearch');
        if (searchInput) searchInput.value = 'Vị trí hiện tại của bạn';

        showPickedLocation();
        placePickMarker();
        document.getElementById('errLocation').textContent = '';

        const map = window.MapCore?.getMap();
        if (map) map.flyTo({ center: [pos.coords.longitude, pos.coords.latitude], zoom: 17 });
    }, err => {
        if (btn) { btn.innerHTML = origContent; btn.disabled = false; }
        showToastReport('Không thể lấy định vị: ' + err.message, 'error');
    });
}

// ═══════════════════════════════════════════════════════════
// LOCATION PICKING (manual pin)
// ═══════════════════════════════════════════════════════════

let onMapPickClick = null;

function startPickingLocation() {
    const map = window.MapCore.getMap();
    if (!map) return;

    const modal = document.getElementById('gmReportModal');
    if (modal) modal.style.display = 'none'; // Ẩn hẳn thay vì làm mờ

    // Banner hướng dẫn
    if (!pickingBanner) {
        pickingBanner = document.createElement('div');
        pickingBanner.className = 'gm-picking-banner';
        pickingBanner.style.pointerEvents = 'auto'; // allow interaction
        pickingBanner.style.zIndex = '3000'; // Đảm bảo trên cùng
        pickingBanner.innerHTML = `${ICONS.pin} Nhấp vào bản đồ để ghim vị trí sự cố <button onclick="window.MapReport.cancelPicking()" style="margin-left:12px;background:#fff;color:#0f172a;border:none;padding:5px 12px;border-radius:6px;cursor:pointer;font-weight:700;font-size:12px;transition:background 0.15s">Hủy ghim</button>`;
        document.body.appendChild(pickingBanner);
    }

    map.getCanvas().style.cursor = 'crosshair';
    pickingMode = true;

    if (onMapPickClick) {
        map.off('click', onMapPickClick);
    }

    onMapPickClick = (e) => {
        pickedLatLng = { lat: e.lngLat.lat, lng: e.lngLat.lng };
        map.getCanvas().style.cursor = '';
        pickingMode = false;
        map.off('click', onMapPickClick);
        onMapPickClick = null;

        if (modal) modal.style.display = 'flex'; // Hiện lại form

        if (pickingBanner) { pickingBanner.remove(); pickingBanner = null; }

        showPickedLocation();
        placePickMarker();
    };

    map.on('click', onMapPickClick);
}

function cancelPickingLocation() {
    pickingMode = false;
    const map = window.MapCore?.getMap();
    if (map && onMapPickClick) {
        map.off('click', onMapPickClick);
        onMapPickClick = null;
        map.getCanvas().style.cursor = '';
    }

    const modal = document.getElementById('gmReportModal');
    if (modal) {
        modal.style.display = 'flex';
    }
    if (pickingBanner) { pickingBanner.remove(); pickingBanner = null; }
}

function showPickedLocation() {
    if (!pickedLatLng) return;

    const coordsEl = document.getElementById('pickedCoords');
    const coordsText = document.getElementById('pickedCoordsText');
    const btn = document.getElementById('btnPickLocation');

    if (coordsEl) coordsEl.style.display = 'flex';
    if (coordsText) coordsText.textContent = `${pickedLatLng.lat.toFixed(6)}, ${pickedLatLng.lng.toFixed(6)} — Vị trí đã được xác định`;
    if (btn) {
        btn.classList.add('gm-btn-location--active');
        btn.querySelector('span').textContent = 'Ghim lại vị trí';
    }

    const errLoc = document.getElementById('errLocation');
    if (errLoc) errLoc.textContent = '';
}

function placePickMarker() {
    const map = window.MapCore.getMap();
    if (!map || !pickedLatLng) return;

    if (pickMarker) pickMarker.remove();

    const el = document.createElement('div');
    el.style.cssText = `
        width:36px;height:36px;
        background:#dc2626;
        border:3px solid #fff;
        border-radius:50% 50% 50% 0;
        transform:translate(-50%,-100%) rotate(-45deg);
        box-shadow:0 3px 10px rgba(220,38,38,0.5);
        cursor:pointer;
    `;

    pickMarker = new goongjs.Marker({ element: el })
        .setLngLat([pickedLatLng.lng, pickedLatLng.lat])
        .addTo(map);
}

// ═══════════════════════════════════════════════════════════
// TYPE SELECTOR
// ═══════════════════════════════════════════════════════════

const CATEGORY_COLOR = {
    1: 'red',    // property_crime (trộm cắp, cướp)
    2: 'blue',   // public_disorder (giao thông, trật tự)
    3: 'amber',  // tourism_security (lừa đảo, chặt chém)
};

function renderTypeSelector() {
    const container = document.getElementById('reportTypeSelector');
    if (!container) return;

    if (alertTypes.length === 0) {
        setTimeout(renderTypeSelector, 500);
        return;
    }

    const groups = {};
    alertTypes.forEach(t => {
        const key = t.categoryId || 'other';
        if (!groups[key]) groups[key] = { name: t.categoryName || 'Khác', types: [] };
        groups[key].types.push(t);
    });

    const dotColor = { red: '#E24B4A', blue: '#378ADD', amber: '#BA7517' };

    let html = '';
    Object.values(groups).forEach(group => {
        const firstColor = dotColor[CATEGORY_COLOR[group.types[0]?.categoryId]] || '#888';

        html += `<div class="gm-type-group">
            <div class="gm-type-group__label" style="display:flex;align-items:center;gap:6px;">
                <span style="width:8px;height:8px;border-radius:50%;background:${firstColor};display:inline-block;flex-shrink:0"></span>
                ${escHtml(group.name)}
            </div>
            <div class="gm-type-grid">`;

        group.types.forEach(t => {
            const icon = ICONS[t.slug] || ICONS.alert;
            const colorClass = CATEGORY_COLOR[t.categoryId] || 'gray';

            html += `<div class="gm-type-option ${colorClass}" data-id="${t.id}" onclick="selectType(${t.id}, this)">
                <div class="gm-type-dot">${icon}</div>
                <span class="gm-type-name">${escHtml(t.name)}</span>
            </div>`;
        });
        html += `</div></div>`;
    });

    container.innerHTML = html;
}

function selectType(id, el) {
    selectedTypeId = id;
    document.querySelectorAll('.gm-type-option').forEach(o => o.classList.remove('selected'));
    el.classList.add('selected');
    document.getElementById('errType').textContent = '';
}

// ═══════════════════════════════════════════════════════════
// FILE PREVIEW
// ═══════════════════════════════════════════════════════════

function clearSelectedFiles() {
    previewObjectUrls.forEach(url => URL.revokeObjectURL(url));
    previewObjectUrls = [];
    selectedFiles = [];
}

function setSelectedFiles(files) {
    clearSelectedFiles();

    const accepted = [];
    const rejectedNames = [];

    files.forEach(file => {
        if (!isAllowedMediaFile(file)) {
            rejectedNames.push(file.name);
            return;
        }
        accepted.push(file);
    });

    selectedFiles = accepted;
    renderFilePreview();

    if (rejectedNames.length > 0) {
        showToastReport(`Một số file không hợp lệ: ${rejectedNames.slice(0, 3).join(', ')}`, 'warning');
    }
}

function isAllowedMediaFile(file) {
    const lowerName = file.name.toLowerCase();
    const hasAllowedExtension = CLIENT_ALLOWED_MEDIA_EXTENSIONS.some(ext => lowerName.endsWith(ext));
    const hasAllowedType = CLIENT_ALLOWED_MEDIA_TYPES.has(file.type);
    return file.size > 0 && file.size <= CLIENT_MAX_MEDIA_SIZE && hasAllowedExtension && (hasAllowedType || file.type === '');
}

function renderFilePreview() {
    const preview = document.getElementById('filePreview');
    if (!preview) return;
    preview.innerHTML = '';

    selectedFiles.forEach(file => {
        const objectUrl = URL.createObjectURL(file);
        previewObjectUrls.push(objectUrl);

        const item = document.createElement('div');
        item.className = 'gm-preview-item';

        const name = escHtml(file.name.length > 18 ? `${file.name.slice(0, 15)}...` : file.name);
        const size = formatFileSize(file.size);

        if (file.type.startsWith('image/')) {
            item.innerHTML = `<img src="${objectUrl}" alt="${escHtml(file.name)}">
                <span>${name}</span><small>${size}</small>`;
        } else {
            item.innerHTML = `<video src="${objectUrl}" muted preload="metadata"></video>
                <span>${name}</span><small>${size}</small>`;
        }
        preview.appendChild(item);
    });
}

function handleFilePreview(input) {
    setSelectedFiles(Array.from(input.files || []));
}

// ═══════════════════════════════════════════════════════════
// VALIDATION & SUBMIT
// ═══════════════════════════════════════════════════════════

function validateForm() {
    clearErrors();
    let valid = true;

    if (!selectedTypeId) {
        document.getElementById('errType').textContent = 'Vui lòng chọn loại sự cố';
        valid = false;
    }
    if (!pickedLatLng) {
        document.getElementById('errLocation').textContent = 'Vui lòng xác định vị trí sự cố';
        valid = false;
    }
    const desc = document.getElementById('reportDesc')?.value?.trim() || '';
    if (desc.length < 20) {
        document.getElementById('errDesc').textContent = 'Mô tả cần ít nhất 20 ký tự';
        valid = false;
    }
    if (!document.getElementById('reportConfirm')?.checked) {
        document.getElementById('errConfirm').textContent = 'Vui lòng xác nhận trách nhiệm';
        valid = false;
    }

    return valid;
}

function clearErrors() {
    ['errType', 'errLocation', 'errDesc', 'errConfirm'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.textContent = '';
    });
}

async function submitReport() {
    if (!validateForm()) return;

    const btn = document.getElementById('btnSubmitReport');
    btn.disabled = true;
    btn.innerHTML = `<div class="gm-search-spinner" style="display:block;position:static;width:16px;height:16px;border-color:#ffffff44;border-top-color:#fff;margin:0 auto"></div> Đang gửi...`;

    try {
        const title = document.getElementById('reportTitle')?.value?.trim()
            || document.querySelector('.gm-type-option.selected .gm-type-name')?.textContent
            || 'Báo cáo sự cố';
        const desc = document.getElementById('reportDesc').value.trim();
        const incidentTime = document.getElementById('reportIncidentTime')?.value;
        const addrText = document.getElementById('reportLocationSearch')?.value?.trim() || '';

        const formData = new FormData();
        formData.append('alertTypeId', selectedTypeId);
        formData.append('latitude', pickedLatLng.lat);
        formData.append('longitude', pickedLatLng.lng);
        formData.append('addressText', addrText);
        formData.append('title', title);
        formData.append('description', desc);
        if (incidentTime) formData.append('incidentTime', incidentTime);
        formData.append('userConfirmed', 'true');

        const token = localStorage.getItem('token');
        const res = await fetch('/api/alerts', {
            method: 'POST',
            headers: token ? { 'Authorization': `Bearer ${token}` } : {},
            body: formData
        });

        if (!res.ok) {
            showToastReport(await readResponseMessage(res, 'Gửi báo cáo thất bại'), 'error');
            return;
        }

        const data = await res.json();
        if (!data.success) {
            showToastReport(data.message || 'Gửi báo cáo thất bại', 'error');
            return;
        }

        let finalResult = data;
        if (selectedFiles.length > 0 && data.status !== 'REJECTED') {
            btn.innerHTML = `<div class="gm-search-spinner" style="display:block;position:static;width:16px;height:16px;border-color:#ffffff44;border-top-color:#fff;margin:0 auto"></div> Đang tải bằng chứng...`;
            finalResult = await uploadSelectedMediaFiles(data.alertId, token);
        }

        closeReportModal();
        showReportSuccess(finalResult || data);
    } catch (err) {
        showToastReport(err.message || 'Lỗi kết nối. Vui lòng thử lại.', 'error');
    } finally {
        btn.disabled = false;
        btn.innerHTML = `${ICONS.send} Gửi báo cáo`;
    }
}

async function uploadSelectedMediaFiles(alertId, token) {
    const progressList = document.getElementById('uploadProgressList');
    if (progressList) progressList.innerHTML = '';

    let lastResult = null;
    for (let index = 0; index < selectedFiles.length; index++) {
        lastResult = await uploadMediaFile(alertId, selectedFiles[index], token, index);
    }

    return lastResult;
}

async function uploadMediaFile(alertId, file, token, index) {
    renderUploadProgress(file, index, 0, 'Đang tạo phiên upload');

    const sessionRes = await fetch(`/api/alerts/${alertId}/media/session`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        },
        body: JSON.stringify({
            fileName: file.name,
            fileSize: file.size,
            contentType: file.type
        })
    });

    if (!sessionRes.ok) {
        throw new Error(await readResponseMessage(sessionRes, `Không thể tạo phiên upload cho ${file.name}`));
    }

    const session = await sessionRes.json();
    const chunkSize = session.chunkSize || (5 * 1024 * 1024);
    const totalChunks = Math.ceil(file.size / chunkSize);

    for (let chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++) {
        const start = chunkIndex * chunkSize;
        const end = Math.min(start + chunkSize, file.size);
        const chunk = file.slice(start, end);

        await uploadChunk(alertId, session.uploadId, chunkIndex, chunk, token, (loaded, total) => {
            const chunkProgress = total > 0 ? loaded / total : 0;
            const overall = ((chunkIndex + chunkProgress) / totalChunks) * 100;
            renderUploadProgress(file, index, overall, `Đang tải ${chunkIndex + 1}/${totalChunks}`);
        });
    }

    const completeRes = await fetch(`/api/alerts/${alertId}/media/complete`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        },
        body: JSON.stringify({
            uploadId: session.uploadId,
            totalChunks
        })
    });

    if (!completeRes.ok) {
        throw new Error(await readResponseMessage(completeRes, `Không thể hoàn tất upload cho ${file.name}`));
    }

    const result = await completeRes.json();
    if (!result.success) {
        throw new Error(result.message || `Không thể hoàn tất upload cho ${file.name}`);
    }

    renderUploadProgress(file, index, 100, 'Đã tải xong');
    return result;
}

function uploadChunk(alertId, uploadId, chunkIndex, chunk, token, onProgress) {
    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.open('POST', `/api/alerts/${alertId}/media/chunk?uploadId=${encodeURIComponent(uploadId)}&chunkIndex=${chunkIndex}`);
        xhr.setRequestHeader('Content-Type', 'application/octet-stream');
        if (token) xhr.setRequestHeader('Authorization', `Bearer ${token}`);

        xhr.upload.onprogress = (event) => {
            if (event.lengthComputable && onProgress) onProgress(event.loaded, event.total);
        };
        xhr.onload = () => {
            if (xhr.status >= 200 && xhr.status < 300) {
                resolve();
                return;
            }
            reject(new Error(readXhrMessage(xhr, 'Upload chunk thất bại')));
        };
        xhr.onerror = () => reject(new Error('Không thể kết nối máy chủ khi upload'));
        xhr.send(chunk);
    });
}

function renderUploadProgress(file, index, percent, statusText) {
    const list = document.getElementById('uploadProgressList');
    if (!list) return;

    let row = list.querySelector(`[data-upload-index="${index}"]`);
    if (!row) {
        row = document.createElement('div');
        row.className = 'gm-upload-progress-item';
        row.dataset.uploadIndex = index;
        row.innerHTML = `
            <div class="gm-upload-progress-head">
                <span>${escHtml(file.name)}</span>
                <small>${formatFileSize(file.size)}</small>
            </div>
            <div class="gm-upload-progress-bar"><div></div></div>
            <div class="gm-upload-progress-status"></div>
        `;
        list.appendChild(row);
    }

    const clamped = Math.max(0, Math.min(100, percent));
    const bar = row.querySelector('.gm-upload-progress-bar div');
    const status = row.querySelector('.gm-upload-progress-status');
    if (bar) bar.style.width = `${clamped.toFixed(0)}%`;
    if (status) status.textContent = `${statusText} (${clamped.toFixed(0)}%)`;
}

async function readResponseMessage(res, fallback) {
    try {
        const data = await res.json();
        return data.message || fallback;
    } catch (_) {
        return `${fallback} (${res.status})`;
    }
}

function readXhrMessage(xhr, fallback) {
    try {
        const data = JSON.parse(xhr.responseText || '{}');
        return data.message || fallback;
    } catch (_) {
        return `${fallback} (${xhr.status})`;
    }
}

function formatFileSize(size) {
    if (size >= 1024 * 1024 * 1024) return `${(size / (1024 * 1024 * 1024)).toFixed(2)} GB`;
    if (size >= 1024 * 1024) return `${(size / (1024 * 1024)).toFixed(1)} MB`;
    if (size >= 1024) return `${(size / 1024).toFixed(1)} KB`;
    return `${size} B`;
}

function showReportSuccess(result) {
    const message = result?.message
        || (result?.status === 'VISIBLE_UNVERIFIED'
            ? 'Báo cáo đã được gửi thành công và đang hiển thị ở trạng thái Chưa xác thực.'
            : 'Báo cáo đã được gửi thành công.');

    showToastReport(message, 'success');
    if (window.MapData) setTimeout(() => window.MapData.refresh(), 1500);
}

function showToastReport(msg, type) {
    const colors = { success: '#16a34a', error: '#dc2626', warning: '#d97706' };
    const el = document.createElement('div');
    el.className = 'gm-toast';
    el.style.background = colors[type] || '#3b82f6';
    el.textContent = msg;
    document.body.appendChild(el);
    setTimeout(() => el.remove(), 4500);
}

// ═══════════════════════════════════════════════════════════
// HELPERS
// ═══════════════════════════════════════════════════════════

function escHtml(str) {
    if (!str) return '';
    const d = document.createElement('div');
    d.textContent = str;
    return d.innerHTML;
}

function escAttr(str) {
    if (!str) return '';
    return str.replace(/'/g, "\\'").replace(/"/g, '&quot;');
}

// ═══════════════════════════════════════════════════════════
// EXPORT
// ═══════════════════════════════════════════════════════════

window.MapReport = {
    open: openReportModal,
    close: closeReportModal,
    startPicking: startPickingLocation,
    cancelPicking: cancelPickingLocation,
    selectSearchedLocation: selectSearchedLocation,
    useMyLocation: useMyLocation,
    loadMyReports: loadMyReports
};

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        initReportForm();
        initDashboard();
    });
} else {
    initReportForm();
    initDashboard();
}

// ═══════════════════════════════════════════════════════════
// DASHBOARD LOGIC (BÁO CÁO CỦA TÔI)
// ═══════════════════════════════════════════════════════════
function initDashboard() {
    const btnMap = document.getElementById('btnNavMap');
    const btnReports = document.getElementById('btnNavReports');
    const stateSearch = document.getElementById('stateSearch');
    const placeCard = document.getElementById('gmPlaceCard');
    const routeCard = document.getElementById('gmRouteCard');
    const reportsCard = document.getElementById('gmMyReportsCard');

    if (!btnMap || !btnReports) return;

    btnMap.addEventListener('click', () => {
        btnMap.classList.add('active');
        btnReports.classList.remove('active');

        // Hiện lại map UI
        reportsCard.style.display = 'none';
        stateSearch.style.display = 'block';
    });

    btnReports.addEventListener('click', () => {
        btnReports.classList.add('active');
        btnMap.classList.remove('active');

        // Ẩn map UI, hiện reports
        if (stateSearch) stateSearch.style.display = 'none';
        if (placeCard) placeCard.style.display = 'none';
        if (routeCard) routeCard.style.display = 'none';

        reportsCard.style.display = 'flex';

        loadMyReports();
    });
}

async function loadMyReports() {
    const listEl = document.getElementById('myReportsList');
    if (!listEl) return;

    // Kiểm tra token xem đã đăng nhập chưa
    const token = localStorage.getItem('token');
    if (!token) {
        listEl.innerHTML = `
            <div class="gm-report-empty">
                <p>Bạn cần đăng nhập để xem báo cáo của mình.</p>
                <button onclick="window.location.href='/Auth/Login'" class="gm-btn gm-btn-primary" style="margin-top: 10px; width: auto; padding: 6px 16px;">Đăng nhập ngay</button>
            </div>
        `;
        return;
    }

    listEl.innerHTML = `
        <div class="gm-report-empty">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                <polyline points="17 8 12 3 7 8"></polyline>
                <line x1="12" y1="3" x2="12" y2="15"></line>
            </svg>
            <p>Đang tải dữ liệu...</p>
        </div>
    `;

    try {
        const res = await fetch('/api/alerts/my', {
            headers: { 'Authorization': 'Bearer ' + token }
        });

        if (res.status === 401) {
            localStorage.removeItem('token');
            loadMyReports();
            return;
        }

        if (!res.ok) throw new Error('Lỗi khi tải dữ liệu');
        const data = await res.json();

        if (!data || data.length === 0) {
            listEl.innerHTML = `
                <div class="gm-report-empty">
                    <p>Bạn chưa gửi báo cáo nào.</p>
                </div>
            `;
            return;
        }

        let html = '';
        data.forEach(r => {
            const icon = ICONS[r.alertTypeSlug] || ICONS.alert;
            let statusHtml = '';
            if (r.status === 'Pending') statusHtml = '<span class="gm-report-status gm-status-pending">Chờ duyệt</span>';
            else if (r.status === 'Approved') statusHtml = '<span class="gm-report-status gm-status-approved">Đã duyệt</span>';
            else if (r.status === 'Rejected') statusHtml = '<span class="gm-report-status gm-status-rejected">Từ chối</span>';
            else if (r.status === 'NeedsMoreInfo') statusHtml = '<span class="gm-report-status gm-status-moreinfo">Cần bổ sung</span>';

            const d = new Date(r.createdAt);
            const dateStr = d.toLocaleDateString('vi-VN') + ' ' + d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });

            html += `
                <div class="gm-report-item" onclick="if(window.MapCore) { window.MapCore.getMap().flyTo({center: [${r.longitude}, ${r.latitude}], zoom: 16}); }">
                    <div class="gm-report-item-header">
                        <div class="gm-report-type">
                            <div class="gm-report-type-icon">${icon}</div>
                            ${escHtml(r.alertTypeName)}
                        </div>
                        ${statusHtml}
                    </div>
                    <div class="gm-report-desc">${escHtml(r.description || 'Không có mô tả')}</div>
                    <div class="gm-report-footer">
                        <span>${dateStr}</span>
                    </div>
                </div>
            `;
        });

        listEl.innerHTML = html;

    } catch (e) {
        listEl.innerHTML = `
            <div class="gm-report-empty">
                <p style="color: #dc2626">Không thể tải báo cáo. Vui lòng thử lại sau.</p>
            </div>
        `;
    }
}
