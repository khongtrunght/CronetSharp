using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CronetSharp.Client;
using CronetSharp.Cronet;

namespace CronetSharp.Example.Examples
{
    /// <summary>
    /// Example demonstrating how to monitor request status during execution.
    /// This shows real-time notifications of request lifecycle events.
    /// </summary>
    public static class StatusMonitoringExample
    {
        public static void Run()
        {
            Console.WriteLine("=== Status Monitoring Example ===\n");

            // Create engine
            var engineParams = new CronetEngineParams
            {
                EnableQuic = true,
                EnableHttp2 = true,
                UserAgent = "CronetSharp-StatusExample/1.0"
            };

            using var engine = new CronetEngine(engineParams);

            // Example 1: Basic status monitoring
            Console.WriteLine("Example 1: Basic Status Monitoring");
            Console.WriteLine("-----------------------------------");
            BasicStatusMonitoring(engine);
            Console.WriteLine();

            // Example 2: Status timeline tracking
            Console.WriteLine("Example 2: Status Timeline Tracking");
            Console.WriteLine("------------------------------------");
            StatusTimelineTracking(engine);
            Console.WriteLine();

            // Example 3: Network activity detection
            Console.WriteLine("Example 3: Network Activity Detection");
            Console.WriteLine("--------------------------------------");
            NetworkActivityDetection(engine);
            Console.WriteLine();

            // Example 4: Progress reporting
            Console.WriteLine("Example 4: Progress Reporting");
            Console.WriteLine("------------------------------");
            ProgressReporting(engine);
            Console.WriteLine();

            Console.WriteLine("=== All examples completed ===");
        }

        /// <summary>
        /// Example 1: Basic status monitoring with simple callback
        /// </summary>
        private static void BasicStatusMonitoring(CronetEngine engine)
        {
            Console.WriteLine("Monitoring request to https://httpbin.org/delay/1");
            Console.WriteLine("Status updates:");

            using var statusListener = new UrlRequestStatusListener(status =>
            {
                var description = UrlRequestStatusDescriptions.GetDescription(status);
                Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] {status}: {description}");
            });

