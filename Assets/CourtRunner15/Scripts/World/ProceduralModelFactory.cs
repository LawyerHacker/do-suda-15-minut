using UnityEngine;
using UnityEngine.Rendering;

namespace CourtRunner15
{
    /// <summary>
    /// Собирает все модели игры из примитивов: юриста, препятствия,
    /// бусты и декорации. Никаких внешних ресурсов.
    /// </summary>
    public static class ProceduralModelFactory
    {
        // ------------------------------------------------------------------
        // Базовые кирпичики
        // ------------------------------------------------------------------

        public static GameObject Part(Transform parent, PrimitiveType type, Vector3 localPos,
            Vector3 localScale, Material mat, string name)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            return go;
        }

        public static GameObject PartRot(Transform parent, PrimitiveType type, Vector3 localPos,
            Vector3 localScale, Vector3 euler, Material mat, string name)
        {
            GameObject go = Part(parent, type, localPos, localScale, mat, name);
            go.transform.localRotation = Quaternion.Euler(euler);
            return go;
        }

        public static void NoShadow(GameObject go)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        public static GameObject Group(Transform parent, string name, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        /// <summary>3D-текст (русский), читается со стороны игрока/камеры.</summary>
        public static TextMesh TextLabel(Transform parent, string text, Vector3 localPos,
            float worldHeight, Color color, float rotY)
        {
            GameObject go = new GameObject("label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            TextMesh tm = go.AddComponent<TextMesh>();
            Font font = MaterialFactory.UiFont;
            tm.font = font;
            tm.fontSize = 64;
            tm.characterSize = worldHeight * 10f / tm.fontSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (font != null) mr.sharedMaterial = font.material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return tm;
        }

        /// <summary>Вывеска: подложка, рамка и текст.</summary>
        public static GameObject SignBoard(Transform parent, string text, Vector3 localPos,
            Vector2 size, Color bg, Color textColor, float rotY, bool glow)
        {
            GameObject root = Group(parent, "sign", localPos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

            Material bgMat = glow ? MaterialFactory.Emissive(bg, bg, 1.2f)
                                  : MaterialFactory.Solid(bg, 0.5f, 0f);
            Part(root.transform, PrimitiveType.Cube, Vector3.zero,
                new Vector3(size.x, size.y, 0.07f), bgMat, "board");

            Material frameMat = MaterialFactory.Solid(new Color(0.9f, 0.78f, 0.45f), 0.7f, 0.6f);
            float t = 0.05f;
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, size.y * 0.5f, 0f),
                new Vector3(size.x + t, t, 0.09f), frameMat, "frameT");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, -size.y * 0.5f, 0f),
                new Vector3(size.x + t, t, 0.09f), frameMat, "frameB");
            Part(root.transform, PrimitiveType.Cube, new Vector3(-size.x * 0.5f, 0f, 0f),
                new Vector3(t, size.y + t, 0.09f), frameMat, "frameL");
            Part(root.transform, PrimitiveType.Cube, new Vector3(size.x * 0.5f, 0f, 0f),
                new Vector3(t, size.y + t, 0.09f), frameMat, "frameR");

