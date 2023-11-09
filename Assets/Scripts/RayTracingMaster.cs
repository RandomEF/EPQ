using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    private RenderTexture target;
    public Texture SkyboxTexture;
    private Camera _camera;
    private uint _currentSample = 0;
    private Material _addMaterial;

    private void Awake(){
        _camera = GetComponent<Camera>();
    }
    
    private void Update() {
        if (transform.hasChanged){
            _currentSample = 0;
            transform.hasChanged = false;
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
    }
}
