using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CleanNightSky : MonoBehaviour
{
    private class Comet
    {
        public LineRenderer line;
        public Vector3 headPosition;
        public Vector3 velocity;
        public float life;
        public float maxLife;
        public float tailLength;
    }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Light sunLight;
    [SerializeField] private Light moonLight;

    [Header("Debug")]
    [SerializeField] private bool forceNightVisuals;

    [Header("Moon")]
    [SerializeField] private bool enableMoon = true;
    [SerializeField] private float moonDistance = 3200f;
    [SerializeField] private float moonSize = 140f;
    [SerializeField] private float moonGlowSize = 280f;
    [SerializeField] private int moonGlowSegments = 48;
    [SerializeField] private Color moonColor = new Color(0.72f, 0.78f, 0.95f, 1f);
    [SerializeField] private Color moonGlowColor = new Color(0.35f, 0.48f, 0.85f, 0.16f);

    [Header("Stars")]
    [SerializeField] private int starCount = 2500;
    [SerializeField] private float starDistance = 1200f;
    [SerializeField] private float starMinHeight = -0.05f;
    [SerializeField] private float minStarSize = 1f;
    [SerializeField] private float maxStarSize = 2f;
    [SerializeField] private Color starColor = new Color(0.58f, 0.66f, 0.9f, 0.72f);
    [SerializeField] private Texture2D starTexture;
    [SerializeField] private int generatedStarTextureWidth = 2048;
    [SerializeField] private int generatedStarTextureHeight = 1024;
    [SerializeField] private int starDomeLongitudeSegments = 64;
    [SerializeField] private int starDomeLatitudeSegments = 32;
    [SerializeField] private float generatedStarBrightness = 0.65f;
    [SerializeField] private float milkyWayBrightness = 0.12f;

    [Header("Comets")]
    [SerializeField] private float cometDistance = 800f;
    [SerializeField] private float cometSpeed = 180f;
    [SerializeField] private float cometTailLength = 120f;
    [SerializeField] private float cometLifeTime = 6f;
    [SerializeField] private float cometWidth = 2f;
    [SerializeField] private Vector2 cometSpawnInterval = new Vector2(3f, 6f);

    private GameObject moonObject;
    private GameObject moonGlowObject;
    private GameObject starsObject;

    private Transform skyRoot;

    private Mesh starMesh;
    private Mesh moonGlowMesh;

    private Material moonMaterial;
    private Material moonGlowMaterial;
    private Material starMaterial;
    private Material cometMaterial;

    private Texture2D softCircleTexture;
    private Texture2D runtimeStarTexture;

    private readonly List<Comet> activeComets = new List<Comet>();

    private float nextCometTimer;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        CreateSkyRoot();
        CreateMaterials();
        CreateMoon();
        CreateStars();

        nextCometTimer = Random.Range(cometSpawnInterval.x, cometSpawnInterval.y);
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            return;

        if (skyRoot != null)
        {
            skyRoot.position = mainCamera.transform.position;
            skyRoot.rotation = Quaternion.identity;
        }

        float nightAmount = GetNightAmount();

        UpdateMoon(nightAmount);
        UpdateStars(nightAmount);
        UpdateComets(nightAmount);
    }

    private float GetNightAmount()
    {
        if (forceNightVisuals)
            return 1f;

        if (sunLight == null)
            return 1f;

        float sunHeight = -sunLight.transform.forward.y;

        return Mathf.SmoothStep(
            0f,
            1f,
            Mathf.InverseLerp(0.05f, -0.16f, sunHeight)
        );
    }

    private void CreateSkyRoot()
    {
        GameObject skyRootObject = new GameObject("RuntimeNightSkyRoot");
        skyRootObject.transform.SetParent(transform, false);
        skyRootObject.transform.localPosition = Vector3.zero;
        skyRootObject.transform.localRotation = Quaternion.identity;
        skyRootObject.transform.localScale = Vector3.one;

        skyRoot = skyRootObject.transform;
    }

    private void CreateMaterials()
    {
        softCircleTexture = CreateSoftCircleTexture(128, 0.05f);

        Shader shader = Shader.Find("Custom/Sky Additive Unlit");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        runtimeStarTexture = starTexture != null
            ? starTexture
            : CreateProceduralStarTexture(
                Mathf.Max(512, generatedStarTextureWidth),
                Mathf.Max(256, generatedStarTextureHeight)
            );

        moonMaterial = CreateSkyMaterial(shader, Texture2D.whiteTexture, moonColor);
        moonGlowMaterial = CreateSkyMaterial(shader, softCircleTexture, moonGlowColor);
        starMaterial = CreateSkyMaterial(shader, runtimeStarTexture, starColor);
        cometMaterial = CreateSkyMaterial(shader, softCircleTexture, Color.white);
    }

    private Material CreateSkyMaterial(Shader shader, Texture texture, Color color)
    {
        if (shader == null)
        {
            Debug.LogError("No shader found for CleanNightSky.");
            return null;
        }

        Material material = new Material(shader);
        material.name = "Runtime_SkyMaterial";

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 2f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.One);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");

        material.renderQueue = 3100;

        return material;
    }

    private void CreateMoon()
    {
        if (!enableMoon)
            return;

        moonGlowObject = new GameObject("RuntimeMoonGlow");
        moonGlowObject.transform.SetParent(skyRoot, false);
        moonGlowObject.transform.localPosition = Vector3.zero;
        moonGlowObject.transform.localRotation = Quaternion.identity;
        moonGlowObject.transform.localScale = Vector3.one;

        MeshFilter glowFilter = moonGlowObject.AddComponent<MeshFilter>();
        MeshRenderer glowRenderer = moonGlowObject.AddComponent<MeshRenderer>();

        moonGlowMesh = new Mesh();
        moonGlowMesh.name = "Runtime Moon Glow Mesh";
        moonGlowMesh.MarkDynamic();

        glowFilter.sharedMesh = moonGlowMesh;

        glowRenderer.material = moonGlowMaterial;
        glowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        glowRenderer.receiveShadows = false;
        glowRenderer.allowOcclusionWhenDynamic = false;

        moonObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moonObject.name = "RuntimeMoon";
        moonObject.transform.SetParent(skyRoot, false);
        moonObject.transform.localPosition = Vector3.zero;
        moonObject.transform.localRotation = Quaternion.identity;
        moonObject.transform.localScale = Vector3.one;

        Collider moonCollider = moonObject.GetComponent<Collider>();

        if (moonCollider != null)
            Destroy(moonCollider);

        MeshRenderer moonRenderer = moonObject.GetComponent<MeshRenderer>();

        if (moonRenderer != null)
        {
            moonRenderer.material = moonMaterial;
            moonRenderer.shadowCastingMode = ShadowCastingMode.Off;
            moonRenderer.receiveShadows = false;
            moonRenderer.allowOcclusionWhenDynamic = false;
        }
    }

    private void UpdateMoon(float nightAmount)
    {
        if (!enableMoon || moonObject == null || moonGlowObject == null)
            return;

        bool visible = nightAmount > 0.04f;

        moonObject.SetActive(visible);
        moonGlowObject.SetActive(visible);

        if (!visible)
            return;

        Vector3 moonDirection;

        if (moonLight != null)
            moonDirection = -moonLight.transform.forward;
        else if (sunLight != null)
            moonDirection = sunLight.transform.forward;
        else
            moonDirection = Vector3.up;

        moonDirection.Normalize();

        Vector3 center = moonDirection * moonDistance;

        moonObject.transform.localPosition = center;
        moonObject.transform.localRotation = Quaternion.identity;
        moonObject.transform.localScale = Vector3.one * moonSize;

        Color finalMoonColor = moonColor;
        finalMoonColor.a = Mathf.Clamp01(nightAmount);

        SetMaterialColor(moonMaterial, finalMoonColor);

        Color finalGlowColor = moonGlowColor;
        finalGlowColor.a = moonGlowColor.a * nightAmount;

        BuildDiscMesh(moonGlowMesh, center, moonGlowSize, finalGlowColor, moonDistance, moonGlowSegments);
    }

    private void CreateStars()
    {
        starsObject = new GameObject("RuntimeStars");
        starsObject.transform.SetParent(skyRoot, false);
        starsObject.transform.localPosition = Vector3.zero;
        starsObject.transform.localRotation = Quaternion.identity;
        starsObject.transform.localScale = Vector3.one;

        MeshFilter meshFilter = starsObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = starsObject.AddComponent<MeshRenderer>();

        meshRenderer.material = starMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.allowOcclusionWhenDynamic = false;

        starMesh = new Mesh();
        starMesh.name = "Runtime Star Dome Mesh";
        starMesh.indexFormat = IndexFormat.UInt32;
        starMesh.MarkDynamic();
        BuildStarDomeMesh(
            starMesh,
            starDistance,
            Mathf.Max(16, starDomeLongitudeSegments),
            Mathf.Max(8, starDomeLatitudeSegments)
        );

        meshFilter.sharedMesh = starMesh;
    }

    private void UpdateStars(float nightAmount)
    {
        if (starsObject == null || starMesh == null)
            return;

        bool visible = nightAmount > 0.04f;

        starsObject.SetActive(visible);

        if (!visible)
            return;

        Color finalStarColor = starColor;
        finalStarColor.a = starColor.a * Mathf.Clamp01(nightAmount * 0.9f);

        SetMaterialColor(starMaterial, finalStarColor);
    }

    private void UpdateComets(float nightAmount)
    {
        nextCometTimer -= Time.deltaTime;

        if (nightAmount > 0.55f && nextCometTimer <= 0f)
        {
            SpawnComet();
            nextCometTimer = Random.Range(cometSpawnInterval.x, cometSpawnInterval.y);
        }

        for (int i = activeComets.Count - 1; i >= 0; i--)
        {
            Comet comet = activeComets[i];

            comet.life += Time.deltaTime;
            comet.headPosition += comet.velocity * Time.deltaTime;

            float life01 = comet.life / comet.maxLife;
            float fade = Mathf.Sin(life01 * Mathf.PI) * nightAmount;

            Vector3 tailPosition =
                comet.headPosition -
                comet.velocity.normalized * comet.tailLength;

            if (comet.line != null)
            {
                comet.line.SetPosition(0, comet.headPosition);
                comet.line.SetPosition(1, tailPosition);

                comet.line.startColor = new Color(0.65f, 0.78f, 1f, fade * 0.65f);
                comet.line.endColor = new Color(0.08f, 0.18f, 0.34f, 0f);
            }

            if (comet.life >= comet.maxLife)
            {
                if (comet.line != null)
                    Destroy(comet.line.gameObject);

                activeComets.RemoveAt(i);
            }
        }
    }

    private void SpawnComet()
    {
        if (mainCamera == null)
            return;

        Vector3 direction = Random.onUnitSphere;

        while (direction.y < 0.25f)
            direction = Random.onUnitSphere;

        Vector3 startPosition =
            mainCamera.transform.position +
            direction * cometDistance;

        Vector3 tangent = Vector3.Cross(direction, Vector3.up);

        if (tangent.sqrMagnitude < 0.01f)
            tangent = Vector3.Cross(direction, Vector3.right);

        tangent.Normalize();

        if (Random.value > 0.5f)
            tangent = -tangent;

        GameObject cometObject = new GameObject("RuntimeComet");
        cometObject.transform.SetParent(skyRoot, false);

        LineRenderer line = cometObject.AddComponent<LineRenderer>();

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = cometWidth;
        line.endWidth = 0f;
        line.numCapVertices = 6;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = cometMaterial;

        Comet comet = new Comet();
        comet.line = line;
        comet.headPosition = startPosition;
        comet.velocity = tangent * Random.Range(cometSpeed * 0.8f, cometSpeed * 1.25f);
        comet.life = 0f;
        comet.maxLife = cometLifeTime;
        comet.tailLength = cometTailLength;

        activeComets.Add(comet);
    }

    private void BuildDiscMesh(Mesh mesh, Vector3 center, float radius, Color color, float boundsSize, int segments)
    {
        if (mesh == null)
            return;

        segments = Mathf.Max(16, segments);

        Vector3 cameraRight = mainCamera.transform.right;
        Vector3 cameraUp = mainCamera.transform.up;

        Vector3[] meshVertices = new Vector3[segments + 1];
        Vector2[] meshUvs = new Vector2[segments + 1];
        Color[] meshColors = new Color[segments + 1];
        int[] meshTriangles = new int[segments * 3];

        meshVertices[0] = center;
        meshUvs[0] = new Vector2(0.5f, 0.5f);
        meshColors[0] = color;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            Vector3 offset = cameraRight * x * radius + cameraUp * y * radius;

            meshVertices[i + 1] = center + offset;
            meshUvs[i + 1] = new Vector2(0.5f + x * 0.5f, 0.5f + y * 0.5f);
            meshColors[i + 1] = color;
        }

        for (int i = 0; i < segments; i++)
        {
            int t = i * 3;

            meshTriangles[t + 0] = 0;
            meshTriangles[t + 1] = i + 1;
            meshTriangles[t + 2] = i == segments - 1 ? 1 : i + 2;
        }

        mesh.Clear();
        mesh.vertices = meshVertices;
        mesh.uv = meshUvs;
        mesh.triangles = meshTriangles;
        mesh.colors = meshColors;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * boundsSize * 4f);
    }

    private void BuildStarDomeMesh(Mesh mesh, float radius, int longitudeSegments, int latitudeSegments)
    {
        Vector3[] meshVertices = new Vector3[(longitudeSegments + 1) * (latitudeSegments + 1)];
        Vector2[] meshUvs = new Vector2[meshVertices.Length];
        int[] meshTriangles = new int[longitudeSegments * latitudeSegments * 6];

        int vertex = 0;

        for (int y = 0; y <= latitudeSegments; y++)
        {
            float v = (float)y / latitudeSegments;
            float latitude = Mathf.Lerp(-Mathf.PI * 0.08f, Mathf.PI * 0.92f, v);
            float cosLatitude = Mathf.Cos(latitude);
            float sinLatitude = Mathf.Sin(latitude);

            for (int x = 0; x <= longitudeSegments; x++)
            {
                float u = (float)x / longitudeSegments;
                float longitude = u * Mathf.PI * 2f;

                meshVertices[vertex] = new Vector3(
                    Mathf.Cos(longitude) * cosLatitude,
                    sinLatitude,
                    Mathf.Sin(longitude) * cosLatitude
                ) * radius;

                meshUvs[vertex] = new Vector2(u, v);
                vertex++;
            }
        }

        int triangle = 0;

        for (int y = 0; y < latitudeSegments; y++)
        {
            for (int x = 0; x < longitudeSegments; x++)
            {
                int a = y * (longitudeSegments + 1) + x;
                int b = a + longitudeSegments + 1;
                int c = b + 1;
                int d = a + 1;

                meshTriangles[triangle++] = a;
                meshTriangles[triangle++] = c;
                meshTriangles[triangle++] = b;
                meshTriangles[triangle++] = a;
                meshTriangles[triangle++] = d;
                meshTriangles[triangle++] = c;
            }
        }

        mesh.Clear();
        mesh.vertices = meshVertices;
        mesh.uv = meshUvs;
        mesh.triangles = meshTriangles;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.5f);
    }

    private Texture2D CreateProceduralStarTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Runtime_ProceduralStarTexture";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Random.State randomState = Random.state;
        Random.InitState(42731);

        int proceduralStarCount = Mathf.Max(starCount, width * height / 900);

        for (int i = 0; i < proceduralStarCount; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(Mathf.RoundToInt(height * Mathf.Clamp01(starMinHeight + 0.12f)), height);
            float normalizedY = (float)y / height;
            float horizonFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.34f, normalizedY));

            float milkyWay = Mathf.Exp(-Mathf.Pow((normalizedY - 0.58f) * 7.5f, 2f));
            float brightness = Random.Range(0.18f, 1f) * generatedStarBrightness * horizonFade;
            brightness += milkyWay * milkyWayBrightness * Random.Range(0.2f, 0.75f);

            if (brightness <= 0.02f)
                continue;

            int radius = Mathf.Clamp(Mathf.RoundToInt(Random.Range(minStarSize, maxStarSize)), 1, 3);
            if (Random.value < 0.95f)
                radius = 1;
            Color color = Color.Lerp(
                new Color(0.72f, 0.78f, 1f, 1f),
                new Color(1f, 0.92f, 0.74f, 1f),
                Random.value
            );

            DrawStar(pixels, width, height, x, y, radius, color, brightness);
        }

        Random.state = randomState;

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        return texture;
    }

    private void DrawStar(
        Color[] pixels,
        int width,
        int height,
        int centerX,
        int centerY,
        int radius,
        Color color,
        float brightness
    )
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int px = (centerX + x + width) % width;
                int py = Mathf.Clamp(centerY + y, 0, height - 1);
                float distance = Mathf.Sqrt(x * x + y * y);
                float alpha = brightness * (1f - Mathf.Clamp01(distance / (radius + 0.4f)));

                if (alpha <= 0f)
                    continue;

                int index = py * width + px;
                Color current = pixels[index];
                Color next = color;
                next.a = alpha;

                pixels[index] = Color.Lerp(current, next, Mathf.Clamp01(alpha));
            }
        }
    }

    private Texture2D CreateSoftCircleTexture(int size, float solidCenter)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.Clamp01(distance / radius);

                float alpha = 1f - Mathf.SmoothStep(solidCenter, 1f, t);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        return texture;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
    }
}
