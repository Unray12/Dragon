using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DragonAR.UI
{
    // Dashboard hien thi so lieu moi truong (humidity/temperature/wind + bieu do lich su)
    // dang the (card) sang mau, bo goc, co bong do nhe - theo anh tham chieu dashboard IoT
    // nguoi dung cung cap (kieu Grafana/smart-home). Du lieu hien dang RANDOM (chua noi cam
    // bien/API thoi tiet that) - xem context/OPEN_QUESTIONS.md.
    //
    // QUAN TRONG - day la World Space Canvas, NEO VAO 1 TOA DO THAT TRONG KHONG GIAN AR
    // (khong phai gan theo camera, khong phai vi tri co dinh tren man hinh) - phai xoay
    // camera huong ve vi tri do moi thay duoc panel, giong 1 vat the AR thuc su. VI TRI BAN
    // DAU duoc tinh 1 LAN DUY NHAT luc spawn de nam BEN CANH con rong (dragon cung gan theo
    // camera luc do) - xem cach goi ham nay trong AppBootstrapper. Sau khi spawn, panel
    // dung yen trong khong gian, khong tu di chuyen theo camera nua. Panel tu xoay quanh
    // truc dung de luon huong mat ve camera (billboard) cho de doc du dung tu goc nao. Keo
    // tha bang ngon tay se CHIEU vi tri cham man hinh xuong 1 mat phang huong ve camera tai
    // vi tri hien tai cua panel, doi VI TRI 3D THAT cua panel - sau khi tha tay, panel dung
    // yen (fix) tai toa do 3D moi do, khong quay lai vi tri cu.
    public static class EnvironmentDashboardOverlay
    {
        private const float PanelWidth = 440f;
        private const float PanelHeight = 480f;
        private const float RootPadding = 16f;
        private const float CardSpacing = 12f;

        // 1 don vi UI (pixel) = bao nhieu met ngoai doi that. 440x480 o scale nay ra panel
        // roughly 0.53m x 0.58m - kich thuoc hop ly cho 1 "man hinh" AR lo lung.
        private const float WorldUnitsPerPixel = 0.0012f;

        private static readonly Color PageBackgroundColor = new(0.933f, 0.949f, 0.965f, 0.42f); // #EEF2F6, trong suot
        private static readonly Color CardBackgroundColor = new(1f, 1f, 1f, 0.55f); // trong suot
        private static readonly Color ShadowColor = new(0f, 0f, 0f, 0.18f);
        private static readonly Color LabelInkColor = new(0.42f, 0.45f, 0.49f, 1f); // #6B7280
        private static readonly Color ValueInkColor = new(0.07f, 0.09f, 0.15f, 1f); // #12172A
        private static readonly Color ChartColor = new(0.165f, 0.471f, 0.843f, 1f); // #2A78D6

        public static void Spawn(Vector3 worldPosition)
        {
            EnsureEventSystem();

            var camera = Camera.main;

            var canvasGo = new GameObject("DashboardCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            canvasGo.transform.localScale = Vector3.one * WorldUnitsPerPixel;

            canvasGo.AddComponent<GraphicRaycaster>();

            var roundedSprite = RoundedRectSpriteFactory.GetShared();

            var rootGo = new GameObject("DashboardRoot", typeof(RectTransform));
            rootGo.transform.SetParent(canvasGo.transform, false);

            var rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootBackground = rootGo.AddComponent<Image>();
            rootBackground.sprite = roundedSprite;
            rootBackground.type = Image.Type.Sliced;
            rootBackground.color = PageBackgroundColor;

            // Handle keo - di chuyen TOA DO THE GIOI THAT cua ca canvas (khong phai
            // anchoredPosition man hinh). Gan tren rootGo (co Image de nhan tia cham).
            var dragHandler = rootGo.AddComponent<DashboardDragHandler>();
            dragHandler.Init(canvasGo.transform, camera);

            var rootLayout = rootGo.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset((int)RootPadding, (int)RootPadding, (int)RootPadding, (int)RootPadding);
            rootLayout.spacing = CardSpacing;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true; // bat buoc = true, neu khong cac phan (title/statsRow/historyCard) giu nguyen kich thuoc RectTransform mac dinh cua Unity thay vi preferredHeight da khai bao, gay chong lan
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            CreateText(rootGo.transform, "ENVIRONMENT", 20, FontStyles.Bold, ValueInkColor, TextAlignmentOptions.Left, preferredHeight: 30f);

            var statsRowGo = new GameObject("StatsRow", typeof(RectTransform));
            statsRowGo.transform.SetParent(rootGo.transform, false);
            var statsRowLayoutElement = statsRowGo.AddComponent<LayoutElement>();
            statsRowLayoutElement.preferredHeight = 130f;
            var statsRowLayout = statsRowGo.AddComponent<HorizontalLayoutGroup>();
            statsRowLayout.spacing = CardSpacing;
            statsRowLayout.childControlWidth = true;
            statsRowLayout.childControlHeight = true;
            statsRowLayout.childForceExpandWidth = true;
            statsRowLayout.childForceExpandHeight = true;

            var humidityValue = CreateStatCard(statsRowGo.transform, roundedSprite, "HUMIDITY");
            var temperatureValue = CreateStatCard(statsRowGo.transform, roundedSprite, "TEMPERATURE");
            var windValue = CreateStatCard(statsRowGo.transform, roundedSprite, "WIND");

            var historyCard = CreateCard(rootGo.transform, roundedSprite);
            var historyLayoutElement = historyCard.AddComponent<LayoutElement>();
            historyLayoutElement.preferredHeight = 190f;
            historyLayoutElement.flexibleWidth = 1f;

            var historyLayout = historyCard.AddComponent<VerticalLayoutGroup>();
            historyLayout.padding = new RectOffset(16, 16, 12, 12);
            historyLayout.spacing = 8f;
            historyLayout.childControlWidth = true;
            historyLayout.childControlHeight = true; // cung ly do o rootLayout - tranh "HISTORY" chong len bieu do
            historyLayout.childForceExpandWidth = true;
            historyLayout.childForceExpandHeight = false;

            CreateText(historyCard.transform, "HISTORY", 16, FontStyles.Bold, LabelInkColor, TextAlignmentOptions.Left, preferredHeight: 24f);

            var graphContainer = new GameObject("GraphContainer", typeof(RectTransform));
            graphContainer.transform.SetParent(historyCard.transform, false);
            var graphLayoutElement = graphContainer.AddComponent<LayoutElement>();
            graphLayoutElement.preferredHeight = 120f;
            graphLayoutElement.flexibleWidth = 1f;

            var bars = CreateBarGraph(graphContainer.transform, barCount: 16);

            var driver = rootGo.AddComponent<DashboardDataDriver>();
            driver.Init(humidityValue, temperatureValue, windValue, bars);

            canvasGo.transform.position = worldPosition;
            canvasGo.AddComponent<BillboardToCamera>();
        }

        // 1 the trang bo goc + bong do nhe, dung chung cho ca stat card va history card.
        private static GameObject CreateCard(Transform parent, Sprite roundedSprite)
        {
            var cardGo = new GameObject("Card", typeof(RectTransform));
            cardGo.transform.SetParent(parent, false);

            var image = cardGo.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = CardBackgroundColor;

            var shadow = cardGo.AddComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = new Vector2(0f, -3f);

            return cardGo;
        }

        // The stat nho (nhan + so lieu to dam), tra ve TextMeshProUGUI de DashboardDataDriver
        // tu cap nhat gia tri.
        private static TextMeshProUGUI CreateStatCard(Transform parent, Sprite roundedSprite, string label)
        {
            var cardGo = CreateCard(parent, roundedSprite);

            var layout = cardGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true; // tranh nhan/gia tri chong len nhau ben trong 1 stat card
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateText(cardGo.transform, label, 13, FontStyles.Normal, LabelInkColor, TextAlignmentOptions.Left, preferredHeight: 18f);
            var valueText = CreateText(cardGo.transform, "--", 30, FontStyles.Bold, ValueInkColor, TextAlignmentOptions.Left, preferredHeight: 40f);

            return valueText;
        }

        private static void EnsureEventSystem()
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
                return;
            }

            // Project dung Input System moi (activeInputHandler=1 trong ProjectSettings) -
            // EventSystem bat buoc phai co InputSystemUIInputModule moi nhan duoc cham man
            // hinh, StandaloneInputModule (cu) se khong hoat dong.
            if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static TextMeshProUGUI CreateText(Transform parent, string initialText, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment, float preferredHeight)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = initialText;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.richText = true;

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;

            return text;
        }

        private static Image[] CreateBarGraph(Transform parent, int barCount)
        {
            var layout = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var bars = new Image[barCount];
            for (var i = 0; i < barCount; i++)
            {
                var barGo = new GameObject($"Bar_{i}", typeof(RectTransform));
                barGo.transform.SetParent(parent, false);

                var rect = barGo.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0f); // moc duoi, de bar "moc len" tu day khi tang chieu cao
                rect.sizeDelta = new Vector2(0f, 10f);

                var image = barGo.AddComponent<Image>();
                image.color = ChartColor;

                bars[i] = image;
            }

            return bars;
        }

        // Random hoa so lieu hien thi + cap nhat bieu do cot moi ~1.5s. CHI la du lieu gia
        // de demo layout - chua noi vao cam bien/API that nao.
        private sealed class DashboardDataDriver : MonoBehaviour
        {
            private const float UpdateIntervalSeconds = 1.5f;
            private const float MaxBarHeight = 110f;

            private TextMeshProUGUI _humidityText;
            private TextMeshProUGUI _temperatureText;
            private TextMeshProUGUI _windText;
            private Image[] _bars;
            private readonly List<float> _history = new();
            private float _timer;

            public void Init(TextMeshProUGUI humidityText, TextMeshProUGUI temperatureText, TextMeshProUGUI windText, Image[] bars)
            {
                _humidityText = humidityText;
                _temperatureText = temperatureText;
                _windText = windText;
                _bars = bars;

                for (var i = 0; i < _bars.Length; i++)
                {
                    _history.Add(Random.Range(0.2f, 0.6f));
                }

                RefreshValues();
                RefreshGraph();
            }

            private void Update()
            {
                // Bao ve neu Init() chua kip chay truoc Update() dau tien (vi du do
                // recompile/domain-reload xen giua luc dang Play) - tranh
                // ArgumentOutOfRangeException tren RemoveAt(0) khi list rong.
                if (_history.Count == 0)
                {
                    return;
                }

                _timer += Time.deltaTime;
                if (_timer < UpdateIntervalSeconds)
                {
                    return;
                }

                _timer = 0f;
                RefreshValues();

                _history.RemoveAt(0);
                _history.Add(Random.Range(0.1f, 1f));
                RefreshGraph();
            }

            private void RefreshValues()
            {
                _humidityText.text = $"{Random.Range(30, 96)}%";
                _temperatureText.text = $"{Random.Range(18, 39)}C";
                _windText.text = $"{Random.Range(0, 46)} km/h";
            }

            private void RefreshGraph()
            {
                for (var i = 0; i < _bars.Length; i++)
                {
                    var rect = _bars[i].rectTransform;
                    var size = rect.sizeDelta;
                    size.y = Mathf.Max(6f, _history[i] * MaxBarHeight);
                    rect.sizeDelta = size;
                }
            }
        }

        // Keo tha bang ngon tay -> doi VI TRI THE GIOI THAT (transform.position cua
        // canvas), khong phai vi tri tren man hinh. Cach lam: ban 1 tia tu camera qua diem
        // dang cham, giao voi 1 mat phang huong ve camera di qua vi tri HIEN TAI cua canvas
        // - lay diem giao do lam vi tri moi. Sau khi tha tay, panel dung yen (fix) dung tai
        // toa do 3D moi vua tinh duoc.
        private sealed class DashboardDragHandler : MonoBehaviour, IDragHandler
        {
            private Transform _worldTransform;
            private Camera _fallbackCamera;

            public void Init(Transform worldTransform, Camera fallbackCamera)
            {
                _worldTransform = worldTransform;
                _fallbackCamera = fallbackCamera;
            }

            public void OnDrag(PointerEventData eventData)
            {
                var cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : _fallbackCamera;
                if (cam == null || _worldTransform == null)
                {
                    return;
                }

                var ray = cam.ScreenPointToRay(eventData.position);
                var plane = new Plane(-cam.transform.forward, _worldTransform.position);
                if (plane.Raycast(ray, out var distanceAlongRay))
                {
                    _worldTransform.position = ray.GetPoint(distanceAlongRay);
                }
            }
        }

        // Luon xoay panel ve phia camera (chi quanh truc dung) de doc duoc du nguoi dung
        // dung o goc nao quanh no - giong 1 man hinh AR lo lung thuc su.
        private sealed class BillboardToCamera : MonoBehaviour
        {
            private void LateUpdate()
            {
                var camera = Camera.main;
                if (camera == null)
                {
                    return;
                }

                // World Space Canvas hien noi dung khi nguoi xem dung o phia -Z nhin ve +Z
                // (khong phai nguoc lai) - dung "huong ra xa camera" (tu camera toi panel,
                // keo dai them) lam forward, KHONG PHAI "huong ve camera", neu khong chu se
                // bi lat guong (da xac nhan qua anh chup thuc te: "ENVIRONMENT" hien thanh
                // "TNEMNORIVNE").
                var directionAwayFromCamera = transform.position - camera.transform.position;
                directionAwayFromCamera.y = 0f;
                if (directionAwayFromCamera.sqrMagnitude < 0.0001f)
                {
                    return;
                }

                transform.rotation = Quaternion.LookRotation(directionAwayFromCamera.normalized, Vector3.up);
            }
        }
    }
}
