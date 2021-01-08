using System.Threading.Tasks;

namespace MusicRecorder.Service.Interfaces
{
    public interface IAudioPlayback
    {
        Task PlayAudioTrack(byte[] audioBuffer);
        Task PlayAudioTrack(short[] audioBuffer);
    }
}
