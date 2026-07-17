/* ============================================================
   Natane Toon Shader - Home page content

   リリース時は VERSION と CONTENT 内の配列を更新する。
   各項目に日本語・英語をまとめ、表示順と番号は描画時に自動生成する。
   ============================================================ */

(function () {
  'use strict';

  var VERSION = 'v1.6.0';

  var CONTENT = {
    featured: [
      {
        label: 'CHARACTER LOOK',
        title: { ja: '顔直交投影', en: 'Face Ortho Projection' },
        description: {
          ja: 'カメラ距離やFOVに左右されず、顔のプロポーションを理想的に保ちます。VR用の個別強度、マスク、アウトライン追従にも対応。',
          en: 'Keep ideal face proportions at any camera distance or FOV, with separate VR strength, mask support, and outline tracking.'
        },
        href: 'params/advanced/face-ortho.html',
        wide: true
      },
      {
        label: 'TRANSLUCENT FX',
        title: { ja: 'ゴーストバリアント', en: 'Ghost Variant' },
        description: {
          ja: '重なりによる二重ブレンドを抑え、破綻しにくい半透明表現を実現します。',
          en: 'Reliable translucent rendering that avoids double-blend artifacts where body parts overlap.'
        },
        href: 'params/effects/ghost.html'
      },
      {
        label: 'MOTION & EFFECT',
        title: { ja: 'GPUパーティクル', en: 'GPU Particles' },
        description: {
          ja: 'アバターセーフな頂点アニメ方式。AudioLinkや疑似流体表現にも対応します。',
          en: 'Avatar-safe vertex animation with AudioLink and math-based fake fluid effects.'
        },
        href: 'tools/gpu-particle-mesh.html'
      }
    ],
    updates: [
      { title: { ja: '鏡・カメラ写り分けテクスチャ', en: 'Mirror / Camera Alt Texture' }, href: 'params/advanced/mirror-alt-texture.html', status: 'VIEW' },
      { title: { ja: '目のセットアップツール', en: 'Eye Setup Tool' }, href: 'tools/eye-setup.html', status: 'VIEW' },
      { title: { ja: 'Unity 6 URP対応', en: 'Unity 6 URP Support' }, status: 'NEW' },
      { title: { ja: 'VRCFallback対応', en: 'VRCFallback Support' }, status: 'NEW' },
      { title: { ja: 'ステンシルプリセット', en: 'Stencil Presets' }, href: 'tools/stencil-presets.html', status: 'VIEW' },
      { title: { ja: 'ビルド時自動最適化', en: 'Automatic Build Optimization' }, href: 'tools/build-optimization.html', status: 'VIEW' },
      { title: { ja: 'ライティング品質修正', en: 'Lighting Quality Fixes' }, status: 'UPDATE' }
    ],
    docs: [
      {
        title: { ja: '基本', en: 'Basic' },
        description: { ja: 'メインテクスチャ、メイクアップ、シェーディング', en: 'Main texture, makeup, shading' },
        href: 'params/basic/main-texture.html'
      },
      {
        title: { ja: 'ライティング', en: 'Lighting' },
        description: { ja: '光源制御、バックライト、VRC Light Volumes', en: 'Light control, backlight, VRC Light Volumes' },
        href: 'params/lighting/lighting-general.html'
      },
      {
        title: { ja: 'エフェクト', en: 'Effects' },
        description: { ja: 'MatCap、アウトライン、ホログラム、グリッチ', en: 'MatCap, outline, hologram, glitch' },
        href: 'params/effects/specular.html'
      },
      {
        title: { ja: '環境', en: 'Environment' },
        description: { ja: 'リフレクション、虹色効果、屈折', en: 'Reflection, iridescence, refraction' },
        href: 'params/environment/reflection.html'
      },
      {
        title: { ja: '詳細', en: 'Advanced' },
        description: { ja: 'AudioLink、VAT、Distance Fade', en: 'AudioLink, VAT, Distance Fade' },
        href: 'params/advanced/normal-map.html'
      },
      {
        title: { ja: 'エディタツール', en: 'Editor Tools' },
        description: { ja: '27以上の制作支援ツール', en: '27+ artist-friendly production tools' },
        href: 'tools/index.html',
        accent: true
      }
    ],
    capabilities: [
      'CEL SHADING',
      'AUDIOLINK',
      'MULTI MATCAP',
      'HOLOGRAM / GLITCH',
      'SSS',
      'DECAL / DISSOLVE',
      'LIGHT VOLUMES / LTCGI',
      '59 PRESETS',
      'JAPANESE UI'
    ]
  };

  function localized(value, language) {
    return typeof value === 'string' ? value : value[language];
  }

  function numberLabel(index) {
    return String(index + 1).padStart(2, '0');
  }

  function element(tagName, className, text) {
    var node = document.createElement(tagName);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
  }

  function renderFeatured(container, items, language) {
    items.forEach(function (item, index) {
      var card = element('a', 'feature-card' + (item.wide ? ' feature-card-wide' : ''));
      card.href = item.href;

      var content = element('div');
      content.appendChild(element('p', 'card-label', item.label));
      content.appendChild(element('h3', '', localized(item.title, language)));
      content.appendChild(element('p', '', localized(item.description, language)));

      var arrow = element('span', 'card-arrow', '↗');
      arrow.setAttribute('aria-hidden', 'true');

      card.appendChild(element('span', 'feature-number', numberLabel(index)));
      card.appendChild(content);
      card.appendChild(arrow);
      container.appendChild(card);
    });
  }

  function renderUpdates(container, items, numberOffset, language) {
    items.forEach(function (item, index) {
      var row = element(item.href ? 'a' : 'div');
      if (item.href) row.href = item.href;
      row.appendChild(element('span', '', numberLabel(numberOffset + index)));
      row.appendChild(element('strong', '', localized(item.title, language)));
      row.appendChild(element('small', '', item.status));
      container.appendChild(row);
    });
  }

  function renderDocs(container, items, language) {
    items.forEach(function (item, index) {
      var card = element('a', 'documentation-card' + (item.accent ? ' documentation-card-accent' : ''));
      card.href = item.href;
      card.appendChild(element('span', '', numberLabel(index)));
      card.appendChild(element('h3', '', localized(item.title, language)));
      card.appendChild(element('p', '', localized(item.description, language)));
      container.appendChild(card);
    });
  }

  function renderCapabilities(container, items) {
    items.forEach(function (item) {
      container.appendChild(element('span', '', item));
    });
  }

  function setupMotion() {
    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var revealItems = document.querySelectorAll(
      '.home-facts, .section-heading, .feature-card, .update-list > *, ' +
      '.documentation-card, .capability-list > span, .home-cta'
    );

    document.body.classList.add('home-motion-ready');
    window.requestAnimationFrame(function () {
      document.body.classList.add('home-motion-active');
    });

    revealItems.forEach(function (node, index) {
      node.classList.add('reveal-item');
      node.style.setProperty('--reveal-delay', String((index % 4) * 45) + 'ms');
    });

    if (reduceMotion || !('IntersectionObserver' in window)) {
      revealItems.forEach(function (node) { node.classList.add('is-visible'); });
      return;
    }

    var observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (!entry.isIntersecting) return;
        entry.target.classList.add('is-visible');
        observer.unobserve(entry.target);
      });
    }, { rootMargin: '0px 0px -8% 0px', threshold: 0.08 });

    revealItems.forEach(function (node) { observer.observe(node); });
  }

  function renderHome() {
    var language = document.documentElement.lang === 'en' ? 'en' : 'ja';
    var featured = document.querySelector('[data-home-featured]');
    var updates = document.querySelector('[data-home-updates]');
    var docs = document.querySelector('[data-home-docs]');
    var capabilities = document.querySelector('[data-home-capabilities]');

    document.querySelectorAll('[data-release-version]').forEach(function (node) {
      node.textContent = VERSION;
    });

    if (featured) renderFeatured(featured, CONTENT.featured, language);
    if (updates) renderUpdates(updates, CONTENT.updates, CONTENT.featured.length, language);
    if (docs) renderDocs(docs, CONTENT.docs, language);
    if (capabilities) renderCapabilities(capabilities, CONTENT.capabilities);
    setupMotion();
  }

  window.NataneHome = {
    version: VERSION,
    content: CONTENT
  };

  document.addEventListener('DOMContentLoaded', renderHome);
})();
