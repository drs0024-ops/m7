using UnityEngine;
using VContainer;
using MessagePipe;
using Game.Core.Enums;
using Game.Core.Messages;

namespace Game.Gameplay.Camera
{
    [RequireComponent(typeof(Collider2D))]
    public class CameraControlTrigger : MonoBehaviour
    {
        [SerializeField] private TriggerConfig _config;
        [SerializeField] private LayerMask _playerLayer;

        private Collider2D _coll;
        private IPublisher<CameraPanRequest> _panPublisher;
        private IPublisher<CameraSwitchRequest> _switchPublisher;

        [Inject]
        public void Construct(
            IPublisher<CameraPanRequest> panPublisher,
            IPublisher<CameraSwitchRequest> switchPublisher)
        {
            _panPublisher = panPublisher;
            _switchPublisher = switchPublisher;
        }

        private void Awake()
        {
            _coll = GetComponent<Collider2D>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!IsPlayer(collision)) return;

            if (_config.panCameraOnContact)
            {
                Vector2 offset = GetOffset(_config.panDirection, _config.panDistance);
                _panPublisher.Publish(new CameraPanRequest(offset, _config.panTime));
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!IsPlayer(collision)) return;

            Vector2 exitDir = (collision.transform.position - _coll.bounds.center).normalized;
            float dirX = exitDir.x;
            if (Mathf.Approximately(dirX, 0f)) dirX = 1f;

            if (_config.swapCameras && _config.switchData != null)
            {
                CameraMode target = dirX > 0 ? _config.switchData.cameraOnRight : _config.switchData.cameraOnLeft;
                _switchPublisher.Publish(new CameraSwitchRequest(target, exitDir));
            }

            if (_config.panCameraOnContact)
            {
                _panPublisher.Publish(new CameraPanRequest(Vector2.zero, _config.panTime));
            }
        }

        private bool IsPlayer(Collider2D collision)
        {
            return (_playerLayer.value & (1 << collision.gameObject.layer)) > 0;
        }

        private static Vector2 GetOffset(PanDirection dir, float distance)
        {
            return dir switch
            {
                PanDirection.Up => Vector2.up * distance,
                PanDirection.Down => Vector2.down * distance,
                PanDirection.Left => Vector2.left * distance,
                PanDirection.Right => Vector2.right * distance,
                _ => Vector2.zero
            };
        }
    }

    [System.Serializable]
    public class TriggerConfig
    {
        public bool swapCameras;
        public bool panCameraOnContact;

        [Header("Switch")]
        public CameraSwitchDataSO switchData;

        [Header("Pan")]
        public PanDirection panDirection;
        public float panDistance = 3f;
        public float panTime = 0.35f;
    }
}   



#region Prior version 7/8/2026
/*
using UnityEngine;
using Unity.Cinemachine;
using UnityEditor;

namespace CameraStuff
{
    public class CameraControlTrigger : MonoBehaviour {
    public CustomInspectorObjects customInspectorObjects;
    private Collider2D _coll;


    private void Start() 
    {
        _coll = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision) 
    {
        if (collision.CompareTag("Player"))
        {
            
            if (customInspectorObjects.panCameraOnContact)
            {
                //pan camera based on the pan direction in the inspector
                CameraManager.Instance.PanCameraOnContact(customInspectorObjects.panDistance, customInspectorObjects.panTime, customInspectorObjects.panDirection, false);
                Debug.Log("NOW PANING");
            }
            
        }
    }

    private void OnTriggerExit2D(Collider2D collision) 
    {
        if (!collision.CompareTag("Player"))
            return;

        Debug.Log("Player exited");

        Vector2 exitDirection =(collision.transform.position - _coll.bounds.center).normalized;

        if (customInspectorObjects.swapCameras && customInspectorObjects.cameraOnLeft != null && customInspectorObjects.cameraOnRight != null)
        {
            //swap cameras
            CameraManager.Instance.SwapCameras(customInspectorObjects.cameraOnLeft, customInspectorObjects.cameraOnRight, exitDirection);
        }
        if (customInspectorObjects.panCameraOnContact)
        {
            //pan camera based on the pan direction in the inspector
            CameraManager.Instance.PanCameraOnContact(customInspectorObjects.panDistance, customInspectorObjects.panTime, customInspectorObjects.panDirection, true);
        }
    }

}

    [System.Serializable]
    public class CustomInspectorObjects
    {
        public bool swapCameras = false;
        public bool panCameraOnContact = false;

        [HideInInspector] public CinemachineCamera cameraOnLeft;
        [HideInInspector] public CinemachineCamera cameraOnRight;

        [HideInInspector] public PanDirection panDirection;
        [HideInInspector] public float panDistance = 3f;
        [HideInInspector] public float panTime = 0.35f;
    }

    public enum PanDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [CustomEditor(typeof(CameraControlTrigger))]
    public class MyScriptEditor : Editor
    {
        CameraControlTrigger cameraControlTrigger;

        private void OnEnable() {
            cameraControlTrigger = (CameraControlTrigger)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (cameraControlTrigger.customInspectorObjects.swapCameras)
            {
                cameraControlTrigger.customInspectorObjects.cameraOnLeft = EditorGUILayout.ObjectField("Camera on Left", cameraControlTrigger.customInspectorObjects.cameraOnLeft, typeof(CinemachineCamera), true) as CinemachineCamera;
                cameraControlTrigger.customInspectorObjects.cameraOnRight = EditorGUILayout.ObjectField("Camera on Right", cameraControlTrigger.customInspectorObjects.cameraOnRight, typeof(CinemachineCamera), true) as CinemachineCamera;

            }

            if (cameraControlTrigger.customInspectorObjects.panCameraOnContact)
            {
                cameraControlTrigger.customInspectorObjects.panDirection = (PanDirection)EditorGUILayout.EnumPopup("Camera Pan Direction", cameraControlTrigger.customInspectorObjects.panDirection);
                cameraControlTrigger.customInspectorObjects.panDistance = EditorGUILayout.FloatField("Pan Distance", cameraControlTrigger.customInspectorObjects.panDistance);
                cameraControlTrigger.customInspectorObjects.panTime = EditorGUILayout.FloatField("Pan Time", cameraControlTrigger.customInspectorObjects.panTime);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(cameraControlTrigger);
            }
            
        }
    }
}
*/
#endregion
