using UnityEngine;

namespace ROMA2.Logic.Client.Controllers
{
    public class OutlineController : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private float _targetWidth = 0.02f;
        [SerializeField] private float _defaultWidth = 0.0001f;

        private Material _outlineMaterial;
        private static readonly int _OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        private static readonly int _Color = Shader.PropertyToID("_Color");

        private void Start()
        {
            if (skinnedMeshRenderer == null)
            {
                skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            }
            _outlineMaterial = skinnedMeshRenderer.materials[5];
        }

        public void ToggleOutline(bool isActive)
        {
            float currentWidth = isActive ? _targetWidth : _defaultWidth;
            _outlineMaterial.SetFloat(_OutlineWidth, currentWidth);
        }

        public void SetColor(Color color)
        {
            _outlineMaterial.SetColor(_Color, color);
        }
    }
}