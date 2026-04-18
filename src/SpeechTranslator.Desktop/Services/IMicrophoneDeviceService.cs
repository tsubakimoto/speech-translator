namespace SpeechTranslator.Desktop;

public interface IMicrophoneDeviceService
{
    IReadOnlyList<MicrophoneDeviceOption> GetInputDevices();
}
