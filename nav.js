/* ============================================================
   Natane Toon Shader - Documentation Site Navigation
   natanetoon.com  v3 - Bilingual (JA/EN) navigation
   ============================================================ */

(function () {
  'use strict';

  /* ---------- Language Detection ---------- */
  var IS_EN = location.pathname.indexOf('/en/') !== -1;

  /* ---------- Site Map Data (JA) ---------- */
  var CATEGORIES_JA = {
    basic:       { label: '基本',           icon: '🎨' },
    lighting:    { label: 'ライティング',   icon: '💡' },
    effects:     { label: 'エフェクト',     icon: '✨' },
    environment: { label: '環境',           icon: '🌐' },
    advanced:    { label: '詳細',           icon: '⚙️' }
  };

  var CATEGORIES_EN = {
    basic:       { label: 'Basic',         icon: '🎨' },
    lighting:    { label: 'Lighting',      icon: '💡' },
    effects:     { label: 'Effects',       icon: '✨' },
    environment: { label: 'Environment',   icon: '🌐' },
    advanced:    { label: 'Advanced',      icon: '⚙️' }
  };

  var TOOL_CATEGORIES_JA = {
    general:      { label: '全般' },
    material:     { label: 'マテリアル' },
    preset:       { label: 'プリセット' },
    effect:       { label: 'エフェクト' },
    optimization: { label: '最適化' },
    migration:    { label: '移行' },
    shader:       { label: 'シェーダー' }
  };

  var TOOL_CATEGORIES_EN = {
    general:      { label: 'General' },
    material:     { label: 'Material' },
    preset:       { label: 'Preset' },
    effect:       { label: 'Effect' },
    optimization: { label: 'Optimization' },
    migration:    { label: 'Migration' },
    shader:       { label: 'Shader' }
  };

  /* Page order within each category */
  var PAGE_ORDER_JA = {
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

  var PAGE_ORDER_EN = {
    basic: [
      { file: 'main-texture.html',      label: 'Main Texture' },
      { file: 'color-enhancement.html',  label: 'Color Enhancement' },
      { file: 'final-blend.html',        label: 'Final Color Blend' },
      { file: 'surface-finish.html',     label: 'Surface Finish' },
      { file: 'makeup.html',             label: 'Makeup' },
      { file: 'shading.html',            label: 'Shading' }
    ],
    lighting: [
      { file: 'lighting-general.html',   label: 'Lighting General' },
      { file: 'soft-lighting.html',      label: 'Soft Lighting' },
      { file: 'backlight.html',          label: 'Backlight' },
      { file: 'light-volumes.html',      label: 'VRC Light Volumes' },
      { file: 'ltcgi.html',              label: 'LTCGI' },
      { file: 'ao.html',                 label: 'Ambient Occlusion' },
      { file: 'dithering.html',          label: 'Dithering' },
      { file: 'shadow-color.html',       label: 'Shadow Color Texture' }
    ],
    effects: [
      { file: 'specular.html',           label: 'Specular' },
      { file: 'rimlight1.html',          label: 'Rim Light 1' },
      { file: 'rimlight2.html',          label: 'Rim Light 2' },
      { file: 'rim-direction.html',      label: 'Rim Light Direction' },
      { file: 'sss.html',               label: 'SSS' },
      { file: 'matcap1.html',            label: 'MatCap 1' },
      { file: 'matcap23.html',           label: 'MatCap 2/3' },
      { file: 'glitter.html',            label: 'Glitter' },
      { file: 'waterdrip.html',          label: 'Water Drop Effect' },
      { file: 'hologram.html',           label: 'Hologram' },
      { file: 'glitch.html',             label: 'Glitch' },
      { file: 'decal.html',              label: 'Decal' },
      { file: 'outline.html',            label: 'Outline' },
      { file: 'emission.html',           label: 'Emission' },
      { file: 'dissolve.html',           label: 'Dissolve' },
      { file: 'alpha-mask.html',         label: 'Alpha Mask' },
      { file: 'hue-shift.html',          label: 'Hue Shift' },
      { file: 'audiolink.html',          label: 'AudioLink' }
    ],
    environment: [
      { file: 'reflection.html',         label: 'Reflection' },
      { file: 'iridescence.html',        label: 'Iridescence' },
      { file: 'env-rim.html',            label: 'Environmental Rim' },
      { file: 'refraction.html',         label: 'Refraction' }
    ],
    advanced: [
      { file: 'normal-map.html',         label: 'Normal Map' },
      { file: 'parallax.html',           label: 'Parallax' },
      { file: 'vat.html',               label: 'VAT' },
      { file: 'backface.html',           label: 'Backface Texture' },
      { file: 'video-texture.html',      label: 'Video Texture' },
      { file: 'distance-fade.html',      label: 'Distance Fade' },
      { file: 'vertex-animation.html',   label: 'Vertex Animation' },
      { file: 'rendering.html',          label: 'Rendering Settings' }
    ]
  };

  var TOOL_ORDER_JA = [
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

  var TOOL_ORDER_EN = [
    { file: 'dashboard.html',             label: 'Dashboard',                    cat: 'general' },
    { file: 'help.html',                  label: 'Help',                         cat: 'general' },
    { file: 'material-validator.html',    label: 'Material Validator',           cat: 'material' },
    { file: 'material-editor.html',       label: 'Material Editor',              cat: 'material' },
    { file: 'material-preview.html',      label: 'Material Preview',             cat: 'material' },
    { file: 'material-comparison.html',   label: 'Material Comparison',          cat: 'material' },
    { file: 'makeup-layer-manager.html',  label: 'Makeup Layer Manager',         cat: 'material' },
    { file: 'preset-browser.html',        label: 'Preset Browser',               cat: 'preset' },
    { file: 'color-palette.html',         label: 'Color Palette',                cat: 'preset' },
    { file: 'preset-generator.html',      label: 'Default Preset Generator',     cat: 'preset' },
    { file: 'preset-regenerator.html',    label: 'Regenerate All Presets',        cat: 'preset' },
    { file: 'shadow-wizard.html',         label: 'Shadow Adjustment Wizard',     cat: 'effect' },
    { file: 'matcap-composer.html',       label: 'MatCap Composer',              cat: 'effect' },
    { file: 'dissolve-generator.html',    label: 'Dissolve Pattern Generator',   cat: 'effect' },
    { file: 'rimlight-visualizer.html',   label: 'Rim Light Direction Visualizer', cat: 'effect' },
    { file: 'screen-fx.html',             label: 'Screen Effect Settings',       cat: 'effect' },
    { file: 'performance-budget.html',    label: 'Performance Budget',           cat: 'optimization' },
    { file: 'texture-optimizer.html',     label: 'Texture Optimizer',            cat: 'optimization' },
    { file: 'outline-optimizer.html',     label: 'Outline Optimizer',            cat: 'optimization' },
    { file: 'refraction-balancer.html',   label: 'Refraction Quality Balancer',  cat: 'optimization' },
    { file: 'liltoon-migration.html',     label: 'lilToon Migration',            cat: 'migration' },
    { file: 'batch-converter.html',       label: 'Batch Material Converter',     cat: 'migration' },
    { file: 'prefab-converter.html',      label: 'Prefab Variant Converter',     cat: 'migration' },
    { file: 'shader-variant-collector.html', label: 'Shader Variant Collector',  cat: 'shader' },
    { file: 'light-volumes-helper.html',  label: 'Light Volumes Helper',         cat: 'shader' },
    { file: 'uv-texture-generator.html',  label: 'UV Texture Generator',         cat: 'shader' }
  ];

  /* ---------- Header Nav Data ---------- */
  var NAV_JA = [
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

  var NAV_EN = [
    { label: 'Home', href: '{root}index.html' },
    {
      label: 'Parameters',
      children: [
        { label: 'Basic',         href: '{root}params/basic/main-texture.html' },
        { label: 'Lighting',      href: '{root}params/lighting/lighting-general.html' },
        { label: 'Effects',       href: '{root}params/effects/specular.html' },
        { label: 'Environment',   href: '{root}params/environment/reflection.html' },
        { label: 'Advanced',      href: '{root}params/advanced/normal-map.html' },
        { label: '── All',        href: '{root}params/index.html' }
      ]
    },
    {
      label: 'Tools',
      children: [
        { label: 'Material',      href: '{root}tools/material-validator.html' },
        { label: 'Preset',        href: '{root}tools/preset-browser.html' },
        { label: 'Effect',        href: '{root}tools/shadow-wizard.html' },
        { label: 'Optimization',  href: '{root}tools/performance-budget.html' },
        { label: 'Migration',     href: '{root}tools/liltoon-migration.html' },
        { label: 'Shader',        href: '{root}tools/shader-variant-collector.html' },
        { label: '── All',        href: '{root}tools/index.html' }
      ]
    }
  ];

  /* ---------- Select language data ---------- */
  var CATEGORIES      = IS_EN ? CATEGORIES_EN      : CATEGORIES_JA;
  var TOOL_CATEGORIES = IS_EN ? TOOL_CATEGORIES_EN  : TOOL_CATEGORIES_JA;
  var PAGE_ORDER      = IS_EN ? PAGE_ORDER_EN       : PAGE_ORDER_JA;
  var TOOL_ORDER      = IS_EN ? TOOL_ORDER_EN       : TOOL_ORDER_JA;
  var NAV             = IS_EN ? NAV_EN              : NAV_JA;

  /* ---------- Localized strings ---------- */
  var L = IS_EN ? {
    home: 'Home', params: 'Parameters', tools: 'Tools',
    toc: 'Contents', menu: 'Menu', openToc: 'Open table of contents',
    pages: ' pages'
  } : {
    home: 'ホーム', params: 'パラメータ', tools: 'ツール',
    toc: '目次', menu: 'メニュー', openToc: '目次を開く',
    pages: ' ページ'
  };

  /* ---------- Utilities ---------- */
  function getRoot() {
    var path = location.pathname;
    var depth = 0;

    /* add 1 for /en/ prefix */
    if (IS_EN) depth++;

    if (path.indexOf('/params/') !== -1 || path.indexOf('/tools/') !== -1) {
      var parts = path.split('/');
      var idx = -1;
      for (var i = 0; i < parts.length; i++) {
        if (parts[i] === 'params' || parts[i] === 'tools') { idx = i; break; }
      }
      if (idx !== -1) {
        depth += parts.length - 1 - idx;
      }
    }

    if (depth === 0) return '';
    var result = '';
    for (var i = 0; i < depth; i++) result += '../';
    return result;
  }

  var ROOT = getRoot();

  /* Build a path that includes /en/ prefix when on EN pages */
  function makePath(relPath) {
    return ROOT + (IS_EN ? 'en/' : '') + relPath;
  }

  function resolveHref(href) {
    if (IS_EN) {
      return href.replace('{root}', ROOT + 'en/');
    }
    return href.replace('{root}', ROOT);
  }

  /* current file path relative to Website root (without /en/) */
  function getCurrentRelPath() {
    var path = location.pathname;
    var parts = path.split('/');
    for (var i = 0; i < parts.length; i++) {
      if (parts[i] === 'params') return parts.slice(i).join('/');
      if (parts[i] === 'tools')  return parts.slice(i).join('/');
    }
    return parts[parts.length - 1] || 'index.html';
  }

  var CURRENT_REL = getCurrentRelPath();

  function isActive(href) {
    var resolved = resolveHref(href);
    var base = resolved.replace(/^(\.\.\/)+/, '').replace(/^en\//, '');
    return CURRENT_REL.indexOf(base) !== -1 || CURRENT_REL === base;
  }

  /* ---------- Detect page context ---------- */
  function getPageContext() {
    var parts = CURRENT_REL.split('/');
    if (parts[0] === 'params' && parts.length === 3) {
      return { section: 'params', category: parts[1], file: parts[2] };
    }
    if (parts[0] === 'tools' && parts.length === 2 && parts[1] !== 'index.html') {
      return { section: 'tools', category: null, file: parts[1] };
    }
    return null;
  }

  /* ---------- Language toggle URL ---------- */
  function getLangToggleHref() {
    if (IS_EN) {
      /* EN → JP: strip /en/ prefix, go to website root + relPath */
      return ROOT + CURRENT_REL;
    } else {
      /* JP → EN: go to website root + en/ + relPath */
      return ROOT + 'en/' + CURRENT_REL;
    }
  }

  /* ---------- Build header ---------- */
  function buildHeader() {
    var header = document.querySelector('.site-header');
    if (!header) return;

    var inner = document.createElement('div');
    inner.className = 'header-inner';

    var logo = document.createElement('div');
    logo.className = 'site-logo';
    logo.innerHTML = '<a href="' + makePath('index.html') + '">Natane Toon Shader</a>';

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

    /* Language toggle */
    var langLink = document.createElement('a');
    langLink.className = 'lang-toggle';
    langLink.textContent = IS_EN ? '日本語' : 'English';
    langLink.href = getLangToggleHref();
    nav.appendChild(langLink);

    /* Hamburger */
    var hamburger = document.createElement('button');
    hamburger.className = 'hamburger';
    hamburger.setAttribute('aria-label', L.menu);
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

    var crumbs = [{ label: L.home, href: makePath('index.html') }];

    if (ctx.section === 'params') {
      crumbs.push({ label: L.params, href: makePath('params/index.html') });
      if (ctx.category && CATEGORIES[ctx.category]) {
        crumbs.push({
          label: CATEGORIES[ctx.category].label,
          href: makePath('params/' + ctx.category + '/' + PAGE_ORDER[ctx.category][0].file)
        });
      }
      var pages = PAGE_ORDER[ctx.category] || [];
      for (var i = 0; i < pages.length; i++) {
        if (pages[i].file === ctx.file) {
          crumbs.push({ label: pages[i].label, href: null });
          break;
        }
      }
    } else if (ctx.section === 'tools') {
      crumbs.push({ label: L.tools, href: makePath('tools/index.html') });
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
      catLabel = L.tools;
    }

    if (!pages) { buildTOC(); return; }

    var title = document.createElement('div');
    title.className = 'sidebar-title';
    title.textContent = catLabel;
    sidebar.appendChild(title);

    var ul = document.createElement('ul');
    ul.className = 'category-nav';

    pages.forEach(function (p) {
      var li = document.createElement('li');
      var a = document.createElement('a');
      if (ctx.section === 'params') {
        a.href = makePath('params/' + ctx.category + '/' + p.file);
      } else {
        a.href = makePath('tools/' + p.file);
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
      basePath = makePath('params/' + ctx.category + '/');
    } else if (ctx.section === 'tools') {
      pages = TOOL_ORDER;
      basePath = makePath('tools/');
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
    title.textContent = L.toc;
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
    btn.setAttribute('aria-label', L.openToc);
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
    ROOT: ROOT,
    IS_EN: IS_EN,
    makePath: makePath,
    L: L
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
