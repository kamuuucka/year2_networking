using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using shared;
using System.Threading;
using PARAMDESC = System.Runtime.InteropServices.PARAMDESC;

class TCPServerSample
{
    private readonly int _port;
    private readonly List<TcpClient> _clients = new List<TcpClient>();
    private readonly List<TcpClient> _faultyClients = new List<TcpClient>();
    private readonly List<string> _registeredNames = new List<string>();
    private IDictionary<TcpClient, string> _clientNames = new Dictionary<TcpClient, string>();
    private IDictionary<TcpClient, string> _newClientNames = new Dictionary<TcpClient, string>();
    private int _number = 0;
    private bool _doesExist = false;

    private TCPServerSample(int port)
    {
        _port = port;
    }


    private void Run()
    {
        Console.WriteLine("Server started on port " + _port);
        TcpListener listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();

        while (true)
        {
            ProcessNewClients(listener);
            ProcessExistingClients();
            CleanupFaultyClients();
            UpdateNames();
            //NamesCleanup();

            //Although technically not required, now that we are no longer blocking, 
            //it is good to cut your CPU some slack
            Thread.Sleep(100);
        }
    }

    private void ProcessNewClients(TcpListener listener)
    {
        //First big change with respect to example 001
        //We no longer block waiting for a client to connect, but we only block if we know
        //a client is actually waiting (in other words, we will not block)
        //In order to serve multiple clients, we add that client to a list
        while (listener.Pending())
        {
            TcpClient newClient = listener.AcceptTcpClient();
            _clients.Add(newClient);
            _clientNames.Add(newClient, "guest" + _number);
            _registeredNames.Add(_clientNames[newClient]);
            Console.WriteLine("Client: " + _clientNames[newClient] + " joined the server.");
            byte[] welcomeMessage =
                Encoding.UTF8.GetBytes("You joined server as " + _clientNames[newClient]);
            StreamUtil.Write(newClient.GetStream(), welcomeMessage);
            welcomeMessage = Encoding.UTF8.GetBytes("Welcome " + _clientNames[newClient] + " to the server!");
            SentToEveryone(welcomeMessage);
            _number++;
        }
    }

    private void ProcessExistingClients()
    {
        //Second big change, instead of blocking on one client, 
        //we now process all clients IF they have data available
        foreach (TcpClient client in _clients)
        {
            if (client.Available == 0) continue;
            NetworkStream stream = client.GetStream();
            byte[] message = StreamUtil.Read(stream);
            string inString = Encoding.UTF8.GetString(message);
            string[] command = inString.Split(' ');
            foreach (var com in command)
            {
                Console.WriteLine(com);
            }

            string serverMessage = "This command does not exist";
            bool nameChanged = false;
            bool commandsUsed = false;

            switch (command[0])
            {
                case "/help":
                    commandsUsed = true;
                    if (command.Length == 1)
                    {
                        serverMessage = "/help - Shows the list of commands\n" +
                                        "/list - Shows the list of names\n" +
                                        "/setname {name} - Sets the new name for you";
                    }

                    break;
                case "/list":
                    commandsUsed = true;
                    if (command.Length == 1)
                    {
                        string list = "";
                        foreach (var name in _registeredNames)
                        {
                            list += name + "\n";
                        }

                        serverMessage = list.Substring(0, list.Length - 1);
                    }

                    break;
                case "/setname":
                    commandsUsed = true;
                    if (command.Length > 1)
                    {
                        string newName = command[1].ToLower();
                        
                        foreach (var name in _registeredNames)
                        {
                            if (name.Equals(newName))
                            {
                                serverMessage = "This name is already taken!";
                                _doesExist = true;
                            }
                        }

                        if (!_doesExist)
                        {
                            _newClientNames[client] = newName;
                            Console.WriteLine("Name {0} added to the list for: {1}", newName, _clientNames[client]);
                            serverMessage = _clientNames[client] + " changed name to " + newName;
                            nameChanged = true;
                        }
                    }
                    else
                    {
                        serverMessage = "No name registered!";
                    }

                    

                    break;
                case "/whisper":
                    if (_registeredNames.Contains(command[1]))
                    {
                        
                    }
                    break;
                default:
                    serverMessage = "This command does not exist";
                    break;
            }

            if (commandsUsed)
            {
                byte[] newMessage = Encoding.UTF8.GetBytes(serverMessage);
                if (nameChanged)
                {
                    SentToEveryone(newMessage);
                }
                else
                {
                    StreamUtil.Write(stream, newMessage);
                }
            }
            else
            {
                string wholeMessage = _clientNames[client] + " says: " + inString;
                byte[] toEveryoneMessage = Encoding.UTF8.GetBytes(wholeMessage);
                SentToEveryone(toEveryoneMessage);
            }


            //Getting the client's IP address: string ipAddress = client.Client.RemoteEndPoint.ToString();
        }
    }

    private void CleanupFaultyClients()
    {
        foreach (var client in _faultyClients)
        {
            _clientNames.Remove(client);
            _clients.Remove(client);
            Console.WriteLine("Removing faulty client");
        }

        _faultyClients.Clear();
    }

    private void NamesCleanup()
    {
        List<string> namesToKeep = new List<string>();
        for (int i = 0; i < _registeredNames.Count; i++)
        {
            foreach (var name in _clientNames)
            {
                if (name.Value == _registeredNames[i])
                {
                    Console.WriteLine(name.Value);
                    namesToKeep.Add(name.Value);
                    break;
                }   
            }
        }
        
        _registeredNames.Clear();

        foreach (var name in namesToKeep)
        {
            _registeredNames.Add(name);
        }

    }

    private void UpdateNames()
    {
        foreach (var newClient in _newClientNames)
        {
            Console.WriteLine("Updating names");
            _registeredNames.Remove(_clientNames[newClient.Key]);
            _clientNames[newClient.Key] = newClient.Value;
            _registeredNames.Add(_clientNames[newClient.Key]);
        }

        _newClientNames.Clear();
        _doesExist = false;
    }

    private void SentToEveryone(byte[] newMessage)
    {
        foreach (var other in _clients)
        {
            try
            {
                StreamUtil.Write(other.GetStream(), newMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (!other.Connected)
                {
                    Console.WriteLine("Skipping disconnected client");
                    _registeredNames.Remove(_clientNames[other]);
                    _faultyClients.Add(other);
                }
            }
        }
    }


    /**
	 * This class implements a simple concurrent TCP Echo server.
	 * Read carefully through the comments below.
	 */
    public static void Main(string[] args)
    {
        new TCPServerSample(55558).Run();
    }
}