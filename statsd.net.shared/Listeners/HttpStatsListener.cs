using statsd.net.shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Listeners
{
  public class HttpStatsListener : IListener
  {
    private HttpListener _listener;
    private ITargetBlock<string> _target;
    private CancellationToken _token;
    private ISystemMetricsService _systemMetrics;
    private const string FLASH_CROSSDOMAIN = "<?xml version=\"1.0\" ?>\r\n<cross-domain-policy>\r\n  <allow-access-from domain=\"*\" />\r\n</cross-domain-policy>\r\n";

    public bool IsListening { get; private set; }

    public HttpStatsListener(int port, ISystemMetricsService systemMetrics)
    {
      _listener = new HttpListener();
      _listener.Prefixes.Add("http://*:" + port + "/");
      _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
      _systemMetrics = systemMetrics;
      IsListening = false;
    }

    public async void LinkTo(ITargetBlock<string> target, CancellationToken token)
    {
      _target = target;
      _token = token;
      _listener.Start();
      IsListening = true;
      await Listen();
    }

    private async Task Listen()
    {
      while(!_token.IsCancellationRequested)
      {
        var context = await _listener.GetContextAsync();
#pragma warning disable 4014
        Task.Factory.StartNew(() => { ProcessRequest(context); }, _token);
#pragma warning restore 4014
      }
      _listener.Close();
      IsListening = false;
    }

    private void ProcessRequest(HttpListenerContext context)
    {
      context.Response.Headers.Add("Server", "statsd.net");
      System.Threading.Thread.Sleep(10000);

      if (context.Request.Url.PathAndQuery.Equals("/crossdomain.xml", StringComparison.OrdinalIgnoreCase))
      {
        SendCrossdomainFile(context.Response);
        return;
      }
      else if (context.Request.Url.PathAndQuery.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
      {
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.Close();
        return;
      }
      else if (context.Request.HttpMethod.ToUpper() != "POST")
      {
        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
        context.Response.StatusDescription = "Please use POST.";
        context.Response.Close();
        return;
      }

      context.Response.StatusCode = (int)HttpStatusCode.OK;
      context.Response.StatusDescription = "OK";

      // Get the body of this request
      using (var body = context.Request.InputStream)
      {
        using (var reader = new System.IO.StreamReader(body, context.Request.ContentEncoding))
        {
          var rawPacket = reader.ReadToEnd();
          _systemMetrics.LogCount("listeners.http.bytes", (int)context.Request.ContentLength64);
          string[] lines = rawPacket.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
          for (int index = 0; index < lines.Length; index++)
          {
            _target.Post(lines[index]);
          }
          _systemMetrics.LogCount("listeners.http.lines", lines.Length);
        }
      }
      context.Response.Close();
    }

    private void SendCrossdomainFile(HttpListenerResponse response)
    {
      var bytes = System.Text.Encoding.UTF8.GetBytes(FLASH_CROSSDOMAIN);
      response.ContentLength64 = bytes.Length;
      response.ContentType = "application/xml";
      response.OutputStream.Write(bytes, 0, bytes.Length);
    }
  }
}
