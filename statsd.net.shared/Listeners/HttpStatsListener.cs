using Griffin.Networking.Messaging;
using Griffin.Networking.Protocol.Http.Protocol;
using Griffin.WebServer;
using Griffin.WebServer.Modules;
using statsd.net.shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
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
    private const string FLASH_CROSSDOMAIN = "<?xml version=\"1.0\" ?>\r\n<cross-domain-policy>\r\n  <allow-access-from domain=\"*\" />\r\n</cross-domain-policy>\r\n";

    private HttpServer _httpServer;
    private ISystemMetricsService _systemMetrics;
    private ITargetBlock<string> _target;
    private int _port;

    public HttpStatsListener(int port, ISystemMetricsService systemMetrics)
    {
      _systemMetrics = systemMetrics;
      _port = port;
      var moduleManager = new ModuleManager();
      moduleManager.Add(new HttpStatsListenerModule(this));
      _httpServer = new HttpServer(moduleManager);
    }

    public void LinkTo(ITargetBlock<string> target, CancellationToken token)
    {
      _target = target;
      _httpServer.Start(IPAddress.Any, _port);
      IsListening = true;
      Task.Factory.StartNew(() =>
        {
          // Wait for cancellation
          token.WaitHandle.WaitOne();
          _httpServer.Stop();
          IsListening = false;
        });
    }

    public bool IsListening { get; private set; }

    private class HttpStatsListenerModule : IWorkerModule
    {
      private HttpStatsListener _parent;
      public HttpStatsListenerModule(HttpStatsListener parent)
      {
        _parent = parent;
      }

      public void HandleRequestAsync(IHttpContext context, Action<IAsyncModuleResult> callback)
      {
        callback(new AsyncModuleResult(context, HandleRequest(context)));
      }

      public void BeginRequest(IHttpContext context)
      {
        context.Response.AddHeader("server", "statsd.net");
        context.Response.AddHeader("Access-Control-Allow-Origin", "*");
      }

      public void EndRequest(IHttpContext context)
      {
      }

      private ModuleResult HandleRequest(IHttpContext context)
      {
        if (!context.Request.Method.Equals("POST", StringComparison.InvariantCultureIgnoreCase))
        {
          context.Response.StatusCode = 405; // Method not allowed
          context.Response.StatusDescription = "Please use POST.";
        }

        if (context.Request.Uri.PathAndQuery.Equals("/crossdomain.xml", StringComparison.InvariantCultureIgnoreCase))
        {
          SendCrossDomainFile(context.Response);
        }
        else
        {
          ExtractMetrics(context);
        }

        return ModuleResult.Stop;
      }

      private void SendCrossDomainFile(IResponse response)
      {
        var bytes = Encoding.UTF8.GetBytes(FLASH_CROSSDOMAIN);
        response.ContentLength = bytes.Length;
        response.ContentType = "application/xml";
        response.Body.Write(bytes, 0, bytes.Length);
      }

      private void ExtractMetrics(IHttpContext context)
      {
        // Process the body of the request
        using (var reader = new StreamReader(context.Request.Body, context.Request.ContentEncoding))
        {
          var rawPacket = reader.ReadToEnd();
          _parent._systemMetrics.LogCount("listeners.http.bytes", (int)context.Request.ContentLength);
          string[] lines = rawPacket.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
          for (int index = 0; index < lines.Length; index++)
          {
            _parent._target.Post(lines[index]);
          }
          _parent._systemMetrics.LogCount("listeners.http.lines", lines.Length);
        }

        // Say OK
        context.Response.StatusCode = 200; // Method not allowed
        context.Response.StatusDescription = "OK";
      }
    }
  }
}
