using System;
class Util
{
	public static string ReadPassword()
	{
		string pass = "";
		Console.Write("Enter your password: ");
		ConsoleKeyInfo key;
		do
		{
			key = Console.ReadKey(true);

			// Backspace Should Not Work
			if (key.Key != ConsoleKey.Backspace)
			{
				pass += key.KeyChar;
			}
			else
			{
				if(pass.Length>0) pass=pass.Substring(0,pass.Length-1);
			}
		}
		// Stops Receving Keys Once Enter is Pressed
		while (key.Key != ConsoleKey.Enter);
		Console.WriteLine();
		return pass;
	}
}