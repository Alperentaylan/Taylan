using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alperen.CityRoads
{
    public enum RoadPreset
    {
        Sokak,
        AnaCadde,
        CevreYolu,
        Rampa
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class RoadPath : MonoBehaviour
    {
        [Header("Yol Noktaları")]
        [SerializeField] private List<Vector3> controlPoints = new List<Vector3>();
        [SerializeField] private bool closedLoop;
        [SerializeField, Range(2, 40)] private int samplesPerSegment = 12;

        [Header("Yol Ayarları")]
        [SerializeField] private RoadPreset preset = RoadPreset.Sokak;
        [SerializeField, Min(2f)] private float roadWidth = 7f;
        [SerializeField, Min(1)] private int laneCount = 2;
        [SerializeField, Min(0.01f)] private float roadThickness = 0.18f;
        [SerializeField] private float surfaceOffset = 0.04f;

        [Header("Şerit Çizgileri")]
        [SerializeField] private bool createLaneMarkings = true;
        [SerializeField, Min(0.03f)] private float markingWidth = 0.13f;
        [SerializeField, Min(0.2f)] private float dashLength = 3.5f;
        [SerializeField, Min(0.2f)] private float dashGap = 3f;

        [Header("Kaldırım")]
        [SerializeField] private bool createSidewalks = true;
        [SerializeField, Min(0.2f)] private float sidewalkWidth = 1.8f;
        [SerializeField, Min(0.02f)] private float sidewalkHeight = 0.16f;

        [Header("Araziye Oturtma")]
        [SerializeField] private bool conformToTerrain;
        [SerializeField] private LayerMask terrainLayerMask = ~0;
        [SerializeField, Min(1f)] private float terrainRayHeight = 500f;
        [SerializeField] private float terrainYOffset = 0.03f;

        [Header("Materyaller")]
        [SerializeField] private Material asphaltMaterial;
        [SerializeField] private Material markingMaterial;
        [SerializeField] private Material sidewalkMaterial;

        private MeshFilter roadMeshFilter;
        private MeshRenderer roadMeshRenderer;
        private MeshCollider roadMeshCollider;

        private const string RoadMeshName = "MiniCity_RoadMesh";
        private const string SidewalkObjectName = "Sidewalks";
        private const string SidewalkMeshName = "MiniCity_SidewalkMesh";

        public List<Vector3> ControlPoints { get { return controlPoints; } }
        public bool ClosedLoop { get { return closedLoop; } set { closedLoop = value; } }
        public RoadPreset Preset { get { return preset; } }
        public Material AsphaltMaterial { get { return asphaltMaterial; } set { asphaltMaterial = value; } }
        public Material MarkingMaterial { get { return markingMaterial; } set { markingMaterial = value; } }
        public Material SidewalkMaterial { get { return sidewalkMaterial; } set { sidewalkMaterial = value; } }

        private void Reset()
        {
            CacheComponents();
            ApplyPreset(RoadPreset.Sokak);
        }

        private void OnEnable()
        {
            CacheComponents();

            if (!Application.isPlaying)
            {
                Rebuild();
            }
        }

        private void OnValidate()
        {
            roadWidth = Mathf.Max(2f, roadWidth);
            laneCount = Mathf.Max(1, laneCount);
            samplesPerSegment = Mathf.Clamp(samplesPerSegment, 2, 40);
            roadThickness = Mathf.Max(0.01f, roadThickness);
            sidewalkWidth = Mathf.Max(0.2f, sidewalkWidth);
            sidewalkHeight = Mathf.Max(0.02f, sidewalkHeight);
            markingWidth = Mathf.Max(0.03f, markingWidth);
            dashLength = Mathf.Max(0.2f, dashLength);
            dashGap = Mathf.Max(0.2f, dashGap);

            if (!Application.isPlaying)
            {
                Rebuild();
            }
        }

        public void ApplyPreset(RoadPreset newPreset)
        {
            preset = newPreset;

            switch (preset)
            {
                case RoadPreset.Sokak:
                    roadWidth = 7f;
                    laneCount = 2;
                    roadThickness = 0.18f;
                    createLaneMarkings = true;
                    createSidewalks = true;
                    sidewalkWidth = 1.8f;
                    sidewalkHeight = 0.16f;
                    dashLength = 3.5f;
                    dashGap = 3f;
                    break;

                case RoadPreset.AnaCadde:
                    roadWidth = 14f;
                    laneCount = 4;
                    roadThickness = 0.22f;
                    createLaneMarkings = true;
                    createSidewalks = true;
                    sidewalkWidth = 2.5f;
                    sidewalkHeight = 0.18f;
                    dashLength = 4f;
                    dashGap = 3f;
                    break;

                case RoadPreset.CevreYolu:
                    roadWidth = 21f;
                    laneCount = 6;
                    roadThickness = 0.28f;
                    createLaneMarkings = true;
                    createSidewalks = false;
                    sidewalkWidth = 0f;
                    sidewalkHeight = 0.18f;
                    dashLength = 5f;
                    dashGap = 4f;
                    break;

                case RoadPreset.Rampa:
                    roadWidth = 7.5f;
                    laneCount = 2;
                    roadThickness = 0.22f;
                    createLaneMarkings = true;
                    createSidewalks = false;
                    sidewalkWidth = 0f;
                    sidewalkHeight = 0.18f;
                    dashLength = 3.5f;
                    dashGap = 3f;
                    break;
            }

            Rebuild();
        }

        public void SetMaterials(Material asphalt, Material markings, Material sidewalks)
        {
            asphaltMaterial = asphalt;
            markingMaterial = markings;
            sidewalkMaterial = sidewalks;
            ApplyMaterials();
        }

        public void AddPointWorld(Vector3 worldPoint)
        {
            controlPoints.Add(transform.InverseTransformPoint(worldPoint));
            Rebuild();
        }

        public void RemoveLastPoint()
        {
            if (controlPoints.Count == 0)
            {
                return;
            }

            controlPoints.RemoveAt(controlPoints.Count - 1);
            Rebuild();
        }

        public void ReversePoints()
        {
            controlPoints.Reverse();
            Rebuild();
        }

        public void ClearPoints()
        {
            controlPoints.Clear();
            Rebuild();
        }

        public void Rebuild()
        {
            CacheComponents();

            if (controlPoints == null || controlPoints.Count < 2)
            {
                ClearGeneratedMeshes();
                return;
            }

            List<Vector3> centerLine = BuildCenterLine();

            if (centerLine.Count < 2)
            {
                ClearGeneratedMeshes();
                return;
            }

            if (conformToTerrain)
            {
                ConformCenterLineToTerrain(centerLine);
            }

            BuildRoadMesh(centerLine);
            BuildSidewalkMesh(centerLine);
            ApplyMaterials();
        }

        private void CacheComponents()
        {
            if (roadMeshFilter == null)
            {
                roadMeshFilter = GetComponent<MeshFilter>();
            }

            if (roadMeshRenderer == null)
            {
                roadMeshRenderer = GetComponent<MeshRenderer>();
            }

            if (roadMeshCollider == null)
            {
                roadMeshCollider = GetComponent<MeshCollider>();
            }
        }

        private void ApplyMaterials()
        {
            if (roadMeshRenderer != null)
            {
                roadMeshRenderer.sharedMaterials = new Material[]
                {
                    asphaltMaterial,
                    markingMaterial
                };
            }

            Transform sidewalkTransform = transform.Find(SidewalkObjectName);

            if (sidewalkTransform != null)
            {
                MeshRenderer renderer = sidewalkTransform.GetComponent<MeshRenderer>();

                if (renderer != null)
                {
                    renderer.sharedMaterial = sidewalkMaterial;
                }
            }
        }

        private List<Vector3> BuildCenterLine()
        {
            List<Vector3> result = new List<Vector3>();

            int pointCount = controlPoints.Count;
            int segmentCount = closedLoop ? pointCount : pointCount - 1;

            for (int segment = 0; segment < segmentCount; segment++)
            {
                Vector3 p0;
                Vector3 p1;
                Vector3 p2;
                Vector3 p3;

                if (closedLoop)
                {
                    p0 = controlPoints[Mod(segment - 1, pointCount)];
                    p1 = controlPoints[Mod(segment, pointCount)];
                    p2 = controlPoints[Mod(segment + 1, pointCount)];
                    p3 = controlPoints[Mod(segment + 2, pointCount)];
                }
                else
                {
                    p0 = controlPoints[Mathf.Max(segment - 1, 0)];
                    p1 = controlPoints[segment];
                    p2 = controlPoints[segment + 1];
                    p3 = controlPoints[Mathf.Min(segment + 2, pointCount - 1)];
                }

                for (int sample = 0; sample < samplesPerSegment; sample++)
                {
                    float t = sample / (float)samplesPerSegment;
                    result.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }

            if (closedLoop)
            {
                result.Add(result[0]);
            }
            else
            {
                result.Add(controlPoints[pointCount - 1]);
            }

            return result;
        }

        private void ConformCenterLineToTerrain(List<Vector3> centerLine)
        {
            bool previousColliderState = roadMeshCollider != null && roadMeshCollider.enabled;

            if (roadMeshCollider != null)
            {
                roadMeshCollider.enabled = false;
            }

            for (int i = 0; i < centerLine.Count; i++)
            {
                Vector3 worldPoint = transform.TransformPoint(centerLine[i]);
                Vector3 rayStart = worldPoint + Vector3.up * terrainRayHeight;
                RaycastHit hit;

                if (Physics.Raycast(
                    rayStart,
                    Vector3.down,
                    out hit,
                    terrainRayHeight * 2f,
                    terrainLayerMask,
                    QueryTriggerInteraction.Ignore))
                {
                    centerLine[i] = transform.InverseTransformPoint(
                        hit.point + Vector3.up * terrainYOffset);
                }
            }

            if (roadMeshCollider != null)
            {
                roadMeshCollider.enabled = previousColliderState;
            }
        }

        private void BuildRoadMesh(List<Vector3> centerLine)
        {
            Mesh mesh = GetOrCreateMesh(roadMeshFilter, RoadMeshName);
            mesh.Clear();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> asphaltTriangles = new List<int>();
            List<int> markingTriangles = new List<int>();

            float halfWidth = roadWidth * 0.5f;
            float accumulatedDistance = 0f;

            for (int i = 0; i < centerLine.Count; i++)
            {
                Vector3 tangent = GetTangent(centerLine, i);
                Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;

                Vector3 center = centerLine[i] + Vector3.up * surfaceOffset;
                Vector3 topLeft = center - right * halfWidth;
                Vector3 topRight = center + right * halfWidth;
                Vector3 bottomLeft = topLeft - Vector3.up * roadThickness;
                Vector3 bottomRight = topRight - Vector3.up * roadThickness;

                if (i > 0)
                {
                    accumulatedDistance += Vector3.Distance(centerLine[i - 1], centerLine[i]);
                }

                vertices.Add(topLeft);
                vertices.Add(topRight);
                vertices.Add(bottomLeft);
                vertices.Add(bottomRight);

                uvs.Add(new Vector2(0f, accumulatedDistance / 4f));
                uvs.Add(new Vector2(1f, accumulatedDistance / 4f));
                uvs.Add(new Vector2(0f, accumulatedDistance / 4f));
                uvs.Add(new Vector2(1f, accumulatedDistance / 4f));
            }

            for (int i = 0; i < centerLine.Count - 1; i++)
            {
                int current = i * 4;
                int next = (i + 1) * 4;

                // Üst yüzey
                AddTriangle(asphaltTriangles, current + 0, next + 0, current + 1);
                AddTriangle(asphaltTriangles, current + 1, next + 0, next + 1);

                // Alt yüzey
                AddTriangle(asphaltTriangles, current + 2, current + 3, next + 2);
                AddTriangle(asphaltTriangles, current + 3, next + 3, next + 2);

                // Sol yan
                AddTriangle(asphaltTriangles, current + 0, current + 2, next + 0);
                AddTriangle(asphaltTriangles, current + 2, next + 2, next + 0);

                // Sağ yan
                AddTriangle(asphaltTriangles, current + 1, next + 1, current + 3);
                AddTriangle(asphaltTriangles, current + 3, next + 1, next + 3);
            }

            if (!closedLoop)
            {
                int first = 0;
                int last = (centerLine.Count - 1) * 4;

                // Başlangıç kapağı
                AddTriangle(asphaltTriangles, first + 0, first + 1, first + 2);
                AddTriangle(asphaltTriangles, first + 1, first + 3, first + 2);

                // Bitiş kapağı
                AddTriangle(asphaltTriangles, last + 0, last + 2, last + 1);
                AddTriangle(asphaltTriangles, last + 1, last + 2, last + 3);
            }

            if (createLaneMarkings && laneCount > 1)
            {
                BuildLaneMarkings(
                    centerLine,
                    vertices,
                    uvs,
                    markingTriangles,
                    halfWidth);
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(asphaltTriangles, 0);
            mesh.SetTriangles(markingTriangles, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            roadMeshFilter.sharedMesh = mesh;

            if (roadMeshCollider != null)
            {
                roadMeshCollider.sharedMesh = null;
                roadMeshCollider.sharedMesh = mesh;
            }
        }

        private void BuildLaneMarkings(
            List<Vector3> centerLine,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            float halfWidth)
        {
            float cycleLength = dashLength + dashGap;
            float travelled = 0f;

            for (int segment = 0; segment < centerLine.Count - 1; segment++)
            {
                Vector3 start = centerLine[segment];
                Vector3 end = centerLine[segment + 1];
                float segmentLength = Vector3.Distance(start, end);

                if (segmentLength < 0.001f)
                {
                    continue;
                }

                Vector3 startTangent = GetTangent(centerLine, segment);
                Vector3 endTangent = GetTangent(centerLine, segment + 1);
                Vector3 startRight = Vector3.Cross(Vector3.up, startTangent).normalized;
                Vector3 endRight = Vector3.Cross(Vector3.up, endTangent).normalized;

                float middleDistance = travelled + segmentLength * 0.5f;
                bool drawDash = Mathf.Repeat(middleDistance, cycleLength) <= dashLength;

                if (drawDash)
                {
                    for (int boundary = 1; boundary < laneCount; boundary++)
                    {
                        float laneOffset = -halfWidth + roadWidth * boundary / laneCount;

                        Vector3 aCenter = start
                                          + startRight * laneOffset
                                          + Vector3.up * (surfaceOffset + 0.025f);

                        Vector3 bCenter = end
                                          + endRight * laneOffset
                                          + Vector3.up * (surfaceOffset + 0.025f);

                        Vector3 aLeft = aCenter - startRight * (markingWidth * 0.5f);
                        Vector3 aRight = aCenter + startRight * (markingWidth * 0.5f);
                        Vector3 bLeft = bCenter - endRight * (markingWidth * 0.5f);
                        Vector3 bRight = bCenter + endRight * (markingWidth * 0.5f);

                        int index = vertices.Count;
                        vertices.Add(aLeft);
                        vertices.Add(aRight);
                        vertices.Add(bLeft);
                        vertices.Add(bRight);

                        uvs.Add(new Vector2(0f, 0f));
                        uvs.Add(new Vector2(1f, 0f));
                        uvs.Add(new Vector2(0f, 1f));
                        uvs.Add(new Vector2(1f, 1f));

                        AddTriangle(triangles, index + 0, index + 2, index + 1);
                        AddTriangle(triangles, index + 1, index + 2, index + 3);
                    }
                }

                travelled += segmentLength;
            }
        }

        private void BuildSidewalkMesh(List<Vector3> centerLine)
        {
            Transform sidewalkTransform = GetOrCreateSidewalkObject();
            sidewalkTransform.gameObject.SetActive(createSidewalks && sidewalkWidth > 0.01f);

            if (!createSidewalks || sidewalkWidth <= 0.01f)
            {
                return;
            }

            MeshFilter meshFilter = sidewalkTransform.GetComponent<MeshFilter>();
            MeshCollider meshCollider = sidewalkTransform.GetComponent<MeshCollider>();
            Mesh mesh = GetOrCreateMesh(meshFilter, SidewalkMeshName);
            mesh.Clear();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            float halfRoad = roadWidth * 0.5f;
            float accumulatedDistance = 0f;

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float sideSign = sideIndex == 0 ? -1f : 1f;
                int sideStartVertex = vertices.Count;

                for (int i = 0; i < centerLine.Count; i++)
                {
                    Vector3 tangent = GetTangent(centerLine, i);
                    Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
                    Vector3 center = centerLine[i] + Vector3.up * surfaceOffset;

                    Vector3 innerTop = center
                                       + right * (halfRoad * sideSign)
                                       + Vector3.up * sidewalkHeight;

                    Vector3 outerTop = center
                                       + right * ((halfRoad + sidewalkWidth) * sideSign)
                                       + Vector3.up * sidewalkHeight;

                    Vector3 innerBottom = innerTop - Vector3.up * sidewalkHeight;
                    Vector3 outerBottom = outerTop - Vector3.up * sidewalkHeight;

                    if (i > 0 && sideIndex == 0)
                    {
                        accumulatedDistance += Vector3.Distance(
                            centerLine[i - 1],
                            centerLine[i]);
                    }

                    if (sideSign < 0f)
                    {
                        vertices.Add(outerTop);
                        vertices.Add(innerTop);
                        vertices.Add(outerBottom);
                        vertices.Add(innerBottom);
                    }
                    else
                    {
                        vertices.Add(innerTop);
                        vertices.Add(outerTop);
                        vertices.Add(innerBottom);
                        vertices.Add(outerBottom);
                    }

                    uvs.Add(new Vector2(0f, accumulatedDistance / 3f));
                    uvs.Add(new Vector2(1f, accumulatedDistance / 3f));
                    uvs.Add(new Vector2(0f, accumulatedDistance / 3f));
                    uvs.Add(new Vector2(1f, accumulatedDistance / 3f));
                }

                for (int i = 0; i < centerLine.Count - 1; i++)
                {
                    int current = sideStartVertex + i * 4;
                    int next = sideStartVertex + (i + 1) * 4;

                    // Üst
                    AddTriangle(triangles, current + 0, next + 0, current + 1);
                    AddTriangle(triangles, current + 1, next + 0, next + 1);

                    // Alt
                    AddTriangle(triangles, current + 2, current + 3, next + 2);
                    AddTriangle(triangles, current + 3, next + 3, next + 2);

                    // Dış yüz
                    AddTriangle(triangles, current + 0, current + 2, next + 0);
                    AddTriangle(triangles, current + 2, next + 2, next + 0);

                    // İç yüz
                    AddTriangle(triangles, current + 1, next + 1, current + 3);
                    AddTriangle(triangles, current + 3, next + 1, next + 3);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }

        private Transform GetOrCreateSidewalkObject()
        {
            Transform child = transform.Find(SidewalkObjectName);

            if (child == null)
            {
                GameObject sidewalkObject = new GameObject(SidewalkObjectName);
                child = sidewalkObject.transform;
                child.SetParent(transform, false);
                sidewalkObject.AddComponent<MeshFilter>();
                sidewalkObject.AddComponent<MeshRenderer>();
                sidewalkObject.AddComponent<MeshCollider>();
            }

            return child;
        }

        private void ClearGeneratedMeshes()
        {
            if (roadMeshFilter != null && roadMeshFilter.sharedMesh != null)
            {
                roadMeshFilter.sharedMesh.Clear();
            }

            if (roadMeshCollider != null)
            {
                roadMeshCollider.sharedMesh = null;
            }

            Transform sidewalk = transform.Find(SidewalkObjectName);

            if (sidewalk != null)
            {
                MeshFilter filter = sidewalk.GetComponent<MeshFilter>();
                MeshCollider collider = sidewalk.GetComponent<MeshCollider>();

                if (filter != null && filter.sharedMesh != null)
                {
                    filter.sharedMesh.Clear();
                }

                if (collider != null)
                {
                    collider.sharedMesh = null;
                }
            }
        }

        private static Mesh GetOrCreateMesh(MeshFilter filter, string meshName)
        {
            Mesh mesh = filter.sharedMesh;

            if (mesh == null || mesh.name != meshName)
            {
                mesh = new Mesh();
                mesh.name = meshName;
                filter.sharedMesh = mesh;
            }

            return mesh;
        }

        private static Vector3 GetTangent(List<Vector3> points, int index)
        {
            if (points.Count < 2)
            {
                return Vector3.forward;
            }

            Vector3 tangent;

            if (index <= 0)
            {
                tangent = points[1] - points[0];
            }
            else if (index >= points.Count - 1)
            {
                tangent = points[points.Count - 1] - points[points.Count - 2];
            }
            else
            {
                tangent = points[index + 1] - points[index - 1];
            }

            tangent.y = 0f;

            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = Vector3.forward;
            }

            return tangent.normalized;
        }

        private static Vector3 CatmullRom(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f *
                   ((2f * p1) +
                    (-p0 + p2) * t +
                    (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                    (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private static int Mod(int value, int modulus)
        {
            return (value % modulus + modulus) % modulus;
        }

        private static void AddTriangle(List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (controlPoints == null || controlPoints.Count == 0)
            {
                return;
            }

            Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);

            for (int i = 0; i < controlPoints.Count; i++)
            {
                Vector3 world = transform.TransformPoint(controlPoints[i]);
                Gizmos.DrawSphere(world, 0.25f);

                if (i < controlPoints.Count - 1)
                {
                    Gizmos.DrawLine(
                        world,
                        transform.TransformPoint(controlPoints[i + 1]));
                }
            }

            if (closedLoop && controlPoints.Count > 2)
            {
                Gizmos.DrawLine(
                    transform.TransformPoint(controlPoints[controlPoints.Count - 1]),
                    transform.TransformPoint(controlPoints[0]));
            }
        }
#endif
    }
}
