using System.Collections.Generic;
using UnityEngine;

namespace MagicaVoxelTools
{
    public class VoxelParticle : MonoBehaviour
    {
        public float scale = .1f;
        public bool removeHidden = true;
        public bool autoDelete = true;
        public bool center = false;

        public ParticleSystem particleSystem;
        public VoxelData voxelData;

        //[HideInInspector]
        //public VoxelData3D voxelData3D;

        [HideInInspector]
        public ParticleSystem.Particle[] voxelParticles;

        public bool Valid
        {
            get
            {
                if (particleSystem == null) return false;
                if (voxelData == null) return false;
                return true;
            }
        }

        private void Awake()
        {
            if (!Valid) return;
            //voxelData3D = new VoxelData3D(voxelData);
            SetupParticles();
        }

        private void Update()
        {
            if (!Valid) return;
            if (!autoDelete) return;

            if (particleSystem.particleCount == 0)
            {
                Destroy(this.gameObject);
            }
        }

        public void ResetParticles()
        {
            if (!Valid) return;

            particleSystem.gravityModifier = 0;

            for (int i = 0; i < voxelParticles.Length; i++)
            {
                voxelParticles[i].startLifetime = float.PositiveInfinity;
                voxelParticles[i].remainingLifetime = float.PositiveInfinity;
                voxelParticles[i].velocity = Vector3.zero;
            }

            UpdateVoxels();
        }

        private void SetupParticles()
        {
            particleSystem.Clear(true);
            particleSystem.gravityModifier = 0;

            Vector3 offset = voxelData.pivot;

            if (center)
            {
                offset.x = (float)voxelData.width / 2f;
                offset.y = (float)voxelData.height / 2f;
                offset.z = (float)voxelData.depth / 2f;
            }

            List<ParticleSystem.Particle> voxs = new List<ParticleSystem.Particle>();

            for (int x = 0; x < voxelData.width; x++)
            {
                for (int y = 0; y < voxelData.height; y++)
                {
                    for (int z = 0; z < voxelData.depth; z++)
                    {
                        if (voxelData.FilledVoxel(x, y, z))
                        {
                            if (removeHidden && !voxelData.IsVisible(x, y, z)) continue;

                            Vector3 pos = new Vector3(x, y, z);
                            pos -= offset - new Vector3(.5f, .5f, .5f);
                            pos *= scale;
                            //pos += this.transform.root.position;

                            ParticleSystem.Particle p = new ParticleSystem.Particle
                            {
                                startLifetime = float.PositiveInfinity,
                                remainingLifetime = float.PositiveInfinity,

                                startSize = scale,
                                startColor = voxelData.VoxelColor(x, y, z),

                                position = pos,
                                velocity = Vector3.zero
                            };

                            voxs.Add(p);
                        }
                    }
                }
            }

            voxelParticles = voxs.ToArray();
            UpdateVoxels();
        }

        public void UpdateVoxels()
        {
            if (!Valid) return;
            particleSystem.SetParticles(voxelParticles, voxelParticles.Length);
        }
    }
}