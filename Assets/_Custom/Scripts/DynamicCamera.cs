using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[DefaultExecutionOrder(100)]
public class DynamicCamera : MonoBehaviour
{
    [Header("Cinemachine Cameras (Opcional - auto-busca por nombre si está vacío)")]
    [Tooltip("Cámara principal. Si está vacía, se busca por nombre.")]
    public CinemachineCamera mainCamera;
    [Tooltip("Cámara de zoom para Finisher. Si está vacía, se busca por nombre.")]
    public CinemachineCamera finisherCamera;

    [Header("Búsqueda automática por nombre")]
    [Tooltip("Si no hay cámaras asignadas, las busca en la escena por nombre.")]
    public bool autoFindCameras = true;
    public string mainCameraName = "MainVCam";
    public string finisherCameraName = "FinisherVCam";

    [Header("Prioridades de Cinemachine")]
    [Tooltip("Prioridad de la cámara principal por defecto (mayor = más prioridad).")]
    public int defaultMainPriority = 10;
    [Tooltip("Prioridad de la cámara de Finisher cuando NO está activa.")]
    public int defaultFinisherPriority = 5;
    [Tooltip("Prioridad de la cámara de Finisher cuando SÍ está activa (debe ser MAYOR que la principal).")]
    public int activeFinisherPriority = 20;

    [Header("Tiempos")]
    [Tooltip("Duración del zoom en segundos reales (no afectados por timeScale).")]
    public float zoomDuration = 0.15f;

    [Header("Debug")]
    [Tooltip("Muestra en consola qué cámaras encontró.")]
    public bool logCameraSetup = true;

    private Coroutine zoomCoroutine;

    void OnEnable()
    {
        GameEvents.OnFinisherPerformed += TriggerFinisherZoom;
    }

    void OnDisable()
    {
        GameEvents.OnFinisherPerformed -= TriggerFinisherZoom;
    }

    void Start()
    {
        ResolveCameras();

        // Configurar prioridades iniciales
        if (mainCamera != null)
        {
            mainCamera.Priority = defaultMainPriority;
            if (logCameraSetup) Debug.Log($"[DynamicCamera] MainVCam prioridad inicial: {defaultMainPriority}");
        }
        if (finisherCamera != null)
        {
            finisherCamera.Priority = defaultFinisherPriority;
            if (logCameraSetup) Debug.Log($"[DynamicCamera] FinisherVCam prioridad inicial: {defaultFinisherPriority}");
        }
        else if (logCameraSetup)
        {
            Debug.LogWarning($"[DynamicCamera] No se encontró '{finisherCameraName}'. El zoom del Finisher no funcionará hasta que la crees.");
        }
    }

    private void ResolveCameras()
    {
        if (!autoFindCameras) return;

        if (mainCamera == null)
        {
            GameObject mainCamObj = GameObject.Find(mainCameraName);
            if (mainCamObj != null)
            {
                mainCamera = mainCamObj.GetComponent<CinemachineCamera>();
                if (mainCamera == null)
                {
                    Debug.LogError($"[DynamicCamera] '{mainCameraName}' existe pero NO tiene componente CinemachineCamera.");
                }
            }
            else
            {
                Debug.LogError($"[DynamicCamera] No se encontró ningún GameObject llamado '{mainCameraName}'.");
            }
        }

        if (finisherCamera == null)
        {
            GameObject finisherCamObj = GameObject.Find(finisherCameraName);
            if (finisherCamObj != null)
            {
                finisherCamera = finisherCamObj.GetComponent<CinemachineCamera>();
            }
        }
    }

    private void TriggerFinisherZoom()
    {
        if (finisherCamera == null)
        {
            // Re-intentar la búsqueda por si la cámara se creó dinámicamente
            ResolveCameras();
            if (finisherCamera == null) return;
        }

        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(ZoomRoutine());
    }

    private IEnumerator ZoomRoutine()
    {
        // Subir prioridad de la cámara de Finisher (ahora toma el control)
        finisherCamera.Priority = activeFinisherPriority;

        // Esperar en tiempo real (no afectado por timeScale del slow-motion)
        yield return new WaitForSecondsRealtime(zoomDuration);

        // Restaurar prioridad para devolver el control a la MainVCam
        finisherCamera.Priority = defaultFinisherPriority;
        zoomCoroutine = null;
    }
}
