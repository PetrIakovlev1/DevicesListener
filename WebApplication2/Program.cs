using Newtonsoft.Json;

// Пример класса, представляющего устройство
public class Device
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string DeviceType { get; set; }
    public string Location { get; set; }
}

// Пример класса, представляющего событие
public class Event
{
    public int EventId { get; set; }
    public int DeviceId { get; set; }
    public string EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> EventData { get; set; }
}

// Пример класса, представляющего базу данных
public class Database
{
    private List<Device> devices;
    private List<Event> events;

    public Database()
    {
        devices = new List<Device>();
        events = new List<Event>();
    }

    public void RegisterDevice(Device device)
    {
        devices.Add(device);
    }

    public void AddEvent(Event newEvent)
    {
        events.Add(newEvent);
    }

    public List<Event> GetDeviceEvents(int deviceId)
    {
        return events.Where(e => e.DeviceId == deviceId).ToList();
    }
}

public class Startup
{
    private Database database;

    public Startup()
    {
        database = new Database();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHealthChecks();
        services.AddMvc();
        services.AddSingleton(database);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapPost("/register-device", RegisterDevice);
            endpoints.MapPost("/add-event", AddEvent);
            endpoints.MapGet("/get-device-events/{deviceId}", GetDeviceEvents);
        });
    }

    private async Task RegisterDevice(HttpContext context)
    {
        var device = await DeserializeRequest<Device>(context.Request);
        database.RegisterDevice(device);
        await context.Response.WriteAsync($"Device {device.DeviceId} registered.");
    }

    private async Task AddEvent(HttpContext context)
    {
        var newEvent = await DeserializeRequest<Event>(context.Request);
        database.AddEvent(newEvent);
        await context.Response.WriteAsync($"Event {newEvent.EventId} added for Device {newEvent.DeviceId}.");
    }

    private async Task GetDeviceEvents(HttpContext context)
    {
        if (int.TryParse(context.Request.RouteValues["deviceId"] as string, out int deviceId))
        {
            var deviceEvents = database.GetDeviceEvents(deviceId);
            var responseJson = JsonConvert.SerializeObject(deviceEvents);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(responseJson);
        }
    }

    private async Task<T> DeserializeRequest<T>(HttpRequest request)
    {
        var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
        return JsonConvert.DeserializeObject<T>(requestBody);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var host = new WebHostBuilder()
            .UseKestrel()
            .UseStartup<Startup>()
            .Build();

        host.Run();
    }
}
