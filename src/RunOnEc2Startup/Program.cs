namespace RunOnEc2Startup
{
  using System;
  using System.Net;

  using Amazon;
  using Amazon.EC2.Model;

  using RunOnEc2Startup.Properties;

  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine(@"************************");
      Console.WriteLine(@"*    __  _    _  ___ ");
      Console.WriteLine(@"*   (  )( \/\/ )/ __)");
      Console.WriteLine(@"*   /__\ \    / \__ \");
      Console.WriteLine(@"*  (_)(_) \/\/  (___/");
      Console.WriteLine(@"* ");
      Console.WriteLine(@"*  Run On EC2 Startup");
      Console.WriteLine(@"* ");
      Console.WriteLine(@"************************");
      Console.WriteLine(@"");

      var webClient = new WebClient();
      var instanceId = webClient.DownloadString(Settings.Default.InstanceIdAddress);
      Console.WriteLine("Instance id: {0}", instanceId);

      var userData = webClient.DownloadString(Settings.Default.UserDataAddress);
      Console.WriteLine("User data: {0}", userData);

      if (ShouldLaunchProcess(userData))
      {
        LaunchProcess();
      }

      if (Settings.Default.StopInstanceOnExit)
      {
        StopInstance(instanceId);
      }

      Console.WriteLine("Bye");
    }

    /// <summary>
    /// Whether the process should be launched.
    /// </summary>
    /// <param name="userData">The user data.</param>
    /// <returns></returns>
    private static bool ShouldLaunchProcess(string userData)
    {
      if (string.IsNullOrEmpty(userData))
      {
        return false;
      }

      var elements = userData.Split('=');
      if (elements.Length != 2)
      {
        return false;
      }

      var name = elements[0];
      if (name != Settings.Default.Key)
      {
        return false;
      }

      var value = elements[1];
      return value == Settings.Default.Value;
    }

    /// <summary>
    /// Launches the process.
    /// </summary>
    private static void LaunchProcess()
    {
      var processFileName = Settings.Default.ProcessFileName;
      Console.WriteLine("Launching process: {0}", processFileName);

      var process = System.Diagnostics.Process.Start(processFileName);
      if (process != null)
      {
        const int FiftyMinutes = 1000 * 60 * 50;
        process.WaitForExit(FiftyMinutes);
      }
    }

    /// <summary>
    /// Stops the instance.
    /// </summary>
    /// <param name="instanceId">The instance id.</param>
    private static void StopInstance(string instanceId)
    {
      Console.WriteLine("Stopping instance");
      var client = AWSClientFactory.CreateAmazonEC2Client(
        Settings.Default.AwsAccessKey, Settings.Default.AwsSecretAccessKey);
      var request = new StopInstancesRequest { InstanceId = { instanceId } };
      client.StopInstances(request);
    }
  }
}
