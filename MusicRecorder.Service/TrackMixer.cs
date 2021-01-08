using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicRecorder.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicRecorder.Service
{
    public class TrackMixer : ITrackMixer
    {
        private IAudioPlayback _audioPlayback;

        public TrackMixer(IAudioPlayback audioPlayback)
        {
            _audioPlayback = audioPlayback;
        }
        public async Task MixAudioTracks(List<byte[]> audioTracks)
        {
            var audioTrackShortList = new List<short[]>();

            foreach (var audioTrack in audioTracks)
            {
                var audioTrackShort = new short[(int)Math.Ceiling((double)audioTrack.Length / 2)];
                Buffer.BlockCopy(audioTrack, 0, audioTrackShort, 0, audioTrack.Length);

                audioTrackShortList.Add(audioTrackShort);
            }

            var output = new short[audioTrackShortList.Max(x => x.Length)];

            for (int i = 0; i < output.Length; i++)
            {
                float mixed = MixTracks(audioTrackShortList, i);

                // reduce the volume a bit:
                // mixed *= (float)0.8;
                // hard clipping
                if (mixed > 1.0f) mixed = 1.0f;
                if (mixed < -1.0f) mixed = -1.0f;
                short outputSample = (short)(mixed * 32768.0f);

                output[i] = outputSample;
            }

            await _audioPlayback.PlayAudioTrack(output);
        }

        private float MixTracks(List<short[]> audioTrackShortList, int i)
        {
            float mixed = 0;

            foreach (var audioTrack in audioTrackShortList)
            {
                if (i < audioTrack.Length)
                {
                    mixed += (audioTrack[i] / 32768.0f);
                }
            }

            return mixed;
        }
    }
}