using UnityEngine;
using UnityEngine.Rendering;

namespace CourtRunner15
{
    /// <summary>
    /// Строит окружение уровня: свет/туман/палитру, повторяющиеся сегменты
    /// (офис, улица, суд) и дальний задник-«портал».
    /// </summary>
    public class LevelVisualBuilder : MonoBehaviour
    {
        private GameManager _gm;
        private GameObject _backdrop;

        public void Init(GameManager gm)
        {
            _gm = gm;
        }

        // ------------------------------------------------------------------
        // Глобальный вид
        // ------------------------------------------------------------------

        public void ApplyGlobalLook(LevelData data, Light keyLight, Camera cam)
        {
            LevelPalette p = data.Palette;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = p.FogColor;
            RenderSettings.fogStartDistance = p.FogStart;
            RenderSettings.fogEndDistance = p.FogEnd;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = p.Ambient;

            if (keyLight != null)
            {
                keyLight.color = p.KeyLight;
                keyLight.intensity = p.Indoor ? 1.16f : 1.28f;
                keyLight.shadowStrength = p.Indoor ? 0.46f : 0.40f;
                keyLight.transform.rotation = Quaternion.Euler(50f, -34f, 0f);
            }
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = p.Sky;
                cam.farClipPlane = 260f;
            }

            BuildBackdrop(data);
        }

