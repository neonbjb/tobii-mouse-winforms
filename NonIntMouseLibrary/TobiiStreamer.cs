using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tobii.StreamEngine;
using System.Threading;

namespace TobiiNoMouse
{
    public class TobiiStreamer
    {
        IntPtr tobiiApiPtr = IntPtr.Zero;
        IntPtr tobiiEnginePtr = IntPtr.Zero;
        IntPtr deviceHandle = IntPtr.Zero;
        Thread tobiiThread;
        bool isStreaming = false;
        public bool isInitialized { get; set; } = false;
        public bool init()
        {
            // Initialize the Tobii API.
            tobii_custom_log_t customLog = new tobii_custom_log_t();
            customLog.log_func = new Interop.tobii_log_func_t(tobiiLogOut);
            customLog.log_context = IntPtr.Zero;
            tobii_error_t err = Interop.tobii_api_create(out tobiiApiPtr, customLog);
            if(err != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                Console.WriteLine("Failed to initialize stream api: " + err);
                return false;
            }

            err = Interop.tobii_engine_create(tobiiApiPtr, out tobiiEnginePtr);
            if (err != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                Console.WriteLine("Failed to initialize stream engine: " + err);
                cleanup();
                return false;
            }

            // Find a device to attach to.
            List<tobii_enumerated_device_t> devices;
            err = Interop.tobii_enumerate_devices(tobiiEnginePtr, out devices);
            if (err != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                Console.WriteLine("Failed to enumerate devices: " + err);
                cleanup();
                return false;
            }
            if(devices.Count == 0)
            {
                Console.WriteLine("No tobii devices available.");
                cleanup();
                return false;
            }

            tobii_enumerated_device_t enumDev = devices.First();
            deviceHandle = IntPtr.Zero;
            err = Interop.tobii_device_create(tobiiApiPtr, enumDev.url, out deviceHandle);
            if (err != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                Console.WriteLine("Failed to open devices: " + err);
                cleanup();
                return false;
            }

            // Register callbacks.
            err = Interop.tobii_gaze_point_subscribe(deviceHandle, new tobii_gaze_point_callback_t(gazePointCallback));
            if(err != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                Console.WriteLine("Failed to subscribe to gaze points.");
                cleanup();
                return false;
            }

            isInitialized = true;
            return true;
        }

        public void start()
        {
            if(!isInitialized)
            {
                Console.Write("Tobii: Not initialized.");
                return;
            }

            if(tobiiThread != null && tobiiThread.IsAlive)
            {
                Console.WriteLine("Tobii tracker thread already running.");
                return;
            }

            tobiiThread = new Thread(tobiiRunFn);
            isStreaming = true;
            tobiiThread.Start();
        }

        public void stop()
        {
            isStreaming = false;
            if (tobiiThread != null && tobiiThread.IsAlive)
            {
                tobiiThread.Join();
            }
        }

        public void cleanup()
        {
            stop();
            if(deviceHandle != IntPtr.Zero)
            {
                Interop.tobii_device_destroy(deviceHandle);
                deviceHandle = IntPtr.Zero;
            }
            if(tobiiEnginePtr != IntPtr.Zero)
            {
                Interop.tobii_engine_destroy(tobiiEnginePtr);
                tobiiEnginePtr = IntPtr.Zero;
            }
            if(tobiiApiPtr != IntPtr.Zero)
            {
                Interop.tobii_api_destroy(tobiiApiPtr);
                tobiiApiPtr = IntPtr.Zero;
            }
        }

        public void tobiiRunFn()
        {
            const int MAX_RETRIES = 5;
            int retriesLeft = MAX_RETRIES;
            IntPtr[] devices = { deviceHandle };
            Interop.tobii_device_clear_callback_buffers(deviceHandle);
            Interop.tobii_engine_clear_callback_buffers(tobiiEnginePtr);
            while (isStreaming)
            {
                tobii_error_t err = Interop.tobii_wait_for_callbacks(tobiiEnginePtr, devices);
                switch(err)
                {
                    case tobii_error_t.TOBII_ERROR_CONNECTION_FAILED:
                        Console.WriteLine("Connection failed. Attempting reconnect. " + retriesLeft + " retries left.");
                        tobii_error_t cErr = Interop.tobii_device_reconnect(deviceHandle);
                        if(cErr != tobii_error_t.TOBII_ERROR_NO_ERROR)
                        {
                            Console.WriteLine("Failed reconnect. Pausing briefly before another attempt." + cErr);
                            Thread.Sleep(500);
                        }
                        break;
                    case tobii_error_t.TOBII_ERROR_TIMED_OUT:
                        // expected. no-op.
                        break;
                    case tobii_error_t.TOBII_ERROR_NO_ERROR:
                        Interop.tobii_device_process_callbacks(deviceHandle);
                        Interop.tobii_engine_process_callbacks(tobiiEnginePtr);
                        retriesLeft = MAX_RETRIES;
                        break;
                    default:
                        Console.WriteLine("Unknown error. Retrying " + retriesLeft + " more times.");
                        if(retriesLeft-- <= 0)
                        {
                            isStreaming = false;
                        }
                        break;
                }
            }
            Console.WriteLine("Tobii: Exiting run thread.");
        }

        private void gazePointCallback(ref tobii_gaze_point_t gaze_point)
        { 
            gazePointEvent.Invoke(this, new GazeEventArgs(gaze_point.position.x, gaze_point.position.y));
        }

        public class GazeEventArgs : EventArgs
        {
            public float x { get; }
            public float y { get; }

            public GazeEventArgs(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
        }
        public event EventHandler<GazeEventArgs> gazePointEvent;

        private void tobiiLogOut(IntPtr log_context, tobii_log_level_t level, string text)
        {
            Console.WriteLine("[tobii]: " + text);
        }
    }
}
