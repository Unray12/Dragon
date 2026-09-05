using System;
using System.Collections.Generic;
using DragonAR.Core;
using TMPro;
using UnityEngine;

namespace DragonAR.UI
{
    // Cac chi so moi truong hien trong nhung QUA CAU 3D bay vong quanh con rong, thay cho
    // panel dashboard phang truoc day.
    //
    // Quy dao la 1 vong tron NGHIENG quanh truc dung, khong phai vong tron nam trong mat
    // phang man hinh: nho vay bong bong di ra TRUOC va ra SAU con rong, bi con rong che
    // khuat khi o phia sau - day chinh la thu tao cam giac chieu sau that, cai ma 1 vong
    // tron phang khong bao gio co duoc.
    //
    // Chu luon xoay ve phia nguoi xem (WorldPanelBillboard) nen doc duoc o moi vi tri tren
    // quy dao. Chu duoc dat lech ve phia camera 1 doan bang ban kinh qua cau, neu khong no
    // se nam ngay giua qua cau va bi mat mot nua.
    public static class MetricBubbleRing
    {
        private const float OrbitRadius = 0.46f;
        private const float OrbitDegreesPerSecond = 14f;

        // Nghieng quy dao de nhin thay ro la 1 vong tron trong khong gian chu khong phai 1
        // duong thang ngang. 0 do = nam ngang hoan toan, 90 do = dung trong mat phang man hinh.
        private const float OrbitTiltDegrees = 28f;

        private const float BubbleRadius = 0.105f;
        private const float BobAmplitude = 0.025f;
        private const float BobPeriodSeconds = 3.4f;

        // Chu phai nam GON TRONG duong tron cua qua cau, khong duoc tran ra ngoai. Be rong
        // dung duoc cua 1 hinh tron ~ 0.7 lan duong kinh; 0.105*2*0.7 = 0.147m = 122 don vi
        // UI o ti le 0.0012 m/don vi.
        private const float LabelCanvasWidth = 122f;
        private const float LabelCanvasHeight = 122f;
        private const float WorldUnitsPerPixel = 0.0012f;

        // Xanh luc dam bong nhu anh tham chieu: toi de chu trang noi bat, smoothness cao de
        // bat highlight cua den moi truong (Light Estimation sau nay se lam no "thuoc ve"
        // can phong).
        private static readonly Color BubbleColor = new(0.016f, 0.121f, 0.078f, 1f);
        private static readonly Color BubbleRimEmission = new(0.05f, 0.42f, 0.28f, 1f);
        private static readonly Color LabelInkColor = new(0.78f, 0.90f, 0.84f, 1f);
        private static readonly Color ValueInkColor = Color.white;

        public static void Spawn(Transform parent, Vector3 localPosition,
            ITelemetrySource source, IReadOnlyList<MetricDisplay> metrics)
        {
            if (source == null || metrics == null || metrics.Count == 0)
            {
                Debug.LogError("[BubbleRing] Thieu nguon so lieu hoac danh sach chi so, khong spawn.");
                return;
            }

            var ringGo = new GameObject("MetricBubbleRing");
            ringGo.transform.SetParent(parent, worldPositionStays: false);
            ringGo.transform.localPosition = localPosition;
            ringGo.transform.localRotation = Quaternion.identity;

            var material = CreateBubbleMaterial();
            var bubbles = new Bubble[metrics.Count];

            for (var i = 0; i < metrics.Count; i++)
            {
                bubbles[i] = CreateBubble(ringGo.transform, metrics[i], material, i, metrics.Count);
            }

            var ring = ringGo.AddComponent<MetricBubbleRingDriver>();
            ring.Init(source, metrics, bubbles);
        }

        private static Material CreateBubbleMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader);
            material.SetColor("_BaseColor", BubbleColor);
            material.SetFloat("_Smoothness", 0.92f);
            material.SetFloat("_Metallic", 0.15f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", BubbleRimEmission * 0.35f);
            return material;
        }

        private static Bubble CreateBubble(Transform ringRoot, MetricDisplay metric, Material material,
            int index, int total)
        {
            var pivot = new GameObject($"Bubble_{metric.Key}");
            pivot.transform.SetParent(ringRoot, false);

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Sphere";
            sphere.transform.SetParent(pivot.transform, false);
            sphere.transform.localScale = Vector3.one * (BubbleRadius * 2f);
            sphere.GetComponent<Renderer>().sharedMaterial = material;

            // Primitive luon kem Collider - khong dung physics o day, bo di cho nhe va tranh
            // no chan tia cham cua UI sau nay.
            UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());

