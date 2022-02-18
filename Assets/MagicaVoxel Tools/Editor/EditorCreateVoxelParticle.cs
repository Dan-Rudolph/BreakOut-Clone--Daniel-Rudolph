using UnityEditor;
using UnityEngine;

namespace MagicaVoxelTools
{
    public class EditorCreateVoxelParticle
    {
        [MenuItem("GameObject/Voxel Particle", false, 0)]
        public static void CreateVoxelParticle()
        {
            GameObject obj = new GameObject("Voxel Particle");
            VoxelParticle vp = obj.AddComponent<VoxelParticle>();

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            vp.particleSystem = ps;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = false;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;

            GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            renderer.mesh = t.GetComponent<MeshFilter>().sharedMesh;
            renderer.material = Resources.Load<Material>("Materials/DefaultVoxelParticleMat");
            renderer.alignment = ParticleSystemRenderSpace.Local;
            GameObject.DestroyImmediate(t);
        }
    }
}