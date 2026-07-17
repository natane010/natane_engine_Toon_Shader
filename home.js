/* ============================================================
   Natane Toon Shader - Home page content

   リリース時は VERSION と CONTENT 内の配列を更新する。
   各項目に日本語・英語をまとめ、表示順と番号は描画時に自動生成する。
   ============================================================ */

(function () {
  'use strict';

  var VERSION = 'v1.6.0';

  /* Feature preview images (optional): drop a screenshot at
     assets/previews/<preview>.jpg and it replaces the styled placeholder
     automatically. Recommended size ~ 800x480, dark background. */
  var PREVIEW_DIR = 'assets/previews/';

  var CONTENT = {
    featured: [
      {
        label: 'CHARACTER LOOK',
        preview: 'face-ortho',
        title: { ja: '顔直交投影', en: 'Face Ortho Projection' },
        description: {
          ja: 'カメラを近づけても遠ざけても、顔のパーツが歪まず理想の輪郭をキープ。自撮りや至近距離でも「盛れた顔」のまま。VR用の個別強度・マスク・アウトライン追従つき。',
          en: 'Faces keep their ideal shape whether the camera is close or far — no more distorted features in selfies or close-ups. Includes separate VR strength, masking, and outline tracking.'
        },
        href: 'params/advanced/face-ortho.html',
        wide: true
      },
      {
        label: 'TRANSLUCENT FX',
        preview: 'ghost',
        title: { ja: 'ゴーストバリアント', en: 'Ghost Variant' },
        description: {
          ja: '半透明の体や服が重なっても、濃く沈んだり縁がチラつかない。破綻しない透け表現でお化け・幽霊・霊体アバターが綺麗に見えます。',
          en: 'Overlapping transparent parts no longer darken or flicker at the edges — clean see-through rendering for ghost and spirit avatars.'
        },
        href: 'params/effects/ghost.html'
      },
      {
        label: 'MOTION & EFFECT',
        preview: 'gpu-particles',
        title: { ja: 'GPUパーティクル', en: 'GPU Particles' },
        description: {
          ja: 'スクリプト無しで動くアバターセーフな粒子演出。AudioLinkで音に反応させたり、疑似流体で水・炎のような流れも作れます。',
          en: 'Script-free, avatar-safe particle motion — react to music with AudioLink or fake fluid-like water and fire flows.'
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

  function assetRoot() {
    return document.documentElement.lang === 'en' ? '../' : '';
  }

  function buildPreview(item, language) {
    var preview = element('div', 'feature-preview');
    preview.setAttribute('aria-hidden', 'true');
    if (item.preview) {
      var img = element('img');
      img.alt = '';
      img.loading = 'lazy';
      img.src = assetRoot() + PREVIEW_DIR + item.preview + '.jpg';
      /* Fall back to the styled placeholder if the screenshot is not present yet. */
      img.addEventListener('error', function () { img.remove(); });
      preview.appendChild(img);
    }
    preview.appendChild(element('span', 'preview-tag', 'PREVIEW'));
    preview.appendChild(element('em', 'preview-name', localized(item.title, language)));
    return preview;
  }

  function renderFeatured(container, items, language) {
    items.forEach(function (item, index) {
      var card = element('a', 'feature-card' + (item.wide ? ' feature-card-wide' : ''));
      card.href = item.href;

      card.appendChild(buildPreview(item, language));

      var content = element('div', 'feature-text');
      var head = element('div', 'feature-text-head');
      head.appendChild(element('p', 'card-label', item.label));
      head.appendChild(element('span', 'feature-number', numberLabel(index)));
      content.appendChild(head);
      content.appendChild(element('h3', '', localized(item.title, language)));
      content.appendChild(element('p', '', localized(item.description, language)));

      var cta = element('span', 'feature-cta', language === 'en' ? 'Learn more' : '詳しく見る');
      cta.appendChild(element('span', 'card-arrow', ' ↗'));
      content.appendChild(cta);

      card.appendChild(content);
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