        /// <summary>Дальняя арка-портал и силуэты — не скроллятся.</summary>
        public void BuildBackdrop(LevelData data)
        {
            if (_backdrop != null) Destroy(_backdrop);
            _backdrop = new GameObject("Backdrop");
            _backdrop.transform.SetParent(transform, false);
            Transform root = _backdrop.transform;
            LevelPalette p = data.Palette;

            float archZ = p.FogEnd * 0.88f;
            Material towerMat = MaterialFactory.Solid(p.Wall * 0.85f, 0.5f, 0.05f);
            Material goldMat = MaterialFactory.Solid(new Color(0.88f, 0.72f, 0.35f), 0.8f, 0.7f);

            // Башни-опоры по сторонам.
            ProceduralModelFactory.Part(root, PrimitiveType.Cube,
                new Vector3(-7.5f, 4.5f, archZ), new Vector3(2.2f, 9f, 2.2f), towerMat, "towerL");
            ProceduralModelFactory.Part(root, PrimitiveType.Cube,
                new Vector3(7.5f, 4.5f, archZ), new Vector3(2.2f, 9f, 2.2f), towerMat, "towerR");
            // Перекладина.
            ProceduralModelFactory.Part(root, PrimitiveType.Cube,
                new Vector3(0f, 8.2f, archZ), new Vector3(17.2f, 1.6f, 1.8f), towerMat, "beam");
            ProceduralModelFactory.Part(root, PrimitiveType.Cube,
                new Vector3(0f, 7.35f, archZ), new Vector3(17.2f, 0.14f, 1.9f), goldMat, "trim");

            string gateText = data.Theme == LevelTheme.Office ? "К ЗАЛУ СУДА →"
                : (data.Theme == LevelTheme.Street ? "РОСРЕЕСТР" : "ЗАЛ №3 · ЗАСЕДАНИЕ");
            ProceduralModelFactory.SignBoard(root, gateText, new Vector3(0f, 8.2f, archZ - 1.0f),
                new Vector2(9f, 1.2f), p.SignBg, p.SignText, 0f, true);

            if (data.Theme == LevelTheme.Street)
            {
                ProceduralModelFactory.CreateCourtDestinationFacade(root,
                    new Vector3(0f, 0f, archZ + 1.9f), 0f, 1.65f);

                // Дальний силуэт города.
                System.Random rng = new System.Random(777);
                for (int i = 0; i < 8; i++)
                {
                    float x = -32f + i * 9f + (float)rng.NextDouble() * 3f;
                    float h = 10f + (float)rng.NextDouble() * 14f;
                    ProceduralModelFactory.CreateBuilding(root,
                        new Vector3(x, 0f, archZ + 22f + (float)rng.NextDouble() * 14f),
                        new Vector3(7f, h, 7f), p.Wall * 0.8f, new Color(1f, 0.9f, 0.6f), 100 + i);
                }
                // Кластер Москва-Сити справа (как в референсе), одна башня —
                // золотой «Меркурий». Ближе fogEnd, иначе туман съест силуэты.
                Color glassCool = new Color(0.30f, 0.36f, 0.46f);
                Color glassGold = new Color(0.66f, 0.44f, 0.20f);
                Color windowGlow = new Color(1f, 0.82f, 0.5f);
                float cityZ = archZ - 22f;
                ProceduralModelFactory.CreateGlassTower(root, new Vector3(24f, 0f, cityZ), 6.5f, 30f, glassCool, windowGlow, 501);
                ProceduralModelFactory.CreateGlassTower(root, new Vector3(31f, 0f, cityZ + 8f), 7.5f, 38f, glassCool * 0.9f, windowGlow, 502);
                ProceduralModelFactory.CreateGlassTower(root, new Vector3(38f, 0f, cityZ + 2f), 6f, 33f, glassGold, new Color(1f, 0.75f, 0.35f), 503);
                ProceduralModelFactory.CreateGlassTower(root, new Vector3(45f, 0f, cityZ + 10f), 7f, 27f, glassCool * 1.1f, windowGlow, 504);
                ProceduralModelFactory.CreateGlassTower(root, new Vector3(19f, 0f, cityZ + 14f), 5f, 22f, glassCool, windowGlow, 505);
                // Сталинская высотка слева — второй узнаваемый силуэт Москвы.
                ProceduralModelFactory.CreateStalinTower(root, new Vector3(-27f, 0f, cityZ + 4f), 2.2f);
                ProceduralModelFactory.SignBoard(root, "Б. Тульская, 17 →",
                    new Vector3(0f, 6.4f, archZ - 1.1f), new Vector2(5.0f, 0.85f),
                    new Color(0.07f, 0.16f, 0.34f), new Color(1f, 0.88f, 0.58f), 0f, true);
                // Портал = вход в суд: рамки-металлодетекторы и флаг.
                ProceduralModelFactory.CreateMetalDetector(root, new Vector3(-2.2f, 0f, archZ - 3f), 0f);
                ProceduralModelFactory.CreateMetalDetector(root, new Vector3(2.2f, 0f, archZ - 3f), 0f);
                ProceduralModelFactory.CreateFlagPole(root, new Vector3(-6.2f, 0f, archZ - 2f), 7f);
            }

            if (data.Theme == LevelTheme.Court)
            {
                StaticIllustrationFactory.CreateUnframedBackdrop(root, "CourtQueue_Hall",
                    new Vector2(16f, 8f), new Vector3(0f, 4.75f, archZ + 1.9f));

                // Герб над порталом без текстовой вывески суда.
                ProceduralModelFactory.CreateCourtEmblem(root,
                    new Vector3(0f, 10.6f, archZ), 0f, 1.6f);
                ProceduralModelFactory.Part(root, PrimitiveType.Cube,
                    new Vector3(0f, 6.55f, archZ - 1.0f), new Vector3(10.5f, 0.42f, 0.16f),
                    MaterialFactory.Solid(new Color(0.12f, 0.13f, 0.16f), 0.55f, 0.08f), "blankCourtNameplate");
                // Ряд колонн у входа — строгий фасад под козырьком.
                for (int i = -2; i <= 2; i++)
                {
                    if (i == 0) continue;
                    ProceduralModelFactory.CreateColumn(root,
                        new Vector3(i * 3.4f, 0f, archZ - 2.2f), 7.2f);
                }
            }
        }

        // ------------------------------------------------------------------
        // Сегменты
        // ------------------------------------------------------------------

        public GameObject BuildSegment(LevelData data, int variant, float length)
        {
            GameObject seg = new GameObject("Segment_" + variant);
            Transform t = seg.transform;
            System.Random rng = new System.Random(variant * 7919 + data.Index * 131 + 17);

            switch (data.Theme)
            {
                case LevelTheme.Street: BuildStreetSegment(data, t, rng, variant, length); break;
                case LevelTheme.Court: BuildCourtSegment(data, t, rng, variant, length); break;
                default: BuildOfficeSegment(data, t, rng, variant, length); break;
            }
            return seg;
        }

        private float Rnd(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private void BuildFloor(Transform t, LevelPalette p, float width, float length, Material floorMat)
        {
            GameObject floor = ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                new Vector3(0f, -0.15f, length * 0.5f), new Vector3(width, 0.3f, length + 0.06f),
                floorMat, "floor");
            floor.isStatic = false;
        }