            float textHeight = Mathf.Min(size.y * 0.42f, size.x * 1.5f / Mathf.Max(4, text.Length));
            TextLabel(root.transform, text, new Vector3(0f, 0f, -0.06f), textHeight, textColor, 0f);
            TextLabel(root.transform, text, new Vector3(0f, 0f, 0.06f), textHeight, textColor, 180f);
            return root;
        }

        // ------------------------------------------------------------------
        // Юрист
        // ------------------------------------------------------------------

        public static PlayerVisual CreateLawyer(Transform parent)
        {
            Material suit = MaterialFactory.Solid(new Color(0.13f, 0.17f, 0.27f), 0.55f, 0.05f);
            Material skin = MaterialFactory.Solid(new Color(0.98f, 0.80f, 0.65f), 0.35f, 0f);
            Material hair = MaterialFactory.Solid(new Color(0.22f, 0.14f, 0.09f), 0.4f, 0f);
            Material shirt = MaterialFactory.Solid(new Color(0.96f, 0.96f, 0.98f), 0.4f, 0f);
            Material tie = MaterialFactory.Solid(new Color(0.80f, 0.13f, 0.16f), 0.5f, 0f);
            Material shoes = MaterialFactory.Solid(new Color(0.07f, 0.07f, 0.09f), 0.85f, 0.2f);
            Material caseMat = MaterialFactory.Solid(new Color(0.45f, 0.27f, 0.13f), 0.65f, 0.1f);
            Material gold = MaterialFactory.Solid(new Color(0.95f, 0.78f, 0.35f), 0.8f, 0.8f);

            GameObject visualGo = new GameObject("LawyerVisual");
            visualGo.transform.SetParent(parent, false);
            PlayerVisual visual = visualGo.AddComponent<PlayerVisual>();

            GameObject bodyRoot = Group(visualGo.transform, "BodyRoot", Vector3.zero);

            // Ноги.
            GameObject legL = Group(bodyRoot.transform, "LegLPivot", new Vector3(-0.15f, 0.92f, 0f));
            Part(legL.transform, PrimitiveType.Cube, new Vector3(0f, -0.42f, 0f),
                new Vector3(0.17f, 0.84f, 0.2f), suit, "legL");
            Part(legL.transform, PrimitiveType.Cube, new Vector3(0f, -0.88f, 0.07f),
                new Vector3(0.2f, 0.11f, 0.42f), shoes, "shoeL");

            GameObject legR = Group(bodyRoot.transform, "LegRPivot", new Vector3(0.15f, 0.92f, 0f));
            Part(legR.transform, PrimitiveType.Cube, new Vector3(0f, -0.42f, 0f),
                new Vector3(0.17f, 0.84f, 0.2f), suit, "legR");
            Part(legR.transform, PrimitiveType.Cube, new Vector3(0f, -0.88f, 0.07f),
                new Vector3(0.2f, 0.11f, 0.42f), shoes, "shoeR");

            // Корпус в костюме.
            Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.26f, 0f),
                new Vector3(0.54f, 0.64f, 0.32f), suit, "torso");
            Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 0.97f, 0f),
                new Vector3(0.56f, 0.18f, 0.34f), suit, "jacket");
            Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.38f, 0.155f),
                new Vector3(0.2f, 0.34f, 0.05f), shirt, "shirt");
            PartRot(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.27f, 0.175f),
                new Vector3(0.1f, 0.3f, 0.04f), new Vector3(4f, 0f, 0f), tie, "tie");

            // Руки.
            GameObject armL = Group(bodyRoot.transform, "ArmLPivot", new Vector3(-0.34f, 1.5f, 0f));
            Part(armL.transform, PrimitiveType.Cube, new Vector3(0f, -0.3f, 0f),
                new Vector3(0.14f, 0.6f, 0.16f), suit, "armL");
            Part(armL.transform, PrimitiveType.Sphere, new Vector3(0f, -0.62f, 0f),
                new Vector3(0.15f, 0.15f, 0.15f), skin, "handL");

            GameObject armR = Group(bodyRoot.transform, "ArmRPivot", new Vector3(0.34f, 1.5f, 0f));
            Part(armR.transform, PrimitiveType.Cube, new Vector3(0f, -0.3f, 0f),
                new Vector3(0.14f, 0.6f, 0.16f), suit, "armR");
            Part(armR.transform, PrimitiveType.Sphere, new Vector3(0f, -0.62f, 0f),
                new Vector3(0.15f, 0.15f, 0.15f), skin, "handR");

            // Портфель в правой руке.
            GameObject briefcase = Group(armR.transform, "Briefcase", new Vector3(0.06f, -0.74f, 0.05f));
            Part(briefcase.transform, PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.5f, 0.34f, 0.14f), caseMat, "caseBody");
            Part(briefcase.transform, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f),
                new Vector3(0.18f, 0.06f, 0.04f), caseMat, "caseHandle");
            Part(briefcase.transform, PrimitiveType.Cube, new Vector3(0f, 0.05f, -0.075f),
                new Vector3(0.1f, 0.08f, 0.02f), gold, "caseLock");

            // Голова (слегка карикатурно крупная).
            GameObject headPivot = Group(bodyRoot.transform, "HeadPivot", new Vector3(0f, 1.64f, 0f));
            Part(headPivot.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f),
                new Vector3(0.14f, 0.05f, 0.14f), skin, "neck");
            Part(headPivot.transform, PrimitiveType.Sphere, new Vector3(0f, 0.24f, 0f),
                new Vector3(0.44f, 0.44f, 0.44f), skin, "head");
            Part(headPivot.transform, PrimitiveType.Sphere, new Vector3(0f, 0.37f, -0.03f),
                new Vector3(0.46f, 0.26f, 0.46f), hair, "hairTop");
            Part(headPivot.transform, PrimitiveType.Cube, new Vector3(0f, 0.24f, -0.21f),
                new Vector3(0.4f, 0.3f, 0.08f), hair, "hairBack");

            // Мягкая тень-блоб.
            GameObject shadow = new GameObject("Shadow");
            shadow.transform.SetParent(visualGo.transform, false);
            GameObject shadowQuad = PartRot(shadow.transform, PrimitiveType.Quad, Vector3.zero,
                new Vector3(1.15f, 0.8f, 1f), new Vector3(90f, 0f, 0f),
                MaterialFactory.UnlitTransparent(new Color(0f, 0f, 0f, 0.34f)), "shadowQuad");
            NoShadow(shadowQuad);

            visual.BodyRoot = bodyRoot.transform;
            visual.Head = headPivot.transform;
            visual.ArmLPivot = armL.transform;
            visual.ArmRPivot = armR.transform;
            visual.LegLPivot = legL.transform;
            visual.LegRPivot = legR.transform;
            visual.Briefcase = briefcase.transform;
            visual.Shadow = shadow.transform;
            visual.CacheRenderers();
            return visual;
        }

        public static PlayerVisual CreateLawyerV2(Transform parent)
        {
            Material suit = MaterialFactory.Solid(new Color(0.08f, 0.12f, 0.20f), 0.68f, 0.04f);
            Material suitLight = MaterialFactory.Solid(new Color(0.13f, 0.18f, 0.29f), 0.72f, 0.05f);
            Material suitDark = MaterialFactory.Solid(new Color(0.035f, 0.045f, 0.065f), 0.64f, 0.03f);
            Material skin = MaterialFactory.Solid(new Color(0.96f, 0.76f, 0.58f), 0.42f, 0f);
            Material skinWarm = MaterialFactory.Solid(new Color(1.0f, 0.66f, 0.50f), 0.36f, 0f);
            Material hair = MaterialFactory.Solid(new Color(0.12f, 0.075f, 0.045f), 0.55f, 0f);
            Material shirt = MaterialFactory.Solid(new Color(0.94f, 0.95f, 0.94f), 0.45f, 0f);
            Material tie = MaterialFactory.Solid(new Color(0.78f, 0.08f, 0.12f), 0.6f, 0f);
            Material shoes = MaterialFactory.Solid(new Color(0.025f, 0.025f, 0.035f), 0.9f, 0.15f);
            Material caseMat = MaterialFactory.Solid(new Color(0.34f, 0.19f, 0.09f), 0.72f, 0.08f);
            Material caseEdge = MaterialFactory.Solid(new Color(0.19f, 0.10f, 0.045f), 0.82f, 0.08f);
            Material gold = MaterialFactory.Solid(new Color(0.95f, 0.74f, 0.32f), 0.82f, 0.7f);
            Material eye = MaterialFactory.Solid(new Color(0.04f, 0.035f, 0.03f), 0.35f, 0f);

            GameObject visualGo = new GameObject("LawyerVisual");
            visualGo.transform.SetParent(parent, false);
            PlayerVisual visual = visualGo.AddComponent<PlayerVisual>();
            GameObject bodyRoot = Group(visualGo.transform, "BodyRoot", Vector3.zero);

            GameObject legL = Group(bodyRoot.transform, "LegLPivot", new Vector3(-0.16f, 0.94f, 0f));
            Part(legL.transform, PrimitiveType.Capsule, new Vector3(0f, -0.30f, 0f),
                new Vector3(0.18f, 0.44f, 0.18f), suitLight, "thighL");
            Part(legL.transform, PrimitiveType.Capsule, new Vector3(0f, -0.72f, 0.015f),
                new Vector3(0.15f, 0.43f, 0.15f), suit, "shinL");
            Part(legL.transform, PrimitiveType.Cube, new Vector3(0f, -1.03f, -0.04f),
                new Vector3(0.23f, 0.10f, 0.42f), shoes, "shoeL");
            NoShadow(Part(legL.transform, PrimitiveType.Cube, new Vector3(0f, -0.50f, -0.10f),
                new Vector3(0.16f, 0.025f, 0.025f), suitDark, "kneeCreaseL"));

            GameObject legR = Group(bodyRoot.transform, "LegRPivot", new Vector3(0.16f, 0.94f, 0f));
            Part(legR.transform, PrimitiveType.Capsule, new Vector3(0f, -0.30f, 0f),
                new Vector3(0.18f, 0.44f, 0.18f), suitLight, "thighR");
            Part(legR.transform, PrimitiveType.Capsule, new Vector3(0f, -0.72f, 0.015f),
                new Vector3(0.15f, 0.43f, 0.15f), suit, "shinR");
            Part(legR.transform, PrimitiveType.Cube, new Vector3(0f, -1.03f, -0.04f),
                new Vector3(0.23f, 0.10f, 0.42f), shoes, "shoeR");
            NoShadow(Part(legR.transform, PrimitiveType.Cube, new Vector3(0f, -0.50f, -0.10f),
                new Vector3(0.16f, 0.025f, 0.025f), suitDark, "kneeCreaseR"));

            Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.07f, -0.005f),
                new Vector3(0.48f, 0.26f, 0.32f), suitDark, "hips");
            Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.36f, -0.005f),
                new Vector3(0.58f, 0.68f, 0.34f), suit, "jacketBody");
            Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.61f, -0.005f),
                new Vector3(0.76f, 0.18f, 0.36f), suitLight, "shoulders");
            NoShadow(Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.03f, -0.18f),
                new Vector3(0.42f, 0.08f, 0.06f), suitDark, "jacketBackHem"));
            NoShadow(Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.35f, -0.185f),
                new Vector3(0.035f, 0.48f, 0.025f), suitLight, "backSeam"));
            NoShadow(Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.70f, -0.17f),
                new Vector3(0.34f, 0.08f, 0.05f), shirt, "shirtCollarBack"));
            Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.42f, 0.18f),
                new Vector3(0.25f, 0.44f, 0.045f), shirt, "shirtFront");
            NoShadow(PartRot(bodyRoot.transform, PrimitiveType.Cube, new Vector3(-0.115f, 1.47f, 0.205f),
                new Vector3(0.075f, 0.46f, 0.045f), new Vector3(0f, 0f, -16f), suitLight, "lapelL"));
            NoShadow(PartRot(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0.115f, 1.47f, 0.205f),
                new Vector3(0.075f, 0.46f, 0.045f), new Vector3(0f, 0f, 16f), suitLight, "lapelR"));
            NoShadow(Part(bodyRoot.transform, PrimitiveType.Sphere, new Vector3(0f, 1.54f, 0.235f),
                new Vector3(0.10f, 0.08f, 0.04f), tie, "tieNode"));
            NoShadow(PartRot(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.30f, 0.225f),
                new Vector3(0.095f, 0.36f, 0.035f), new Vector3(4f, 0f, 0f), tie, "tieBlade"));

            GameObject armL = Group(bodyRoot.transform, "ArmLPivot", new Vector3(-0.43f, 1.55f, 0f));
            Part(armL.transform, PrimitiveType.Capsule, new Vector3(0f, -0.31f, 0f),
                new Vector3(0.15f, 0.38f, 0.15f), suitLight, "upperArmL");
            Part(armL.transform, PrimitiveType.Capsule, new Vector3(0f, -0.68f, 0.02f),
                new Vector3(0.13f, 0.34f, 0.13f), suit, "forearmL");
            Part(armL.transform, PrimitiveType.Cube, new Vector3(0f, -0.89f, 0.02f),
                new Vector3(0.15f, 0.055f, 0.15f), shirt, "cuffL");
            Part(armL.transform, PrimitiveType.Sphere, new Vector3(0f, -0.98f, 0.02f),
                new Vector3(0.16f, 0.15f, 0.13f), skin, "handL");

            GameObject armR = Group(bodyRoot.transform, "ArmRPivot", new Vector3(0.43f, 1.55f, 0f));
            Part(armR.transform, PrimitiveType.Capsule, new Vector3(0f, -0.31f, 0f),
                new Vector3(0.15f, 0.38f, 0.15f), suitLight, "upperArmR");
            Part(armR.transform, PrimitiveType.Capsule, new Vector3(0f, -0.68f, 0.02f),
                new Vector3(0.13f, 0.34f, 0.13f), suit, "forearmR");
            Part(armR.transform, PrimitiveType.Cube, new Vector3(0f, -0.89f, 0.02f),
                new Vector3(0.15f, 0.055f, 0.15f), shirt, "cuffR");
            Part(armR.transform, PrimitiveType.Sphere, new Vector3(0f, -0.98f, 0.02f),
                new Vector3(0.16f, 0.15f, 0.13f), skin, "handR");

            GameObject briefcase = Group(armR.transform, "Briefcase", new Vector3(0.06f, -1.12f, 0.07f));
            Part(briefcase.transform, PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.54f, 0.36f, 0.16f), caseMat, "caseBody");
            Part(briefcase.transform, PrimitiveType.Cube, new Vector3(0f, 0.20f, -0.005f),
                new Vector3(0.26f, 0.055f, 0.05f), caseEdge, "caseHandle");
            Part(briefcase.transform, PrimitiveType.Cube, new Vector3(-0.29f, 0f, 0f),
                new Vector3(0.035f, 0.38f, 0.18f), caseEdge, "caseSideL");
            Part(briefcase.transform, PrimitiveType.Cube, new Vector3(0.29f, 0f, 0f),
                new Vector3(0.035f, 0.38f, 0.18f), caseEdge, "caseSideR");
            NoShadow(Part(briefcase.transform, PrimitiveType.Cube, new Vector3(0f, 0.04f, -0.09f),
                new Vector3(0.13f, 0.085f, 0.022f), gold, "caseLock"));

            GameObject headPivot = Group(bodyRoot.transform, "HeadPivot", new Vector3(0f, 1.72f, 0f));
            Part(headPivot.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f),
                new Vector3(0.13f, 0.08f, 0.13f), skin, "neck");
            Part(headPivot.transform, PrimitiveType.Sphere, new Vector3(0f, 0.24f, 0f),
                new Vector3(0.38f, 0.44f, 0.36f), skin, "head");
            Part(headPivot.transform, PrimitiveType.Sphere, new Vector3(-0.22f, 0.24f, 0f),
                new Vector3(0.08f, 0.13f, 0.055f), skinWarm, "earL");
            Part(headPivot.transform, PrimitiveType.Sphere, new Vector3(0.22f, 0.24f, 0f),
                new Vector3(0.08f, 0.13f, 0.055f), skinWarm, "earR");
            Part(headPivot.transform, PrimitiveType.Sphere, new Vector3(0f, 0.42f, -0.015f),
                new Vector3(0.42f, 0.22f, 0.36f), hair, "hairCrown");
            Part(headPivot.transform, PrimitiveType.Cube, new Vector3(0f, 0.29f, -0.19f),
                new Vector3(0.34f, 0.28f, 0.07f), hair, "hairBack");
            PartRot(headPivot.transform, PrimitiveType.Cube, new Vector3(-0.11f, 0.48f, 0.08f),
                new Vector3(0.16f, 0.07f, 0.05f), new Vector3(0f, 0f, -18f), hair, "hairLockL");
            PartRot(headPivot.transform, PrimitiveType.Cube, new Vector3(0.10f, 0.49f, 0.09f),
                new Vector3(0.16f, 0.07f, 0.05f), new Vector3(0f, 0f, 16f), hair, "hairLockR");
            NoShadow(Part(headPivot.transform, PrimitiveType.Sphere, new Vector3(-0.075f, 0.25f, 0.185f),
                new Vector3(0.035f, 0.026f, 0.012f), eye, "eyeL"));
            NoShadow(Part(headPivot.transform, PrimitiveType.Sphere, new Vector3(0.075f, 0.25f, 0.185f),
                new Vector3(0.035f, 0.026f, 0.012f), eye, "eyeR"));
            NoShadow(Part(headPivot.transform, PrimitiveType.Cube, new Vector3(0f, 0.19f, 0.195f),
                new Vector3(0.035f, 0.07f, 0.035f), skinWarm, "nose"));

            GameObject shadow = new GameObject("Shadow");
            shadow.transform.SetParent(visualGo.transform, false);
            GameObject shadowQuad = PartRot(shadow.transform, PrimitiveType.Quad, Vector3.zero,
                new Vector3(1.05f, 0.75f, 1f), new Vector3(90f, 0f, 0f),
                MaterialFactory.UnlitTransparent(new Color(0f, 0f, 0f, 0.30f)), "shadowQuad");
            NoShadow(shadowQuad);

            visual.BodyRoot = bodyRoot.transform;
            visual.Head = headPivot.transform;
            visual.ArmLPivot = armL.transform;
            visual.ArmRPivot = armR.transform;
            visual.LegLPivot = legL.transform;
            visual.LegRPivot = legR.transform;
            visual.Briefcase = briefcase.transform;
            visual.Shadow = shadow.transform;
            visual.CacheRenderers();
            return visual;
        }

        /// <summary>
        /// Юрист V3: человеческие пропорции (~1:7), низкополигональные
        /// flat-shaded меши вместо кубов, колени/локти для живого бега,
        /// полноценный костюм и портфель. Хитбокс игрока не меняется.
        /// </summary>
        public static PlayerVisual CreateLawyerV3(Transform parent)
        {
            Material suit = MaterialFactory.Solid(new Color(0.10f, 0.14f, 0.24f), 0.62f, 0.05f);
            Material suitLight = MaterialFactory.Solid(new Color(0.145f, 0.195f, 0.315f), 0.66f, 0.05f);
            Material suitDark = MaterialFactory.Solid(new Color(0.055f, 0.075f, 0.13f), 0.6f, 0.04f);
            Material trousers = MaterialFactory.Solid(new Color(0.085f, 0.115f, 0.20f), 0.6f, 0.04f);
            Material beltM = MaterialFactory.Solid(new Color(0.16f, 0.09f, 0.05f), 0.7f, 0.1f);
            Material skin = MaterialFactory.Solid(new Color(0.93f, 0.72f, 0.56f), 0.38f, 0f);
            Material skinWarm = MaterialFactory.Solid(new Color(0.88f, 0.62f, 0.47f), 0.34f, 0f);
            Material hair = MaterialFactory.Solid(new Color(0.16f, 0.11f, 0.07f), 0.5f, 0f);
            Material shirt = MaterialFactory.Solid(new Color(0.95f, 0.96f, 0.97f), 0.42f, 0f);
            Material tie = MaterialFactory.Solid(new Color(0.62f, 0.10f, 0.14f), 0.55f, 0f);
            Material shoes = MaterialFactory.Solid(new Color(0.05f, 0.045f, 0.05f), 0.88f, 0.18f);
            Material caseMat = MaterialFactory.Solid(new Color(0.36f, 0.20f, 0.10f), 0.7f, 0.08f);
            Material caseEdge = MaterialFactory.Solid(new Color(0.20f, 0.11f, 0.05f), 0.8f, 0.08f);
            Material gold = MaterialFactory.Solid(new Color(0.93f, 0.74f, 0.34f), 0.85f, 0.75f);
            Material eye = MaterialFactory.Solid(new Color(0.06f, 0.05f, 0.05f), 0.4f, 0f);
            Material lips = MaterialFactory.Solid(new Color(0.72f, 0.42f, 0.36f), 0.3f, 0f);

            GameObject visualGo = new GameObject("LawyerVisual");
            visualGo.transform.SetParent(parent, false);
            PlayerVisual visual = visualGo.AddComponent<PlayerVisual>();
            GameObject bodyRoot = Group(visualGo.transform, "BodyRoot", Vector3.zero);

            // ---- Ноги: бедро → колено → голень → ботинок ----
            Mesh thighMesh = LowPolyMeshFactory.Prism(7, 0.072f, 0.088f, 0.47f);
            Mesh shinMesh = LowPolyMeshFactory.Prism(7, 0.05f, 0.068f, 0.43f);
            Mesh shoeMesh = LowPolyMeshFactory.Frustum(0.115f, 0.31f, 0.095f, 0.24f, 0.10f);
            Mesh soleMesh = LowPolyMeshFactory.Frustum(0.12f, 0.32f, 0.115f, 0.31f, 0.03f);

            GameObject legL = Group(bodyRoot.transform, "LegLPivot", new Vector3(-0.105f, 1.03f, 0f));
            LowPolyMeshFactory.MeshPart(legL.transform, thighMesh,
                new Vector3(0f, -0.235f, 0f), trousers, "thighL");
            GameObject kneeL = Group(legL.transform, "KneeLPivot", new Vector3(0f, -0.465f, 0.01f));
            LowPolyMeshFactory.MeshPart(kneeL.transform, shinMesh,
                new Vector3(0f, -0.215f, 0f), trousers, "shinL");
            LowPolyMeshFactory.MeshPart(kneeL.transform, shoeMesh,
                new Vector3(0f, -0.475f, 0.055f), shoes, "shoeL");
            LowPolyMeshFactory.MeshPart(kneeL.transform, soleMesh,
                new Vector3(0f, -0.535f, 0.055f), shoes, "soleL");

            GameObject legR = Group(bodyRoot.transform, "LegRPivot", new Vector3(0.105f, 1.03f, 0f));
            LowPolyMeshFactory.MeshPart(legR.transform, thighMesh,
                new Vector3(0f, -0.235f, 0f), trousers, "thighR");
            GameObject kneeR = Group(legR.transform, "KneeRPivot", new Vector3(0f, -0.465f, 0.01f));
            LowPolyMeshFactory.MeshPart(kneeR.transform, shinMesh,
                new Vector3(0f, -0.215f, 0f), trousers, "shinR");
            LowPolyMeshFactory.MeshPart(kneeR.transform, shoeMesh,
                new Vector3(0f, -0.475f, 0.055f), shoes, "shoeR");
            LowPolyMeshFactory.MeshPart(kneeR.transform, soleMesh,
                new Vector3(0f, -0.535f, 0.055f), shoes, "soleR");

            // ---- Корпус: таз, ремень, полы пиджака, грудная клетка, плечи ----
            LowPolyMeshFactory.MeshPart(bodyRoot.transform,
                LowPolyMeshFactory.Frustum(0.29f, 0.195f, 0.335f, 0.22f, 0.16f),
                new Vector3(0f, 1.05f, 0f), trousers, "pelvis");
            LowPolyMeshFactory.MeshPart(bodyRoot.transform,
                LowPolyMeshFactory.Frustum(0.34f, 0.225f, 0.34f, 0.225f, 0.04f),
                new Vector3(0f, 1.15f, 0f), beltM, "belt");
            NoShadow(Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.15f, 0.112f),
                new Vector3(0.045f, 0.03f, 0.012f), gold, "buckle"));
            LowPolyMeshFactory.MeshPart(bodyRoot.transform,
                LowPolyMeshFactory.Frustum(0.405f, 0.265f, 0.375f, 0.24f, 0.24f),
                new Vector3(0f, 1.07f, -0.005f), suit, "jacketSkirt");
            LowPolyMeshFactory.MeshPart(bodyRoot.transform,
                LowPolyMeshFactory.Frustum(0.36f, 0.235f, 0.465f, 0.275f, 0.35f),
                new Vector3(0f, 1.345f, -0.005f), suit, "chest");
            LowPolyMeshFactory.MeshPart(bodyRoot.transform,
                LowPolyMeshFactory.Frustum(0.465f, 0.275f, 0.30f, 0.20f, 0.07f),
                new Vector3(0f, 1.555f, -0.005f), suitLight, "shoulderSlope");

            // Спина: центральный шов и линия подола — читаемый силуэт сзади.
            NoShadow(Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.35f, -0.138f),
                new Vector3(0.012f, 0.32f, 0.014f), suitDark, "backSeam"));
            NoShadow(Part(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0f, 0.965f, -0.125f),
                new Vector3(0.30f, 0.015f, 0.015f), suitDark, "backHem"));

            // Лацканы, рубашка, галстук, пуговицы, платок.
            NoShadow(PartRot(bodyRoot.transform, PrimitiveType.Cube, new Vector3(-0.088f, 1.415f, 0.128f),
                new Vector3(0.08f, 0.29f, 0.018f), new Vector3(-5f, 0f, -15f), suitLight, "lapelL"));
            NoShadow(PartRot(bodyRoot.transform, PrimitiveType.Cube, new Vector3(0.088f, 1.415f, 0.128f),
                new Vector3(0.08f, 0.29f, 0.018f), new Vector3(-5f, 0f, 15f), suitLight, "lapelR"));
            LowPolyMeshFactory.MeshPart(bodyRoot.transform,
                LowPolyMeshFactory.Frustum(0.135f, 0.018f, 0.185f, 0.018f, 0.36f),
                new Vector3(0f, 1.35f, 0.122f), new Vector3(-4f, 0f, 0f), Vector3.one, shirt, "shirtFront");
            LowPolyMeshFactory.MeshPart(bodyRoot.transform,
                LowPolyMeshFactory.Frustum(0.045f, 0.028f, 0.06f, 0.032f, 0.05f),
                new Vector3(0f, 1.505f, 0.138f), new Vector3(-4f, 0f, 0f), Vector3.one, tie, "tieKnot");
            LowPolyMeshFactory.MeshPart(bodyRoot.transform,
                LowPolyMeshFactory.Frustum(0.075f, 0.03f, 0.035f, 0.026f, 0.24f),
                new Vector3(0f, 1.375f, 0.140f), new Vector3(-4f, 0f, 0f), Vector3.one, tie, "tieBlade");
            LowPolyMeshFactory.MeshPart(bodyRoot.transform,
                LowPolyMeshFactory.Frustum(0.012f, 0.024f, 0.075f, 0.03f, 0.07f),
                new Vector3(0f, 1.225f, 0.143f), new Vector3(-5f, 0f, 0f), Vector3.one, tie, "tieTip");
            NoShadow(LowPolyMeshFactory.MeshPart(bodyRoot.transform,
                LowPolyMeshFactory.Sphere(6, 4, 0.011f),
                new Vector3(0.048f, 1.24f, 0.128f), suitDark, "button1"));
            NoShadow(LowPolyMeshFactory.MeshPart(bodyRoot.transform,
                LowPolyMeshFactory.Sphere(6, 4, 0.011f),
                new Vector3(0.05f, 1.18f, 0.131f), suitDark, "button2"));
            NoShadow(PartRot(bodyRoot.transform, PrimitiveType.Cube, new Vector3(-0.135f, 1.435f, 0.118f),
                new Vector3(0.045f, 0.03f, 0.012f), new Vector3(-5f, 0f, 8f), shirt, "pocketSquare"));

            // ---- Руки: плечо → локоть → предплечье → манжета → кисть ----
            Mesh upperArmMesh = LowPolyMeshFactory.Prism(7, 0.054f, 0.065f, 0.31f);
            Mesh forearmMesh = LowPolyMeshFactory.Prism(7, 0.044f, 0.052f, 0.27f);
            Mesh cuffMesh = LowPolyMeshFactory.Prism(7, 0.054f, 0.054f, 0.04f);
            Mesh handMesh = LowPolyMeshFactory.Sphere(7, 5, 0.048f);
            Mesh shoulderMesh = LowPolyMeshFactory.Sphere(8, 5, 0.078f);

            GameObject armL = Group(bodyRoot.transform, "ArmLPivot", new Vector3(-0.27f, 1.48f, 0f));
            LowPolyMeshFactory.MeshPart(armL.transform, shoulderMesh,
                new Vector3(0f, 0.01f, 0f), suitLight, "shoulderL");
            LowPolyMeshFactory.MeshPart(armL.transform, upperArmMesh,
                new Vector3(0f, -0.165f, 0f), suit, "upperArmL");
            GameObject elbowL = Group(armL.transform, "ElbowLPivot", new Vector3(0f, -0.325f, 0f));
            LowPolyMeshFactory.MeshPart(elbowL.transform, forearmMesh,
                new Vector3(0f, -0.135f, 0f), suit, "forearmL");
            LowPolyMeshFactory.MeshPart(elbowL.transform, cuffMesh,
                new Vector3(0f, -0.275f, 0f), shirt, "cuffL");
            LowPolyMeshFactory.MeshPart(elbowL.transform, handMesh,
                new Vector3(0f, -0.325f, 0.008f), Vector3.zero, new Vector3(0.95f, 1.25f, 0.85f),
                skin, "handL");

            GameObject armR = Group(bodyRoot.transform, "ArmRPivot", new Vector3(0.27f, 1.48f, 0f));
            LowPolyMeshFactory.MeshPart(armR.transform, shoulderMesh,
                new Vector3(0f, 0.01f, 0f), suitLight, "shoulderR");
            LowPolyMeshFactory.MeshPart(armR.transform, upperArmMesh,
                new Vector3(0f, -0.165f, 0f), suit, "upperArmR");
            GameObject elbowR = Group(armR.transform, "ElbowRPivot", new Vector3(0f, -0.325f, 0f));
            LowPolyMeshFactory.MeshPart(elbowR.transform, forearmMesh,
                new Vector3(0f, -0.135f, 0f), suit, "forearmR");
            LowPolyMeshFactory.MeshPart(elbowR.transform, cuffMesh,
                new Vector3(0f, -0.275f, 0f), shirt, "cuffR");
            LowPolyMeshFactory.MeshPart(elbowR.transform, handMesh,
                new Vector3(0f, -0.325f, 0.008f), Vector3.zero, new Vector3(0.95f, 1.25f, 0.85f),
                skin, "handR");

            // ---- Портфель в правой руке (плоской стороной по ходу бега) ----
            GameObject briefcase = Group(elbowR.transform, "Briefcase", new Vector3(0.045f, -0.415f, 0.03f));
            LowPolyMeshFactory.MeshPart(briefcase.transform,
                LowPolyMeshFactory.Frustum(0.155f, 0.50f, 0.165f, 0.53f, 0.34f),
                Vector3.zero, caseMat, "caseBody");
            Part(briefcase.transform, PrimitiveType.Cube, new Vector3(0f, 0.195f, 0f),
                new Vector3(0.028f, 0.05f, 0.14f), caseEdge, "caseHandle");
            Part(briefcase.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0.26f),
                new Vector3(0.17f, 0.36f, 0.035f), caseEdge, "caseFront");
            Part(briefcase.transform, PrimitiveType.Cube, new Vector3(0f, 0f, -0.26f),
                new Vector3(0.17f, 0.36f, 0.035f), caseEdge, "caseBack");
            NoShadow(Part(briefcase.transform, PrimitiveType.Cube, new Vector3(-0.085f, 0.10f, 0.11f),
                new Vector3(0.012f, 0.035f, 0.05f), gold, "lock1"));
            NoShadow(Part(briefcase.transform, PrimitiveType.Cube, new Vector3(-0.085f, 0.10f, -0.11f),
                new Vector3(0.012f, 0.035f, 0.05f), gold, "lock2"));

            // ---- Голова: шея, воротник, череп, причёска, лицо ----
            GameObject headPivot = Group(bodyRoot.transform, "HeadPivot", new Vector3(0f, 1.565f, 0f));
            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Prism(7, 0.05f, 0.055f, 0.09f),
                new Vector3(0f, 0.045f, 0f), skin, "neck");
            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Prism(7, 0.058f, 0.062f, 0.05f),
                new Vector3(0f, 0.015f, 0f), shirt, "shirtCollar");
            PartRot(headPivot.transform, PrimitiveType.Cube, new Vector3(0f, 0.03f, -0.075f),
                new Vector3(0.16f, 0.06f, 0.03f), new Vector3(10f, 0f, 0f), suitDark, "jacketCollar");

            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Sphere(8, 6, 0.115f),
                new Vector3(0f, 0.19f, 0.01f), Vector3.zero, new Vector3(0.93f, 1.12f, 1.0f),
                skin, "skull");
            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Frustum(0.135f, 0.125f, 0.165f, 0.15f, 0.085f),
                new Vector3(0f, 0.095f, 0.02f), skin, "jaw");
            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Sphere(8, 5, 0.122f),
                new Vector3(0f, 0.225f, -0.005f), Vector3.zero, new Vector3(0.96f, 0.92f, 1.02f),
                hair, "hairCrown");
            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Frustum(0.20f, 0.07f, 0.215f, 0.085f, 0.15f),
                new Vector3(0f, 0.13f, -0.098f), hair, "hairBack");
            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Frustum(0.022f, 0.085f, 0.02f, 0.075f, 0.09f),
                new Vector3(-0.104f, 0.185f, -0.01f), hair, "templeL");
            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Frustum(0.022f, 0.085f, 0.02f, 0.075f, 0.09f),
                new Vector3(0.104f, 0.185f, -0.01f), hair, "templeR");
            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Frustum(0.19f, 0.02f, 0.20f, 0.03f, 0.05f),
                new Vector3(0f, 0.27f, 0.083f), new Vector3(-12f, 0f, 0f), Vector3.one,
                hair, "hairline");

            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Sphere(6, 4, 0.028f),
                new Vector3(-0.107f, 0.175f, 0.005f), Vector3.zero, new Vector3(0.6f, 1.1f, 0.9f),
                skinWarm, "earL");
            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Sphere(6, 4, 0.028f),
                new Vector3(0.107f, 0.175f, 0.005f), Vector3.zero, new Vector3(0.6f, 1.1f, 0.9f),
                skinWarm, "earR");
            NoShadow(LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Sphere(6, 4, 0.012f),
                new Vector3(-0.043f, 0.195f, 0.098f), eye, "eyeL"));
            NoShadow(LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Sphere(6, 4, 0.012f),
                new Vector3(0.043f, 0.195f, 0.098f), eye, "eyeR"));
            NoShadow(PartRot(headPivot.transform, PrimitiveType.Cube, new Vector3(-0.045f, 0.235f, 0.100f),
                new Vector3(0.045f, 0.012f, 0.01f), new Vector3(0f, 0f, 8f), hair, "browL"));
            NoShadow(PartRot(headPivot.transform, PrimitiveType.Cube, new Vector3(0.045f, 0.235f, 0.100f),
                new Vector3(0.045f, 0.012f, 0.01f), new Vector3(0f, 0f, -8f), hair, "browR"));
            LowPolyMeshFactory.MeshPart(headPivot.transform,
                LowPolyMeshFactory.Frustum(0.02f, 0.02f, 0.032f, 0.035f, 0.055f),
                new Vector3(0f, 0.165f, 0.112f), new Vector3(-8f, 0f, 0f), Vector3.one,
                skinWarm, "nose");
            NoShadow(Part(headPivot.transform, PrimitiveType.Cube, new Vector3(0f, 0.105f, 0.096f),
                new Vector3(0.042f, 0.008f, 0.008f), lips, "mouth"));

            // ---- Мягкая тень-блоб ----
            GameObject shadow = new GameObject("Shadow");
            shadow.transform.SetParent(visualGo.transform, false);
            GameObject shadowQuad = PartRot(shadow.transform, PrimitiveType.Quad, Vector3.zero,
                new Vector3(1.0f, 0.72f, 1f), new Vector3(90f, 0f, 0f),
                MaterialFactory.UnlitTransparent(new Color(0f, 0f, 0f, 0.30f)), "shadowQuad");
            NoShadow(shadowQuad);

            visual.BodyRoot = bodyRoot.transform;
            visual.Head = headPivot.transform;
            visual.ArmLPivot = armL.transform;
            visual.ArmRPivot = armR.transform;
            visual.LegLPivot = legL.transform;
            visual.LegRPivot = legR.transform;
            visual.KneeLPivot = kneeL.transform;
            visual.KneeRPivot = kneeR.transform;
            visual.ElbowLPivot = elbowL.transform;
            visual.ElbowRPivot = elbowR.transform;
            visual.Briefcase = briefcase.transform;
            visual.Shadow = shadow.transform;
            visual.CacheRenderers();
            return visual;
        }

        // ------------------------------------------------------------------
        // Препятствия
        // ------------------------------------------------------------------

        public static Obstacle CreateObstacle(ObstacleSpec spec, LevelPalette pal)
        {
            GameObject root = new GameObject("obs_" + spec.Id);
            Obstacle obstacle = root.AddComponent<Obstacle>();
            obstacle.Spec = spec;
            GameObject body = Group(root.transform, "Body", Vector3.zero);
            obstacle.Body = body.transform;

            switch (spec.Kind)
            {
                case ObstacleKind.Jump:
                    obstacle.HalfExtents = new Vector3(0.95f, 0.28f, 0.45f);
                    obstacle.CenterOffset = new Vector3(0f, 0.28f, 0f);
                    break;
                case ObstacleKind.Slide:
                    obstacle.HalfExtents = new Vector3(0.95f, 0.42f, 0.35f);
                    obstacle.CenterOffset = new Vector3(0f, 1.45f, 0f);
                    break;
                default:
                    obstacle.HalfExtents = new Vector3(0.85f, 0.9f, 0.5f);
                    obstacle.CenterOffset = new Vector3(0f, 0.9f, 0f);
                    break;
            }

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = obstacle.CenterOffset;
            box.size = obstacle.HalfExtents * 2f;

            BuildObstacleVisual(spec, pal, body.transform);
            return obstacle;
        }

        private static void BuildObstacleVisual(ObstacleSpec spec, LevelPalette pal, Transform body)
        {
            switch (spec.Id)
            {
                case "folders": BuildFolderStack(body, pal, false); break;
                case "contracts": BuildFolderStack(body, pal, true); break;
                case "deadline": BuildStandingSign(body, pal, "ДЕДЛАЙН!", pal.Danger); break;
                case "nopower": BuildStandingSign(body, pal, "НЕТ ДОВЕРЕННОСТИ!", new Color(0.5f, 0.07f, 0.07f)); break;
                case "coffee": BuildCoffeeSpill(body); break;
                case "voice": BuildVoiceMessage(body); break;
                case "edits": BuildHangingPapers(body, pal, "ПРАВКИ"); break;
                case "oldlaw": BuildHangingBooks(body, pal); break;
                case "attachments": BuildPaperPiles(body); break;
                case "fraud": BuildBarrier(body, pal, "МОШЕННИЧЕСТВО ОБНАРУЖЕНО"); break;
                case "rumors": BuildPuddle(body, "слухи… слухи…"); break;
                case "transfer": BuildScamPhone(body, pal); break;
                case "wires": BuildWires(body); break;
                case "press": BuildPress(body, pal); break;
                case "atm": BuildAtm(body, pal, true); break;
                case "queue": BuildQueue(body); break;
                case "frame": BuildSecurityFrame(body, pal); break;
                case "bailiff": BuildBailiff(body, pal); break;
                case "skull": BuildSkullPassport(body); break;
                case "ailink": BuildAiLink(body, pal); break;
                case "fine": BuildFineBar(body, pal); break;
                // «Ядовитые» препятствия с дебаффами.
                case "pdf700": BuildPdf700(body); break;
                case "flip": BuildFlipBoard(body); break;
                case "kad502": BuildKadDown(body); break;
                case "monopoly": BuildMonopolyBanner(body); break;
                case "bribe": BuildBribeScales(body); break;
                case "chamber": BuildChamberDoor(body); break;
                case "badpoa": BuildBadPoaStamp(body); break;
                case "aiban": BuildAiBan(body); break;
                default: BuildStandingSign(body, pal, spec.Label, pal.Danger); break;
            }
        }

        // ------------------------------------------------------------------
        // «Ядовитые» препятствия
        // ------------------------------------------------------------------

        private static readonly Color Acid = new Color(0.55f, 0.95f, 0.25f);
        private static readonly Color AcidDark = new Color(0.07f, 0.11f, 0.02f);

        /// <summary>Кислотная зона на полу — общий маркер «ядовитого» препятствия.</summary>
        private static void AddToxicAura(Transform body, float radius)
        {
            GameObject zone = PartRot(body, PrimitiveType.Quad, new Vector3(0f, 0.032f, 0f),
                new Vector3(radius * 2f, radius * 2f, 1f), new Vector3(90f, 0f, 0f),
                MaterialFactory.UnlitTransparent(new Color(Acid.r, Acid.g, Acid.b, 0.16f)), "toxicZone");
            NoShadow(zone);
        }

        private static GameObject AcidTag(Transform body, string text, Vector3 pos, Vector2 size)
        {
            GameObject tag = Part(body, PrimitiveType.Cube, pos,
                new Vector3(size.x, size.y, 0.04f), MaterialFactory.Neon(Acid), "acidTag");
            NoShadow(tag);
            TextLabel(tag.transform, text, new Vector3(0f, 0f, -0.6f),
                Mathf.Min(0.55f, 3.4f / Mathf.Max(5, text.Length)), AcidDark, 0f);
            return tag;
        }

        /// <summary>«Подписка на ИИ забанена»: терминал с красным экраном отказа.</summary>
        private static void BuildAiBan(Transform body)
        {
            AddToxicAura(body, 1.35f);
            Material darkM = MaterialFactory.Solid(new Color(0.09f, 0.10f, 0.13f), 0.6f, 0.3f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f),
                new Vector3(1.5f, 1.8f, 0.5f), darkM, "tower");
            GameObject scr = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.1f, -0.26f),
                new Vector3(1.2f, 0.85f, 0.05f),
                MaterialFactory.Neon(new Color(0.95f, 0.15f, 0.10f)), "screen");
            NoShadow(scr);
            TextLabel(body, "ИИ", new Vector3(0f, 1.22f, -0.30f), 0.32f,
                new Color(0.25f, 0.02f, 0.02f), 0f);
            TextLabel(body, "ДОСТУП ЗАКРЫТ", new Vector3(0f, 0.84f, -0.30f), 0.09f,
                new Color(0.3f, 0.04f, 0.04f), 0f);
            // Перечёркнутый «бан».
            GameObject slash = PartRot(body, PrimitiveType.Cube, new Vector3(0f, 1.1f, -0.31f),
                new Vector3(1.1f, 0.09f, 0.02f), new Vector3(0f, 0f, 45f),
                MaterialFactory.Solid(new Color(0.98f, 0.95f, 0.90f), 0.4f, 0f), "slash");
            NoShadow(slash);
            AcidTag(body, "ПОДПИСКА ЗАБАНЕНА", new Vector3(0f, 2.25f, 0f), new Vector2(2.4f, 0.5f));
        }

        /// <summary>«700 страниц одним PDF»: гигантские пачки бумаги (прыжок).</summary>
        private static void BuildPdf700(Transform body)
        {
            Material paper = MaterialFactory.Solid(new Color(0.94f, 0.93f, 0.88f), 0.35f, 0f);
            Material paperDim = MaterialFactory.Solid(new Color(0.82f, 0.80f, 0.74f), 0.3f, 0f);
            for (int i = 0; i < 3; i++)
            {
                PartRot(body, PrimitiveType.Cube,
                    new Vector3(Mathf.Sin(i * 2.5f) * 0.06f, 0.09f + i * 0.17f, Mathf.Cos(i * 1.9f) * 0.04f),
                    new Vector3(1.5f - i * 0.07f, 0.16f, 0.9f),
                    new Vector3(0f, Mathf.Sin(i * 3.1f) * 6f, 0f),
                    i % 2 == 0 ? paper : paperDim, "ream" + i);
            }
            // Гигантский зажим сверху.
            Material clip = MaterialFactory.Solid(new Color(0.55f, 0.58f, 0.62f), 0.8f, 0.8f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.60f, 0f),
                new Vector3(0.5f, 0.08f, 0.5f), clip, "clip");
            PartRot(body, PrimitiveType.Cube, new Vector3(0f, 0.70f, 0f),
                new Vector3(0.34f, 0.16f, 0.06f), new Vector3(0f, 0f, 0f), clip, "clipHandle");
            AcidTag(body, "PDF · 700 стр.", new Vector3(0f, 0.42f, -0.5f), new Vector2(1.15f, 0.28f));
            AddToxicAura(body, 1.05f);
        }

        /// <summary>«Позиция поменялась»: верхнее табло со стрелками (подкат).</summary>
        private static void BuildFlipBoard(Transform body)
        {
            Material post = MaterialFactory.Solid(new Color(0.35f, 0.37f, 0.42f), 0.6f, 0.5f);
            Part(body, PrimitiveType.Cylinder, new Vector3(-1.0f, 0.95f, 0f),
                new Vector3(0.07f, 0.95f, 0.07f), post, "postL");
            Part(body, PrimitiveType.Cylinder, new Vector3(1.0f, 0.95f, 0f),
                new Vector3(0.07f, 0.95f, 0.07f), post, "postR");
            GameObject board = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.62f, 0f),
                new Vector3(2.05f, 0.62f, 0.1f),
                MaterialFactory.Solid(new Color(0.10f, 0.08f, 0.14f), 0.5f, 0.1f), "board");
            TextLabel(body, "ПОЗИЦИЯ ПОМЕНЯЛАСЬ", new Vector3(0f, 1.70f, -0.07f), 0.135f, Acid, 0f);
            // Круговые стрелки-«перевёртыши».
            Material arrow = MaterialFactory.Neon(Acid);
            for (int i = 0; i < 4; i++)
            {
                GameObject seg = PartRot(body, PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(i * Mathf.PI * 0.5f) * 0.22f,
                        1.44f + Mathf.Sin(i * Mathf.PI * 0.5f) * 0.13f, -0.07f),
                    new Vector3(0.16f, 0.045f, 0.03f),
                    new Vector3(0f, 0f, i * 90f + 45f), arrow, "arc" + i);
                NoShadow(seg);
            }
            NoShadow(board);
            AddToxicAura(body, 0.9f);
        }

        /// <summary>«КАД лёг»: упавшая серверная стойка картотеки (глухая).</summary>
        private static void BuildKadDown(Transform body)
        {
            Material rack = MaterialFactory.Solid(new Color(0.14f, 0.15f, 0.19f), 0.6f, 0.3f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f),
                new Vector3(1.05f, 1.7f, 0.6f), rack, "rack");
            GameObject screen = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.28f, -0.31f),
                new Vector3(0.8f, 0.5f, 0.04f), MaterialFactory.Neon(new Color(0.95f, 0.2f, 0.15f)), "screen");
            NoShadow(screen);
            TextLabel(body, "КАД: 502", new Vector3(0f, 1.33f, -0.35f), 0.15f, Color.white, 0f);
            TextLabel(body, "kad.arbitr.ru", new Vector3(0f, 1.13f, -0.35f), 0.09f,
                new Color(1f, 0.75f, 0.7f), 0f);
            // Мигающие диоды и «дымок».
            for (int i = 0; i < 3; i++)
            {
                GameObject led = Part(body, PrimitiveType.Sphere,
                    new Vector3(-0.3f + i * 0.3f, 0.72f, -0.31f),
                    new Vector3(0.07f, 0.07f, 0.07f), MaterialFactory.Neon(Acid), "led" + i);
                NoShadow(led);
            }
            for (int i = 0; i < 2; i++)
            {
                GameObject smoke = Part(body, PrimitiveType.Sphere,
                    new Vector3(0.2f - i * 0.35f, 1.78f + i * 0.16f, 0.1f),
                    new Vector3(0.22f + i * 0.1f, 0.16f, 0.2f),
                    MaterialFactory.UnlitTransparent(new Color(0.6f, 0.6f, 0.62f, 0.35f)), "smoke" + i);
                NoShadow(smoke);
            }
            AddToxicAura(body, 1.0f);
        }

        /// <summary>«Адвокатская монополия»: верхний транспарант Госдумы (подкат).</summary>
        private static void BuildMonopolyBanner(Transform body)
        {
            Material post = MaterialFactory.Solid(new Color(0.4f, 0.35f, 0.3f), 0.55f, 0.3f);
            Part(body, PrimitiveType.Cylinder, new Vector3(-1.0f, 0.95f, 0f),
                new Vector3(0.07f, 0.95f, 0.07f), post, "postL");
            Part(body, PrimitiveType.Cylinder, new Vector3(1.0f, 0.95f, 0f),
                new Vector3(0.07f, 0.95f, 0.07f), post, "postR");
            GameObject banner = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.58f, 0f),
                new Vector3(2.1f, 0.55f, 0.08f),
                MaterialFactory.Solid(new Color(0.45f, 0.08f, 0.10f), 0.45f, 0f), "banner");
            NoShadow(banner);
            TextLabel(body, "АДВОКАТСКАЯ МОНОПОЛИЯ", new Vector3(0f, 1.66f, -0.06f), 0.115f,
                new Color(1f, 0.9f, 0.75f), 0f);
            TextLabel(body, "первое чтение · Госдума", new Vector3(0f, 1.48f, -0.06f), 0.085f,
                new Color(1f, 0.72f, 0.6f), 0f);
            // Золотая печать.
            Material goldM = MaterialFactory.Solid(new Color(0.9f, 0.72f, 0.32f), 0.8f, 0.8f);
            GameObject seal = PartRot(body, PrimitiveType.Cylinder, new Vector3(0.85f, 1.42f, -0.05f),
                new Vector3(0.18f, 0.015f, 0.18f), new Vector3(90f, 0f, 0f), goldM, "seal");
            NoShadow(seal);
            AddToxicAura(body, 0.9f);
        }

        /// <summary>«Оппонент коррумпировал судью»: покосившиеся весы (глухая).</summary>
        private static void BuildBribeScales(Transform body)
        {
            Material bronze = MaterialFactory.Solid(new Color(0.55f, 0.42f, 0.2f), 0.75f, 0.7f);
            Material goldM = MaterialFactory.Solid(new Color(0.95f, 0.78f, 0.32f), 0.85f, 0.85f);
            Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.7f, 0f),
                new Vector3(0.12f, 0.7f, 0.12f), bronze, "pillar");
            Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f),
                new Vector3(0.55f, 0.06f, 0.55f), bronze, "base");
            // Сильно наклонённое коромысло.
            GameObject beam = PartRot(body, PrimitiveType.Cube, new Vector3(0f, 1.42f, 0f),
                new Vector3(1.7f, 0.06f, 0.06f), new Vector3(0f, 0f, -16f), bronze, "beam");
            // Чаши: левая перевешивает (набита «золотом»).
            GameObject panL = Part(body, PrimitiveType.Cylinder, new Vector3(-0.8f, 1.02f, 0f),
                new Vector3(0.42f, 0.03f, 0.42f), bronze, "panL");
            for (int i = 0; i < 3; i++)
            {
                Part(panL.transform, PrimitiveType.Cylinder,
                    new Vector3(Mathf.Sin(i * 2.4f) * 0.25f, 1.6f + i * 1.4f, Mathf.Cos(i * 1.8f) * 0.2f),
                    new Vector3(0.35f, 1.2f, 0.35f), goldM, "coin" + i);
            }
            Part(body, PrimitiveType.Cylinder, new Vector3(0.76f, 1.72f, 0f),
                new Vector3(0.42f, 0.03f, 0.42f), bronze, "panR");
            // Нити чаш.
            PartRot(body, PrimitiveType.Cylinder, new Vector3(-0.8f, 1.22f, 0f),
                new Vector3(0.02f, 0.2f, 0.02f), new Vector3(0f, 0f, 4f), bronze, "wireL");
            PartRot(body, PrimitiveType.Cylinder, new Vector3(0.76f, 1.56f, 0f),
                new Vector3(0.02f, 0.16f, 0.02f), new Vector3(0f, 0f, -4f), bronze, "wireR");
            AcidTag(body, "СУДЬЯ ВНЕЗАПНО ПРОТИВ", new Vector3(0f, 1.95f, 0f), new Vector2(1.7f, 0.26f));
            AddToxicAura(body, 1.05f);
        }

        /// <summary>«Судья ушёл в совещательную»: дверь с табличкой (глухая).</summary>
        private static void BuildChamberDoor(Transform body)
        {
            Material wood = MaterialFactory.Solid(new Color(0.33f, 0.21f, 0.11f), 0.6f, 0.05f);
            Material goldM = MaterialFactory.Solid(new Color(0.85f, 0.7f, 0.35f), 0.85f, 0.8f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.95f, 0f),
                new Vector3(1.35f, 1.9f, 0.14f), wood, "door");
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.95f, 0f),
                new Vector3(1.55f, 0.14f, 0.2f), goldM, "lintel");
            Part(body, PrimitiveType.Sphere, new Vector3(0.5f, 0.95f, -0.1f),
                new Vector3(0.09f, 0.09f, 0.09f), goldM, "knob");
            GameObject plate = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.45f, -0.09f),
                new Vector3(0.95f, 0.24f, 0.03f),
                MaterialFactory.Solid(new Color(0.55f, 0.08f, 0.08f), 0.5f, 0.2f), "plate");
            NoShadow(plate);
            TextLabel(body, "СОВЕЩАТЕЛЬНАЯ", new Vector3(0f, 1.45f, -0.12f), 0.10f,
                new Color(1f, 0.9f, 0.7f), 0f);
            // Табличка «не беспокоить» на ручке.
            GameObject tag = PartRot(body, PrimitiveType.Cube, new Vector3(0.5f, 0.68f, -0.1f),
                new Vector3(0.3f, 0.4f, 0.02f), new Vector3(0f, 0f, 7f),
                MaterialFactory.Solid(new Color(0.92f, 0.9f, 0.82f), 0.35f, 0f), "dndTag");
            TextLabel(tag.transform, "НЕ\nБЕСПОКОИТЬ", new Vector3(0f, 0f, -0.7f), 0.22f,
                new Color(0.35f, 0.1f, 0.1f), 0f);
            // Часы без стрелок — время здесь не действует.
            GameObject clock = PartRot(body, PrimitiveType.Cylinder, new Vector3(-0.45f, 1.7f, -0.08f),
                new Vector3(0.22f, 0.012f, 0.22f), new Vector3(90f, 0f, 0f),
                MaterialFactory.Solid(new Color(0.95f, 0.95f, 0.92f), 0.4f, 0f), "clock");
            NoShadow(clock);
            TextLabel(body, "?", new Vector3(-0.45f, 1.7f, -0.11f), 0.13f,
                new Color(0.25f, 0.25f, 0.3f), 0f);
            AddToxicAura(body, 1.0f);
        }

        /// <summary>«Доверенность без полномочий»: верхний штамп (подкат).</summary>
        private static void BuildBadPoaStamp(Transform body)
        {
            Material post = MaterialFactory.Solid(new Color(0.42f, 0.4f, 0.44f), 0.6f, 0.5f);
            Part(body, PrimitiveType.Cylinder, new Vector3(-1.0f, 0.95f, 0f),
                new Vector3(0.07f, 0.95f, 0.07f), post, "postL");
            Part(body, PrimitiveType.Cylinder, new Vector3(1.0f, 0.95f, 0f),
                new Vector3(0.07f, 0.95f, 0.07f), post, "postR");
            // Верхний «документ» с гигантской печатью.
            Material paper = MaterialFactory.Solid(new Color(0.95f, 0.94f, 0.88f), 0.35f, 0f);
            GameObject doc = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.6f, 0f),
                new Vector3(1.9f, 0.6f, 0.06f), paper, "doc");
            NoShadow(doc);
            TextLabel(body, "ДОВЕРЕННОСТЬ", new Vector3(0f, 1.74f, -0.05f), 0.11f,
                new Color(0.25f, 0.28f, 0.4f), 0f);
            Material red = MaterialFactory.Solid(new Color(0.8f, 0.12f, 0.1f), 0.5f, 0f);
            GameObject stamp = PartRot(body, PrimitiveType.Cylinder, new Vector3(0.02f, 1.52f, -0.05f),
                new Vector3(0.5f, 0.012f, 0.5f), new Vector3(90f, 0f, -14f), red, "stampRing");
            NoShadow(stamp);
            GameObject stampIn = PartRot(body, PrimitiveType.Cylinder, new Vector3(0.02f, 1.52f, -0.055f),
                new Vector3(0.42f, 0.012f, 0.42f), new Vector3(90f, 0f, -14f), paper, "stampIn");
            NoShadow(stampIn);
            TextLabel(body, "БЕЗ ПОЛНОМОЧИЙ", new Vector3(0.02f, 1.52f, -0.075f), 0.075f,
                new Color(0.8f, 0.12f, 0.1f), 0f);
            AddToxicAura(body, 0.9f);
        }

        private static void BuildFolderStack(Transform body, LevelPalette pal, bool blue)
        {
            Color a = blue ? new Color(0.85f, 0.88f, 0.95f) : new Color(0.93f, 0.82f, 0.55f);
            Color b = blue ? new Color(0.65f, 0.75f, 0.9f) : new Color(0.85f, 0.68f, 0.4f);
            for (int i = 0; i < 6; i++)
            {
                Material m = MaterialFactory.Solid(i % 2 == 0 ? a : b, 0.35f, 0f);
                PartRot(body, PrimitiveType.Cube,
                    new Vector3(Mathf.Sin(i * 2.1f) * 0.08f, 0.14f + i * 0.27f, Mathf.Cos(i * 1.7f) * 0.05f),
                    new Vector3(1.15f, 0.24f, 0.8f),
                    new Vector3(0f, Mathf.Sin(i * 3.3f) * 7f, 0f), m, "folder" + i);
            }
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.72f, 0f),
                new Vector3(0.9f, 0.05f, 0.65f),
                MaterialFactory.Solid(new Color(0.97f, 0.97f, 0.94f), 0.3f, 0f), "papersTop");
            GameObject plate = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.44f),
                new Vector3(0.7f, 0.3f, 0.04f), MaterialFactory.Neon(pal.Danger), "urgent");
            TextLabel(plate.transform, "СРОЧНО", new Vector3(0f, 0f, -0.6f), 0.16f, Color.white, 0f);
        }

        private static void BuildStandingSign(Transform body, LevelPalette pal, string text, Color boardColor)
        {
            Material metal = MaterialFactory.Solid(new Color(0.35f, 0.36f, 0.4f), 0.7f, 0.6f);
            PartRot(body, PrimitiveType.Cylinder, new Vector3(-0.5f, 0.6f, 0.1f),
                new Vector3(0.07f, 0.6f, 0.07f), new Vector3(8f, 0f, 0f), metal, "legL");
            PartRot(body, PrimitiveType.Cylinder, new Vector3(0.5f, 0.6f, 0.1f),
                new Vector3(0.07f, 0.6f, 0.07f), new Vector3(8f, 0f, 0f), metal, "legR");
            GameObject boardGo = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.35f, 0f),
                new Vector3(1.55f, 0.85f, 0.1f), MaterialFactory.Neon(boardColor), "board");
            TextLabel(body, text, new Vector3(0f, 1.42f, -0.08f),
                Mathf.Min(0.3f, 1.35f / Mathf.Max(5, text.Length)), Color.white, 0f);
            // Предупреждающие полосы внизу.
            for (int i = 0; i < 4; i++)
            {
                Color c = i % 2 == 0 ? Color.white : boardColor;
                Part(body, PrimitiveType.Cube, new Vector3(-0.58f + i * 0.39f, 0.98f, -0.005f),
                    new Vector3(0.38f, 0.12f, 0.11f), MaterialFactory.Solid(c, 0.4f, 0f), "stripe" + i);
            }
            NoShadow(boardGo);
        }

        private static void BuildCoffeeSpill(Transform body)
        {
            Material coffee = MaterialFactory.Solid(new Color(0.32f, 0.18f, 0.08f), 0.9f, 0.05f);
            Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0f),
                new Vector3(1.7f, 0.015f, 1.15f), coffee, "spill");
            Part(body, PrimitiveType.Cylinder, new Vector3(0.55f, 0.02f, 0.45f),
                new Vector3(0.5f, 0.012f, 0.4f), coffee, "spill2");
            // Упавший стакан.
            Material cup = MaterialFactory.Solid(new Color(0.95f, 0.93f, 0.9f), 0.5f, 0f);
            GameObject cupGo = PartRot(body, PrimitiveType.Cylinder, new Vector3(-0.55f, 0.14f, -0.2f),
                new Vector3(0.24f, 0.18f, 0.24f), new Vector3(0f, 0f, 90f), cup, "cup");
            Part(cupGo.transform, PrimitiveType.Cylinder, new Vector3(0f, 1.05f, 0f),
                new Vector3(1.05f, 0.1f, 1.05f),
                MaterialFactory.Solid(new Color(0.5f, 0.3f, 0.15f), 0.4f, 0f), "lid");
            for (int i = 0; i < 4; i++)
            {
                Part(body, PrimitiveType.Sphere,
                    new Vector3(Mathf.Sin(i * 1.9f) * 0.8f, 0.04f, Mathf.Cos(i * 2.4f) * 0.5f),
                    new Vector3(0.09f, 0.05f, 0.09f), coffee, "drop" + i);
            }
        }

        private static void BuildVoiceMessage(Transform body)
        {
            Color green = new Color(0.2f, 0.75f, 0.35f);
            GameObject bubble = Part(body, PrimitiveType.Cube, new Vector3(0f, 0.33f, 0f),
                new Vector3(1.35f, 0.45f, 0.22f), MaterialFactory.Neon(green), "bubble");
            NoShadow(bubble);
            PartRot(body, PrimitiveType.Cube, new Vector3(-0.55f, 0.09f, 0f),
                new Vector3(0.2f, 0.2f, 0.18f), new Vector3(0f, 0f, 45f),
                MaterialFactory.Neon(green), "tail");
            TextLabel(body, "Голосовое · 05:47", new Vector3(0f, 0.33f, -0.13f), 0.14f, Color.white, 0f);
            // «Волны» звука.
            for (int i = 0; i < 4; i++)
            {
                Part(body, PrimitiveType.Cube, new Vector3(0.35f + i * 0.12f, 0.52f, -0.05f),
                    new Vector3(0.05f, 0.1f + 0.06f * Mathf.PingPong(i, 2), 0.05f),
                    MaterialFactory.Solid(Color.white, 0.4f, 0f), "wave" + i);
            }
        }

        private static void BuildHangingPapers(Transform body, LevelPalette pal, string title)
        {
            Material bar = MaterialFactory.Solid(pal.Danger, 0.6f, 0.2f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.88f, 0f),
                new Vector3(2.0f, 0.09f, 0.09f), bar, "bar");
            Material post = MaterialFactory.Solid(new Color(0.4f, 0.4f, 0.45f), 0.6f, 0.5f);
            Part(body, PrimitiveType.Cylinder, new Vector3(-1.0f, 0.95f, 0f),
                new Vector3(0.07f, 0.95f, 0.07f), post, "postL");
            Part(body, PrimitiveType.Cylinder, new Vector3(1.0f, 0.95f, 0f),
                new Vector3(0.07f, 0.95f, 0.07f), post, "postR");
            Material paper = MaterialFactory.Solid(new Color(0.97f, 0.96f, 0.92f), 0.3f, 0f);
            for (int i = 0; i < 5; i++)
            {
                float x = -0.72f + i * 0.36f;
                GameObject sheet = PartRot(body, PrimitiveType.Cube, new Vector3(x, 1.55f, 0f),
                    new Vector3(0.3f, 0.44f, 0.02f), new Vector3(0f, 0f, Mathf.Sin(i * 2.7f) * 8f),
                    paper, "sheet" + i);
                for (int l = 0; l < 3; l++)
                {
                    Part(sheet.transform, PrimitiveType.Cube, new Vector3(0f, 0.25f - l * 0.22f, -0.55f),
                        new Vector3(0.7f, 0.05f, 0.1f),
                        MaterialFactory.Solid(new Color(0.75f, 0.2f, 0.2f), 0.3f, 0f), "line" + l);
                }
            }
            TextLabel(body, title, new Vector3(0f, 1.88f, -0.1f), 0.16f, Color.white, 0f);
        }

        private static void BuildHangingBooks(Transform body, LevelPalette pal)
        {
            Material bar = MaterialFactory.Solid(new Color(0.35f, 0.25f, 0.15f), 0.5f, 0.2f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.88f, 0f),
                new Vector3(2.0f, 0.09f, 0.09f), bar, "bar");
            Part(body, PrimitiveType.Cylinder, new Vector3(-1.0f, 0.95f, 0f),
                new Vector3(0.07f, 0.95f, 0.07f), bar, "postL");
            Part(body, PrimitiveType.Cylinder, new Vector3(1.0f, 0.95f, 0f),
                new Vector3(0.07f, 0.95f, 0.07f), bar, "postR");
            string[] years = { "1998", "2007", "2011" };
            for (int i = 0; i < 3; i++)
            {
                float x = -0.55f + i * 0.55f;
                Material book = MaterialFactory.Solid(
                    new Color(0.45f - i * 0.06f, 0.3f - i * 0.04f, 0.16f), 0.35f, 0f);
                GameObject b = PartRot(body, PrimitiveType.Cube, new Vector3(x, 1.42f, 0f),
                    new Vector3(0.42f, 0.55f, 0.14f), new Vector3(0f, 0f, Mathf.Sin(i * 4f) * 6f),
                    book, "tome" + i);
                Part(b.transform, PrimitiveType.Cube, new Vector3(0.04f, 0f, 0f),
                    new Vector3(0.92f, 0.94f, 0.85f),
                    MaterialFactory.Solid(new Color(0.9f, 0.86f, 0.75f), 0.3f, 0f), "pages");
                TextLabel(b.transform, years[i], new Vector3(0f, 0f, -0.65f), 0.55f,
                    new Color(0.95f, 0.85f, 0.6f), 0f);
            }
            TextLabel(body, "УСТАРЕЛО", new Vector3(0f, 1.9f, -0.1f), 0.15f, pal.SignText, 0f);
        }

        private static void BuildPaperPiles(Transform body)
        {
            Material paper = MaterialFactory.Solid(new Color(0.96f, 0.95f, 0.9f), 0.3f, 0f);
            Material folder = MaterialFactory.Solid(new Color(0.9f, 0.75f, 0.45f), 0.35f, 0f);
            PartRot(body, PrimitiveType.Cube, new Vector3(-0.55f, 0.14f, 0.1f),
                new Vector3(0.55f, 0.28f, 0.5f), new Vector3(0f, 14f, 0f), paper, "pile1");
            PartRot(body, PrimitiveType.Cube, new Vector3(0.25f, 0.18f, -0.1f),
                new Vector3(0.6f, 0.36f, 0.55f), new Vector3(0f, -20f, 0f), folder, "pile2");
            PartRot(body, PrimitiveType.Cube, new Vector3(0.75f, 0.1f, 0.2f),
                new Vector3(0.45f, 0.2f, 0.4f), new Vector3(0f, 33f, 0f), paper, "pile3");
            GameObject tab = Part(body, PrimitiveType.Cube, new Vector3(0.25f, 0.45f, -0.1f),
                new Vector3(0.5f, 0.16f, 0.03f),
                MaterialFactory.Neon(new Color(0.9f, 0.25f, 0.2f)), "tab");
            TextLabel(tab.transform, "ПРИЛОЖЕНИЯ?", new Vector3(0f, 0f, -0.7f), 0.4f, Color.white, 0f);
        }

        private static void BuildBarrier(Transform body, LevelPalette pal, string text)
        {
            Material metal = MaterialFactory.Solid(new Color(0.5f, 0.52f, 0.55f), 0.6f, 0.5f);
            PartRot(body, PrimitiveType.Cube, new Vector3(-0.8f, 0.5f, 0.12f),
                new Vector3(0.08f, 1.05f, 0.08f), new Vector3(12f, 0f, 0f), metal, "legL1");
            PartRot(body, PrimitiveType.Cube, new Vector3(-0.8f, 0.5f, -0.12f),
                new Vector3(0.08f, 1.05f, 0.08f), new Vector3(-12f, 0f, 0f), metal, "legL2");
            PartRot(body, PrimitiveType.Cube, new Vector3(0.8f, 0.5f, 0.12f),
                new Vector3(0.08f, 1.05f, 0.08f), new Vector3(12f, 0f, 0f), metal, "legR1");
            PartRot(body, PrimitiveType.Cube, new Vector3(0.8f, 0.5f, -0.12f),
                new Vector3(0.08f, 1.05f, 0.08f), new Vector3(-12f, 0f, 0f), metal, "legR2");
            // Полосатая перекладина.
            for (int i = 0; i < 6; i++)
            {
                Color c = i % 2 == 0 ? pal.Danger : Color.white;
                Part(body, PrimitiveType.Cube, new Vector3(-0.75f + i * 0.3f, 0.95f, 0f),
                    new Vector3(0.3f, 0.28f, 0.1f), MaterialFactory.Solid(c, 0.45f, 0f), "seg" + i);
            }
            GameObject banner = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.5f, 0f),
                new Vector3(1.75f, 0.5f, 0.06f), MaterialFactory.Neon(pal.Danger), "banner");
            NoShadow(banner);
            TextLabel(body, text, new Vector3(0f, 1.5f, -0.06f), 0.115f, Color.white, 0f);
            Part(body, PrimitiveType.Sphere, new Vector3(-0.8f, 1.12f, 0f),
                new Vector3(0.14f, 0.14f, 0.14f), MaterialFactory.Neon(pal.Danger), "lampL");
            Part(body, PrimitiveType.Sphere, new Vector3(0.8f, 1.12f, 0f),
                new Vector3(0.14f, 0.14f, 0.14f), MaterialFactory.Neon(pal.Danger), "lampR");
        }

        private static void BuildPuddle(Transform body, string text)
        {
            Material water = MaterialFactory.Solid(new Color(0.35f, 0.42f, 0.5f), 0.95f, 0.1f);
            Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0f),
                new Vector3(1.85f, 0.015f, 1.2f), water, "puddle");
            Part(body, PrimitiveType.Cylinder, new Vector3(0.7f, 0.02f, -0.35f),
                new Vector3(0.6f, 0.012f, 0.45f), water, "puddle2");
            for (int i = 0; i < 3; i++)
            {
                Part(body, PrimitiveType.Sphere,
                    new Vector3(-0.5f + i * 0.5f, 0.06f, Mathf.Sin(i * 3f) * 0.3f),
                    new Vector3(0.12f, 0.1f, 0.12f),
                    MaterialFactory.Solid(new Color(0.55f, 0.62f, 0.7f), 0.9f, 0f), "bubble" + i);
            }
            TextLabel(body, text, new Vector3(0f, 0.12f, -0.9f), 0.14f,
                new Color(0.25f, 0.3f, 0.38f), 0f);
        }

        private static void BuildScamPhone(Transform body, LevelPalette pal)
        {
            Material dark = MaterialFactory.Solid(new Color(0.12f, 0.12f, 0.16f), 0.7f, 0.3f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f),
                new Vector3(0.95f, 1.7f, 0.18f), dark, "phone");
            GameObject screen = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.06f),
                new Vector3(0.78f, 1.15f, 0.1f),
                MaterialFactory.Emissive(new Color(0.9f, 0.9f, 0.95f), new Color(0.85f, 0.9f, 1f), 1.1f),
                "screen");
            NoShadow(screen);
            TextLabel(body, "Подтвердить\nперевод?", new Vector3(0f, 1.25f, -0.13f), 0.13f,
                new Color(0.12f, 0.12f, 0.2f), 0f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.62f, -0.12f),
                new Vector3(0.5f, 0.18f, 0.05f), MaterialFactory.Neon(pal.Danger), "btn");
            TextLabel(body, "ДА", new Vector3(0f, 0.62f, -0.16f), 0.11f, Color.white, 0f);
            Part(body, PrimitiveType.Sphere, new Vector3(0.35f, 1.78f, -0.05f),
                new Vector3(0.12f, 0.12f, 0.12f), MaterialFactory.Neon(pal.Danger), "alert");
        }

        private static void BuildWires(Transform body)
        {
            Material wire = MaterialFactory.Solid(new Color(0.08f, 0.08f, 0.1f), 0.55f, 0.1f);
            for (int i = 0; i < 5; i++)
            {
                PartRot(body, PrimitiveType.Cube,
                    new Vector3(0f, 0.12f + 0.07f * i, Mathf.Sin(i * 2.2f) * 0.25f),
                    new Vector3(1.9f, 0.05f, 0.05f),
                    new Vector3(0f, Mathf.Sin(i * 3.1f) * 22f, Mathf.Cos(i * 1.4f) * 10f),
                    wire, "wire" + i);
            }
            for (int i = 0; i < 3; i++)
            {
                Part(body, PrimitiveType.Sphere,
                    new Vector3(-0.6f + i * 0.6f, 0.22f, Mathf.Cos(i * 2.6f) * 0.2f),
                    new Vector3(0.13f, 0.13f, 0.13f),
                    MaterialFactory.Neon(new Color(0.95f, 0.2f, 0.15f)), "node" + i);
            }
            GameObject tag = Part(body, PrimitiveType.Cube, new Vector3(0.4f, 0.5f, 0f),
                new Vector3(0.66f, 0.2f, 0.03f),
                MaterialFactory.Solid(new Color(0.95f, 0.9f, 0.7f), 0.3f, 0f), "tag");
            TextLabel(tag.transform, "неизвестный номер", new Vector3(0f, 0f, -0.6f), 0.35f,
                new Color(0.3f, 0.25f, 0.15f), 0f);
        }

        private static void BuildPress(Transform body, LevelPalette pal)
        {
            CreatePerson(body, new Vector3(-1.05f, 0f, 0.15f),
                new Color(0.25f, 0.3f, 0.38f), new Color(0.15f, 0.15f, 0.2f), 1.55f, 35f);
            CreatePerson(body, new Vector3(1.05f, 0f, -0.1f),
                new Color(0.5f, 0.25f, 0.3f), new Color(0.2f, 0.2f, 0.25f), 1.6f, -30f);
            Material pole = MaterialFactory.Solid(new Color(0.2f, 0.2f, 0.24f), 0.6f, 0.4f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.5f, 0f),
                new Vector3(2.15f, 0.07f, 0.07f), pole, "boom");
            Part(body, PrimitiveType.Sphere, new Vector3(-0.2f, 1.38f, 0f),
                new Vector3(0.2f, 0.26f, 0.2f),
                MaterialFactory.Solid(new Color(0.35f, 0.35f, 0.4f), 0.3f, 0f), "mic");
            GameObject camBox = Part(body, PrimitiveType.Cube, new Vector3(0.75f, 1.62f, 0f),
                new Vector3(0.3f, 0.22f, 0.35f), pole, "camera");
            Part(camBox.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.6f),
                new Vector3(0.5f, 0.35f, 0.5f), pole, "lens");
            Part(camBox.transform, PrimitiveType.Sphere, new Vector3(0.4f, 0.4f, 0f),
                new Vector3(0.25f, 0.25f, 0.25f), MaterialFactory.Neon(pal.Danger), "rec");
            TextLabel(body, "ПРЕССА", new Vector3(0f, 1.78f, -0.08f), 0.14f, pal.SignText, 0f);
        }

        private static void BuildAtm(Transform body, LevelPalette pal, bool broken)
        {
            Material metal = MaterialFactory.Solid(new Color(0.55f, 0.58f, 0.62f), 0.65f, 0.6f);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f),
                new Vector3(0.95f, 1.7f, 0.6f), metal, "case");
            Color screenC = broken ? pal.Danger : new Color(0.3f, 0.7f, 0.9f);
            GameObject screen = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.25f, -0.29f),
                new Vector3(0.6f, 0.45f, 0.06f), MaterialFactory.Neon(screenC), "screen");
            NoShadow(screen);
            TextLabel(body, broken ? "ОШИБКА" : "БАНК", new Vector3(0f, 1.25f, -0.34f), 0.13f, Color.white, 0f);
            for (int i = 0; i < 6; i++)
            {
                Part(body, PrimitiveType.Cube,
                    new Vector3(-0.12f + (i % 3) * 0.12f, 0.86f - (i / 3) * 0.1f, -0.3f),
                    new Vector3(0.08f, 0.06f, 0.03f),
                    MaterialFactory.Solid(new Color(0.85f, 0.85f, 0.85f), 0.4f, 0f), "key" + i);
            }
            Part(body, PrimitiveType.Cube, new Vector3(0.25f, 0.68f, -0.3f),
                new Vector3(0.22f, 0.04f, 0.04f),
                MaterialFactory.Solid(new Color(0.1f, 0.1f, 0.12f), 0.4f, 0f), "slot");
        }

        private static void BuildQueue(Transform body)
        {
            CreatePerson(body, new Vector3(-0.45f, 0f, -0.3f),
                new Color(0.55f, 0.45f, 0.35f), new Color(0.25f, 0.22f, 0.2f), 1.6f, 12f);
            CreatePerson(body, new Vector3(0.35f, 0f, 0.1f),
                new Color(0.3f, 0.4f, 0.55f), new Color(0.18f, 0.18f, 0.22f), 1.72f, -8f);
            CreatePerson(body, new Vector3(-0.1f, 0f, 0.45f),
                new Color(0.6f, 0.3f, 0.3f), new Color(0.3f, 0.3f, 0.35f), 1.5f, 25f);
            GameObject tag = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.95f, 0f),
                new Vector3(0.9f, 0.24f, 0.04f),
                MaterialFactory.Neon(new Color(0.85f, 0.6f, 0.15f)), "tag");
            TextLabel(tag.transform, "ОЧЕРЕДЬ", new Vector3(0f, 0f, -0.6f), 0.55f, Color.white, 0f);
        }

        private static void BuildSecurityFrame(Transform body, LevelPalette pal)
        {
            Material metal = MaterialFactory.Solid(new Color(0.45f, 0.47f, 0.5f), 0.7f, 0.7f);
            Part(body, PrimitiveType.Cube, new Vector3(-0.95f, 1.05f, 0f),
                new Vector3(0.18f, 2.1f, 0.35f), metal, "colL");
            Part(body, PrimitiveType.Cube, new Vector3(0.95f, 1.05f, 0f),
                new Vector3(0.18f, 2.1f, 0.35f), metal, "colR");
            Part(body, PrimitiveType.Cube, new Vector3(0f, 1.55f, 0f),
                new Vector3(2.08f, 0.75f, 0.35f), metal, "beam");
            // Красные лампы на перекладине.
            for (int i = 0; i < 4; i++)
            {
                Part(body, PrimitiveType.Sphere, new Vector3(-0.6f + i * 0.4f, 1.42f, -0.16f),
                    new Vector3(0.1f, 0.1f, 0.1f), MaterialFactory.Neon(pal.Danger), "lamp" + i);
            }
            GameObject band = Part(body, PrimitiveType.Cube, new Vector3(0f, 1.78f, -0.19f),
                new Vector3(1.9f, 0.22f, 0.02f), MaterialFactory.Neon(pal.Danger), "band");
            NoShadow(band);
            TextLabel(body, "КОНТРОЛЬ", new Vector3(0f, 1.78f, -0.21f), 0.14f, Color.white, 0f);
        }

        private static void BuildBailiff(Transform body, LevelPalette pal)
        {
            GameObject person = CreatePerson(body, Vector3.zero,
                new Color(0.12f, 0.14f, 0.2f), new Color(0.1f, 0.11f, 0.15f), 1.8f, 0f);
            // Фуражка.
            Part(person.transform, PrimitiveType.Cylinder, new Vector3(0f, 1.92f, 0f),
                new Vector3(0.34f, 0.05f, 0.34f),
                MaterialFactory.Solid(new Color(0.1f, 0.12f, 0.18f), 0.4f, 0f), "capBrim");
            Part(person.transform, PrimitiveType.Cylinder, new Vector3(0f, 1.99f, 0.02f),
                new Vector3(0.26f, 0.08f, 0.26f),
                MaterialFactory.Solid(new Color(0.1f, 0.12f, 0.18f), 0.4f, 0f), "capTop");
            // Значок и красная зона.
            Part(person.transform, PrimitiveType.Sphere, new Vector3(0.12f, 1.35f, -0.17f),
                new Vector3(0.08f, 0.1f, 0.03f),
                MaterialFactory.Solid(new Color(0.95f, 0.8f, 0.35f), 0.8f, 0.8f), "badge");
            GameObject zone = PartRot(body, PrimitiveType.Quad, new Vector3(0f, 0.03f, 0f),
                new Vector3(2.1f, 2.1f, 1f), new Vector3(90f, 0f, 0f),
                MaterialFactory.UnlitTransparent(new Color(pal.Danger.r, pal.Danger.g, pal.Danger.b, 0.22f)),
                "zone");
            NoShadow(zone);
            TextLabel(body, "ПРИСТАВ", new Vector3(0f, 2.2f, -0.05f), 0.15f, pal.SignText, 0f);
        }

        private static void BuildSkullPassport(Transform body)
        {
            Material cover = MaterialFactory.Solid(new Color(0.55f, 0.08f, 0.1f), 0.6f, 0.1f);
            GameObject book = PartRot(body, PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f),
                new Vector3(1.0f, 0.2f, 0.72f), new Vector3(0f, 12f, 0f), cover, "passport");
            Part(book.transform, PrimitiveType.Cube, new Vector3(0f, 0.52f, 0f),
                new Vector3(0.96f, 0.06f, 0.96f),
                MaterialFactory.Solid(new Color(0.92f, 0.88f, 0.8f), 0.3f, 0f), "pages");
            // Череп из простых форм.
            GameObject skull = Group(book.transform, "skull", new Vector3(0f, 0.62f, 0f));
            Material bone = MaterialFactory.Emissive(new Color(0.95f, 0.95f, 0.9f),
                new Color(1f, 0.9f, 0.8f), 0.5f);
            Part(skull.transform, PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0f),
                new Vector3(0.34f, 0.3f, 0.25f), bone, "cranium");
            Part(skull.transform, PrimitiveType.Cube, new Vector3(0f, -0.06f, 0f),
                new Vector3(0.2f, 0.12f, 0.2f), bone, "jaw");
            Material dark = MaterialFactory.Solid(new Color(0.1f, 0.02f, 0.02f), 0.3f, 0f);
            Part(skull.transform, PrimitiveType.Sphere, new Vector3(-0.08f, 0.12f, -0.11f),
                new Vector3(0.09f, 0.1f, 0.06f), dark, "eyeL");
            Part(skull.transform, PrimitiveType.Sphere, new Vector3(0.08f, 0.12f, -0.11f),
                new Vector3(0.09f, 0.1f, 0.06f), dark, "eyeR");
            TextLabel(body, "«сувенирная» обложка", new Vector3(0f, 0.95f, -0.2f), 0.11f,
                new Color(1f, 0.5f, 0.4f), 0f);
        }

        private static void BuildAiLink(Transform body, LevelPalette pal)
        {
            GameObject card = Part(body, PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f),
                new Vector3(1.25f, 0.5f, 0.08f),
                MaterialFactory.Solid(new Color(0.12f, 0.05f, 0.07f), 0.5f, 0.1f), "card");
            GameObject frame = Part(card.transform, PrimitiveType.Cube, Vector3.zero,
                new Vector3(1.04f, 1.1f, 0.8f), MaterialFactory.Neon(pal.Danger), "frame");
            NoShadow(frame);
            Part(card.transform, PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.98f, 1.0f, 1.05f),
                MaterialFactory.Solid(new Color(0.12f, 0.05f, 0.07f), 0.5f, 0.1f), "inner");
            TextLabel(body, "Фейковая ссылка ИИ", new Vector3(0f, 0.44f, -0.06f), 0.11f,
                new Color(1f, 0.55f, 0.5f), 0f);
            TextLabel(body, "sud-praktika-2024.fake/…", new Vector3(0f, 0.24f, -0.06f), 0.08f,
                new Color(1f, 0.35f, 0.3f), 0f);
            // «Глючные» кубики.
            for (int i = 0; i < 4; i++)
            {
                GameObject g = Part(body, PrimitiveType.Cube,
                    new Vector3(Mathf.Sin(i * 2.4f) * 0.7f, 0.55f + 0.12f * i, -0.05f),
                    new Vector3(0.07f, 0.07f, 0.07f), MaterialFactory.Neon(pal.Danger), "glitch" + i);
                NoShadow(g);
            }
        }

        private static void BuildFineBar(Transform body, LevelPalette pal)
        {
            GameObject bar = Part(body, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f),
                new Vector3(1.65f, 0.38f, 0.16f), MaterialFactory.Neon(pal.Danger), "bar");
            NoShadow(bar);
            TextLabel(body, "ШТРАФ", new Vector3(0f, 0.3f, -0.1f), 0.22f, Color.white, 0f);
            Part(body, PrimitiveType.Cube, new Vector3(-0.7f, 0.09f, 0f),
                new Vector3(0.12f, 0.18f, 0.12f),
                MaterialFactory.Solid(new Color(0.3f, 0.3f, 0.32f), 0.5f, 0.3f), "footL");
            Part(body, PrimitiveType.Cube, new Vector3(0.7f, 0.09f, 0f),
                new Vector3(0.12f, 0.18f, 0.12f),
                MaterialFactory.Solid(new Color(0.3f, 0.3f, 0.32f), 0.5f, 0.3f), "footR");
        }

        // ------------------------------------------------------------------
        // Бусты
        // ------------------------------------------------------------------

        public static Pickup CreatePickup(PickupSpec spec, LevelPalette pal)
        {
            GameObject root = new GameObject("pick_" + spec.Id);
            Pickup pickup = root.AddComponent<Pickup>();
            pickup.Spec = spec;
            bool super = spec.Super;
            pickup.HalfExtents = super ? new Vector3(0.85f, 0.95f, 0.7f) : new Vector3(0.7f, 0.85f, 0.6f);
            pickup.CenterOffset = new Vector3(0f, 1.05f, 0f);

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = pickup.CenterOffset;
            box.size = pickup.HalfExtents * 2f;

            Color glow = super ? new Color(1f, 0.82f, 0.30f)
                : (spec.Secondary ? pal.BoostSecondary : pal.BoostPrimary);

            GameObject core = Group(root.transform, "Core", Vector3.zero);
            pickup.Core = core;

            // Светящееся основание (у суперприза — шире и ярче).
            float ringScale = super ? 1.55f : 1.15f;
            GameObject baseRing = Part(core.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f),
                new Vector3(ringScale, 0.02f, ringScale), MaterialFactory.Neon(glow), "base");
            NoShadow(baseRing);
            // Аддитивное «отражение» в глянце пола: мягкое пятно, вытянутое вдоль
            // дорожки (как блик на мокром асфальте в референсе), цветёт в bloom.
            float haloScale = super ? 3.2f : 2.2f;
            GameObject floorHalo = PartRot(core.transform, PrimitiveType.Quad, new Vector3(0f, 0.045f, 0f),
                new Vector3(haloScale, haloScale * 1.5f, 1f), new Vector3(90f, 0f, 0f),
                MaterialFactory.AdditiveGlow(new Color(glow.r, glow.g, glow.b, super ? 0.55f : 0.38f)),
                "floorHalo");
            NoShadow(floorHalo);

            // Вертикальный луч (два скрещенных квада, аддитивный столб света).
            Color beamC = new Color(glow.r, glow.g, glow.b, super ? 0.62f : 0.46f);
            Material beamMat = MaterialFactory.AdditiveBeam(beamC);
            float beamW = super ? 1.5f : 1.05f;
            float beamH = super ? 7.4f : 5.8f;
            GameObject beam1 = PartRot(core.transform, PrimitiveType.Quad, new Vector3(0f, beamH * 0.47f, 0f),
                new Vector3(beamW, beamH, 1f), new Vector3(0f, 0f, 0f), beamMat, "beam1");
            GameObject beam2 = PartRot(core.transform, PrimitiveType.Quad, new Vector3(0f, beamH * 0.47f, 0f),
                new Vector3(beamW, beamH, 1f), new Vector3(0f, 90f, 0f), beamMat, "beam2");
            NoShadow(beam1);
            NoShadow(beam2);

            // Вращающийся предмет.
            GameObject item = Group(core.transform, "Item", new Vector3(0f, super ? 1.25f : 1.1f, 0f));
            if (super) BuildSuperItem(item.transform);
            else BuildPickupItem(spec, pal, glow, item.transform);

            FloatingPickupEffect fx = core.AddComponent<FloatingPickupEffect>();
            fx.Target = item.transform;
            if (super) fx.SpinSpeed = 150f;

            // Подпись.
            float plateW = super ? 1.9f : 1.35f;
            GameObject plate = Part(core.transform, PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f),
                new Vector3(plateW, super ? 0.28f : 0.22f, 0.02f),
                MaterialFactory.UnlitTransparent(new Color(0f, 0f, 0f, 0.55f)), "labelPlate");
            NoShadow(plate);
            TextLabel(core.transform, spec.Label, new Vector3(0f, 0.38f, -0.03f),
                super ? 0.16f : 0.13f, super ? new Color(1f, 0.9f, 0.55f) : Color.white, 0f);

            return pickup;
        }

        /// <summary>Суперприз: золотая звезда из перекрещенных лучей.</summary>
        private static void BuildSuperItem(Transform item)
        {
            Material glowM = MaterialFactory.Neon(new Color(1f, 0.85f, 0.35f));
            Material goldM = MaterialFactory.Solid(new Color(0.98f, 0.8f, 0.3f), 0.9f, 0.9f);
            for (int i = 0; i < 4; i++)
            {
                float len = i % 2 == 0 ? 1.0f : 0.66f;
                GameObject ray = PartRot(item, PrimitiveType.Cube, Vector3.zero,
                    new Vector3(0.13f, len, 0.09f), new Vector3(0f, 0f, i * 45f),
                    i % 2 == 0 ? glowM : goldM, "ray" + i);
                if (i % 2 == 0) NoShadow(ray);
            }
            GameObject corePart = Part(item, PrimitiveType.Sphere, Vector3.zero,
                new Vector3(0.3f, 0.3f, 0.3f), glowM, "core");
            NoShadow(corePart);
        }

        private static void BuildPickupItem(PickupSpec spec, LevelPalette pal, Color glow, Transform item)
        {
            Material goldM = MaterialFactory.Solid(new Color(0.95f, 0.78f, 0.32f), 0.85f, 0.85f);
            Material paperM = MaterialFactory.Solid(new Color(0.97f, 0.95f, 0.88f), 0.35f, 0f);

            switch (spec.Id)
            {
                case "power":
                case "egrn":
                case "notary":
                {
                    // Документ с печатью/лентой.
                    Part(item, PrimitiveType.Cube, Vector3.zero, new Vector3(0.58f, 0.78f, 0.05f), paperM, "doc");
                    Color headC = spec.Id == "egrn" ? new Color(0.2f, 0.45f, 0.8f) : new Color(0.75f, 0.15f, 0.15f);
                    Part(item, PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.01f),
                        new Vector3(0.5f, 0.12f, 0.05f), MaterialFactory.Solid(headC, 0.4f, 0f), "head");
                    for (int i = 0; i < 3; i++)
                    {
                        Part(item, PrimitiveType.Cube, new Vector3(0f, 0.1f - i * 0.14f, -0.01f),
                            new Vector3(0.44f, 0.04f, 0.05f),
                            MaterialFactory.Solid(new Color(0.6f, 0.6f, 0.62f), 0.3f, 0f), "line" + i);
                    }
                    Part(item, PrimitiveType.Cylinder, new Vector3(0.16f, -0.26f, -0.02f),
                        new Vector3(0.16f, 0.02f, 0.16f), goldM, "seal");
                    break;
                }
                case "coffeecup":
                {
                    Material cup = MaterialFactory.Solid(new Color(0.96f, 0.94f, 0.9f), 0.5f, 0f);
                    Part(item, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.34f, 0.26f, 0.34f), cup, "cup");
                    Part(item, PrimitiveType.Cylinder, new Vector3(0f, 0.27f, 0f),
                        new Vector3(0.37f, 0.03f, 0.37f),
                        MaterialFactory.Solid(new Color(0.45f, 0.28f, 0.14f), 0.5f, 0f), "lid");
                    Part(item, PrimitiveType.Cube, new Vector3(0f, -0.02f, -0.16f),
                        new Vector3(0.36f, 0.14f, 0.04f),
                        MaterialFactory.Solid(new Color(0.5f, 0.32f, 0.18f), 0.4f, 0f), "band");
                    for (int i = 0; i < 3; i++)
                    {
                        GameObject steam = Part(item, PrimitiveType.Sphere,
                            new Vector3(-0.08f + i * 0.08f, 0.42f + i * 0.1f, 0f),
                            new Vector3(0.08f - i * 0.015f, 0.08f, 0.08f),
                            MaterialFactory.UnlitTransparent(new Color(1f, 1f, 1f, 0.4f)), "steam" + i);
                        NoShadow(steam);
                    }
                    break;
                }
                case "casefile":
                case "perfectcase":
                {
                    Material folderM = spec.Id == "perfectcase" ? goldM
                        : MaterialFactory.Solid(new Color(0.9f, 0.76f, 0.45f), 0.4f, 0f);
                    PartRot(item, PrimitiveType.Cube, new Vector3(-0.05f, 0f, 0.03f),
                        new Vector3(0.62f, 0.8f, 0.04f), new Vector3(0f, 8f, 0f), folderM, "back");
                    PartRot(item, PrimitiveType.Cube, new Vector3(0.05f, -0.04f, -0.03f),
                        new Vector3(0.62f, 0.72f, 0.04f), new Vector3(0f, -8f, 0f), folderM, "front");
                    Part(item, PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f),
                        new Vector3(0.55f, 0.72f, 0.03f), paperM, "papers");
                    break;
                }
                case "call":
                case "client":
                {
                    if (spec.Id == "call")
                    {
                        Part(item, PrimitiveType.Cube, Vector3.zero,
                            new Vector3(0.38f, 0.66f, 0.07f),
                            MaterialFactory.Solid(new Color(0.12f, 0.12f, 0.16f), 0.7f, 0.2f), "phone");
                        GameObject scr = Part(item, PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.025f),
                            new Vector3(0.32f, 0.52f, 0.03f), MaterialFactory.Neon(pal.BoostSecondary), "scr");
                        NoShadow(scr);
                    }
                    else
                    {
                        Part(item, PrimitiveType.Sphere, Vector3.zero,
                            new Vector3(0.5f, 0.5f, 0.5f), MaterialFactory.Neon(pal.BoostSecondary), "face");
                        Material dark = MaterialFactory.Solid(new Color(0.1f, 0.2f, 0.12f), 0.3f, 0f);
                        Part(item, PrimitiveType.Sphere, new Vector3(-0.1f, 0.08f, -0.21f),
                            new Vector3(0.07f, 0.09f, 0.05f), dark, "eyeL");
                        Part(item, PrimitiveType.Sphere, new Vector3(0.1f, 0.08f, -0.21f),
                            new Vector3(0.07f, 0.09f, 0.05f), dark, "eyeR");
                        PartRot(item, PrimitiveType.Cube, new Vector3(0f, -0.1f, -0.22f),
                            new Vector3(0.2f, 0.04f, 0.04f), new Vector3(0f, 0f, 0f), dark, "smile");
                    }
                    break;
                }
                case "plan":
                {
                    Part(item, PrimitiveType.Cube, Vector3.zero, new Vector3(0.58f, 0.78f, 0.05f), paperM, "board");
                    for (int i = 0; i < 3; i++)
                    {
                        Part(item, PrimitiveType.Cube, new Vector3(-0.18f, 0.2f - i * 0.2f, -0.01f),
                            new Vector3(0.1f, 0.1f, 0.05f),
                            MaterialFactory.Neon(pal.BoostSecondary), "check" + i);
                        Part(item, PrimitiveType.Cube, new Vector3(0.08f, 0.2f - i * 0.2f, -0.01f),
                            new Vector3(0.3f, 0.05f, 0.05f),
                            MaterialFactory.Solid(new Color(0.55f, 0.55f, 0.58f), 0.3f, 0f), "row" + i);
                    }
                    break;
                }
                case "keys":
                {
                    GameObject key1 = Group(item, "key1", new Vector3(-0.05f, 0f, 0f));
                    key1.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
                    Part(key1.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0f),
                        new Vector3(0.22f, 0.03f, 0.22f), goldM, "ring");
                    Part(key1.transform, PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f),
                        new Vector3(0.07f, 0.5f, 0.05f), goldM, "shaft");
                    Part(key1.transform, PrimitiveType.Cube, new Vector3(0.07f, -0.3f, 0f),
                        new Vector3(0.12f, 0.05f, 0.05f), goldM, "tooth1");
                    Part(key1.transform, PrimitiveType.Cube, new Vector3(0.06f, -0.2f, 0f),
                        new Vector3(0.1f, 0.05f, 0.05f), goldM, "tooth2");
                    GameObject key2 = Group(item, "key2", new Vector3(0.14f, -0.02f, 0.05f));
                    key2.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
                    Part(key2.transform, PrimitiveType.Cube, new Vector3(0f, -0.05f, 0f),
                        new Vector3(0.06f, 0.4f, 0.04f), goldM, "shaft2");
                    Part(key2.transform, PrimitiveType.Cube, new Vector3(0.05f, -0.22f, 0f),
                        new Vector3(0.1f, 0.04f, 0.04f), goldM, "tooth3");
                    break;
                }
                case "argument":
                case "speech":
                {
                    Part(item, PrimitiveType.Cube, Vector3.zero,
                        new Vector3(0.6f, 0.8f, 0.06f),
                        MaterialFactory.Solid(new Color(0.1f, 0.14f, 0.25f), 0.6f, 0.2f), "card");
                    string icon = spec.Id == "argument" ? "!" : "★";
                    Font f = MaterialFactory.UiFont;
                    if (icon == "★" && (f == null || !f.HasCharacter('★'))) icon = "*";
                    TextLabel(item, icon, new Vector3(0f, 0.05f, -0.05f), 0.42f, glow, 0f);
                    GameObject rim = Part(item, PrimitiveType.Cube, Vector3.zero,
                        new Vector3(0.66f, 0.86f, 0.03f), MaterialFactory.Neon(glow), "rim");
                    NoShadow(rim);
                    break;
                }
                case "cover":
                case "pass":
                {
                    Color coverC = spec.Id == "cover" ? new Color(0.15f, 0.2f, 0.45f) : new Color(0.93f, 0.93f, 0.95f);
                    Part(item, PrimitiveType.Cube, Vector3.zero,
                        new Vector3(0.52f, 0.72f, 0.07f), MaterialFactory.Solid(coverC, 0.5f, 0.1f), "book");
                    if (spec.Id == "cover")
                    {
                        Part(item, PrimitiveType.Cylinder, new Vector3(0f, 0.08f, -0.04f),
                            new Vector3(0.2f, 0.01f, 0.2f), goldM, "emblem");
                        TextLabel(item, "ПАСПОРТ", new Vector3(0f, -0.22f, -0.05f), 0.07f,
                            new Color(0.95f, 0.8f, 0.4f), 0f);
                    }
                    else
                    {
                        Part(item, PrimitiveType.Cube, new Vector3(0f, 0.24f, -0.04f),
                            new Vector3(0.46f, 0.16f, 0.02f),
                            MaterialFactory.Solid(new Color(0.15f, 0.35f, 0.65f), 0.4f, 0f), "header");
                        Part(item, PrimitiveType.Cube, new Vector3(-0.12f, -0.05f, -0.04f),
                            new Vector3(0.18f, 0.22f, 0.02f),
                            MaterialFactory.Solid(new Color(0.75f, 0.6f, 0.5f), 0.3f, 0f), "photo");
                        TextLabel(item, "ПРОПУСК", new Vector3(0.08f, -0.28f, -0.05f), 0.07f,
                            new Color(0.2f, 0.25f, 0.35f), 0f);
                    }
                    break;
                }
                case "practice":
                {
                    Part(item, PrimitiveType.Cube, Vector3.zero,
                        new Vector3(0.55f, 0.72f, 0.14f),
                        MaterialFactory.Solid(new Color(0.15f, 0.5f, 0.25f), 0.5f, 0.1f), "book");
                    Part(item, PrimitiveType.Cube, new Vector3(0.04f, 0f, 0f),
                        new Vector3(0.9f, 0.94f, 0.8f), paperM, "pages");
                    TextLabel(item, "ВС РФ", new Vector3(-0.01f, 0.1f, -0.08f), 0.11f,
                        new Color(0.95f, 0.85f, 0.5f), 0f);
                    break;
                }
                case "lawbook":
                {
                    // Референс: тёмно-бордовый том «ЗАКОН» с золотым гербом и обрезом.
                    Material cover = MaterialFactory.Solid(new Color(0.36f, 0.09f, 0.07f), 0.6f, 0.2f);
                    Part(item, PrimitiveType.Cube, Vector3.zero,
                        new Vector3(0.60f, 0.80f, 0.16f), cover, "book");
                    Part(item, PrimitiveType.Cube, new Vector3(0.035f, 0f, 0f),
                        new Vector3(0.55f, 0.73f, 0.135f),
                        MaterialFactory.Solid(new Color(0.93f, 0.86f, 0.68f), 0.5f, 0.3f), "pages");
                    GameObject emblem = PartRot(item, PrimitiveType.Cylinder,
                        new Vector3(0f, 0.15f, -0.085f), new Vector3(0.26f, 0.008f, 0.26f),
                        new Vector3(90f, 0f, 0f), goldM, "emblem");
                    NoShadow(emblem);
                    TextLabel(item, "ЗАКОН", new Vector3(0f, -0.18f, -0.10f), 0.105f,
                        new Color(1f, 0.86f, 0.45f), 0f);
                    for (int i = -1; i <= 1; i += 2)
                    {
                        GameObject trim = Part(item, PrimitiveType.Cube,
                            new Vector3(0f, 0.36f * i, -0.083f), new Vector3(0.52f, 0.025f, 0.01f),
                            goldM, "trim");
                        NoShadow(trim);
                    }
                    break;
                }
                case "claim":
                {
                    // Референс: светящийся лист «ИСКОВОЕ ЗАЯВЛЕНИЕ» с красной печатью-розеткой.
                    Part(item, PrimitiveType.Cube, Vector3.zero,
                        new Vector3(0.60f, 0.82f, 0.05f), paperM, "doc");
                    TextLabel(item, "ИСКОВОЕ\nЗАЯВЛЕНИЕ", new Vector3(0f, 0.24f, -0.035f), 0.075f,
                        new Color(0.16f, 0.14f, 0.30f), 0f);
                    for (int i = 0; i < 3; i++)
                    {
                        Part(item, PrimitiveType.Cube, new Vector3(0f, -0.02f - i * 0.1f, -0.01f),
                            new Vector3(0.46f, 0.035f, 0.05f),
                            MaterialFactory.Solid(new Color(0.62f, 0.62f, 0.64f), 0.3f, 0f), "line" + i);
                    }
                    Material sealM = MaterialFactory.Solid(new Color(0.72f, 0.10f, 0.10f), 0.55f, 0.1f);
                    GameObject seal = PartRot(item, PrimitiveType.Cylinder,
                        new Vector3(0.17f, -0.30f, -0.035f), new Vector3(0.17f, 0.008f, 0.17f),
                        new Vector3(90f, 0f, 0f), sealM, "seal");
                    NoShadow(seal);
                    PartRot(item, PrimitiveType.Cube, new Vector3(0.12f, -0.40f, -0.03f),
                        new Vector3(0.05f, 0.14f, 0.012f), new Vector3(0f, 0f, 20f), sealM, "ribbonL");
                    PartRot(item, PrimitiveType.Cube, new Vector3(0.22f, -0.40f, -0.03f),
                        new Vector3(0.05f, 0.14f, 0.012f), new Vector3(0f, 0f, -20f), sealM, "ribbonR");
                    break;
                }
                case "scales":
                {
                    // Референс: латунные весы правосудия.
                    Material brass = MaterialFactory.Solid(new Color(0.88f, 0.68f, 0.28f), 0.9f, 0.9f);
                    Part(item, PrimitiveType.Cylinder, new Vector3(0f, -0.38f, 0f),
                        new Vector3(0.30f, 0.02f, 0.30f), brass, "base");
                    Part(item, PrimitiveType.Cylinder, new Vector3(0f, -0.05f, 0f),
                        new Vector3(0.05f, 0.33f, 0.05f), brass, "column");
                    Part(item, PrimitiveType.Cube, new Vector3(0f, 0.30f, 0f),
                        new Vector3(0.74f, 0.035f, 0.035f), brass, "beam");
                    Part(item, PrimitiveType.Sphere, new Vector3(0f, 0.36f, 0f),
                        new Vector3(0.08f, 0.08f, 0.08f), brass, "finial");
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Part(item, PrimitiveType.Cube, new Vector3(0.35f * s, 0.17f, 0f),
                            new Vector3(0.015f, 0.24f, 0.015f), brass, "chain");
                        Part(item, PrimitiveType.Cylinder, new Vector3(0.35f * s, 0.04f, 0f),
                            new Vector3(0.22f, 0.014f, 0.22f), brass, "pan");
                    }
                    break;
                }
                case "aicourse":
                {
                    Part(item, PrimitiveType.Cube, Vector3.zero,
                        new Vector3(0.55f, 0.55f, 0.08f),
                        MaterialFactory.Solid(new Color(0.08f, 0.1f, 0.14f), 0.6f, 0.3f), "chip");
                    GameObject cpu = Part(item, PrimitiveType.Cube, new Vector3(0f, 0f, -0.03f),
                        new Vector3(0.3f, 0.3f, 0.05f),
                        MaterialFactory.Neon(new Color(0.25f, 0.9f, 0.85f)), "cpu");
                    NoShadow(cpu);
                    TextLabel(item, "ИИ", new Vector3(0f, 0f, -0.07f), 0.16f,
                        new Color(0.05f, 0.2f, 0.2f), 0f);
                    for (int i = 0; i < 4; i++)
                    {
                        Part(item, PrimitiveType.Cube, new Vector3(-0.33f, 0.18f - i * 0.12f, 0f),
                            new Vector3(0.1f, 0.04f, 0.04f), goldM, "pinL" + i);
                        Part(item, PrimitiveType.Cube, new Vector3(0.33f, 0.18f - i * 0.12f, 0f),
                            new Vector3(0.1f, 0.04f, 0.04f), goldM, "pinR" + i);
                    }
                    break;
                }
                default:
                {
                    Part(item, PrimitiveType.Cube, Vector3.zero,
                        new Vector3(0.6f, 0.8f, 0.06f), MaterialFactory.Neon(glow), "card");
                    break;
                }
            }
        }

        // ------------------------------------------------------------------
        // Люди и декорации
        // ------------------------------------------------------------------

        public static GameObject CreatePerson(Transform parent, Vector3 localPos, Color coat,
            Color pants, float height, float rotY)
        {
            GameObject root = Group(parent, "person", localPos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            float k = height / 1.7f;
            Material coatM = MaterialFactory.Solid(coat, 0.4f, 0f);
            Material pantsM = MaterialFactory.Solid(pants, 0.4f, 0f);
            Material skinM = MaterialFactory.Solid(new Color(0.95f, 0.78f, 0.62f), 0.35f, 0f);
            Material hairM = MaterialFactory.Solid(new Color(0.2f + coat.r * 0.2f, 0.14f, 0.1f), 0.35f, 0f);

            Part(root.transform, PrimitiveType.Cube, new Vector3(-0.09f * k, 0.42f * k, 0f),
                new Vector3(0.15f, 0.84f, 0.18f) * k, pantsM, "legL");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0.09f * k, 0.42f * k, 0f),
                new Vector3(0.15f, 0.84f, 0.18f) * k, pantsM, "legR");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.13f * k, 0f),
                new Vector3(0.46f, 0.62f, 0.26f) * k, coatM, "torso");
            PartRot(root.transform, PrimitiveType.Cube, new Vector3(-0.28f * k, 1.1f * k, 0f),
                new Vector3(0.11f, 0.55f, 0.13f) * k, new Vector3(0f, 0f, 6f), coatM, "armL");
            PartRot(root.transform, PrimitiveType.Cube, new Vector3(0.28f * k, 1.1f * k, 0f),
                new Vector3(0.11f, 0.55f, 0.13f) * k, new Vector3(0f, 0f, -6f), coatM, "armR");
            Part(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.63f * k, 0f),
                new Vector3(0.34f, 0.34f, 0.34f) * k, skinM, "head");
            Part(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.72f * k, -0.02f * k),
                new Vector3(0.35f, 0.2f, 0.35f) * k, hairM, "hair");
            return root;
        }

        public static GameObject CreateDesk(Transform parent, Vector3 pos, float rotY, bool withMonitor)
        {
            GameObject root = Group(parent, "desk", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material wood = MaterialFactory.Solid(new Color(0.55f, 0.42f, 0.3f), 0.55f, 0.05f);
            Material metal = MaterialFactory.Solid(new Color(0.4f, 0.42f, 0.45f), 0.6f, 0.5f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.74f, 0f),
                new Vector3(1.7f, 0.07f, 0.85f), wood, "top");
            Part(root.transform, PrimitiveType.Cube, new Vector3(-0.75f, 0.37f, 0f),
                new Vector3(0.07f, 0.74f, 0.75f), metal, "legL");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0.75f, 0.37f, 0f),
                new Vector3(0.07f, 0.74f, 0.75f), metal, "legR");
            if (withMonitor)
            {
                Part(root.transform, PrimitiveType.Cube, new Vector3(-0.3f, 1.05f, 0.15f),
                    new Vector3(0.62f, 0.42f, 0.05f),
                    MaterialFactory.Solid(new Color(0.08f, 0.08f, 0.1f), 0.6f, 0.2f), "monitor");
                GameObject scr = Part(root.transform, PrimitiveType.Cube, new Vector3(-0.3f, 1.05f, 0.125f),
                    new Vector3(0.54f, 0.34f, 0.01f),
                    MaterialFactory.Emissive(new Color(0.65f, 0.8f, 0.95f), new Color(0.5f, 0.7f, 1f), 1.1f), "screen");
                NoShadow(scr);
                Part(root.transform, PrimitiveType.Cube, new Vector3(-0.3f, 0.82f, 0.15f),
                    new Vector3(0.1f, 0.12f, 0.1f), metal, "stand");
            }
            Part(root.transform, PrimitiveType.Cube, new Vector3(0.45f, 0.81f, 0.05f),
                new Vector3(0.4f, 0.08f, 0.3f),
                MaterialFactory.Solid(new Color(0.95f, 0.93f, 0.87f), 0.3f, 0f), "papers");
            return root;
        }

        public static GameObject CreatePrinter(Transform parent, Vector3 pos, float rotY)
        {
            GameObject root = Group(parent, "printer", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material bodyM = MaterialFactory.Solid(new Color(0.8f, 0.8f, 0.78f), 0.5f, 0.1f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f),
                new Vector3(0.7f, 0.4f, 0.6f), bodyM, "body");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f),
                new Vector3(0.6f, 0.8f, 0.55f),
                MaterialFactory.Solid(new Color(0.5f, 0.5f, 0.52f), 0.5f, 0.1f), "stand");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.06f, -0.18f),
                new Vector3(0.4f, 0.02f, 0.3f),
                MaterialFactory.Solid(new Color(0.97f, 0.97f, 0.94f), 0.3f, 0f), "paper");
            Part(root.transform, PrimitiveType.Sphere, new Vector3(0.24f, 1.02f, -0.28f),
                new Vector3(0.06f, 0.06f, 0.06f),
                MaterialFactory.Neon(new Color(0.3f, 0.9f, 0.4f)), "led");
            return root;
        }

        public static GameObject CreatePlant(Transform parent, Vector3 pos, float scale)
        {
            GameObject root = Group(parent, "plant", pos);
            root.transform.localScale = new Vector3(scale, scale, scale);
            Part(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f),
                new Vector3(0.4f, 0.25f, 0.4f),
                MaterialFactory.Solid(new Color(0.6f, 0.35f, 0.2f), 0.4f, 0f), "pot");
            Material leaf = MaterialFactory.Solid(new Color(0.2f, 0.55f, 0.25f), 0.4f, 0f);
            Part(root.transform, PrimitiveType.Sphere, new Vector3(0f, 0.85f, 0f),
                new Vector3(0.65f, 0.75f, 0.65f), leaf, "leaf1");
            Part(root.transform, PrimitiveType.Sphere, new Vector3(0.2f, 1.1f, 0.1f),
                new Vector3(0.45f, 0.5f, 0.45f), leaf, "leaf2");
            Part(root.transform, PrimitiveType.Sphere, new Vector3(-0.2f, 1.05f, -0.1f),
                new Vector3(0.4f, 0.45f, 0.4f), leaf, "leaf3");
            return root;
        }

        public static GameObject CreateGlassWall(Transform parent, Vector3 pos, Vector2 size, float rotY)
        {
            GameObject root = Group(parent, "glass", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            GameObject pane = Part(root.transform, PrimitiveType.Cube,
                new Vector3(0f, size.y * 0.5f, 0f), new Vector3(size.x, size.y, 0.04f),
                MaterialFactory.UnlitTransparent(new Color(0.6f, 0.8f, 0.9f, 0.18f)), "pane");
            NoShadow(pane);
            Material frame = MaterialFactory.Solid(new Color(0.3f, 0.32f, 0.36f), 0.6f, 0.5f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, size.y, 0f),
                new Vector3(size.x, 0.07f, 0.07f), frame, "frameT");
            Part(root.transform, PrimitiveType.Cube, new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f),
                new Vector3(0.07f, size.y, 0.07f), frame, "frameL");
            Part(root.transform, PrimitiveType.Cube, new Vector3(size.x * 0.5f, size.y * 0.5f, 0f),
                new Vector3(0.07f, size.y, 0.07f), frame, "frameR");
            return root;
        }

        public static GameObject CreateBuilding(Transform parent, Vector3 pos, Vector3 size,
            Color wall, Color glow, int seed)
        {
            GameObject root = Group(parent, "building", pos);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, size.y * 0.5f, 0f),
                size, MaterialFactory.WindowsMaterial(wall, glow, seed), "block");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, size.y + 0.25f, 0f),
                new Vector3(size.x * 1.04f, 0.5f, size.z * 1.04f),
                MaterialFactory.Solid(wall * 0.7f, 0.4f, 0f), "roof");
            return root;
        }

        public static GameObject CreateStreetLight(Transform parent, Vector3 pos, float rotY)
        {
            GameObject root = Group(parent, "streetlight", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material pole = MaterialFactory.Solid(new Color(0.25f, 0.27f, 0.3f), 0.6f, 0.6f);
            Part(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 2.2f, 0f),
                new Vector3(0.12f, 2.2f, 0.12f), pole, "pole");
            PartRot(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 4.3f, -0.55f),
                new Vector3(0.08f, 0.6f, 0.08f), new Vector3(90f, 0f, 0f), pole, "arm");
            GameObject lamp = Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 4.25f, -1.05f),
                new Vector3(0.45f, 0.12f, 0.25f),
                MaterialFactory.Neon(new Color(1f, 0.9f, 0.6f)), "lamp");
            NoShadow(lamp);
            return root;
        }

        public static GameObject CreateMediaVan(Transform parent, Vector3 pos, float rotY)
        {
            GameObject root = Group(parent, "van", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material bodyM = MaterialFactory.Solid(new Color(0.9f, 0.9f, 0.92f), 0.6f, 0.1f);
            Material darkM = MaterialFactory.Solid(new Color(0.15f, 0.15f, 0.18f), 0.5f, 0.2f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.1f, 0.3f),
                new Vector3(2.0f, 1.5f, 4.2f), bodyM, "box");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.85f, -2.4f),
                new Vector3(1.9f, 1.0f, 1.4f), bodyM, "cab");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.05f, -3.0f),
                new Vector3(1.7f, 0.5f, 0.15f), darkM, "windshield");
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -1.0f : 1.0f;
                float z = i < 2 ? -2.2f : 1.5f;
                PartRot(root.transform, PrimitiveType.Cylinder, new Vector3(x, 0.35f, z),
                    new Vector3(0.7f, 0.12f, 0.7f), new Vector3(0f, 0f, 90f), darkM, "wheel" + i);
            }
            Part(root.transform, PrimitiveType.Sphere, new Vector3(0f, 2.15f, 0.8f),
                new Vector3(1.0f, 0.35f, 1.0f),
                MaterialFactory.Solid(new Color(0.8f, 0.82f, 0.85f), 0.6f, 0.4f), "dish");
            Part(root.transform, PrimitiveType.Cylinder, new Vector3(0.6f, 2.5f, -0.8f),
                new Vector3(0.04f, 0.7f, 0.04f), darkM, "antenna");
            GameObject band = Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.15f, 0.3f),
                new Vector3(2.02f, 0.35f, 4.0f),
                MaterialFactory.Solid(new Color(0.85f, 0.2f, 0.2f), 0.5f, 0f), "band");
            TextLabel(root.transform, "НОВОСТИ 24", new Vector3(-1.03f, 1.15f, 0.3f), 0.24f,
                Color.white, 90f);
            NoShadow(band);
            return root;
        }

        public static GameObject CreateKiosk(Transform parent, Vector3 pos, float rotY, string signText)
        {
            GameObject root = Group(parent, "kiosk", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.2f, 0f),
                new Vector3(2.2f, 2.4f, 1.8f),
                MaterialFactory.Solid(new Color(0.45f, 0.55f, 0.65f), 0.5f, 0.1f), "box");
            GameObject win = Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.3f, -0.91f),
                new Vector3(1.6f, 0.9f, 0.03f),
                MaterialFactory.Emissive(new Color(0.9f, 0.95f, 1f), new Color(0.8f, 0.9f, 1f), 0.9f), "window");
            NoShadow(win);
            for (int i = 0; i < 4; i++)
            {
                Color c = i % 2 == 0 ? new Color(0.9f, 0.5f, 0.2f) : Color.white;
                PartRot(root.transform, PrimitiveType.Cube, new Vector3(-0.75f + i * 0.5f, 2.5f, -0.95f),
                    new Vector3(0.5f, 0.35f, 0.05f), new Vector3(25f, 0f, 0f),
                    MaterialFactory.Solid(c, 0.4f, 0f), "awning" + i);
            }
            SignBoard(root.transform, signText, new Vector3(0f, 2.15f, -0.95f),
                new Vector2(2.0f, 0.45f), new Color(0.1f, 0.3f, 0.55f), Color.white, 0f, true);
            return root;
        }

        public static GameObject CreateColumn(Transform parent, Vector3 pos, float height)
        {
            GameObject root = Group(parent, "column", pos);
            Material marble = MaterialFactory.Solid(new Color(0.88f, 0.85f, 0.78f), 0.75f, 0.05f);
            Material goldM = MaterialFactory.Solid(new Color(0.85f, 0.7f, 0.35f), 0.8f, 0.7f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f),
                new Vector3(1.0f, 0.3f, 1.0f), marble, "base");
            Part(root.transform, PrimitiveType.Cylinder, new Vector3(0f, height * 0.5f, 0f),
                new Vector3(0.62f, height * 0.5f, 0.62f), marble, "shaft");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, height - 0.12f, 0f),
                new Vector3(0.95f, 0.24f, 0.95f), goldM, "cap");
            return root;
        }

        public static GameObject CreateBench(Transform parent, Vector3 pos, float rotY)
        {
            GameObject root = Group(parent, "bench", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material wood = MaterialFactory.Solid(new Color(0.42f, 0.28f, 0.16f), 0.6f, 0.05f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f),
                new Vector3(2.2f, 0.09f, 0.55f), wood, "seat");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.85f, 0.26f),
                new Vector3(2.2f, 0.7f, 0.08f), wood, "back");
            Part(root.transform, PrimitiveType.Cube, new Vector3(-0.95f, 0.22f, 0f),
                new Vector3(0.09f, 0.45f, 0.5f), wood, "legL");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0.95f, 0.22f, 0f),
                new Vector3(0.09f, 0.45f, 0.5f), wood, "legR");
            return root;
        }

        public static GameObject CreateCourtDoor(Transform parent, Vector3 pos, float rotY, string label)
        {
            GameObject root = Group(parent, "door", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material wood = MaterialFactory.Solid(new Color(0.35f, 0.22f, 0.12f), 0.65f, 0.05f);
            Material goldM = MaterialFactory.Solid(new Color(0.85f, 0.7f, 0.35f), 0.85f, 0.8f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(-0.55f, 1.5f, 0f),
                new Vector3(1.05f, 3.0f, 0.12f), wood, "doorL");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0.55f, 1.5f, 0f),
                new Vector3(1.05f, 3.0f, 0.12f), wood, "doorR");
            Part(root.transform, PrimitiveType.Sphere, new Vector3(-0.15f, 1.4f, -0.09f),
                new Vector3(0.09f, 0.09f, 0.09f), goldM, "knobL");
            Part(root.transform, PrimitiveType.Sphere, new Vector3(0.15f, 1.4f, -0.09f),
                new Vector3(0.09f, 0.09f, 0.09f), goldM, "knobR");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 3.15f, 0f),
                new Vector3(2.4f, 0.3f, 0.2f), goldM, "lintel");
            SignBoard(root.transform, label, new Vector3(0f, 3.6f, 0f), new Vector2(1.7f, 0.5f),
                new Color(0.32f, 0.07f, 0.07f), new Color(1f, 0.92f, 0.75f), 0f, true);
            return root;
        }

        public static GameObject CreateStanchions(Transform parent, Vector3 pos, float rotY, int count)
        {
            GameObject root = Group(parent, "stanchions", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material goldM = MaterialFactory.Solid(new Color(0.8f, 0.65f, 0.3f), 0.8f, 0.8f);
            Material rope = MaterialFactory.Solid(new Color(0.6f, 0.12f, 0.15f), 0.5f, 0f);
            for (int i = 0; i < count; i++)
            {
                float z = i * 1.4f;
                Part(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, z),
                    new Vector3(0.09f, 0.5f, 0.09f), goldM, "post" + i);
                Part(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.02f, z),
                    new Vector3(0.14f, 0.14f, 0.14f), goldM, "cap" + i);
                if (i > 0)
                {
                    PartRot(root.transform, PrimitiveType.Cylinder,
                        new Vector3(0f, 0.85f, z - 0.7f),
                        new Vector3(0.035f, 0.68f, 0.035f), new Vector3(90f, 0f, 0f), rope, "rope" + i);
                }
            }
            return root;
        }

        public static GameObject CreateCeilingLamp(Transform parent, Vector3 pos, Vector2 size, Color warm)
        {
            GameObject root = Group(parent, "lampPanel", pos);
            GameObject panel = Part(root.transform, PrimitiveType.Cube, Vector3.zero,
                new Vector3(size.x, 0.08f, size.y), MaterialFactory.Neon(warm), "panel");
            NoShadow(panel);
            // Мягкий «отблеск» на полу.
            GameObject sheen = PartRot(root.transform, PrimitiveType.Quad,
                new Vector3(0f, -pos.y + 0.035f, 0f),
                new Vector3(size.x * 1.6f, size.y * 2.2f, 1f), new Vector3(90f, 0f, 0f),
                MaterialFactory.UnlitTransparent(new Color(warm.r, warm.g, warm.b, 0.07f)), "sheen");
            NoShadow(sheen);
            return root;
        }

        public static GameObject CreateScheduleBoard(Transform parent, Vector3 pos, float rotY)
        {
            GameObject root = Group(parent, "schedule", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.9f, 0f),
                new Vector3(2.6f, 2.2f, 0.12f),
                MaterialFactory.Solid(new Color(0.12f, 0.1f, 0.09f), 0.5f, 0.1f), "board");
            TextLabel(root.transform, "РАСПИСАНИЕ ЗАСЕДАНИЙ", new Vector3(0f, 2.75f, -0.08f),
                0.14f, new Color(1f, 0.85f, 0.5f), 0f);
            for (int i = 0; i < 6; i++)
            {
                Part(root.transform, PrimitiveType.Cube, new Vector3(-0.15f, 2.4f - i * 0.3f, -0.07f),
                    new Vector3(1.9f, 0.08f, 0.02f),
                    MaterialFactory.Neon(new Color(0.9f, 0.85f, 0.7f)), "row" + i);
                Part(root.transform, PrimitiveType.Cube, new Vector3(1.05f, 2.4f - i * 0.3f, -0.07f),
                    new Vector3(0.25f, 0.08f, 0.02f),
                    MaterialFactory.Neon(i % 2 == 0 ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.95f, 0.75f, 0.3f)),
                    "status" + i);
            }
            return root;
        }

        public static GameObject CreateReceptionDesk(Transform parent, Vector3 pos, float rotY)
        {
            GameObject root = Group(parent, "reception", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material wood = MaterialFactory.Solid(new Color(0.4f, 0.26f, 0.14f), 0.65f, 0.05f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f),
                new Vector3(2.6f, 1.2f, 0.8f), wood, "counter");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.24f, 0f),
                new Vector3(2.7f, 0.08f, 0.9f),
                MaterialFactory.Solid(new Color(0.85f, 0.8f, 0.72f), 0.8f, 0.1f), "top");
            CreatePerson(root.transform, new Vector3(0f, 0f, 0.7f),
                new Color(0.25f, 0.25f, 0.35f), new Color(0.2f, 0.2f, 0.25f), 1.65f, 180f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(-0.8f, 1.35f, 0f),
                new Vector3(0.35f, 0.14f, 0.25f),
                MaterialFactory.Solid(new Color(0.9f, 0.88f, 0.82f), 0.3f, 0f), "docs");
            GameObject lampArm = Part(root.transform, PrimitiveType.Sphere, new Vector3(0.8f, 1.5f, 0f),
                new Vector3(0.18f, 0.18f, 0.18f), MaterialFactory.Neon(new Color(1f, 0.85f, 0.55f)), "lamp");
            NoShadow(lampArm);
            return root;
        }

        // ------------------------------------------------------------------
        // Российский арбитражный вайб: герб, картотека, пропускная, канцелярия
        // ------------------------------------------------------------------

        /// <summary>
        /// Стилизованный герб-орёл на тёмном щите с подписью суда.
        /// Собран из примитивов по мотивам фасадов арбитражных судов.
        /// </summary>
        public static GameObject CreateCourtEmblem(Transform parent, Vector3 pos, float rotY, float scale)
        {
            GameObject root = Group(parent, "emblem", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            root.transform.localScale = new Vector3(scale, scale, scale);

            Material board = MaterialFactory.Solid(new Color(0.09f, 0.14f, 0.28f), 0.5f, 0.1f);
            Material goldM = MaterialFactory.Solid(new Color(0.93f, 0.76f, 0.34f), 0.85f, 0.8f);
            Material goldGlow = MaterialFactory.Emissive(new Color(0.9f, 0.72f, 0.3f),
                new Color(1f, 0.85f, 0.45f), 0.5f);

            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0f),
                new Vector3(1.5f, 1.8f, 0.07f), board, "shield");
            float t = 0.045f;
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f),
                new Vector3(1.5f + t, t, 0.09f), goldM, "frameT");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, -0.9f, 0f),
                new Vector3(1.5f + t, t, 0.09f), goldM, "frameB");
            Part(root.transform, PrimitiveType.Cube, new Vector3(-0.75f, 0f, 0f),
                new Vector3(t, 1.8f + t, 0.09f), goldM, "frameL");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0.75f, 0f, 0f),
                new Vector3(t, 1.8f + t, 0.09f), goldM, "frameR");

            // Орёл: корпус, две головы, корона, крылья веером, хвост.
            GameObject eagle = Group(root.transform, "eagle", new Vector3(0f, 0.22f, -0.05f));
            Part(eagle.transform, PrimitiveType.Sphere, new Vector3(0f, 0f, 0f),
                new Vector3(0.30f, 0.46f, 0.09f), goldGlow, "bodyE");
            Part(eagle.transform, PrimitiveType.Sphere, new Vector3(-0.11f, 0.30f, 0f),
                new Vector3(0.12f, 0.12f, 0.07f), goldGlow, "headL");
            Part(eagle.transform, PrimitiveType.Sphere, new Vector3(0.11f, 0.30f, 0f),
                new Vector3(0.12f, 0.12f, 0.07f), goldGlow, "headR");
            NoShadow(PartRot(eagle.transform, PrimitiveType.Cube, new Vector3(-0.19f, 0.30f, 0f),
                new Vector3(0.07f, 0.03f, 0.05f), new Vector3(0f, 0f, -8f), goldM, "beakL"));
            NoShadow(PartRot(eagle.transform, PrimitiveType.Cube, new Vector3(0.19f, 0.30f, 0f),
                new Vector3(0.07f, 0.03f, 0.05f), new Vector3(0f, 0f, 8f), goldM, "beakR"));
            NoShadow(Part(eagle.transform, PrimitiveType.Sphere, new Vector3(0f, 0.42f, 0f),
                new Vector3(0.09f, 0.09f, 0.06f), goldM, "crown"));
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float ang = 18f + i * 26f;
                    NoShadow(PartRot(eagle.transform, PrimitiveType.Cube,
                        new Vector3(side * (0.24f + i * 0.115f), 0.06f - i * 0.10f, 0f),
                        new Vector3(0.30f, 0.075f, 0.05f),
                        new Vector3(0f, 0f, side * ang), goldGlow, "wing" + side + "_" + i));
                }
            }
            for (int i = -1; i <= 1; i++)
            {
                NoShadow(PartRot(eagle.transform, PrimitiveType.Cube,
                    new Vector3(i * 0.09f, -0.33f, 0f), new Vector3(0.07f, 0.20f, 0.05f),
                    new Vector3(0f, 0f, i * 16f), goldGlow, "tail" + (i + 1)));
            }
            // Щиток на груди.
            Part(eagle.transform, PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.055f),
                new Vector3(0.11f, 0.15f, 0.03f), MaterialFactory.Solid(new Color(0.55f, 0.1f, 0.12f), 0.5f, 0.2f), "chestShield");

            return root;
        }

        /// <summary>Табло-столбик картотеки дел: тёмный экран со строками заседаний.</summary>
        public static GameObject CreateInfoTerminal(Transform parent, Vector3 pos, float rotY)
        {
            GameObject root = Group(parent, "infoTerminal", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material darkM = MaterialFactory.Solid(new Color(0.13f, 0.14f, 0.18f), 0.6f, 0.4f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f),
                new Vector3(0.55f, 2.1f, 0.32f), darkM, "pillar");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f),
                new Vector3(0.7f, 0.1f, 0.45f), darkM, "base");
            GameObject screen = Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.35f, -0.17f),
                new Vector3(0.46f, 0.85f, 0.03f),
                MaterialFactory.Emissive(new Color(0.06f, 0.10f, 0.20f), new Color(0.25f, 0.45f, 0.9f), 0.6f),
                "screen");
            NoShadow(screen);
            // Строки дел: номер + статус.
            for (int i = 0; i < 5; i++)
            {
                NoShadow(Part(root.transform, PrimitiveType.Cube,
                    new Vector3(-0.05f, 1.62f - i * 0.15f, -0.19f),
                    new Vector3(0.26f, 0.035f, 0.01f),
                    MaterialFactory.Neon(new Color(0.85f, 0.9f, 1f)), "row" + i));
                NoShadow(Part(root.transform, PrimitiveType.Cube,
                    new Vector3(0.15f, 1.62f - i * 0.15f, -0.19f),
                    new Vector3(0.08f, 0.035f, 0.01f),
                    MaterialFactory.Neon(i % 2 == 0 ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.95f, 0.75f, 0.3f)),
                    "st" + i));
            }
            TextLabel(root.transform, "КАРТОТЕКА ДЕЛ", new Vector3(0f, 1.88f, -0.19f), 0.075f,
                new Color(1f, 0.85f, 0.5f), 0f);
            TextLabel(root.transform, "А40-15/2026", new Vector3(0f, 0.82f, -0.19f), 0.06f,
                new Color(0.7f, 0.8f, 1f), 0f);
            return root;
        }

        /// <summary>Пост охраны: стойка «ПРОПУСКА», турникет и пристав.</summary>
        public static GameObject CreateSecurityPost(Transform parent, Vector3 pos, float rotY)
        {
            GameObject root = Group(parent, "securityPost", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material greyM = MaterialFactory.Solid(new Color(0.45f, 0.47f, 0.52f), 0.6f, 0.4f);
            Material steel = MaterialFactory.Solid(new Color(0.62f, 0.65f, 0.7f), 0.75f, 0.7f);

            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f),
                new Vector3(1.7f, 1.1f, 0.6f), greyM, "counter");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.14f, 0f),
                new Vector3(1.8f, 0.07f, 0.7f), steel, "top");
            SignBoard(root.transform, "ПРОПУСКА", new Vector3(0f, 2.0f, 0f),
                new Vector2(1.5f, 0.42f), new Color(0.09f, 0.2f, 0.42f), Color.white, 0f, true);
            // Турникет-трипод.
            GameObject turn = Group(root.transform, "turnstile", new Vector3(1.35f, 0f, 0f));
            Part(turn.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f),
                new Vector3(0.12f, 0.5f, 0.12f), steel, "column");
            for (int i = 0; i < 3; i++)
            {
                PartRot(turn.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.95f, 0f),
                    new Vector3(0.045f, 0.34f, 0.045f),
                    new Vector3(64f, i * 120f, 0f), steel, "arm" + i);
            }
            NoShadow(Part(turn.transform, PrimitiveType.Sphere, new Vector3(0f, 1.02f, 0f),
                new Vector3(0.08f, 0.08f, 0.08f),
                MaterialFactory.Neon(new Color(0.3f, 0.9f, 0.4f)), "lamp"));
            // Пристав за стойкой.
            CreatePerson(root.transform, new Vector3(-0.3f, 0f, 0.55f),
                new Color(0.12f, 0.14f, 0.2f), new Color(0.1f, 0.11f, 0.15f), 1.75f, 180f);
            TextLabel(root.transform, "Предъявите паспорт", new Vector3(0f, 1.35f, -0.37f), 0.075f,
                new Color(0.95f, 0.95f, 0.9f), 0f);
            return root;
        }

        /// <summary>Окно канцелярии с вечным «Перерыв до 14:00» и очередью.</summary>
        public static GameObject CreateChanceryWindow(Transform parent, Vector3 pos, float rotY)
        {
            GameObject root = Group(parent, "chancery", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material wallM = MaterialFactory.Solid(new Color(0.55f, 0.48f, 0.38f), 0.5f, 0.05f);
            Material wood = MaterialFactory.Solid(new Color(0.35f, 0.22f, 0.12f), 0.6f, 0.05f);

            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.25f, 0.1f),
                new Vector3(2.3f, 2.5f, 0.2f), wallM, "panel");
            GameObject pane = Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.5f, -0.02f),
                new Vector3(1.05f, 0.75f, 0.05f),
                MaterialFactory.Emissive(new Color(0.9f, 0.93f, 0.95f), new Color(0.85f, 0.9f, 1f), 0.8f),
                "window");
            NoShadow(pane);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.08f, -0.06f),
                new Vector3(1.2f, 0.09f, 0.18f), wood, "sill");
            SignBoard(root.transform, "КАНЦЕЛЯРИЯ", new Vector3(0f, 2.18f, -0.02f),
                new Vector2(1.6f, 0.4f), new Color(0.09f, 0.2f, 0.42f), Color.white, 0f, true);
            // Легендарная бумажка на стекле.
            GameObject note = PartRot(root.transform, PrimitiveType.Cube, new Vector3(0.28f, 1.42f, -0.055f),
                new Vector3(0.44f, 0.3f, 0.01f), new Vector3(0f, 0f, -4f),
                MaterialFactory.Solid(new Color(0.97f, 0.95f, 0.85f), 0.3f, 0f), "note");
            NoShadow(note);
            TextLabel(note.transform, "ПЕРЕРЫВ\n13:00–14:00", new Vector3(0f, 0f, -0.7f), 0.26f,
                new Color(0.5f, 0.15f, 0.12f), 0f);
            // Очередь из двух страдальцев.
            CreatePerson(root.transform, new Vector3(-0.55f, 0f, -0.55f),
                new Color(0.35f, 0.3f, 0.4f), new Color(0.2f, 0.2f, 0.25f), 1.7f, 15f);
            CreatePerson(root.transform, new Vector3(-0.95f, 0f, -1.05f),
                new Color(0.5f, 0.35f, 0.25f), new Color(0.22f, 0.2f, 0.2f), 1.6f, -10f);
            return root;
        }

        /// <summary>
        /// Ролл-ап «КонсультантПлюс»: знакомый каждому юристу сине-белый стенд
        /// с золотой полосой. Ставится у стен офиса и в коридорах суда.
        /// </summary>
        public static GameObject CreateConsultantStand(Transform parent, Vector3 pos, float rotY)
        {
            GameObject g = Group(parent, "consultantStand", pos);
            g.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Color navy = new Color(0.05f, 0.20f, 0.46f);
            Material navyM = MaterialFactory.Solid(navy, 0.55f, 0.05f);
            Material whiteM = MaterialFactory.Solid(new Color(0.95f, 0.96f, 0.98f), 0.4f, 0f);
            Material goldM = MaterialFactory.Solid(new Color(0.95f, 0.78f, 0.32f), 0.85f, 0.85f);

            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f),
                new Vector3(0.75f, 0.1f, 0.38f), navyM, "base");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 1.15f, 0f),
                new Vector3(0.98f, 1.95f, 0.06f), navyM, "banner");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 1.7f, -0.035f),
                new Vector3(0.86f, 0.46f, 0.02f), whiteM, "logoPlate");
            TextLabel(g.transform, "КонсультантПлюс", new Vector3(0f, 1.76f, -0.06f),
                0.078f, navy, 0f);
            TextLabel(g.transform, "надёжная правовая поддержка", new Vector3(0f, 1.60f, -0.06f),
                0.042f, navy, 0f);
            TextLabel(g.transform, "К+", new Vector3(0f, 0.95f, -0.05f), 0.30f,
                new Color(1f, 0.92f, 0.62f), 0f);
            GameObject stripe = Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 0.42f, -0.035f),
                new Vector3(0.86f, 0.06f, 0.02f), goldM, "goldStripe");
            NoShadow(stripe);
            return g;
        }

        /// <summary>Стеклянная башня для силуэта Москва-Сити на заднике.</summary>
        public static GameObject CreateGlassTower(Transform parent, Vector3 pos, float width,
            float height, Color glass, Color glow, int seed)
        {
            GameObject g = Group(parent, "glassTower", pos);
            Material win = MaterialFactory.WindowsMaterial(glass, glow, seed);
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, height * 0.5f, 0f),
                new Vector3(width, height, width), win, "body");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, height + 0.35f, 0f),
                new Vector3(width * 0.55f, 0.7f, width * 0.55f), win, "top");
            return g;
        }

        // ------------------------------------------------------------------
        // Боссы: перегороженный проход + фигура за кафедрой/трибуной
        // ------------------------------------------------------------------

        private static readonly Color BossSkin = new Color(0.93f, 0.78f, 0.66f);
        private static readonly Color BossDark = new Color(0.16f, 0.14f, 0.15f);

        /// <summary>
        /// Сцена босса перед игроком: золотая баррикада со стойками и табличкой
        /// «ПРОХОД ЗАКРЫТ», подиум, фигура босса, лучи света. Уничтожается
        /// GameManager-ом после развязки; OpenBossBarrier открывает проход.
        /// </summary>
        public static GameObject CreateBossStage(BossData boss, LevelData data)
        {
            GameObject root = new GameObject("BossStage");
            root.transform.position = new Vector3(0f, 0f, 11.5f);
            Transform t = root.transform;
            LevelPalette p = data != null ? data.Palette : null;
            Color navy = new Color(0.07f, 0.10f, 0.26f);
            Color goldC = new Color(1f, 0.85f, 0.45f);
            Material goldM = MaterialFactory.Solid(new Color(0.9f, 0.74f, 0.34f), 0.85f, 0.85f);

            // Красная дорожка к боссу.
            GameObject carpet = Part(t, PrimitiveType.Cube, new Vector3(0f, 0.012f, 1.2f),
                new Vector3(4.6f, 0.02f, 7.5f),
                MaterialFactory.Solid(new Color(0.55f, 0.06f, 0.06f), 0.45f, 0f), "carpet");
            NoShadow(carpet);

            // Баррикада: стойки + две золотые ленты + табличка по центру.
            GameObject barrier = Group(t, "Barrier", new Vector3(0f, 0f, -1.8f));
            for (int i = -2; i <= 2; i++)
            {
                CreateStanchionPost(barrier.transform, new Vector3(i * 2.0f, 0f, 0f), goldM);
            }
            for (int lvl = 0; lvl < 2; lvl++)
            {
                GameObject band = Part(barrier.transform, PrimitiveType.Cube,
                    new Vector3(0f, 0.62f + lvl * 0.32f, 0f), new Vector3(8.2f, 0.07f, 0.05f),
                    MaterialFactory.Emissive(new Color(0.85f, 0.68f, 0.28f) * 0.8f, goldC, 0.8f), "band");
                NoShadow(band);
            }
            SignBoard(barrier.transform, "ПРОХОД ЗАКРЫТ", new Vector3(0f, 1.35f, 0f),
                new Vector2(2.6f, 0.6f), navy, goldC, 0f, true);

            // Зелёная табличка «ПРОХОД ОТКРЫТ» — включается при победе.
            GameObject openSign = SignBoard(t, "ПРОХОД ОТКРЫТ", new Vector3(0f, 1.35f, -1.8f),
                new Vector2(2.6f, 0.6f), new Color(0.05f, 0.28f, 0.10f),
                new Color(0.7f, 1f, 0.6f), 0f, true);
            openSign.name = "OpenSign";
            openSign.SetActive(false);

            // Подиум босса.
            Part(t, PrimitiveType.Cube, new Vector3(0f, 0.12f, 2.6f),
                new Vector3(5.4f, 0.24f, 3.2f),
                MaterialFactory.Solid(new Color(0.32f, 0.22f, 0.14f), 0.6f, 0.1f), "podium");

            // Фигура босса за кафедрой/трибуной — слегка крупнее жизни,
            // чтобы читалась из-за баррикады как «босс».
            GameObject figure = Group(t, "Figure", new Vector3(0f, 0.24f, 2.8f));
            figure.transform.localScale = Vector3.one * 1.3f;
            switch (boss != null ? boss.Id : "judge")
            {
                case "deputy": BuildBossDeputy(figure.transform); break;
                case "chairman": BuildBossChairman(figure.transform); break;
                default: BuildBossJudge(figure.transform); break;
            }

            // Золотые лучи и пятно света — босс в сиянии (bloom дожмёт).
            Material beam = MaterialFactory.AdditiveBeam(new Color(1f, 0.82f, 0.42f, 0.5f));
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject b1 = PartRot(t, PrimitiveType.Quad,
                    new Vector3(2.6f * side, 3.1f, 3.6f), new Vector3(1.4f, 6.4f, 1f),
                    new Vector3(0f, 0f, 0f), beam, "beam");
                NoShadow(b1);
            }
            GameObject pool = PartRot(t, PrimitiveType.Quad, new Vector3(0f, 0.03f, 2.6f),
                new Vector3(6.5f, 5f, 1f), new Vector3(90f, 0f, 0f),
                MaterialFactory.AdditiveGlow(new Color(1f, 0.8f, 0.4f, 0.4f)), "glowPool");
            NoShadow(pool);

            // Колонны по бокам — торжественная рамка кадра.
            CreateColumn(t, new Vector3(-4.6f, 0f, 3.4f), 5.0f);
            CreateColumn(t, new Vector3(4.6f, 0f, 3.4f), 5.0f);

            return root;
        }

        /// <summary>Победа над боссом: баррикада исчезает, загорается «ПРОХОД ОТКРЫТ».</summary>
        public static void OpenBossBarrier(GameObject stage)
        {
            if (stage == null) return;
            Transform barrier = stage.transform.Find("Barrier");
            if (barrier != null) barrier.gameObject.SetActive(false);
            Transform openSign = stage.transform.Find("OpenSign");
            if (openSign != null) openSign.gameObject.SetActive(true);
        }

        private static void CreateStanchionPost(Transform parent, Vector3 pos, Material goldM)
        {
            Part(parent, PrimitiveType.Cylinder, pos + new Vector3(0f, 0.05f, 0f),
                new Vector3(0.34f, 0.03f, 0.34f), goldM, "postBase");
            Part(parent, PrimitiveType.Cylinder, pos + new Vector3(0f, 0.55f, 0f),
                new Vector3(0.07f, 0.5f, 0.07f), goldM, "postPole");
            Part(parent, PrimitiveType.Sphere, pos + new Vector3(0f, 1.08f, 0f),
                new Vector3(0.14f, 0.14f, 0.14f), goldM, "postTop");
        }

        /// <summary>Общая голова босса: лицо, суровые брови, глаза, рот.</summary>
        private static GameObject BuildBossHead(Transform parent, Vector3 pos, float scale, bool stern)
        {
            GameObject head = Group(parent, "head", pos);
            Part(head.transform, PrimitiveType.Sphere, Vector3.zero,
                new Vector3(0.5f, 0.56f, 0.5f) * scale, MaterialFactory.Solid(BossSkin, 0.35f, 0f), "face");
            Material dark = MaterialFactory.Solid(BossDark, 0.3f, 0f);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(head.transform, PrimitiveType.Sphere,
                    new Vector3(0.10f * side, 0.04f, -0.21f) * scale,
                    new Vector3(0.055f, 0.06f, 0.03f) * scale, dark, "eye");
                float tilt = stern ? -14f * side : 0f;
                GameObject brow = PartRot(head.transform, PrimitiveType.Cube,
                    new Vector3(0.10f * side, 0.13f, -0.22f) * scale,
                    new Vector3(0.13f, 0.028f, 0.02f) * scale, new Vector3(0f, 0f, tilt), dark, "brow");
                NoShadow(brow);
            }
            GameObject mouth = Part(head.transform, PrimitiveType.Cube,
                new Vector3(0f, -0.15f, -0.225f) * scale,
                new Vector3(0.16f, 0.02f, 0.02f) * scale, dark, "mouth");
            NoShadow(mouth);
            // Нос.
            Part(head.transform, PrimitiveType.Cube, new Vector3(0f, -0.03f, -0.24f) * scale,
                new Vector3(0.06f, 0.10f, 0.06f) * scale,
                MaterialFactory.Solid(BossSkin * 0.96f, 0.3f, 0f), "nose");
            return head;
        }

        /// <summary>Босс 1: судья ВС в мантии за кафедрой, с зелёной обложкой-«уликой».</summary>
        private static void BuildBossJudge(Transform t)
        {
            Material wood = MaterialFactory.Solid(new Color(0.30f, 0.19f, 0.11f), 0.6f, 0.08f);
            Material robe = MaterialFactory.Solid(new Color(0.09f, 0.08f, 0.10f), 0.45f, 0.05f);
            Material goldM = MaterialFactory.Solid(new Color(0.9f, 0.74f, 0.34f), 0.85f, 0.85f);

            // Красное судейское кресло с высокой спинкой (как в референсе).
            Part(t, PrimitiveType.Cube, new Vector3(0f, 1.55f, 0.75f),
                new Vector3(1.25f, 2.1f, 0.16f),
                MaterialFactory.Solid(new Color(0.45f, 0.09f, 0.08f), 0.5f, 0.05f), "chairBack");

            // Кафедра с гербом и монитором.
            Part(t, PrimitiveType.Cube, new Vector3(0f, 0.7f, -0.3f),
                new Vector3(3.4f, 1.4f, 0.85f), wood, "bench");
            Part(t, PrimitiveType.Cube, new Vector3(0f, 1.44f, -0.3f),
                new Vector3(3.6f, 0.1f, 1.0f), wood, "benchTop");
            GameObject emblem = PartRot(t, PrimitiveType.Cylinder, new Vector3(0f, 0.85f, -0.74f),
                new Vector3(0.5f, 0.015f, 0.5f), new Vector3(90f, 0f, 0f), goldM, "benchEmblem");
            NoShadow(emblem);
            Part(t, PrimitiveType.Cube, new Vector3(0.75f, 1.75f, -0.28f),
                new Vector3(0.75f, 0.5f, 0.06f),
                MaterialFactory.Solid(new Color(0.06f, 0.07f, 0.09f), 0.7f, 0.3f), "monitor");
            Part(t, PrimitiveType.Cylinder, new Vector3(-0.6f, 1.72f, -0.35f),
                new Vector3(0.025f, 0.22f, 0.025f),
                MaterialFactory.Solid(BossDark, 0.5f, 0.3f), "micPole");
            Part(t, PrimitiveType.Sphere, new Vector3(-0.6f, 1.96f, -0.35f),
                new Vector3(0.07f, 0.07f, 0.07f), MaterialFactory.Solid(BossDark, 0.5f, 0.3f), "micHead");

            // Мантия, белое жабо, голова.
            Part(t, PrimitiveType.Cube, new Vector3(0f, 1.85f, 0.28f),
                new Vector3(1.15f, 0.95f, 0.55f), robe, "robe");
            Part(t, PrimitiveType.Cube, new Vector3(0f, 2.1f, 0.02f),
                new Vector3(0.34f, 0.30f, 0.08f),
                MaterialFactory.Solid(new Color(0.96f, 0.96f, 0.94f), 0.35f, 0f), "jabot");
            GameObject head = BuildBossHead(t, new Vector3(0f, 2.62f, 0.28f), 1.15f, true);

            // Светлые волосы: каре с чёлкой.
            Material hair = MaterialFactory.Solid(new Color(0.83f, 0.62f, 0.36f), 0.4f, 0f);
            Part(head.transform, PrimitiveType.Sphere, new Vector3(0f, 0.14f, 0.03f),
                new Vector3(0.56f, 0.42f, 0.56f), hair, "hairCap");
            Part(head.transform, PrimitiveType.Cube, new Vector3(0f, 0.20f, -0.20f),
                new Vector3(0.46f, 0.14f, 0.12f), hair, "fringe");
            for (int side = -1; side <= 1; side += 2)
            {
                Part(head.transform, PrimitiveType.Cube, new Vector3(0.26f * side, -0.05f, 0.05f),
                    new Vector3(0.10f, 0.42f, 0.30f), hair, "hairSide");
            }

            // Очки: линзы + перемычка.
            Material glassM = MaterialFactory.Solid(new Color(0.2f, 0.2f, 0.22f), 0.7f, 0.4f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject lens = Part(head.transform, PrimitiveType.Cube,
                    new Vector3(0.115f * side, 0.045f, -0.245f),
                    new Vector3(0.14f, 0.10f, 0.015f), glassM, "lens");
                NoShadow(lens);
            }
            GameObject bridge = Part(head.transform, PrimitiveType.Cube,
                new Vector3(0f, 0.05f, -0.245f), new Vector3(0.06f, 0.02f, 0.015f), glassM, "bridge");
            NoShadow(bridge);

            // Поднятая рука с зелёной обложкой со скелетом (та самая).
            GameObject arm = PartRot(t, PrimitiveType.Cube, new Vector3(0.72f, 2.25f, 0.1f),
                new Vector3(0.22f, 0.7f, 0.22f), new Vector3(0f, 0f, -18f), robe, "arm");
            GameObject cover = PartRot(t, PrimitiveType.Cube, new Vector3(0.88f, 2.75f, 0.05f),
                new Vector3(0.34f, 0.46f, 0.035f), new Vector3(0f, 0f, -8f),
                MaterialFactory.Solid(new Color(0.13f, 0.5f, 0.22f), 0.5f, 0.05f), "greenCover");
            // Мультяшный «череп» на обложке.
            GameObject skull = Part(cover.transform, PrimitiveType.Sphere,
                new Vector3(0f, 0.06f, -0.6f), new Vector3(0.45f, 0.5f, 0.4f),
                MaterialFactory.Solid(new Color(0.93f, 0.92f, 0.86f), 0.35f, 0f), "skull");
            Material dark = MaterialFactory.Solid(BossDark, 0.3f, 0f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject eyeHole = Part(skull.transform, PrimitiveType.Sphere,
                    new Vector3(0.22f * side, 0.05f, -0.45f), new Vector3(0.2f, 0.24f, 0.2f), dark, "eyeHole");
                NoShadow(eyeHole);
            }
        }

        /// <summary>Босс 2: депутат у трибуны, над ним «АДВОКАТСКАЯ МОНОПОЛИЯ».</summary>
        private static void BuildBossDeputy(Transform t)
        {
            Color navy = new Color(0.07f, 0.10f, 0.26f);
            Material navyM = MaterialFactory.Solid(navy, 0.5f, 0.05f);
            Material goldM = MaterialFactory.Solid(new Color(0.9f, 0.74f, 0.34f), 0.85f, 0.85f);

            // Трибуна с гербом.
            Part(t, PrimitiveType.Cube, new Vector3(0f, 0.75f, -0.25f),
                new Vector3(1.7f, 1.5f, 0.8f), navyM, "tribune");
            Part(t, PrimitiveType.Cube, new Vector3(0f, 1.53f, -0.25f),
                new Vector3(1.9f, 0.08f, 0.95f),
                MaterialFactory.Solid(new Color(0.30f, 0.19f, 0.11f), 0.6f, 0.1f), "tribuneTop");
            GameObject emblem = PartRot(t, PrimitiveType.Cylinder, new Vector3(0f, 0.95f, -0.66f),
                new Vector3(0.44f, 0.015f, 0.44f), new Vector3(90f, 0f, 0f), goldM, "emblem");
            NoShadow(emblem);
            Part(t, PrimitiveType.Cylinder, new Vector3(0.35f, 1.75f, -0.3f),
                new Vector3(0.02f, 0.2f, 0.02f), MaterialFactory.Solid(BossDark, 0.5f, 0.3f), "mic");
            Part(t, PrimitiveType.Sphere, new Vector3(0.35f, 1.96f, -0.3f),
                new Vector3(0.06f, 0.06f, 0.06f), MaterialFactory.Solid(BossDark, 0.5f, 0.3f), "micHead");

            // Костюм: серый пиджак, белая рубашка, красный галстук.
            Material suit = MaterialFactory.Solid(new Color(0.38f, 0.38f, 0.42f), 0.45f, 0.05f);
            Part(t, PrimitiveType.Cube, new Vector3(0f, 1.9f, 0.2f),
                new Vector3(1.05f, 0.9f, 0.5f), suit, "torso");
            Part(t, PrimitiveType.Cube, new Vector3(0f, 2.02f, -0.06f),
                new Vector3(0.30f, 0.55f, 0.06f),
                MaterialFactory.Solid(new Color(0.95f, 0.95f, 0.93f), 0.35f, 0f), "shirt");
            GameObject tie = PartRot(t, PrimitiveType.Cube, new Vector3(0f, 1.95f, -0.09f),
                new Vector3(0.11f, 0.42f, 0.03f), new Vector3(0f, 0f, 0f),
                MaterialFactory.Solid(new Color(0.72f, 0.10f, 0.10f), 0.5f, 0.05f), "tie");
            NoShadow(tie);

            GameObject head = BuildBossHead(t, new Vector3(0f, 2.62f, 0.2f), 1.1f, false);
            Material hair = MaterialFactory.Solid(new Color(0.22f, 0.17f, 0.13f), 0.4f, 0f);
            Part(head.transform, PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0.04f),
                new Vector3(0.52f, 0.34f, 0.52f), hair, "hairCap");

            // Поднятая рука с законопроектом.
            PartRot(t, PrimitiveType.Cube, new Vector3(-0.68f, 2.3f, 0.1f),
                new Vector3(0.2f, 0.72f, 0.2f), new Vector3(0f, 0f, 24f), suit, "arm");
            PartRot(t, PrimitiveType.Cube, new Vector3(-0.9f, 2.78f, 0.05f),
                new Vector3(0.36f, 0.48f, 0.03f), new Vector3(0f, 0f, 10f),
                MaterialFactory.Solid(new Color(0.96f, 0.94f, 0.88f), 0.35f, 0f), "bill");

            // Большая табличка на стойках позади.
            for (int side = -1; side <= 1; side += 2)
            {
                Part(t, PrimitiveType.Cylinder, new Vector3(2.3f * side, 1.7f, 0.9f),
                    new Vector3(0.06f, 1.7f, 0.06f), goldM, "signPole");
            }
            SignBoard(t, "АДВОКАТСКАЯ МОНОПОЛИЯ", new Vector3(0f, 3.5f, 0.9f),
                new Vector2(4.9f, 0.75f), new Color(0.55f, 0.08f, 0.07f),
                new Color(1f, 0.93f, 0.85f), 0f, true);
        }

        /// <summary>Босс 3: председатель ВС за высокой кафедрой, мантия с золотой отделкой.</summary>
        private static void BuildBossChairman(Transform t)
        {
            Material wood = MaterialFactory.Solid(new Color(0.26f, 0.16f, 0.10f), 0.6f, 0.08f);
            Material marble = MaterialFactory.Solid(new Color(0.78f, 0.74f, 0.68f), 0.75f, 0.1f);
            Material robe = MaterialFactory.Solid(new Color(0.08f, 0.07f, 0.09f), 0.45f, 0.05f);
            Material goldM = MaterialFactory.Solid(new Color(0.9f, 0.74f, 0.34f), 0.85f, 0.85f);

            // Мраморная плита-задник и высокая кафедра.
            Part(t, PrimitiveType.Cube, new Vector3(0f, 2.1f, 1.15f),
                new Vector3(3.4f, 3.6f, 0.2f), marble, "backSlab");
            Part(t, PrimitiveType.Cube, new Vector3(0f, 0.9f, -0.3f),
                new Vector3(3.8f, 1.8f, 0.95f), wood, "bench");
            Part(t, PrimitiveType.Cube, new Vector3(0f, 1.84f, -0.3f),
                new Vector3(4.0f, 0.1f, 1.1f), wood, "benchTop");
            GameObject emblem = PartRot(t, PrimitiveType.Cylinder, new Vector3(0f, 1.1f, -0.79f),
                new Vector3(0.62f, 0.015f, 0.62f), new Vector3(90f, 0f, 0f), goldM, "emblem");
            NoShadow(emblem);
            // Три микрофона, красная папка, стопка бумаг.
            for (int i = -1; i <= 1; i++)
            {
                Part(t, PrimitiveType.Cylinder, new Vector3(i * 0.5f, 2.05f, -0.42f),
                    new Vector3(0.02f, 0.18f, 0.02f), MaterialFactory.Solid(BossDark, 0.5f, 0.3f), "mic");
                Part(t, PrimitiveType.Sphere, new Vector3(i * 0.5f, 2.24f, -0.42f),
                    new Vector3(0.06f, 0.06f, 0.06f), MaterialFactory.Solid(BossDark, 0.5f, 0.3f), "micHead");
            }
            Part(t, PrimitiveType.Cube, new Vector3(0.95f, 1.93f, -0.35f),
                new Vector3(0.5f, 0.06f, 0.36f),
                MaterialFactory.Solid(new Color(0.62f, 0.10f, 0.09f), 0.45f, 0f), "redFolder");
            Part(t, PrimitiveType.Cube, new Vector3(-0.95f, 1.95f, -0.35f),
                new Vector3(0.44f, 0.10f, 0.32f),
                MaterialFactory.Solid(new Color(0.94f, 0.92f, 0.86f), 0.35f, 0f), "papers");

            // Мантия с золотой плетёной отделкой (как на референсе).
            Part(t, PrimitiveType.Cube, new Vector3(0f, 2.3f, 0.28f),
                new Vector3(1.25f, 1.0f, 0.55f), robe, "robe");
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject trim = PartRot(t, PrimitiveType.Cube,
                    new Vector3(0.28f * side, 2.32f, 0.0f),
                    new Vector3(0.07f, 0.95f, 0.03f), new Vector3(0f, 0f, -6f * side), goldM, "goldTrim");
                NoShadow(trim);
            }
            Part(t, PrimitiveType.Cube, new Vector3(0f, 2.62f, 0.02f),
                new Vector3(0.30f, 0.16f, 0.07f),
                MaterialFactory.Solid(new Color(0.95f, 0.95f, 0.93f), 0.35f, 0f), "collar");

            GameObject head = BuildBossHead(t, new Vector3(0f, 3.05f, 0.28f), 1.15f, true);
            Material hair = MaterialFactory.Solid(new Color(0.19f, 0.15f, 0.12f), 0.4f, 0f);
            Part(head.transform, PrimitiveType.Sphere, new Vector3(0f, 0.17f, 0.04f),
                new Vector3(0.54f, 0.32f, 0.54f), hair, "hairCap");
        }

        // ------------------------------------------------------------------
        // Узнаваемость локаций: Москва-Сити, АС Москвы, Росреестр, очереди
        // ------------------------------------------------------------------

        /// <summary>Панорамное окно офиса с видом на Москва-Сити (эмиссивный постер).</summary>
        public static GameObject CreateOfficeWindow(Transform parent, Vector3 pos, float rotY, int seed)
        {
            GameObject g = Group(parent, "cityWindow", pos);
            g.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material frame = MaterialFactory.Solid(new Color(0.18f, 0.16f, 0.15f), 0.55f, 0.2f);

            GameObject pane = Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 1.5f, 0.02f),
                new Vector3(3.3f, 2.0f, 0.06f), MaterialFactory.SkylineMaterial(seed), "skyline");
            NoShadow(pane);
            // Рама и перемычки.
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 2.55f, 0f),
                new Vector3(3.5f, 0.1f, 0.12f), frame, "frameT");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f),
                new Vector3(3.5f, 0.12f, 0.18f), frame, "sill");
            for (int i = -1; i <= 1; i++)
            {
                Part(g.transform, PrimitiveType.Cube, new Vector3(i * 1.72f, 1.5f, 0f),
                    new Vector3(0.1f, 2.2f, 0.12f), frame, "mullion");
            }
            return g;
        }

        /// <summary>Рамка-металлодетектор с огоньком.</summary>
        public static GameObject CreateMetalDetector(Transform parent, Vector3 pos, float rotY)
        {
            GameObject g = Group(parent, "metalDetector", pos);
            g.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material metal = MaterialFactory.Solid(new Color(0.42f, 0.44f, 0.48f), 0.65f, 0.4f);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(g.transform, PrimitiveType.Cube, new Vector3(0.55f * side, 1.05f, 0f),
                    new Vector3(0.14f, 2.1f, 0.32f), metal, "post");
            }
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 2.14f, 0f),
                new Vector3(1.24f, 0.18f, 0.32f), metal, "top");
            GameObject lamp = Part(g.transform, PrimitiveType.Sphere, new Vector3(0f, 2.14f, -0.18f),
                new Vector3(0.09f, 0.09f, 0.09f),
                MaterialFactory.Neon(new Color(0.3f, 1f, 0.4f)), "lamp");
            NoShadow(lamp);
            return g;
        }

        /// <summary>Флагшток с триколором.</summary>
        public static GameObject CreateFlagPole(Transform parent, Vector3 pos, float height)
        {
            GameObject g = Group(parent, "flagPole", pos);
            Part(g.transform, PrimitiveType.Cylinder, new Vector3(0f, height * 0.5f, 0f),
                new Vector3(0.06f, height * 0.5f, 0.06f),
                MaterialFactory.Solid(new Color(0.7f, 0.72f, 0.75f), 0.7f, 0.6f), "pole");
            Color[] bands = { Color.white, new Color(0.16f, 0.32f, 0.7f), new Color(0.8f, 0.12f, 0.12f) };
            for (int i = 0; i < 3; i++)
            {
                GameObject band = Part(g.transform, PrimitiveType.Cube,
                    new Vector3(0.52f, height - 0.22f - i * 0.2f, 0f),
                    new Vector3(1.0f, 0.2f, 0.03f),
                    MaterialFactory.Solid(bands[i], 0.4f, 0f), "band" + i);
                NoShadow(band);
            }
            return g;
        }

        public static GameObject CreateStreetTree(Transform parent, Vector3 pos, float scale, int seed)
        {
            GameObject g = Group(parent, "streetTree", pos);
            g.transform.localScale = new Vector3(scale, scale, scale);

            System.Random rng = new System.Random(seed);
            Material trunk = MaterialFactory.Solid(new Color(0.34f, 0.22f, 0.12f), 0.45f, 0f);
            Material leaf = MaterialFactory.Solid(new Color(0.18f, 0.42f, 0.16f), 0.38f, 0f);
            Material leafWarm = MaterialFactory.Solid(new Color(0.36f, 0.54f, 0.18f), 0.36f, 0f);

            Part(g.transform, PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f),
                new Vector3(0.12f, 1.0f, 0.12f), trunk, "trunk");
            for (int i = 0; i < 4; i++)
            {
                float x = -0.28f + i * 0.18f + (float)rng.NextDouble() * 0.12f;
                float y = 2.0f + (float)rng.NextDouble() * 0.55f;
                float z = -0.16f + (float)rng.NextDouble() * 0.32f;
                Material m = i % 2 == 0 ? leaf : leafWarm;
                Part(g.transform, PrimitiveType.Sphere, new Vector3(x, y, z),
                    new Vector3(0.72f, 0.60f, 0.72f), m, "crown" + i);
            }
            return g;
        }

        private static void AddFacadeWindowGrid(Transform parent, int columns, int rows,
            float startX, float startY, float z, float stepX, float stepY,
            Vector2 paneSize, Material mat, string prefix)
        {
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    GameObject pane = Part(parent, PrimitiveType.Cube,
                        new Vector3(startX + x * stepX, startY + y * stepY, z),
                        new Vector3(paneSize.x, paneSize.y, 0.045f), mat,
                        prefix + "_" + x + "_" + y);
                    NoShadow(pane);
                }
            }
        }

        public static GameObject CreateRosreestrCityFacade(Transform parent, Vector3 pos, float rotY)
        {
            GameObject g = Group(parent, "rosreestrCityFacade", pos);
            g.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

            Color stoneColor = new Color(0.72f, 0.66f, 0.54f);
            Color navy = new Color(0.03f, 0.22f, 0.45f);
            Material stone = MaterialFactory.Solid(stoneColor, 0.52f, 0.05f);
            Material stoneDark = MaterialFactory.Solid(stoneColor * 0.72f, 0.55f, 0.05f);
            Material glass = MaterialFactory.Emissive(new Color(0.10f, 0.16f, 0.18f),
                new Color(0.26f, 0.45f, 0.52f), 0.35f);
            Material canopy = MaterialFactory.Solid(new Color(0.08f, 0.08f, 0.09f), 0.72f, 0.22f);
            Material blue = MaterialFactory.Solid(new Color(0.05f, 0.36f, 0.62f), 0.55f, 0.1f);
            Material green = MaterialFactory.Solid(new Color(0.47f, 0.62f, 0.22f), 0.50f, 0.05f);

            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 3.05f, 0.85f),
                new Vector3(7.4f, 6.1f, 1.7f), stone, "mainBlock");
            Part(g.transform, PrimitiveType.Cube, new Vector3(-3.95f, 3.05f, 0.55f),
                new Vector3(0.34f, 6.2f, 1.9f), stoneDark, "sideRib");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 6.28f, 0.85f),
                new Vector3(7.8f, 0.30f, 1.9f), stoneDark, "parapet");

            AddFacadeWindowGrid(g.transform, 4, 3, -2.05f, 1.75f, -0.04f, 1.35f, 0.82f,
                new Vector2(0.82f, 0.50f), glass, "upperWindow");
            AddFacadeWindowGrid(g.transform, 3, 1, -1.35f, 0.82f, -0.05f, 1.35f, 0.82f,
                new Vector2(0.88f, 0.56f), glass, "lowerWindow");

            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 1.15f, -0.16f),
                new Vector3(3.25f, 2.30f, 0.15f), glass, "entranceGlass");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 2.35f, -0.48f),
                new Vector3(4.25f, 0.24f, 1.10f), canopy, "canopy");
            StaticIllustrationFactory.CreateUnframedBackdrop(g.transform, "Rosreestr_Sign",
                new Vector2(3.5f, 0.78f), new Vector3(0.62f, 2.82f, -0.60f));

            PartRot(g.transform, PrimitiveType.Cylinder, new Vector3(-2.55f, 4.55f, -0.18f),
                new Vector3(0.38f, 0.018f, 0.38f), new Vector3(90f, 0f, 0f), blue, "logoBlue");
            GameObject logoGreen = PartRot(g.transform, PrimitiveType.Cylinder, new Vector3(-2.34f, 4.33f, -0.19f),
                new Vector3(0.34f, 0.018f, 0.34f), new Vector3(90f, 0f, 0f), green, "logoGreen");
            NoShadow(logoGreen);
            TextLabel(g.transform, "РОСРЕЕСТР", new Vector3(0.78f, 4.18f, -0.32f), 0.52f,
                navy, 0f);
            TextLabel(g.transform, "Управление Федеральной службы\nгосрегистрации, кадастра\nи картографии по Москве",
                new Vector3(0.84f, 3.36f, -0.32f), 0.145f, new Color(0.12f, 0.12f, 0.12f), 0f);
            TextLabel(g.transform, "Выдача выписок ЕГРН", new Vector3(0f, 1.85f, -0.27f),
                0.12f, new Color(0.95f, 0.97f, 1f), 0f);

            for (int i = 0; i < 4; i++)
            {
                Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 0.07f + i * 0.12f, -0.72f - i * 0.18f),
                    new Vector3(4.4f - i * 0.35f, 0.11f, 0.48f), stoneDark, "step" + i);
            }
            return g;
        }

        public static GameObject CreateGovernmentCityFacade(Transform parent, Vector3 pos, float rotY)
        {
            GameObject g = Group(parent, "governmentCityFacade", pos);
            g.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

            Material stone = MaterialFactory.Solid(new Color(0.66f, 0.59f, 0.48f), 0.55f, 0.08f);
            Material darkStone = MaterialFactory.Solid(new Color(0.24f, 0.20f, 0.18f), 0.70f, 0.20f);
            Material glass = MaterialFactory.Emissive(new Color(0.07f, 0.11f, 0.13f),
                new Color(0.22f, 0.35f, 0.42f), 0.32f);

            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 3.45f, 0.95f),
                new Vector3(7.1f, 6.9f, 1.9f), darkStone, "glassTower");
            Part(g.transform, PrimitiveType.Cube, new Vector3(-3.7f, 3.45f, 0.85f),
                new Vector3(0.28f, 6.9f, 2.05f), stone, "stoneRibL");
            Part(g.transform, PrimitiveType.Cube, new Vector3(3.7f, 3.45f, 0.85f),
                new Vector3(0.28f, 6.9f, 2.05f), stone, "stoneRibR");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 6.96f, 0.95f),
                new Vector3(7.6f, 0.28f, 2.05f), stone, "roofCap");

            AddFacadeWindowGrid(g.transform, 5, 5, -2.45f, 1.45f, -0.08f, 1.22f, 0.92f,
                new Vector2(0.78f, 0.58f), glass, "governmentWindow");
            for (int x = -2; x <= 2; x++)
            {
                Part(g.transform, PrimitiveType.Cube, new Vector3(x * 1.22f, 3.45f, -0.12f),
                    new Vector3(0.08f, 5.8f, 0.08f), stone, "mullion" + x);
            }

            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 1.16f, -0.20f),
                new Vector3(2.75f, 2.15f, 0.16f), glass, "entryGlass");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 2.45f, -0.54f),
                new Vector3(3.65f, 0.24f, 1.05f), darkStone, "entryCanopy");
            CreateCourtEmblem(g.transform, new Vector3(0f, 3.42f, -0.23f), 0f, 0.72f);

            for (int i = 0; i < 3; i++)
            {
                Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 0.07f + i * 0.12f, -0.70f - i * 0.18f),
                    new Vector3(3.8f - i * 0.32f, 0.11f, 0.46f), stone, "step" + i);
            }
            return g;
        }

        public static GameObject CreateCourtDestinationFacade(Transform parent, Vector3 pos, float rotY, float scale)
        {
            GameObject g = CreateCourtEntrance(parent, pos, rotY);
            g.name = "courtDestinationFacade";
            g.transform.localScale = new Vector3(scale, scale, scale);
            return g;
        }

        /// <summary>Входная группа Арбитражного суда Москвы: фасад, вывеска,
        /// герб, колонны, рамки, флаг — «это арбитраж» с одного взгляда.</summary>
        public static GameObject CreateCourtEntrance(Transform parent, Vector3 pos, float rotY)
        {
            GameObject g = Group(parent, "courtEntrance", pos);
            g.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Material stone = MaterialFactory.Solid(new Color(0.72f, 0.68f, 0.62f), 0.55f, 0.08f);
            Color navy = new Color(0.07f, 0.10f, 0.26f);

            // Фасадная плита с тёмным порталом входа.
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 2.2f, 0.5f),
                new Vector3(6.4f, 4.4f, 0.5f), stone, "facade");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 1.35f, 0.22f),
                new Vector3(2.4f, 2.7f, 0.15f),
                MaterialFactory.Solid(new Color(0.10f, 0.10f, 0.12f), 0.7f, 0.2f), "doorway");
            // Ступени.
            for (int i = 0; i < 2; i++)
            {
                Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 0.07f + i * 0.14f, -0.1f - i * -0.18f),
                    new Vector3(3.6f - i * 0.5f, 0.14f, 0.5f), stone, "step" + i);
            }
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 3.58f, 0.18f),
                new Vector3(5.7f, 0.55f, 0.12f),
                MaterialFactory.Solid(stone.color * 0.82f, 0.55f, 0.08f), "blankLintel");
            CreateCourtEmblem(g.transform, new Vector3(0f, 4.6f, 0.45f), 0f, 0.7f);
            SignBoard(g.transform, "Б. ТУЛЬСКАЯ, 17", new Vector3(-2.6f, 2.4f, 0.2f),
                new Vector2(1.7f, 0.4f), navy, new Color(0.95f, 0.97f, 1f), 0f, true);
            // Колонны, рамки и флаг.
            CreateColumn(g.transform, new Vector3(-1.9f, 0f, 0.05f), 4.0f);
            CreateColumn(g.transform, new Vector3(1.9f, 0f, 0.05f), 4.0f);
            CreateMetalDetector(g.transform, new Vector3(-0.75f, 0f, -0.55f), 0f);
            CreateMetalDetector(g.transform, new Vector3(0.75f, 0f, -0.55f), 0f);
            CreateFlagPole(g.transform, new Vector3(2.9f, 0f, -0.6f), 4.6f);
            return g;
        }

        /// <summary>Вход Росреестра: синяя стела, козырёк, «МОИ ДОКУМЕНТЫ».</summary>
        public static GameObject CreateRosreestrEntrance(Transform parent, Vector3 pos, float rotY)
        {
            GameObject g = Group(parent, "rosreestr", pos);
            g.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            Color navy = new Color(0.05f, 0.22f, 0.48f);
            Material navyM = MaterialFactory.Solid(navy, 0.55f, 0.05f);
            Material white = MaterialFactory.Solid(new Color(0.94f, 0.95f, 0.97f), 0.45f, 0f);

            // Стела с логотипом.
            Part(g.transform, PrimitiveType.Cube, new Vector3(-1.9f, 1.3f, -0.4f),
                new Vector3(0.7f, 2.6f, 0.35f), navyM, "stela");
            TextLabel(g.transform, "РОСРЕЕСТР", new Vector3(-1.9f, 1.7f, -0.6f), 0.11f,
                new Color(0.95f, 0.97f, 1f), 0f);
            GameObject logo = PartRot(g.transform, PrimitiveType.Cylinder,
                new Vector3(-1.9f, 2.25f, -0.59f), new Vector3(0.3f, 0.012f, 0.3f),
                new Vector3(90f, 0f, 0f), white, "logo");
            NoShadow(logo);

            // Входная группа с козырьком.
            Part(g.transform, PrimitiveType.Cube, new Vector3(0.6f, 1.5f, 0.35f),
                new Vector3(3.4f, 3.0f, 0.4f), white, "wall");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0.6f, 1.2f, 0.12f),
                new Vector3(1.5f, 2.4f, 0.12f),
                MaterialFactory.Solid(new Color(0.35f, 0.55f, 0.72f), 0.8f, 0.3f), "glassDoor");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0.6f, 2.62f, -0.25f),
                new Vector3(2.6f, 0.1f, 0.9f), navyM, "canopy");
            SignBoard(g.transform, "МОИ ДОКУМЕНТЫ", new Vector3(0.6f, 2.95f, 0.1f),
                new Vector2(2.4f, 0.45f), navy, Color.white, 0f, true);
            SignBoard(g.transform, "Выдача выписок ЕГРН", new Vector3(0.6f, 1.9f, 0.05f),
                new Vector2(1.9f, 0.32f), new Color(0.9f, 0.92f, 0.95f),
                new Color(0.1f, 0.2f, 0.4f), 0f, false);
            return g;
        }

        /// <summary>Очередь страдальцев с папками вдоль стены (судебная бытовуха).</summary>
        public static GameObject CreateQueueLine(Transform parent, Vector3 pos, float rotY,
            int count, int seed)
        {
            GameObject g = Group(parent, "queueLine", pos);
            g.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            System.Random rng = new System.Random(seed);
            Material folderM = MaterialFactory.Solid(new Color(0.85f, 0.72f, 0.45f), 0.4f, 0f);
            for (int i = 0; i < count; i++)
            {
                float x = i * 0.62f + (float)rng.NextDouble() * 0.16f;
                float face = 80f + (float)rng.NextDouble() * 20f; // смотрят вдоль очереди
                GameObject person = CreatePerson(g.transform,
                    new Vector3(x, 0f, (float)rng.NextDouble() * 0.14f),
                    new Color(0.25f + (float)rng.NextDouble() * 0.3f,
                              0.22f + (float)rng.NextDouble() * 0.22f,
                              0.25f + (float)rng.NextDouble() * 0.25f),
                    new Color(0.17f, 0.16f, 0.19f),
                    1.55f + (float)rng.NextDouble() * 0.25f, face);
                // Папка с документами в руках.
                Vector3 folderPos = new Vector3(0f, 1.02f, -0.22f);
                GameObject folder = Part(person.transform, PrimitiveType.Cube, folderPos,
                    new Vector3(0.30f, 0.22f, 0.07f), folderM, "folder");
                NoShadow(folder);
            }
            return g;
        }

        /// <summary>Стопки документов на полу — канцелярия не справляется.</summary>
        public static GameObject CreatePaperStacks(Transform parent, Vector3 pos, int seed)
        {
            GameObject g = Group(parent, "paperStacks", pos);
            System.Random rng = new System.Random(seed);
            Material paper = MaterialFactory.Solid(new Color(0.93f, 0.91f, 0.85f), 0.3f, 0f);
            Material folder = MaterialFactory.Solid(new Color(0.78f, 0.62f, 0.38f), 0.4f, 0f);
            int stacks = 2 + rng.Next(2);
            for (int i = 0; i < stacks; i++)
            {
                float h = 0.25f + (float)rng.NextDouble() * 0.4f;
                float x = i * 0.5f + (float)rng.NextDouble() * 0.1f;
                float rot = (float)rng.NextDouble() * 24f - 12f;
                PartRot(g.transform, PrimitiveType.Cube, new Vector3(x, h * 0.5f, 0f),
                    new Vector3(0.36f, h, 0.46f), new Vector3(0f, rot, 0f), paper, "stack" + i);
                PartRot(g.transform, PrimitiveType.Cube, new Vector3(x, h + 0.03f, 0f),
                    new Vector3(0.34f, 0.06f, 0.44f), new Vector3(0f, rot + 6f, 0f), folder, "topFolder" + i);
            }
            return g;
        }

        /// <summary>Сталинская высотка: ярусы, крылья, шпиль со звездой.</summary>
        public static GameObject CreateStalinTower(Transform parent, Vector3 pos, float scale)
        {
            GameObject g = Group(parent, "stalinTower", pos);
            Material stone = MaterialFactory.Solid(new Color(0.64f, 0.54f, 0.42f), 0.45f, 0.05f);
            Material spireM = MaterialFactory.Solid(new Color(0.80f, 0.68f, 0.46f), 0.7f, 0.5f);

            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 3f * scale, 0f),
                new Vector3(6f, 6f, 6f) * scale, stone, "tier1");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 7.4f * scale, 0f),
                new Vector3(4f, 3f, 4f) * scale, stone, "tier2");
            Part(g.transform, PrimitiveType.Cube, new Vector3(0f, 10f * scale, 0f),
                new Vector3(2.4f, 2.6f, 2.4f) * scale, stone, "tier3");
            Part(g.transform, PrimitiveType.Cube, new Vector3(-4.2f * scale, 2f * scale, 0f),
                new Vector3(3f, 4f, 5f) * scale, stone, "wingL");
            Part(g.transform, PrimitiveType.Cube, new Vector3(4.2f * scale, 2f * scale, 0f),
                new Vector3(3f, 4f, 5f) * scale, stone, "wingR");
            Part(g.transform, PrimitiveType.Cylinder, new Vector3(0f, 12.4f * scale, 0f),
                new Vector3(0.25f, 1.5f, 0.25f) * scale, spireM, "spire");
            GameObject star = Part(g.transform, PrimitiveType.Sphere,
                new Vector3(0f, 14.0f * scale, 0f), Vector3.one * 0.4f * scale,
                MaterialFactory.Neon(new Color(1f, 0.62f, 0.28f)), "star");
            NoShadow(star);
            return g;
        }
    }
}
