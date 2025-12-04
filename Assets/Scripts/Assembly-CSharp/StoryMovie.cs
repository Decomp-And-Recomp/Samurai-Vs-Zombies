using UnityEngine;
using UnityEngine.Video;

public class StoryMovie : MonoBehaviour
{
	private int mStaggeredOperationsPerUpdates;

    public VideoPlayer videoPlayer;

    bool isPlaying;

    private void Update()
	{
		mStaggeredOperationsPerUpdates++;
		switch (mStaggeredOperationsPerUpdates)
		{
		case 4:
			NUF.AllowPortrait(false);
			break;
		case 8:
			StartMovie();
			break;
		case 15:
			NUF.AllowPortrait(true);
			break;
		case 21:
			//Application.LoadLevel("MainMenu");
			break;
		}

        if (!isPlaying)
        {
            if (videoPlayer.isPlaying) isPlaying = true;
            return;
        }
		if (!videoPlayer.isPlaying || Input.GetMouseButton(0) || Input.GetKeyDown(KeyCode.Space))
		{
			Application.LoadLevel("MainMenu");
		}
    }

	private void StartMovie()
	{
		string text = "SvZRough.mp4";
		Debug.Log("Playing movie: " + text);
		//Handheld.PlayFullScreenMovie(text, Color.black, FullScreenMovieControlMode.CancelOnTouch, FullScreenMovieScalingMode.AspectFit);
		videoPlayer.Play();
	}

#if UNITY_EDITOR
	void OnApplicationQuit()
    {
        videoPlayer.targetTexture.Release();
    }
#endif
}
