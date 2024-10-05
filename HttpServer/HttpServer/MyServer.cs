using System.Net;
using System.Text.Json;
using RazorEngine;
using RazorEngine.Templating;

namespace HttpServer;

public class MyServer
{
    private string _siteDirectory;
    private HttpListener _listener;
    private int _port;
    private static List<Exercises> _exercises = JsonSerializer.Deserialize<List<Exercises>>(File.ReadAllText("../../../tasks.json"));
    private int id = _exercises.Any() ? _exercises.Last().Id : 0;
    private Exercises ChoosenExercise;

    public async Task RunServerAsync(string path, int port)
    {
        _siteDirectory = path;
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port.ToString()}/");
        _listener.Start();
        Console.WriteLine($"Server started on {_port} \nFiles in {_siteDirectory}");
        await ListenAsync();
    }

    private async Task ListenAsync()
    {
        try
        {
            while (true)
            {
                HttpListenerContext context = await _listener.GetContextAsync();
                Process(context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void Process(HttpListenerContext context)
    {
        var filename = _siteDirectory + context.Request.Url.AbsolutePath;

        if (!File.Exists(filename))
        {
            context.Response.StatusCode = 404;
            context.Response.OutputStream.Write(new byte[0]);
            context.Response.OutputStream.Close();
            return;
        }

        try
        {
            if (context.Request.HttpMethod == "POST")
            {
                using (var reader = new StreamReader(context.Request.InputStream))
                {
                    var body = reader.ReadToEnd();
                    var newExerciseData = body.Split("&").Select(param => WebUtility.UrlDecode(param.Split("=")[1]))
                        .ToArray();
                    id++;
                    var newExercise = new Exercises(id, newExerciseData[0], newExerciseData[1], newExerciseData[2]);
                    _exercises.Add(newExercise);
                    File.WriteAllText("../../../tasks.json", JsonSerializer.Serialize(_exercises));
                }
            }

            if (context.Request.QueryString.Count > 0)
            {
                var action = context.Request.QueryString["action"];
                var idValue = WebUtility.UrlDecode(context.Request.QueryString["id"]);
                var exerciseId = Convert.ToInt32(idValue);

                if (action == "done")
                {
                    var exercise = _exercises.FirstOrDefault(e => e.Id == exerciseId);
                    if (exercise != null)
                    {
                        exercise.IsDone = true;
                        exercise.Completed = DateTime.Now;
                        File.WriteAllText("../../../tasks.json", JsonSerializer.Serialize(_exercises));
                    }
                }
                else if (action == "delete")
                {
                    var exercise = _exercises.FirstOrDefault(e => e.Id == exerciseId);
                    if (exercise != null)
                    {
                        _exercises.Remove(exercise);
                        File.WriteAllText("../../../tasks.json", JsonSerializer.Serialize(_exercises));
                    }
                }

                if (context.Request.Url.AbsolutePath.Contains("details.html"))
                {
                    ChoosenExercise = _exercises.FirstOrDefault(e => e.Id == exerciseId);
                }
            }

            var content = filename.Contains("html") ? BuildHtml(filename) : File.ReadAllText(filename);
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);

            using (var memoryStream = new MemoryStream(contentBytes))
            {
                context.Response.ContentType = GetContentType(filename);
                context.Response.ContentLength64 = memoryStream.Length;
                memoryStream.CopyTo(context.Response.OutputStream);
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Flush();
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.OutputStream.Write(new byte[0]);
        }
        finally
        {
            context.Response.OutputStream.Close();
        }
    }

    private string BuildHtml(string filename)
    {
        string html = "";
        string layoutPath = _siteDirectory + "/layout.html";
        var razorService = Engine.Razor;
        if(!razorService.IsTemplateCached("layout", null))
            razorService.AddTemplate("layout", File.ReadAllText(layoutPath));
        if (!razorService.IsTemplateCached(filename, null))
        {
            razorService.AddTemplate(filename, File.ReadAllText(filename));
            razorService.Compile(filename);
        }

        html = razorService.Run(filename, null, new
        {
            Exercises = _exercises,
            Exercise = ChoosenExercise
        });
        return html;
    }

    private string? GetContentType(string filename)
    {
        var Dictionary = new Dictionary<string, string>()
        {
            {".css", "text/css"},
            {".js", "application/javascript"},
            {".png", "image/png"},
            {".jpg", "image/jpeg"},
            {".gif", "image/gif"},
            {".html", "text/html; charset=utf-8"},
            {".json", "application/json"}
        };
        string contentype = "";
        string extension = Path.GetExtension(filename);
        Dictionary.TryGetValue(extension, out contentype);
        return contentype;
    }

    public void Stop()
    {
        _listener.Abort();
        _listener.Stop();
    }
}