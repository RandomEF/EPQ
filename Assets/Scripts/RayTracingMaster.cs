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

    private void Awake(){
        _camera = GetComponent<Camera>();
        transformsWatching.Add(transform);
        transformsWatching.Add(directionalLight.transform);
    }
    
    private void Update() {
        SetShaderParamsUpdate();
        if (Input.GetKeyDown(KeyCode.Equals)){
            maxBounceNumber++;
            transform.hasChanged = true;
        } else if (Input.GetKeyDown(KeyCode.Minus) && maxBounceNumber != 0){
            maxBounceNumber--;
            transform.hasChanged = true;
        }
        foreach (Transform transformSelected in transformsWatching){
            if (transformSelected.hasChanged || transform.hasChanged){
                _currentSample = 0;
                transform.hasChanged = false;
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
        SetShaderParameters();
        Render(dest);
    }

    private void SetShaderParameters(){
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        RayTracingShader.SetFloat("_maxBounceNumber", maxBounceNumber);
        Vector3 lightDirection = directionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, directionalLight.intensity));
    }

    private void SetShaderParamsUpdate(){
        RayTracingShader.SetFloat("_maxBounceNumber", maxBounceNumber);
    }
}
