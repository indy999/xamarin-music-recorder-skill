using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MusicRecorder.Service.Interfaces
{
    public interface IAudioRecorder
    {
        Task StartRecording(List<byte[]> audioTracks, MemoryStream memoryStream);
        Task StopRecording();
    }
}
