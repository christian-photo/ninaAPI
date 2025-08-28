using System.Collections.Specialized;
using System.Drawing;
using System.Net;
using System.Security.Principal;
using System.Text;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.Sessions;
using NINA.Core.Enum;
using ninaAPI.Utility;

namespace ninaAPI.Test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestQueryParameters()
    {
        NameValueCollection query = new NameValueCollection();
        query.Add("Test", "1");

        ContextMock context = new ContextMock(new RequestMock(query));

        QueryParameter<int> p = new QueryParameter<int>("test", 0, false);
        p.Get(context);
        Assert.Multiple(() =>
        {
            Assert.That(p.WasProvided, Is.True);
            Assert.That(p.Value, Is.EqualTo(1));
        });


        query.Add("width", "200");
        query.Add("height", "300");
        SizeQueryParameter sp = new SizeQueryParameter(new Size(200, 300), false);
        sp.Get(context);
        Assert.Multiple(() =>
        {
            Assert.That(sp.WasProvided, Is.True);
            Assert.That(sp.Value, Is.EqualTo(new Size(200, 300)));
        });

        query.Set("width", "200");
        query.Remove("height");
        SizeQueryParameter sp2 = new SizeQueryParameter(new Size(200, 300), true, true);
        sp2.Get(context);
        Assert.Multiple(() =>
        {
            Assert.That(sp2.WasProvided, Is.True);
            Assert.That(sp2.Value, Is.EqualTo(new Size(200, 0)));
        });

        SizeQueryParameter sp3 = new SizeQueryParameter(new Size(200, 300), false, false);
        sp3.Get(context);
        Assert.Multiple(() =>
        {
            Assert.That(sp3.WasProvided, Is.False);
            Assert.That(sp3.Value, Is.EqualTo(new Size(200, 300)));
        });

        query.Add("enum", "dcraw");
        QueryParameter<RawConverterEnum> raw = new QueryParameter<RawConverterEnum>("enum", RawConverterEnum.DCRAW, false);
        raw.Get(context);
        Assert.Multiple(() =>
        {
            Assert.That(raw.WasProvided, Is.True);
            Assert.That(raw.Value, Is.EqualTo(RawConverterEnum.DCRAW));
        });

        Assert.Pass();
    }
}

public class RequestMock(NameValueCollection query) : IHttpRequest
{
    public NameValueCollection Headers => throw new NotImplementedException();

    public bool KeepAlive => throw new NotImplementedException();

    public string RawUrl => throw new NotImplementedException();

    public NameValueCollection QueryString { get; } = query;

    public string HttpMethod => throw new NotImplementedException();

    public HttpVerbs HttpVerb => throw new NotImplementedException();

    public Uri Url => throw new NotImplementedException();

    public bool HasEntityBody => throw new NotImplementedException();

    public Stream InputStream => throw new NotImplementedException();

    public Encoding ContentEncoding => throw new NotImplementedException();

    public IPEndPoint RemoteEndPoint => throw new NotImplementedException();

    public bool IsLocal => throw new NotImplementedException();

    public bool IsSecureConnection => throw new NotImplementedException();

    public string UserAgent => throw new NotImplementedException();

    public bool IsWebSocketRequest => throw new NotImplementedException();

    public IPEndPoint LocalEndPoint => throw new NotImplementedException();

    public string? ContentType => throw new NotImplementedException();

    public long ContentLength64 => throw new NotImplementedException();

    public bool IsAuthenticated => throw new NotImplementedException();

    public Uri? UrlReferrer => throw new NotImplementedException();

    public ICookieCollection Cookies => throw new NotImplementedException();

    public Version ProtocolVersion => throw new NotImplementedException();
}

public class ContextMock(IHttpRequest request) : IHttpContext
{
    public string Id => throw new NotImplementedException();

    public CancellationToken CancellationToken => throw new NotImplementedException();

    public IPEndPoint LocalEndPoint => throw new NotImplementedException();

    public IPEndPoint RemoteEndPoint => throw new NotImplementedException();

    public RouteMatch Route => throw new NotImplementedException();

    public string RequestedPath => throw new NotImplementedException();

    public IPrincipal User => throw new NotImplementedException();

    public ISessionProxy Session => throw new NotImplementedException();

    public bool SupportCompressedRequests => throw new NotImplementedException();

    public IDictionary<object, object> Items => throw new NotImplementedException();

    public long Age => throw new NotImplementedException();

    public bool IsHandled => throw new NotImplementedException();

    IHttpRequest IHttpContext.Request { get; } = request;

    IHttpResponse IHttpContext.Response => throw new NotImplementedException();

    public string GetMimeType(string extension)
    {
        throw new NotImplementedException();
    }

    public void OnClose(Action<IHttpContext> callback)
    {
        throw new NotImplementedException();
    }

    public void SetHandled()
    {
        throw new NotImplementedException();
    }

    public bool TryDetermineCompression(string mimeType, out bool preferCompression)
    {
        throw new NotImplementedException();
    }
}
