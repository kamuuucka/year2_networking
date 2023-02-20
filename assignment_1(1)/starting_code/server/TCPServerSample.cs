using System;
using System.Net.Sockets;
using System.Text;
using System.Net;

class TCPServerSample
{
	/**
	 * This class implements a simple TCP Echo server.
	 * Read carefully through the comments below.
	 */
	public static void Main (string[] args)
	{
		//Start listening for connection requests on any local NIC (network interface controller) on port 55555
		//The Start call is NON blocking (e.g. code continues after calling .Start)
		Console.WriteLine("Starting server ...");
		//Line that defines if it is a server
		TcpListener listener = new TcpListener (IPAddress.Any, 55558); //original port in the code: 55555, but for me it's being already used
		listener.Start ();
		Console.WriteLine($"Server started on {listener.LocalEndpoint}");

		while (true) {
			//AcceptTcpClient is a BLOCKING call.
			//This means your code will not continue until a client actually connects or an error occurs.
			//IF a client connects, a TcpClient instance representing THAT client is returned, 
			//which is bound to the SAME port as the TcpListener.
			//Connecting clients are put in a queue while waiting for AcceptTcpClient to be called.
			Console.WriteLine("Wait for client to connect...");
			//blocking thingy - code waits until the client matches the server
			TcpClient client = listener.AcceptTcpClient ();

			//If we get here, we have a client, print some info (using a different method than above)
			IPEndPoint endPoint = client.Client.RemoteEndPoint as IPEndPoint;
			Console.WriteLine($"Client connected from {endPoint.Address}:{endPoint.Port}, waiting to serve ...");

			//Now that we have a client, we can retrieve its 'stream'.
			//A 'stream' is something we can write to or read from.
			//Everything you write is send to the other side, 
			//and when the other side writes something back you can read it
			NetworkStream stream = client.GetStream ();

			//Now go into a loop, to see whether we are receiving any data so that we can reply to it
			while (true)
			{
				//A lot of things can go wrong while trying to communicate over a network
				try
				{
					//To read from the stream we use stream.Read (byte[] receiveBuffer, startIndex, byteCount).
					//But how big should the buffer be?? 
					//By default: we don't know (hence all those buffer overflow issues ;)).
					//For now, until we come up with something better, we just create a 1k buffer.
					byte[] inBytes = new byte[1024];
					
					//Now we try to actually read from the stream. 
					//This call will also BLOCK until some (but maybe NOT ALL) bytes have been received.
					//The amount of bytes ACTUALLY received will be returned by the call.
					int inByteCount = stream.Read(inBytes, 0, inBytes.Length);

					//So now we have a bunch of bytes, but what to do with them?
					//We HAVE to know what the client actually send, 
					//which in this case MUST be an UTF8 encoded string (see client)
					string inString = Encoding.UTF8.GetString(inBytes, 0, inByteCount);
					Console.WriteLine($"Received:{inString} ({inByteCount} bytes)");

					//This server is a simple echo server, 
					//which means that we just 'echo' all the bytes we received back to the client
					Console.WriteLine($"Sending:{inString} ({inByteCount} bytes)\n");
					stream.Write(inBytes, 0, inByteCount);
				} 
				catch (Exception e)
				{
					//What could possibly go wrong? Find out as we print all these error messages:
					Console.WriteLine(e.Message);

					//both stream and client must be closed to free up resources and in this order
					stream.Close();
					client.Close();

					break;
				}
			}
		}
	}
}


