using System.Collections.Specialized;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using ninaAPI.Utility;

namespace ninaAPI.Test;

public class TestClass
{
    public int Test { get; set; }
    public List<CameraInfo> Info { get; set; }
    public Dictionary<string, string> TestDict { get; set; }

    public TestClass()
    {
        Test = 1;
        Info = [DeviceInfo.CreateDefaultInstance<CameraInfo>()];
        TestDict = new Dictionary<string, string>();
        TestDict.Add("Test", "Test");
    }
}

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestInBetween()
    {
        Assert.Multiple(() =>
        {
            Assert.That(0.IsBetween(0, 1), Is.True);
            Assert.That(1.IsBetween(0, 1), Is.True);
            Assert.That(0.IsBetween(1, 2), Is.False);
            Assert.That(2.IsBetween(1, 2), Is.True); // Inclusive
            Assert.That(1.IsBetween(0, 2), Is.True);
            Assert.That(2.IsBetween(0, 2), Is.True);
        });
    }

    [Test]
    public void TestReflectedPropertySet()
    {
        var obj = new TestClass();
        CoreUtility.SetValueReflected(obj, "Test", 2);
        Assert.That(obj.Test, Is.EqualTo(2));
        CoreUtility.SetValueReflected(obj, "Info-0-Name", "Test");
        Assert.That(obj.Info[0].Name, Is.EqualTo("Test"));
        CoreUtility.SetValueReflected(obj, "TestDict-Test", "Test2");
        Assert.That(obj.TestDict["Test"], Is.EqualTo("Test2"));
    }

    [Test]
    public void TestQueryParameters()
    {
        NameValueCollection query = new NameValueCollection();
        query.Add("Test", "1");

        // TODO: Reimplement

        // ContextMock context = new ContextMock(new RequestMock(query));

        // QueryParameter<int> p = new QueryParameter<int>("test", 0, false);
        // p.Get(context);
        // Assert.Multiple(() =>
        // {
        //     Assert.That(p.WasProvided, Is.True);
        //     Assert.That(p.Value, Is.EqualTo(1));
        // });


        // query.Add("width", "200");
        // query.Add("height", "300");
        // SizeQueryParameter sp = new SizeQueryParameter(new Size(200, 300), false);
        // sp.Get(context);
        // Assert.Multiple(() =>
        // {
        //     Assert.That(sp.WasProvided, Is.True);
        //     Assert.That(sp.Value, Is.EqualTo(new Size(200, 300)));
        // });

        // query.Set("width", "200");
        // query.Remove("height");
        // SizeQueryParameter sp2 = new SizeQueryParameter(new Size(200, 300), true, true);
        // sp2.Get(context);
        // Assert.Multiple(() =>
        // {
        //     Assert.That(sp2.WasProvided, Is.True);
        //     Assert.That(sp2.Value, Is.EqualTo(new Size(200, 0)));
        // });

        // SizeQueryParameter sp3 = new SizeQueryParameter(new Size(200, 300), false, false);
        // sp3.Get(context);
        // Assert.Multiple(() =>
        // {
        //     Assert.That(sp3.WasProvided, Is.False);
        //     Assert.That(sp3.Value, Is.EqualTo(new Size(200, 300)));
        // });

        // query.Add("enum", "dcraw");
        // QueryParameter<RawConverterEnum> raw = new QueryParameter<RawConverterEnum>("enum", RawConverterEnum.DCRAW, false);
        // raw.Get(context);
        // Assert.Multiple(() =>
        // {
        //     Assert.That(raw.WasProvided, Is.True);
        //     Assert.That(raw.Value, Is.EqualTo(RawConverterEnum.DCRAW));
        // });

        // Assert.Pass();
    }
}

