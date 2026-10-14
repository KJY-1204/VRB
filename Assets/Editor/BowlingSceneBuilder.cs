using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRB;
using VRB.Core;

namespace VRB.EditorTools
{
    /// <summary>
    /// Builds the bowling prefabs and the single-lane environment at real-world scale.
    /// Coordinates: lane runs along +Z, the foul line is at z = 0, the head pin at z = LaneLength,
    /// the floor top is y = 0 and the lane center is x = 0.
    /// </summary>
    public static class BowlingSceneBuilder
    {
        const string BowlingClubFbx = "Assets/VRB/bowling-club/source/bowling club.fbx";
        const string PrefabDir = "Assets/VRB/Prefabs";
        const string MaterialDir = "Assets/VRB/Materials";
        const string PinPrefabPath = PrefabDir + "/Pin.prefab";
        const string BallPrefabPath = PrefabDir + "/Ball.prefab";

        // The FBX meshes are authored in centimeters (pin ~132 tall, ball ~72 wide) with the long axis on local Z.
        const float FbxPinHeight = 132.544f;
        const float FbxPinWidth = 40.097f;
        const float FbxBallDiameter = 72.520f;

        const float HallHalfWidth = 2.5f;
        const float HallBackZ = -5.0f;
        const float HallCeilingY = 3.2f;
        const float PitFloorY = -0.35f;
        const float GutterTopY = -0.12f;

        static float LaneEndZ => BowlingDimensions.LaneLength + 0.9f;
        static float PitEndZ => LaneEndZ + BowlingDimensions.PitLength;

        [MenuItem("VRB/Build All")]
        public static void BuildAll()
        {
            BuildPrefabs();
            BuildEnvironment();
            BuildPins();
            BuildBallStand();
        }

        // ---------------------------------------------------------------- prefabs

        [MenuItem("VRB/Build Prefabs")]
        public static void BuildPrefabs()
        {
            EnsureFolder("Assets/VRB", "Prefabs");
            EnsureFolder("Assets/VRB", "Materials");
            BuildPinPrefab();
            BuildBallPrefab();
            AssetDatabase.SaveAssets();
        }

        static void BuildPinPrefab()
        {
            var pin = new GameObject("Pin");
            var body = pin.AddComponent<Rigidbody>();
            pin.AddComponent<Pin>();
            body.mass = BowlingDimensions.PinMass;
            body.centerOfMass = new Vector3(0f, 0.13f, 0f);
            body.angularDamping = 0.25f; // lying pins wobble in place; damping makes them settle
            body.linearDamping = 0f;
            body.sleepThreshold = 0.05f;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;

            var material = GetOrCreatePhysicsMaterial("Pin", 0.35f, 0.3f, 0.15f);

            // Wide lower body and narrow neck/head, simple primitives instead of a convex mesh.
            var lower = pin.AddComponent<CapsuleCollider>();
            lower.direction = 1;
            lower.radius = 0.056f;
            lower.height = 0.25f;
            lower.center = new Vector3(0f, 0.13f, 0f);
            lower.sharedMaterial = material;

            var upper = pin.AddComponent<CapsuleCollider>();
            upper.direction = 1;
            upper.radius = 0.027f;
            upper.height = 0.2f;
            upper.center = new Vector3(0f, BowlingDimensions.PinHeight - 0.1f, 0f);
            upper.sharedMaterial = material;

            AddVisual(pin.transform, "Bowling_pin001", FbxPinWidth, FbxPinHeight,
                BowlingDimensions.PinMaxDiameter, BowlingDimensions.PinHeight);

            PrefabUtility.SaveAsPrefabAsset(pin, PinPrefabPath);
            Object.DestroyImmediate(pin);
        }

