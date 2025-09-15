using UnityEngine;

public class MusicRandomizer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] songs;

    private void Awake()
    {
        PlayRandomSong();
    }
    private void Update()
    {
        // Check if the song finished
        if (!audioSource.isPlaying)
        {
            PlayRandomSong();
        }
    }

    public void PlayRandomSong()
    {
        if (songs.Length == 0 || audioSource == null) return;

        int randomIndex = Random.Range(0, songs.Length);
        audioSource.clip = songs[randomIndex];
        audioSource.Play();
    }
   
}
