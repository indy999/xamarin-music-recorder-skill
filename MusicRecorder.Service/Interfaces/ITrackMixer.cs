using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicRecorder.Service.Interfaces
{
    public interface ITrackMixer
    {
        Task MixAudioTracks(List<byte[]> audioTracks);
    }
}
