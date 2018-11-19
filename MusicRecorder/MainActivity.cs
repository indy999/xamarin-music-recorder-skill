using Android.App;
using Android.Widget;
using Android.OS;
using Android.Media;
using System;
using Android;
using Android.Content.PM;
using Android.Support.Design.Widget;
using System.Threading.Tasks;
using System.IO;
using Android.Content.Res;
using System.Threading;

namespace MusicRecorder
{
    [Activity(Label = "MusicRecorder", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public Action<bool> RecordingStateChanged;

        static string filePath = "sample.wav";
        byte[] buffer = null;

        bool endRecording = false;
        bool isRecording = true;

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
            stopRecordingButton.Click += async (sender, e) => await StopRecordingButton_Click();
            playbackButton.Click += async (sender, e) => await PlaybackButton_Click(); ;

        }

        async Task PlaybackButton_Click()
        {
            AssetManager assets = this.Assets;
            long totalBytes = 37534;

            BinaryReader binaryReader = new BinaryReader(assets.Open("sample.wav"));

            audioBuffer = binaryReader.ReadBytes((Int32)totalBytes);

            binaryReader.Close();


            PlayAudioTrack(audioBuffer);
        }

        async Task StopRecordingButton_Click()
        {
            await StopRecording();
        }

        public Boolean IsRecording
        {
            get { return (isRecording); }
        }

        private void RaiseRecordingStateChangedEvent()
        {
            if (RecordingStateChanged != null)
                RecordingStateChanged(isRecording);
        }


        async Task StartRecordingButton_Click(object sender, EventArgs e)
        {
            Console.Out.WriteLine("START RECORDING EVENT");

            await RequestRecordAudioPermission();
        }

        async Task StopRecording()
        {


            await Task.Run(()=> { Console.Out.WriteLine("STOP RECORDING EVENT"); });
            //endRecording = true;
            //Thread.Sleep(500); // Give it time to drop out.


            //RecordAudio();
        }

        async Task PlayAudioTrack(byte[] audioBuffer)
        {
            var attributes = new AudioAttributes.Builder().SetUsage(AudioUsageKind.Media).SetContentType(AudioContentType.Movie).Build();

            audioTrack = new AudioTrack(attributes, new AudioFormat.Builder()
                .SetEncoding(Encoding.Pcm8bit)
                .SetChannelMask(ChannelOut.Mono)
                .SetSampleRate(11025).Build()
                , audioBuffer.Length,
                AudioTrackMode.Stream,
                1);
            //audioTrack = new AudioTrack(
            // // Stream type
            // Android.Media.Stream.Music,
            // // Frequency
            // 11025,
            // // Mono or stereo
            // ChannelOut.Mono,
            // // Audio encoding
            // Android.Media.Encoding.Pcm16bit,
            // // Length of the audio clip.
            // audioBuffer.Length,
            // // Mode. Stream or static.
            // AudioTrackMode.Stream);

            audioTrack.Play();
            await audioTrack.WriteAsync(audioBuffer, 0, audioBuffer.Length);
        }

        private async Task RecordAudio()
        {

            while (true)
            {
                if (endRecording)
                {
                    endRecording = false;
                    break;
                }

                try
                {
                    // Keep reading the buffer while there is audio input.

                    //await audioRecorder.ReadAsync(audioBuffer, 0, audioBuffer.Length);

                    // Write out the audio file.

                    await Console.Out.WriteLineAsync("RECORDING SOUND");   
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.Message);
                    break;
                }
            }


            audioRecorder.Stop();
            audioRecorder.Release();

            isRecording = false;

            RaiseRecordingStateChangedEvent();
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
                Console.Out.WriteLine("RECORDING PERMISSION GRANTED");

                endRecording = false;
                isRecording = true;

                InitialiseAudioRecording();

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

        private void InitialiseAudioRecording()
        {
            audioRecorder = FindAudioRecord();

            audioBuffer = new byte[audioRecorder.BufferSizeInFrames];
            audioRecorder.StartRecording();
        }
    }
}

