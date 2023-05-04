
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[ExecuteInEditMode]
public class CustomAudioSource : MonoBehaviour
{
    private AudioSource audioSource;

    [SerializeField]
    [Tooltip("Min Distance")]
    [Range(1f, 500f)]
    private float minDistance = 1f;

    public float MinDistance
    {
        get { return minDistance; }
        set
        {
            minDistance = value;
            audioSource.minDistance = minDistance;
        }
    }

    [SerializeField]
    [Tooltip("Max Distance")]
    [Range(1f, 500f)]
    private float maxDistance = 50f;

    public float MaxDistance
    {
        get { return maxDistance; }
        set
        {
            maxDistance = value;
            audioSource.maxDistance = maxDistance;
        }
    }

    [SerializeField]
    [Tooltip("Spread")]
    [Range(0f, 360f)]
    private float spread = 0f;

    public float Spread
    {
        get { return spread; }
        set
        {
            spread = value;
            audioSource.spread = spread;
        }
    }

    [SerializeField]
    [Tooltip("Volume")]
    [Range(0f, 1f)]
    private float volume = 1f;

    public float Volume
    {
        get { return volume; }
        set
        {
            volume = value;
            audioSource.volume = volume;
        }
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.spread = spread;
        audioSource.volume = volume;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw max and min distance spheres
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, maxDistance);

        Gizmos.color = new Color(0f, 0f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, minDistance);

        // Draw spread cone
        Vector3 spreadVector = Quaternion.Euler(0f, 0f, -spread / 2f) * transform.right;
        Gizmos.color = new Color(1f, 1f, 1f, 0.80f);
        Gizmos.DrawRay(transform.position, spreadVector * maxDistance);

        Gizmos.color = new Color(1f, 1f, 1f, 0.80f);
        spreadVector = Quaternion.Euler(0f, 0f, spread / 2f) * transform.right;
        Gizmos.DrawRay(transform.position, spreadVector * maxDistance);

        // Draw sound direction arrow
        Gizmos.color = new Color(1f, 0f, 0f, 0.80f);
        Gizmos.DrawRay(transform.position, transform.forward * maxDistance);
    }


    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.spread = spread;
            audioSource.volume = volume;
            OnDrawGizmosSelected();
        }
    }
}