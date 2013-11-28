using Kayak;
using Kayak.Http;
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
using System.Web;

namespace statsd.net.shared.Listeners
{
  public class HttpStatsListener : IListener
  {
    private const string FLASH_CROSSDOMAIN = "<?xml version=\"1.0\" ?>\r\n<cross-domain-policy>\r\n  <allow-access-from domain=\"*\" />\r\n</cross-domain-policy>\r\n";

    private IScheduler _scheduler;
    private ISystemMetricsService _systemMetrics;
    private ITargetBlock<string> _target;
    private int _port;

    public HttpStatsListener(int port, ISystemMetricsService systemMetrics)
    {
      _systemMetrics = systemMetrics;
      _port = port;
    }

    public void LinkTo(ITargetBlock<string> target, CancellationToken token)
    {
      _target = target;
      IsListening = true;
      _scheduler = KayakScheduler.Factory.Create(new SchedulerDelegate());
      var wsTask = Task.Factory.StartNew(() =>
        {
          var server = KayakServer.Factory.CreateHttp(
            new RequestDelegate(this),
            _scheduler);

          using (server.Listen(new IPEndPoint(IPAddress.Any, _port)))
          {
            _scheduler.Start();
          }
          IsListening = false;
        });

      Task.Factory.StartNew(() =>
        {
          token.WaitHandle.WaitOne();
          _scheduler.Stop();
        });
    }

    public bool IsListening { get; private set; }

    private class SchedulerDelegate : ISchedulerDelegate
    {
      public void OnException(IScheduler scheduler, Exception e)
      {
         // Ignore
      }

      public void OnStop(IScheduler scheduler)
      {
      }
    }

    private class RequestDelegate : IHttpRequestDelegate
    {
      private HttpStatsListener _parent;
      public RequestDelegate(HttpStatsListener parent)
      {
        _parent = parent;
      }

      public void OnRequest(HttpRequestHead head, 
        IDataProducer body, 
        IHttpResponseDelegate response)
      {
        if (head.Method.ToUpperInvariant() == "POST")
        {
          ProcessPOSTRequest(body, response);
        }
        else if (head.Method.ToUpperInvariant() == "GET" && head.Uri == "/crossdomain.xml")
        {
          ProcessCrossDomainRequest(body, response);
        }
        else if (head.Method.ToUpperInvariant() == "GET" && head.QueryString.Contains("metrics"))
        {
          ProcessGETRequest(body, head, response);
        }
        else if (head.Method.ToUpperInvariant() == "GET" && head.Uri == "/")
        {
          ProcessLoadBalancerRequest(body, response);
        }
        else
        {
          ProcessFileNotFound(body, response);
        }
      }


      private void ProcessPOSTRequest(IDataProducer body, IHttpResponseDelegate response)
      {
          body.Connect(new BufferedConsumer(
            (payload) =>
            {
              try
              {
                _parent._systemMetrics.LogCount("listeners.http.bytes", Encoding.UTF8.GetByteCount(payload));
                string[] lines = payload.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int index = 0; index < lines.Length; index++)
                {
                  _parent._target.Post(lines[index]);
                }
                _parent._systemMetrics.LogCount("listeners.http.lines", lines.Length);
                Respond(response, "200 OK");
              }
              catch
              {
                Respond(response, "400 bad request");
              }

            },
            (error) =>
            {
              Respond(response, "500 Internal server error");
            }));
      }

      private void ProcessCrossDomainRequest(IDataProducer body, IHttpResponseDelegate response)
      {
        var responseHead = new HttpResponseHead()
        {
          Status = "200 OK",
          Headers = new Dictionary<string, string>
            {
              { "Content-Type", "application-xml" },
              { "Content-Length", Encoding.UTF8.GetByteCount(FLASH_CROSSDOMAIN).ToString() },
              { "Access-Control-Allow-Origin", "*"}
            }
        };
        response.OnResponse(responseHead, new BufferedProducer(FLASH_CROSSDOMAIN));
      }

      private void ProcessGETRequest(IDataProducer body, HttpRequestHead head, IHttpResponseDelegate response)
      {
        var qs = head.QueryString.Split(new string[] { "&" }, StringSplitOptions.RemoveEmptyEntries)
          .Select(p => p.Split(new string[] { "=" }, StringSplitOptions.None))
          .ToDictionary(p => p[0], p => HttpUtility.UrlDecode(p[1]));

        string[] lines = qs["metrics"].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
        for (int index = 0; index < lines.Length; index++)
        {
          _parent._target.Post(lines[index]);
        }
        _parent._systemMetrics.LogCount("listeners.http.lines", lines.Length);
        _parent._systemMetrics.LogCount("listeners.http.bytes", Encoding.UTF8.GetByteCount(qs["metrics"]));

        var responseHead = new HttpResponseHead()
        {
          Status = "200 OK",
          Headers = new Dictionary<string, string>
            {
              { "Content-Type", "application-xml" },
              { "Content-Length", "0" },
              { "Access-Control-Allow-Origin", "*"}
            }
        };
        response.OnResponse(responseHead, new EmptyResponse());
      }

      private void ProcessLoadBalancerRequest(IDataProducer body, IHttpResponseDelegate response)
      {
        _parent._systemMetrics.LogCount("listeners.http.loadbalancer");
        Respond(response, "200 OK");
      }
      
      private void ProcessFileNotFound(IDataProducer body, IHttpResponseDelegate response)
      {
        _parent._systemMetrics.LogCount("listeners.http.404");
        var headers = new HttpResponseHead()
        {
          Status = "404 Not Found",
          Headers = new Dictionary<string, string>
            {
              { "Content-Type", "text/plain" },
              { "Content-Length", Encoding.UTF8.GetByteCount("not found").ToString() },
              { "Access-Control-Allow-Origin", "*"}
            }
        };
        response.OnResponse(headers, new BufferedProducer("not found"));
      }

      private void Respond(IHttpResponseDelegate response, string status)
      {
        var responseHead = new HttpResponseHead()
        {
          Status = status,
          Headers = new Dictionary<string, string>()
          {
              { "Content-Type", "text/plain" },
              { "Content-Length", "0" },
              { "Access-Control-Allow-Origin", "*"}
          }
        };
        response.OnResponse(responseHead, new EmptyResponse());
      }
    }

    private class BufferedConsumer : IDataConsumer
    {
      private List<ArraySegment<byte>> _buffer = new List<ArraySegment<byte>>();
      private Action<string> _callback;
      private Action<Exception> _error;

      public BufferedConsumer(Action<string> callback,
        Action<Exception> error)
      {
        _callback = callback;
        _error = error;
      }
      
      public bool OnData(ArraySegment<byte> data, Action continuation)
      {
        _buffer.Add(data);
        return false;
      }

      public void OnEnd()
      {
        var payload = _buffer
          .Select(p => Encoding.UTF8.GetString(p.Array, p.Offset, p.Count))
          .Aggregate((result, next) => result + next);
        _callback(payload);
      }

      public void OnError(Exception e)
      {
        _error(e);
      }
    }

    private class BufferedProducer : IDataProducer
    {
      private ArraySegment<byte> _rawData;

      public BufferedProducer(string data)
      {
        _rawData = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
      }

      public IDisposable Connect(IDataConsumer channel)
      {
        channel.OnData(_rawData, null);
        channel.OnEnd();
        return null;
      }
    }

    private class EmptyResponse : IDataProducer
    {
      public EmptyResponse()
      {
      }

      public IDisposable Connect(IDataConsumer channel)
      {
        channel.OnData(new ArraySegment<byte>(), null);
        channel.OnEnd();
        return null;
      }
    }
  }
}
