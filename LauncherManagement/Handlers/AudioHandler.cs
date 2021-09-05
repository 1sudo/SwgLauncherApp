// using NAudio.Wave;
using System.IO;

namespace LauncherManagement
{
    /*
    public class AudioHandler
    {
        public WaveOutEvent outputDevice;
        public AudioFileReader audioFile;

        public void PlayHoverSound()
        {
            if (outputDevice == null)
            {
                outputDevice = new WaveOutEvent();
                outputDevice.PlaybackStopped += OnPlaybackStopped;
            }
            if (audioFile == null)
            {
                audioFile = new AudioFileReader(Path.Join(Directory.GetCurrentDirectory(), "audio/select.wav"));
                outputDevice.Init(audioFile);
            }
            outputDevice.Volume = 0.35f;
            outputDevice.Play();
        }

        public void PlayClickSound()
        {
            if (outputDevice == null)
            {
                outputDevice = new WaveOutEvent();
                outputDevice.PlaybackStopped += OnPlaybackStopped;
            }
            if (audioFile == null)
            {
                audioFile = new AudioFileReader(Path.Join(Directory.GetCurrentDirectory(), "audio/click.wav"));
                outputDevice.Init(audioFile);
            }
            outputDevice.Volume = 0.35f;
            outputDevice.Play();
        }

        public void OnPlaybackStopped(object sender, StoppedEventArgs args)
        {
            try
            {
                outputDevice.Dispose();
                outputDevice = null;
                audioFile.Dispose();
                audioFile = null;
            }
            catch
            { }
        }
    }
    */
}
