using UnityEngine;

namespace MagicaVoxelTools
{
    public class ClickToExplode : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    Explodable exp = hit.collider.gameObject.GetComponent<Explodable>();
                    if (exp != null) exp.Explode();
                }
            }
        }
    }
}