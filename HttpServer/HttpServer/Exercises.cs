namespace HttpServer;

public class Exercises
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime Created { get; set; }
    public DateTime Completed { get; set; }
    public string UserName { get; set; }
    public bool IsDone { get; set; }
    
    public Exercises() {}

    public Exercises(int id, string title, string userName, string description)
    {
        Id = id;
        Title = title;
        Created = DateTime.Now;
        UserName = userName;
        Description = description;
        IsDone = false;
    }
}