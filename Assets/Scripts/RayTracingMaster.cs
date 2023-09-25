using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    private RenderTexture target;
    public Texture SkyboxTexture;

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

    private void Render(RenderTexture dest){
        InitRenderTexture();

        RayTracingShader.SetTexture(0, "Result", target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        Graphics.Blit(target, dest);
    }
    
    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        SetShaderParameters();
        Render(dest);
    }

    private Camera _camera;
    
    private void Awake(){
        _camera = GetComponent<Camera>();
    }

    private void SetShaderParameters(){
        RayTracingShader.SetMatrix("CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "SkyboxTexture", SkyboxTexture);
    }
}
