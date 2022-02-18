using UnityEngine;

namespace MagicaVoxelTools
{
    [RequireComponent(typeof(VoxelParticle))]
    public class VoxelParticleExplode : MonoBehaviour
    {
        public bool autoExplode = true;
        public float speed = 5;
        public float life = 2;
        public float gravity = 0;
        public Vector3 direction = Vector3.zero;

        private void Start()
        {
            if (autoExplode) Explode();
        }

        public void Explode()
        {
            VoxelParticle voxParticle = GetComponent<VoxelParticle>();
            if (voxParticle == null) return;
            if (!voxParticle.Valid) return;

            voxParticle.particleSystem.gravityModifier = gravity;

            for (int i = 0; i < voxParticle.voxelParticles.Length; i++)
            {
                voxParticle.voxelParticles[i].startLifetime = life;
                voxParticle.voxelParticles[i].remainingLifetime = Random.Range(life / 2, life);
                voxParticle.voxelParticles[i].velocity = new Vector3(Random.Range(-speed, speed), Random.Range(-speed, speed), Random.Range(-speed, speed)) + direction;
            }

            voxParticle.UpdateVoxels();
        }
    }
}