            // Goc billboard rieng: xoay mat ve camera, roi chu moi duoc day ra truoc qua cau
            // theo huong -Z cuc bo (WorldPanelBillboard cho +Z huong RA XA camera).
            var labelRoot = new GameObject("Label");
            labelRoot.transform.SetParent(pivot.transform, false);
            labelRoot.AddComponent<WorldPanelBillboard>();

            var canvasGo = new GameObject("LabelCanvas");
            canvasGo.transform.SetParent(labelRoot.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, -(BubbleRadius + 0.005f));

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(LabelCanvasWidth, LabelCanvasHeight);
            canvasGo.transform.localScale = Vector3.one * WorldUnitsPerPixel;

            var label = CreateText(canvasGo.transform, metric.Label, 13f, FontStyles.Normal, LabelInkColor,
                anchorY: 0.74f, height: 20f);
            var value = CreateText(canvasGo.transform, "--", 30f, FontStyles.Bold, ValueInkColor,
                anchorY: 0.46f, height: 40f);
            CreateText(canvasGo.transform, metric.Unit.Trim(), 10f, FontStyles.Normal, LabelInkColor,
                anchorY: 0.24f, height: 16f);

            // Chia deu quanh vong tron, lech pha nhip nhun de ca cum khong nhun cung luc
            // nhu mot khoi.
            return new Bubble(pivot.transform, value, 360f / total * index, Mathf.PI * 2f / total * index);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string content, float fontSize,
            FontStyles style, Color color, float anchorY, float height)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, anchorY);
            rect.anchorMax = new Vector2(1f, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;

            // Gia tri dai ("1013 hPa") thi tu thu nho lai cho vua qua cau thay vi tran ra ngoai.
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.5f;
            text.fontSizeMax = fontSize;

            return text;
        }

        private readonly struct Bubble
        {
            public Transform Pivot { get; }
            public TextMeshProUGUI ValueText { get; }
            public float AngleOffsetDegrees { get; }
            public float BobPhase { get; }

            public Bubble(Transform pivot, TextMeshProUGUI valueText, float angleOffsetDegrees, float bobPhase)
            {
                Pivot = pivot;
                ValueText = valueText;
                AngleOffsetDegrees = angleOffsetDegrees;
                BobPhase = bobPhase;
            }
        }

        // Dat lai vi tri bong bong tren quy dao moi frame + do so lieu vao chu. Chi subscribe
        // su kien, khong tu hoi nguon du lieu.
        private sealed class MetricBubbleRingDriver : MonoBehaviour
        {
            private ITelemetrySource _source;
            private IReadOnlyList<MetricDisplay> _metrics;
            private Bubble[] _bubbles;
            private float _orbitDegrees;

            public void Init(ITelemetrySource source, IReadOnlyList<MetricDisplay> metrics, Bubble[] bubbles)
            {
                _source = source;
                _metrics = metrics;
                _bubbles = bubbles;

                _source.SamplesReceived += OnSamplesReceived;
                Layout();
            }

            private void OnDestroy()
            {
                if (_source != null)
                {
                    _source.SamplesReceived -= OnSamplesReceived;
                }
            }

            private void Update()
            {
                _orbitDegrees = Mathf.Repeat(_orbitDegrees + OrbitDegreesPerSecond * Time.deltaTime, 360f);
                Layout();
            }

            private void Layout()
            {
                var tilt = Quaternion.AngleAxis(OrbitTiltDegrees, Vector3.right);

                for (var i = 0; i < _bubbles.Length; i++)
                {
                    var bubble = _bubbles[i];
                    var angle = (_orbitDegrees + bubble.AngleOffsetDegrees) * Mathf.Deg2Rad;

                    // Vong tron nam trong mat phang XZ roi nghieng di - bong bong o nua sau
                    // cua vong se bi con rong che, tao chieu sau.
                    var onCircle = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * OrbitRadius;
                    var bob = Mathf.Sin(Time.time * (Mathf.PI * 2f / BobPeriodSeconds) + bubble.BobPhase) * BobAmplitude;

                    bubble.Pivot.localPosition = tilt * onCircle + Vector3.up * bob;
                }
            }

            private void OnSamplesReceived(IReadOnlyList<TelemetrySample> samples)
            {
                for (var i = 0; i < _metrics.Count; i++)
                {
                    for (var s = 0; s < samples.Count; s++)
                    {
                        if (samples[s].Key != _metrics[i].Key)
                        {
                            continue;
                        }

                        _bubbles[i].ValueText.text = _metrics[i].FormatValueOnly(samples[s].Value);
                        break;
                    }
                }
            }
        }
    }
}
