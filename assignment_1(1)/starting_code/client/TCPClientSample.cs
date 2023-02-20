using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/**
 * This class implements a simple TCP Echo Client.
 */
class TCPClientSample
{
	public static void Main (string[] args)
	{
		//Create a new TcpClient so we can setup a connection.
		//We can already pass in a host and port during construction,
		//or do this later while connecting.
		TcpClient client = new TcpClient();

		//Try to connect (ignoring any exceptions for now...) using the BLOCKING Connect call.
		//In other words, Connect will not continue until we actually have a connection or fail.
		Console.WriteLine("Connecting to server...");
		client.Connect ("localhost", 55558);
		Console.WriteLine($"Connected to server {client.Client.RemoteEndPoint} from {client.Client.LocalEndPoint}");
		Console.WriteLine();

		//When we get here, we ARE connected, so we can get the stream 
		NetworkStream stream = client.GetStream ();

		while (true)
		{
			//Note the lack of error handling --> bad practice!

			//Construct a string to send, convert it to bytes using UTF8 encoding and write it into the stream
			Console.WriteLine("Enter message to send:");
			string outString = Console.ReadLine();
			byte[] outBytes = Encoding.UTF8.GetBytes(outString);
			Console.WriteLine("Sending:" + outString);
			stream.Write(outBytes, 0, outBytes.Length);
			
			//In this case we know exactly how many bytes we sent and how many to expect.
			//That is an exception to the rule, normally we would have no clue.
			byte[] inBytes = new byte[1024];
			int inByteCount = stream.Read(inBytes, 0, inBytes.Length);
			
			//print the message
			string inString = Encoding.UTF8.GetString(inBytes, 0, inByteCount);
			Console.WriteLine("Received:" + inString);
			Console.WriteLine("");
		}

	}
}


