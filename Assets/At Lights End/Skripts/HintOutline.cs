using UnityEngine;
using UnityEngine.Rendering;

namespace AtLightsEnd
{
    /// <summary>Built-in pipeline silhouette outline. Does not alter target materials or bounds.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class HintOutline : MonoBehaviour
    {
        [SerializeField] private Shader outlineShader;
        [SerializeField] private Color outlineColor = new Color(1f, 0.72f, 0.16f, 1f);
        [SerializeField, Range(1f, 10f)] private float widthPixels = 2f;
        private Camera view;
        private Material material;
        private CommandBuffer commands;
        private Renderer[] renderers = new Renderer[0];
        public InspectableObject Target { get; private set; }

        private void OnEnable()
        {
            view = GetComponent<Camera>();
            if (outlineShader == null) return;
            material = new Material(outlineShader) { hideFlags = HideFlags.HideAndDontSave };
            commands = new CommandBuffer { name = "Single-object hint outline" };
            view.AddCommandBuffer(CameraEvent.BeforeImageEffects, commands);
        }

        public void SetTarget(InspectableObject target)
        {
            Target = target;
            renderers = target == null ? new Renderer[0] : target.GetComponentsInChildren<Renderer>();
            if (commands != null) commands.Clear();
        }

        private void LateUpdate()
        {
            if (commands == null) return;
            commands.Clear();
            if (Target == null || !Target.isActiveAndEnabled) return;
            material.SetColor("_OutlineColor", outlineColor);
            material.SetFloat("_WidthPixels", widthPixels);
            // Mask all submeshes first so overlapping parts share one silhouette.
            for (int pass = 0; pass < 2; pass++)
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    int submeshes = 0;
                    if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                        submeshes = skinned.sharedMesh.subMeshCount;
                    else if (renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                        submeshes = filter.sharedMesh.subMeshCount;
                    for (int submesh = 0; submesh < submeshes; submesh++)
                        commands.DrawRenderer(renderer, material, submesh, pass);
                }
        }

        private void OnDisable()
        {
            Target = null;
            if (commands != null)
            {
                if (view != null) view.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, commands);
                commands.Release();
                commands = null;
            }
            if (material != null) Destroy(material);
        }
    }
}
