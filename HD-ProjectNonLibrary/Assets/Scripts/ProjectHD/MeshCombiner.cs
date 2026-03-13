using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD
{
    public class MeshCombiner : MonoBehaviour
    {
        public GameObject Parent;
        public Material Material;
        public bool DeactivateParentAfterMerge = false;
        public bool DestroyParentAfterMerge = false;
        public bool DeactivateOriginMesheRenderersAfterMerge = true;

        public string AssetSavePath = "Assets/GameSource/CombinedMeshes/";

        [Button("Merge Meshes")]
        public void MergeMeshes()
        {
            MeshFilter[] meshFilters = Parent.GetComponentsInChildren<MeshFilter>();
            List<CombineInstance> combineList = new List<CombineInstance>();

            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (meshFilters[i].sharedMesh != null)
                {
                    CombineInstance combineInstance = new CombineInstance
                    {
                        mesh = meshFilters[i].sharedMesh,
                        transform = meshFilters[i].transform.localToWorldMatrix,
                    };
                    combineList.Add(combineInstance);
                }
            }

            //GameObject combinedObject = new GameObject("Combined Mesh");
            this.gameObject.AddComponent<MeshFilter>();
            var meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
            var newMesh = new Mesh();
            newMesh.CombineMeshes(combineList.ToArray());
            this.gameObject.GetComponent<MeshFilter>().sharedMesh = newMesh;
            this.gameObject.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combineList.ToArray());
            this.gameObject.GetComponent<MeshRenderer>().material = Material;

            if (DeactivateOriginMesheRenderersAfterMerge)
            {
                MeshRenderer[] meshRenderers = Parent.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer renderer in meshRenderers)
                {
                    renderer.enabled = false;
                }
            }

            meshRenderer.enabled = true;

            if (DeactivateParentAfterMerge)
            {
                Parent.SetActive(false);
            }

            if (DestroyParentAfterMerge)
            {
                Destroy(Parent);
            }

#if UNITY_EDITOR
            SaveCombinedMesh(newMesh, AssetSavePath);
#endif
        }

        public void SaveCombinedMesh(Mesh mesh, string path)
        {
#if UNITY_EDITOR
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            }

            string assetPath = path + name + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(mesh, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
    }
}