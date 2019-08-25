using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebServerSocket
{
    class Program
    {
        static void Main(string[] args)
        {
            MyServer _myServer = new MyServer();
            _myServer.StartServer();
            Console.ReadKey();
        }
    }

    public class MyServer
    {
        private SocketListener _socketListener;
        public MyServer()
        {
            _socketListener = new SocketListener();
        }

        public void StartServer()
        {
            _socketListener.StartListener();
            _socketListener.OnConnected();
        }
    }

    public class SocketListener
    {
        private Socket _socketListener;
        private IPEndPoint _ipAndPort;
        private Request _request;
  
        public SocketListener()
        {
            _request = new Request();
        }

        public Socket StartListener()
        {
            _socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _ipAndPort = new IPEndPoint(IPAddress.Any, 1234);
            _socketListener.Bind(_ipAndPort);
            _socketListener.Listen(10);
            Console.WriteLine("Server Running");
            return _socketListener;
        }

        public void OnConnected()
        {
            Socket _getRequest;
            try
            {
                new Thread(() =>
                {
                    while(true)
                    {
                        _getRequest = _socketListener.Accept();
                        _request.HandleRequest(_getRequest);
                        Console.WriteLine("Sever Connected");
                        _getRequest.Close();
                    }
                }).Start();
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error Occur due to {e}");
            }
        }
    }

    public class Request
    {
        private Parser _parser;
        private FileHandler _fileHandler;

        public Request()
        {
            _parser = new Parser();
            _fileHandler = new FileHandler();
        }
        public void HandleRequest(Socket clientRequest)
        {
            NetworkStream getRequest = new NetworkStream(clientRequest);
            byte[] byteData = new byte[1024];
            int byteDataCount = getRequest.Read(byteData, 0, byteData.Length);
            string request = Encoding.ASCII.GetString(byteData, 0, byteDataCount);
            Console.WriteLine($"client request {request}");
            var _urls = _parser.ParsingUrl(request);
            var _url = _urls[1].Split('/');
            var _method = _urls[0];
            Console.WriteLine($"url {_urls[1]}");
            Console.WriteLine($"method {_method}");
            if(_method == "GET")
            {
                Console.WriteLine($"This is GET Method {_method}");
                if (DomainNameSystem.IsDomainExist(_url[1]))
                {
                    string _filePath = _fileHandler.ResolveVirtualPath(_urls[1], DomainNameSystem.GetRootDirectory(_url[1]), "/" + _url);
                    byte[] _data = Encoding.ASCII.GetBytes(_fileHandler.TryGetFile(_filePath));
                    Response.ViewResponse(clientRequest, _data, "200 OK", "text/html");
                }
                else
                {
                    string file = "<HTML><BODY><h1>404 Not Found</h1><p>FileNotFound</p></BODY></HTML>";
                    byte[] _data = Encoding.ASCII.GetBytes(file);
                    Response.ViewResponse(clientRequest, _data, "404 Not Found", "text/html");
                }
            }
            else if(_method == "POST")
            {
                Console.WriteLine($"This is Post Method {_method}");
                if(_url[1] == "api")
                {
                    string _getResponse = REST.PerformOperations(request, clientRequest);
                    byte[] _data = Encoding.ASCII.GetBytes(_getResponse);
                    Response.ViewResponse(clientRequest, _data, "200 OK", "application/json");
                }
                else
                {
                    Console.WriteLine("Please use api for call function");
                }
            }
            else
            {
                string file = "<HTML><BODY><h1>There is No Method Available</h1><p>FileNotFound</p></BODY></HTML>";
                byte[] _data = Encoding.ASCII.GetBytes(file);
                Response.ViewResponse(clientRequest, _data, "404 Not Found", "text/html");
            }
        }
    }

    public class Parser
    {
        public string[] ParsingUrl(string request)
        {
            var _parameters = request.Split('\n');
            return _parameters[0].Split(' '); ;

        }
    }

    public class DomainNameSystem
    {
        private static Dictionary<string,string> _domainNames = new Dictionary<string, string>()
        {
            { "facebook.com", "C://Users//rkaruppaiah//Desktop//RaguWs//Webpages//facebook/facebook.html" },
            { "google.com", "C://Users//rkaruppaiah//Desktop//RaguWs//Webpages//google/google.html" }
        };
        
        public static bool IsDomainExist(string domain)
        {
            return _domainNames.ContainsKey(domain);
        }
        public static string GetRootDirectory(string domain)
        {
            return _domainNames[domain];
        }


    }

    public interface IFileSystem
    {
        string ResolveVirtualPath(string virtualPath, string rootDirectory, string url);
        string TryGetFile(string filePath);
    }

    public class  FileHandler : IFileSystem
    {
        public string ResolveVirtualPath(string virtualPath, string rootDirectory, string url)
        {
            return virtualPath.Replace(virtualPath, rootDirectory);
        }

        public string TryGetFile(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch(Exception e)
            {
                return null;
            }
        }
    }

    public class Response
    {
        public static void ViewResponse(Socket getRequest, byte[] data, string status, string mime)
        {
            StringBuilder _header = new StringBuilder();
            _header.AppendLine("HTTP/1.1 "+ status);
            _header.AppendLine("Content-Type: "+ mime + ";charset=UTF-8");
            _header.AppendLine();
            List<byte> _responses = new List<byte>();
            _responses.AddRange(Encoding.ASCII.GetBytes(_header.ToString()));
            _responses.AddRange(data);
            byte[] _response = _responses.ToArray();
            getRequest.Send(_response);
        }
    }

    public class REST
    {
        public static string PerformOperations(string request, Socket clientRequest)
        {
            var _bodyContents = GetBodyContent(request).Split('\n')[1];
            var _getYear = _bodyContents.Split(':')[1];
            _getYear = _getYear.Replace('"', ' ').Trim();
            int _year;
            int.TryParse(_getYear, out _year);
            Console.WriteLine($" the year is {_year}");
            Console.WriteLine($"the body is {GetBodyContent(request)}");

            if(Operations.IsLeapYear(_year))
                return "{\n" + "\tisLeap : true" + "\n}";
            else
                return "{\n" + "\tisLeap : false" + "\n}";
        }

        public static string GetBodyContent(string request)
        {
            return request.Split('{')[1];
        }
    }

    public class Operations
    {
        public static bool IsLeapYear(int year)
        {
            if (((year % 4 == 0) && (year % 100 != 0)) || (year % 400 == 0))
                return true;
            else
                return false;
            
        }
    }
}
