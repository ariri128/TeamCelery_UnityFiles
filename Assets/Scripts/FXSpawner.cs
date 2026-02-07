using UnityEngine;

public class FXSpawner : MonoBehaviour
{
    public ParticleSystem popPrefab;

    public void Spawn(Vector3 pos)
    {
        if (popPrefab == null) return;

        ParticleSystem ps = Instantiate(popPrefab, pos, Quaternion.identity);
        ps.Play();
        Destroy(ps.gameObject, 0.2f);
    }
}
