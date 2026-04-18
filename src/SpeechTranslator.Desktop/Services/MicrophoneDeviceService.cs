using NAudio.CoreAudioApi;

namespace SpeechTranslator.Desktop;

public sealed class MicrophoneDeviceService : IMicrophoneDeviceService
{
    public IReadOnlyList<MicrophoneDeviceOption> GetInputDevices()
    {
        using var enumerator = new MMDeviceEnumerator();

        var devices = enumerator
            .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(device => new MicrophoneDeviceOption(device.FriendlyName, device.FriendlyName))
            .ToList();

        devices.Insert(0, new MicrophoneDeviceOption(string.Empty, "Default microphone"));
        return devices;
    }
}
