using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class RayMarchingMaster : MonoBehaviour
{
    public ComputeShader RayMarchingShader;
    public Texture SkyboxTexture;
    private RenderTexture target;
    private Camera _camera;
    [SerializeField] Light directionalLight;
    private List<Transform> transformsWatching = new List<Transform>();
    private ComputeBuffer _ShapeBuffer;

    [Header("Shader Settings")]
    [SerializeField] bool useShader = true;

    [Header("Random Shape Settings")]
    public uint shapeCount = 10;
    public Vector2 sphereRadius = new Vector2(1.0f, 5.0f);
    public Vector2 cubeSize = new Vector2(3.0f, 8.0f);
    public Vector2 lineLength = new Vector2(3.0f, 8.0f);
    
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

    Shape CreateFloor(){
        Shape shape = new Shape();
        shape.shapeType = 1;
        shape.size = new Vector3(100, 0.01f, 100);
        shape.origin = Vector3.zero;
        shape.firstPoint = Vector3.zero;
        shape.secondPoint = Vector3.zero;
        shape.rounding = 0f;
        shape.colour = new Vector3(0.9f, 0.9f, 0.9f);
        shape.operation = 0;
        shape.blendStrength = 0;

        return shape;
    }

    Shape CreateSphere(){
        Shape shape = new Shape();
        shape.shapeType = 0;
        shape.size = new Vector3(Random.value * (sphereRadius.y - sphereRadius.x), 0, 0);
        shape.origin = new Vector3(Random.value * 10, Random.value * 10, Random.value * 10);
        shape.firstPoint = Vector3.zero;
        shape.secondPoint = Vector3.zero;
        shape.rounding = 0f;
        Color color = Random.ColorHSV();
        shape.colour = new Vector3(color.r, color.g, color.b);
        shape.operation = 0;
        shape.blendStrength = 0;

        return shape;
    }

    Shape CreateCube(){
        Shape shape = new Shape();
        shape.shapeType = 1;
        shape.size = new Vector3(Random.value * (cubeSize.y - cubeSize.x), Random.value * (cubeSize.y - cubeSize.x), Random.value * (cubeSize.y - cubeSize.x));
        shape.origin = new Vector3(Random.value * 50, Random.value * 50, Random.value * 50);
        shape.firstPoint = Vector3.zero;
        shape.secondPoint = Vector3.zero;
        shape.rounding = 0f;
        Color color = Random.ColorHSV();
        shape.colour = new Vector3(color.r, color.g, color.b);
        shape.operation = 0;
        shape.blendStrength = 0;

        return shape;
    }

    Shape CreateLine(){
        Shape shape = new Shape();
        shape.shapeType = 2;
        shape.size = Vector3.zero;
        shape.origin = new Vector3(Random.value * 50, Random.value * 50, Random.value * 50);
        shape.firstPoint = new Vector3(Random.value * (lineLength.y - lineLength.x), Random.value * (lineLength.y - lineLength.x), Random.value * (lineLength.y - lineLength.x));
        shape.secondPoint = new Vector3(Random.value * (lineLength.y - lineLength.x), Random.value * (lineLength.y - lineLength.x), Random.value * (lineLength.y - lineLength.x));
        shape.rounding = 0.2f;
        Color color = Random.ColorHSV();
        shape.colour = new Vector3(color.r, color.g, color.b);
        shape.operation = 0;
        shape.blendStrength = 0;

        return shape;
    }

    void SetUpScene(){
        List<Shape> shapes= new List<Shape>();

        Shape floor = CreateFloor();
        shapes.Add(floor);
        
        for (int i = 0; i < shapeCount; i++)
        {
            int shapeTypeSelector = Random.Range(0, 3);
            Shape shape;
            if (shapeTypeSelector == 0){
                shape = CreateSphere();
            } else if (shapeTypeSelector == 1){
                shape = CreateCube();
            } else { // (shapeTypeSelector == 2)
                shape = CreateLine();
            }

            shapes.Add(shape);
        }



        _ShapeBuffer = new ComputeBuffer(shapes.Count, 76);
        _ShapeBuffer.SetData(shapes);
    }
    private void OnEnable() {
        if (_ShapeBuffer != null){
            _ShapeBuffer.SetCounterValue(0);
        }
        SetUpScene();
    }
    private void OnDisable() {
        if (_ShapeBuffer != null){
            _ShapeBuffer.Release();
        }
    }
    private void Awake(){
        _camera = GetComponent<Camera>();
        transformsWatching.Add(transform);
        transformsWatching.Add(directionalLight.transform);
    }

    void Update(){
        foreach(Transform transformSelected in transformsWatching){
            if(transformSelected.hasChanged){
                transformSelected.hasChanged = false;
            }
        }
    }

    private void InitRenderTexture(){
        if (target == null || target.width != Screen.width || target.height != Screen.height){
            if (target != null){
                target.Release();
            }
            
            target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }
    }

    private void Render(RenderTexture src, RenderTexture dest){
        InitRenderTexture();

        //RayMarchingShader.SetTexture(0, "Source", src);
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
            Render(src, dest);
        }
    }


    private void SetShaderParameters(){
        RayMarchingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayMarchingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayMarchingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        Vector3 lightDirection = directionalLight.transform.forward;
        RayMarchingShader.SetVector("_DirectionalLight", new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, directionalLight.intensity));
        RayMarchingShader.SetBuffer(0, "_Shapes", _ShapeBuffer);
    }
}