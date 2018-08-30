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
        byte[] audioBuffer;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            layout = FindViewById<LinearLayout>(Resource.Layout.Main);
            Button startRecordingButton = FindViewById<Button>(Resource.Id.btnStartRecording);
            Button stopRecordingButton = FindViewById<Button>(Resource.Id.btnStopRecording);
            Button playbackButton = FindViewById<Button>(Resource.Id.btnPlayback);

            startRecordingButton.Click += async (sender, e) => await RequestRecordAudioPermission();
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

        async Task StartRecordingButton_Click(object sender, EventArgs e)
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

        private async Task RecordAudio()
        {

            //var bufferSize = AudioRecord.GetMinBufferSize(11025, ChannelIn.Mono, Encoding.Pcm16bit);

            //audioRecorder = new AudioRecord(
            //  // Hardware source of recording.
            //  AudioSource.Mic,
            //  // Frequency
            //  11025,
            //  // Mono or stereo
            //  ChannelIn.Mono,
            //  // Audio encoding
            //  Android.Media.Encoding.Pcm16bit,
            //  // Length of the audio clip.
            //  bufferSize
            //);

            audioRecorder = FindAudioRecord();
            //if (audioRecorder.State == State.Uninitialized)
                //return;

            audioBuffer = new byte[audioRecorder.BufferSizeInFrames];
            audioRecorder.StartRecording();

            while (true)
            {
                try
                {
                    // Keep reading the buffer while there is audio input.
                    await audioRecorder.ReadAsync(audioBuffer, 0, audioBuffer.Length);
                    
                    // Write out the audio file.
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.Message);
                    break;
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
                            await RecordAudio();
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
                await RecordAudio();
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

