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
using System.Linq;
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
            long totalBytesTrack1 = 1032000;
            long totalBytesTrack2 = 1422184;
            long totalBytesTrack3 = 1016484;
            short[] track1Short;
            short[] track2Short;
            short[] track3Short;

            long totalBytes = 37534;

            rate = 48000;
            audioEncoding = Encoding.Pcm16bit;

            BinaryReader binaryReader = new BinaryReader(assets.Open("sample.wav"));

            audioBuffer = binaryReader.ReadBytes((Int32)totalBytes);

            binaryReader.Close();


            binaryReader = new BinaryReader(assets.Open("Track 1.wav"));

            audioBufferTrack1 = binaryReader.ReadBytes((Int32)totalBytesTrack1);


            track1Short = new short[(int)Math.Ceiling((double)audioBufferTrack1.Length / 2)];
            Buffer.BlockCopy(audioBufferTrack1, 0, track1Short, 0, audioBufferTrack1.Length);

            var track1ShortList = track1Short.ToList();

            var zeroFillArrayLength = totalBytesTrack2 - totalBytesTrack1;
            short[] zeroFillArray = new short[zeroFillArrayLength];

            track1ShortList.AddRange(zeroFillArray);
            var reformedTrack1ShortArray = track1ShortList.ToArray();
            
            binaryReader.Close();

            binaryReader = new BinaryReader(assets.Open("Track 2.wav"));

            audioBufferTrack2 = binaryReader.ReadBytes((Int32)totalBytesTrack2);

            track2Short = new short[(int)Math.Ceiling((double)audioBufferTrack2.Length / 2)];
            Buffer.BlockCopy(audioBufferTrack2, 0, track2Short, 0, audioBufferTrack2.Length);

            binaryReader.Close();

            binaryReader = new BinaryReader(assets.Open("Track 3.wav"));

            audioBufferTrack3 = binaryReader.ReadBytes((Int32)totalBytesTrack3);

            track3Short = new short[(int)Math.Ceiling((double)audioBufferTrack3.Length / 2)];
            Buffer.BlockCopy(audioBufferTrack3, 0, track3Short, 0, audioBufferTrack3.Length);

            var track3ShortList = track3Short.ToList();

            zeroFillArrayLength = totalBytesTrack2 - totalBytesTrack3;
            zeroFillArray = new short[zeroFillArrayLength];

            track3ShortList.AddRange(zeroFillArray);
            var reformedTrack3ShortArray = track3ShortList.ToArray();

            binaryReader.Close();

            var output = new short[track2Short.Length];

            for (int i = 0; i < output.Length; i++)
            {

                float samplef1 = reformedTrack1ShortArray[i] / 32768.0f;
                float samplef2 = track2Short[i] / 32768.0f;
                float samplef3 = reformedTrack3ShortArray[i] / 32768.0f;

                float mixed = samplef1 + samplef2 + samplef3;
                // reduce the volume a bit:
                mixed *= (float)0.8;
                // hard clipping
                if (mixed > 1.0f) mixed = 1.0f;
                if (mixed < -1.0f) mixed = -1.0f;
                short outputSample = (short)(mixed * 32768.0f);

                output[i] = outputSample;
            }

            await PlayAudioTrack(output);


        }

        async Task PlaybackButton_Click()
        {
            AssetManager assets = this.Assets;
            
            if (memoryStream.Length > 0)
            {
                var audioArray = memoryStream.ToArray();
           
                var audioArrayShort = new short[(int)Math.Ceiling((double)audioArray.Length / 2)];
                Buffer.BlockCopy(audioArray, 0, audioArrayShort, 0, audioArray.Length);

                await PlayAudioTrack(audioArrayShort);
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

        async Task PlayAudioTrack(short[] audioBuffer)
        {
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

                    await Console.Out.WriteLineAsync("RECORDING SOUND. Memory stream size:" + memoryStream.Length);
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

