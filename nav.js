/* ============================================================
   Natane Toon Shader - Documentation Site Navigation
   natanetoon.com  v2 - Individual page navigation
   ============================================================ */

(function () {
  'use strict';

  /* ---------- Site Map Data ---------- */
  var CATEGORIES = {
    basic:       { label: '基本',           icon: '🎨' },
    lighting:    { label: 'ライティング',   icon: '💡' },
    effects:     { label: 'エフェクト',     icon: '✨' },
    environment: { label: '環境',           icon: '🌐' },
    advanced:    { label: '詳細',           icon: '⚙️' }
  };

  var TOOL_CATEGORIES = {
    general:      { label: '全般' },
    material:     { label: 'マテリアル' },
    preset:       { label: 'プリセット' },
    effect:       { label: 'エフェクト' },
    optimization: { label: '最適化' },
    migration:    { label: '移行' },
    shader:       { label: 'シェーダー' }
  };

  /* Page order within each category */
  var PAGE_ORDER = {
    basic: [
      { file: 'main-texture.html',      label: 'メインテクスチャ' },
      { file: 'color-enhancement.html',  label: 'カラー保持・強化' },
      { file: 'final-blend.html',        label: '最終カラーブレンド' },
      { file: 'surface-finish.html',     label: '表面仕上げ' },
      { file: 'makeup.html',             label: 'メイクアップ' },
      { file: 'shading.html',            label: 'シェーディング' }
    ],
    lighting: [
      { file: 'lighting-general.html',   label: 'ライティング全般' },
      { file: 'soft-lighting.html',      label: 'ソフトライティング' },
      { file: 'backlight.html',          label: 'バックライト' },
      { file: 'light-volumes.html',      label: 'VRC Light Volumes' },
      { file: 'ltcgi.html',              label: 'LTCGI' },
      { file: 'ao.html',                 label: 'アンビエントオクルージョン' },
      { file: 'dithering.html',          label: 'ディザリング' },
      { file: 'shadow-color.html',       label: 'シャドウカラーテクスチャ' }
    ],
    effects: [
      { file: 'specular.html',           label: 'スペキュラー' },
      { file: 'rimlight1.html',          label: 'リムライト1' },
      { file: 'rimlight2.html',          label: 'リムライト2' },
      { file: 'rim-direction.html',      label: 'リムライト方向制御' },
      { file: 'sss.html',               label: 'SSS' },
      { file: 'matcap1.html',            label: 'MatCap 1' },
      { file: 'matcap23.html',           label: 'MatCap 2/3' },
      { file: 'glitter.html',            label: 'グリッター' },
      { file: 'waterdrip.html',          label: '雫エフェクト' },
      { file: 'hologram.html',           label: 'ホログラム' },
      { file: 'glitch.html',             label: 'グリッチ' },
      { file: 'decal.html',              label: 'デカル' },
      { file: 'outline.html',            label: 'アウトライン' },
      { file: 'emission.html',           label: 'エミッション' },
      { file: 'dissolve.html',           label: '溶解' },
      { file: 'alpha-mask.html',         label: 'アルファマスク' },
      { file: 'hue-shift.html',          label: '色相シフト' },
      { file: 'audiolink.html',          label: 'AudioLink' }
    ],
    environment: [
      { file: 'reflection.html',         label: 'リフレクション' },
      { file: 'iridescence.html',        label: 'イリデッセンス' },
      { file: 'env-rim.html',            label: '環境リム' },
      { file: 'refraction.html',         label: '屈折' }
    ],
    advanced: [
      { file: 'normal-map.html',         label: 'ノーマルマップ' },
      { file: 'parallax.html',           label: 'パララックス' },
      { file: 'vat.html',               label: 'VAT' },
      { file: 'backface.html',           label: '裏面テクスチャ' },
      { file: 'video-texture.html',      label: 'ビデオテクスチャ' },
      { file: 'distance-fade.html',      label: '距離フェード' },
      { file: 'vertex-animation.html',   label: '頂点アニメーション' },
      { file: 'rendering.html',          label: 'レンダリング設定' }
    ]
  };

  var TOOL_ORDER = [
    { file: 'dashboard.html',             label: 'ダッシュボード',               cat: 'general' },
    { file: 'help.html',                  label: 'ヘルプ',                       cat: 'general' },
    { file: 'material-validator.html',    label: 'マテリアル検証',               cat: 'material' },
    { file: 'material-editor.html',       label: 'マテリアルエディタ',           cat: 'material' },
    { file: 'material-preview.html',      label: 'マテリアルプレビュー',         cat: 'material' },
    { file: 'material-comparison.html',   label: 'マテリアル比較',               cat: 'material' },
    { file: 'makeup-layer-manager.html',  label: 'メイクアップレイヤー管理',     cat: 'material' },
    { file: 'preset-browser.html',        label: 'プリセットブラウザ',           cat: 'preset' },
    { file: 'color-palette.html',         label: 'カラーパレット',               cat: 'preset' },
    { file: 'preset-generator.html',      label: 'デフォルトプリセット生成',     cat: 'preset' },
    { file: 'preset-regenerator.html',    label: '全プリセット再生成',           cat: 'preset' },
    { file: 'shadow-wizard.html',         label: 'シャドウ調整ウィザード',       cat: 'effect' },
    { file: 'matcap-composer.html',       label: 'MatCapコンポーザー',           cat: 'effect' },
    { file: 'dissolve-generator.html',    label: 'ディゾルブパターン生成',       cat: 'effect' },
    { file: 'rimlight-visualizer.html',   label: 'リムライト方向ビジュアライザー', cat: 'effect' },
    { file: 'screen-fx.html',             label: 'スクリーンエフェクト設定',     cat: 'effect' },
    { file: 'performance-budget.html',    label: 'パフォーマンスバジェット',     cat: 'optimization' },
    { file: 'texture-optimizer.html',     label: 'テクスチャ最適化',             cat: 'optimization' },
    { file: 'outline-optimizer.html',     label: 'アウトライン最適化',           cat: 'optimization' },
    { file: 'refraction-balancer.html',   label: '屈折品質バランサー',           cat: 'optimization' },
    { file: 'liltoon-migration.html',     label: 'lilToon移行',                  cat: 'migration' },
    { file: 'batch-converter.html',       label: '一括マテリアル変換',           cat: 'migration' },
    { file: 'prefab-converter.html',      label: 'プレハブバリアント変換',       cat: 'migration' },
    { file: 'shader-variant-collector.html', label: 'シェーダーバリアント収集',  cat: 'shader' },
    { file: 'light-volumes-helper.html',  label: 'ライトボリュームヘルパー',     cat: 'shader' },
    { file: 'uv-texture-generator.html',  label: 'UVテクスチャ生成',             cat: 'shader' }
  ];

  /* ---------- Header Nav Data ---------- */
  var NAV = [
    { label: 'ホーム', href: '{root}index.html' },
    {
      label: 'パラメータ',
      children: [
        { label: '基本',           href: '{root}params/basic/main-texture.html' },
        { label: 'ライティング',   href: '{root}params/lighting/lighting-general.html' },
        { label: 'エフェクト',     href: '{root}params/effects/specular.html' },
        { label: '環境',           href: '{root}params/environment/reflection.html' },
        { label: '詳細',           href: '{root}params/advanced/normal-map.html' },
        { label: '── 一覧',       href: '{root}params/index.html' }
      ]
    },
    {
      label: 'ツール',
      children: [
        { label: 'マテリアル',     href: '{root}tools/material-validator.html' },
        { label: 'プリセット',     href: '{root}tools/preset-browser.html' },
        { label: 'エフェクト',     href: '{root}tools/shadow-wizard.html' },
        { label: '最適化',         href: '{root}tools/performance-budget.html' },
        { label: '移行',           href: '{root}tools/liltoon-migration.html' },
        { label: 'シェーダー',     href: '{root}tools/shader-variant-collector.html' },
        { label: '── 一覧',       href: '{root}tools/index.html' }
      ]
    }
  ];

  /* ---------- Utilities ---------- */
  function getRoot() {
    var path = location.pathname;
    /* count depth from Website root */
    if (path.indexOf('/params/') !== -1 || path.indexOf('/tools/') !== -1) {
      /* e.g. /params/basic/foo.html => depth 2 from website root */
      var parts = path.split('/');
      var idx = -1;
      for (var i = 0; i < parts.length; i++) {
        if (parts[i] === 'params' || parts[i] === 'tools') { idx = i; break; }
      }
      if (idx !== -1) {
        var depth = parts.length - 1 - idx; /* segments after params/tools dir */
        if (depth === 1) return '../';       /* params/index.html or tools/index.html */
        if (depth === 2) return '../../';    /* params/basic/foo.html */
      }
    }
    return '';
  }

  var ROOT = getRoot();

  function resolveHref(href) {
    return href.replace('{root}', ROOT);
  }

  /* current file path relative to Website root */
  function getCurrentRelPath() {
    var path = location.pathname;
    var parts = path.split('/');
    /* find index of params or tools */
    for (var i = 0; i < parts.length; i++) {
      if (parts[i] === 'params') return parts.slice(i).join('/');
      if (parts[i] === 'tools')  return parts.slice(i).join('/');
    }
    return parts[parts.length - 1] || 'index.html';
  }

  var CURRENT_REL = getCurrentRelPath();

  function isActive(href) {
    var resolved = resolveHref(href);
    /* strip leading ../ for comparison */
    var base = resolved.replace(/^(\.\.\/)+/, '');
    return CURRENT_REL.indexOf(base) !== -1 || CURRENT_REL === base;
  }

  /* ---------- Detect page context ---------- */
  function getPageContext() {
    /* returns { section: 'params'|'tools', category: 'basic'|..., file: 'main-texture.html' } or null */
    var parts = CURRENT_REL.split('/');
    if (parts[0] === 'params' && parts.length === 3) {
      return { section: 'params', category: parts[1], file: parts[2] };
    }
    if (parts[0] === 'tools' && parts.length === 2 && parts[1] !== 'index.html') {
      return { section: 'tools', category: null, file: parts[1] };
    }
    return null;
  }

  /* ---------- Build header ---------- */
  function buildHeader() {
    var header = document.querySelector('.site-header');
    if (!header) return;

    var inner = document.createElement('div');
    inner.className = 'header-inner';

    var logo = document.createElement('div');
    logo.className = 'site-logo';
    logo.innerHTML = '<a href="' + ROOT + 'index.html">Natane Toon Shader</a>';

    var nav = document.createElement('nav');
    nav.className = 'nav-links';

    NAV.forEach(function (item) {
      if (item.children) {
        var dd = document.createElement('div');
        dd.className = 'nav-dropdown';
        var span = document.createElement('span');
        span.textContent = item.label + ' ▾';
        dd.appendChild(span);

        var menu = document.createElement('div');
        menu.className = 'nav-dropdown-menu';
        item.children.forEach(function (child) {
          var a = document.createElement('a');
          a.href = resolveHref(child.href);
          a.textContent = child.label;
          if (isActive(child.href)) a.classList.add('active');
          menu.appendChild(a);
        });
        dd.appendChild(menu);

        if (item.children.some(function (c) { return isActive(c.href); })) {
          span.classList.add('active');
        }
        nav.appendChild(dd);
      } else {
        var a = document.createElement('a');
        a.href = resolveHref(item.href);
        a.textContent = item.label;
        if (isActive(item.href)) a.classList.add('active');
        nav.appendChild(a);
      }
    });

    /* Hamburger */
    var hamburger = document.createElement('button');
    hamburger.className = 'hamburger';
    hamburger.setAttribute('aria-label', 'メニュー');
    hamburger.innerHTML = '<span></span><span></span><span></span>';
    hamburger.addEventListener('click', function () {
      hamburger.classList.toggle('open');
      nav.classList.toggle('open');
    });

    inner.appendChild(logo);
    inner.appendChild(nav);
    inner.appendChild(hamburger);
    header.appendChild(inner);
  }

  /* ---------- Build breadcrumb ---------- */
  function buildBreadcrumb() {
    var bc = document.querySelector('.breadcrumb');
    if (!bc) return;

    var ctx = getPageContext();
    if (!ctx) return;

    var crumbs = [{ label: 'ホーム', href: ROOT + 'index.html' }];

    if (ctx.section === 'params') {
      crumbs.push({ label: 'パラメータ', href: ROOT + 'params/index.html' });
      if (ctx.category && CATEGORIES[ctx.category]) {
        crumbs.push({
          label: CATEGORIES[ctx.category].label,
          href: ROOT + 'params/' + ctx.category + '/' + PAGE_ORDER[ctx.category][0].file
        });
      }
      /* current page */
      var pages = PAGE_ORDER[ctx.category] || [];
      for (var i = 0; i < pages.length; i++) {
        if (pages[i].file === ctx.file) {
          crumbs.push({ label: pages[i].label, href: null });
          break;
        }
      }
    } else if (ctx.section === 'tools') {
      crumbs.push({ label: 'ツール', href: ROOT + 'tools/index.html' });
      for (var j = 0; j < TOOL_ORDER.length; j++) {
        if (TOOL_ORDER[j].file === ctx.file) {
          crumbs.push({ label: TOOL_ORDER[j].label, href: null });
          break;
        }
      }
    }

    crumbs.forEach(function (c, idx) {
      if (idx > 0) {
        var sep = document.createElement('span');
        sep.className = 'breadcrumb-sep';
        sep.textContent = ' > ';
        bc.appendChild(sep);
      }
      if (c.href) {
        var a = document.createElement('a');
        a.href = c.href;
        a.textContent = c.label;
        bc.appendChild(a);
      } else {
        var span = document.createElement('span');
        span.className = 'breadcrumb-current';
        span.textContent = c.label;
        bc.appendChild(span);
      }
    });
  }

  /* ---------- Build category sidebar ---------- */
  function buildCategorySidebar() {
    var sidebar = document.querySelector('.sidebar');
    if (!sidebar) return;

    var ctx = getPageContext();
    if (!ctx) {
      /* fallback to TOC for non-individual pages */
      buildTOC();
      return;
    }

    var pages = null;
    var catLabel = '';

    if (ctx.section === 'params' && ctx.category) {
      pages = PAGE_ORDER[ctx.category];
      catLabel = CATEGORIES[ctx.category] ? CATEGORIES[ctx.category].label : '';
    } else if (ctx.section === 'tools') {
      pages = TOOL_ORDER;
      catLabel = 'ツール';
    }

    if (!pages) { buildTOC(); return; }

    /* Category title */
    var title = document.createElement('div');
    title.className = 'sidebar-title';
    title.textContent = catLabel;
    sidebar.appendChild(title);

    /* Page list */
    var ul = document.createElement('ul');
    ul.className = 'category-nav';

    pages.forEach(function (p) {
      var li = document.createElement('li');
      var a = document.createElement('a');
      if (ctx.section === 'params') {
        a.href = ROOT + 'params/' + ctx.category + '/' + p.file;
      } else {
        a.href = ROOT + 'tools/' + p.file;
      }
      a.textContent = p.label;
      if (p.file === ctx.file) a.classList.add('active');
      li.appendChild(a);
      ul.appendChild(li);
    });

    sidebar.appendChild(ul);
  }

  /* ---------- Build prev/next navigation ---------- */
  function buildPageNav() {
    var nav = document.querySelector('.page-nav');
    if (!nav) return;

    var ctx = getPageContext();
    if (!ctx) return;

    var pages, basePath;
    if (ctx.section === 'params' && ctx.category) {
      pages = PAGE_ORDER[ctx.category];
      basePath = ROOT + 'params/' + ctx.category + '/';
    } else if (ctx.section === 'tools') {
      pages = TOOL_ORDER;
      basePath = ROOT + 'tools/';
    } else {
      return;
    }

    var idx = -1;
    for (var i = 0; i < pages.length; i++) {
      if (pages[i].file === ctx.file) { idx = i; break; }
    }
    if (idx === -1) return;

    if (idx > 0) {
      var prev = document.createElement('a');
      prev.className = 'page-nav-prev';
      prev.href = basePath + pages[idx - 1].file;
      prev.innerHTML = '&larr; ' + pages[idx - 1].label;
      nav.appendChild(prev);
    }

    if (idx < pages.length - 1) {
      var next = document.createElement('a');
      next.className = 'page-nav-next';
      next.href = basePath + pages[idx + 1].file;
      next.innerHTML = pages[idx + 1].label + ' &rarr;';
      nav.appendChild(next);
    }
  }

  /* ---------- Build sidebar TOC (for portal/index pages) ---------- */
  function buildTOC() {
    var sidebar = document.querySelector('.sidebar');
    if (!sidebar) return;

    var content = document.querySelector('.main-content');
    if (!content) return;

    var headings = content.querySelectorAll('h2, h3');
    if (headings.length === 0) {
      sidebar.style.display = 'none';
      var wrapper = document.querySelector('.page-wrapper');
      if (wrapper) wrapper.classList.add('no-sidebar');
      var footer = document.querySelector('.site-footer');
      if (footer) footer.style.marginLeft = '0';
      return;
    }

    var title = document.createElement('div');
    title.className = 'sidebar-title';
    title.textContent = '目次';
    sidebar.appendChild(title);

    var ul = document.createElement('ul');
    ul.className = 'toc-list';

    headings.forEach(function (h, i) {
      if (!h.id) h.id = 'section-' + i;
      var li = document.createElement('li');
      if (h.tagName === 'H3') li.classList.add('toc-h3');
      var a = document.createElement('a');
      a.href = '#' + h.id;
      a.textContent = h.textContent;
      li.appendChild(a);
      ul.appendChild(li);
    });
    sidebar.appendChild(ul);

    /* Scroll spy */
    if ('IntersectionObserver' in window) {
      var links = ul.querySelectorAll('a');
      var observer = new IntersectionObserver(
        function (entries) {
          entries.forEach(function (entry) {
            if (entry.isIntersecting) {
              links.forEach(function (l) { l.classList.remove('active'); });
              var active = ul.querySelector('a[href="#' + entry.target.id + '"]');
              if (active) active.classList.add('active');
            }
          });
        },
        { rootMargin: '-80px 0px -60% 0px', threshold: 0 }
      );
      headings.forEach(function (h) { observer.observe(h); });
    }
  }

  /* ---------- Sidebar mobile toggle ---------- */
  function buildSidebarToggle() {
    var sidebar = document.querySelector('.sidebar');
    if (!sidebar) return;

    var btn = document.createElement('button');
    btn.className = 'sidebar-toggle';
    btn.setAttribute('aria-label', '目次を開く');
    btn.textContent = '☰';
    btn.addEventListener('click', function () {
      sidebar.classList.toggle('open');
      btn.textContent = sidebar.classList.contains('open') ? '✕' : '☰';
    });
    document.body.appendChild(btn);
  }

  /* ---------- Expose data for portal pages ---------- */
  window.NataneNav = {
    PAGE_ORDER: PAGE_ORDER,
    CATEGORIES: CATEGORIES,
    TOOL_ORDER: TOOL_ORDER,
    TOOL_CATEGORIES: TOOL_CATEGORIES,
    ROOT: ROOT
  };

  /* ---------- Init ---------- */
  document.addEventListener('DOMContentLoaded', function () {
    buildHeader();
    buildBreadcrumb();
    buildCategorySidebar();
    buildPageNav();
    buildSidebarToggle();
  });
})();
