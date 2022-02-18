using UnityEngine;

namespace MagicaVoxelTools
{
    public class VoxelParticleDebug : MonoBehaviour
    {
        private VoxelParticle voxParticle = null;
        private VoxelParticleExplode voxExplode = null;

        private void Start()
        {
            voxParticle = GetComponent<VoxelParticle>();
            voxExplode = GetComponent<VoxelParticleExplode>();
        }

        private void OnGUI()
        {
            if (voxParticle == null) return;

            if (GUILayout.Button("Reset")) voxParticle.ResetParticles();

            if (voxExplode)
            {
                if (GUILayout.Button("Explode")) voxExplode.Explode();
            }
        }
    }
}