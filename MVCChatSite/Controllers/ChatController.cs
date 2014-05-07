using System.Web.Mvc;

namespace MVCChatSite.Controllers
{
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
    
public class ChatResponse
{
    public List<MessageInfo> messages { get; set; }
}

public class MessageInfo
{
    public long id { get; set; }
    public long timestamp { get; set; }
    public string message { get; set; }
    public UserInfo user { get; set; }
}

public class UserInfo
{
    public long id { get; set; }
    public string name { get; set; }
}

public class ChatServer
{
    public const int MaxHistoryCount = 100;
    public const int MaxWaitSeconds = 60;

    private static object _msgLock = new object();
    private static Subject<MessageInfo> _messages = new Subject<MessageInfo>();

    private static object _historyLock = new object();
    private static Queue<MessageInfo> _history = new Queue<MessageInfo>(MaxHistoryCount + 5);        

    static ChatServer()
    {
        _messages
            .Subscribe(msg =>
            {
                lock (_historyLock)
                { 
                    while (_history.Count > MaxHistoryCount)
                        _history.Dequeue();   
                 
                    _history.Enqueue(msg);
                }
            });
    }

    public static void CheckForMessagesAsync(Action<List<MessageInfo>> onMessages)
    {
        var queued = ThreadPool.QueueUserWorkItem(new WaitCallback(parm =>
        {
            var msgs = new List<MessageInfo>();
            var wait = new AutoResetEvent(false);
            using (var subscriber = _messages.Subscribe(msg =>
                                            {
                                                msgs.Add(msg);
                                                wait.Set();
                                            }))
            {
                // Wait for the max seconds for a new msg
                wait.WaitOne(TimeSpan.FromSeconds(MaxWaitSeconds));                    
            }

            ((Action<List<MessageInfo>>)parm)(msgs);
        }), onMessages);

        if (!queued)
            onMessages(new List<MessageInfo>());
    }

    private static long currMsgId = 0;
    public static void AddMessage(string userName, string message)
    {
        _messages
            .OnNext(new MessageInfo
            {
                message = message,
                timestamp = UnixTicks(DateTime.UtcNow),
                user = new UserInfo
                {
                    id = currMsgId++,
                    name = userName
                }
            });            
    }

    public static List<MessageInfo> GetHistory()
    {
        var msgs = new List<MessageInfo>();
        lock (_historyLock) 
            msgs = _history.ToList();
            
        return msgs;
    }

    private static long UnixTicks(DateTime dt)
    {
        DateTime d1 = new DateTime(1970, 1, 1);
        DateTime d2 = dt.ToUniversalTime();
        TimeSpan ts = new TimeSpan(d2.Ticks - d1.Ticks);
        return (long)ts.TotalMilliseconds;
    }
}

public class ChatController : AsyncController
{
    [AsyncTimeout(ChatServer.MaxWaitSeconds * 1002)]
    public void IndexAsync()
    {
        AsyncManager.OutstandingOperations.Increment();

        ChatServer.CheckForMessagesAsync(msgs =>
        {
            AsyncManager.Parameters["response"] = new ChatResponse
            {
                messages = msgs
            };
            AsyncManager.OutstandingOperations.Decrement();
        });
    }

    public ActionResult IndexCompleted(ChatResponse response)
    {
        return Json(response);
    }

    [HttpPost]
    public ActionResult New(string user, string msg)
    {
        ChatServer.AddMessage(user, msg);
        return Json(new
            {
                d = 1
            });
    }
}
}
