using ninaAPI.Utility.Http;

namespace ninaAPI.Test;

public class ConflictsTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ApiProcessType.CameraWarm.ConflictsWith(ApiProcessType.CameraCool));
            Assert.That(ApiProcessType.CameraCapture.ConflictsWith(ApiProcessType.CameraCapture));
            Assert.That(!ApiProcessType.CameraWarm.ConflictsWith(ApiProcessType.CameraCapture));
        });
    }
}