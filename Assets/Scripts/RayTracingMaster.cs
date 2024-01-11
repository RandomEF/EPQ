using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
//using UnityEngine.Windows;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    private RenderTexture target;
    public Texture SkyboxTexture;
    private Camera _camera;
    private uint _currentSample = 0;
    private Material _addMaterial;
    [SerializeField] float maxBounceNumber = 4;
    [SerializeField] Light directionalLight;
    private List<Transform> transformsWatching = new List<Transform>();
    private bool sceneChanged = false;

    private ComputeBuffer _SphereBuffer;

    [Header("Shader Settings")]
    [SerializeField] bool useShader = true;
    [SerializeField] bool enableRandomSpheres = true;
    [SerializeField] bool regenerateSpheres = false;
    
    [Header("Random Sphere Settings")]
    public Vector2 sphereRadius = new Vector2(3.0f, 8.0f);
    public uint sphereMax = 100;
    public float distBetweenSpheres = 100.0f;

    struct Sphere {
        public Vector3 position; // 12 bytes
        public float radius; // 4 bytes
        public Vector3 specular; // 12 bytes
        public Vector3 albedo; // 12 bytes
    } // totals 40 bytes

    private void OnEnable() {
        _currentSample = 0;
        if (_SphereBuffer != null){
            _SphereBuffer.SetCounterValue(0);
        }
        if (enableRandomSpheres){
            SetUpScene();
        } else {
            SetUpSpheres();
        }
    }
    private void OnDisable() {
        if (_SphereBuffer != null){
            _SphereBuffer.Release();
        }
    }

    private void Awake(){
        _camera = GetComponent<Camera>();
        transformsWatching.Add(transform);
        transformsWatching.Add(directionalLight.transform);
        if (!enableRandomSpheres){
            GameObject[] spheresInScene = GameObject.FindGameObjectsWithTag("Spheres");
            foreach (GameObject sphereInScene in spheresInScene){
                transformsWatching.Add(sphereInScene.transform);
            }
        }
    }
    
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Equals)){
            maxBounceNumber++;
            transform.hasChanged = true;
        } else if (Input.GetKeyDown(KeyCode.Minus) && maxBounceNumber != 0){
            maxBounceNumber--;
            transform.hasChanged = true;
        }
        foreach (Transform transformSelected in transformsWatching){
            if (transformSelected.hasChanged){
                _currentSample = 0;
                transformSelected.hasChanged = false;
                if(!enableRandomSpheres){
                    if(_SphereBuffer != null){
                        _SphereBuffer.Release();
                    }
                    SetUpSpheres();
                }
            }
        }
    }

    public void SetUpScene(){
        List<Sphere> spheres = new List<Sphere>();
        for (int i = 0; i < sphereMax; i++){
            Sphere sphere = new Sphere();
            sphere.radius = sphereRadius.x + Random.value * (sphereRadius.y - sphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * distBetweenSpheres;
            sphere.position = new Vector3(randomPos.x, sphere.radius + 0.0001f, randomPos.y);

            foreach (Sphere otherSphere in spheres){
                float minimumDist = sphere.radius + otherSphere.radius;
                if (Vector3.SqrMagnitude(sphere.position - otherSphere.position) < minimumDist * minimumDist){
                    goto SkipSphere;
                }
            }
            Color color = Random.ColorHSV();
            bool isMetal = Random.value < 0.5f;
            sphere.albedo = isMetal ? Vector3.zero : new Vector3(color.r, color.g, color. b);
            sphere.specular = isMetal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;

            spheres.Add(sphere);

            SkipSphere: continue;
        }
        

        _SphereBuffer = new ComputeBuffer(spheres.Count, 40);
        _SphereBuffer.SetData(spheres);
    }

    private void SetUpSpheres(){
        List<Sphere> spheres = new List<Sphere>();
        GameObject[] spheresInScene = GameObject.FindGameObjectsWithTag("Spheres");
        foreach (GameObject sphereInScene in spheresInScene){
            Sphere sphere = new Sphere();
            sphere.radius = sphereInScene.transform.localScale.x * 0.5f;
            sphere.position = sphereInScene.transform.position;
            Color albedoOfSphere = sphereInScene.GetComponent<Renderer>().material.color;
            sphere.albedo = new Vector3(albedoOfSphere.r, albedoOfSphere.g, albedoOfSphere.b);
            float smoothness = sphereInScene.GetComponent<Renderer>().material.GetFloat("_Glossiness");
            float smoothnessEnable = sphereInScene.GetComponent<Renderer>().material.GetFloat("_UseSmoothness");
            if (smoothnessEnable == 1){
                sphere.specular = new Vector3(smoothness, smoothness, smoothness);
            } else {
                sphere.specular = new Vector3(albedoOfSphere.r, albedoOfSphere.g, albedoOfSphere.b);
            }

            spheres.Add(sphere);
        }

        _SphereBuffer = new ComputeBuffer(spheres.Count, 40);
        _SphereBuffer.SetData(spheres);
    }
    private void InitRenderTexture(){
        if (target == null || target.width != Screen.width || target.height != Screen.height){
            if (target != null){
                target.Release();
            }
            
            target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            _currentSample = 0;
            target.Create();
        }
    }

    private void Render(RenderTexture dest){
        InitRenderTexture();

        RayTracingShader.SetTexture(0, "Result", target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        if (_addMaterial == null){
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        }
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(target, dest, _addMaterial);
        _currentSample++;
    }
    
    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        if (!useShader){
            Graphics.Blit(src, dest);
        } else {
            SetShaderParameters();
            Render(dest);
        }
    }

    private void SetShaderParameters(){
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        RayTracingShader.SetFloat("_maxBounceNumber", maxBounceNumber);
        Vector3 lightDirection = directionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, directionalLight.intensity));
        RayTracingShader.SetBuffer(0, "_Spheres", _SphereBuffer);
    }
}