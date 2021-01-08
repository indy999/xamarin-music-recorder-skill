using Android.App;
using Android.Widget;
using Android.OS;
using Android.Media;
using System;
using System.Collections.Generic;
using Android;
using Android.Content.PM;
using Android.Support.Design.Widget;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Android.Content.Res;
using System.Threading;
using Environment = System.Environment;
using TinyIoC;
using MusicRecorder.Service.Interfaces;
using MusicRecorder.Service;

namespace MusicRecorder
{
    [Activity(Label = "MusicRecorder", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private static TinyIoCContainer _container;

        private IAudioPlayback _audioPlayback;
        private IAudioRecorder _audioRecorder;
        private ITrackMixer _trackMixer;

        //private bool endRecording = false;
        //private bool isRecording = true;

        //private AudioTrack audioTrack;
        //private AudioRecord audioRecorder;
        //private const int RequestLocationId = 0;
        //private Android.Views.View layout;
        private List<byte[]> audioTracks;
        //private byte[] audioBuffer;

        //private TextView mainTextView;
        private MemoryStream memoryStream;

        //private int[] mSampleRates = new int[] { 8000, 11025, 22050, 44100 };
        //private int[] audioFormats = new int[] { (int)Encoding.Pcm8bit, (int)Encoding.Pcm16bit };
        //private int[] channelConfigs = new int[] { (int)ChannelIn.Mono, (int)ChannelIn.Stereo };

        //private int rate = 44100;
        //private Encoding audioEncoding = Encoding.Pcm16bit;
        //private ChannelIn channelConfig = ChannelIn.Stereo;

        //public Action<bool> RecordingStateChanged;
        //public string[] PermissionsAudio { get; } = {
        //      Manifest.Permission.RecordAudio
        //    };

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _container = new TinyIoCContainer();

            _container.Register<IAudioRecorder, AudioRecorder>().AsSingleton();
            _container.Register<IAudioPlayback, AudioPlayback>().AsSingleton();
            _container.Register<ITrackMixer, TrackMixer>().AsSingleton();

            _audioPlayback = _container.Resolve<IAudioPlayback>();
            _audioRecorder = _container.Resolve<IAudioRecorder>();
            _trackMixer = _container.Resolve<ITrackMixer>();


            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            audioTracks = new List<byte[]>();

            //layout = FindViewById<LinearLayout>(Resource.Layout.Main);
            //mainTextView = FindViewById<TextView>(Resource.Id.textView1);
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
            await _trackMixer.MixAudioTracks(audioTracks);
        }

        //private async Task MixAudioTracks()
        //{
        //    var audioTrackShortList = new List<short[]>();

        //    foreach (var audioTrack in audioTracks)
        //    {
        //        var audioTrackShort = new short[(int)Math.Ceiling((double)audioTrack.Length / 2)];
        //        Buffer.BlockCopy(audioTrack, 0, audioTrackShort, 0, audioTrack.Length);

        //        audioTrackShortList.Add(audioTrackShort);
        //    }

        //    var output = new short[audioTrackShortList.Max(x => x.Length)];

        //    for (int i = 0; i < output.Length; i++)
        //    {
        //        float mixed = MixTracks(audioTrackShortList, i);

        //        // reduce the volume a bit:
        //        // mixed *= (float)0.8;
        //        // hard clipping
        //        if (mixed > 1.0f) mixed = 1.0f;
        //        if (mixed < -1.0f) mixed = -1.0f;
        //        short outputSample = (short)(mixed * 32768.0f);

        //        output[i] = outputSample;
        //    }

        //    await PlayAudioTrack(output);
        //}

        //private float MixTracks(List<short[]> audioTrackShortList, int i)
        //{
        //    float mixed = 0;

        //    foreach (var audioTrack in audioTrackShortList)
        //    {
        //        if (i < audioTrack.Length)
        //        {
        //            mixed += (audioTrack[i] / 32768.0f); 
        //        }
        //    }

        //    return mixed;
        //}

        async Task PlaybackButton_Click()
        {
            if (memoryStream.Length > 0)
            {
                var audioArray = memoryStream.ToArray();

                await _audioPlayback.PlayAudioTrack(audioArray);
            }
        }

        async Task StopRecordingButton_Click()
        {
            await StopRecording();
        }

       
        //private void RaiseRecordingStateChangedEvent()
        //{
        //    if (RecordingStateChanged != null)
        //        RecordingStateChanged(isRecording);
        //}


        async Task StartRecordingButton_Click()
        {
             await StartRecording();
        }

        private async Task StartRecording()
        {
            await _audioRecorder.StartRecording(audioTracks, memoryStream);
        }

        async Task StopRecording()
        {
            await _audioRecorder.StopRecording();
        }

        //async Task PlayAudioTrack(short[] audioBuffer)
        //{
        //    AudioTrackStop();

        //    var attributes = new AudioAttributes.Builder().SetUsage(AudioUsageKind.Media).SetContentType(AudioContentType.Speech).Build();
            
        //    audioTrack = new AudioTrack(attributes, new AudioFormat.Builder()
        //        .SetEncoding(audioEncoding)
        //        .SetChannelMask(ChannelOut.Stereo)
        //        .SetSampleRate(rate).Build()
        //        , audioBuffer.Length,
        //        AudioTrackMode.Stream,
        //        1);

        //    audioTrack.Play();
        //    await audioTrack.WriteAsync(audioBuffer, 0, audioBuffer.Length);
        //}

        //async Task PlayAudioTrack(byte[] audioBuffer)
        //{
        //    AudioTrackStop();

        //    var attributes = new AudioAttributes.Builder().SetUsage(AudioUsageKind.Media).SetContentType(AudioContentType.Speech).Build();

        //    audioTrack = new AudioTrack(attributes, new AudioFormat.Builder()
        //            .SetEncoding(audioEncoding)
        //            .SetChannelMask(ChannelOut.Stereo)
        //            .SetSampleRate(rate).Build()
        //        , audioBuffer.Length,
        //        AudioTrackMode.Stream,
        //        1);


        //    audioTrack.Play();
        //    await audioTrack.WriteAsync(audioBuffer, 0, audioBuffer.Length);
        //}
        //public void AudioTrackStop()
        //{
        //    if (audioTrack != null)
        //    {
        //        audioTrack.Stop();
        //        audioTrack.Release();
        //        audioTrack = null;
        //    }
        //}

        //private async Task RecordAudio()
        //{
        //    memoryStream = new MemoryStream();

        //    while (true)
        //    {
        //        if (endRecording)
        //        {
        //            endRecording = false;
        //            break;
        //        }

        //        try
        //        {
        //            // Keep reading the buffer while there is audio input.

        //            await audioRecorder.ReadAsync(audioBuffer, 0, audioBuffer.Length);

        //            // Write out the audio file.
        //            await memoryStream.WriteAsync(audioBuffer, 0, audioBuffer.Length);

        //            await Console.Out.WriteLineAsync("RECORDING SOUND. Memory stream size:" + memoryStream.Length);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.Out.WriteLine(ex.Message);
        //            break;
        //        }
        //    }

        //    await Console.Out.WriteLineAsync("We successfully stopped recording.");
        //    audioRecorder.Stop();
        //    audioRecorder.Release();

        //    if (memoryStream.Length > 0)
        //    {
        //        audioTracks.Add(memoryStream.ToArray());
        //        mainTextView.Text = "Numbers of Tracks:" + audioTracks.Count;
        //    }

        //    isRecording = false;

        //    RaiseRecordingStateChangedEvent();
        //}



        //public AudioRecord FindAudioRecord()
        //{

        //    foreach (int rate in mSampleRates)
        //    {
        //        foreach (short audioFormat in audioFormats)
        //        {
        //            foreach (int channelConfig in channelConfigs)
        //            {
        //                try
        //                {
        //                    int bufferSize = AudioRecord.GetMinBufferSize(rate, (ChannelIn)channelConfig, (Encoding)audioFormat);

        //                    if (bufferSize != (int)Android.Media.TrackStatus.ErrorBadValue)
        //                    {
        //                        AudioRecord recorder = new AudioRecord(AudioSource.Mic, rate, (ChannelIn)channelConfig, (Encoding)audioFormat, bufferSize);

        //                        if (recorder.State == State.Initialized)
        //                            return recorder;
        //                    }
        //                }
        //                catch (Exception e)
        //                {
        //                    // Log.e(TAG, rate + "Exception, keep trying.", e);
        //                }
        //            }
        //        }
        //    }
        //    return null;
        //}

        //public AudioRecord FindAudioRecordNew()
        //{
        //    try
        //    {
        //        int bufferSize = AudioRecord.GetMinBufferSize(rate, (ChannelIn)channelConfig, (Encoding)audioEncoding);

        //        if (bufferSize != (int)Android.Media.TrackStatus.ErrorBadValue)
        //        {
        //            AudioRecord recorder = new AudioRecord(AudioSource.Mic, rate, (ChannelIn)channelConfig, (Encoding)audioEncoding, bufferSize);

        //            if (recorder.State == State.Initialized)
        //                return recorder;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        // Log.e(TAG, rate + "Exception, keep trying.", e);
        //    }

        //    return null;
        //}

        
        //private async Task RequestRecordAudioPermission()
        //{

        //    string permission = PermissionsAudio[0];

        //    if (CheckSelfPermission(permission) == Android.Content.PM.Permission.Granted)
        //    {
        //        Console.Out.WriteLine("RECORDING PERMISSION GRANTED");

        //        endRecording = false;
        //        isRecording = true;

        //        InitialiseAudioRecording();

        //        await RecordAudio();
        //        return;
        //    }

        //    if (ShouldShowRequestPermissionRationale(permission))
        //    {
        //        Snackbar.Make(layout, "Permission to record audio required", Snackbar.LengthIndefinite)
        //                .SetAction("OK", v => RequestPermissions(PermissionsAudio, RequestLocationId))
        //                .Show();
        //        return;
        //    }

        //    RequestPermissions(PermissionsAudio, RequestLocationId);
        //}

        //private void InitialiseAudioRecording()
        //{
        //    audioRecorder = FindAudioRecordNew();

        //    audioBuffer = new byte[audioRecorder.BufferSizeInFrames];
        //    audioRecorder.StartRecording();
        //}
    }
}

