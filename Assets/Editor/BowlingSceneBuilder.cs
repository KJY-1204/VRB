using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
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
        const string HallPrefabPath = PrefabDir + "/BowlingHall.prefab";
        const string HallMeshPath = PrefabDir + "/BowlingHallPlane.asset";
        const string RollClipPath = "Assets/VRB/Bowling ball roll.wav";
        const string HitClipPath = "Assets/VRB/Bowling pin hit.wav";
        const string StrikeClipPath = "Assets/VRB/bowlingstrike.wav";

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
            BuildUI();
            BuildGameController();
            ConfigureRig();
            BuildSeat();
            BuildMirror();
            BuildNetwork();
            ConfigureLighting();
        }

        // ---------------------------------------------------------------- prefabs

        [MenuItem("VRB/Build Prefabs")]
        public static void BuildPrefabs()
        {
            EnsureFolder("Assets/VRB", "Prefabs");
            EnsureFolder("Assets/VRB", "Materials");
            ConfigureAudioImports();
            BuildPinPrefab();
            BuildBallPrefab();
            AssetDatabase.SaveAssets();
        }

        const string PinBodyMeshPath = PrefabDir + "/PinBody.asset";

        // Silhouette of a regulation pin from its flat base up to where the neck begins: height and radius in inches.
        // It is wide at 4.5 in (4.766 in across) and stands on a base 2 in across, so a pin rocks upright again after a
        // small tilt (about 11 degrees) instead of toppling like a ball-bottomed capsule would.
        static readonly float[,] PinBodyProfileInches =
        {
            { 0.00f, 1.00f }, { 0.15f, 1.13f }, { 1.00f, 1.60f }, { 2.00f, 2.05f }, { 3.00f, 2.30f },
            { 4.50f, 2.383f }, { 6.00f, 2.25f }, { 7.00f, 1.95f }, { 8.00f, 1.55f }, { 8.70f, 1.25f },
        };

        /// <summary>A solid of revolution of <see cref="PinBodyProfileInches"/>, used as a convex collider.</summary>
        static Mesh BuildPinBodyMesh()
        {
            const int segments = 20;
            const float inch = 0.0254f;
            var rings = PinBodyProfileInches.GetLength(0);
            var vertices = new List<Vector3>();
            for (var ring = 0; ring < rings; ring++)
            {
                var y = PinBodyProfileInches[ring, 0] * inch;
                var radius = PinBodyProfileInches[ring, 1] * inch;
                for (var i = 0; i < segments; i++)
                {
                    var angle = i * Mathf.PI * 2f / segments;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
                }
            }

            var bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0f, PinBodyProfileInches[0, 0] * inch, 0f));
            var topCenter = vertices.Count;
            vertices.Add(new Vector3(0f, PinBodyProfileInches[rings - 1, 0] * inch, 0f));

            var triangles = new List<int>();
            for (var ring = 0; ring < rings - 1; ring++)
            {
                for (var i = 0; i < segments; i++)
                {
                    var next = (i + 1) % segments;
                    int a = ring * segments + i, b = ring * segments + next;
                    int c = (ring + 1) * segments + i, d = (ring + 1) * segments + next;
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }
            }
            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                triangles.AddRange(new[] { bottomCenter, i, next });
                triangles.AddRange(new[] { topCenter, (rings - 1) * segments + next, (rings - 1) * segments + i });
            }

            var mesh = new Mesh { name = "PinBody" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Overwrite in place so the pin prefab keeps its reference to this asset.
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(PinBodyMeshPath);
            if (saved != null)
            {
                EditorUtility.CopySerialized(mesh, saved);
                Object.DestroyImmediate(mesh);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, PinBodyMeshPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(PinBodyMeshPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Mesh>(PinBodyMeshPath);
        }

        static void BuildPinPrefab()
        {
            var pin = new GameObject("Pin");
            var body = pin.AddComponent<Rigidbody>();
            pin.AddComponent<Pin>();
            body.mass = BowlingDimensions.PinMass;
            body.centerOfMass = new Vector3(0f, BowlingDimensions.PinCenterOfMassHeight, 0f);
            body.angularDamping = 0.1f; // lying pins wobble in place; Pin adds damping while they lie down
            body.linearDamping = 0f;
            body.sleepThreshold = 0.05f;
            body.solverIterations = 16; // pins lean on each other, so contacts need a few more passes to stay stable
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;

            var material = GetOrCreatePhysicsMaterial("Pin", 0.40f, 0.30f, 0.50f, PhysicsMaterialCombine.Average);

            // The belly is the real silhouette with a flat base; the slim neck and the head are two capsules.
            var belly = pin.AddComponent<MeshCollider>();
            belly.convex = true;
            belly.sharedMesh = BuildPinBodyMesh();
            belly.sharedMaterial = material;

            var neck = pin.AddComponent<CapsuleCollider>();
            neck.direction = 1;
            neck.radius = 0.0235f;
            neck.height = 0.14f;
            neck.center = new Vector3(0f, 0.255f, 0f);
            neck.sharedMaterial = material;

            var head = pin.AddComponent<CapsuleCollider>();
            head.direction = 1;
            head.radius = 0.031f;
            head.height = 0.085f;
            head.center = new Vector3(0f, BowlingDimensions.PinHeight - 0.0425f, 0f);
            head.sharedMaterial = material;

            AddVisual(pin.transform, "Bowling_pin001", FbxPinWidth, FbxPinHeight,
                BowlingDimensions.PinMaxDiameter, BowlingDimensions.PinHeight);

            PrefabUtility.SaveAsPrefabAsset(pin, PinPrefabPath);
            Object.DestroyImmediate(pin);
        }

        static void BuildBallPrefab()
        {
            var ball = new GameObject("Ball");
            var body = ball.AddComponent<Rigidbody>();
            ball.AddComponent<Ball>();
            body.mass = BowlingDimensions.BallMass;
            body.angularDamping = 0.05f;
            body.linearDamping = 0f;
            // A ball rolling at 10 m/s spins at ~90 rad/s; the default cap of 7 rad/s would make it skid forever.
            body.maxAngularVelocity = 150f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var sphere = ball.AddComponent<SphereCollider>();
            sphere.radius = BowlingDimensions.BallDiameter / 2f;
            sphere.sharedMaterial = GetOrCreatePhysicsMaterial("Ball", 0.3f, 0.2f, 0.60f, PhysicsMaterialCombine.Average);

            AddVisual(ball.transform, "Bowling_ball", FbxBallDiameter, FbxBallDiameter,
                BowlingDimensions.BallDiameter, BowlingDimensions.BallDiameter);
            ball.transform.Find("Visual").localPosition = new Vector3(0f, 0f, 0f);

            var source = ball.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.minDistance = 1.5f;
            source.maxDistance = 40f;
            var audio = ball.AddComponent<BallAudio>();
            var audioFields = new SerializedObject(audio);
            audioFields.FindProperty("m_RollClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(RollClipPath);
            audioFields.ApplyModifiedPropertiesWithoutUndo();

            // Grabbing: the toolkit holds the ball, but BallThrower computes the release velocity itself.
            var grab = ball.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;
            ball.AddComponent<NearGrabFilter>();
            var thrower = ball.AddComponent<BallThrower>();
            var throwerFields = new SerializedObject(thrower);
            throwerFields.FindProperty("m_Config").objectReferenceValue = GetOrCreateConfig();
            throwerFields.ApplyModifiedPropertiesWithoutUndo();
            ball.AddComponent<BallRespawner>();

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

            var laneFriction = GetOrCreatePhysicsMaterial("Lane", 0.3f, 0.2f, 0.15f);
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

            BuildHallVisual(root);

            MarkStaticRecursively(root);
            EditorSceneManagerUtility.MarkDirty(root.scene);
        }

        const float FbxFoulLineX = -15.06f;
        const float FbxLaneCenterZ = -17.0f;
        const float FbxHeadPinX = 4.03f;
        const float FbxTvFaceX = 3.33f;

        /// <summary>Scales the hall along the lane so the FBX's head pin spot lands on the game's head pin.</summary>
        static float HallStretch => BowlingDimensions.LaneLength / (FbxHeadPinX - FbxFoulLineX);

        /// <summary>
        /// Saves the bowling club FBX as a prefab with all of its decoration. Only the loose pins and balls standing
        /// on the lane we play on are removed, because the game brings its own physical ones.
        /// </summary>
        static void BuildHallPrefab()
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(BowlingClubFbx);
            var hall = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            PrefabUtility.UnpackPrefabInstance(hall, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            hall.name = "BowlingHall";

            var removed = new List<GameObject>();
            foreach (var r in hall.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.name.StartsWith("Bowling_pin") && !r.name.StartsWith("Bowling_ball")) continue;
                if (Mathf.Abs(r.bounds.center.z - FbxLaneCenterZ) < 0.9f) removed.Add(r.gameObject);
            }
            foreach (var go in removed) Object.DestroyImmediate(go);

            OpenMaskingUnits(hall);

            PrefabUtility.SaveAsPrefabAsset(hall, HallPrefabPath);
            Object.DestroyImmediate(hall);
            // Without this the next instantiation can see the prefab's new mesh reference as missing.
            AssetDatabase.ImportAsset(HallPrefabPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        // The hall's four lanes, as the z of each lane's center in the FBX. Only the second one has its masking unit
        // raised and pins standing behind it; the other three are shut.
        static readonly float[] FbxLaneCentersZ = { -17.0f, -18.8f, -20.55f, -22.35f };
        const int FbxOpenLane = 1;
        const float FbxPinDeckStartX = 2.4f;
        const float FbxLaneHalfSpan = 0.9f;

        /// <summary>
        /// Gives the other lanes the same open masking unit as the second one. The lanes are baked into one mesh, so
        /// the pin-deck end of that lane (everything fully inside its span past the deck start) is copied over the
        /// same part of the other lanes. The loose pins standing in the open one are duplicated to every lane, one group
        /// per lane: <see cref="LaneSelector"/> hides the group on the lane being played, which has the game's own pins.
        /// </summary>
        static void OpenMaskingUnits(GameObject hall)
        {
            var plane = hall.transform.Find("Plane001");
            var filter = plane.GetComponent<MeshFilter>();
            var source = filter.sharedMesh;
            var vertices = new List<Vector3>(source.vertices);
            var normals = new List<Vector3>(source.normals);
            var uvs = new List<Vector2>(source.uv);
            var targets = new List<int>();
            for (var lane = 0; lane < FbxLaneCentersZ.Length; lane++)
                if (lane != FbxOpenLane) targets.Add(lane);

            bool InSpan(int[] indices, int first, float centerZ)
            {
                var minX = float.MaxValue;
                var minZ = float.MaxValue;
                var maxZ = float.MinValue;
                for (var k = 0; k < 3; k++)
                {
                    var p = hall.transform.InverseTransformPoint(plane.TransformPoint(vertices[indices[first + k]]));
                    minX = Mathf.Min(minX, p.x);
                    minZ = Mathf.Min(minZ, p.z);
                    maxZ = Mathf.Max(maxZ, p.z);
                }
                return minX >= FbxPinDeckStartX && minZ >= centerZ - FbxLaneHalfSpan && maxZ <= centerZ + FbxLaneHalfSpan;
            }

            var submeshes = new List<int[]>();
            var sourceVertexCount = vertices.Count;
            for (var s = 0; s < source.subMeshCount; s++)
            {
                var indices = source.GetIndices(s);
                var result = new List<int>();
                for (var t = 0; t < indices.Length; t += 3)
                {
                    var replaced = false;
                    foreach (var lane in targets)
                        if (InSpan(indices, t, FbxLaneCentersZ[lane])) replaced = true;
                    if (replaced) continue;
                    result.Add(indices[t]);
                    result.Add(indices[t + 1]);
                    result.Add(indices[t + 2]);
                }

                foreach (var lane in targets)
                {
                    var shift = plane.InverseTransformVector(
                        hall.transform.TransformVector(new Vector3(0f, 0f, FbxLaneCentersZ[lane] - FbxLaneCentersZ[FbxOpenLane])));
                    var copies = new Dictionary<int, int>();
                    for (var t = 0; t < indices.Length; t += 3)
                    {
                        if (!InSpan(indices, t, FbxLaneCentersZ[FbxOpenLane])) continue;
                        for (var k = 0; k < 3; k++)
                        {
                            var original = indices[t + k];
                            if (!copies.TryGetValue(original, out var copy))
                            {
                                copy = vertices.Count;
                                vertices.Add(vertices[original] + shift);
                                normals.Add(normals[original]);
                                uvs.Add(uvs[original]);
                                copies[original] = copy;
                            }
                            result.Add(copy);
                        }
                    }
                }
                submeshes.Add(result.ToArray());
            }

            var mesh = new Mesh { name = source.name + "_open", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = submeshes.Count;
            for (var s = 0; s < submeshes.Count; s++) mesh.SetIndices(submeshes[s], MeshTopology.Triangles, s);
            mesh.RecalculateBounds();
            // Overwrite in place when it exists: deleting it would break the references the hall prefab holds.
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(HallMeshPath);
            if (saved != null)
            {
                EditorUtility.CopySerialized(mesh, saved);
                Object.DestroyImmediate(mesh);
                mesh = saved;
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, HallMeshPath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(HallMeshPath, ImportAssetOptions.ForceSynchronousImport);
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(HallMeshPath);
            filter.sharedMesh = mesh;

            var openZ = FbxLaneCentersZ[FbxOpenLane];
            var pins = new List<Renderer>();
            foreach (var r in hall.GetComponentsInChildren<Renderer>())
                if (r.name.StartsWith("Bowling_pin") && Mathf.Abs(r.bounds.center.z - openZ) < FbxLaneHalfSpan) pins.Add(r);
            // One group of decorative pins per lane, so the one under the game's own pins can be hidden.
            for (var lane = 0; lane < FbxLaneCentersZ.Length; lane++)
            {
                var group = new GameObject("FbxPins_Lane" + (lane + 1));
                group.transform.SetParent(hall.transform, false);
                foreach (var pin in pins)
                {
                    if (lane == FbxOpenLane)
                    {
                        pin.transform.SetParent(group.transform, true);
                        continue;
                    }

                    var copy = Object.Instantiate(pin.gameObject, pin.transform.parent);
                    copy.transform.SetParent(group.transform, true);
                    copy.name = pin.name + "_lane" + (lane + 1);
                    copy.transform.position = pin.transform.position + new Vector3(0f, 0f, FbxLaneCentersZ[lane] - openZ);
                }
            }
        }

        /// <summary>
        /// Places the decorated hall around the physics lane. The FBX lanes run along its +X in meters, so the hall
        /// is turned onto +Z and shifted so that the 4th lane's foul line and center land on this lane's. The FBX pin
        /// head pin stands 19.1m past the foul line, 0.8m too far, so the hall is shortened along the lane to fit.
        /// The hand-built boxes stay as colliders; only their renderers are hidden.
        /// </summary>
        static void BuildHallVisual(GameObject lane)
        {
            BuildHallPrefab();

            var stretch = HallStretch;

            foreach (var r in lane.GetComponentsInChildren<MeshRenderer>())
                if (r.name != "Ceiling") r.enabled = false;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HallPrefabPath);
            var hall = (GameObject)PrefabUtility.InstantiatePrefab(prefab, lane.transform);
            hall.name = "Hall";
            hall.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            hall.transform.localScale = new Vector3(stretch, 1f, 1f);
            hall.transform.localPosition = new Vector3(FbxLaneCenterZ, 0f, -FbxFoulLineX * stretch);

            // Lets the player play on any of the hall's lanes by sliding the hall sideways.
            var offsets = new float[FbxLaneCentersZ.Length];
            for (var i = 0; i < offsets.Length; i++) offsets[i] = FbxLaneCenterZ - FbxLaneCentersZ[i];

            var selector = lane.AddComponent<LaneSelector>();
            var fields = new SerializedObject(selector);
            fields.FindProperty("m_Hall").objectReferenceValue = hall.transform;
            fields.FindProperty("m_BasePosition").vector3Value = hall.transform.localPosition;
            var offsetArray = fields.FindProperty("m_LaneOffsets");
            offsetArray.arraySize = offsets.Length;
            var pinArray = fields.FindProperty("m_DecorPins");
            pinArray.arraySize = offsets.Length;
            for (var i = 0; i < offsets.Length; i++)
            {
                offsetArray.GetArrayElementAtIndex(i).floatValue = offsets[i];
                pinArray.GetArrayElementAtIndex(i).objectReferenceValue = hall.transform.Find("FbxPins_Lane" + (i + 1)).gameObject;
            }
            fields.FindProperty("m_Seats").arraySize = offsets.Length;
            fields.ApplyModifiedPropertiesWithoutUndo();
            selector.Select(0);
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

            var sounds = root.AddComponent<PinSounds>();
            var soundFields = new SerializedObject(sounds);
            soundFields.FindProperty("m_Rack").objectReferenceValue = rack;
            soundFields.FindProperty("m_HitClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(HitClipPath);
            soundFields.FindProperty("m_StrikeClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(StrikeClipPath);
            soundFields.ApplyModifiedPropertiesWithoutUndo();

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


        // ---------------------------------------------------------------- UI and game wiring

        [MenuItem("VRB/Build UI")]
        public static void BuildUI()
        {
            EnsureUiSprites();
            DestroyIfExists("SelectionCanvas");
            DestroyIfExists("ScoreboardCanvas");
            DestroyIfExists("TvCanvas");
            DestroyIfExists("EventSystem");

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<XRUIInputModule>();

            BuildSelectionCanvas();
            BuildScoreboardCanvas();
            BuildTvCanvas();
            EditorSceneManagerUtility.MarkDirty(SceneManager.GetActiveScene());
        }

        const string UiDir = "Assets/VRB/UI";
        const string UiFillPath = UiDir + "/RoundedFill.png";
        const string UiRingPath = UiDir + "/RoundedRing.png";
        const int UiSpriteSize = 96;
        const float UiSpriteRadius = 36f;
        const float UiSpriteBorder = 40f;

        /// <summary>
        /// Writes the two 9-slice sprites every panel is made of: a filled rounded rectangle and a thin rounded outline.
        /// They are generated rather than imported, so the look can be tuned here, and the outline can stay hollow, which
        /// the see-through panels need (a solid edge behind them would tint the whole panel).
        /// </summary>
        static void EnsureUiSprites()
        {
            EnsureFolder("Assets/VRB", "UI");
            WriteRoundedSprite(UiFillPath, 0f);
            WriteRoundedSprite(UiRingPath, 4f);
        }

        static void WriteRoundedSprite(string path, float ringThickness)
        {
            var texture = new Texture2D(UiSpriteSize, UiSpriteSize, TextureFormat.RGBA32, false);
            var half = UiSpriteSize / 2f;
            var inner = new Vector2(half - UiSpriteRadius, half - UiSpriteRadius);
            for (var y = 0; y < UiSpriteSize; y++)
            {
                for (var x = 0; x < UiSpriteSize; x++)
                {
                    var q = new Vector2(Mathf.Abs(x + 0.5f - half), Mathf.Abs(y + 0.5f - half)) - inner;
                    var distance = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - UiSpriteRadius;
                    var alpha = Mathf.Clamp01(0.5f - distance);
                    if (ringThickness > 0f) alpha *= Mathf.Clamp01(distance + ringThickness + 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(UiSpriteBorder, UiSpriteBorder, UiSpriteBorder, UiSpriteBorder);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static Image MakeCard(Transform parent, string name, Color color, Vector2 position, Vector2 size, float radius = 28f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            RoundCorners(image, radius);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A thin outline that follows the rounded corners of a rectangle of the given size.</summary>
        static Image MakeRing(Transform parent, string name, Color color, Vector2 size, float radius)
        {
            var ring = MakeCard(parent, name, color, Vector2.zero, size, radius);
            ring.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiRingPath);
            return ring;
        }

        static void RoundCorners(Image image, float radius)
        {
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiFillPath);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = UiSpriteRadius / radius;
        }

        static GameObject CreateWorldCanvas(string name, Vector2 size, float scale, Vector3 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(TrackedDeviceGraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;

            // A see-through navy panel with a thin neon edge.
            MakeCard(go.transform, "Background", UiPalette.Panel, Vector2.zero, size, 36f);
            MakeRing(go.transform, "Edge", UiPalette.Edge, size, 36f);
            return go;
        }

        static TextMeshProUGUI MakeText(Transform parent, string name, string content, float fontSize,
            Vector2 position, Vector2 size, Color color, FontStyles style = FontStyles.Normal, float spacing = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.characterSpacing = spacing;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        static Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 size,
            float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            RoundCorners(image, Mathf.Min(size.y * 0.4f, 36f));
            image.color = UiPalette.Button;
            var radius = Mathf.Min(size.y * 0.4f, 36f);
            MakeRing(go.transform, "Ring", new Color(UiPalette.Edge.r, UiPalette.Edge.g, UiPalette.Edge.b, 0.7f), size, radius);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            button.colors = colors;

            var text = MakeText(go.transform, "Label", label, fontSize, Vector2.zero, size, Color.white, FontStyles.Bold, 2f);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        static void SetReference(Object owner, string field, Object value)
        {
            var so = new SerializedObject(owner);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildSelectionCanvas()
        {
            // 1000 x 900 units at 1.5 mm per unit = 1.5 m x 1.35 m, about 2.6 m in front of the player. Its top stays under
            // the hall's hanging light cove, whose underside is 2.12 m up over the lane.
            var canvas = CreateWorldCanvas("SelectionCanvas", new Vector2(1000f, 900f), 0.0015f, new Vector3(0f, 1.4f, 1.0f));
            var view = canvas.AddComponent<LevelSelectionView>();
            var root = canvas.transform;

            MakeText(root, "Title", "VR BOWLING", 84f, new Vector2(0f, 395f), new Vector2(900f, 110f), Color.white, FontStyles.Bold, 12f);
            MakeCard(root, "TitleBar", UiPalette.Accent, new Vector2(0f, 338f), new Vector2(260f, 8f), 4f);

            var laneButtons = new Button[FbxLaneCentersZ.Length];
            MakeText(root, "LaneLabel", "LANE", 38f, new Vector2(-395f, 262f), new Vector2(170f, 60f), UiPalette.Muted, FontStyles.Bold, 6f);
            for (var i = 0; i < laneButtons.Length; i++)
                laneButtons[i] = MakeButton(root, "Lane_" + (i + 1), (i + 1).ToString(),
                    new Vector2(-190f + i * 190f, 262f), new Vector2(160f, 90f), 52f);

            var playerButtons = new Button[Match.MaxPlayers];
            MakeText(root, "PlayersLabel", "PLAYERS", 38f, new Vector2(-385f, 152f), new Vector2(210f, 60f), UiPalette.Muted, FontStyles.Bold, 6f);
            for (var i = 0; i < playerButtons.Length; i++)
                playerButtons[i] = MakeButton(root, "Players_" + (i + 1), (i + 1).ToString(),
                    new Vector2(-190f + i * 190f, 152f), new Vector2(160f, 90f), 52f);

            MakeText(root, "LevelLabel", "THROW ASSIST", 38f, new Vector2(0f, 72f), new Vector2(700f, 60f), UiPalette.Muted, FontStyles.Bold, 6f);
            var labels = new[] { "Raw Physics", "Weak", "Medium", "Strong" };
            var positions = new[] { new Vector2(-230f, -8f), new Vector2(230f, -8f), new Vector2(-230f, -128f), new Vector2(230f, -128f) };
            var buttons = new Button[4];
            for (var i = 0; i < 4; i++)
                buttons[i] = MakeButton(root, "Level_" + labels[i].Replace(" ", ""), labels[i], positions[i], new Vector2(420f, 108f), 48f);

            var description = MakeText(root, "Description", string.Empty, 38f, new Vector2(0f, -245f), new Vector2(920f, 80f),
                UiPalette.Muted);
            var start = MakeButton(root, "StartButton", "START", new Vector2(0f, -350f), new Vector2(460f, 120f), 62f);
            start.targetGraphic.color = UiPalette.Go;
            start.transform.Find("Ring").GetComponent<Image>().color = new Color(0.55f, 1f, 0.75f, 0.9f);
            var online = MakeButton(root, "NetworkButton", "ONLINE", new Vector2(-370f, -350f), new Vector2(210f, 90f), 36f);

            var so = new SerializedObject(view);
            so.FindProperty("m_NetworkButton").objectReferenceValue = online;
            var array = so.FindProperty("m_LevelButtons");
            array.arraySize = 4;
            for (var i = 0; i < 4; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            var laneArray = so.FindProperty("m_LaneButtons");
            laneArray.arraySize = laneButtons.Length;
            for (var i = 0; i < laneButtons.Length; i++) laneArray.GetArrayElementAtIndex(i).objectReferenceValue = laneButtons[i];
            var playerArray = so.FindProperty("m_PlayerButtons");
            playerArray.arraySize = playerButtons.Length;
            for (var i = 0; i < playerButtons.Length; i++) playerArray.GetArrayElementAtIndex(i).objectReferenceValue = playerButtons[i];
            so.FindProperty("m_StartButton").objectReferenceValue = start;
            so.FindProperty("m_DescriptionText").objectReferenceValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildScoreboardCanvas()
        {
            // 1500 x 860 units at 0.9 mm per unit = 1.35 m x 0.77 m, about 0.85 m to the player's left, hung just off the
            // hall's left wall, whose face is 0.96 m from the lane center at this height. Turn the head left to read it.
            // It is only turned 8 degrees toward the player: turning it fully would swing one end into the wall. Picking
            // another lane slides the hall to the left, so the wall is only ever farther from it. Low enough not to
            // overlap the mirror screen above.
            var position = new Vector3(-0.84f, 1.05f, -0.9f);
            var canvas = CreateWorldCanvas("ScoreboardCanvas", new Vector2(1500f, 860f), 0.0009f, position);
            canvas.transform.rotation = Quaternion.Euler(0f, 270f + 8f, 0f);
            var view = canvas.AddComponent<ScoreboardView>();
            var root = canvas.transform;

            MakeCard(root, "StatusPill", UiPalette.Card, new Vector2(0f, 375f), new Vector2(1000f, 100f), 48f);
            var status = MakeText(root, "Status", string.Empty, 64f, new Vector2(0f, 375f), new Vector2(960f, 90f),
                UiPalette.Amber, FontStyles.Bold, 2f);

            // One row per player (up to four; unused rows are hidden at run time): name, ten frame cells (number, marks of
            // each ball, running score; the tenth is wider since it can hold three balls) and the total.
            const float rowPitch = 165f, firstRowY = 250f, cellHeight = 150f;
            const float cellWidth = 100f, lastCellWidth = 150f, gap = 8f;
            var rows = new ScoreboardView.RowView[Match.MaxPlayers];
            for (var p = 0; p < rows.Length; p++)
            {
                var y = firstRowY - p * rowPitch;
                var rowRoot = new GameObject("Row" + (p + 1), typeof(RectTransform));
                rowRoot.transform.SetParent(root, false);
                var rowRect = rowRoot.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(1500f, 860f);
                var row = new ScoreboardView.RowView { Root = rowRoot };

                row.Highlight = MakeCard(rowRoot.transform, "Highlight", new Color(1f, 1f, 1f, 0f), new Vector2(0f, y),
                    new Vector2(1490f, cellHeight + 12f), 30f);
                row.Name = MakeText(rowRoot.transform, "Name", "P" + (p + 1), 50f, new Vector2(-690f, y),
                    new Vector2(110f, 80f), Color.white, FontStyles.Bold, 2f);

                var left = -620f;
                for (var i = 0; i < Scorecard.FrameCount; i++)
                {
                    var width = i == Scorecard.FrameCount - 1 ? lastCellWidth : cellWidth;
                    var x = left + width / 2f;
                    left += width + gap;

                    row.Cards[i] = MakeCard(rowRoot.transform, "Frame" + (i + 1) + "Card", UiPalette.Card, new Vector2(x, y),
                        new Vector2(width, cellHeight), 20f);
                    MakeText(rowRoot.transform, "Frame" + (i + 1) + "Number", (i + 1).ToString(), 24f,
                        new Vector2(x, y + 56f), new Vector2(width, 30f), UiPalette.Muted, FontStyles.Bold, 2f);
                    row.Marks[i] = MakeText(rowRoot.transform, "Frame" + (i + 1) + "Marks", string.Empty, 38f,
                        new Vector2(x, y + 18f), new Vector2(width, 50f), Color.white, FontStyles.Bold, 3f);
                    row.Scores[i] = MakeText(rowRoot.transform, "Frame" + (i + 1) + "Score", string.Empty, 52f,
                        new Vector2(x, y - 38f), new Vector2(width, 66f), UiPalette.Accent, FontStyles.Bold);
                }

                MakeCard(rowRoot.transform, "TotalCard", UiPalette.CardHighlight, new Vector2(left + 95f, y),
                    new Vector2(190f, cellHeight), 28f);
                row.Total = MakeText(rowRoot.transform, "Total", "0", 84f, new Vector2(left + 95f, y),
                    new Vector2(190f, 120f), Color.white, FontStyles.Bold);
                rows[p] = row;
            }

            var restart = MakeButton(root, "RestartButton", "RESTART", new Vector2(0f, -378f), new Vector2(360f, 90f), 48f);

            var so = new SerializedObject(view);
            so.FindProperty("m_StatusText").objectReferenceValue = status;
            var rowArray = so.FindProperty("m_Rows");
            rowArray.arraySize = rows.Length;
            for (var p = 0; p < rows.Length; p++)
            {
                var element = rowArray.GetArrayElementAtIndex(p);
                element.FindPropertyRelative("Root").objectReferenceValue = rows[p].Root;
                element.FindPropertyRelative("Highlight").objectReferenceValue = rows[p].Highlight;
                element.FindPropertyRelative("Name").objectReferenceValue = rows[p].Name;
                element.FindPropertyRelative("Total").objectReferenceValue = rows[p].Total;
                var marksArray = element.FindPropertyRelative("Marks");
                var scoresArray = element.FindPropertyRelative("Scores");
                var cardsArray = element.FindPropertyRelative("Cards");
                marksArray.arraySize = scoresArray.arraySize = cardsArray.arraySize = Scorecard.FrameCount;
                for (var i = 0; i < Scorecard.FrameCount; i++)
                {
                    marksArray.GetArrayElementAtIndex(i).objectReferenceValue = rows[p].Marks[i];
                    scoresArray.GetArrayElementAtIndex(i).objectReferenceValue = rows[p].Scores[i];
                    cardsArray.GetArrayElementAtIndex(i).objectReferenceValue = rows[p].Cards[i];
                }
            }
            so.FindProperty("m_RestartButton").objectReferenceValue = restart;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Puts the score on the hall's TV over our lane's pins. The screen is 0.93 x 0.6 m and sits about 19 m from the
        /// player, so only the big total is meant to be read from the throwing spot.
        /// </summary>
        static void BuildTvCanvas()
        {
            var z = (FbxTvFaceX - FbxFoulLineX) * HallStretch - 0.02f;
            var canvas = CreateWorldCanvas("TvCanvas", new Vector2(930f, 600f), 0.001f, new Vector3(0f, 1.3f, z));
            var view = canvas.AddComponent<TvScoreView>();
            var root = canvas.transform;

            MakeCard(root, "StatusPill", UiPalette.Card, new Vector2(0f, -235f), new Vector2(840f, 120f), 56f);
            var label = MakeText(root, "TotalLabel", "TOTAL", 70f, new Vector2(0f, 235f), new Vector2(800f, 90f),
                UiPalette.Accent, FontStyles.Bold, 14f);
            var total = MakeText(root, "Total", "0", 380f, new Vector2(0f, 10f), new Vector2(800f, 400f), Color.white, FontStyles.Bold);
            var status = MakeText(root, "Status", string.Empty, 80f, new Vector2(0f, -235f), new Vector2(820f, 110f),
                UiPalette.Amber, FontStyles.Bold, 2f);
            Object.DestroyImmediate(canvas.GetComponent<TrackedDeviceGraphicRaycaster>()); // display only

            var so = new SerializedObject(view);
            so.FindProperty("m_LabelText").objectReferenceValue = label;
            so.FindProperty("m_TotalText").objectReferenceValue = total;
            so.FindProperty("m_StatusText").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("VRB/Build Game Controller")]
        public static void BuildGameController()
        {
            DestroyIfExists("Game");

            var game = new GameObject("Game");
            game.SetActive(false); // wire references first, then enable so Awake and OnEnable see them
            var controller = game.AddComponent<GameController>();
            var ball = GameObject.Find("Ball");

            SetReference(controller, "m_Rack", GameObject.Find("Pins").GetComponent<PinRack>());
            SetReference(controller, "m_Thrower", ball.GetComponent<BallThrower>());
            SetReference(controller, "m_Respawner", ball.GetComponent<BallRespawner>());
            SetReference(controller, "m_GrabFilter", ball.GetComponent<NearGrabFilter>());
            SetReference(controller, "m_SelectionView", GameObject.Find("SelectionCanvas").GetComponent<LevelSelectionView>());
            SetReference(controller, "m_Scoreboard", GameObject.Find("ScoreboardCanvas").GetComponent<ScoreboardView>());
            SetReference(controller, "m_Tv", GameObject.Find("TvCanvas").GetComponent<TvScoreView>());
            SetReference(controller, "m_Lanes", GameObject.Find("Lane").GetComponent<LaneSelector>());

            game.SetActive(true);
            EditorSceneManagerUtility.MarkDirty(game.scene);
        }

        /// <summary>
        /// Quest-friendly lighting: one directional light with no realtime shadows (the hall is enclosed, so it simply
        /// brightens the interior) plus a flat ambient fill. The URP asset already has additional lights disabled.
        /// </summary>
        [MenuItem("VRB/Configure Lighting")]
        public static void ConfigureLighting()
        {
            var lightObject = GameObject.Find("Directional Light");
            if (lightObject != null)
            {
                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = LightShadows.None;
                light.intensity = 1.1f;
                light.color = new Color(1f, 0.96f, 0.90f);
                light.transform.rotation = Quaternion.Euler(60f, -20f, 0f);
                light.lightmapBakeType = LightmapBakeType.Realtime;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.40f, 0.41f, 0.47f);
            EditorSceneManagerUtility.MarkDirty(SceneManager.GetActiveScene());
        }

        /// <summary>The player stays at the foul line: turn off walking and teleporting so nobody wanders onto the lane.</summary>
        [MenuItem("VRB/Configure Rig")]
        public static void ConfigureRig()
        {
            var rig = GameObject.Find("XR Origin (XR Rig)");
            if (rig == null) return;

            rig.transform.SetPositionAndRotation(new Vector3(0f, 0f, -1.6f), Quaternion.identity);
            var locomotion = rig.transform.Find("Locomotion");
            if (locomotion != null) locomotion.gameObject.SetActive(false);
            EditorSceneManagerUtility.MarkDirty(rig.scene);
        }

        /// <summary>
        /// Makes one of the hall's chairs sit-able for each lane: the one that, once the hall has been slid to that
        /// lane, stands closest to just behind and beside the throwing spot. The FBX has no colliders, so a box is
        /// added for the ray to hit. Seats are children of the hall, so they slide with it.
        /// </summary>
        [MenuItem("VRB/Build Seat")]
        public static void BuildSeat()
        {
            DestroyIfExists("Seat");
            var rig = GameObject.Find("XR Origin (XR Rig)");
            var hall = GameObject.Find("Hall");
            var selector = GameObject.Find("Lane")?.GetComponent<LaneSelector>();
            if (rig == null || hall == null || selector == null) return;

            foreach (var old in hall.GetComponentsInChildren<SeatSpot>(true).ToArray())
                Object.DestroyImmediate(old.gameObject);

            selector.Select(0);
            var offsets = new SerializedObject(selector).FindProperty("m_LaneOffsets");
            var chairs = hall.GetComponentsInChildren<Renderer>()
                .Where(r => r.name.StartsWith("Object") && r.bounds.size.y > 0.3f && r.bounds.size.y < 0.5f
                    && r.bounds.size.x < 0.7f && r.bounds.size.z < 0.7f && r.bounds.size.x > 0.3f)
                .ToList();

            var head = rig.GetComponentInChildren<Camera>();
            var seats = new SeatSpot[offsets.arraySize];
            var wanted = new Vector2(1.5f, -2.7f);
            for (var lane = 0; lane < seats.Length; lane++)
            {
                var offset = offsets.GetArrayElementAtIndex(lane).floatValue;
                var chair = chairs
                    .Where(r => Mathf.Abs(r.bounds.center.x - offset) < 2.2f && r.bounds.center.z > -4.5f && r.bounds.center.z < -2.0f)
                    .OrderBy(r => Vector2.Distance(new Vector2(r.bounds.center.x - offset, r.bounds.center.z), wanted))
                    .FirstOrDefault();
                if (chair == null) continue;

                var seat = new GameObject("Seat_Lane" + (lane + 1));
                seat.transform.SetParent(hall.transform, true);
                seat.transform.position = new Vector3(chair.bounds.center.x, 0.25f, chair.bounds.center.z);
                seat.transform.rotation = Quaternion.identity;
                seat.AddComponent<BoxCollider>().size = new Vector3(0.7f, 0.5f, 0.7f);
                var spot = seat.AddComponent<SeatSpot>();
                var fields = new SerializedObject(spot);
                fields.FindProperty("m_Rig").objectReferenceValue = rig.transform;
                fields.FindProperty("m_Head").objectReferenceValue = head;
                fields.ApplyModifiedPropertiesWithoutUndo();
                seats[lane] = spot;
            }

            var selectorFields = new SerializedObject(selector);
            var seatArray = selectorFields.FindProperty("m_Seats");
            seatArray.arraySize = seats.Length;
            for (var i = 0; i < seats.Length; i++) seatArray.GetArrayElementAtIndex(i).objectReferenceValue = seats[i];
            selectorFields.ApplyModifiedPropertiesWithoutUndo();
            selector.Select(0);
            EditorSceneManagerUtility.MarkDirty(hall.scene);
        }

        const string MirrorTexturePath = MaterialDir + "/MirrorView.renderTexture";

        /// <summary>
        /// A small screen at the edge of the player's view showing the pins and the TV score together, filmed from above
        /// the lane about 7 m before the pins. At 19 m the TV and the pins are hard to read, and the board in front of
        /// the player can be hidden by the ball or the hand.
        /// </summary>
        [MenuItem("VRB/Build Mirror")]
        public static void BuildMirror()
        {
            DestroyIfExists("MirrorCamera");
            DestroyIfExists("MirrorPanel");
            var rig = GameObject.Find("XR Origin (XR Rig)");
            var game = GameObject.Find("Game");
            if (rig == null || game == null) return;

            EnsureFolder("Assets/VRB", "Materials");
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(MirrorTexturePath);
            if (texture == null)
            {
                texture = new RenderTexture(768, 432, 24) { name = "MirrorView", antiAliasing = 1 };
                AssetDatabase.CreateAsset(texture, MirrorTexturePath);
            }

            var cameraObject = new GameObject("MirrorCamera");
            var position = new Vector3(0f, 0.95f, BowlingDimensions.LaneLength - 6.8f);
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 0.85f, BowlingDimensions.LaneLength + 0.4f) - position);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 18f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
            camera.targetTexture = texture;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            var cameraData = cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.renderShadows = false;
            cameraData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;

            var view = cameraObject.AddComponent<MirrorView>();
            var viewFields = new SerializedObject(view);
            viewFields.FindProperty("m_Camera").objectReferenceValue = camera;
            viewFields.FindProperty("m_Game").objectReferenceValue = game.GetComponent<GameController>();
            viewFields.ApplyModifiedPropertiesWithoutUndo();

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/MirrorScreen.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, MaterialDir + "/MirrorScreen.mat");
            }
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);

            var panel = new GameObject("MirrorPanel");
            var follow = panel.AddComponent<PeripheralFollow>();
            var followFields = new SerializedObject(follow);
            followFields.FindProperty("m_Head").objectReferenceValue = rig.GetComponentInChildren<Camera>();
            followFields.ApplyModifiedPropertiesWithoutUndo();

            var frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frame.name = "Frame";
            frame.transform.SetParent(panel.transform, false);
            frame.transform.localScale = new Vector3(0.50f, 0.29f, 1f);
            frame.transform.localPosition = new Vector3(0f, 0f, 0.002f);
            frame.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMaterial("MirrorFrame", new Color(0.04f, 0.04f, 0.06f), 0.1f);
            Object.DestroyImmediate(frame.GetComponent<Collider>());

            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "Screen";
            screen.transform.SetParent(panel.transform, false);
            screen.transform.localScale = new Vector3(0.48f, 0.27f, 1f);
            screen.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(screen.GetComponent<Collider>());

            AssetDatabase.SaveAssets();
            EditorSceneManagerUtility.MarkDirty(panel.scene);
        }

        const string NetPlayerPrefabPath = PrefabDir + "/NetPlayer.prefab";

        /// <summary>
        /// What multiplayer needs in the scene: a "Network" object with the network manager, its transport and the session
        /// that starts and stops games, plus a "NetworkBridge" object that ties the game on this device to the other
        /// players. The player prefab it spawns for every connected device is built here too.
        /// </summary>
        [MenuItem("VRB/Build Network")]
        public static void BuildNetwork()
        {
            DestroyIfExists("Network");
            DestroyIfExists("NetworkBridge");
            DestroyIfExists("NetworkMenu");
            DestroyIfExists("NetworkMenuCanvas");
            var game = GameObject.Find("Game");
            var lane = GameObject.Find("Lane")?.GetComponent<LaneSelector>();
            if (game == null || lane == null) return;

            EnsureFolder("Assets/VRB", "Prefabs");
            var playerObject = new GameObject("NetPlayer");
            playerObject.AddComponent<Unity.Netcode.NetworkObject>();
            playerObject.AddComponent<NetPlayer>();
            var playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerObject, NetPlayerPrefabPath);
            Object.DestroyImmediate(playerObject);
            AssetDatabase.ImportAsset(NetPlayerPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetPlayerPrefabPath);

            var root = new GameObject("Network");
            var manager = root.AddComponent<Unity.Netcode.NetworkManager>();
            var transport = root.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = playerPrefab;
            manager.NetworkConfig.ConnectionApproval = true;

            var session = root.AddComponent<NetworkSession>();
            var sessionFields = new SerializedObject(session);
            sessionFields.FindProperty("m_PlayerPrefab").objectReferenceValue = playerPrefab;
            sessionFields.ApplyModifiedPropertiesWithoutUndo();

            // The network manager keeps itself alive across scene loads, so everything that points into this scene lives on
            // an object of its own that goes away with the scene.
            var bridge = new GameObject("NetworkBridge").AddComponent<NetworkBridge>();
            var bridgeFields = new SerializedObject(bridge);
            bridgeFields.FindProperty("m_Game").objectReferenceValue = game.GetComponent<GameController>();
            bridgeFields.FindProperty("m_Lanes").objectReferenceValue = lane;
            var selection = GameObject.Find("SelectionCanvas").GetComponent<LevelSelectionView>();
            bridgeFields.FindProperty("m_Selection").objectReferenceValue = selection;
            bridgeFields.ApplyModifiedPropertiesWithoutUndo();

            BuildNetworkMenu(session, selection);
            BuildRemoteLanes(lane, bridge.transform);

            // Sockets need the internet permission on the headset, even for a game on the local network.
            PlayerSettings.Android.forceInternetPermission = true;
            EditorSceneManagerUtility.MarkDirty(root.scene);
        }

        /// <summary>
        /// Stand-in pins and a ball for each of the hall's lanes (looks only, no physics) that show another player's lane
        /// live, plus the broadcaster that reports this device's own lane to the others.
        /// </summary>
        static void BuildRemoteLanes(LaneSelector lanes, Transform bridge)
        {
            DestroyIfExists("RemoteLanes");
            var pinVisual = AssetDatabase.LoadAssetAtPath<GameObject>(PinPrefabPath).transform.Find("Visual").gameObject;
            var ballVisual = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath).transform.Find("Visual").gameObject;

            var root = new GameObject("RemoteLanes");
            var remoteLanes = new RemoteLane[lanes.LaneCount];
            for (var i = 0; i < remoteLanes.Length; i++)
            {
                var laneObject = new GameObject("RemoteLane_" + (i + 1));
                laneObject.transform.SetParent(root.transform, false);
                var remote = laneObject.AddComponent<RemoteLane>();

                var bodies = new Transform[LaneSnapshot.BodyCount];
                for (var b = 0; b < bodies.Length; b++)
                {
                    var holder = new GameObject(b < LaneSnapshot.PinCount ? "Pin_" + b : "Ball").transform;
                    holder.SetParent(laneObject.transform, false);
                    var visual = Object.Instantiate(b < LaneSnapshot.PinCount ? pinVisual : ballVisual, holder);
                    visual.name = "Visual";
                    bodies[b] = holder;
                }

                var fields = new SerializedObject(remote);
                var array = fields.FindProperty("m_Bodies");
                array.arraySize = bodies.Length;
                for (var b = 0; b < bodies.Length; b++) array.GetArrayElementAtIndex(b).objectReferenceValue = bodies[b];
                fields.ApplyModifiedPropertiesWithoutUndo();

                remoteLanes[i] = remote;
                laneObject.SetActive(false);
            }

            var view = root.AddComponent<RemoteLanes>();
            var viewFields = new SerializedObject(view);
            viewFields.FindProperty("m_Lanes").objectReferenceValue = lanes;
            var lanesArray = viewFields.FindProperty("m_RemoteLanes");
            lanesArray.arraySize = remoteLanes.Length;
            for (var i = 0; i < remoteLanes.Length; i++) lanesArray.GetArrayElementAtIndex(i).objectReferenceValue = remoteLanes[i];
            viewFields.ApplyModifiedPropertiesWithoutUndo();

            var broadcaster = bridge.gameObject.AddComponent<LaneBroadcaster>();
            var broadcasterFields = new SerializedObject(broadcaster);
            broadcasterFields.FindProperty("m_Rack").objectReferenceValue = GameObject.Find("Pins").GetComponent<PinRack>();
            broadcasterFields.FindProperty("m_Ball").objectReferenceValue = GameObject.Find("Ball").transform;
            broadcasterFields.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The screen for hosting a game or joining one: three or four panels on a canvas that takes the start screen's
        /// place, and an always-active "NetworkMenu" object that runs them.
        /// </summary>
        static void BuildNetworkMenu(NetworkSession session, LevelSelectionView selection)
        {
            var canvas = CreateWorldCanvas("NetworkMenuCanvas", new Vector2(1000f, 900f), 0.0015f, new Vector3(0f, 1.4f, 1.0f));
            var root = canvas.transform;
            MakeText(root, "Title", "PLAY TOGETHER", 76f, new Vector2(0f, 395f), new Vector2(900f, 110f), Color.white, FontStyles.Bold, 8f);
            MakeCard(root, "TitleBar", UiPalette.Accent, new Vector2(0f, 338f), new Vector2(260f, 8f), 4f);
            var status = MakeText(root, "Status", string.Empty, 40f, new Vector2(0f, -395f), new Vector2(920f, 60f),
                UiPalette.Amber, FontStyles.Bold, 1f);

            GameObject Panel(string name)
            {
                var panel = new GameObject(name, typeof(RectTransform));
                panel.transform.SetParent(root, false);
                return panel;
            }

            // First screen: host or join.
            var offline = Panel("OfflinePanel");
            MakeText(offline.transform, "Hint", "Same Wi-Fi, up to four players.\nEveryone bowls on a lane of their own.", 42f,
                new Vector2(0f, 235f), new Vector2(900f, 140f), UiPalette.Muted);
            var host = MakeButton(offline.transform, "HostButton", "HOST A GAME", new Vector2(0f, 60f), new Vector2(520f, 120f), 56f);
            host.targetGraphic.color = UiPalette.Go;
            var join = MakeButton(offline.transform, "JoinButton", "JOIN A GAME", new Vector2(0f, -100f), new Vector2(520f, 120f), 56f);
            var close = MakeButton(offline.transform, "CloseButton", "BACK", new Vector2(0f, -270f), new Vector2(360f, 100f), 48f);

            // Hosting: read the address out to the others.
            var hosting = Panel("HostingPanel");
            MakeText(hosting.transform, "AddressLabel", "TELL THE OTHERS THIS ADDRESS", 38f, new Vector2(0f, 280f),
                new Vector2(900f, 60f), UiPalette.Muted, FontStyles.Bold, 4f);
            var address = MakeText(hosting.transform, "Address", string.Empty, 104f, new Vector2(0f, 160f), new Vector2(940f, 140f),
                Color.white, FontStyles.Bold);
            MakeText(hosting.transform, "Hint", "They press ONLINE, then JOIN A GAME, and type it.", 38f, new Vector2(0f, 55f),
                new Vector2(900f, 60f), UiPalette.Muted);
            var hostingDone = MakeButton(hosting.transform, "DoneButton", "DONE", new Vector2(0f, -120f), new Vector2(440f, 110f), 54f);
            hostingDone.targetGraphic.color = UiPalette.Go;
            var hostingLeave = MakeButton(hosting.transform, "LeaveButton", "CLOSE THE GAME", new Vector2(0f, -265f), new Vector2(440f, 100f), 40f);

            // Joining: type the address on a keypad.
            var joining = Panel("JoinPanel");
            MakeText(joining.transform, "EntryLabel", "HOST ADDRESS", 34f, new Vector2(0f, 300f), new Vector2(900f, 60f),
                UiPalette.Muted, FontStyles.Bold, 4f);
            var entry = MakeText(joining.transform, "Entry", string.Empty, 88f, new Vector2(0f, 220f), new Vector2(940f, 120f),
                Color.white, FontStyles.Bold);
            var keys = new Button[11];
            var columns = new[] { -300f, -140f, 20f };
            var rows = new[] { 120f, 10f, -100f, -210f };
            for (var i = 1; i <= 9; i++)
                keys[i] = MakeButton(joining.transform, "Key" + i, i.ToString(),
                    new Vector2(columns[(i - 1) % 3], rows[(i - 1) / 3]), new Vector2(140f, 95f), 52f);
            keys[10] = MakeButton(joining.transform, "KeyDot", ".", new Vector2(columns[0], rows[3]), new Vector2(140f, 95f), 60f);
            keys[0] = MakeButton(joining.transform, "Key0", "0", new Vector2(columns[1], rows[3]), new Vector2(140f, 95f), 52f);
            var delete = MakeButton(joining.transform, "DeleteButton", "DEL", new Vector2(columns[2], rows[3]), new Vector2(140f, 95f), 40f);
            var connect = MakeButton(joining.transform, "ConnectButton", "CONNECT", new Vector2(290f, 65f), new Vector2(270f, 210f), 44f);
            connect.targetGraphic.color = UiPalette.Go;
            var joinBack = MakeButton(joining.transform, "BackButton", "BACK", new Vector2(290f, -155f), new Vector2(270f, 100f), 44f);

            // Connected to someone else's game.
            var connected = Panel("ConnectedPanel");
            MakeText(connected.transform, "Title", "CONNECTED", 88f, new Vector2(0f, 230f), new Vector2(900f, 120f), Color.white, FontStyles.Bold, 6f);
            var lane = MakeText(connected.transform, "Lane", string.Empty, 60f, new Vector2(0f, 100f), new Vector2(900f, 90f),
                UiPalette.Accent, FontStyles.Bold);
            MakeText(connected.transform, "Hint", "Pick your throw assist and press START.", 38f, new Vector2(0f, 10f),
                new Vector2(900f, 60f), UiPalette.Muted);
            var connectedDone = MakeButton(connected.transform, "DoneButton", "DONE", new Vector2(0f, -120f), new Vector2(440f, 110f), 54f);
            connectedDone.targetGraphic.color = UiPalette.Go;
            var connectedLeave = MakeButton(connected.transform, "LeaveButton", "LEAVE THE GAME", new Vector2(0f, -265f), new Vector2(440f, 100f), 40f);

            var menu = new GameObject("NetworkMenu").AddComponent<NetworkMenuView>();
            var so = new SerializedObject(menu);
            so.FindProperty("m_Session").objectReferenceValue = session;
            so.FindProperty("m_Selection").objectReferenceValue = selection;
            so.FindProperty("m_Canvas").objectReferenceValue = canvas;
            so.FindProperty("m_OfflinePanel").objectReferenceValue = offline;
            so.FindProperty("m_HostingPanel").objectReferenceValue = hosting;
            so.FindProperty("m_JoinPanel").objectReferenceValue = joining;
            so.FindProperty("m_ConnectedPanel").objectReferenceValue = connected;
            so.FindProperty("m_StatusText").objectReferenceValue = status;
            so.FindProperty("m_AddressText").objectReferenceValue = address;
            so.FindProperty("m_EntryText").objectReferenceValue = entry;
            so.FindProperty("m_ConnectedText").objectReferenceValue = lane;
            so.FindProperty("m_HostButton").objectReferenceValue = host;
            so.FindProperty("m_JoinButton").objectReferenceValue = join;
            so.FindProperty("m_CloseButton").objectReferenceValue = close;
            so.FindProperty("m_DeleteButton").objectReferenceValue = delete;
            so.FindProperty("m_ConnectButton").objectReferenceValue = connect;
            so.FindProperty("m_JoinBackButton").objectReferenceValue = joinBack;

            void Fill(string field, params Button[] buttons)
            {
                var array = so.FindProperty(field);
                array.arraySize = buttons.Length;
                for (var i = 0; i < buttons.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            }

            Fill("m_DoneButtons", hostingDone, connectedDone);
            Fill("m_LeaveButtons", hostingLeave, connectedLeave);
            Fill("m_KeyButtons", keys);
            so.ApplyModifiedPropertiesWithoutUndo();
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

        /// <summary>Mono, compressed audio keeps memory low on Quest; 3D sounds do not need stereo.</summary>
        static void ConfigureAudioImports()
        {
            foreach (var path in new[] { RollClipPath, HitClipPath, StrikeClipPath })
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                if (importer == null) continue;

                importer.forceToMono = true;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.5f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
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

        /// <summary>
        /// Bounce between two surfaces uses the higher-priority mode of the two (Average, Minimum, Multiply, Maximum).
        /// Floors and walls use Minimum, so nothing bounces much off them; pins and the ball use Average, so they spring
        /// off each other the way the real hard-shelled ones do.
        /// </summary>
        static PhysicsMaterial GetOrCreatePhysicsMaterial(string name, float staticFriction,
            float dynamicFriction, float bounciness, PhysicsMaterialCombine bounceCombine = PhysicsMaterialCombine.Minimum)
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
            material.bounceCombine = bounceCombine;
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
