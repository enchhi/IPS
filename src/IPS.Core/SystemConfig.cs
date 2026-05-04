using System.Xml.Linq;

namespace IPS;

public class SystemConfig
{
    public int WorkerCount { get; set; }
    public int MaxQueueSize { get; set; }
    public List<Job> InitialJobs { get; set; } = new List<Job>();

    public static SystemConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Config not found: " + path);

        var doc = XDocument.Load(path);
        return Parse(doc);
    }

    public static SystemConfig Parse(XDocument doc)
    {
        var root = doc.Root!;
        var config = new SystemConfig
        {
            WorkerCount = int.Parse(root.Element("WorkerCount")!.Value),
            MaxQueueSize = int.Parse(root.Element("MaxQueueSize")!.Value),
        };

        var jobsRoot = root.Element("Jobs");
        if (jobsRoot != null)
        {
            foreach (var je in jobsRoot.Elements("Job"))
            {
                var typeStr = je.Attribute("Type")!.Value;
                var payload = je.Attribute("Payload")!.Value;
                var prio = int.Parse(je.Attribute("Priority")!.Value);
                var type = (JobType)Enum.Parse(typeof(JobType), typeStr, true);
                config.InitialJobs.Add(new Job(type, payload, prio));
            }
        }

        return config;
    }
}