        private void BuildLightPool(Transform t, Vector3 pos, Vector2 size, Color color, float alpha)
        {
            // Аддитивное мягкое пятно: читается как отражение лампы в глянце пола
            // и цветёт в bloom. Альфа = яркость.
            Color c = new Color(color.r, color.g, color.b, alpha);
            GameObject pool = ProceduralModelFactory.PartRot(t, PrimitiveType.Quad, pos,
                new Vector3(size.x, size.y, 1f), new Vector3(90f, 0f, 0f),
                MaterialFactory.AdditiveGlow(c), "lightPool");
            ProceduralModelFactory.NoShadow(pool);
        }

        private void BuildInsetStrip(Transform t, float x, float z, Vector3 size, Material mat, string name)
        {
            GameObject strip = ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                new Vector3(x, 0.018f, z), size, mat, name);
            ProceduralModelFactory.NoShadow(strip);
        }

        private void BuildLaneLines(Transform t, LevelPalette p, float length, bool dashed)
        {
            Material lineMat = MaterialFactory.Emissive(p.LaneLine * 0.75f, p.LaneLine, 0.9f);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = 1.2f * side;
                if (dashed)
                {
                    int dashes = Mathf.FloorToInt(length / 5f);
                    for (int i = 0; i < dashes; i++)
                    {
                        GameObject d = ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                            new Vector3(x, 0.012f, 2.5f + i * 5f), new Vector3(0.1f, 0.02f, 1.7f),
                            lineMat, "dash");
                        ProceduralModelFactory.NoShadow(d);
                    }
                }
                else
                {
                    GameObject line = ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                        new Vector3(x, 0.012f, length * 0.5f), new Vector3(0.07f, 0.02f, length),
                        lineMat, "line");
                    ProceduralModelFactory.NoShadow(line);
                }
            }
            // Светлые кромки трассы.
            Material edgeMat = MaterialFactory.Emissive(p.FloorAccent * 0.6f, p.FloorAccent, 0.55f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject e = ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                    new Vector3(3.7f * side, 0.012f, length * 0.5f), new Vector3(0.12f, 0.02f, length),
                    edgeMat, "edge");
                ProceduralModelFactory.NoShadow(e);
            }
        }

        private void PlaceWallSigns(LevelData data, Transform t, System.Random rng, float length,
            float wallX, float y, float scale)
        {
            int count = 1 + rng.Next(2);
            for (int i = 0; i < count; i++)
            {
                string text = data.WallSigns[rng.Next(data.WallSigns.Length)];
                float side = rng.Next(2) == 0 ? -1f : 1f;
                float z = Rnd(rng, 4f, length - 4f);
                float rotY = side > 0f ? 90f : -90f;
                ProceduralModelFactory.SignBoard(t, text,
                    new Vector3(wallX * side, y, z),
                    new Vector2(3.4f * scale, 0.75f * scale), data.Palette.SignBg,
                    data.Palette.SignText, rotY, true);
            }
        }

        // ---------------- ОФИС ----------------

        private void BuildOfficeSegment(LevelData data, Transform t, System.Random rng,
            int variant, float length)
        {
            LevelPalette p = data.Palette;
            BuildFloor(t, p, 11.2f, length,
                MaterialFactory.StylizedTile(p.Floor, p.Floor * 0.62f, p.FloorAccent * 0.55f, 9, 0.88f, 0.10f));
            BuildLaneLines(t, p, length, false);

            Material wallMat = MaterialFactory.Solid(p.Wall, 0.45f, 0f);
            Material woodMat = MaterialFactory.Solid(p.WallAccent, 0.55f, 0.05f);
            Material ceilMat = MaterialFactory.Solid(p.Ceiling, 0.4f, 0f);

            for (int side = -1; side <= 1; side += 2)
            {
                ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                    new Vector3(5.65f * side, 2.1f, length * 0.5f),
                    new Vector3(0.25f, 4.2f, length), wallMat, "wall");
                ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                    new Vector3(5.55f * side, 0.45f, length * 0.5f),
                    new Vector3(0.1f, 0.9f, length), woodMat, "wainscot");
            }
            ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                new Vector3(0f, 4.28f, length * 0.5f), new Vector3(11.4f, 0.16f, length),
                ceilMat, "ceiling");

            // Потолочные панели света + отблеск на полу.
            for (int i = 0; i < 4; i++)
            {
                float z = 3.75f + i * 7.5f;
                ProceduralModelFactory.CreateCeilingLamp(t,
                    new Vector3(0f, 4.18f, z), new Vector2(2.4f, 1.0f),
                    new Color(1f, 0.94f, 0.78f));
                BuildLightPool(t, new Vector3(0f, 0.026f, z), new Vector2(4.6f, 6.2f),
                    new Color(1f, 0.86f, 0.50f), 0.34f);
            }

            // Столы, принтеры, растения, переговорки по сторонам.
            for (int side = -1; side <= 1; side += 2)
            {
                float sx = 4.55f * side;
                int props = 3 + rng.Next(2);
                for (int i = 0; i < props; i++)
                {
                    float z = Rnd(rng, 2.5f, length - 2.5f);
                    int kind = rng.Next(5);
                    if (kind <= 1)
                    {
                        ProceduralModelFactory.CreateDesk(t, new Vector3(sx, 0f, z),
                            side > 0 ? -90f : 90f, true);
                    }
                    else if (kind == 2)
                    {
                        ProceduralModelFactory.CreatePrinter(t, new Vector3(sx, 0f, z),
                            side > 0 ? -90f : 90f);
                    }
                    else if (kind == 3)
                    {
                        ProceduralModelFactory.CreatePlant(t, new Vector3(sx, 0f, z), Rnd(rng, 0.8f, 1.2f));
                    }
                    else
                    {
                        ProceduralModelFactory.CreateGlassWall(t, new Vector3(5.3f * side, 0f, z),
                            new Vector2(3.2f, 2.6f), side > 0 ? 90f : -90f);
                    }
                }
            }

            // Панорамные окна с фотореалистичным видом на Москва-Сити.
            // Корень чуть внутри коридора, иначе панель утонет в коробе стены.
            for (int side = -1; side <= 1; side += 2)
            {
                float rotY = side > 0 ? 90f : -90f;
                int sideIndex = side > 0 ? 1 : 0;
                float firstWidth = 5.35f + ((variant + sideIndex) % 2) * 0.45f;
                float secondWidth = 4.65f + ((variant + sideIndex + 1) % 2) * 0.50f;
                StaticIllustrationFactory.CreateMaskedWindowView(t, "MoscowCity_Panorama",
                    new Vector2(firstWidth, 2.65f),
                    new Vector3(5.45f * side, 2.45f, 6.8f + sideIndex * 0.55f),
                    rotY, variant * 31 + sideIndex);
                StaticIllustrationFactory.CreateMaskedWindowView(t, "MoscowCity_Panorama",
                    new Vector2(secondWidth, 2.45f),
                    new Vector3(5.45f * side, 2.38f, 20.6f - sideIndex * 0.45f),
                    rotY, variant * 31 + sideIndex + 17);
            }

            // Ролл-ап КонсультантПлюс у стены — примета любого юротдела.
            if (variant % 3 == 1)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                ProceduralModelFactory.CreateConsultantStand(t,
                    new Vector3(4.95f * side, 0f, Rnd(rng, 3f, length - 3f)),
                    side > 0 ? 90f : -90f);
            }

            PlaceWallSigns(data, t, rng, length, 5.5f, 2.9f, 1f);

            // Подвесной баннер поперёк коридора удалён: игрок видел его
            // пустой тыльной стороной, и у камеры он заполнял весь кадр.
        }

        // ---------------- УЛИЦА ----------------

        private void BuildStreetSegment(LevelData data, Transform t, System.Random rng,
            int variant, float length)
        {
            LevelPalette p = data.Palette;
            // Мокрый глянцевый асфальт под закатным светом, как на референсе.
            BuildFloor(t, p, 16f, length,
                MaterialFactory.StylizedTile(p.Floor, p.Floor * 0.55f, new Color(0.30f, 0.24f, 0.20f), 12, 0.90f, 0.14f));
            BuildLaneLines(t, p, length, true);

            // Тротуары и бордюры.
            Material sidewalkMat = MaterialFactory.Solid(new Color(0.55f, 0.55f, 0.53f), 0.4f, 0f);
            Material curbMat = MaterialFactory.Solid(new Color(0.7f, 0.7f, 0.68f), 0.45f, 0f);
            for (int side = -1; side <= 1; side += 2)
            {
                ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                    new Vector3(6.7f * side, 0.07f, length * 0.5f),
                    new Vector3(4.6f, 0.16f, length), sidewalkMat, "sidewalk");
                ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                    new Vector3(4.45f * side, 0.06f, length * 0.5f),
                    new Vector3(0.18f, 0.14f, length), curbMat, "curb");
            }

            // Фотореалистичный фасад как плоскость на тротуаре: виден сразу,
            // а low-poly город остаётся лёгкой обвязкой вокруг него.
            // Identity comes from procedural entrances and the far portal, not sidewalk photo billboards.

            // Пешеходный переход в начале сегмента.
            if (variant % 3 == 0)
            {
                Material zebra = MaterialFactory.Solid(p.FloorAccent, 0.5f, 0f);
                for (int i = 0; i < 6; i++)
                {
                    GameObject stripe = ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                        new Vector3(-3f + i * 1.2f, 0.011f, 1.6f),
                        new Vector3(0.7f, 0.02f, 2.6f), zebra, "zebra" + i);
                    ProceduralModelFactory.NoShadow(stripe);
                }
            }

            // Лужи на асфальте: отражают закатное небо тёплым зеркалом.
            int puddles = rng.Next(3);
            for (int i = 0; i < puddles; i++)
            {
                float px = Rnd(rng, -5.5f, 5.5f);
                float pz = Rnd(rng, 3f, length - 3f);
                GameObject puddle = ProceduralModelFactory.Part(t, PrimitiveType.Cylinder,
                    new Vector3(px, 0.012f, pz),
                    new Vector3(Rnd(rng, 0.8f, 1.6f), 0.01f, Rnd(rng, 0.6f, 1.1f)),
                    MaterialFactory.Solid(new Color(0.82f, 0.58f, 0.36f), 0.97f, 0.45f), "puddle");
                ProceduralModelFactory.NoShadow(puddle);
                BuildLightPool(t, new Vector3(px, 0.024f, pz), new Vector2(1.9f, 1.5f),
                    new Color(1f, 0.72f, 0.40f), 0.22f);
            }

            // Здания.
            for (int side = -1; side <= 1; side += 2)
            {
                float z = 2f;
                while (z < length - 6f)
                {
                    float h = Rnd(rng, 7f, 14f);
                    float d = Rnd(rng, 6f, 9f);
                    ProceduralModelFactory.CreateBuilding(t,
                        new Vector3(Rnd(rng, 13.8f, 15.8f) * side, 0f, z + d * 0.5f),
                        new Vector3(Rnd(rng, 4.5f, 6.3f), h, d), p.Wall,
                        new Color(1f, 0.92f, 0.65f), variant * 10 + (int)z + (side + 1) * 50);
                    z += d + Rnd(rng, 1.5f, 4f);
                }
            }

            // Фонари.
            for (int i = 0; i < 2; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = 6f + i * 15f;
                ProceduralModelFactory.CreateStreetLight(t,
                    new Vector3(5.6f * side, 0f, z), side > 0 ? 180f : 0f);
                BuildLightPool(t, new Vector3(4.3f * side, 0.026f, z - 1.2f), new Vector2(4.0f, 5.6f),
                    new Color(1f, 0.78f, 0.40f), 0.36f);
            }

            // Узнаваемые фасады по бокам: Росреестр с названием и безымянное правительственное здание.
            float nearFacadeZ = Mathf.Min(9.0f, length - 9f);
            float farFacadeZ = Mathf.Min(20.0f, length - 6f);
            int facadeLayout = ((variant % 4) + 4) % 4;
            if (facadeLayout == 0)
            {
                ProceduralModelFactory.CreateRosreestrCityFacade(t,
                    new Vector3(-8.95f, 0f, nearFacadeZ), -90f);
                ProceduralModelFactory.CreateGovernmentCityFacade(t,
                    new Vector3(9.05f, 0f, farFacadeZ), 90f);
            }
            else if (facadeLayout == 1)
            {
                ProceduralModelFactory.CreateGovernmentCityFacade(t,
                    new Vector3(-9.05f, 0f, nearFacadeZ + 1.0f), -90f);
                ProceduralModelFactory.CreateRosreestrCityFacade(t,
                    new Vector3(8.95f, 0f, farFacadeZ), 90f);
            }
            else if (facadeLayout == 2)
            {
                ProceduralModelFactory.CreateRosreestrCityFacade(t,
                    new Vector3(-8.95f, 0f, farFacadeZ), -90f);
                ProceduralModelFactory.CreateRosreestrCityFacade(t,
                    new Vector3(8.95f, 0f, nearFacadeZ), 90f);
            }
            else
            {
                ProceduralModelFactory.CreateGovernmentCityFacade(t,
                    new Vector3(-9.05f, 0f, farFacadeZ), -90f);
                ProceduralModelFactory.CreateRosreestrCityFacade(t,
                    new Vector3(8.95f, 0f, nearFacadeZ), 90f);
            }
            ProceduralModelFactory.CreateStreetTree(t, new Vector3(-6.6f, 0f, nearFacadeZ - 2.8f), 1.05f, variant * 31 + 1);
            ProceduralModelFactory.CreateStreetTree(t, new Vector3(6.6f, 0f, farFacadeZ + 2.2f), 1.10f, variant * 31 + 2);

            // Уличные декорации: киоски, фургон, барьеры, прохожие.
            if (variant % 3 == 1)
            {
                string[] kioskSigns = { "ВЫПИСКИ", "ПЕЧАТЬ 24/7", "КонсультантПлюс" };
                ProceduralModelFactory.CreateKiosk(t, new Vector3(7.6f, 0f, Rnd(rng, 6f, length - 6f)),
                    180f, kioskSigns[rng.Next(kioskSigns.Length)]);
            }
            if (variant % 4 == 2)
            {
                ProceduralModelFactory.CreateMediaVan(t,
                    new Vector3(-7.4f, 0f, Rnd(rng, 8f, length - 8f)), Rnd(rng, -12f, 12f));
            }
            int people = rng.Next(3);
            for (int i = 0; i < people; i++)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                ProceduralModelFactory.CreatePerson(t,
                    new Vector3(Rnd(rng, 5.5f, 7.5f) * side, 0.15f, Rnd(rng, 2f, length - 2f)),
                    new Color(Rnd(rng, 0.2f, 0.7f), Rnd(rng, 0.2f, 0.6f), Rnd(rng, 0.3f, 0.7f)),
                    new Color(0.2f, 0.2f, 0.25f), Rnd(rng, 1.5f, 1.8f), Rnd(rng, 0f, 360f));
            }

            // Вывески на зданиях.
            PlaceWallSigns(data, t, rng, length, 9.0f, Rnd(rng, 5f, 8f), 1.5f);
        }

        // ---------------- СУД ----------------

        private void BuildCourtSegment(LevelData data, Transform t, System.Random rng,
            int variant, float length)
        {
            LevelPalette p = data.Palette;
            // Полированный мрамор в шахматку, как в референсном холле.
            BuildFloor(t, p, 13f, length,
                MaterialFactory.StylizedTile(p.Floor, p.Floor * 0.42f, new Color(0.22f, 0.22f, 0.26f), 10, 0.92f, 0.14f));

            // Красная дорожка с золотыми кромками.
            GameObject carpet = ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                new Vector3(0f, 0.011f, length * 0.5f), new Vector3(7.5f, 0.02f, length),
                MaterialFactory.StylizedTile(p.FloorAccent, p.FloorAccent * 0.45f,
                    new Color(1f, 0.70f, 0.22f), 12, 0.42f, 0f), "carpet");
            ProceduralModelFactory.NoShadow(carpet);
            Material carpetShade = MaterialFactory.UnlitTransparent(new Color(0.18f, 0f, 0f, 0.10f));
            for (int i = 0; i < 6; i++)
            {
                BuildInsetStrip(t, 0f, 2.5f + i * 5f, new Vector3(7.1f, 0.018f, 0.09f),
                    carpetShade, "carpetBand");
            }
            Material goldTrim = MaterialFactory.Emissive(new Color(0.85f, 0.7f, 0.35f) * 0.8f,
                new Color(1f, 0.85f, 0.45f), 0.7f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject trim = ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                    new Vector3(3.8f * side, 0.016f, length * 0.5f),
                    new Vector3(0.14f, 0.02f, length), goldTrim, "trim");
                ProceduralModelFactory.NoShadow(trim);
            }
            BuildLaneLines(t, p, length, false);

            // Деревянные стены с мраморной верхней частью.
            Material woodMat = MaterialFactory.Solid(p.Wall, 0.6f, 0.05f);
            Material marbleMat = MaterialFactory.Solid(p.WallAccent, 0.7f, 0.05f);
            Material wallGold = MaterialFactory.Emissive(new Color(0.75f, 0.55f, 0.22f) * 0.7f,
                new Color(1f, 0.76f, 0.26f), 0.55f);
            for (int side = -1; side <= 1; side += 2)
            {
                ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                    new Vector3(6.6f * side, 1.4f, length * 0.5f),
                    new Vector3(0.3f, 2.8f, length), woodMat, "wallWood");
                ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                    new Vector3(6.6f * side, 4.05f, length * 0.5f),
                    new Vector3(0.3f, 2.5f, length), marbleMat, "wallMarble");
                for (int i = 0; i < 4; i++)
                {
                    float z = 3.5f + i * 7.5f;
                    GameObject trim = ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                        new Vector3(6.43f * side, 3.02f, z),
                        new Vector3(0.08f, 3.6f, 0.08f), wallGold, "wallGold");
                    ProceduralModelFactory.NoShadow(trim);
                }
            }
            ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                new Vector3(0f, 5.4f, length * 0.5f), new Vector3(13.4f, 0.2f, length),
                MaterialFactory.Solid(p.Ceiling, 0.4f, 0f), "ceiling");
            for (int i = 0; i < 4; i++)
            {
                float z = 2.5f + i * 7.5f;
                GameObject ceilingRib = ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                    new Vector3(0f, 5.27f, z), new Vector3(13.2f, 0.08f, 0.16f),
                    wallGold, "ceilingRib");
                ProceduralModelFactory.NoShadow(ceilingRib);
            }

            // Колонны.
            for (int i = 0; i < 4; i++)
            {
                float z = 3.5f + i * 7.5f;
                ProceduralModelFactory.CreateColumn(t, new Vector3(-5.9f, 0f, z), 5.3f);
                ProceduralModelFactory.CreateColumn(t, new Vector3(5.9f, 0f, z), 5.3f);
            }

            // Фото-панели как богатые задники, low-poly очередь/стойки остаются впереди.
            if (variant % 2 == 0)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float z = side > 0 ? 9f : 21f;
                    if (side > 0)
                    {
                        ProceduralModelFactory.CreateScheduleBoard(t,
                            new Vector3(6.35f * side, 0f, z), 90f);
                    }
                    else
                    {
                        ProceduralModelFactory.CreateChanceryWindow(t,
                            new Vector3(6.35f * side, 0f, z), -90f);
                    }
                }
            }

            // Люстры: тёплые лампы.
            for (int i = 0; i < 3; i++)
            {
                float z = 5f + i * 10f;
                GameObject bulb = ProceduralModelFactory.Part(t, PrimitiveType.Sphere,
                    new Vector3(0f, 4.6f, z), new Vector3(0.55f, 0.55f, 0.55f),
                    MaterialFactory.Neon(new Color(1f, 0.85f, 0.55f)), "chandelier");
                ProceduralModelFactory.NoShadow(bulb);
                ProceduralModelFactory.Part(t, PrimitiveType.Cylinder,
                    new Vector3(0f, 5.1f, z), new Vector3(0.05f, 0.45f, 0.05f),
                    MaterialFactory.Solid(new Color(0.3f, 0.25f, 0.18f), 0.5f, 0.3f), "chain");
                BuildLightPool(t, new Vector3(0f, 0.035f, z), new Vector2(3.6f, 2.6f),
                    new Color(1f, 0.85f, 0.55f), 0.30f);
                BuildLightPool(t, new Vector3(0f, 0.028f, z), new Vector2(5.6f, 7.0f),
                    new Color(1f, 0.74f, 0.33f), 0.38f);
            }

            // Скамьи, двери, стойки, декор — узнаваемый арбитражный набор.
            string[] doorLabels = { "Зал №1", "Зал №2", "Зал №3", "Канцелярия", "Картотека", "Судебные акты" };
            for (int side = -1; side <= 1; side += 2)
            {
                if (rng.Next(2) == 0)
                {
                    ProceduralModelFactory.CreateBench(t,
                        new Vector3(5.6f * side, 0f, Rnd(rng, 3f, length - 3f)),
                        side > 0 ? -90f : 90f);
                }
                if (rng.Next(2) == 0)
                {
                    ProceduralModelFactory.CreateCourtDoor(t,
                        new Vector3(6.4f * side, 0f, Rnd(rng, 4f, length - 4f)),
                        side > 0 ? 90f : -90f, doorLabels[rng.Next(doorLabels.Length)]);
                }
            }
            if (variant % 3 == 0)
            {
                ProceduralModelFactory.CreateStanchions(t,
                    new Vector3(rng.Next(2) == 0 ? -4.6f : 4.6f, 0f, Rnd(rng, 3f, length * 0.5f)),
                    0f, 3);
            }
            if (variant % 3 == 1)
            {
                ProceduralModelFactory.CreateScheduleBoard(t,
                    new Vector3(6.35f * (rng.Next(2) == 0 ? -1f : 1f), 0f, Rnd(rng, 5f, length - 5f)),
                    rng.Next(2) == 0 ? 90f : -90f);
            }
            if (variant % 4 == 2)
            {
                ProceduralModelFactory.CreateReceptionDesk(t,
                    new Vector3(5.3f * (rng.Next(2) == 0 ? -1f : 1f), 0f, Rnd(rng, 6f, length - 6f)),
                    rng.Next(2) == 0 ? 90f : -90f);
            }
            // Табло картотеки дел у стены.
            if (variant % 3 == 2)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                ProceduralModelFactory.CreateInfoTerminal(t,
                    new Vector3(5.7f * side, 0f, Rnd(rng, 4f, length - 4f)),
                    side > 0 ? 90f : -90f);
            }
            // Пропускная зона со стойкой и турникетом.
            if (variant % 5 == 0)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                ProceduralModelFactory.CreateSecurityPost(t,
                    new Vector3(5.2f * side, 0f, Rnd(rng, 6f, length - 6f)),
                    side > 0 ? 90f : -90f);
            }
            // Окно канцелярии с «Перерыв до 14:00».
            if (variant % 5 == 3)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                ProceduralModelFactory.CreateChanceryWindow(t,
                    new Vector3(6.35f * side, 0f, Rnd(rng, 5f, length - 5f)),
                    side > 0 ? 90f : -90f);
            }
            // Ролл-ап КонсультантПлюс у зала ознакомления.
            if (variant % 5 == 4)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                ProceduralModelFactory.CreateConsultantStand(t,
                    new Vector3(5.9f * side, 0f, Rnd(rng, 4f, length - 4f)),
                    side > 0 ? 90f : -90f);
            }
            // Судебная бытовуха: очередь с папками вдоль стены…
            if (variant % 2 == 0)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                ProceduralModelFactory.CreateQueueLine(t,
                    new Vector3(5.45f * side, 0f, Rnd(rng, 4f, length - 7f)),
                    90f, 3 + rng.Next(3), variant * 31 + (side > 0 ? 9 : 5));
            }
            // …стопки дел у стен…
            if (variant % 2 == 1)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                ProceduralModelFactory.CreatePaperStacks(t,
                    new Vector3(5.7f * side, 0f, Rnd(rng, 3f, length - 3f)),
                    variant * 17 + 3);
            }
            // …и расписание дел ещё чаще.
            if (variant % 4 == 0)
            {
                ProceduralModelFactory.CreateScheduleBoard(t,
                    new Vector3(6.35f * (rng.Next(2) == 0 ? -1f : 1f), 0f,
                        Rnd(rng, 5f, length - 5f)),
                    rng.Next(2) == 0 ? 90f : -90f);
            }
            // Герб на стене между золотыми пилястрами.
            if (variant % 4 == 1)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                ProceduralModelFactory.CreateCourtEmblem(t,
                    new Vector3(6.4f * side, 3.4f, Rnd(rng, 6f, length - 6f)),
                    side > 0 ? 90f : -90f, 0.85f);
            }
            // Люди в коридоре (ожидающие).
            int people = rng.Next(3);
            for (int i = 0; i < people; i++)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                ProceduralModelFactory.CreatePerson(t,
                    new Vector3(Rnd(rng, 4.8f, 5.8f) * side, 0f, Rnd(rng, 2f, length - 2f)),
                    new Color(Rnd(rng, 0.25f, 0.5f), Rnd(rng, 0.22f, 0.4f), Rnd(rng, 0.25f, 0.45f)),
                    new Color(0.18f, 0.16f, 0.18f), Rnd(rng, 1.5f, 1.8f), Rnd(rng, 0f, 360f));
            }

            PlaceWallSigns(data, t, rng, length, 6.45f, 3.6f, 1.1f);

            // Поперечный золотой портал-балка (торжественность).
            if (variant % 2 == 1)
            {
                float z = Rnd(rng, 10f, length - 10f);
                ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                    new Vector3(0f, 5.0f, z), new Vector3(13.2f, 0.5f, 0.6f),
                    marbleMat, "portalBeam");
                GameObject gold = ProceduralModelFactory.Part(t, PrimitiveType.Cube,
                    new Vector3(0f, 4.72f, z), new Vector3(13.2f, 0.08f, 0.65f), goldTrim, "portalGold");
                ProceduralModelFactory.NoShadow(gold);
            }
        }
    }
}
