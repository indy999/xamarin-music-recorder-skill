using Android.App;
using Android.Widget;
using Android.OS;
using Android.Media;
using System;
using Android;
using Android.Content.PM;
using Android.Support.Design.Widget;
using System.Threading.Tasks;

namespace MusicRecorder
{
    [Activity(Label = "MusicRecorder", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private AudioTrack audioTrack;
        private AudioRecord audioRecorder;
        readonly string[] PermissionsAudio =
            {
              Manifest.Permission.RecordAudio         
            };

        const int RequestLocationId = 0;
        Android.Views.View layout;
        byte[] audioRecordBuffer;
        byte[] audioTrackRecordBuffer;

        private static  int SAMPLERATE = 8000;
        private static ChannelIn RECORDER_CHANNELS = ChannelIn.Mono;
        private static ChannelOut TRACK_CHANNELS = ChannelOut.Mono;
        private static  Encoding AUDIO_ENCODING = Android.Media.Encoding.Pcm16bit;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            layout = FindViewById<LinearLayout>(Resource.Layout.Main);
            Button startRecordingButton = FindViewById<Button>(Resource.Id.btnStartRecording);
            Button stopRecordingButton = FindViewById<Button>(Resource.Id.btnStopRecording);
            Button playbackButton = FindViewById<Button>(Resource.Id.btnPlayback);

            startRecordingButton.Click += async (sender, e) => await StartRecordingButton_Click();
            stopRecordingButton.Click += StopRecordingButton_Click;
            playbackButton.Click += PlaybackButton_Click;

        }

        private void PlaybackButton_Click(object sender, EventArgs e)
        {
            // PlayAudioTrack(audioRecorder.)
        }

        private void StopRecordingButton_Click(object sender, EventArgs e)
        {
            StopRecording();
        }

        async Task StartRecordingButton_Click()
        {
            await RequestRecordAudioPermission();
        }

        private void StopRecording()
        {
            audioRecorder.Stop();
        }

        private void PlayAudioTrack(byte[] audioBuffer)
        {
            audioTrack = new AudioTrack(
             // Stream type
             Stream.Music,
             // Frequency
             11025,
             // Mono or stereo
             ChannelOut.Mono,
             // Audio encoding
             Android.Media.Encoding.Pcm16bit,
             // Length of the audio clip.
             audioBuffer.Length,
             // Mode. Stream or static.
             AudioTrackMode.Stream);

            audioTrack.Play();
            audioTrack.Write(audioBuffer, 0, audioBuffer.Length);
        }

        private async Task RecordAudio(bool isRunning)
        {
            if (isRunning)
            {
                audioRecorder = FindAudioRecord();
                if (audioRecorder == null || audioRecorder.State == State.Uninitialized)
                {
                    return;
                }

                audioRecordBuffer = new byte[audioRecorder.BufferSizeInFrames];
                audioTrackRecordBuffer = new byte[audioRecorder.BufferSizeInFrames];

                audioTrack = FindAudioTrack(audioTrack);
                if (audioTrack == null)
                {
                    return;
                }

                audioTrack.SetPlaybackRate(SAMPLERATE);

                audioTrackRecordBuffer = new byte[audioRecorder.BufferSizeInFrames];

                audioRecorder.StartRecording();
                audioTrack.Play();
                var minbufferSizeRecorder = audioRecorder.BufferSizeInFrames;

                //if the audio recording doesnt work change the buffers to short[] and divide by 2 as in example
                while (isRunning)
                {
                    try
                    {
                        // Keep reading the buffer while there is audio input.
                        await audioRecorder.ReadAsync(audioRecordBuffer, 0, minbufferSizeRecorder);

                        for (int i = 0; i < audioTrackRecordBuffer.Length; i++)
                        {
                            audioTrackRecordBuffer[i] = audioRecordBuffer[i];
                        }

                        await audioTrack.WriteAsync(audioTrackRecordBuffer, 0, audioTrackRecordBuffer.Length);
                        audioRecordBuffer = new byte[minbufferSizeRecorder];
                        audioTrackRecordBuffer = new byte[minbufferSizeRecorder];

                        // Write out the audio file.
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine(ex.Message);
                        break;
                    }
                }
            }
        }

        private static int[] mSampleRates = new int[] { 8000, 11025, 22050, 44100 };
        private static int[] audioFormats = new int[] { (int)Encoding.Pcm8bit, (int)Encoding.Pcm16bit };
        private static int[] channelConfigs = new int[] { (int)ChannelIn.Mono, (int)ChannelIn.Stereo };

        public AudioRecord FindAudioRecord()
        {

            foreach (int rate in mSampleRates)
            {
                foreach (short audioFormat in audioFormats)
                {
                    foreach (int channelConfig in channelConfigs)
                    {
                        try
                        {
                            int bufferSize = AudioRecord.GetMinBufferSize(rate, (ChannelIn)channelConfig, (Encoding)audioFormat);

                            if (bufferSize != (int)Android.Media.TrackStatus.ErrorBadValue)
                            {
                                AudioRecord recorder = new AudioRecord(AudioSource.Mic, rate, (ChannelIn)channelConfig, (Encoding)audioFormat, bufferSize);

                                if (recorder.State == State.Initialized)
                                    return recorder;
                            }
                        }
                        catch (Exception e)
                        {
                           // Log.e(TAG, rate + "Exception, keep trying.", e);
                        }
                    }
                }
            }
            return null;
        }

        public AudioTrack FindAudioTrack(AudioTrack audioTrack)
        {
            int bufferSize = AudioTrack.GetMinBufferSize(SAMPLERATE, TRACK_CHANNELS, AUDIO_ENCODING);
            if (bufferSize != (int)Android.Media.TrackStatus.ErrorBadValue)
            {
                audioTrack = new AudioTrack(Stream.Music,
                                 SAMPLERATE,
                                 TRACK_CHANNELS,
                                 AUDIO_ENCODING,
                                 bufferSize,
                                 AudioTrackMode.Stream);

                audioTrack.SetPlaybackRate(SAMPLERATE);

                if (audioTrack.State == AudioTrackState.Uninitialized)
                {                    
                    return null;
                }
            }
            return audioTrack;
        }

        public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            switch (requestCode)
            {
                case RequestLocationId:
                    {
                        if (grantResults[0] == Permission.Granted)
                        {
                            ////Permission granted
                            //var snack = Snackbar.Make(layout, "Location permission is available, getting lat/long.", Snackbar.LengthShort);
                            //snack.Show();

                            //await GetLocationAsync();
                            await RecordAudio(true);
                        }
                        else
                        {
                            //Permission Denied :(
                            //Disabling location functionality
                            var snack = Snackbar.Make(layout, "Audio permission is denied.", Snackbar.LengthShort);
                            snack.Show();
                        }
                    }
                    break;
            }
        }

         async Task RequestRecordAudioPermission()
        {

            string permission = PermissionsAudio[0];

            if (CheckSelfPermission(permission) == Android.Content.PM.Permission.Granted)
            {
                await RecordAudio(true);
                return;
            }

            if (ShouldShowRequestPermissionRationale(permission))
            {
                //Explain to the user why we need to read the contacts
                Snackbar.Make(layout, "Permission to record audio required", Snackbar.LengthIndefinite)
                        .SetAction("OK", v => RequestPermissions(PermissionsAudio, RequestLocationId))
                        .Show();
                return;
            }

            RequestPermissions(PermissionsAudio, RequestLocationId);
        }
    }
}

