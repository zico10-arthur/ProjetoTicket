window.plateiaSeatMap = {
    _instances: {},

    init: function (viewportId) {
        const root = document.getElementById(viewportId);
        if (!root) return;

        const surface = root.querySelector('.t360-map__surface') || root.querySelector('.seat-map-viewport');
        const transform = root.querySelector('.t360-map__transform') || root.querySelector('.seat-map-transform');
        if (!surface || !transform) return;

        let scale = 1;
        let panX = 0;
        let panY = 0;
        let dragging = false;
        let startX = 0;
        let startY = 0;

        const applyTransform = () => {
            transform.style.transform = `translate(${panX}px, ${panY}px) scale(${scale})`;
        };

        const isSeat = (target) => !!target.closest('.t360-seat, .seat-svg-group');

        const onWheel = (e) => {
            e.preventDefault();
            const delta = e.deltaY > 0 ? -0.07 : 0.07;
            scale = Math.min(1.8, Math.max(0.85, scale + delta));
            applyTransform();
        };

        const onMouseDown = (e) => {
            if (e.button !== 0 || isSeat(e.target)) return;
            dragging = true;
            startX = e.clientX - panX;
            startY = e.clientY - panY;
            surface.style.cursor = 'grabbing';
        };

        const onMouseMove = (e) => {
            if (!dragging) return;
            panX = e.clientX - startX;
            panY = e.clientY - startY;
            applyTransform();
        };

        const onMouseUp = () => {
            dragging = false;
            surface.style.cursor = 'grab';
        };

        let lastTouchDist = 0;

        const onTouchStart = (e) => {
            if (e.touches.length === 2) {
                e.preventDefault();
                lastTouchDist = Math.hypot(
                    e.touches[0].clientX - e.touches[1].clientX,
                    e.touches[0].clientY - e.touches[1].clientY);
            } else if (e.touches.length === 1 && !isSeat(e.target)) {
                e.preventDefault();
                dragging = true;
                startX = e.touches[0].clientX - panX;
                startY = e.touches[0].clientY - panY;
            }
        };

        const onTouchMove = (e) => {
            if (e.touches.length === 2) {
                e.preventDefault();
                const dist = Math.hypot(
                    e.touches[0].clientX - e.touches[1].clientX,
                    e.touches[0].clientY - e.touches[1].clientY);
                if (lastTouchDist > 0) {
                    scale = Math.min(1.8, Math.max(0.85, scale + (dist - lastTouchDist) * 0.004));
                    applyTransform();
                }
                lastTouchDist = dist;
            } else if (dragging && e.touches.length === 1) {
                panX = e.touches[0].clientX - startX;
                panY = e.touches[0].clientY - startY;
                applyTransform();
            }
        };

        const onTouchEnd = () => {
            dragging = false;
            lastTouchDist = 0;
        };

        surface.addEventListener('wheel', onWheel, { passive: false });
        surface.addEventListener('mousedown', onMouseDown);
        window.addEventListener('mousemove', onMouseMove);
        window.addEventListener('mouseup', onMouseUp);
        surface.addEventListener('touchstart', onTouchStart, { passive: false });
        surface.addEventListener('touchmove', onTouchMove, { passive: false });
        surface.addEventListener('touchend', onTouchEnd);

        applyTransform();

        this._instances[viewportId] = {
            dispose: () => {
                surface.removeEventListener('wheel', onWheel);
                surface.removeEventListener('mousedown', onMouseDown);
                window.removeEventListener('mousemove', onMouseMove);
                window.removeEventListener('mouseup', onMouseUp);
                surface.removeEventListener('touchstart', onTouchStart);
                surface.removeEventListener('touchmove', onTouchMove);
                surface.removeEventListener('touchend', onTouchEnd);
            },
            reset: () => {
                scale = 1;
                panX = 0;
                panY = 0;
                applyTransform();
            },
            zoomIn: () => {
                scale = Math.min(1.8, scale + 0.1);
                applyTransform();
            },
            zoomOut: () => {
                scale = Math.max(0.85, scale - 0.1);
                applyTransform();
            }
        };
    },

    reset: function (viewportId) {
        this._instances[viewportId]?.reset();
    },

    zoomIn: function (viewportId) {
        this._instances[viewportId]?.zoomIn();
    },

    zoomOut: function (viewportId) {
        this._instances[viewportId]?.zoomOut();
    },

    dispose: function (viewportId) {
        this._instances[viewportId]?.dispose();
        delete this._instances[viewportId];
    }
};
