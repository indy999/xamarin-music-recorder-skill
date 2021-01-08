using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using MusicRecorder.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.CurrentActivity;
using Android.Support.V4.App;
using Android.Support.Design.Widget;

namespace MusicRecorder.Service
{
    public class AudioRecorder : IAudioRecorder
    {
       
        private const int RequestLocationId = 0;

        private bool endRecording = false;
        private bool isRecording = true;
        private Android.Views.View layout;

        private Context context;
        private Activity activity;
        
        private AudioRecord audioRecorder;

        private int mainLayoutId = 2131361792;
        private int mainTextViewId = 2131230888;

        private int rate = 44100;
        private Android.Media.Encoding audioEncoding = Android.Media.Encoding.Pcm16bit;
        private ChannelIn channelConfig = ChannelIn.Stereo;
        private byte[] audioBuffer;

        public string[] PermissionsAudio { get; } = {
              Manifest.Permission.RecordAudio
            };

        public Action<bool> RecordingStateChanged;

        public AudioRecorder()
        {

        }

        public Task StartRecording(List<byte[]> audioTracks, MemoryStream memoryStream)
        {
            context = CrossCurrentActivity.Current.AppContext;
            activity = CrossCurrentActivity.Current.Activity;

            return Task.Run(async () =>
            {
                Console.Out.WriteLine("START RECORDING EVENT");

                await RequestRecordAudioPermission(audioTracks, memoryStream);
            });
        }

        public async Task StopRecording()
        {
            await Console.Out.WriteLineAsync("STOP RECORDING EVENT CLICKED");
            endRecording = true;
        }

        private async Task RequestRecordAudioPermission(List<byte[]> audioTracks, MemoryStream memoryStream)
        {
            string permission = PermissionsAudio[0];

            if (ContextCompat.CheckSelfPermission(context, permission) == Android.Content.PM.Permission.Granted)
            {
                Console.Out.WriteLine("RECORDING PERMISSION GRANTED");

                endRecording = false;
                isRecording = true;

                InitialiseAudioRecording();

                await RecordAudio(audioTracks, memoryStream);
                return;
            }

            if (activity.ShouldShowRequestPermissionRationale(permission))
            {
                layout = activity.FindViewById<LinearLayout>(mainLayoutId);

                Snackbar.Make(layout, "Permission to record audio required", Snackbar.LengthIndefinite)
                        .SetAction("OK", v => ActivityCompat.RequestPermissions(activity, PermissionsAudio, RequestLocationId))
                        .Show();
                return;
            }

            ActivityCompat.RequestPermissions(activity, PermissionsAudio, RequestLocationId);
        }

        private void InitialiseAudioRecording()
        {
            audioRecorder = FindAudioRecordNew();

            audioBuffer = new byte[audioRecorder.BufferSizeInFrames];
            audioRecorder.StartRecording();
        }


        private async Task RecordAudio(List<byte[]> audioTracks, MemoryStream memoryStream)
        {
            if (memoryStream == null)
            {
                memoryStream = new MemoryStream();
            }

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

            var mainTextView = activity.FindViewById<TextView>(mainTextViewId);

            if (memoryStream.Length > 0)
            {
                audioTracks.Add(memoryStream.ToArray());
                mainTextView.Text = "Numbers of Tracks:" + audioTracks.Count;
            }

            isRecording = false;

            RaiseRecordingStateChangedEvent();
        }

        public AudioRecord FindAudioRecordNew()
        {
            try
            {
                int bufferSize = AudioRecord.GetMinBufferSize(rate, (ChannelIn)channelConfig, (Android.Media.Encoding)audioEncoding);

                if (bufferSize != (int)Android.Media.TrackStatus.ErrorBadValue)
                {
                    AudioRecord recorder = new AudioRecord(AudioSource.Mic, rate, (ChannelIn)channelConfig, (Android.Media.Encoding)audioEncoding, bufferSize);

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
        private void RaiseRecordingStateChangedEvent()
        {
            if (RecordingStateChanged != null)
                RecordingStateChanged(isRecording);
        }
    }

 
}