using Android.Media;
using MusicRecorder.Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace MusicRecorder.Service
{
    public class AudioPlayback : IAudioPlayback
    {
        private AudioTrack audioTrack;

        private int rate = 44100;
        private Encoding audioEncoding = Encoding.Pcm16bit;

        public async Task PlayAudioTrack(byte[] audioBuffer)
        {
            AudioTrackStop();

            var attributes = new AudioAttributes.Builder().SetUsage(AudioUsageKind.Media).SetContentType(AudioContentType.Speech).Build();

            audioTrack = new AudioTrack(attributes, new AudioFormat.Builder()
                    .SetEncoding(audioEncoding)
                    .SetChannelMask(ChannelOut.Stereo)
                    .SetSampleRate(rate).Build()
                , audioBuffer.Length,
                AudioTrackMode.Stream,
                1);

            audioTrack.Play();
            await audioTrack.WriteAsync(audioBuffer, 0, audioBuffer.Length);
        }

        public async Task PlayAudioTrack(short[] audioBuffer)
        {
            AudioTrackStop();

            var attributes = new AudioAttributes.Builder().SetUsage(AudioUsageKind.Media).SetContentType(AudioContentType.Speech).Build();

            audioTrack = new AudioTrack(attributes, new AudioFormat.Builder()
                .SetEncoding(audioEncoding)
                .SetChannelMask(ChannelOut.Stereo)
                .SetSampleRate(rate).Build()
                , audioBuffer.Length,
                AudioTrackMode.Stream,
                1);

            audioTrack.Play();
            await audioTrack.WriteAsync(audioBuffer, 0, audioBuffer.Length);
        }

        private void AudioTrackStop()
        {
            if (audioTrack != null)
            {
                audioTrack.Stop();
                audioTrack.Release();
                audioTrack = null;
            }
        }
    }
}
