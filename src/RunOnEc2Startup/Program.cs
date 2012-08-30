namespace RunOnEc2Startup
{
  using System;
  using System.IO;
  using System.Net;

  using Amazon;
  using Amazon.EC2.Model;

  using RunOnEc2Startup.Properties;

  using log4net;
  using log4net.Config;

  class Program
  {
    static void Main(string[] args)
    {
      XmlConfigurator.Configure(new FileInfo("log4net.config"));

      var logger = LogManager.GetLogger(typeof(Program));

      logger.Info(@"************************");
      logger.Info(@"*    __  _    _  ___ ");
      logger.Info(@"*   (  )( \/\/ )/ __)");
      logger.Info(@"*   /__\ \    / \__ \");
      logger.Info(@"*  (_)(_) \/\/  (___/");
      logger.Info(@"* ");
      logger.Info(@"*  Run On EC2 Startup");
      logger.Info(@"* ");
      logger.Info(@"************************");
      logger.Info(@"");

      try
      {
        var webClient = new WebClient();
        var instanceId = webClient.DownloadString(Settings.Default.InstanceIdAddress);
        logger.InfoFormat("Instance id: {0}", instanceId);

        var userData = webClient.DownloadString(Settings.Default.UserDataAddress);
        logger.InfoFormat("User data: {0}", userData);

        if (ShouldLaunchProcess(userData))
        {
          LaunchProcess();

          if (Settings.Default.StopInstanceOnExit)
          {
            StopInstance(instanceId);
          }
        }

        logger.Info("Bye");
      }
      catch (Exception ex)
      {
        logger.Error("Exception: ", ex);
      }
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
      var logger = LogManager.GetLogger(typeof(Program));

      var processFileName = Settings.Default.ProcessFileName;
      logger.InfoFormat("Launching process: {0}", processFileName);

      var process = System.Diagnostics.Process.Start(processFileName);
      if (process == null)
      {
        return;
      }

      var timeoutMinutes = Settings.Default.ProcessTimeoutMinutes;
      var timeoutMilliseconds = 1000 * 60 * timeoutMinutes;
      if (process.WaitForExit(timeoutMilliseconds))
      {
        logger.Warn("Timed out waiting for process to exit");
      }
    }

    /// <summary>
    /// Stops the instance.
    /// </summary>
    /// <param name="instanceId">The instance id.</param>
    private static void StopInstance(string instanceId)
    {
      var logger = LogManager.GetLogger(typeof(Program));
      logger.Info("Stopping instance");

      var client = AWSClientFactory.CreateAmazonEC2Client(
        Settings.Default.AwsAccessKey, Settings.Default.AwsSecretAccessKey);
      var request = new StopInstancesRequest { InstanceId = { instanceId } };
      client.StopInstances(request);
    }
  }
}
