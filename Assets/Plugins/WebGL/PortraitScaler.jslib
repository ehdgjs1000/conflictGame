mergeInto(LibraryManager.library, {
  InstallPortraitScaler: function () {
    try {
      console.log('[PortraitScaler] InstallPortraitScaler called');

      // ===== 필요 시 모드만 바꾸면 됨 =====
      // 'contain' : 비율 유지 + 레터박스
      // 'height'  : 세로 꽉(좌우 잘림 가능) ← 권장
      // 'width'   : 가로 꽉(상하 잘림 가능)
      // 'cover'   : 화면 가득(잘림 허용, 가장 큼)
      var MODE  = 'height';
      var DEBUG = false;
      // ===================================

      var LOGICAL_W = 1080, LOGICAL_H = 1920;

      // 캔버스
      var canvas = document.querySelector('#unity-canvas, canvas');
      if (!canvas) { console.log('[PortraitScaler] no canvas'); return; }

      // 1) html/body 최소 정규화
      (function normalizeRootOnce(){
        var html = document.documentElement, body = document.body;
        html.style.background = '#000';
        html.style.overflow   = 'hidden';
        body.style.background = '#000';
        body.style.overflow   = 'hidden';
        body.style.setProperty('-webkit-text-size-adjust','100%','important');
      })();

      // 2) 캔버스 버퍼/스타일 고정(값이 다를 때만)
      function lockCanvasWH() {
        if (canvas.width  !== LOGICAL_W)  canvas.width  = LOGICAL_W;
        if (canvas.height !== LOGICAL_H)  canvas.height = LOGICAL_H;

        if (canvas.style.width  !== LOGICAL_W + 'px')
          canvas.style.setProperty('width',  LOGICAL_W + 'px', 'important');
        if (canvas.style.height !== LOGICAL_H + 'px')
          canvas.style.setProperty('height', LOGICAL_H + 'px', 'important');

        canvas.style.display    = 'block';
        canvas.style.background = '#000';
        canvas.style.maxWidth   = LOGICAL_W + 'px';
        canvas.style.maxHeight  = LOGICAL_H + 'px';
        // 입력/제스처 간섭 최소화
        canvas.style.pointerEvents = 'auto';
        canvas.style.touchAction   = 'none';
        canvas.style.userSelect    = 'none';
      }
      lockCanvasWH();

      // 3) 오버레이/루트 생성(중앙 정렬 컨테이너)
      var overlay = document.getElementById('portrait-overlay');
      if (!overlay) {
        overlay = document.createElement('div');
        overlay.id = 'portrait-overlay';
        document.body.appendChild(overlay);
      }
      var root = document.getElementById('portrait-root');
      if (!root) {
        root = document.createElement('div');
        root.id = 'portrait-root';
        overlay.appendChild(root);
      }
      if (canvas.parentElement !== root) {
        if (canvas.parentElement) canvas.parentElement.removeChild(canvas);
        root.appendChild(canvas);
      }

      (function styleOverlayRootOnce(){
        // overlay: 전체 화면 중앙 정렬
        overlay.style.position       = 'fixed';
        overlay.style.inset          = '0';
        overlay.style.display        = 'flex';
        overlay.style.alignItems     = 'center';
        overlay.style.justifyContent = 'center';
        overlay.style.zIndex         = '9999';
        overlay.style.background     = '#000';
        // ★ 입력 통과(반드시 auto)
        overlay.style.pointerEvents  = 'auto';

        // root: 스케일 대상
        root.style.width            = LOGICAL_W + 'px';
        root.style.height           = LOGICAL_H + 'px';
        root.style.transformOrigin  = 'center center';
        root.style.willChange       = 'transform';
        // ★ 입력 정상화
        root.style.pointerEvents    = 'auto';
      })();

      // 4) 뷰포트 & 스케일 계산
      function getViewport() {
        var vv = window.visualViewport;
        var vw = vv ? vv.width  : 0;
        var vh = vv ? vh = vv.height : 0;
        var w = Math.min(
          vw || Infinity,
          document.documentElement.clientWidth  || Infinity,
          window.innerWidth || Infinity
        );
        var h = Math.min(
          vh || Infinity,
          document.documentElement.clientHeight || Infinity,
          window.innerHeight || Infinity
        );
        return { w: isFinite(w) ? w : window.innerWidth,
                 h: isFinite(h) ? h : window.innerHeight };
      }
      function computeScale(vpW, vpH) {
        var sx = vpW / LOGICAL_W, sy = vpH / LOGICAL_H;
        switch (MODE) {
          case 'height': return sy;
          case 'width' : return sx;
          case 'cover' : return Math.max(sx, sy);
          case 'contain':
          default      : return Math.min(sx, sy);
        }
      }

      // 5) 변경 감지 리스케일(주기 타이머 없음)
      var lastVpW = 0, lastVpH = 0, lastScale = 0;
      function fit(force) {
        lockCanvasWH();

        var vp = getViewport();
        var scale = computeScale(vp.w, vp.h);

        var vpChanged    = Math.abs(vp.w - lastVpW) > 1 || Math.abs(vp.h - lastVpH) > 1;
        var scaleChanged = Math.abs(scale - lastScale) > 0.001;

        if (force || vpChanged || scaleChanged) {
          root.style.transform = 'scale(' + scale + ')';
          lastVpW = vp.w; lastVpH = vp.h; lastScale = scale;
          if (DEBUG) console.log('[PortraitScaler] fit vp=', vp.w+'x'+vp.h, 'mode=', MODE, 'scale=', scale.toFixed(3));
        }
      }

      // 6) 이벤트에만 반응
      window.addEventListener('resize', fit, { passive: true });
      window.addEventListener('orientationchange', fit, { passive: true });
      if ('visualViewport' in window) {
        window.visualViewport.addEventListener('resize', fit, { passive:true });
        window.visualViewport.addEventListener('scroll', fit, { passive:true });
      }

      // 7) 초기 안정화: 2프레임 후 강제 1회
      requestAnimationFrame(()=>requestAnimationFrame(()=>fit(true)));

      console.log('[PortraitScaler] installed (buffer:', canvas.width, 'x', canvas.height, ', mode:', MODE, ')');
    } catch (e) {
      console.log('[PortraitScaler] error:', e);
    }
  }
});
