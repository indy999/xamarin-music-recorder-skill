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
using Environment = System.Environment;

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

        private byte[] audioBufferTrack1;
        private byte[] audioBufferTrack2;
        private byte[] audioBufferTrack3;


        private MemoryStream memoryStream;

        int[] mSampleRates = new int[] { 8000, 11025, 22050, 44100 };
        int[] audioFormats = new int[] { (int)Encoding.Pcm8bit, (int)Encoding.Pcm16bit };
        int[] channelConfigs = new int[] { (int)ChannelIn.Mono, (int)ChannelIn.Stereo };

        int rate = 44100;
        private Encoding audioEncoding = Encoding.Pcm16bit;
        ChannelIn channelConfig = ChannelIn.Stereo;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            layout = FindViewById<LinearLayout>(Resource.Layout.Main);
            Button startRecordingButton = FindViewById<Button>(Resource.Id.btnStartRecording);
            Button stopRecordingButton = FindViewById<Button>(Resource.Id.btnStopRecording);
            Button playbackButton = FindViewById<Button>(Resource.Id.btnPlayback);
            Button mixtrackButton = FindViewById<Button>(Resource.Id.btnMixTracks);

            startRecordingButton.Click += async (sender, e) => await StartRecordingButton_Click();
            stopRecordingButton.Click += async (sender, e) => await StopRecordingButton_Click();
            playbackButton.Click += async (sender, e) => await PlaybackButton_Click();
            mixtrackButton.Click += async (sender, e) => await MixtrackButton_Click();


        }

        async Task MixtrackButton_Click()
        {
            AssetManager assets = this.Assets;
            long totalBytesTrack1 = 121217;
            long totalBytesTrack2 = 153271;
            long totalBytesTrack3 = 117838;

            long totalBytes = 37534;

            rate = 11025;
            audioEncoding = Encoding.Pcm8bit;

            BinaryReader binaryReader = new BinaryReader(assets.Open("sample.wav"));

            audioBuffer = binaryReader.ReadBytes((Int32)totalBytes);

            binaryReader.Close();


            binaryReader = new BinaryReader(assets.Open("Track 1.m4a"));

            audioBufferTrack1 = binaryReader.ReadBytes((Int32)totalBytesTrack1);

            binaryReader.Close();

            binaryReader = new BinaryReader(assets.Open("Track 2.m4a"));

            audioBufferTrack2 = binaryReader.ReadBytes((Int32)totalBytesTrack2);

            binaryReader.Close();

            binaryReader = new BinaryReader(assets.Open("Track 3.m4a"));

            audioBufferTrack3 = binaryReader.ReadBytes((Int32)totalBytesTrack3);

            binaryReader.Close();

            await PlayAudioTrack(audioBuffer);


        }

        async Task PlaybackButton_Click()
        {
            AssetManager assets = this.Assets;
            //long totalBytes = 37534;

            //BinaryReader binaryReader = new BinaryReader(assets.Open("sample.wav"));

            //audioBuffer = binaryReader.ReadBytes((Int32)totalBytes);

            //binaryReader.Close();
            string path = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            if (memoryStream.Length > 0)
            {
                await PlayAudioTrack(memoryStream.ToArray());
                //File.WriteAllBytes(path+"/test.wav",memoryStream.ToArray());
            }
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


        Task StartRecordingButton_Click()
        {
            Task recordAudioTask = Task.Run(async () =>
            {
                Console.Out.WriteLine("START RECORDING EVENT");

                await RequestRecordAudioPermission();
            });

            return recordAudioTask;
        }

        async Task StopRecording()
        {

            await Console.Out.WriteLineAsync("STOP RECORDING EVENT CLICKED");
            endRecording = true;
            //Thread.Sleep(500); // Give it time to drop out.
            }

        async Task PlayAudioTrack(byte[] audioBuffer)
        {
            var attributes = new AudioAttributes.Builder().SetUsage(AudioUsageKind.Media).SetContentType(AudioContentType.Speech).Build();

            audioTrack = new AudioTrack(attributes, new AudioFormat.Builder()
                .SetEncoding(audioEncoding)
                .SetChannelMask(ChannelOut.Mono)
                .SetSampleRate(rate).Build()
                , audioBuffer.Length,
                AudioTrackMode.Stream,
                1);


            audioTrack.Play();
            await audioTrack.WriteAsync(audioBuffer, 0, audioBuffer.Length);
        }

        private async Task RecordAudio()
        {
            memoryStream = new MemoryStream();

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

                    await audioRecorder.ReadAsync(audioBuffer, 0, audioBuffer.Length);

                    // Write out the audio file.
                    await memoryStream.WriteAsync(audioBuffer, 0, audioBuffer.Length);

                    await Console.Out.WriteLineAsync("RECORDING SOUND. Memory stream size:"+ memoryStream.Length);   
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.Message);
                    break;
                }
            }

            await Console.Out.WriteLineAsync("We successfully stopped recording.");
            audioRecorder.Stop();
            audioRecorder.Release();

            isRecording = false;

            RaiseRecordingStateChangedEvent();
        }



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

        public AudioRecord FindAudioRecordNew()
        {
            try
            {
                int bufferSize = AudioRecord.GetMinBufferSize(rate, (ChannelIn)channelConfig, (Encoding)audioEncoding);

                if (bufferSize != (int)Android.Media.TrackStatus.ErrorBadValue)
                {
                    AudioRecord recorder = new AudioRecord(AudioSource.Mic, rate, (ChannelIn)channelConfig, (Encoding)audioEncoding, bufferSize);

                    if (recorder.State == State.Initialized)
                        return recorder;
                }
            }
            catch (Exception e)
            {
                // Log.e(TAG, rate + "Exception, keep trying.", e);
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

        private async Task RequestRecordAudioPermission()
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
            audioRecorder = FindAudioRecordNew();

            audioBuffer = new byte[audioRecorder.BufferSizeInFrames];
            audioRecorder.StartRecording();
        }
    }
}

