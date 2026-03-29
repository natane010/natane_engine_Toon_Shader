using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    using static NataneToonLocalization;

    /// <summary>
    /// Brush mode for mask texture painting.
    /// マスクテクスチャペイント用ブラシモード
    /// </summary>
    internal enum BrushMode { Paint, Erase, Smooth, EraseAlpha }

    /// <summary>
    /// Settings for the mask texture brush tool.
    /// マスクテクスチャブラシツールの設定
    /// </summary>
    [System.Serializable]
    internal class BrushSettings
    {
        public float size = 20f;
        public float hardness = 0.8f;
        public float opacity = 1f;
        public float strength = 1f;
        public Color paintColor = Color.white;
        public Color backgroundColor = Color.black;
        public bool colorMode = false;  // true=カラー, false=グレースケール互換
        public float paintAlpha = 1f;
        public bool eraseRgb = true;
        public float alphaEpsilon = 0.001f;
        public BrushMode mode = BrushMode.Paint;
        public BrushStabilizerSettings stabilizer = new BrushStabilizerSettings();
        public bool pressureOpacityEnabled = true;
        public bool pressureSizeEnabled = false;
        public AnimationCurve pressureOpacityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve pressureSizeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        // Pressure → Hardness / 筆圧→硬さ
        public bool pressureHardnessEnabled = false;
        public AnimationCurve pressureHardnessCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        // Pressure → Density (stamp spacing) / 筆圧→濃度（スタンプ間隔制御）
        public bool pressureDensityEnabled = false;
        public AnimationCurve pressureDensityCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);

        // Pressure → Color Mix (two-color blend) / 筆圧→色混合（2色ブレンド）
        public bool pressureColorMixEnabled = false;
        public Color pressureColorA = Color.white;
        public Color pressureColorB = Color.black;
        public AnimationCurve pressureColorMixCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        // Entry/Exit Taper (pixel distances) / 入り抜き（ピクセル距離）
        public bool entryExitEnabled = false;
        public float entryLength = 20f;
        public float exitLength = 20f;
        public AnimationCurve entryCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve exitCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        // Pressure Smoothing / 筆圧スムージング
        public bool pressureSmoothingEnabled = false;
        public float pressureSmoothingStrength = 0.5f;

        // Mouse Speed → Pressure Simulation / マウス速度→筆圧シミュレーション
        public bool mouseSpeedPressureEnabled = false;
        public float mouseSpeedMin = 20f;
        public float mouseSpeedMax = 500f;
        public AnimationCurve mouseSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        // Wet brush mixing / ウェットブラシ混色
        public bool wetBrushEnabled = false;
        public float wetness = 0.5f;

        // Minimum size ratio at zero pressure / ゼロ筆圧時の最小サイズ比率
        public float pressureSizeMin = 0f;

        // Velocity → Size modulation / 速度→サイズ変調
        public bool velocitySizeEnabled = false;
        public float velocitySizeInfluence = 0.3f;  // 速度のサイズへの影響度 (0=影響なし, 1=最大)

        // Pressure dead zone / 筆圧デッドゾーン
        public float pressureDeadZone = 0f;

        // Texture brush / テクスチャブラシ
        public TextureBrushSettings textureBrush = new TextureBrushSettings();
    }

    internal readonly struct BrushStrokeCommit
    {
        public BrushStrokeCommit(Color[] beforePixels, Color[] afterPixels, Vector2 lastPixelPosition)
        {
            BeforePixels = beforePixels;
            AfterPixels = afterPixels;
            LastPixelPosition = lastPixelPosition;
        }

        public Color[] BeforePixels { get; }
        public Color[] AfterPixels { get; }
        public Vector2 LastPixelPosition { get; }
        public bool HasValue => BeforePixels != null && AfterPixels != null;
    }

    /// <summary>
    /// UV-space brush for painting directly onto mask textures.
    /// マスクテクスチャに直接ペイントするためのUV空間ブラシ
    /// </summary>
    internal class MaskTextureBrush
    {
        private BrushSettings settings;
        private Vector2 lastStrokePosition;
        private bool isStroking;
        private Color[] undoSnapshot;

        // Stroke state for pressure filter / 筆圧フィルタ用ストローク状態
        private BrushPressureFilter pressureFilter;
        private float strokeAccumulatedDistance;
        private float strokeRemainder; // サブピクセル移動の残余距離アキュムレータ
        private float lastStrokeTime;
        private float lastStrokePressure = 1f;  // Previous frame's pressure for interpolation

        // Catmull-Rom spline interpolation / Catmull-Romスプライン補間用バッファ
        private Vector2[] splinePoints = new Vector2[4]; // 直近4点のリングバッファ
        private int splinePointCount;

        // Stroke accumulation buffer for smooth strokes (Clip Studio style)
        // スムーズストローク用累積バッファ（クリスタ方式）
        private float[] strokeAlphaBuffer;    // max-alpha per pixel during stroke
        private Color[] strokeColorBuffer;    // brush color per pixel during stroke
        private Color[] canvasSnapshot;       // canvas state at stroke start (for live compositing)
        private int strokeBufferWidth;
        private int strokeBufferHeight;

        // Dirty rect for efficient compositing / 効率的な合成のためのダーティ矩形
        private int dirtyMinX, dirtyMinY, dirtyMaxX, dirtyMaxY;
        private bool hasDirtyRect;

        public MaskTextureBrush(BrushSettings settings)
        {
            this.settings = settings;
            this.pressureFilter = new BrushPressureFilter();
        }

        /// <summary>Pressure filter instance for this brush.</summary>
        public BrushPressureFilter PressureFilter => pressureFilter;

        /// <summary>
        /// Calculate brush falloff based on distance, radius, and hardness.
        /// 距離・半径・硬さに基づくブラシフォールオフを計算
        /// </summary>
        public static float GetHardnessInnerRatio(float hardness)
        {
            hardness = Mathf.Clamp01(hardness);
            if (hardness <= 0.001f)
                return 0f;
            if (hardness >= 0.999f)
                return 1f;

            // Use a perceptual remap so slider changes produce clearer edge differences.
            return hardness * hardness;
        }

        public static float BrushFalloff(float distance, float radius, float hardness)
        {
            float t = distance / radius;
            if (t > 1f) return 0f;
            float inner = GetHardnessInnerRatio(hardness);
            if (t < inner) return 1f;
            float s = (t - inner) / (1f - inner + 0.0001f);
            return 1f - s * s * (3f - 2f * s);
        }

        /// <summary>
        /// Compute squared distance from point to line segment (for capsule stamps).
        /// 点から線分への二乗距離を計算（カプセルスタンプ用）
        /// </summary>
        private static float DistanceToSegmentSq(float px, float py, Vector2 a, Vector2 b)
        {
            float abx = b.x - a.x;
            float aby = b.y - a.y;
            float lengthSq = abx * abx + aby * aby;

            float t;
            if (lengthSq < 0.0001f)
            {
                // a and b are the same point
                t = 0f;
            }
            else
            {
                t = ((px - a.x) * abx + (py - a.y) * aby) / lengthSq;
                t = Mathf.Clamp01(t);
            }

            float closestX = a.x + t * abx;
            float closestY = a.y + t * aby;
            float dx = px - closestX;
            float dy = py - closestY;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Optimized falloff using squared distance to avoid sqrt in inner loop.
        /// sqrtを回避する二乗距離版の最適化フォールオフ
        /// </summary>
        private static float BrushFalloffSq(float distSq, float radiusSq, float radius, float hardness)
        {
            if (distSq > radiusSq) return 0f;
            // Only compute sqrt when pixel is within radius / 半径内ピクセルのみsqrtを計算
            float t = Mathf.Sqrt(distSq) / radius;
            float inner = GetHardnessInnerRatio(hardness);
            if (t < inner) return 1f;
            float s = (t - inner) / (1f - inner + 0.0001f);
            return 1f - s * s * (3f - 2f * s);
        }

        public static Vector2 WrapPixelPosition(Vector2 pixelPos, int width, int height)
        {
            return new Vector2(
                Mathf.Repeat(pixelPos.x, Mathf.Max(1, width)),
                Mathf.Repeat(pixelPos.y, Mathf.Max(1, height)));
        }

        private static int RepeatIndex(int value, int length)
        {
            if (length <= 0)
                return 0;

            int result = value % length;
            return result < 0 ? result + length : result;
        }

        /// <summary>
        /// Start a new brush stroke, saving undo snapshot.
        /// 新しいブラシストロークを開始し、アンドゥスナップショットを保存
        /// </summary>
        public void StartStroke(Vector2 position, Color[] pixels)
        {
            StartStroke(position, pixels, 0, 0, false);
        }

        public void StartStroke(
            Vector2 position,
            Color[] pixels,
            int width,
            int height,
            bool lockTransparentPixels,
            float pressure = 1f,
            bool wrapCoordinates = false)
        {
            isStroking = true;
            lastStrokePosition = position;
            strokeAccumulatedDistance = 0f;
            strokeRemainder = 0f;
            lastStrokeTime = Time.realtimeSinceStartup;
            lastStrokePressure = pressure;
            pressureFilter.Reset();

            // Catmull-Romスプラインバッファを初期化
            splinePointCount = 1;
            splinePoints[0] = position;
            if (pixels != null)
            {
                undoSnapshot = new Color[pixels.Length];
                System.Array.Copy(pixels, undoSnapshot, pixels.Length);
            }

            // Initialize stroke accumulation buffers
            // ストローク累積バッファを初期化
            strokeBufferWidth = width;
            strokeBufferHeight = height;
            int totalPixels = width * height;
            if (totalPixels > 0)
            {
                if (strokeAlphaBuffer == null || strokeAlphaBuffer.Length != totalPixels)
                {
                    strokeAlphaBuffer = new float[totalPixels];
                    strokeColorBuffer = new Color[totalPixels];
                }
                else
                {
                    System.Array.Clear(strokeAlphaBuffer, 0, totalPixels);
                    System.Array.Clear(strokeColorBuffer, 0, totalPixels);
                }

                // Save canvas snapshot for live compositing
                // ライブ合成用にキャンバススナップショットを保存
                if (canvasSnapshot == null || canvasSnapshot.Length != totalPixels)
                    canvasSnapshot = new Color[totalPixels];
                System.Array.Copy(pixels, canvasSnapshot, totalPixels);
            }

            hasDirtyRect = false;
            ExpandDirtyRect(position, settings.size, width, height);
            ApplyStamp(position, pixels, settings, width, height, lockTransparentPixels, pressure, wrapCoordinates,
                strokeAlphaBuffer, strokeColorBuffer, canvasSnapshot, 1f);

            // Live preview: composite stroke buffer onto canvas
            // ライブプレビュー: ストロークバッファをキャンバスに合成
            CompositeStrokeToCanvas(pixels, width, height);
        }

        /// <summary>
        /// End the current stroke and return the undo snapshot.
        /// 現在のストロークを終了し、アンドゥスナップショットを返す
        /// </summary>
        public Color[] EndStroke()
        {
            isStroking = false;
            // Final compositing is already done by the last CompositeStrokeToCanvas call
            // Clean up stroke buffers (keep arrays allocated for reuse)
            // 最終合成は最後のCompositeStrokeToCanvas呼び出しで完了済み
            // ストロークバッファはクリーンアップ（配列は再利用のため保持）
            canvasSnapshot = null;  // Release reference but keep strokeAlpha/Color for reuse
            var snapshot = undoSnapshot;
            undoSnapshot = null;
            return snapshot;
        }

        /// <summary>
        /// Interpolate stamps from the last position to the new position.
        /// 最後の位置から新しい位置までスタンプを補間
        /// </summary>
        public void StrokeToPosition(Vector2 newPosition, Color[] pixels, int width, int height)
        {
            StrokeToPosition(newPosition, pixels, width, height, false);
        }

        public void StrokeToPosition(
            Vector2 newPosition,
            Color[] pixels,
            int width,
            int height,
            bool lockTransparentPixels,
            float pressure = 1f,
            bool wrapCoordinates = false)
        {
            if (!isStroking) return;

            // Add new point to spline buffer
            // スプラインバッファに新しい点を追加
            AddSplinePoint(newPosition);

            // Clip Studio compatible spacing: 7% of brush size
            // クリスタ互換スペーシング: ブラシサイズの7%
            float baseSpacing = Mathf.Max(settings.size * 0.07f, 0.5f);
            float densityFactor = 1f;
            if (settings.pressureDensityEnabled && pressure > 0.0001f)
            {
                float densityEval = Mathf.Clamp(settings.pressureDensityCurve.Evaluate(pressure), 0.1f, 1f);
                densityFactor = 1f / densityEval;
            }
            float spacing = Mathf.Max(baseSpacing * densityFactor, 0.5f);

            Vector2 delta = newPosition - lastStrokePosition;
            float distance = delta.magnitude;

            if (distance <= 0f)
            {
                lastStrokePosition = newPosition;
                return;
            }

            // Calculate velocity and track stroke speed
            // 速度を計算しストローク速度を追跡
            float currentTime = Time.realtimeSinceStartup;
            float frameDeltaTime = Mathf.Max(currentTime - lastStrokeTime, 0.001f);
            lastStrokeTime = currentTime;
            pressureFilter.UpdateStrokeSpeed(distance, frameDeltaTime);

            float velocityFactor = 1f;
            if (settings.velocitySizeEnabled)
            {
                float speed = distance / frameDeltaTime;
                float normalizedSpeed = Mathf.Clamp01((speed - 50f) / (800f - 50f));
                velocityFactor = 1f - normalizedSpeed;
            }

            // Use Catmull-Rom spline interpolation when 4+ points available (Clip Studio style)
            // 4点以上でCatmull-Romスプライン補間を使用（クリスタ方式）
            bool useSpline = splinePointCount >= 4;

            if (useSpline)
            {
                // Interpolate along the spline segment between splinePoints[1] and splinePoints[2]
                // splinePoints[1]→splinePoints[2]のスプラインセグメントに沿って補間
                Vector2 p0 = splinePoints[0], p1 = splinePoints[1], p2 = splinePoints[2], p3 = splinePoints[3];
                float segmentLength = EstimateSplineLength(p0, p1, p2, p3, 16);

                if (segmentLength > 0.001f)
                {
                    float totalDistance = strokeRemainder + segmentLength;
                    float walked = 0f;
                    Vector2 prevStampPos = lastStrokePosition;

                    while (totalDistance >= spacing)
                    {
                        float stepInSegment = (walked == 0f) ? (spacing - strokeRemainder) : spacing;
                        walked += stepInSegment;
                        totalDistance -= spacing;

                        strokeAccumulatedDistance += spacing;
                        pressureFilter.AddStrokeDistance(spacing);

                        // Evaluate spline at normalized position
                        float t = Mathf.Clamp01(walked / segmentLength);
                        Vector2 stampPos = CatmullRom(p0, p1, p2, p3, t);

                        // Interpolate pressure between previous and current frame
                        // 前フレームと今フレーム間で筆圧を補間（はらい対応）
                        float stampPressure = Mathf.Lerp(lastStrokePressure, pressure, t);

                        ExpandDirtyRect(prevStampPos, settings.size, width, height);
                        ExpandDirtyRect(stampPos, settings.size, width, height);
                        ApplyStamp(stampPos, pixels, settings, width, height, lockTransparentPixels, stampPressure, wrapCoordinates,
                            strokeAlphaBuffer, strokeColorBuffer, canvasSnapshot, velocityFactor, prevStampPos);
                        prevStampPos = stampPos;
                    }

                    strokeRemainder = totalDistance;
                }
            }
            else
            {
                // Fallback: linear interpolation for first few points
                // フォールバック: 最初の数ポイントは線形補間
                Vector2 direction = delta / distance;
                float totalDistance = strokeRemainder + distance;
                float walked = 0f;

                Vector2 prevStampPos = lastStrokePosition;
                while (totalDistance >= spacing)
                {
                    float stepInSegment = (walked == 0f) ? (spacing - strokeRemainder) : spacing;
                    walked += stepInSegment;
                    totalDistance -= spacing;

                    strokeAccumulatedDistance += spacing;
                    pressureFilter.AddStrokeDistance(spacing);

                    Vector2 stampPos = lastStrokePosition + direction * walked;

                    // Interpolate pressure along segment for smooth trailing
                    // セグメントに沿って筆圧を補間（滑らかなはらい対応）
                    float stampPressure = Mathf.Lerp(lastStrokePressure, pressure, Mathf.Clamp01(walked / distance));

                    ExpandDirtyRect(prevStampPos, settings.size, width, height);
                    ExpandDirtyRect(stampPos, settings.size, width, height);
                    ApplyStamp(stampPos, pixels, settings, width, height, lockTransparentPixels, stampPressure, wrapCoordinates,
                        strokeAlphaBuffer, strokeColorBuffer, canvasSnapshot, velocityFactor, prevStampPos);
                    prevStampPos = stampPos;
                }

                strokeRemainder = totalDistance;
            }

            lastStrokePosition = newPosition;

            // Save pressure for next frame's interpolation
            // 次フレームの補間用に筆圧を保存
            lastStrokePressure = pressure;

            // Live preview: composite stroke buffer onto canvas
            // ライブプレビュー: ストロークバッファをキャンバスに合成
            CompositeStrokeToCanvas(pixels, width, height);
        }

        /// <summary>
        /// Add a new point to the Catmull-Rom spline buffer, shifting old points.
        /// Catmull-Romスプラインバッファに新しい点を追加し、古い点をシフト
        /// </summary>
        private void AddSplinePoint(Vector2 point)
        {
            if (splinePointCount < 4)
            {
                splinePoints[splinePointCount] = point;
                splinePointCount++;
            }
            else
            {
                // リングバッファ: 古い点をシフトして最新の点を末尾に追加
                splinePoints[0] = splinePoints[1];
                splinePoints[1] = splinePoints[2];
                splinePoints[2] = splinePoints[3];
                splinePoints[3] = point;
            }
        }

        /// <summary>
        /// Evaluate Catmull-Rom spline at parameter t (0-1) between p1 and p2.
        /// p0, p1, p2, p3 are the four control points.
        /// Catmull-Romスプラインをパラメータt(0-1)でp1とp2の間を評価
        /// </summary>
        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        /// <summary>
        /// Estimate arc length of Catmull-Rom segment by sampling N points.
        /// N点サンプリングによるCatmull-Romセグメントの弧長推定
        /// </summary>
        private static float EstimateSplineLength(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int samples = 8)
        {
            float length = 0f;
            Vector2 prev = p1;
            for (int i = 1; i <= samples; i++)
            {
                float t = (float)i / samples;
                Vector2 current = CatmullRom(p0, p1, p2, p3, t);
                length += Vector2.Distance(prev, current);
                prev = current;
            }
            return length;
        }

        /// <summary>
        /// Expand the dirty rect to include a stamp's bounding box.
        /// ダーティ矩形をスタンプのバウンディングボックスを含むように拡張
        /// </summary>
        private void ExpandDirtyRect(Vector2 stampCenter, float radius, int width, int height)
        {
            int sMinX = Mathf.Max(0, Mathf.FloorToInt(stampCenter.x - radius));
            int sMinY = Mathf.Max(0, Mathf.FloorToInt(stampCenter.y - radius));
            int sMaxX = Mathf.Min(width - 1, Mathf.CeilToInt(stampCenter.x + radius));
            int sMaxY = Mathf.Min(height - 1, Mathf.CeilToInt(stampCenter.y + radius));

            if (!hasDirtyRect)
            {
                dirtyMinX = sMinX;
                dirtyMinY = sMinY;
                dirtyMaxX = sMaxX;
                dirtyMaxY = sMaxY;
                hasDirtyRect = true;
            }
            else
            {
                dirtyMinX = Mathf.Min(dirtyMinX, sMinX);
                dirtyMinY = Mathf.Min(dirtyMinY, sMinY);
                dirtyMaxX = Mathf.Max(dirtyMaxX, sMaxX);
                dirtyMaxY = Mathf.Max(dirtyMaxY, sMaxY);
            }
        }

        public bool IsStroking => isStroking;
        public Vector2 LastStrokePosition => lastStrokePosition;

        public void PaintStraightLine(
            Vector2 startPosition,
            Vector2 endPosition,
            Color[] pixels,
            int width,
            int height,
            bool lockTransparentPixels,
            float pressure = 1f,
            bool wrapCoordinates = false)
        {
            float distance = Vector2.Distance(startPosition, endPosition);
            if (distance <= 0.001f)
            {
                ApplyStamp(endPosition, pixels, settings, width, height, lockTransparentPixels, pressure, wrapCoordinates);
                return;
            }

            float spacing = Mathf.Max(settings.size * 0.07f, 0.5f);
            Vector2 direction = (endPosition - startPosition).normalized;

            for (float traveled = 0f; traveled <= distance; traveled += spacing)
            {
                ApplyStamp(startPosition + direction * traveled, pixels, settings, width, height, lockTransparentPixels, pressure, wrapCoordinates);
            }

            ApplyStamp(endPosition, pixels, settings, width, height, lockTransparentPixels, pressure, wrapCoordinates);
        }

        /// <summary>
        /// Apply a single brush stamp at the given pixel position.
        /// 指定ピクセル位置にブラシスタンプを適用
        /// </summary>
        private static void ApplyStamp(
            Vector2 center,
            Color[] pixels,
            BrushSettings settings,
            int width,
            int height,
            bool lockTransparentPixels,
            float pressure = 1f,
            bool wrapCoordinates = false,
            float[] strokeAlpha = null,
            Color[] strokeColor = null,
            Color[] canvasSnap = null,
            float velocityFactor = 1f,
            Vector2? prevCenter = null)
        {
            if (pixels == null || width <= 0 || height <= 0) return;

            // Determine if we use stroke buffer path
            // ストロークバッファパスを使用するか判定
            bool useStrokeBuffer = strokeAlpha != null && strokeColor != null && canvasSnap != null;

            // Pressure → Size with minimum size floor / 筆圧→サイズ（最小サイズフロア付き）
            float radius;
            if (settings.pressureSizeEnabled && pressure > 0.0001f)
            {
                float sizeT = Mathf.Clamp01(settings.pressureSizeCurve.Evaluate(pressure));
                radius = settings.size * Mathf.Lerp(settings.pressureSizeMin, 1f, sizeT);
            }
            else
            {
                radius = settings.size;
            }

            // Velocity → Size modulation / 速度→サイズ変調
            if (settings.velocitySizeEnabled && velocityFactor < 1f)
                radius *= Mathf.Lerp(velocityFactor, 1f, 1f - settings.velocitySizeInfluence);

            // Enforce minimum radius to prevent sub-pixel strokes
            // サブピクセルストロークを防止するための最小半径保証
            radius = Mathf.Max(radius, 0.5f);

            // Pressure → Hardness / 筆圧→硬さ
            float effHardness = settings.hardness;
            if (settings.pressureHardnessEnabled && pressure > 0.0001f)
                effHardness = settings.hardness * Mathf.Clamp01(settings.pressureHardnessCurve.Evaluate(pressure));

            // Pressure → Color Mix / 筆圧→色混合
            Color mixedColor = Color.clear;
            bool useColorMix = settings.pressureColorMixEnabled && settings.mode == BrushMode.Paint;
            if (useColorMix)
            {
                float mixT = Mathf.Clamp01(settings.pressureColorMixCurve.Evaluate(pressure));
                mixedColor = Color.Lerp(settings.pressureColorA, settings.pressureColorB, mixT);
            }

            int minX, maxX, minY, maxY;
            if (prevCenter.HasValue)
            {
                // Capsule bounding box: union of both stamp circles
                // カプセルバウンディングボックス: 両スタンプ円の和
                Vector2 pc = prevCenter.Value;
                minX = Mathf.FloorToInt(Mathf.Min(center.x, pc.x) - radius);
                maxX = Mathf.CeilToInt(Mathf.Max(center.x, pc.x) + radius);
                minY = Mathf.FloorToInt(Mathf.Min(center.y, pc.y) - radius);
                maxY = Mathf.CeilToInt(Mathf.Max(center.y, pc.y) + radius);
            }
            else
            {
                minX = Mathf.FloorToInt(center.x - radius);
                maxX = Mathf.CeilToInt(center.x + radius);
                minY = Mathf.FloorToInt(center.y - radius);
                maxY = Mathf.CeilToInt(center.y + radius);
            }

            if (!wrapCoordinates)
            {
                minX = Mathf.Max(0, minX);
                maxX = Mathf.Min(width - 1, maxX);
                minY = Mathf.Max(0, minY);
                maxY = Mathf.Min(height - 1, maxY);
            }

            // Evaluate pressure curve once outside pixel loop / ピクセルループ外で筆圧カーブを一度だけ評価
            float effOpacity = settings.pressureOpacityEnabled && pressure > 0.0001f
                ? settings.opacity * Mathf.Clamp01(settings.pressureOpacityCurve.Evaluate(pressure))
                : settings.opacity;

            // Pre-compute squared radius for distance comparison / 距離比較用に半径の二乗を事前計算
            float radiusSq = radius * radius;

            // Pre-compute target color for stroke buffer Paint mode
            // ストロークバッファPaintモード用のターゲットカラーを事前計算
            Color strokeTargetColor = Color.clear;
            if (useStrokeBuffer && settings.mode == BrushMode.Paint)
            {
                if (useColorMix)
                    strokeTargetColor = mixedColor;
                else if (settings.colorMode)
                    strokeTargetColor = settings.paintColor;
                else
                    strokeTargetColor = new Color(settings.strength, settings.strength, settings.strength, settings.paintAlpha);
            }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float distSq;
                    if (prevCenter.HasValue)
                    {
                        // Capsule distance: distance to line segment
                        // カプセル距離: 線分への距離
                        distSq = DistanceToSegmentSq(x, y, prevCenter.Value, center);
                    }
                    else
                    {
                        float dx = x - center.x;
                        float dy = y - center.y;
                        distSq = dx * dx + dy * dy;
                    }
                    float falloff = BrushFalloffSq(distSq, radiusSq, radius, effHardness);
                    if (falloff <= 0f) continue;

                    int sampleX = wrapCoordinates ? RepeatIndex(x, width) : x;
                    int sampleY = wrapCoordinates ? RepeatIndex(y, height) : y;
                    if (!wrapCoordinates && (sampleX < 0 || sampleX >= width || sampleY < 0 || sampleY >= height))
                        continue;

                    int idx = sampleY * width + sampleX;
                    // Use original canvas for lockTransparent check when stroke buffer is active
                    // ストロークバッファ使用時はオリジナルキャンバスでlockTransparentチェック
                    Color current = useStrokeBuffer ? canvasSnap[idx] : pixels[idx];
                    if (!CanModifyPixel(current, lockTransparentPixels, settings.alphaEpsilon))
                        continue;

                    float alpha = falloff * effOpacity;

                    switch (settings.mode)
                    {
                        case BrushMode.Paint:
                            if (useStrokeBuffer)
                            {
                                // Stroke buffer path: max-alpha accumulation
                                // ストロークバッファパス: max-alpha累積
                                if (alpha > strokeAlpha[idx])
                                {
                                    strokeAlpha[idx] = alpha;
                                    strokeColor[idx] = strokeTargetColor;
                                }
                            }
                            else
                            {
                                // Legacy direct paint path
                                // レガシー直接ペイントパス
                                if (useColorMix)
                                    pixels[idx] = ApplyPaintColor(current, alpha, mixedColor);
                                else if (settings.wetBrushEnabled)
                                {
                                    Color brushColor = settings.colorMode ? settings.paintColor
                                        : new Color(settings.strength, settings.strength, settings.strength, settings.paintAlpha);
                                    pixels[idx] = WetBrushMixer.ApplyWetBrush(current, falloff, effOpacity, brushColor, settings.wetness);
                                }
                                else
                                    pixels[idx] = ApplyPaint(current, alpha, settings);
                            }
                            break;

                        case BrushMode.Erase:
                        case BrushMode.EraseAlpha:
                            if (useStrokeBuffer)
                            {
                                // Stroke buffer erase: max-alpha accumulation with erase target
                                // ストロークバッファ消去: 消去ターゲットでmax-alpha累積
                                if (alpha > strokeAlpha[idx])
                                {
                                    strokeAlpha[idx] = alpha;
                                    if (settings.mode == BrushMode.EraseAlpha || !settings.eraseRgb)
                                        strokeColor[idx] = new Color(current.r, current.g, current.b, 0f);
                                    else
                                        strokeColor[idx] = Color.clear;
                                }
                            }
                            else
                            {
                                pixels[idx] = ApplyErase(current, alpha,
                                    settings.mode == BrushMode.Erase && settings.eraseRgb);
                            }
                            break;

                        case BrushMode.Smooth:
                            // Smooth mode always paints directly (needs live canvas state)
                            // Smoothモードは常に直接ペイント（ライブキャンバス状態が必要）
                            pixels[idx] = ApplySmooth(current, GetAverageNeighbors(pixels, sampleX, sampleY, width, height), alpha);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Apply stamp with symmetry support.
        /// 対称描画サポート付きスタンプ適用
        /// </summary>
        public static void ApplyStampWithSymmetry(
            Vector2 center, Color[] pixels, BrushSettings settings,
            int width, int height, bool lockTransparentPixels,
            float pressure = 1f,
            float[] strokeAlpha = null,
            Color[] strokeColor = null,
            Color[] canvasSnap = null,
            float velocityFactor = 1f,
            Vector2? prevCenter = null)
        {
            // Get all symmetry positions
            Vector2[] positions = SymmetryDrawing.GetMirroredPositions(center, width, height);
            foreach (var pos in positions)
            {
                ApplyStamp(pos, pixels, settings, width, height, lockTransparentPixels, pressure, false,
                    strokeAlpha, strokeColor, canvasSnap, velocityFactor, prevCenter);
            }
        }

        private static bool CanModifyPixel(Color current, bool lockTransparentPixels, float alphaEpsilon)
        {
            return !lockTransparentPixels || current.a > alphaEpsilon;
        }

        private static Color ApplyPaint(Color current, float alpha, BrushSettings settings)
        {
            Color targetColor;
            if (settings.colorMode)
            {
                targetColor = settings.paintColor;
            }
            else
            {
                targetColor = new Color(settings.strength, settings.strength, settings.strength, settings.paintAlpha);
            }
            return ApplyPaintColor(current, alpha, targetColor);
        }

        private static Color ApplyPaintColor(Color current, float alpha, Color targetColor)
        {
            float r = Mathf.Lerp(current.r, targetColor.r, alpha);
            float g = Mathf.Lerp(current.g, targetColor.g, alpha);
            float b = Mathf.Lerp(current.b, targetColor.b, alpha);
            float a2 = Mathf.Lerp(current.a, targetColor.a, alpha);
            return new Color(r, g, b, a2);
        }

        private static Color ApplyErase(Color current, float alpha, bool eraseRgb)
        {
            float a2 = Mathf.Lerp(current.a, 0f, alpha);
            if (!eraseRgb)
                return new Color(current.r, current.g, current.b, a2);

            float r = Mathf.Lerp(current.r, 0f, alpha);
            float g = Mathf.Lerp(current.g, 0f, alpha);
            float b = Mathf.Lerp(current.b, 0f, alpha);
            return new Color(r, g, b, a2);
        }

        private static Color ApplySmooth(Color current, Color avg, float alpha)
        {
            float r = Mathf.Lerp(current.r, avg.r, alpha);
            float g = Mathf.Lerp(current.g, avg.g, alpha);
            float b = Mathf.Lerp(current.b, avg.b, alpha);
            float a2 = Mathf.Lerp(current.a, avg.a, alpha);
            return new Color(r, g, b, a2);
        }

        /// <summary>
        /// Compute the average color of 3x3 neighborhood.
        /// 3x3近傍の平均色を計算
        /// </summary>
        private static Color GetAverageNeighbors(Color[] pixels, int cx, int cy, int width, int height)
        {
            float r = 0f, g = 0f, b = 0f, a = 0f;
            int count = 0;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                    Color c = pixels[ny * width + nx];
                    r += c.r;
                    g += c.g;
                    b += c.b;
                    a += c.a;
                    count++;
                }
            }

            if (count == 0) return Color.clear;
            return new Color(r / count, g / count, b / count, a / count);
        }

        /// <summary>
        /// Composite the stroke accumulation buffer onto the canvas for live preview.
        /// ライブプレビュー用にストローク累積バッファをキャンバスに合成
        /// </summary>
        public void CompositeStrokeToCanvas(Color[] pixels, int width, int height)
        {
            if (strokeAlphaBuffer == null || canvasSnapshot == null) return;
            if (width != strokeBufferWidth || height != strokeBufferHeight) return;

            if (!hasDirtyRect) return;

            // Only composite the dirty region
            // ダーティ領域のみ合成
            for (int y = dirtyMinY; y <= dirtyMaxY; y++)
            {
                int rowStart = y * width;
                for (int x = dirtyMinX; x <= dirtyMaxX; x++)
                {
                    int i = rowStart + x;
                    float sa = strokeAlphaBuffer[i];
                    if (sa <= 0f)
                    {
                        pixels[i] = canvasSnapshot[i];
                        continue;
                    }
                    pixels[i] = Color.Lerp(canvasSnapshot[i], strokeColorBuffer[i], sa);
                }
            }
        }
    }

    /// <summary>
    /// Renders UV wireframe overlay on the brush canvas using GL drawing.
    /// GLドローイングを使用してブラシキャンバス上にUVワイヤーフレームオーバーレイを描画
    /// </summary>
    internal static class UVWireframeRenderer
    {
        private static Material lineMaterial;

        private static void EnsureLineMaterial()
        {
            if (lineMaterial != null) return;
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return;
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }

        /// <summary>
        /// Draw UV wireframe for the given mesh onto the canvas rect.
        /// 指定メッシュのUVワイヤーフレームをキャンバス矩形に描画
        /// </summary>
        public static void DrawWireframe(Rect canvasRect, Mesh mesh, List<Vector2> uvs, int[] triangles = null)
        {
            if (mesh == null || uvs == null || uvs.Count == 0) return;

            EnsureLineMaterial();
            if (lineMaterial == null) return;

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(new Color(0f, 1f, 0f, 0.3f));

            int[] tris = triangles ?? mesh.triangles;
            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                int i0 = tris[i];
                int i1 = tris[i + 1];
                int i2 = tris[i + 2];

                if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                Vector2 a = UVToCanvas(uvs[i0], canvasRect);
                Vector2 b = UVToCanvas(uvs[i1], canvasRect);
                Vector2 c = UVToCanvas(uvs[i2], canvasRect);

                DrawLineGL(a, b);
                DrawLineGL(b, c);
                DrawLineGL(c, a);
            }

            GL.End();
            GL.PopMatrix();
        }

        internal static Vector2 UVToCanvas(Vector2 uv, Rect canvasRect)
        {
            return new Vector2(
                canvasRect.x + uv.x * canvasRect.width,
                canvasRect.y + (1f - uv.y) * canvasRect.height
            );
        }

        internal static void DrawLineGL(Vector2 a, Vector2 b)
        {
            GL.Vertex3(a.x, a.y, 0);
            GL.Vertex3(b.x, b.y, 0);
        }

        /// <summary>
        /// Draw UV wireframe with per-island coloring.
        /// アイランドごとに色分けしたUVワイヤーフレームを描画
        /// </summary>
        public static void DrawWireframePerIsland(
            Rect canvasRect, Mesh mesh, List<Vector2> uvs,
            IList<UVTextureGenerator.UVIsland> islands, Color[] islandColors, int hoveredIndex, int[] triangles = null)
        {
            if (mesh == null || uvs == null || uvs.Count == 0 || islands == null) return;

            EnsureLineMaterial();
            if (lineMaterial == null) return;

            int[] tris = triangles ?? mesh.triangles;

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.Begin(GL.LINES);

            for (int idx = 0; idx < islands.Count; idx++)
            {
                var island = islands[idx];
                Color color = idx < islandColors.Length ? islandColors[idx] : Color.gray;
                GL.Color(island.selected
                    ? new Color(color.r, color.g, color.b, 0.8f)
                    : new Color(color.r * 0.4f, color.g * 0.4f, color.b * 0.4f, 0.15f));

                foreach (int t in island.triangleIndices)
                {
                    int i0 = tris[t * 3 + 0];
                    int i1 = tris[t * 3 + 1];
                    int i2 = tris[t * 3 + 2];
                    if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                    Vector2 a = UVToCanvas(uvs[i0], canvasRect);
                    Vector2 b = UVToCanvas(uvs[i1], canvasRect);
                    Vector2 c = UVToCanvas(uvs[i2], canvasRect);
                    DrawLineGL(a, b);
                    DrawLineGL(b, c);
                    DrawLineGL(c, a);
                }
            }

            // Hovered island highlight
            if (hoveredIndex >= 0 && hoveredIndex < islands.Count)
            {
                GL.Color(new Color(1f, 1f, 1f, 0.9f));
                foreach (int t in islands[hoveredIndex].triangleIndices)
                {
                    int i0 = tris[t * 3 + 0];
                    int i1 = tris[t * 3 + 1];
                    int i2 = tris[t * 3 + 2];
                    if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                    Vector2 a = UVToCanvas(uvs[i0], canvasRect);
                    Vector2 b = UVToCanvas(uvs[i1], canvasRect);
                    Vector2 c = UVToCanvas(uvs[i2], canvasRect);
                    DrawLineGL(a, b);
                    DrawLineGL(b, c);
                    DrawLineGL(c, a);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        /// <summary>
        /// Draw filled triangles for selected islands.
        /// 選択アイランドの三角形を半透明塗りつぶしで描画
        /// </summary>
        public static void DrawIslandFill(
            Rect canvasRect, Mesh mesh, List<Vector2> uvs,
            IList<UVTextureGenerator.UVIsland> islands, Color[] islandColors, int[] triangles = null)
        {
            if (mesh == null || uvs == null || uvs.Count == 0 || islands == null) return;

            EnsureLineMaterial();
            if (lineMaterial == null) return;

            int[] tris = triangles ?? mesh.triangles;

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.Begin(GL.TRIANGLES);

            for (int idx = 0; idx < islands.Count; idx++)
            {
                var island = islands[idx];
                if (!island.selected) continue;

                Color color = idx < islandColors.Length ? islandColors[idx] : Color.gray;
                GL.Color(new Color(color.r, color.g, color.b, 0.2f));

                foreach (int t in island.triangleIndices)
                {
                    int i0 = tris[t * 3 + 0];
                    int i1 = tris[t * 3 + 1];
                    int i2 = tris[t * 3 + 2];
                    if (i0 >= uvs.Count || i1 >= uvs.Count || i2 >= uvs.Count) continue;

                    Vector2 a = UVToCanvas(uvs[i0], canvasRect);
                    Vector2 b = UVToCanvas(uvs[i1], canvasRect);
                    Vector2 c = UVToCanvas(uvs[i2], canvasRect);
                    GL.Vertex3(a.x, a.y, 0);
                    GL.Vertex3(b.x, b.y, 0);
                    GL.Vertex3(c.x, c.y, 0);
                }
            }

            GL.End();
            GL.PopMatrix();
        }
    }

    /// <summary>
    /// Renders a circular brush cursor on the canvas.
    /// キャンバス上に円形ブラシカーソルを描画
    /// </summary>
    internal static class BrushCursorRenderer
    {
        /// <summary>
        /// Draw a circle cursor at the given center with specified radius and color.
        /// 指定された中心・半径・色で円形カーソルを描画
        /// </summary>
        public static void DrawCursor(Vector2 center, float radius, Color color)
        {
            Handles.color = color;
            int segments = 32;
            Vector3[] points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * 2f * Mathf.PI;
                points[i] = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius, 0);
            }
            Handles.DrawPolyLine(points);
        }

        /// <summary>
        /// Draw cursor with inner hardness circle and outer falloff circle.
        /// 内側の硬さ円と外側のフォールオフ円でカーソルを描画
        /// </summary>
        public static void DrawCursorWithHardness(Vector2 center, float radius, float hardness, Color color)
        {
            DrawCursor(center, radius, color);

            float innerRatio = MaskTextureBrush.GetHardnessInnerRatio(hardness);
            if (innerRatio > 0.01f && innerRatio < 0.99f)
            {
                Color innerColor = new Color(color.r, color.g, color.b, color.a * 0.5f);
                DrawCursor(center, radius * innerRatio, innerColor);
            }
        }

        /// <summary>
        /// Draw cursor with dynamic hardness reflecting current pressure.
        /// 現在の筆圧を反映した動的な硬さでカーソルを描画
        /// </summary>
        public static void DrawCursorWithPressureHardness(
            Vector2 center, float radius, float baseHardness,
            float pressure, BrushSettings settings, Color color)
        {
            DrawCursor(center, radius, color);

            float effHardness = baseHardness;
            if (settings != null && settings.pressureHardnessEnabled && pressure > 0.001f)
                effHardness = baseHardness * Mathf.Clamp01(settings.pressureHardnessCurve.Evaluate(pressure));

            float innerRatio = MaskTextureBrush.GetHardnessInnerRatio(effHardness);
            if (innerRatio > 0.01f && innerRatio < 0.99f)
            {
                Color innerColor = new Color(color.r, color.g, color.b, color.a * 0.5f);
                DrawCursor(center, radius * innerRatio, innerColor);
            }
        }

        public static void DrawLineGuide(Vector2 start, Vector2 end, Color color)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(2f, new Vector3(start.x, start.y, 0f), new Vector3(end.x, end.y, 0f));
        }

        /// <summary>
        /// Draw enhanced cursor with foreground color dot and optional size label.
        /// 前景色ドットとオプションのサイズラベル付き拡張カーソル描画
        /// </summary>
        public static void DrawEnhancedCursor(Vector2 center, float radius, float hardness, Color cursorColor, Color foregroundColor, bool showSizeLabel, float brushSize)
        {
            // Outer circle
            DrawCursor(center, radius, cursorColor);

            // Inner hardness circle
            float innerRatio = MaskTextureBrush.GetHardnessInnerRatio(hardness);
            if (innerRatio > 0.01f && innerRatio < 0.99f)
            {
                Color innerColor = new Color(cursorColor.r, cursorColor.g, cursorColor.b, cursorColor.a * 0.5f);
                DrawCursor(center, radius * innerRatio, innerColor);
            }

            // Foreground color dot at center
            float dotRadius = Mathf.Clamp(radius * 0.15f, 2f, 6f);
            Handles.color = foregroundColor;
            Handles.DrawSolidDisc(new Vector3(center.x, center.y, 0), Vector3.forward, dotRadius);
            // Outline for visibility
            Handles.color = new Color(1f - foregroundColor.r, 1f - foregroundColor.g, 1f - foregroundColor.b, 0.8f);
            Handles.DrawWireDisc(new Vector3(center.x, center.y, 0), Vector3.forward, dotRadius);

            // Size label when adjusting
            if (showSizeLabel)
            {
                var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.white }
                };
                Vector2 labelPos = new Vector2(center.x + radius + 8, center.y - 8);
                Rect labelRect = new Rect(labelPos.x, labelPos.y, 60, 18);
                EditorGUI.DrawRect(new Rect(labelRect.x - 2, labelRect.y - 1, labelRect.width + 4, labelRect.height + 2), new Color(0f, 0f, 0f, 0.6f));
                GUI.Label(labelRect, $"{brushSize:F0}px", labelStyle);
            }
        }
    }

    /// <summary>
    /// UI drawing utilities for brush settings panel.
    /// ブラシ設定パネル用のUI描画ユーティリティ
    /// </summary>
    internal static class BrushSettingsUI
    {
        private const float FoldoutBottomPadding = 4f;
        // Foldout states (persisted via SessionState)
        private static bool pressureFoldout;
        private static bool taperFoldout;
        private static bool correctionFoldout;
        private static int selectedPressureParam = -1; // PS-style: which param's curve is shown

        private static readonly string[] pressureParamNames = new string[]
        {
            "Opacity", "Size", "Hardness", "Density", "ColorMix"
        };
        private static readonly string[] pressureParamLabelsJP = new string[]
        {
            "不透明度", "サイズ", "硬さ", "濃度", "色混合"
        };

        private static GUIContent[] modeLabels => new GUIContent[]
        {
            new GUIContent(L("ペイント", "Paint"), L("ペイント", "Paint")),
            new GUIContent(L("消しゴム", "Erase"), L("黒に消去", "Erase to black")),
            new GUIContent(L("スムーズ", "Smooth"), L("値をスムーズ・ぼかし", "Smooth/blur values")),
            new GUIContent(L("α消去", "Erase Alpha"), L("アルファチャンネルを消去", "Erase alpha channel"))
        };

        private static void DrawGrayscaleIndicator(float value)
        {
            Rect barRect = GUILayoutUtility.GetRect(0, 14, GUILayout.ExpandWidth(true));
            barRect = EditorGUI.IndentedRect(barRect);
            if (Event.current.type != EventType.Repaint) return;

            int steps = Mathf.Max(1, (int)barRect.width);
            for (int i = 0; i < steps; i++)
            {
                float t = (float)i / steps;
                EditorGUI.DrawRect(new Rect(barRect.x + i, barRect.y, 1, barRect.height - 2), new Color(t, t, t, 1f));
            }
            Color borderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y + barRect.height - 3, barRect.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, 1, barRect.height - 2), borderColor);
            EditorGUI.DrawRect(new Rect(barRect.x + barRect.width - 1, barRect.y, 1, barRect.height - 2), borderColor);
            float markerX = barRect.x + value * barRect.width;
            Color markerColor = value > 0.5f ? Color.black : Color.white;
            EditorGUI.DrawRect(new Rect(markerX - 1, barRect.y - 1, 3, barRect.height), markerColor);
        }

        public static void DrawBrushSettingsUI(BrushSettings settings)
        {
            if (settings == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Mode toolbar
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(L("モード", "Mode"), GUILayout.Width(60));
                int modeIndex = GUILayout.Toolbar((int)settings.mode, modeLabels);
                settings.mode = (BrushMode)modeIndex;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                // Core settings - コンパクト1行スライダー化
                DrawCompactSlider(L("サイズ", "Size"), ref settings.size, 1f, 100f);
                DrawCompactSlider(L("硬さ", "Hard"), ref settings.hardness, 0f, 1f);
                DrawCompactSlider(L("不透明度", "Opac"), ref settings.opacity, 0f, 1f);

                if (settings.mode == BrushMode.Paint)
                {
                    settings.colorMode = EditorGUILayout.Toggle(L("カラーモード", "Color Mode"), settings.colorMode);

                    if (settings.colorMode)
                    {
                        EditorGUILayout.BeginHorizontal();
                        settings.paintColor = EditorGUILayout.ColorField(
                            new GUIContent(L("前景色", "FG")), settings.paintColor, GUILayout.MinWidth(60));
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            var tmp = settings.paintColor;
                            settings.paintColor = settings.backgroundColor;
                            settings.backgroundColor = tmp;
                        }
                        settings.backgroundColor = EditorGUILayout.ColorField(
                            new GUIContent(L("背景色", "BG")), settings.backgroundColor, GUILayout.MinWidth(60));
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        settings.strength = EditorGUILayout.Slider(L("強度", "Strength"), settings.strength, 0f, 1f);
                        DrawGrayscaleIndicator(settings.strength);
                        settings.paintAlpha = EditorGUILayout.Slider(L("アルファ", "Alpha"), settings.paintAlpha, 0f, 1f);
                    }
                }
                else if (settings.mode == BrushMode.Erase)
                {
                    settings.eraseRgb = EditorGUILayout.Toggle(L("RGB消去", "Clear RGB"), settings.eraseRgb);
                }

                // Stabilizer
                settings.stabilizer.mode = (BrushStabilizerMode)EditorGUILayout.EnumPopup(L("手ブレ補正", "Stabilizer"), settings.stabilizer.mode);
                if (settings.stabilizer.mode != BrushStabilizerMode.Off)
                    settings.stabilizer.strength = EditorGUILayout.Slider(L("補正強度", "Strength"), settings.stabilizer.strength, 0f, 1f);

                // Wet Brush / ウェットブラシ
                if (settings.mode == BrushMode.Paint)
                {
                    EditorGUILayout.Space(2);
                    settings.wetBrushEnabled = EditorGUILayout.Toggle(L("ウェットブラシ", "Wet Brush"), settings.wetBrushEnabled);
                    if (settings.wetBrushEnabled)
                        settings.wetness = EditorGUILayout.Slider(L("水分量", "Wetness"), settings.wetness, 0f, 1f);
                }

                // Texture Brush / テクスチャブラシ
                TextureBrushSystem.DrawSettingsUI(settings.textureBrush);
            }

            // ===== Pressure Settings (Foldout, PS-style grouped) =====
            pressureFoldout = EditorGUILayout.Foldout(pressureFoldout, L("筆圧設定", "Pressure Settings"), true);
            if (pressureFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPressureParamList(settings);
                    EditorGUILayout.Space(2);
                    DrawSelectedPressureCurve(settings);
                    EditorGUILayout.Space(FoldoutBottomPadding);
                }
            }

            // ===== Entry/Exit Taper (Foldout) =====
            taperFoldout = EditorGUILayout.Foldout(taperFoldout, L("入り抜き設定", "Entry/Exit Taper"), true);
            if (taperFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    settings.entryExitEnabled = EditorGUILayout.Toggle(L("入り抜き有効", "Enable Taper"), settings.entryExitEnabled);
                    if (settings.entryExitEnabled)
                    {
                        settings.entryLength = EditorGUILayout.Slider(L("入り長さ", "Entry"), settings.entryLength, 0f, 0.5f);
                        settings.entryCurve = EditorGUILayout.CurveField(L("入りカーブ", "Entry Curve"), settings.entryCurve);
                        settings.exitLength = EditorGUILayout.Slider(L("抜き長さ", "Exit"), settings.exitLength, 0f, 0.5f);
                        settings.exitCurve = EditorGUILayout.CurveField(L("抜きカーブ", "Exit Curve"), settings.exitCurve);
                    }

                    EditorGUILayout.Space(FoldoutBottomPadding);
                }
            }

            // ===== Pressure Correction (Foldout) =====
            correctionFoldout = EditorGUILayout.Foldout(correctionFoldout, L("筆圧補正", "Pressure Correction"), true);
            if (correctionFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    settings.pressureSmoothingEnabled = EditorGUILayout.Toggle(L("スムージング", "Smoothing"), settings.pressureSmoothingEnabled);
                    if (settings.pressureSmoothingEnabled)
                        settings.pressureSmoothingStrength = EditorGUILayout.Slider(L("強度", "Strength"), settings.pressureSmoothingStrength, 0f, 1f);

                    settings.mouseSpeedPressureEnabled = EditorGUILayout.Toggle(L("マウス速度筆圧", "Mouse Speed Pressure"), settings.mouseSpeedPressureEnabled);
                    if (settings.mouseSpeedPressureEnabled)
                    {
                        EditorGUI.indentLevel++;
                        settings.mouseSpeedMin = EditorGUILayout.FloatField(L("最小速度", "Min Speed"), settings.mouseSpeedMin);
                        settings.mouseSpeedMax = EditorGUILayout.FloatField(L("最大速度", "Max Speed"), settings.mouseSpeedMax);
                        settings.mouseSpeedCurve = EditorGUILayout.CurveField(L("速度カーブ", "Speed Curve"), settings.mouseSpeedCurve);
                        EditorGUI.indentLevel--;
                    }

                    if (GUILayout.Button(L("キャリブレーション", "Calibration")))
                    {
                        BrushPressureCalibration.Open((curve) =>
                        {
                            if (curve != null)
                            {
                                settings.pressureOpacityCurve = new AnimationCurve(curve.keys);
                                settings.pressureSizeCurve = new AnimationCurve(curve.keys);
                            }
                        });
                    }

                    EditorGUILayout.Space(FoldoutBottomPadding);
                }
            }
        }

        private static void DrawCompactSlider(string label, ref float value, float min, float max)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleRight,
                fixedWidth = 40
            }, GUILayout.Width(40));
            value = EditorGUILayout.Slider(value, min, max);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// PS-style pressure parameter list: checkboxes on left, click to show curve on right.
        /// PS方式の筆圧パラメータリスト: 左にチェックボックス、クリックでカーブ表示
        /// </summary>
        private static void DrawPressureParamList(BrushSettings s)
        {
            bool[] enabled = { s.pressureOpacityEnabled, s.pressureSizeEnabled, s.pressureHardnessEnabled, s.pressureDensityEnabled, s.pressureColorMixEnabled };

            for (int i = 0; i < pressureParamNames.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // Checkbox
                bool newEnabled = EditorGUILayout.Toggle(enabled[i], GUILayout.Width(15));
                if (newEnabled != enabled[i])
                {
                    switch (i)
                    {
                        case 0: s.pressureOpacityEnabled = newEnabled; break;
                        case 1: s.pressureSizeEnabled = newEnabled; break;
                        case 2: s.pressureHardnessEnabled = newEnabled; break;
                        case 3: s.pressureDensityEnabled = newEnabled; break;
                        case 4: s.pressureColorMixEnabled = newEnabled; break;
                    }
                }

                // Clickable label to select for curve editing
                bool isSelected = (selectedPressureParam == i);
                var style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
                string label = L(pressureParamLabelsJP[i], pressureParamNames[i]);
                if (GUILayout.Button(label, style))
                    selectedPressureParam = isSelected ? -1 : i;

                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Draw the curve editor for the selected pressure parameter.
        /// 選択中の筆圧パラメータのカーブエディタを描画
        /// </summary>
        private static void DrawSelectedPressureCurve(BrushSettings s)
        {
            if (selectedPressureParam < 0) return;

            string name = L(pressureParamLabelsJP[selectedPressureParam], pressureParamNames[selectedPressureParam]);
            EditorGUILayout.LabelField($"{name} " + L("カーブ", "Curve"), EditorStyles.miniLabel);

            switch (selectedPressureParam)
            {
                case 0: // Opacity
                    s.pressureOpacityCurve = EditorGUILayout.CurveField(s.pressureOpacityCurve, GUILayout.Height(50));
                    break;
                case 1: // Size
                    s.pressureSizeCurve = EditorGUILayout.CurveField(s.pressureSizeCurve, GUILayout.Height(50));
                    break;
                case 2: // Hardness
                    s.pressureHardnessCurve = EditorGUILayout.CurveField(s.pressureHardnessCurve, GUILayout.Height(50));
                    break;
                case 3: // Density
                    s.pressureDensityCurve = EditorGUILayout.CurveField(s.pressureDensityCurve, GUILayout.Height(50));
                    break;
                case 4: // Color Mix
                    s.pressureColorMixCurve = EditorGUILayout.CurveField(s.pressureColorMixCurve, GUILayout.Height(50));
                    EditorGUILayout.Space(2);
                    s.pressureColorA = EditorGUILayout.ColorField(L("カラーA", "Color A"), s.pressureColorA);
                    s.pressureColorB = EditorGUILayout.ColorField(L("カラーB", "Color B"), s.pressureColorB);
                    break;
            }
        }
    }

    /// <summary>
    /// Handles mouse input on the brush canvas and dispatches to the brush.
    /// ブラシキャンバス上のマウス入力を処理しブラシにディスパッチ
    /// </summary>
    internal static class BrushCanvasInputHandler
    {
        /// <summary>
        /// Process input events for the brush canvas. Returns true if the event was consumed.
        /// ブラシキャンバスの入力イベントを処理。イベントが消費された場合trueを返す
        /// </summary>
        /// <param name="canvasRect">Display area for hit testing (visible canvas region).</param>
        /// <param name="textureRect">Actual texture display rect for coordinate mapping (accounts for zoom/pan).</param>
        /// <param name="brush">Brush instance.</param>
        /// <param name="settings">Brush settings.</param>
        /// <param name="pixels">Pixel array to paint on.</param>
        /// <param name="width">Texture width.</param>
        /// <param name="height">Texture height.</param>
        /// <param name="textureModified">Set to true if pixels were modified.</param>
        public static bool HandleBrushInput(
            Rect canvasRect,
            Rect textureRect,
            MaskTextureBrush brush,
            BrushSettings settings,
            Color[] pixels,
            int width,
            int height,
            out bool textureModified)
        {
            BrushStrokeCommit strokeCommit;
            return HandleBrushInput(
                canvasRect, textureRect, brush, settings, pixels, width, height, false, null, false, out textureModified, out strokeCommit);
        }

        public static bool HandleBrushInput(
            Rect canvasRect,
            Rect textureRect,
            MaskTextureBrush brush,
            BrushSettings settings,
            Color[] pixels,
            int width,
            int height,
            bool lockTransparentPixels,
            out bool textureModified)
        {
            BrushStrokeCommit strokeCommit;
            return HandleBrushInput(
                canvasRect, textureRect, brush, settings, pixels, width, height, lockTransparentPixels, null, false, out textureModified, out strokeCommit);
        }

        public static bool HandleBrushInput(
            Rect canvasRect,
            Rect textureRect,
            MaskTextureBrush brush,
            BrushSettings settings,
            Color[] pixels,
            int width,
            int height,
            bool lockTransparentPixels,
            MaskTextureBrushStabilizer stabilizer,
            bool wrapCoordinates,
            out bool textureModified,
            out BrushStrokeCommit strokeCommit)
        {
            textureModified = false;
            strokeCommit = default;
            if (brush == null || settings == null || pixels == null) return false;

            Event e = Event.current;
            if (!canvasRect.Contains(e.mousePosition) && !brush.IsStroking)
                return false;

            // Use textureRect for coordinate mapping (zoom/pan aware)
            Vector2 rawCanvasPos = MouseToPixelPos(e.mousePosition, textureRect, width, height);
            // Only apply stabilizer during mouse events to prevent drift from Layout/Repaint
            // マウスイベント時のみスタビライザを適用（Layout/Repaintからのドリフトを防止）
            bool isMouseEvent = e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseUp;
            Vector2 canvasPos = isMouseEvent
                ? FilterBrushPosition(rawCanvasPos, settings, stabilizer)
                : rawCanvasPos;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && canvasRect.Contains(e.mousePosition))
                    {
                        float pressure = ResolvePressure(e, rawCanvasPos, brush, settings);
                        stabilizer?.Reset();
                        canvasPos = FilterBrushPosition(rawCanvasPos, settings, stabilizer);
                        brush.StartStroke(canvasPos, pixels, width, height, lockTransparentPixels, pressure, wrapCoordinates);
                        if (strokeDebugCount < 3)
                        {
                            Debug.Log($"[TextureStudio Debug] StrokeStart: mouse={e.mousePosition}, pixel={canvasPos}, textureRect={textureRect}, canvasRect={canvasRect}");
                            strokeDebugCount++;
                        }
                        textureModified = true;
                        e.Use();
                        RequestRepaint();
                        return true;
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && brush.IsStroking)
                    {
                        float pressure = ResolvePressure(e, rawCanvasPos, brush, settings);
                        // Apply entry/exit taper / 入り抜き適用
                        // Update stroke speed for exit taper
                        float strokeSpeed = brush.PressureFilter.LastStrokeSpeed;
                        float taper = brush.PressureFilter.CalculateTaper(
                            settings.entryExitEnabled,
                            settings.entryLength, settings.exitLength,
                            settings.entryCurve, settings.exitCurve,
                            strokeSpeed);
                        if (settings.entryExitEnabled && taper > 0f)
                            pressure = Mathf.Max(pressure * taper, 0.01f);
                        else
                            pressure *= taper;
                        brush.StrokeToPosition(canvasPos, pixels, width, height, lockTransparentPixels, pressure, wrapCoordinates);
                        if (strokeDebugCount > 0 && strokeDebugCount < 3)
                        {
                            Debug.Log($"[TextureStudio Debug] StrokeDrag: mouse={e.mousePosition}, pixel={canvasPos}, lastPos={brush.LastStrokePosition}");
                        }
                        textureModified = true;
                        e.Use();
                        RequestRepaint();
                        return true;
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0 && brush.IsStroking)
                    {
                        Color[] beforePixels = brush.EndStroke();
                        strokeCommit = CreateStrokeCommit(beforePixels, pixels, rawCanvasPos);
                        stabilizer?.Reset();
                        e.Use();
                        RequestRepaint();
                        return true;
                    }
                    break;

                case EventType.ScrollWheel:
                    if (canvasRect.Contains(e.mousePosition))
                    {
                        float delta = -e.delta.y;
                        settings.size = Mathf.Clamp(settings.size + delta * 2f, 1f, 100f);
                        e.Use();
                        RequestRepaint();
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Process input events for the brush canvas (legacy overload without separate textureRect).
        /// ブラシキャンバスの入力イベントを処理（textureRect分離なしのレガシーオーバーロード）
        /// </summary>
        public static bool HandleBrushInput(
            Rect canvasRect,
            MaskTextureBrush brush,
            BrushSettings settings,
            Color[] pixels,
            int width,
            int height,
            out bool textureModified)
        {
            BrushStrokeCommit strokeCommit;
            return HandleBrushInput(
                canvasRect, canvasRect, brush, settings, pixels, width, height, false, null, false, out textureModified, out strokeCommit);
        }

        /// <summary>
        /// Convert mouse position in canvas rect to pixel coordinates.
        /// キャンバス矩形内のマウス位置をピクセル座標に変換
        /// </summary>
        public static Vector2 MouseToPixelPos(Vector2 mousePos, Rect canvasRect, int width, int height)
        {
            float u = (mousePos.x - canvasRect.x) / canvasRect.width;
            float v = (mousePos.y - canvasRect.y) / canvasRect.height;
            v = 1f - v; // Flip Y: screen space top-down → texture space bottom-up
            return new Vector2(u * width, v * height);
        }

        /// <summary>
        /// Convert mouse position in canvas rect to UV coordinates (0-1 range).
        /// キャンバス矩形内のマウス位置をUV座標（0-1範囲）に変換
        /// </summary>
        public static Vector2 MouseToUV(Vector2 mousePos, Rect canvasRect)
        {
            float u = Mathf.Clamp01((mousePos.x - canvasRect.x) / canvasRect.width);
            float v = Mathf.Clamp01((mousePos.y - canvasRect.y) / canvasRect.height);
            v = 1f - v;
            return new Vector2(u, v);
        }

        public static Vector2 PixelToCanvasPos(Vector2 pixelPos, Rect canvasRect, int width, int height)
        {
            float u = width > 0 ? pixelPos.x / Mathf.Max(1f, width) : 0f;
            float v = height > 0 ? 1f - (pixelPos.y / Mathf.Max(1f, height)) : 1f;
            return new Vector2(
                canvasRect.x + u * canvasRect.width,
                canvasRect.y + v * canvasRect.height);
        }

        /// <summary>
        /// Resolve final pressure value from pen input, mouse simulation, and smoothing.
        /// ペン入力・マウスシミュレーション・スムージングから最終筆圧値を解決
        /// </summary>
        // Debug: log first few stroke positions to diagnose coordinate issues
        // デバッグ: 座標問題の診断用に最初の数ストローク位置をログ出力
        private static int strokeDebugCount = 0;

        private static bool penPressureDetected;
        private static bool penPressureLogShown;

        /// <summary>Whether pen pressure has been successfully detected in this session.</summary>
        public static bool IsPenPressureDetected => penPressureDetected;

        private static float ResolvePressure(Event e, Vector2 rawCanvasPos, MaskTextureBrush brush, BrushSettings settings)
        {
            float rawPressure;

            // 1. Try Event.pressure (IMGUI pen pressure)
            // IMGUI ペン筆圧を試行
            if (e.pressure > 0.0001f)
            {
                rawPressure = e.pressure;
                if (!penPressureLogShown)
                {
                    penPressureLogShown = true;
                    penPressureDetected = true;
                    Debug.Log("[TextureStudio] ペンタブレット筆圧を検出しました (Pen pressure detected)");
                }
            }
            // 2. Try pointer type detection: pen may report pressure=0 but still be a pen
            // ポインタータイプ検出: 筆圧0でもペンとして認識されている場合がある
            else if (e.pointerType == UnityEngine.PointerType.Pen)
            {
                // Pen is detected but pressure is 0 - use speed simulation as substitute
                // ペン検出されたが筆圧0 - 速度シミュレーションで代替
                rawPressure = brush.PressureFilter.SimulateMousePressure(
                    rawCanvasPos, true,
                    settings.mouseSpeedMin, settings.mouseSpeedMax,
                    settings.mouseSpeedCurve);
                if (!penPressureLogShown)
                {
                    penPressureLogShown = true;
                    Debug.LogWarning("[TextureStudio] ペンタブレットは検出されましたが筆圧が取得できません。速度シミュレーションで代替します。\n" +
                        "タブレットドライバの設定で「Windows Ink」を有効にすると筆圧が使えるようになる場合があります。\n" +
                        "(Pen detected but pressure=0. Using speed simulation. Enable 'Windows Ink' in tablet driver settings.)");
                }
            }
            // 3. Mouse speed pressure simulation (always available fallback)
            // マウス速度筆圧シミュレーション（常時利用可能なフォールバック）
            else if (settings.mouseSpeedPressureEnabled)
            {
                rawPressure = brush.PressureFilter.SimulateMousePressure(
                    rawCanvasPos, true,
                    settings.mouseSpeedMin, settings.mouseSpeedMax,
                    settings.mouseSpeedCurve);
            }
            else
            {
                rawPressure = 1f;
            }

            // Apply dead zone and smoothing filter
            // デッドゾーンとスムージングフィルタを適用
            return brush.PressureFilter.FilterPressure(
                rawPressure,
                settings.pressureSmoothingEnabled,
                settings.pressureSmoothingStrength,
                settings.pressureDeadZone);
        }

        private static void RequestRepaint()
        {
            if (EditorWindow.focusedWindow != null)
                EditorWindow.focusedWindow.Repaint();
        }

        private static Vector2 FilterBrushPosition(Vector2 rawCanvasPos, BrushSettings settings, MaskTextureBrushStabilizer stabilizer)
        {
            if (stabilizer == null)
                return rawCanvasPos;

            return stabilizer.Filter(rawCanvasPos, settings.stabilizer);
        }

        private static BrushStrokeCommit CreateStrokeCommit(Color[] beforePixels, Color[] pixels, Vector2 lastPixelPosition)
        {
            if (beforePixels == null || pixels == null)
                return default;

            return new BrushStrokeCommit(beforePixels, ClonePixels(pixels), lastPixelPosition);
        }

        private static Color[] ClonePixels(Color[] pixels)
        {
            if (pixels == null)
                return null;

            Color[] clone = new Color[pixels.Length];
            System.Array.Copy(pixels, clone, pixels.Length);
            return clone;
        }
    }
}
