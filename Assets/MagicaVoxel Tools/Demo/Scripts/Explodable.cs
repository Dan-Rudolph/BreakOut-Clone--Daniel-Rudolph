using UnityEngine;

namespace MagicaVoxelTools
{
    public class Explodable : MonoBehaviour
    {
        public GameObject prefab;

        public void Explode()
        {
            if (prefab != null)
            {
                GameObject vp = Instantiate(prefab, transform.position, transform.rotation);
                VoxelParticleExplode vpe = vp.GetComponent<VoxelParticleExplode>();
                if (vpe != null) vpe.Explode();
            }

            Destroy(this.gameObject);
        }
    }
}