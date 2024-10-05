using System.Text;
using HttpServer;

public class Program
{
    static async Task Main()
    {
        MyServer server = new MyServer();
        await server.RunServerAsync("../../../site", 7777);
    }
}