        static void BuildBallPrefab()
        {
            var ball = new GameObject("Ball");
            var body = ball.AddComponent<Rigidbody>();
            body.mass = BowlingDimensions.BallMass;
            body.angularDamping = 0.05f;
            body.linearDamping = 0f;
            // A ball rolling at 10 m/s spins at ~90 rad/s; the default cap of 7 rad/s would make it skid forever.
            body.maxAngularVelocity = 150f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var sphere = ball.AddComponent<SphereCollider>();
            sphere.radius = BowlingDimensions.BallDiameter / 2f;
            sphere.sharedMaterial = GetOrCreatePhysicsMaterial("Ball", 0.3f, 0.2f, 0.05f);

            AddVisual(ball.transform, "Bowling_ball", FbxBallDiameter, FbxBallDiameter,
                BowlingDimensions.BallDiameter, BowlingDimensions.BallDiameter);
            ball.transform.Find("Visual").localPosition = new Vector3(0f, 0f, 0f);

            PrefabUtility.SaveAsPrefabAsset(ball, BallPrefabPath);
            Object.DestroyImmediate(ball);
        }

        /// <summary>Copies a mesh and its materials from the FBX and scales it to real-world size.</summary>
        static void AddVisual(Transform parent, string fbxNodeName, float fbxWidth, float fbxHeight,
            float targetWidth, float targetHeight)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(BowlingClubFbx);
            var node = fbx.transform.Find(fbxNodeName);
            var sourceFilter = node.GetComponent<MeshFilter>();
            var sourceRenderer = node.GetComponent<MeshRenderer>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(parent, false);
            visual.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = sourceRenderer.sharedMaterials;

            // The mesh's long axis is local Z; rotating -90 degrees about X stands it up along world Y.
            visual.transform.localRotation = Quaternion.Euler(270f, 0f, 0f);
            var horizontal = targetWidth / fbxWidth;
            var vertical = targetHeight / fbxHeight;
            visual.transform.localScale = new Vector3(horizontal, horizontal, vertical);

            // The mesh is centered on its pivot, so lift pins by half their height so the base sits at y = 0.
            var isPin = fbxNodeName.Contains("pin");
            visual.transform.localPosition = isPin ? new Vector3(0f, targetHeight / 2f, 0f) : Vector3.zero;
        }

        // ---------------------------------------------------------------- environment

