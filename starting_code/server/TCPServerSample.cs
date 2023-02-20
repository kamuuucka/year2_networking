using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using shared;
using System.Threading;

class TCPServerSample
{
	private readonly int _port;
	private readonly List<TcpClient> _clients = new List<TcpClient>();
	private readonly List<TcpClient> _faultyClients = new List<TcpClient>();
	private readonly List<string> _registeredNames = new List<string>();
	private IDictionary<TcpClient, string> _clientNames = new Dictionary<TcpClient, string>();
	private IDictionary<TcpClient, string> _newClientNames = new Dictionary<TcpClient, string>();
	private int _number = 0;
	//private string _newName;

	private TCPServerSample(int port)
	{
		_port = port;
	}
	

	private void Run()
	{
		Console.WriteLine("Server started on port " + _port);
		TcpListener listener = new TcpListener (IPAddress.Any, _port);
		listener.Start ();
		
		while (true)
		{
			ProcessNewClients(listener);
			ProcessExistingClients();
			CleanupFaultyClients();
			UpdateNames();
			
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
			_clientNames.Add(newClient, "Guest" + _number); 
			_registeredNames.Add(_clientNames[newClient]);
			Console.WriteLine("Client: " + _clientNames[newClient] + " joined the server.");
			byte[] welcomeMessage =
				Encoding.UTF8.GetBytes("You joined server as " + _clientNames[newClient]);
			StreamUtil.Write(newClient.GetStream(), welcomeMessage);
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
			if (inString.StartsWith("/"))
			{
				
				Console.WriteLine("Using commands");
				string serverMessage = "This command does not exist";
				bool nameChanged = false;

				if (inString.Equals("/help"))
				{
					serverMessage = "Commands list.";
				} else if (inString.Equals("/list"))
				{
					serverMessage = "list of users";
				} 
				else if (inString.StartsWith("/setname "))
				{
					string newName = inString.Substring(9).ToLower();
					foreach (var name in _registeredNames)
					{
						//TODO: all clients are getting new names!!!!
						//Console.WriteLine(key.Value);
						if (name.Equals(newName))
						{
							serverMessage = "This name is already taken!";
						}
						else
						{
							_newClientNames[client] = newName;
							Console.WriteLine(_newClientNames[client] +" is now " + newName);
							serverMessage = _clientNames[client] + " changed name to " + newName;
							nameChanged = true;
						}
					}
				}
				
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
				byte[] newMessage = Encoding.UTF8.GetBytes(wholeMessage);
				SentToEveryone(newMessage);
				
			}
			//Getting the client's IP address: string ipAddress = client.Client.RemoteEndPoint.ToString();
		}
	}

	private void CleanupFaultyClients()
	{
		foreach (var client in _faultyClients)
		{
			_clients.Remove(client);
			_clientNames.Remove(client);
			Console.WriteLine("Removing faulty client");
		}
		
		_faultyClients.Clear();
	}

	private void UpdateNames()
	{
		foreach (var newClient in _newClientNames)
		{
			Console.WriteLine("I am changing names");
			_registeredNames.Remove(_clientNames[newClient.Key]);
			_clientNames[newClient.Key] = newClient.Value;
			_registeredNames.Add(newClient.Value);
		}
		
		_newClientNames.Clear();
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
					_faultyClients.Add(other);
				}
			}
		}
	}
	
	
	/**
	 * This class implements a simple concurrent TCP Echo server.
	 * Read carefully through the comments below.
	 */
	public static void Main (string[] args)
	{
		new TCPServerSample(55558).Run();
	}
}


