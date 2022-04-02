using System;
using System.Threading;
using System.Threading.Tasks;

public static class EscCancel
{
    public static Task StartAsync(CancellationTokenSource cancellationTokenSource)
    {
        Console.WriteLine("Press the ESC key to stop server...");

        return Task.Run(() =>
        {
            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                Console.WriteLine("Press the ESC key to stop server...");
            }

            Console.WriteLine("\n> ESC key pressed: stop server.\n");
            cancellationTokenSource.Cancel();
        });
    }
}