        [MenuItem("VRB/Build Environment")]
        public static void BuildEnvironment()
        {
            EnsureFolder("Assets/VRB", "Materials");
            DestroyIfExists("Lane");
            DestroyIfExists("bowling club");

            var root = new GameObject("Lane");
            var wood = GetOrCreateMaterial("LaneWood", new Color(0.86f, 0.66f, 0.40f), 0.55f);
            var approach = GetOrCreateMaterial("ApproachWood", new Color(0.62f, 0.45f, 0.28f), 0.4f);
            var gutter = GetOrCreateMaterial("Gutter", new Color(0.12f, 0.12f, 0.14f), 0.3f);
            var side = GetOrCreateMaterial("SideDeck", new Color(0.30f, 0.20f, 0.14f), 0.3f);
            var wall = GetOrCreateMaterial("HallWall", new Color(0.18f, 0.20f, 0.30f), 0.1f);
            var ceiling = GetOrCreateMaterial("HallCeiling", new Color(0.10f, 0.10f, 0.13f), 0.05f);
            var pit = GetOrCreateMaterial("Pit", new Color(0.03f, 0.03f, 0.04f), 0.05f);
            var line = GetOrCreateMaterial("FoulLine", new Color(0.9f, 0.1f, 0.1f), 0.2f);

            var laneFriction = GetOrCreatePhysicsMaterial("Lane", 0.3f, 0.2f, 0.05f);
            var gutterFriction = GetOrCreatePhysicsMaterial("Gutter", 0.5f, 0.4f, 0.05f);
            var solidFriction = GetOrCreatePhysicsMaterial("Solid", 0.5f, 0.4f, 0.1f);

            var halfLane = BowlingDimensions.LaneWidth / 2f;
            var gutterCenter = halfLane + BowlingDimensions.GutterWidth / 2f;
            var sideInner = halfLane + BowlingDimensions.GutterWidth;
            var sideWidth = HallHalfWidth - sideInner;
            var sideCenter = sideInner + sideWidth / 2f;

            // Floors (top surface at the given y)
            Box(root, "Approach", approach, solidFriction, 0f, 0f, HallHalfWidth * 2f, HallBackZ, 0f);
            Box(root, "LaneSurface", wood, laneFriction, 0f, 0f, BowlingDimensions.LaneWidth, 0f, LaneEndZ);
            Box(root, "GutterLeft", gutter, gutterFriction, -gutterCenter, GutterTopY,
                BowlingDimensions.GutterWidth, 0f, LaneEndZ);
            Box(root, "GutterRight", gutter, gutterFriction, gutterCenter, GutterTopY,
                BowlingDimensions.GutterWidth, 0f, LaneEndZ);
            Box(root, "SideDeckLeft", side, solidFriction, -sideCenter, 0f, sideWidth, 0f, LaneEndZ);
            Box(root, "SideDeckRight", side, solidFriction, sideCenter, 0f, sideWidth, 0f, LaneEndZ);

            // Pit and pin curtain
            Box(root, "PitFloor", pit, solidFriction, 0f, PitFloorY, HallHalfWidth * 2f, LaneEndZ, PitEndZ);
            Wall(root, "PinCurtain", pit, solidFriction, 0f, PitEndZ + 0.1f, HallHalfWidth * 2f, 0.2f, 1.6f, PitFloorY);

            // Hall shell: side walls, back wall behind the player, front wall behind the pin curtain, ceiling
            var shellFrontZ = PitEndZ + 0.5f;
            var shellLength = shellFrontZ - HallBackZ;
            var shellCenterZ = (shellFrontZ + HallBackZ) / 2f;
            var shellHeight = HallCeilingY - PitFloorY;
            Wall(root, "WallLeft", wall, solidFriction, -HallHalfWidth - 0.1f, shellCenterZ,
                0.2f, shellLength, shellHeight, PitFloorY);
            Wall(root, "WallRight", wall, solidFriction, HallHalfWidth + 0.1f, shellCenterZ,
                0.2f, shellLength, shellHeight, PitFloorY);
            Wall(root, "WallBack", wall, solidFriction, 0f, HallBackZ - 0.1f,
                HallHalfWidth * 2f + 0.4f, 0.2f, shellHeight, PitFloorY);
            Wall(root, "WallFront", wall, solidFriction, 0f, shellFrontZ + 0.1f,
                HallHalfWidth * 2f + 0.4f, 0.2f, shellHeight, PitFloorY);
            Wall(root, "Ceiling", ceiling, solidFriction, 0f, shellCenterZ,
                HallHalfWidth * 2f + 0.4f, shellLength + 0.4f, 0.2f, HallCeilingY);

            // Foul line: visual only, drawn just above the surface
            var foul = Box(root, "FoulLine", line, null, 0f, 0.003f,
                BowlingDimensions.LaneWidth + 2f * BowlingDimensions.GutterWidth, -0.02f, 0.02f);
            Object.DestroyImmediate(foul.GetComponent<Collider>());

            // Pit trigger: pins that end up here count as fallen
            var trigger = new GameObject("PitTrigger");
            trigger.transform.SetParent(root.transform, false);
            trigger.transform.localPosition = new Vector3(0f, (PitFloorY + 0.3f) / 2f,
                (LaneEndZ + PitEndZ) / 2f);
            var triggerBox = trigger.AddComponent<BoxCollider>();
            triggerBox.isTrigger = true;
            triggerBox.size = new Vector3(HallHalfWidth * 2f, 0.3f - PitFloorY, BowlingDimensions.PitLength);
            trigger.AddComponent<PitZone>();

            // Foul line sensor volume: the hand must stay behind z = 0
            var foulZone = new GameObject("FoulZone");
            foulZone.transform.SetParent(root.transform, false);
            foulZone.transform.localPosition = new Vector3(0f, 1.5f, 1.0f);
            var foulBox = foulZone.AddComponent<BoxCollider>();
            foulBox.isTrigger = true;
            foulBox.size = new Vector3(HallHalfWidth * 2f, 3f, 2f);

            MarkStaticRecursively(root);
            EditorSceneManagerUtility.MarkDirty(root.scene);
        }