            // Note: In a real implementation, we would attach the listener to the request
            // For demonstration, we'll manually trigger some status changes
            statusListener.OnStatus(UrlRequestStatus.Idle);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.ResolvingHost);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.Connecting);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.SslHandshake);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.SendingRequest);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.WaitingForResponse);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.ReadingResponse);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.Idle);
            Thread.Sleep(200); // Allow callbacks to complete
        }

        /// <summary>
        /// Example 2: Track complete status timeline with timestamps
        /// </summary>
        private static void StatusTimelineTracking(CronetEngine engine)
        {
            var timeline = new List<(DateTime Time, UrlRequestStatus Status)>();

            using var statusListener = new UrlRequestStatusListener(status =>
            {
                lock (timeline)
                {
                    timeline.Add((DateTime.Now, status));
                }
            });

            // Simulate request lifecycle
            statusListener.OnStatus(UrlRequestStatus.Idle);
            Thread.Sleep(50);
            statusListener.OnStatus(UrlRequestStatus.ResolvingHost);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.Connecting);
            Thread.Sleep(150);
            statusListener.OnStatus(UrlRequestStatus.SendingRequest);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.ReadingResponse);
            Thread.Sleep(200);

            // Print timeline
            lock (timeline)
            {
                if (timeline.Count == 0)
                {
                    Console.WriteLine("  No status updates recorded");
                    return;
                }

                var startTime = timeline[0].Time;
                foreach (var (time, status) in timeline)
                {
                    var elapsed = (time - startTime).TotalMilliseconds;
                    Console.WriteLine($"  +{elapsed,6:F0}ms: {status}");
                }

                var totalTime = (timeline[timeline.Count - 1].Time - startTime).TotalMilliseconds;
                Console.WriteLine($"  Total time: {totalTime:F0}ms");
            }
        }

        /// <summary>
        /// Example 3: Detect and report network activity
        /// </summary>
        private static void NetworkActivityDetection(CronetEngine engine)
        {
            bool isNetworkActive = false;
            DateTime networkStartTime = DateTime.MinValue;
            TimeSpan totalNetworkTime = TimeSpan.Zero;

            using var statusListener = new UrlRequestStatusListener(status =>
            {
                bool wasActive = isNetworkActive;
                isNetworkActive = UrlRequestStatusDescriptions.IsNetworkActive(status);

                if (!wasActive && isNetworkActive)
                {
                    // Network activity started
                    networkStartTime = DateTime.Now;
                    Console.WriteLine($"  Network activity started: {status}");
                }
                else if (wasActive && !isNetworkActive)
                {
                    // Network activity ended
                    var duration = DateTime.Now - networkStartTime;
                    totalNetworkTime += duration;
                    Console.WriteLine($"  Network activity ended: {status} (duration: {duration.TotalMilliseconds:F0}ms)");
                }
                else if (isNetworkActive)
                {
                    Console.WriteLine($"  Network activity continues: {status}");
                }
            });

            // Simulate request with network activity
            statusListener.OnStatus(UrlRequestStatus.Idle);
            Thread.Sleep(50);
            statusListener.OnStatus(UrlRequestStatus.ResolvingHost);
            Thread.Sleep(50);
            statusListener.OnStatus(UrlRequestStatus.Connecting);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.SslHandshake);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.SendingRequest);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.WaitingForResponse);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.ReadingResponse);
            Thread.Sleep(100);
            statusListener.OnStatus(UrlRequestStatus.Idle);
            Thread.Sleep(200);

            Console.WriteLine($"  Total network time: {totalNetworkTime.TotalMilliseconds:F0}ms");
        }

        /// <summary>
        /// Example 4: Progress reporting with visual indicator
        /// </summary>
        private static void ProgressReporting(CronetEngine engine)
        {
            var stages = new[]
            {
                UrlRequestStatus.ResolvingHost,
                UrlRequestStatus.Connecting,
                UrlRequestStatus.SslHandshake,
                UrlRequestStatus.SendingRequest,
                UrlRequestStatus.WaitingForResponse,
                UrlRequestStatus.ReadingResponse
            };

            int currentStage = -1;
            var stageNames = new Dictionary<UrlRequestStatus, string>
            {
                { UrlRequestStatus.ResolvingHost, "Resolving" },
                { UrlRequestStatus.Connecting, "Connecting" },
                { UrlRequestStatus.SslHandshake, "SSL/TLS" },
                { UrlRequestStatus.SendingRequest, "Uploading" },
                { UrlRequestStatus.WaitingForResponse, "Waiting" },
                { UrlRequestStatus.ReadingResponse, "Downloading" }
            };

            using var statusListener = new UrlRequestStatusListener(status =>
            {
                int index = Array.IndexOf(stages, status);
                if (index >= 0 && index > currentStage)
                {
                    currentStage = index;
                    int percentage = (currentStage + 1) * 100 / stages.Length;
                    string bar = new string('█', percentage / 5) + new string('░', 20 - percentage / 5);
                    string stageName = stageNames[status];
                    Console.WriteLine($"  [{bar}] {percentage,3}% - {stageName}");
                }
            });

            // Simulate request progression
            foreach (var stage in stages)
            {
                statusListener.OnStatus(stage);
                Thread.Sleep(150);
            }

            Thread.Sleep(200); // Allow final callback to complete
            Console.WriteLine("  Request completed!");
        }

        /// <summary>
        /// Helper method to create a visual progress bar
        /// </summary>
        private static string CreateProgressBar(int percentage, int width = 20)
        {
            int filled = percentage * width / 100;
            return new string('█', filled) + new string('░', width - filled);
        }
    }
}
