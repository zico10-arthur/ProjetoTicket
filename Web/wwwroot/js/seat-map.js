window.plateiaSeatMap = {
    _instances: {},

    init: function (viewportId, dotNetRef) {
        const viewport = document.getElementById(viewportId);
        if (!viewport) return;

        const transform = viewport.querySelector('.seat-map-transform');
        if (!transform) return;

        let scale = 1;
        let panX = 0;
        let panY = 0;
        let dragging = false;
        let startX = 0;
        let startY = 0;

        const applyTransform = () => {
            transform.style.transform = `translate(${panX}px, ${panY}px) scale(${scale})`;
        };

        const onWheel = (e) => {
            e.preventDefault();
            const delta = e.deltaY > 0 ? -0.08 : 0.08;
            scale = Math.min(2.5, Math.max(0.5, scale + delta));
            applyTransform();
        };

        const onMouseDown = (e) => {
            if (e.button !== 0) return;
            dragging = true;
            startX = e.clientX - panX;
            startY = e.clientY - panY;
            viewport.style.cursor = 'grabbing';
        };

        const onMouseMove = (e) => {
            if (!dragging) return;
            panX = e.clientX - startX;
            panY = e.clientY - startY;
            applyTransform();
        };

        const onMouseUp = () => {
            dragging = false;
            viewport.style.cursor = 'grab';
        };

        let lastTouchDist = 0;

        const onTouchStart = (e) => {
            if (e.touches.length === 2) {
                lastTouchDist = Math.hypot(
                    e.touches[0].clientX - e.touches[1].clientX,
                    e.touches[0].clientY - e.touches[1].clientY);
            } else if (e.touches.length === 1) {
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
                    scale = Math.min(2.5, Math.max(0.5, scale + (dist - lastTouchDist) * 0.005));
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

        viewport.addEventListener('wheel', onWheel, { passive: false });
        viewport.addEventListener('mousedown', onMouseDown);
        window.addEventListener('mousemove', onMouseMove);
        window.addEventListener('mouseup', onMouseUp);
        viewport.addEventListener('touchstart', onTouchStart, { passive: true });
        viewport.addEventListener('touchmove', onTouchMove, { passive: false });
        viewport.addEventListener('touchend', onTouchEnd);

        viewport.style.cursor = 'grab';
        applyTransform();

        this._instances[viewportId] = {
            dispose: () => {
                viewport.removeEventListener('wheel', onWheel);
                viewport.removeEventListener('mousedown', onMouseDown);
                window.removeEventListener('mousemove', onMouseMove);
                window.removeEventListener('mouseup', onMouseUp);
                viewport.removeEventListener('touchstart', onTouchStart);
                viewport.removeEventListener('touchmove', onTouchMove);
                viewport.removeEventListener('touchend', onTouchEnd);
            },
            reset: () => {
                scale = 1;
                panX = 0;
                panY = 0;
                applyTransform();
            },
            zoomIn: () => {
                scale = Math.min(2.5, scale + 0.15);
                applyTransform();
            },
            zoomOut: () => {
                scale = Math.max(0.5, scale - 0.15);
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