        // ---------------------------------------------------------------- pins and ball stand

        [MenuItem("VRB/Build Pins")]
        public static void BuildPins()
        {
            DestroyIfExists("Pins");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PinPrefabPath);
            var root = new GameObject("Pins");
            root.transform.position = new Vector3(0f, 0f, BowlingDimensions.LaneLength);
            var rack = root.AddComponent<PinRack>();
            var rackConfig = new SerializedObject(rack);
            rackConfig.FindProperty("m_Config").objectReferenceValue = GetOrCreateConfig();
            rackConfig.ApplyModifiedPropertiesWithoutUndo();

            for (var i = 0; i < PinLayout.PinCount; i++)
            {
                var (x, z) = PinLayout.GetPosition(i);
                var pin = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                pin.name = $"Pin_{i}";
                pin.transform.localPosition = new Vector3(x, 0.001f, z);
                pin.transform.localRotation = Quaternion.identity;
            }

            EditorSceneManagerUtility.MarkDirty(root.scene);
        }

        [MenuItem("VRB/Build Ball Stand")]
        public static void BuildBallStand()
        {
            DestroyIfExists("BallStand");
            DestroyIfExists("Ball");

            var stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.name = "BallStand";
            stand.transform.position = new Vector3(0.55f, 0.4f, -1.2f);
            stand.transform.localScale = new Vector3(0.28f, 0.4f, 0.28f); // cylinder is 2 units tall
            stand.GetComponent<MeshRenderer>().sharedMaterial =
                GetOrCreateMaterial("BallStand", new Color(0.25f, 0.25f, 0.28f), 0.4f);
            stand.isStatic = true;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath);
            var ball = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            ball.name = "Ball";
            ball.transform.position = new Vector3(0.55f, 0.8f + BowlingDimensions.BallDiameter / 2f + 0.002f, -1.2f);

            EditorSceneManagerUtility.MarkDirty(stand.scene);
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>A slab spanning [zStart, zEnd] in Z whose top surface is at topY (0.3 thick).</summary>
        static GameObject Box(GameObject parent, string name, Material material, PhysicsMaterial physics,
            float centerX, float topY, float width, float zStart, float zEnd)
        {
            const float thickness = 0.3f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = new Vector3(centerX, topY - thickness / 2f, (zStart + zEnd) / 2f);
            go.transform.localScale = new Vector3(width, thickness, zEnd - zStart);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (physics != null) go.GetComponent<BoxCollider>().sharedMaterial = physics;
            return go;
        }

        /// <summary>A box with explicit size: width (X), depth (Z) and height (Y), resting on baseY.</summary>
        static GameObject Wall(GameObject parent, string name, Material material, PhysicsMaterial physics,
            float centerX, float centerZ, float width, float depth, float height, float baseY)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = new Vector3(centerX, baseY + height / 2f, centerZ);
            go.transform.localScale = new Vector3(width, height, depth);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.GetComponent<BoxCollider>().sharedMaterial = physics;
            return go;
        }

        static void MarkStaticRecursively(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI |
                    StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        static void DestroyIfExists(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        static BowlingConfig GetOrCreateConfig()
        {
            EnsureFolder("Assets/VRB", "Config");
            const string path = "Assets/VRB/Config/BowlingConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<BowlingConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BowlingConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            return config;
        }

        static Material GetOrCreateMaterial(string name, Color color, float smoothness)
        {
            var path = $"{MaterialDir}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static PhysicsMaterial GetOrCreatePhysicsMaterial(string name, float staticFriction,
            float dynamicFriction, float bounciness)
        {
            var path = $"{MaterialDir}/{name}.physicMaterial";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material == null)
            {
                material = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(material, path);
            }

            material.staticFriction = staticFriction;
            material.dynamicFriction = dynamicFriction;
            material.bounciness = bounciness;
            material.frictionCombine = PhysicsMaterialCombine.Average;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);
            return material;
        }
    }

    static class EditorSceneManagerUtility
    {
        public static void MarkDirty(Scene scene)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
