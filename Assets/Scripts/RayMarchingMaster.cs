using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class RayMarchingMaster : MonoBehaviour
{
    public ComputeShader RayMarchingShader;
    private RenderTexture target;
    public Texture SkyboxTexture;
    private Camera _camera;
    [SerializeField] Light directionalLight;
    private List<Transform> transformsWatching = new List<Transform>();
    private ComputeBuffer _ShapeBuffer;

    [Header("Shader Settings")]
    [SerializeField] bool useShader = true;

    struct Shape{
        public int shapeType;  // 4 bytes
        public Vector3 origin; // 12 bytes
        public Vector3 firstPoint; // 12 bytes
        public Vector3 secondPoint; // 12 bytes
        public Vector3 size; // 12 bytes
        public float rounding; // 4 bytes
        public Vector3 colour; // 12 bytes
        public int operation; // 4 bytes
        public float blendStrength; // 4 bytes
    }

    void SetUpScene(){
        _ShapeBuffer = new ComputeBuffer(1, 76);
    }
    void Awake()
    {
        _camera = GetComponent<Camera>();
        transformsWatching.Add(transform);
        transformsWatching.Add(directionalLight.transform);
    }

    void Update()
    {
        foreach(Transform transformSelected in transformsWatching){
            if(transformSelected.hasChanged){
                transformSelected.hasChanged = false;
            }
        }
    }

    private void InitRenderTexture(){
        if(target == null || target.width != Screen.width || target.height != Screen.height){
            if(target != null){
                target.Release();
            }

            target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }
    }

    private void Render(RenderTexture dest){
        InitRenderTexture();
        RayMarchingShader.SetTexture(0, "Result", target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8f);
        RayMarchingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        Graphics.Blit(target, dest);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        if (!useShader){
            Graphics.Blit(src, dest);
        } else {
            SetShaderParameters();
            Render(dest);
        }
    }

    void SetShaderParameters(){
        RayMarchingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayMarchingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
    }
}
