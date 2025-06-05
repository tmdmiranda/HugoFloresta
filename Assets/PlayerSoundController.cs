using UnityEngine;

public class PlayerSoundController : MonoBehaviour
{
    private AudioSource Walk;
    private PlayerInputHandler inputHandler;

    void Start()
    {
        Walk = GetComponent<AudioSource>();
        inputHandler = GetComponent<PlayerInputHandler>();

        if (Walk != null)
        {
            Walk.Stop();
        }
    }    void Update()
    {
        if (Walk != null && inputHandler != null)
        {
            // Se houver input de movimento (andar ou sprintar)
            if (inputHandler.MovementInput.magnitude > 0.1f)
            {
                if (!Walk.isPlaying)
                {
                    Walk.Play();
                }
            }
            else
            {
                if (Walk.isPlaying)
                {
                    Walk.Stop();
                }
            }
        }
    }
}
